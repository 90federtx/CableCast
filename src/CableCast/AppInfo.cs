using System.IO;

namespace CableCast;

/// <summary>Costanti dell'applicazione. Il nome è definito solo qui.</summary>
internal static class AppInfo
{
    public const string Name = "CableCast";
    public const string Version = "1.0.0";
    public const string ScrcpyVersion = "5.0.1";
    public const string ScrcpyUrl = "https://github.com/Genymobile/scrcpy";
    public const string ScrcpySha256 = "B12A2C4EE8BE317422451FC7DCF8EE20A71B5EA7EF9AD73DDD825A25316227A5";

    public static string DataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Name);

    public static string SettingsPath => Path.Combine(DataDir, "settings.json");

    public static string ScrcpyDir => Path.Combine(AppContext.BaseDirectory, "tools", "scrcpy");
}
