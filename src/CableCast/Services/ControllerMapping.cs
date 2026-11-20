using System.Text;

namespace CableCast.Services;

/// <summary>
/// Costruisce le stringhe di mappatura SDL (SDL_GAMECONTROLLERCONFIG) usate da scrcpy sul PC,
/// così qualunque controller (PS4, Switch Pro, Xbox...) si comporta con il layout di un DualSense.
/// Logica pura, testabile senza controller.
/// </summary>
public static class ControllerMapping
{
    /// <summary>Pulsanti nell'ordine di SDL_GamepadButton; l'indice è anche l'indice del pulsante "grezzo".</summary>
    public static readonly string[] Buttons =
    {
        "a", "b", "x", "y", "back", "guide", "start", "leftstick", "rightstick",
        "leftshoulder", "rightshoulder", "dpup", "dpdown", "dpleft", "dpright",
    };

    /// <summary>Mappa personalizzata: "bersaglio:origine" separati da virgola. Vuota = nessuna modifica.</summary>
    public static Dictionary<string, int> Parse(string? text)
    {
        var map = new Dictionary<string, int>();
        if (string.IsNullOrWhiteSpace(text)) return map;
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = part.Split(':');
            if (kv.Length != 2) continue;
            if (Array.IndexOf(Buttons, kv[0]) < 0) continue;
            if (!int.TryParse(kv[1], out var src) || src < 0 || src >= Buttons.Length) continue;
            map[kv[0]] = src;
        }
        return map;
    }

    /// <summary>Serializza solo le voci diverse dall'identità.</summary>
    public static string Serialize(IReadOnlyDictionary<string, int> map)
    {
        var parts = new List<string>();
        for (var i = 0; i < Buttons.Length; i++)
            if (map.TryGetValue(Buttons[i], out var src) && src != i)
                parts.Add($"{Buttons[i]}:{src}");
        return string.Join(',', parts);
    }

    public static string BuildMapping(string guid, string name, IReadOnlyDictionary<string, int> map)
    {
        var sb = new StringBuilder();
        sb.Append(guid).Append(',').Append(name.Replace(",", " ")).Append(',');
        for (var i = 0; i < Buttons.Length; i++)
        {
            var src = map.TryGetValue(Buttons[i], out var s) ? s : i;
            sb.Append(Buttons[i]).Append(":b").Append(src).Append(',');
        }
        sb.Append("leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,platform:Windows,");
        return sb.ToString();
    }

    public static string BuildConfig(IEnumerable<ControllerInfo> controllers, string customMap)
    {
        var map = Parse(customMap);
        return string.Join('\n', controllers.Select(c => BuildMapping(c.Guid, c.Name, map)));
    }
}

public sealed record ControllerInfo(string Guid, string Name);
