using System.Text;

namespace CableCast.Services;

public enum LogLevel { Info, Warning, Error, Raw }

public sealed record LogEntry(DateTime Time, LogLevel Level, string Message)
{
    public override string ToString() => $"{Time:HH:mm:ss} [{Level}] {Message}";
}

/// <summary>Registro in memoria (ultime 1000 righe). Thread-safe.</summary>
public sealed class LogService
{
    private const int MaxEntries = 1000;
    private readonly object _lock = new();
    private readonly List<LogEntry> _entries = new();

    public event Action<LogEntry>? Added;
    public event Action? Cleared;

    public void Add(LogLevel level, string message)
    {
        var entry = new LogEntry(DateTime.Now, level, message);
        lock (_lock)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries) _entries.RemoveAt(0);
        }
        Added?.Invoke(entry);
    }

    public void Info(string m) => Add(LogLevel.Info, m);
    public void Warn(string m) => Add(LogLevel.Warning, m);
    public void Error(string m) => Add(LogLevel.Error, m);
    public void Raw(string m) => Add(LogLevel.Raw, m);

    public IReadOnlyList<LogEntry> Snapshot()
    {
        lock (_lock) return _entries.ToArray();
    }

    public void Clear()
    {
        lock (_lock) _entries.Clear();
        Cleared?.Invoke();
    }

    public string AsText()
    {
        var sb = new StringBuilder();
        foreach (var e in Snapshot()) sb.AppendLine(e.ToString());
        return sb.ToString();
    }
}
