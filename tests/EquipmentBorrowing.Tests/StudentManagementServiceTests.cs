using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Tests;

public class StudentManagementServiceTests
{
    private sealed class Store : IStudentManagementSessionFactory, IStudentManagementSession,
        IStudentManagementRepository, IWorkflowFailures
    {
        public Student Current { get; private set; } = new(7, "S-007", "Original", "contact", false);
        public IStudentManagementRepository Students => this;
        public int Writes { get; private set; }
        public int Commits { get; private set; }
        public bool Disposed { get; private set; }
        public bool UnknownCommit { get; set; }
        public StudentManagementService Service => new(this, this, this);
        public Task<IStudentManagementSession> OpenAsync(CancellationToken cancellationToken = default) => Task.FromResult<IStudentManagementSession>(this);
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Commits++;
            if (UnknownCommit) throw new CommitOutcomeUnknownException();
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
        public OperationOutcome Classify(Exception error) => OperationOutcome.Failed;
        private Student Copy() => new(Current.Id, Current.StudentNumber, Current.FullName, Current.ContactInformation, Current.IsEligibleToBorrow);
        public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Student?>(id == Current.Id ? Copy() : null);
        public Task<IEnumerable<Student>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Student>>([Copy()]);
        public Task<bool> NumberExistsAsync(string number, int? excludingId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(excludingId != Current.Id && DomainText.NormalizeIdentifier(number) == Current.NormalizedStudentNumber);
        public Task AddAsync(Student student, CancellationToken cancellationToken = default)
        { Writes++; Current = student; return Task.CompletedTask; }
        public Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
        { Writes++; Current = student; return Task.CompletedTask; }
    }

    [Fact]
    public async Task RegistrationTrimsValuesDefaultsEligibleAndCommitsOnce()
    {
        var store = new Store();
        var result = await store.Service.RegisterAsync(new(" new-1 ", " New Student ", " "));
        Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        Assert.Equal("new-1", result.Value!.StudentNumber);
        Assert.Equal("New Student", result.Value.FullName);
        Assert.Null(result.Value.ContactInformation);
        Assert.True(result.Value.IsEligibleToBorrow);
        Assert.Equal(1, store.Commits);
        Assert.True(store.Disposed);
    }

    [Theory]
    [InlineData(" s-007 ", "Duplicate")]
    [InlineData(" ", "Name")]
    [InlineData("New", " ")]
    public async Task InvalidRegistrationDoesNotWriteOrCommit(string number, string name)
    {
        var store = new Store();
        var result = await store.Service.RegisterAsync(new(number, name, null));
        Assert.Equal(OperationOutcome.Rejected, result.Outcome);
        Assert.Equal(0, store.Writes);
        Assert.Equal(0, store.Commits);
    }

    [Fact]
    public async Task UpdatePreservesIdentityAndEligibilityAndClearsOptionalContact()
    {
        var store = new Store();
        var result = await store.Service.UpdateAsync(new(7, "S-008", "Renamed", " ", StudentSnapshot.From(store.Current)));
        Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        Assert.Equal(7, store.Current.Id);
        Assert.False(store.Current.IsEligibleToBorrow);
        Assert.Null(store.Current.ContactInformation);
        Assert.Equal(1, store.Commits);
    }

    [Fact]
    public async Task StaleEditIsRejectedBeforeMutation()
    {
        var store = new Store();
        var stale = new StudentSnapshot("S-007", "Old name", "contact", false);
        var result = await store.Service.UpdateAsync(new(7, "NEW", "Overwrite", null, stale));
        Assert.Equal(OperationOutcome.Conflict, result.Outcome);
        Assert.Equal("Original", store.Current.FullName);
        Assert.Equal(0, store.Writes);
    }

    [Fact]
    public async Task EligibilityChangesDoNotEditDescriptiveFields()
    {
        var store = new Store();
        var result = await store.Service.SetEligibilityAsync(new(7, true, StudentSnapshot.From(store.Current)));
        Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        Assert.True(store.Current.IsEligibleToBorrow);
        Assert.Equal("S-007", store.Current.StudentNumber);
        Assert.Equal("Original", store.Current.FullName);
        Assert.Equal("contact", store.Current.ContactInformation);
        Assert.Equal(1, store.Commits);
    }

    [Fact]
    public async Task RepeatedEligibilityDoesNotWriteOrCommit()
    {
        var store = new Store();
        var result = await store.Service.SetEligibilityAsync(new(7, false, StudentSnapshot.From(store.Current)));
        Assert.Equal(OperationOutcome.Rejected, result.Outcome);
        Assert.Contains("already", result.Message);
        Assert.Equal(0, store.Writes);
        Assert.Equal(0, store.Commits);
    }

    [Fact]
    public async Task UnknownRegistrationCommitIsNotReplayedOrReportedAsSuccess()
    {
        var store = new Store { UnknownCommit = true };
        var result = await store.Service.RegisterAsync(new("New", "Name", null));
        Assert.Equal(OperationOutcome.Unknown, result.Outcome);
        Assert.Equal(1, store.Writes);
        Assert.Equal(1, store.Commits);
        Assert.True(store.Disposed);
    }

    [Fact]
    public async Task FindSupportsTrimmedCaseInsensitiveNumberAndNameAndEmptyResults()
    {
        var store = new Store();
        Assert.Single((await store.Service.FindAsync(" s-007 ")).Value!);
        Assert.Single((await store.Service.FindAsync(" ORIGINAL ")).Value!);
        Assert.Empty((await store.Service.FindAsync("missing")).Value!);
    }
}
