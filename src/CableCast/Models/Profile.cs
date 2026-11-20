using System.Text.Json.Serialization;

namespace CableCast.Models;

/// <summary>Un profilo = una copia "congelata" dei valori di <see cref="MirrorSettings"/> con un nome.</summary>
public sealed class Profile
{
    public string Name { get; set; } = "";
    public int MaxSize { get; set; }
    public int MaxFps { get; set; } = 60;
    public int VideoBitRateMbit { get; set; } = 8;
    public bool AudioEnabled { get; set; } = true;
    public bool TurnScreenOff { get; set; }
    public bool StayAwake { get; set; } = true;
    public bool AlwaysOnTop { get; set; }
    public bool Fullscreen { get; set; }
    public string WindowTitle { get; set; } = "";
    public GamepadMode Gamepad { get; set; } = GamepadMode.Uhid;
    public bool ControllerAsPs5 { get; set; }
    public string ButtonMap { get; set; } = "";

    ///<summary>I profili predefiniti non si cancellano e non si salvano nel file.</summary>
    [JsonIgnore] public bool BuiltIn { get; set; }

    public void ApplyTo(MirrorSettings s)
    {
        s.MaxSize = MaxSize;
        s.MaxFps = MaxFps;
        s.VideoBitRateMbit = VideoBitRateMbit;
        s.AudioEnabled = AudioEnabled;
        s.TurnScreenOff = TurnScreenOff;
        s.StayAwake = StayAwake;
        s.AlwaysOnTop = AlwaysOnTop;
        s.Fullscreen = Fullscreen;
        s.WindowTitle = WindowTitle ?? "";
        s.Gamepad = Enum.IsDefined(Gamepad) ? Gamepad : GamepadMode.Uhid;
        s.ControllerAsPs5 = ControllerAsPs5;
        s.ButtonMap = ButtonMap ?? "";
    }

    public static Profile From(string name, MirrorSettings s) => new()
    {
        Name = name,
        MaxSize = s.MaxSize,
        MaxFps = s.MaxFps,
        VideoBitRateMbit = s.VideoBitRateMbit,
        AudioEnabled = s.AudioEnabled,
        TurnScreenOff = s.TurnScreenOff,
        StayAwake = s.StayAwake,
        AlwaysOnTop = s.AlwaysOnTop,
        Fullscreen = s.Fullscreen,
        WindowTitle = s.WindowTitle,
        Gamepad = s.Gamepad,
        ControllerAsPs5 = s.ControllerAsPs5,
        ButtonMap = s.ButtonMap,
    };
}
