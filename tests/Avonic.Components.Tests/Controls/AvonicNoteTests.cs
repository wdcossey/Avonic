using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
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
/// The numbers are from real ion-note in Chromium, at 390 px wide. See docs/components/ion-note/PLAN.md.
/// </summary>
public class AvonicNoteTests
{
    private const string Long = "A very long piece of text that certainly has to wrap around because it does not fit into a phone-sized viewport at all";

    private static readonly Color Danger = Color.FromRgb(197, 0, 15);
    private static readonly Color PrimaryBase = Color.FromRgb(0, 84, 233);
    private static readonly Color Grey = Color.FromRgb(102, 102, 102);
    private static readonly Color Token = Color.FromRgb(18, 52, 86);

    private sealed class Stub(ItemControlType type) : Border, IItemControl
    {
        public int ItemTaps { get; private set; }

        public ItemControlType ItemControlType => type;

        public void OnItemTap() => ItemTaps++;
    }

    private static Color TextColor(Control control) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(TextElement.GetForeground(control)).Color;

    private static TextBlock Text(AvonicNote note) => note.GetVisualDescendants().OfType<TextBlock>().First();

    private static Rect Box(Visual visual, Visual root) => new(visual.TranslatePoint(default, root)!.Value, visual.Bounds.Size);

    private static Scene InScene(params Control[] children)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(children);
        return new Scene(panel);
    }

    private static void Tap(Scene scene, Point point)
    {
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Layout rounds to whole pixels, so an odd gap can't be split exactly.</summary>
    private static void AssertCentred(Visual visual, AvonicItem item) =>
        Assert.InRange(Box(visual, item).Center.Y - item.Bounds.Height / 2, -1, 1);

    /// <summary>Three lines of text, so the row is much taller than a note: measured 88 in Ionic.</summary>
    private static StackPanel Tall() => new()
    {
        Children =
        {
            new TextBlock { Text = "One", Height = 30 },
            new TextBlock { Text = "Two", Height = 30 },
            new TextBlock { Text = "Three", Height = 30 },
        },
    };

    // ── 1. Alone ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Alone_It_Has_Its_Own_Colour_Size_And_Font() => InstalledTheme.Run(theme =>
    {
        var note = new AvonicNote { Content = "x" };
        var parent = new StackPanel { Children = { note } };
        TextElement.SetForeground(parent, new SolidColorBrush(Danger));
        TextElement.SetFontSize(parent, 30);
        TextElement.SetFontFamily(parent, new FontFamily("serif"));
        using var scene = new Scene(parent);

        Assert.Equal(Grey, TextColor(Text(note)));
        Assert.Equal(14, Text(note).FontSize);
        Assert.True(theme.TryGetResource("Avonic.FontFamily", null, out var font));
        Assert.Equal(font, Text(note).FontFamily);
    });

    [AvaloniaFact]
    public void B1_The_Global_Text_Colour_Sets_The_Default() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        using var scene = InScene(note);

        scene.Window.Resources["Avonic.TextColorStep400Brush"] = new SolidColorBrush(Token);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Token, TextColor(Text(note)));
    });

    // ── 2. Color ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_Color_Paints_The_Text_Alone_And_In_An_Item() => InstalledTheme.Run(_ =>
    {
        var alone = new AvonicNote { Content = "x", Color = "danger" };
        var slotted = new AvonicNote { Content = "x", Color = "danger" };
        using var scene = InScene(alone, new AvonicItem { EndContent = slotted });

        Assert.Equal(Danger, TextColor(Text(alone)));
        Assert.Equal(Danger, TextColor(Text(slotted)));
    });

    [AvaloniaFact]
    public void B2_A_Change_Of_Color_Follows() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x", Color = "danger" };
        using var scene = InScene(note);

        note.Color = "primary";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PrimaryBase, TextColor(Text(note)));

        note.Color = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Grey, TextColor(Text(note)));
    });

    // ── 3. An unknown colour ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_An_Unknown_Color_Inherits_From_Its_Parent() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x", Color = "nonexistent" };
        var parent = new StackPanel { Children = { note } };
        TextElement.SetForeground(parent, new SolidColorBrush(Danger));
        using var scene = new Scene(parent);

        Assert.Equal(Danger, TextColor(Text(note)));
    });

    [AvaloniaFact]
    public void B3_An_Unknown_Color_In_A_Coloured_Item_Shows_Its_Base() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x", Color = "nonexistent" };
        using var scene = InScene(new AvonicItem { Color = "primary", EndContent = note });

        Assert.Equal(PrimaryBase, TextColor(Text(note)));
    });

    // ── 4. The token ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_The_Token_Sets_The_Text_Alone_And_In_An_Item() => InstalledTheme.Run(_ =>
    {
        var alone = new AvonicNote { Content = "x" };
        var slotted = new AvonicNote { Content = "x" };
        using var scene = InScene(alone, new AvonicItem { EndContent = slotted });

        scene.Window.Resources["Avonic.Note.Color"] = new SolidColorBrush(Token);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Token, TextColor(Text(alone)));
        Assert.Equal(Token, TextColor(Text(slotted)));
    });

    [AvaloniaFact]
    public void B4_Color_Beats_The_Token() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x", Color = "danger" };
        using var scene = InScene(note);

        scene.Window.Resources["Avonic.Note.Color"] = new SolidColorBrush(Token);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Danger, TextColor(Text(note)));
    });

    // ── 5. In a coloured item ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_A_Coloured_Item_Does_Not_Change_A_Note() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        using var scene = InScene(new AvonicItem { Color = "primary", EndContent = note });

        Assert.Equal(Grey, TextColor(Text(note)));
    });

    // ── 6. The size in a slot ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_In_Any_Slot_It_Is_11() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicNote { Content = "x" };
        var main = new AvonicNote { Content = "x" };
        var end = new AvonicNote { Content = "x" };
        using var scene = InScene(new AvonicItem { StartContent = start, Content = main, EndContent = end });

        Assert.Equal(11, Text(start).FontSize);
        Assert.Equal(11, Text(main).FontSize);
        Assert.Equal(11, Text(end).FontSize);
    });

    [AvaloniaFact]
    public void B6_In_A_Panel_Directly_In_A_Slot_It_Is_11() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        using var scene = InScene(new AvonicItem { EndContent = new StackPanel { Children = { note } } });

        Assert.Equal(11, Text(note).FontSize);
    });

    [AvaloniaFact]
    public void B6_It_Does_Not_Depend_On_The_Items_Size() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        using var scene = InScene(new AvonicItem { FontSize = 30, EndContent = note });

        Assert.Equal(11, Text(note).FontSize);
    });

    [AvaloniaFact]
    public void B6_Nested_Deeper_Or_In_A_Label_It_Is_14() => InstalledTheme.Run(_ =>
    {
        var deeper = new AvonicNote { Content = "x" };
        var inLabel = new AvonicNote { Content = "x" };
        using var scene = InScene(
            new AvonicItem { EndContent = new Border { Child = deeper } },
            new AvonicItem { Content = new AvonicLabel { Content = inLabel } });

        Assert.Equal(14, Text(deeper).FontSize);
        Assert.Equal(14, Text(inLabel).FontSize);
    });

    // ── 7. The padding in a slot ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_In_The_Start_And_End_Slots_It_Has_Padding() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicNote { Content = "x" };
        var end = new AvonicNote { Content = "x" };
        var inStart = new AvonicNote { Content = "x" };
        var inEnd = new AvonicNote { Content = "x" };
        using var scene = InScene(
            new AvonicItem { StartContent = start, EndContent = end },
            new AvonicItem { StartContent = new StackPanel { Children = { inStart } }, EndContent = new StackPanel { Children = { inEnd } } });

        foreach (var note in new[] { start, end, inStart, inEnd })
        {
            Assert.Equal(new Thickness(0, 18, 0, 10), note.Padding);
            Assert.Equal(28 + Text(note).Bounds.Height, note.Bounds.Height);
        }
    });

    [AvaloniaFact]
    public void B7_In_The_Default_Slot_Or_Alone_It_Has_None() => InstalledTheme.Run(_ =>
    {
        var main = new AvonicNote { Content = "x" };
        var inPanel = new AvonicNote { Content = "x" };
        var alone = new AvonicNote { Content = "x" };
        using var scene = InScene(
            new AvonicItem { Content = main },
            new AvonicItem { Content = new StackPanel { Children = { inPanel } } },
            alone);

        Assert.Equal(default, main.Padding);
        Assert.Equal(default, inPanel.Padding);
        Assert.Equal(default, alone.Padding);
    });

    // ── 8. At the top of the row ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_It_Is_At_The_Top_Of_A_Plain_Row_In_Any_Slot() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicNote { Content = "x" };
        var main = new AvonicNote { Content = "x" };
        var end = new AvonicNote { Content = "x" };
        var itemStart = new AvonicItem { StartContent = start };
        var itemMain = new AvonicItem { Content = main };
        var itemEnd = new AvonicItem { EndContent = end };
        using var scene = InScene(itemStart, itemMain, itemEnd);

        Assert.Equal(0, Box(start, itemStart).Y);
        Assert.Equal(0, Box(main, itemMain).Y);
        Assert.Equal(0, Box(end, itemEnd).Y);
    });

    [AvaloniaFact]
    public void B8_It_Is_At_The_Top_Of_A_Tall_Row_In_Any_Slot() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicNote { Content = "x" };
        var main = new AvonicNote { Content = "x" };
        var end = new AvonicNote { Content = "x" };
        var itemStart = new AvonicItem { StartContent = start, Content = Tall() };
        var itemMain = new AvonicItem { StartContent = Tall(), Content = main };
        var itemEnd = new AvonicItem { Content = Tall(), EndContent = end };
        using var scene = InScene(itemStart, itemMain, itemEnd);

        Assert.True(itemEnd.Bounds.Height >= 90);
        Assert.Equal(0, Box(start, itemStart).Y);
        Assert.Equal(0, Box(main, itemMain).Y);
        Assert.Equal(0, Box(end, itemEnd).Y);

        // Measured 40, 12 and 40 high in a row of 88: it's at the top, and it doesn't fill the row.
        Assert.True(start.Bounds.Height < 50);
        Assert.True(main.Bounds.Height < 25);
        Assert.True(end.Bounds.Height < 50);
    });

    [AvaloniaFact]
    public void B8_What_Is_In_The_Other_Slots_Stays_Centred() => InstalledTheme.Run(_ =>
    {
        var other = new TextBlock { Text = "x", Height = 20 };
        var item = new AvonicItem { StartContent = other, Content = Tall(), EndContent = new AvonicNote { Content = "x" } };
        using var scene = InScene(item);

        AssertCentred(other, item);
    });

    [AvaloniaFact]
    public void B8_In_A_Panel_It_Is_At_The_Top_And_The_Rest_Is_Centred() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        var other = new TextBlock { Text = "x" };
        var label = new AvonicLabel { Content = "x" };
        var end = new StackPanel { Orientation = Orientation.Horizontal, Children = { note, other, label } };
        var item = new AvonicItem { Content = Tall(), EndContent = end };
        using var scene = InScene(item);

        Assert.Equal(0, Box(note, item).Y);
        Assert.True(note.Bounds.Height < 50);

        // Centred at their own height. A stretched TextBlock would be centred too, but with its text at the top.
        AssertCentred(other, item);
        Assert.True(other.Bounds.Height < 25);
        AssertCentred(label, item);
        Assert.True(label.Bounds.Height < 50);
    });

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void B8_A_Stacked_Or_Floating_Label_In_The_Panel_Still_Fills_It(LabelPosition position) => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x", Position = position };
        var content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new AvonicNote { Content = "x" }, label } };
        var item = new AvonicItem { Content = content };
        using var scene = InScene(item);

        Assert.Equal(content.Bounds.Height, label.Bounds.Height);
        Assert.Equal(55, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void B8_A_Vertical_Stack_Is_A_Group_The_Item_Centres() => InstalledTheme.Run(_ =>
    {
        var group = new StackPanel { Children = { new AvonicNote { Content = "x" }, new TextBlock { Text = "x" } } };
        var item = new AvonicItem { Content = Tall(), EndContent = group };
        using var scene = InScene(item);

        Assert.True(group.Bounds.Height < item.Bounds.Height);
        AssertCentred(group, item);
    });

    [AvaloniaFact]
    public void B8_In_A_Textarea_Item_The_Rest_Of_A_Panel_Is_Not_Centred() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        var other = new TextBlock { Text = "x" };
        var end = new StackPanel { Orientation = Orientation.Horizontal, Children = { note, other } };
        var item = new AvonicItem { Content = Tall(), EndContent = end };
        using var scene = InScene(item);

        note.RaiseEvent(new IonStyleEventArgs(new Dictionary<string, bool> { ["textarea"] = true }));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(end.Bounds.Height, other.Bounds.Height);
    });

    [AvaloniaFact]
    public void B8_When_It_Leaves_The_Slot_Is_Centred_Again() => InstalledTheme.Run(_ =>
    {
        var other = new TextBlock { Text = "x", Height = 20 };
        var item = new AvonicItem { Content = Tall(), EndContent = new AvonicNote { Content = "x" } };
        using var scene = InScene(item);

        item.EndContent = other;
        Dispatcher.UIThread.RunJobs();

        AssertCentred(other, item);
    });

    [AvaloniaFact]
    public void B8_When_One_Of_Two_Leaves_The_Other_Stays_At_The_Top() => InstalledTheme.Run(_ =>
    {
        var one = new AvonicNote { Content = "x" };
        var two = new AvonicNote { Content = "x" };
        var end = new StackPanel { Orientation = Orientation.Horizontal, Children = { one, two } };
        var item = new AvonicItem { Content = Tall(), EndContent = end };
        using var scene = InScene(item);

        end.Children.Remove(one);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, Box(two, item).Y);
    });

    [AvaloniaFact]
    public void B8_When_The_Only_Note_In_A_Panel_Leaves_The_Slot_Is_Centred_Again() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        var other = new TextBlock { Text = "x", VerticalAlignment = VerticalAlignment.Top };
        var end = new StackPanel { Orientation = Orientation.Horizontal, Children = { note, other } };
        var item = new AvonicItem { Content = Tall(), EndContent = end };
        using var scene = InScene(item);
        Assert.Equal(0, Box(other, item).Y);

        end.Children.Remove(note);
        Dispatcher.UIThread.RunJobs();

        AssertCentred(other, item);
    });

    [AvaloniaFact]
    public void B8_When_A_Panel_Of_Two_Leaves_The_Slot_Is_Centred_Again() => InstalledTheme.Run(_ =>
    {
        var other = new TextBlock { Text = "x", Height = 20 };
        var end = new StackPanel { Children = { new AvonicNote { Content = "x" }, new AvonicNote { Content = "x" } } };
        var item = new AvonicItem { Content = Tall(), EndContent = end };
        using var scene = InScene(item);

        item.EndContent = other;
        Dispatcher.UIThread.RunJobs();

        AssertCentred(other, item);
    });

    [AvaloniaFact]
    public void B8_Notes_Taken_Out_While_The_Item_Is_Off_Screen_Leave_It_Centred() => InstalledTheme.Run(_ =>
    {
        var other = new TextBlock { Text = "x", VerticalAlignment = VerticalAlignment.Top };
        var end = new StackPanel { Orientation = Orientation.Horizontal, Children = { new AvonicNote { Content = "x" }, new AvonicNote { Content = "x" }, other } };
        var item = new AvonicItem { Content = Tall(), EndContent = end };
        var root = new StackPanel { Children = { item } };
        using var scene = new Scene(root);

        root.Children.Remove(item);
        Dispatcher.UIThread.RunJobs();
        end.Children.RemoveRange(0, 2);
        root.Children.Add(item);
        Dispatcher.UIThread.RunJobs();

        AssertCentred(other, item);
    });

    [AvaloniaFact]
    public void B8_It_Moves_To_Another_Item_And_Takes_The_Alignment_With_It() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        var first = new AvonicItem { Content = Tall(), EndContent = note };
        var second = new AvonicItem { Content = Tall() };
        using var scene = InScene(first, second);

        first.EndContent = null;
        second.EndContent = note;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, Box(note, second).Y);
    });

    [AvaloniaFact]
    public void B8_Nested_Deeper_It_Is_Centred_With_The_Slot() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        var item = new AvonicItem { Content = Tall(), EndContent = new Border { Child = note } };
        using var scene = InScene(item);

        AssertCentred(note, item);
    });

    [AvaloniaFact]
    public void B8_A_Presenter_That_Is_Not_An_Items_Is_Left_Alone()
    {
        var note = new AvonicNote { Content = "x" };
        var presenter = new Avalonia.Controls.Presenters.ContentPresenter { Content = note };
        using var scene = new Scene(presenter);

        Assert.DoesNotContain(AvonicNote.HasNotePseudoClass, presenter.Classes);
    }

    // ── 9. The item's slot margins ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_The_Start_And_End_Margins_Apply() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicNote { Content = "x" };
        var end = new AvonicNote { Content = "x" };
        var main = new AvonicNote { Content = "x" };
        using var scene = InScene(new AvonicItem { StartContent = start, Content = main, EndContent = end });

        Assert.Equal(new Thickness(0, 0, 16, 0), start.Margin);
        Assert.Equal(new Thickness(16, 0, 0, 0), end.Margin);
        Assert.Equal(default, main.Margin);
    });

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void B9_Beside_A_Stacked_Or_Floating_Label_It_Is_7_From_The_Top(LabelPosition position) => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        var item = new AvonicItem { Content = new AvonicLabel { Content = "x", Position = position }, EndContent = note };
        using var scene = InScene(item);

        Assert.Equal(7, Box(note, item).Y);
    });

    // ── 10. As wide as its text ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_It_Is_As_Wide_As_Its_Text() => InstalledTheme.Run(_ =>
    {
        var alone = new AvonicNote { Content = "x" };
        var main = new AvonicNote { Content = "x" };
        var item = new AvonicItem { Content = main };
        using var scene = InScene(alone, item);

        // Its TextBlock would stretch with it, so this has to be against the row.
        Assert.True(alone.Bounds.Width < 30);
        Assert.True(main.Bounds.Width < 30);
        Assert.Equal(new Point(16, 0), Box(main, item).TopLeft);
    });

    [AvaloniaFact]
    public void B10_The_Stub_Alone_Is_As_Wide_As_Its_Text_And_Wraps()
    {
        var hug = new AvonicNote { Content = "x" };
        var wrap = new AvonicNote { Content = Long };
        using var scene = InScene(hug, wrap);

        Assert.True(hug.Bounds.Width < 30);
        Assert.True(Text(wrap).Bounds.Height > Text(hug).Bounds.Height);
    }

    // ── 11. Long text ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Long_Text_Wraps_In_Its_Parent() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = Long };
        var one = new AvonicNote { Content = "x" };
        using var scene = new Scene(new StackPanel { Width = 200, Children = { note, one } });

        Assert.True(note.Bounds.Width <= 200);
        Assert.True(note.Bounds.Height >= 3 * one.Bounds.Height);
    });

    [AvaloniaFact]
    public void B11_Long_Text_Wraps_In_The_Default_Slot() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = Long };
        var item = new AvonicItem { Content = note };
        using var scene = InScene(item);

        var box = Box(note, item);
        Assert.Equal(0, box.Y);
        Assert.Equal(16, box.X);
        Assert.True(box.Right <= 374);
        Assert.True(Text(note).Bounds.Height >= 2 * 11);
    });

    [AvaloniaFact]
    public void B11_It_Does_Not_Clip() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        using var scene = InScene(note);

        Assert.False(note.ClipToBounds);
    });

    [AvaloniaFact]
    public void B11_The_Stub_Does_Not_Clip()
    {
        var note = new AvonicNote { Content = "x" };
        using var scene = InScene(note);

        Assert.False(note.ClipToBounds);
    }

    // ── 15. white-space inherits from a label ────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_In_A_Nowrap_Label_It_Is_One_Line_With_An_Ellipsis() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = Long };
        var label = new AvonicLabel { Content = new StackPanel { Children = { note } }, Classes = { "ion-text-nowrap" } };
        var one = new AvonicNote { Content = "x" };
        using var scene = InScene(new AvonicItem { Content = label }, new AvonicItem { Content = new AvonicLabel { Content = one } });

        Assert.Equal(TextWrapping.NoWrap, Text(note).TextWrapping);
        Assert.Equal(TextTrimming.CharacterEllipsis, Text(note).TextTrimming);
        Assert.Equal(one.Bounds.Height, note.Bounds.Height);
    });

    [AvaloniaFact]
    public void B15_In_A_Plain_Label_It_Wraps() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = Long };
        using var scene = InScene(new AvonicItem { Content = new AvonicLabel { Content = note } });

        Assert.Equal(TextWrapping.Wrap, Text(note).TextWrapping);
    });

    // ── 12. Taps ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_A_Tap_On_A_Note_Does_Not_Forward_Even_On_Its_Padding() => InstalledTheme.Run(_ =>
    {
        var control = new Stub(ItemControlType.Toggle) { Height = 20 };
        var note = new AvonicNote { Content = "note" };
        var item = new AvonicItem { Content = control, EndContent = note };
        using var scene = InScene(item);

        var box = Box(note, scene.Window);
        Tap(scene, box.TopLeft + new Point(2, 2));
        Assert.Equal(0, control.ItemTaps);
        Tap(scene, box.Center);
        Assert.Equal(0, control.ItemTaps);
    });

    [AvaloniaFact]
    public void B12_The_Stub_Alone_Takes_The_Tap_On_Its_Padding()
    {
        var control = new Stub(ItemControlType.Toggle) { Height = 20 };
        var note = new AvonicNote { Content = "note", Padding = new Thickness(0, 18, 0, 10) };
        var item = new AvonicItem { Content = control, EndContent = note };
        using var scene = InScene(item);

        var box = Box(note, scene.Window);
        Assert.Equal(28 + Text(note).Bounds.Height, box.Height);
        Tap(scene, box.TopLeft + new Point(2, 2));

        Assert.Equal(0, control.ItemTaps);
    }

    [AvaloniaFact]
    public void B12_A_Tap_Beside_Or_Below_A_Note_In_The_Default_Slot_Forwards() => InstalledTheme.Run(_ =>
    {
        var control = new Stub(ItemControlType.Toggle) { Height = 20 };
        var note = new AvonicNote { Content = "note" };
        var item = new AvonicItem { StartContent = control, Content = note };
        using var scene = InScene(item);
        var box = Box(note, scene.Window);

        Tap(scene, new Point(box.Right + 20, box.Center.Y));
        Assert.Equal(1, control.ItemTaps);

        Tap(scene, new Point(box.Center.X, box.Bottom + 10));
        Assert.Equal(2, control.ItemTaps);
    });

    [AvaloniaFact]
    public void B12_A_Tap_On_The_Items_Padding_Forwards() => InstalledTheme.Run(_ =>
    {
        var control = new Stub(ItemControlType.Toggle) { Height = 20 };
        var item = new AvonicItem { Content = control, EndContent = new AvonicNote { Content = "note" } };
        using var scene = InScene(item);

        Tap(scene, Box(item, scene.Window).TopLeft + new Point(4, item.Bounds.Height / 2));

        Assert.Equal(1, control.ItemTaps);
    });

    // ── The default slot's other property ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Content_Template_Is_Used() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x", ContentTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Text = "From a template" }) };
        using var scene = InScene(note);

        Assert.Equal("From a template", Text(note).Text);
    });

    [AvaloniaFact]
    public void The_Stub_Passes_On_What_A_ContentControl_Has()
    {
        var brush = new SolidColorBrush(Token);
        var note = new AvonicNote
        {
            Content = "x",
            BorderBrush = brush,
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(4),
            HorizontalContentAlignment = HorizontalAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Bottom,
        };
        using var scene = InScene(note);

        var presenter = Assert.IsType<Avalonia.Controls.Presenters.ContentPresenter>(Text(note).GetVisualParent());
        Assert.Same(brush, presenter.BorderBrush);
        Assert.Equal(new Thickness(3), presenter.BorderThickness);
        Assert.Equal(new CornerRadius(4), presenter.CornerRadius);
        Assert.Equal(HorizontalAlignment.Right, presenter.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Bottom, presenter.VerticalContentAlignment);
    }

    [AvaloniaFact]
    public void The_Stub_Uses_The_Content_Template()
    {
        var note = new AvonicNote { Content = "x", ContentTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock { Text = "From a template" }) };
        using var scene = InScene(note);

        Assert.Equal("From a template", Text(note).Text);
    }

    // ── 13. Automation ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_A_Button_Items_Name_Includes_The_Note() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true, Content = "Mail", EndContent = new AvonicNote { Content = "Yesterday" } };
        using var scene = InScene(item);

        Assert.Contains("Yesterday", ControlAutomationPeer.CreatePeerForElement(item).GetName());
    });

    // ── 14. Right to left ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_In_RTL_An_End_Note_Is_At_The_Left() => InstalledTheme.Run(_ =>
    {
        var note = new AvonicNote { Content = "x" };
        var item = new AvonicItem { EndContent = note, FlowDirection = FlowDirection.RightToLeft };
        using var scene = InScene(item);

        // A mirrored element's origin is its right-hand edge.
        Assert.Equal(16, Box(note, scene.Window).X - note.Bounds.Width, 1);
    });
}
