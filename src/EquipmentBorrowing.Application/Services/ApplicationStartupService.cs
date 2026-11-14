using EquipmentBorrowing.Application.Interfaces;

namespace EquipmentBorrowing.Application.Services;

public sealed class ApplicationStartupService(IApplicationInitializer initializer)
{
    public async Task<bool> InitializeAsync()
    {
        try { await initializer.InitializeAsync(); return true; }
        catch (Exception) { return false; } // UI receives no provider details or credentials.
    }
}
