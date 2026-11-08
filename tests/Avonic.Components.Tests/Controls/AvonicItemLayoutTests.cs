using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-item in Chromium, at 390 px wide, under "The flex layout" in docs/components/ion-item/PLAN.md.
/// Each is named after its row there. They don't depend on a font, except where a test says so, and then it's Roboto, from
/// tools/fonts/roboto/, on both sides.
/// </summary>
public class AvonicItemLayoutTests
{
    private const string Long = "A very long piece of text in a slot that certainly has to wrap, because it does not fit on one line at all";
    private const string Word = "Antidisestablishmentarianismlike";
    private const string VeryLongWord = "Antidisestablishmentarianismantidisestablishmentarianismantidisestablishmentarianism";

    private static AvonicLabel Label(string text = "Default") => new() { Content = text };

    private static AvonicNote Note(string text = Long) => new() { Content = text };

    private static Rect Box(Visual visual, Visual root) => new(visual.TranslatePoint(default, root)!.Value, visual.Bounds.Size);

    private static T Part<T>(Control host, string name)
        where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

    private static Border Inner(Control host) => Part<Border>(host, "PART_Inner");

    private static Scene InScene(Control host) => new(new StackPanel { Children = { host } });

    /// <summary>The theme's font, Roboto, that the labels are drawn in, or the default one without the theme, as the stubs are.</summary>
    private static FontFamily ThemeFont =>
        Application.Current!.TryFindResource("Avonic.FontFamily", out var font) && font is FontFamily family ? family : FontFamily.Default;

    private static double OneLine(string text, double fontSize = 16)
    {
        var block = new TextBlock { Text = text, FontSize = fontSize, FontFamily = ThemeFont };
        block.Measure(Size.Infinity);
        return block.DesiredSize.Width;
    }

    private static void AssertNear(double expected, double actual, double within = 1) =>
        Assert.InRange(actual, expected - within, expected + within);

    /// <summary>
    /// A note and AvonicText are aligned left, so once wrapped they're as wide as their longest line, where CSS's box is all
    /// of what's available. Nothing shows it: the text is in the same place. It's in the plan.
    /// </summary>
    private static void AssertWrapsWithin(double available, Control control)
    {
        Assert.InRange(control.Bounds.Width, available * 0.8, available);
        Assert.True(control.Bounds.Height > OneLineHeight());
    }

    [AvaloniaFact]
    public void F1_A_Start_Label_Takes_Half_The_Row() => InstalledTheme.Run(_ =>
    {
        var start = Label("Start");
        var content = Label();
        var item = new AvonicItem { StartContent = start, Content = content };
        using var scene = InScene(item);

        Assert.Equal(16, Box(start, item).X);
        Assert.Equal(171, start.Bounds.Width);
        Assert.Equal(new Rect(203, 0, 187, 48), Box(Inner(item), item));
        Assert.Equal(203, Box(content, item).X);
        Assert.Equal(171, content.Bounds.Width);
        Assert.Equal(48, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void F1_Beside_An_End_Label_It_Is_Still_Half() => InstalledTheme.Run(_ =>
    {
        var start = Label("Start");
        var content = Label();
        var end = Label("End");
        using var scene = InScene(new AvonicItem { StartContent = start, Content = content, EndContent = end });

        Assert.Equal(171, start.Bounds.Width);
        Assert.Equal(171, content.Bounds.Width + 16 + end.Bounds.Width, 0);
    });

    [AvaloniaFact]
    public void F2_A_Long_Start_Label_Wraps_At_Half_The_Row() => InstalledTheme.Run(_ =>
    {
        var start = Label(Long);
        var content = Label();
        var item = new AvonicItem { StartContent = start, Content = content };
        using var scene = InScene(item);

        Assert.Equal(171, start.Bounds.Width);
        Assert.True(start.Bounds.Height > 60);
        Assert.Equal(start.Bounds.Height + 20, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void F2_The_Default_Slot_Is_Centred_Beside_It() => InstalledTheme.Run(_ =>
    {
        var start = Label(Long);
        var content = Label();
        var item = new AvonicItem { StartContent = start, Content = content };
        using var scene = InScene(item);

        AssertNear((item.Bounds.Height - 1 - content.Bounds.Height) / 2, Box(content, item).Y);
    });

    [AvaloniaFact]
    public void F3_Alone_It_Is_Still_Half() => InstalledTheme.Run(_ =>
    {
        var start = Label("Start");
        var item = new AvonicItem { StartContent = start };
        using var scene = InScene(item);

        Assert.Equal(171, start.Bounds.Width);
        Assert.Equal(187, Inner(item).Bounds.Width);
    });

    [AvaloniaFact]
    public void F4_Fixed_In_Start_Is_100_Wide() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicLabel { Content = Long, Position = LabelPosition.Fixed };
        var item = new AvonicItem { StartContent = start, Content = Label() };
        using var scene = InScene(item);

        Assert.Equal(100, start.Bounds.Width);
        Assert.Equal(132, Box(Inner(item), item).X);
    });

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void F5_Stacked_Or_Floating_In_Start_Takes_Half_With_No_Margin(LabelPosition position) => InstalledTheme.Run(_ =>
    {
        var start = new AvonicLabel { Content = "Start", Position = position };
        var item = new AvonicItem { StartContent = start, Content = Label() };
        using var scene = InScene(item);

        Assert.Equal(179, start.Bounds.Width);
        Assert.Equal(195, Box(Inner(item), item).X);
    });

    [AvaloniaFact]
    public void F5_In_An_Input_Item_It_Is_As_Wide_As_Its_Text() => InstalledTheme.Run(_ =>
    {
        var start = Label("Start");
        var item = new AvonicItem { StartContent = start, Content = Label() };
        using var scene = InScene(item);

        start.RaiseEvent(new IonStyleEventArgs(new Dictionary<string, bool> { ["input"] = true }));
        Dispatcher.UIThread.RunJobs();

        AssertNear(OneLine("Start"), start.Bounds.Width);

        // flex: initial, so the slot is that wide too, and it isn't half the row with the label at its left.
        AssertNear(16 + start.Bounds.Width + 16, Box(Inner(item), item).X);
    });

    [AvaloniaFact]
    public void F6_A_Long_End_Label_Wraps_And_Squeezes_The_Default_Slot() => InstalledTheme.Run(_ =>
    {
        var content = Label();
        var end = Label(Long);
        var item = new AvonicItem { Content = content, EndContent = end };
        using var scene = InScene(item);

        Assert.Equal(32, Box(end, item).X);
        Assert.Equal(342, end.Bounds.Width);
        Assert.Equal(0, Part<FlexSlot>(item, "PART_Container").Bounds.Width);
    });

    [AvaloniaFact]
    public void F6_A_Short_End_Label_Is_As_Wide_As_Its_Text() => InstalledTheme.Run(_ =>
    {
        var content = Label();
        var end = Label("End");
        var item = new AvonicItem { Content = content, EndContent = end };
        using var scene = InScene(item);

        AssertNear(OneLine("End"), end.Bounds.Width);
        Assert.Equal(374, Box(end, item).Right, 0);
        Assert.Equal(16, Box(content, item).X);
    });

    [AvaloniaFact]
    public void F7_A_Label_Squeezed_To_Nothing_Wraps_At_Every_Character() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = Label("Default"), EndContent = Label(Long) };
        using var scene = InScene(item);

        // 154 in Chromium, as seven lines of 19 with its margins. 153 here, as a line is 18.75: Chromium rounds a line's
        // ascent and descent each to a whole pixel.
        AssertNear(154, item.Bounds.Height);
    });

    private static double OneLineHeight()
    {
        var block = new TextBlock { Text = "x", FontSize = 16, FontFamily = ThemeFont };
        block.Measure(Size.Infinity);
        return block.DesiredSize.Height;
    }

    [AvaloniaFact]
    public void F8_Plain_Text_Squeezed_To_Nothing_Is_One_Word_A_Line() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = "Plain default words", EndContent = Note() };
        using var scene = InScene(item);

        // 58 in Chromium, and here, as three lines of 19 and the margins. Character by character would be 19 lines.
        AssertNear(58, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void F8_A_Note_Squeezed_To_Nothing_Keeps_Its_Words() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = Note("Note default words"), EndContent = Note() };
        using var scene = InScene(item);

        // 55 in Chromium, and here.
        AssertNear(55, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void F7_A_Squeezed_Label_With_A_Margin_Still_Wraps() => InstalledTheme.Run(_ =>
    {
        // What's in the end slot is short, so the height can only come from the label.
        var item = new AvonicItem { Content = new AvonicLabel { Content = "Default", Margin = new Thickness(8, 10) }, EndContent = new Border { Width = 342, Height = 10 } };
        using var scene = InScene(item);

        Assert.Equal(0, Part<FlexSlot>(item, "PART_Container").Bounds.Width);
        Assert.True(item.Bounds.Height > 3 * OneLineHeight());
    });

    [AvaloniaFact]
    public void F8_Squeezed_Text_Is_Drawn_Word_By_Word_At_The_Height_It_Asked_For() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "Plain default words", TextWrapping = TextWrapping.Wrap };
        var item = new AvonicItem { Content = text, EndContent = new Border { Width = 326, Height = 10 } };
        using var scene = InScene(item);

        AssertNear(OneLine("default"), text.Bounds.Width);
        Assert.Equal(3, text.TextLayout.TextLines.Count);
    });

    [AvaloniaFact]
    public void F8_What_Is_Squeezed_Is_Clipped() => InstalledTheme.Run(_ =>
    {
        var content = new TextBlock { Text = "Plain default words", TextWrapping = TextWrapping.Wrap };
        var item = new AvonicItem { Content = content, EndContent = Note() };
        using var scene = InScene(item);

        // overflow: hidden on .input-wrapper, at a box of width 0.
        var container = Part<FlexSlot>(item, "PART_Container");
        Assert.Equal(0, container.Bounds.Width);
        Assert.True(container.ClipToBounds);
    });

    [AvaloniaFact]
    public void F8_Plain_Text_In_A_Narrow_Slot_Is_One_Word_A_Line() => InstalledTheme.Run(_ =>
    {
        // 16 wide. It's a character a line and about 400 high if it doesn't look for its words.
        var item = new AvonicItem { Content = "Plain default words", EndContent = new Border { Width = 326, Height = 10 } };
        using var scene = InScene(item);

        Assert.Equal(16, Part<FlexSlot>(item, "PART_Container").Bounds.Width);
        Assert.InRange(item.Bounds.Height, 54, 100);
    });

    [AvaloniaFact]
    public void F7_A_Label_In_A_Narrow_Slot_Still_Wraps_At_Every_Character() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = Label("Default"), EndContent = new Border { Width = 326, Height = 10 } };
        using var scene = InScene(item);

        Assert.True(item.Bounds.Height > 3 * OneLineHeight());
    });

    [AvaloniaFact]
    public void F9_Long_Labels_In_Start_And_End() => InstalledTheme.Run(_ =>
    {
        var start = Label(Long);
        var end = Label(Long);
        var item = new AvonicItem { StartContent = start, Content = Label(), EndContent = end };
        using var scene = InScene(item);

        Assert.Equal(171, start.Bounds.Width);
        Assert.Equal(155, end.Bounds.Width);
        Assert.Equal(219, Box(end, item).X);
    });

    [AvaloniaFact]
    public void F10_A_Word_Wider_Than_Half_Keeps_A_Start_Label_Wider() => InstalledTheme.Run(_ =>
    {
        var start = Label(Word);
        var content = Label();
        var item = new AvonicItem { StartContent = start, Content = content };
        using var scene = InScene(item);

        var word = OneLine(Word);
        Assert.True(word > 171);
        AssertNear(word, start.Bounds.Width);
        AssertNear(16 + start.Bounds.Width + 16, Box(Inner(item), item).X);
        AssertNear(390, Box(Inner(item), item).Right);
    });

    [AvaloniaFact]
    public void F11_A_Word_Wider_Than_The_Row_Is_Limited_To_It_And_Breaks() => InstalledTheme.Run(_ =>
    {
        var start = Label(VeryLongWord);
        var item = new AvonicItem { StartContent = start, Content = Label() };
        using var scene = InScene(item);

        Assert.True(OneLine(VeryLongWord) > 374);
        Assert.Equal(374, start.Bounds.Width);
        Assert.True(start.Bounds.Height > OneLineHeight());
        Assert.Equal(406, Box(Inner(item), item).X);
    });

    [AvaloniaFact]
    public void F12_A_Word_Wider_Than_The_Row_In_End_Overflows() => InstalledTheme.Run(_ =>
    {
        var end = Label(VeryLongWord);
        var item = new AvonicItem { Content = Label(), EndContent = end };
        using var scene = InScene(item);

        AssertNear(OneLine(VeryLongWord), end.Bounds.Width);
        Assert.Equal(32, Box(end, item).X);
        Assert.True(Part<Border>(item, "PART_Native").ClipToBounds);
    });

    [AvaloniaFact]
    public void F13_Nowrap_In_End_Can_Shrink_As_Overflow_Hidden_Takes_Its_Minimum() => InstalledTheme.Run(_ =>
    {
        var end = new AvonicLabel { Content = Long, Classes = { "ion-text-nowrap" } };
        var item = new AvonicItem { Content = Label(), EndContent = end };
        using var scene = InScene(item);

        Assert.Equal(342, end.Bounds.Width);
        Assert.Equal(OneLineHeight(), end.Bounds.Height, 0);
    });

    [AvaloniaFact]
    public void F13_Nowrap_In_Start_Is_Half_The_Row() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicLabel { Content = Long, Classes = { "ion-text-nowrap" } };
        var content = Label();
        using var scene = InScene(new AvonicItem { StartContent = start, Content = content });

        Assert.Equal(171, start.Bounds.Width);
        Assert.Equal(171, content.Bounds.Width);
    });

    [AvaloniaFact]
    public void F14_A_Long_Note_In_End() => InstalledTheme.Run(_ =>
    {
        var content = Label();
        var end = Note();
        var item = new AvonicItem { Content = content, EndContent = end };
        using var scene = InScene(item);

        AssertWrapsWithin(342, end);
        Assert.Equal(32, Box(end, item).X);
        Assert.Equal(0, Box(end, item).Y);
    });

    [AvaloniaFact]
    public void F14_A_Long_Note_In_Start() => InstalledTheme.Run(_ =>
    {
        var start = Note();
        var item = new AvonicItem { StartContent = start, Content = Label() };
        using var scene = InScene(item);

        AssertWrapsWithin(342, start);
        Assert.Equal(new Rect(374, 0, 16, item.Bounds.Height), Box(Inner(item), item));
    });

    [AvaloniaFact]
    public void F14_A_Short_Note_Is_As_Wide_As_Its_Text() => InstalledTheme.Run(_ =>
    {
        var start = Note("Short");
        var end = Note("Short");
        var content = Label();
        var item = new AvonicItem { StartContent = start, Content = content, EndContent = end };
        using var scene = InScene(item);

        AssertNear(OneLine("Short", 11), start.Bounds.Width);
        AssertNear(OneLine("Short", 11), end.Bounds.Width);
        Assert.Equal(374, Box(end, item).Right, 0);
    });

    [AvaloniaFact]
    public void F15_Long_Notes_In_Both_Leave_The_End_One_At_Its_Longest_Word() => InstalledTheme.Run(_ =>
    {
        var start = Note();
        var end = Note();
        var item = new AvonicItem { StartContent = start, Content = Label(), EndContent = end };
        using var scene = InScene(item);

        AssertWrapsWithin(342, start);
        AssertNear(OneLine("certainly", 11), end.Bounds.Width);
        Assert.Equal(390, Box(end, item).X);
    });

    [AvaloniaFact]
    public void F16_A_Start_Icon_Keeps_Its_Size_Beside_A_Long_End_Label() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "star" };
        var end = Label(Long);
        var item = new AvonicItem { StartContent = icon, Content = Label(), EndContent = end };
        using var scene = InScene(item);

        Assert.Equal(24, icon.Bounds.Width);
        Assert.Equal(72, Box(Inner(item), item).X);
        Assert.Equal(286, end.Bounds.Width);
    });

    [AvaloniaFact]
    public void F16_An_End_Icon_In_A_Narrow_Item_Shrinks() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "star" };
        var item = new AvonicItem { Content = Label(), EndContent = icon, Width = 40, HorizontalAlignment = HorizontalAlignment.Left };
        using var scene = InScene(item);

        // 40 - 16 - 16 leaves 8, after the icon's margin of 16.
        Assert.Equal(0, icon.Bounds.Width);
    });

    [AvaloniaFact]
    public void F17_The_Detail_Icon_Shrinks_Beside_A_Long_End_Label() => InstalledTheme.Run(_ =>
    {
        var end = Label(Long);
        var item = new AvonicItem { Content = Label(), EndContent = end, Detail = true };
        using var scene = InScene(item);

        // In proportion to the label's length on one line: 330.5 and 9.5 in Chromium, 331 and 10 here.
        var text = OneLine(Long);
        var overflow = text + 16 + 22 - 358;
        var icon = Part<AvonicIcon>(item, "PART_DetailIcon");
        AssertNear(20 - (20 * overflow / (text + 20)), icon.Bounds.Width);
        AssertNear(text - (text * overflow / (text + 20)), end.Bounds.Width);
        AssertNear(9.5, icon.Bounds.Width);
        AssertNear(330.5, end.Bounds.Width);
    });

    [AvaloniaFact]
    public void F17_The_Detail_Icon_Keeps_Its_Size_Beside_A_Short_One() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = Label(), EndContent = Label("End"), Detail = true };
        using var scene = InScene(item);

        var icon = Part<AvonicIcon>(item, "PART_DetailIcon");
        Assert.Equal(new Rect(360, 14, 20, 20), Box(icon, item));
    });

    [AvaloniaFact]
    public void F18_Plain_Text_In_End_Wraps() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = Label(), EndContent = Long };
        using var scene = InScene(item);

        var end = Part<FlexSlot>(item, "PART_End");
        Assert.Equal(358, end.Bounds.Width);
        Assert.True(end.Child!.Bounds.Height > OneLineHeight());
    });

    [AvaloniaFact]
    public void F18_Plain_Text_In_Start_Wraps() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = Label(), StartContent = Long };
        using var scene = InScene(item);

        var start = Part<FlexSlot>(item, "PART_Start");
        Assert.Equal(358, start.Bounds.Width);
        Assert.True(start.Child!.Bounds.Height > OneLineHeight());
    });

    [AvaloniaFact]
    public void F18_AvonicText_In_End() => InstalledTheme.Run(_ =>
    {
        var end = new AvonicText { Content = Long };
        var item = new AvonicItem { Content = Label(), EndContent = end };
        using var scene = InScene(item);

        AssertWrapsWithin(342, end);
    });

    [AvaloniaFact]
    public void F18_AvonicText_In_Start() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicText { Content = Long };
        var item = new AvonicItem { StartContent = start, Content = Label() };
        using var scene = InScene(item);

        AssertWrapsWithin(342, start);
    });

    [AvaloniaFact]
    public void F18_A_Box_Around_A_Note_In_End() => InstalledTheme.Run(_ =>
    {
        var note = Note();
        var box = new Border { Child = note };
        var item = new AvonicItem { Content = Label(), EndContent = box };
        using var scene = InScene(item);

        Assert.Equal(342, box.Bounds.Width);
        Assert.True(note.Bounds.Width <= 342);
        Assert.True(note.Bounds.Height > OneLineHeight());
    });

    [AvaloniaFact]
    public void F18_A_Panel_In_Start_Is_Not_Flex_Even_With_A_Label_In_It() => InstalledTheme.Run(_ =>
    {
        var label = Label(Long);
        var panel = new Grid { Children = { label } };
        var item = new AvonicItem { StartContent = panel, Content = Label() };
        using var scene = InScene(item);

        Assert.Equal(342, panel.Bounds.Width);
    });

    [AvaloniaFact]
    public void F19_A_Note_Reaches_The_Top_Of_A_Tall_Row_And_A_Label_Does_Not() => InstalledTheme.Run(_ =>
    {
        var start = Label("Start");
        var end = Note("Top");
        var content = Label(Long);
        var item = new AvonicItem { StartContent = start, Content = content, EndContent = end };
        using var scene = InScene(item);

        Assert.True(item.Bounds.Height > 48);
        Assert.Equal(0, Box(end, item).Y);

        // Its own height, or "centred" would also pass if it filled the row.
        AssertNear(OneLineHeight(), start.Bounds.Height);
        AssertNear((item.Bounds.Height - 1 - start.Bounds.Height) / 2, Box(start, item).Y);
    });

    [AvaloniaFact]
    public void F20_RTL_Mirrors_It() => InstalledTheme.Run(_ =>
    {
        var start = Label("Start");
        var content = Label();
        var end = Note();
        var item = new AvonicItem { StartContent = start, Content = content, EndContent = end, FlowDirection = FlowDirection.RightToLeft };
        using var scene = InScene(item);

        // A mirrored element's origin is its right-hand edge.
        Assert.Equal(171, start.Bounds.Width);
        Assert.Equal(203, Box(start, scene.Window).X - start.Bounds.Width, 1);
        AssertWrapsWithin(155, end);
        Assert.Equal(171, Box(end, scene.Window).X, 1);
    });

    [AvaloniaFact]
    public void F19_A_Button_Follows_Align_Items() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "x" };
        var item = new AvonicItem { Content = Label(Long), EndContent = button };
        using var scene = InScene(item);

        AssertNear((item.Bounds.Height - 1 - button.Bounds.Height) / 2, Box(button, item).Y);
    });

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void F19_A_Button_Beside_A_Stacked_Label_Is_At_The_Top(LabelPosition position) => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "x" };
        var item = new AvonicItem
        {
            StartContent = new Border { Width = 10, Height = 100 },
            Content = new AvonicLabel { Content = "x", Position = position },
            EndContent = button,
        };
        using var scene = InScene(item);

        Assert.True(item.Bounds.Height >= 100);
        Assert.Equal(7, Box(button, item).Y);
    });

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void F19_What_A_Panel_Holds_Beside_A_Stacked_Label_Is_Not_Centred(LabelPosition position) => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "x" };
        var item = new AvonicItem
        {
            StartContent = new Border { Width = 10, Height = 100 },
            Content = new AvonicLabel { Content = "x", Position = position },
            EndContent = new StackPanel { Orientation = Orientation.Horizontal, Children = { text } },
        };
        using var scene = InScene(item);

        // The end slot's margin of 7, and no lower, as it isn't centred.
        Assert.Equal(7, Box(text, item).Y);
    });

    [AvaloniaFact]
    public void F19_A_Child_Of_A_Panel_That_Asks_To_Stretch_Does() => InstalledTheme.Run(_ =>
    {
        var line = new Border { Width = 1, VerticalAlignment = VerticalAlignment.Stretch };
        var end = new StackPanel { Orientation = Orientation.Horizontal, Children = { line } };
        var item = new AvonicItem { Content = Label(Long), EndContent = end };
        using var scene = InScene(item);

        Assert.Equal(end.Bounds.Height, line.Bounds.Height);
    });

    [AvaloniaFact]
    public void F19_A_Child_Of_A_Panel_With_No_Height_Of_Its_Own_Collapses_As_It_Does_In_CSS() => InstalledTheme.Run(_ =>
    {
        var line = new Border { Width = 1 };
        var end = new StackPanel { Orientation = Orientation.Horizontal, Children = { line } };
        using var scene = InScene(new AvonicItem { Content = Label(Long), EndContent = end });

        Assert.Equal(0, line.Bounds.Height);
    });

    [AvaloniaFact]
    public void F21_What_A_Panel_Holds_In_A_Divider_Is_Centred() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "x" };
        var divider = new AvonicItemDivider
        {
            StartContent = new Border { Width = 10, Height = 100 },
            EndContent = new StackPanel { Orientation = Orientation.Horizontal, Children = { text } },
        };
        using var scene = InScene(divider);

        Assert.True(text.Bounds.Height < 30);
        AssertNear((divider.Bounds.Height - 1 - text.Bounds.Height) / 2, Box(text, divider).Y);
    });

    [AvaloniaFact]
    public void F21_A_Divider_Start_Label_Is_As_Wide_As_Its_Text() => InstalledTheme.Run(_ =>
    {
        var start = Label("Start");
        var divider = new AvonicItemDivider { StartContent = start, Content = Label() };
        using var scene = InScene(divider);

        AssertNear(OneLine("Start", 14), start.Bounds.Width);
        Assert.Equal(16 + start.Bounds.Width + 16, Box(Inner(divider), divider).X, 0);
    });

    [AvaloniaFact]
    public void F21_A_Long_Divider_Start_Label_Wraps() => InstalledTheme.Run(_ =>
    {
        var start = Label(Long);
        var divider = new AvonicItemDivider { StartContent = start, Content = Label() };
        using var scene = InScene(divider);

        Assert.Equal(342, start.Bounds.Width);
        Assert.Equal(374, Box(Inner(divider), divider).X);
    });

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void F22_Long_Divider_End_Content_Wraps_And_A_Default_Label_Keeps_Its_Words(bool label)
    {
        InstalledTheme.Run(_ =>
        {
            Control end = label ? Label(Long) : Note();
            var content = Label();
            var divider = new AvonicItemDivider { Content = content, EndContent = end };
            using var scene = InScene(divider);

            AssertWrapsWithin(342, end);
            Assert.Equal(32, Box(end, divider).X);
            Assert.Equal(0, Part<FlexSlot>(divider, "PART_Container").Bounds.Width);
            Assert.True(content.GetVisualDescendants().OfType<TextBlock>().First().Bounds.Height <= OneLineHeight());
        });
    }

    [AvaloniaFact]
    public void An_Item_Aligned_To_The_Left_Is_As_Wide_As_Its_Content() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = "Short", HorizontalAlignment = HorizontalAlignment.Left };
        using var scene = InScene(item);

        AssertNear(16 + OneLine("Short") + 16, item.Bounds.Width);
    });

    [AvaloniaFact]
    public void A_Divider_Aligned_To_The_Left_Is_As_Wide_As_Its_Content() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider { Content = "Short", HorizontalAlignment = HorizontalAlignment.Left };
        using var scene = InScene(divider);

        Assert.True(divider.Bounds.Width < 150);
    });

    [AvaloniaFact]
    public void The_Stub_Lays_Out_Without_A_Theme()
    {
        var start = new TextBlock { Text = "S" };
        var end = new TextBlock { Text = "E" };
        var item = new AvonicItem { StartContent = start, Content = "x", EndContent = end };
        using var scene = InScene(item);

        // The inner fills what's left, and the default slot pushes the end to the edge.
        Assert.Equal(390, Box(Inner(item), item).Right, 0);
        Assert.Equal(390, Box(end, item).Right, 0);
    }

    [AvaloniaFact]
    public void The_Stubs_Wrap_Plain_Text_In_Their_Slots()
    {
        var item = new AvonicItem { StartContent = Long };
        var divider = new AvonicItemDivider { EndContent = Long };
        using var scene = InScene(new StackPanel { Children = { item, divider } });

        Assert.True(Part<FlexSlot>(item, "PART_Start").Child!.Bounds.Height > OneLineHeight());
        Assert.True(Part<FlexSlot>(divider, "PART_End").Child!.Bounds.Height > OneLineHeight());
    }

    [AvaloniaFact]
    public void The_Divider_Stub_Lays_Out_Without_A_Theme()
    {
        var end = new TextBlock { Text = Long, TextWrapping = TextWrapping.Wrap };
        var divider = new AvonicItemDivider { Content = "x", EndContent = end };
        using var scene = InScene(divider);

        Assert.Equal(390, end.Bounds.Width, 0);
        Assert.True(end.Bounds.Height > OneLineHeight());
    }

    [AvaloniaFact]
    public void A_Hidden_Start_Label_Takes_No_Share() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicLabel { Content = "Start", IsVisible = false };
        var item = new AvonicItem { StartContent = start, Content = Label() };
        using var scene = InScene(item);

        Assert.Equal(374, Inner(item).Bounds.Width);
        Assert.Equal(16, Box(Inner(item), item).X);

        start.IsVisible = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(171, start.Bounds.Width);
    });

    [AvaloniaFact]
    public void A_Label_That_Moves_Out_Of_Start_Stops_Being_Half() => InstalledTheme.Run(_ =>
    {
        var label = Label("Start");
        var item = new AvonicItem { StartContent = label };
        using var scene = InScene(item);
        Assert.Equal(171, label.Bounds.Width);

        item.StartContent = null;
        item.EndContent = label;
        Dispatcher.UIThread.RunJobs();

        AssertNear(OneLine("Start"), label.Bounds.Width);
    });

    [AvaloniaFact]
    public void A_Note_Added_To_A_Panel_Goes_To_The_Top_And_The_Rest_Stays_Centred() => InstalledTheme.Run(_ =>
    {
        var other = new TextBlock { Text = "x" };
        var end = new StackPanel { Orientation = Orientation.Horizontal, Children = { other } };
        var item = new AvonicItem { Content = Label(Long), EndContent = end };
        using var scene = InScene(item);

        var note = Note("x");
        end.Children.Add(note);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, Box(note, item).Y);
        AssertNear((item.Bounds.Height - 1 - other.Bounds.Height) / 2, Box(other, item).Y);
    });
}
