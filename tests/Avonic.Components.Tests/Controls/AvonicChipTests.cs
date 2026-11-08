using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-chip in Chromium, at 390 px wide, and the tests are named after the behaviours in
/// docs/components/ion-chip/PLAN.md. Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub.
/// Text is a different font here, so a child that has to be a known size is a box of that size.
/// </summary>
public class AvonicChipTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color PrimaryShade = Color.Parse("#004ACD");
    private static readonly Color Danger = Color.Parse("#C5000F");
    private static readonly Color DangerShade = Color.Parse("#AD000D");

    private static AvonicChip Chip(params object[] items)
    {
        var chip = new AvonicChip();
        foreach (var item in items)
        {
            chip.Items.Add(item);
        }

        return chip;
    }

    /// <summary>On the stub, which has no padding or size of its own, a box to press, at the top left.</summary>
    private static AvonicChip Bare(params object[] items)
    {
        var chip = Chip(items.Length == 0 ? [Box()] : items);
        chip.HorizontalAlignment = HorizontalAlignment.Left;
        chip.VerticalAlignment = VerticalAlignment.Top;
        chip.Padding = new Thickness(12, 6);
        return chip;
    }

    /// <summary>A child of a known size, where text would be a different font's.</summary>
    private static Border Box(double width = 40, double height = 16) => new() { Width = width, Height = height };

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static Border Native(AvonicChip chip) => Part<Border>(chip, "PART_Native");
    private static AvonicRippleEffect Ripple(AvonicChip chip) => Part<AvonicRippleEffect>(chip, "PART_Ripple");

    private static bool Has(Control control, string pseudoClass) => control.Classes.Contains(pseudoClass);

    private static Rect Bounds(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void AssertBox(double x, double y, double width, double height, Rect actual)
    {
        // Layout rounds to whole pixels, and Chromium keeps fractions.
        Assert.InRange(actual.X, x - 0.5, x + 0.5);
        Assert.InRange(actual.Y, y - 0.5, y + 0.5);
        Assert.InRange(actual.Width, width - 0.5, width + 0.5);
        Assert.InRange(actual.Height, height - 0.5, height + 0.5);
    }

    /// <summary>A brush as CSS's <c>rgba()</c>: its colour, with its opacity folded into the alpha. Null is transparent.</summary>
    private static Color Rgba(IBrush? brush)
    {
        if (brush is null)
        {
            return Colors.Transparent;
        }

        var solid = Assert.IsAssignableFrom<ISolidColorBrush>(brush);
        return Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B);
    }

    private static void AssertRgba(Color rgb, double alpha, IBrush? brush)
    {
        var actual = Rgba(brush);
        Assert.InRange(actual.A, alpha * 255 - 1, alpha * 255 + 1);
        if (alpha > 0)
        {
            Assert.Equal((rgb.R, rgb.G, rgb.B), (actual.R, actual.G, actual.B));
        }
    }

    private static void AssertTransparent(IBrush? brush) => Assert.Equal(0, Rgba(brush).A);

    private static Color Pixel(Scene scene, int x, int y)
    {
        Dispatcher.UIThread.RunJobs();
        return Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, x, y);
    }

    private static void AssertClose(Color expected, Color actual, int tolerance = 2)
    {
        Assert.InRange(actual.R, expected.R - tolerance, expected.R + tolerance);
        Assert.InRange(actual.G, expected.G - tolerance, expected.G + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
    }

    /// <summary>Controls in a column on white, from the top left of the window.</summary>
    private static Scene Themed(params Control[] controls)
    {
        var holder = new StackPanel();
        holder.Children.AddRange(controls);
        var scene = new Scene(holder);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();
        return scene;
    }

    private static Point Centre(Control control, Scene scene) => Bounds(control, scene.Window).Center;

    private static readonly Point Away = new(380, 830);

    private static void Hover(Scene scene, Point point)
    {
        scene.Window.MouseMove(point, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void PressKey(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static Pointer Finger() => new(Pointer.GetNextFreeId(), PointerType.Touch, true);

    /// <summary>A finger, which never hovers, as on a phone, where <c>any-hover: hover</c> doesn't match. Headless input is a mouse.</summary>
    private static void FingerDown(AvonicChip chip, Pointer finger, Scene scene)
    {
        chip.RaiseEvent(new PointerPressedEventArgs(chip, finger, scene.Window, Centre(chip, scene), 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        Dispatcher.UIThread.RunJobs();
    }

    private static void FingerUp(AvonicChip chip, Pointer finger, Scene scene)
    {
        chip.RaiseEvent(new PointerReleasedEventArgs(chip, finger, scene.Window, Centre(chip, scene), 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        Dispatcher.UIThread.RunJobs();
    }

    // ── Behaviour 1: a row as wide as its content ─────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_The_Box_Is_Its_Content_With_Padding_Margin_And_A_Minimum_Height() => InstalledTheme.Run(_ =>
    {
        var child = Box();
        var chip = Chip(child);
        using var scene = Themed(chip);

        // margin 4, padding 12 and 6, min-height 32: 40 + 24 wide, and 16 + 12 is under the minimum.
        AssertBox(4, 4, 64, 32, Bounds(chip, scene.Window));
        // align-items: center, so the child is at y + 8, not at the padding's 6.
        AssertBox(16, 12, 40, 16, Bounds(child, scene.Window));
        Assert.Equal(new CornerRadius(16), Native(chip).CornerRadius);
        Assert.Equal(new Cursor(StandardCursorType.Hand).ToString(), chip.Cursor?.ToString());
    });

    [AvaloniaFact]
    public void B1_Taller_Content_Grows_The_Box_And_The_Rest_Stay_Centred() => InstalledTheme.Run(_ =>
    {
        var tall = Box(20, 30);
        var small = Box(20, 10);
        var chip = Chip(tall, small);
        using var scene = Themed(chip);

        AssertBox(4, 4, 64, 42, Bounds(chip, scene.Window));
        AssertBox(16, 10, 20, 30, Bounds(tall, scene.Window));
        AssertBox(36, 20, 20, 10, Bounds(small, scene.Window));
    });

    [AvaloniaFact]
    public void B1_Text_Is_14_In_The_Font_Family() => InstalledTheme.Run(_ =>
    {
        var chip = Chip("Text");
        using var scene = Themed(chip);

        var text = chip.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(14, text.FontSize);
        Assert.Equal(FontWeight.Normal, text.FontWeight);
        Assert.Equal(FontFamily.Parse("Roboto, Helvetica Neue, sans-serif"), text.FontFamily);
    });

    [AvaloniaFact]
    public void B1_It_Sits_In_The_Middle_Of_A_Taller_Row() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { Box(20, 80), chip } };
        using var scene = Themed(row);

        // vertical-align: middle, beside something 80 high.
        AssertBox(24, 24, 64, 32, Bounds(chip, scene.Window));
    });

    [AvaloniaFact]
    public void B1_A_String_Is_A_Child_Too()
    {
        var chip = Chip("Plain text");
        using var scene = new Scene(chip);

        var text = chip.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal("Plain text", text.Text);
        Assert.True(text.Bounds.Width > 0);
    }

    // ── Behaviour 2: plain ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_Plain_Is_Text_At_12_Percent_Behind_Text_At_87() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        using var scene = Themed(chip);

        AssertRgba(Colors.Black, 0.12, Native(chip).Background);
        AssertRgba(Colors.Black, 0.87, chip.Foreground);
        Assert.Equal(default, Native(chip).BorderThickness);

        // 12% black on white is 224.
        AssertClose(Color.FromRgb(224, 224, 224), Pixel(scene, 36, 6));
    });

    // ── Behaviour 3: outline ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Outline_Is_A_Border_And_No_Background_With_The_Padding_Inside_It() => InstalledTheme.Run(_ =>
    {
        var child = Box();
        var chip = Chip(child);
        chip.Outline = true;
        using var scene = Themed(chip);

        AssertTransparent(Native(chip).Background);
        Assert.Equal(new Thickness(1), Native(chip).BorderThickness);
        AssertRgba(Colors.Black, 0.32, Native(chip).BorderBrush);
        AssertRgba(Colors.Black, 0.87, chip.Foreground);

        // box-sizing: border-box: 2 wider, the same height, and the child 1 further in.
        AssertBox(4, 4, 66, 32, Bounds(chip, scene.Window));
        AssertBox(17, 12, 40, 16, Bounds(child, scene.Window));
    });

    [AvaloniaFact]
    public void B3_Outline_Can_Be_Turned_Off() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        chip.Outline = true;
        using var scene = Themed(chip);

        chip.Outline = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(default, Native(chip).BorderThickness);
        AssertRgba(Colors.Black, 0.12, Native(chip).Background);
    });

    // ── Behaviour 4: colour ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Color_Is_Its_Base_At_8_Percent_Behind_Its_Shade() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        chip.Color = "primary";
        using var scene = Themed(chip);

        AssertRgba(Primary, 0.08, Native(chip).Background);
        AssertRgba(PrimaryShade, 1, chip.Foreground);
    });

    [AvaloniaFact]
    public void B4_A_Coloured_Outline_Is_Transparent_With_The_Base_At_32_Percent() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        chip.Color = "primary";
        chip.Outline = true;
        using var scene = Themed(chip);

        AssertTransparent(Native(chip).Background);
        AssertRgba(Primary, 0.32, Native(chip).BorderBrush);
        AssertRgba(PrimaryShade, 1, chip.Foreground);
    });

    [AvaloniaFact]
    public void B4_Changing_The_Color_Repaints() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        chip.Color = "primary";
        using var scene = Themed(chip);

        chip.Color = "danger";
        Dispatcher.UIThread.RunJobs();
        AssertRgba(Danger, 0.08, Native(chip).Background);
        AssertRgba(DangerShade, 1, chip.Foreground);

        chip.Color = null;
        Dispatcher.UIThread.RunJobs();
        AssertRgba(Colors.Black, 0.12, Native(chip).Background);
        AssertRgba(Colors.Black, 0.87, chip.Foreground);
    });

    // ── Behaviour 5: an unknown colour ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_An_Unknown_Color_Is_Transparent_With_The_Text_Inherited() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        chip.Color = "nope";
        var outline = Chip(Box());
        outline.Color = "nope";
        outline.Outline = true;
        var holder = new StackPanel { Children = { chip, outline } };
        holder.SetValue(TextElement.ForegroundProperty, Brushes.Green);
        using var scene = Themed(holder);

        AssertTransparent(Native(chip).Background);
        AssertRgba(Colors.Green, 1, chip.Foreground);

        // border-color falls back to its initial value, currentcolor.
        AssertTransparent(Native(outline).Background);
        AssertRgba(Colors.Green, 1, Native(outline).BorderBrush);
    });

    [AvaloniaFact]
    public void B5_An_Unknown_Color_Inside_A_Coloured_Ancestor_Paints_The_Ancestors() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        chip.Color = "nope";
        var holder = new StackPanel { Children = { chip } };
        ColorPalette.SetColor(holder, "danger");
        using var scene = Themed(holder);

        AssertRgba(Danger, 0.08, Native(chip).Background);
        AssertRgba(DangerShade, 1, chip.Foreground);
    });

    // ── Behaviour 6: disabled ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Disabled_Fades_To_40_Percent() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        chip.IsEnabled = false;
        var inside = Chip(Box());
        var holder = new StackPanel { IsEnabled = false, Children = { inside } };
        using var scene = Themed(chip, holder);

        Assert.Equal(0.4, chip.Opacity, 3);
        Assert.Equal(0.4, inside.Opacity, 3);

        chip.IsEnabled = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, chip.Opacity, 3);
    });

    [AvaloniaFact]
    public void B6_A_Tap_On_A_Disabled_Chip_Goes_To_What_Is_Behind_It()
    {
        var behind = new Border { Background = Brushes.White };
        var pressed = 0;
        behind.PointerPressed += (_, _) => pressed++;
        var chip = Bare();
        chip.IsEnabled = false;
        using var scene = new Scene(new Panel { Children = { behind, chip } });
        using var clock = new TestClock();

        var centre = Centre(chip, scene);
        scene.Window.MouseDown(centre, MouseButton.Left, RawInputModifiers.None);
        clock.At(300);
        Assert.Equal(1, pressed);
        Assert.False(Has(chip, Activatable.ActivatedPseudoClass));
        Assert.Empty(Ripple(chip).Ripples);
        scene.Window.MouseUp(centre, MouseButton.Left, RawInputModifiers.None);
    }

    // ── Behaviour 7: activatable, with a ripple ──────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_A_Press_Activates_After_100ms_With_A_Ripple()
    {
        var chip = Bare();
        using var scene = new Scene(chip);
        using var clock = new TestClock();

        var at = new Point(10, 10);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        clock.At(99);
        Assert.False(Has(chip, Activatable.ActivatedPseudoClass));
        clock.At(100);
        Assert.True(Has(chip, Activatable.ActivatedPseudoClass));
        Assert.Single(Ripple(chip).Ripples);

        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        clock.At(1000);
        Assert.False(Has(chip, Activatable.ActivatedPseudoClass));
    }

    [AvaloniaFact]
    public void B7_A_Quick_Tap_Still_Activates()
    {
        var chip = Bare();
        using var scene = new Scene(chip);
        using var clock = new TestClock();

        scene.Window.MouseDown(new Point(10, 10), MouseButton.Left, RawInputModifiers.None);
        clock.At(20);
        scene.Window.MouseUp(new Point(10, 10), MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(Has(chip, Activatable.ActivatedPseudoClass));
        Assert.Single(Ripple(chip).Ripples);
    }

    [AvaloniaFact]
    public void B7_A_Press_On_The_Padding_Activates_Too()
    {
        var chip = Bare();
        using var scene = new Scene(chip);
        using var clock = new TestClock();

        // Inside the padding, left of the child.
        scene.Window.MouseDown(new Point(4, 14), MouseButton.Left, RawInputModifiers.None);
        clock.At(200);
        Assert.True(Has(chip, Activatable.ActivatedPseudoClass));
        scene.Window.MouseUp(new Point(4, 14), MouseButton.Left, RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void B7_No_Ripple_With_The_Ripple_Effect_Off()
    {
        var chip = Bare();
        using var scene = new Scene(chip);
        using var clock = new TestClock();

        AvonicConfig.RippleEffect = false;
        try
        {
            scene.Window.MouseDown(new Point(10, 10), MouseButton.Left, RawInputModifiers.None);
            clock.At(200);
            Assert.True(Has(chip, Activatable.ActivatedPseudoClass));
            Assert.Empty(Ripple(chip).Ripples);
            scene.Window.MouseUp(new Point(10, 10), MouseButton.Left, RawInputModifiers.None);
        }
        finally
        {
            AvonicConfig.RippleEffect = true;
        }
    }

    [AvaloniaFact]
    public void B7_The_Ripple_Fills_The_Padding_Box_In_The_Current_Colour() => InstalledTheme.Run(_ =>
    {
        var plain = Chip(Box());
        var outline = Chip(Box());
        outline.Outline = true;
        var coloured = Chip(Box());
        coloured.Color = "primary";
        var token = Chip(Box());
        token.Resources["Avonic.Chip.Color"] = Brushes.Green;
        using var scene = Themed(plain, outline, coloured, token);

        AssertBox(4, 4, 64, 32, Bounds(Ripple(plain), scene.Window));
        // Inside the border, as position: absolute fills the padding box.
        AssertBox(5, 45, 64, 30, Bounds(Ripple(outline), scene.Window));

        AssertRgba(Colors.Black, 0.87, Ripple(plain).Foreground);
        AssertRgba(PrimaryShade, 1, Ripple(coloured).Foreground);
        AssertRgba(Colors.Green, 1, Ripple(token).Foreground);
    });

    [AvaloniaFact]
    public void B7_The_Ripple_Is_Clipped_To_The_Radius() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        chip.Resources["Avonic.Chip.Background"] = Brushes.White;
        chip.Resources["Avonic.Chip.Color"] = Brushes.Blue;
        using var scene = Themed(chip);
        using var clock = new TestClock();

        // Pressed at the left end, and held until the ripple has grown past every corner.
        var at = new Point(12, 20);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        clock.At(1000);
        Ripple(chip).Tick();

        // Inside the box, but outside the 16 px radius: white. Beside the press, in the ripple: blue.
        AssertClose(Colors.White, Pixel(scene, 5, 5));
        var inRipple = Pixel(scene, 14, 20);
        Assert.True(inRipple.B > inRipple.R + 20, $"{inRipple} has no ripple");
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
    });

    /// <summary>On the stub, or in the theme, whose template is its own.</summary>
    private static void StubOrTheme(bool themed, Action test)
    {
        if (themed)
        {
            InstalledTheme.Run(_ => test());
        }
        else
        {
            test();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void B7_The_Ripple_Paints_Over_The_Children(bool themed) => StubOrTheme(themed, () =>
    {
        var chip = Bare();
        using var scene = new Scene(chip);

        var ripple = Ripple(chip);
        var panel = Assert.IsAssignableFrom<Panel>(ripple.GetVisualParent());
        var presenter = panel.Children.OfType<ItemsPresenter>().Single();
        Assert.True(ripple.ZIndex > presenter.ZIndex);
    });

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void B7_The_Chips_Ripple_Is_Found_Before_One_In_A_Child(bool themed) => StubOrTheme(themed, () =>
    {
        var inner = new AvonicRippleEffect();
        var child = new Border { Width = 40, Height = 16, Background = Brushes.Blue, Child = inner };
        var chip = Bare(child);
        using var scene = new Scene(chip);
        using var clock = new TestClock();

        var at = Centre(child, scene);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        clock.At(200);
        Assert.Single(Ripple(chip).Ripples);
        Assert.Empty(inner.Ripples);
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
    });

    [AvaloniaFact]
    public void B7_It_Is_A_Plain_Group_To_Automation()
    {
        var chip = Bare(new AvonicLabel { Content = "Label" });
        using var scene = new Scene(chip);

        var peer = ControlAutomationPeer.CreatePeerForElement(chip);
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.False(peer.IsContentElement());
        Assert.True(peer.IsEnabled());

        // aria-disabled
        chip.IsEnabled = false;
        Assert.False(peer.IsEnabled());
    }

    // ── Behaviour 8: activated, with a finger ────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(null, false, 0.2)]
    [InlineData(null, true, 0.08)]
    [InlineData("primary", false, 0.16)]
    [InlineData("primary", true, 0.16)]
    public void B8_A_Finger_Shows_The_Activated_Background(string? color, bool outline, double alpha) => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        chip.Color = color;
        chip.Outline = outline;
        using var scene = Themed(chip);
        using var clock = new TestClock();
        var finger = Finger();

        FingerDown(chip, finger, scene);
        clock.At(200);
        Assert.True(Has(chip, Activatable.ActivatedPseudoClass));
        AssertRgba(color is null ? Colors.Black : Primary, alpha, Native(chip).Background);
        FingerUp(chip, finger, scene);
    });

    // ── Behaviour 9: hover ───────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(null, false, 0.16, 0.12)]
    [InlineData(null, true, 0.04, 0)]
    [InlineData("primary", false, 0.12, 0.08)]
    [InlineData("primary", true, 0.12, 0)]
    public void B9_A_Mouse_Over_Shows_The_Hover_Background_And_Keeps_It_Through_A_Press(string? color, bool outline, double alpha, double rest) =>
        InstalledTheme.Run(_ =>
        {
            var chip = Chip(Box());
            chip.Color = color;
            chip.Outline = outline;
            using var scene = Themed(chip);
            using var clock = new TestClock();
            var rgb = color is null ? Colors.Black : Primary;

            var centre = Centre(chip, scene);
            Hover(scene, centre);
            AssertRgba(rgb, alpha, Native(chip).Background);

            // Hover comes after activated in chip.md.css, so a mouse press shows the hover value.
            scene.Window.MouseDown(centre, MouseButton.Left, RawInputModifiers.None);
            clock.At(300);
            Assert.True(Has(chip, Activatable.ActivatedPseudoClass));
            AssertRgba(rgb, alpha, Native(chip).Background);
            scene.Window.MouseUp(centre, MouseButton.Left, RawInputModifiers.None);
            clock.At(1000);

            Hover(scene, Away);
            AssertRgba(rgb, rest, Native(chip).Background);
        });

    // ── Behaviour 10: focus ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_It_Is_Not_A_Tab_Stop_And_A_Click_Leaves_Focus_Where_It_Was() => InstalledTheme.Run(_ =>
    {
        var before = new Button { Content = "before" };
        var chip = Chip(Box());
        var after = new Button { Content = "after" };
        using var scene = Themed(before, chip, after);

        before.Focus();
        PressKey(scene, PhysicalKey.Tab);
        Assert.True(after.IsFocused);

        var centre = Centre(chip, scene);
        scene.Window.MouseDown(centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(after.IsFocused);
    });

    [AvaloniaTheory]
    [InlineData(null, false, 0.16)]
    [InlineData(null, true, 0.04)]
    [InlineData("primary", false, 0.12)]
    [InlineData("primary", true, 0.12)]
    public void B10_A_Focusable_Chip_Shows_Focus_From_Any_Source(string? color, bool outline, double alpha) => InstalledTheme.Run(_ =>
    {
        var before = new Button { Content = "before" };
        var chip = Chip(Box());
        chip.Color = color;
        chip.Outline = outline;
        chip.Focusable = true;
        using var scene = Themed(before, chip);
        using var clock = new TestClock();
        var rgb = color is null ? Colors.Black : Primary;

        before.Focus();
        PressKey(scene, PhysicalKey.Tab);
        Assert.True(chip.IsFocused);
        AssertRgba(rgb, alpha, Native(chip).Background);

        // :focus, not :focus-visible: a click shows it too.
        before.Focus();
        Dispatcher.UIThread.RunJobs();
        var centre = Centre(chip, scene);
        scene.Window.MouseDown(centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(centre, MouseButton.Left, RawInputModifiers.None);
        Hover(scene, Away);
        clock.At(1000);
        Assert.True(chip.IsFocused);
        AssertRgba(rgb, alpha, Native(chip).Background);
    });

    // outline: none. The adorner comes from the app's base theme, which TestApp doesn't load, so this adds it underneath. FocusAdorner
    // reads null whether or not it's set, so only the layer shows what's really drawn.
    [AvaloniaFact]
    public void B10_There_Is_No_Focus_Adorner_Over_A_Base_Theme()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var chip = Chip(Box());
                chip.Focusable = true;
                var other = new Button();
                using var scene = new Scene(new StackPanel { Children = { chip, other } });

                var layer = AdornerLayer.GetAdornerLayer(chip)!;

                PressKey(scene, PhysicalKey.Tab);
                Assert.True(chip.IsFocused);
                Assert.Empty(layer.Children);

                // Proves the base theme really does give a focused control one.
                PressKey(scene, PhysicalKey.Tab);
                Assert.True(other.IsFocused);
                Assert.Single(layer.Children);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    // ── Behaviour 11: the variables ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Background_Beats_Every_State_On_A_Plain_Chip() => InstalledTheme.Run(_ =>
    {
        var chip = Chip(Box());
        chip.Focusable = true;
        chip.Resources["Avonic.Chip.Background"] = Brushes.Red;
        using var scene = Themed(chip);
        using var clock = new TestClock();

        AssertRgba(Colors.Red, 1, Native(chip).Background);

        var centre = Centre(chip, scene);
        Hover(scene, centre);
        AssertRgba(Colors.Red, 1, Native(chip).Background);

        scene.Window.MouseDown(centre, MouseButton.Left, RawInputModifiers.None);
        clock.At(300);
        Assert.True(Has(chip, Activatable.ActivatedPseudoClass));
        AssertRgba(Colors.Red, 1, Native(chip).Background);
        scene.Window.MouseUp(centre, MouseButton.Left, RawInputModifiers.None);
        clock.At(1000);
        Hover(scene, Away);

        // Focused by the click, and then pressed with a finger, with no hover.
        Assert.True(chip.IsFocused);
        AssertRgba(Colors.Red, 1, Native(chip).Background);
        var finger = Finger();
        FingerDown(chip, finger, scene);
        clock.At(1300);
        Assert.True(Has(chip, Activatable.ActivatedPseudoClass));
        AssertRgba(Colors.Red, 1, Native(chip).Background);
        FingerUp(chip, finger, scene);
    });

    [AvaloniaFact]
    public void B11_Outline_And_Colour_Ignore_The_Variables() => InstalledTheme.Run(_ =>
    {
        var outline = Chip(Box());
        outline.Outline = true;
        var coloured = Chip(Box());
        coloured.Color = "primary";
        var holder = new StackPanel { Children = { outline, coloured } };
        holder.Resources["Avonic.Chip.Background"] = Brushes.Red;
        holder.Resources["Avonic.Chip.Color"] = Brushes.Green;
        using var scene = Themed(holder);

        AssertTransparent(Native(outline).Background);
        AssertRgba(Colors.Green, 1, outline.Foreground);

        Hover(scene, Centre(outline, scene));
        AssertRgba(Colors.Black, 0.04, Native(outline).Background);

        AssertRgba(Primary, 0.08, Native(coloured).Background);
        AssertRgba(PrimaryShade, 1, coloured.Foreground);
    });

    [AvaloniaFact]
    public void B11_Color_Colours_The_Text() => InstalledTheme.Run(_ =>
    {
        var chip = Chip("Text");
        chip.Resources["Avonic.Chip.Color"] = Brushes.Green;
        using var scene = Themed(chip);

        var text = chip.GetVisualDescendants().OfType<TextBlock>().Single();
        AssertRgba(Colors.Green, 1, text.Foreground);
    });

    // ── Behaviour 12: a slotted icon ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_An_Icon_Is_20_And_Grey_And_First_And_Last_Have_Their_Margins() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicIcon { IconName = "pin" };
        var last = new AvonicIcon { IconName = "close-circle" };
        var chip = Chip(first, Box(), last);
        using var scene = Themed(chip);

        Assert.Equal(20, first.FontSize, 3);
        AssertRgba(Colors.Black, 0.54, first.Foreground);
        Assert.Equal(new Thickness(-4, -4, 8, -4), first.Margin);
        Assert.Equal(new Thickness(8, -4, -4, -4), last.Margin);

        // 20 + 8 + 40 + 8 + 20 - 4 - 4, inside 12 and 12: the first icon at x + 8, and the row still 32 high.
        AssertBox(4, 4, 112, 32, Bounds(chip, scene.Window));
        AssertBox(12, 10, 20, 20, Bounds(first, scene.Window));
        AssertBox(88, 10, 20, 20, Bounds(last, scene.Window));
    });

    [AvaloniaFact]
    public void B12_An_Only_Icon_Has_The_Last_Childs_Margins() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "star" };
        var chip = Chip(icon);
        using var scene = Themed(chip);

        // Measured: 48 wide, with the icon at + 20.
        Assert.Equal(new Thickness(8, -4, -4, -4), icon.Margin);
        AssertBox(4, 4, 48, 32, Bounds(chip, scene.Window));
        AssertBox(24, 10, 20, 20, Bounds(icon, scene.Window));
    });

    [AvaloniaFact]
    public void B12_An_Icon_Follows_The_Chips_Font_Size() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "pin" };
        var chip = Chip(icon, Box());
        chip.FontSize = 28;
        using var scene = Themed(chip);

        Assert.Equal(40, icon.FontSize, 3);
    });

    [AvaloniaFact]
    public void B12_An_Icon_In_A_Coloured_Chip_Is_The_Shade() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "heart" };
        var chip = Chip(icon, Box());
        chip.Color = "danger";
        using var scene = Themed(chip);

        AssertRgba(DangerShade, 1, icon.Foreground);
    });

    [AvaloniaFact]
    public void B12_A_Middle_Icon_Has_No_Margins_And_A_Nested_Icon_Gets_None_Of_It() => InstalledTheme.Run(_ =>
    {
        var middle = new AvonicIcon { IconName = "pin" };
        var nested = new AvonicIcon { IconName = "pin" };
        var chip = Chip(Box(), middle, new StackPanel { Children = { nested } });
        using var scene = Themed(chip);

        Assert.Equal(20, middle.FontSize, 3);
        Assert.Equal(default, middle.Margin);

        // It inherits the chip's 14 and 0.87, measured.
        Assert.Equal(14, nested.FontSize, 3);
        Assert.Equal(default, nested.Margin);
        AssertRgba(Colors.Black, 0.87, nested.Foreground);
    });

    [AvaloniaFact]
    public void B12_First_And_Last_Follow_The_Children_As_They_Change() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "pin" };
        var chip = Chip(icon, Box());
        using var scene = Themed(chip);

        Assert.Equal(new Thickness(-4, -4, 8, -4), icon.Margin);

        chip.Items.Insert(0, Box());
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(default, icon.Margin);

        chip.Items.RemoveAt(2);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(8, -4, -4, -4), icon.Margin);
    });

    [AvaloniaFact]
    public void B12_Icons_From_An_Item_Template_Are_Slotted_Too() => InstalledTheme.Run(_ =>
    {
        var names = new ObservableCollection<string> { "pin", "star" };
        var chip = new AvonicChip
        {
            ItemsSource = names,
            ItemTemplate = new FuncDataTemplate<string>((name, _) => new AvonicIcon { IconName = name }),
        };
        using var scene = Themed(chip);

        AvonicIcon Icon(int index) => Assert.IsType<AvonicIcon>(Assert.IsAssignableFrom<ContentPresenter>(chip.ContainerFromIndex(index)).Child);

        Assert.Equal(20, Icon(0).FontSize, 3);
        AssertRgba(Colors.Black, 0.54, Icon(0).Foreground);
        Assert.Equal(new Thickness(-4, -4, 8, -4), Icon(0).Margin);
        Assert.Equal(new Thickness(8, -4, -4, -4), Icon(1).Margin);

        // The first moves to the middle, and the new one is first, as soon as it's added.
        var pin = Icon(0);
        names.Insert(0, "heart");
        Dispatcher.UIThread.RunJobs();
        Assert.Same(pin, Icon(1));
        Assert.Equal(default, pin.Margin);
        Assert.Equal(new Thickness(-4, -4, 8, -4), Icon(0).Margin);
        Assert.Equal(new Thickness(8, -4, -4, -4), Icon(2).Margin);
    });

    [AvaloniaFact]
    public void B12_RTL_Mirrors_The_Icons() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicIcon { IconName = "pin" };
        var last = new AvonicIcon { IconName = "close-circle" };
        var chip = Chip(first, Box(), last);
        var holder = new StackPanel { FlowDirection = FlowDirection.RightToLeft, Children = { chip } };
        using var scene = Themed(holder);

        // From the right of 390: the chip ends at 386, and the first icon is 8 in from there.
        AssertBox(274, 4, 112, 32, Bounds(chip, scene.Window));
        AssertBox(358, 10, 20, 20, Bounds(first, scene.Window));
        AssertBox(282, 10, 20, 20, Bounds(last, scene.Window));
    });

    // ── Behaviour 13: long content ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_A_Long_Label_Wraps_At_The_Containers_Width_And_An_Icon_Shrinks() => InstalledTheme.Run(_ =>
    {
        const string Long = "A long label that has to wrap because it does not fit on one line of a phone, not even close";
        var label = new AvonicLabel { Content = Long };
        var chip = Chip(label);
        var icon = new AvonicIcon { IconName = "pin" };
        var withIcon = Chip(icon, new AvonicLabel { Content = Long });
        using var scene = Themed(chip, withIcon);

        Assert.Equal(382, chip.Bounds.Width, 0.5);
        Assert.True(chip.Bounds.Height > 40, $"{chip.Bounds.Height} is one line");
        Assert.True(label.Bounds.Width <= 358.5, $"{label.Bounds.Width} overflows");

        // contain: strict leaves the icon no minimum, so it shares the shrinking. Measured 14.03.
        Assert.Equal(382, withIcon.Bounds.Width, 0.5);
        Assert.InRange(icon.Bounds.Width, 1, 19.5);
    });

    [AvaloniaFact]
    public void B13_A_Long_String_Wraps_Too() => InstalledTheme.Run(_ =>
    {
        var chip = Chip("A long piece of text that has to wrap because it does not fit on one line of a phone, not even close");
        using var scene = Themed(chip);

        Assert.Equal(382, chip.Bounds.Width, 0.5);
        Assert.True(chip.Bounds.Height > 40, $"{chip.Bounds.Height} is one line");
    });

    [AvaloniaFact]
    public void B13_On_The_Stub_Too_Content_Shrinks_To_The_Space()
    {
        var text = new TextBlock
        {
            Text = "A long piece of text that has to wrap because it does not fit on one line of a phone, not even close",
            TextWrapping = TextWrapping.Wrap,
        };
        var chip = Bare(text);
        using var scene = new Scene(chip);

        Assert.Equal(390, chip.Bounds.Width, 0.5);
        Assert.True(text.Bounds.Height > text.FontSize * 2, $"{text.Bounds.Height} is one line");
    }

    // ── Behaviour 14: keys ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_Arrow_Keys_Do_Not_Move_Focus_Between_What_Is_Inside()
    {
        var a = new Button { Content = "a" };
        var b = new Button { Content = "b" };
        var chip = Chip(a, b);
        using var scene = new Scene(chip);

        a.Focus();
        Dispatcher.UIThread.RunJobs();
        foreach (var key in new[] { PhysicalKey.ArrowRight, PhysicalKey.ArrowDown })
        {
            PressKey(scene, key);
            Assert.True(a.IsFocused, $"{key} moved focus");
        }
    }

    [AvaloniaFact]
    public void B14_Enter_And_Space_Do_Not_Activate()
    {
        var chip = Bare();
        chip.Focusable = true;
        using var scene = new Scene(chip);
        using var clock = new TestClock();

        chip.Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        scene.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        clock.At(200);
        Assert.False(Has(chip, Activatable.ActivatedPseudoClass));
        Assert.Empty(Ripple(chip).Ripples);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
    }
}
