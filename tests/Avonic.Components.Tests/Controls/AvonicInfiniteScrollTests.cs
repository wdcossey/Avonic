using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-infinite-scroll in Chromium: 30 rows of 50 px in ion-content, 390 x 844, the scroll 84 high.
/// See the Measured table in docs/components/ion-infinite-scroll/PLAN.md. These run on the functional stub, with a plain
/// child of 84 px standing in for md's minimum height.
/// </summary>
public class AvonicInfiniteScrollTests
{
    /// <summary>A page of rows and an infinite scroll, on the test clock, counting <c>IonInfinite</c>.</summary>
    private sealed class Stage : IDisposable
    {
        public TestClock Clock { get; } = new();
        public StackPanel Rows { get; } = new();
        public AvonicInfiniteScroll Infinite { get; }
        public AvonicContent Content { get; }
        public Scene Scene { get; }
        public int Fired { get; private set; }

        public Stage(InfiniteScrollPosition position = InfiniteScrollPosition.Bottom, string? threshold = null, int rows = 30,
            Control? child = null, Func<Control, Control>? wrap = null)
        {
            Infinite = new AvonicInfiniteScroll { Position = position, Content = child ?? new Border { Height = 84 } };
            if (threshold is not null)
            {
                Infinite.Threshold = threshold;
            }

            Infinite.IonInfinite += (_, _) => Fired++;
            for (var i = 0; i < rows; i++)
            {
                Rows.Children.Add(Row());
            }

            Rows.Children.Insert(position == InfiniteScrollPosition.Top ? 0 : rows, wrap?.Invoke(Infinite) ?? Infinite);
            Content = new AvonicContent { Content = Rows };
            Scene = new Scene(Content);
        }

        public ScrollViewer Scroll => Content.GetScrollElement()!;

        public static Border Row() => new() { Height = 50 };

        /// <summary>Scrolls, and lets the frame come in which a browser dispatches <c>scroll</c>.</summary>
        public void ScrollTo(double y)
        {
            Scroll.Offset = new Vector(0, y);
            Frame();
        }

        public void Frame(double milliseconds = 16) => Clock.At(Clock.Now.TotalMilliseconds + milliseconds);

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    private sealed class ErrorSink : ILogSink
    {
        public List<(object? Source, string Message)> Errors { get; } = [];

        public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Error;

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate) => Errors.Add((source, messageTemplate));

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Errors.Add((source, messageTemplate));
    }

    // ── B1: the bottom, 15% ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_At_The_Bottom_It_Fires_Within_15_Percent_Of_The_Height_Strictly_And_Once()
    {
        using var stage = new Stage();
        Assert.Equal(1584, stage.Scroll.Extent.Height);
        Assert.Equal(844, stage.Scroll.Bounds.Height);

        // 1584 - 84 - scrollTop - 126.6 - 844 < 0 from 529.4
        stage.ScrollTo(529);
        Assert.Equal(0, stage.Fired);
        stage.ScrollTo(530);
        Assert.Equal(1, stage.Fired);

        // Scrolling on while it loads doesn't fire again
        stage.ScrollTo(600);
        stage.ScrollTo(740);
        stage.ScrollTo(530);
        Assert.Equal(1, stage.Fired);
    }

    // ── B2: the threshold ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_A_Threshold_In_Pixels_Fires_Strictly_Inside_It()
    {
        using var stage = new Stage(threshold: "100px");

        // 1584 - 84 - scrollTop - 100 - 844: exactly 0 at 556
        stage.ScrollTo(556);
        Assert.Equal(0, stage.Fired);
        stage.ScrollTo(557);
        Assert.Equal(1, stage.Fired);
    }

    [AvaloniaFact]
    public void B2_A_Change_Of_Threshold_Takes_Effect_And_Is_Read_As_ParseFloat_Reads_It()
    {
        using var stage = new Stage();

        // 50% of 844 is 422: 1584 - 84 - scrollTop - 422 - 844 < 0 from 234
        stage.Infinite.Threshold = " 50% ";
        stage.ScrollTo(234);
        Assert.Equal(0, stage.Fired);
        stage.ScrollTo(235);
        Assert.Equal(1, stage.Fired);
        stage.Infinite.Complete();

        // A bare number is pixels; a unit after it is ignored
        stage.Infinite.Threshold = "  300 whatever";
        stage.ScrollTo(0);
        stage.ScrollTo(356);
        Assert.Equal(1, stage.Fired);
        stage.ScrollTo(357);
        Assert.Equal(2, stage.Fired);
        stage.Infinite.Complete();

        // Not a number: NaN, which is never less than 0
        stage.Infinite.Threshold = "auto";
        stage.ScrollTo(0);
        stage.ScrollTo(740);
        Assert.Equal(2, stage.Fired);
    }

    [AvaloniaFact]
    public void B2_ParseFloat_Reads_The_Longest_Number_At_The_Start()
    {
        Assert.Equal(15, AvonicInfiniteScroll.ParseFloat("15%"));
        Assert.Equal(-2.5, AvonicInfiniteScroll.ParseFloat(" -2.5px"));
        Assert.Equal(0.5, AvonicInfiniteScroll.ParseFloat(".5"));
        Assert.Equal(1e3, AvonicInfiniteScroll.ParseFloat("1e3x"));
        Assert.Equal(double.PositiveInfinity, AvonicInfiniteScroll.ParseFloat("Infinity"));
        Assert.True(double.IsNaN(AvonicInfiniteScroll.ParseFloat("px15")));
        Assert.True(double.IsNaN(AvonicInfiniteScroll.ParseFloat(null)));
        // JavaScript's white space, which isn't .NET's
        Assert.Equal(50, AvonicInfiniteScroll.ParseFloat("\uFEFF\u00A050px"));
        Assert.True(double.IsNaN(AvonicInfiniteScroll.ParseFloat("\u008550px")));
    }

    // ── B3: the scroll event's frame ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_The_Check_Runs_At_The_Next_Frame_Once_For_The_Position_Then()
    {
        using var stage = new Stage();

        stage.Scroll.Offset = new Vector(0, 600);
        Assert.Equal(0, stage.Fired);
        stage.Frame(15);
        Assert.Equal(0, stage.Fired);
        stage.Frame(1);
        Assert.Equal(1, stage.Fired);
        stage.Infinite.Complete();

        // Into the threshold and out again within a frame: one check, of where it is
        stage.ScrollTo(0);
        stage.Scroll.Offset = new Vector(0, 600);
        stage.Scroll.Offset = new Vector(0, 100);
        stage.Frame();
        Assert.Equal(1, stage.Fired);
    }

    [AvaloniaFact]
    public void B3_One_Scroll_Event_A_Frame_So_A_Handler_That_Completes_At_Once_Fires_Once()
    {
        using var stage = new Stage();
        stage.Infinite.IonInfinite += (_, _) => stage.Infinite.Complete();

        stage.Scroll.Offset = new Vector(0, 600);
        stage.Scroll.Offset = new Vector(0, 610);
        stage.Frame();
        Assert.Equal(1, stage.Fired);
        stage.ScrollTo(620);
        Assert.Equal(2, stage.Fired);
    }

    // ── B4: only a scroll checks ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Nothing_Fires_Without_A_Scroll()
    {
        // Everything fits: there's never a scroll
        using (var shortStage = new Stage(rows: 2))
        {
            shortStage.Frame(500);
            Assert.Equal(0, shortStage.Fired);
        }

        // The end comes near by a change of layout, not of position: at 0, which a shorter extent doesn't clamp
        using var stage = new Stage();
        stage.Rows.Children.RemoveRange(0, 20);
        Dispatcher.UIThread.RunJobs();
        stage.Frame(500);
        Assert.Equal(0, stage.Fired);
    }

    // ── B5: complete() ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Complete_Lets_The_Next_Scroll_Fire_Again()
    {
        using var stage = new Stage();
        stage.ScrollTo(530);
        stage.ScrollTo(531);
        Assert.Equal(1, stage.Fired);

        stage.Infinite.Complete();
        Assert.Equal(1, stage.Fired);
        stage.ScrollTo(532);
        Assert.Equal(2, stage.Fired);
    }

    // ── B6: disabled ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Disabled_Hides_It_And_Stops_It_Listening_Until_Enabled()
    {
        using var stage = new Stage();
        stage.Infinite.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();

        // display: none takes its height away
        Assert.False(stage.Infinite.IsVisible);
        Assert.DoesNotContain(":infinite-scroll-enabled", stage.Infinite.Classes);
        Assert.Equal(1500, stage.Scroll.Extent.Height);
        stage.ScrollTo(656);
        Assert.Equal(0, stage.Fired);

        stage.Infinite.IsEnabled = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Infinite.IsVisible);
        Assert.Contains(":infinite-scroll-enabled", stage.Infinite.Classes);
        stage.ScrollTo(530);
        Assert.Equal(1, stage.Fired);
    }

    [AvaloniaFact]
    public void B6_An_Apps_Own_IsVisible_Wins_Over_Disabled_And_It_Still_Does_Not_Listen()
    {
        using var stage = new Stage();
        stage.Infinite.IsEnabled = false;
        stage.Infinite.IsVisible = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Infinite.IsVisible);
        stage.ScrollTo(530);
        stage.ScrollTo(740);
        Assert.Equal(0, stage.Fired);
    }

    [AvaloniaFact]
    public void B6_A_Scroll_While_Disabled_Is_Heard_If_It_Is_Enabled_By_The_Frame()
    {
        // As the listener is added back before the browser dispatches the scroll
        using var stage = new Stage();
        stage.Infinite.IsEnabled = false;
        stage.Scroll.Offset = new Vector(0, 600);
        stage.Infinite.IsEnabled = true;
        Dispatcher.UIThread.RunJobs();
        stage.Frame();
        Assert.Equal(1, stage.Fired);
    }

    [AvaloniaFact]
    public void B6_A_Disabled_Ancestor_Is_Not_Its_Own_Disabled()
    {
        using var stage = new Stage();
        stage.Rows.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.True(stage.Infinite.IsVisible);
        stage.ScrollTo(530);
        Assert.Equal(1, stage.Fired);
    }

    // ── B7: Ionic's quirk ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_Disabled_While_Loading_Then_Enabled_It_Never_Fires_Again()
    {
        // Measured: disabling clears isLoading, not didFire, so complete() returns at once and didFire stays.
        using var stage = new Stage();
        stage.ScrollTo(530);
        Assert.Equal(1, stage.Fired);

        stage.Infinite.IsEnabled = false;
        stage.Infinite.IsEnabled = true;
        Dispatcher.UIThread.RunJobs();
        stage.Infinite.Complete();
        stage.ScrollTo(0);
        stage.ScrollTo(740);
        Assert.Equal(1, stage.Fired);
    }

    // ── B8, B9: the top ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_At_The_Top_It_Starts_At_The_End_And_Fires_Near_The_Start()
    {
        using var stage = new Stage(InfiniteScrollPosition.Top);
        Assert.Equal(0, stage.Scroll.Offset.Y);
        stage.Frame();
        Assert.Equal(740, stage.Scroll.Offset.Y);
        // The scroll to the end is a scroll, and checks: far from the start
        stage.Frame();
        Assert.Equal(0, stage.Fired);

        // scrollTop - 84 - 126.6 < 0 from 210.6
        stage.ScrollTo(211);
        Assert.Equal(0, stage.Fired);
        stage.ScrollTo(210);
        Assert.Equal(1, stage.Fired);
    }

    [AvaloniaFact]
    public void B8_At_The_Top_Its_Scroll_To_The_End_Lays_Out_First()
    {
        // Added to a page already shown, with no layout before the frame: the end is the one with it in
        using var stage = new Stage();
        stage.Rows.Children.Remove(stage.Infinite);
        Dispatcher.UIThread.RunJobs();
        var top = new AvonicInfiniteScroll { Position = InfiniteScrollPosition.Top, Content = new Border { Height = 84 } };
        stage.Rows.Children.Insert(0, top);
        stage.Frame();
        Assert.Equal(740, stage.Scroll.Offset.Y);
    }

    [AvaloniaFact]
    public void B9_At_The_Top_Complete_Keeps_What_Is_Shown_In_Place_At_The_Fourth_Frame()
    {
        using var stage = new Stage(InfiniteScrollPosition.Top);
        stage.Frame();
        stage.ScrollTo(210);
        Assert.Equal(1, stage.Fired);

        // Ten rows above, and complete(), with no layout in between: the read two frames on lays them out
        for (var i = 0; i < 10; i++)
        {
            stage.Rows.Children.Insert(1, Stage.Row());
        }

        var start = stage.Clock.Now.TotalMilliseconds;
        stage.Infinite.Complete();
        stage.Clock.At(start + 33);
        Assert.Equal(210, stage.Scroll.Offset.Y);

        // Busy: a scroll in the meantime doesn't fire, and is overwritten
        stage.Scroll.Offset = new Vector(0, 100);
        stage.Clock.At(start + 63);
        Assert.Equal(1, stage.Fired);
        Assert.Equal(100, stage.Scroll.Offset.Y);
        stage.Clock.At(start + 64);
        Assert.Equal(710, stage.Scroll.Offset.Y);

        // Then it can fire again
        stage.Frame();
        Assert.Equal(1, stage.Fired);
        stage.ScrollTo(100);
        Assert.Equal(2, stage.Fired);
    }

    [AvaloniaFact]
    public void B9_At_The_Bottom_Complete_Changes_No_Position()
    {
        using var stage = new Stage();
        stage.ScrollTo(530);
        for (var i = 0; i < 10; i++)
        {
            stage.Rows.Children.Insert(0, Stage.Row());
        }

        stage.Infinite.Complete();
        stage.Frame(100);
        Assert.Equal(530, stage.Scroll.Offset.Y);
    }

    // ── B10: a forced layout ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_A_Check_Lays_Out_What_Is_Due_First()
    {
        using var stage = new Stage();
        stage.ScrollTo(400);
        Assert.Equal(0, stage.Fired);

        // Three rows fewer: 1434 - 84 - 401 - 126.6 - 844 < 0, though the last layout still says 1584
        stage.Rows.Children.RemoveRange(0, 3);
        stage.ScrollTo(401);
        Assert.Equal(1, stage.Fired);
    }

    // ── B11: it bubbles ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_IonInfinite_Bubbles()
    {
        using var stage = new Stage();
        object? source = null;
        stage.Content.AddHandler(AvonicInfiniteScroll.IonInfiniteEvent, (_, e) => source = e.Source);
        stage.ScrollTo(530);
        Assert.Same(stage.Infinite, source);
    }

    // ── B12: the scroller ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_A_ScrollViewer_With_Ion_Content_Scroll_Host_Is_A_Scroller()
    {
        using var clock = new TestClock();
        var fired = 0;
        var infinite = new AvonicInfiniteScroll { Content = new Border { Height = 84 } };
        infinite.IonInfinite += (_, _) => fired++;
        var rows = new StackPanel();
        for (var i = 0; i < 30; i++)
        {
            rows.Children.Add(Stage.Row());
        }

        rows.Children.Add(infinite);
        var scroll = new ScrollViewer
        {
            Theme = (ControlTheme)Application.Current!.FindResource("AvonicContentScrollViewer")!,
            Classes = { AvonicInfiniteScroll.ScrollHostClass },
            Content = rows,
        };
        using var scene = new Scene(scroll);

        scroll.Offset = new Vector(0, 529);
        clock.At(16);
        Assert.Equal(0, fired);
        scroll.Offset = new Vector(0, 530);
        clock.At(32);
        Assert.Equal(1, fired);
    }

    [AvaloniaFact]
    public void B12_Outside_A_Content_It_Logs_Ionics_Error_And_Does_Nothing()
    {
        using var clock = new TestClock();
        var previous = Logger.Sink;
        var sink = new ErrorSink();
        Logger.Sink = sink;
        try
        {
            var fired = 0;
            var infinite = new AvonicInfiniteScroll { Content = new Border { Height = 84 } };
            infinite.IonInfinite += (_, _) => fired++;
            var rows = new StackPanel();
            for (var i = 0; i < 30; i++)
            {
                rows.Children.Add(Stage.Row());
            }

            rows.Children.Add(infinite);
            // A ScrollViewer without the class isn't one
            var scroll = new ScrollViewer
            {
                Theme = (ControlTheme)Application.Current!.FindResource("AvonicContentScrollViewer")!,
                Content = rows,
            };
            using var scene = new Scene(scroll);
            scroll.Offset = new Vector(0, 740);
            clock.At(32);

            Assert.Equal(0, fired);
            Assert.Contains(sink.Errors, e => ReferenceEquals(e.Source, infinite) && e.Message == "AvonicInfiniteScroll must be used inside AvonicContent.");
        }
        finally
        {
            Logger.Sink = previous;
        }
    }

    [AvaloniaFact]
    public void B12_A_Scroll_Host_That_Is_No_ScrollViewer_Scrolls_By_The_One_Inside_It()
    {
        // As a ListBox does in its template: the host has the class, the ScrollViewer is between it and the infinite scroll
        using var clock = new TestClock();
        var fired = 0;
        var infinite = new AvonicInfiniteScroll { Content = new Border { Height = 84 } };
        infinite.IonInfinite += (_, _) => fired++;
        var rows = new StackPanel();
        for (var i = 0; i < 30; i++)
        {
            rows.Children.Add(Stage.Row());
        }

        rows.Children.Add(infinite);
        var scroll = new ScrollViewer
        {
            Theme = (ControlTheme)Application.Current!.FindResource("AvonicContentScrollViewer")!,
            Content = rows,
        };
        using var scene = new Scene(new Border { Classes = { AvonicInfiniteScroll.ScrollHostClass }, Child = scroll });

        scroll.Offset = new Vector(0, 530);
        clock.At(16);
        Assert.Equal(1, fired);
    }

    [AvaloniaFact]
    public void B12_The_Nearest_Scroll_Host_Wins_And_One_That_Is_No_ScrollViewer_Is_Logged()
    {
        var previous = Logger.Sink;
        var sink = new ErrorSink();
        Logger.Sink = sink;
        try
        {
            // closest() stops at the class, here on something that can't scroll, inside a content that does
            Border? host = null;
            using var stage = new Stage(wrap: infinite => host = new Border { Classes = { AvonicInfiniteScroll.ScrollHostClass }, Child = infinite });
            stage.ScrollTo(740);
            Assert.Equal(0, stage.Fired);
            Assert.Contains(sink.Errors, e => ReferenceEquals(e.Source, stage.Infinite) && e.Message == "AvonicInfiniteScroll found no ScrollViewer in {Host}.");
        }
        finally
        {
            Logger.Sink = previous;
        }
    }

    // ── B13: removed from the tree ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_Removed_It_Stops_Listening_And_Back_It_Listens_Again()
    {
        using var stage = new Stage();
        stage.Rows.Children.Remove(stage.Infinite);
        Dispatcher.UIThread.RunJobs();
        stage.ScrollTo(656);
        Assert.Equal(0, stage.Fired);

        stage.Rows.Children.Add(stage.Infinite);
        Dispatcher.UIThread.RunJobs();
        stage.ScrollTo(530);
        Assert.Equal(1, stage.Fired);
    }

    [AvaloniaFact]
    public void B13_A_Scroll_Waiting_For_Its_Frame_Is_Heard_Only_If_It_Is_Back_By_Then()
    {
        // A browser dispatches the scroll to whoever listens at the frame: removed and added again, it's still heard
        using (var stage = new Stage())
        {
            stage.Scroll.Offset = new Vector(0, 600);
            stage.Rows.Children.Remove(stage.Infinite);
            stage.Rows.Children.Add(stage.Infinite);
            Dispatcher.UIThread.RunJobs();
            stage.Frame();
            Assert.Equal(1, stage.Fired);
        }

        // Removed until after the frame, it isn't
        using (var stage = new Stage())
        {
            stage.Scroll.Offset = new Vector(0, 600);
            stage.Rows.Children.Remove(stage.Infinite);
            stage.Frame();
            stage.Rows.Children.Add(stage.Infinite);
            Dispatcher.UIThread.RunJobs();
            stage.Frame();
            Assert.Equal(0, stage.Fired);
        }
    }

    // ── B14: hidden ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_Inside_Something_Hidden_Its_Height_Is_Zero_So_It_Does_Not_Fire()
    {
        Panel? box = null;
        using var stage = new Stage(wrap: infinite => box = new Panel { Children = { infinite } });
        box!.IsVisible = false;
        Dispatcher.UIThread.RunJobs();

        stage.ScrollTo(656);
        Assert.Equal(0, stage.Fired);

        box.IsVisible = true;
        Dispatcher.UIThread.RunJobs();
        stage.ScrollTo(600);
        Assert.Equal(1, stage.Fired);
    }

    // ── B15: the content shows only while loading ────────────────────────────────────────────

    private static Control Loading(AvonicInfiniteScrollContent content) =>
        content.GetVisualDescendants().OfType<Control>().First(c => c.Name == "PART_Loading");

    [AvaloniaFact]
    public void B15_The_Content_Shows_Its_Loading_Part_Only_While_Its_Infinite_Scroll_Loads()
    {
        var content = new AvonicInfiniteScrollContent { MinHeight = 84, LoadingText = "Loading" };
        // Wrapped, as a descendant rule reaches it through anything
        using var stage = new Stage(child: new Border { Child = content });
        Assert.False(Loading(content).IsVisible);

        stage.ScrollTo(530);
        Assert.True(Loading(content).IsVisible);
        Assert.Contains(":infinite-scroll-loading", stage.Infinite.Classes);

        stage.Infinite.Complete();
        Assert.DoesNotContain(":infinite-scroll-loading", stage.Infinite.Classes);
        Assert.False(Loading(content).IsVisible);

        stage.ScrollTo(531);
        Assert.True(Loading(content).IsVisible);
        stage.Infinite.IsEnabled = false;
        Assert.False(Loading(content).IsVisible);
    }

    [AvaloniaFact]
    public void B15_A_Content_Leaving_A_Loading_Infinite_Scroll_Stops_Showing_It()
    {
        var content = new AvonicInfiniteScrollContent { MinHeight = 84 };
        using var stage = new Stage(child: content);
        stage.ScrollTo(530);
        Assert.True(Loading(content).IsVisible);

        stage.Infinite.Content = null;
        Assert.False(Loading(content).IsVisible);
        var other = new Border { Child = content };
        stage.Rows.Children.Insert(0, other);
        Dispatcher.UIThread.RunJobs();
        Assert.False(Loading(content).IsVisible);

        // and doesn't follow it any more
        other.Child = null;
        stage.Infinite.Content = new Border { Height = 84 };
        stage.Infinite.Complete();
        stage.ScrollTo(560);
        Assert.False(Loading(content).IsVisible);
    }

    // ── B16: the default spinner ─────────────────────────────────────────────────────────────

    private static AvonicSpinner Spinner(AvonicInfiniteScrollContent content) =>
        content.GetVisualDescendants().OfType<AvonicSpinner>().Single();

    [AvaloniaFact]
    public void B16_The_Spinner_Is_Crescent_Or_The_Configs_Once_And_None_For_Null()
    {
        using var clock = new TestClock();
        var previous = AvonicConfig.Spinner;
        try
        {
            AvonicConfig.Spinner = null;
            var host = new StackPanel();
            using var scene = new Scene(host);
            AvonicInfiniteScrollContent Attach(AvonicInfiniteScrollContent content)
            {
                host.Children.Add(content);
                Dispatcher.UIThread.RunJobs();
                return content;
            }

            var content = new AvonicInfiniteScrollContent();
            Assert.Null(content.LoadingSpinner);
            Attach(content);
            Assert.Equal(SpinnerName.Crescent, content.LoadingSpinner);
            Assert.Equal(SpinnerName.Crescent, Spinner(content).SpinnerName);
            Assert.True(Spinner(content).IsVisible);

            // Read once: a later attach keeps what the first gave
            AvonicConfig.Spinner = SpinnerName.Dots;
            host.Children.Remove(content);
            Attach(content);
            Assert.Equal(SpinnerName.Crescent, content.LoadingSpinner);

            Assert.Equal(SpinnerName.Dots, Attach(new AvonicInfiniteScrollContent()).LoadingSpinner);

            var none = Attach(new AvonicInfiniteScrollContent { LoadingSpinner = null });
            Assert.Null(none.LoadingSpinner);
            Assert.False(Spinner(none).IsVisible);

            var named = Attach(new AvonicInfiniteScrollContent { LoadingSpinner = SpinnerName.Lines });
            Assert.Equal(SpinnerName.Lines, Spinner(named).SpinnerName);
        }
        finally
        {
            AvonicConfig.Spinner = previous;
        }
    }

    // ── B18: the text as it's written ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_Loading_Text_Is_Shown_As_It_Is_Written_And_None_For_Null()
    {
        var content = new AvonicInfiniteScrollContent { LoadingText = "<b>bold</b> &amp; x" };
        var none = new AvonicInfiniteScrollContent();
        using var scene = new Scene(new StackPanel { Children = { content, none } });

        var text = content.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal("<b>bold</b> &amp; x", text.Text);
        Assert.True(text.IsVisible);
        Assert.False(none.GetVisualDescendants().OfType<Control>().First(c => c.Name == "PART_LoadingText").IsVisible);
    }
}
