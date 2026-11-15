namespace EquipmentBorrowing.Application.Interfaces;

public interface IHistoryWorkflow
{
    Task<QueryResult<IReadOnlyList<HistorySubject>>> GetSubjectsAsync(HistoryKind kind, CancellationToken cancellationToken = default);
    Task<OperationResult<HistorySnapshot>> GetHistoryAsync(HistoryKind kind, int subjectId, CancellationToken cancellationToken = default);
}
public interface IBorrowingSearchWorkflow
{
    Task<QueryResult<IReadOnlyList<BorrowingDetails>>> SearchAsync(BorrowingSearchRequest request, CancellationToken cancellationToken = default);
}
