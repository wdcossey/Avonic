using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Tests.Behaviors;

public class StyleValueTests
{
    [AvaloniaFact]
    public void It_Sets_A_Value_At_Style_Priority_And_Takes_It_Away()
    {
        var border = new Border();
        var value = new StyleValue<double>();
        border.Bind(Layoutable.WidthProperty, value, BindingPriority.Style);
        Assert.True(double.IsNaN(border.Width));

        value.Set(40);
        Assert.Equal(40, border.Width);

        // Below a local value, as a style is.
        border.Width = 10;
        Assert.Equal(10, border.Width);
        border.ClearValue(Layoutable.WidthProperty);
        Assert.Equal(40, border.Width);

        value.Clear();
        Assert.True(double.IsNaN(border.Width));

        value.SetWhen(true, 30);
        Assert.Equal(30, border.Width);
        value.SetWhen(false, 30);
        Assert.True(double.IsNaN(border.Width));
    }

    [AvaloniaFact]
    public void A_Value_That_Is_The_Types_Default_Still_Counts()
    {
        var border = new Border();
        var hidden = new StyleValue<bool>();
        border.Bind(Visual.IsVisibleProperty, hidden, BindingPriority.Style);

        hidden.Set(false);
        Assert.False(border.IsVisible);
        hidden.Clear();
        Assert.True(border.IsVisible);
    }

    // Of two values at one priority the later wins: a style already there when the value turns on still wins, as the binding
    // was made first.
    [AvaloniaFact]
    public void An_Apps_Style_Wins_Even_When_The_Value_Turns_On_After_It()
    {
        var border = new Border();
        var value = new StyleValue<double>();
        border.Bind(Layoutable.WidthProperty, value, BindingPriority.Style);
        var window = new Window
        {
            Content = border,
            Styles = { new Style(x => x.OfType<Border>()) { Setters = { new Setter(Layoutable.WidthProperty, 99.0) } } },
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        value.Set(40);
        Assert.Equal(99, border.Width);
        window.Close();
    }
}
