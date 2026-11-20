using CommunityToolkit.Mvvm.ComponentModel;

namespace CableCast.Models;

public enum GamepadMode { Disabled, Uhid, Aoa }

/// <summary>Impostazioni dello mirroring. 0 = "nessun limite" per risoluzione e FPS.</summary>
public partial class MirrorSettings : ObservableObject
{
    [ObservableProperty] private int _maxSize;
    [ObservableProperty] private int _maxFps = 60;
    [ObservableProperty] private int _videoBitRateMbit = 8;
    [ObservableProperty] private bool _audioEnabled = true;
    [ObservableProperty] private bool _turnScreenOff;
    [ObservableProperty] private bool _stayAwake = true;
    [ObservableProperty] private bool _alwaysOnTop;
    [ObservableProperty] private bool _fullscreen;
    [ObservableProperty] private string _windowTitle = "";
    [ObservableProperty] private GamepadMode _gamepad = GamepadMode.Uhid;
    [ObservableProperty] private bool _controllerAsPs5;
    [ObservableProperty] private string _buttonMap = "";
}
