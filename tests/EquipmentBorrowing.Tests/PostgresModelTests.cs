using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Tests;

public class PostgresModelTests
{
    [Fact]
    public void FrozenMigrationModelMatchesRuntimeModel()
    {
        // Building/comparing models does not open a database connection.
        var options = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>()
            .UseNpgsql("Host=localhost;Database=model_check;Username=model_check")
            .Options;
        using var context = new EquipmentBorrowingDbContext(options);
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Equal(new[] { "20261114000100_InitialPostgres" }, context.Database.GetMigrations());
    }
}
