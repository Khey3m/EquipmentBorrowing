using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfStudentRepository : EfRepository, IStudentRepository
{
    public EfStudentRepository(IDbContextFactory<EquipmentBorrowingDbContext> factory) : base(factory) { }
    internal EfStudentRepository(EquipmentBorrowingDbContext context) : base(context) { }

    public async Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        return await lease.Context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        return await lease.Context.Students
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}