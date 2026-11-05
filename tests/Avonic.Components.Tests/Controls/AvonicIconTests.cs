using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary><see cref="IconStore"/> is process-wide, so the tests that use it can't run in parallel.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IconStoreCollection
{
    public const string Name = "IconStore";
}

/// <summary>
/// Behaviours from ionicons' <c>icon.js</c>, <c>utils.js</c> and <c>icon.css</c>, and what a browser does
/// with the SVG. Each was measured in real Ionic; see docs/components/ion-icon/PLAN.md. Painting is
/// asserted on pixels, using small SVGs that isolate one rule at a time.
/// </summary>
[Collection(IconStoreCollection.Name)]
public sealed class AvonicIconTests : IDisposable
{
    private static readonly Color White = Colors.White;

    public void Dispose() => IconStore.ResetRegisteredForTests();

    private static string Svg(string content, string rootAttributes = """viewBox="0 0 512 512" """) =>
        $"data:image/svg+xml;utf8,<svg {rootAttributes}>{content}</svg>";

    /// <summary>The left half of the box, solid. Which side it's on shows whether it flipped.</summary>
    private static readonly string LeftHalf = Svg("""<rect width="256" height="512"/>""");

    /// <summary>The whole box, solid. Its colour is the colour the icon paints in.</summary>
    private static readonly string Solid = Svg("""<rect width="512" height="512"/>""");

    /// <summary>A window exactly the icon's size, so a pixel in the icon is that pixel in the frame.</summary>
    private static Scene Show(Control content, int size = 64, FlowDirection direction = FlowDirection.LeftToRight)
    {
        var scene = new Scene(content, size, size);
        scene.Window.Background = Brushes.White;
        scene.Window.FlowDirection = direction;
        Dispatcher.UIThread.RunJobs();
        return scene;
    }

    private static AvonicIcon Icon(string? src = null, int size = 64) => new() { Src = src, FontSize = size };

    private static WriteableBitmap Frame(Scene scene)
    {
        Dispatcher.UIThread.RunJobs();
        return scene.Window.CaptureRenderedFrame()!;
    }

    private static unsafe Color PixelAt(WriteableBitmap bitmap, int x, int y)
    {
        using var fb = bitmap.Lock();
        var p = (byte*)fb.Address + y * fb.RowBytes + x * 4;
        return fb.Format == Avalonia.Platform.PixelFormats.Rgba8888
            ? Color.FromArgb(p[3], p[0], p[1], p[2])
            : Color.FromArgb(p[3], p[2], p[1], p[0]);
    }

    private static bool IsBlank(Scene scene)
    {
        var frame = Frame(scene);
        for (var y = 0; y < frame.PixelSize.Height; y += 2)
        {
            for (var x = 0; x < frame.PixelSize.Width; x += 2)
            {
                if (PixelAt(frame, x, y) != White)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void AssertBlankWith(AvonicIcon icon)
    {
        using var scene = Show(icon);
        Assert.True(IsBlank(scene), "Expected nothing painted");
    }

    private static void AssertPaints(AvonicIcon icon)
    {
        using var scene = Show(icon);
        Assert.False(IsBlank(scene), "Expected something painted");
    }

    // ── Which icon: getUrl, getName ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A_Name_Paints_The_Bundled_Icon() => AssertPaints(new AvonicIcon { IconName = "heart", FontSize = 64 });

    [AvaloniaFact]
    public void Src_Beats_A_Name()
    {
        var icon = new AvonicIcon { IconName = "heart", Src = Svg(""), FontSize = 64 };

        AssertBlankWith(icon);
    }

    [AvaloniaFact]
    public void Src_Without_A_Slash_Or_Dot_Is_Ignored() =>
        AssertPaints(new AvonicIcon { IconName = "heart", Src = "notasource", FontSize = 64 });

    [AvaloniaFact]
    public void Md_Beats_A_Name()
    {
        var icon = new AvonicIcon { IconName = "no-such-icon", Md = "heart", FontSize = 64 };

        AssertPaints(icon);
    }

    [AvaloniaFact]
    public void Icon_Is_Used_As_A_Name() => AssertPaints(new AvonicIcon { Icon = "heart", FontSize = 64 });

    [AvaloniaFact]
    public void Name_Beats_Icon() => AssertBlankWith(new AvonicIcon { IconName = "no-such-icon", Icon = "heart", FontSize = 64 });

    [AvaloniaFact]
    public void Icon_With_A_Slash_Is_A_Source() => AssertPaints(new AvonicIcon { Icon = Solid, FontSize = 64 });

    [AvaloniaFact]
    public void A_Name_Beats_A_Source_In_Icon() => AssertBlankWith(new AvonicIcon { IconName = "no-such-icon", Icon = Solid, FontSize = 64 });

    [AvaloniaFact]
    public void Names_Are_Lowercased() => AssertPaints(new AvonicIcon { IconName = "HEART", FontSize = 64 });

    [AvaloniaTheory]
    [InlineData("he_art")]
    [InlineData("heart!")]
    [InlineData("   ")]
    [InlineData("")]
    public void An_Invalid_Name_Resolves_To_Nothing(string name) => AssertBlankWith(new AvonicIcon { IconName = name, FontSize = 64 });

    [AvaloniaFact]
    public void An_Unknown_Name_Keeps_The_Box_And_Paints_Nothing()
    {
        var icon = new AvonicIcon { IconName = "no-such-icon", FontSize = 40 };
        using var scene = new Scene(new StackPanel { Children = { icon } }, 64, 64);

        Assert.Equal(40, icon.Bounds.Height);
        Assert.Equal(40, icon.DesiredSize.Height);
    }

    [AvaloniaFact]
    public void No_Name_Keeps_The_Box_And_Paints_Nothing()
    {
        var icon = new AvonicIcon { FontSize = 40 };
        using var scene = new Scene(new StackPanel { Children = { icon } }, 64, 64);

        Assert.Equal(40, icon.Bounds.Height);
        AssertBlankWith(new AvonicIcon { FontSize = 64 });
    }

    [AvaloniaFact]
    public void Changing_The_Name_Reloads() => AssertReloads(icon => icon.IconName = "heart");

    [AvaloniaFact]
    public void Changing_Md_Reloads() => AssertReloads(icon => icon.Md = "heart");

    [AvaloniaFact]
    public void Changing_Icon_Reloads() => AssertReloads(icon => icon.Icon = "heart");

    [AvaloniaFact]
    public void Changing_Src_Reloads() => AssertReloads(icon => icon.Src = Solid);

    // loadIcon only changes the content `if (url)`, so with nothing to load the last icon stays.
    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("he_art")]
    public void A_Change_To_No_Icon_Keeps_The_Last_One(string? name)
    {
        var icon = new AvonicIcon { IconName = "heart", FontSize = 64 };
        using var scene = Show(icon);
        Assert.False(IsBlank(scene));

        icon.IconName = name;

        Assert.False(IsBlank(scene), "Expected the last icon to stay");
    }

    // An unknown name does have a url, which fails to load.
    [AvaloniaFact]
    public void A_Change_To_An_Unknown_Name_Is_Blank()
    {
        var icon = new AvonicIcon { IconName = "heart", FontSize = 64 };
        using var scene = Show(icon);

        icon.IconName = "no-such-icon";

        Assert.True(IsBlank(scene));
    }

    private static void AssertReloads(Action<AvonicIcon> change)
    {
        var icon = new AvonicIcon { FontSize = 64 };
        using var scene = Show(icon);
        Assert.True(IsBlank(scene));

        change(icon);

        Assert.False(IsBlank(scene), "Expected the new icon to paint");
    }

    // ── Sources ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Src_Loads_A_Utf8_Data_Url() => AssertPaints(Icon(Solid));

    [AvaloniaFact]
    public void Src_Loads_A_Base64_Data_Url()
    {
        var svg = """<svg viewBox="0 0 512 512"><rect width="512" height="512"/></svg>""";
        AssertPaints(Icon("data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg))));
    }

    [AvaloniaFact]
    public void Src_Loads_A_Percent_Encoded_Data_Url()
    {
        var svg = """<svg viewBox="0 0 512 512"><rect width="512" height="512"/></svg>""";
        AssertPaints(Icon("data:image/svg+xml," + Uri.EscapeDataString(svg)));
    }

    [AvaloniaFact]
    public void Src_Loads_An_Avalonia_Asset() =>
        AssertPaints(Icon("avares://Avonic.Components.Tests/Controls/TestIcon.svg"));

    // A browser's fetch percent-decodes a data url before it reads the base64.
    [AvaloniaFact]
    public void Src_Loads_Percent_Encoded_Base64()
    {
        var svg = """<svg viewBox="0 0 512 512"><rect width="512" height="512"/></svg>""";
        AssertPaints(Icon("data:image/svg+xml;base64," + Uri.EscapeDataString(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg + " ")))));
    }

    [AvaloniaTheory]
    [InlineData("avares://Avonic.Components.Tests/missing.svg")]
    [InlineData("https://example.com/icon.svg")]
    [InlineData("/assets/icon.svg")]
    [InlineData("data:text/plain,hello")]
    [InlineData("data:image/svg+xml;base64,not-base64!")]
    public void A_Source_That_Cant_Load_Is_Blank(string src) => AssertBlankWith(Icon(src));

    // ── addIcons ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void AddIcons_Registers_A_Name()
    {
        AvonicIcon.AddIcons(new Dictionary<string, string> { ["my-icon"] = Solid });

        AssertPaints(new AvonicIcon { IconName = "my-icon", FontSize = 64 });
    }

    [AvaloniaTheory]
    [InlineData("addCircleOutline", "add-circle-outline")]
    [InlineData("heart", "heart")]
    [InlineData("logoX", "logo-x")]
    [InlineData("a1", "a-1")]
    public void AddIcons_Maps_CamelCase_To_Kebab_Case(string name, string kebab) =>
        Assert.Equal(kebab, IconStore.ToKebabCase(name));

    [AvaloniaFact]
    public void AddIcons_Also_Registers_The_Kebab_Case_Name()
    {
        AvonicIcon.AddIcons(new Dictionary<string, string> { ["myCustomIcon"] = Solid });

        AssertPaints(new AvonicIcon { IconName = "my-custom-icon", FontSize = 64 });
    }

    [AvaloniaFact]
    public void A_Registered_Name_Beats_A_Bundled_Icon()
    {
        // Paints heart before registering, so a stale cache entry would keep showing it.
        AssertPaints(new AvonicIcon { IconName = "heart", FontSize = 64 });

        AvonicIcon.AddIcons(new Dictionary<string, string> { ["heart"] = Svg("") });

        AssertBlankWith(new AvonicIcon { IconName = "heart", FontSize = 64 });
    }

    [AvaloniaFact]
    public void The_First_Registration_Of_A_Name_Wins()
    {
        AvonicIcon.AddIcons(new Dictionary<string, string> { ["twice"] = Solid });
        AvonicIcon.AddIcons(new Dictionary<string, string> { ["twice"] = Svg("") });

        AssertPaints(new AvonicIcon { IconName = "twice", FontSize = 64 });
    }

    // ── Sizing: icon.css ─────────────────────────────────────────────────────────────────────

    private static Size Measured(AvonicIcon icon, double? parentFontSize = null)
    {
        var parent = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Children = { icon } };
        if (parentFontSize is { } size)
        {
            parent.SetValue(TextElement.FontSizeProperty, size);
        }

        icon.HorizontalAlignment = HorizontalAlignment.Left;
        using var scene = new Scene(parent, 200, 200);
        return icon.Bounds.Size;
    }

    [AvaloniaFact]
    public void The_Box_Is_1em_Square() => Assert.Equal(new Size(40, 40), Measured(new AvonicIcon { FontSize = 40 }));

    [AvaloniaFact]
    public void The_Box_Follows_An_Inherited_Font_Size() => Assert.Equal(new Size(24, 24), Measured(new AvonicIcon(), parentFontSize: 24));

    [AvaloniaFact]
    public void Small_Is_18() => Assert.Equal(new Size(18, 18), Measured(new AvonicIcon { Size = IconSize.Small }));

    [AvaloniaFact]
    public void Large_Is_32() => Assert.Equal(new Size(32, 32), Measured(new AvonicIcon { Size = IconSize.Large }));

    // :host(.icon-small) { font-size: 1.125rem !important; }
    [AvaloniaFact]
    public void Size_Beats_A_Local_Font_Size() => Assert.Equal(new Size(18, 18), Measured(new AvonicIcon { Size = IconSize.Small, FontSize = 40 }));

    [AvaloniaFact]
    public void An_Explicit_Box_Wins() => Assert.Equal(new Size(64, 32), Measured(new AvonicIcon { Width = 64, Height = 32, FontSize = 20 }));

    [AvaloniaFact]
    public void Changing_Size_Remeasures()
    {
        var icon = new AvonicIcon { FontSize = 40, HorizontalAlignment = HorizontalAlignment.Left };
        using var scene = new Scene(new StackPanel { Children = { icon } }, 200, 200);

        icon.Size = IconSize.Large;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(32, icon.Bounds.Height);
    }

    // display: inline-block. It stays 1em in any parent, at the start.
    [AvaloniaFact]
    public void It_Does_Not_Stretch_To_Its_Parent()
    {
        var icon = new AvonicIcon { Src = Solid, FontSize = 24 };
        using var scene = Show(new Border { Width = 64, Height = 64, Child = icon });
        var frame = Frame(scene);

        Assert.Equal(new Size(24, 24), icon.Bounds.Size);
        Assert.NotEqual(White, PixelAt(frame, 12, 12));
        Assert.Equal(White, PixelAt(frame, 40, 40));
    }

    [AvaloniaFact]
    public void It_Does_Not_Stretch_In_A_Grid()
    {
        var icon = new AvonicIcon { FontSize = 24 };
        using var scene = new Scene(new Grid { Width = 100, Height = 100, Children = { icon } }, 200, 200);

        Assert.Equal(new Size(24, 24), icon.Bounds.Size);
    }

    [AvaloniaFact]
    public void It_Sits_At_The_Start_Of_A_Vertical_StackPanel()
    {
        var icon = new AvonicIcon { FontSize = 24 };
        using var scene = new Scene(new StackPanel { Width = 100, Children = { icon } }, 200, 200);

        Assert.Equal(0, icon.Bounds.X);
        Assert.Equal(24, icon.Bounds.Width);
    }

    // An app can still ask for it.
    [AvaloniaFact]
    public void An_Explicit_Stretch_Fills_The_Parent()
    {
        var icon = new AvonicIcon { FontSize = 24, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        using var scene = new Scene(new Border { Width = 64, Height = 64, Child = icon }, 200, 200);

        Assert.Equal(new Size(64, 64), icon.Bounds.Size);
    }

    // preserveAspectRatio="xMidYMid meet": a 32 square, centred in a 64 by 32 box.
    [AvaloniaFact]
    public void Artwork_Is_Centred_And_Scaled_To_Fit()
    {
        var icon = new AvonicIcon { Src = Solid, Width = 64, Height = 32, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = Show(icon);
        var frame = Frame(scene);

        Assert.Equal(White, PixelAt(frame, 8, 16));
        Assert.NotEqual(White, PixelAt(frame, 32, 16));
        Assert.Equal(White, PixelAt(frame, 56, 16));
    }

    [AvaloniaFact]
    public void The_ViewBox_Scales_The_Artwork()
    {
        var icon = Icon(Svg("""<rect width="8" height="16"/>""", """viewBox="0 0 16 16" """));
        using var scene = Show(icon);
        var frame = Frame(scene);

        Assert.NotEqual(White, PixelAt(frame, 16, 32));
        Assert.Equal(White, PixelAt(frame, 48, 32));
    }

    // contain: strict; overflow: hidden. The line runs well past the viewBox on both sides.
    [AvaloniaFact]
    public void Painting_Is_Clipped_To_The_Box()
    {
        var src = Svg("""<path d="M-2000 256H2000" stroke="currentColor" stroke-width="256"/>""");
        var icon = new AvonicIcon { Src = src, Width = 32, Height = 32, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = Show(icon);
        var frame = Frame(scene);

        Assert.NotEqual(White, PixelAt(frame, 16, 16));
        Assert.Equal(White, PixelAt(frame, 48, 16));
    }

    // ── Colour ───────────────────────────────────────────────────────────────────────────────

    private static Color Painted(Control content)
    {
        using var scene = Show(content);
        return PixelAt(Frame(scene), 32, 32);
    }

    private static readonly Color Danger = Color.Parse("#c5000f");

    [AvaloniaFact]
    public void It_Paints_In_The_Inherited_Foreground()
    {
        var parent = new Border { Child = Icon(Solid) };
        parent.SetValue(TextElement.ForegroundProperty, Brushes.Lime);

        Assert.Equal(Colors.Lime, Painted(parent));
    }

    [AvaloniaFact]
    public void A_Local_Foreground_Paints()
    {
        var icon = Icon(Solid);
        icon.Foreground = Brushes.Lime;

        Assert.Equal(Colors.Lime, Painted(icon));
    }

    [AvaloniaFact]
    public void Changing_Foreground_Repaints()
    {
        var icon = Icon(Solid);
        icon.Foreground = Brushes.Lime;
        using var scene = Show(icon);

        icon.Foreground = Brushes.Blue;

        Assert.Equal(Colors.Blue, PixelAt(Frame(scene), 32, 32));
    }

    [AvaloniaFact]
    public void Color_Paints_The_Base() => InstalledTheme.Run(_ =>
    {
        var icon = Icon(Solid);
        icon.Color = "danger";

        Assert.Equal(Danger, Painted(icon));
    });

    // :host(.ion-color) { color: var(--ion-color-base) !important; }
    [AvaloniaFact]
    public void Color_Beats_A_Local_Foreground() => InstalledTheme.Run(_ =>
    {
        var icon = Icon(Solid);
        icon.Color = "danger";
        icon.Foreground = Brushes.Lime;

        Assert.Equal(Danger, Painted(icon));
    });

    [AvaloniaFact]
    public void An_Unknown_Color_Paints_The_Inherited_Foreground() => InstalledTheme.Run(_ =>
    {
        var icon = Icon(Solid);
        icon.Color = "nonexistent";
        var parent = new Border { Child = icon };
        parent.SetValue(TextElement.ForegroundProperty, Brushes.Lime);

        Assert.Equal(Colors.Lime, Painted(parent));
    });

    [AvaloniaFact]
    public void An_Unknown_Color_Inside_A_Colored_Ancestor_Paints_Its_Base() => InstalledTheme.Run(_ =>
    {
        var icon = Icon(Solid);
        icon.Color = "nonexistent";
        var ancestor = new Border { Child = icon };
        ColorPalette.SetColor(ancestor, "danger");

        Assert.Equal(Danger, Painted(ancestor));
    });

    // Ionic: the variable inherits, but only .ion-color reads it.
    [AvaloniaFact]
    public void No_Color_Inside_A_Colored_Ancestor_Paints_The_Foreground() => InstalledTheme.Run(_ =>
    {
        var icon = Icon(Solid);
        icon.Foreground = Brushes.Lime;
        var ancestor = new Border { Child = icon };
        ColorPalette.SetColor(ancestor, "danger");

        Assert.Equal(Colors.Lime, Painted(ancestor));
    });

    [AvaloniaFact]
    public void Changing_Color_Repaints() => InstalledTheme.Run(_ =>
    {
        var icon = Icon(Solid);
        icon.Foreground = Brushes.Lime;
        using var scene = Show(icon);
        Assert.Equal(Colors.Lime, PixelAt(Frame(scene), 32, 32));

        icon.Color = "danger";
        Assert.Equal(Danger, PixelAt(Frame(scene), 32, 32));

        icon.Color = null;
        Assert.Equal(Colors.Lime, PixelAt(Frame(scene), 32, 32));
    });

    [AvaloniaFact]
    public void Overriding_The_Palette_Repaints() => InstalledTheme.Run(_ =>
    {
        var icon = Icon(Solid);
        icon.Color = "danger";
        var scope = new Border { Child = icon };
        using var scene = Show(scope);
        Assert.Equal(Danger, PixelAt(Frame(scene), 32, 32));

        scope.Resources["Avonic.Color.Danger"] = Colors.Lime;

        Assert.Equal(Colors.Lime, PixelAt(Frame(scene), 32, 32));
    });

    // The inherited base doesn't change here, only whether the icon reads it.
    [AvaloniaFact]
    public void Setting_Color_Under_A_Colored_Ancestor_Repaints() => InstalledTheme.Run(_ =>
    {
        var icon = Icon(Solid);
        icon.Foreground = Brushes.Lime;
        var ancestor = new Border { Child = icon };
        ColorPalette.SetColor(ancestor, "danger");
        using var scene = Show(ancestor);
        Assert.Equal(Colors.Lime, PixelAt(Frame(scene), 32, 32));

        icon.Color = "nonexistent";

        Assert.Equal(Danger, PixelAt(Frame(scene), 32, 32));
    });

    // ── RTL: render() ────────────────────────────────────────────────────────────────────────

    /// <summary>True if a shape drawn on the left has moved to the right.</summary>
    private static bool Flipped(AvonicIcon icon, FlowDirection direction)
    {
        using var scene = Show(icon, direction: direction);
        var frame = Frame(scene);
        var left = PixelAt(frame, 16, 32) != White;
        var right = PixelAt(frame, 48, 32) != White;
        Assert.True(left != right, "Expected paint on exactly one side");
        return right;
    }

    // Avalonia mirrors drawing under an RTL ancestor, and Ionic doesn't.
    [AvaloniaFact]
    public void Ordinary_Icons_Do_Not_Flip_In_Rtl() => Assert.False(Flipped(Icon(LeftHalf), FlowDirection.RightToLeft));

    [AvaloniaFact]
    public void FlipRtl_Flips_In_Rtl()
    {
        var icon = Icon(LeftHalf);
        icon.FlipRtl = true;

        Assert.True(Flipped(icon, FlowDirection.RightToLeft));
    }

    [AvaloniaFact]
    public void Nothing_Flips_In_Ltr()
    {
        var icon = Icon(LeftHalf);
        icon.FlipRtl = true;

        Assert.False(Flipped(icon, FlowDirection.LeftToRight));
    }

    [AvaloniaTheory]
    [InlineData("arrow")]
    [InlineData("chevron")]
    [InlineData("my-arrow-icon")]
    [InlineData("CHEVRON-LEFT")]
    public void Arrows_And_Chevrons_Flip_Automatically(string name)
    {
        AvonicIcon.AddIcons(new Dictionary<string, string> { [name.ToLowerInvariant()] = LeftHalf });

        Assert.True(Flipped(new AvonicIcon { IconName = name, FontSize = 64 }, FlowDirection.RightToLeft));
    }

    [AvaloniaFact]
    public void FlipRtl_False_Stops_The_Automatic_Flip()
    {
        AvonicIcon.AddIcons(new Dictionary<string, string> { ["chevron"] = LeftHalf });

        Assert.False(Flipped(new AvonicIcon { IconName = "chevron", FlipRtl = false, FontSize = 64 }, FlowDirection.RightToLeft));
    }

    [AvaloniaFact]
    public void Other_Names_Do_Not_Flip_Automatically()
    {
        AvonicIcon.AddIcons(new Dictionary<string, string> { ["my-icon"] = LeftHalf });

        Assert.False(Flipped(new AvonicIcon { IconName = "my-icon", FontSize = 64 }, FlowDirection.RightToLeft));
    }

    // The flip check uses getName even when src supplies the artwork.
    [AvaloniaFact]
    public void The_Flip_Check_Uses_The_Name_With_Src() =>
        Assert.True(Flipped(new AvonicIcon { IconName = "chevron-forward", Src = LeftHalf, FontSize = 64 }, FlowDirection.RightToLeft));

    [AvaloniaFact]
    public void Src_Alone_Does_Not_Flip_Automatically() => Assert.False(Flipped(Icon(LeftHalf), FlowDirection.RightToLeft));

    [AvaloniaFact]
    public void Changing_FlipRtl_Repaints()
    {
        var icon = Icon(LeftHalf);
        using var scene = Show(icon, direction: FlowDirection.RightToLeft);
        Assert.NotEqual(White, PixelAt(Frame(scene), 16, 32));

        icon.FlipRtl = true;

        Assert.NotEqual(White, PixelAt(Frame(scene), 48, 32));
    }

    // Under an LTR parent, the icon's mirror transform doesn't change, so nothing else repaints it.
    [AvaloniaFact]
    public void Setting_FlowDirection_On_The_Icon_Repaints()
    {
        var icon = Icon(LeftHalf);
        icon.FlipRtl = true;
        using var scene = Show(icon);
        Assert.NotEqual(White, PixelAt(Frame(scene), 16, 32));

        icon.FlowDirection = FlowDirection.RightToLeft;

        Assert.NotEqual(White, PixelAt(Frame(scene), 48, 32));
    }

    [AvaloniaFact]
    public void Changing_FlowDirection_Repaints()
    {
        var icon = Icon(LeftHalf);
        icon.FlipRtl = true;
        using var scene = Show(icon);
        Assert.NotEqual(White, PixelAt(Frame(scene), 16, 32));

        scene.Window.FlowDirection = FlowDirection.RightToLeft;

        Assert.NotEqual(White, PixelAt(Frame(scene), 48, 32));
    }

    // ── SVG painting: the browser's defaults ─────────────────────────────────────────────────

    private static Color PaintedAt(string src, int x, int y, int size = 512)
    {
        using var scene = Show(Icon(src, size), size);
        return PixelAt(Frame(scene), x, y);
    }

    // .ionicon { stroke: currentColor } with SVG's default width of 1.
    [AvaloniaFact]
    public void An_Ionicon_Element_Has_A_One_Unit_Stroke() =>
        Assert.NotEqual(White, PaintedAt(Svg("""<path fill="none" d="M0 256H512"/>""", """viewBox="0 0 512 512" class="ionicon" """), 100, 256));

    [AvaloniaFact]
    public void Without_The_Class_There_Is_No_Default_Stroke() =>
        Assert.Equal(White, PaintedAt(Svg("""<path fill="none" d="M0 256H512"/>"""), 100, 256));

    // A 20-degree corner: its miter length is about 5.5 times the width, between 4 and 10.
    private const string SharpCorner = """<path d="M100 200L400 256L100 312" fill="none" stroke="currentColor" stroke-width="40" {0}/>""";

    [AvaloniaFact]
    public void The_Default_Miter_Limit_Is_4() =>
        Assert.Equal(White, PaintedAt(Svg(string.Format(SharpCorner, "")), 470, 256));

    [AvaloniaFact]
    public void A_Miter_Limit_Attribute_Is_Used() =>
        Assert.NotEqual(White, PaintedAt(Svg(string.Format(SharpCorner, """stroke-miterlimit="10" """)), 470, 256));

    private const string CapLine = """<path d="M100 256H300" fill="none" stroke="currentColor" stroke-width="100" {0}/>""";

    [AvaloniaFact]
    public void The_Default_Line_Cap_Is_Butt() =>
        Assert.Equal(White, PaintedAt(Svg(string.Format(CapLine, "")), 330, 256));

    [AvaloniaFact]
    public void A_Square_Line_Cap_Extends() =>
        Assert.NotEqual(White, PaintedAt(Svg(string.Format(CapLine, """stroke-linecap="square" """)), 330, 300));

    [AvaloniaFact]
    public void A_Round_Line_Cap_Rounds()
    {
        var src = Svg(string.Format(CapLine, """stroke-linecap="round" """));

        Assert.NotEqual(White, PaintedAt(src, 330, 256));
        Assert.Equal(White, PaintedAt(src, 345, 300));
    }

    private const string Corner = """<path d="M100 100H400V400" fill="none" stroke="currentColor" stroke-width="100" {0}/>""";

    [AvaloniaFact]
    public void The_Default_Line_Join_Is_Miter() =>
        Assert.NotEqual(White, PaintedAt(Svg(string.Format(Corner, "")), 445, 55));

    [AvaloniaFact]
    public void A_Round_Line_Join_Rounds() =>
        Assert.Equal(White, PaintedAt(Svg(string.Format(Corner, """stroke-linejoin="round" """)), 445, 55));

    [AvaloniaFact]
    public void A_Bevel_Line_Join_Cuts_The_Corner() =>
        Assert.Equal(White, PaintedAt(Svg(string.Format(Corner, """stroke-linejoin="bevel" """)), 445, 55));

    // Two squares wound the same way.
    private const string NestedSquares = """<path d="M100 100H400V400H100ZM200 200H300V300H200Z" {0}/>""";

    [AvaloniaFact]
    public void The_Default_Fill_Rule_Is_Nonzero() =>
        Assert.NotEqual(White, PaintedAt(Svg(string.Format(NestedSquares, "")), 256, 256));

    [AvaloniaFact]
    public void An_Evenodd_Fill_Rule_Leaves_A_Hole() =>
        Assert.Equal(White, PaintedAt(Svg(string.Format(NestedSquares, """fill-rule="evenodd" """)), 256, 256));

    [AvaloniaFact]
    public void Fill_Defaults_To_The_Current_Color() =>
        Assert.NotEqual(White, PaintedAt(Svg("""<rect x="100" y="100" width="300" height="300"/>"""), 256, 256));

    [AvaloniaFact]
    public void Fill_None_On_The_Root_Inherits() =>
        Assert.Equal(White, PaintedAt(Svg("""<rect x="100" y="100" width="300" height="300"/>""", """viewBox="0 0 512 512" fill="none" """), 256, 256));

    [AvaloniaFact]
    public void A_Child_Can_Override_An_Inherited_Fill_None() =>
        Assert.NotEqual(White, PaintedAt(Svg("""<rect fill="currentColor" x="100" y="100" width="300" height="300"/>""", """viewBox="0 0 512 512" fill="none" """), 256, 256));

    [AvaloniaFact]
    public void Literal_Colors_Are_Painted() =>
        Assert.Equal(Colors.Red, PaintedAt(Svg("""<rect fill="#ff0000" x="100" y="100" width="300" height="300"/>"""), 256, 256));

    // CSS puts alpha last. Avalonia's own parser would read this as opaque navy.
    [AvaloniaFact]
    public void Eight_Digit_Hex_Is_Rgba()
    {
        var pixel = PaintedAt(Svg("""<rect fill="#ff000080" width="512" height="512"/>"""), 256, 256);

        Assert.Equal(255, pixel.R);
        Assert.InRange(pixel.G, 120, 135);
        Assert.InRange(pixel.B, 120, 135);
    }

    [AvaloniaFact]
    public void Four_Digit_Hex_Is_Rgba() =>
        Assert.Equal(Colors.Red, PaintedAt(Svg("""<rect fill="#f00f" width="512" height="512"/>"""), 256, 256));

    [AvaloniaFact]
    public void Stroke_Attributes_Inherit_From_A_Group()
    {
        var src = Svg("""<g stroke="currentColor" stroke-width="100" fill="none"><path d="M0 256H512"/></g>""");

        Assert.NotEqual(White, PaintedAt(src, 256, 300));
        Assert.Equal(White, PaintedAt(src, 256, 320));
    }

    [AvaloniaFact]
    public void A_Stroke_Width_Takes_A_Px_Suffix() =>
        Assert.NotEqual(White, PaintedAt(Svg("""<path d="M0 256H512" stroke="currentColor" stroke-width="100px"/>"""), 256, 300));

    // The left half, turned 90 degrees clockwise about the centre, is the top half.
    [AvaloniaFact]
    public void A_Rotate_Transform_Is_Applied()
    {
        var src = Svg("""<rect width="256" height="512" transform="rotate(90 256 256)"/>""");

        Assert.NotEqual(White, PaintedAt(src, 256, 100));
        Assert.Equal(White, PaintedAt(src, 256, 400));
    }

    [AvaloniaFact]
    public void A_Matrix_Transform_Is_Applied()
    {
        var src = Svg("""<rect width="256" height="512" transform="matrix(-1 0 0 -1 512 512)"/>""");

        Assert.Equal(White, PaintedAt(src, 100, 256));
        Assert.NotEqual(White, PaintedAt(src, 400, 256));
    }

    // Tools such as SVGO run numbers together.
    [AvaloniaFact]
    public void Number_Lists_Can_Run_Together()
    {
        var src = Svg("""<rect width="100" height="100" transform="translate(300-0)"/><polygon points="0-0 100-0 100 100"/>""");

        Assert.NotEqual(White, PaintedAt(src, 350, 50));
        Assert.NotEqual(White, PaintedAt(src, 90, 10));
    }

    [AvaloniaFact]
    public void A_SkewX_Transform_Is_Applied()
    {
        var src = Svg("""<rect width="100" height="512" transform="skewX(45)"/>""");

        Assert.Equal(White, PaintedAt(src, 50, 400));
        Assert.NotEqual(White, PaintedAt(src, 450, 400));
    }

    [AvaloniaFact]
    public void A_SkewY_Transform_Is_Applied()
    {
        var src = Svg("""<rect width="512" height="100" transform="skewY(45)"/>""");

        Assert.Equal(White, PaintedAt(src, 400, 50));
        Assert.NotEqual(White, PaintedAt(src, 400, 450));
    }

    [AvaloniaFact]
    public void A_Group_Transform_Composes_With_A_Childs()
    {
        var src = Svg("""<g transform="translate(200 0)"><rect width="100" height="512" transform="translate(100 0)"/></g>""");

        Assert.Equal(White, PaintedAt(src, 150, 256));
        Assert.NotEqual(White, PaintedAt(src, 350, 256));
    }

    [AvaloniaFact]
    public void Circles_Are_Painted()
    {
        var src = Svg("""<circle cx="256" cy="256" r="100"/>""");

        Assert.NotEqual(White, PaintedAt(src, 256, 256));
        Assert.Equal(White, PaintedAt(src, 180, 180));
    }

    [AvaloniaFact]
    public void Ellipses_Are_Painted()
    {
        var src = Svg("""<ellipse cx="256" cy="256" rx="200" ry="50"/>""");

        Assert.NotEqual(White, PaintedAt(src, 400, 256));
        Assert.Equal(White, PaintedAt(src, 256, 350));
    }

    [AvaloniaFact]
    public void Rounded_Rects_Are_Painted()
    {
        var src = Svg("""<rect x="100" y="100" width="300" height="300" rx="100"/>""");

        Assert.NotEqual(White, PaintedAt(src, 256, 110));
        Assert.Equal(White, PaintedAt(src, 105, 105));
    }

    [AvaloniaFact]
    public void Lines_Are_Painted() =>
        Assert.NotEqual(White, PaintedAt(Svg("""<line x1="0" y1="256" x2="512" y2="256" stroke="currentColor" stroke-width="40"/>"""), 256, 256));

    [AvaloniaFact]
    public void Polylines_Are_Painted() =>
        Assert.NotEqual(White, PaintedAt(Svg("""<polyline points="0,256 512,256" fill="none" stroke="currentColor" stroke-width="40"/>"""), 256, 256));

    [AvaloniaFact]
    public void Polygons_Are_Painted() =>
        Assert.NotEqual(White, PaintedAt(Svg("""<polygon points="100,100 400,100 400,400 100,400"/>"""), 256, 256));

    [AvaloniaFact]
    public void Unsupported_Elements_Draw_Nothing() =>
        Assert.Equal(White, PaintedAt(Svg("""<defs><rect width="512" height="512"/></defs>"""), 256, 256));

    // ── The classes in icon.css ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void The_Fill_None_Class_Beats_A_Fill_Attribute() =>
        Assert.Equal(White, PaintedAt(Svg("""<rect class="ionicon-fill-none" fill="currentColor" width="512" height="512"/>"""), 256, 256));

    private static readonly string VariableWidthLine = Svg("""<path class="ionicon-stroke-width" stroke="currentColor" stroke-width="2" d="M0 256H512"/>""");

    // var(--ionicon-stroke-width, 32px): 16 either side of the line.
    [AvaloniaFact]
    public void The_Stroke_Width_Class_Defaults_To_32()
    {
        Assert.NotEqual(White, PaintedAt(VariableWidthLine, 256, 268));
        Assert.Equal(White, PaintedAt(VariableWidthLine, 256, 280));
    }

    private static Border WithStrokeWidth(Control child, double width)
    {
        var scope = new Border { Child = child };
        scope.Resources[AvonicIcon.StrokeWidthKey] = width;
        return scope;
    }

    [AvaloniaFact]
    public void The_Stroke_Width_Class_Reads_The_Token()
    {
        using var scene = Show(WithStrokeWidth(Icon(VariableWidthLine, 512), 100), 512);

        Assert.NotEqual(White, PixelAt(Frame(scene), 256, 300));
    }

    [AvaloniaFact]
    public void Changing_The_Token_Repaints()
    {
        var scope = WithStrokeWidth(Icon(VariableWidthLine, 512), 8);
        using var scene = Show(scope, 512);
        Assert.Equal(White, PixelAt(Frame(scene), 256, 280));

        scope.Resources[AvonicIcon.StrokeWidthKey] = 100d;

        Assert.NotEqual(White, PixelAt(Frame(scene), 256, 280));
    }

    // Measured: chevron-forward stayed at 48px with the variable at 8px.
    [AvaloniaFact]
    public void The_Token_Does_Not_Affect_A_Literal_Stroke_Width()
    {
        var src = Svg("""<path stroke="currentColor" stroke-width="100" d="M0 256H512"/>""");
        using var scene = Show(WithStrokeWidth(Icon(src, 512), 8), 512);

        Assert.NotEqual(White, PixelAt(Frame(scene), 256, 300));
    }

    [AvaloniaFact]
    public void The_Theme_Token_Is_Ionics_Default() => InstalledTheme.Run(_ =>
    {
        Assert.True(Application.Current!.TryGetResource(AvonicIcon.StrokeWidthKey, null, out var value));
        Assert.Equal(32d, value);
    });

    // ── Invalid content: validateContent ─────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData("""<svg viewBox="0 0 512 512"><script>1</script><rect width="512" height="512"/></svg>""")]
    [InlineData("""<svg viewBox="0 0 512 512"><rect width="512" height="512"/><script>1</script></svg>""")]
    [InlineData("""<svg viewBox="0 0 512 512"><g><script>1</script></g><rect width="512" height="512"/></svg>""")]
    [InlineData("""<svg viewBox="0 0 512 512"><defs><script>1</script></defs><rect width="512" height="512"/></svg>""")]
    [InlineData("""<svg viewBox="0 0 512 512" onload="1"><rect width="512" height="512"/></svg>""")]
    [InlineData("""<svg viewBox="0 0 512 512"><rect onclick="1" width="512" height="512"/></svg>""")]
    [InlineData("""<div><rect width="512" height="512"/></div>""")]
    [InlineData("""<svg viewBox="0 0 512 512"><rect width="512" """)]
    [InlineData("not xml")]
    [InlineData("")]
    public void Invalid_Content_Is_Blank(string svg) => Assert.Empty(SvgArtwork.Parse(svg).Layers);

    // What design tools export.
    [AvaloniaFact]
    public void A_Document_Type_Is_Skipped() =>
        Assert.Single(SvgArtwork.Parse("""
            <?xml version="1.0" encoding="utf-8"?>
            <!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd">
            <svg viewBox="0 0 512 512"><rect width="512" height="512"/></svg>
            """).Layers);

    [AvaloniaFact]
    public void Leading_Whitespace_Is_Skipped() =>
        Assert.Single(SvgArtwork.Parse("""

              <?xml version="1.0"?><svg viewBox="0 0 512 512"><rect width="512" height="512"/></svg>
            """).Layers);

    // The internal subset is ignored, so the entity is never defined, let alone expanded.
    [AvaloniaFact]
    public void Entities_Are_Not_Expanded() =>
        Assert.Empty(SvgArtwork.Parse("""<!DOCTYPE svg [<!ENTITY x "1">]><svg viewBox="0 0 512 512"><rect width="&x;" height="512"/></svg>""").Layers);

    // A stack overflow would kill the process, and can't be caught.
    [AvaloniaFact]
    public void Very_Deep_Nesting_Is_Blank_Not_Fatal()
    {
        const int Depth = 200_000;
        var svg = """<svg viewBox="0 0 512 512">""" + string.Concat(Enumerable.Repeat("<g>", Depth)) + """<rect width="512" height="512"/>""" + string.Concat(Enumerable.Repeat("</g>", Depth)) + "</svg>";

        Assert.Empty(SvgArtwork.Parse(svg).Layers);
    }

    [AvaloniaFact]
    public void Ordinary_Nesting_Paints()
    {
        var svg = """<svg viewBox="0 0 512 512">""" + string.Concat(Enumerable.Repeat("<g>", 50)) + """<rect width="512" height="512"/>""" + string.Concat(Enumerable.Repeat("</g>", 50)) + "</svg>";

        Assert.Single(SvgArtwork.Parse(svg).Layers);
    }

    [AvaloniaFact]
    public void An_Invalid_Path_Paints_Nothing_And_Is_Not_Fatal() =>
        Assert.NotEqual(White, PaintedAt(Svg("""<path d="not a path"/><rect width="512" height="512"/>"""), 256, 256));

    // Ionic's browser reads these as HTML, which doesn't care about either.
    [AvaloniaTheory]
    [InlineData("""<svg sketch:type="MSPage" viewBox="0 0 512 512"><rect width="512" height="512"/></svg>""")]
    [InlineData("""<svg viewBox="0 0 512 512"><rect xlink:href="#a" width="512" height="512"/></svg>""")]
    [InlineData("\uFEFF<svg viewBox=\"0 0 512 512\"><rect width=\"512\" height=\"512\"/></svg>")]
    [InlineData("  \uFEFF<svg viewBox=\"0 0 512 512\"><rect width=\"512\" height=\"512\"/></svg>")]
    public void Export_Quirks_Are_Tolerated(string svg) => Assert.Single(SvgArtwork.Parse(svg).Layers);

    [AvaloniaFact]
    public void Display_None_Draws_Nothing() =>
        Assert.Equal(White, PaintedAt(Svg("""<rect display="none" width="512" height="512"/>"""), 256, 256));

    [AvaloniaFact]
    public void Display_None_Hides_A_Groups_Children() =>
        Assert.Equal(White, PaintedAt(Svg("""<g display="none"><rect width="512" height="512"/></g>"""), 256, 256));

    [AvaloniaFact]
    public void Display_None_Still_Blanks_A_Hidden_Script() =>
        Assert.Empty(SvgArtwork.Parse("""<svg viewBox="0 0 512 512"><g display="none"><script>1</script></g><rect width="512" height="512"/></svg>""").Layers);

    [AvaloniaFact]
    public void An_Unsupported_Gradient_Paints_Nothing() =>
        Assert.Equal(White, PaintedAt(Svg("""<rect fill="url(#g)" width="512" height="512"/>"""), 256, 256));

    [AvaloniaFact]
    public void An_Unsupported_Gradient_Uses_Its_Fallback() =>
        Assert.Equal(Colors.Red, PaintedAt(Svg("""<rect fill="url(#g) #ff0000" width="512" height="512"/>"""), 256, 256));

    [AvaloniaTheory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    public void An_Invalid_Stroke_Width_Is_Ignored(string width) =>
        Assert.NotEqual(White, PaintedAt(Svg($"""<path stroke="currentColor" stroke-width="{width}" fill="none" d="M0 256H512"/>"""), 256, 256));

    // Measured in Chromium: rx alone gave a bounding box 10 tall, with ry computing to auto.
    [AvaloniaTheory]
    [InlineData("""rx="100" """)]
    [InlineData("""ry="100" """)]
    public void An_Ellipse_Radius_Takes_The_Other_If_Omitted(string radius)
    {
        var src = Svg($"""<ellipse cx="256" cy="256" {radius}/>""");

        Assert.NotEqual(White, PaintedAt(src, 256, 180));
        Assert.NotEqual(White, PaintedAt(src, 180, 256));
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    // The hollow middle of a ring, which paints nothing.
    private static readonly string Ring = Svg("""<circle cx="256" cy="256" r="240" fill="none" stroke="currentColor" stroke-width="32"/>""");

    [AvaloniaFact]
    public void All_Of_The_Box_Takes_Input()
    {
        var icon = Icon(Ring);
        using var scene = Show(icon);
        Frame(scene);

        Assert.Contains(icon, scene.Window.GetInputElementsAt(new Point(32, 32)));
        Assert.Contains(icon, scene.Window.GetInputElementsAt(new Point(1, 1)));
    }

    [AvaloniaFact]
    public void A_Blank_Icon_Takes_Input()
    {
        var icon = new AvonicIcon { IconName = "no-such-icon", FontSize = 64 };
        using var scene = Show(icon);
        Frame(scene);

        Assert.Contains(icon, scene.Window.GetInputElementsAt(new Point(32, 32)));
    }

    [AvaloniaFact]
    public void Clipping_Can_Be_Styled()
    {
        var icon = new AvonicIcon();
        Assert.True(icon.ClipToBounds);
        Assert.False(icon.IsSet(Visual.ClipToBoundsProperty), "A local value would beat a style");
    }

    // ── Threading ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void AddIcons_Off_The_Ui_Thread_Throws()
    {
        var failure = Task.Run(() => Record.Exception(() => AvonicIcon.AddIcons(new Dictionary<string, string> { ["off-thread"] = Solid }))).Result;

        Assert.IsType<InvalidOperationException>(failure);
    }

    // ── Accessibility ────────────────────────────────────────────────────────────────────────

    // role="img"
    [AvaloniaFact]
    public void The_Automation_Peer_Is_An_Image()
    {
        var icon = new AvonicIcon { IconName = "heart" };
        using var scene = Show(icon);

        Assert.Equal(AutomationControlType.Image, ControlAutomationPeer.CreatePeerForElement(icon).GetAutomationControlType());
    }

    // aria-label
    [AvaloniaFact]
    public void AutomationProperties_Name_Is_The_Label()
    {
        var icon = new AvonicIcon { IconName = "heart" };
        AutomationProperties.SetName(icon, "Favourite");
        using var scene = Show(icon);

        Assert.Equal("Favourite", ControlAutomationPeer.CreatePeerForElement(icon).GetName());
    }

    // ── Host classes ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Size_Sets_Its_Pseudoclass()
    {
        var icon = new AvonicIcon { Size = IconSize.Small };
        Assert.Contains(":size-small", icon.Classes);

        icon.Size = IconSize.Large;
        Assert.Contains(":size-large", icon.Classes);
        Assert.DoesNotContain(":size-small", icon.Classes);

        icon.Size = null;
        Assert.DoesNotContain(":size-large", icon.Classes);
    }

    [AvaloniaFact]
    public void A_Flippable_Icon_Has_Its_Pseudoclass()
    {
        Assert.Contains(":flip-rtl", new AvonicIcon { IconName = "chevron-forward" }.Classes);
        Assert.DoesNotContain(":flip-rtl", new AvonicIcon { IconName = "heart" }.Classes);
        Assert.Contains(":flip-rtl", new AvonicIcon { IconName = "heart", FlipRtl = true }.Classes);
        Assert.DoesNotContain(":flip-rtl", new AvonicIcon { IconName = "chevron-forward", FlipRtl = false }.Classes);
    }

    [AvaloniaFact]
    public void Color_Sets_The_Ion_Color_Pseudoclass() =>
        Assert.Contains(ColorPalette.IonColorPseudoClass, new AvonicIcon { Color = "danger" }.Classes);
}
