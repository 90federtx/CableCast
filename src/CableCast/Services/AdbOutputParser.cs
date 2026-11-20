using CableCast.Models;

namespace CableCast.Services;

/// <summary>Interpreta l'output testuale di adb. Funzioni pure, testabili senza telefono.</summary>
public static class AdbOutputParser
{
    /// <summary>Output di "adb devices -l".</summary>
    public static IReadOnlyList<AdbDevice> ParseDevices(string? output)
    {
        var result = new List<AdbDevice>();
        if (string.IsNullOrWhiteSpace(output)) return result;

        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith('*') || line.StartsWith("adb server", StringComparison.OrdinalIgnoreCase)) continue;

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            var serial = parts[0];
            var rest = 1;
            var state = parts[1];
            // Stato di due parole: "no permissions (...)".
            if (state == "no" && parts.Length > 2 && parts[2] == "permissions")
            {
                state = "no permissions";
                rest = 3;
                while (rest < parts.Length && !parts[rest].Contains(':')) rest++;
            }
            else rest = 2;

            string? model = null, product = null, transport = null;
            for (var i = rest; i < parts.Length; i++)
            {
                var kv = parts[i].Split(':', 2);
                if (kv.Length != 2) continue;
                switch (kv[0])
                {
                    case "model": model = kv[1]; break;
                    case "product": product = kv[1]; break;
                    case "transport_id": transport = kv[1]; break;
                }
            }
            result.Add(new AdbDevice(serial, state, model, product, transport));
        }
        return result;
    }

    /// <summary>Output di "adb shell getprop ro.product.model" o simili: prima riga non vuota.</summary>
    public static string? ParseSingleValue(string? output)
    {
        if (string.IsNullOrWhiteSpace(output)) return null;
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length > 0) return line;
        }
        return null;
    }

    public static AdbDeviceInfo ParseDeviceInfo(string? output)
    {
        if (string.IsNullOrWhiteSpace(output)) return AdbDeviceInfo.Empty;
        var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            var open = line.IndexOf('[');
            var closeKey = line.IndexOf("]: [", open + 1, StringComparison.Ordinal);
            if (open < 0 || closeKey < 0) continue;
            var key = line.Substring(open + 1, closeKey - open - 1);
            var valueStart = closeKey + 4;
            var valueEnd = line.LastIndexOf(']');
            if (valueStart < valueEnd)
                props[key] = line.Substring(valueStart, valueEnd - valueStart).Trim();
        }
        props.TryGetValue("ro.product.manufacturer", out var manufacturer);
        props.TryGetValue("ro.product.model", out var model);
        props.TryGetValue("ro.build.version.release", out var android);
        props.TryGetValue("ro.miui.ui.version.name", out var miui);
        props.TryGetValue("ro.mi.os.version.name", out var hyperOs);
        return new AdbDeviceInfo(
            string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
            string.IsNullOrWhiteSpace(model) ? null : model,
            string.IsNullOrWhiteSpace(android) ? null : android,
            string.IsNullOrWhiteSpace(miui) ? null : miui,
            string.IsNullOrWhiteSpace(hyperOs) ? null : hyperOs);
    }
}
