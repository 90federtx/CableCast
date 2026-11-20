using System.Windows;
using System.Windows.Controls;
using CableCast.Services;
using Wpf.Ui.Controls;

namespace CableCast.Views;

public partial class PhonesPage : Page
{
    private sealed record Brand(string Id, string Name);

    private static readonly string[] BrandIds =
        ["xiaomi", "samsung", "pixel", "oneplus", "oppo", "vivo", "huawei", "other"];

    public PhonesPage()
    {
        InitializeComponent();
        try
        {
            BrandBox.ItemsSource = BrandIds.Select(id => new Brand(id, Loc.Get("Phones_Brand_" + id))).ToList();
            Fill(CommonPanel, Loc.Get("Phones_CommonSteps"));
            BrandBox.SelectedIndex = 0;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    private void OnBrandChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (BrandBox.SelectedItem is Brand b)
                Fill(StepsPanel, Loc.Get("Phones_Steps_" + b.Id));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    private static void Fill(Panel panel, string text)
    {
        panel.Children.Clear();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var number = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                VerticalAlignment = VerticalAlignment.Top,
                Background = (System.Windows.Media.Brush)Application.Current.FindResource("AccentFillColorDefaultBrush"),
                Child = new System.Windows.Controls.TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextOnAccentFillColorPrimaryBrush"),
                },
            };
            var label = new System.Windows.Controls.TextBlock
            {
                Text = lines[i],
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(14, 4, 0, 0),
            };
            label.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            var row = new DockPanel();
            DockPanel.SetDock(number, Dock.Left);
            row.Children.Add(number);
            row.Children.Add(label);
            panel.Children.Add(new Card { Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 0, 8), Content = row });
        }
    }
}
