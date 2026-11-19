using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace CableCast.Services;

public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr, bool TimedOut, bool Started, string? StartError = null)
{
    public bool Success => Started && !TimedOut && ExitCode == 0;
}

/// <summary>Esegue un programma con argomenti passati come lista (mai concatenati in una stringa).</summary>
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string exePath, IReadOnlyList<string> args, TimeSpan timeout, CancellationToken ct = default);
}

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(string exePath, IReadOnlyList<string> args, TimeSpan timeout, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(exePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = System.IO.Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or System.IO.FileNotFoundException)
        {
            return new ProcessResult(-1, "", "", false, false, ex.Message);
        }

        var outTask = process.StandardOutput.ReadToEndAsync();
        var errTask = process.StandardError.ReadToEndAsync();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            if (ct.IsCancellationRequested) throw;
            return new ProcessResult(-1, "", "", true, true);
        }
        return new ProcessResult(process.ExitCode, await outTask.ConfigureAwait(false), await errTask.ConfigureAwait(false), false, true);
    }

    private static void TryKill(Process p)
    {
        try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { /* già terminato */ }
    }
}
