using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

internal record CheckoutSelection(Student Student, IReadOnlyList<Equipment> Items);

internal static class CheckoutValidation
{
    public static async Task<OperationResult<CheckoutSelection>> ValidateAsync(
        IStudentRepository students, IEquipmentRepository equipment, IBorrowingRepository borrowings,
        int studentId, IReadOnlyList<int> itemIds, DateOnly expectedReturn, DateOnly checkout,
        int minimumItems, CancellationToken cancellationToken)
    {
        if (itemIds.Count < minimumItems)
            return new(OperationOutcome.Rejected, $"Select at least {minimumItems} distinct equipment item(s).", Error: WorkflowError.InvalidInput);
        if (itemIds.Any(id => id <= 0) || itemIds.Distinct().Count() != itemIds.Count)
            return new(OperationOutcome.Rejected, "Duplicate or invalid equipment selections are not allowed.", Error: WorkflowError.InvalidInput);
        if (expectedReturn <= checkout)
            return new(OperationOutcome.Rejected, "Expected return must be later than checkout.", Error: WorkflowError.InvalidInput);
        var student = await students.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
            return new(OperationOutcome.Conflict, "Student was not found. Refresh the selection.", Error: WorkflowError.NotFound);
        if (!student.IsEligibleToBorrow)
            return new(OperationOutcome.Rejected, "This student is restricted from new borrowing.", Error: WorkflowError.BusinessRuleConflict);
        var activeCount = await borrowings.GetActiveCountByStudentIdAsync(student.Id, cancellationToken);
        if (!student.CanBorrow(activeCount, itemIds.Count))
            return new(OperationOutcome.Rejected, "Existing active items plus requested items must not exceed three.", Error: WorkflowError.BusinessRuleConflict);
        var items = new List<Equipment>();
        // A consistent order reduces competing-request lock-order differences.
        foreach (var id in itemIds.Order())
        {
            var item = await equipment.GetByIdAsync(id, cancellationToken);
            if (item is null)
                return new(OperationOutcome.Conflict, $"Equipment #{id} was not found. Refresh the selection.", Error: WorkflowError.NotFound);
            if (!item.IsAvailable || await borrowings.HasActiveForEquipmentAsync(id, cancellationToken))
                return new(OperationOutcome.Conflict, $"Equipment '{item.Name}' is currently unavailable. Refresh the selection.", Error: WorkflowError.BusinessRuleConflict);
            items.Add(item);
        }
        return new(OperationOutcome.Succeeded, "", new(student, items));
    }
}
