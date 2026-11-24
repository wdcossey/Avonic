using Avonic.Components.Overlays;

namespace Avonic.Components.Tests.Overlays;

/// <summary>
/// <c>htmlAttributes</c> as Ionic's spread gives it to the alert, the action sheet, the toast and the loading: a missing or null
/// key isn't there, a value is written as text, an empty <c>id</c> or <c>aria-label</c> names nothing, and <c>role</c> and
/// <c>aria-checked</c> are compared as the values themselves, as <c>action-sheet.js</c> reads them.
/// </summary>
public class OverlayAttributesTests
{
    private static Dictionary<string, object?> Attributes(string key, object? value) => new() { [key] = value };

    [Fact]
    public void Read_Gives_A_String_As_It_Is()
    {
        Assert.Equal("Close the toast", OverlayAttributes.Read(Attributes("aria-label", "Close the toast"), "aria-label"));
    }

    [Fact]
    public void Read_Keeps_An_Empty_String_As_The_Spread_Sets_An_Empty_Attribute()
    {
        Assert.Equal("", OverlayAttributes.Read(Attributes("aria-label", ""), "aria-label"));
    }

    [Fact]
    public void Read_Gives_A_Number_As_Its_Digits()
    {
        Assert.Equal("3", OverlayAttributes.Read(Attributes("maxlength", 3), "maxlength"));
    }

    [Fact]
    public void Read_Gives_Nothing_For_A_Missing_Or_Null_Key_Or_No_Dictionary()
    {
        Assert.Null(OverlayAttributes.Read(Attributes("id", "x"), "aria-label"));
        Assert.Null(OverlayAttributes.Read(Attributes("aria-label", null), "aria-label"));
        Assert.Null(OverlayAttributes.Read(null, "aria-label"));
    }

    [Fact]
    public void Id_Is_The_Id_And_Nothing_When_It_Is_Empty_Or_Missing()
    {
        Assert.Equal("my-toast", OverlayAttributes.Id(Attributes("id", "my-toast")));
        Assert.Equal("7", OverlayAttributes.Id(Attributes("id", 7)));
        Assert.Null(OverlayAttributes.Id(Attributes("id", "")));
        Assert.Null(OverlayAttributes.Id(Attributes("id", null)));
        Assert.Null(OverlayAttributes.Id(Attributes("aria-label", "A toast")));
        Assert.Null(OverlayAttributes.Id(null));
    }

    [Fact]
    public void AriaLabel_Is_The_Label_And_Nothing_When_It_Is_Empty_Or_Missing()
    {
        Assert.Equal("Busy", OverlayAttributes.AriaLabel(Attributes("aria-label", "Busy")));
        Assert.Null(OverlayAttributes.AriaLabel(Attributes("aria-label", "")));
        Assert.Null(OverlayAttributes.AriaLabel(Attributes("aria-label", null)));
        Assert.Null(OverlayAttributes.AriaLabel(Attributes("id", "busy")));
        Assert.Null(OverlayAttributes.AriaLabel(null));
    }

    [Fact]
    public void Is_Matches_Only_The_Same_String()
    {
        Assert.True(OverlayAttributes.Is(Attributes("role", "radio"), "role", "radio"));
        Assert.True(OverlayAttributes.Is(Attributes("aria-checked", "true"), "aria-checked", "true"));
        Assert.False(OverlayAttributes.Is(Attributes("role", "Radio"), "role", "radio"));
        Assert.False(OverlayAttributes.Is(Attributes("aria-checked", "false"), "aria-checked", "true"));
        Assert.False(OverlayAttributes.Is(Attributes("role", null), "role", "radio"));
        Assert.False(OverlayAttributes.Is(null, "role", "radio"));
    }

    [Fact]
    public void Is_Does_Not_Match_A_Value_That_Is_Not_A_String_As_Ionics_Strict_Comparison()
    {
        // action-sheet.js: htmlAttributes?.['aria-checked'] === 'true', which a boolean true never is.
        Assert.False(OverlayAttributes.Is(Attributes("aria-checked", true), "aria-checked", "true"));
    }
}
