using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public record ReturnResult(bool Success, string Message, OperationOutcome Outcome = OperationOutcome.Rejected, WorkflowError Error = WorkflowError.None, Borrowing? Borrowing = null);

public class ReturnEquipmentService
{
    private readonly IBorrowingSessionFactory? _sessions;
    private readonly IWorkflowFailures? _failures;
    private readonly ICampusClock _clock;
    private readonly IBorrowingRepository _borrowingRepo;
    private readonly IEquipmentRepository _equipmentRepo;

    public ReturnEquipmentService(
        IBorrowingRepository borrowingRepo,
        IEquipmentRepository equipmentRepo, ICampusClock? clock = null, IBorrowingSessionFactory? sessions = null, IWorkflowFailures? failures = null)
    {
        _sessions = sessions;
        _failures = failures;
        _clock = clock ?? new CampusClock();
        _borrowingRepo = borrowingRepo;
        _equipmentRepo = equipmentRepo;
    }

    public async Task<ReturnResult> ExecuteAsync(int borrowingId, CancellationToken cancellationToken = default)
    {
        try
        {
            // In-memory demo/tests can execute without a database session; production injects the factory.
            if (_sessions is null) return await ExecuteCoreAsync(borrowingId, cancellationToken);
            await using var session = await _sessions.OpenAsync(cancellationToken);
            var result = await new ReturnEquipmentService(session.Borrowings, session.Equipment, _clock).ExecuteCoreAsync(borrowingId, cancellationToken);
            if (result.Success) await session.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception error)
        {
            var outcome = error is CommitOutcomeUnknownException ? OperationOutcome.Unknown :
                _failures?.Classify(error) ?? OperationOutcome.Failed;
            return new ReturnResult(false, WorkflowFeedback.Message(outcome), outcome, outcome switch
            {
                OperationOutcome.Unknown => WorkflowError.UnknownOutcome,
                OperationOutcome.Conflict => WorkflowError.ConcurrentConflict,
                _ => WorkflowError.DatabaseFailure
            });
        }
    }

    private async Task<ReturnResult> ExecuteCoreAsync(int borrowingId, CancellationToken cancellationToken)
    {
        var borrowing = await _borrowingRepo.GetByIdAsync(borrowingId, cancellationToken);
        if (borrowing == null)
            return new ReturnResult(false, $"Borrowing record #{borrowingId} was not found. Refresh the selection.", OperationOutcome.Conflict, WorkflowError.NotFound);

        if (borrowing.Status != BorrowingStatus.Active)
            return new ReturnResult(false, "This borrowing record has already been returned. Refresh the selection.", OperationOutcome.Conflict, WorkflowError.BusinessRuleConflict);

        var returnDate = _clock.Today;
        if (returnDate < borrowing.BorrowedAt)
            return new ReturnResult(false, "Return cannot precede checkout.", Error: WorkflowError.InvalidInput);

        var equipment = await _equipmentRepo.GetByIdAsync(borrowing.EquipmentId, cancellationToken);
        if (equipment == null)
            return new ReturnResult(false, "Equipment was not found; refresh before retrying.", OperationOutcome.Conflict, WorkflowError.NotFound);
        equipment.MarkAsReturned();
        await _equipmentRepo.UpdateAsync(equipment, cancellationToken);

        borrowing.MarkAsReturned(returnDate);
        await _borrowingRepo.UpdateAsync(borrowing, cancellationToken);

        return new ReturnResult(true, "Equipment successfully returned!", OperationOutcome.Succeeded, Borrowing: borrowing);
    }
}
