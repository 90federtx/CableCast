using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace CableCast.Services;

/// <summary>Scarica l'installer della nuova versione. Mai eccezioni verso l'esterno: ritorna null e scrive nel log.</summary>
public static class UpdateDownloader
{
    public static async Task<string?> DownloadAsync(UpdateInfo info, IProgress<double>? progress,
        LogService? log, CancellationToken ct)
    {
        string? target = null;
        try
        {
            if (info.AssetUrl is null || info.AssetName is null || !UpdateChecker.IsAllowedAssetUri(info.AssetUrl))
                return null;

            var dir = Path.Combine(Path.GetTempPath(), "CableCast-Update");
            Directory.CreateDirectory(dir);
            target = Path.Combine(dir, info.AssetName);
            if (File.Exists(target)) File.Delete(target);

            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CableCast");
            using var resp = await http.GetAsync(info.AssetUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? info.AssetSize;

            using (var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            using (var dst = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buf = new byte[81920];
                long done = 0;
                int n;
                while ((n = await src.ReadAsync(buf, ct).ConfigureAwait(false)) > 0)
                {
                    await dst.WriteAsync(buf.AsMemory(0, n), ct).ConfigureAwait(false);
                    done += n;
                    if (total > 0) progress?.Report((double)done / total);
                }
            }

            var len = new FileInfo(target).Length;
            if (info.AssetSize > 0 && len != info.AssetSize) throw new IOException("size mismatch");
            if (info.AssetSha256 is { Length: > 0 } expected)
            {
                using var fs = File.OpenRead(target);
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(fs, ct).ConfigureAwait(false));
                if (!hash.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new IOException("sha256 mismatch");
            }
            return target;
        }
        catch (Exception ex)
        {
            log?.Info(Loc.Format("Log_UpdateDownloadFailed", ex.Message));
            try { if (target is not null && File.Exists(target)) File.Delete(target); } catch { }
            return null;
        }
    }
}
