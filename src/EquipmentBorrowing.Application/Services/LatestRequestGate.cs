namespace EquipmentBorrowing.Application.Services;

// Coordinates asynchronous read consumers; cancellation is advisory, versioning prevents stale publication.
public sealed class LatestRequestGate : IDisposable
{
    private CancellationTokenSource? _cancellation;
    private long _version;
    public Request Begin()
    {
        var version = Interlocked.Increment(ref _version);
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = new();
        return new(this, version, _cancellation.Token);
    }
    public sealed record Request(LatestRequestGate Owner, long Version, CancellationToken Token)
    {
        public bool IsCurrent => Interlocked.Read(ref Owner._version) == Version && !Token.IsCancellationRequested;
    }
    public void Dispose()
    {
        Interlocked.Increment(ref _version);
        _cancellation?.Cancel(); _cancellation?.Dispose(); _cancellation = null;
    }
}
