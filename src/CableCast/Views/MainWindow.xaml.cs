using CableCast.Services;
using Wpf.Ui.Controls;

namespace CableCast.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(AppServices services)
    {
        InitializeComponent();
        _log = services.Log;
        Title = AppInfo.Name;

        RootNavigation.SetServiceProvider(services);
        services.Navigate = t => RootNavigation.Navigate(t);
        Loaded += (_, _) => RootNavigation.Navigate(typeof(HomePage));
    }

    private UpdateInfo? _update;
    private string? _installerPath;
    private CancellationTokenSource? _cts;
    private bool _downloading;
    private readonly LogService? _log;

    public void ShowUpdate(UpdateInfo info)
    {
        _update = info;
        UpdateText.Text = Loc.Format("Update_Available", info.Version, AppInfo.Version);
        UpdateBanner.Visibility = System.Windows.Visibility.Visible;
    }

    private void OnDismissUpdate(object sender, System.Windows.RoutedEventArgs e)
    {
        _cts?.Cancel();
        UpdateBanner.Visibility = System.Windows.Visibility.Collapsed;
    }

    private void OpenReleasePage()
    {
        try
        {
            if (_update?.Url is { } url &&
                UpdateChecker.TryCreateAllowedReleaseUri(url, out var uri) && uri is not null)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex) { _log?.Info(ex.Message); }
    }

    private async void OnDownloadUpdate(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (_installerPath is not null) { InstallAndExit(); return; }
            if (_downloading) { _cts?.Cancel(); return; }
            if (_update is null) return;
            if (_update.AssetUrl is null) { OpenReleasePage(); return; }

            _downloading = true;
            _cts = new CancellationTokenSource();
            UpdateAction.Content = Loc.Get("Update_Cancel");
            UpdateProgress.Value = 0;
            UpdateProgress.Visibility = System.Windows.Visibility.Visible;
            UpdateText.Text = Loc.Format("Update_Downloading", 0);
            var progress = new Progress<double>(p =>
            {
                UpdateProgress.Value = p;
                UpdateText.Text = Loc.Format("Update_Downloading", (int)(p * 100));
            });

            var path = await UpdateDownloader.DownloadAsync(_update, progress, _log, _cts.Token);
            _downloading = false;
            UpdateProgress.Visibility = System.Windows.Visibility.Collapsed;
            if (path is not null)
            {
                _installerPath = path;
                UpdateText.Text = Loc.Get("Update_Ready");
                UpdateAction.Content = Loc.Get("Update_Install");
            }
            else
            {
                var cancelled = _cts.IsCancellationRequested;
                UpdateAction.Content = Loc.Get("Update_Download");
                UpdateText.Text = cancelled
                    ? Loc.Format("Update_Available", _update.Version, AppInfo.Version)
                    : Loc.Get("Update_Failed");
                if (!cancelled) OpenReleasePage();
            }
        }
        catch (Exception ex)
        {
            _downloading = false;
            _log?.Info(ex.Message);
            UpdateText.Text = Loc.Get("Update_Failed");
        }
    }

    private void InstallAndExit()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_installerPath!) { UseShellExecute = true });
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _log?.Info(ex.Message);
            UpdateText.Text = Loc.Get("Update_Failed");
        }
    }
}
