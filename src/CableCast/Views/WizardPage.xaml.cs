using System.Windows.Controls;
using CableCast.ViewModels;

namespace CableCast.Views;

public partial class WizardPage : Page
{
    public WizardPage(WizardViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}
