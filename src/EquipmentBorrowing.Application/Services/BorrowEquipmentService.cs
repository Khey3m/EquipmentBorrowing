using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public record BorrowRequest(int StudentId, int EquipmentId, DateOnly ExpectedReturnDate);
public record BorrowResult(bool Success, string Message, Borrowing? Borrowing = null);

public class BorrowEquipmentService
{
    private readonly ICampusClock _clock;
    private readonly IStudentRepository _studentRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IBorrowingRepository _borrowingRepo;

    public BorrowEquipmentService(
        IStudentRepository studentRepo,
        IEquipmentRepository equipmentRepo,
        IBorrowingRepository borrowingRepo, ICampusClock? clock = null)
    {
        _clock = clock ?? new CampusClock();
        _studentRepo = studentRepo;
        _equipmentRepo = equipmentRepo;
        _borrowingRepo = borrowingRepo;
    }

    public async Task<BorrowResult> ExecuteAsync(BorrowRequest request, CancellationToken cancellationToken = default)
    {
        var checkoutDate = _clock.Today;
        if (request.ExpectedReturnDate <= checkoutDate)
            return new BorrowResult(false, "Expected return must be later than checkout.");

        var student = await _studentRepo.GetByIdAsync(request.StudentId, cancellationToken);
        if (student == null)
            return new BorrowResult(false, $"Student with ID {request.StudentId} was not found.");

        var equipment = await _equipmentRepo.GetByIdAsync(request.EquipmentId, cancellationToken);
        if (equipment == null)
            return new BorrowResult(false, $"Equipment with ID {request.EquipmentId} was not found.");

        if (!equipment.IsAvailable)
            return new BorrowResult(false, $"Equipment '{equipment.Name}' is currently unavailable.");

        int activeCount = await _borrowingRepo.GetActiveCountByStudentIdAsync(student.Id, cancellationToken);
        if (!student.CanBorrow(activeCount))
            return new BorrowResult(false, $"Student '{student.FullName}' is not allowed to borrow.");

        var borrowing = new Borrowing(student.Id, equipment.Id, checkoutDate, request.ExpectedReturnDate);
        equipment.MarkAsBorrowed();

        await _equipmentRepo.UpdateAsync(equipment, cancellationToken);
        await _borrowingRepo.AddAsync(borrowing, cancellationToken);

        return new BorrowResult(true, "Equipment successfully borrowed!", borrowing);
    }
}
