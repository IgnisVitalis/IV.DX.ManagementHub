namespace IV.DX.ManagementHub.Web.Services;

public sealed class ConsoleLogBroadcaster
{
    public event Action<ConsoleLogEntry>? OnLog;

    public void Broadcast(ConsoleLogEntry entry) => OnLog?.Invoke(entry);
}
