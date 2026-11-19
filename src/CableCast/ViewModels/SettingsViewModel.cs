using CableCast.Models;
using CableCast.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CableCast.ViewModels;

public sealed record IntOption(int Value, string Label);
public sealed record GamepadOption(GamepadMode Mode, string Label);

public sealed class SettingsViewModel : ObservableObject
{
    public MirrorSettings Settings { get; }

    public IReadOnlyList<IntOption> Resolutions { get; } = new[]
    {
        new IntOption(0, Loc.Get("Res_Original")),
        new IntOption(720, "720p"), new IntOption(1080, "1080p"), new IntOption(1280, "1280"),
        new IntOption(1440, "1440p"), new IntOption(1920, "1920"),
    };

    public IReadOnlyList<IntOption> FpsOptions { get; } = new[]
    {
        new IntOption(30, "30"), new IntOption(60, "60"), new IntOption(90, "90"), new IntOption(120, "120"),
    };

    public IReadOnlyList<IntOption> Bitrates { get; } = new[]
    {
        new IntOption(4, "4"), new IntOption(8, "8"), new IntOption(16, "16"), new IntOption(32, "32"),
    };

    public IReadOnlyList<GamepadOption> Gamepads { get; } = new[]
    {
        new GamepadOption(GamepadMode.Disabled, Loc.Get("Gamepad_Disabled")),
        new GamepadOption(GamepadMode.Uhid, Loc.Get("Gamepad_Uhid")),
        new GamepadOption(GamepadMode.Aoa, Loc.Get("Gamepad_Aoa")),
    };

    public SettingsViewModel(MirrorSettings settings) => Settings = settings;
}
