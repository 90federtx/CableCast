using System.IO;
using CableCast.Models;

namespace CableCast.Services;

public interface IAdbService
{
    Task<AdbQueryResult> ListDevicesAsync(CancellationToken ct = default);
    Task<bool> RestartServerAsync(CancellationToken ct = default);
    Task<AdbDeviceInfo> GetDeviceInfoAsync(string serial, CancellationToken ct = default);
}

/// <summary>Usa l'adb incluso con scrcpy (stessa versione, nessun conflitto con altri adb).</summary>
public sealed class AdbService : IAdbService
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan ServerTimeout = TimeSpan.FromSeconds(15);

    private readonly IProcessRunner _runner;
    private readonly string _adbPath;

    public AdbService(IProcessRunner runner, string? adbPath = null)
    {
        _runner = runner;
        _adbPath = adbPath ?? Path.Combine(AppInfo.ScrcpyDir, "adb.exe");
    }

    /// <summary>Ferma il server adb avviato dall'app (altrimenti resta vivo e blocca adb.exe). Mai eccezioni.</summary>
    public static void KillServerBestEffort(LogService? log = null)
    {
        try
        {
            var path = Path.Combine(AppInfo.ScrcpyDir, "adb.exe");
            if (!File.Exists(path)) return;
            var psi = new System.Diagnostics.ProcessStartInfo(path)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            psi.ArgumentList.Add("kill-server");
            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null) return;
            if (!p.WaitForExit(3000)) { try { p.Kill(true); } catch { } }
        }
        catch (Exception ex)
        {
            try { log?.Info(ex.Message); } catch { }
        }
    }

    public async Task<AdbQueryResult> ListDevicesAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_adbPath))
            return AdbQueryResult.Failure($"adb non trovato: {_adbPath}");

        var r = await _runner.RunAsync(_adbPath, new[] { "devices", "-l" }, ShortTimeout, ct).ConfigureAwait(false);
        if (!r.Started) return AdbQueryResult.Failure(r.StartError ?? "adb non avviabile");
        if (r.TimedOut) return AdbQueryResult.Failure("adb non risponde (timeout)");
        if (r.ExitCode != 0) return AdbQueryResult.Failure(FirstNonEmpty(r.StdErr, r.StdOut) ?? $"adb ha restituito il codice {r.ExitCode}");
        return AdbQueryResult.Success(AdbOutputParser.ParseDevices(r.StdOut));
    }

    public async Task<bool> RestartServerAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_adbPath)) return false;
        var kill = await _runner.RunAsync(_adbPath, new[] { "kill-server" }, ServerTimeout, ct).ConfigureAwait(false);
        if (!kill.Started) return false;
        var start = await _runner.RunAsync(_adbPath, new[] { "start-server" }, ServerTimeout, ct).ConfigureAwait(false);
        return start.Success;
    }

    public async Task<AdbDeviceInfo> GetDeviceInfoAsync(string serial, CancellationToken ct = default)
    {
        if (!File.Exists(_adbPath) || string.IsNullOrWhiteSpace(serial))
            return AdbDeviceInfo.Empty;

        var r = await _runner.RunAsync(
            _adbPath,
            new[] { "-s", serial, "shell", "getprop" },
            ShortTimeout,
            ct).ConfigureAwait(false);
        if (!r.Success) return AdbDeviceInfo.Empty;
        return AdbOutputParser.ParseDeviceInfo(r.StdOut);
    }

    private static string? FirstNonEmpty(params string[] values) =>
        values.Select(v => v.Trim()).FirstOrDefault(v => v.Length > 0);
}
