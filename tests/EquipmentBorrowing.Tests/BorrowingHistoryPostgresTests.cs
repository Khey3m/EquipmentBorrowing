using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace EquipmentBorrowing.Tests;

public class BorrowingHistoryPostgresTests(ITestOutputHelper output)
{
    private sealed class Factory(DbContextOptions<EquipmentBorrowingDbContext> options) : IDbContextFactory<EquipmentBorrowingDbContext>
    { public EquipmentBorrowingDbContext CreateDbContext() => new(options); }

    [PostgresIntegrationFact]
    public async Task RelatedHistoriesFilterSubjectsAndRetainBothStatusesAndEmptySubjects()
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
        output.WriteLine("Disposable history database retained: " + name);
        connection.Database = name;
        var factory = new Factory(new DbContextOptionsBuilder<EquipmentBorrowingDbContext>().UseNpgsql(connection.ConnectionString).Options);
        int[] studentIds, itemIds;
        await using (var setup = factory.CreateDbContext())
        {
            await setup.Database.MigrateAsync();
            var students = Enumerable.Range(1, 3).Select(i => new Student(0, "H-S"+i, "History Student "+i)).ToArray();
            var items = Enumerable.Range(1, 3).Select(i => new Equipment(0, "H-E"+i, "History Equipment "+i)).ToArray();
            setup.Students.AddRange(students); setup.Equipment.AddRange(items); await setup.SaveChangesAsync();
            studentIds = students.Select(s => s.Id).ToArray(); itemIds = items.Select(e => e.Id).ToArray();
            var returned = new Borrowing(studentIds[0], itemIds[0], new(2026,11,10), new(2026,11,13));
            returned.MarkAsReturned(new(2026,11,11));
            var active = new Borrowing(studentIds[0], itemIds[0], new(2026,11,12), new(2026,11,15));
            var other = new Borrowing(studentIds[1], itemIds[1], new(2026,11,12), new(2026,11,16));
            items[0].MarkAsBorrowed(); items[1].MarkAsBorrowed();
            setup.Borrowings.AddRange(returned, active, other); await setup.SaveChangesAsync();
        }
        var queries = new EfHistoryQueries(factory); // Every call owns a fresh context.
        foreach (var kind in Enum.GetValues<HistoryKind>())
        {
            var ids = kind == HistoryKind.Student ? studentIds : itemIds;
            Assert.Equal(3, (await queries.GetSubjectsAsync(kind)).Count);
            var first = (await queries.GetHistoryAsync(kind, ids[0]))!;
            Assert.Equal(2, first.Rows.Count);
            Assert.All(first.Rows, row => Assert.Equal(ids[0], kind == HistoryKind.Student ? row.StudentId : row.EquipmentId));
            Assert.Contains(first.Rows, row => row.Status == BorrowingStatus.Returned && row.ActualReturnDate == new DateOnly(2026,11,11));
            Assert.Contains(first.Rows, row => row.Status == BorrowingStatus.Active && row.ActualReturnDate is null);
            Assert.Single((await queries.GetHistoryAsync(kind, ids[1]))!.Rows);
            Assert.Empty((await queries.GetHistoryAsync(kind, ids[2]))!.Rows);
            Assert.Null(await queries.GetHistoryAsync(kind, int.MaxValue));
        }
        await using var readback = factory.CreateDbContext();
        Assert.Equal(3, await readback.Borrowings.CountAsync()); // History reads do not mutate data.
    }
}
