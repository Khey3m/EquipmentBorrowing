using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfBorrowingRepository : EfRepository, IBorrowingRepository
{
    public EfBorrowingRepository(IDbContextFactory<EquipmentBorrowingDbContext> factory) : base(factory) { }
    internal EfBorrowingRepository(EquipmentBorrowingDbContext context) : base(context) { }

    public async Task<Borrowing?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        return await lease.Context.Borrowings
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        return await lease.Context.Borrowings
            .AsNoTracking()
            .Where(b => b.Status == BorrowingStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveCountByStudentIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        return await lease.Context.Borrowings
            .CountAsync(b => b.StudentId == studentId && b.Status == BorrowingStatus.Active, cancellationToken);
    }

    public async Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        await lease.Context.Borrowings.AddAsync(borrowing, cancellationToken);
        await lease.Context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        lease.Context.Borrowings.Update(borrowing);
        await lease.Context.SaveChangesAsync(cancellationToken);
    }
}