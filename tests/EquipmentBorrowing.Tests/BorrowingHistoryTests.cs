using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Tests;

public class BorrowingHistoryTests
{
    private sealed class Queries : IHistoryQueries
    {
        public bool Fail { get; set; }
        public Task<IReadOnlyList<HistorySubject>> GetSubjectsAsync(HistoryKind kind, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Fail) throw new IOException("sensitive provider details");
            return Task.FromResult<IReadOnlyList<HistorySubject>>([new(1, "One"), new(2, "Two"), new(3, "Empty")]);
        }
        public Task<HistorySnapshot?> GetHistoryAsync(HistoryKind kind, int id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Fail) throw new IOException("sensitive provider details");
            if (id == 999) return Task.FromResult<HistorySnapshot?>(null);
            var rows = id == 3 ? Array.Empty<BorrowingDetails>() : new[] {
                new BorrowingDetails(id, id, "S"+id, "Student "+id, id, "E"+id, "Equipment "+id,
                    new(2026,11,14), new(2026,11,17), new(2026,11,15), BorrowingStatus.Returned),
                new BorrowingDetails(id+10, id, "S"+id, "Student "+id, id, "E"+id, "Equipment "+id,
                    new(2026,11,16), new(2026,11,18), null, BorrowingStatus.Active) };
            return Task.FromResult<HistorySnapshot?>(new(new(id, kind+" "+id), rows));
        }
    }

    [Theory]
    [InlineData(HistoryKind.Student)]
    [InlineData(HistoryKind.Equipment)]
    public async Task SubjectsAndHistoriesRemainDistinctWithBothStatuses(HistoryKind kind)
    {
        var service = new BorrowingHistoryService(new Queries());
        Assert.Equal(3, (await service.GetSubjectsAsync(kind)).Value!.Count);
        var first = await service.GetHistoryAsync(kind, 1);
        var second = await service.GetHistoryAsync(kind, 2);
        Assert.Equal(OperationOutcome.Succeeded, first.Outcome);
        Assert.All(first.Value!.Rows, row => Assert.Equal(1, kind == HistoryKind.Student ? row.StudentId : row.EquipmentId));
        Assert.All(second.Value!.Rows, row => Assert.Equal(2, kind == HistoryKind.Student ? row.StudentId : row.EquipmentId));
        Assert.Contains(first.Value.Rows, row => row.Status == BorrowingStatus.Returned && row.ActualReturnDate.HasValue);
        Assert.Contains(first.Value.Rows, row => row.Status == BorrowingStatus.Active && row.ActualReturnDate is null);
    }

    [Fact]
    public async Task EmptyMissingAndDatabaseErrorHaveDifferentOutcomes()
    {
        var queries = new Queries(); var service = new BorrowingHistoryService(queries);
        var empty = await service.GetHistoryAsync(HistoryKind.Student, 3);
        Assert.Equal(OperationOutcome.Succeeded, empty.Outcome); Assert.Empty(empty.Value!.Rows);
        var missing = await service.GetHistoryAsync(HistoryKind.Student, 999);
        Assert.Equal(OperationOutcome.Rejected, missing.Outcome); Assert.Null(missing.Value);
        queries.Fail = true;
        var failure = await service.GetHistoryAsync(HistoryKind.Student, 1);
        Assert.Equal(OperationOutcome.Failed, failure.Outcome); Assert.DoesNotContain("sensitive", failure.Message);
        Assert.False((await service.GetSubjectsAsync(HistoryKind.Student)).Success);
    }

    [Fact]
    public async Task CallerCancellationIsNotReportedAsDatabaseFailure()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var service = new BorrowingHistoryService(new Queries());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetHistoryAsync(HistoryKind.Student, 1, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetSubjectsAsync(HistoryKind.Student, cancellation.Token));
    }

    [Fact]
    public async Task OlderCompletionCannotReplaceLatestEvenIfProviderIgnoresCancellation()
    {
        using var gate = new LatestRequestGate();
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var published = new List<int>();
        async Task Read(Task<int> task)
        {
            var request = gate.Begin();
            var value = await task; // Deliberately ignores cancellation.
            if (request.IsCurrent) published.Add(value);
        }
        var oldRead = Read(first.Task); var newRead = Read(second.Task);
        second.SetResult(2); await newRead;
        first.SetResult(1); await oldRead;
        Assert.Equal(new[] { 2 }, published);
    }

    [Fact]
    public void DeactivationInvalidatesPendingReadsAndNavigationCanBeginAgain()
    {
        using var gate = new LatestRequestGate(); var old = gate.Begin();
        gate.Dispose(); Assert.False(old.IsCurrent); Assert.True(old.Token.IsCancellationRequested);
        var current = gate.Begin(); Assert.True(current.IsCurrent);
    }
}
