using CableCast.Services;
using Xunit;

namespace CableCast.Tests;

public class ErrorTranslatorTests
{
    [Theory]
    [InlineData("adb: device unauthorized.", "Err_Unauthorized")]
    [InlineData("error: device offline", "Err_Offline")]
    [InlineData("ERROR: Could not find any ADB device", "Err_NoDevice")]
    [InlineData("ERROR: Device disconnected", "Err_Disconnected")]
    [InlineData("Injecting input events requires INJECT_EVENTS permission", "Err_InjectEvents")]
    [InlineData("Could not forward gamepad input", "Err_Gamepad")]
    [InlineData("UHID initialization failed", "Err_Uhid")]
    public void Known_messages_map_to_keys(string raw, string key)
        => Assert.Equal(key, ErrorTranslator.FindKey(raw));

    [Fact]
    public void Unknown_message_returns_null()
        => Assert.Null(ErrorTranslator.FindKey("qualcosa di mai visto"));

    [Fact]
    public void Matching_is_case_insensitive()
        => Assert.Equal("Err_Unauthorized", ErrorTranslator.FindKey("DEVICE UNAUTHORIZED"));
}
