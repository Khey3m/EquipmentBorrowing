using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface ISingleCheckoutWorkflow
{
    Task<BorrowResult> ExecuteAsync(BorrowRequest request, CancellationToken cancellationToken = default);
}
public interface IReturnWorkflow
{
    Task<ReturnResult> ExecuteAsync(int borrowingId, CancellationToken cancellationToken = default);
}
public interface IMultiCheckoutWorkflow
{
    Task<OperationResult<IReadOnlyList<Borrowing>>> ExecuteAsync(MultiItemCheckoutRequest request, CancellationToken cancellationToken = default);
}
public interface IActiveBorrowingReader
{
    Task<QueryResult<IReadOnlyList<BorrowingDetails>>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default);
}
