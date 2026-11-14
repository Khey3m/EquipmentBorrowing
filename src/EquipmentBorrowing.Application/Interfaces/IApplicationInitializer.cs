namespace EquipmentBorrowing.Application.Interfaces;

public interface IApplicationInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
