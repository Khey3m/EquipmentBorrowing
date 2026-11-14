using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public sealed class EfExistingWorkflowQueries(IDbContextFactory<EquipmentBorrowingDbContext> factory) : IExistingWorkflowQueries
{
    public async Task<CatalogSnapshot> GetCatalogAsync(bool availableOnly, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var query = context.Equipment.AsNoTracking();
        if (availableOnly)
            query = query.Where(e => e.IsAvailable && !context.Borrowings.Any(b => b.EquipmentId == e.Id && b.Status == BorrowingStatus.Active));
        var equipment = await query.OrderBy(e => e.EquipmentIdentifier).ToListAsync(cancellationToken);
        var students = await context.Students.AsNoTracking().OrderBy(s => s.FullName).ThenBy(s => s.Id).ToListAsync(cancellationToken);
        return new(equipment, students);
    }

    public async Task<IReadOnlyList<BorrowingDetails>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return await (from b in context.Borrowings.AsNoTracking()
                      join s in context.Students on b.StudentId equals s.Id
                      join e in context.Equipment on b.EquipmentId equals e.Id
                      where b.Status == BorrowingStatus.Active
                      orderby b.BorrowedAt, b.Id
                      select new BorrowingDetails(b.Id, s.Id, s.StudentNumber, s.FullName, e.Id,
                          e.EquipmentIdentifier, e.Name, b.BorrowedAt, b.ExpectedReturnAt, b.ActualReturnAt, b.Status))
            .ToListAsync(cancellationToken);
    }
}
