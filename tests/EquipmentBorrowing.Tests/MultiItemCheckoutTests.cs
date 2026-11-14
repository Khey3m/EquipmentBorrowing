using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Tests;

public class MultiItemCheckoutTests
{
    private sealed class Clock : ICampusClock { public DateOnly Today => new(2026, 11, 14); }
    private sealed class FailAfterOne : ICheckoutWriteObserver
    {
        public int ObservedCount { get; private set; }
        public Task AfterItemPersistedAsync(int count, CancellationToken cancellationToken)
        { ObservedCount = count; throw new IOException("Controlled failure after writes"); }
    }
    private sealed class Store : IBorrowingSessionFactory, IBorrowingSession, IStudentRepository, IEquipmentRepository, IBorrowingRepository, IWorkflowFailures
    {
        public Student Student { get; } = new(1, "S1", "Student");
        public Dictionary<int, Equipment> Persisted { get; } = new() { [10] = new(10, "E10", "One"), [20] = new(20, "E20", "Two"), [30] = new(30, "E30", "Three") };
        private Dictionary<int, Equipment> _working = new();
        private readonly List<Borrowing> _pending = [];
        public List<Borrowing> Loans { get; } = [];
        public int ActiveCount { get; set; }
        public int? AlreadyActiveItem { get; set; }
        public int Writes { get; private set; }
        public int Commits { get; private set; }
        public bool Disposed { get; private set; }
        public bool UnknownCommit { get; set; }
        public IStudentRepository Students => this;
        public IEquipmentRepository Equipment => this;
        public IBorrowingRepository Borrowings => this;
        public MultiItemCheckoutService Service(ICheckoutWriteObserver? observer = null) => new(this, new Clock(), this, observer);
        public OperationOutcome Classify(Exception error) => OperationOutcome.Failed;
        public Task<IBorrowingSession> OpenAsync(CancellationToken cancellationToken = default)
        {
            _working = Persisted.ToDictionary(p => p.Key, p => {
                var copy = new Equipment(p.Key, p.Value.EquipmentIdentifier, p.Value.Name);
                if (!p.Value.IsAvailable) copy.MarkAsBorrowed();
                return copy;
            });
            return Task.FromResult<IBorrowingSession>(this);
        }
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Commits++;
            if (UnknownCommit) throw new CommitOutcomeUnknownException();
            foreach (var pair in _working) Persisted[pair.Key] = pair.Value;
            Loans.AddRange(_pending);
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
        Task<Student?> IStudentRepository.GetByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<Student?>(id == 1 ? Student : null);
        Task<IEnumerable<Student>> IStudentRepository.GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Student>>([Student]);
        Task<Equipment?> IEquipmentRepository.GetByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult(_working.GetValueOrDefault(id));
        Task<IEnumerable<Equipment>> IEquipmentRepository.GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Equipment>>(_working.Values);
        public Task UpdateAsync(Equipment item, CancellationToken cancellationToken = default) { Writes++; return Task.CompletedTask; }
        Task<Borrowing?> IBorrowingRepository.GetByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<Borrowing?>(null);
        public Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Borrowing>>(Loans);
        public Task<int> GetActiveCountByStudentIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(ActiveCount);
        public Task<bool> HasActiveForEquipmentAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(AlreadyActiveItem == id);
        public Task AddAsync(Borrowing loan, CancellationToken cancellationToken = default)
        { Writes++; loan.AssignIdentity(_pending.Count + 1); _pending.Add(loan); return Task.CompletedTask; }
        public Task UpdateAsync(Borrowing loan, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 2)]
    public async Task ValidRequestCommitsEveryItemExactlyAtLimit(int active, int count)
    {
        var store = new Store { ActiveCount = active };
        var result = await store.Service().ExecuteAsync(new(1, new[] { 30, 20, 10 }.Take(count).ToArray(), new(2026, 11, 15)));
        Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        Assert.Equal(count, store.Loans.Count);
        Assert.Equal(count * 2, store.Writes);
        Assert.Equal(1, store.Commits);
        Assert.Equal(count, store.Persisted.Values.Count(e => !e.IsAvailable));
        Assert.True(store.Disposed);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("one")]
    [InlineData("empty")]
    [InlineData("missing")]
    [InlineData("invalid-id")]
    [InlineData("restricted")]
    [InlineData("over-limit")]
    [InlineData("invalid-date")]
    [InlineData("unavailable")]
    [InlineData("active-loan")]
    [InlineData("missing-student")]
    public async Task InvalidRequestMakesNoWrites(string scenario)
    {
        var store = new Store();
        var ids = scenario switch { "duplicate" => new[] { 10, 10 }, "one" => new[] { 10 }, "empty" => [], "missing" => new[] { 10, 999 }, "invalid-id" => new[] { 10, 0 }, _ => new[] { 10, 20 } };
        if (scenario == "restricted") store.Student.SetEligibility(false);
        if (scenario == "over-limit") store.ActiveCount = 2;
        if (scenario == "unavailable") store.Persisted[20].MarkAsBorrowed();
        if (scenario == "active-loan") store.AlreadyActiveItem = 20;
        var result = await store.Service().ExecuteAsync(new(scenario == "missing-student" ? 999 : 1, ids,
            scenario == "invalid-date" ? new(2026, 11, 14) : new(2026, 11, 15)));
        Assert.NotEqual(OperationOutcome.Succeeded, result.Outcome);
        Assert.Equal(0, store.Writes);
        Assert.Equal(0, store.Commits);
        Assert.Empty(store.Loans);
    }

    [Fact]
    public async Task FailureAfterFirstPersistedItemDisposesWithoutCommit()
    {
        var store = new Store(); var observer = new FailAfterOne();
        var result = await store.Service(observer).ExecuteAsync(new(1, [10, 20], new(2026, 11, 15)));
        Assert.Equal(OperationOutcome.Failed, result.Outcome);
        Assert.Equal(1, observer.ObservedCount);
        Assert.Equal(2, store.Writes);
        Assert.Equal(0, store.Commits);
        Assert.Empty(store.Loans);
        Assert.All(store.Persisted.Values, e => Assert.True(e.IsAvailable));
        Assert.True(store.Disposed);
        // The recording store simulates transactional publication; PostgreSQL is tested separately.
    }

    [Fact]
    public async Task UnknownCommitDoesNotReplayWrites()
    {
        var store = new Store { UnknownCommit = true };
        var result = await store.Service().ExecuteAsync(new(1, [10, 20], new(2026, 11, 15)));
        Assert.Equal(OperationOutcome.Unknown, result.Outcome);
        Assert.Null(result.Value);
        Assert.Equal(4, store.Writes);
        Assert.Equal(1, store.Commits);
    }
}
