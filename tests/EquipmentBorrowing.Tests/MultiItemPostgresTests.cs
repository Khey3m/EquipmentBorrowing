using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace EquipmentBorrowing.Tests;

public sealed class PostgresIntegrationFactAttribute : FactAttribute
{
    public PostgresIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("POSTGRES_INTEGRATION_ADMIN_CONNECTION")))
            Skip = "Set POSTGRES_INTEGRATION_ADMIN_CONNECTION locally on an approved runtime to create a disposable verification database.";
    }
}

public class MultiItemPostgresTests(ITestOutputHelper output)
{
    private sealed class Clock : ICampusClock { public DateOnly Today => new(2026, 11, 14); }
    private sealed class Factory(DbContextOptions<EquipmentBorrowingDbContext> options) : IDbContextFactory<EquipmentBorrowingDbContext>
    { public EquipmentBorrowingDbContext CreateDbContext() => new(options); }
    private sealed class FailAfterOne : ICheckoutWriteObserver
    {
        public int Count { get; private set; }
        public Task AfterItemPersistedAsync(int count, CancellationToken cancellationToken)
        { Count = count; throw new IOException("Controlled failure after first equipment update and borrowing insert"); }
    }

    private async Task<(Factory Factory, int Student, int[] Items)> CreateDisposableDatabaseAsync()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("POSTGRES_INTEGRATION_ADMIN_CONNECTION")!)
        { Database = "postgres", IncludeErrorDetail = false };
        var name = "equipment_borrowing_verify_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(connection.ConnectionString))
        {
            await admin.OpenAsync();
            // Name is a fixed prefix plus a generated hexadecimal GUID, never user-controlled SQL.
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await command.ExecuteNonQueryAsync();
        }
        output.WriteLine("Disposable verification database retained: " + name);
        connection.Database = name;
        var factory = new Factory(new DbContextOptionsBuilder<EquipmentBorrowingDbContext>().UseNpgsql(connection.ConnectionString).Options);
        await using var context = factory.CreateDbContext();
        await context.Database.MigrateAsync(); // Real EF migration/history, no manually populated history.
        var student = new Student(0, "VERIFY-S1", "Verification Student");
        var items = new[] { new Equipment(0, "VERIFY-E1", "One"), new Equipment(0, "VERIFY-E2", "Two") };
        context.Students.Add(student); context.Equipment.AddRange(items);
        await context.SaveChangesAsync();
        return (factory, student.Id, items.Select(e => e.Id).ToArray());
    }

    [PostgresIntegrationFact]
    public async Task SuccessfulCheckoutPersistsBothLoansAndAvailability()
    {
        var data = await CreateDisposableDatabaseAsync();
        var service = new MultiItemCheckoutService(new PostgresBorrowingSessionFactory(data.Factory), new Clock(), new PostgresWorkflowFailures());
        var result = await service.ExecuteAsync(new(data.Student, data.Items, new(2026, 11, 15)));
        Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        await using var fresh = data.Factory.CreateDbContext();
        Assert.Equal(2, await fresh.Borrowings.CountAsync(b => b.Status == BorrowingStatus.Active));
        Assert.All(await fresh.Equipment.AsNoTracking().ToListAsync(), e => Assert.False(e.IsAvailable));
    }

    [PostgresIntegrationFact]
    public async Task ControlledFailureAfterWritesRollsBackAllChanges()
    {
        var data = await CreateDisposableDatabaseAsync(); var observer = new FailAfterOne();
        var service = new MultiItemCheckoutService(new PostgresBorrowingSessionFactory(data.Factory), new Clock(), new PostgresWorkflowFailures(), observer);
        var result = await service.ExecuteAsync(new(data.Student, data.Items, new(2026, 11, 15)));
        Assert.Equal(OperationOutcome.Failed, result.Outcome);
        Assert.Equal(1, observer.Count); // Reached only after actual SaveChanges calls inside transaction.
        await using var fresh = data.Factory.CreateDbContext();
        Assert.Equal(0, await fresh.Borrowings.CountAsync());
        Assert.All(await fresh.Equipment.AsNoTracking().ToListAsync(), e => Assert.True(e.IsAvailable));
    }
}
