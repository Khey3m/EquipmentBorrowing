using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public record ReturnResult(bool Success, string Message);

public class ReturnEquipmentService
{
    private readonly ICampusClock _clock;
    private readonly IBorrowingRepository _borrowingRepo;
    private readonly IEquipmentRepository _equipmentRepo;

    public ReturnEquipmentService(
        IBorrowingRepository borrowingRepo,
        IEquipmentRepository equipmentRepo, ICampusClock? clock = null)
    {
        _clock = clock ?? new CampusClock();
        _borrowingRepo = borrowingRepo;
        _equipmentRepo = equipmentRepo;
    }

    public async Task<ReturnResult> ExecuteAsync(int borrowingId, CancellationToken cancellationToken = default)
    {
        var borrowing = await _borrowingRepo.GetByIdAsync(borrowingId, cancellationToken);
        if (borrowing == null)
            return new ReturnResult(false, $"Borrowing record #{borrowingId} was not found.");

        if (borrowing.Status == BorrowingStatus.Returned)
            return new ReturnResult(false, "This borrowing record has already been returned.");

        var returnDate = _clock.Today;
        if (returnDate < borrowing.BorrowedAt)
            return new ReturnResult(false, "Return cannot precede checkout.");

        var equipment = await _equipmentRepo.GetByIdAsync(borrowing.EquipmentId, cancellationToken);
        if (equipment == null)
            return new ReturnResult(false, "Equipment was not found; refresh before retrying.");
        equipment.MarkAsReturned();
        await _equipmentRepo.UpdateAsync(equipment, cancellationToken);

        borrowing.MarkAsReturned(returnDate);
        await _borrowingRepo.UpdateAsync(borrowing, cancellationToken);

        return new ReturnResult(true, "Equipment successfully returned!");
    }
}
