using CableCast.Models;

namespace CableCast.Services;

/// <summary>
/// Costruisce la lista di argomenti per scrcpy (funzione pura, nessuna concatenazione in stringa).
/// Le opzioni sono verificate con "scrcpy --help" della versione inclusa (v5.0.1).
/// </summary>
public static class ScrcpyArgsBuilder
{
    public static List<string> Build(string serial, MirrorSettings s)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);

        var args = new List<string> { $"--serial={serial}" };

        if (s.MaxSize > 0) args.Add($"--max-size={s.MaxSize}");
        if (s.MaxFps > 0) args.Add($"--max-fps={s.MaxFps}");
        if (s.VideoBitRateMbit > 0) args.Add($"--video-bit-rate={s.VideoBitRateMbit}M");

        if (!s.AudioEnabled) args.Add("--no-audio");
        if (s.TurnScreenOff) args.Add("--turn-screen-off");
        if (s.StayAwake) args.Add("--stay-awake");
        if (s.AlwaysOnTop) args.Add("--always-on-top");
        if (s.Fullscreen) args.Add("--fullscreen");

        var title = (s.WindowTitle ?? "").Trim();
        if (title.Length > 0) args.Add($"--window-title={title}");

        args.Add(s.Gamepad switch
        {
            GamepadMode.Uhid => "--gamepad=uhid",
            GamepadMode.Aoa => "--gamepad=aoa",
            _ => "--gamepad=disabled",
        });

        return args;
    }

    /// <summary>Restituisce la chiave di una stringa di errore se le impostazioni non sono utilizzabili con questo telefono, altrimenti null.</summary>
    public static string? Validate(MirrorSettings s, AdbDevice device) =>
        s.MaxSize is < 0 or > 7680 ||
        s.MaxFps is < 0 or > 240 ||
        s.VideoBitRateMbit is < 0 or > 64
            ? "Err_InvalidSettings"
            : s.Gamepad == GamepadMode.Aoa && !device.IsUsb
                ? "Err_AoaNeedsUsb"
                : null;
}
