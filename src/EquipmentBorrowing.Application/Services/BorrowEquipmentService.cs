using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public record BorrowRequest(int StudentId, int EquipmentId, DateOnly ExpectedReturnDate);
public record BorrowResult(bool Success, string Message, Borrowing? Borrowing = null, OperationOutcome Outcome = OperationOutcome.Rejected);

public class BorrowEquipmentService
{
    private readonly IBorrowingSessionFactory? _sessions;
    private readonly IWorkflowFailures? _failures;
    private readonly ICampusClock _clock;
    private readonly IStudentRepository _studentRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IBorrowingRepository _borrowingRepo;

    public BorrowEquipmentService(
        IStudentRepository studentRepo,
        IEquipmentRepository equipmentRepo,
        IBorrowingRepository borrowingRepo, ICampusClock? clock = null, IBorrowingSessionFactory? sessions = null, IWorkflowFailures? failures = null)
    {
        _sessions = sessions;
        _failures = failures;
        _clock = clock ?? new CampusClock();
        _studentRepo = studentRepo;
        _equipmentRepo = equipmentRepo;
        _borrowingRepo = borrowingRepo;
    }

    public async Task<BorrowResult> ExecuteAsync(BorrowRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // In-memory demo/tests can execute without a database session; production injects the factory.
            if (_sessions is null) return await ExecuteCoreAsync(request, cancellationToken);
            await using var session = await _sessions.OpenAsync(cancellationToken);
            var result = await new BorrowEquipmentService(session.Students, session.Equipment, session.Borrowings, _clock).ExecuteCoreAsync(request, cancellationToken);
            if (result.Success) await session.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception error)
        {
            var outcome = error is CommitOutcomeUnknownException ? OperationOutcome.Unknown :
                _failures?.Classify(error) ?? OperationOutcome.Failed;
            return new BorrowResult(false, WorkflowFeedback.Message(outcome), Outcome: outcome);
        }
    }

    private async Task<BorrowResult> ExecuteCoreAsync(BorrowRequest request, CancellationToken cancellationToken)
    {
        var checkoutDate = _clock.Today;
        if (request.ExpectedReturnDate <= checkoutDate)
            return new BorrowResult(false, "Expected return must be later than checkout.");

        var student = await _studentRepo.GetByIdAsync(request.StudentId, cancellationToken);
        if (student == null)
            return new BorrowResult(false, $"Student with ID {request.StudentId} was not found. Refresh the selection.", Outcome: OperationOutcome.Conflict);

        var equipment = await _equipmentRepo.GetByIdAsync(request.EquipmentId, cancellationToken);
        if (equipment == null)
            return new BorrowResult(false, $"Equipment with ID {request.EquipmentId} was not found. Refresh the selection.", Outcome: OperationOutcome.Conflict);

        if (!equipment.IsAvailable || await _borrowingRepo.HasActiveForEquipmentAsync(equipment.Id, cancellationToken))
            return new BorrowResult(false, $"Equipment '{equipment.Name}' is currently unavailable. Refresh the selection.", Outcome: OperationOutcome.Conflict);

        int activeCount = await _borrowingRepo.GetActiveCountByStudentIdAsync(student.Id, cancellationToken);
        if (!student.IsEligibleToBorrow)
            return new BorrowResult(false, "This student is restricted from new borrowing.");
        if (!student.CanBorrow(activeCount))
            return new BorrowResult(false, "This student already has the maximum of three active items.");

        var borrowing = new Borrowing(student.Id, equipment.Id, checkoutDate, request.ExpectedReturnDate);
        equipment.MarkAsBorrowed();

        await _equipmentRepo.UpdateAsync(equipment, cancellationToken);
        await _borrowingRepo.AddAsync(borrowing, cancellationToken);

        return new BorrowResult(true, "Equipment successfully borrowed!", borrowing, OperationOutcome.Succeeded);
    }
}
