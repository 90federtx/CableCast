using System.Windows;
using CableCast.Models;
using CableCast.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CableCast.ViewModels;

public partial class WizardViewModel : ObservableObject
{
    private readonly DeviceMonitor _monitor;
    private readonly Action _back;

    [ObservableProperty] private string _statusLine = "";

    public WizardViewModel(DeviceMonitor monitor, Action back)
    {
        _monitor = monitor;
        _back = back;
        Apply(monitor.Current);
        monitor.StatusChanged += s => Application.Current?.Dispatcher.BeginInvoke(() => Apply(s));
    }

    private void Apply(DeviceStatus s) =>
        StatusLine = Loc.Format("Wizard_Status", DeviceTextMapper.Map(s).Title);

    [RelayCommand]
    private async Task CheckNowAsync() => await _monitor.RefreshAsync();

    [RelayCommand]
    private void Back() => _back();
}
