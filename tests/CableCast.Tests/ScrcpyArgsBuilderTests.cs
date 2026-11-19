using CableCast.Models;
using CableCast.Services;
using Xunit;

namespace CableCast.Tests;

public class ScrcpyArgsBuilderTests
{
    private static MirrorSettings Plain() => new()
    {
        MaxSize = 0, MaxFps = 0, VideoBitRateMbit = 0, AudioEnabled = true,
        TurnScreenOff = false, StayAwake = false, AlwaysOnTop = false, Fullscreen = false,
        WindowTitle = "", Gamepad = GamepadMode.Disabled,
    };

    [Fact]
    public void Minimal_has_only_serial_and_gamepad()
    {
        var args = ScrcpyArgsBuilder.Build("ABC123", Plain());
        Assert.Equal(new[] { "--serial=ABC123", "--gamepad=disabled" }, args);
    }

    [Fact]
    public void Numeric_options_use_equals_form()
    {
        var s = Plain();
        s.MaxSize = 1080; s.MaxFps = 60; s.VideoBitRateMbit = 8;
        var args = ScrcpyArgsBuilder.Build("X", s);
        Assert.Contains("--max-size=1080", args);
        Assert.Contains("--max-fps=60", args);
        Assert.Contains("--video-bit-rate=8M", args);
    }

    [Fact]
    public void Audio_off_adds_no_audio()
    {
        var s = Plain(); s.AudioEnabled = false;
        Assert.Contains("--no-audio", ScrcpyArgsBuilder.Build("X", s));
    }

    [Fact]
    public void Boolean_flags_are_added_when_enabled()
    {
        var s = Plain();
        s.TurnScreenOff = true; s.StayAwake = true; s.AlwaysOnTop = true; s.Fullscreen = true;
        var args = ScrcpyArgsBuilder.Build("X", s);
        Assert.Contains("--turn-screen-off", args);
        Assert.Contains("--stay-awake", args);
        Assert.Contains("--always-on-top", args);
        Assert.Contains("--fullscreen", args);
    }

    [Theory]
    [InlineData(GamepadMode.Uhid, "--gamepad=uhid")]
    [InlineData(GamepadMode.Aoa, "--gamepad=aoa")]
    [InlineData(GamepadMode.Disabled, "--gamepad=disabled")]
    public void Gamepad_modes(GamepadMode mode, string expected)
    {
        var s = Plain(); s.Gamepad = mode;
        Assert.Contains(expected, ScrcpyArgsBuilder.Build("X", s));
    }

    [Fact]
    public void Title_with_spaces_stays_a_single_argument()
    {
        var s = Plain(); s.WindowTitle = "  Il mio telefono  ";
        Assert.Contains("--window-title=Il mio telefono", ScrcpyArgsBuilder.Build("X", s));
    }

    [Fact]
    public void Blank_title_is_omitted()
    {
        var s = Plain(); s.WindowTitle = "   ";
        Assert.DoesNotContain(ScrcpyArgsBuilder.Build("X", s), a => a.StartsWith("--window-title"));
    }

    [Fact]
    public void Serial_with_spaces_stays_a_single_argument()
    {
        var args = ScrcpyArgsBuilder.Build("192.168.1.5:5555 x", Plain());
        Assert.Equal("--serial=192.168.1.5:5555 x", args[0]);
    }

    [Fact]
    public void Aoa_over_wifi_is_rejected()
    {
        var s = Plain(); s.Gamepad = GamepadMode.Aoa;
        var wifi = new AdbDevice("192.168.1.5:5555", "device", null, null, null);
        Assert.Equal("Err_AoaNeedsUsb", ScrcpyArgsBuilder.Validate(s, wifi));
    }

    [Fact]
    public void Aoa_over_usb_is_accepted()
    {
        var s = Plain(); s.Gamepad = GamepadMode.Aoa;
        var usb = new AdbDevice("R58M123ABC", "device", null, null, null);
        Assert.Null(ScrcpyArgsBuilder.Validate(s, usb));
    }
}
