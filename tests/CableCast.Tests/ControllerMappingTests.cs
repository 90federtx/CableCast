using System.IO;
using CableCast.Models;
using CableCast.Services;
using Xunit;

namespace CableCast.Tests;

public class ControllerMappingTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cablecast-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Parse_EmptyOrNull_GivesEmptyMap()
    {
        Assert.Empty(ControllerMapping.Parse(null));
        Assert.Empty(ControllerMapping.Parse("  "));
    }

    [Fact]
    public void Parse_IgnoresInvalidEntries()
    {
        var map = ControllerMapping.Parse("a:1,zzz:2,b:99,x:-1,y:abc,start:3:4,dpup:5");
        Assert.Equal(2, map.Count);
        Assert.Equal(1, map["a"]);
        Assert.Equal(5, map["dpup"]);
    }

    [Fact]
    public void Serialize_SkipsIdentity_AndRoundTrips()
    {
        var map = new Dictionary<string, int> { ["a"] = 0, ["b"] = 2, ["x"] = 1 };
        var text = ControllerMapping.Serialize(map);
        Assert.Equal("b:2,x:1", text);
        var back = ControllerMapping.Parse(text);
        Assert.Equal(2, back["b"]);
        Assert.Equal(1, back["x"]);
    }

    [Fact]
    public void BuildMapping_Identity_ListsAllButtonsAndAxes()
    {
        var text = ControllerMapping.BuildMapping("guid1", "Pad, X", new Dictionary<string, int>());
        Assert.StartsWith("guid1,Pad  X,", text);
        Assert.Contains("a:b0,", text);
        Assert.Contains("dpright:b14,", text);
        Assert.Contains("righttrigger:a5,", text);
    }

    [Fact]
    public void BuildMapping_AppliesCustomSource()
    {
        var text = ControllerMapping.BuildMapping("g", "n", new Dictionary<string, int> { ["a"] = 1, ["b"] = 0 });
        Assert.Contains("a:b1,", text);
        Assert.Contains("b:b0,", text);
    }

    [Fact]
    public void BuildConfig_OneLinePerController()
    {
        var cfg = ControllerMapping.BuildConfig(
            new[] { new ControllerInfo("g1", "A"), new ControllerInfo("g2", "B") }, "a:1");
        var lines = cfg.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("g1,A,", lines[0]);
        Assert.Contains("a:b1,", lines[1]);
    }

    [Fact]
    public void Profile_PersistsControllerFields()
    {
        var s = new MirrorSettings { ControllerAsPs5 = true, ButtonMap = "a:1" };
        var p = Profile.From("x", s);
        var s2 = new MirrorSettings();
        p.ApplyTo(s2);
        Assert.True(s2.ControllerAsPs5);
        Assert.Equal("a:1", s2.ButtonMap);
    }

    [Fact]
    public void Theme_IsPersisted_AndInvalidFallsBackToDark()
    {
        var a = new ProfileStore(FilePath, new MirrorSettings());
        a.Load();
        a.SetTheme("Light");
        a.Dispose();

        var b = new ProfileStore(FilePath, new MirrorSettings());
        b.Load();
        Assert.Equal("Light", b.Theme);
        b.SetTheme("Rosa");
        Assert.Equal("Dark", b.Theme);
    }
}
