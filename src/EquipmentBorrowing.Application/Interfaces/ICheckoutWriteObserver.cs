namespace EquipmentBorrowing.Application.Interfaces;

// Optional verification seam. Production does not register an observer or expose a failure switch.
public interface ICheckoutWriteObserver
{
    Task AfterItemPersistedAsync(int persistedItemCount, CancellationToken cancellationToken);
}
