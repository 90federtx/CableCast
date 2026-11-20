using System.Windows;
using CableCast.Services;
using CableCast.Views;

namespace CableCast;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private TrayIcon? _tray;
    private MainWindow? _window;
    private AppServices? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(true, $"Local\\{AppInfo.Name}.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            // Nessun crash: l'errore viene mostrato e l'app continua.
            System.Diagnostics.Debug.WriteLine(args.Exception);
            args.Handled = true;
        };

        _services = new AppServices();
        ApplyTheme(_services.Profiles.Theme);
        _services.Profiles.ThemeChanged += () => ApplyTheme(_services.Profiles.Theme);
        _window = new MainWindow(_services);
        _tray = new TrayIcon(_window);
        _window.Show();
        _services.Start();
        _ = CheckForUpdateAsync();
    }

    private async Task CheckForUpdateAsync()
    {
        try
        {
            var s = _services;
            if (s is null || !s.Profiles.CheckUpdates) return;
            var info = await UpdateChecker.CheckAsync(AppInfo.Version, s.Log);
            if (info is null) return;
            s.Log.Info(Loc.Format("Log_UpdateAvailable", info.Version));
            await Dispatcher.InvokeAsync(() => _window?.ShowUpdate(info));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    private static void ApplyTheme(string theme)
    {
        try
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(theme == "Light"
                ? Wpf.Ui.Appearance.ApplicationTheme.Light
                : Wpf.Ui.Appearance.ApplicationTheme.Dark);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
