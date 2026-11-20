using System.Net;
using System.Net.Http.Json;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace EquipmentBorrowing.ApiTests;

public sealed class ConstraintApiTests(ITestOutputHelper output)
{
    [ApiPostgresFact, Trait("TC6", "E01")]
    public async Task E01_NormalizedIdentifiersAreUniqueThroughApiAndPostgres()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var student = scenario.Students["ST-400"];
        var equipment = scenario.Equipment["EQ-002"];

        await Reject(scenario, () => scenario.Client.PostAsJsonAsync("/api/v1/students",
            new StudentInput("  st-100  ", "Duplicate", null)), "duplicate_identifier");
        await Reject(scenario, () => scenario.Client.PutAsJsonAsync($"/api/v1/students/{student.Id}",
            new StudentUpdateInput("  st-100  ", student.FullName, null)), "duplicate_identifier");
        await Reject(scenario, () => scenario.Client.PostAsJsonAsync("/api/v1/equipment",
            new EquipmentInput("  eq-001  ", "Duplicate", null)), "duplicate_identifier");
        await Reject(scenario, () => scenario.Client.PutAsJsonAsync($"/api/v1/equipment/{equipment.Id}",
            new EquipmentUpdateInput("  eq-001  ", equipment.Name, null)), "duplicate_identifier");

        await RejectSql(scenario, """
            INSERT INTO "Students" ("StudentNumber", "FullName", "IsEligibleToBorrow")
            VALUES ('st-100', 'Duplicate', true)
            """, PostgresErrorCodes.UniqueViolation, "IX_Students_NormalizedStudentNumber");
        await RejectSql(scenario, """
            INSERT INTO "Equipment" ("EquipmentIdentifier", "Name", "IsAvailable")
            VALUES ('eq-001', 'Duplicate', true)
            """, PostgresErrorCodes.UniqueViolation, "IX_Equipment_NormalizedEquipmentIdentifier");

        await using var fresh = scenario.CreateDbContext();
        Assert.Equal(4, await fresh.Students.CountAsync());
        Assert.Equal(7, await fresh.Equipment.CountAsync());
    }

    [ApiPostgresFact, Trait("TC6", "E05")]
    public async Task E05_StaleSelectionsAndSecondActiveLoanAreRejected()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        const string loans = "/api/v1/borrowings";
        var laptop = scenario.Equipment["EQ-002"];
        var tripod = scenario.Equipment["EQ-006"];
        var firstStudent = scenario.Students["ST-400"].Id;
        var staleStudent = scenario.Students["ST-100"].Id;
        var due = ApiScenario.D.AddDays(2);
        var available = await scenario.GetAsync<EquipmentResponse[]>("/api/v1/equipment?available=true");
        Assert.Contains(laptop, available);
        Assert.Contains(tripod, available);

        using var otherClient = scenario.CreateClient();
        using var issue = await otherClient.PostAsJsonAsync(loans, new BorrowInput(firstStudent, laptop.Id, due));
        Assert.Equal(HttpStatusCode.Created, issue.StatusCode);
        var original = Assert.IsType<BorrowingResponse>(await issue.Content.ReadFromJsonAsync<BorrowingResponse>());
        output.WriteLine($"Independent client issued equipment {laptop.Id} with HTTP 201 and loan ID {original.Id}.");

        await Reject(scenario, () => scenario.Client.PostAsJsonAsync(loans,
            new BorrowInput(staleStudent, laptop.Id, due)), "business_conflict");
        await Reject(scenario, () => scenario.Client.PostAsJsonAsync("/api/v1/checkout-requests",
            new CheckoutInput(staleStudent, [tripod.Id, laptop.Id], due)), "business_conflict");

        const string insert = """
            INSERT INTO "Borrowings" ("StudentId", "EquipmentId", "BorrowedAt", "ExpectedReturnAt", "Status")
            VALUES (@student, @equipment, @borrowed, @due, 'Active')
            """;
        void Bind(NpgsqlCommand command, int student, int equipment, DateOnly expected)
        {
            command.Parameters.AddWithValue("student", student);
            command.Parameters.AddWithValue("equipment", equipment);
            command.Parameters.AddWithValue("borrowed", ApiScenario.D);
            command.Parameters.AddWithValue("due", expected);
        }

        await RejectSql(scenario, insert, PostgresErrorCodes.UniqueViolation, "IX_Borrowings_EquipmentId",
            command => Bind(command, staleStudent, laptop.Id, due));
        await RejectSql(scenario, insert, PostgresErrorCodes.ForeignKeyViolation, "FK_Borrowings_Students_StudentId",
            command => Bind(command, int.MaxValue, tripod.Id, due));
        await RejectSql(scenario, insert, PostgresErrorCodes.ForeignKeyViolation, "FK_Borrowings_Equipment_EquipmentId",
            command => Bind(command, staleStudent, int.MaxValue, due));
        await RejectSql(scenario, insert, PostgresErrorCodes.CheckViolation, "CK_Borrowings_Due",
            command => Bind(command, staleStudent, tripod.Id, ApiScenario.D));

        await using var fresh = scenario.CreateDbContext();
        var active = Assert.Single(await fresh.Borrowings.AsNoTracking()
            .Where(row => row.EquipmentId == laptop.Id && row.Status == BorrowingStatus.Active).ToListAsync());
        Assert.Equal(original.Id, active.Id);
        Assert.Equal(firstStudent, active.StudentId);
        Assert.False((await fresh.Equipment.SingleAsync(row => row.Id == laptop.Id)).IsAvailable);
        Assert.True((await fresh.Equipment.SingleAsync(row => row.Id == tripod.Id)).IsAvailable);
        Assert.False(await fresh.Borrowings.AnyAsync(row => row.EquipmentId == tripod.Id));
    }

    private async Task Reject(ApiScenario scenario, Func<Task<HttpResponseMessage>> send, string expectedCode)
    {
        var before = await scenario.SnapshotAsync();
        using var response = await send();
        var body = await response.Content.ReadAsStringAsync();
        output.WriteLine($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.AbsolutePath} => {(int)response.StatusCode} {body}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = Assert.IsType<ApiError>(await response.Content.ReadFromJsonAsync<ApiError>());
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(before, await scenario.SnapshotAsync());
        output.WriteLine("Fresh PostgreSQL snapshot unchanged after HTTP rejection.");
    }

    private async Task RejectSql(ApiScenario scenario, string sql, string sqlState, string constraint, Action<NpgsqlCommand>? bind = null)
    {
        var before = await scenario.SnapshotAsync();
        await using (var connection = scenario.CreateFreshConnection())
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            bind?.Invoke(command);
            var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal(sqlState, error.SqlState);
            Assert.Equal(constraint, error.ConstraintName);
            output.WriteLine($"PostgreSQL rejected statement with SQLSTATE {error.SqlState} and constraint {error.ConstraintName}.");
        }
        Assert.Equal(before, await scenario.SnapshotAsync());
    }
}
