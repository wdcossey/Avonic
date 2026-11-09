using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// CSS's <c>min-content</c> and <c>min-width: auto</c>, which <see cref="FlexRow"/> and <see cref="RowPanel"/> share. Each is
/// measured on one line first, as the panels do.
/// </summary>
public class MinContentTests
{
    private static TextBlock Text(string text, TextWrapping wrapping = TextWrapping.Wrap) =>
        new() { Text = text, TextWrapping = wrapping, FontSize = 16 };

    /// <summary>Shows <paramref name="control"/> in a window, then measures it on one line.</summary>
    private static Scene OneLine(Control control)
    {
        var host = new Canvas();
        host.Children.Add(control);
        var scene = new Scene(host);
        control.Measure(Size.Infinity);
        return scene;
    }

    /// <summary>A word's width on its own, which layout rounds up to a whole pixel, so comparisons allow 1 a word.</summary>
    private static double Alone(string word)
    {
        var text = Text(word, TextWrapping.NoWrap);
        using var scene = OneLine(text);
        return text.DesiredSize.Width;
    }

    [AvaloniaFact]
    public void Text_That_Wraps_Is_Its_Longest_Word()
    {
        var text = Text("a bbbbbbbb cc");
        using var scene = OneLine(text);

        Assert.Equal(Alone("bbbbbbbb"), MinContent.Of(text), 1d);
        Assert.True(MinContent.Of(text) < text.DesiredSize.Width - 10);
    }

    [AvaloniaFact]
    public void Text_That_Does_Not_Wrap_Is_Its_Line()
    {
        var text = Text("a bbbbbbbb cc", TextWrapping.NoWrap);
        using var scene = OneLine(text);

        Assert.Equal(text.DesiredSize.Width, MinContent.Of(text));
        Assert.Equal(double.PositiveInfinity, MinContent.LongestWord(text));
    }

    [AvaloniaFact]
    public void Text_Keeps_Its_Margin_And_Padding()
    {
        var text = Text("a bbbbbbbb cc");
        text.Margin = new Thickness(3, 0, 4, 0);
        text.Padding = new Thickness(5, 0, 6, 0);
        using var scene = OneLine(text);

        Assert.Equal(Alone("bbbbbbbb") + 18, MinContent.Of(text), 1d);
    }

    [AvaloniaFact]
    public void Empty_Text_Has_No_Longest_Word()
    {
        var text = Text(string.Empty);
        using var scene = OneLine(text);

        Assert.Equal(0, MinContent.LongestWord(text));
    }

    [AvaloniaFact]
    public void A_Set_Width_Is_Its_Own_Minimum()
    {
        var text = Text("a bbbbbbbb cc");
        text.Width = 200;
        using var scene = OneLine(text);

        Assert.Equal(200, MinContent.Of(text));
    }

    [AvaloniaFact]
    public void A_Row_Adds_Its_Children_And_A_Column_Takes_The_Widest()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { Text("aaaa bb"), Text("cccccc d") } };
        var column = new StackPanel { Children = { Text("aaaa bb"), Text("cccccc d") } };
        using var rowScene = OneLine(row);
        using var columnScene = OneLine(column);

        Assert.Equal(Alone("aaaa") + Alone("cccccc"), MinContent.Of(row), 2d);
        Assert.Equal(Alone("cccccc"), MinContent.Of(column), 1d);
    }

    [AvaloniaFact]
    public void A_Box_Keeps_What_Is_Around_Its_Content()
    {
        var border = new Border
        {
            Margin = new Thickness(1, 0, 2, 0),
            Padding = new Thickness(3, 0, 4, 0),
            BorderThickness = new Thickness(5, 0, 6, 0),
            Child = Text("aaaa bb"),
        };
        using var scene = OneLine(border);

        Assert.Equal(21, MinContent.OwnBox(border));
        Assert.Equal(Alone("aaaa") + 21, MinContent.Of(border), 1d);
    }

    [AvaloniaFact]
    public void A_Hidden_Child_Takes_Nothing()
    {
        var hidden = Text("cccccccccccc");
        hidden.IsVisible = false;
        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { Text("aaaa bb"), hidden } };
        using var scene = OneLine(row);

        Assert.Equal(Alone("aaaa"), MinContent.Of(row), 1d);
    }

    [AvaloniaFact]
    public void A_Wrapping_Row_Is_Its_Widest_Items_Narrowest()
    {
        // ion-row wraps, so it doesn't add its items, however wide it was on one line.
        var row = new RowPanel { Children = { Text("aaaa bb"), Text("cccccc d"), Text("ee f") } };
        using var scene = OneLine(row);

        Assert.True(row.DesiredSize.Width > Alone("aaaa") + Alone("cccccc"));
        Assert.Equal(Alone("cccccc"), MinContent.Of(row), 1d);
    }

    [AvaloniaFact]
    public void A_Slot_Adds_Its_Childs_Box_Unless_The_Child_Is_Hidden()
    {
        var child = new Border { Margin = new Thickness(2, 0, 3, 0), Padding = new Thickness(4, 0, 5, 0), Child = Text("aaaa bb") };
        var slot = new ContentPresenter { Padding = new Thickness(1, 0, 1, 0), Content = child };
        using var scene = OneLine(slot);

        Assert.Same(child, MinContent.Slotted(slot));
        Assert.Equal(2 + 14, MinContent.Box(slot));

        child.IsVisible = false;
        Assert.Null(MinContent.Slotted(slot));
        Assert.Equal(2, MinContent.Box(slot));
    }

    [AvaloniaFact]
    public void An_Item_That_Clips_Or_Is_An_Icon_Has_Only_Its_Box()
    {
        var clipped = new Border { ClipToBounds = true, Padding = new Thickness(3, 0, 3, 0), Child = Text("aaaa bb") };
        var icon = new AvonicIcon { Margin = new Thickness(2, 0, 2, 0) };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { clipped, icon } };
        using var scene = OneLine(row);

        Assert.Equal(6, MinContent.OfItem(clipped));
        Assert.Equal(4, MinContent.OfItem(icon));

        // A slot looks through to what's in it.
        var slotted = new Border { ClipToBounds = true, Child = Text("aaaa bb") };
        var slot = new ContentPresenter { Content = slotted };
        using var slotScene = OneLine(slot);
        Assert.Equal(0, MinContent.OfItem(slot));
    }

    [AvaloniaFact]
    public void An_Item_That_Clips_Only_By_Default_Keeps_Its_Content()
    {
        // A TemplatedControl clips by default, which isn't overflow: hidden.
        var label = new ContentControl { Content = Text("aaaa bb") };
        using var scene = OneLine(label);

        Assert.True(label.ClipToBounds);
        Assert.Equal(MinContent.Of(label), MinContent.OfItem(label));
        Assert.True(MinContent.OfItem(label) > 0);
    }

    [AvaloniaFact]
    public void A_Wrapping_Row_Leaves_Out_A_Hidden_Column_It_Did_Not_Measure()
    {
        var hidden = Text("cccccccccccc dd");
        var container = new ContentPresenter { Content = hidden };
        var row = new RowPanel { Children = { Text("aaaa bb"), container } };
        using var scene = OneLine(row);
        Assert.True(container.DesiredSize.Width > Alone("cccccccccccc"));

        // display: none, as a column an ItemTemplate hid in its container is. The row skips it, so its size is the old one.
        hidden.IsVisible = false;
        row.InvalidateMeasure();
        row.Measure(Size.Infinity);

        Assert.Equal(Alone("aaaa"), MinContent.Of(row), 1d);
    }
}
