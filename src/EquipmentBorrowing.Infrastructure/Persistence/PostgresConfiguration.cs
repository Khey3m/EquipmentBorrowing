using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class PostgresConfiguration
{
    public const string EnvironmentVariable = "EQUIPMENT_BORROWING_CONNECTION_STRING";

    public static string GetConnectionString() =>
        Environment.GetEnvironmentVariable(EnvironmentVariable) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Set {EnvironmentVariable} locally before starting the application or migration tools.");

    public static void Configure(DbContextOptionsBuilder options) =>
        options.UseNpgsql(GetConnectionString()); // No automatic retry: a commit outcome may be unknown.
}
