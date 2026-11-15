using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avonic.Components.Animation;

namespace Avonic.Components.Tests.Animation;

/// <summary>
/// <see cref="CssBoxShadow"/> and <c>{a:CssShadow}</c>: CSS's box-shadow read as Avalonia's, with the blur converted. The numbers are
/// Chromium's, measured as docs/infra/box-shadow/README.md says: a white 120 x 80 box on white at 1x, the column below its centre.
/// </summary>
public class CssShadowTests
{
    private static readonly Color Black40 = Color.FromArgb(102, 0, 0, 0);

    // ── The blur ────────────────────────────────────────────────────────────────────────────

    [Theory]
    // The measured range of Avalonia blurs, in steps of 0.05, that drew each CSS blur closest to Chromium (max channel error 1 to 3):
    // Skia's box blur moves in steps of about 1.84, so each range is one window.
    [InlineData(2, 1.05, 2.85)]
    [InlineData(3, 2.90, 4.70)]
    [InlineData(4, 4.75, 6.55)]
    [InlineData(5, 6.60, 8.40)]
    [InlineData(6, 8.45, 10.20)]
    [InlineData(8, 12.10, 13.90)]
    [InlineData(10, 15.80, 17.60)]
    [InlineData(14, 23.15, 24.95)]
    [InlineData(20, 34.20, 36.00)]
    [InlineData(22, 37.90, 39.70)]
    [InlineData(25, 43.45, 45.25)]
    public void The_Blur_Falls_In_The_Window_That_Matched_Chromium(double css, double low, double high)
    {
        var blur = CssBoxShadow.Blur(css);
        Assert.InRange(blur, low - 0.05, high + 0.05);
    }

    [Fact]
    public void The_Blur_Is_The_Formula()
    {
        // 2 sqrt(3) (4 B / (3 sqrt(2 pi)) - 0.5)
        Assert.Equal(1.8426 * 10 - 1.7321, CssBoxShadow.Blur(10), 3);
        Assert.Equal(1.8426 * 1 - 1.7321, CssBoxShadow.Blur(1), 3);
        // Below about 0.94 px it would go negative: none, as Avalonia draws no blur for anything under 1.04 at 1x anyway
        Assert.Equal(0, CssBoxShadow.Blur(0));
        Assert.Equal(0, CssBoxShadow.Blur(0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => CssBoxShadow.Blur(-1));
    }

    [AvaloniaTheory]
    // Chromium's column below the box, from its edge, and the most any pixel of the frame was out when measured. Avalonia's own
    // reading of the same numbers was out by 9 to 20 on each.
    [InlineData("0 0 4px rgba(0, 0, 0, 0.4)", 2, new[] { 214, 231, 244, 251, 254, 255 })]
    [InlineData("0 0 10px rgba(0, 0, 0, 0.4)", 1, new[] { 208, 215, 222, 229, 235, 240, 244, 248, 250, 252, 254, 254, 255 })]
    [InlineData("0 0 20px rgba(0, 0, 0, 0.4)", 1, new[] { 206, 210, 214, 217, 221, 224, 228, 231, 234, 237, 239, 242, 244, 246, 247, 248, 250, 251, 252, 252, 253, 254, 254, 254, 255 })]
    [InlineData("0 0 20px -5px rgba(0, 0, 0, 0.4)", 1, new[] { 224, 228, 231, 234, 237, 239, 242, 244, 246, 247, 248, 250, 251, 252, 252, 253, 254, 254, 254, 255 })]
    [InlineData("0 4px 10px rgba(0, 0, 0, 0.4)", 1, new[] { 179, 186, 193, 200, 208, 215, 222, 229, 235, 240, 244, 248, 250, 252, 254, 254, 255 })]
    public void It_Draws_As_Chromium_Does(string css, int tolerance, int[] chromium)
    {
        var column = ColumnBelow(CssBoxShadow.Parse(css));
        for (var i = 0; i < chromium.Length; i++)
        {
            Assert.True(Math.Abs(column[i] - chromium[i]) <= tolerance, $"{i} px below: {column[i]}, where Chromium has {chromium[i]}");
        }
    }

    // A white 120 x 80 border at (140, 140) of a white 400 x 400 window at 1x, and the red channel of the 40 pixels below its centre.
    private static int[] ColumnBelow(BoxShadows shadow)
    {
        var box = new Border { Width = 120, Height = 80, Background = Brushes.White, BoxShadow = shadow };
        Canvas.SetLeft(box, 140);
        Canvas.SetTop(box, 140);
        using var scene = new Scene(new Canvas { Children = { box } }, 400, 400);
        scene.Window.Background = Brushes.White;
        var frame = scene.Window.CaptureRenderedFrame()!;
        return Enumerable.Range(0, 40).Select(d => (int)Frames.PixelAt(frame, 200, 220 + d).R).ToArray();
    }

    // ── Reading CSS ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Px_And_Unitless_Read_The_Same()
    {
        Assert.Equal(CssBoxShadow.Parse("0px 3px 1px -2px rgba(0, 0, 0, 0.2)"), CssBoxShadow.Parse("0 3 1 -2 rgba(0,0,0,.2)"));
    }

    [Fact]
    public void Two_To_Four_Lengths_Are_Offset_Blur_And_Spread()
    {
        Assert.Equal(new BoxShadow { OffsetX = 1, OffsetY = 2, Color = Colors.Red }, Single("1px 2px red"));
        Assert.Equal(new BoxShadow { OffsetX = 1, OffsetY = 2, Blur = CssBoxShadow.Blur(6), Color = Colors.Red }, Single("1px 2px 6px red"));
        Assert.Equal(new BoxShadow { OffsetX = -1, OffsetY = -2, Blur = CssBoxShadow.Blur(6), Spread = -3, Color = Colors.Red }, Single("-1px -2px 6px -3px red"));
    }

    [Fact]
    public void Several_Shadows_Keep_Their_Order()
    {
        var shadows = CssBoxShadow.Parse("0 3px 1px -2px rgba(0, 0, 0, 0.2), 0 2px 2px 0 rgba(0, 0, 0, 0.14), 0 1px 5px 0 rgba(0, 0, 0, 0.12)");
        Assert.Equal(3, shadows.Count);
        Assert.Equal(new BoxShadow { OffsetY = 3, Blur = CssBoxShadow.Blur(1), Spread = -2, Color = Color.Parse("#33000000") }, shadows[0]);
        Assert.Equal(new BoxShadow { OffsetY = 2, Blur = CssBoxShadow.Blur(2), Color = Color.Parse("#24000000") }, shadows[1]);
        Assert.Equal(new BoxShadow { OffsetY = 1, Blur = CssBoxShadow.Blur(5), Color = Color.Parse("#1F000000") }, shadows[2]);
    }

    [Fact]
    public void Inset_And_The_Colour_Go_Either_Side()
    {
        var expected = new BoxShadow { OffsetY = 4, Blur = CssBoxShadow.Blur(6), Color = Colors.Blue, IsInset = true };
        Assert.Equal(expected, Single("inset 0 4px 6px blue"));
        Assert.Equal(expected, Single("0 4px 6px blue inset"));
        Assert.Equal(expected, Single("blue 0 4px 6px inset"));
        Assert.Equal(expected, Single("INSET blue 0 4px 6px"));
        Assert.False(Single("0 4px 6px blue").IsInset);
    }

    [Theory]
    [InlineData("rgba(0, 0, 0, 0.4)", 0x66, 0, 0, 0)]
    [InlineData("rgba(0,0,0,.14)", 0x24, 0, 0, 0)]
    [InlineData("rgb(255, 128, 0)", 0xFF, 255, 128, 0)]
    [InlineData("rgb(255 128 0 / 50%)", 0x80, 255, 128, 0)]
    [InlineData("rgba(100%, 0%, 50%, 1)", 0xFF, 255, 0, 128)]
    [InlineData("#fff", 0xFF, 255, 255, 255)]
    [InlineData("#f008", 0x88, 255, 0, 0)]
    [InlineData("#336699", 0xFF, 0x33, 0x66, 0x99)]
    // CSS's #rrggbbaa: alpha last, where Avalonia's #aarrggbb has it first
    [InlineData("#33669980", 0x80, 0x33, 0x66, 0x99)]
    [InlineData("transparent", 0, 0, 0, 0)]
    [InlineData("red", 0xFF, 255, 0, 0)]
    [InlineData("DarkSlateGray", 0xFF, 0x2F, 0x4F, 0x4F)]
    public void Colours_Read_As_Css_Does(string color, byte a, byte r, byte g, byte b)
    {
        Assert.Equal(Color.FromArgb(a, r, g, b), Single($"0 0 4px {color}").Color);
    }

    [Fact]
    public void None_Is_No_Shadow()
    {
        Assert.Equal(0, CssBoxShadow.Parse("none").Count);
        Assert.Equal(BoxShadows.Parse("none"), CssBoxShadow.Parse(" None "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0 0 4px")] // no colour: CSS's currentColor, which a BoxShadow can't follow
    [InlineData("0 red")] // one length
    [InlineData("0 0 0 0 0 red")] // five
    [InlineData("0 0 -4px red")] // a negative blur
    [InlineData("0 0 4em red")] // a unit other than px
    [InlineData("0 0 red 4px")] // lengths apart
    [InlineData("0 0 4px red blue")]
    [InlineData("0 0 4px #12345")]
    [InlineData("0 0 4px rgba(0, 0, 0)x")]
    [InlineData("0 0 4px notacolour")]
    [InlineData("0 0 4px red,")]
    [InlineData("inset inset 0 0 red")]
    public void Anything_Else_Is_Refused(string css)
    {
        Assert.Throws<FormatException>(() => CssBoxShadow.Parse(css));
    }

    private static BoxShadow Single(string css)
    {
        var shadows = CssBoxShadow.Parse(css);
        Assert.Equal(1, shadows.Count);
        return shadows[0];
    }

    // ── In XAML ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void The_Extension_Reads_A_Resource_And_An_Attribute()
    {
        var usage = new ResourceInclude(new Uri("avares://Avonic.Components.Tests/"))
        {
            Source = new Uri("avares://Avonic.Components.Tests/Animation/CssShadowUsage.axaml"),
        };
        var expected = new BoxShadows(new BoxShadow { Blur = CssBoxShadow.Blur(10), Color = Black40 });

        // <a:CssShadow x:Key="...">...</a:CssShadow>: the resource is the shadow, not the extension
        Assert.True(usage.TryGetResource("Test.CssShadow.Element", null, out var element));
        Assert.Equal(expected, Assert.IsType<BoxShadows>(element));

        // Value="{a:CssShadow '...'}" on a setter
        Assert.True(usage.TryGetResource("Test.CssShadow.Theme", null, out var theme));
        var border = new Border { Theme = (ControlTheme)theme! };
        using var scene = new Scene(border);
        Assert.Equal(CssBoxShadow.Parse("0 3px 1px -2px rgba(0, 0, 0, 0.2), 0 2px 2px 0 rgba(0, 0, 0, 0.14)"), border.BoxShadow);
    }

    [Fact]
    public void The_Extension_Needs_A_Value()
    {
        Assert.Throws<InvalidOperationException>(() => new CssShadowExtension().ProvideValue(null!));
        Assert.Equal(CssBoxShadow.Parse("0 0 10px red"), new CssShadowExtension("0 0 10px red").ProvideValue(null!));
    }
}
