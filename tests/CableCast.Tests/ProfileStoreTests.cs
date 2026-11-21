using System.IO;
using CableCast.Models;
using CableCast.Services;
using Xunit;

namespace CableCast.Tests;

public class ProfileStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cablecast-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    private ProfileStore Make(out MirrorSettings s)
    {
        s = new MirrorSettings();
        return new ProfileStore(FilePath, s);
    }

    [Fact]
    public void MissingFile_UsesDefaultProfile()
    {
        var store = Make(out var s);
        store.Load();
        Assert.Equal(store.Default.Name, store.ActiveName);
        Assert.Equal(1280, s.MaxSize);
    }

    [Fact]
    public void CorruptFile_DoesNotThrow_AndUsesDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, "{ non è json !!");
        var store = Make(out var s);
        store.Load();
        Assert.Equal(store.Default.Name, store.ActiveName);
        Assert.Equal(1280, s.MaxSize);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var a = Make(out var sa);
        a.Load();
        sa.MaxFps = 30;
        sa.Gamepad = GamepadMode.Aoa;
        sa.WindowTitle = "Prova";
        a.SaveAs("Mio");
        a.Dispose();

        var b = Make(out var sb);
        b.Load();
        Assert.Equal(30, sb.MaxFps);
        Assert.Equal(GamepadMode.Aoa, sb.Gamepad);
        Assert.Equal("Prova", sb.WindowTitle);
        Assert.Equal("Mio", b.ActiveName);
        Assert.Contains(b.Profiles, p => p.Name == "Mio" && !p.BuiltIn);
    }

    [Fact]
    public void Settings_AreSavedAsEnumString()
    {
        var store = Make(out var s);
        store.Load();
        Directory.CreateDirectory(_dir);
        s.Gamepad = GamepadMode.Aoa;
        store.Save();
        Assert.Contains("\"Aoa\"", File.ReadAllText(FilePath));
    }

    [Fact]
    public async Task ChangingSettings_SavesAutomatically()
    {
        var store = Make(out var s);
        store.Load();
        Directory.CreateDirectory(_dir);
        s.MaxFps = 24;
        await Task.Delay(800);
        Assert.True(File.Exists(FilePath));
        Assert.Contains("24", File.ReadAllText(FilePath));
    }

    [Fact]
    public void BuiltIn_CannotBeDeletedOrOverwritten()
    {
        var store = Make(out _);
        store.Load();
        var builtIn = store.Default;
        Assert.False(store.Delete(builtIn));
        Assert.Null(store.SaveAs(builtIn.Name));
        Assert.Contains(builtIn, store.Profiles);
    }

    [Fact]
    public void BlankName_IsRejected()
    {
        var store = Make(out _);
        store.Load();
        Assert.Null(store.SaveAs("   "));
    }

    [Fact]
    public void SaveAs_SameCustomName_ReplacesInsteadOfDuplicating()
    {
        var store = Make(out var s);
        store.Load();
        store.SaveAs("Mio");
        s.MaxFps = 15;
        store.SaveAs("mio");
        Assert.Single(store.Profiles, p => !p.BuiltIn);
        Assert.Equal(15, store.Profiles.Single(p => !p.BuiltIn).MaxFps);
    }

    [Fact]
    public void Delete_Custom_RevertsActiveToDefault()
    {
        var store = Make(out _);
        store.Load();
        var p = store.SaveAs("Mio")!;
        Assert.True(store.Delete(p));
        Assert.Equal(store.Default.Name, store.ActiveName);
        Assert.DoesNotContain(store.Profiles, x => x.Name == "Mio");
    }

    [Fact]
    public void Apply_CopiesValuesToSettings()
    {
        var store = Make(out var s);
        store.Load();
        var hq = store.Profiles.First(p => p.BuiltIn && p.MaxSize == 0);
        store.Apply(hq);
        Assert.Equal(0, s.MaxSize);
        Assert.Equal(16, s.VideoBitRateMbit);
        Assert.Equal(hq.Name, store.ActiveName);
    }

    [Fact]
    public void Load_IgnoresDuplicateAndBlankCustomProfiles()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, """
            { "CustomProfiles": [
                { "Name": "A", "MaxFps": 30 },
                { "Name": "a", "MaxFps": 45 },
                { "Name": "  " } ] }
            """);
        var store = Make(out _);
        store.Load();
        Assert.Single(store.Profiles, p => !p.BuiltIn);
        Assert.Equal(30, store.Profiles.Single(p => !p.BuiltIn).MaxFps);
    }

    [Fact]
    public void Load_InvalidGamepadValue_FallsBackToUhid()
    {
        var p = new Profile { Name = "x", Gamepad = (GamepadMode)99 };
        var s = new MirrorSettings();
        p.ApplyTo(s);
        Assert.Equal(GamepadMode.Uhid, s.Gamepad);
    }

    [Fact]
    public void Delete_ActiveCustomProfile_ReloadsDefaultSettings()
    {
        var store = Make(out var s);
        store.Load();
        var custom = store.SaveAs("Mio")!;
        s.MaxSize = 1920;
        s.MaxFps = 24;
        s.Gamepad = GamepadMode.Aoa;
        store.Apply(custom);
        s.MaxSize = 1920;

        Assert.True(store.Delete(custom));
        Assert.Equal(store.Default.Name, store.ActiveName);
        Assert.Equal(store.Default.MaxSize, s.MaxSize);
        Assert.Equal(store.Default.MaxFps, s.MaxFps);
        Assert.Equal(store.Default.Gamepad, s.Gamepad);
    }

    [Fact]
    public async Task Settings_SaveIsDebouncedAndFinalSaveIsImmediate()
    {
        var store = Make(out var s);
        store.Load();
        s.MaxFps = 24;
        await Task.Delay(100);
        s.MaxFps = 30;
        await Task.Delay(100);

        Assert.False(File.Exists(FilePath));

        store.Save();
        Assert.True(File.Exists(FilePath));
        Assert.Contains("\"MaxFps\": 30", File.ReadAllText(FilePath));
    }

    [Fact]
    public void InvalidPersistedNumbers_AreClampedToSafeValues()
    {
        var p = new Profile
        {
            Name = "Test",
            MaxSize = 99999,
            MaxFps = -100,
            VideoBitRateMbit = 999
        };
        var s = new MirrorSettings();
        p.ApplyTo(s);

        Assert.Equal(7680, s.MaxSize);
        Assert.Equal(1, s.MaxFps);
        Assert.Equal(64, s.VideoBitRateMbit);
    }

    [Fact]
    public void DismissedUpdate_IsPersistedAndOnlySuppressesThatVersion()
    {
        var first = Make(out _);
        first.Load();

        Assert.True(first.ShouldNotifyUpdate("1.1.5"));
        first.DismissUpdate("1.1.5");
        Assert.False(first.ShouldNotifyUpdate("1.1.5"));
        Assert.True(first.ShouldNotifyUpdate("1.1.6"));
        first.Dispose();

        var second = Make(out _);
        second.Load();
        Assert.False(second.ShouldNotifyUpdate("1.1.5"));
        Assert.True(second.ShouldNotifyUpdate("1.1.6"));
        second.Dispose();
    }
}

public class DiagnosticsBuilderTests
{
    [Fact]
    public void Output_HasNoSerial_AndTruncatesLog()
    {
        var dev = new AdbDevice("SERIAL123456", "device", "Pixel", null, null);
        var status = new DeviceStatus(DeviceState.Ready, dev);
        var log = string.Join("\n", Enumerable.Range(1, 499).Select(i => "riga" + i)) + "\nriga500 SERIAL123456";
        var text = DiagnosticsBuilder.Build(status, new MirrorSettings(), "Profilo", log, 100);
        Assert.DoesNotContain("SERIAL123456", text);
        Assert.Contains("Pixel", text);
        Assert.Contains("riga500", text);
        Assert.DoesNotContain("riga400\r", text);
        Assert.DoesNotContain("riga1\r", text);
    }
}
