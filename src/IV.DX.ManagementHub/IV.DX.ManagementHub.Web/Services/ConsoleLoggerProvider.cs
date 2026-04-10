using Microsoft.Extensions.Logging;

namespace IV.DX.ManagementHub.Web.Services;

internal sealed class ConsoleLoggerProvider(ConsoleLogBroadcaster broadcaster) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new ConsoleLogger(categoryName, broadcaster);

    public void Dispose() { }
}

internal sealed class ConsoleLogger(string category, ConsoleLogBroadcaster broadcaster) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        var isDxCategory =
            category.StartsWith("IV.DX", StringComparison.OrdinalIgnoreCase) ||
            category.StartsWith("MH.", StringComparison.OrdinalIgnoreCase);

        return isDxCategory
            ? logLevel >= LogLevel.Information
            : logLevel >= LogLevel.Warning;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var level = logLevel switch
        {
            LogLevel.Warning => ConsoleLogLevel.Warning,
            LogLevel.Error or LogLevel.Critical => ConsoleLogLevel.Error,
            _ => ConsoleLogLevel.Info
        };

        var lastDot = category.LastIndexOf('.');
        var shortCategory = lastDot >= 0 ? category[(lastDot + 1)..] : category;

        broadcaster.Broadcast(new ConsoleLogEntry(
            DateTime.Now,
            formatter(state, exception),
            level,
            Category: shortCategory,
            StackTrace: exception?.ToString()));
    }
}
