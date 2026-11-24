using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Ionic's <c>hostContext('ion-toolbar', el)</c> and <c>hostContext('ion-toolbar[color]', el)</c>: <c>el.closest(selector) !== null</c>,
/// which the button, the menu button, the back button, the segment, its buttons and the breadcrumbs read at render.
/// </summary>
public class ToolbarHostContextTests
{
    [AvaloniaFact]
    public void Outside_A_Toolbar_Neither_Matches() => InstalledTheme.Run(_ =>
    {
        var loose = new Border();
        Assert.Equal(new ToolbarHostContext(false, false), ToolbarHostContext.Of(loose));

        using var scene = new Scene(new StackPanel { Children = { loose } });
        Assert.Equal(new ToolbarHostContext(false, false), ToolbarHostContext.Of(loose));
    });

    [AvaloniaFact]
    public void Any_Slot_At_Any_Depth_Is_In_The_Toolbar() => InstalledTheme.Run(_ =>
    {
        // closest() walks every ancestor, so a child inside another element in a slot matches as a slotted one does.
        var start = new Border();
        var deeper = new Border();
        var end = new Border();
        using var scene = new Scene(new AvonicToolbar
        {
            StartContent = start,
            Content = new StackPanel { Children = { new Border { Child = deeper } } },
            EndContent = end,
        });

        foreach (var control in new[] { start, deeper, end })
        {
            Assert.Equal(new ToolbarHostContext(true, false), ToolbarHostContext.Of(control));
        }
    });

    [AvaloniaFact]
    public void An_Empty_Color_Is_Still_A_Color_Attribute() => InstalledTheme.Run(_ =>
    {
        // ion-toolbar[color] matches color="", which gives no ion-color class.
        var child = new Border();
        using var scene = new Scene(new AvonicToolbar { Color = "", Content = child });

        Assert.Equal(new ToolbarHostContext(true, true), ToolbarHostContext.Of(child));
    });

    [AvaloniaFact]
    public void A_Coloured_Toolbar_Need_Not_Be_The_Nearest() => InstalledTheme.Run(_ =>
    {
        // closest('ion-toolbar[color]') finds the coloured one past a plain one, either way round.
        var inPlainInColoured = new Border();
        var inColouredInPlain = new Border();
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicToolbar { Color = "danger", Content = new AvonicToolbar { Content = inPlainInColoured } },
                new AvonicToolbar { Content = new AvonicToolbar { Color = "danger", Content = inColouredInPlain } },
            },
        });

        Assert.Equal(new ToolbarHostContext(true, true), ToolbarHostContext.Of(inPlainInColoured));
        Assert.Equal(new ToolbarHostContext(true, true), ToolbarHostContext.Of(inColouredInPlain));
    });

    [AvaloniaFact]
    public void It_Reads_The_Tree_As_It_Is_Each_Time() => InstalledTheme.Run(_ =>
    {
        // Ionic reads it at each render, so a toolbar's colour set later, or a move out of the toolbar, shows at the next read.
        var child = new Border();
        var toolbar = new AvonicToolbar { Content = child };
        var outside = new StackPanel();
        using var scene = new Scene(new StackPanel { Children = { toolbar, outside } });
        Assert.Equal(new ToolbarHostContext(true, false), ToolbarHostContext.Of(child));

        toolbar.Color = "primary";
        Assert.Equal(new ToolbarHostContext(true, true), ToolbarHostContext.Of(child));

        toolbar.Color = null;
        Assert.Equal(new ToolbarHostContext(true, false), ToolbarHostContext.Of(child));

        toolbar.Content = null;
        outside.Children.Add(child);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new ToolbarHostContext(false, false), ToolbarHostContext.Of(child));
    });

    [AvaloniaFact]
    public void Set_Gives_And_Takes_Both_Classes() => InstalledTheme.Run(_ =>
    {
        var child = new Border();
        var toolbar = new AvonicToolbar { Color = "primary", Content = child };
        using var scene = new Scene(toolbar);
        var classes = (IPseudoClasses)child.Classes;

        ToolbarHostContext.Set(classes, child);
        Assert.Contains(":in-toolbar", child.Classes);
        Assert.Contains(":in-toolbar-color", child.Classes);

        toolbar.Color = null;
        ToolbarHostContext.Set(classes, child);
        Assert.Contains(":in-toolbar", child.Classes);
        Assert.DoesNotContain(":in-toolbar-color", child.Classes);

        toolbar.Content = null;
        Dispatcher.UIThread.RunJobs();
        ToolbarHostContext.Set(classes, child);
        Assert.DoesNotContain(":in-toolbar", child.Classes);
        Assert.DoesNotContain(":in-toolbar-color", child.Classes);
    });
}
