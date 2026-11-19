using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CableCast.Services;

namespace CableCast.Views;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        VersionText.Text = Loc.Format("About_Version", AppInfo.Name, AppInfo.Version);
        ScrcpyText.Text = Loc.Format("About_ScrcpyLine", AppInfo.ScrcpyVersion);
        ShaText.Text = "SHA256: " + AppInfo.ScrcpySha256;
    }

    private void OpenScrcpy_Click(object sender, RoutedEventArgs e) => Open(AppInfo.ScrcpyUrl);

    private void OpenNotices_Click(object sender, RoutedEventArgs e) =>
        Open(Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.md"));

    private void OpenScrcpyLicense_Click(object sender, RoutedEventArgs e) =>
        Open(Path.Combine(AppInfo.ScrcpyDir, "LICENSE.txt"));

    private void Open(string target)
    {
        try
        {
            if (!target.StartsWith("http") && !File.Exists(target))
                throw new FileNotFoundException(target);
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = Loc.Format("About_OpenFailed", ex.Message);
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
