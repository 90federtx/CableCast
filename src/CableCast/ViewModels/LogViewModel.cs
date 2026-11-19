using System.Collections.ObjectModel;
using System.Windows;
using CableCast.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CableCast.ViewModels;

public partial class LogViewModel : ObservableObject
{
    private readonly LogService _log;

    public ObservableCollection<LogEntry> Entries { get; } = new();

    private readonly Func<string> _diagnostics;

    public LogViewModel(LogService log, Func<string>? diagnostics = null)
    {
        _log = log;
        _diagnostics = diagnostics ?? log.AsText;
        foreach (var e in log.Snapshot()) Entries.Add(e);
        log.Added += e => RunOnUi(() =>
        {
            Entries.Add(e);
            while (Entries.Count > 1000) Entries.RemoveAt(0);
        });
        log.Cleared += () => RunOnUi(Entries.Clear);
    }

    private static void RunOnUi(Action a) => Application.Current?.Dispatcher.BeginInvoke(a);

    [RelayCommand]
    private void Clear() => _log.Clear();

    [RelayCommand]
    private void Copy()
    {
        try { Clipboard.SetText(_log.AsText()); } catch { /* clipboard occupata */ }
    }

    [RelayCommand]
    private void CopyDiagnostics()
    {
        try
        {
            Clipboard.SetText(_diagnostics());
            _log.Info(Loc.Get("Log_DiagnosticsCopied"));
        }
        catch { /* clipboard occupata */ }
    }
}
