using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

// Explicit requests exclude protected identity, availability and transaction state.
public record RegisterStudentRequest(string StudentNumber, string FullName, string? ContactInformation);
public record StudentSnapshot(string StudentNumber, string FullName, string? ContactInformation, bool IsEligibleToBorrow)
{
    public static StudentSnapshot From(Student student) => new(student.StudentNumber, student.FullName, student.ContactInformation, student.IsEligibleToBorrow);
}
public record UpdateStudentRequest(int StudentId, string StudentNumber, string FullName, string? ContactInformation, StudentSnapshot Expected);
public record SetStudentEligibilityRequest(int StudentId, bool IsEligible, StudentSnapshot Expected);
public record RegisterEquipmentRequest(string EquipmentIdentifier, string Name, string? Description);
public record EquipmentSnapshot(string EquipmentIdentifier, string Name, string? Description, bool IsAvailable)
{
    public static EquipmentSnapshot From(Equipment equipment) => new(equipment.EquipmentIdentifier, equipment.Name, equipment.Description, equipment.IsAvailable);
}
public record UpdateEquipmentRequest(int EquipmentId, string EquipmentIdentifier, string Name, string? Description, EquipmentSnapshot Expected);
public record MultiItemCheckoutRequest(int StudentId, IReadOnlyList<int> EquipmentIds, DateOnly ExpectedReturnDate);
public enum BorrowingSortField { CheckoutDate, ExpectedReturnDate }
public record BorrowingSearchRequest(string? SearchText = null, BorrowingStatus? Status = null,
    BorrowingSortField SortBy = BorrowingSortField.CheckoutDate, bool Descending = false);
public record BorrowingDetails(int Id, int StudentId, string StudentNumber, string StudentName,
    int EquipmentId, string EquipmentIdentifier, string EquipmentName, DateOnly CheckoutDate,
    DateOnly ExpectedReturnDate, DateOnly? ActualReturnDate, BorrowingStatus Status);
public enum OperationOutcome { Succeeded, Rejected, Conflict, Failed, Unknown }
public enum WorkflowError { None, InvalidInput, BusinessRuleConflict, DuplicateIdentifier, NotFound, StaleState, NoChange, ConcurrentConflict, DatabaseFailure, UnknownOutcome }
public record OperationResult<T>(OperationOutcome Outcome, string Message, T? Value = default,
    WorkflowError Error = WorkflowError.None);

public interface IStudentManagementRepository : IStudentRepository
{
    Task<bool> NumberExistsAsync(string studentNumber, int? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Student student, CancellationToken cancellationToken = default);
    Task UpdateAsync(Student student, CancellationToken cancellationToken = default);
}

public interface IEquipmentManagementRepository : IEquipmentRepository
{
    Task<bool> IdentifierExistsAsync(string equipmentIdentifier, int? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default);
}

// History reads use IHistoryQueries; this interface covers borrowing-list retrieval only.
public interface IBorrowingSearchQueries
{
    Task<IReadOnlyList<BorrowingDetails>> SearchAsync(BorrowingSearchRequest request, CancellationToken cancellationToken = default);
}
