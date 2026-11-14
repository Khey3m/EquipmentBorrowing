namespace EquipmentBorrowing.Application.Interfaces;

public enum HistoryKind { Student, Equipment }
public record HistorySubject(int Id, string Label);
public record HistorySnapshot(HistorySubject Subject, IReadOnlyList<BorrowingDetails> Rows);
public interface IHistoryQueries
{
    Task<IReadOnlyList<HistorySubject>> GetSubjectsAsync(HistoryKind kind, CancellationToken cancellationToken = default);
    // Null distinguishes a missing subject from an existing subject with no borrowing history.
    Task<HistorySnapshot?> GetHistoryAsync(HistoryKind kind, int subjectId, CancellationToken cancellationToken = default);
}
