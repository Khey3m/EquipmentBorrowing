using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Tests;

public class WorkflowSessionTests
{
    private sealed class Clock : ICampusClock { public DateOnly Today => new(2026, 11, 14); }

    private sealed class Session : IBorrowingSessionFactory, IBorrowingSession, IStudentRepository, IEquipmentRepository, IBorrowingRepository
    {
        public Student Student { get; } = new(1, "S1", "Student");
        public Equipment Item { get; } = new(10, "E10", "Item");
        public Borrowing? Loan { get; set; }
        public int Commits { get; private set; }
        public int Writes { get; private set; }
        public bool Disposed { get; private set; }
        public bool FailOnAdd { get; set; }
        public bool UnknownCommit { get; set; }
        public IStudentRepository Students => this;
        public IEquipmentRepository Equipment => this;
        public IBorrowingRepository Borrowings => this;
        public Task<IBorrowingSession> OpenAsync(CancellationToken cancellationToken = default) => Task.FromResult<IBorrowingSession>(this);
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Commits++;
            if (UnknownCommit) throw new CommitOutcomeUnknownException();
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
        Task<Student?> IStudentRepository.GetByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<Student?>(id == 1 ? Student : null);
        Task<IEnumerable<Student>> IStudentRepository.GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Student>>([Student]);
        Task<Equipment?> IEquipmentRepository.GetByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<Equipment?>(id == 10 ? Item : null);
        Task<IEnumerable<Equipment>> IEquipmentRepository.GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Equipment>>([Item]);
        public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default) { Writes++; return Task.CompletedTask; }
        Task<Borrowing?> IBorrowingRepository.GetByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Loan?.Id == id ? Loan : null);
        public Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Borrowing>>(Loan is { Status: BorrowingStatus.Active } ? [Loan] : []);
        public Task<int> GetActiveCountByStudentIdAsync(int studentId, CancellationToken cancellationToken = default) => Task.FromResult(Loan is { Status: BorrowingStatus.Active } ? 1 : 0);
        public Task<bool> HasActiveForEquipmentAsync(int equipmentId, CancellationToken cancellationToken = default) => Task.FromResult(Loan is { Status: BorrowingStatus.Active });
        public Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
        {
            if (FailOnAdd) throw new IOException("sensitive provider details");
            Writes++; borrowing.AssignIdentity(1); Loan = borrowing; return Task.CompletedTask;
        }
        public Task UpdateAsync(Borrowing borrowing, CancellationToken cancellationToken = default) { Writes++; return Task.CompletedTask; }
        public BorrowEquipmentService BorrowService() => new(this, this, this, new Clock(), this);
        public ReturnEquipmentService ReturnService() => new(this, this, new Clock(), this);
        public void SeedActiveLoan()
        {
            Loan = new(1, 10, new(2026, 11, 14), new(2026, 11, 17));
            Loan.AssignIdentity(1); Item.MarkAsBorrowed();
        }
    }

    [Fact]
    public async Task ValidBorrowCommitsBothWritesOnceAndDisposesSession()
    {
        var session = new Session();
        var result = await session.BorrowService().ExecuteAsync(new(1, 10, new(2026, 11, 15)));
        Assert.True(result.Success);
        Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        Assert.Equal(2, session.Writes);
        Assert.Equal(1, session.Commits);
        Assert.True(session.Disposed);
    }

    [Fact]
    public async Task InvalidDateMakesNoWritesAndDoesNotCommit()
    {
        var session = new Session();
        var result = await session.BorrowService().ExecuteAsync(new(1, 10, new(2026, 11, 14)));
        Assert.Equal(OperationOutcome.Rejected, result.Outcome);
        Assert.Equal(0, session.Writes);
        Assert.Equal(0, session.Commits);
        Assert.True(session.Disposed);
    }

    [Fact]
    public async Task FailureAfterEquipmentWriteDoesNotCommitOrExposeProviderDetails()
    {
        var session = new Session { FailOnAdd = true };
        var result = await session.BorrowService().ExecuteAsync(new(1, 10, new(2026, 11, 15)));
        Assert.False(result.Success);
        Assert.Equal(OperationOutcome.Failed, result.Outcome);
        Assert.DoesNotContain("sensitive", result.Message);
        Assert.Equal(1, session.Writes);
        Assert.Equal(0, session.Commits);
        Assert.True(session.Disposed);
        // Real rollback/state is verified by PostgreSQL integration, not by this recording fake.
    }

    [Fact]
    public async Task UnknownCommitIsReportedWithoutSuccessOrReplay()
    {
        var session = new Session { UnknownCommit = true };
        var result = await session.BorrowService().ExecuteAsync(new(1, 10, new(2026, 11, 15)));
        Assert.False(result.Success);
        Assert.Equal(OperationOutcome.Unknown, result.Outcome);
        Assert.Equal(1, session.Commits);
        Assert.Equal(2, session.Writes);
        Assert.True(session.Disposed);
    }

    [Fact]
    public async Task ActiveLoanBlocksIssueEvenIfAvailabilityFlagIsStale()
    {
        var session = new Session(); session.SeedActiveLoan(); session.Item.MarkAsReturned();
        var result = await session.BorrowService().ExecuteAsync(new(1, 10, new(2026, 11, 15)));
        Assert.False(result.Success);
        Assert.Equal(0, session.Writes);
        Assert.Equal(0, session.Commits);
    }

    [Fact]
    public async Task RestrictedStudentCanReturnAndActualDateIsRecorded()
    {
        var session = new Session(); session.SeedActiveLoan(); session.Student.SetEligibility(false);
        var result = await session.ReturnService().ExecuteAsync(1);
        Assert.True(result.Success);
        Assert.Equal(new DateOnly(2026, 11, 14), session.Loan!.ActualReturnAt);
        Assert.True(session.Item.IsAvailable);
        Assert.Equal(2, session.Writes);
        Assert.Equal(1, session.Commits);
    }

    [Fact]
    public async Task RepeatedReturnDoesNotWriteOrCommit()
    {
        var session = new Session(); session.SeedActiveLoan(); session.Loan!.MarkAsReturned(new(2026, 11, 14));
        var result = await session.ReturnService().ExecuteAsync(1);
        Assert.False(result.Success);
        Assert.Equal(0, session.Writes);
        Assert.Equal(0, session.Commits);
    }
}
