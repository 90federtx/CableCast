using System.Diagnostics;
using System.IO;
using System.Text;
using CableCast.Models;

namespace CableCast.Services;

public enum MirrorState { Stopped, Starting, Running, Stopping }

/// <summary>Gestisce il processo scrcpy (lunga durata). Non lancia mai eccezioni verso l'esterno.</summary>
public sealed class ScrcpyRunner : IDisposable
{
    private readonly LogService _log;
    private readonly object _lock = new();
    private readonly Queue<string> _stderrTail = new();
    private Process? _process;
    private bool _stopRequested;

    public ScrcpyRunner(LogService log) => _log = log;

    public MirrorState State { get; private set; } = MirrorState.Stopped;
    public event Action<MirrorState>? StateChanged;

    public bool IsActive => State != MirrorState.Stopped;

    private void SetState(MirrorState s)
    {
        State = s;
        try { StateChanged?.Invoke(s); } catch { /* un handler UI non deve mai far cadere il runner */ }
    }

    public void Start(AdbDevice device, MirrorSettings settings)
    {
        lock (_lock)
        {
            if (State != MirrorState.Stopped) return;

            var exe = Path.Combine(AppInfo.ScrcpyDir, "scrcpy.exe");
            if (!File.Exists(exe))
            {
                _log.Error(Loc.Format("Err_ScrcpyMissing", exe));
                return;
            }

            var invalid = ScrcpyArgsBuilder.Validate(settings, device);
            if (invalid is not null)
            {
                _log.Error(Loc.Get(invalid));
                return;
            }

            var args = ScrcpyArgsBuilder.Build(device.Serial, settings);
            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = AppInfo.ScrcpyDir,
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            if (settings.ControllerAsPs5)
            {
                try
                {
                    var pads = SdlControllers.Detect(_log);
                    if (pads.Count > 0)
                    {
                        psi.Environment["SDL_GAMECONTROLLERCONFIG"] = ControllerMapping.BuildConfig(pads, settings.ButtonMap);
                        psi.Environment["SDL_GAMECONTROLLER_USE_BUTTON_LABELS"] = "0";
                    }
                    _log.Info(Loc.Format("Log_ControllerPs5", pads.Count));
                }
                catch (Exception ex)
                {
                    _log.Warn(Loc.Format("Log_ControllerDetectFailed", ex.Message));
                }
            }

            _stderrTail.Clear();
            _stopRequested = false;
            SetState(MirrorState.Starting);
            _log.Info(Loc.Format("Log_Starting", device.DisplayName));
            _log.Raw("scrcpy " + string.Join(' ', args));

            try
            {
                var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
                p.OutputDataReceived += (_, e) => OnLine(e.Data, false);
                p.ErrorDataReceived += (_, e) => OnLine(e.Data, true);
                p.Exited += (_, _) => OnExited(p);
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                _process = p;
            }
            catch (Exception ex)
            {
                _process = null;
                _log.Error(Loc.Format("Err_StartFailed", ex.Message));
                SetState(MirrorState.Stopped);
            }
        }
    }

    private void OnLine(string? line, bool isErr)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        _log.Raw(line);
        lock (_lock)
        {
            if (isErr)
            {
                _stderrTail.Enqueue(line);
                while (_stderrTail.Count > 20) _stderrTail.Dequeue();
            }
            // scrcpy stampa "INFO: Device: ..." quando la sessione è avviata
            if (State == MirrorState.Starting &&
                (line.Contains("Renderer", StringComparison.OrdinalIgnoreCase) ||
                 line.Contains("Texture", StringComparison.OrdinalIgnoreCase) ||
                 line.Contains("Device:", StringComparison.OrdinalIgnoreCase)))
                SetState(MirrorState.Running);
        }
    }

    private void OnExited(Process p)
    {
        int code = -1;
        try { p.WaitForExit(); code = p.ExitCode; } catch { }

        string tail;
        bool requested;
        lock (_lock)
        {
            tail = string.Join(Environment.NewLine, _stderrTail);
            requested = _stopRequested;
            _process = null;
        }

        if (requested) _log.Info(Loc.Get("Log_Stopped"));
        else if (code == 0) _log.Info(Loc.Get("Log_ClosedByUser"));
        else if (code == 2) _log.Error(Loc.Get("Err_Disconnected"));
        else
        {
            var translated = ErrorTranslator.Translate(tail);
            _log.Error(translated ?? Loc.Format("Err_ScrcpyExit", code));
        }

        try { p.Dispose(); } catch { }
        lock (_lock) SetState(MirrorState.Stopped);
    }

    public void Stop()
    {
        Process? p;
        lock (_lock)
        {
            p = _process;
            if (p is null || State == MirrorState.Stopped) return;
            _stopRequested = true;
            SetState(MirrorState.Stopping);
        }

        _ = Task.Run(() =>
        {
            try
            {
                if (p.HasExited) return;
                p.CloseMainWindow();
                if (!p.WaitForExit(3000)) p.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _log.Warn(Loc.Format("Err_StopFailed", ex.Message));
                try { p.Kill(entireProcessTree: true); } catch { }
            }
        });
    }

    public void Dispose()
    {
        Process? p;
        lock (_lock) { p = _process; _stopRequested = true; }
        try { if (p is { HasExited: false }) p.Kill(entireProcessTree: true); } catch { }
    }
}
