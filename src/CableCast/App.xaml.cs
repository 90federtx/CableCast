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
        _window = new MainWindow(_services);
        _tray = new TrayIcon(_window);
        _window.Show();
        _services.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
