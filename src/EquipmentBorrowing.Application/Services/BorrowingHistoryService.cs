using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public sealed class BorrowingHistoryService(IHistoryQueries queries) : IHistoryWorkflow
{
    public async Task<QueryResult<IReadOnlyList<HistorySubject>>> GetSubjectsAsync(HistoryKind kind, CancellationToken cancellationToken = default)
    {
        try { return new(true, await queries.GetSubjectsAsync(kind, cancellationToken), ""); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return new(false, null, "Unable to load subjects. Restore the connection and refresh to retry."); }
    }
    public async Task<OperationResult<HistorySnapshot>> GetHistoryAsync(HistoryKind kind, int subjectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await queries.GetHistoryAsync(kind, subjectId, cancellationToken);
            return snapshot is null
                ? new(OperationOutcome.Rejected, "The selected student or equipment no longer exists. Refresh and select again.", Error: WorkflowError.NotFound)
                : new(OperationOutcome.Succeeded, snapshot.Rows.Count == 0 ? "No borrowing history for this selection." : "", snapshot);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { return new(OperationOutcome.Failed, "Unable to load history. Restore the connection and retry the selected history.", Error: WorkflowError.DatabaseFailure); }
    }
}
