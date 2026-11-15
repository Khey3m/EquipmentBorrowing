using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IStudentManagementWorkflow
{
    Task<QueryResult<IReadOnlyList<Student>>> FindAsync(string? searchText, CancellationToken cancellationToken = default);
    Task<OperationResult<Student>> RegisterAsync(RegisterStudentRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<Student>> UpdateAsync(UpdateStudentRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<Student>> SetEligibilityAsync(SetStudentEligibilityRequest request, CancellationToken cancellationToken = default);
}
public interface IEquipmentManagementWorkflow
{
    Task<QueryResult<IReadOnlyList<Equipment>>> FindAsync(string? searchText, CancellationToken cancellationToken = default);
    Task<OperationResult<Equipment>> RegisterAsync(RegisterEquipmentRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<Equipment>> UpdateAsync(UpdateEquipmentRequest request, CancellationToken cancellationToken = default);
}
public interface ICatalogReader
{
    Task<QueryResult<CatalogSnapshot>> GetCatalogAsync(bool availableOnly, CancellationToken cancellationToken = default);
}
