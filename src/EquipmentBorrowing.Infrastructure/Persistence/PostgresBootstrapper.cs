using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public sealed class PostgresBootstrapper(IDbContextFactory<EquipmentBorrowingDbContext> factory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await DbInitializer.SeedAsync(context);
    }
}
