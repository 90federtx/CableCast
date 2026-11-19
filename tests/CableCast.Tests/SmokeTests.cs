using CableCast.Services;
using Xunit;

namespace CableCast.Tests;

public class SmokeTests
{
    [Fact]
    public void Localized_string_is_found() =>
        Assert.Equal("Telefono", Loc.Get("Nav_Home"));

    [Fact]
    public void Missing_key_is_marked() =>
        Assert.Equal("[Nope]", Loc.Get("Nope"));
}
