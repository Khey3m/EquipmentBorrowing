using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public sealed class EfBorrowingSearchQueries(IDbContextFactory<EquipmentBorrowingDbContext> factory) : IBorrowingSearchQueries
{
    public async Task<IReadOnlyList<BorrowingDetails>> SearchAsync(BorrowingSearchRequest request, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var query = from b in context.Borrowings.AsNoTracking()
                    join s in context.Students on b.StudentId equals s.Id
                    join e in context.Equipment on b.EquipmentId equals e.Id
                    select new { Loan = b, Student = s, Equipment = e };

        var text = request.SearchText?.Trim() ?? "";
        if (text.Length > 0)
        {
            // Treat staff input as literal partial text, including SQL LIKE wildcard characters.
            var escaped = text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = "%" + escaped + "%";
            query = query.Where(x => EF.Functions.ILike(x.Student.FullName, pattern, "\\") ||
                EF.Functions.ILike(x.Student.StudentNumber, pattern, "\\") ||
                EF.Functions.ILike(x.Equipment.Name, pattern, "\\"));
        }
        if (request.Status.HasValue) query = query.Where(x => x.Loan.Status == request.Status.Value);
        var ordered = (request.SortBy, request.Descending) switch
        {
            (BorrowingSortField.CheckoutDate, false) => query.OrderBy(x => x.Loan.BorrowedAt),
            (BorrowingSortField.CheckoutDate, true) => query.OrderByDescending(x => x.Loan.BorrowedAt),
            (BorrowingSortField.ExpectedReturnDate, false) => query.OrderBy(x => x.Loan.ExpectedReturnAt),
            (BorrowingSortField.ExpectedReturnDate, true) => query.OrderByDescending(x => x.Loan.ExpectedReturnAt),
            _ => throw new ArgumentOutOfRangeException(nameof(request.SortBy))
        };
        return await ordered.ThenBy(x => x.Loan.Id)
            .Select(x => new BorrowingDetails(x.Loan.Id, x.Student.Id, x.Student.StudentNumber, x.Student.FullName,
                x.Equipment.Id, x.Equipment.EquipmentIdentifier, x.Equipment.Name, x.Loan.BorrowedAt,
                x.Loan.ExpectedReturnAt, x.Loan.ActualReturnAt, x.Loan.Status))
            .ToListAsync(cancellationToken);
    }
}
