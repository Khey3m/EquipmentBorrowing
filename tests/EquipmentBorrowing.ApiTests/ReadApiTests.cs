using System.Net;
using System.Text.Json;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace EquipmentBorrowing.ApiTests;

public sealed class ReadApiTests(ITestOutputHelper output)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly (string Code, string Name, bool Available)[] Catalog =
    [
        ("EQ-001", "Camera", false), ("EQ-002", "Laptop", true), ("EQ-003", "Projector", true),
        ("EQ-004", "Tablet", false), ("EQ-005", "Microphone", false),
        ("EQ-006", "Tripod", true), ("EQ-007", "Speaker", true)
    ];

    [ApiPostgresFact, Trait("TC6", "U01")]
    public async Task U01_CatalogReturnsExactFixtureAndEmptyArray()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var items = await ReadUnchangedAsync<EquipmentResponse[]>(scenario, "/api/v1/equipment");
        Assert.Equal(7, items.Length);
        Assert.Equal(Catalog.Select(e => e.Code), items.Select(e => e.EquipmentCode).Order());
        Assert.Equal(7, items.Select(e => e.Id).Distinct().Count());
        foreach (var expected in Catalog)
        {
            var item = Assert.Single(items, e => e.EquipmentCode == expected.Code);
            Assert.True(item.Id > 0);
            Assert.Equal(scenario.Equipment[expected.Code].Id, item.Id);
            Assert.Equal(expected.Name, item.Name);
            Assert.Equal(expected.Available, item.IsAvailable);
            Assert.Null(item.Description);
        }

        await scenario.ResetAsync(seed: false);
        Assert.Empty(await ReadUnchangedAsync<EquipmentResponse[]>(scenario, "/api/v1/equipment"));
    }

    [ApiPostgresFact, Trait("TC6", "U02")]
    public async Task U02_AvailableFilterIncludesOnlyAvailableItemsAndHandlesNone()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var available = await ReadUnchangedAsync<EquipmentResponse[]>(scenario, "/api/v1/equipment?available=true");
        Assert.Equal(new[] { "EQ-002", "EQ-003", "EQ-006", "EQ-007" }, available.Select(e => e.EquipmentCode).Order());
        Assert.All(available, e =>
        {
            Assert.True(e.IsAvailable);
            Assert.Equal(scenario.Equipment[e.EquipmentCode].Id, e.Id);
            Assert.Equal(Catalog.Single(c => c.Code == e.EquipmentCode).Name, e.Name);
        });
        Assert.DoesNotContain(available, e => new[] { "EQ-001", "EQ-004", "EQ-005" }.Contains(e.EquipmentCode));

        // Reset before the none-available subcase, then prepare consistent active loans in PostgreSQL.
        await scenario.ResetAsync();
        await using (var setup = scenario.CreateDbContext())
        {
            foreach (var (student, code) in new[]
            {
                ("ST-400", "EQ-002"), ("ST-400", "EQ-003"), ("ST-400", "EQ-006"), ("ST-100", "EQ-007")
            })
            {
                var item = await setup.Equipment.SingleAsync(e => e.Id == scenario.Equipment[code].Id);
                item.MarkAsBorrowed();
                setup.Add(new Borrowing(scenario.Students[student].Id, item.Id, ApiScenario.D.AddDays(-1), ApiScenario.D.AddDays(2)));
            }
            await setup.SaveChangesAsync();
        }
        await using (var fresh = scenario.CreateDbContext())
        {
            Assert.Equal(7, await fresh.Equipment.CountAsync());
            Assert.Equal(0, await fresh.Equipment.CountAsync(e => e.IsAvailable));
            Assert.Equal(7, await fresh.Borrowings.CountAsync(b => b.Status == BorrowingStatus.Active));
        }
        Assert.Empty(await ReadUnchangedAsync<EquipmentResponse[]>(scenario, "/api/v1/equipment?available=true"));
        var all = await ReadUnchangedAsync<EquipmentResponse[]>(scenario, "/api/v1/equipment");
        Assert.Equal(7, all.Length);
        Assert.All(all, e => Assert.False(e.IsAvailable));
    }

    [ApiPostgresFact, Trait("TC6", "U04")]
    public async Task U04_ActiveLoansHaveExactIdsAndRelatedRecordsWithoutReturnedLoan()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var rows = await ReadUnchangedAsync<BorrowingResponse[]>(scenario, "/api/v1/borrowings?status=Active");
        Assert.Equal(new[] { "EQ-001", "EQ-004", "EQ-005" }, rows.Select(b => b.EquipmentCode).Order());
        Assert.Equal(3, rows.Select(b => b.Id).Distinct().Count());
        Assert.DoesNotContain(rows, b => b.EquipmentId == scenario.Equipment["EQ-003"].Id);
        await AssertLoanAsync(scenario, Assert.Single(rows, b => b.EquipmentCode == "EQ-001"), "ST-100", "EQ-001", -9, -1);
        await AssertLoanAsync(scenario, Assert.Single(rows, b => b.EquipmentCode == "EQ-004"), "ST-300", "EQ-004", -7, 2);
        await AssertLoanAsync(scenario, Assert.Single(rows, b => b.EquipmentCode == "EQ-005"), "ST-300", "EQ-005", -7, 2);
        await using var fresh = scenario.CreateDbContext();
        Assert.Equal(await fresh.Borrowings.AsNoTracking().Where(b => b.Status == BorrowingStatus.Active).Select(b => b.Id).OrderBy(id => id).ToArrayAsync(),
            rows.Select(b => b.Id).Order());
    }

    [ApiPostgresFact, Trait("TC6", "U07")]
    public async Task U07_StudentHistoryDistinguishesMixedEmptyAndMissingSubjects()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var s1 = scenario.Students["ST-100"];
        Assert.Equal(s1, await ReadUnchangedAsync<StudentResponse>(scenario, $"/api/v1/students/{s1.Id}"));
        var history = await ReadUnchangedAsync<BorrowingResponse[]>(scenario, $"/api/v1/students/{s1.Id}/borrowings");
        Assert.Equal(new[] { "EQ-001", "EQ-003" }, history.Select(b => b.EquipmentCode).Order());
        Assert.Equal(2, history.Select(b => b.Id).Distinct().Count());
        await AssertLoanAsync(scenario, Assert.Single(history, b => b.EquipmentCode == "EQ-001"), "ST-100", "EQ-001", -9, -1);
        await AssertLoanAsync(scenario, Assert.Single(history, b => b.EquipmentCode == "EQ-003"), "ST-100", "EQ-003", -8, 2, -2);

        await scenario.ResetAsync();
        var s4 = scenario.Students["ST-400"];
        Assert.Equal(s4, await ReadUnchangedAsync<StudentResponse>(scenario, $"/api/v1/students/{s4.Id}"));
        Assert.Empty(await ReadUnchangedAsync<BorrowingResponse[]>(scenario, $"/api/v1/students/{s4.Id}/borrowings"));

        await scenario.ResetAsync();
        Assert.DoesNotContain(scenario.Students.Values, s => s.Id == int.MaxValue);
        await AssertMissingAsync(scenario, $"/api/v1/students/{int.MaxValue}");
        await AssertMissingAsync(scenario, $"/api/v1/students/{int.MaxValue}/borrowings");
    }

    [ApiPostgresFact, Trait("TC6", "U08")]
    public async Task U08_EquipmentHistoryDistinguishesActiveReturnedEmptyAndMissingSubjects()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        foreach (var code in new[] { "EQ-001", "EQ-003", "EQ-002" })
        {
            await scenario.ResetAsync();
            var item = scenario.Equipment[code];
            Assert.Equal(item, await ReadUnchangedAsync<EquipmentResponse>(scenario, $"/api/v1/equipment/{item.Id}"));
            var rows = await ReadUnchangedAsync<BorrowingResponse[]>(scenario, $"/api/v1/equipment/{item.Id}/borrowings");
            if (code == "EQ-002") Assert.Empty(rows);
            else if (code == "EQ-001") await AssertLoanAsync(scenario, Assert.Single(rows), "ST-100", code, -9, -1);
            else await AssertLoanAsync(scenario, Assert.Single(rows), "ST-100", code, -8, 2, -2);
        }

        await scenario.ResetAsync();
        Assert.DoesNotContain(scenario.Equipment.Values, e => e.Id == int.MaxValue);
        await AssertMissingAsync(scenario, $"/api/v1/equipment/{int.MaxValue}");
        await AssertMissingAsync(scenario, $"/api/v1/equipment/{int.MaxValue}/borrowings");
    }

    private async Task<T> ReadUnchangedAsync<T>(ApiScenario scenario, string path, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var before = await scenario.SnapshotAsync();
        using var response = await scenario.Client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        var after = await scenario.SnapshotAsync();
        output.WriteLine($"GET {path} => {(int)response.StatusCode} {response.Content.Headers.ContentType}; {body}");
        output.WriteLine("PostgreSQL before: " + before);
        output.WriteLine("PostgreSQL after: " + after);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(before, after); // All rows/fields in all three tables, not only result counts.
        return Assert.IsType<T>(JsonSerializer.Deserialize<T>(body, Json));
    }

    private async Task AssertMissingAsync(ApiScenario scenario, string path)
    {
        var error = await ReadUnchangedAsync<ApiError>(scenario, path, HttpStatusCode.NotFound);
        Assert.Equal("not_found", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    private static async Task AssertLoanAsync(ApiScenario scenario, BorrowingResponse row, string student, string code,
        int borrowedOffset, int dueOffset, int? returnedOffset = null)
    {
        Assert.True(row.Id > 0);
        Assert.Equal(scenario.Students[student].Id, row.StudentId);
        Assert.Equal(scenario.Equipment[code].Id, row.EquipmentId);
        Assert.Equal(student, row.StudentNumber);
        Assert.Equal(student == "ST-100" ? "Ada" : "Cara", row.StudentName);
        Assert.Equal(code, row.EquipmentCode);
        Assert.Equal(Catalog.Single(c => c.Code == code).Name, row.EquipmentName);
        Assert.Equal(ApiScenario.D.AddDays(borrowedOffset), row.BorrowedDate);
        Assert.Equal(ApiScenario.D.AddDays(dueOffset), row.ExpectedReturnDate);
        Assert.Equal(returnedOffset is int offset ? ApiScenario.D.AddDays(offset) : (DateOnly?)null, row.ReturnedDate);
        Assert.Equal(returnedOffset is null ? BorrowingState.Active : BorrowingState.Returned, row.Status);

        await using var fresh = scenario.CreateDbContext();
        var persisted = await fresh.Borrowings.AsNoTracking().SingleAsync(b => b.Id == row.Id);
        Assert.Equal(row.StudentId, persisted.StudentId);
        Assert.Equal(row.EquipmentId, persisted.EquipmentId);
        Assert.Equal(row.BorrowedDate, persisted.BorrowedAt);
        Assert.Equal(row.ExpectedReturnDate, persisted.ExpectedReturnAt);
        Assert.Equal(row.ReturnedDate, persisted.ActualReturnAt);
        Assert.Equal(returnedOffset is null ? BorrowingStatus.Active : BorrowingStatus.Returned, persisted.Status);
    }
}
