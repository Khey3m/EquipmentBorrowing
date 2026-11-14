using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Tests;

public class BorrowingSearchTests
{
    private sealed class Queries : IBorrowingSearchQueries
    {
        public BorrowingSearchRequest? Last { get; private set; }
        public bool Fail { get; set; }
        public Task<IReadOnlyList<BorrowingDetails>> SearchAsync(BorrowingSearchRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Fail) throw new IOException("sensitive connection details");
            Last = request;
            return Task.FromResult<IReadOnlyList<BorrowingDetails>>([]);
        }
    }
    [Fact]
    public async Task CriteriaAreTrimmedAndCombinedWithoutDroppingOptions()
    {
        var queries = new Queries(); var service = new BorrowingSearchService(queries);
        var result = await service.SearchAsync(new(" Alice ", BorrowingStatus.Returned, BorrowingSortField.ExpectedReturnDate, true));
        Assert.True(result.Success); Assert.Empty(result.Value!);
        Assert.Equal(new BorrowingSearchRequest("Alice", BorrowingStatus.Returned, BorrowingSortField.ExpectedReturnDate, true), queries.Last);
        Assert.Contains("No borrowings", result.Message);
    }
    [Fact]
    public async Task DefaultRequestRestoresAllStatusesAndDefaultSort()
    {
        var queries = new Queries(); var service = new BorrowingSearchService(queries);
        await service.SearchAsync(new());
        Assert.Equal(new BorrowingSearchRequest("", null, BorrowingSortField.CheckoutDate, false), queries.Last);
    }
    [Fact]
    public async Task FailureDiffersFromEmptyAndDoesNotExposeDetails()
    {
        var service = new BorrowingSearchService(new Queries { Fail = true });
        var result = await service.SearchAsync(new());
        Assert.False(result.Success); Assert.Null(result.Value);
        Assert.DoesNotContain("sensitive", result.Message);
    }
    [Fact]
    public async Task CancellationPropagatesInsteadOfReportingConnectionFailure()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var service = new BorrowingSearchService(new Queries());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SearchAsync(new(), cancellation.Token));
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidEnumCriteriaAreRejectedBeforeDatabaseAccess(bool invalidSort)
    {
        var queries = new Queries(); var service = new BorrowingSearchService(queries);
        var request = invalidSort ? new BorrowingSearchRequest(SortBy: (BorrowingSortField)999)
            : new BorrowingSearchRequest(Status: (BorrowingStatus)999);
        Assert.False((await service.SearchAsync(request)).Success);
        Assert.Null(queries.Last);
    }
}
