namespace EquipmentBorrowing.Application.Interfaces;

public interface IEquipmentManagementSessionFactory
{
    Task<IEquipmentManagementSession> OpenAsync(CancellationToken cancellationToken = default);
}

public interface IEquipmentManagementSession : IAsyncDisposable
{
    IEquipmentManagementRepository Equipment { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
}
