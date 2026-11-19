using System.Windows.Controls;
using CableCast.ViewModels;

namespace CableCast.Views;

public partial class ProfilesPage : Page
{
    public ProfilesPage(ProfilesViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}
