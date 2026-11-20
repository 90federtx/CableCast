using CableCast.Services;
using Wpf.Ui.Controls;

namespace CableCast.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(AppServices services)
    {
        InitializeComponent();
        Title = AppInfo.Name;

        RootNavigation.SetServiceProvider(services);
        services.Navigate = t => RootNavigation.Navigate(t);
        Loaded += (_, _) => RootNavigation.Navigate(typeof(HomePage));
    }

    private string? _updateUrl;

    public void ShowUpdate(UpdateInfo info)
    {
        _updateUrl = info.Url;
        UpdateText.Text = Loc.Format("Update_Available", info.Version, AppInfo.Version);
        UpdateBanner.Visibility = System.Windows.Visibility.Visible;
    }

    private void OnDismissUpdate(object sender, System.Windows.RoutedEventArgs e)
        => UpdateBanner.Visibility = System.Windows.Visibility.Collapsed;

    private void OnDownloadUpdate(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (_updateUrl is { } url && url.StartsWith(UpdateChecker.UrlPrefix, StringComparison.Ordinal))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
}
