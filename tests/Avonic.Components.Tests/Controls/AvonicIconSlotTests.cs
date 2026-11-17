using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ionicons 8.1.0's default slot: <c>render()</c> puts a <c>&lt;slot&gt;</c> in <c>.icon-inner</c> when there's no
/// <c>svgContent</c>, and <c>icon.css</c> centres it (<c>.icon-inner { display: flex; align-items: center; justify-content:
/// center }</c>), sizes it (<c>::slotted(*) { font-size: inherit !important }</c>) and no longer clips (<c>contain: layout
/// style</c>). Measured in Chromium: docs/components/ion-icon/PLAN.md, "Font icons".
/// </summary>
[Collection(IconStoreCollection.Name)]
public sealed class AvonicIconSlotTests : IDisposable
{
    public void Dispose() => IconStore.ResetRegisteredForTests();

    private static Scene Show(Control content, int width = 64, int height = 64, FlowDirection direction = FlowDirection.LeftToRight)
    {
        var scene = new Scene(content, width, height);
        scene.Window.Background = Brushes.White;
        scene.Window.FlowDirection = direction;
        Dispatcher.UIThread.RunJobs();
        return scene;
    }

    private static Color PixelAt(Scene scene, int x, int y)
    {
        Dispatcher.UIThread.RunJobs();
        return Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, x, y);
    }

    private static TextBlock Glyph(string text = "M") => new() { Text = text };

    [AvaloniaFact]
    public void Content_Shows_When_There_Is_No_Icon()
    {
        var glyph = Glyph();
        var icon = new AvonicIcon { FontSize = 24, Content = glyph };
        using var scene = Show(icon);

        Assert.True(glyph.IsEffectivelyVisible);
        Assert.True(glyph.Bounds.Width > 0);
    }

    [AvaloniaFact]
    public void Content_Hides_While_An_Svg_Is_Drawn()
    {
        var glyph = Glyph();
        var icon = new AvonicIcon { FontSize = 24, IconName = "heart", Content = glyph };
        using var scene = Show(icon);

        Assert.False(Shown(glyph));
    }

    /// <summary>A hidden presenter isn't measured, so it never makes its child: either way, it isn't shown.</summary>
    private static bool Shown(Control control) => TopLevel.GetTopLevel(control) is not null && control.IsEffectivelyVisible;

    // getSvgContent of an unknown name fails, and svgContent is '', which is falsy: the slot.
    [AvaloniaFact]
    public void Content_Shows_For_An_Unknown_Name()
    {
        var glyph = Glyph();
        var icon = new AvonicIcon { FontSize = 24, IconName = "no-such-icon", Content = glyph };
        using var scene = Show(icon);

        Assert.True(glyph.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Content_Hides_When_A_Name_Is_Set_Later()
    {
        var glyph = Glyph();
        var icon = new AvonicIcon { FontSize = 24, Content = glyph };
        using var scene = Show(icon);

        icon.IconName = "heart";
        Dispatcher.UIThread.RunJobs();

        Assert.False(Shown(glyph));
    }

    // Chromium: a 24 px icon's span is 20.95 × 28 at (1.52, -2), its centre the box's (12, 12). Avalonia rounds the
    // position to a pixel, so within half of one.
    [AvaloniaFact]
    public void Content_Is_Centred_In_The_Box()
    {
        var glyph = Glyph();
        var icon = new AvonicIcon { FontSize = 24, Content = glyph };
        using var scene = Show(icon);

        var origin = glyph.TranslatePoint(default, icon)!.Value;
        Assert.Equal(12, origin.X + glyph.Bounds.Width / 2, 0.5);
        Assert.Equal(12, origin.Y + glyph.Bounds.Height / 2, 0.5);
        Assert.Equal(new Size(24, 24), icon.Bounds.Size);
    }

    // Chromium: a 48 px icon's "WWW" is 127.77 wide, from -39.87 to 87.9: evenly over both sides, and painted.
    [AvaloniaFact]
    public void Wide_Content_Overflows_Both_Sides_And_Is_Not_Clipped()
    {
        var wide = new Border { Width = 64, Height = 16, Background = Brushes.Black };
        var icon = new AvonicIcon { FontSize = 32, Content = wide };
        var host = new Border { Padding = new Thickness(32, 24), Child = icon };
        using var scene = Show(host, 96, 80);

        Assert.Equal(-16, wide.TranslatePoint(default, icon)!.Value.X, 0.01);
        Assert.Equal(Colors.Black, PixelAt(scene, 20, 40));
        Assert.Equal(Colors.Black, PixelAt(scene, 76, 40));
    }

    // ::slotted(*) { font-size: inherit !important }, and :host(.icon-small) { font-size: 1.125rem !important }.
    [AvaloniaTheory]
    [InlineData(IconSize.Small, 18)]
    [InlineData(IconSize.Large, 32)]
    public void Content_Takes_The_Size_Over_Its_Own_Font_Size(IconSize size, double expected)
    {
        var glyph = Glyph();
        glyph.FontSize = 40;
        var icon = new AvonicIcon { FontSize = 24, Size = size, Content = glyph };
        using var scene = Show(icon);

        Assert.Equal(expected, glyph.FontSize);
    }

    [AvaloniaFact]
    public void Content_Takes_The_Icons_Font_Size_And_Follows_It()
    {
        var glyph = Glyph();
        glyph.FontSize = 40;
        var icon = new AvonicIcon { FontSize = 24, Content = glyph };
        using var scene = Show(icon);
        Assert.Equal(24, glyph.FontSize);

        icon.FontSize = 48;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(48, glyph.FontSize);
    }

    [AvaloniaFact]
    public void A_String_Is_Shown_At_The_Icons_Size()
    {
        var icon = new AvonicIcon { FontSize = 24, Size = IconSize.Large, Content = "M" };
        using var scene = Show(icon);

        var text = icon.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(32, text.FontSize);
    }

    [AvaloniaFact]
    public void A_Glyph_Removed_From_The_Slot_Gets_Its_Own_Font_Size_Back()
    {
        var glyph = Glyph();
        glyph.FontSize = 40;
        var icon = new AvonicIcon { FontSize = 24, Content = glyph };
        using var scene = Show(icon);

        icon.Content = null;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(40, glyph.FontSize);
    }

    // :host(.ion-color) { color: var(--ion-color-base) !important }, which the slotted element inherits.
    [AvaloniaFact]
    public void Content_Is_In_The_Colour() => InstalledTheme.Run(_ =>
    {
        var glyph = Glyph();
        var icon = new AvonicIcon { FontSize = 24, Color = "danger", Content = glyph };
        using var scene = Show(icon);

        Assert.Equal(Color.Parse("#c5000f"), Assert.IsAssignableFrom<ISolidColorBrush>(glyph.Foreground).Color);
    });

    [AvaloniaFact]
    public void Content_Inherits_The_Foreground()
    {
        var glyph = Glyph();
        var icon = new AvonicIcon { FontSize = 24, Foreground = Brushes.Lime, Content = glyph };
        using var scene = Show(icon);

        Assert.Equal(Colors.Lime, Assert.IsAssignableFrom<ISolidColorBrush>(glyph.Foreground).Color);
    }

    /// <summary>The mean x of the ink in the frame: an "L" has its stem on the left, so a mirrored one has it right of its middle.</summary>
    private static double InkCentre(Scene scene, out double glyphMiddle, TextBlock glyph)
    {
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        double sum = 0, count = 0;
        for (var y = 0; y < frame.PixelSize.Height; y++)
        {
            for (var x = 0; x < frame.PixelSize.Width; x++)
            {
                if (Frames.PixelAt(frame, x, y).R < 128)
                {
                    sum += x + 0.5;
                    count++;
                }
            }
        }

        var origin = glyph.TranslatePoint(new Point(glyph.Bounds.Width / 2, 0), scene.Window)!.Value;
        glyphMiddle = origin.X;
        Assert.True(count > 0, "Expected the glyph painted");
        return sum / count;
    }

    // :host(.flip-rtl):host-context([dir='rtl']) .icon-inner { transform: scaleX(-1) }: the slot is in .icon-inner. Chromium
    // draws an "R" in an RTL icon as it is, and mirrored with flip-rtl.
    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight, true, false)]
    [InlineData(FlowDirection.RightToLeft, true, true)]
    [InlineData(FlowDirection.RightToLeft, false, false)]
    public void Content_Flips_With_FlipRtl_In_Rtl(FlowDirection direction, bool flipRtl, bool flipped)
    {
        var glyph = Glyph("L");
        var icon = new AvonicIcon { FontSize = 64, FlipRtl = flipRtl, Content = glyph };
        using var scene = Show(icon, 96, 96, direction);

        var ink = InkCentre(scene, out var middle, glyph);

        Assert.True(flipped ? ink > middle : ink < middle, $"ink at {ink}, the glyph's middle at {middle}");
    }

    // role="img": its children are presentational.
    [AvaloniaFact]
    public void The_Peer_Is_Still_An_Image_With_No_Children()
    {
        var icon = new AvonicIcon { FontSize = 24, Content = Glyph() };
        using var scene = Show(icon);

        var peer = ControlAutomationPeer.CreatePeerForElement(icon);
        Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
        Assert.Empty(peer.GetChildren());
    }

    [AvaloniaFact]
    public void An_Svg_Is_Still_Clipped_To_The_Box()
    {
        // A square twice the viewBox, from its origin: overflow: hidden on the SVG keeps it in the box.
        var icon = new AvonicIcon
        {
            FontSize = 32,
            Src = """data:image/svg+xml;utf8,<svg viewBox="0 0 512 512"><rect width="1024" height="1024"/></svg>""",
        };
        var host = new Border { Padding = new Thickness(16), Child = icon };
        using var scene = Show(host);

        Assert.Equal(Colors.Black, PixelAt(scene, 40, 40));
        Assert.Equal(Colors.White, PixelAt(scene, 56, 56));
    }
}
