using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-text in Chromium, at 390 px wide. See docs/components/ion-text/PLAN.md.
/// The rule is in the stub, so what doesn't need the palette or an item runs without the theme.
/// </summary>
public class AvonicTextTests
{
    private const string Long = "A very long piece of text that certainly has to wrap around because it does not fit into a phone-sized viewport at all";

    private static readonly Color Danger = Color.FromRgb(197, 0, 15);
    private static readonly Color PrimaryBase = Color.FromRgb(0, 84, 233);
    private static readonly Color Success = Color.FromRgb(45, 213, 91);
    private static readonly Color Grey = Color.FromRgb(102, 102, 102);
    private static readonly Color Parent = Color.FromRgb(1, 2, 3);

    private sealed class Stub(ItemControlType type) : Border, IItemControl
    {
        public int ItemTaps { get; private set; }

        public ItemControlType ItemControlType => type;

        public void OnItemTap() => ItemTaps++;
    }

    private static Color TextColor(Control control) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(TextElement.GetForeground(control)).Color;

    private static Color SpanColor(Inline inline) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(inline.Foreground).Color;

    private static TextBlock Text(AvonicText text) => text.GetVisualDescendants().OfType<TextBlock>().First();

    private static Rect Box(Visual visual, Visual root) => new(visual.TranslatePoint(default, root)!.Value, visual.Bounds.Size);

    /// <summary>Layout rounds to whole pixels, so an odd gap can't be split exactly.</summary>
    private static void AssertCentred(Visual visual, AvonicItem item) =>
        Assert.InRange(Box(visual, item).Center.Y - item.Bounds.Height / 2, -1, 1);

    private static Run Word(string text = "x") => new(text);

    private static AvonicTextSpan Span(string? color, params Inline[] inlines)
    {
        var span = new AvonicTextSpan { Color = color };
        span.Inlines!.AddRange(inlines);
        return span;
    }

    private static TextBlock Block(params Inline[] inlines)
    {
        var block = new TextBlock();
        block.Inlines!.AddRange(inlines);
        return block;
    }

    private static Scene InScene(params Control[] children)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(children);
        return new Scene(panel);
    }

    private static StackPanel Tall() => new()
    {
        Children =
        {
            new TextBlock { Text = "One", Height = 30 },
            new TextBlock { Text = "Two", Height = 30 },
            new TextBlock { Text = "Three", Height = 30 },
        },
    };

    private static void Tap(Scene scene, Point point)
    {
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>How many pixels in a band of rows are nearly this colour, which antialiasing doesn't reach.</summary>
    private static unsafe int Ink(WriteableBitmap frame, Color color, int top, int bottom)
    {
        using var buffer = frame.Lock();
        var count = 0;
        for (var y = top; y < bottom; y++)
        {
            var row = (byte*)buffer.Address + y * buffer.RowBytes;
            for (var x = 0; x < frame.PixelSize.Width; x++)
            {
                var (r, g, b) = (row[x * 4], row[x * 4 + 1], row[x * 4 + 2]);
                if (Math.Abs(r - color.R) < 8 && Math.Abs(g - color.G) < 8 && Math.Abs(b - color.B) < 8)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static WriteableBitmap Frame(Scene scene)
    {
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();
        return scene.Window.CaptureRenderedFrame()!;
    }

    // ── 1. With no colour ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_A_Box_With_No_Colour_Takes_Everything_From_Its_Parent() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = "x" };
        var parent = new StackPanel { Children = { text } };
        var family = new FontFamily("serif");
        TextElement.SetForeground(parent, new SolidColorBrush(Parent));
        TextElement.SetFontSize(parent, 30);
        TextElement.SetFontFamily(parent, family);
        using var scene = new Scene(parent);

        Assert.Equal(Parent, TextColor(Text(text)));
        Assert.Equal(30, Text(text).FontSize);
        Assert.Equal(family, Text(text).FontFamily);
    });

    [AvaloniaFact]
    public void B1_A_Span_With_No_Colour_Takes_Everything_From_Its_Parent() => InstalledTheme.Run(_ =>
    {
        var span = Span(null, Word());
        var block = Block(span);
        var family = new FontFamily("serif");
        block.Foreground = new SolidColorBrush(Parent);
        block.FontSize = 30;
        block.FontFamily = family;
        using var scene = InScene(block);

        Assert.Equal(Parent, SpanColor(span));
        Assert.Equal(30, span.FontSize);
        Assert.Equal(family, span.FontFamily);
    });

    // ── 2. Color ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_Color_Paints_A_Box() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = "x", Color = "danger" };
        using var scene = InScene(text);

        Assert.Equal(Danger, TextColor(Text(text)));
    });

    [AvaloniaFact]
    public void B2_Color_Paints_A_Span_And_Its_Words() => InstalledTheme.Run(_ =>
    {
        var word = Word();
        var span = Span("danger", word);
        using var scene = InScene(Block(span));

        Assert.Equal(Danger, SpanColor(span));
        Assert.Equal(Danger, SpanColor(word));
    });

    [AvaloniaFact]
    public void B2_A_Spans_Colour_Reaches_The_Pixels_And_Not_The_Words_Around_It() => InstalledTheme.Run(_ =>
    {
        var block = new TextBlock { FontSize = 60, Foreground = Brushes.Black };
        block.Inlines!.Add(Word("ooo"));
        block.Inlines.Add(Span("danger", Word("OOO")));
        block.Inlines.Add(Word("ooo"));
        using var scene = InScene(block);

        var frame = Frame(scene);

        Assert.True(Ink(frame, Danger, 0, 100) > 100);
        Assert.True(Ink(frame, Colors.Black, 0, 100) > 100);
    });

    // ── 3. An unknown colour ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_An_Unknown_Color_Inherits_From_Its_Parent() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = "x", Color = "nonexistent" };
        var span = Span("nonexistent", Word());
        var block = Block(span);
        block.Foreground = new SolidColorBrush(Parent);
        var parent = new StackPanel { Children = { text, block } };
        TextElement.SetForeground(parent, new SolidColorBrush(Parent));
        using var scene = new Scene(parent);

        Assert.Equal(Parent, TextColor(Text(text)));
        Assert.Equal(Parent, SpanColor(span));
    });

    [AvaloniaFact]
    public void B3_An_Unknown_Color_On_A_Span_Inherits_From_A_Plain_Span_Around_It() => InstalledTheme.Run(_ =>
    {
        var inner = Span("nonexistent", Word());
        var outer = new Span { Foreground = new SolidColorBrush(Parent), Inlines = { inner } };
        using var scene = InScene(Block(outer));

        Assert.Equal(Parent, SpanColor(inner));
    });

    [AvaloniaFact]
    public void B3_An_Unknown_Color_In_A_Coloured_Item_Shows_Its_Base() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = "x", Color = "nonexistent" };
        var span = Span("nonexistent", Word());
        using var scene = InScene(
            new AvonicItem { Color = "primary", Content = text },
            new AvonicItem { Color = "primary", Content = Block(span) });

        Assert.Equal(PrimaryBase, TextColor(Text(text)));
        Assert.Equal(PrimaryBase, SpanColor(span));
    });

    // ── 4. Nested ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Nested_Spans_Follow_Measured_T_Outer() => InstalledTheme.Run(_ =>
    {
        var plain = Span(null, Word());
        var coloured = Span("success", Word());
        var unknown = Span("nonexistent", Word());
        using var scene = InScene(Block(Span("danger", plain, coloured, unknown)));

        Assert.Equal(Danger, SpanColor(plain));
        Assert.Equal(Success, SpanColor(coloured));
        Assert.Equal(Danger, SpanColor(unknown));
    });

    [AvaloniaFact]
    public void B4_Nested_Boxes_Follow_Measured_T_Outer() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicText { Content = "x" };
        var coloured = new AvonicText { Content = "x", Color = "success" };
        var unknown = new AvonicText { Content = "x", Color = "nonexistent" };
        using var scene = InScene(new AvonicText { Color = "danger", Content = new StackPanel { Children = { plain, coloured, unknown } } });

        Assert.Equal(Danger, TextColor(Text(plain)));
        Assert.Equal(Success, TextColor(Text(coloured)));
        Assert.Equal(Danger, TextColor(Text(unknown)));
    });

    [AvaloniaFact]
    public void B4_A_Span_In_A_Box_Follows_The_Box() => InstalledTheme.Run(_ =>
    {
        var span = Span("nonexistent", Word());
        using var scene = InScene(new AvonicText { Color = "danger", Content = Block(span) });

        Assert.Equal(Danger, SpanColor(span));
    });

    // ── 5. Everything inside ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Blocks_And_Icons_Inside_Take_The_Colour() => InstalledTheme.Run(_ =>
    {
        var one = new TextBlock { Text = "x" };
        var two = new TextBlock { Text = "x" };
        var icon = new AvonicIcon { IconName = "heart" };
        using var scene = InScene(new AvonicText { Color = "danger", Content = new StackPanel { Children = { one, two, icon } } });

        Assert.Equal(Danger, TextColor(one));
        Assert.Equal(Danger, TextColor(two));
        Assert.Equal(Danger, TextColor(icon));
    });

    // ── 6. In a sentence ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_A_Span_Flows_And_Wraps_With_The_Words_Around_It() => InstalledTheme.Run(_ =>
    {
        const string Before = "Some words before ";
        const string Middle = "the coloured part of a sentence which wraps around the line";
        const string After = " and some after.";
        var coloured = new TextBlock { Width = 200, TextWrapping = TextWrapping.Wrap, FontSize = 16, Foreground = Brushes.Black };
        coloured.Inlines!.Add(Word(Before));
        coloured.Inlines.Add(Span("danger", Word(Middle)));
        coloured.Inlines.Add(Word(After));
        var plain = new TextBlock { Width = 200, TextWrapping = TextWrapping.Wrap, FontSize = 16, Text = Before + Middle + After };
        using var scene = InScene(coloured, plain);

        // The same wrapping as one run of words, so it isn't a box: three lines at least, with colour on each.
        Assert.Equal(plain.Bounds.Height, coloured.Bounds.Height);
        var lines = coloured.TextLayout.TextLines;
        Assert.True(lines.Count >= 3);

        var frame = Frame(scene);
        var top = 0.0;
        foreach (var line in lines.Take(lines.Count - 1))
        {
            Assert.True(Ink(frame, Danger, (int)top, (int)(top + line.Height)) > 0);
            top += line.Height;
        }
    });

    // ── 7. In an item ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_In_An_Item_It_Follows_The_Items_Colour() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicText { Content = "x" };
        var coloured = new AvonicText { Content = "x" };
        var span = Span(null, Word());
        using var scene = InScene(
            new AvonicItem { Content = plain },
            new AvonicItem { Color = "primary", Content = coloured },
            new AvonicItem { Color = "primary", Content = Block(span) });

        Assert.Equal(Colors.Black, TextColor(Text(plain)));
        Assert.Equal(Colors.White, TextColor(Text(coloured)));
        Assert.Equal(Colors.White, SpanColor(span));
    });

    [AvaloniaFact]
    public void B7_Its_Own_Colour_Beats_The_Items() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = "x", Color = "danger" };
        var span = Span("danger", Word());
        using var scene = InScene(
            new AvonicItem { Color = "primary", Content = text },
            new AvonicItem { Color = "primary", Content = Block(span) });

        Assert.Equal(Danger, TextColor(Text(text)));
        Assert.Equal(Danger, SpanColor(span));
    });

    // ── 8. In an item's slot ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_The_Slots_Margins_Apply_And_It_Is_Centred() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicText { Content = "x" };
        var end = new AvonicText { Content = "x" };
        var item = new AvonicItem { StartContent = start, Content = Tall(), EndContent = end };
        using var scene = InScene(item);

        Assert.Equal(new Thickness(0, 0, 16, 0), start.Margin);
        Assert.Equal(new Thickness(16, 0, 0, 0), end.Margin);
        AssertCentred(start, item);
        AssertCentred(end, item);
    });

    [AvaloniaFact]
    public void B8_In_The_Default_Slot_It_Is_Centred_And_As_Wide_As_Its_Text() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = "x" };
        var item = new AvonicItem { Content = text };
        using var scene = InScene(item);

        AssertCentred(text, item);
        Assert.Equal(16, Box(text, item).X);

        // Its TextBlock would stretch with it, so this has to be against the row.
        Assert.True(text.Bounds.Width < 30);
    });

    [AvaloniaFact]
    public void B8_The_Stub_Is_As_Wide_As_Its_Text()
    {
        var text = new AvonicText { Content = "x" };
        using var scene = InScene(text);

        Assert.True(text.Bounds.Width < 30);
    }

    // ── 9. Long text ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_Long_Text_Wraps_And_Grows_The_Item() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = Long };
        var item = new AvonicItem { Content = text };
        using var scene = InScene(item);

        Assert.True(Box(text, item).Right <= 374);
        Assert.True(item.Bounds.Height > 48);
    });

    [AvaloniaFact]
    public void B9_The_Stub_Wraps_And_Does_Not_Clip()
    {
        var text = new AvonicText { Content = Long };
        var one = new AvonicText { Content = "x" };
        using var scene = InScene(text, one);

        Assert.True(text.Bounds.Width <= 390);
        Assert.True(text.Bounds.Height > one.Bounds.Height);
        Assert.False(text.ClipToBounds);
    }

    // ── 10. In a label's paragraph ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_A_Span_In_A_Labels_P_Has_The_Ps_Size_And_Its_Own_Colour() => InstalledTheme.Run(_ =>
    {
        var span = Span("danger", Word());
        var plain = Word();
        var p = Block(plain, span);
        p.Classes.Add("p");
        using var scene = InScene(new AvonicItem { Content = new AvonicLabel { Content = p } });

        Assert.Equal(14, p.FontSize);
        Assert.Equal(20, p.LineHeight);
        Assert.Equal(14, span.FontSize);
        Assert.Equal(Danger, SpanColor(span));
        Assert.Equal(Grey, SpanColor(plain));
    });

    // ── 14. white-space inherits from a label ────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_In_A_Nowrap_Label_It_Is_One_Line_With_An_Ellipsis() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = Long };
        var label = new AvonicLabel { Content = new StackPanel { Children = { text } }, Classes = { "ion-text-nowrap" } };
        var one = new AvonicText { Content = "x" };
        using var scene = InScene(new AvonicItem { Content = label }, new AvonicItem { Content = new AvonicLabel { Content = one } });

        Assert.Equal(TextWrapping.NoWrap, Text(text).TextWrapping);
        Assert.Equal(TextTrimming.CharacterEllipsis, Text(text).TextTrimming);
        Assert.Equal(one.Bounds.Height, text.Bounds.Height);
    });

    [AvaloniaFact]
    public void B14_The_Class_On_The_Box_Itself_Is_One_Line() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = Long, Classes = { "ion-text-nowrap" } };
        using var scene = InScene(text);

        Assert.Equal(TextWrapping.NoWrap, Text(text).TextWrapping);
    });

    [AvaloniaFact]
    public void B14_In_A_Plain_Label_It_Wraps() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = Long };
        using var scene = InScene(new AvonicItem { Content = new AvonicLabel { Content = text } });

        Assert.Equal(TextWrapping.Wrap, Text(text).TextWrapping);
    });

    [AvaloniaFact]
    public void B14_A_Span_In_A_Nowrap_Label_Follows_Its_TextBlock() => InstalledTheme.Run(_ =>
    {
        var block = Block(Word(Long), Span("danger", Word(Long)));
        var label = new AvonicLabel { Content = block, Classes = { "ion-text-nowrap" } };
        using var scene = InScene(new AvonicItem { Content = label });

        Assert.Equal(TextWrapping.NoWrap, block.TextWrapping);
        Assert.Equal(TextTrimming.CharacterEllipsis, block.TextTrimming);
    });

    // ── 11. Taps ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_A_Tap_On_It_Does_Not_Forward_And_A_Tap_Beside_It_Does() => InstalledTheme.Run(_ =>
    {
        var control = new Stub(ItemControlType.Toggle) { Height = 20 };
        var text = new AvonicText { Content = "words" };
        var item = new AvonicItem { Content = text, StartContent = control };
        using var scene = InScene(item);

        Tap(scene, Box(text, scene.Window).Center);
        Assert.Equal(0, control.ItemTaps);

        Tap(scene, new Point(Box(text, scene.Window).Right + 20, item.Bounds.Height / 2));
        Assert.Equal(1, control.ItemTaps);
    });

    [AvaloniaFact]
    public void B11_The_Stubs_Whole_Box_Takes_The_Tap()
    {
        var control = new Stub(ItemControlType.Toggle) { Height = 20 };
        var text = new AvonicText { Content = "words", Padding = new Thickness(0, 20) };
        var item = new AvonicItem { Content = text, StartContent = control };
        using var scene = InScene(item);

        Assert.Equal(40 + Text(text).Bounds.Height, text.Bounds.Height);
        Tap(scene, Box(text, scene.Window).TopLeft + new Point(2, 2));

        Assert.Equal(0, control.ItemTaps);
    }

    // ── 12. Automation ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_A_Button_Items_Name_Includes_A_Boxs_Text() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true, Content = new AvonicText { Content = "Words" } };
        using var scene = InScene(item);

        Assert.Contains("Words", ControlAutomationPeer.CreatePeerForElement(item).GetName());
    });

    // ── 13. A change of colour ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_A_Change_Of_Color_Repaints_A_Box() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicText { Content = "x", Color = "danger" };
        using var scene = InScene(text);

        text.Color = "success";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Success, TextColor(Text(text)));

        text.Color = null;
        Dispatcher.UIThread.RunJobs();
        Assert.NotEqual(Success, TextColor(Text(text)));
    });

    [AvaloniaFact]
    public void B13_A_Change_Of_Color_Repaints_A_Span() => InstalledTheme.Run(_ =>
    {
        var block = new TextBlock { FontSize = 60, Foreground = Brushes.Black };
        var span = Span("danger", Word("OOO"));
        block.Inlines!.Add(span);
        using var scene = InScene(block);
        Assert.True(Ink(Frame(scene), Danger, 0, 100) > 100);

        span.Color = "success";
        Dispatcher.UIThread.RunJobs();
        var after = Frame(scene);

        Assert.Equal(0, Ink(after, Danger, 0, 100));
        Assert.True(Ink(after, Success, 0, 100) > 100);
    });

    // ── The default slot's other property ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void The_Box_Passes_On_What_A_ContentControl_Has()
    {
        var brush = new SolidColorBrush(Danger);
        var text = new AvonicText
        {
            Content = "x",
            BorderBrush = brush,
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(4),
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
        };
        using var scene = InScene(text);

        var presenter = Assert.IsType<Avalonia.Controls.Presenters.ContentPresenter>(Text(text).GetVisualParent());
        Assert.Same(brush, presenter.BorderBrush);
        Assert.Equal(new Thickness(3), presenter.BorderThickness);
        Assert.Equal(new CornerRadius(4), presenter.CornerRadius);
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Right, presenter.HorizontalContentAlignment);
        Assert.Equal(Avalonia.Layout.VerticalAlignment.Bottom, presenter.VerticalContentAlignment);
    }

    [AvaloniaFact]
    public void The_Box_Uses_The_Content_Template()
    {
        var text = new AvonicText { Content = "x", ContentTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Text = "From a template" }) };
        using var scene = InScene(text);

        Assert.Equal("From a template", Text(text).Text);
    }

    // ── The theme isn't needed for the rule itself ───────────────────────────────────────────

    [AvaloniaFact]
    public void Without_The_Theme_A_Custom_Colour_Still_Paints_Both_Shapes()
    {
        var text = new AvonicText { Content = "x", Color = "sea" };
        var span = Span("sea", Word());
        var panel = new StackPanel { Children = { text, Block(span) } };
        panel.Resources["Avonic.Color.Sea"] = Danger;
        using var scene = new Scene(panel);

        Assert.Equal(Danger, TextColor(Text(text)));
        Assert.Equal(Danger, SpanColor(span));
    }
}
