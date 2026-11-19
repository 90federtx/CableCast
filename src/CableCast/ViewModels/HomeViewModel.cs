using System.Windows;
using CableCast.Models;
using CableCast.Services;
using CableCast.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CableCast.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly DeviceMonitor _monitor;
    private readonly ScrcpyRunner _runner;
    private readonly MirrorSettings _settings;
    private readonly LogService _log;
    private readonly Action<Type> _navigate;

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private DeviceState _state = DeviceState.Searching;
    [ObservableProperty] private bool _showWizardButton;
    [ObservableProperty] private string _mirrorButtonText = Loc.Get("Home_Start");

    public HomeViewModel(DeviceMonitor monitor, ScrcpyRunner runner, MirrorSettings settings, LogService log, Action<Type> navigate)
    {
        _monitor = monitor;
        _runner = runner;
        _settings = settings;
        _log = log;
        _navigate = navigate;
        Apply(monitor.Current);
        ApplyMirror(runner.State);
        monitor.StatusChanged += s => RunOnUi(() => Apply(s));
        runner.StateChanged += s => RunOnUi(() => ApplyMirror(s));
    }

    private void Apply(DeviceStatus s)
    {
        var (title, text) = DeviceTextMapper.Map(s);
        Title = title;
        Text = text;
        State = s.State;
        ShowWizardButton = DeviceTextMapper.NeedsWizard(s.State);
        StartStopCommand.NotifyCanExecuteChanged();

        // Telefono perso mentre si trasmette: si ferma da soli, senza lasciare scrcpy appeso.
        // AdbUnavailable e Searching sono volutamente esclusi: possono essere un singolo intoppo di adb.
        if (_runner.State is MirrorState.Running or MirrorState.Starting
            && s.State is DeviceState.NoDevice or DeviceState.UsbDebuggingOff
                or DeviceState.Unauthorized or DeviceState.Offline)
        {
            _log.Warn(Loc.Get("Log_PhoneLost"));
            _runner.Stop();
        }
    }

    private void ApplyMirror(MirrorState s)
    {
        MirrorButtonText = s switch
        {
            MirrorState.Starting => Loc.Get("Home_Starting"),
            MirrorState.Running => Loc.Get("Home_Stop"),
            MirrorState.Stopping => Loc.Get("Home_Stopping"),
            _ => Loc.Get("Home_Start"),
        };
        StartStopCommand.NotifyCanExecuteChanged();
    }

    private static void RunOnUi(Action a)
    {
        var d = Application.Current?.Dispatcher;
        if (d is null) return;
        d.BeginInvoke(a);
    }

    private bool CanStartStop() => _runner.State switch
    {
        MirrorState.Stopped => _monitor.Current.IsReady,
        MirrorState.Running or MirrorState.Starting => true,
        _ => false,
    };

    [RelayCommand(CanExecute = nameof(CanStartStop))]
    private void StartStop()
    {
        try
        {
            if (_runner.State == MirrorState.Stopped)
            {
                var device = _monitor.Current.Device;
                if (device is not null) _runner.Start(device, _settings);
            }
            else _runner.Stop();
        }
        catch (Exception ex)
        {
            _log.Error(Loc.Format("Err_StartFailed", ex.Message));
        }
    }

    [RelayCommand]
    private async Task CheckNowAsync() => await _monitor.RefreshAsync();

    [RelayCommand]
    private void OpenWizard() => _navigate(typeof(WizardPage));
}
