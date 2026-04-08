namespace IV.ManagementHub.Web.Services;

public enum ConsoleLogLevel { Info, Warning, Error }

public sealed record ConsoleLogEntry(DateTime Timestamp, string Message, ConsoleLogLevel Level);

public sealed class ConsoleLogService
{
    private readonly List<ConsoleLogEntry> _entries = [];
    public IReadOnlyList<ConsoleLogEntry> Entries => _entries;

    public event Action? Changed;

    public void Log(string message, ConsoleLogLevel level = ConsoleLogLevel.Info)
    {
        _entries.Add(new ConsoleLogEntry(DateTime.Now, message, level));
        Changed?.Invoke();
    }

    public void Error(string message) => Log(message, ConsoleLogLevel.Error);
    public void Warning(string message) => Log(message, ConsoleLogLevel.Warning);

    public void Clear()
    {
        _entries.Clear();
        Changed?.Invoke();
    }
}
