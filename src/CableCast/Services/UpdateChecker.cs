using System.Net.Http;
using System.Text.Json;

namespace CableCast.Services;

public sealed record UpdateInfo(string Version, string Url);

/// <summary>Controlla su GitHub se esiste una release più recente. Mai eccezioni verso l'esterno.</summary>
public static class UpdateChecker
{
    private const string ReleasesApi = "https://api.github.com/repos/90federtx/CableCast/releases/latest";
    public const string UrlPrefix = "https://github.com/90federtx/CableCast/";

    public static bool IsNewer(string current, string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        return TryParse(current, out var c) && TryParse(tag, out var t) && t > c;
    }

    private static bool TryParse(string text, out Version version)
    {
        var s = text.Trim().TrimStart('v', 'V');
        var cut = s.IndexOfAny(new[] { '-', '+' });
        if (cut >= 0) s = s[..cut];
        return Version.TryParse(s, out version!);
    }

    public static UpdateInfo? Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            var root = doc.RootElement;
            if (!root.TryGetProperty("tag_name", out var tagEl) || !root.TryGetProperty("html_url", out var urlEl)) return null;
            var tag = tagEl.GetString();
            var url = urlEl.GetString();
            if (string.IsNullOrWhiteSpace(tag) || url is null) return null;
            if (!TryCreateAllowedReleaseUri(url, out _)) return null;
            return new UpdateInfo(tag.TrimStart('v', 'V'), url);
        }
        catch { return null; }
    }

    public static bool TryCreateAllowedReleaseUri(string? text, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var parsed)) return false;
        if (!parsed.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
        if (!parsed.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)) return false;
        var path = parsed.AbsolutePath.TrimEnd('/');
        if (!path.Equals("/90federtx/CableCast", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/90federtx/CableCast/", StringComparison.OrdinalIgnoreCase)) return false;
        uri = parsed;
        return true;
    }

    public static async Task<UpdateInfo?> CheckAsync(string currentVersion, LogService? log = null)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CableCast");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            var json = await http.GetStringAsync(ReleasesApi).ConfigureAwait(false);
            var info = Parse(json);
            return info is not null && IsNewer(currentVersion, info.Version) ? info : null;
        }
        catch (Exception ex)
        {
            log?.Info(Loc.Format("Log_UpdateCheckFailed", ex.Message));
            return null;
        }
    }
}
