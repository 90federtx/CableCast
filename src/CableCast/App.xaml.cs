using System.Windows;
using CableCast.Services;
using CableCast.Views;

namespace CableCast;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private EventWaitHandle? _showEvent;
    private TrayIcon? _tray;
    private MainWindow? _window;
    private AppServices? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(true, $"Local\\{AppInfo.Name}.SingleInstance", out var createdNew);
        var showName = $"Local\\{AppInfo.Name}.Show";
        if (!createdNew)
        {
            // Già aperta (magari nascosta nell'area di notifica): la riporto in primo piano.
            try { using var h = EventWaitHandle.OpenExisting(showName); h.Set(); } catch { }
            Shutdown();
            return;
        }
        try
        {
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, showName);
            var t = new Thread(() =>
            {
                try
                {
                    while (_showEvent.WaitOne())
                        Dispatcher.BeginInvoke(() => _tray?.Restore());
                }
                catch { }
            }) { IsBackground = true };
            t.Start();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

        DispatcherUnhandledException += (_, args) =>
        {
            var ex = args.Exception;
            _services?.Log.Error($"Eccezione UI non gestita: {ex.GetType().Name}: {ex.Message}");
            if (IsRecoverable(ex))
            {
                args.Handled = true;
                MessageBox.Show(
                    "Si è verificato un problema non critico. L'applicazione continuerà a funzionare, ma potrebbe essere necessario riprovare l'ultima operazione.",
                    "CableCast - Errore recuperabile",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                args.Handled = false;
                MessageBox.Show(
                    "Si è verificato un errore critico. CableCast verrà chiusa per evitare di lasciare lo stato dell'applicazione danneggiato.",
                    "CableCast - Errore critico",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        };

        try
        {
            _services = new AppServices();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore critico all'avvio: {ex}");
            MessageBox.Show(
                "CableCast non può essere avviata correttamente perché lo stato dell'applicazione non è disponibile.",
                "CableCast - Avvio interrotto",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }
        ApplyTheme(_services.Profiles.Theme);
        _services.Profiles.ThemeChanged += () => ApplyTheme(_services.Profiles.Theme);
        _window = new MainWindow(_services);
        _tray = new TrayIcon(_window);
        _window.Show();
        _services.Start();
        _ = CheckForUpdateAsync();
    }

    private static bool IsRecoverable(Exception ex) =>
        ex is System.IO.IOException or UnauthorizedAccessException or System.TimeoutException
        or System.Net.Http.HttpRequestException;

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
        _showEvent?.Dispose();
        base.OnExit(e);
    }
}
