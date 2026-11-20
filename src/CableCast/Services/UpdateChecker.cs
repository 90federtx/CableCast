using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace CableCast.Services;

public sealed record UpdateInfo(string Version, string Url,
    string? AssetUrl = null, string? AssetName = null, long AssetSize = 0, string? AssetSha256 = null);

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
            var (aUrl, aName, aSize, aSha) = FindInstallerAsset(root);
            return new UpdateInfo(tag.TrimStart('v', 'V'), url, aUrl, aName, aSize, aSha);
        }
        catch { return null; }
    }

    private static (string? Url, string? Name, long Size, string? Sha) FindInstallerAsset(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) return default;
        foreach (var a in assets.EnumerateArray())
        {
            if (a.ValueKind != JsonValueKind.Object) continue;
            var name = a.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
            var url = a.TryGetProperty("browser_download_url", out var u) && u.ValueKind == JsonValueKind.String ? u.GetString() : null;
            if (name is null || url is null) continue;
            if (!name.Contains("Setup", StringComparison.OrdinalIgnoreCase) ||
                !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;
            if (!IsAllowedAssetUri(url)) continue;
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) continue;
            long size = a.TryGetProperty("size", out var sz) && sz.ValueKind == JsonValueKind.Number && sz.TryGetInt64(out var l) ? l : 0;
            string? sha = null;
            if (a.TryGetProperty("digest", out var d) && d.ValueKind == JsonValueKind.String)
            {
                var t = d.GetString();
                if (t is not null && t.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) sha = t[7..];
            }
            return (url, name, size, sha);
        }
        return default;
    }

    /// <summary>Solo allegati di release del repository ufficiale, via https.</summary>
    public static bool IsAllowedAssetUri(string? text)
        => TryCreateAllowedReleaseUri(text, out var uri) && uri is not null &&
           uri.AbsolutePath.StartsWith("/90federtx/CableCast/releases/download/", StringComparison.OrdinalIgnoreCase);

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
