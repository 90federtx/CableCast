using System.Collections.Specialized;
using System.Windows.Controls;
using CableCast.ViewModels;

namespace CableCast.Views;

public partial class LogPage : Page
{
    public LogPage(LogViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        ((INotifyCollectionChanged)vm.Entries).CollectionChanged += (_, _) =>
        {
            if (List.Items.Count > 0) List.ScrollIntoView(List.Items[^1]);
        };
    }
}
