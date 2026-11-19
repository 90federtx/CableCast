namespace CableCast.Services;

/// <summary>Traduce i messaggi tecnici di adb/scrcpy in frasi comprensibili (chiavi in Strings.resx).</summary>
public static class ErrorTranslator
{
    private static readonly (string Pattern, string Key)[] Rules =
    {
        ("unauthorized", "Err_Unauthorized"),
        ("device offline", "Err_Offline"),
        ("could not find any adb device", "Err_NoDevice"),
        ("no devices/emulators found", "Err_NoDevice"),
        ("device disconnected", "Err_Disconnected"),
        ("device not found", "Err_NoDevice"),
        ("cannot connect to daemon", "Err_AdbDaemon"),
        ("adb server", "Err_AdbDaemon"),
        ("server connection failed", "Err_ServerConnection"),
        ("could not create uhid", "Err_Uhid"),
        ("uhid", "Err_Uhid"),
        ("libusb", "Err_Aoa"),
        ("aoa", "Err_Aoa"),
        ("could not open usb", "Err_Aoa"),
        ("mediacodec", "Err_Encoder"),
        ("encoder", "Err_Encoder"),
        ("could not open video stream", "Err_Encoder"),
        ("inject_events", "Err_InjectEvents"),
        ("could not inject", "Err_InjectEvents"),
    };

    /// <summary>Ritorna la frase tradotta, o null se il testo non è riconosciuto.</summary>
    public static string? Translate(string? raw)
    {
        var key = FindKey(raw);
        return key is null ? null : Loc.Get(key);
    }

    public static string? FindKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        foreach (var (pattern, key) in Rules)
            if (raw.Contains(pattern, StringComparison.OrdinalIgnoreCase)) return key;
        return null;
    }
}
