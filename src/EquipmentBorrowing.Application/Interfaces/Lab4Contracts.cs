using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

// Explicit requests exclude protected identity, availability and transaction state.
public record RegisterStudentRequest(string StudentNumber, string FullName, string? ContactInformation);
public record UpdateStudentRequest(int StudentId, string StudentNumber, string FullName, string? ContactInformation);
public record SetStudentEligibilityRequest(int StudentId, bool IsEligible);
public record RegisterEquipmentRequest(string EquipmentIdentifier, string Name, string? Description);
public record UpdateEquipmentRequest(int EquipmentId, string EquipmentIdentifier, string Name, string? Description);
public record MultiItemCheckoutRequest(int StudentId, IReadOnlyList<int> EquipmentIds, DateOnly ExpectedReturnDate);
public enum BorrowingSortField { CheckoutDate, ExpectedReturnDate }
public record BorrowingSearchRequest(string? SearchText = null, BorrowingStatus? Status = null,
    BorrowingSortField SortBy = BorrowingSortField.CheckoutDate, bool Descending = false);
public record BorrowingDetails(int Id, int StudentId, string StudentNumber, string StudentName,
    int EquipmentId, string EquipmentIdentifier, string EquipmentName, DateOnly CheckoutDate,
    DateOnly ExpectedReturnDate, DateOnly? ActualReturnDate, BorrowingStatus Status);
public enum OperationOutcome { Succeeded, Rejected, Conflict, Failed, Unknown }
public record OperationResult<T>(OperationOutcome Outcome, string Message, T? Value = default);

public interface IStudentManagementRepository : IStudentRepository
{
    Task<bool> NumberExistsAsync(string normalizedNumber, int? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Student student, CancellationToken cancellationToken = default);
    Task UpdateAsync(Student student, CancellationToken cancellationToken = default);
}

public interface IEquipmentManagementRepository : IEquipmentRepository
{
    Task<bool> IdentifierExistsAsync(string normalizedIdentifier, int? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default);
}

// Implementations follow in PostgreSQL/workflow sessions; unknown outcomes must never auto-retry.
public interface ILab4BorrowingQueries
{
    Task<IReadOnlyList<BorrowingDetails>> SearchAsync(BorrowingSearchRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BorrowingDetails>> GetStudentHistoryAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BorrowingDetails>> GetEquipmentHistoryAsync(int equipmentId, CancellationToken cancellationToken = default);
}
