using System.Net;
using System.Net.Http.Json;
using EquipmentBorrowing.Contracts;
using EquipmentBorrowing.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace EquipmentBorrowing.ApiTests;

public sealed class LifecycleApiTests(ITestOutputHelper output)
{
    private const string Loans = "/api/v1/borrowings";
    private const string Checkout = "/api/v1/checkout-requests";
    private static readonly DateOnly Due = ApiScenario.D.AddDays(2);

    [ApiPostgresFact, Trait("TC6", "U03")]
    public async Task U03_IssueAndFollowLocation()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var student = scenario.Students["ST-400"].Id;
        var item = scenario.Equipment["EQ-002"].Id;
        using var response = await scenario.Client.PostAsJsonAsync(Loans, new BorrowInput(student, item, Due));
        var loan = await Read<BorrowingResponse>(response, HttpStatusCode.Created);
        Assert.Equal($"{Loans}/{loan.Id}", response.Headers.Location?.ToString());
        using var detail = await scenario.Client.GetAsync(response.Headers.Location!);
        Assert.Equal(loan, await Read<BorrowingResponse>(detail, HttpStatusCode.OK));
        await AssertIssued(scenario, loan, student, item);
        await using var fresh = scenario.CreateDbContext();
        Assert.Equal(5, await fresh.Borrowings.CountAsync());
    }

    [ApiPostgresFact, Trait("TC6", "U05")]
    public async Task U05_ReturnRetainsHistoryAndRepeatLeavesStateUntouched()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var student = scenario.Students["ST-100"].Id;
        var item = scenario.Equipment["EQ-001"].Id;
        var historyPath = $"/api/v1/students/{student}/borrowings";
        var history = await scenario.GetAsync<BorrowingResponse[]>(historyPath);
        var original = Assert.Single(history, row => row.EquipmentId == item);
        Assert.Equal(BorrowingState.Active, original.Status);
        var path = $"{Loans}/{original.Id}/return";
        using var response = await scenario.Client.PostAsJsonAsync(path, new ReturnInput());
        var returned = await Read<BorrowingResponse>(response, HttpStatusCode.OK);
        var expected = original with { ReturnedDate = ApiScenario.D, Status = BorrowingState.Returned };
        Assert.Equal(expected with { StudentNumber = null, StudentName = null, EquipmentCode = null, EquipmentName = null }, returned);
        Assert.Equal(history.Select(row => row.Id == original.Id ? expected : row).OrderBy(row => row.Id),
            (await scenario.GetAsync<BorrowingResponse[]>(historyPath)).OrderBy(row => row.Id));
        await using (var fresh = scenario.CreateDbContext())
        {
            var saved = await fresh.Borrowings.AsNoTracking().SingleAsync(row => row.Id == original.Id);
            Assert.Equal((student, item, original.BorrowedDate, original.ExpectedReturnDate, BorrowingStatus.Returned, (DateOnly?)ApiScenario.D),
                (saved.StudentId, saved.EquipmentId, saved.BorrowedAt, saved.ExpectedReturnAt, saved.Status, saved.ActualReturnAt));
            Assert.True((await fresh.Equipment.SingleAsync(row => row.Id == item)).IsAvailable);
            Assert.Equal(4, await fresh.Borrowings.CountAsync());
        }
        var before = await scenario.SnapshotAsync();
        using var repeat = await scenario.Client.PostAsJsonAsync(path, new ReturnInput());
        await AssertConflict(scenario, repeat, before);
    }

    [ApiPostgresFact, Trait("TC6", "U06")]
    public async Task U06_CheckoutPersistsBothLoansAndAvailability()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var student = scenario.Students["ST-400"].Id;
        int[] items = [scenario.Equipment["EQ-002"].Id, scenario.Equipment["EQ-006"].Id];
        using var response = await scenario.Client.PostAsJsonAsync(Checkout, new CheckoutInput(student, items, Due));
        var result = await Read<CheckoutResponse>(response, HttpStatusCode.Created);
        await AssertCheckout(scenario, result, student, items);
        await using var fresh = scenario.CreateDbContext();
        Assert.Equal(6, await fresh.Borrowings.CountAsync());
    }

    [ApiPostgresFact, Trait("TC6", "E02")]
    public async Task E02_SingleAtLimitSucceeds()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var student = scenario.Students["ST-300"].Id;
        var item = scenario.Equipment["EQ-002"].Id;
        var existing = await AssertActiveCount(scenario, student, 2);
        using var response = await scenario.Client.PostAsJsonAsync(Loans, new BorrowInput(student, item, Due));
        var loan = await Read<BorrowingResponse>(response, HttpStatusCode.Created);
        await AssertIssued(scenario, loan, student, item);
        Assert.Equal(existing.Append(loan.Id).Order(), await AssertActiveCount(scenario, student, 3));
    }

    [ApiPostgresFact, Trait("TC6", "E02")]
    public async Task E02_MultipleAtLimitSucceeds()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var student = scenario.Students["ST-100"].Id;
        var existing = await AssertActiveCount(scenario, student, 1);
        int[] items = [scenario.Equipment["EQ-002"].Id, scenario.Equipment["EQ-006"].Id];
        using var response = await scenario.Client.PostAsJsonAsync(Checkout, new CheckoutInput(student, items, Due));
        var result = await Read<CheckoutResponse>(response, HttpStatusCode.Created);
        await AssertCheckout(scenario, result, student, items);
        Assert.Equal(existing.Concat(result.Borrowings.Select(row => row.Id)).Order(), await AssertActiveCount(scenario, student, 3));
    }

    [ApiPostgresFact, Trait("TC6", "E02")]
    public async Task E02_MultipleOverLimitLeavesBothItemsAvailable()
    {
        await using var scenario = await ApiScenario.CreateAsync(output);
        var student = scenario.Students["ST-300"].Id;
        var existing = await AssertActiveCount(scenario, student, 2);
        int[] items = [scenario.Equipment["EQ-002"].Id, scenario.Equipment["EQ-006"].Id];
        var before = await scenario.SnapshotAsync();
        using var response = await scenario.Client.PostAsJsonAsync(Checkout, new CheckoutInput(student, items, Due));
        await AssertConflict(scenario, response, before);
        Assert.Equal(existing, await AssertActiveCount(scenario, student, 2));
        await using var fresh = scenario.CreateDbContext();
        Assert.Equal(2, await fresh.Equipment.CountAsync(row => items.Contains(row.Id) && row.IsAvailable));
    }

    private static async Task AssertIssued(ApiScenario scenario, BorrowingResponse loan, int student, int item)
    {
        Assert.True(loan.Id > 0);
        Assert.Equal((student, item, ApiScenario.D, Due, BorrowingState.Active, (DateOnly?)null),
            (loan.StudentId, loan.EquipmentId, loan.BorrowedDate, loan.ExpectedReturnDate, loan.Status, loan.ReturnedDate));
        await using var fresh = scenario.CreateDbContext();
        var saved = await fresh.Borrowings.AsNoTracking().SingleAsync(row => row.Id == loan.Id);
        Assert.Equal((student, item, ApiScenario.D, Due, BorrowingStatus.Active, (DateOnly?)null),
            (saved.StudentId, saved.EquipmentId, saved.BorrowedAt, saved.ExpectedReturnAt, saved.Status, saved.ActualReturnAt));
        Assert.False((await fresh.Equipment.SingleAsync(row => row.Id == item)).IsAvailable);
    }

    private static async Task AssertCheckout(ApiScenario scenario, CheckoutResponse result, int student, int[] items)
    {
        Assert.Equal(items.Order(), result.Borrowings.Select(row => row.EquipmentId).Order());
        Assert.Equal(items.Length, result.Borrowings.Select(row => row.Id).Distinct().Count());
        foreach (var loan in result.Borrowings)
            await AssertIssued(scenario, loan, student, loan.EquipmentId);
    }

    private async Task<int[]> AssertActiveCount(ApiScenario scenario, int student, int count)
    {
        await using var fresh = scenario.CreateDbContext();
        var ids = await fresh.Borrowings.Where(row => row.StudentId == student && row.Status == BorrowingStatus.Active)
            .Select(row => row.Id).OrderBy(id => id).ToArrayAsync();
        Assert.Equal(count, ids.Length);
        output.WriteLine($"Fresh PostgreSQL active loan IDs for student {student}: {string.Join(", ", ids)}");
        return ids;
    }

    private async Task AssertConflict(ApiScenario scenario, HttpResponseMessage response, string before)
    {
        var error = await Read<ApiError>(response, HttpStatusCode.Conflict);
        Assert.False(string.IsNullOrWhiteSpace(error.Code));
        var after = await scenario.SnapshotAsync();
        output.WriteLine($"PostgreSQL before: {before}\nPostgreSQL after: {after}");
        Assert.Equal(before, after);
    }

    private async Task<T> Read<T>(HttpResponseMessage response, HttpStatusCode status)
    {
        output.WriteLine($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.PathAndQuery} => {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        return Assert.IsType<T>(await response.Content.ReadFromJsonAsync<T>());
    }
}
