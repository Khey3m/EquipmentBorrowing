using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public abstract class EfRepository
{
    private readonly IDbContextFactory<EquipmentBorrowingDbContext>? _factory;
    private readonly EquipmentBorrowingDbContext? _sharedContext;
    protected EfRepository(IDbContextFactory<EquipmentBorrowingDbContext> factory) => _factory = factory;
    protected EfRepository(EquipmentBorrowingDbContext context) => _sharedContext = context;
    protected async Task<RepositoryContext> OpenAsync(CancellationToken cancellationToken) =>
        _sharedContext is not null ? new(_sharedContext, false) :
        new(await _factory!.CreateDbContextAsync(cancellationToken), true);
}

public sealed class RepositoryContext(EquipmentBorrowingDbContext context, bool ownsContext) : IAsyncDisposable
{
    public EquipmentBorrowingDbContext Context => context;
    public ValueTask DisposeAsync() => ownsContext ? context.DisposeAsync() : ValueTask.CompletedTask;
}
