using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CableCast.Services;

/// <summary>Fa scorrere con la rotella un ScrollViewer (o il ScrollViewer interno di un controllo).</summary>
public static class WheelScroll
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(WheelScroll), new PropertyMetadata(false, OnChanged));

    public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject d, bool v) => d.SetValue(EnabledProperty, v);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement el) return;
        el.PreviewMouseWheel -= OnWheel;
        if ((bool)e.NewValue) el.PreviewMouseWheel += OnWheel;
    }

    private static void OnWheel(object sender, MouseWheelEventArgs e)
    {
        try
        {
            var sv = sender as ScrollViewer ?? Find((DependencyObject)sender);
            if (sv is null || sv.ScrollableHeight <= 0)
                sv = FindScrollableAncestor((DependencyObject)sender);
            if (sv is null) return;
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
        catch (Exception ex) when (ex is not null)
        {
            System.Diagnostics.Debug.WriteLine($"Scroll con rotella non riuscito: {ex.Message}"); // meglio ignorare: la rotella resta al comportamento standard
        }
    }

    private static ScrollViewer? FindScrollableAncestor(DependencyObject d)
    {
        for (var p = VisualTreeHelper.GetParent(d); p is not null; p = VisualTreeHelper.GetParent(p))
            if (p is ScrollViewer s && s.ScrollableHeight > 0) return s;
        return null;
    }

    private static ScrollViewer? Find(DependencyObject d)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
        {
            var c = VisualTreeHelper.GetChild(d, i);
            if (c is ScrollViewer s) return s;
            var r = Find(c);
            if (r is not null) return r;
        }
        return null;
    }
}
