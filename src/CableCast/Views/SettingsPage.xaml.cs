using System.Windows.Controls;
using CableCast.ViewModels;

namespace CableCast.Views;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}
