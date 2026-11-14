using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace EquipmentBorrowing.Tests;

internal sealed class PostgresVerificationDatabase(DbContextOptions<EquipmentBorrowingDbContext> options)
    : IDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext() => new(options);
    public static async Task<PostgresVerificationDatabase> CreateAsync(ITestOutputHelper output)
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("POSTGRES_INTEGRATION_ADMIN_CONNECTION")!)
        { Database = "postgres", IncludeErrorDetail = false };
        var name = "equipment_borrowing_verify_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(connection.ConnectionString))
        {
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await command.ExecuteNonQueryAsync();
        }
        output.WriteLine("Disposable integrated verification database: " + name);
        connection.Database = name;
        var factory = new PostgresVerificationDatabase(new DbContextOptionsBuilder<EquipmentBorrowingDbContext>().UseNpgsql(connection.ConnectionString).Options);
        await new PostgresBootstrapper(factory).InitializeAsync();
        return factory;
    }
}
