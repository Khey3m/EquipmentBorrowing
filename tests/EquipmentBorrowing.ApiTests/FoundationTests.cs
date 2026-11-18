using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace EquipmentBorrowing.ApiTests;

public sealed class FoundationTests(ITestOutputHelper output)
{
    [ApiPostgresFact]
    public async Task SeededHostUsesRealRoutesAndExactRelativeFixture()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        using var response = await scenario.Client.GetAsync("/api/v1/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("CampusEQ API", (await response.Content.ReadFromJsonAsync<ApiStatus>())!.Service);
        using var missing = await scenario.Client.GetAsync("/lab6-unknown-route");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("not_found", (await missing.Content.ReadFromJsonAsync<ApiError>())!.Code);

        Assert.Equal(new[] { "ST-100", "ST-200", "ST-300", "ST-400" }, scenario.Students.Keys.Order());
        Assert.Equal(new[] { "Ada", "Ben", "Cara", "Dani" }, scenario.Students.Values.OrderBy(s => s.StudentNumber).Select(s => s.FullName));
        Assert.False(scenario.Students["ST-200"].IsEligible);
        Assert.All(scenario.Students.Values.Where(s => s.StudentNumber != "ST-200"), s => Assert.True(s.IsEligible));
        Assert.Equal(new[] { "Camera", "Laptop", "Projector", "Tablet", "Microphone", "Tripod", "Speaker" },
            scenario.Equipment.Values.OrderBy(e => e.EquipmentCode).Select(e => e.Name));
        Assert.Equal(new[] { "EQ-002", "EQ-003", "EQ-006", "EQ-007" },
            scenario.Equipment.Values.Where(e => e.IsAvailable).Select(e => e.EquipmentCode).Order());
        await using var fresh = scenario.CreateDbContext();
        Assert.Equal(new[] { "20261114000100_InitialPostgres" }, await fresh.Database.GetAppliedMigrationsAsync());
        var rows = await fresh.Borrowings.AsNoTracking().ToListAsync();
        Assert.Equal(4, rows.Count);
        foreach (var (student, code, borrowed, due, returned) in new (string, string, int, int, int?)[]
        {
            ("ST-100", "EQ-001", -9, -1, null), ("ST-100", "EQ-003", -8, 2, -2),
            ("ST-300", "EQ-004", -7, 2, null), ("ST-300", "EQ-005", -7, 2, null)
        })
        {
            var row = Assert.Single(rows, b => b.EquipmentId == scenario.Equipment[code].Id);
            Assert.Equal(scenario.Students[student].Id, row.StudentId);
            Assert.Equal(ApiScenario.D.AddDays(borrowed), row.BorrowedAt);
            Assert.Equal(ApiScenario.D.AddDays(due), row.ExpectedReturnAt);
            Assert.Equal(returned is int offset ? ApiScenario.D.AddDays(offset) : (DateOnly?)null, row.ActualReturnAt);
            Assert.Equal(returned is null ? BorrowingStatus.Active : BorrowingStatus.Returned, row.Status);
        }
        output.WriteLine("Real pipeline: GET status=200 JSON, unknown route=404/not_found; exact fixture: 4 students, 7 items, 4 loans and migration verified.");
        output.WriteLine("Fresh physical PostgreSQL read: " + await scenario.SnapshotAsync());
    }

    [ApiPostgresFact]
    public async Task FixedClockAndResetRestoreFixtureAfterMutationAndEmptyState()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var baseline = await scenario.SnapshotAsync();
        using var response = await scenario.Client.PostAsJsonAsync("/api/v1/borrowings", new BorrowInput(
            scenario.Students["ST-400"].Id, scenario.Equipment["EQ-002"].Id, ApiScenario.D.AddDays(2)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var loan = (await response.Content.ReadFromJsonAsync<BorrowingResponse>())!;
        Assert.Equal(ApiScenario.D, loan.BorrowedDate); // Verifies clock replacement in the actual HTTP host.
        Assert.NotEqual(baseline, await scenario.SnapshotAsync());

        await scenario.ResetAsync(seed: false);
        Assert.Empty(await scenario.GetAsync<StudentResponse[]>("/api/v1/students"));
        Assert.Empty(await scenario.GetAsync<EquipmentResponse[]>("/api/v1/equipment"));
        using (var empty = JsonDocument.Parse(await scenario.SnapshotAsync()))
            foreach (var table in new[] { "students", "equipment", "borrowings" })
                Assert.Equal(0, empty.RootElement.GetProperty(table).GetArrayLength());
        await scenario.ResetAsync();
        Assert.Equal(baseline, await scenario.SnapshotAsync());
        output.WriteLine($"POST borrowing=201 with fixed D={loan.BorrowedDate:yyyy-MM-dd}; empty reset clears all 3 tables; reseed restores exact baseline.");
    }

    [ApiPostgresFact]
    public async Task SeparateScenariosRemainIndependentAndOwnedDatabasesAreRemoved()
    {
        var first = await ApiScenario.CreateAsync(output);
        try
        {
            await using var second = await ApiScenario.CreateAsync(output);
            Assert.NotEqual(first.DatabaseName, second.DatabaseName);
            var secondBaseline = await second.SnapshotAsync();
            await first.ResetAsync(seed: false);
            Assert.Empty(first.Students);
            Assert.Equal(4, second.Students.Count);
            Assert.Equal(secondBaseline, await second.SnapshotAsync());
            output.WriteLine("Two simultaneous databases/hosts: clearing first leaves second's complete fixture unchanged.");
        }
        finally { await first.DisposeAsync(); }
        Assert.False(await first.DatabaseExistsAsync());
        await first.DisposeAsync(); // Idempotent cleanup.
        output.WriteLine("Independent admin connection confirms first owned database was removed; repeated disposal is safe.");
    }
}
