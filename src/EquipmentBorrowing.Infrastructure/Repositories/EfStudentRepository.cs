using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EfStudentRepository : EfRepository, IStudentManagementRepository
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
    public async Task<bool> NumberExistsAsync(string studentNumber, int? excludingId = null, CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        // Trim in Application; canonicalize in PostgreSQL just like the generated column.
        var canonical = await lease.Context.Database.SqlQuery<string>($"SELECT upper({studentNumber.Trim()}) AS \"Value\"")
            .SingleAsync(cancellationToken);
        return await lease.Context.Students.AnyAsync(s => s.NormalizedStudentNumber == canonical &&
            (!excludingId.HasValue || s.Id != excludingId.Value), cancellationToken);
    }

    public async Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        await lease.Context.Students.AddAsync(student, cancellationToken);
        await lease.Context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        await using var lease = await OpenAsync(cancellationToken);
        lease.Context.Students.Update(student);
        await lease.Context.SaveChangesAsync(cancellationToken);
    }
}