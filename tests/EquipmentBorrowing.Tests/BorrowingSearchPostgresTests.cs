using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace EquipmentBorrowing.Tests;

public class BorrowingSearchPostgresTests(ITestOutputHelper output)
{
    private sealed class Factory(DbContextOptions<EquipmentBorrowingDbContext> options) : IDbContextFactory<EquipmentBorrowingDbContext>
    { public EquipmentBorrowingDbContext CreateDbContext() => new(options); }

    [PostgresIntegrationFact]
    public async Task SearchFiltersSortDirectionsTiesAndResetMatchControlledRecords()
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
        output.WriteLine("Disposable search database retained: " + name);
        connection.Database = name;
        var factory = new Factory(new DbContextOptionsBuilder<EquipmentBorrowingDbContext>().UseNpgsql(connection.ConnectionString).Options);
        int[] ids;
        await using (var setup = factory.CreateDbContext())
        {
            await setup.Database.MigrateAsync();
            var alice = new Student(0, "CS-001", "Alice"); var bob = new Student(0, "EE-002", "Bob");
            var items = new[] { new Equipment(0, "Q1", "Scope_100%"), new Equipment(0, "Q2", "Meter\\Lab"),
                new Equipment(0, "Q3", "Camera"), new Equipment(0, "Q4", "ScopeZ100X") };
            setup.Students.AddRange(alice, bob); setup.Equipment.AddRange(items); await setup.SaveChangesAsync();
            var loans = new[] {
                new Borrowing(alice.Id, items[0].Id, new(2026,11,10), new(2026,11,15)),
                new Borrowing(alice.Id, items[1].Id, new(2026,11,12), new(2026,11,14)),
                new Borrowing(bob.Id, items[2].Id, new(2026,11,12), new(2026,11,16)),
                new Borrowing(bob.Id, items[3].Id, new(2026,11,9), new(2026,11,14)) };
            loans[0].MarkAsReturned(new(2026,11,11)); loans[1].MarkAsReturned(new(2026,11,13));
            items[2].MarkAsBorrowed(); items[3].MarkAsBorrowed();
            // Persist sequentially so fixture IDs have a known order independent of batching.
            foreach (var loan in loans) { setup.Borrowings.Add(loan); await setup.SaveChangesAsync(); }
            ids = loans.Select(b => b.Id).ToArray();
        }
        var service = new BorrowingSearchService(new EfBorrowingSearchQueries(factory));
        async Task<int[]> Search(BorrowingSearchRequest request)
        {
            var result = await service.SearchAsync(request);
            Assert.True(result.Success, result.Message);
            return result.Value!.Select(b => b.Id).ToArray();
        }
        Assert.Equal(new[] { ids[0], ids[1] }, await Search(new(" aLiCe ")));
        Assert.Equal(new[] { ids[3], ids[2] }, await Search(new(" ee-002 ")));
        Assert.Equal(new[] { ids[2] }, await Search(new(" cam ")));
        Assert.Equal(new[] { ids[0] }, await Search(new("_")));
        Assert.Equal(new[] { ids[0] }, await Search(new("%")));
        Assert.Equal(new[] { ids[1] }, await Search(new("\\")));
        Assert.Equal(new[] { ids[3], ids[2] }, await Search(new(Status: BorrowingStatus.Active)));
        Assert.Equal(new[] { ids[0], ids[1] }, await Search(new(Status: BorrowingStatus.Returned)));
        Assert.Equal(new[] { ids[3], ids[0], ids[1], ids[2] }, await Search(new()));
        Assert.Equal(new[] { ids[1], ids[2], ids[0], ids[3] }, await Search(new(Descending: true)));
        Assert.Equal(new[] { ids[1], ids[3], ids[0], ids[2] }, await Search(new(SortBy: BorrowingSortField.ExpectedReturnDate)));
        Assert.Equal(new[] { ids[2], ids[0], ids[1], ids[3] }, await Search(new(SortBy: BorrowingSortField.ExpectedReturnDate, Descending: true)));
        Assert.Equal(new[] { ids[2], ids[3] }, await Search(new("bob", BorrowingStatus.Active, BorrowingSortField.ExpectedReturnDate, true)));
        Assert.Empty(await Search(new("bob", BorrowingStatus.Returned)));
        Assert.Empty(await Search(new("no-such-subject")));
        Assert.Equal(4, (await Search(new())).Length); // Reset after combined/no-match criteria.
        await using var fresh = factory.CreateDbContext();
        Assert.Equal(4, await fresh.Borrowings.CountAsync());
    }
}
