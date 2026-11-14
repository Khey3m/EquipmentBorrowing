using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Tests;

public class EquipmentManagementServiceTests
{
    private sealed class Store : IEquipmentManagementSessionFactory, IEquipmentManagementSession,
        IEquipmentManagementRepository, IWorkflowFailures
    {
        public Equipment Current { get; private set; } = new(10, "EQ-010", "Scope", "Original description");
        public IEquipmentManagementRepository Equipment => this;
        public int Writes { get; private set; }
        public int Commits { get; private set; }
        public bool Disposed { get; private set; }
        public bool UnknownCommit { get; set; }
        public EquipmentManagementService Service => new(this, this, this);
        public Task<IEquipmentManagementSession> OpenAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEquipmentManagementSession>(this);
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Commits++;
            if (UnknownCommit) throw new CommitOutcomeUnknownException();
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
        public OperationOutcome Classify(Exception error) => OperationOutcome.Failed;
        private Equipment Copy()
        {
            var copy = new Equipment(Current.Id, Current.EquipmentIdentifier, Current.Name, Current.Description);
            if (!Current.IsAvailable) copy.MarkAsBorrowed();
            return copy;
        }
        public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Equipment?>(id == Current.Id ? Copy() : null);
        public Task<IEnumerable<Equipment>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Equipment>>([Copy()]);
        public Task<bool> IdentifierExistsAsync(string identifier, int? excludingId = null, CancellationToken cancellationToken = default)
        {
            var key = DomainText.NormalizeIdentifier(identifier);
            return Task.FromResult(key == "TAKEN" || (excludingId != Current.Id && key == Current.NormalizedEquipmentIdentifier));
        }
        public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
        { Writes++; Current = equipment; return Task.CompletedTask; }
        public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
        { Writes++; Current = equipment; return Task.CompletedTask; }
    }

    [Fact]
    public async Task RegistrationTrimsValuesAndDefaultsToAvailable()
    {
        var store = new Store();
        var result = await store.Service.RegisterAsync(new(" new-1 ", " New Item ", " "));
        Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        Assert.Equal("new-1", result.Value!.EquipmentIdentifier);
        Assert.Equal("New Item", result.Value.Name);
        Assert.Null(result.Value.Description);
        Assert.True(result.Value.IsAvailable);
        Assert.Equal(1, store.Commits);
        Assert.True(store.Disposed);
    }

    [Theory]
    [InlineData(" eq-010 ", "Duplicate")]
    [InlineData(" ", "Name")]
    [InlineData("New", " ")]
    public async Task InvalidRegistrationDoesNotWriteOrCommit(string identifier, string name)
    {
        var store = new Store();
        var result = await store.Service.RegisterAsync(new(identifier, name, null));
        Assert.Equal(OperationOutcome.Rejected, result.Outcome);
        Assert.Equal(0, store.Writes);
        Assert.Equal(0, store.Commits);
    }

    [Fact]
    public async Task DescriptiveEditPreservesBorrowedAvailabilityAndIdentity()
    {
        var store = new Store(); store.Current.MarkAsBorrowed();
        var result = await store.Service.UpdateAsync(new(10, "EQ-011", "Renamed", " ", EquipmentSnapshot.From(store.Current)));
        Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        Assert.Equal(10, store.Current.Id);
        Assert.False(store.Current.IsAvailable);
        Assert.Null(store.Current.Description);
        Assert.Equal(1, store.Commits);
    }

    [Fact]
    public async Task AvailabilityChangedSinceSelectionRejectsStaleEdit()
    {
        var store = new Store(); var expected = EquipmentSnapshot.From(store.Current);
        store.Current.MarkAsBorrowed();
        var result = await store.Service.UpdateAsync(new(10, "NEW", "Overwrite", null, expected));
        Assert.Equal(OperationOutcome.Conflict, result.Outcome);
        Assert.Equal("Scope", store.Current.Name);
        Assert.False(store.Current.IsAvailable);
        Assert.Equal(0, store.Writes);
    }

    [Fact]
    public async Task MissingEquipmentRejectsWithoutWrites()
    {
        var store = new Store();
        var result = await store.Service.UpdateAsync(new(999, "NEW", "Name", null, EquipmentSnapshot.From(store.Current)));
        Assert.Equal(OperationOutcome.Conflict, result.Outcome);
        Assert.Equal(0, store.Writes);
    }

    [Fact]
    public async Task DuplicateEditPreservesExistingData()
    {
        var store = new Store();
        var result = await store.Service.UpdateAsync(new(10, " taken ", "Name", null, EquipmentSnapshot.From(store.Current)));
        Assert.Equal(OperationOutcome.Rejected, result.Outcome);
        Assert.Equal("EQ-010", store.Current.EquipmentIdentifier);
        Assert.Equal(0, store.Writes);
        Assert.Equal(0, store.Commits);
    }

    [Fact]
    public async Task UnknownCommitDoesNotReplayOrClaimSuccess()
    {
        var store = new Store { UnknownCommit = true };
        var result = await store.Service.RegisterAsync(new("NEW", "Item", null));
        Assert.Equal(OperationOutcome.Unknown, result.Outcome);
        Assert.Equal(1, store.Writes);
        Assert.Equal(1, store.Commits);
        Assert.True(store.Disposed);
    }

    [Fact]
    public async Task FindSupportsIdentifierNameAndEmptyResults()
    {
        var store = new Store();
        Assert.Single((await store.Service.FindAsync(" eq-010 ")).Value!);
        Assert.Single((await store.Service.FindAsync(" SCOPE ")).Value!);
        Assert.Empty((await store.Service.FindAsync("missing")).Value!);
    }
}
