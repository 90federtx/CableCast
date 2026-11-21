using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private static readonly Regex EmailPattern = new(
        @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex NetworkAddressPattern = new(
        @"(?<![\d.])(?:\d{1,3}\.){3}\d{1,3}(?::\d+)?\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex NetworkAddressV6Pattern = new(
        @"(?<![0-9A-F:])(?:[0-9A-F]{0,4}:){2,7}[0-9A-F]{0,4}(?![0-9A-F:])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly object _lock = new();
    private readonly List<LogEntry> _entries = new();
    private readonly HashSet<string> _sensitiveValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly string? _logDirectory;
    private int _writesSincePrune;

    public LogService(string? logDirectory = null) => _logDirectory = logDirectory;

    public event Action<LogEntry>? Added;
    public event Action? Cleared;

    public void RegisterSensitiveValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        lock (_lock) _sensitiveValues.Add(value.Trim());
    }

    public void Add(LogLevel level, string message)
    {
        LogEntry entry;
        lock (_lock)
        {
            entry = new LogEntry(DateTime.Now, level, Redact(message));
            _entries.Add(entry);
            if (_entries.Count > MaxEntries) _entries.RemoveAt(0);
            try { AppendToFile(entry); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                System.Diagnostics.Trace.TraceError($"Impossibile scrivere il registro CableCast: {ex.Message}");
            }
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

    private string Redact(string message)
    {
        var text = message.Length > 16384 ? message[..16384] + "…" : message;
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profile))
            text = text.Replace(profile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        text = EmailPattern.Replace(text, "[email nascosta]");
        text = NetworkAddressPattern.Replace(text, "[indirizzo rete nascosto]");
        text = NetworkAddressV6Pattern.Replace(text, "[indirizzo rete nascosto]");
        foreach (var value in _sensitiveValues)
        {
            var pattern = $@"(?<![A-Za-z0-9]){Regex.Escape(value)}(?![A-Za-z0-9])";
            text = Regex.Replace(text, pattern, "[identificativo nascosto]",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        return text;
    }

    private void AppendToFile(LogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(_logDirectory)) return;
        Directory.CreateDirectory(_logDirectory);
        var date = entry.Time.ToString("yyyyMMdd");
        var path = Path.Combine(_logDirectory, $"CableCast-{date}.log");
        var line = entry + Environment.NewLine;
        if (File.Exists(path) && new FileInfo(path).Length + Encoding.UTF8.GetByteCount(line) > MaxFileBytes)
        {
            var archive = Path.Combine(_logDirectory,
                $"CableCast-{date}-{entry.Time:HHmmssfff}.log");
            File.Move(path, archive);
        }
        File.AppendAllText(path, line, new UTF8Encoding(false));
        if (++_writesSincePrune % 100 == 0) PruneOldFiles(entry.Time);
    }

    private void PruneOldFiles(DateTime now)
    {
        if (string.IsNullOrWhiteSpace(_logDirectory)) return;
        foreach (var path in Directory.EnumerateFiles(_logDirectory, "CableCast-*.log"))
        {
            if (File.GetLastWriteTimeUtc(path) < now.ToUniversalTime().AddDays(-14))
                File.Delete(path);
        }
    }
}
