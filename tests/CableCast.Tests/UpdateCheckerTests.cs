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

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{\"tag_name\":\"v1.2.3\"}")]
    [InlineData("{\"tag_name\":\"v1.2.3\",\"html_url\":\"https://evil.example/x\"}")]
    public void Parse_RejectsInvalid(string json) => Assert.Null(UpdateChecker.Parse(json));
}
