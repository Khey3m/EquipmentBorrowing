using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public record CatalogSnapshot(IReadOnlyList<Equipment> Equipment, IReadOnlyList<Student> Students);
public record QueryResult<T>(bool Success, T? Value, string Message);

public interface IExistingWorkflowQueries
{
    Task<CatalogSnapshot> GetCatalogAsync(bool availableOnly, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BorrowingDetails>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default);
}
