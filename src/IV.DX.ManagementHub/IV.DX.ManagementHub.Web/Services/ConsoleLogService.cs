namespace IV.DX.ManagementHub.Web.Services;

public enum ConsoleLogLevel { Info, Warning, Error }

public sealed record ConsoleLogEntry(
    DateTime Timestamp,
    string Message,
    ConsoleLogLevel Level,
    string? Category = null,
    string? StackTrace = null);

public sealed class ConsoleLogService : IDisposable
{
    private readonly ConsoleLogBroadcaster _broadcaster;
    private readonly List<ConsoleLogEntry> _entries = [];
    private readonly object _lock = new();

    public IReadOnlyList<ConsoleLogEntry> Entries
    {
        get { lock (_lock) { return _entries.ToList(); } }
    }

    public event Action? Changed;

    public ConsoleLogService(ConsoleLogBroadcaster broadcaster)
    {
        _broadcaster = broadcaster;
        _broadcaster.OnLog += Receive;
    }

    private void Receive(ConsoleLogEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }
        Changed?.Invoke();
    }

    public void Log(string message, ConsoleLogLevel level = ConsoleLogLevel.Info)
        => _broadcaster.Broadcast(new ConsoleLogEntry(DateTime.Now, message, level));

    public void Error(string message) => Log(message, ConsoleLogLevel.Error);
    public void Warning(string message) => Log(message, ConsoleLogLevel.Warning);

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _broadcaster.OnLog -= Receive;
    }
}
