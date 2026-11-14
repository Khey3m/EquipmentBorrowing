namespace EquipmentBorrowing.Application.Interfaces;

public interface IStudentManagementSessionFactory
{
    Task<IStudentManagementSession> OpenAsync(CancellationToken cancellationToken = default);
}

public interface IStudentManagementSession : IAsyncDisposable
{
    IStudentManagementRepository Students { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
}
