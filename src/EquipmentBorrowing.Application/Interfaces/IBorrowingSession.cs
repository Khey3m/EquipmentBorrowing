namespace EquipmentBorrowing.Application.Interfaces;

// A write operation shares one context/transaction without exposing EF to Application.
public interface IBorrowingSessionFactory
{
    Task<IBorrowingSession> OpenAsync(CancellationToken cancellationToken = default);
}

public interface IBorrowingSession : IAsyncDisposable
{
    IStudentRepository Students { get; }
    IEquipmentRepository Equipment { get; }
    IBorrowingRepository Borrowings { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public sealed class CommitOutcomeUnknownException() : Exception(
    "Commit acknowledgement was lost. The outcome is unknown; reconnect and refresh before deciding whether to retry.");
