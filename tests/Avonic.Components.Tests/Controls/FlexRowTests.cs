using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>The algorithm on its own, with sizes that don't depend on a font. See docs/components/ion-item/PLAN.md.</summary>
public class FlexRowTests
{
    private const string Long = "Words that certainly do not fit on one line of a row this narrow, whatever the font turns out to be";

    private static Border Fixed(double width, double height = 10) => new() { Width = width, Height = height };

    /// <summary>A box of a width that isn't fixed, which can shrink as far as CSS lets it.</summary>
    private static TextBlock Text(string text = Long) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

    private static Scene InRow(double width, out FlexRow row, params Control[] children)
    {
        row = new FlexRow { Width = width, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        row.Children.AddRange(children);
        return new Scene(row);
    }

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static TextBlock Numbered(int lines) => new() { Text = string.Join(" ", Enumerable.Repeat("x", lines)), TextWrapping = TextWrapping.Wrap };

    [AvaloniaFact]
    public void Flex_Items_Share_What_Is_Left()
    {
        var one = new Border { [FlexRow.FlexProperty] = 1d };
        var other = new Border { [FlexRow.FlexProperty] = 1d };
        using var scene = InRow(300, out _, one, Fixed(100), other);

        Assert.Equal(100, one.Bounds.Width);
        Assert.Equal(100, other.Bounds.Width);
        Assert.Equal(200, other.Bounds.X);
    }

    [AvaloniaFact]
    public void Flex_Is_In_Proportion()
    {
        var one = new Border { [FlexRow.FlexProperty] = 1d };
        var two = new Border { [FlexRow.FlexProperty] = 2d };
        using var scene = InRow(300, out _, one, two);

        Assert.Equal(100, one.Bounds.Width);
        Assert.Equal(200, two.Bounds.Width);
    }

    [AvaloniaFact]
    public void A_Flex_Box_Is_Not_Below_Its_Padding_Border_And_Margin()
    {
        var padded = new Border { Padding = new Thickness(16, 0, 8, 0), BorderThickness = new Thickness(1, 0, 2, 0), Margin = new Thickness(3, 0, 4, 0), [FlexRow.FlexProperty] = 1d };
        var other = new Border { [FlexRow.FlexProperty] = 1d };
        using var scene = InRow(10, out _, padded, other);

        // 16 + 8 + 1 + 2 is the box, and the margins go outside it.
        Assert.Equal(27, padded.Bounds.Width);
        Assert.Equal(34, other.Bounds.X);
    }

    [AvaloniaFact]
    public void Padding_Counts_Before_Sharing()
    {
        var padded = new Border { Padding = new Thickness(0, 0, 16, 0), [FlexRow.FlexProperty] = 1d };
        var other = new Border { [FlexRow.FlexProperty] = 1d };
        using var scene = InRow(116, out _, other, padded);

        // Ionic's 171: (374 - 16 - 16) / 2. flex-basis: 0 keeps the padding, and the rest is shared.
        Assert.Equal(50, other.Bounds.Width);
        Assert.Equal(66, padded.Bounds.Width);
    }

    [AvaloniaFact]
    public void Content_Keeps_Its_Width_When_It_Fits()
    {
        var fixedOne = Fixed(40);
        var text = Text("x");
        var flex = new Border { [FlexRow.FlexProperty] = 1d };
        using var scene = InRow(300, out _, fixedOne, text, flex);

        Assert.Equal(40, fixedOne.Bounds.Width);
        Assert.True(text.Bounds.Width < 40);
        Assert.Equal(300, flex.Bounds.Right);
    }

    [AvaloniaFact]
    public void Text_Shrinks_And_Wraps_And_Something_Fixed_Does_Not()
    {
        var fixedOne = Fixed(40);
        var text = Text();
        using var scene = InRow(200, out var row, text, fixedOne);

        Assert.Equal(160, text.Bounds.Width);
        Assert.Equal(40, fixedOne.Bounds.Width);
        Assert.True(text.Bounds.Height > 40);
        Assert.Equal(text.Bounds.Height, row.Bounds.Height);
    }

    [AvaloniaFact]
    public void Shrinking_Is_In_Proportion_To_Size()
    {
        var one = Text();
        var other = Text(Long + " " + Long);
        using var scene = InRow(150, out _, one, other);

        one.Measure(Size.Infinity);
        other.Measure(Size.Infinity);
        var total = one.DesiredSize.Width + other.DesiredSize.Width;

        Assert.InRange(one.Bounds.Width, (150 * one.DesiredSize.Width / total) - 1, (150 * one.DesiredSize.Width / total) + 1);
        Assert.InRange(other.Bounds.Right, 149, 151);
    }

    [AvaloniaFact]
    public void Text_Does_Not_Shrink_Below_Its_Longest_Word()
    {
        var word = Text("Antidisestablishmentarianism x");
        using var scene = InRow(300, out _, Fixed(290), word);

        var alone = new TextBlock { Text = "Antidisestablishmentarianism" };
        alone.Measure(Size.Infinity);

        Assert.InRange(word.Bounds.Width, alone.DesiredSize.Width - 1, alone.DesiredSize.Width + 1);
    }

    [AvaloniaFact]
    public void A_Word_Is_Split_At_A_Hyphen()
    {
        var word = Text("well-known");
        using var scene = InRow(300, out _, Fixed(300), word);

        var whole = new TextBlock { Text = "well-known" };
        whole.Measure(Size.Infinity);

        Assert.True(word.Bounds.Width < whole.DesiredSize.Width - 5);
    }

    [AvaloniaFact]
    public void The_Last_Item_Can_Overflow_When_Minimums_Do_Not_Fit()
    {
        var first = Fixed(200);
        var second = Fixed(200);
        using var scene = InRow(300, out _, first, second);

        Assert.Equal(200, second.Bounds.Width);
        Assert.Equal(200, second.Bounds.X);
    }

    [AvaloniaFact]
    public void One_Hitting_Its_Minimum_Leaves_The_Rest_To_The_Others()
    {
        var word = Text("Antidisestablishmentarianism");
        var flex = new Border { [FlexRow.FlexProperty] = 1d };
        var slot = new FlexSlot { Content = word, [FlexRow.FlexProperty] = 0d };
        FlexRow.SetFlex(word, 1);
        using var scene = InRow(200, out _, flex, slot);

        var alone = new TextBlock { Text = "Antidisestablishmentarianism" };
        alone.Measure(Size.Infinity);
        Assert.InRange(alone.DesiredSize.Width, 101, 199);

        // Half would be 100, which is less than the word, so it keeps the word and the other takes what's left.
        Assert.Equal(alone.DesiredSize.Width, word.Bounds.Width, 0);
        Assert.InRange(flex.Bounds.Width, 199 - alone.DesiredSize.Width, 201 - alone.DesiredSize.Width);
    }

    [AvaloniaFact]
    public void A_Flex_Slot_Child_Is_Limited_To_The_Row_As_Max_Width_100_Is()
    {
        var word = Text("Antidisestablishmentarianismantidisestablishmentarianism");
        var slot = new FlexSlot { Content = word };
        FlexRow.SetFlex(word, 1);
        using var scene = InRow(100, out _, slot, new Border { [FlexRow.FlexProperty] = 1d });

        Assert.Equal(100, word.Bounds.Width);
    }

    [AvaloniaFact]
    public void A_Part_With_Flex_Has_No_Minimum()
    {
        var part = new FlexSlot { Content = Text("Antidisestablishmentarianism"), [FlexRow.FlexProperty] = 1d };
        using var scene = InRow(100, out _, Fixed(100), part);

        Assert.Equal(0, part.Bounds.Width);
    }

    [AvaloniaFact]
    public void An_Icon_Has_No_Minimum_And_Shrinks_With_The_Rest()
    {
        var icon = new AvonicIcon { IconName = "star", FontSize = 24 };
        var text = Text();
        using var scene = InRow(200, out _, text, icon);

        text.Measure(Size.Infinity);
        var total = text.DesiredSize.Width + 24;
        var expected = 24 * 200 / total;

        Assert.InRange(icon.Bounds.Width, expected - 1, expected + 1);
    }

    [AvaloniaFact]
    public void An_Icon_Keeps_Its_Size_Where_Nothing_Overflows()
    {
        var icon = new AvonicIcon { IconName = "star", FontSize = 24 };
        using var scene = InRow(200, out _, icon, Text("x"));

        Assert.Equal(24, icon.Bounds.Width);
    }

    [AvaloniaFact]
    public void Overflow_Hidden_Takes_Away_The_Minimum()
    {
        var clipped = new Border { ClipToBounds = true, Child = new Border { Width = 200 } };
        var other = Fixed(100);
        using var scene = InRow(100, out _, clipped, other);

        Assert.Equal(0, clipped.Bounds.Width);
        Assert.Equal(100, other.Bounds.Width);
    }

    [AvaloniaFact]
    public void Clipping_By_Default_Is_Not_Overflow_Hidden()
    {
        // A TemplatedControl clips unless told not to, so it has to keep its minimum.
        var button = new Avalonia.Controls.Button { Content = new Border { Width = 200 } };
        var other = Fixed(100);
        using var scene = InRow(100, out _, button, other);

        Assert.True(button.Bounds.Width >= 200);
    }

    [AvaloniaFact]
    public void A_Minimum_Width_Is_Kept_Around_Text()
    {
        var box = new ContentControl { MinWidth = 150, ClipToBounds = false, Content = Text() };
        using var scene = InRow(100, out _, box, Fixed(100));

        Assert.Equal(150, box.Bounds.Width);
    }

    [AvaloniaFact]
    public void Padding_Around_Text_Counts_In_The_Minimum()
    {
        var box = new Border { Padding = new Thickness(10, 0), Child = Text("x") };
        using var scene = InRow(100, out _, box, Fixed(100));

        var alone = new TextBlock { Text = "x" };
        alone.Measure(Size.Infinity);
        Assert.Equal(20 + alone.DesiredSize.Width, box.Bounds.Width, 0);
    }

    [AvaloniaFact]
    public void A_Row_Inside_Adds_Its_Minimums_Up()
    {
        var inner = new StackPanel { Orientation = Orientation.Horizontal, Children = { Fixed(30), Fixed(40) } };
        using var scene = InRow(100, out _, inner, Fixed(100));

        Assert.Equal(70, inner.Bounds.Width);
    }

    [AvaloniaFact]
    public void A_Stack_Inside_Takes_The_Largest_Minimum()
    {
        var inner = new StackPanel { Children = { Fixed(30), Fixed(40) } };
        using var scene = InRow(100, out _, inner, Fixed(100));

        Assert.Equal(40, inner.Bounds.Width);
    }

    [AvaloniaFact]
    public void Nowrap_Text_Keeps_Its_Width()
    {
        var text = new TextBlock { Text = Long, TextWrapping = TextWrapping.NoWrap };
        using var scene = InRow(100, out _, text, Fixed(100));

        var alone = new TextBlock { Text = Long };
        alone.Measure(Size.Infinity);
        Assert.Equal(alone.DesiredSize.Width, text.Bounds.Width, 0);
    }

    [AvaloniaFact]
    public void Empty_Text_Has_No_Minimum_And_Inlines_Count()
    {
        var empty = new TextBlock();
        var inlines = new TextBlock { TextWrapping = TextWrapping.Wrap };
        inlines.Inlines!.Add(new Avalonia.Controls.Documents.Run("Antidisestablishmentarianism"));
        using var scene = InRow(100, out _, empty, inlines, Fixed(100));

        Assert.Equal(0, empty.Bounds.Width);
        Assert.True(inlines.Bounds.Width > 50);
    }

    [AvaloniaFact]
    public void What_Is_Hidden_Takes_No_Space()
    {
        var hidden = new Border { Width = 50, IsVisible = false };
        var text = new Border { [FlexRow.FlexProperty] = 1d };
        using var scene = InRow(100, out _, hidden, text);

        Assert.Equal(0, text.Bounds.X);
        Assert.Equal(100, text.Bounds.Width);
    }

    [AvaloniaFact]
    public void Height_Is_The_Tallest_And_Every_Child_Gets_All_Of_It()
    {
        var tall = Fixed(10, 50);
        var stretched = new Border();
        using var scene = InRow(100, out var row, tall, stretched);

        Assert.Equal(50, row.Bounds.Height);
        Assert.Equal(50, stretched.Bounds.Height);
    }

    [AvaloniaFact]
    public void Height_Is_After_Wrapping()
    {
        var text = Numbered(100);
        using var scene = InRow(40, out var row, text);

        Assert.True(row.Bounds.Height > 40);
    }

    [AvaloniaFact]
    public void A_Change_Of_Flex_Lays_It_Out_Again()
    {
        var box = new Border();
        var other = new Border { [FlexRow.FlexProperty] = 1d };
        using var scene = InRow(100, out _, box, other);
        Assert.Equal(0, box.Bounds.Width);

        FlexRow.SetFlex(box, 1);
        Settle();

        Assert.Equal(50, box.Bounds.Width);
    }

    [AvaloniaFact]
    public void A_Change_Of_Flex_On_What_Is_In_A_Slot_Lays_It_Out_Again()
    {
        var child = new Border();
        var other = new Border { [FlexRow.FlexProperty] = 1d };
        using var scene = InRow(100, out _, new FlexSlot { Content = child }, other);
        Assert.Equal(0, child.Bounds.Width);

        FlexRow.SetFlex(child, 1);
        Settle();

        Assert.Equal(50, child.Bounds.Width);
    }

    [AvaloniaFact]
    public void In_A_Parent_That_Gives_No_Width_Everything_Is_One_Line()
    {
        var flex = new FlexSlot { Content = Text("xxxx"), [FlexRow.FlexProperty] = 1d };
        var text = Text("yyyy");
        var row = new FlexRow { Children = { flex, text } };
        using var scene = new Scene(new StackPanel { Orientation = Orientation.Horizontal, Children = { row } });

        Assert.True(flex.Bounds.Width > 0);
        Assert.Equal(row.Bounds.Width, flex.Bounds.Width + text.Bounds.Width, 0);
    }

    [AvaloniaFact]
    public void Arranged_Wider_Than_Measured_Gives_The_Rest_To_Flex()
    {
        var flex = new Border { [FlexRow.FlexProperty] = 1d, Child = Fixed(20) };
        var row = new FlexRow { Children = { flex, Fixed(10) } };
        row.Measure(Size.Infinity);
        row.Arrange(new Rect(0, 0, 100, 10));

        Assert.Equal(90, flex.Bounds.Width);
    }

    [AvaloniaFact]
    public void Arranged_Narrower_Than_Measured_Shrinks()
    {
        var text = Text("xxxx yyyy");
        var row = new FlexRow { Children = { text, Fixed(10) } };
        row.Measure(Size.Infinity);
        var width = row.DesiredSize.Width;
        row.Arrange(new Rect(0, 0, width - 5, 40));

        Assert.Equal(width - 15, text.Bounds.Width, 0);
    }

    // ── FlexSlot ─────────────────────────────────────────────────────────────────────────────

    private static (FlexSlot Slot, Control Child, Scene Scene) InTallSlot(Control child, VerticalAlignment? alignItems = VerticalAlignment.Center)
    {
        var slot = new FlexSlot { Content = child };
        if (alignItems is { } alignment)
        {
            slot.VerticalContentAlignment = alignment;
        }

        var scene = InRow(100, out _, slot, Fixed(10, 100));
        return (slot, child, scene);
    }

    [AvaloniaFact]
    public void A_Slot_Fills_The_Row()
    {
        var (slot, _, scene) = InTallSlot(Fixed(10));
        using (scene)
        {
            Assert.Equal(100, slot.Bounds.Height);
            Assert.Equal(0, slot.Bounds.Y);
        }
    }

    [AvaloniaFact]
    public void A_Child_That_Says_Nothing_Follows_Align_Items()
    {
        var (_, child, scene) = InTallSlot(new Border { Height = 10 });
        using (scene)
        {
            Assert.Equal(45, child.Bounds.Y);
        }
    }

    [AvaloniaTheory]
    [InlineData(VerticalAlignment.Top, 0)]
    [InlineData(VerticalAlignment.Bottom, 90)]
    public void Align_Items_Follows_The_Slot(VerticalAlignment alignItems, double y)
    {
        var (_, child, scene) = InTallSlot(new Border { Height = 10 }, alignItems);
        using (scene)
        {
            Assert.Equal(y, child.Bounds.Y);
        }
    }

    [AvaloniaFact]
    public void Align_Items_Stretch_Stretches()
    {
        var (_, child, scene) = InTallSlot(new Border(), VerticalAlignment.Stretch);
        using (scene)
        {
            Assert.Equal(100, child.Bounds.Height);
        }
    }

    [AvaloniaTheory]
    [InlineData(VerticalAlignment.Top, 0)]
    [InlineData(VerticalAlignment.Bottom, 90)]
    [InlineData(VerticalAlignment.Center, 45)]
    public void Align_Self_Beats_Align_Items(VerticalAlignment alignSelf, double y)
    {
        var (_, child, scene) = InTallSlot(new Border { Height = 10, VerticalAlignment = alignSelf }, VerticalAlignment.Bottom);
        using (scene)
        {
            Assert.Equal(y, child.Bounds.Y);
        }
    }

    [AvaloniaFact]
    public void Align_Self_Stretch_Beats_Align_Items()
    {
        var (_, child, scene) = InTallSlot(new Border { VerticalAlignment = VerticalAlignment.Stretch });
        using (scene)
        {
            Assert.Equal(100, child.Bounds.Height);
        }
    }

    [AvaloniaFact]
    public void A_Change_Of_Align_Self_Moves_It()
    {
        var (_, child, scene) = InTallSlot(new Border { Height = 10 });
        using (scene)
        {
            Assert.Equal(45, child.Bounds.Y);

            child.VerticalAlignment = VerticalAlignment.Top;
            Settle();

            Assert.Equal(0, child.Bounds.Y);
        }
    }

    [AvaloniaFact]
    public void Clearing_Align_Self_Goes_Back_To_Align_Items()
    {
        var (_, child, scene) = InTallSlot(new Border { Height = 10, VerticalAlignment = VerticalAlignment.Top });
        using (scene)
        {
            child.ClearValue(Layoutable.VerticalAlignmentProperty);
            Settle();

            Assert.Equal(45, child.Bounds.Y);
        }
    }

    [AvaloniaFact]
    public void A_Replaced_Child_Is_The_One_Watched()
    {
        var first = new Border { Height = 10 };
        var second = new Border { Height = 10 };
        var slot = new FlexSlot { Content = first, VerticalContentAlignment = VerticalAlignment.Center };
        using var scene = InRow(100, out _, slot, Fixed(10, 100));

        slot.Content = second;
        Settle();
        second.VerticalAlignment = VerticalAlignment.Top;
        Settle();

        Assert.Equal(0, second.Bounds.Y);
    }

    [AvaloniaFact]
    public void A_Panel_Fills_The_Slot()
    {
        var (_, child, scene) = InTallSlot(new Grid());
        using (scene)
        {
            Assert.Equal(100, child.Bounds.Height);
        }
    }

    [AvaloniaFact]
    public void A_Horizontal_Stack_Fills_The_Slot()
    {
        var (_, child, scene) = InTallSlot(new StackPanel { Orientation = Orientation.Horizontal });
        using (scene)
        {
            Assert.Equal(100, child.Bounds.Height);
        }
    }

    [AvaloniaFact]
    public void A_Vertical_Stack_Is_A_Group_That_Is_Centred()
    {
        var stack = new StackPanel { Children = { Fixed(10, 20) } };
        var (_, child, scene) = InTallSlot(stack);
        using (scene)
        {
            Assert.Equal(40, child.Bounds.Y);
            Assert.Equal(20, child.Bounds.Height);
        }
    }

    [AvaloniaFact]
    public void A_Panel_That_Says_Where_Goes_There()
    {
        var (_, child, scene) = InTallSlot(new Grid { Height = 10, VerticalAlignment = VerticalAlignment.Bottom });
        using (scene)
        {
            Assert.Equal(90, child.Bounds.Y);
        }
    }

    [AvaloniaFact]
    public void An_Empty_Slot_Takes_No_Space()
    {
        var slot = new FlexSlot();
        var flex = new Border { [FlexRow.FlexProperty] = 1d };
        using var scene = InRow(100, out _, slot, flex);

        Assert.Equal(0, slot.Bounds.Width);
        Assert.Equal(100, flex.Bounds.Width);
    }

    [AvaloniaFact]
    public void A_Slot_Is_Styled_As_A_ContentPresenter()
    {
        var slot = new FlexSlot { Content = "x" };
        var root = new FlexRow { Children = { slot } };
        root.Styles.Add(new Style(x => x.OfType<Avalonia.Controls.Presenters.ContentPresenter>())
        {
            Setters = { new Setter(Control.TagProperty, "styled") },
        });
        using var scene = new Scene(root);

        Assert.Equal("styled", slot.Tag);
    }
}
