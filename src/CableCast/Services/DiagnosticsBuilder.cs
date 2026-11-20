using System.Text;
using CableCast.Models;

namespace CableCast.Services;

/// <summary>Testo da incollare quando si chiede aiuto: versioni, sistema, stato del telefono, impostazioni, log recente.</summary>
public static class DiagnosticsBuilder
{
    public static string Build(DeviceStatus status, MirrorSettings s, string activeProfile, string logText, int maxLogLines = 100,
        AdbDeviceInfo? deviceInfo = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{AppInfo.Name} {AppInfo.Version}");
        sb.AppendLine($"scrcpy {AppInfo.ScrcpyVersion}");
        sb.AppendLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        sb.AppendLine($".NET: {Environment.Version}");
        sb.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine($"Stato telefono: {status.State}");
        if (status.Device is { } d)
        {
            sb.AppendLine($"Dispositivo: {d.Model ?? deviceInfo?.Model ?? "?"} (USB: {(d.IsUsb ? "sì" : "no")})");
            sb.AppendLine($"Produttore: {deviceInfo?.Manufacturer ?? d.Manufacturer ?? "?"}");
            sb.AppendLine($"Android: {deviceInfo?.AndroidVersion ?? d.AndroidVersion ?? "?"}");
            sb.AppendLine($"MIUI: {deviceInfo?.MiuiVersion ?? d.MiuiVersion ?? "n/d"}");
            sb.AppendLine($"HyperOS: {deviceInfo?.HyperOsVersion ?? d.HyperOsVersion ?? "n/d"}");
            var compat = XiaomiCompatibility.Diagnostic(d with
            {
                Manufacturer = deviceInfo?.Manufacturer,
                Model = d.Model ?? deviceInfo?.Model
            }, status);
            if (!string.IsNullOrEmpty(compat)) sb.AppendLine($"Compatibilità: {compat}");
        }
        if (!string.IsNullOrWhiteSpace(status.Detail)) sb.AppendLine($"Dettaglio: {status.Detail}");
        sb.AppendLine();
        sb.AppendLine($"Profilo attivo: {activeProfile}");
        sb.AppendLine($"Risoluzione max: {(s.MaxSize == 0 ? "originale" : s.MaxSize.ToString())}");
        sb.AppendLine($"FPS max: {s.MaxFps}  Bitrate: {s.VideoBitRateMbit} Mbit/s");
        sb.AppendLine($"Audio: {s.AudioEnabled}  Schermo spento: {s.TurnScreenOff}  Sempre acceso: {s.StayAwake}");
        sb.AppendLine($"Sempre in primo piano: {s.AlwaysOnTop}  Schermo intero: {s.Fullscreen}");
        sb.AppendLine($"Gamepad: {s.Gamepad}");
        sb.AppendLine();
        sb.AppendLine("--- Log recente ---");
        var lines = logText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var l in lines.Skip(Math.Max(0, lines.Length - maxLogLines)))
        {
            var safe = l;
            if (status.Device?.Serial is { Length: > 0 } serial)
                safe = safe.Replace(serial, "[serial nascosto]", StringComparison.Ordinal);
            sb.AppendLine(safe);
        }
        return sb.ToString();
    }
}
