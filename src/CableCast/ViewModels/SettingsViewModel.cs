using System.Collections.ObjectModel;
using CableCast.Models;
using CableCast.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CableCast.ViewModels;

public sealed record IntOption(int Value, string Label);
public sealed record GamepadOption(GamepadMode Mode, string Label);
public sealed record ThemeOption(string Value, string Label);

/// <summary>Una riga della mappa tasti: "il tasto PS5 X viene dal pulsante fisico Y".</summary>
public sealed class ButtonRow : ObservableObject
{
    private readonly Action<string, int> _changed;
    private int _source;

    public ButtonRow(int index, IReadOnlyList<IntOption> options, int source, Action<string, int> changed)
    {
        Key = ControllerMapping.Buttons[index];
        Label = Loc.Get("Ps5_" + Key);
        Options = options;
        _source = source;
        _changed = changed;
    }

    public string Key { get; }
    public string Label { get; }
    public IReadOnlyList<IntOption> Options { get; }

    public int Source
    {
        get => _source;
        set
        {
            if (!SetProperty(ref _source, value)) return;
            _changed(Key, value);
        }
    }
}

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ProfileStore _profiles;
    private readonly LogService _log;
    private bool _loadingMap;

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

    public IReadOnlyList<ThemeOption> Themes { get; } = new[]
    {
        new ThemeOption("Light", Loc.Get("Theme_Light")),
        new ThemeOption("Dark", Loc.Get("Theme_Dark")),
    };

    public ObservableCollection<ButtonRow> ButtonRows { get; } = new();

    [ObservableProperty] private string _detectResult = "";

    public SettingsViewModel(MirrorSettings settings, ProfileStore profiles, LogService log)
    {
        Settings = settings;
        _profiles = profiles;
        _log = log;
        LoadRows();
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MirrorSettings.ButtonMap) && !_loadingMap) LoadRows();
        };
        _profiles.ActiveChanged += () => { if (!_loadingMap) LoadRows(); };
    }

    public string Theme
    {
        get => _profiles.Theme;
        set
        {
            if (value == _profiles.Theme) return;
            _profiles.SetTheme(value);
            OnPropertyChanged();
        }
    }

    public bool CheckUpdates
    {
        get => _profiles.CheckUpdates;
        set
        {
            if (value == _profiles.CheckUpdates) return;
            _profiles.SetCheckUpdates(value);
            OnPropertyChanged();
        }
    }

    private void LoadRows()
    {
        _loadingMap = true;
        try
        {
            var map = ControllerMapping.Parse(Settings.ButtonMap);
            var options = ControllerMapping.Buttons
                .Select((k, i) => new IntOption(i, Loc.Get("Ps5_" + k)))
                .ToList();
            ButtonRows.Clear();
            for (var i = 0; i < ControllerMapping.Buttons.Length; i++)
            {
                var src = map.TryGetValue(ControllerMapping.Buttons[i], out var s) ? s : i;
                ButtonRows.Add(new ButtonRow(i, options, src, OnRowChanged));
            }
        }
        finally { _loadingMap = false; }
    }

    private void OnRowChanged(string key, int source)
    {
        if (_loadingMap) return;
        try
        {
            var map = ControllerMapping.Parse(Settings.ButtonMap);
            map[key] = source;
            _loadingMap = true;
            try { Settings.ButtonMap = ControllerMapping.Serialize(map); }
            finally { _loadingMap = false; }
        }
        catch (Exception ex) { _log.Warn(Loc.Format("Log_ControllerDetectFailed", ex.Message)); }
    }

    [RelayCommand]
    private void ResetMap() => Settings.ButtonMap = "";

    [RelayCommand]
    private void DetectControllers()
    {
        try
        {
            var pads = SdlControllers.Detect(_log);
            DetectResult = pads.Count == 0
                ? Loc.Get("Controller_None")
                : Loc.Format("Controller_Found", pads.Count, string.Join(", ", pads.Select(p => p.Name)));
        }
        catch (Exception ex)
        {
            _log.Warn(Loc.Format("Log_ControllerDetectFailed", ex.Message));
            DetectResult = Loc.Get("Controller_None");
        }
    }
}
