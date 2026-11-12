using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Gestures.GestureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ion-refresher's md native refresher, from <c>refresher.js</c>'s <c>setupMDNativeRefresher</c> and <c>refresher.utils.js</c>,
/// driven by a real finger on the headless window and the test clock. The numbers from Chromium are in
/// docs/components/ion-refresher/ref/measure-*.json.
/// </summary>
public class AvonicRefresherTests
{
    /// <summary>
    /// The page of the comparison: a header 56 high, then a content holding the refresher in its fixed slot and a column of
    /// rows. A press at (195, 200), and a drag down in 8 px steps, 16 ms apart.
    /// </summary>
    internal sealed class Page : IDisposable
    {
        private Finger? _finger;
        private double _y;

        public Page(bool header = true, AvonicRefresherContent? refresherContent = null, bool fixedSlot = true, double rows = 40)
        {
            RefresherContent = refresherContent ?? new AvonicRefresherContent();
            Refresher = new AvonicRefresher { Content = RefresherContent };
            Column = new StackPanel();
            for (var i = 0; i < rows; i++)
            {
                Column.Children.Add(new Border { Height = 48, Background = Brushes.Transparent, Focusable = true });
            }

            Content = new AvonicContent { Content = Column };
            if (fixedSlot)
            {
                Content.FixedContent = Refresher;
            }
            else
            {
                Column.Children.Insert(0, Refresher);
            }

            Header = new AvonicHeader { Height = 56 };
            var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
            if (header)
            {
                grid.Children.Add(Header);
            }

            Grid.SetRow(Content, 1);
            grid.Children.Add(Content);
            Scene = new Scene(grid);
            Scroll = Content.GetScrollElement()!;
            Scroll.IsScrollInertiaEnabled = false;


            Refresher.IonStart += (_, _) => Log.Add(("ionStart", Clock.Now.TotalMilliseconds));
            Refresher.IonPullStart += (_, _) => Log.Add(("ionPullStart", Clock.Now.TotalMilliseconds));
            Refresher.IonPull += (_, _) => Log.Add(("ionPull", Clock.Now.TotalMilliseconds));
            Refresher.IonRefresh += (_, e) =>
            {
                Log.Add(("ionRefresh", Clock.Now.TotalMilliseconds));
                LastRefresh = e;
            };
            Refresher.IonPullEnd += (_, e) =>
            {
                Log.Add(("ionPullEnd:" + e.Reason, Clock.Now.TotalMilliseconds));
            };
        }

        public TestClock Clock { get; } = new();
        public AvonicHeader Header { get; }
        public AvonicContent Content { get; }
        public AvonicRefresher Refresher { get; }
        public AvonicRefresherContent RefresherContent { get; }
        public StackPanel Column { get; }
        public ScrollViewer Scroll { get; }
        public Scene Scene { get; }
        public List<(string Name, double At)> Log { get; } = [];
        public RefresherEventArgs? LastRefresh { get; private set; }
        public double At { get; private set; } = 1000;

        public IEnumerable<string> Names => Log.Select(e => e.Name);

        public T Part<T>(string name) where T : Control =>
            RefresherContent.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

        /// <summary>Presses at (195, 200) and drags by <paramref name="dy"/> in 8 px steps, or on from the last drag.</summary>
        public Page Drag(double dy, double step = 8)
        {
            if (_finger is null)
            {
                _y = 200;
                _finger = new Finger(Scene, Clock).Down(195, _y, At);
            }

            var target = _y + dy;
            while (Math.Abs(target - _y) > 1e-9)
            {
                _y += Math.Sign(dy) * Math.Min(step, Math.Abs(target - _y));
                At += 16;
                _finger.Move(195, _y, At);
            }

            return this;
        }

        /// <summary>Lets go 16 ms after the last move, when that move's frame has run.</summary>
        public Page Lift()
        {
            At += 16;
            _finger!.Up(195, _y, At);
            _finger = null;
            return this;
        }

        /// <summary>Runs the clock on by <paramref name="ms"/>.</summary>
        public Page Wait(double ms)
        {
            At += ms;
            Clock.At(At);
            Settle();
            return this;
        }

        public bool Shows(string part) => Part<Control>(part).IsEffectivelyVisible;

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    /// <summary>The y of a part's top in the window, with its render transform, as getBoundingClientRect gives it.</summary>
    internal static Rect BoundsInWindow(Visual visual, Visual window)
    {
        var transform = visual.TransformToVisual(window)!.Value;
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    // ── B1–B3: the pull ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_A_Pull_From_The_Top_Starts_At_The_First_Move_After_The_Capture_And_Pulls_On_Each_Move()
    {
        using var page = new Page();

        // The gesture captures at the first move past 5 px, at 208: the next move starts the pull, with no progress yet
        page.Drag(8);
        Assert.Empty(page.Log);
        page.Drag(8).Wait(16);
        Assert.Equal(["ionStart", "ionPullStart"], page.Names);
        Assert.True(((IPseudoClasses)page.Refresher.Classes).Contains(":pulling"));
        Assert.Equal(0, page.Refresher.GetProgress());

        // Then each frame's move pulls, by (deltaY / 180) * 0.5 from the capture
        page.Drag(72).Wait(16);
        Assert.Equal(9, page.Names.Count(n => n == "ionPull"));
        Assert.Equal((288 - 208) / 360d, page.Refresher.GetProgress(), 9);

        // and no further than 1
        page.Drag(400).Wait(16);
        Assert.Equal(1, page.Refresher.GetProgress());
        Assert.True(page.Shows("PART_Pulling"));
        Assert.False(page.Shows("PART_Refreshing"));
    }

    [AvaloniaFact]
    public void B2_The_Pulling_Frame_Is_Chromiums()
    {
        // Chromium, pulled with a header (translate): the circle's dash was 53.5331px, which puts the eased time at
        // 0.2 + 52.5331 / 99 * 0.35; every other value is read at that time. measure-pull.json, pull-0.25.
        var eased = 0.2 + (53.5331 - 1) / 99 * 0.35;
        var step = InverseEaseOut(eased);
        var frame = RefresherPullFrame.At(step, RefresherAnimationType.Translate, 60);
        Assert.Equal(53.5331, frame.CircleDash, 3);
        Assert.Equal(1.71567, frame.IconTransform.M32, 3);
        Assert.Equal(0.3, frame.SpinnerArrowOpacity, 6);
        AssertRotation(0.900949, 0.433925, frame.SpinnerRotation);
        AssertRotation(-0.104697, 0.994504, frame.ArrowContainerRotation);
        Assert.Equal(0.342892, frame.ArrowTransform.M11, 3);
        Assert.Equal(0.799879, frame.ArrowTransform.M31, 3);

        // Without a header (scale), a short pull: matrix(0.452466, 0, 0, 0.452466, 0, 5.60811). measure-scale.json, short-0.3.
        var scaled = RefresherPullFrame.At(InverseEaseOut(0.452466), RefresherAnimationType.Scale, 60);
        Assert.Equal(0.452466, scaled.IconTransform.M11, 4);
        Assert.Equal(5.60811, scaled.IconTransform.M32, 3);
        Assert.Equal(72.4118, scaled.CircleDash, 2);
        Assert.Equal(0.317261, scaled.SpinnerArrowOpacity, 3);
    }

    private static double InverseEaseOut(double eased)
    {
        var easing = new CubicBezierEasing(0, 0, 0.58, 1);
        double lo = 0, hi = 1;
        for (var i = 0; i < 60; i++)
        {
            var mid = (lo + hi) / 2;
            if (easing.Ease(mid) < eased)
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return (lo + hi) / 2;
    }

    /// <summary>RotateTransform builds its matrix through its own angle maths, so only to 1e-6.</summary>
    private static void AssertNear(Matrix expected, Matrix actual)
    {
        Assert.Equal(expected.M11, actual.M11, 6);
        Assert.Equal(expected.M12, actual.M12, 6);
        Assert.Equal(expected.M21, actual.M21, 6);
        Assert.Equal(expected.M22, actual.M22, 6);
        Assert.Equal(expected.M31, actual.M31, 6);
        Assert.Equal(expected.M32, actual.M32, 6);
    }

    private static void AssertRotation(double cos, double sin, double degrees)
    {
        Assert.Equal(cos, Math.Cos(degrees * Math.PI / 180), 4);
        Assert.Equal(sin, Math.Sin(degrees * Math.PI / 180), 4);
    }

    [AvaloniaFact]
    public void B3_The_Pull_Moves_The_Parts_And_A_Header_Before_The_Content_Makes_It_Translate() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        page.Drag(8 + 90).Wait(16);
        var progress = page.Refresher.GetProgress();
        Assert.Equal(90 / 360d, progress, 9);
        var expected = RefresherPullFrame.At(progress, RefresherAnimationType.Translate, 60);

        var icon = page.Part<Border>("PART_PullingIcon");
        Assert.Equal(expected.IconTransform, icon.RenderTransform!.Value);
        Assert.Equal(expected.SpinnerArrowOpacity, page.Part<Panel>("PART_SpinnerArrowContainer").Opacity, 9);
        Assert.Equal(expected.CircleDash, page.Part<RefresherCircle>("PART_PullingCircle").Dash, 9);
        AssertNear(Matrix.CreateRotation(expected.SpinnerRotation * Math.PI / 180), page.Part<Panel>("PART_PullingSpinner").RenderTransform!.Value);
        AssertNear(Matrix.CreateRotation(expected.ArrowContainerRotation * Math.PI / 180), page.Part<Panel>("PART_ArrowContainer").RenderTransform!.Value);
        Assert.Equal(expected.ArrowTransform, page.Part<AvonicIcon>("PART_Arrow").RenderTransform!.Value);

        // Chromium's icon, unmoved, is 42 square at (174, 65): 9 down the 60 px refresher, below the 56 px header
        var moved = BoundsInWindow(icon, page.Scene.Window);
        var natural = moved.Translate(new Vector(0, -expected.IconTransform.M32));
        Assert.Equal(new Rect(174, 65, 42, 42), new Rect(natural.X, Math.Round(natural.Y, 9), natural.Width, natural.Height));

        // border-radius: 100% of its 42 px
        Assert.Equal(new CornerRadius(21), icon.CornerRadius);
    });

    [AvaloniaFact]
    public void B4_With_No_Header_Before_It_The_Icon_Scales_In() => InstalledTheme.Run(_ =>
    {
        using var page = new Page(header: false);
        page.Drag(8 + 90).Wait(16);
        var expected = RefresherPullFrame.At(page.Refresher.GetProgress(), RefresherAnimationType.Scale, 60);
        Assert.Equal(expected.IconTransform, page.Part<Border>("PART_PullingIcon").RenderTransform!.Value);
        Assert.NotEqual(1, expected.IconTransform.M11);
    });

    // ── B5–B6: letting go ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Letting_Go_Past_0_4_Snaps_The_Icon_Back_In_125ms_Then_Refreshes() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        page.Drag(8 + 180).Wait(16);
        Assert.Equal(0.5, page.Refresher.GetProgress(), 9);
        var rotation = page.Part<Panel>("PART_PullingSpinner").RenderTransform!.Value;
        page.Lift();
        var released = page.At;

        // writeTask, then the snap back from translateY(50px), linear
        page.Wait(16);
        var icon = page.Part<Border>("PART_PullingIcon");
        Assert.Equal(50, icon.RenderTransform!.Value.M32, 6);
        page.Wait(48);
        Assert.Equal(50 * (1 - 48 / 125d), icon.RenderTransform!.Value.M32, 6);

        // The pulling animation is held where it was
        Assert.Equal(rotation, page.Part<Panel>("PART_PullingSpinner").RenderTransform!.Value);
        Assert.DoesNotContain("ionRefresh", page.Names);

        // 125 ms after it started, it refreshes: the refreshing icon shows in the pulling one's place
        page.Wait(16 + 125 - 64 + 16);
        var refresh = Assert.Single(page.Log, e => e.Name == "ionRefresh");
        Assert.InRange(refresh.At - released, 16 + 125, 16 + 125 + 16);
        Assert.True(page.Shows("PART_Refreshing"));
        Assert.False(page.Shows("PART_Pulling"));
        Assert.Equal(new Rect(174, 65, 42, 42), BoundsInWindow(page.Part<Border>("PART_RefreshingIcon"), page.Scene.Window));
        Assert.DoesNotContain(page.Names, n => n.StartsWith("ionPullEnd"));
    });

    [AvaloniaFact]
    public void B6_Letting_Go_At_0_4_Or_Less_Plays_The_Pull_Back_Over_Progress_Times_500ms_And_Cancels()
    {
        using var page = new Page();
        page.Drag(8 + 108).Wait(16);
        Assert.Equal(0.3, page.Refresher.GetProgress(), 9);
        page.Lift();
        var released = page.At;

        // Back along the same frames, at 500 ms for the whole way
        page.Wait(16);
        var dash = page.Part<RefresherCircle>("PART_PullingCircle").Dash;
        Assert.Equal(RefresherPullFrame.At(0.3 - 16 / 500d, RefresherAnimationType.Translate, page.Refresher.Bounds.Height).CircleDash, dash, 6);
        Assert.DoesNotContain(page.Names, n => n.StartsWith("ionPullEnd"));

        // At 0.3 × 500 = 150 ms it's done: inactive, and nothing shows
        page.Wait(160 - 16);
        var end = Assert.Single(page.Log, e => e.Name.StartsWith("ionPullEnd"));
        Assert.Equal("ionPullEnd:Cancel", end.Name);
        Assert.InRange(end.At - released, 150, 166);
        Assert.DoesNotContain("ionRefresh", page.Names);
        Assert.False(page.Shows("PART_Pulling"));
        Assert.False(((IPseudoClasses)page.Refresher.Classes).Contains(":active"));
        Assert.Equal(RefresherPullFrame.BaseCircleDash, page.Part<RefresherCircle>("PART_PullingCircle").Dash);

        // and it can be pulled again
        page.Drag(32).Wait(16);
        Assert.Equal(2, page.Names.Count(n => n == "ionStart"));
    }

    // ── B7–B8: complete and cancel ─────────────────────────────────────────────────────────────

    private static Page Refreshing()
    {
        var page = new Page();
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        Assert.Contains("ionRefresh", page.Names);
        return page;
    }

    [AvaloniaFact]
    public void B7_Complete_Waits_Two_Frames_Then_Shrinks_The_Icon_In_200ms_And_Ends() => InstalledTheme.Run(_ =>
    {
        using var page = Refreshing();
        page.LastRefresh!.Complete();
        var called = page.At;

        page.Wait(16);
        Assert.False(((IPseudoClasses)page.Refresher.Classes).Contains(":completing"));
        page.Wait(16);
        Assert.True(((IPseudoClasses)page.Refresher.Classes).Contains(":completing"));

        // transition: 200ms, ease. Chromium had scale 0.4239 at about 88 ms in, and 0.0394 at about 168.
        var icon = page.Part<Border>("PART_RefreshingIcon");
        page.Wait(96);
        var ease = new CubicBezierEasing(0.25, 0.1, 0.25, 1);
        Assert.Equal(1 - ease.Ease(96 / 200d), icon.RenderTransform!.Value.M11, 6);

        page.Wait(104);
        var end = Assert.Single(page.Log, e => e.Name.StartsWith("ionPullEnd"));
        Assert.Equal("ionPullEnd:Complete", end.Name);
        Assert.Equal(called + 32 + 200, end.At, 0);
        Assert.False(page.Shows("PART_Refreshing"));
        Assert.False(((IPseudoClasses)page.Refresher.Classes).Contains(":active"));
    });

    [AvaloniaFact]
    public void B8_Cancel_Shows_Both_Icons_And_Ends_At_Ionics_700ms_Fallback() => InstalledTheme.Run(_ =>
    {
        using var page = Refreshing();
        page.Refresher.Cancel();
        var called = page.At;
        page.Wait(32);
        Assert.True(((IPseudoClasses)page.Refresher.Classes).Contains(":cancelling"));
        Assert.True(page.Shows("PART_Pulling"));
        Assert.True(page.Shows("PART_Refreshing"));

        page.Wait(699);
        Assert.DoesNotContain(page.Names, n => n.StartsWith("ionPullEnd"));
        page.Wait(1);
        var end = Assert.Single(page.Log, e => e.Name.StartsWith("ionPullEnd"));
        Assert.Equal("ionPullEnd:Cancel", end.Name);
        Assert.Equal(called + 32 + 700, end.At, 0);
        Assert.False(page.Shows("PART_Refreshing"));
    });

    // ── B9–B12: when it pulls ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_A_Scrolled_Content_Scrolls_And_Does_Not_Pull()
    {
        using var page = new Page();
        page.Scroll.Offset = new Vector(0, 100);
        page.Wait(16);
        page.Drag(60).Lift().Wait(200);
        Assert.Empty(page.Log);
        Assert.True(page.Scroll.Offset.Y < 100);
    }

    [AvaloniaFact]
    public void B10_An_Upward_Drag_From_The_Top_Scrolls_The_Content_And_Does_Not_Pull()
    {
        using var page = new Page();
        page.Drag(-100).Lift().Wait(200);
        Assert.Empty(page.Log);
        Assert.True(page.Scroll.Offset.Y > 50, $"scrolled {page.Scroll.Offset.Y}");
    }

    [AvaloniaFact]
    public void B11_Not_While_Refreshing()
    {
        using var page = Refreshing();
        var before = page.Log.Count;
        page.Drag(100).Lift().Wait(200);
        Assert.Equal(before, page.Log.Count);
    }

    [AvaloniaFact]
    public void B12_A_First_Move_Upward_After_The_Capture_Cancels_The_Pull()
    {
        using var page = new Page();
        page.Drag(8).Drag(-6, step: 6).Wait(16).Drag(100).Lift().Wait(600);
        Assert.Empty(page.Log);
    }

    [AvaloniaFact]
    public void B13_Disabled_Does_Not_Pull_And_Enabling_Again_Does()
    {
        using var page = new Page();
        page.Refresher.IsEnabled = false;
        page.Drag(100).Lift().Wait(600);
        Assert.Empty(page.Log);

        page.Refresher.IsEnabled = true;
        page.Drag(40).Wait(16);
        Assert.Contains("ionStart", page.Names);
    }

    [AvaloniaFact]
    public void B14_Outside_A_Contents_Fixed_Slot_It_Does_Nothing()
    {
        using var page = new Page(fixedSlot: false);
        page.Drag(100).Lift().Wait(600);
        Assert.Empty(page.Log);
        Assert.False(((IPseudoClasses)page.Refresher.Classes).Contains(":native"));
    }

    [AvaloniaFact]
    public void B15_The_Contents_Defaults_Are_Mds_And_With_No_Spinner_It_Is_Not_Native()
    {
        using (var page = new Page())
        {
            Assert.Equal(SpinnerName.Circular, page.RefresherContent.PullingIcon);
            Assert.Equal(SpinnerName.Circular, page.RefresherContent.RefreshingSpinner);
            Assert.True(((IPseudoClasses)page.Refresher.Classes).Contains(":native"));
        }

        var previous = AvonicConfig.Spinner;
        AvonicConfig.Spinner = SpinnerName.Dots;
        try
        {
            using var page = new Page();
            Assert.Equal(SpinnerName.Circular, page.RefresherContent.PullingIcon);
            Assert.Equal(SpinnerName.Dots, page.RefresherContent.RefreshingSpinner);
        }
        finally
        {
            AvonicConfig.Spinner = previous;
        }

        using (var page = new Page(refresherContent: new AvonicRefresherContent { RefreshingSpinner = null }))
        {
            Assert.False(((IPseudoClasses)page.Refresher.Classes).Contains(":native"));
            page.Drag(100).Lift().Wait(600);
            Assert.Empty(page.Log);
        }
    }

    [AvaloniaFact]
    public void B16_Another_Pulling_Spinner_Is_A_Paused_Spinner_That_Turns_With_No_Arrow() => InstalledTheme.Run(_ =>
    {
        using var page = new Page(refresherContent: new AvonicRefresherContent { PullingIcon = SpinnerName.Lines });
        page.Drag(8 + 120).Wait(16);
        var spinner = page.Part<AvonicSpinner>("PART_PullingOtherSpinner");
        Assert.True(spinner.IsEffectivelyVisible);
        Assert.True(spinner.Paused);
        Assert.Equal(SpinnerName.Lines, spinner.SpinnerName);
        Assert.False(page.Part<RefresherCircle>("PART_PullingCircle").IsEffectivelyVisible);
        Assert.False(page.Part<Panel>("PART_ArrowContainer").IsEffectivelyVisible);
        var expected = RefresherPullFrame.At(page.Refresher.GetProgress(), RefresherAnimationType.Translate, 60);
        AssertNear(Matrix.CreateRotation(expected.SpinnerRotation * Math.PI / 180), page.Part<Panel>("PART_PullingSpinner").RenderTransform!.Value);
    });

    // ── B17–B19: the rest ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_Refreshing_Clears_The_Focus()
    {
        using var page = new Page();
        var row = (Border)page.Column.Children[10];
        row.Focus();
        Assert.True(row.IsFocused);
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        Assert.Contains("ionRefresh", page.Names);
        Assert.False(row.IsFocused);
    }

    [AvaloniaFact]
    public void B18_It_Takes_No_Input_And_Fills_The_Contents_Width_60_High() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        Assert.False(page.Refresher.IsHitTestVisible);
        Assert.Equal(new Rect(0, 56, 390, 60), BoundsInWindow(page.Refresher, page.Scene.Window));
        Render();
        var hit = page.Scene.Window.InputHitTest(new Point(195, 80));
        Assert.True(hit is Visual v && page.Column.IsVisualAncestorOf(v) || ReferenceEquals(hit, page.Column), $"hit {hit}");
    });

    [AvaloniaFact]
    public void B19_Complete_And_Cancel_When_Not_Native_Close_In_600ms_Showing_Nothing()
    {
        // complete() and cancel() take close() when nativeRefresher is false: the state at once, inactive and ionPullEnd 600 ms on
        using var page = new Page(fixedSlot: false);
        page.Wait(0);
        page.Refresher.Complete();
        Assert.True(((IPseudoClasses)page.Refresher.Classes).Contains(":completing"));
        Assert.False(page.Shows("PART_Refreshing"));
        page.Refresher.Cancel();
        page.Wait(599);
        Assert.Empty(page.Log);
        page.Wait(1);
        Assert.Equal(["ionPullEnd:Complete", "ionPullEnd:Cancel"], page.Names);
        Assert.False(((IPseudoClasses)page.Refresher.Classes).Contains(":active"));
    }

    [AvaloniaFact]
    public void B20_The_Refreshing_Circle_Starts_655ms_Into_Its_Animation_When_It_Shows_And_Stops_When_It_Hides() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        var circle = page.Part<RefresherCircle>("PART_RefreshingCircle");
        Assert.False(circle.IsRunning);
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        Assert.Contains("ionRefresh", page.Names);
        Assert.True(circle.IsEffectivelyVisible);
        Assert.False(page.Part<AvonicSpinner>("PART_RefreshingSpinner").IsEffectivelyVisible);

        // When it showed, its animation was already 655 ms in: spinner-circular-inner's arc there, not the 1 px it starts with
        var refreshedAt = page.Log.Single(e => e.Name == "ionRefresh").At;
        var config = SpinnerConfigs.Get(SpinnerName.Circular);
        var expected = SpinnerFrame.Compute(config, config.Dur, 0, page.Clock.Now.TotalMilliseconds - refreshedAt + 655).Arcs!;
        Assert.Equal(expected, circle.CurrentArcs());
        Assert.Equal(TimeSpan.FromMilliseconds(refreshedAt - 655), circle.StartedAt);

        page.LastRefresh!.Complete();
        page.Wait(400);
        Assert.False(circle.IsRunning);
    });

    [AvaloniaFact]
    public void B22_Cancel_With_No_Pull_Still_Cancels_With_The_Pulling_Icon_At_Its_Resting_Place_Above() => InstalledTheme.Run(_ =>
    {
        // Ionic resets whatever the state: cancelling shows .refresher-pulling, its icon at translateY(calc(-100% - 10px))
        using var page = new Page();
        page.Wait(0);
        page.Refresher.Cancel();
        page.Wait(32);
        Assert.True(((IPseudoClasses)page.Refresher.Classes).Contains(":cancelling"));
        Assert.Equal(-52, page.Part<Border>("PART_PullingIcon").RenderTransform!.Value.M32);
        page.Wait(700);
        Assert.Equal(["ionPullEnd:Cancel"], page.Names);
    });

    [AvaloniaFact]
    public void B21_Another_Refreshing_Spinner_Is_An_AvonicSpinner() => InstalledTheme.Run(_ =>
    {
        using var page = new Page(refresherContent: new AvonicRefresherContent { RefreshingSpinner = SpinnerName.Dots });
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        var spinner = page.Part<AvonicSpinner>("PART_RefreshingSpinner");
        Assert.True(spinner.IsEffectivelyVisible);
        Assert.Equal(SpinnerName.Dots, spinner.SpinnerName);
        Assert.False(page.Part<RefresherCircle>("PART_RefreshingCircle").IsEffectivelyVisible);
        Assert.False(page.Part<RefresherCircle>("PART_RefreshingCircle").IsRunning);
    });

    // ── R1–R5: from the first review ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R1_Removed_Mid_Pull_And_Back_An_Upward_Drag_Still_Scrolls()
    {
        using var page = new Page();
        page.Drag(8 + 40).Wait(16);
        Assert.Contains("ionStart", page.Names);
        var grid = (Grid)page.Content.Parent!;
        grid.Children.Remove(page.Content);
        page.Wait(16);
        grid.Children.Add(page.Content);
        page.Wait(16);
        page.Lift().Wait(200);

        // Leaving the tree took the pointer from the scroll element: the pull ended there, as Ionic's touchcancel ends it
        Assert.Contains("ionPullEnd:Cancel", page.Names);
        Assert.False(((IPseudoClasses)page.Refresher.Classes).Contains(":active"));

        page.Drag(-100).Lift().Wait(200);
        Assert.True(page.Scroll.Offset.Y > 50, $"scrolled {page.Scroll.Offset.Y}");
    }

    [AvaloniaFact]
    public void R2_It_Shares_The_Fixed_Slot_Through_A_Panel()
    {
        using var page = new Page();
        page.Content.FixedContent = null;
        page.Wait(16);
        page.Content.FixedContent = new Panel { Children = { page.Refresher, new AvonicFab() } };
        page.Wait(16);
        page.Drag(8 + 40).Wait(16);
        Assert.Contains("ionStart", page.Names);
    }

    [AvaloniaFact]
    public void R3_Disabled_In_The_Refresh_Handler_Stays_Disabled()
    {
        using var page = new Page();
        page.Refresher.IonRefresh += (_, _) => page.Refresher.IsEnabled = false;
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        page.LastRefresh!.Complete();
        page.Wait(400);
        Assert.Contains("ionPullEnd:Complete", page.Names);
        var before = page.Log.Count;

        page.Drag(8 + 200).Wait(16).Lift().Wait(400);
        Assert.Equal(before, page.Log.Count);
    }

    [AvaloniaFact]
    public void R4_A_Content_Given_After_It_Is_Attached_Is_Used()
    {
        using var page = new Page(refresherContent: new AvonicRefresherContent { RefreshingSpinner = null });
        Assert.False(((IPseudoClasses)page.Refresher.Classes).Contains(":native"));
        var content = new AvonicRefresherContent();
        page.Refresher.Content = content;
        page.Wait(16);
        Assert.True(((IPseudoClasses)page.Refresher.Classes).Contains(":native"));
        page.Drag(8 + 90).Wait(16);
        Assert.Contains("ionStart", page.Names);
        Assert.Equal(
            RefresherPullFrame.At(page.Refresher.GetProgress(), RefresherAnimationType.Translate, page.Refresher.Bounds.Height).CircleDash,
            content.GetVisualDescendants().OfType<RefresherCircle>().First(c => c.Name == "PART_PullingCircle").Dash);
    }

    [AvaloniaFact]
    public void R5_Every_Wait_Ends_At_The_Icons_Transitionend() => InstalledTheme.Run(_ =>
    {
        // Two completes: both waits end with the one transition, and each raises ionPullEnd, as each of Ionic's does
        using (var page = Refreshing())
        {
            page.LastRefresh!.Complete();
            page.Wait(16);
            page.Refresher.Complete();
            var first = page.At + 16;
            page.Wait(16 + 200 + 16);
            var ends = page.Log.Where(e => e.Name == "ionPullEnd:Complete").ToList();
            Assert.Equal(2, ends.Count);
            Assert.All(ends, e => Assert.Equal(first + 200, e.At, 0));
        }

        // A cancel, then a complete before the cancel's 700 ms: the completing transition ends both
        using (var page = Refreshing())
        {
            page.Refresher.Cancel();
            page.Wait(32 + 100);
            page.Refresher.Complete();
            var completing = page.At + 32;
            page.Wait(32 + 200);
            Assert.Equal(["ionPullEnd:Cancel", "ionPullEnd:Complete"], page.Names.Where(n => n.StartsWith("ionPullEnd")));
            Assert.All(page.Log.Where(e => e.Name.StartsWith("ionPullEnd")), e => Assert.Equal(completing + 200, e.At, 0));
        }
    });

    // ── R6: from the second review ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R6_A_Refresh_Completed_While_Detached_Still_Ends_And_It_Pulls_Again_After()
    {
        using var page = Refreshing();
        var grid = (Grid)page.Content.Parent!;
        grid.Children.Remove(page.Content);
        page.Wait(16);

        // disconnectedCallback turned the native refresher off, so complete() takes close()
        page.LastRefresh!.Complete();
        page.Wait(600);
        Assert.Contains("ionPullEnd:Complete", page.Names);

        grid.Children.Add(page.Content);
        page.Wait(16);
        var starts = page.Names.Count(n => n == "ionStart");
        page.Drag(8 + 40).Wait(16);
        Assert.Equal(starts + 1, page.Names.Count(n => n == "ionStart"));
    }
}
