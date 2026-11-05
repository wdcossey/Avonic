using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-label in Chromium, at 390 px wide. See docs/components/ion-label/PLAN.md.
/// Almost all of a label's look depends on the theme, so nearly every test installs it.
/// </summary>
public class AvonicLabelTests
{
    private const string Long = "A very long piece of text that certainly has to wrap around because it does not fit into a phone-sized viewport at all";

    private static readonly Color Danger = Color.FromRgb(197, 0, 15);
    private static readonly Color PrimaryBase = Color.FromRgb(0, 84, 233);
    private static readonly Color Success = Color.FromRgb(45, 213, 91);
    private static readonly Color Grey = Color.FromRgb(102, 102, 102);
    private static readonly Color Token = Color.FromRgb(18, 52, 86);

    private sealed class Stub(ItemControlType type) : Border, IItemControl
    {
        public int ItemTaps { get; private set; }

        public ItemControlType ItemControlType => type;

        public void OnItemTap() => ItemTaps++;
    }

    private static bool Has(Control control, string pseudoClass) => control.Classes.Contains(pseudoClass);

    private static void SetState(Control control, string pseudoClass, bool value = true)
    {
        ((IPseudoClasses)control.Classes).Set(pseudoClass, value);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Raise(Control source, params (string Key, bool Value)[] styles)
    {
        source.RaiseEvent(new IonStyleEventArgs(styles.ToDictionary(style => style.Key, style => style.Value)));
        Dispatcher.UIThread.RunJobs();
    }

    private static Color TextColor(Control control)
    {
        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(TextElement.GetForeground(control));
        return brush.Color;
    }

    private static TextBlock Text(AvonicLabel label) => label.GetVisualDescendants().OfType<TextBlock>().First();

    private static Rect Box(Visual visual, Visual root)
    {
        var topLeft = visual.TranslatePoint(default, root)!.Value;
        return new Rect(topLeft, visual.Bounds.Size);
    }

    private static Matrix Transform(AvonicLabel label) =>
        Assert.IsType<TransformOperations>(label.RenderTransform).Value;

    /// <summary>Ends a test's easing, so it can check the end value. A local value beats the theme's.</summary>
    private static AvonicLabel NoTransitions(AvonicLabel label)
    {
        label.Transitions = [];
        return label;
    }

    private static void Tap(Scene scene, Point point)
    {
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static Scene InScene(params Control[] children)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(children);
        return new Scene(panel);
    }

    // ── 1. Outside an item ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Alone_It_Inherits() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var parent = new StackPanel { Children = { label } };
        TextElement.SetForeground(parent, new SolidColorBrush(Danger));
        TextElement.SetFontSize(parent, 21);
        using var scene = new Scene(parent);

        Assert.Equal(Danger, TextColor(Text(label)));
        Assert.Equal(21, Text(label).FontSize);
    });

    [AvaloniaFact]
    public void B1_Alone_The_Token_Does_Nothing() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var parent = new StackPanel { Children = { label } };
        TextElement.SetForeground(parent, new SolidColorBrush(Danger));
        using var scene = new Scene(parent);

        scene.Window.Resources["Avonic.Label.Color"] = new SolidColorBrush(Token);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Danger, TextColor(label));
    });

    [AvaloniaTheory]
    [InlineData(LabelPosition.Fixed)]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void B1_Alone_Position_Does_Nothing(LabelPosition position)
    {
        InstalledTheme.Run(_ =>
        {
            var plain = new AvonicLabel { Content = "x" };
            var label = new AvonicLabel { Content = "x", Position = position };
            using var scene = InScene(plain, label);

            // Measured: an inline box can't take a transform or a width.
            Assert.Null(label.RenderTransform);
            Assert.Equal(plain.Bounds.Size, label.Bounds.Size);
        });
    }

    // ── 2, 3. Color ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_Color_Paints_The_Text_Alone() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Color = "danger" };
        using var scene = InScene(label);

        Assert.True(Has(label, ":ion-color"));
        Assert.Equal(Danger, TextColor(Text(label)));
    });

    [AvaloniaFact]
    public void B2_Color_Paints_The_Text_In_An_Item() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Color = "danger" };
        using var scene = InScene(new AvonicItem { Content = label });

        Assert.Equal(Danger, TextColor(Text(label)));
    });

    [AvaloniaFact]
    public void B2_Color_Can_Change() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Color = "danger" };
        using var scene = InScene(new AvonicItem { Content = label });

        label.Color = "success";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Success, TextColor(label));

        label.Color = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Colors.Black, TextColor(label));
    });

    [AvaloniaFact]
    public void B3_Unknown_Color_Alone_Inherits() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Color = "nonexistent" };
        var parent = new StackPanel { Children = { label } };
        TextElement.SetForeground(parent, new SolidColorBrush(Danger));
        using var scene = new Scene(parent);

        Assert.Equal(Danger, TextColor(label));
    });

    [AvaloniaFact]
    public void B3_Unknown_Color_In_A_Plain_Item_Inherits() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Color = "nonexistent" };
        using var scene = InScene(new AvonicItem { Content = label });

        Assert.Equal(Colors.Black, TextColor(label));
    });

    [AvaloniaFact]
    public void B3_Unknown_Color_In_A_Coloured_Item_Shows_The_Items_Base() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Color = "nonexistent" };
        using var scene = InScene(new AvonicItem { Content = label, Color = "primary" });

        // Measured: blue on blue, because --ion-color-base is inherited.
        Assert.Equal(PrimaryBase, TextColor(label));
    });

    // ── 4. In an item ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_The_Item_Colour_Is_The_Default() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicLabel { Content = "x" };
        var inColoured = new AvonicLabel { Content = "x" };
        using var scene = InScene(new AvonicItem { Content = plain }, new AvonicItem { Content = inColoured, Color = "primary" });

        Assert.Equal(Colors.Black, TextColor(Text(plain)));
        Assert.Equal(Colors.White, TextColor(Text(inColoured)));
    });

    [AvaloniaFact]
    public void B4_Inherits_From_What_Is_Between_It_And_The_Item() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var between = new StackPanel { Children = { label } };
        TextElement.SetForeground(between, new SolidColorBrush(Danger));
        using var scene = InScene(new AvonicItem { Content = between });

        Assert.Equal(Danger, TextColor(label));
    });

    [AvaloniaFact]
    public void B4_The_Token_Sets_The_Text() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        using var scene = InScene(new AvonicItem { Content = label });

        scene.Window.Resources["Avonic.Label.Color"] = new SolidColorBrush(Token);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Token, TextColor(Text(label)));
    });

    [AvaloniaFact]
    public void B4_Color_Beats_The_Token() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Color = "danger" };
        using var scene = InScene(new AvonicItem { Content = label });

        scene.Window.Resources["Avonic.Label.Color"] = new SolidColorBrush(Token);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Danger, TextColor(label));
    });

    [AvaloniaFact]
    public void B4_The_Token_Font_Applies_In_An_Item_And_The_Size_Inherits() => InstalledTheme.Run(theme =>
    {
        var label = new AvonicLabel { Content = "x" };
        using var scene = InScene(new AvonicItem { Content = label, FontFamily = new FontFamily("Courier New") });

        theme.TryGetResource("Avonic.FontFamily", null, out var font);
        Assert.Equal(font, label.FontFamily);
        Assert.Equal(16, Text(label).FontSize);
    });

    // ── 5, 6. ionStyle ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_It_Tells_The_Item_About_Itself() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicItem { Content = new AvonicLabel { Content = "x" } };
        var stacked = new AvonicItem { Content = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked } };
        using var scene = InScene(plain, stacked);

        Assert.True(Has(plain, ":item-label"));
        Assert.False(Has(plain, ":item-label-stacked"));
        Assert.True(Has(stacked, ":item-label"));
        Assert.True(Has(stacked, ":item-label-stacked"));
    });

    [AvaloniaFact]
    public void B5_It_Works_On_The_Stub_Too()
    {
        var item = new AvonicItem { Content = new AvonicLabel { Content = "x", Position = LabelPosition.Floating } };
        using var scene = InScene(item);

        Assert.True(Has(item, ":item-label"));
        Assert.True(Has(item, ":item-label-floating"));
    }

    [AvaloniaFact]
    public void B5_A_Position_Change_Tells_The_Item() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var item = new AvonicItem { Content = label };
        using var scene = InScene(item);
        Assert.Equal(48, item.Bounds.Height);

        label.Position = LabelPosition.Stacked;
        Dispatcher.UIThread.RunJobs();
        Assert.True(Has(item, ":item-label-stacked"));
        Assert.Equal(55, item.Bounds.Height);

        label.Position = LabelPosition.Fixed;
        Dispatcher.UIThread.RunJobs();
        Assert.False(Has(item, ":item-label-stacked"));
        Assert.True(Has(item, ":item-label-fixed"));

        label.Position = null;
        Dispatcher.UIThread.RunJobs();
        Assert.False(Has(item, ":item-label-fixed"));
        Assert.True(Has(item, ":item-label"));
        Assert.Equal(48, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void B5_It_Tells_An_Item_Attached_Later()
    {
        var item = new AvonicItem { Content = new AvonicLabel { Content = "x" } };
        var panel = new StackPanel();
        using var scene = new Scene(panel);
        Assert.False(Has(item, ":item-label"));

        panel.Children.Add(item);
        Dispatcher.UIThread.RunJobs();

        Assert.True(Has(item, ":item-label"));
    }

    [AvaloniaFact]
    public void B5_Setting_Position_Before_Attaching_Raises_Nothing()
    {
        var raised = 0;
        var label = new AvonicLabel { Content = "x" };
        label.AddHandler(IonStyle.IonStyleEvent, (_, _) => raised++);

        label.Position = LabelPosition.Stacked;

        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void B5_A_Label_Moved_To_Another_Item_Tells_It() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        var first = new AvonicItem { Content = label };
        var second = new AvonicItem();
        using var scene = InScene(first, second);

        first.Content = null;
        Dispatcher.UIThread.RunJobs();
        second.Content = label;
        Dispatcher.UIThread.RunJobs();

        // Deliberate: measured, Ionic's second item gets nothing. Its first keeps the classes, as this one does.
        Assert.True(Has(second, ":item-label-stacked"));
        Assert.True(Has(first, ":item-label-stacked"));
    });

    [AvaloniaFact]
    public void B6_Inside_A_Range_It_Tells_Nobody()
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        var range = new Stub(ItemControlType.Range) { Child = label };
        var item = new AvonicItem { Content = range };
        using var scene = InScene(item);

        Assert.False(Has(item, ":item-label"));

        label.Position = LabelPosition.Floating;
        Dispatcher.UIThread.RunJobs();

        Assert.False(Has(item, ":item-label-floating"));
    }

    [AvaloniaFact]
    public void B6_Inside_Another_Control_It_Still_Tells_The_Item()
    {
        var item = new AvonicItem { Content = new Stub(ItemControlType.Toggle) { Child = new AvonicLabel { Content = "x" } } };
        using var scene = InScene(item);

        Assert.True(Has(item, ":item-label"));
    }

    // ── 7. Margins ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_Margins_In_Each_Slot() => InstalledTheme.Run(_ =>
    {
        var main = new AvonicLabel { Content = "x" };
        var start = new AvonicLabel { Content = "x" };
        var end = new AvonicLabel { Content = "x" };
        using var scene = InScene(new AvonicItem { Content = main, StartContent = start, EndContent = end });

        // Measured: "0px 10px 0px 10px", "0px 10px 16px 10px" and "16px 10px 0px 10px".
        Assert.Equal(new Thickness(0, 10, 0, 10), main.Margin);
        Assert.Equal(new Thickness(0, 10, 16, 10), start.Margin);
        Assert.Equal(new Thickness(16, 10, 0, 10), end.Margin);
    });

    [AvaloniaFact]
    public void B7_No_Margin_Alone() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        using var scene = InScene(label);

        Assert.Equal(default, label.Margin);
    });

    [AvaloniaTheory]
    [InlineData("toggle")]
    [InlineData("radio")]
    public void B7_A_Toggle_Or_Radio_Item_Has_No_Margin_Start_On_An_End_Label(string key)
    {
        InstalledTheme.Run(_ =>
        {
            var end = new AvonicLabel { Content = "x" };
            var child = new Border();
            using var scene = InScene(new AvonicItem { Content = child, EndContent = end });

            Raise(child, (key, true));

            Assert.Equal(new Thickness(0, 10, 0, 10), end.Margin);
        });
    }

    [AvaloniaFact]
    public void B7_A_Panel_Stands_For_The_Slot_But_Deeper_Does_Not_Match() => InstalledTheme.Run(_ =>
    {
        var inPanel = new AvonicLabel { Content = "x" };
        var deeper = new AvonicLabel { Content = "x" };
        var nested = new AvonicLabel { Content = "x" };
        using var scene = InScene(
            new AvonicItem { Content = new StackPanel { Children = { inPanel } } },
            new AvonicItem { Content = new StackPanel { Children = { new StackPanel { Children = { deeper } } } } },
            new AvonicItem { Content = new Border { Child = nested } });

        Assert.Equal(new Thickness(0, 10, 0, 10), inPanel.Margin);
        Assert.Equal(default, deeper.Margin);
        Assert.Equal(default, nested.Margin);
    });

    // ── 8. Wrapping ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_Long_Text_Wraps_And_Grows_The_Item() => InstalledTheme.Run(_ =>
    {
        var single = new AvonicLabel { Content = "x" };
        var label = new AvonicLabel { Content = Long };
        var item = new AvonicItem { Content = label };
        using var scene = InScene(new AvonicItem { Content = single }, item);

        Assert.True(label.Bounds.Height >= single.Bounds.Height * 2);
        Assert.Equal(358, label.Bounds.Width);
        // Measured: 54 + 20 of margin + 1 of border = 75.
        Assert.Equal(label.Bounds.Height + 21, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void B8_Long_Text_Wraps_Alone() => InstalledTheme.Run(_ =>
    {
        var single = new AvonicLabel { Content = "x" };
        var label = new AvonicLabel { Content = Long };
        using var scene = InScene(single, label);

        Assert.True(label.Bounds.Height >= single.Bounds.Height * 2);
    });

    [AvaloniaFact]
    public void B8_A_TextBlock_Inside_Wraps_Too()
    {
        var single = new TextBlock { Text = "x" };
        var label = new AvonicLabel { Content = new TextBlock { Text = Long } };
        using var scene = InScene(single, label);

        Assert.True(label.Bounds.Height >= single.Bounds.Height * 2);
    }

    // ── 9. .ion-text-nowrap ──────────────────────────────────────────────────────────────────

    private static AvonicLabel WithClass(string @class, object content)
    {
        var label = new AvonicLabel { Content = content };
        label.Classes.Add(@class);
        return label;
    }

    [AvaloniaFact]
    public void B9_Nowrap_Is_One_Line_With_An_Ellipsis_And_Clipped() => InstalledTheme.Run(_ =>
    {
        var single = new AvonicLabel { Content = "x" };
        var label = WithClass("ion-text-nowrap", Long);
        var item = new AvonicItem { Content = label };
        using var scene = InScene(new AvonicItem { Content = single }, item);

        Assert.Equal(single.Bounds.Height, label.Bounds.Height);
        Assert.Equal(48, item.Bounds.Height);
        Assert.Equal(358, label.Bounds.Width);
        Assert.Equal(TextTrimming.CharacterEllipsis, Text(label).TextTrimming);
        Assert.True(label.ClipToBounds);
    });

    [AvaloniaFact]
    public void B9_Nowrap_Reaches_Headings_And_Paragraphs() => InstalledTheme.Run(_ =>
    {
        var h2 = new TextBlock { Text = Long, Classes = { "h2" } };
        var p = new TextBlock { Text = Long, Classes = { "p" } };
        var label = WithClass("ion-text-nowrap", new StackPanel { Children = { h2, p } });
        using var scene = InScene(new AvonicItem { Content = label });

        foreach (var block in new[] { h2, p })
        {
            Assert.Equal(TextWrapping.NoWrap, block.TextWrapping);
            Assert.Equal(TextTrimming.CharacterEllipsis, block.TextTrimming);
        }

        // Measured: 19.19 + 2 + 2, and 20 + 2, which is 45.19. Layout rounding makes the 19.2 a 20.
        Assert.InRange(label.Bounds.Height, 45, 46);
    });

    [AvaloniaFact]
    public void B9_Nowrap_Alone_Does_Not_Wrap_And_Has_No_Ellipsis() => InstalledTheme.Run(_ =>
    {
        var label = WithClass("ion-text-nowrap", Long);
        using var scene = InScene(label);

        Assert.Equal(TextWrapping.NoWrap, Text(label).TextWrapping);
        Assert.Equal(TextTrimming.None, Text(label).TextTrimming);
        Assert.False(label.ClipToBounds);
    });

    [AvaloniaFact]
    public void B9_Without_The_Class_There_Is_No_Ellipsis() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = Long };
        using var scene = InScene(new AvonicItem { Content = label });

        Assert.Equal(TextTrimming.None, Text(label).TextTrimming);
        Assert.False(label.ClipToBounds);
    });

    // ── 10. .ion-text-wrap ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_Wrap_Is_Line_Height_1_5_Of_Each_Elements_Own_Size() => InstalledTheme.Run(_ =>
    {
        var plain = new TextBlock { Text = "x" };
        var big = new TextBlock { Text = "x", FontSize = 20 };
        var label = WithClass("ion-text-wrap", new StackPanel { Children = { plain, big } });
        using var scene = InScene(new AvonicItem { Content = label });

        Assert.Equal(24, plain.LineHeight);
        Assert.Equal(30, big.LineHeight);
    });

    [AvaloniaFact]
    public void B10_Wrap_Grows_By_Lines_Of_24() => InstalledTheme.Run(_ =>
    {
        var label = WithClass("ion-text-wrap", Long);
        using var scene = InScene(new AvonicItem { Content = label });

        // Measured: 72 for three lines.
        Assert.Equal(0, label.Bounds.Height % 24, 3);
        Assert.True(label.Bounds.Height >= 48);
    });

    [AvaloniaFact]
    public void B10_Headings_And_Paragraphs_Keep_Their_Own_Line_Height() => InstalledTheme.Run(_ =>
    {
        var h2 = new TextBlock { Text = "x", Classes = { "h2" } };
        var h3 = new TextBlock { Text = "x", Classes = { "h3" } };
        var p = new TextBlock { Text = "x", Classes = { "p" } };
        var label = WithClass("ion-text-wrap", new StackPanel { Children = { h2, h3, p } });
        using var scene = InScene(new AvonicItem { Content = label });

        Assert.Equal(19.2, h2.LineHeight);
        Assert.True(double.IsNaN(h3.LineHeight));
        Assert.Equal(20, p.LineHeight);
    });

    [AvaloniaFact]
    public void B10_Without_The_Class_Line_Height_Is_Normal() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        using var scene = InScene(new AvonicItem { Content = label });

        Assert.True(double.IsNaN(Text(label).LineHeight));
    });

    // ── 11. Headings and paragraphs ──────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData("h1", 24, 0, 2, 28.8)]
    [InlineData("h2", 16, 2, 2, 19.2)]
    [InlineData("h3", 14, 2, 2, double.NaN)]
    [InlineData("h4", 14, 2, 2, double.NaN)]
    [InlineData("h5", 14, 2, 2, double.NaN)]
    [InlineData("h6", 14, 2, 2, double.NaN)]
    [InlineData("p", 14, 0, 2, 20)]
    public void B11_Each_Element_Has_Its_Measured_Values(string element, double size, double top, double bottom, double lineHeight)
    {
        InstalledTheme.Run(_ =>
        {
            var block = new TextBlock { Text = "x", Classes = { element } };
            var parent = new AvonicLabel { Content = block };
            using var scene = InScene(new AvonicItem { Content = parent });

            Assert.Equal(size, block.FontSize);
            Assert.Equal(new Thickness(0, top, 0, bottom), block.Margin);
            Assert.Equal(lineHeight, block.LineHeight);
            Assert.Equal(FontWeight.Normal, block.FontWeight);
        });
    }

    [AvaloniaTheory]
    [InlineData("h1")]
    [InlineData("h2")]
    [InlineData("h3")]
    [InlineData("h6")]
    public void B11_Headings_Are_Not_Bold_Even_If_The_Parent_Is(string element)
    {
        InstalledTheme.Run(_ =>
        {
            var block = new TextBlock { Text = "x", Classes = { element } };
            var label = new AvonicLabel { Content = block, FontWeight = FontWeight.Bold };
            using var scene = InScene(label);

            Assert.Equal(FontWeight.Normal, block.FontWeight);
        });
    }

    [AvaloniaFact]
    public void B11_Line_Height_Does_Not_Clip_Descenders() => InstalledTheme.Run(_ =>
    {
        // A line height far below the font's height, as h1's and h2's can be for some fonts.
        var block = new TextBlock { Text = "ggg", Classes = { "p" }, LineHeight = 4 };
        var item = new AvonicItem { Content = new AvonicLabel { Content = block }, Lines = ItemLines.None };
        using var scene = InScene(item);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();

        var frame = scene.Window.CaptureRenderedFrame()!;
        var box = Box(block, scene.Window);
        var (left, _) = InkRange(frame, (int)box.Bottom + 1, (int)item.Bounds.Height);

        Assert.NotEqual(int.MaxValue, left);
    });

    [AvaloniaFact]
    public void B11_The_Rules_Reach_Any_Depth() => InstalledTheme.Run(_ =>
    {
        var block = new TextBlock { Text = "x", Classes = { "h2" } };
        var label = new AvonicLabel { Content = new Border { Child = new Border { Child = block } } };
        using var scene = InScene(label);

        Assert.Equal(19.2, block.LineHeight);
    });

    [AvaloniaFact]
    public void B11_Outside_A_Label_They_Do_Nothing() => InstalledTheme.Run(_ =>
    {
        var block = new TextBlock { Text = "x", Classes = { "h1" } };
        using var scene = InScene(new AvonicItem { Content = block });

        Assert.Equal(16, block.FontSize);
    });

    // ── 12, 13. A paragraph's colour ─────────────────────────────────────────────────────────

    private static TextBlock Paragraph() => new() { Text = "x", Classes = { "p" } };

    [AvaloniaFact]
    public void B12_A_Direct_P_Is_Grey() => InstalledTheme.Run(_ =>
    {
        var p = Paragraph();
        using var scene = InScene(new AvonicItem { Content = new AvonicLabel { Content = p } });

        Assert.Equal(Grey, TextColor(p));
    });

    [AvaloniaFact]
    public void B12_A_P_Alone_Is_Grey() => InstalledTheme.Run(_ =>
    {
        var p = Paragraph();
        using var scene = InScene(new AvonicLabel { Content = p });

        Assert.Equal(Grey, TextColor(p));
    });

    [AvaloniaFact]
    public void B12_A_P_In_A_Panel_Is_Grey_As_The_Panel_Stands_For_The_Slot() => InstalledTheme.Run(_ =>
    {
        var p = Paragraph();
        using var scene = InScene(new AvonicItem { Content = new AvonicLabel { Content = new StackPanel { Children = { p } } } });

        Assert.Equal(Grey, TextColor(p));
    });

    [AvaloniaFact]
    public void B12_A_P_Nested_Deeper_Inherits() => InstalledTheme.Run(_ =>
    {
        var p = Paragraph();
        var deeper = Paragraph();
        using var scene = InScene(
            new AvonicItem { Content = new AvonicLabel { Content = new Border { Child = p } } },
            new AvonicItem { Content = new AvonicLabel { Content = new StackPanel { Children = { new StackPanel { Children = { deeper } } } } } });

        Assert.Equal(Colors.Black, TextColor(p));
        Assert.Equal(Colors.Black, TextColor(deeper));
    });

    [AvaloniaFact]
    public void B12_A_Heading_Is_Not_Grey() => InstalledTheme.Run(_ =>
    {
        var h2 = new TextBlock { Text = "x", Classes = { "h2" } };
        using var scene = InScene(new AvonicItem { Content = new AvonicLabel { Content = h2 } });

        Assert.Equal(Colors.Black, TextColor(h2));
    });

    [AvaloniaFact]
    public void B13_A_P_In_A_Coloured_Item_Inherits() => InstalledTheme.Run(_ =>
    {
        var p = Paragraph();
        var inPanel = Paragraph();
        using var scene = InScene(
            new AvonicItem { Color = "primary", Content = new AvonicLabel { Content = p } },
            new AvonicItem { Color = "primary", Content = new AvonicLabel { Content = new StackPanel { Children = { inPanel } } } });

        Assert.Equal(Colors.White, TextColor(p));
        Assert.Equal(Colors.White, TextColor(inPanel));
    });

    [AvaloniaFact]
    public void B13_A_P_In_A_Coloured_Label_In_A_Coloured_Item_Takes_The_Labels_Colour() => InstalledTheme.Run(_ =>
    {
        var p = Paragraph();
        using var scene = InScene(new AvonicItem { Color = "primary", Content = new AvonicLabel { Content = p, Color = "danger" } });

        Assert.Equal(Danger, TextColor(p));
    });

    [AvaloniaFact]
    public void B13_A_P_In_A_Coloured_Label_In_A_Plain_Item_Stays_Grey() => InstalledTheme.Run(_ =>
    {
        var p = Paragraph();
        using var scene = InScene(new AvonicItem { Content = new AvonicLabel { Content = p, Color = "danger" } });

        Assert.Equal(Grey, TextColor(p));
    });

    // ── 14. Fixed ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_Fixed_Is_100_Wide_At_The_Start() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "Fixed", Position = LabelPosition.Fixed };
        var item = new AvonicItem { Content = label };
        using var scene = InScene(item);

        // Measured: x = 16, width = 100, in a 48 row, with its 10s of margin.
        var box = Box(label, scene.Window);
        Assert.Equal(16, box.X);
        Assert.Equal(100, box.Width);
        Assert.Equal(new Thickness(0, 10, 0, 10), label.Margin);
        Assert.Equal(48, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void B14_Fixed_Is_At_The_Start_In_RTL() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "Fixed", Position = LabelPosition.Fixed };
        using var scene = InScene(new AvonicItem { Content = label, FlowDirection = FlowDirection.RightToLeft });

        // Measured: x = 274, width = 100, so its start edge is at 374.
        Assert.Equal(374, Box(label, scene.Window).X);
    });

    [AvaloniaFact]
    public void B14_Fixed_Wraps_Long_Text() => InstalledTheme.Run(_ =>
    {
        var single = new AvonicLabel { Content = "x", Position = LabelPosition.Fixed };
        var label = new AvonicLabel { Content = Long, Position = LabelPosition.Fixed };
        using var scene = InScene(new AvonicItem { Content = single }, new AvonicItem { Content = label });

        Assert.Equal(100, label.Bounds.Width);
        Assert.True(label.Bounds.Height >= single.Bounds.Height * 5);
    });

    [AvaloniaFact]
    public void B14_Fixed_Has_Its_Limits() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Fixed };
        using var scene = InScene(new AvonicItem { Content = label });

        Assert.Equal(100, label.MinWidth);
        Assert.Equal(200, label.MaxWidth);
    });

    // ── 15, 16. Stacked ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_Stacked_Is_Shrunk_And_Moved_Down_By_Half_Its_Height() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "Stacked", Position = LabelPosition.Stacked };
        var item = new AvonicItem { Content = label };
        using var scene = InScene(item);

        // Measured with no sibling: matrix(0.75, 0, 0, 0.75, 0, 27), at x = 16 and y = 27, in a 55 row.
        var matrix = Transform(label);
        Assert.Equal(0.75, matrix.M11, 3);
        Assert.Equal(0.75, matrix.M22, 3);
        Assert.Equal(27, matrix.M32, 3);
        Assert.Equal(55, item.Bounds.Height);

        var topLeft = label.TranslatePoint(default, scene.Window)!.Value;
        Assert.Equal(16, topLeft.X, 3);
        Assert.Equal(27, topLeft.Y, 3);

        var bottomRight = label.TranslatePoint(new Point(label.Bounds.Width, label.Bounds.Height), scene.Window)!.Value;
        Assert.Equal(16 + 268.5, bottomRight.X, 3);
        Assert.Equal(27 + 40.5, bottomRight.Y, 3);
    });

    [AvaloniaFact]
    public void B15_Stacked_Has_No_Margin_In_Any_Slot() => InstalledTheme.Run(_ =>
    {
        var main = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        var end = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        using var scene = InScene(new AvonicItem { Content = main }, new AvonicItem { Content = new Border(), EndContent = end });

        Assert.Equal(default, main.Margin);
        Assert.Equal(default, end.Margin);
    });

    [AvaloniaFact]
    public void B15_Stacked_Eases_Its_Colour_Only() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        using var scene = InScene(new AvonicItem { Content = label });

        var transition = Assert.IsType<BrushTransition>(Assert.Single(label.Transitions!));
        Assert.Equal(TemplatedControl.ForegroundProperty, transition.Property);
        Assert.Equal(TimeSpan.FromMilliseconds(150), transition.Duration);
    });

    [AvaloniaFact]
    public void B15_A_Plain_Label_Has_No_Transitions_Or_Transform() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        using var scene = InScene(new AvonicItem { Content = label });

        Assert.Empty(label.Transitions ?? []);
        Assert.Null(label.RenderTransform);
    });

    [AvaloniaFact]
    public void B16_Stacked_Fills_The_Items_Content_Height() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        using var scene = InScene(new AvonicItem { Content = label });

        // Measured: the wrapper and the label are 54, in a 55 row with a 1 px border.
        Assert.Equal(54, label.Bounds.Height);
        Assert.Equal(358, label.Bounds.Width);
    });

    [AvaloniaFact]
    public void B16_With_A_Sibling_A_Star_Row_Gives_What_Flex_Gives() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        var input = new Border { Height = 24 };
        Grid.SetRow(input, 1);
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { label, input } };
        using var scene = InScene(new AvonicItem { Content = grid });

        // Measured: the label is 30 beside a 24 input, moved down by 15.
        Assert.Equal(30, label.Bounds.Height);
        Assert.Equal(15, Transform(label).M32, 3);
    });

    [AvaloniaFact]
    public void B16_The_Transform_Follows_The_Height() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        var filler = new Border { Height = 0 };
        Grid.SetRow(filler, 1);
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { label, filler } };
        using var scene = InScene(new AvonicItem { Content = grid });
        Assert.Equal(27, Transform(label).M32, 3);

        filler.Height = 14;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(20, Transform(label).M32, 3);
    });

    [AvaloniaFact]
    public void B16_It_Does_Not_Stretch_Other_Items() => InstalledTheme.Run(_ =>
    {
        var content = new Border { Height = 10 };
        using var scene = InScene(new AvonicItem { Content = content });

        // Vertically centred in 47, where layout rounding makes the 18.5 an 18.
        Assert.InRange(Box(content, scene.Window).Y, 18, 19);
    });

    [AvaloniaFact]
    public void B16_Other_Content_In_A_Stacked_Item_Stays_At_The_Top() => InstalledTheme.Run(_ =>
    {
        var content = new Border { Height = 10, VerticalAlignment = VerticalAlignment.Top };
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        var panel = new StackPanel { Children = { label, content } };
        using var scene = InScene(new AvonicItem { Content = panel });

        Assert.Equal(0, Box(panel, scene.Window).Y);
    });

    // ── 17, 18, 19. Floating ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_Floating_Moves_Down_By_96_Percent_At_Full_Size() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Floating };
        var filler = new Border { Height = 24 };
        Grid.SetRow(filler, 1);
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { label, filler } };
        var item = new AvonicItem { Content = grid };
        using var scene = InScene(item);

        // Measured: matrix(1, 0, 0, 1, 0, 28.8) for a 30 high label, in a 55 row.
        var matrix = Transform(label);
        Assert.Equal(1, matrix.M11, 3);
        Assert.Equal(1, matrix.M22, 3);
        Assert.Equal(28.8, matrix.M32, 3);
        Assert.Equal(55, item.Bounds.Height);
        Assert.Equal(default, label.Margin);
    });

    [AvaloniaFact]
    public void B17_Floating_Eases_Colour_And_Transform_After_Its_First_Second() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Floating };
        using var scene = InScene(new AvonicItem { Content = label });

        clock.At(1000);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, label.Transitions!.Count);
        Assert.Contains(label.Transitions, transition => transition is BrushTransition { Duration.TotalMilliseconds: 150 });
        var transform = Assert.IsType<TransformOperationsTransition>(label.Transitions.Single(transition => transition is TransformOperationsTransition));
        Assert.Equal(Visual.RenderTransformProperty, transform.Property);
        Assert.Equal(TimeSpan.FromMilliseconds(150), transform.Duration);
    });

    private static (AvonicItem Item, AvonicLabel Label, Border Child) FloatingItem()
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Floating };
        var child = new Border { Height = 24 };
        Grid.SetRow(child, 1);
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { label, child } };
        return (new AvonicItem { Content = grid }, label, child);
    }

    private static void AssertRaised(AvonicLabel label)
    {
        var matrix = Transform(label);
        Assert.Equal(0.75, matrix.M11, 3);
        Assert.Equal(15, matrix.M32, 3);
    }

    private static void AssertLowered(AvonicLabel label)
    {
        var matrix = Transform(label);
        Assert.Equal(1, matrix.M11, 3);
        Assert.Equal(28.8, matrix.M32, 3);
    }

    [AvaloniaFact]
    public void B18_Keyboard_Focus_On_The_Item_Raises_Floating() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (item, label, _) = FloatingItem();
        using var scene = InScene(item);
        NoTransitions(label);
        AssertLowered(label);

        SetState(item, ":ion-focused");
        AssertRaised(label);

        SetState(item, ":ion-focused", false);
        AssertLowered(label);
    });

    [AvaloniaTheory]
    [InlineData("has-focus")]
    [InlineData("has-value")]
    [InlineData("has-placeholder")]
    public void B18_What_A_Child_Reports_Raises_Floating(string key)
    {
        InstalledTheme.Run(_ =>
        {
            using var clock = new TestClock();
            var (item, label, child) = FloatingItem();
            using var scene = InScene(item);
            NoTransitions(label);

            Raise(child, (key, true));
            AssertRaised(label);

            Raise(child, (key, false));
            AssertLowered(label);
        });
    }

    [AvaloniaFact]
    public void B18_A_Placeholder_In_An_Input_Item_Does_Not_Raise_Floating() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (item, label, child) = FloatingItem();
        using var scene = InScene(item);
        NoTransitions(label);

        Raise(child, ("has-placeholder", true), ("input", true));

        Assert.Equal(1, Transform(label).M11, 3);
    });

    [AvaloniaFact]
    public void B18_Stacked_Does_Not_Move_With_Any_Of_These() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        var child = new Border();
        var item = new AvonicItem { Content = new Grid { Children = { label, child } } };
        using var scene = InScene(item);
        var before = Transform(label);

        Raise(child, ("has-value", true));
        SetState(item, ":ion-focused");

        Assert.Equal(before, Transform(label));
    });

    [AvaloniaFact]
    public void B19_No_Transitions_For_The_First_Second_If_Floating_At_Load() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (item, label, _) = FloatingItem();
        using var scene = InScene(item);

        Assert.True(Has(label, ":label-no-animate"));
        Assert.Empty(label.Transitions ?? []);

        clock.At(999);
        Dispatcher.UIThread.RunJobs();
        Assert.True(Has(label, ":label-no-animate"));

        clock.At(1000);
        Dispatcher.UIThread.RunJobs();
        Assert.False(Has(label, ":label-no-animate"));
        Assert.NotEmpty(label.Transitions!);
    });

    [AvaloniaFact]
    public void B19_In_The_First_Second_It_Jumps_And_After_It_Eases() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (item, label, _) = FloatingItem();
        using var scene = InScene(item);

        SetState(item, ":ion-focused");
        Assert.Equal(0.75, Transform(label).M11, 3);
        SetState(item, ":ion-focused", false);
        Assert.Equal(1, Transform(label).M11, 3);

        clock.At(1000);
        Dispatcher.UIThread.RunJobs();

        SetState(item, ":ion-focused");

        // Still on its way: the easing runs on real time, and none has passed.
        Assert.True(Transform(label).M11 > 0.75);
    });

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData(LabelPosition.Fixed)]
    [InlineData(LabelPosition.Stacked)]
    public void B19_Not_Floating_At_Load_It_Does_Not_Start(LabelPosition? position)
    {
        InstalledTheme.Run(_ =>
        {
            using var clock = new TestClock();
            var label = new AvonicLabel { Content = "x", Position = position };
            using var scene = InScene(new AvonicItem { Content = label });
            Assert.False(Has(label, ":label-no-animate"));

            // Measured: changing to floating later eases straight away.
            label.Position = LabelPosition.Floating;
            Dispatcher.UIThread.RunJobs();

            Assert.False(Has(label, ":label-no-animate"));
            Assert.Equal(2, label.Transitions!.Count);
        });
    }

    [AvaloniaFact]
    public void B19_A_Timer_Does_Not_Outlive_Its_Label() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (item, label, _) = FloatingItem();
        var panel = new StackPanel { Children = { item } };
        using var scene = new Scene(panel);

        panel.Children.Clear();
        Dispatcher.UIThread.RunJobs();

        // Deliberate: Ionic leaves the state on for good here. Attached again, it starts no new window.
        Assert.False(Has(label, ":label-no-animate"));

        panel.Children.Add(item);
        Dispatcher.UIThread.RunJobs();

        Assert.False(Has(label, ":label-no-animate"));
        Assert.NotEmpty(label.Transitions!);
    });

    [AvaloniaFact]
    public void B19_The_Window_Is_Only_On_Floating() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (item, label, _) = FloatingItem();
        using var scene = InScene(item);

        label.Position = LabelPosition.Stacked;
        Dispatcher.UIThread.RunJobs();

        // :host(.label-no-animate.label-floating): the state's still on, but only for floating.
        Assert.True(Has(label, ":label-no-animate"));
        Assert.Single(label.Transitions!);
    });

    // ── 20, 21. Focus and validity colours ───────────────────────────────────────────────────

    public enum FocusKind
    {
        Keyboard,
        Reported,
    }

    private static void Focus(AvonicItem item, Control child, FocusKind kind)
    {
        if (kind == FocusKind.Keyboard)
        {
            SetState(item, ":ion-focused");
        }
        else
        {
            Raise(child, ("has-focus", true));
        }
    }

    private static (AvonicItem Item, AvonicLabel Label, Control Child) WithLabel(LabelPosition? position, string? itemColor = null, string? labelColor = null)
    {
        var label = NoTransitions(new AvonicLabel { Content = "x", Position = position, Color = labelColor });
        var child = new Border();
        var item = new AvonicItem { Content = new Grid { Children = { label, child } }, Color = itemColor };
        return (item, label, child);
    }

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked, FocusKind.Keyboard)]
    [InlineData(LabelPosition.Stacked, FocusKind.Reported)]
    [InlineData(LabelPosition.Floating, FocusKind.Keyboard)]
    [InlineData(LabelPosition.Floating, FocusKind.Reported)]
    public void B20_Focused_Stacked_Or_Floating_Is_Primary(LabelPosition position, FocusKind kind)
    {
        InstalledTheme.Run(_ =>
        {
            var (item, label, child) = WithLabel(position);
            using var scene = InScene(item);

            Focus(item, child, kind);

            Assert.Equal(PrimaryBase, TextColor(Text(label)));
        });
    }

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData(LabelPosition.Fixed)]
    public void B20_A_Plain_Or_Fixed_Label_Does_Not_Change(LabelPosition? position)
    {
        InstalledTheme.Run(_ =>
        {
            var (item, label, child) = WithLabel(position);
            using var scene = InScene(item);

            Focus(item, child, FocusKind.Keyboard);
            Focus(item, child, FocusKind.Reported);

            Assert.Equal(Colors.Black, TextColor(label));
        });
    }

    [AvaloniaFact]
    public void B20_Focus_Beats_The_Token() => InstalledTheme.Run(_ =>
    {
        var (item, label, child) = WithLabel(LabelPosition.Stacked);
        using var scene = InScene(item);
        scene.Window.Resources["Avonic.Label.Color"] = new SolidColorBrush(Token);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Token, TextColor(label));

        Focus(item, child, FocusKind.Keyboard);

        Assert.Equal(PrimaryBase, TextColor(label));
    });

    [AvaloniaTheory]
    [InlineData(FocusKind.Keyboard)]
    [InlineData(FocusKind.Reported)]
    public void B20_In_A_Coloured_Item_Focus_Is_Contrast(FocusKind kind)
    {
        InstalledTheme.Run(_ =>
        {
            var (item, label, child) = WithLabel(LabelPosition.Stacked, itemColor: "danger");
            using var scene = InScene(item);

            Focus(item, child, kind);

            Assert.Equal(Colors.White, TextColor(label));
        });
    }

    [AvaloniaFact]
    public void B20_In_An_Unknown_Coloured_Item_Focus_Inherits() => InstalledTheme.Run(_ =>
    {
        var (item, label, child) = WithLabel(LabelPosition.Stacked, itemColor: "nonexistent");
        using var scene = InScene(item);

        Focus(item, child, FocusKind.Keyboard);

        Assert.Equal(Colors.Black, TextColor(label));
    });

    private static (AvonicItem Item, AvonicLabel Label) BehindAPanel(string itemColor)
    {
        // Between the item and the label, so inheriting differs from the item's own colours.
        var label = NoTransitions(new AvonicLabel { Content = "x", Position = LabelPosition.Stacked });
        var panel = new Grid { Children = { label } };
        TextElement.SetForeground(panel, new SolidColorBrush(Success));
        return (new AvonicItem { Content = panel, Color = itemColor }, label);
    }

    [AvaloniaFact]
    public void B20_In_A_Coloured_Item_Focus_Is_The_Contrast_Not_What_It_Inherits() => InstalledTheme.Run(_ =>
    {
        var (item, label) = BehindAPanel("danger");
        using var scene = InScene(item);
        Assert.Equal(Success, TextColor(label));

        SetState(item, ":ion-focused");

        Assert.Equal(Colors.White, TextColor(label));
    });

    [AvaloniaFact]
    public void B20_In_An_Unknown_Coloured_Item_Focus_Inherits_What_Is_Nearest() => InstalledTheme.Run(_ =>
    {
        var (item, label) = BehindAPanel("nonexistent");
        using var scene = InScene(item);

        SetState(item, ":ion-focused");

        Assert.Equal(Success, TextColor(label));
    });

    [AvaloniaFact]
    public void B20_A_Labels_Own_Color_Wins() => InstalledTheme.Run(_ =>
    {
        var (item, label, child) = WithLabel(LabelPosition.Stacked, labelColor: "success");
        using var scene = InScene(item);

        Focus(item, child, FocusKind.Keyboard);

        Assert.Equal(Success, TextColor(label));
    });

    [AvaloniaFact]
    public void B20_A_Colour_Change_Eases() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        var item = new AvonicItem { Content = label };
        using var scene = InScene(item);

        SetState(item, ":ion-focused");

        // Still on its way: the easing runs on real time, and none has passed.
        Assert.NotEqual(PrimaryBase, TextColor(label));
    });

    private static void Invalid(AvonicItem item)
    {
        item.Classes.Add("ion-invalid");
        item.Classes.Add("ion-touched");
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void B21_Invalid_And_Touched_Inherits_Beating_Focus(LabelPosition position)
    {
        InstalledTheme.Run(_ =>
        {
            var (item, label, child) = WithLabel(position);
            using var scene = InScene(item);

            Focus(item, child, FocusKind.Keyboard);
            Assert.Equal(PrimaryBase, TextColor(label));

            Invalid(item);

            Assert.Equal(Colors.Black, TextColor(label));
        });
    }

    [AvaloniaFact]
    public void B21_Invalid_And_Touched_Beats_The_Token() => InstalledTheme.Run(_ =>
    {
        var (item, label, _) = WithLabel(LabelPosition.Stacked);
        using var scene = InScene(item);
        scene.Window.Resources["Avonic.Label.Color"] = new SolidColorBrush(Token);

        Invalid(item);

        Assert.Equal(Colors.Black, TextColor(label));
    });

    [AvaloniaFact]
    public void B21_Invalid_Alone_Does_Nothing() => InstalledTheme.Run(_ =>
    {
        var (item, label, child) = WithLabel(LabelPosition.Stacked);
        using var scene = InScene(item);

        item.Classes.Add("ion-invalid");
        Focus(item, child, FocusKind.Keyboard);

        Assert.Equal(PrimaryBase, TextColor(label));
    });

    [AvaloniaFact]
    public void B21_A_Plain_Label_Keeps_The_Token_When_Invalid() => InstalledTheme.Run(_ =>
    {
        var (item, label, _) = WithLabel(null);
        using var scene = InScene(item);
        scene.Window.Resources["Avonic.Label.Color"] = new SolidColorBrush(Token);

        Invalid(item);

        Assert.Equal(Token, TextColor(label));
    });

    [AvaloniaFact]
    public void B21_A_Labels_Own_Color_Wins_When_Invalid() => InstalledTheme.Run(_ =>
    {
        var (item, label, _) = WithLabel(LabelPosition.Stacked, labelColor: "success");
        using var scene = InScene(item);

        Invalid(item);

        Assert.Equal(Success, TextColor(label));
    });

    // ── 22, 23, 24. What a child reports ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B22_Interactive_Disabled_Dims_And_Takes_No_Input() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var child = new Border();
        using var scene = InScene(new AvonicItem { Content = new StackPanel { Children = { label, child } } });
        Assert.Equal(1, label.Opacity);
        Assert.True(label.IsHitTestVisible);
        Assert.Null(label.Cursor);

        Raise(child, ("interactive-disabled", true));

        Assert.Equal(0.3, label.Opacity);
        Assert.False(label.IsHitTestVisible);
        Assert.Equal(new Cursor(StandardCursorType.Arrow).ToString(), label.Cursor?.ToString());
    });

    [AvaloniaFact]
    public void B22_With_Multiple_Inputs_It_Does_Not() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var first = new Stub(ItemControlType.Toggle);
        var second = new Stub(ItemControlType.Toggle);
        var item = new AvonicItem { Content = new StackPanel { Children = { label, first } }, EndContent = second };
        using var scene = InScene(item);
        Assert.True(Has(item, ":item-multiple-inputs"));

        Raise(first, ("interactive-disabled", true));

        Assert.Equal(1, label.Opacity);
        Assert.True(label.IsHitTestVisible);
    });

    [AvaloniaFact]
    public void B23_An_Input_Item_Limits_The_Label_And_Takes_Input_Away() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = Long };
        var child = new Border();
        using var scene = InScene(new AvonicItem { Content = new StackPanel { Children = { label, child } } });

        Raise(child, ("input", true));

        Assert.True(label.Bounds.Width <= 200);
        Assert.Equal(16, Box(label, scene.Window).X);
        Assert.False(label.IsHitTestVisible);
    });

    [AvaloniaFact]
    public void B23_A_Tap_On_It_Goes_Through_To_The_Item() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var toggle = new Stub(ItemControlType.Toggle);
        var item = new AvonicItem { Content = label, EndContent = toggle };
        using var scene = InScene(item);

        Raise(label, ("input", true));
        Tap(scene, new Point(20, 24));

        Assert.Equal(1, toggle.ItemTaps);
    });

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void B23_Stacked_Or_Floating_Has_No_Limit_And_Keeps_Its_Own_Height(LabelPosition position)
    {
        InstalledTheme.Run(_ =>
        {
            var label = new AvonicLabel { Content = "x", Position = position };
            var child = new Border();
            using var scene = InScene(new AvonicItem { Content = new Grid { Children = { label, child } } });

            Raise(child, ("input", true));

            // Measured: width 358, and a height of its own text, at the top of the item.
            Assert.Equal(358, label.Bounds.Width);
            Assert.True(label.Bounds.Height < 54);
            Assert.Equal(VerticalAlignment.Top, label.VerticalAlignment);
        });
    }

    [AvaloniaFact]
    public void B24_A_Textarea_Item_Puts_The_Label_At_The_Top() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var tall = new AvonicItem { Content = label, MinHeight = 100 };
        using var scene = InScene(tall);
        Assert.NotEqual(10, Box(label, scene.Window).Y);

        Raise(label, ("textarea", true));

        // Measured: its 10 of margin from the top of the row, where the others are centred, at its own height.
        Assert.Equal(10, Box(label, scene.Window).Y);
        Assert.True(label.Bounds.Height < 40);
    });

    [AvaloniaFact]
    public void B24_A_Skeleton_Item_Clips_The_Label() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        using var scene = InScene(new AvonicItem { Content = label });
        Assert.False(label.ClipToBounds);

        Raise(label, ("skeleton-text", true));

        Assert.True(label.ClipToBounds);
    });

    // ── 25, 26. Taps ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B25_A_Tap_On_The_Text_Does_Not_Forward() => InstalledTheme.Run(_ =>
    {
        var toggle = new Stub(ItemControlType.Toggle);
        var item = new AvonicItem { Content = new AvonicLabel { Content = "Label" }, EndContent = toggle };
        using var scene = InScene(item);

        Tap(scene, new Point(20, 24));

        Assert.Equal(0, toggle.ItemTaps);
    });

    [AvaloniaFact]
    public void B25_A_Tap_On_The_Blank_Part_Does_Not_Forward() => InstalledTheme.Run(_ =>
    {
        var toggle = new Stub(ItemControlType.Toggle);
        var item = new AvonicItem { Content = new AvonicLabel { Content = "L" }, EndContent = toggle };
        using var scene = InScene(item);

        Tap(scene, new Point(300, 24));

        Assert.Equal(0, toggle.ItemTaps);
    });

    [AvaloniaFact]
    public void B25_A_Tap_On_The_Padding_Forwards() => InstalledTheme.Run(_ =>
    {
        var toggle = new Stub(ItemControlType.Toggle);
        var item = new AvonicItem { Content = new AvonicLabel { Content = "Label" }, EndContent = toggle };
        using var scene = InScene(item);

        Tap(scene, new Point(6, 24));

        Assert.Equal(1, toggle.ItemTaps);
    });

    [AvaloniaFact]
    public void B26_A_Tap_On_A_Label_Clicks_A_Button_Item() => InstalledTheme.Run(_ =>
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = new AvonicLabel { Content = "Label" } };
        item.Click += (_, _) => clicks++;
        using var scene = InScene(item);

        Tap(scene, new Point(20, 24));

        Assert.Equal(1, clicks);
    });

    // ── 27. RTL ──────────────────────────────────────────────────────────────────────────────

    private static unsafe (int Left, int Right) InkRange(WriteableBitmap frame, int top, int bottom)
    {
        using var buffer = frame.Lock();
        var left = int.MaxValue;
        var right = -1;
        for (var y = top; y < bottom; y++)
        {
            var row = (uint*)(buffer.Address + y * buffer.RowBytes);
            for (var x = 0; x < frame.PixelSize.Width; x++)
            {
                if ((row[x] & 0xFFFFFF) != 0xFFFFFF)
                {
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                }
            }
        }

        return (left, right);
    }

    [AvaloniaFact]
    public void B27_In_RTL_Stacked_Shrinks_Towards_The_Right() => InstalledTheme.Run(_ =>
    {
        var rtl = new AvonicItem
        {
            Content = new AvonicLabel { Content = "RTL", Position = LabelPosition.Stacked },
            FlowDirection = FlowDirection.RightToLeft,
            Lines = ItemLines.None,
        };
        using var scene = InScene(rtl);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();

        var frame = scene.Window.CaptureRenderedFrame()!;
        var (left, right) = InkRange(frame, 0, 55);

        // Measured with the document right-to-left: 105.5 to 374. Shrunk from the left, it would end at 284.5.
        Assert.InRange(right, 360, 374);
        Assert.True(left > 284);
    });

    [AvaloniaFact]
    public void B27_In_LTR_Stacked_Shrinks_Towards_The_Left() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem
        {
            Content = new AvonicLabel { Content = "LTR", Position = LabelPosition.Stacked },
            Lines = ItemLines.None,
        };
        using var scene = InScene(item);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();

        var frame = scene.Window.CaptureRenderedFrame()!;
        var (left, right) = InkRange(frame, 0, 55);

        Assert.InRange(left, 16, 20);
        Assert.True(right < 100);
    });

    // ── 28. Automation ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B28_A_Button_Items_Name_Includes_Its_Labels_Text() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true, Content = new AvonicLabel { Content = "Settings" } };
        using var scene = InScene(item);

        Assert.Equal("Settings", ControlAutomationPeer.CreatePeerForElement(item).GetName());
    });

    [AvaloniaFact]
    public void B28_A_Button_Items_Name_Includes_Headings_And_Paragraphs() => InstalledTheme.Run(_ =>
    {
        var content = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = "Title", Classes = { "h2" } },
                new TextBlock { Text = "Detail", Classes = { "p" } },
            },
        };
        var item = new AvonicItem { Button = true, Content = new AvonicLabel { Content = content } };
        using var scene = InScene(item);

        Assert.Equal("Title Detail", ControlAutomationPeer.CreatePeerForElement(item).GetName());
    });
}
