using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public sealed class BorrowingSearchService(IBorrowingSearchQueries queries) : IBorrowingSearchWorkflow
{
    public async Task<QueryResult<IReadOnlyList<BorrowingDetails>>> SearchAsync(BorrowingSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.SortBy) || (request.Status.HasValue && !Enum.IsDefined(request.Status.Value)))
            return new(false, null, "Choose a valid status and date sort field.");
        try
        {
            var normalized = request with { SearchText = request.SearchText?.Trim() ?? "" };
            var rows = await queries.SearchAsync(normalized, cancellationToken);
            return new(true, rows, rows.Count == 0 ? "No borrowings match these criteria." : "");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return new(false, null, "Unable to load borrowings. Restore the connection and apply the criteria again to retry."); }
    }
}
