using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfEquipmentRepository : EfRepository, IEquipmentRepository
{
    public EfEquipmentRepository(IDbContextFactory<EquipmentBorrowingDbContext> factory) : base(factory) { }
    internal EfEquipmentRepository(EquipmentBorrowingDbContext context) : base(context) { }

    public async Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        return await lease.Context.Equipment
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        return await lease.Context.Equipment
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        lease.Context.Equipment.Update(equipment);
        await lease.Context.SaveChangesAsync(cancellationToken);
    }
}