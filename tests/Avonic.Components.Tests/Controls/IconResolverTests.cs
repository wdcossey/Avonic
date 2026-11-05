using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>Ionicons' <c>getName</c>, <c>getSrc</c> and <c>isSrc</c> in <c>icon/utils.js</c>.</summary>
public class IconResolverTests
{
    [Theory]
    [InlineData("heart", null, null, "heart")]
    [InlineData("HEART", null, null, "heart")]
    [InlineData("add-circle-outline", null, null, "add-circle-outline")]
    [InlineData("a1-b2", null, null, "a1-b2")]
    [InlineData("heart", null, "star", "star")]
    [InlineData("heart", "moon", "star", "star")]
    [InlineData(null, null, "STAR", "star")]
    [InlineData("heart", "star", null, "heart")]
    [InlineData(null, "star", null, "star")]
    [InlineData("", "star", null, "star")]
    [InlineData(null, "STAR", null, "star")]
    public void GetName_Valid(string? name, string? icon, string? md, string expected) =>
        Assert.Equal(expected, IconResolver.GetName(name, icon, md));

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", "", "")]
    [InlineData("   ", null, null)]
    [InlineData("he_art", null, null)]
    [InlineData("he art", null, null)]
    [InlineData("heart.svg", null, null)]
    [InlineData(null, "a/b.svg", null)]
    [InlineData(null, null, "st_ar")]
    public void GetName_Invalid(string? name, string? icon, string? md) =>
        Assert.Null(IconResolver.GetName(name, icon, md));

    // An empty md falls through to name, as `md && mode === 'md'` does.
    [Fact]
    public void GetName_Empty_Md_Is_Ignored() => Assert.Equal("heart", IconResolver.GetName("heart", null, ""));

    [Theory]
    [InlineData("a/b.svg", "a/b.svg")]
    [InlineData("  a/b.svg  ", "a/b.svg")]
    [InlineData("b.svg", "b.svg")]
    [InlineData("data:image/svg+xml;utf8,<svg/>", "data:image/svg+xml;utf8,<svg/>")]
    public void GetSrc_Valid(string src, string expected) => Assert.Equal(expected, IconResolver.GetSrc(src));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("heart")]
    public void GetSrc_Invalid(string? src) => Assert.Null(IconResolver.GetSrc(src));
}
