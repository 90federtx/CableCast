using CableCast.Models;
using CableCast.ViewModels;
using CableCast.Views;

namespace CableCast.Services;

/// <summary>Composition root minimale: un solo punto dove si creano i servizi.</summary>
public sealed class AppServices : IServiceProvider, IDisposable
{
    public DeviceMonitor Monitor { get; }
    public LogService Log { get; }
    public ScrcpyRunner Runner { get; }
    public MirrorSettings Settings { get; }
    public HomeViewModel Home { get; }
    public WizardViewModel Wizard { get; }
    public SettingsViewModel SettingsVm { get; }
    public LogViewModel LogVm { get; }
    public ProfileStore Profiles { get; }
    public ProfilesViewModel ProfilesVm { get; }
    public Action<Type>? Navigate { get; set; }

    public AppServices()
    {
        var runner = new ProcessRunner();
        var adb = new AdbService(runner);
        Log = new LogService();
        Settings = new MirrorSettings();
        Profiles = new ProfileStore(AppInfo.SettingsPath, Settings, Log);
        Profiles.Load();
        Runner = new ScrcpyRunner(Log);
        Monitor = new DeviceMonitor(adb, new UsbProbe());
        Home = new HomeViewModel(Monitor, Runner, Settings, Log, t => Navigate?.Invoke(t));
        Wizard = new WizardViewModel(Monitor, () => Navigate?.Invoke(typeof(HomePage)));
        SettingsVm = new SettingsViewModel(Settings, Profiles, Log);
        ProfilesVm = new ProfilesViewModel(Profiles);
        LogVm = new LogViewModel(Log,
            () => DiagnosticsBuilder.Build(Monitor.Current, Settings, Profiles.ActiveName, Log.AsText()));
    }

    public void Start() => Monitor.Start();

    /// <summary>Usato da WPF-UI per creare le pagine di navigazione.</summary>
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(HomePage)) return new HomePage(Home);
        if (serviceType == typeof(WizardPage)) return new WizardPage(Wizard);
        if (serviceType == typeof(SettingsPage)) return new SettingsPage(SettingsVm);
        if (serviceType == typeof(LogPage)) return new LogPage(LogVm);
        if (serviceType == typeof(ProfilesPage)) return new ProfilesPage(ProfilesVm);
        if (typeof(System.Windows.FrameworkElement).IsAssignableFrom(serviceType))
            return Activator.CreateInstance(serviceType);
        return null;
    }

    public void Dispose()
    {
        Monitor.Dispose();
        Runner.Dispose();
        Profiles.Dispose();
    }
}
