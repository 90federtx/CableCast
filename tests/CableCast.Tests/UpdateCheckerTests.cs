using CableCast.Services;
using Xunit;

namespace CableCast.Tests;

public class UpdateCheckerTests
{
    [Theory]
    [InlineData("1.0.0", "v1.0.1", true)]
    [InlineData("1.0.0", "1.1.0", true)]
    [InlineData("1.0.0", "v1.0.0", false)]
    [InlineData("1.2.0", "v1.10.0", true)]
    [InlineData("1.2.0", "v1.1.9", false)]
    [InlineData("1.0.0", "v2.0.0-beta", true)]
    [InlineData("1.0.0", "boh", false)]
    [InlineData("1.0.0", null, false)]
    public void IsNewer_Compares(string current, string? tag, bool expected)
        => Assert.Equal(expected, UpdateChecker.IsNewer(current, tag));

    [Fact]
    public void Parse_ValidJson()
    {
        var info = UpdateChecker.Parse("{\"tag_name\":\"v1.2.3\",\"html_url\":\"https://github.com/90federtx/CableCast/releases/tag/v1.2.3\"}");
        Assert.NotNull(info);
        Assert.Equal("1.2.3", info!.Version);
    }

    private const string Rel = "https://github.com/90federtx/CableCast/releases/";

    [Fact]
    public void Parse_FindsInstallerAsset()
    {
        var json = "{\"tag_name\":\"v1.2.3\",\"html_url\":\"" + Rel + "tag/v1.2.3\",\"assets\":["
            + "{\"name\":\"notes.txt\",\"browser_download_url\":\"" + Rel + "download/v1.2.3/notes.txt\"},"
            + "{\"name\":\"CableCast-Setup-1.2.3.exe\",\"size\":1234,\"digest\":\"sha256:abcd\","
            + "\"browser_download_url\":\"" + Rel + "download/v1.2.3/CableCast-Setup-1.2.3.exe\"}]}";
        var info = UpdateChecker.Parse(json)!;
        Assert.Equal("CableCast-Setup-1.2.3.exe", info.AssetName);
        Assert.Equal(1234, info.AssetSize);
        Assert.Equal("abcd", info.AssetSha256);
        Assert.NotNull(info.AssetUrl);
    }

    [Fact]
    public void Parse_WithoutAssets_HasNoAssetUrl()
    {
        var info = UpdateChecker.Parse("{\"tag_name\":\"v1.2.3\",\"html_url\":\"" + Rel + "tag/v1.2.3\"}")!;
        Assert.Null(info.AssetUrl);
    }

    [Fact]
    public void Parse_IgnoresAssetFromForeignHost()
    {
        var json = "{\"tag_name\":\"v1.2.3\",\"html_url\":\"" + Rel + "tag/v1.2.3\",\"assets\":["
            + "{\"name\":\"CableCast-Setup.exe\",\"browser_download_url\":\"https://evil.example/CableCast-Setup.exe\"}]}";
        Assert.Null(UpdateChecker.Parse(json)!.AssetUrl);
    }

    [Theory]
    [InlineData(Rel + "download/v1/CableCast-Setup.exe", true)]
    [InlineData(Rel + "tag/v1", false)]
    [InlineData("http://github.com/90federtx/CableCast/releases/download/v1/a.exe", false)]
    [InlineData("https://evil.example/90federtx/CableCast/releases/download/v1/a.exe", false)]
    [InlineData(null, false)]
    public void IsAllowedAssetUri_Validates(string? url, bool expected)
        => Assert.Equal(expected, UpdateChecker.IsAllowedAssetUri(url));

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{\"tag_name\":\"v1.2.3\"}")]
    [InlineData("{\"tag_name\":\"v1.2.3\",\"html_url\":\"https://evil.example/x\"}")]
    [InlineData("{\"tag_name\":\"v1.2.3\",\"html_url\":\"https://github.com/evil/CableCast/releases/tag/v1.2.3\"}")]
    [InlineData("{\"tag_name\":\"v1.2.3\",\"html_url\":\"http://github.com/90federtx/CableCast/releases/tag/v1.2.3\"}")]
    [InlineData("{\"tag_name\":\"v1.2.3\",\"html_url\":\"https://github.com.evil.example/90federtx/CableCast/releases/tag/v1.2.3\"}")]
    public void Parse_RejectsInvalid(string json) => Assert.Null(UpdateChecker.Parse(json));

    [Theory]
    [InlineData("https://github.com/90federtx/CableCast/releases/tag/v1.2.3", true)]
    [InlineData("https://github.com/90federtx/CableCast", true)]
    [InlineData("https://github.com/evil/CableCast/releases/tag/v1.2.3", false)]
    [InlineData("http://github.com/90federtx/CableCast/releases/tag/v1.2.3", false)]
    [InlineData("not a url", false)]
    public void StrictRepositoryValidation(string text, bool expected)
    {
        Assert.Equal(expected, UpdateChecker.TryCreateAllowedReleaseUri(text, out _));
    }
}
