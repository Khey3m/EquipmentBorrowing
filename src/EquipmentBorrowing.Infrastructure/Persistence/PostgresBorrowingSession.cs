using System.Data;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public sealed class PostgresBorrowingSessionFactory(IDbContextFactory<EquipmentBorrowingDbContext> factory)
    : IBorrowingSessionFactory
{
    public async Task<IBorrowingSession> OpenAsync(CancellationToken cancellationToken = default)
    {
        var context = await factory.CreateDbContextAsync(cancellationToken);
        try
        {
            var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            return new PostgresBorrowingSession(context, transaction);
        }
        catch { await context.DisposeAsync(); throw; }
    }
}

internal sealed class PostgresBorrowingSession : IBorrowingSession
{
    private readonly EquipmentBorrowingDbContext _context;
    private readonly IDbContextTransaction _transaction;
    public IStudentRepository Students { get; }
    public IEquipmentRepository Equipment { get; }
    public IBorrowingRepository Borrowings { get; }

    public PostgresBorrowingSession(EquipmentBorrowingDbContext context, IDbContextTransaction transaction)
    {
        _context = context;
        _transaction = transaction;
        Students = new EfStudentRepository(context);
        Equipment = new EfEquipmentRepository(context);
        Borrowings = new EfBorrowingRepository(context);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try { await _transaction.CommitAsync(cancellationToken); }
        // Explicit server errors (e.g. serialization rejection) have a known unsuccessful outcome.
        catch (PostgresException) { throw; }
        catch (Exception ex) when (ex is NpgsqlException or IOException or OperationCanceledException)
        { throw new CommitOutcomeUnknownException(); }
    }

    public async ValueTask DisposeAsync()
    {
        try { await _transaction.DisposeAsync(); }
        finally { await _context.DisposeAsync(); }
    }
}
