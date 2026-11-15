using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public sealed class ExistingWorkflowQueryService(IExistingWorkflowQueries queries) : ICatalogReader
{
    public async Task<QueryResult<CatalogSnapshot>> GetCatalogAsync(bool availableOnly, CancellationToken cancellationToken = default)
    {
        try { return new(true, await queries.GetCatalogAsync(availableOnly, cancellationToken), ""); }
        catch (Exception) { return new(false, null, "Unable to load catalog. Check the connection and refresh to retry."); }
    }

    public async Task<QueryResult<IReadOnlyList<BorrowingDetails>>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default)
    {
        try { return new(true, await queries.GetActiveBorrowingsAsync(cancellationToken), ""); }
        catch (Exception) { return new(false, null, "Unable to load active borrowings. Check the connection and refresh to retry."); }
    }
}
