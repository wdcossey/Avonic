using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-content in Chromium, at 390 px wide. See docs/components/ion-content/PLAN.md.
/// Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub.
/// </summary>
public class AvonicContentTests
{
    private static AvonicContent Tall(double height = 1000) => new() { Content = new Border { Height = height } };

    private static Control InBox(Control content, double height = 300) =>
        new Panel { Height = height, VerticalAlignment = VerticalAlignment.Top, Children = { content } };

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static ScrollViewer Scroll(AvonicContent content) => Part<ScrollViewer>(content, "PART_Scroll");
    private static Control Background(AvonicContent content) => Part<Control>(content, "PART_Background");
    private static ContentPresenter Container(AvonicContent content) => Part<ContentPresenter>(content, "PART_Container");
    private static ContentPresenter Fixed(AvonicContent content) => Part<ContentPresenter>(content, "PART_Fixed");

    /// <summary>A control's box in the window, through any mirroring.</summary>
    private static Rect Box(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>Hit tests read the last rendered scene.</summary>
    private static void Render()
    {
        Settle();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Settle();
    }

    private static IInputElement? Hit(Scene scene, Point point)
    {
        Render();
        return scene.Window.InputHitTest(point);
    }

    private static void SetOffset(AvonicContent content, double y, double x = 0)
    {
        Scroll(content).Offset = new Vector(x, y);
        Settle();
    }

    private static void Wheel(Scene scene, Point point, double dy, double dx = 0)
    {
        Render();
        // One notch is 50 px in Avalonia. A positive scroll down is a negative delta.
        scene.Window.MouseWheel(point, new Vector(-dx / 50, -dy / 50), RawInputModifiers.None);
        Settle();
    }

    /// <summary>A finger pressing, moving up by <paramref name="distance"/> in steps, and optionally lifting.</summary>
    private static void Drag(Scene scene, Point from, double dy, double dx = 0)
    {
        Render();
        var pointer = scene.Window.TouchBegin(from, RawInputModifiers.None);
        const int steps = 10;
        for (var i = 1; i <= steps; i++)
        {
            scene.Window.TouchMove(pointer, new Point(from.X - dx * i / steps, from.Y - dy * i / steps), RawInputModifiers.None);
            Settle();
        }

        scene.Window.TouchEnd(pointer, new Point(from.X - dx, from.Y - dy), RawInputModifiers.None);
        Settle();
    }

    private static Color BrushColor(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static unsafe Color PixelAt(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var pixel = (byte*)buffer.Address + y * buffer.RowBytes + x * 4;
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? Color.FromArgb(pixel[3], pixel[0], pixel[1], pixel[2])
            : Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }

    /// <summary>Records every scroll event at the clock's time.</summary>
    private sealed class Recorder
    {
        public List<(string Name, double At)> Events { get; } = [];
        public List<ScrollDetailEventArgs> Details { get; } = [];
        public List<ScrollBaseDetailEventArgs> Bases { get; } = [];

        public Recorder(AvonicContent content, TestClock clock)
        {
            content.IonScrollStart += (_, e) => { Events.Add(("start", clock.Now.TotalMilliseconds)); Bases.Add(e); };
            content.IonScroll += (_, e) => { Events.Add(("scroll", clock.Now.TotalMilliseconds)); Details.Add(e); };
            content.IonScrollEnd += (_, e) => { Events.Add(("end", clock.Now.TotalMilliseconds)); Bases.Add(e); };
        }
    }

    // ── 1. Size ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Fills_What_It_Is_Given_However_Small_Its_Content()
    {
        // Measured c3: 150 of a 200 high column with 50 above it. An alignment doesn't shrink it: width and height are 100%.
        var content = new AvonicContent { Content = "x", VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
        using var scene = new Scene(new Grid { Height = 200, VerticalAlignment = VerticalAlignment.Top, RowDefinitions = new RowDefinitions("50,*"), Children = { content } });
        Grid.SetRow(content, 1);
        Settle();

        Assert.Equal(new Size(390, 150), content.Bounds.Size);
    }

    [AvaloniaFact]
    public void B1_With_No_Height_To_Fill_It_Is_Zero_High()
    {
        // Measured c2: contain: size gives it 390 x 0, in a parent with no height.
        var content = Tall();
        var after = new Border { Height = 10 };
        using var scene = new Scene(new StackPanel { Children = { content, after } });

        Assert.Equal(new Size(390, 0), content.Bounds.Size);
        Assert.Equal(0, after.Bounds.Y);
    }

    [AvaloniaFact]
    public void B1_With_No_Width_To_Fill_It_Is_Zero_Wide()
    {
        var content = Tall();
        using var scene = new Scene(new StackPanel { Orientation = Orientation.Horizontal, Children = { content } });

        Assert.Equal(0, content.Bounds.Width);
    }

    [AvaloniaFact]
    public void B1_An_Explicit_Height_Counts()
    {
        var content = new AvonicContent { Height = 100, Content = new Border { Height = 400 } };
        using var scene = new Scene(new StackPanel { Children = { content } });

        Assert.Equal(100, content.Bounds.Height);
        Assert.Equal(100, Scroll(content).Viewport.Height);
        Assert.Equal(400, Scroll(content).Extent.Height);
    }

    [AvaloniaFact]
    public void B2_The_Parts_Fill_The_Host_With_No_Scroll_Bars()
    {
        var content = Tall();
        using var scene = new Scene(InBox(content));

        Assert.Equal(new Size(390, 300), Background(content).Bounds.Size);
        Assert.Equal(new Size(390, 300), Scroll(content).Bounds.Size);
        // Measured: clientWidth is 390 with a 1000 high scrollHeight. Nothing takes room from the content.
        Assert.Equal(new Size(390, 300), Scroll(content).Viewport);
        Assert.Equal(new Size(390, 1000), Scroll(content).Extent);
        Assert.Empty(scene.Window.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ScrollBar>());
    }

    // ── 3–6. The look ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Defaults_Are_White_With_Black_Text_And_The_Global_Font() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "x" };
        var content = new AvonicContent { Content = text };
        using var scene = new Scene(InBox(content));

        Assert.Equal(Colors.White, BrushColor(((Border)Background(content)).Background));
        Assert.Null(Scroll(content).Background);
        Assert.Equal(Colors.Black, BrushColor(text.Foreground));
        Assert.Contains("Roboto", content.FontFamily.ToString());
    });

    [AvaloniaFact]
    public void B3_The_Global_Variables_Are_The_Fallback() => InstalledTheme.Run(_ =>
    {
        // --background: var(--ion-background-color, #fff), and --color: var(--ion-text-color, #000)
        var text = new TextBlock { Text = "x" };
        var content = new AvonicContent { Content = text };
        content.Resources["Avonic.BackgroundColorBrush"] = new SolidColorBrush(Colors.Navy);
        content.Resources["Avonic.TextColorBrush"] = new SolidColorBrush(Colors.Orange);
        using var scene = new Scene(InBox(content));

        Assert.Equal(Colors.Navy, BrushColor(((Border)Background(content)).Background));
        Assert.Equal(Colors.Orange, BrushColor(text.Foreground));
    });

    [AvaloniaFact]
    public void B3_The_Tokens_Beat_The_Global_Variables() => InstalledTheme.Run(_ =>
    {
        // Measured v: rgb(18, 52, 86) and rgb(254, 220, 186)
        var text = new TextBlock { Text = "x" };
        var content = new AvonicContent { Content = text };
        content.Resources["Avonic.Content.Background"] = new SolidColorBrush(Color.Parse("#123456"));
        content.Resources["Avonic.Content.Color"] = new SolidColorBrush(Color.Parse("#FEDCBA"));
        using var scene = new Scene(InBox(content));

        Assert.Equal(Color.Parse("#123456"), BrushColor(((Border)Background(content)).Background));
        Assert.Equal(Color.Parse("#FEDCBA"), BrushColor(text.Foreground));
    });

    [AvaloniaFact]
    public void B4_Color_Paints_The_Scroll_Part_And_Leaves_The_Background_Part() => InstalledTheme.Run(_ =>
    {
        // Measured p: the scroll part is rgb(0, 84, 233) with white text, and the background stays white.
        var text = new TextBlock { Text = "x" };
        var content = new AvonicContent { Color = "primary", Content = text };
        using var scene = new Scene(InBox(content));

        Assert.Equal(Color.Parse("#0054E9"), BrushColor(Scroll(content).Background));
        Assert.Equal(Colors.White, BrushColor(text.Foreground));
        Assert.Equal(Colors.White, BrushColor(((Border)Background(content)).Background));
    });

    [AvaloniaFact]
    public void B4_Color_Beats_The_Color_Token_And_Keeps_The_Background_Token() => InstalledTheme.Run(_ =>
    {
        // Measured vp
        var text = new TextBlock { Text = "x" };
        var content = new AvonicContent { Color = "primary", Content = text };
        content.Resources["Avonic.Content.Background"] = new SolidColorBrush(Color.Parse("#123456"));
        content.Resources["Avonic.Content.Color"] = new SolidColorBrush(Color.Parse("#FEDCBA"));
        using var scene = new Scene(InBox(content));

        Assert.Equal(Colors.White, BrushColor(text.Foreground));
        Assert.Equal(Color.Parse("#0054E9"), BrushColor(Scroll(content).Background));
        Assert.Equal(Color.Parse("#123456"), BrushColor(((Border)Background(content)).Background));
    });

    [AvaloniaFact]
    public void B5_An_Unknown_Color_Has_No_Background_And_The_Hosts_Text_Color() => InstalledTheme.Run(_ =>
    {
        // Measured bad: transparent, and the text colour the host inherits, not --color.
        var text = new TextBlock { Text = "x" };
        var content = new AvonicContent { Color = "badcolor", Content = text };
        content.Resources["Avonic.Content.Color"] = new SolidColorBrush(Colors.Red);
        var host = InBox(content);
        TextElement.SetForeground(host, new SolidColorBrush(Colors.Green));
        using var scene = new Scene(host);

        Assert.Null(Scroll(content).Background);
        Assert.Equal(Colors.Green, BrushColor(text.Foreground));
    });

    [AvaloniaFact]
    public void B5_An_Unknown_Color_Shows_A_Coloured_Ancestors() => InstalledTheme.Run(_ =>
    {
        // Measured in: rgb(197, 0, 15) with white text, from the ancestor's --ion-color-base.
        var text = new TextBlock { Text = "x" };
        var content = new AvonicContent { Color = "badcolor", Content = text };
        var ancestor = new AvonicContent { Color = "danger", Content = InBox(content) };
        using var scene = new Scene(InBox(ancestor, 400));

        Assert.Equal(Color.Parse("#C5000F"), BrushColor(Scroll(content).Background));
        Assert.Equal(Colors.White, BrushColor(text.Foreground));
    });

    [AvaloniaFact]
    public void B5_An_Unknown_Color_Still_Takes_Input() => InstalledTheme.Run(_ =>
    {
        var content = new AvonicContent { Color = "badcolor", Content = "x" };
        using var scene = new Scene(InBox(content));

        var hit = Hit(scene, new Point(200, 200));
        Assert.Same(Container(content), hit);
    });

    [AvaloniaFact]
    public void B6_The_Fixed_Slot_Keeps_What_The_Host_Inherits() => InstalledTheme.Run(_ =>
    {
        // Measured vf, pf and gf: --color and Color are on the scroll part, and the fixed slot is outside it.
        var text = new TextBlock { Text = "fixed" };
        var content = new AvonicContent { Color = "primary", FixedContent = text };
        content.Resources["Avonic.Content.Color"] = new SolidColorBrush(Colors.Red);
        var host = InBox(content);
        TextElement.SetForeground(host, new SolidColorBrush(Colors.Green));
        using var scene = new Scene(host);

        Assert.Equal(Colors.Green, BrushColor(text.Foreground));
    });

    [AvaloniaFact]
    public void B6_The_Font_Reaches_The_Fixed_Slot_Too() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "fixed" };
        using var scene = new Scene(InBox(new AvonicContent { FixedContent = text }));

        Assert.Contains("Roboto", text.FontFamily.ToString());
    });

    // ── 7–9. Padding, wrapping and the flow of the content ──────────────────────────────────

    private static AvonicContent Padded(Control child, string? start = "10", string? top = "20", string? end = "30", string? bottom = "40")
    {
        var content = new AvonicContent { Content = child };
        content.Resources["Avonic.Content.PaddingStart"] = double.Parse(start!);
        content.Resources["Avonic.Content.PaddingTop"] = double.Parse(top!);
        content.Resources["Avonic.Content.PaddingEnd"] = double.Parse(end!);
        content.Resources["Avonic.Content.PaddingBottom"] = double.Parse(bottom!);
        return content;
    }

    [AvaloniaFact]
    public void B7_Padding_Moves_And_Narrows_The_Content() => InstalledTheme.Run(_ =>
    {
        // Measured pac: 10,20 350 wide.
        var child = new Border { Height = 100 };
        var content = Padded(child);
        using var scene = new Scene(InBox(content));

        Assert.Equal(new Rect(10, 20, 350, 100), Box(child, scene.Window));
        // Measured pa: it fits, so nothing to scroll.
        Assert.Equal(300, Scroll(content).Extent.Height);
    });

    [AvaloniaFact]
    public void B7_Padding_Scrolls_With_The_Content() => InstalledTheme.Run(_ =>
    {
        // Measured pb: scrollHeight 1060, which is 1000 and both paddings.
        var child = new Border { Height = 1000 };
        var content = Padded(child);
        using var scene = new Scene(InBox(content));

        Assert.Equal(1060, Scroll(content).Extent.Height);
        SetOffset(content, 20);
        Assert.Equal(0, Box(child, scene.Window).Y);
    });

    [AvaloniaFact]
    public void B7_The_Keyboard_Offset_Adds_To_The_Bottom() => InstalledTheme.Run(_ =>
    {
        // Measured kb: scrollHeight 1050, padding-bottom 50.
        var content = Tall();
        content.Resources["Avonic.Content.KeyboardOffset"] = 50d;
        using var scene = new Scene(InBox(content));

        Assert.Equal(1050, Scroll(content).Extent.Height);
    });

    [AvaloniaFact]
    public void B7_The_Keyboard_Offset_And_Bottom_Padding_Add_Up() => InstalledTheme.Run(_ =>
    {
        var child = new Border { Height = 1000 };
        var content = Padded(child, "0", "0", "0", "40");
        content.Resources["Avonic.Content.KeyboardOffset"] = 50d;
        using var scene = new Scene(InBox(content));

        Assert.Equal(1090, Scroll(content).Extent.Height);
    });

    [AvaloniaFact]
    public void B7_Rtl_Mirrors_The_Padding() => InstalledTheme.Run(_ =>
    {
        // Measured pad: 0 0 10 0 with --padding-start: 10, in dir="rtl"
        var child = new Border { Height = 100 };
        var content = Padded(child, "10", "0", "0", "0");
        content.FlowDirection = FlowDirection.RightToLeft;
        using var scene = new Scene(InBox(content));

        Assert.Equal(new Rect(0, 0, 380, 100), Box(child, scene.Window));
    });

    [AvaloniaFact]
    public void B7_A_Padding_On_The_Control_Beats_The_Tokens() => InstalledTheme.Run(_ =>
    {
        var child = new Border { Height = 100 };
        var content = Padded(child);
        content.Padding = new Thickness(5);
        using var scene = new Scene(InBox(content));

        Assert.Equal(new Rect(5, 5, 380, 100), Box(child, scene.Window));
    });

    [AvaloniaFact]
    public void B7_The_Keyboard_Offset_Survives_A_Padding_On_The_Control() => InstalledTheme.Run(_ =>
    {
        // calc(var(--padding-bottom) + var(--keyboard-offset)): .ion-padding only sets the first.
        var content = Tall();
        content.Padding = new Thickness(16);
        content.Resources["Avonic.Content.KeyboardOffset"] = 50d;
        using var scene = new Scene(InBox(content));

        Assert.Equal(new Thickness(16, 16, 16, 66), Container(content).Padding);
        Assert.Equal(1082, Scroll(content).Extent.Height);
    });

    [AvaloniaFact]
    public void B7_The_Keyboard_Offset_Follows_A_Change()
    {
        var content = Tall();
        using var scene = new Scene(InBox(content));

        content.KeyboardOffset = 50;
        Settle();

        Assert.Equal(1050, Scroll(content).Extent.Height);
    }

    [AvaloniaFact]
    public void B8_Long_Text_Wraps_To_The_Width()
    {
        // Measured fitc: 390 wide, three lines.
        var text = new TextBlock { Text = "A very long piece of text that certainly has to wrap around because it does not fit into a phone-sized viewport at all" };
        var content = new AvonicContent { Content = text };
        using var scene = new Scene(InBox(content));

        Assert.Equal(390, text.Bounds.Width);
        Assert.True(text.Bounds.Height > 30, $"{text.Bounds.Height}");
        Assert.Equal(390, Scroll(content).Extent.Width);
    }

    [AvaloniaFact]
    public void B8_Text_As_A_String_Wraps_Too()
    {
        var content = new AvonicContent { Content = "A very long piece of text that certainly has to wrap around because it does not fit into a phone-sized viewport at all" };
        using var scene = new Scene(InBox(content));

        Assert.True(Container(content).Child!.Bounds.Height > 30);
    }

    [AvaloniaFact]
    public void B8_An_App_Can_Turn_Wrapping_Off()
    {
        var text = new TextBlock { TextWrapping = TextWrapping.NoWrap, Text = "A very long piece of text that certainly has to wrap around because it does not fit into a phone-sized viewport at all" };
        using var scene = new Scene(InBox(new AvonicContent { Content = text, ScrollX = true }));

        Assert.True(text.Bounds.Width > 390);
    }

    [AvaloniaFact]
    public void B8_A_Text_Box_Stays_On_One_Line() => InstalledTheme.Run(_ =>
    {
        // TextWrapping inherits, and an <input> doesn't wrap. A text box asked to wrap still does.
        var single = new TextBox();
        var multi = new TextBox { TextWrapping = TextWrapping.Wrap };
        var text = new TextBlock();
        var slotted = new TextBox();
        var content = new AvonicContent
        {
            Content = new StackPanel { Children = { new StackPanel { Children = { single } }, multi, text } },
            FixedContent = slotted,
        };
        using var scene = new Scene(InBox(content));

        Assert.Equal(TextWrapping.NoWrap, single.TextWrapping);
        Assert.Equal(TextWrapping.NoWrap, slotted.TextWrapping);
        Assert.Equal(TextWrapping.Wrap, multi.TextWrapping);
        Assert.Equal(TextWrapping.Wrap, text.TextWrapping);
    });

    [AvaloniaFact]
    public void B9_A_Child_Is_As_High_As_Its_Content()
    {
        // Measured blkc: 18 high in a 300 high content, where Avalonia would stretch it.
        var child = new Border { Child = new Border { Height = 18 } };
        var content = new AvonicContent { Content = child };
        using var scene = new Scene(InBox(content));

        Assert.Equal(18, child.Bounds.Height);
        Assert.Equal(0, Box(child, scene.Window).Y);
    }

    [AvaloniaFact]
    public void B9_A_Replaced_Child_Gets_The_Same()
    {
        var content = new AvonicContent { Content = new Border { Child = new Border { Height = 18 } } };
        using var scene = new Scene(InBox(content));

        var first = (Border)content.Content!;
        var second = new Border { Child = new Border { Height = 18 } };
        content.Content = second;
        Settle();

        Assert.Equal(18, second.Bounds.Height);
        // The old one is free of it.
        Assert.Equal(VerticalAlignment.Stretch, first.VerticalAlignment);
    }

    [AvaloniaFact]
    public void B9_An_Apps_Alignment_Wins()
    {
        var stretched = new Border { VerticalAlignment = VerticalAlignment.Stretch };
        using var scene = new Scene(InBox(new AvonicContent { Content = stretched }));
        Assert.Equal(300, stretched.Bounds.Height);
    }

    [AvaloniaFact]
    public void B9_An_Apps_Centre_Is_In_The_Viewport()
    {
        var centred = new Border { Height = 100, VerticalAlignment = VerticalAlignment.Center };
        using var scene = new Scene(InBox(new AvonicContent { Content = centred }));
        Assert.Equal(100, Box(centred, scene.Window).Y);
    }

    // ── 10–13. What scrolls, and how ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_Vertical_Is_On_And_Horizontal_Off_By_Default()
    {
        // Measured y: wheel 30,60 moves 0,60, overflow hidden/auto.
        var content = new AvonicContent { Content = new Border { Width = 800, Height = 1000 } };
        using var scene = new Scene(InBox(content));

        Wheel(scene, new Point(100, 100), 60, 30);

        Assert.Equal(new Vector(0, 60), Scroll(content).Offset);
    }

    [AvaloniaFact]
    public void B10_ScrollY_False_Stops_The_Wheel()
    {
        // Measured nn
        var content = new AvonicContent { ScrollY = false, Content = new Border { Width = 800, Height = 1000 } };
        using var scene = new Scene(InBox(content));

        Wheel(scene, new Point(100, 100), 60, 30);

        Assert.Equal(new Vector(0, 0), Scroll(content).Offset);
    }

    [AvaloniaFact]
    public void B10_ScrollX_True_Lets_The_Wheel_Move_Both()
    {
        // Measured xx
        var content = new AvonicContent { ScrollX = true, Content = new Border { Width = 800, Height = 1000 } };
        using var scene = new Scene(InBox(content));

        Wheel(scene, new Point(100, 100), 60, 30);

        Assert.Equal(new Vector(30, 60), Scroll(content).Offset);
        Assert.Equal(new Size(800, 1000), Scroll(content).Extent);
    }

    [AvaloniaFact]
    public void B10_ScrollX_Alone_Moves_Only_X()
    {
        // Measured xo
        var content = new AvonicContent { ScrollX = true, ScrollY = false, Content = new Border { Width = 800, Height = 1000 } };
        using var scene = new Scene(InBox(content));

        Wheel(scene, new Point(100, 100), 60, 30);

        Assert.Equal(new Vector(30, 0), Scroll(content).Offset);
    }

    [AvaloniaFact]
    public void B10_ScrollY_False_Is_Still_A_Scroller_To_A_Script()
    {
        // Measured scrollY false: 100. overflow: hidden.
        var content = new AvonicContent { ScrollY = false, Content = new Border { Height = 1000 } };
        using var scene = new Scene(InBox(content));

        content.ScrollToPoint(null, 100);

        Assert.Equal(100, Scroll(content).Offset.Y);
        Assert.Equal(1000, Scroll(content).Extent.Height);
    }

    [AvaloniaFact]
    public void B10_Turning_ScrollY_Off_Keeps_The_Position_And_Raises_Nothing()
    {
        // Locking a page while a sheet is open.
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        using var scene = new Scene(InBox(content));
        SetOffset(content, 500);
        clock.At(1000);
        var log = new Recorder(content, clock);

        content.ScrollY = false;
        Settle();
        Wheel(scene, new Point(100, 100), 60);
        Assert.Equal(500, Scroll(content).Offset.Y);

        content.ScrollY = true;
        Settle();
        Assert.Equal(500, Scroll(content).Offset.Y);
        clock.At(2000);
        Assert.Empty(log.Events);

        Wheel(scene, new Point(100, 100), 60);
        Assert.Equal(560, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B10_Page_Keys_Scroll_With_ScrollY_On_And_Not_Off()
    {
        var target = Focusable("target");
        var content = new AvonicContent { Content = new StackPanel { Children = { target, new Border { Height = 1000 } } } };
        var keys = 0;
        var box = InBox(content);
        box.AddHandler(InputElement.KeyDownEvent, (object? _, KeyEventArgs _) => keys++);
        using var scene = new Scene(box);
        target.Focus();
        Settle();

        scene.Window.KeyPressQwerty(PhysicalKey.PageDown, RawInputModifiers.None);
        Settle();
        Assert.Equal(300, Scroll(content).Offset.Y);
        Assert.Equal(0, keys);

        content.ScrollY = false;
        Settle();
        scene.Window.KeyPressQwerty(PhysicalKey.PageDown, RawInputModifiers.None);
        Settle();
        Assert.Equal(300, Scroll(content).Offset.Y);
        // It goes on to whatever's outside.
        Assert.Equal(1, keys);
    }

    [AvaloniaFact]
    public void B10_Other_Keys_Are_Left_Alone_With_ScrollY_Off()
    {
        var target = Focusable("target");
        var content = new AvonicContent { ScrollY = false, Content = target };
        var keys = 0;
        var box = InBox(content);
        box.AddHandler(InputElement.KeyDownEvent, (object? _, KeyEventArgs e) => keys++, Avalonia.Interactivity.RoutingStrategies.Bubble);
        using var scene = new Scene(box);
        target.Focus();
        Settle();

        scene.Window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);
        Settle();

        Assert.Equal(1, keys);
    }

    [AvaloniaFact]
    public void B10_A_Handled_Wheel_Stays_Handled_With_ScrollY_Off()
    {
        // A child that took the wheel keeps it from an outer scroller.
        var child = new Border { Height = 1000, Background = Brushes.Gray };
        child.AddHandler(InputElement.PointerWheelChangedEvent, (object? _, PointerWheelEventArgs e) => e.Handled = true);
        var inner = new AvonicContent { ScrollY = false, Content = child };
        var outer = new AvonicContent
        {
            Content = new StackPanel { Children = { new Panel { Height = 100, Children = { inner } }, new Border { Height = 1000 } } },
        };
        using var scene = new Scene(InBox(outer, 500));

        Wheel(scene, new Point(50, 50), 50);

        Assert.Equal(0, Scroll(outer).Offset.Y);
    }

    [AvaloniaFact]
    public void B10_The_Axes_Can_Change_At_Runtime()
    {
        var content = new AvonicContent { Content = new Border { Width = 800, Height = 1000 } };
        using var scene = new Scene(InBox(content));

        content.ScrollY = false;
        content.ScrollX = true;
        Settle();
        Wheel(scene, new Point(100, 100), 60, 30);

        Assert.Equal(new Vector(30, 0), Scroll(content).Offset);
    }

    [AvaloniaFact]
    public void B10_In_Rtl_An_Offset_Counts_From_The_Start_Edge()
    {
        // Deliberate. Measured rtl scroll: CSS reads -100 for the same position.
        var child = new Border { Width = 800, Height = 100, HorizontalAlignment = HorizontalAlignment.Right };
        var content = new AvonicContent { ScrollX = true, FlowDirection = FlowDirection.RightToLeft, Content = child };
        using var scene = new Scene(InBox(content));
        Assert.Equal(390, Box(child, scene.Window).Right);

        content.ScrollToPoint(100, null);
        Settle();

        Assert.Equal(490, Box(child, scene.Window).Right);
    }

    [AvaloniaFact]
    public void B11_At_Its_Limit_It_Does_Not_Pass_The_Wheel_To_An_Outer_Content()
    {
        // Measured: overscroll-behavior: contain. The outer one stays at 0 with the inner at its end.
        var inner = new AvonicContent { Content = new Border { Height = 400 } };
        var outer = new AvonicContent
        {
            Content = new StackPanel
            {
                Children =
                {
                    new Panel { Height = 200, Children = { inner } },
                    new Border { Height = 1000 },
                },
            },
        };
        using var scene = new Scene(InBox(outer, 500));

        Wheel(scene, new Point(50, 50), 100);
        Assert.Equal(100, Scroll(inner).Offset.Y);

        Wheel(scene, new Point(50, 50), 400);
        Assert.Equal(200, Scroll(inner).Offset.Y);
        Assert.Equal(0, Scroll(outer).Offset.Y);

        Wheel(scene, new Point(50, 50), 400);
        Assert.Equal(0, Scroll(outer).Offset.Y);
    }

    [AvaloniaFact]
    public void B11_Content_That_Fits_Passes_The_Wheel_On()
    {
        // Measured: wheel on a short inner one moves the outer by 50.
        var inner = new AvonicContent { Content = "short" };
        var outer = new AvonicContent
        {
            Content = new StackPanel
            {
                Children =
                {
                    new Panel { Height = 100, Children = { inner } },
                    new Border { Height = 1000 },
                },
            },
        };
        using var scene = new Scene(InBox(outer, 500));

        Wheel(scene, new Point(50, 50), 50);

        Assert.Equal(50, Scroll(outer).Offset.Y);
    }

    [AvaloniaFact]
    public void B11_ScrollY_False_Passes_The_Wheel_On()
    {
        // overflow: hidden, and no overscroll-behavior: not a scroller for the wheel.
        var inner = new AvonicContent { ScrollY = false, Content = new Border { Height = 400 } };
        var outer = new AvonicContent
        {
            Content = new StackPanel
            {
                Children =
                {
                    new Panel { Height = 100, Children = { inner } },
                    new Border { Height = 1000 },
                },
            },
        };
        using var scene = new Scene(InBox(outer, 500));

        Wheel(scene, new Point(50, 50), 50);

        Assert.Equal(50, Scroll(outer).Offset.Y);
    }

    [AvaloniaFact]
    public void B12_A_Touch_Drag_Scrolls()
    {
        var content = Tall();
        Scroll(ForceTemplate(content, out var scene)).IsScrollInertiaEnabled = false;
        using (scene)
        {
            Drag(scene, new Point(100, 250), 150);

            Assert.True(Scroll(content).Offset.Y > 100, $"{Scroll(content).Offset.Y}");
        }
    }

    [AvaloniaFact]
    public void B12_A_Touch_Drag_On_Blank_Content_Scrolls_Too()
    {
        // Nothing in the content is under the finger, as with a short child and an empty area below.
        var content = new AvonicContent
        {
            Content = new StackPanel { Children = { new Border { Height = 10 }, new Border { Height = 1000, IsHitTestVisible = false } } },
        };
        Scroll(ForceTemplate(content, out var scene)).IsScrollInertiaEnabled = false;
        using (scene)
        {
            Drag(scene, new Point(100, 250), 150);

            Assert.True(Scroll(content).Offset.Y > 100, $"{Scroll(content).Offset.Y}");
        }
    }

    [AvaloniaFact]
    public void B12_ScrollY_False_Stops_The_Drag()
    {
        var content = new AvonicContent { ScrollY = false, Content = new Border { Height = 1000 } };
        Scroll(ForceTemplate(content, out var scene)).IsScrollInertiaEnabled = false;
        using (scene)
        {
            Drag(scene, new Point(100, 250), 150);

            Assert.Equal(0, Scroll(content).Offset.Y);
        }
    }

    [AvaloniaFact]
    public void B12_A_Drag_Reaches_ScrollX()
    {
        var content = new AvonicContent { ScrollX = true, Content = new Border { Width = 800, Height = 100 } };
        Scroll(ForceTemplate(content, out var scene)).IsScrollInertiaEnabled = false;
        using (scene)
        {
            Drag(scene, new Point(300, 50), 0, 200);

            Assert.True(Scroll(content).Offset.X > 100, $"{Scroll(content).Offset.X}");
        }
    }

    [AvaloniaFact]
    public void B12_Momentum_Is_On_And_Avalonias()
    {
        var content = Tall();
        using var scene = new Scene(InBox(content));

        var recognizer = Assert.Single(Part<ScrollContentPresenter>(content, "PART_ContentPresenter").GestureRecognizers.OfType<ScrollGestureRecognizer>());

        Assert.True(recognizer.IsScrollInertiaEnabled);
        Scroll(content).IsScrollInertiaEnabled = false;
        Settle();
        Assert.False(recognizer.IsScrollInertiaEnabled);
    }

    [AvaloniaFact]
    public void B12_A_Drag_At_The_Limit_Of_A_Nested_Content_Stays_There()
    {
        // overscroll-behavior: contain, for a finger as well as a wheel.
        var inner = new AvonicContent { Content = new Border { Height = 400 } };
        var outer = new AvonicContent
        {
            Content = new StackPanel
            {
                Children =
                {
                    new Panel { Height = 200, Children = { inner } },
                    new Border { Height = 1000 },
                },
            },
        };
        using var scene = new Scene(InBox(outer, 500));
        Scroll(inner).IsScrollInertiaEnabled = false;
        Scroll(outer).IsScrollInertiaEnabled = false;
        SetOffset(inner, 200);

        // Measured inner at end, drag up 80: 0, 200.
        Drag(scene, new Point(100, 150), 100);

        Assert.Equal(200, Scroll(inner).Offset.Y);
        Assert.Equal(0, Scroll(outer).Offset.Y);

        // Measured inner at top, outer 100, drag down 60: 100, 0.
        SetOffset(outer, 100);
        SetOffset(inner, 0);
        Drag(scene, new Point(100, 50), -60);

        Assert.Equal(0, Scroll(inner).Offset.Y);
        Assert.Equal(100, Scroll(outer).Offset.Y);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void B12_A_Drag_On_A_Nested_Content_That_Cannot_Scroll_Moves_The_Outer_One(bool scrollY)
    {
        // Measured short inner and scroll-y false inner: 45 of a 60 drag, in the outer one.
        var inner = new AvonicContent { ScrollY = scrollY, Content = scrollY ? "short" : new Border { Height = 400 } };
        var outer = new AvonicContent
        {
            Content = new StackPanel
            {
                Children =
                {
                    new Panel { Height = 200, Children = { inner } },
                    new Border { Height = 1000 },
                },
            },
        };
        using var scene = new Scene(InBox(outer, 500));
        Scroll(outer).IsScrollInertiaEnabled = false;

        Drag(scene, new Point(100, 150), 100);

        Assert.True(Scroll(outer).Offset.Y > 50, $"{Scroll(outer).Offset.Y}");
    }

    [AvaloniaFact]
    public void B12_A_Drag_Over_A_Button_Item_Does_Not_Click_It()
    {
        // Measured: activated false in the drag, and no clicks.
        var clicks = 0;
        var item = new AvonicItem { Button = true, Height = 1000, Content = "row" };
        item.Click += (_, _) => clicks++;
        var content = new AvonicContent { Content = item };
        Scroll(ForceTemplate(content, out var scene)).IsScrollInertiaEnabled = false;
        using (scene)
        {
            Drag(scene, new Point(100, 250), 150);

            Assert.True(Scroll(content).Offset.Y > 100, $"{Scroll(content).Offset.Y}");
            Assert.DoesNotContain(":activated", item.Classes);
            Assert.Equal(0, clicks);
        }
    }

    [AvaloniaFact]
    public void B12_A_Tap_On_A_Button_Item_Still_Clicks_It()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "row" };
        item.Click += (_, _) => clicks++;
        ForceTemplate(new AvonicContent { Content = item }, out var scene);
        using (scene)
        {
            Render();
            var pointer = scene.Window.TouchBegin(new Point(100, 8), RawInputModifiers.None);
            Settle();
            scene.Window.TouchEnd(pointer, new Point(100, 8), RawInputModifiers.None);
            Settle();

            Assert.Equal(1, clicks);
        }
    }

    /// <summary>A shown content, whose parts exist, for a test that sets something on them.</summary>
    private static AvonicContent ForceTemplate(AvonicContent content, out Scene scene)
    {
        scene = new Scene(InBox(content));
        return content;
    }

    [AvaloniaFact]
    public void B13_ForceOverscroll_Makes_Short_Content_Scrollable_By_One_Pixel()
    {
        // Measured t: 301 in 300, and scrollTop reaches 1.
        var content = new AvonicContent { ForceOverscroll = true, Content = "short" };
        using var scene = new Scene(InBox(content));

        Assert.Equal(301, Scroll(content).Extent.Height);
        SetOffset(content, 1000);
        Assert.Equal(1, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B13_ForceOverscroll_Leaves_Long_Content_Alone()
    {
        // Measured l: 1000
        var content = new AvonicContent { ForceOverscroll = true, Content = new Border { Height = 1000 } };
        using var scene = new Scene(InBox(content));

        Assert.Equal(1000, Scroll(content).Extent.Height);
    }

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData(false)]
    public void B13_Without_ForceOverscroll_Short_Content_Does_Not_Scroll(bool? value)
    {
        // Measured d and f: null is off in md, because it's only on for ios.
        var content = new AvonicContent { ForceOverscroll = value, Content = "short" };
        using var scene = new Scene(InBox(content));

        Assert.Equal(300, Scroll(content).Extent.Height);
    }

    [AvaloniaFact]
    public void B13_ForceOverscroll_Needs_ScrollY()
    {
        // Measured nx: with no scroll axis, no overscroll class.
        var content = new AvonicContent { ForceOverscroll = true, ScrollY = false, Content = "short" };
        using var scene = new Scene(InBox(content));

        Assert.Equal(300, Scroll(content).Extent.Height);
    }

    [AvaloniaFact]
    public void B13_ForceOverscroll_Is_One_Pixel_Whatever_The_Padding() => InstalledTheme.Run(_ =>
    {
        // Measured pd: 301, with 10 and 20 of padding.
        var content = new AvonicContent { ForceOverscroll = true, Content = "short" };
        content.Resources["Avonic.Content.PaddingTop"] = 10d;
        content.Resources["Avonic.Content.PaddingBottom"] = 20d;
        using var scene = new Scene(InBox(content));

        Assert.Equal(301, Scroll(content).Extent.Height);
    });

    [AvaloniaFact]
    public void B13_ForceOverscroll_Follows_A_Change()
    {
        var content = new AvonicContent { Content = "short" };
        var box = (Panel)InBox(content);
        using var scene = new Scene(box);

        content.ForceOverscroll = true;
        Settle();
        Assert.Equal(301, Scroll(content).Extent.Height);

        box.Height = 200;
        Settle();
        Assert.Equal(201, Scroll(content).Extent.Height);

        content.ForceOverscroll = false;
        Settle();
        Assert.Equal(200, Scroll(content).Extent.Height);
    }

    [AvaloniaFact]
    public void B13_ScrollY_Off_Later_Clears_ForceOverscroll()
    {
        var content = new AvonicContent { ForceOverscroll = true, Content = "short" };
        using var scene = new Scene(InBox(content));

        content.ScrollY = false;
        Settle();

        Assert.Equal(300, Scroll(content).Extent.Height);
    }

    // ── 14–19. Scroll events ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_Without_ScrollEvents_Nothing_Is_Raised()
    {
        // Measured off: []
        using var clock = new TestClock();
        var content = Tall();
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));

        SetOffset(content, 100);
        clock.At(1000);

        Assert.Empty(log.Events);
    }

    [AvaloniaFact]
    public void B14_One_Scroll_Starts_Reports_And_Ends_On_Ionics_Timeline()
    {
        // Measured one: start at 0, scroll in the frame, end at 203, on a 100 ms watchdog with 120 ms of idle.
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));

        SetOffset(content, 100);
        Assert.Equal([("start", 0d)], log.Events);

        clock.At(199);
        Assert.Equal([("start", 0d), ("scroll", 16d)], log.Events);

        clock.At(1000);
        Assert.Equal([("start", 0d), ("scroll", 16d), ("end", 200d)], log.Events);
    }

    [AvaloniaFact]
    public void B14_The_Watchdog_Waits_For_Quiet()
    {
        // Measured gap 90: the second scroll is at 90, so 200 isn't idle yet, and it ends at 300.
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));

        SetOffset(content, 100);
        clock.At(90);
        SetOffset(content, 110);
        clock.At(1000);

        Assert.Equal(
        [
            ("start", 0d), ("scroll", 16d), ("scroll", 106d), ("end", 300d),
        ], log.Events);
    }

    [AvaloniaFact]
    public void B14_Exactly_120_Ms_Of_Idle_Is_Not_Enough()
    {
        // lastScroll < now - 120, which is strict: a scroll at 80 is still live at 200.
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));

        SetOffset(content, 100);
        clock.At(80);
        SetOffset(content, 110);
        clock.At(1000);

        Assert.Equal(300d, log.Events[^1].At);
    }

    [AvaloniaFact]
    public void B14_A_Scroll_After_The_End_Starts_Again()
    {
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));

        SetOffset(content, 100);
        clock.At(500);
        SetOffset(content, 200);
        clock.At(1000);

        Assert.Equal(
        [
            ("start", 0d), ("scroll", 16d), ("end", 200d), ("start", 500d), ("scroll", 516d), ("end", 700d),
        ], log.Events);
    }

    [AvaloniaFact]
    public void B15_Scrolls_In_One_Frame_Are_One_Event_With_The_Latest_Position()
    {
        // Measured same tick twice: one ionScroll, at 310.
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));

        SetOffset(content, 300);
        SetOffset(content, 310);
        clock.At(20);

        Assert.Equal(310, Assert.Single(log.Details).ScrollTop);
    }

    [AvaloniaFact]
    public void B15_No_Change_No_Event()
    {
        // Measured no real change: []
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));

        SetOffset(content, 0);
        clock.At(1000);

        Assert.Empty(log.Events);
    }

    [AvaloniaFact]
    public void B15_Content_Shrinking_Under_The_Offset_Is_A_Scroll()
    {
        // Measured content shrinks: start, scroll at 300, end.
        using var clock = new TestClock();
        var child = new Border { Height = 1000 };
        var content = new AvonicContent { ScrollEvents = true, Content = child };
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));
        SetOffset(content, 500);
        clock.At(1000);
        log.Events.Clear();
        log.Details.Clear();

        child.Height = 600;
        Settle();
        clock.At(1020);

        Assert.Equal("start", log.Events[0].Name);
        Assert.Equal(300, log.Details[0].ScrollTop);
    }

    [AvaloniaFact]
    public void B15_A_Change_Of_Size_Alone_Is_Not_A_Scroll()
    {
        using var clock = new TestClock();
        var child = new Border { Height = 1000 };
        var content = new AvonicContent { ScrollEvents = true, Content = child };
        var log = new Recorder(content, clock);
        var box = (Panel)InBox(content);
        using var scene = new Scene(box);

        child.Height = 2000;
        box.Height = 200;
        Settle();
        clock.At(1000);

        Assert.Empty(log.Events);
    }

    [AvaloniaFact]
    public void B16_The_First_Detail_Is_Ionics()
    {
        // Measured one: startY = currentY = scrollTop, deltas and velocities 0, startTime = currentTime, type "scroll".
        using var clock = new TestClock();
        var content = new AvonicContent { ScrollEvents = true, ScrollX = true, Content = new Border { Width = 800, Height = 1000 } };
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));
        clock.At(1000);

        SetOffset(content, 100, 40);
        clock.At(1016);

        var detail = Assert.Single(log.Details);
        Assert.Equal("scroll", detail.Type);
        Assert.True(detail.IsScrolling);
        Assert.Equal(100, detail.ScrollTop);
        Assert.Equal(40, detail.ScrollLeft);
        Assert.Equal(100, detail.StartY);
        Assert.Equal(40, detail.StartX);
        Assert.Equal(100, detail.CurrentY);
        Assert.Equal(40, detail.CurrentX);
        Assert.Equal(0, detail.DeltaY);
        Assert.Equal(0, detail.DeltaX);
        Assert.Equal(0, detail.VelocityY);
        Assert.Equal(0, detail.VelocityX);
        Assert.Equal(1016, detail.StartTime);
        Assert.Equal(1016, detail.CurrentTime);
    }

    [AvaloniaFact]
    public void B16_Deltas_Are_From_The_Start_And_Velocity_Is_Smoothed()
    {
        // velocity = instant * 0.7 + previous * 0.3, with times in ms. Measured steps: 10 px in 31.8 ms gave
        // 0.2201 = 10 / 31.8 * 0.7, then 0.2707 = 10 / 33.9... * 0.7 + 0.2201 * 0.3.
        using var clock = new TestClock();
        var content = new AvonicContent { ScrollEvents = true, ScrollX = true, Content = new Border { Width = 800, Height = 1000 } };
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));
        clock.At(1000);

        SetOffset(content, 110, 10);
        clock.At(1016);
        SetOffset(content, 120, 30);
        clock.At(1048);
        SetOffset(content, 130, 40);
        clock.At(1080);

        var second = log.Details[1];
        Assert.Equal(110, second.StartY);
        Assert.Equal(10, second.StartX);
        Assert.Equal(10, second.DeltaY);
        Assert.Equal(20, second.DeltaX);
        Assert.Equal(1016, second.StartTime);
        Assert.Equal(1032, second.CurrentTime);
        Assert.Equal(10d / 16 * 0.7, second.VelocityY, 10);
        Assert.Equal(20d / 16 * 0.7, second.VelocityX, 10);

        var third = log.Details[2];
        Assert.Equal(20, third.DeltaY);
        Assert.Equal(1064, third.CurrentTime);
        Assert.Equal(10d / 32 * 0.7 + second.VelocityY * 0.3, third.VelocityY, 10);
        Assert.Equal(10d / 32 * 0.7 + second.VelocityX * 0.3, third.VelocityX, 10);
    }

    [AvaloniaFact]
    public void B16_A_Gap_Of_100_Ms_Or_More_Leaves_The_Velocity()
    {
        // timeDelta < 100
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));
        clock.At(1000);

        SetOffset(content, 100);
        clock.At(1016);
        SetOffset(content, 110);
        clock.At(1040);
        var before = log.Details[1].VelocityY;
        Assert.NotEqual(0, before);

        // Still the same scroll, for the watchdog, with its frame at 1156, 124 ms after the last.
        clock.At(1140);
        SetOffset(content, 120);
        clock.At(1160);

        Assert.Equal(before, log.Details[2].VelocityY);
    }

    [AvaloniaFact]
    public void B16_A_New_Scroll_Starts_From_Zero_Velocity_And_New_Start()
    {
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));
        clock.At(1000);

        SetOffset(content, 100);
        clock.At(1016);
        SetOffset(content, 200);
        clock.At(1040);
        Assert.NotEqual(0, log.Details[1].VelocityY);

        clock.At(2000);
        SetOffset(content, 300);
        clock.At(2020);

        var detail = log.Details[2];
        Assert.Equal(300, detail.StartY);
        Assert.Equal(0, detail.DeltaY);
        Assert.Equal(0, detail.VelocityY);
        Assert.Equal(2016, detail.StartTime);
    }

    [AvaloniaFact]
    public void B17_Start_And_End_Say_Whether_It_Is_Scrolling()
    {
        // Measured: { isScrolling: true }, { isScrolling: false }
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));

        SetOffset(content, 100);
        clock.At(1000);

        Assert.True(log.Bases[0].IsScrolling);
        Assert.False(log.Bases[1].IsScrolling);
    }

    [AvaloniaFact]
    public void B17_The_Events_Bubble_From_The_Content()
    {
        // Measured: bubbles, with the content as the target, from a listener on an ancestor.
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var box = InBox(content);
        var seen = new List<string>();
        box.AddHandler(AvonicContent.IonScrollStartEvent, (object? _, ScrollBaseDetailEventArgs e) => { Assert.Same(content, e.Source); seen.Add("start"); });
        box.AddHandler(AvonicContent.IonScrollEvent, (object? _, ScrollDetailEventArgs e) => { Assert.Same(content, e.Source); seen.Add("scroll"); });
        box.AddHandler(AvonicContent.IonScrollEndEvent, (object? _, ScrollBaseDetailEventArgs e) => { Assert.Same(content, e.Source); seen.Add("end"); });
        using var scene = new Scene(box);

        SetOffset(content, 100);
        clock.At(1000);

        Assert.Equal(["start", "scroll", "end"], seen);
    }

    [AvaloniaFact]
    public void B18_Removing_It_While_Scrolling_Ends_The_Scroll_At_Once()
    {
        // Measured removed while scrolling: ionScrollEnd is raised before remove() returns.
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        var box = (Panel)InBox(content);
        using var scene = new Scene(box);

        SetOffset(content, 100);
        clock.At(40);
        box.Children.Clear();

        Assert.Equal(("end", 40d), log.Events[^1]);

        var count = log.Events.Count;
        clock.At(1000);
        Assert.Equal(count, log.Events.Count);
    }

    [AvaloniaFact]
    public void B18_Removing_It_Cancels_A_Waiting_Scroll_Event_And_It_Starts_Again_Later()
    {
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        var box = (Panel)InBox(content);
        using var scene = new Scene(box);

        SetOffset(content, 100);
        box.Children.Clear();
        clock.At(1000);
        Assert.Empty(log.Details);

        box.Children.Add(content);
        Settle();
        SetOffset(content, 200);
        clock.At(1020);
        Assert.Equal(200, Assert.Single(log.Details).ScrollTop);
    }

    [AvaloniaFact]
    public void B18_An_Animation_That_Outlives_It_Raises_Nothing_More()
    {
        // A browser's disconnected element doesn't scroll, so no second cycle starts.
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        var box = (Panel)InBox(content);
        using var scene = new Scene(box);

        content.ScrollToPoint(null, 500, 300);
        clock.At(40);
        box.Children.Clear();
        var count = log.Events.Count;
        Assert.Equal("end", log.Events[^1].Name);

        clock.At(1000);

        Assert.Equal(count, log.Events.Count);
    }

    [AvaloniaFact]
    public void B18_Removing_It_When_Idle_Raises_Nothing()
    {
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        var box = (Panel)InBox(content);
        using var scene = new Scene(box);

        box.Children.Clear();

        Assert.Empty(log.Events);
    }

    [AvaloniaFact]
    public void B19_Turning_ScrollEvents_Off_Stops_New_Ones_But_Finishes_The_Current_One()
    {
        // The listener goes, and the watchdog already running doesn't.
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));

        SetOffset(content, 100);
        clock.At(20);
        content.ScrollEvents = false;
        SetOffset(content, 200);
        clock.At(1000);

        Assert.Equal([("start", 0d), ("scroll", 16d), ("end", 200d)], log.Events);
    }

    // ── 20–25. Methods ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B20_GetScrollElement_Is_The_Scroll_Part()
    {
        // Measured: the element with part="scroll".
        var content = Tall();
        using var scene = new Scene(InBox(content));

        Assert.Same(Scroll(content), content.GetScrollElement());
    }

    [AvaloniaFact]
    public void B20_GetScrollElement_Works_Once_It_Is_In_A_Tree_Before_It_Is_Shown()
    {
        var content = new AvonicContent();
        var window = new Window { Content = content };

        Assert.NotNull(content.GetScrollElement());

        window.Close();
    }

    [AvaloniaFact]
    public void B20_GetScrollElement_Is_Null_With_No_Tree_To_Find_A_Theme_In()
    {
        // Deliberate, and raised in the PR: Ionic's waits for load, so this is the one thing a synchronous method can't do.
        Assert.Null(new AvonicContent().GetScrollElement());
    }

    [AvaloniaFact]
    public void B20_A_Template_With_No_Parts_Is_Safe()
    {
        using var clock = new TestClock();
        var content = new AvonicContent
        {
            Template = new FuncControlTemplate<AvonicContent>((_, _) => new Border()),
            Fullscreen = true,
            ForceOverscroll = true,
            FixedSlotPlacement = ContentFixedSlotPlacement.Before,
            ScrollEvents = true,
        };
        using var scene = new Scene(InBox(content));

        Assert.Null(content.GetScrollElement());
        Assert.True(content.ScrollToPoint(0, 100).IsCompletedSuccessfully);
        Assert.True(content.ScrollToBottom().IsCompletedSuccessfully);
        Assert.True(content.ScrollByPoint(0, 1, 0).IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void B21_With_No_Duration_It_Jumps_And_Is_Done()
    {
        // Measured 0 sync?: Ionic has done it after one await, and here it's synchronous.
        var content = new AvonicContent { ScrollX = true, Content = new Border { Width = 800, Height = 1000 } };
        using var scene = new Scene(InBox(content));

        var task = content.ScrollToPoint(10, 100);

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(new Vector(10, 100), Scroll(content).Offset);
    }

    [AvaloniaFact]
    public void B21_Null_Leaves_An_Axis_Where_It_Is()
    {
        // Measured null keeps
        var content = new AvonicContent { ScrollX = true, Content = new Border { Width = 800, Height = 1000 } };
        using var scene = new Scene(InBox(content));
        SetOffset(content, 40, 20);

        content.ScrollToPoint(null, null);
        Assert.Equal(new Vector(20, 40), Scroll(content).Offset);

        content.ScrollToPoint(50, null);
        Assert.Equal(new Vector(50, 40), Scroll(content).Offset);

        content.ScrollToPoint(null, 60);
        Assert.Equal(new Vector(50, 60), Scroll(content).Offset);
    }

    [AvaloniaFact]
    public void B21_It_Is_Limited_To_The_Range()
    {
        // Measured bottom: past 5000 is 700, -50 is 0.
        var content = Tall();
        using var scene = new Scene(InBox(content));

        content.ScrollToPoint(null, 5000);
        Assert.Equal(700, Scroll(content).Offset.Y);

        content.ScrollToPoint(null, -50);
        Assert.Equal(0, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B21_31_Ms_Jumps_And_32_Ms_Animates()
    {
        // Measured 31 vs 32
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(InBox(content));

        Assert.True(content.ScrollToPoint(null, 100, 31).IsCompletedSuccessfully);
        Assert.Equal(100, Scroll(content).Offset.Y);

        SetOffset(content, 0);
        var task = content.ScrollToPoint(null, 100, 32);
        Assert.False(task.IsCompleted);
        Assert.Equal(0, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B22_An_Animated_Scroll_Waits_A_Frame_Then_Eases_Out_Exactly()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(InBox(content));

        var task = content.ScrollToPoint(null, 500, 300);
        Assert.Equal(0, Scroll(content).Offset.Y);

        // The first frame only sets the start time.
        clock.At(16);
        Assert.Equal(0, Scroll(content).Offset.Y);
        Assert.False(task.IsCompleted);

        foreach (var ms in new[] { 32d, 64, 160, 304 })
        {
            clock.At(ms);
            var t = Math.Min(1, (ms - 16) / 300) - 1;
            Assert.Equal(Math.Floor((Math.Pow(t, 3) + 1) * 500), Scroll(content).Offset.Y);
            Assert.False(task.IsCompleted);
        }

        // 316 is the first frame at or past the duration.
        clock.At(320);
        Assert.Equal(500, Scroll(content).Offset.Y);
        Assert.True(task.IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void B22_It_Eases_From_Where_It_Is_On_Both_Axes()
    {
        using var clock = new TestClock();
        var content = new AvonicContent { ScrollX = true, Content = new Border { Width = 1000, Height = 1000 } };
        using var scene = new Scene(InBox(content));
        SetOffset(content, 100, 200);

        var task = content.ScrollToPoint(100, 400, 100);
        clock.At(16);
        Assert.Equal(new Vector(200, 100), Scroll(content).Offset);

        clock.At(64);
        var eased = Math.Pow(48d / 100 - 1, 3) + 1;
        Assert.Equal(new Vector(Math.Floor(eased * -100 + 200), Math.Floor(eased * 300 + 100)), Scroll(content).Offset);

        clock.At(200);
        Assert.Equal(new Vector(100, 400), Scroll(content).Offset);
        Assert.True(task.IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void B22_A_Fractional_Point_Floors()
    {
        // Measured frac anim: 100.75 gives 100.
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(InBox(content));

        content.ScrollToPoint(null, 100.75, 50);
        clock.At(200);

        Assert.Equal(100, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B22_An_Axis_That_Does_Not_Change_Is_Not_Written()
    {
        // deltaX !== 0: a scroll the user makes across an animation on the other axis stays.
        using var clock = new TestClock();
        var content = new AvonicContent { ScrollX = true, Content = new Border { Width = 800, Height = 1000 } };
        using var scene = new Scene(InBox(content));

        content.ScrollToPoint(null, 100, 100);
        clock.At(32);
        SetOffset(content, Scroll(content).Offset.Y, 70);
        clock.At(48);

        Assert.Equal(70, Scroll(content).Offset.X);
    }

    [AvaloniaFact]
    public void B23_Two_At_Once_Both_Run_And_The_Last_Frame_Wins()
    {
        // Measured concurrent: 500 when the first ends, then 100 when the second does.
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(InBox(content));

        var first = content.ScrollToPoint(null, 500, 200);
        var second = content.ScrollToPoint(null, 100, 400);

        clock.At(224);
        Assert.True(first.IsCompletedSuccessfully);
        Assert.False(second.IsCompleted);

        clock.At(500);
        Assert.Equal(100, Scroll(content).Offset.Y);
        Assert.True(second.IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void B23_A_User_Scroll_Does_Not_Stop_It()
    {
        // Measured user during: 10, then 500.
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(InBox(content));

        var task = content.ScrollToPoint(null, 500, 200);
        clock.At(64);
        SetOffset(content, 10);
        Assert.Equal(10, Scroll(content).Offset.Y);

        clock.At(300);
        Assert.Equal(500, Scroll(content).Offset.Y);
        Assert.True(task.IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void B23_Removing_It_Does_Not_Strand_The_Task()
    {
        // Measured detached: done
        using var clock = new TestClock();
        var content = Tall();
        var box = (Panel)InBox(content);
        using var scene = new Scene(box);

        var task = content.ScrollToPoint(null, 500, 200);
        clock.At(40);
        box.Children.Clear();
        clock.At(300);

        Assert.True(task.IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void B23_An_Animated_Scroll_Raises_The_Scroll_Events()
    {
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var log = new Recorder(content, clock);
        using var scene = new Scene(InBox(content));

        content.ScrollToPoint(null, 500, 100);
        clock.At(1000);

        Assert.Equal("start", log.Events[0].Name);
        Assert.Equal("end", log.Events[^1].Name);
        Assert.Equal(500, log.Details[^1].ScrollTop);
    }

    [AvaloniaFact]
    public void B24_With_Animated_Off_It_Jumps()
    {
        // Deliberate: measured animated:false, Ionic still animates (264 at 60 ms). AvonicConfig.Animated is the master switch.
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(InBox(content));

        AvonicConfig.Animated = false;
        try
        {
            var task = content.ScrollToPoint(null, 500, 300);

            Assert.True(task.IsCompletedSuccessfully);
            Assert.Equal(500, Scroll(content).Offset.Y);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    }

    [AvaloniaFact]
    public void B25_ScrollToTop()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(InBox(content));
        SetOffset(content, 300, 0);

        Assert.True(content.ScrollToTop().IsCompletedSuccessfully);
        Assert.Equal(0, Scroll(content).Offset.Y);

        SetOffset(content, 300);
        var task = content.ScrollToTop(100);
        Assert.False(task.IsCompleted);
        clock.At(200);
        Assert.Equal(0, Scroll(content).Offset.Y);
        Assert.True(task.IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void B25_ScrollToTop_Leaves_X()
    {
        var content = new AvonicContent { ScrollX = true, Content = new Border { Width = 800, Height = 1000 } };
        using var scene = new Scene(InBox(content));
        SetOffset(content, 300, 70);

        content.ScrollToTop();

        Assert.Equal(new Vector(70, 0), Scroll(content).Offset);
    }

    [AvaloniaFact]
    public void B25_ScrollToBottom()
    {
        // Measured bottom: 700, with the extent at 1000 and the viewport at 300.
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(InBox(content));

        Assert.True(content.ScrollToBottom().IsCompletedSuccessfully);
        Assert.Equal(700, Scroll(content).Offset.Y);

        SetOffset(content, 0);
        var task = content.ScrollToBottom(100);
        Assert.False(task.IsCompleted);
        clock.At(200);
        Assert.Equal(700, Scroll(content).Offset.Y);
        Assert.True(task.IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void B25_ScrollToBottom_On_Short_Content_Stays()
    {
        // Measured short: 0
        var content = new AvonicContent { Content = "short" };
        using var scene = new Scene(InBox(content));

        content.ScrollToBottom();

        Assert.Equal(0, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B25_ScrollToBottom_Counts_Content_Added_Just_Now()
    {
        // Measured added then bottom: 1300, because reading scrollHeight lays out.
        var stack = new StackPanel { Children = { new Border { Height = 1000 } } };
        var content = new AvonicContent { Content = stack };
        using var scene = new Scene(InBox(content));

        stack.Children.Add(new Border { Height = 500 });
        content.ScrollToBottom();

        Assert.Equal(1200, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B25_ScrollToPoint_Counts_Content_Added_Just_Now()
    {
        var stack = new StackPanel { Children = { new Border { Height = 400 } } };
        var content = new AvonicContent { Content = stack };
        using var scene = new Scene(InBox(content));

        stack.Children.Add(new Border { Height = 500 });
        content.ScrollToPoint(null, 500);

        Assert.Equal(500, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B25_ScrollByPoint_Adds_To_Where_It_Is()
    {
        // Measured bottom: by 30 then 30.5, and -5000 gives 0.
        using var clock = new TestClock();
        var content = new AvonicContent { ScrollX = true, Content = new Border { Width = 800, Height = 1000 } };
        using var scene = new Scene(InBox(content));

        Assert.True(content.ScrollByPoint(10, 30, 0).IsCompletedSuccessfully);
        content.ScrollByPoint(5, 30, 0);
        Assert.Equal(new Vector(15, 60), Scroll(content).Offset);

        content.ScrollByPoint(0, -5000, 0);
        Assert.Equal(0, Scroll(content).Offset.Y);

        var task = content.ScrollByPoint(0, 100, 100);
        Assert.False(task.IsCompleted);
        clock.At(200);
        Assert.Equal(100, Scroll(content).Offset.Y);
        Assert.True(task.IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void B25_ScrollByPoint_Counts_Content_That_Changed_Just_Now()
    {
        // Reading scrollTop lays out, so 700 is already 200 when the -100 is added.
        var body = new Border { Height = 1000 };
        var content = new AvonicContent { Content = body };
        using var scene = new Scene(InBox(content));
        SetOffset(content, 700);

        body.Height = 500;
        content.ScrollByPoint(0, -100, 0);

        Assert.Equal(100, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B25_Removed_Before_Load_A_Waiting_Scroll_Finishes()
    {
        var content = Tall();
        var task = content.ScrollToPoint(null, 100);
        var box = (Panel)InBox(content);
        var window = new Window { Content = box };

        window.Show();
        box.Children.Clear();
        Settle();
        window.Close();
        Settle();

        Assert.True(task.IsCompletedSuccessfully);
    }

    [AvaloniaFact]
    public void B25_Moved_Before_Load_A_Waiting_Scroll_Still_Happens()
    {
        var content = Tall();
        var task = content.ScrollToPoint(null, 100);
        var first = (Panel)InBox(content);
        var second = new Panel { Height = 300, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(new StackPanel { Children = { first, second } });

        // Scene settles, so this one has loaded. A fresh one is what's being moved.
        var moved = Tall();
        var movedTask = moved.ScrollToPoint(null, 100);
        first.Children.Clear();
        first.Children.Add(moved);
        first.Children.Clear();
        second.Children.Add(moved);
        Assert.False(movedTask.IsCompleted);
        Settle();

        Assert.True(task.IsCompletedSuccessfully);
        Assert.True(movedTask.IsCompletedSuccessfully);
        Assert.Equal(100, Scroll(moved).Offset.Y);
    }

    [AvaloniaFact]
    public void B25_After_Removal_Before_Load_A_New_Scroll_Does_Not_Wait()
    {
        var content = Tall();
        var box = (Panel)InBox(content);
        var window = new Window { Content = box };
        window.Show();
        box.Children.Clear();
        Settle();

        Assert.True(content.ScrollToTop().IsCompletedSuccessfully);

        window.Close();
        Settle();
    }

    [AvaloniaFact]
    public void B25_A_Scroll_Requested_Before_Load_Happens_On_Load()
    {
        // Measured before attached: not done until it's in the page, then 100.
        var content = Tall();

        var task = content.ScrollToPoint(null, 100);
        Assert.False(task.IsCompleted);

        using var scene = new Scene(InBox(content));
        Settle();

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(100, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B25_ScrollToBottom_Requested_Before_Load_Happens_On_Load()
    {
        var content = Tall();

        var task = content.ScrollToBottom();

        using var scene = new Scene(InBox(content));
        Settle();

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(700, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B25_A_Second_Request_Before_Load_Also_Runs()
    {
        var content = new AvonicContent { ScrollX = true, Content = new Border { Width = 800, Height = 1000 } };

        var first = content.ScrollToPoint(null, 100);
        var second = content.ScrollToPoint(50, null);

        using var scene = new Scene(InBox(content));
        Settle();

        Assert.True(first.IsCompletedSuccessfully);
        Assert.True(second.IsCompletedSuccessfully);
        Assert.Equal(new Vector(50, 100), Scroll(content).Offset);
    }

    [AvaloniaFact]
    public void B25_After_Removal_It_Does_Not_Wait()
    {
        // Measured detached: a scroll on an element that was loaded and removed resolves.
        var content = Tall();
        var box = (Panel)InBox(content);
        using var scene = new Scene(box);
        box.Children.Clear();

        Assert.True(content.ScrollToPoint(null, 100).IsCompletedSuccessfully);
    }

    // ── 26–28. Fullscreen ────────────────────────────────────────────────────────────────────

    /// <summary>The measured scene: a 400 high page, 56 above the content and 40 below it.</summary>
    private static (Grid Page, Border Head, Border Foot) Page(AvonicContent content)
    {
        var head = new Border { Background = Brushes.Blue, ZIndex = 10 };
        var foot = new Border { Background = Brushes.Red, ZIndex = 10 };
        Grid.SetRow(content, 1);
        Grid.SetRow(foot, 2);
        return (new Grid
        {
            Height = 400,
            VerticalAlignment = VerticalAlignment.Top,
            RowDefinitions = new RowDefinitions("56,*,40"),
            Children = { head, content, foot },
        }, head, foot);
    }

    [AvaloniaFact]
    public void B26_Fullscreen_Takes_The_Background_And_Scroll_To_The_Page_Edges()
    {
        // Measured fs: the host is 56 to 360, and the scroll and background parts are 0 to 400.
        var child = new Border { Height = 100 };
        var content = new AvonicContent { Fullscreen = true, Content = child };
        var (page, _, _) = Page(content);
        using var scene = new Scene(page);

        Assert.Equal(new Rect(0, 56, 390, 304), Box(content, scene.Window));
        Assert.Equal(new Rect(0, 0, 390, 400), Box(Scroll(content), scene.Window));
        Assert.Equal(new Rect(0, 0, 390, 400), Box(Background(content), scene.Window));
        Assert.Equal(400, Scroll(content).Viewport.Height);
    }

    [AvaloniaFact]
    public void B26_Content_Starts_Where_It_Would_Have()
    {
        // Measured first: 466 in both, which is the host's top. Padding is the offset.
        var child = new Border { Height = 100 };
        var content = new AvonicContent { Fullscreen = true, Content = child };
        var (page, _, _) = Page(content);
        using var scene = new Scene(page);

        Assert.Equal(56, Box(child, scene.Window).Y);
        Assert.Equal(new Thickness(0, 56, 0, 40), Container(content).Padding);
    }

    [AvaloniaFact]
    public void B26_Long_Content_Can_Scroll_Clear_Of_What_Is_Around_It()
    {
        // padding-bottom includes the offset, so its end stops above the foot.
        var child = new Border { Height = 1000 };
        var content = new AvonicContent { Fullscreen = true, Content = child };
        var (page, _, _) = Page(content);
        using var scene = new Scene(page);

        Assert.Equal(1096, Scroll(content).Extent.Height);
        content.ScrollToBottom();
        Settle();
        Assert.Equal(360, Box(child, scene.Window).Bottom);
    }

    [AvaloniaFact]
    public void B26_The_Padding_Tokens_Add_To_The_Offsets() => InstalledTheme.Run(_ =>
    {
        // padding-top: calc(var(--padding-top) + var(--offset-top))
        var content = new AvonicContent { Fullscreen = true, Content = new Border { Height = 10 } };
        content.Resources["Avonic.Content.PaddingTop"] = 5d;
        content.Resources["Avonic.Content.PaddingBottom"] = 6d;
        content.Resources["Avonic.Content.KeyboardOffset"] = 7d;
        var (page, _, _) = Page(content);
        using var scene = new Scene(page);

        Assert.Equal(new Thickness(0, 61, 0, 53), Container(content).Padding);
    });

    [AvaloniaFact]
    public void B26_Without_Fullscreen_It_Stays_In_Its_Box()
    {
        // Measured n
        var content = new AvonicContent { Content = new Border { Height = 100 } };
        var (page, _, _) = Page(content);
        using var scene = new Scene(page);

        Assert.Equal(new Rect(0, 56, 390, 304), Box(Scroll(content), scene.Window));
        Assert.Equal(new Rect(0, 56, 390, 304), Box(Background(content), scene.Window));
        Assert.Equal(default, Container(content).Padding);
    }

    [AvaloniaFact]
    public void B26_What_Is_Around_It_Stays_On_Top_And_Takes_Input()
    {
        // Measured hit at head: the head, with its z-index of 10. #12's header and footer need the same.
        var content = new AvonicContent { Fullscreen = true, Content = new Border { Height = 1000 } };
        var (page, head, foot) = Page(content);
        using var scene = new Scene(page);

        Assert.Same(head, Hit(scene, new Point(200, 20)));
        Assert.Same(foot, Hit(scene, new Point(200, 390)));
    }

    [AvaloniaFact]
    public void B26_Under_Nothing_The_Extended_Area_Takes_Input()
    {
        // The scroll part really reaches there, if what's around has no input of its own.
        var content = new AvonicContent { Fullscreen = true, Content = new Border { Height = 1000 } };
        var (page, head, _) = Page(content);
        head.IsHitTestVisible = false;
        using var scene = new Scene(page);

        Assert.Same(Container(content), Hit(scene, new Point(200, 20)));
    }

    [AvaloniaFact]
    public void B26_The_Background_And_Content_Paint_Outside_The_Host() => InstalledTheme.Run(_ =>
    {
        // What makes it worth having: a translucent toolbar shows the content scrolling behind it.
        var content = new AvonicContent { Fullscreen = true };
        content.Resources["Avonic.Content.Background"] = new SolidColorBrush(Colors.Red);
        var (page, head, foot) = Page(content);
        head.Background = null;
        foot.Background = null;
        using var scene = new Scene(page);
        scene.Window.Background = Brushes.White;
        Render();

        var frame = scene.Window.CaptureRenderedFrame()!;

        Assert.Equal(Colors.Red, PixelAt(frame, 200, 20));
        Assert.Equal(Colors.Red, PixelAt(frame, 200, 390));
        Assert.Equal(Colors.White, PixelAt(frame, 200, 410));

        // And content, scrolled up behind it.
        content.Content = new Border { Height = 1000, Background = Brushes.Lime };
        Settle();
        content.ScrollToPoint(null, 56);
        Render();
        Assert.Equal(Colors.Lime, PixelAt(scene.Window.CaptureRenderedFrame()!, 200, 20));
    });

    [AvaloniaFact]
    public void B26_A_Wrapper_Inside_An_Ion_Page_Class_Counts_From_The_Page()
    {
        // Measured nest: 18 from 7 and 11 of padding, and 300 - 18 - 100 = 182.
        var content = new AvonicContent { Fullscreen = true, Height = 100, VerticalAlignment = VerticalAlignment.Top };
        var wrapper = new Border { Padding = new Thickness(11), Child = content };
        var page = new Border { Height = 300, Padding = new Thickness(7), VerticalAlignment = VerticalAlignment.Top, Child = wrapper };
        page.Classes.Add("ion-page");
        using var scene = new Scene(page);

        Assert.Equal(new Rect(18, 0, 354, 300), Box(Scroll(content), scene.Window));
        Assert.Equal(new Thickness(0, 18, 0, 182), Container(content).Padding);
    }

    [AvaloniaFact]
    public void B26_An_Outer_Scroll_Does_Not_Change_The_Offset()
    {
        // offsetTop doesn't move with an ancestor's scroll.
        var inner = new AvonicContent { Fullscreen = true, Height = 100, VerticalAlignment = VerticalAlignment.Top };
        var outer = new AvonicContent
        {
            Content = new StackPanel { Children = { new Border { Height = 50 }, inner, new Border { Height = 1000 } } },
        };
        var page = new Border { Height = 400, VerticalAlignment = VerticalAlignment.Top, Child = outer };
        page.Classes.Add("ion-page");
        using var scene = new Scene(page);
        Assert.Equal(50, Container(inner).Padding.Top);

        SetOffset(outer, 30);

        Assert.Equal(50, Container(inner).Padding.Top);
    }

    [AvaloniaFact]
    public void B26_Above_The_Page_The_Offsets_Stop_At_Zero()
    {
        // Math.max(..., 0), for a content pushed above its page, or taller than it.
        var content = new AvonicContent { Fullscreen = true, Height = 500, Margin = new Thickness(0, -20, 0, 0), VerticalAlignment = VerticalAlignment.Top };
        var page = new Panel { Height = 300, VerticalAlignment = VerticalAlignment.Top, Children = { content } };
        using var scene = new Scene(page);

        Assert.Equal(default, Container(content).Padding);
    }

    [AvaloniaFact]
    public void B27_Fullscreen_Follows_A_Change()
    {
        // Measured toggle on, toggle off
        var content = new AvonicContent { Content = new Border { Height = 100 } };
        var (page, _, _) = Page(content);
        using var scene = new Scene(page);

        content.Fullscreen = true;
        Settle();
        Assert.Equal(new Rect(0, 0, 390, 400), Box(Scroll(content), scene.Window));

        content.Fullscreen = false;
        Settle();
        Assert.Equal(new Rect(0, 56, 390, 304), Box(Scroll(content), scene.Window));
        Assert.Equal(new Rect(0, 56, 390, 304), Box(Background(content), scene.Window));
        Assert.Equal(default, Container(content).Padding);
    }

    [AvaloniaFact]
    public void B27_Fullscreen_Follows_The_Page_At_Once()
    {
        // Deliberate. Measured head grows: Ionic stays at 56 until a window resize and 100 ms.
        var content = new AvonicContent { Fullscreen = true, Content = new Border { Height = 100 } };
        var (page, _, _) = Page(content);
        using var scene = new Scene(page);

        page.RowDefinitions[0].Height = new GridLength(100);
        Settle();

        Assert.Equal(new Thickness(0, 100, 0, 40), Container(content).Padding);
        Assert.Equal(new Rect(0, 0, 390, 400), Box(Scroll(content), scene.Window));
    }

    [AvaloniaFact]
    public void B27_Turned_Off_It_No_Longer_Follows()
    {
        var content = new AvonicContent { Fullscreen = true, Content = new Border { Height = 100 } };
        var (page, _, _) = Page(content);
        using var scene = new Scene(page);

        content.Fullscreen = false;
        Settle();
        page.RowDefinitions[0].Height = new GridLength(100);
        Settle();

        Assert.Equal(default, Container(content).Padding);
    }

    [AvaloniaFact]
    public void B27_Fullscreen_From_Xaml_Order_Works_Before_It_Is_Shown()
    {
        var content = new AvonicContent();
        content.Fullscreen = true;
        content.Content = new Border { Height = 100 };
        var (page, _, _) = Page(content);
        using var scene = new Scene(page);

        Assert.Equal(new Thickness(0, 56, 0, 40), Container(content).Padding);
    }

    [AvaloniaFact]
    public void B28_The_Fixed_Slot_Is_At_The_Hosts_Corner_Not_The_Pages()
    {
        // Measured fixed: 0,466, the same as the host.
        var slot = new Border { Width = 30, Height = 18 };
        var content = new AvonicContent { Fullscreen = true, FixedContent = slot };
        var (page, _, _) = Page(content);
        using var scene = new Scene(page);

        Assert.Equal(new Rect(0, 56, 30, 18), Box(slot, scene.Window));
    }

    // ── 29–32. The fixed slot ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B29_The_Fixed_Slot_Is_As_Big_As_Its_Content_At_The_Corner()
    {
        // Measured nop-f: 164.53 wide, 18 high, at the host's corner.
        var slot = new Border { Child = new Border { Width = 164, Height = 18 } };
        using var scene = new Scene(InBox(new AvonicContent { FixedContent = slot }));

        Assert.Equal(new Rect(0, 0, 164, 18), Box(slot, scene.Window));
    }

    [AvaloniaFact]
    public void B29_An_Apps_Alignment_Places_It()
    {
        // Measured ri-f: right: 10px; bottom: 10px, 60 by 60, at 320, 230 of a 390 by 300 host.
        var slot = new Border
        {
            Width = 60,
            Height = 60,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 10, 10),
        };
        using var scene = new Scene(InBox(new AvonicContent { FixedContent = slot }));

        Assert.Equal(new Rect(320, 230, 60, 60), Box(slot, scene.Window));
    }

    [AvaloniaFact]
    public void B29_An_App_Can_Fill_The_Host()
    {
        // Measured wh-f: all four insets at 0 fill it.
        var slot = new Border { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        using var scene = new Scene(InBox(new AvonicContent { FixedContent = slot }));

        Assert.Equal(new Rect(0, 0, 390, 300), Box(slot, scene.Window));
    }

    [AvaloniaFact]
    public void B29_A_Replaced_Fixed_Child_Is_Treated_The_Same_And_The_Old_One_Freed()
    {
        var first = new Border { Width = 10, Height = 10 };
        var content = new AvonicContent { FixedContent = first };
        using var scene = new Scene(InBox(content));

        var second = new Border { Child = new Border { Width = 20, Height = 20 } };
        content.FixedContent = second;
        Settle();

        Assert.Equal(new Rect(0, 0, 20, 20), Box(second, scene.Window));
        Assert.Equal(HorizontalAlignment.Stretch, first.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Stretch, first.VerticalAlignment);
    }

    [AvaloniaFact]
    public void B30_The_Fixed_Slot_Does_Not_Scroll()
    {
        // Measured scroll keeps fixed
        var slot = new Border { Width = 30, Height = 18 };
        var content = new AvonicContent { FixedContent = slot, Content = new Border { Height = 1000 } };
        using var scene = new Scene(InBox(content));
        var before = Box(slot, scene.Window);

        SetOffset(content, 200);

        Assert.Equal(before, Box(slot, scene.Window));
    }

    [AvaloniaFact]
    public void B30_The_Fixed_Slot_Only_Takes_Input_On_Its_Own_Child()
    {
        var slot = new Border { Width = 30, Height = 18, Background = Brushes.Red };
        var content = new AvonicContent { FixedContent = slot, Content = new Border { Height = 1000 } };
        using var scene = new Scene(InBox(content));

        Assert.Same(slot, Hit(scene, new Point(10, 10)));
        Assert.Same(Container(content), Hit(scene, new Point(100, 100)));
    }

    [AvaloniaFact]
    public void B31_After_Is_The_Default_And_Is_On_Top()
    {
        // Measured after: the hit is the fixed button.
        var slot = new Border { Width = 30, Height = 18, Background = Brushes.Red };
        var body = new Border { Height = 1000, Background = Brushes.Green };
        var content = new AvonicContent { FixedContent = slot, Content = body };
        using var scene = new Scene(InBox(content));

        Assert.Equal(ContentFixedSlotPlacement.After, content.FixedSlotPlacement);
        Assert.Same(slot, Hit(scene, new Point(10, 10)));
    }

    [AvaloniaFact]
    public void B31_Before_Is_Under_The_Scroll_Part_And_Takes_No_Pointer()
    {
        // Measured before: the hit is the body, not the fixed button, and shadow order is background, slot, scroll.
        var slot = new Border { Width = 30, Height = 18, Background = Brushes.Red };
        var body = new Border { Height = 1000, Background = Brushes.Green };
        var content = new AvonicContent { FixedContent = slot, Content = body, FixedSlotPlacement = ContentFixedSlotPlacement.Before };
        using var scene = new Scene(InBox(content));

        Assert.Same(body, Hit(scene, new Point(10, 10)));
        Assert.Equal(["PART_Background", "PART_Fixed", "PART_Scroll"], Fixed(content).GetVisualParent()!.GetVisualChildren().Select(v => ((Control)v).Name));
    }

    private static Button Focusable(string name) => new()
    {
        Name = name,
        Width = 60,
        Height = 30,
        Template = new FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Gray }),
    };

    private static List<string?> TabOrder(Scene scene, int count)
    {
        var order = new List<string?>();
        for (var i = 0; i < count; i++)
        {
            scene.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            Settle();
            order.Add((TopLevel.GetTopLevel(scene.Window)!.FocusManager!.GetFocusedElement() as Control)?.Name);
        }

        return order;
    }

    [AvaloniaFact]
    public void B32_After_Tabs_To_The_Content_First()
    {
        // Measured tab order: after-mb, after-f
        var content = new AvonicContent { Content = Focusable("main"), FixedContent = Focusable("fixed") };
        using var scene = new Scene(InBox(content));

        Assert.Equal(["main", "fixed"], TabOrder(scene, 2));
    }

    [AvaloniaFact]
    public void B32_Before_Tabs_To_The_Fixed_Slot_First()
    {
        // Measured tab order: before-f, before-mb
        var content = new AvonicContent { Content = Focusable("main"), FixedContent = Focusable("fixed"), FixedSlotPlacement = ContentFixedSlotPlacement.Before };
        using var scene = new Scene(InBox(content));

        Assert.Equal(["fixed", "main"], TabOrder(scene, 2));
    }

    [AvaloniaFact]
    public void B32_A_Change_Of_Placement_Moves_It_Both_Ways_Without_Remaking_The_Child()
    {
        var slot = Focusable("fixed");
        var detached = 0;
        slot.DetachedFromVisualTree += (_, _) => detached++;
        var content = new AvonicContent { Content = Focusable("main"), FixedContent = slot };
        using var scene = new Scene(InBox(content));

        content.FixedSlotPlacement = ContentFixedSlotPlacement.Before;
        Settle();
        Assert.Equal(["fixed", "main"], TabOrder(scene, 2));

        content.FixedSlotPlacement = ContentFixedSlotPlacement.After;
        Settle();
        Assert.Equal(["PART_Background", "PART_Scroll", "PART_Fixed"], Fixed(content).GetVisualParent()!.GetVisualChildren().Select(v => ((Control)v).Name));
        Assert.Equal(0, detached);
    }

    // ── 33–35. Everything else ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B33_It_Is_The_Main_Landmark()
    {
        // Measured role: main
        var content = new AvonicContent();
        using var scene = new Scene(InBox(content));

        var peer = ControlAutomationPeer.CreatePeerForElement(content);

        Assert.Equal(AutomationLandmarkType.Main, peer.GetLandmarkType());
        // A control's default peer isn't part of the tree that a screen reader walks.
        Assert.True(peer.IsControlElement());
        Assert.True(peer.IsContentElement());
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
    }

    [AvaloniaFact]
    public void B34_Focus_Scrolls_A_Child_Into_View()
    {
        // A browser does this too, and it's Avalonia's ScrollViewer here.
        var target = Focusable("target");
        var content = new AvonicContent
        {
            Content = new StackPanel { Children = { new Border { Height = 600 }, target } },
        };
        using var scene = new Scene(InBox(content));

        target.Focus();
        Settle();

        Assert.True(Scroll(content).Offset.Y > 0);
    }

    [AvaloniaFact]
    public void B35_Slots_Take_Templates()
    {
        var template = new FuncDataTemplate<string>((text, _) => new Border { Height = 7, Tag = text });
        var content = new AvonicContent { Content = "a", ContentTemplate = template, FixedContent = "b", FixedContentTemplate = template };
        using var scene = new Scene(InBox(content));

        Assert.Equal("a", ((Border)Container(content).Child!).Tag);
        Assert.Equal("b", ((Border)Fixed(content).Child!).Tag);
    }

    [AvaloniaFact]
    public void B36_The_Safe_Area_Tokens_Are_Zero() => InstalledTheme.Run(theme =>
    {
        // Avalonia pads its root view by the safe area itself (TopLevel.AutoSafeAreaPadding), so these add nothing.
        foreach (var key in new[] { "Avonic.SafeAreaTop", "Avonic.SafeAreaBottom", "Avonic.SafeAreaLeft", "Avonic.SafeAreaRight" })
        {
            Assert.True(theme.TryGetResource(key, null, out var value));
            Assert.Equal(0d, value);
        }
    });

    [AvaloniaFact]
    public void B37_Every_Token_Has_Ionics_Default() => InstalledTheme.Run(theme =>
    {
        foreach (var key in new[] { "Avonic.Content.KeyboardOffset", "Avonic.Content.PaddingTop", "Avonic.Content.PaddingBottom", "Avonic.Content.PaddingStart", "Avonic.Content.PaddingEnd" })
        {
            Assert.True(theme.TryGetResource(key, null, out var value));
            Assert.Equal(0d, value);
        }

        foreach (var key in new[] { "Avonic.Content.Background", "Avonic.Content.Color" })
        {
            Assert.True(theme.TryGetResource(key, null, out var value));
            Assert.IsType<CssUnset>(value);
        }
    });
}
