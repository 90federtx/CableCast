using System.Windows;
using CableCast.Services;
using Forms = System.Windows.Forms;

namespace CableCast.Views;

/// <summary>Icona nell'area di notifica: chiudere la finestra la porta qui.</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Window _window;
    private bool _hintShown;

    public TrayIcon(Window window)
    {
        _window = window;
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(Loc.Get("Tray_Open"), null, (_, _) => Restore());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Loc.Get("Tray_Exit"), null, (_, _) => Application.Current.Shutdown());

        var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/app.ico"))!.Stream;
        _icon = new Forms.NotifyIcon
        {
            Icon = new System.Drawing.Icon(stream),
            Text = AppInfo.Name,
            Visible = true,
            ContextMenuStrip = menu,
        };
        _icon.DoubleClick += (_, _) => Restore();

        window.Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (Application.Current.ShutdownMode == ShutdownMode.OnExplicitShutdown) return;
        e.Cancel = true;
        _window.Hide();
        if (!_hintShown)
        {
            _hintShown = true;
            _icon.ShowBalloonTip(2000, AppInfo.Name, Loc.Format("Tray_StillRunning", AppInfo.Name), Forms.ToolTipIcon.Info);
        }
    }

    private void Restore()
    {
        _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    public void Dispose()
    {
        _window.Closing -= OnWindowClosing;
        _icon.Visible = false;
        _icon.Dispose();
    }
}
