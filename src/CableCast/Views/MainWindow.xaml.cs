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
}
