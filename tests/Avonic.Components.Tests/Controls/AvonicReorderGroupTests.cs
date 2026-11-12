using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Gestures.GestureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ion-reorder-group and ion-reorder, as reorder-group.js and reorder.js do them: see docs/components/ion-reorder/PLAN.md, whose
/// numbers these follow. A finger on the headless window on the test clock, 16 ms between moves, as the gesture tests do. The
/// numbers are Chromium's, in docs/components/ion-reorder/ref/measurements.json.
/// </summary>
public class AvonicReorderGroupTests
{
    /// <summary>Five items 48 high, each with a handle in its end slot, at the top of the window or in a content.</summary>
    private sealed class Page : IDisposable
    {
        public Page(int count = 5, bool inContent = false, bool disabled = false, Action<AvonicReorderGroup>? setup = null)
        {
            Group = new AvonicReorderGroup { Disabled = disabled };
            setup?.Invoke(Group);
            if (Group.ItemsSource is null)
            {
                for (var i = 0; i < count; i++)
                {
                    Group.Items.Add(Item($"Item {i + 1}"));
                }
            }

            Control root = new StackPanel { Children = { Group } };
            if (inContent)
            {
                Content = new AvonicContent { Content = root };
                root = Content;
            }

            Scene = new Scene(root);
            if (Content is not null)
            {
                Content.GetScrollElement()!.IsScrollInertiaEnabled = false;
            }

            Render();

            Group.IonReorderStart += (_, _) => Log.Add($"start@{Clock.Now.TotalMilliseconds}");
            Group.IonReorderMove += (_, e) => Log.Add($"move {e.From}>{e.To}");
            Group.IonItemReorder += (_, e) => Log.Add($"item {e.From}>{e.To}");
            Group.IonReorderEnd += (_, e) =>
            {
                Log.Add($"end {e.From}>{e.To}");
                OnEnd?.Invoke(e);
            };
        }

        public TestClock Clock { get; } = new();

        public AvonicReorderGroup Group { get; }

        public AvonicContent? Content { get; }

        public Scene Scene { get; }

        public List<string> Log { get; } = [];

        public Action<ReorderEndEventArgs>? OnEnd { get; set; }

        public double At { get; set; } = 1000;

        public static AvonicItem Item(string text) => new()
        {
            Height = 48,
            Content = new TextBlock { Text = text },
            EndContent = new AvonicReorder(),
        };

        public Control Child(int index) => Group.ItemsPanelRoot!.Children[index];

        public AvonicReorder Handle(int index) => Child(index).GetVisualDescendants().OfType<AvonicReorder>().First();

        public Point Centre(Visual visual) => visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), Scene.Window)!.Value;

        public double Shift(int index) => (Child(index).RenderTransform as TranslateTransform)?.Y ?? 0;

        public IEnumerable<string> Order => Group.ItemsPanelRoot!.Children.Select(c => ((TextBlock)((AvonicItem)c).Content!).Text!);

        /// <summary>A finger down on the handle of the child at <paramref name="index"/>.</summary>
        public (Finger Finger, Point At) Press(int index) => Press(Centre(Handle(index)));

        public (Finger Finger, Point At) Press(Point point)
        {
            At += 1000;
            return (new Finger(Scene, Clock).Down(point.X, point.Y, At), point);
        }

        /// <summary>Moves by <paramref name="dy"/> in 8 px steps, 16 ms apart, and lets the last one's frame run.</summary>
        public Point Move((Finger Finger, Point At) press, double dy, double step = 8) => MoveFrom(press.Finger, press.At, dy, step);

        public Point MoveFrom(Finger finger, Point from, double dy, double step = 8)
        {
            var y = from.Y;
            for (var done = step; done <= Math.Abs(dy); done += step)
            {
                At += 16;
                y = from.Y + Math.Sign(dy) * done;
                finger.Move(from.X, y, At);
            }

            Tick();
            return new Point(from.X, y);
        }

        public void Up(Finger finger, Point at)
        {
            At += 16;
            finger.Up(at.X, at.Y, At);
        }

        /// <summary>The next frame.</summary>
        public void Tick(double ms = 16)
        {
            At += ms;
            Clock.At(At);
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    // ── 1: a handle in a slot shows only in an enabled group ────────────────────────────────

    [AvaloniaFact]
    public void B1_A_Slotted_Handle_Shows_Only_In_An_Enabled_Group()
    {
        using var page = new Page(count: 2, disabled: true);
        Assert.False(page.Handle(0).IsVisible);

        page.Group.Disabled = false;
        Assert.True(page.Handle(0).IsVisible);

        page.Group.Disabled = true;
        Assert.False(page.Handle(0).IsVisible);
    }

    [AvaloniaFact]
    public void B1_The_Group_Is_Disabled_By_Default_So_Its_Handles_Are_Hidden()
    {
        var group = new AvonicReorderGroup { Items = { Page.Item("A") } };
        using var scene = new Scene(group);
        Assert.True(group.Disabled);
        Assert.False(group.GetVisualDescendants().OfType<AvonicReorder>().Single().IsVisible);
    }

    [AvaloniaFact]
    public void B1_A_Handle_Not_In_A_Slot_Always_Shows_And_One_Anywhere_Inside_An_Enabled_Group_Shows()
    {
        var wrapping = new AvonicReorder { Content = new AvonicItem { Height = 48 } };
        var loose = new AvonicItem { Height = 48, EndContent = new AvonicReorder() };
        var inner = new AvonicItem { Height = 48, EndContent = new AvonicReorder() };
        var outer = new AvonicReorderGroup { Disabled = false, Items = { new Border { Child = new AvonicReorderGroup { Items = { inner } } } } };
        using var scene = new Scene(new StackPanel { Children = { new AvonicReorderGroup { Items = { wrapping } }, loose, outer } });

        Assert.True(wrapping.IsVisible);
        // Outside any group, a slotted handle is hidden
        Assert.False(((Control)loose.EndContent!).IsVisible);
        // A disabled group inside an enabled one: .reorder-enabled ion-reorder reaches it
        Assert.True(((Control)inner.EndContent!).IsVisible);
    }

    [AvaloniaFact]
    public void B1_An_Apps_Style_On_The_Handle_Beats_The_Hiding()
    {
        var app = new Style(x => x.OfType<AvonicReorder>()) { Setters = { new Setter(Visual.IsVisibleProperty, true) } };
        var group = new AvonicReorderGroup { Items = { Page.Item("A") } };
        var host = new StackPanel { Styles = { app }, Children = { group } };
        using var scene = new Scene(host);

        Assert.True(group.GetVisualDescendants().OfType<AvonicReorder>().Single().IsVisible);
    }

    // ── 2: the icon is the slot's fallback ───────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_The_Icon_Is_Reorder_Two_Sharp_And_Content_Replaces_It()
    {
        var empty = new AvonicReorder();
        var custom = new AvonicReorder { Content = new AvonicIcon { IconName = "pizza" } };
        using var scene = new Scene(new StackPanel { Children = { empty, custom } });

        var icon = empty.GetVisualDescendants().OfType<AvonicIcon>().Single(i => i.Name == "PART_Icon");
        Assert.Equal("reorder-two-sharp", icon.IconName);
        Assert.True(icon.IsVisible);
        Assert.False(custom.GetVisualDescendants().OfType<AvonicIcon>().Single(i => i.Name == "PART_Icon").IsVisible);

        custom.Content = null;
        Dispatcher.UIThread.RunJobs();
        Assert.True(custom.GetVisualDescendants().OfType<AvonicIcon>().Single(i => i.Name == "PART_Icon").IsVisible);
    }

    // ── 3: a click on a handle stops there, unless its group is disabled ────────────────────

    private static int Clicks(AvonicReorderGroup? group, bool disabled, bool stockButton = false)
    {
        var clicks = 0;
        Control inside;
        if (stockButton)
        {
            // No base theme here, so a template that hit-tests
            var button = new Button { Height = 48, Template = new FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.White }) };
            button.Click += (_, _) => clicks++;
            inside = button;
        }
        else
        {
            var item = new AvonicItem { Button = true, Height = 48 };
            item.Click += (_, _) => clicks++;
            inside = item;
        }

        var handle = new AvonicReorder { Content = inside };
        Control root = handle;
        if (group is not null)
        {
            group.Disabled = disabled;
            group.Items.Add(handle);
            root = group;
        }

        using var clock = new TestClock();
        using var scene = new Scene(new StackPanel { Children = { root } });
        Render();
        var at = inside.TranslatePoint(new Point(100, 24), scene.Window)!.Value;
        new Finger(scene, clock).Down(at.X, at.Y, 1000).Up(at.X, at.Y, 1050);
        if (group is not null && !disabled)
        {
            // The drag that the press started ends with from == to, and completes itself
            clock.At(1100);
        }

        return clicks;
    }

    [AvaloniaFact]
    public void B3_A_Disabled_Group_Lets_A_Click_Through_The_Handle() => Assert.Equal(1, Clicks(new AvonicReorderGroup(), disabled: true));

    [AvaloniaFact]
    public void B3_With_No_Group_The_Handle_Stops_The_Click() => Assert.Equal(0, Clicks(null, disabled: false));

    [AvaloniaFact]
    public void B3_The_Handle_Stops_A_Stock_Buttons_Click_Too()
    {
        Assert.Equal(0, Clicks(null, disabled: false, stockButton: true));
        Assert.Equal(1, Clicks(new AvonicReorderGroup(), disabled: true, stockButton: true));
    }

    [AvaloniaFact]
    public void B3_An_Enabled_Group_Waiting_For_Complete_Still_Stops_The_Click()
    {
        // The group won't start a drag until the last is completed, so only the handle stops the click
        using var page = new Page(count: 2);
        var clicks = 0;
        var item = (AvonicItem)page.Child(0);
        item.Button = true;
        item.Click += (_, _) => clicks++;
        Render();
        var press = page.Press(1);
        page.Up(press.Finger, page.Move(press, -48));
        Assert.Equal(["end 1>0"], page.Log.Where(l => l.StartsWith("end")));

        var at = page.Centre(page.Handle(0));
        new Finger(page.Scene, page.Clock).Down(at.X, at.Y, page.At + 100).Up(at.X, at.Y, page.At + 150);
        Assert.Equal(0, clicks);
        Assert.Single(page.Log, l => l.StartsWith("start"));
    }

    // ── 4: cursor ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_A_Handle_In_An_Enabled_Group_Has_A_Hand_Cursor()
    {
        using var page = new Page(count: 1, disabled: true);
        var handle = page.Handle(0);
        Assert.Null(handle.Cursor);
        page.Group.Disabled = false;
        Assert.NotNull(handle.Cursor);
        page.Group.Disabled = true;
        Assert.Null(handle.Cursor);
    }

    // ── 5, 6: the gesture ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Disabled_Means_No_Drag_And_Enabling_Turns_It_On()
    {
        // A handle that isn't slotted shows when disabled, so it can be pressed
        var handle = new AvonicReorder { Content = new Border { Height = 48, Background = Brushes.White } };
        using var page = new Page(count: 0, disabled: true, setup: g => g.Items.Add(handle));
        page.Move(page.Press(page.Centre(handle)), 40);
        Assert.Empty(page.Log);

        page.Group.Disabled = false;
        page.Move(page.Press(page.Centre(handle)), 40);
        Assert.StartsWith("start", page.Log[0]);
    }

    [AvaloniaFact]
    public void B6_A_Press_On_The_Handle_Starts_At_Once_And_A_Press_Elsewhere_Does_Not()
    {
        using var page = new Page();
        page.Move(page.Press(page.Centre(page.Child(1)) - new Point(120, 0)), 40);
        Assert.Empty(page.Log);
        Assert.Equal(0, page.Shift(1));

        // Threshold 0: the press captures and starts
        page.Press(1);
        Assert.Equal([$"start@{page.At}"], page.Log);
        Assert.True(((IPseudoClasses)page.Child(1).Classes).Contains(AvonicReorderGroup.SelectedPseudoClass));
    }

    // ── 7, 9: moving ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_The_Item_Follows_The_Finger_And_The_Siblings_Make_Room()
    {
        using var page = new Page();
        var press = page.Press(1);
        page.Move(press, 80);
        page.Tick(400);

        // Measured: the dragged item at +80; the two it passed at -48, the rest at 0
        Assert.Equal(80, page.Shift(1));
        Assert.Equal(0, page.Shift(0));
        Assert.Equal(-48, page.Shift(2));
        Assert.Equal(-48, page.Shift(3));
        Assert.Equal(0, page.Shift(4));

        // Back above where it began: the one above moves down, the others back
        page.MoveFrom(press.Finger, press.At + new Point(0, 80), -112);
        page.Tick(400);
        Assert.Equal(48, page.Shift(0));
        Assert.Equal(0, page.Shift(2));
        Assert.Equal(0, page.Shift(3));
        Assert.Equal(-32, page.Shift(1));
    }

    [AvaloniaFact]
    public void B7_A_Move_Says_The_Last_Index_And_The_New_One()
    {
        using var page = new Page();
        page.Move(page.Press(1), 80);

        // Measured: 1>1 until the middle of the next item is passed, then 1>2, 2>2, … 2>3
        var moves = page.Log.Where(l => l.StartsWith("move")).Distinct().ToList();
        Assert.Equal(["move 1>1", "move 1>2", "move 2>2", "move 2>3", "move 3>3"], moves);
    }

    [AvaloniaFact]
    public void B7_The_Item_Is_Held_Inside_The_Group()
    {
        using var page = new Page();
        page.Move(page.Press(1), 600);
        page.Tick(400);

        // Five items of 48: the finger is held at the group's bottom, 240, from the press at 72; the item is over the last
        Assert.Equal(240 - 72, page.Shift(1));
        Assert.Equal("move 4>4", page.Log[^1]);
    }

    [AvaloniaFact]
    public void B9_A_Sibling_Eases_Over_300ms_And_The_Dragged_Item_Does_Not()
    {
        using var page = new Page();
        var press = page.Press(1);
        page.MoveFrom(press.Finger, press.At, 16);
        Assert.Equal(0, page.Shift(2));

        // At 24 px the finger is over the next item: its frame starts the sibling's transition, and the item is where the finger is
        var at = page.MoveFrom(press.Finger, press.At + new Point(0, 16), 8);
        var started = page.At;
        Assert.Equal(24, at.Y - press.At.Y);
        Assert.Equal(24, page.Shift(1));

        // ease, measured: -1.8 a frame in, and -48 at 300 ms
        var ease = new CubicBezierEasing(0.25, 0.1, 0.25, 1);
        page.Clock.At(started + 16);
        Assert.Equal(-48 * ease.Ease(16 / 300.0), page.Shift(2), 6);
        Assert.Equal(-1.8, page.Shift(2), 0);
        page.Clock.At(started + 160);
        Assert.Equal(-48 * ease.Ease(160 / 300.0), page.Shift(2), 6);
        page.Clock.At(started + 304);
        Assert.Equal(-48, page.Shift(2));
    }

    // ── 10: auto-scroll ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_Near_The_Bottom_Of_The_Content_Each_Move_Scrolls_10()
    {
        using var page = new Page(count: 30, inContent: true);
        var scroll = page.Content!.GetScrollElement()!;
        var press = page.Press(1);
        // To 30 px from the bottom of the 844 window
        var at = page.Move(press, 844 - 30 - press.At.Y);
        var before = scroll.Offset.Y;
        Assert.True(before > 0, "it scrolled on the way");

        // Each move scrolls 10, and a finger held still doesn't
        page.Tick(200);
        Assert.Equal(before, scroll.Offset.Y);
        page.MoveFrom(press.Finger, at, 2, step: 1);
        Assert.Equal(before + 20, scroll.Offset.Y);

        // The dragged item moves with the scroll: it stays under the finger
        Assert.Equal(at.Y + 2 - press.At.Y + scroll.Offset.Y, page.Shift(1), 0);
    }

    [AvaloniaFact]
    public void B10_Moves_Away_From_The_Edges_Do_Not_Scroll()
    {
        using var page = new Page(count: 30, inContent: true);
        var scroll = page.Content!.GetScrollElement()!;
        page.Move(page.Press(3), 200);
        Assert.Equal(0, scroll.Offset.Y);
    }

    // ── 11 - 13: the end, and complete ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_A_Drop_Elsewhere_Raises_ItemReorder_Then_ReorderEnd()
    {
        using var page = new Page();
        var press = page.Press(1);
        page.Up(press.Finger, page.Move(press, 80));
        Assert.Equal(["item 1>3", "end 1>3"], page.Log.Where(l => l.StartsWith("item") || l.StartsWith("end")));

        // Nothing moves until complete
        page.Tick(400);
        Assert.Equal(["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"], page.Order);
        Assert.Equal(80, page.Shift(1));
    }

    [AvaloniaFact]
    public void B11_A_Drop_In_Place_Raises_Only_ReorderEnd_And_Completes_Itself()
    {
        using var page = new Page();
        var press = page.Press(1);
        page.Up(press.Finger, page.Move(press, 16));
        Assert.Equal(["end 1>1"], page.Log.Where(l => l.StartsWith("item") || l.StartsWith("end")));

        page.Tick();
        Assert.Equal(0, page.Shift(1));
        // Not waiting for complete: a new drag starts
        page.Press(2);
        Assert.Equal(2, page.Log.Count(l => l.StartsWith("start")));
    }

    [AvaloniaFact]
    public void B12_Complete_Moves_The_Item_And_Clears_Every_Transform_In_The_Next_Frame()
    {
        using var page = new Page();
        page.OnEnd = e => e.Complete();
        var press = page.Press(1);
        var at = page.Move(press, 80);
        page.Tick(200);
        page.Up(press.Finger, at);

        // Measured: the class goes at once, and the transforms stay for a frame
        Assert.False(((IPseudoClasses)page.Child(1).Classes).Contains(AvonicReorderGroup.SelectedPseudoClass));
        Assert.Equal(80, page.Shift(1));
        Assert.Equal(["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"], page.Order);

        page.Tick();
        Assert.Equal(["Item 1", "Item 3", "Item 4", "Item 2", "Item 5"], page.Order);
        // Nothing eases back: every transform is gone in that frame
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(0, page.Shift(i));
        }
    }

    [AvaloniaFact]
    public void B12_Complete_False_Puts_It_Back()
    {
        using var page = new Page();
        page.OnEnd = e => e.Complete(false);
        var press = page.Press(1);
        page.Up(press.Finger, page.Move(press, 80));
        page.Tick();

        Assert.Equal(["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"], page.Order);
        Assert.Equal(0, page.Shift(1));
        Assert.Equal(0, page.Shift(2));
    }

    [AvaloniaFact]
    public void B12_Complete_With_A_List_Reorders_The_List_And_Not_The_Group()
    {
        using var page = new Page();
        var data = new List<int> { 1, 2, 3, 4, 5 };
        IList<int>? returned = null;
        page.OnEnd = e => returned = e.Complete(data);
        var press = page.Press(1);
        page.Up(press.Finger, page.Move(press, 80));
        page.Tick();

        Assert.Equal([1, 3, 4, 2, 5], data);
        Assert.Equal([1, 3, 4, 2, 5], returned);
        Assert.NotSame(data, returned);
        Assert.Equal(["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"], page.Order);
        Assert.Equal(0, page.Shift(1));
    }

    [AvaloniaFact]
    public void B12_Complete_Before_The_End_Or_Twice_Does_Nothing()
    {
        using var page = new Page();
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var press = page.Press(1);
        var at = page.Move(press, 80);

        // Mid-drag: the list comes back as it was, and the drag goes on
        Assert.Same(data, page.Group.Complete(data));
        page.Group.Complete();
        page.Tick();
        Assert.Equal(80, page.Shift(1));

        page.Up(press.Finger, at);
        page.Group.Complete();
        page.Tick();
        Assert.Equal(["Item 1", "Item 3", "Item 4", "Item 2", "Item 5"], page.Order);
        Assert.Same(data, page.Group.Complete(data));
        Assert.Equal([1, 2, 3, 4, 5], data);
    }

    [AvaloniaFact]
    public void B13_Until_Complete_No_New_Drag_Starts_And_A_Late_Complete_Still_Moves()
    {
        using var page = new Page();
        var press = page.Press(1);
        page.Up(press.Finger, page.Move(press, 80));

        page.Tick(500);
        page.Move(page.Press(0), 60);
        Assert.Single(page.Log, l => l.StartsWith("start"));
        Assert.True(((IPseudoClasses)page.Child(1).Classes).Contains(AvonicReorderGroup.SelectedPseudoClass));

        page.Group.Complete();
        page.Tick();
        Assert.Equal(["Item 1", "Item 3", "Item 4", "Item 2", "Item 5"], page.Order);
    }

    // ── 14: the lift ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_The_Dragged_Child_Is_Over_Its_Siblings_Whatever_Its_Type()
    {
        var plain = new Border { Height = 48, Background = Brushes.White, Child = new AvonicReorder() };
        using var page = new Page(count: 2, setup: g => g.Items.Insert(0, plain));
        Render();
        var press = page.Press(page.Centre(plain));
        Assert.Equal(AvonicReorderGroup.SelectedZIndex, plain.ZIndex);
        Assert.Equal(0, page.Child(1).ZIndex);

        page.Up(press.Finger, page.Move(press, 60));
        page.Group.Complete();
        Assert.Equal(0, plain.ZIndex);
    }

    [AvaloniaFact]
    public void B14_A_Templated_Items_Element_Is_Marked_Not_Its_Presenter()
    {
        var items = new ObservableCollection<string> { "A", "B", "C" };
        using var page = new Page(setup: g =>
        {
            g.ItemsSource = items;
            g.ItemTemplate = new FuncDataTemplate<string>((text, _) => Page.Item(text));
        });
        Render();
        page.Press(1);

        var presenter = page.Child(1);
        Assert.IsAssignableFrom<Avalonia.Controls.Presenters.ContentPresenter>(presenter);
        var item = presenter.GetVisualChildren().OfType<AvonicItem>().Single();
        Assert.True(((IPseudoClasses)item.Classes).Contains(AvonicReorderGroup.SelectedPseudoClass));
        Assert.Equal(AvonicReorderGroup.SelectedZIndex, presenter.ZIndex);
    }

    [AvaloniaFact]
    public void B14_ItemsSource_Is_The_Apps_So_Complete_Leaves_It_And_Complete_With_It_Moves_It()
    {
        var items = new ObservableCollection<string> { "A", "B", "C", "D", "E" };
        var moves = 0;
        items.CollectionChanged += (_, e) => moves += e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move ? 1 : 100;
        using var page = new Page(setup: g =>
        {
            g.ItemsSource = items;
            g.ItemTemplate = new FuncDataTemplate<string>((text, _) => Page.Item(text));
        });
        Render();

        page.OnEnd = e => e.Complete();
        var press = page.Press(1);
        page.Up(press.Finger, page.Move(press, 80));
        page.Tick();
        Assert.Equal(["A", "B", "C", "D", "E"], items);

        page.OnEnd = e => e.Complete(items);
        press = page.Press(1);
        page.Up(press.Finger, page.Move(press, 80));
        page.Tick();
        Assert.Equal(["A", "C", "D", "B", "E"], items);
        Assert.Equal(1, moves);
    }

    [AvaloniaFact]
    public void B14_An_Apps_Own_Transform_Comes_Back_After_The_Drag()
    {
        using var page = new Page(count: 3);
        var own = new ScaleTransform(1, 1);
        page.Child(2).RenderTransform = own;
        page.OnEnd = e => e.Complete(false);
        var press = page.Press(1);
        page.Up(press.Finger, page.Move(press, 48));
        page.Tick();

        Assert.Same(own, page.Child(2).RenderTransform);
    }

    // ── 15: leaving, and disabling, mid-drag ────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_Leaving_The_Tree_Mid_Drag_Ends_It()
    {
        using var page = new Page();
        page.Move(page.Press(1), 80);
        ((StackPanel)page.Group.Parent!).Children.Clear();

        Assert.Contains("end 1>3", page.Log);
    }

    [AvaloniaFact]
    public void B15_Disabling_Mid_Drag_Ends_It()
    {
        using var page = new Page();
        page.Move(page.Press(1), 80);
        page.Group.Disabled = true;

        Assert.Equal(["item 1>3", "end 1>3"], page.Log.Where(l => l.StartsWith("item") || l.StartsWith("end")));
    }

    [AvaloniaFact]
    public void Routed_Events_Bubble()
    {
        using var page = new Page();
        var seen = new List<string>();
        page.Scene.Window.AddHandler(AvonicReorderGroup.IonReorderStartEvent, (_, _) => seen.Add("start"));
        page.Scene.Window.AddHandler(AvonicReorderGroup.IonReorderMoveEvent, (_, _) => seen.Add("move"));
        page.Scene.Window.AddHandler(AvonicReorderGroup.IonItemReorderEvent, (_, _) => seen.Add("item"));
        page.Scene.Window.AddHandler(AvonicReorderGroup.IonReorderEndEvent, (_, _) => seen.Add("end"));
        var press = page.Press(1);
        page.Up(press.Finger, page.Move(press, 80));

        Assert.Equal(["start", "move", "item", "end"], seen.Distinct());
    }
}
