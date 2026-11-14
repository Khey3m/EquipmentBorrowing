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
        var validation = await CheckoutValidation.ValidateAsync(_studentRepo, _equipmentRepo, _borrowingRepo,
            request.StudentId, new[] { request.EquipmentId }, request.ExpectedReturnDate, checkoutDate, 1, cancellationToken);
        if (validation.Value is null)
            return new BorrowResult(false, validation.Message, Outcome: validation.Outcome);
        var student = validation.Value.Student;
        var equipment = validation.Value.Items[0];

        var borrowing = new Borrowing(student.Id, equipment.Id, checkoutDate, request.ExpectedReturnDate);
        equipment.MarkAsBorrowed();

        await _equipmentRepo.UpdateAsync(equipment, cancellationToken);
        await _borrowingRepo.AddAsync(borrowing, cancellationToken);

        return new BorrowResult(true, "Equipment successfully borrowed!", borrowing, OperationOutcome.Succeeded);
    }
}
