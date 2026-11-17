using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;
using static Avonic.Components.Tests.Controls.AvonicPopoverTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// B9 to B17 in docs/components/ion-popover/PLAN.md: the trigger actions, dismiss on select, the backdrop, the keyboard, nested
/// popovers, focus, mounting, and the event, from <c>popover.js</c> and <c>utils.js</c>, against M6 to M9 in
/// <c>ref/measurements.txt</c>.
/// </summary>
public class AvonicPopoverInteractionTests
{
    private static Point Centre(Visual visual, Visual relativeTo) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(relativeTo)!.Value).Center;

    /// <summary>A menu of button items, as M6's: five, the fourth disabled.</summary>
    private static (AvonicPopover Popover, AvonicItem[] Items) Menu(int count = 5, int disabled = 3)
    {
        var items = Enumerable.Range(0, count)
            .Select(i => new AvonicItem { Button = true, Content = $"Item {i + 1}", IsEnabled = i != disabled })
            .ToArray();
        var list = new StackPanel();
        list.Children.AddRange(items);
        return (new AvonicPopover { Content = list }, items);
    }

    // ── B9: trigger actions ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_A_Click_On_The_Trigger_Presents_It_Below_The_Trigger()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 200);
        var popover = stage.Watch(Popover());
        popover.Trigger = "Trigger";
        stage.Page.Children.Add(popover);
        Stage.Settle();

        stage.Click(Centre(trigger, stage.Window));
        stage.After(Entered);
        Assert.Contains("didPresent@320", stage.Log);
        Assert.Equal(new Rect(128, 240, 250, 120), Box(popover, stage.Window));
    }

    [AvaloniaFact]
    public void B9_A_Buttons_Click_Trigger_With_Reference_Event_Opens_At_The_Click()
    {
        // A button's Click comes during the release, with no point of its own: the release's is the DOM click's clientX, clientY.
        using var stage = new Stage();
        var button = new AvonicButton { Name = "Button", Content = "Open", Width = 200, Height = 40 };
        Canvas.SetLeft(button, 20);
        Canvas.SetTop(button, 300);
        stage.Page.Children.Add(button);
        stage.Names.Register("Button", button);
        var popover = Popover();
        popover.Trigger = "Button";
        popover.Reference = PopoverReference.Event;
        stage.Page.Children.Add(popover);
        Stage.Settle();

        stage.Click(new Point(60, 310));
        stage.After(Entered);
        Assert.Equal(new Rect(60, 311, 250, 120), Box(popover, stage.Window));
    }

    [AvaloniaFact]
    public void B6_From_A_Tap_On_A_Buttons_Text_Places_It_By_The_Button()
    {
        // Avalonia's tap comes from the text inside the button's template; a DOM click's target is the button, its shadow's host.
        using var stage = new Stage();
        var button = new AvonicButton { Content = "Open", Width = 180, Height = 40 };
        Canvas.SetLeft(button, 30);
        Canvas.SetTop(button, 300);
        stage.Page.Children.Add(button);
        var popover = Popover();
        popover.Size = PopoverSize.Cover;
        button.AddHandler(InputElement.TappedEvent, (_, e) =>
        {
            Assert.NotSame(button, e.Source);
            popover.Present(PopoverEvent.From(e));
        });
        Stage.Settle();

        stage.Click(Centre(button, stage.Window));
        stage.After(Entered);
        var box = new Rect(button.Bounds.Size).TransformToAABB(button.TransformToVisual(stage.Window)!.Value);
        Assert.Equal(new Rect(box.X, box.Bottom, 180, 120), Box(popover, stage.Window));
    }

    [AvaloniaFact]
    public void B6_A_Target_In_A_Popup_Places_It_Beside_It()
    {
        // Headless popups are in the window's overlay layer; a popup that's a top level of its own goes through the screen,
        // which a headless test can't make.
        using var stage = new Stage();
        var inside = new Border { Width = 100, Height = 40, Background = Brushes.Gray };
        var anchor = stage.Trigger(60, 200, name: "Anchor");
        var popup = new Avalonia.Controls.Primitives.Popup
        {
            PlacementTarget = anchor, Placement = PlacementMode.Bottom, Child = inside, IsLightDismissEnabled = false,
        };
        stage.Page.Children.Add(popup);
        popup.IsOpen = true;
        Stage.Settle();
        var expected = stage.Window.PointToClient(inside.PointToScreen(default));

        var popover = Popover();
        popover.Present(new PopoverEvent(inside));
        stage.After(Entered);
        Assert.Equal(expected.X, Box(popover, stage.Window).X, 0.5);
        Assert.Equal(expected.Y + 40, Box(popover, stage.Window).Y, 0.5);
        popup.IsOpen = false;
    }

    [AvaloniaFact]
    public void B9_Hover_Presents_After_100ms_And_A_Frame()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 200);
        var popover = stage.Watch(Popover());
        popover.Trigger = "Trigger";
        popover.TriggerAction = PopoverTriggerAction.Hover;
        stage.Page.Children.Add(popover);
        stage.Move(new Point(5, 5));

        stage.Move(Centre(trigger, stage.Window));
        stage.After(99);
        Assert.Empty(stage.Log);

        // The timeout at 100, then the frame at 116, as M8's 108 ms in Chromium.
        stage.After(16);
        Assert.Empty(stage.Log);
        stage.After(1);
        Assert.Equal(["ionPopoverWillPresent@116", "willPresent@116"], stage.Log);

        // Its backdrop is in it, so leaving the trigger for it doesn't dismiss it.
        stage.After(Entered);
        stage.Move(new Point(5, 5));
        stage.After(Left);
        Assert.DoesNotContain(stage.Log, l => l.StartsWith("willDismiss", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B9_Leaving_A_Hover_Trigger_Before_100ms_Presents_Nothing()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 200);
        var popover = stage.Watch(Popover());
        popover.Trigger = "Trigger";
        popover.TriggerAction = PopoverTriggerAction.Hover;
        stage.Page.Children.Add(popover);
        stage.Move(new Point(5, 5));

        stage.Move(Centre(trigger, stage.Window));
        stage.After(40);
        stage.Move(new Point(5, 5));
        stage.After(300);
        Assert.Empty(stage.Log);
    }

    [AvaloniaFact]
    public void B9_Leaving_A_Hover_Trigger_For_Something_Outside_The_Popover_Dismisses_It_Alone()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 200);
        var popover = stage.Watch(Popover());
        popover.Trigger = "Trigger";
        popover.TriggerAction = PopoverTriggerAction.Hover;
        popover.ShowBackdrop = false;
        stage.Page.Children.Add(popover);
        popover.Present(new PopoverEvent(trigger));
        stage.After(Entered);
        stage.Log.Clear();

        // With a backdrop that doesn't cover the trigger: an app's own one, here taken out of hit testing.
        popover.Backdrop!.IsHitTestVisible = false;
        stage.Move(Centre(trigger, stage.Window));
        stage.Move(new Point(5, 5));
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("willDismiss(,)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B9_A_Context_Request_Presents_It_At_Its_Point_And_Is_Handled()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(0, 0, 390, 844);
        var popover = stage.Watch(Popover());
        popover.Trigger = "Trigger";
        popover.TriggerAction = PopoverTriggerAction.ContextMenu;
        popover.Reference = PopoverReference.Event;
        stage.Page.Children.Add(popover);
        var handled = false;
        stage.Window.AddHandler(Control.ContextRequestedEvent, (_, e) => handled = e.Handled, handledEventsToo: true);

        // A left click does nothing.
        stage.Click(new Point(247, 41));
        stage.After(Entered);
        Assert.Empty(stage.Log);

        // M9: a right click at 247.25, 41: at 128, 42.
        stage.Click(new Point(247, 41), MouseButton.Right);
        stage.After(Entered);
        Assert.True(handled);
        Assert.Equal(new Rect(128, 42, 250, 120), Box(popover, stage.Window));
    }

    [AvaloniaFact]
    public void B9_A_Change_Of_TriggerAction_Moves_The_Listeners()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 200);
        var popover = stage.Watch(Popover());
        popover.Trigger = "Trigger";
        stage.Page.Children.Add(popover);
        popover.TriggerAction = PopoverTriggerAction.ContextMenu;
        Stage.Settle();

        stage.Click(Centre(trigger, stage.Window));
        stage.After(Entered);
        Assert.Empty(stage.Log);
        stage.Click(Centre(trigger, stage.Window), MouseButton.Right);
        stage.After(Entered);
        Assert.Contains(stage.Log, l => l.StartsWith("didPresent", StringComparison.Ordinal));
    }

    // ── B10: dismiss on select ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_DismissOnSelect_A_Click_In_The_Content_Dismisses_It_With_No_Role()
    {
        using var stage = new Stage();
        var (popover, items) = Menu();
        stage.Watch(popover);
        popover.Present(stage.Window);
        stage.After(Entered);

        // Off: a click does nothing.
        stage.Click(Centre(items[0], stage.Window));
        stage.After(Left);
        Assert.DoesNotContain(stage.Log, l => l.StartsWith("willDismiss", StringComparison.Ordinal));

        popover.DismissOnSelect = true;
        stage.Click(Centre(items[1], stage.Window));
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B10_DismissOnSelect_A_Keys_Click_On_An_Item_Dismisses_It()
    {
        using var stage = new Stage();
        var (popover, items) = Menu();
        stage.Watch(popover);
        popover.DismissOnSelect = true;
        popover.Present(stage.Window);
        stage.After(Entered);

        items[0].Focus(NavigationMethod.Tab);
        stage.Key(PhysicalKey.Enter);
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B10_A_Click_A_Hover_Trigger_Stops_Doesnt_Dismiss_It()
    {
        using var stage = new Stage();
        var (popover, items) = Menu();
        var child = Popover();
        child.Trigger = "Item";
        child.TriggerAction = PopoverTriggerAction.Hover;
        stage.Names.Register("Item", items[2]);
        ((StackPanel)popover.Content!).Children.Add(child);
        stage.Watch(popover);
        popover.DismissOnSelect = true;
        stage.Page.Children.Add(popover);
        popover.Present();
        stage.After(Entered);

        stage.Click(Centre(items[2], stage.Window));
        stage.After(Left);
        Assert.DoesNotContain(stage.Log, l => l.StartsWith("willDismiss", StringComparison.Ordinal));
    }

    // ── B11: the backdrop ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_The_Backdrop_Dismisses_With_Its_Role_Unless_BackdropDismiss_Is_False()
    {
        using var stage = new Stage();
        var popover = stage.Watch(Popover());
        popover.BackdropDismiss = false;
        popover.Present(stage.Window);
        stage.After(Entered);
        stage.Click(new Point(5, 830));
        stage.Key(PhysicalKey.Escape);
        stage.After(Left);
        Assert.DoesNotContain(stage.Log, l => l.StartsWith("willDismiss", StringComparison.Ordinal));
        Assert.False(popover.Backdrop!.Tappable);

        popover.BackdropDismiss = true;
        stage.Click(new Point(5, 830));
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,backdrop)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B11_ShowBackdrop_False_Is_Clear_And_Still_Takes_The_Tap()
    {
        using var stage = new Stage();
        var popover = stage.Watch(Popover());
        popover.ShowBackdrop = false;
        popover.Present(stage.Window);
        stage.After(Entered);
        Assert.False(popover.Backdrop!.Visible);
        stage.Click(new Point(5, 830));
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,backdrop)", StringComparison.Ordinal));
    }

    // ── B12: the keyboard ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_Arrows_Home_And_End_Move_Through_The_Enabled_Items_At_The_Next_Frame()
    {
        using var stage = new Stage();
        var (popover, items) = Menu();
        popover.Present(stage.Window);
        stage.After(Entered);
        Assert.Same(popover, stage.Focused);

        // M6: from the host, the first; the disabled fourth skipped; the ends stay.
        foreach (var (key, expected) in new[]
                 {
                     (PhysicalKey.ArrowDown, 0), (PhysicalKey.ArrowDown, 1), (PhysicalKey.ArrowUp, 0), (PhysicalKey.End, 4),
                     (PhysicalKey.ArrowDown, 4), (PhysicalKey.Home, 0), (PhysicalKey.ArrowUp, 0), (PhysicalKey.ArrowDown, 1),
                     (PhysicalKey.ArrowDown, 2), (PhysicalKey.ArrowDown, 4),
                 })
        {
            var before = stage.Focused;
            stage.Key(key);
            Assert.Same(before, stage.Focused);
            stage.After(16);
            Assert.Same(items[expected], stage.Focused);
        }
    }

    [AvaloniaFact]
    public void B12_The_Items_Of_A_Popover_Inside_Dont_Count()
    {
        using var stage = new Stage();
        var (popover, items) = Menu(2, -1);
        var (inner, innerItems) = Menu(2, -1);
        inner.KeepContentsMounted = true;
        ((StackPanel)popover.Content!).Children.Add(inner);
        stage.Page.Children.Add(popover);
        popover.Present();
        stage.After(Entered);

        // The inner one's items are in the tree, kept mounted, but not this popover's: End is the second of its own.
        Assert.True(innerItems[0].IsAttachedToVisualTree());
        stage.Key(PhysicalKey.End);
        stage.After(16);
        Assert.Same(items[1], stage.Focused);
    }

    [AvaloniaFact]
    public void B12_The_Arrows_Are_Handled_Only_From_The_Popover_Or_An_Item()
    {
        using var stage = new Stage();
        var box = new TextBox();
        var (popover, items) = Menu();
        ((StackPanel)popover.Content!).Children.Insert(0, box);
        var handled = new List<bool>();
        stage.Window.AddHandler(InputElement.KeyDownEvent, (_, e) => handled.Add(e.Handled), RoutingStrategies.Bubble, handledEventsToo: true);
        popover.Present(stage.Window);
        stage.After(Entered);

        stage.Key(PhysicalKey.Home);
        stage.After(16);
        Assert.Same(items[0], stage.Focused);
        Assert.True(handled[^1]);

        box.Focus();
        stage.Key(PhysicalKey.ArrowDown);
        stage.After(16);
        Assert.Same(box, stage.Focused);
    }

    [AvaloniaFact]
    public void B12_The_Keys_Do_Nothing_After_The_Dismiss()
    {
        using var stage = new Stage();
        var (popover, items) = Menu();
        popover.Present(stage.Window);
        stage.After(Entered);
        popover.Dismiss();
        stage.After(Left);
        var outside = new Button { Content = "Outside" };
        stage.Page.Children.Add(outside);
        outside.Focus();

        var handled = false;
        stage.Window.AddHandler(InputElement.KeyDownEvent, (_, e) => handled = e.Handled, RoutingStrategies.Bubble, handledEventsToo: true);
        stage.Key(PhysicalKey.End);
        Assert.False(handled);
    }

    // ── B13: nested ─────────────────────────────────────────────────────────────────────────

    /// <summary>M6 and M7's menu: a click trigger with dismiss-on-select, five items, a nested popover on the third, side end.</summary>
    private static (AvonicPopover Parent, AvonicItem[] Items, AvonicPopover Child, AvonicItem[] ChildItems) Nested(
        Stage stage, PopoverTriggerAction childAction = PopoverTriggerAction.Click)
    {
        var opener = stage.Trigger(18, 20, 74, 36, name: "Opener");
        var (parent, items) = Menu();
        parent.Trigger = "Opener";
        parent.DismissOnSelect = true;
        var (child, childItems) = Menu(2, -1);
        child.Trigger = "More";
        child.TriggerAction = childAction;
        child.Side = PopoverSide.End;
        child.DismissOnSelect = true;
        stage.Names.Register("More", items[2]);
        ((StackPanel)parent.Content!).Children.Add(child);
        stage.Page.Children.Add(parent);
        stage.Watch(parent, "p:");
        stage.Watch(child, "c:");
        Stage.Settle();
        stage.Click(Centre(opener, stage.Window));
        stage.After(Entered);
        return (parent, items, child, childItems);
    }

    [AvaloniaFact]
    public void B13_A_Nested_Popover_Has_No_Backdrop_And_Sits_5px_Over()
    {
        using var stage = new Stage();
        var (parent, items, child, _) = Nested(stage);
        Assert.Same(parent, child.ParentPopover);
        stage.Click(Centre(items[2], stage.Window));
        stage.After(Entered);

        Assert.False(child.Backdrop!.IsVisible);
        Assert.False(parent.Backdrop!.IsVisible is false);

        // M7: clamped right to 128, then --offset-x -5 on side end.
        var box = Box(child, stage.Window);
        Assert.Equal(123, box.X, 0.5);
        Assert.Equal(Box(parent, stage.Window).Y + 2 * items[0].Bounds.Height, box.Y, 0.5);
    }

    [AvaloniaFact]
    public void B13_The_Click_On_A_Nested_Trigger_Leaves_A_DismissOnSelect_Parent()
    {
        using var stage = new Stage();
        var (_, items, _, _) = Nested(stage);
        stage.Log.Clear();
        stage.Click(Centre(items[2], stage.Window));
        stage.After(Entered);
        Assert.Equal(["c:ionPopoverWillPresent@320", "c:willPresent@320", "c:ionPopoverDidPresent@640", "c:didPresent@640"], stage.Log);
    }

    [AvaloniaFact]
    public void B13_A_Click_In_The_Child_Dismisses_The_Child_Then_The_Parent()
    {
        using var stage = new Stage();
        var (_, items, _, childItems) = Nested(stage);
        stage.Click(Centre(items[2], stage.Window));
        stage.After(Entered);
        stage.Log.Clear();

        stage.Click(Centre(childItems[0], stage.Window));
        stage.After(Left);
        Assert.Equal(["c:ionPopoverWillDismiss", "c:willDismiss", "p:ionPopoverWillDismiss", "p:willDismiss"],
            stage.Log.Where(l => l.Contains("WillDismiss", StringComparison.Ordinal) || l.Contains("willDismiss", StringComparison.Ordinal))
                .Select(l => l[..l.IndexOf('(')]));
    }

    [AvaloniaFact]
    public void B13_Escape_Dismisses_Both_With_Backdrop_And_The_Parents_Backdrop_Takes_The_Child()
    {
        using var stage = new Stage();
        var (parent, items, child, _) = Nested(stage);
        stage.Click(Centre(items[2], stage.Window));
        stage.After(Entered);
        stage.Log.Clear();
        stage.Key(PhysicalKey.Escape);
        stage.After(Left);
        Assert.Equal(["c:willDismiss(,backdrop)", "p:willDismiss(,backdrop)"],
            stage.Log.Where(l => l.Contains(":willDismiss", StringComparison.Ordinal)).Select(l => l[..l.IndexOf('@')]));

        // M7: the parent's backdrop, then the child with no role.
        stage.Click(Centre((Visual)stage.Names.Find("Opener")!, stage.Window));
        stage.After(Entered);
        stage.Click(Centre(items[2], stage.Window));
        stage.After(Entered);
        stage.Log.Clear();
        stage.Click(new Point(380, 830));
        stage.After(Left);
        Assert.Equal(["p:willDismiss(,backdrop)", "c:willDismiss(,)"],
            stage.Log.Where(l => l.Contains(":willDismiss", StringComparison.Ordinal)).Select(l => l[..l.IndexOf('@')]));
        Assert.False(parent.IsShown || child.IsShown);
    }

    [AvaloniaFact]
    public void B13_A_Click_On_Another_Parent_Item_Dismisses_The_Child_Alone_Then_DismissOnSelect_The_Parent()
    {
        using var stage = new Stage();
        var (parent, items, child, _) = Nested(stage);
        parent.DismissOnSelect = false;
        stage.Click(Centre(items[2], stage.Window));
        stage.After(Entered);
        stage.Log.Clear();

        stage.Click(Centre(items[0], stage.Window));
        stage.After(Left);
        Assert.Equal(["c:willDismiss(,)"], stage.Log.Where(l => l.Contains(":willDismiss", StringComparison.Ordinal)).Select(l => l[..l.IndexOf('@')]));
        Assert.True(parent.IsShown);
        Assert.False(child.IsShown);
    }

    [AvaloniaFact]
    public void B13_Dismiss_With_DismissParentPopover_False_Leaves_The_Parent()
    {
        using var stage = new Stage();
        var (parent, items, child, _) = Nested(stage);
        stage.Click(Centre(items[2], stage.Window));
        stage.After(Entered);
        child.Dismiss(1, "r", dismissParentPopover: false);
        stage.After(Left);
        Assert.True(parent.IsShown);
        Assert.False(child.IsShown);

        stage.Click(Centre(items[2], stage.Window));
        stage.After(Entered);
        child.Dismiss(2, "r");
        Stage.Settle();
        stage.After(Left);
        Assert.False(parent.IsShown, string.Join(" ", stage.Log));
        Assert.True(stage.Log.Any(l => l.StartsWith("p:didDismiss(2,r)", StringComparison.Ordinal)), string.Join(" ", stage.Log));
    }

    [AvaloniaFact]
    public void B13_A_Hover_Child_Goes_When_The_Pointer_Comes_Into_The_Parent_Off_Its_Trigger()
    {
        using var stage = new Stage();
        var (parent, items, child, childItems) = Nested(stage, PopoverTriggerAction.Hover);
        stage.Move(Centre(items[2], stage.Window));
        stage.After(Entered);
        Assert.True(child.IsShown);

        // Into the child, then back onto its trigger: it stays.
        stage.Move(Centre(childItems[0], stage.Window));
        stage.Move(new Point(Box(parent, stage.Window).X + 5, Centre(items[2], stage.Window).Y));
        stage.After(Left);
        Assert.True(child.IsShown);

        // Into the child, then onto another item of the parent: it goes, alone.
        stage.Move(Centre(childItems[0], stage.Window));
        stage.Move(new Point(Box(parent, stage.Window).X + 5, Centre(items[0], stage.Window).Y));
        stage.After(Left);
        Assert.False(child.IsShown);
        Assert.True(parent.IsShown);
    }

    [AvaloniaFact]
    public void B12_ArrowRight_On_A_Trigger_Item_Opens_Its_Popover_And_Focuses_Inside_And_ArrowLeft_Closes_It()
    {
        using var stage = new Stage();
        var (parent, items, child, childItems) = Nested(stage);
        stage.Key(PhysicalKey.ArrowDown);
        stage.After(16);
        stage.Key(PhysicalKey.ArrowDown);
        stage.After(16);
        stage.Key(PhysicalKey.ArrowDown);
        stage.After(16);
        Assert.Same(items[2], stage.Focused);

        stage.Key(PhysicalKey.ArrowRight);
        stage.After(Entered);
        Assert.True(child.IsShown);
        Assert.Same(childItems[0], stage.Focused);

        // M6: ArrowLeft dismisses it alone, and focus goes back to its trigger.
        stage.Key(PhysicalKey.ArrowLeft);
        stage.After(Left);
        Assert.False(child.IsShown);
        Assert.True(parent.IsShown);
        Assert.Same(items[2], stage.Focused);

        // ArrowLeft in the parent does nothing.
        stage.Key(PhysicalKey.ArrowLeft);
        stage.After(Left);
        Assert.True(parent.IsShown);
    }

    // ── B14 to B17 ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_FocusTrap_False_Lets_Focus_Leave()
    {
        using var stage = new Stage();
        var outside = new Button { Content = "Outside" };
        stage.Page.Children.Add(outside);
        var (popover, _) = Menu();
        popover.Present(stage.Window);
        stage.After(Entered);
        outside.Focus();
        Assert.NotSame(outside, stage.Focused);

        popover.FocusTrap = false;
        outside.Focus();
        Assert.Same(outside, stage.Focused);
    }

    [AvaloniaFact]
    public void B15_KeepContentsMounted_Keeps_The_Content_In_The_Tree()
    {
        using var stage = new Stage();
        var content = new Border { Height = 50 };
        var popover = new AvonicPopover { Content = content };
        stage.Page.Children.Add(popover);
        Stage.Settle();
        Assert.False(popover.IsContentMounted);
        popover.Present();
        stage.After(Entered);
        Assert.True(content.IsAttachedToVisualTree());
        popover.Dismiss();
        stage.After(Left);
        Assert.False(content.IsAttachedToVisualTree());

        var keptContent = new Border();
        var kept = new AvonicPopover { Content = keptContent, KeepContentsMounted = true };
        stage.Page.Children.Add(kept);
        Stage.Settle();
        Assert.True(keptContent.IsAttachedToVisualTree());
        kept.Present();
        stage.After(Entered);
        kept.Dismiss();
        stage.After(Left);
        Assert.True(keptContent.IsAttachedToVisualTree());
    }

    [AvaloniaFact]
    public void B15_ContentTemplate_Builds_Content_That_Isnt_A_Control()
    {
        using var stage = new Stage();
        var popover = new AvonicPopover
        {
            Content = "data",
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => new TextBlock { Text = $"Built from {text}" }),
        };
        popover.Present(stage.Window);
        stage.After(Entered);
        Assert.Contains(popover.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Built from data");
    }

    [AvaloniaFact]
    public void B16_The_Direction_Goes_After_The_Dismiss()
    {
        using var stage = new Stage();
        stage.Page.FlowDirection = FlowDirection.RightToLeft;
        var popover = Popover();
        stage.Page.Children.Add(popover);
        popover.Present();
        stage.After(Entered);
        Assert.Equal(FlowDirection.RightToLeft, popover.FlowDirection);
        popover.Dismiss();
        stage.After(Left);
        Assert.False(popover.IsSet(Visual.FlowDirectionProperty));

        // One of its own is kept.
        popover.FlowDirection = FlowDirection.LeftToRight;
        popover.Present();
        stage.After(Entered);
        Assert.Equal(FlowDirection.LeftToRight, popover.FlowDirection);
    }

    [AvaloniaFact]
    public void B17_The_Argument_Wins_Over_The_Event_Property()
    {
        using var stage = new Stage();
        var a = stage.Trigger(10, 100, name: "A");
        var b = stage.Trigger(10, 500, name: "B");
        var popover = Popover();
        popover.Event = new PopoverEvent(a);
        popover.Present(new PopoverEvent(b));
        stage.After(Entered);
        Assert.Equal(540, Box(popover, stage.Window).Y, 0.5);
        popover.Dismiss();
        stage.After(Left);

        popover.Present(stage.Window);
        stage.After(Entered);
        Assert.Equal(140, Box(popover, stage.Window).Y, 0.5);
    }

    [AvaloniaFact]
    public void B17_From_Takes_A_Breadcrumbs_Indicator_And_A_Pointers_Point()
    {
        using var stage = new Stage();
        var indicator = stage.Trigger(10, 100, name: "Indicator");
        var args = new BreadcrumbCollapsedClickEventArgs(AvonicBreadcrumbs.IonCollapsedClickEvent, [], indicator);
        Assert.Same(indicator, PopoverEvent.From(args).Target);
    }

    // ── B18: the look ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_The_Box_Has_Mds_Radius_Shadow_And_Colours()
    {
        using var stage = new Stage();
        var popover = Popover();
        popover.Present(stage.Window);
        stage.After(Entered);
        var content = popover.ContentPart!;
        Assert.Equal(new CornerRadius(4), content.CornerRadius);
        Assert.Equal(3, content.BoxShadow.Count);
        Assert.Equal(Colors.White, ((ISolidColorBrush)content.Background!).Color);
        Assert.Equal(Colors.Black, ((ISolidColorBrush)popover.Foreground!).Color);
        Assert.Equal(20000 + popover.OverlayIndex, popover.ZIndex);
    }

    [AvaloniaFact]
    public void B18_Long_Content_Scrolls_In_The_Box_And_An_AvonicContent_Fits_It()
    {
        using var stage = new Stage();
        var popover = Popover(2000);
        popover.Present(stage.Window);
        stage.After(Entered);
        var scroller = popover.ContentPart!.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.Equal(759.6, popover.ContentPart!.Bounds.Height, 0.5);
        Assert.Equal(2000, scroller.Extent.Height, 0.5);
        popover.Dismiss();
        stage.After(Left);

        var content = new AvonicContent { Content = new Border { Height = 2000 } };
        popover.Content = content;
        popover.Present(stage.Window);
        stage.After(Entered);
        Assert.Equal(759.6, content.Bounds.Height, 0.5);
        Assert.Equal(scroller.Viewport.Height, scroller.Extent.Height, 0.5);
    }
}
