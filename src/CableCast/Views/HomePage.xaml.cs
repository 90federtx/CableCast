using System.Windows.Controls;
using CableCast.ViewModels;

namespace CableCast.Views;

public partial class HomePage : Page
{
    public HomePage(HomeViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}
