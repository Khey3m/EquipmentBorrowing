using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public sealed class EfHistoryQueries(IDbContextFactory<EquipmentBorrowingDbContext> factory) : IHistoryQueries
{
    public async Task<IReadOnlyList<HistorySubject>> GetSubjectsAsync(HistoryKind kind, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return kind == HistoryKind.Student
            ? await context.Students.AsNoTracking().OrderBy(s => s.FullName).ThenBy(s => s.Id)
                .Select(s => new HistorySubject(s.Id, s.StudentNumber + " ? " + s.FullName)).ToListAsync(cancellationToken)
            : await context.Equipment.AsNoTracking().OrderBy(e => e.Name).ThenBy(e => e.Id)
                .Select(e => new HistorySubject(e.Id, e.EquipmentIdentifier + " ? " + e.Name)).ToListAsync(cancellationToken);
    }
    public async Task<HistorySnapshot?> GetHistoryAsync(HistoryKind kind, int subjectId, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        HistorySubject? subject = kind == HistoryKind.Student
            ? await context.Students.AsNoTracking().Where(s => s.Id == subjectId)
                .Select(s => new HistorySubject(s.Id, s.StudentNumber + " ? " + s.FullName)).SingleOrDefaultAsync(cancellationToken)
            : await context.Equipment.AsNoTracking().Where(e => e.Id == subjectId)
                .Select(e => new HistorySubject(e.Id, e.EquipmentIdentifier + " ? " + e.Name)).SingleOrDefaultAsync(cancellationToken);
        if (subject is null) return null;
        var query = context.Borrowings.AsNoTracking().Where(b => kind == HistoryKind.Student ? b.StudentId == subjectId : b.EquipmentId == subjectId);
        var rows = await (from b in query
                          join s in context.Students on b.StudentId equals s.Id
                          join e in context.Equipment on b.EquipmentId equals e.Id
                          orderby b.BorrowedAt descending, b.Id descending
                          select new BorrowingDetails(b.Id, s.Id, s.StudentNumber, s.FullName, e.Id,
                              e.EquipmentIdentifier, e.Name, b.BorrowedAt, b.ExpectedReturnAt, b.ActualReturnAt, b.Status))
            .ToListAsync(cancellationToken);
        return new(subject, rows);
    }
}
