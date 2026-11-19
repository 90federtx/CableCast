using System.Globalization;
using System.Resources;
using System.Windows.Markup;

namespace CableCast.Services;

/// <summary>Accesso alle stringhe localizzate (Resources\Strings.resx).</summary>
internal static class Loc
{
    private static readonly ResourceManager Manager =
        new("CableCast.Resources.Strings", typeof(Loc).Assembly);

    public static string Get(string key) =>
        Manager.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, Get(key), args);
}

/// <summary>Uso in XAML: Text="{loc:L Nav_Home}".</summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LExtension : MarkupExtension
{
    public LExtension() { }
    public LExtension(string key) => Key = key;
    public string Key { get; set; } = "";
    public override object ProvideValue(IServiceProvider serviceProvider) => Loc.Get(Key);
}
