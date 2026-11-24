using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;
using Avonic.Themes;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-modal</c>, md, on the test clock: B1 to B17 in docs/components/ion-modal/PLAN.md, from <c>modal.js</c>,
/// <c>md.enter.js</c>, <c>md.leave.js</c>, <c>modal.md.css</c> and the measurements in <c>ref/measurements.txt</c>. Frames come
/// every 16 ms, so the enter animation, played at 0, shows local time t at 16 + t and finishes at the frame at 304; the leave,
/// 200 ms, at 224.
/// </summary>
public class AvonicModalTests
{
    private const double Entered = 304;
    private const double Left = 224;

    private static readonly CubicBezierEasing EnterEasing = new(0.36, 0.66, 0.04, 1);
    private static readonly CubicBezierEasing LeaveEasing = new(0.47, 0, 0.745, 0.715);

    /// <summary>A page in a window with <c>SimpleTheme</c>, whose template has the overlay layer, and <c>AvonicTheme</c>.</summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Avalonia.Themes.Simple.SimpleTheme _simple = new();
        private readonly AvonicTheme _theme = new();

        public Stage(double width = 390, double height = 844, params Control[] page)
        {
            Application.Current!.Styles.Insert(0, _simple);
            Application.Current.Styles.Add(_theme);
            Clock = new TestClock();
            Opener = new Button { Name = "Opener", Content = "Opener", Height = 40 };
            Page = new StackPanel { Children = { Opener } };
            Page.Children.AddRange(page);
            Scene = new Scene(Page, width, height);
        }

        public TestClock Clock { get; }

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public StackPanel Page { get; }

        public Button Opener { get; }

        public List<string> Log { get; } = [];

        public IInputElement? Focused => Window.FocusManager!.GetFocusedElement();

        public void At(double ms)
        {
            while (Clock.Now.TotalMilliseconds < ms)
            {
                Clock.At(Math.Min(Math.Floor(Clock.Now.TotalMilliseconds) + 1, ms));
                Settle();
            }

            Settle();
        }

        /// <summary>Runs on: the clock to <paramref name="ms"/> past now.</summary>
        public void After(double ms) => At(Clock.Now.TotalMilliseconds + ms);

        public static void Settle() => Dispatcher.UIThread.RunJobs();

        public void Render()
        {
            for (var i = 0; i < 2; i++)
            {
                Settle();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            Settle();
        }

        public AvonicModal Watch(AvonicModal modal, string name = "")
        {
            string Now() => $"@{Clock.Now.TotalMilliseconds}";
            modal.IonModalWillPresent += (_, _) => Log.Add($"{name}ionModalWillPresent{Now()}");
            modal.WillPresent += (_, _) => Log.Add($"{name}willPresent{Now()}");
            modal.IonModalDidPresent += (_, _) => Log.Add($"{name}ionModalDidPresent{Now()}");
            modal.DidPresent += (_, _) => Log.Add($"{name}didPresent{Now()}");
            modal.IonModalWillDismiss += (_, e) => Log.Add($"{name}ionModalWillDismiss({e.Data},{e.Role}){Now()}");
            modal.WillDismiss += (_, e) => Log.Add($"{name}willDismiss({e.Data},{e.Role}){Now()}");
            modal.IonModalDidDismiss += (_, e) => Log.Add($"{name}ionModalDidDismiss({e.Data},{e.Role}){Now()}");
            modal.DidDismiss += (_, e) => Log.Add($"{name}didDismiss({e.Data},{e.Role}){Now()}");
            return modal;
        }

        public void Key(PhysicalKey key)
        {
            Window.KeyPressQwerty(key, RawInputModifiers.None);
            Window.KeyReleaseQwerty(key, RawInputModifiers.None);
            Settle();
        }

        public void Click(Point point)
        {
            Render();
            Window.MouseDown(point, MouseButton.Left);
            Window.MouseUp(point, MouseButton.Left);
            Settle();
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
            Application.Current!.Styles.Remove(_theme);
            Application.Current.Styles.Remove(_simple);
        }
    }

    private static AvonicModal Modal(params Control[] content) => new()
    {
        Content = new StackPanel { Children = { new TextBlock { Text = "Modal" } } }.Also(p => p.Children.AddRange(content)),
    };

    private static double TranslateY(Visual element) => element.RenderTransform?.Value.M32 ?? 0;

    /// <summary>A finished task's result. Never <c>.Result</c> on one still running: it would block the UI thread it waits for.</summary>
    private static T Done<T>(Task<T> task)
    {
        Assert.True(task.IsCompleted, "The task hasn't finished.");
        return task.Result;
    }

    // ── B1, B2: md's animations ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Enter_Is_Mds_Over_280ms_With_Its_Curve()
    {
        using var stage = new Stage();
        var modal = Modal();
        modal.Present(stage.Window);
        Stage.Settle();
        var backdrop = modal.Backdrop!;
        var wrapper = modal.Wrapper!;

        Assert.Equal(0.01, backdrop.Opacity, 6);
        Assert.Equal(0.01, wrapper.Opacity, 6);
        Assert.Equal(40, TranslateY(wrapper), 3);
        Assert.False(backdrop.IsHitTestVisible);

        foreach (var local in new[] { 64.0, 128, 224 })
        {
            stage.At(16 + local);
            var e = EnterEasing.Ease(local / 280);
            Assert.Equal(0.01 + (0.32 - 0.01) * e, backdrop.Opacity, 4);
            Assert.Equal(0.01 + 0.99 * e, wrapper.Opacity, 4);
            Assert.Equal(40 * (1 - e), TranslateY(wrapper), 3);
            Assert.False(backdrop.IsHitTestVisible);
        }

        stage.At(Entered);
        Assert.Equal(0.32, backdrop.Opacity, 6);
        Assert.Equal(1, wrapper.Opacity, 6);
        Assert.Equal(0, TranslateY(wrapper), 3);
        Assert.True(backdrop.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void B1_Backdrop_Opacity_Follows_The_Modals_Key_Then_The_Global_One()
    {
        using var stage = new Stage();
        var modal = Modal();
        stage.Window.Resources["Avonic.BackdropOpacity"] = 0.5;
        modal.Present(stage.Window);
        stage.At(Entered);
        Assert.Equal(0.5, modal.Backdrop!.Opacity, 6);
        modal.Dismiss();
        stage.After(Left);

        modal.Resources["Avonic.Modal.BackdropOpacity"] = 0.2;
        modal.Present(stage.Window);
        stage.After(Entered);
        Assert.Equal(0.2, modal.Backdrop!.Opacity, 6);
    }

    [AvaloniaFact]
    public void B2_Leave_Is_Mds_Over_200ms_And_Rests_After()
    {
        using var stage = new Stage();
        var modal = Modal();
        modal.Present(stage.Window);
        stage.At(Entered);
        var backdrop = modal.Backdrop!;
        var wrapper = modal.Wrapper!;

        var start = stage.Clock.Now.TotalMilliseconds;
        var dismissed = modal.Dismiss("d", "ok");
        Stage.Settle();
        Assert.Equal(0.32, backdrop.Opacity, 6);
        Assert.Equal(0.99, wrapper.Opacity, 6);
        foreach (var local in new[] { 48.0, 112, 176 })
        {
            stage.At(start + 16 + local);
            var e = LeaveEasing.Ease(local / 200);
            Assert.Equal(0.32 * (1 - e), backdrop.Opacity, 4);
            Assert.Equal(0.99 * (1 - e), wrapper.Opacity, 4);
            Assert.Equal(40 * e, TranslateY(wrapper), 3);
        }

        stage.At(start + Left);
        Assert.True(dismissed.IsCompletedSuccessfully);
        Assert.True(dismissed.Result);
        Assert.Equal(0.01, wrapper.Opacity, 6);
        Assert.Equal(40, TranslateY(wrapper), 3);
        Assert.Null(modal.GetVisualParent());
    }

    // ── B3: events ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_The_Modals_Events_Come_Before_The_Generic_Ones()
    {
        using var stage = new Stage();
        var modal = stage.Watch(Modal());
        modal.Present(stage.Window);
        stage.At(Entered);
        modal.Dismiss(7, "ok");
        stage.After(Left);

        Assert.Equal(
        [
            "ionModalWillPresent@0", "willPresent@0", "ionModalDidPresent@304", "didPresent@304",
            "ionModalWillDismiss(7,ok)@304", "willDismiss(7,ok)@304", "ionModalDidDismiss(7,ok)@528", "didDismiss(7,ok)@528",
        ], stage.Log);
    }

    [AvaloniaFact]
    public void B3_The_Modals_Events_Bubble()
    {
        using var stage = new Stage();
        var modal = Modal();
        var heard = new List<string>();
        stage.Page.Children.Add(modal);
        stage.Page.AddHandler(AvonicModal.IonModalWillPresentEvent, (_, _) => heard.Add("ionModalWillPresent"));
        stage.Window.AddHandler(AvonicModal.IonModalDidDismissEvent, (_, _) => heard.Add("ionModalDidDismiss"));
        modal.Present();
        stage.At(Entered);
        modal.Dismiss();
        stage.After(Left);

        // Teleported to the overlay layer, it bubbles there, to the window; Ionic's bubbles to ion-app, where it's moved.
        Assert.Equal(["ionModalDidDismiss"], heard);
    }

    // ── B4: canDismiss ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_CanDismiss_False_Keeps_It_And_Dismiss_Returns_False()
    {
        using var stage = new Stage();
        var modal = stage.Watch(Modal());
        modal.CanDismiss = false;
        modal.Present(stage.Window);
        stage.At(Entered);
        stage.Log.Clear();

        var result = modal.Dismiss(1, "x");
        stage.After(Left);
        Assert.False(Done(result));
        Assert.Empty(stage.Log);
        Assert.NotNull(modal.GetVisualParent());

        // Escape and the backdrop go through it too.
        stage.Key(PhysicalKey.Escape);
        stage.After(Left);
        Assert.Empty(stage.Log);

        // The handler role dismisses without asking.
        var handler = modal.Dismiss(2, OverlayRoles.Handler);
        stage.After(Left);
        Assert.True(Done(handler));
    }

    [AvaloniaFact]
    public void B4_A_CanDismiss_Function_Gets_Data_And_Role_And_Decides()
    {
        using var stage = new Stage();
        var modal = Modal();
        var asked = new List<string>();
        modal.CanDismiss = new ModalCanDismiss((data, role) =>
        {
            asked.Add($"{data},{role}");
            return Task.FromResult(role == "ok");
        });
        modal.Present(stage.Window);
        stage.At(Entered);

        var no = modal.Dismiss(1, "nope");
        stage.After(16);
        Assert.False(Done(no));
        var yes = modal.Dismiss(2, "ok");
        stage.After(Left);
        Assert.True(Done(yes));
        Assert.Equal(["1,nope", "2,ok"], asked);
    }

    [AvaloniaFact]
    public void B4_CanDismiss_Reads_From_Xaml_Text()
    {
        var converter = new ModalCanDismissConverter();
        Assert.Equal(ModalCanDismiss.False, converter.ConvertFrom("False"));
        Assert.Equal(ModalCanDismiss.True, converter.ConvertFrom("true"));
        Assert.True(default(ModalCanDismiss).IsTrue);
    }

    // ── B5: isOpen ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_IsOpen_Presents_And_Dismisses_And_A_Dismiss_Leaves_It()
    {
        using var stage = new Stage();
        var modal = stage.Watch(Modal());
        stage.Page.Children.Add(modal);
        Stage.Settle();

        modal.IsOpen = true;
        stage.At(Entered);
        Assert.Equal(["ionModalWillPresent@0", "willPresent@0", "ionModalDidPresent@304", "didPresent@304"], stage.Log);
        stage.Log.Clear();

        modal.Dismiss();
        stage.After(Left);
        Assert.True(modal.IsOpen);
        Assert.Contains(modal, stage.Page.Children);

        modal.IsOpen = false;
        modal.IsOpen = true;
        stage.After(Entered);
        Assert.Contains("didPresent@832", stage.Log);
        stage.Log.Clear();
        modal.IsOpen = false;
        stage.After(Left);
        Assert.Equal("didDismiss(,)@1056", stage.Log[^1]);
    }

    [AvaloniaFact]
    public void B5_IsOpen_At_Load_Presents_At_The_Next_Frame()
    {
        var sink = new WarningSink();
        var previous = Avalonia.Logging.Logger.Sink;
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            using var stage = new Stage();
            var modal = stage.Watch(Modal());

            // Before it's loaded, as XAML sets it: Stencil calls no watcher yet, so nothing tries to present it with no top level.
            modal.IsOpen = true;
            Stage.Settle();
            Assert.Empty(stage.Log);
            stage.Page.Children.Add(modal);
            Stage.Settle();
            Assert.Empty(stage.Log);
            stage.At(16);
            Assert.Equal(["ionModalWillPresent@16", "willPresent@16"], stage.Log);
            Assert.DoesNotContain(sink.Messages, m => m.Contains("failed", StringComparison.Ordinal));
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    [AvaloniaFact]
    public void B5_IsOpen_On_One_Made_In_Code_After_Its_First_Present()
    {
        // Ionic's controller attaches it at create, so it's loaded: its isOpen watcher runs from then on. With nothing to place
        // it in, Present() looks to the app's lifetime, which a headless test has none of: the watcher's present fails, logged.
        var sink = new WarningSink(Avalonia.Logging.LogEventLevel.Error);
        var previous = Avalonia.Logging.Logger.Sink;
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            using var stage = new Stage();
            var modal = Modal();
            modal.Present(stage.Window);
            stage.At(Entered);
            modal.Dismiss();
            stage.After(Left);
            Assert.Empty(sink.Messages);

            modal.IsOpen = true;
            stage.After(16);

            // The fault is logged from the thread pool, as a dismiss's from input is.
            SpinWait.SpinUntil(() => sink.Messages.Count > 0, TimeSpan.FromSeconds(5));
            Assert.Contains(sink.Messages, m => m.Contains("failed", StringComparison.Ordinal));
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    // ── B6: trigger ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_A_Click_On_The_Trigger_Presents_It()
    {
        var avonicButton = new AvonicButton { Name = "Avonic", Content = "Avonic", Height = 40 };
        var border = new Border { Name = "Box", Height = 40, Background = Avalonia.Media.Brushes.Transparent };
        using var stage = new Stage(390, 844, avonicButton, border);
        NameScope.SetNameScope(stage.Page, NameScopeOf(stage));
        var modal = stage.Watch(Modal());
        modal.Trigger = "Opener";
        stage.Page.Children.Add(modal);
        Stage.Settle();

        stage.Opener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Stage.Settle();
        Assert.Equal("willPresent@0", stage.Log[^1]);
        stage.At(Entered);
        modal.Dismiss();
        stage.After(Left);
        stage.Log.Clear();

        // Another trigger: the old one no longer presents it.
        modal.Trigger = "Avonic";
        stage.Opener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Stage.Settle();
        Assert.Empty(stage.Log);
        avonicButton.RaiseEvent(new RoutedEventArgs(AvonicButton.ClickEvent));
        Stage.Settle();
        Assert.Equal("willPresent@528", stage.Log[^1]);
        stage.After(Entered);
        modal.Dismiss();
        stage.After(Left);
        stage.Log.Clear();

        // A control with no Click of its own: its tap.
        modal.Trigger = "Box";
        border.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, null!));
        Stage.Settle();
        Assert.Contains(stage.Log, l => l.StartsWith("willPresent", StringComparison.Ordinal));
    }

    private static NameScope NameScopeOf(Stage stage)
    {
        var scope = new NameScope();
        foreach (var control in stage.Page.Children.OfType<Control>().Where(c => c.Name is not null))
        {
            scope.Register(control.Name!, control);
        }

        return scope;
    }

    [AvaloniaFact]
    public void B6_A_Tap_On_A_Plain_Item_Trigger_Presents_It()
    {
        // An item's own Click comes only when it's a button; a DOM click comes from a tap on any item.
        var item = new AvonicItem { Name = "Item", Content = "Item" };
        using var stage = new Stage(390, 844, item);
        NameScope.SetNameScope(stage.Page, NameScopeOf(stage));
        var modal = stage.Watch(Modal());
        modal.Trigger = "Item";
        stage.Page.Children.Add(modal);
        Stage.Settle();

        stage.Click(item.TranslatePoint(new Point(20, 10), stage.Window)!.Value);
        stage.After(Entered);
        Assert.Contains(stage.Log, l => l.StartsWith("didPresent", StringComparison.Ordinal));
        Assert.Single(stage.Log, l => l.StartsWith("willPresent", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B6_A_Missing_Trigger_Is_Warned_About()
    {
        var sink = new WarningSink();
        var previous = Avalonia.Logging.Logger.Sink;
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            using var stage = new Stage();
            NameScope.SetNameScope(stage.Page, NameScopeOf(stage));

            // As XAML sets it: Trigger first, then the modal goes in the page. No warning for a trigger that's there.
            var found = Modal();
            found.Trigger = "Opener";
            stage.Page.Children.Add(found);
            Stage.Settle();
            Assert.DoesNotContain(sink.Messages, m => m.Contains("trigger element", StringComparison.Ordinal));

            var modal = Modal();
            modal.Trigger = "Nowhere";
            stage.Page.Children.Add(modal);
            Stage.Settle();
            Assert.Contains(sink.Messages, m => m.Contains("trigger element", StringComparison.Ordinal));
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    // ── B7, B8, B9: inline ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_Inline_It_Moves_To_The_Overlay_Layer_And_Back_Keeping_The_Pages_DataContext()
    {
        using var stage = new Stage();
        stage.Page.DataContext = "page";
        var label = new TextBlock();
        label.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("."));
        var modal = Modal(label);
        stage.Page.Children.Add(modal);
        Stage.Settle();
        var index = stage.Page.Children.IndexOf(modal);

        // A two-way binding in it would write back a null it saw on the way.
        var seen = new List<object?>();
        modal.PropertyChanged += (_, e) =>
        {
            if (e.Property == StyledElement.DataContextProperty)
            {
                seen.Add(e.NewValue);
            }
        };

        modal.Present();
        stage.At(Entered);
        Assert.NotSame(stage.Page, modal.Parent);
        Assert.IsType<OverlayHost>(modal.Parent);
        Assert.Same(stage.Window, TopLevel.GetTopLevel(modal));
        var placeholder = stage.Page.Children[index];
        Assert.False(placeholder.IsVisible);
        Assert.Equal("page", modal.DataContext);
        Assert.Equal("page", label.Text);

        modal.Dismiss();
        stage.After(Left);
        Assert.Same(modal, stage.Page.Children[index]);
        Assert.DoesNotContain(placeholder, stage.Page.Children);
        Assert.DoesNotContain(null, seen);
    }

    [AvaloniaFact]
    public void B7_Inline_In_A_Decorator_And_A_ContentControl()
    {
        var decorator = new Border();
        var contentControl = new ContentControl();
        using var stage = new Stage(390, 844, decorator, contentControl);
        var inDecorator = Modal();
        var inContent = Modal();
        decorator.Child = inDecorator;
        contentControl.Content = inContent;
        Stage.Settle();

        inDecorator.Present();
        inContent.Present();
        stage.At(Entered);
        Assert.NotSame(inDecorator, decorator.Child);
        Assert.NotSame(inContent, contentControl.Content);
        inDecorator.Dismiss();
        inContent.Dismiss();
        stage.After(Left);
        Assert.Same(inDecorator, decorator.Child);
        Assert.Same(inContent, contentControl.Content);
    }

    [AvaloniaFact]
    public void B7_Inline_As_Another_Modals_Own_Content()
    {
        using var stage = new Stage();
        var inner = stage.Watch(Modal(), "inner.");
        var outer = new AvonicModal { Content = inner };
        stage.Page.Children.Add(outer);
        Stage.Settle();
        outer.Present();
        stage.At(Entered);
        var presented = inner.Present();
        stage.After(Entered);
        Assert.True(presented.IsCompletedSuccessfully);
        Assert.NotSame(inner, outer.Content);
        inner.Dismiss();
        stage.After(Left);
        Assert.Same(inner, outer.Content);
    }

    [AvaloniaFact]
    public void B7_Presented_Again_During_Its_Leave_It_Stays_In_Its_Own_Window()
    {
        using var stage = new Stage();
        var modal = stage.Watch(Modal());
        stage.Page.Children.Add(modal);
        Stage.Settle();
        modal.Present();
        stage.At(Entered);
        modal.Dismiss();
        stage.After(100);

        // Still in the overlay host, leaving. Ionic's present waits for the dismiss, then presents it again.
        var again = modal.Present();
        stage.After(Left + Entered);
        Assert.True(again.IsCompletedSuccessfully);
        Assert.Same(stage.Window, TopLevel.GetTopLevel(modal));
        // The dismiss ends at 528 and the present starts then.
        Assert.Equal("didPresent@832", stage.Log[^1]);
    }

    [AvaloniaFact]
    public void B8_A_Nested_Modal_Is_Dismissed_With_Its_Parent()
    {
        using var stage = new Stage();
        var inner = stage.Watch(Modal(), "inner.");
        var outer = stage.Watch(Modal(inner), "outer.");
        stage.Page.Children.Add(outer);
        Stage.Settle();
        outer.Present();
        stage.At(Entered);
        inner.Present();
        stage.After(Entered);
        stage.Log.Clear();

        outer.Dismiss();
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("inner.didDismiss(,parent-dismissed)", StringComparison.Ordinal));
        Assert.Contains(stage.Log, l => l.StartsWith("outer.didDismiss(,)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B8_A_Nested_Modals_CanDismiss_Is_Asked_Once()
    {
        using var stage = new Stage();
        var asked = new List<string?>();
        var inner = stage.Watch(Modal(), "inner.");
        inner.CanDismiss = new ModalCanDismiss(async (_, role) =>
        {
            asked.Add(role);
            await Task.Yield();
            return true;
        });
        var outer = Modal(inner);
        stage.Page.Children.Add(outer);
        Stage.Settle();
        outer.Present();
        stage.At(Entered);
        inner.Present();
        stage.After(Entered);

        // The outer modal unmounts its content after it's dismissed, which takes the inner one's place out of the tree: no
        // second dismiss for that, with parent-removed.
        outer.Dismiss();
        stage.After(Left * 2);
        Assert.Equal([OverlayRoles.ParentDismissed], asked);
        Assert.DoesNotContain(stage.Log, l => l.Contains("parent-removed", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B9_Its_Place_Leaving_The_Tree_Dismisses_It_With_Parent_Removed()
    {
        var holder = new StackPanel();
        using var stage = new Stage(390, 844, holder);
        NameScope.SetNameScope(stage.Page, NameScopeOf(stage));
        var modal = stage.Watch(Modal());
        modal.Trigger = "Opener";
        holder.Children.Add(modal);
        Stage.Settle();
        modal.Present();
        stage.At(Entered);
        stage.Log.Clear();

        stage.Page.Children.Remove(holder);
        stage.After(Left);
        Assert.Contains("didDismiss(,parent-removed)@528", stage.Log);

        // Back in its page, which has gone: its trigger no longer tries to present it, which with no top level to present in
        // would fail, logged.
        stage.Log.Clear();
        var sink = new WarningSink(Avalonia.Logging.LogEventLevel.Error);
        var previous = Avalonia.Logging.Logger.Sink;
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            stage.Opener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            stage.After(Entered);
            SpinWait.SpinUntil(() => sink.Messages.Count > 0, TimeSpan.FromSeconds(2));
            Assert.Empty(stage.Log);
            Assert.Empty(sink.Messages);
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    // ── B10: the backdrop ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_A_Backdrop_Tap_Dismisses_With_Backdrop_Unless_BackdropDismiss_Is_False()
    {
        using var stage = new Stage(1024, 768);
        var modal = stage.Watch(Modal());
        modal.Present(stage.Window);
        stage.At(Entered);
        stage.Log.Clear();

        // The wrapper is over the backdrop: a click in it isn't a tap on the backdrop.
        var inside = new Button { Content = "Inside", Height = 40 };
        ((StackPanel)modal.Content!).Children.Add(inside);
        stage.Render();
        var centre = inside.TranslatePoint(new Point(10, 10), stage.Window)!.Value;
        stage.Click(centre);
        stage.After(Left);
        Assert.Empty(stage.Log);
        Assert.Same(inside, ((Visual?)stage.Window.InputHitTest(centre))?.FindAncestorOfType<Button>(includeSelf: true));

        stage.Click(new Point(50, 50));
        stage.After(Left);
        Assert.Contains("didDismiss(,backdrop)@752", stage.Log);

        modal.BackdropDismiss = false;
        modal.Present(stage.Window);
        stage.After(Entered);
        stage.Log.Clear();
        Assert.False(modal.Backdrop!.Tappable);
        stage.Click(new Point(50, 50));
        stage.Key(PhysicalKey.Escape);
        stage.After(Left);
        Assert.Empty(stage.Log);
    }

    [AvaloniaFact]
    public void B10_ShowBackdrop_False_Hides_It_And_It_Still_Takes_The_Tap()
    {
        using var stage = new Stage(1024, 768);
        var modal = stage.Watch(Modal());
        modal.ShowBackdrop = false;
        modal.Present(stage.Window);
        stage.At(Entered);
        Assert.False(modal.Backdrop!.Visible);
        Assert.Equal(Avalonia.Media.Colors.Transparent, ((Avalonia.Media.ISolidColorBrush)modal.Backdrop.Background!).Color);
        stage.Log.Clear();
        stage.Click(new Point(50, 50));
        stage.After(Left);
        Assert.Contains("didDismiss(,backdrop)@528", stage.Log);
    }

    // ── B11, B12: focus ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Present_Focuses_The_Wrapper_And_Dismiss_Gives_It_Back()
    {
        using var stage = new Stage();
        stage.Opener.Focus();
        var modal = Modal();
        modal.Present(stage.Window);
        stage.At(Entered);
        Assert.Same(modal.Wrapper, stage.Focused);

        modal.Dismiss();
        stage.After(Left);
        Assert.Same(stage.Opener, stage.Focused);
    }

    [AvaloniaFact]
    public void B12_FocusTrap_Keeps_Focus_Inside_Unless_It_Is_False()
    {
        using var stage = new Stage();
        var inside = new Button { Content = "Inside", Height = 40 };
        var modal = Modal(inside);
        modal.Present(stage.Window);
        stage.At(Entered);
        stage.Opener.Focus();
        Stage.Settle();
        Assert.Same(inside, stage.Focused);

        modal.FocusTrap = false;
        stage.Opener.Focus();
        Stage.Settle();
        Assert.Same(stage.Opener, stage.Focused);
    }

    // ── B13: keepContentsMounted ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_The_Content_Is_Mounted_Only_While_Presented()
    {
        using var stage = new Stage();
        var content = new TextBlock { Text = "Content" };
        var modal = new AvonicModal { Content = content };
        stage.Page.Children.Add(modal);
        Stage.Settle();
        Assert.Null(TopLevel.GetTopLevel(content));
        Assert.Same(modal, content.GetLogicalParent());

        modal.Present();
        Stage.Settle();
        Assert.Same(stage.Window, TopLevel.GetTopLevel(content));
        stage.At(Entered);
        modal.Dismiss();
        stage.After(Left);
        Assert.Null(TopLevel.GetTopLevel(content));
    }

    [AvaloniaFact]
    public void B13_KeepContentsMounted_Mounts_It_At_Attach_And_Keeps_It()
    {
        using var stage = new Stage();
        var content = new TextBlock { Text = "Content" };
        var modal = new AvonicModal { Content = content, KeepContentsMounted = true };
        stage.Page.Children.Add(modal);
        Stage.Settle();
        Assert.Same(stage.Window, TopLevel.GetTopLevel(content));

        modal.Present();
        stage.At(Entered);
        modal.Dismiss();
        stage.After(Left);
        Assert.Same(stage.Window, TopLevel.GetTopLevel(content));

        // Turned off while it's closed: unmounted.
        modal.KeepContentsMounted = false;
        Stage.Settle();
        Assert.Null(TopLevel.GetTopLevel(content));
    }

    [AvaloniaFact]
    public void B13_ContentTemplate_Builds_Content_That_Isnt_A_Control()
    {
        using var stage = new Stage();
        var modal = new AvonicModal
        {
            Content = "data",
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => new TextBlock { Name = "Built", Text = text + "!" }),
        };
        modal.Present(stage.Window);
        stage.At(Entered);
        stage.Render();
        var built = modal.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "Built");
        Assert.Equal("data!", built.Text);
    }

    // ── B14, B15: layout and safe area ──────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(390, 844, 0, 0, 390, 844, 0)]
    [InlineData(1024, 768, 212, 84, 600, 600, 2)]
    [InlineData(1024, 700, 212, 100, 600, 500, 2)]
    [InlineData(767, 1000, 0, 0, 767, 1000, 0)]
    [InlineData(800, 599, 0, 0, 800, 599, 0)]
    public void B14_Full_Screen_On_A_Phone_And_Centred_On_A_Tablet(double width, double height, double x, double y, double w, double h, double radius)
    {
        using var stage = new Stage(width, height);
        var modal = Modal();
        modal.Present(stage.Window);
        stage.At(Entered);
        stage.Render();
        var wrapper = modal.Wrapper!;
        var position = wrapper.TranslatePoint(default, stage.Window)!.Value;
        Assert.Equal(new Rect(x, y, w, h), new Rect(position, wrapper.Bounds.Size));
        Assert.Equal(new CornerRadius(radius), wrapper.CornerRadius);
        Assert.Equal(radius > 0, wrapper.BoxShadow.Count > 0);
    }

    [AvaloniaFact]
    public void B14_The_Apps_Sizes_Win_At_Every_Size_As_Percentages_Px_Or_Content()
    {
        using var stage = new Stage(1024, 768);
        var modal = new AvonicModal { Content = new Border { Width = 100, Height = 120 } };
        modal.Resources["Avonic.Modal.Width"] = "50%";
        modal.Resources["Avonic.Modal.Height"] = "auto";
        modal.Resources["Avonic.Modal.BorderRadius"] = 8d;
        modal.Present(stage.Window);
        stage.At(Entered);
        stage.Render();
        Assert.Equal(new Size(512, 120), modal.Wrapper!.Bounds.Size);
        Assert.Equal(new CornerRadius(8), modal.Wrapper.CornerRadius);

        modal.Resources["Avonic.Modal.Height"] = "300px";
        modal.Resources["Avonic.Modal.MaxWidth"] = 400d;
        stage.Render();
        Assert.Equal(new Size(400, 300), modal.Wrapper.Bounds.Size);

        // A content height under the minimum.
        modal.Resources["Avonic.Modal.Height"] = "auto";
        modal.Resources["Avonic.Modal.MinHeight"] = "200px";
        stage.Render();
        Assert.Equal(200, modal.Wrapper.Bounds.Height);
        modal.Resources["Avonic.Modal.Height"] = "300px";

        // The minimum wins over the maximum, as in CSS.
        modal.Resources["Avonic.Modal.MinWidth"] = "450px";
        stage.Render();
        Assert.Equal(new Size(450, 300), modal.Wrapper.Bounds.Size);
    }

    [AvaloniaFact]
    public void B14_Window_Units_Min_And_A_Calc_Of_Three_Read_As_Css()
    {
        // --width and --height are CSS lengths: vw and vh of the window, min(), and calc() of more than two terms.
        using var stage = new Stage(1024, 768);
        var modal = new AvonicModal { Content = new Border { Width = 100, Height = 120 } };
        modal.Resources["Avonic.Modal.Width"] = "50vw";
        modal.Resources["Avonic.Modal.Height"] = "min(50vh, 300px)";
        modal.Present(stage.Window);
        stage.At(Entered);
        stage.Render();
        Assert.Equal(new Size(512, 300), modal.Wrapper!.Bounds.Size);

        modal.Resources["Avonic.Modal.Width"] = "calc(100% - 10px + 4px)";
        modal.Resources["Avonic.Modal.Height"] = "25vh";
        stage.Render();
        Assert.Equal(new Size(1018, 192), modal.Wrapper.Bounds.Size);
    }

    [AvaloniaFact]
    public void B14_Vw_And_Vh_Are_The_Windows_In_A_Host_Smaller_Than_It()
    {
        // vw and vh are of the viewport, the window, not of the box the wrapper is in, as a percentage is.
        var wrapper = new Border();
        var panel = new ModalPanel { ContentWidth = "50vw", ContentHeight = "10vh", Children = { wrapper } };
        using var stage = new Stage(1000, 800, new Border { Width = 400, Height = 300, Child = panel });
        stage.Render();
        Assert.Equal(new Size(500, 80), wrapper.Bounds.Size);

        // The window grows and the host doesn't: the wrapper follows the window.
        stage.Window.Width = 1200;
        stage.Window.Height = 1000;
        stage.Render();
        Assert.Equal(new Size(600, 100), wrapper.Bounds.Size);
    }

    [AvaloniaTheory]
    [InlineData(390, 844, 24)]
    [InlineData(1024, 768, 0)]
    public void B15_The_Safe_Area_Is_0_Inside_A_Modal_That_Doesnt_Reach_The_Edge(double width, double height, double expected)
    {
        using var stage = new Stage(width, height);
        stage.Window.Resources["Avonic.SafeAreaTop"] = 24d;
        stage.Window.Resources["Avonic.SafeAreaBottom"] = 24d;
        var content = new TextBlock();
        var modal = new AvonicModal { Content = content };
        modal.Present(stage.Window);
        stage.At(Entered);
        stage.Render();
        Assert.Equal(expected, content.FindResource("Avonic.SafeAreaTop"));
        Assert.Equal(expected, content.FindResource("Avonic.SafeAreaBottom"));
    }

    [AvaloniaFact]
    public void B15_The_Safe_Area_Follows_A_Wrapper_That_Changes_Size()
    {
        using var stage = new Stage();
        stage.Window.Resources["Avonic.SafeAreaTop"] = 24d;
        var content = new TextBlock();
        var modal = new AvonicModal { Content = content };
        modal.Present(stage.Window);
        stage.At(Entered);
        stage.Render();
        Assert.Equal(24d, content.FindResource("Avonic.SafeAreaTop"));

        // Smaller, in the middle: it reaches no edge. The host's own size doesn't change, so only the wrapper's layout does.
        modal.Resources["Avonic.Modal.Height"] = "50%";
        stage.Render();
        Assert.Equal(0d, content.FindResource("Avonic.SafeAreaTop"));
        modal.Resources["Avonic.Modal.Height"] = "100%";
        stage.Render();
        Assert.Equal(24d, content.FindResource("Avonic.SafeAreaTop"));
    }

    // ── B16, B17: automation and look ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B16_The_Wrapper_Is_A_Dialog_Named_From_The_Host()
    {
        using var stage = new Stage();
        var modal = Modal();
        AutomationProperties.SetName(modal, "Settings");
        modal.Present(stage.Window);
        stage.At(Entered);
        var peer = ControlAutomationPeer.CreatePeerForElement(modal.Wrapper!);
        Assert.Equal(AutomationControlType.Window, peer.GetAutomationControlType());
        Assert.Equal("Settings", peer.GetName());
        Assert.True(modal.Wrapper!.Focusable);
        Assert.False(KeyboardNavigation.GetIsTabStop(modal.Wrapper));
    }

    [AvaloniaFact]
    public void B17_The_Look_Background_Text_Border()
    {
        using var stage = new Stage();
        var modal = Modal();
        modal.Resources["Avonic.Modal.BorderWidth"] = 3d;
        modal.Present(stage.Window);
        stage.At(Entered);
        var wrapper = modal.Wrapper!;
        Assert.Equal(Avalonia.Media.Colors.White, ((Avalonia.Media.ISolidColorBrush)wrapper.Background!).Color);
        Assert.Equal(Avalonia.Media.Colors.Black, ((Avalonia.Media.ISolidColorBrush)modal.Foreground!).Color);
        Assert.Equal(default, wrapper.BorderThickness);

        modal.Resources["Avonic.Modal.BorderStyle"] = "solid";
        Stage.Settle();
        Assert.Equal(new Thickness(3), wrapper.BorderThickness);
        Assert.Equal(Avalonia.Media.Colors.Transparent, ((Avalonia.Media.ISolidColorBrush)wrapper.BorderBrush!).Color);
        modal.Resources["Avonic.Modal.BorderColor"] = Avalonia.Media.Brushes.Blue;
        Stage.Settle();
        Assert.Equal(Avalonia.Media.Colors.Blue, ((Avalonia.Media.ISolidColorBrush)wrapper.BorderBrush!).Color);
        modal.Resources["Avonic.Modal.Background"] = Avalonia.Media.Brushes.Red;
        Stage.Settle();
        Assert.Equal(Avalonia.Media.Colors.Red, ((Avalonia.Media.ISolidColorBrush)wrapper.Background!).Color);
    }

    private sealed class WarningSink(Avalonia.Logging.LogEventLevel least = Avalonia.Logging.LogEventLevel.Warning) : Avalonia.Logging.ILogSink
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(Avalonia.Logging.LogEventLevel level, string area) => level >= least;

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate) => Messages.Add(messageTemplate);

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Messages.Add(messageTemplate);
    }
}

internal static class ModalTestExtensions
{
    public static T Also<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }

}
