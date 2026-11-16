using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Media;
using Avalonia.Styling;
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

        /// <param name="body">What the content holds around the column of rows, which is the column itself when null.</param>
        /// <param name="fixedBody">What the fixed slot holds around the refresher, which is the refresher itself when null.</param>
        public Page(
            bool header = true,
            AvonicRefresherContent? refresherContent = null,
            bool fixedSlot = true,
            double rows = 40,
            Func<StackPanel, Control>? body = null,
            Func<AvonicRefresher, Control>? fixedBody = null,
            ContentFixedSlotPlacement placement = ContentFixedSlotPlacement.After,
            IStyle[]? styles = null)
        {
            RefresherContent = refresherContent ?? new AvonicRefresherContent();
            Refresher = new AvonicRefresher { Content = RefresherContent };
            Column = new StackPanel();
            for (var i = 0; i < rows; i++)
            {
                Column.Children.Add(new Border { Height = 48, Background = Brushes.Transparent, Focusable = true });
            }

            Content = new AvonicContent { Content = body?.Invoke(Column) ?? Column, FixedSlotPlacement = placement };
            if (fixedSlot)
            {
                Content.FixedContent = fixedBody?.Invoke(Refresher) ?? Refresher;
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
            Scene = styles is null ? new Scene(grid) : Scene.Styled(grid, styles);
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
        var circle = page.Part<AvonicSpinner>("PART_RefreshingSpinner");
        Assert.False(circle.IsRequestingFrames);
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        Assert.Contains("ionRefresh", page.Names);
        Assert.True(circle.IsEffectivelyVisible);
        Assert.Equal(SpinnerName.Circular, circle.SpinnerName);
        Assert.True(circle.IsRequestingFrames);

        // When it showed, its animation was already 655 ms in: spinner-circular-inner's arc there, not the 1 px it starts with
        var refreshedAt = page.Log.Single(e => e.Name == "ionRefresh").At;
        var config = SpinnerConfigs.Get(SpinnerName.Circular);
        var expected = SpinnerFrame.Compute(config, config.Dur, 0, page.Clock.Now.TotalMilliseconds - refreshedAt + 655).Arcs!;
        Assert.Equal(expected, Assert.Single(circle.CurrentFrame()).Arcs!);

        page.LastRefresh!.Complete();
        page.Wait(400);
        Assert.False(circle.IsEffectivelyVisible);
        Assert.False(circle.IsRequestingFrames);
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

        // The animation-delay Ionic sets is on the host and the first circle, which dots don't animate: they start from 0 at the show
        var refreshedAt = page.Log.Single(e => e.Name == "ionRefresh").At;
        var config = SpinnerConfigs.Get(SpinnerName.Dots);
        var elapsed = page.Clock.Now.TotalMilliseconds - refreshedAt;
        Assert.Equal(
            Enumerable.Range(0, config.Count).Select(i => SpinnerFrame.Compute(config, config.Dur, i, elapsed)),
            spinner.CurrentFrame());
    });

    [AvaloniaFact]
    public void B20_With_A_Pulling_Spinner_Of_Lines_The_Refreshing_Circle_Starts_From_0() => InstalledTheme.Run(_ =>
    {
        // setupMDNativeRefresher sets the delay only if both spinners have a circle, and lines have none
        using var page = new Page(refresherContent: new AvonicRefresherContent { PullingIcon = SpinnerName.Lines });
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        var spinner = page.Part<AvonicSpinner>("PART_RefreshingSpinner");
        Assert.True(spinner.IsEffectivelyVisible);
        var refreshedAt = page.Log.Single(e => e.Name == "ionRefresh").At;
        var config = SpinnerConfigs.Get(SpinnerName.Circular);
        var expected = SpinnerFrame.Compute(config, config.Dur, 0, page.Clock.Now.TotalMilliseconds - refreshedAt).Arcs!;
        Assert.Equal(expected, Assert.Single(spinner.CurrentFrame()).Arcs!);
    });

    [AvaloniaFact]
    public void B20_From_Refreshing_To_Completing_The_Circle_Carries_On_Even_With_The_Pulling_Icon_Changed() => InstalledTheme.Run(_ =>
    {
        // The class list changes at once, so .refresher-refreshing stays shown and its animation runs on; and Ionic sets the delay
        // once, at setup, so a pulling icon changed in the middle moves nothing
        using var page = new Page();
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        var spinner = page.Part<AvonicSpinner>("PART_RefreshingSpinner");
        var refreshedAt = page.Log.Single(e => e.Name == "ionRefresh").At;
        page.RefresherContent.PullingIcon = SpinnerName.Lines;
        page.LastRefresh!.Complete();
        page.Wait(32);
        Assert.True(((IPseudoClasses)page.Refresher.Classes).Contains(":completing"));
        Assert.True(spinner.IsEffectivelyVisible);
        var config = SpinnerConfigs.Get(SpinnerName.Circular);
        var expected = SpinnerFrame.Compute(config, config.Dur, 0, page.Clock.Now.TotalMilliseconds - refreshedAt + 655).Arcs!;
        Assert.Equal(expected, Assert.Single(spinner.CurrentFrame()).Arcs!);

        // The next refresh, with the lines now pulling, still starts 655 ms in
        page.Wait(1000);
        page.Log.Clear();
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        refreshedAt = page.Log.Single(e => e.Name == "ionRefresh").At;
        expected = SpinnerFrame.Compute(config, config.Dur, 0, page.Clock.Now.TotalMilliseconds - refreshedAt + 655).Arcs!;
        Assert.Equal(expected, Assert.Single(spinner.CurrentFrame()).Arcs!);
    });

    [AvaloniaFact]
    public void B20_A_Template_Applied_Mid_Refresh_Gives_Its_Circle_The_Delay_Too() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        var template = page.RefresherContent.Template;
        page.RefresherContent.Template = null;
        page.RefresherContent.ApplyTemplate();
        page.RefresherContent.Template = template;
        page.RefresherContent.ApplyTemplate();
        page.Wait(16);
        var shownAt = page.Clock.Now.TotalMilliseconds - 16;
        page.Wait(48);

        var spinner = page.Part<AvonicSpinner>("PART_RefreshingSpinner");
        Assert.True(spinner.IsEffectivelyVisible);
        var config = SpinnerConfigs.Get(SpinnerName.Circular);
        var frame = Assert.Single(spinner.CurrentFrame()).Arcs!;
        Assert.NotEqual(SpinnerFrame.Compute(config, config.Dur, 0, page.Clock.Now.TotalMilliseconds - shownAt).Arcs!, frame);
        Assert.Equal(TimeSpan.FromMilliseconds(-655), spinner.AnimationDelay);
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

    // The native icons' border: 1px solid var(ion-color-step-200, var(ion-background-color-step-200, #ececec)) and background:
    // var(ion-color-step-250, var(ion-background-color-step-250, #ffffff)). Light leaves the steps unset, and dark.always.css
    // sets them: measured rgb(65, 65, 65) and rgb(77, 77, 77).
    [AvaloniaFact]
    public void B23_The_Icons_Border_And_Background_Are_The_Literals_And_In_The_Dark_Palette_Steps_200_And_250() => InstalledTheme.Run(_ =>
    {
        static Color Solid(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

        void AssertIcons(AvonicRefresherContent content, string border, string background)
        {
            foreach (var name in new[] { "PART_PullingIcon", "PART_RefreshingIcon" })
            {
                var icon = content.GetVisualDescendants().OfType<Border>().First(b => b.Name == name);
                Assert.Equal(Color.Parse(border), Solid(icon.BorderBrush));
                Assert.Equal(Color.Parse(background), Solid(icon.Background));
            }
        }

        var light = new AvonicRefresherContent();
        using (new Scene(light))
        {
            AssertIcons(light, "#ECECEC", "#FFFFFF");
        }

        ToolbarTest.InDark(() =>
        {
            var dark = new AvonicRefresherContent();
            using var scene = new Scene(dark);
            AssertIcons(dark, "#414141", "#4D4D4D");
        });
    });

    // ── B25: the refresher's own config keys ───────────────────────────────────────────────────

    private static bool IsNative(Page page) => ((IPseudoClasses)page.Refresher.Classes).Contains(":native");

    // componentWillLoad: pullingIcon = config.get('refreshingIcon', 'circular') in md, and
    // refreshingSpinner = config.get('refreshingSpinner', config.get('spinner', 'circular')), each only if it's undefined. A key set
    // to null gives null, and with a spinner missing from either place it's not the native refresher.
    [AvaloniaFact]
    public void B25_The_RefreshingIcon_And_RefreshingSpinner_Configs_Come_First_And_None_Is_No_Spinner()
    {
        var (icon, refreshing, spinner) = (AvonicConfig.RefreshingIcon, AvonicConfig.RefreshingSpinner, AvonicConfig.Spinner);
        try
        {
            AvonicConfig.RefreshingIcon = SpinnerName.Lines;
            AvonicConfig.RefreshingSpinner = SpinnerName.Dots;
            AvonicConfig.Spinner = SpinnerName.Crescent;
            using (var page = new Page())
            {
                Assert.Equal(SpinnerName.Lines, page.RefresherContent.PullingIcon);
                Assert.Equal(SpinnerName.Dots, page.RefresherContent.RefreshingSpinner);
                Assert.True(IsNative(page));

                // Read once: the content keeps what its first load gave
                AvonicConfig.RefreshingIcon = SpinnerName.Bubbles;
                AvonicConfig.RefreshingSpinner = SpinnerName.Bubbles;
                page.Content.FixedContent = null;
                page.Wait(16);
                page.Content.FixedContent = page.Refresher;
                page.Wait(16);
                Assert.Equal(SpinnerName.Lines, page.RefresherContent.PullingIcon);
                Assert.Equal(SpinnerName.Dots, page.RefresherContent.RefreshingSpinner);

                // and cleared after it, a prop is undefined: none, not the config's again
                page.RefresherContent.ClearValue(AvonicRefresherContent.PullingIconProperty);
                page.RefresherContent.ClearValue(AvonicRefresherContent.RefreshingSpinnerProperty);
                Assert.Null(page.RefresherContent.PullingIcon);
                Assert.Null(page.RefresherContent.RefreshingSpinner);
            }

            // Its own values win
            using (var page = new Page(refresherContent: new AvonicRefresherContent
                   {
                       PullingIcon = SpinnerName.Circular, RefreshingSpinner = SpinnerName.Circles,
                   }))
            {
                Assert.Equal(SpinnerName.Circular, page.RefresherContent.PullingIcon);
                Assert.Equal(SpinnerName.Circles, page.RefresherContent.RefreshingSpinner);
            }

            // Not set, the refreshing spinner is the spinner config's
            AvonicConfig.RefreshingSpinner = null;
            using (var page = new Page())
            {
                Assert.Equal(SpinnerName.Crescent, page.RefresherContent.RefreshingSpinner);
            }

            // None is null, and the refresher isn't native, so it doesn't pull
            AvonicConfig.RefreshingIcon = ConfigSpinner.None;
            using (var page = new Page())
            {
                Assert.Null(page.RefresherContent.PullingIcon);
                Assert.False(IsNative(page));
                page.Drag(100).Lift().Wait(600);
                Assert.Empty(page.Log);
            }

            AvonicConfig.RefreshingIcon = null;
            AvonicConfig.RefreshingSpinner = ConfigSpinner.None;
            using (var page = new Page())
            {
                Assert.Equal(SpinnerName.Circular, page.RefresherContent.PullingIcon);
                Assert.Null(page.RefresherContent.RefreshingSpinner);
                Assert.False(IsNative(page));
            }

            // The spinner config's none reaches the refreshing spinner when its own key isn't set
            AvonicConfig.RefreshingSpinner = null;
            AvonicConfig.Spinner = ConfigSpinner.None;
            using (var page = new Page())
            {
                Assert.Null(page.RefresherContent.RefreshingSpinner);
                Assert.False(IsNative(page));
            }

            // and an own spinner beats a none
            using (var page = new Page(refresherContent: new AvonicRefresherContent { RefreshingSpinner = SpinnerName.Circular }))
            {
                Assert.True(IsNative(page));
            }
        }
        finally
        {
            (AvonicConfig.RefreshingIcon, AvonicConfig.RefreshingSpinner, AvonicConfig.Spinner) = (icon, refreshing, spinner);
        }
    }

    // ── B24: a custom scroll host ──────────────────────────────────────────────────────────────

    /// <summary>A host whose scroller is in its template, as a <c>ListBox</c>'s is.</summary>
    private sealed class TemplatedHost : TemplatedControl
    {
        public TemplatedHost(Control content, ScrollViewer scroller, bool styled)
        {
            scroller.Content = content;
            Scroller = scroller;
            if (!styled)
            {
                Template = Make;
            }
        }

        public ScrollViewer Scroller { get; }

        /// <summary>
        /// Its template from an app's style, as a <c>ListBox</c>'s comes from its theme: applied only once it's in the logical tree,
        /// which, with the fixed slot first, is after the refresher is attached.
        /// </summary>
        public static Style Styled => new(x => x.OfType<TemplatedHost>()) { Setters = { new Setter(TemplateProperty, Make) } };

        private static FuncControlTemplate<TemplatedHost> Make => new((host, _) => host.Scroller);
    }

    private static ScrollViewer InnerScroller() =>
        new() { Theme = (ControlTheme)Application.Current!.FindResource("AvonicContentScrollViewer")!, IsScrollInertiaEnabled = false };

    /// <summary>
    /// The content holds a host 400 high, which scrolls the rows, as a virtual scroller does, so the content's own scroller has
    /// nothing to scroll. The host has <c>ion-content-scroll-host</c>: on the scroller itself, on a border around it, or on a control
    /// with it in its template.
    /// </summary>
    private static Page ScrollHostPage(string kind, out ScrollViewer inner, ContentFixedSlotPlacement placement = ContentFixedSlotPlacement.After)
    {
        var scroller = InnerScroller();
        inner = scroller;
        var page = new Page(placement: placement, styles: kind == "Styled" ? [TemplatedHost.Styled] : null, body: column =>
        {
            Control host = kind switch
            {
                "ScrollViewer" => scroller,
                "Border" => new Border { Child = scroller },
                _ => new TemplatedHost(column, scroller, styled: kind == "Styled"),
            };
            if (host is not TemplatedHost)
            {
                scroller.Content = column;
            }

            host.Height = 400;
            host.Classes.Add(AvonicContent.ScrollHostClass);
            return host;
        });

        ScrollsByTouch(scroller);
        return page;
    }

    /// <summary>A touch drag scrolls it, as the content's own scroller's does: <see cref="AvonicContent"/> turns its recognizer on itself.</summary>
    private static void ScrollsByTouch(ScrollViewer scroller) =>
        scroller.GetVisualDescendants().OfType<ScrollContentPresenter>().Single()
            .GestureRecognizers.OfType<ScrollGestureRecognizer>().Single().CanVerticallyScroll = true;

    public static TheoryData<string, ContentFixedSlotPlacement> ScrollHostsAndPlacements => new()
    {
        { "ScrollViewer", ContentFixedSlotPlacement.After },
        { "Border", ContentFixedSlotPlacement.After },
        { "Templated", ContentFixedSlotPlacement.After },
        { "Templated", ContentFixedSlotPlacement.Before },
        { "Styled", ContentFixedSlotPlacement.After },
        { "Styled", ContentFixedSlotPlacement.Before },
    };

    // connectedCallback: getScrollElement(contentEl.querySelector('.ion-content-scroll-host') ?? contentEl). The pull starts only
    // when that element is at its top, as canStart reads its scrollTop, and the content's own scroller, always at its top here,
    // doesn't count. With the fixed slot before the scroller, the refresher is attached first.
    [AvaloniaTheory]
    [MemberData(nameof(ScrollHostsAndPlacements))]
    public void B24_A_Scroll_Host_In_The_Content_Is_The_Scroll_Element(string kind, ContentFixedSlotPlacement placement)
    {
        using (var page = ScrollHostPage(kind, out var inner, placement))
        {
            // The host is scrolled: it scrolls back, and it doesn't pull
            inner.Offset = new Vector(0, 100);
            page.Wait(16);
            Assert.Equal(0, page.Scroll.Offset.Y);
            page.Drag(60).Lift().Wait(200);
            Assert.Empty(page.Log);
            Assert.True(inner.Offset.Y < 100, $"scrolled to {inner.Offset.Y}");
        }

        using (var page = ScrollHostPage(kind, out var inner, placement))
        {
            // At its top, it pulls
            Assert.Equal(0, inner.Offset.Y);
            page.Drag(40).Wait(16);
            Assert.Equal(["ionStart", "ionPullStart"], page.Names.Take(2));
        }
    }

    private sealed class ErrorSink : Avalonia.Logging.ILogSink
    {
        public List<(object? Source, string Message)> Errors { get; } = [];

        public bool IsEnabled(Avalonia.Logging.LogEventLevel level, string area) => level >= Avalonia.Logging.LogEventLevel.Error;

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate) =>
            Errors.Add((source, messageTemplate));

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Errors.Add((source, messageTemplate));
    }

    // A host with nothing that scrolls: it waits for the content's layout, then logs, and doesn't take the content's own scroller,
    // as Ionic's would be the host itself.
    [AvaloniaFact]
    public void B24_A_Scroll_Host_With_No_ScrollViewer_Is_Logged_After_The_Layout_And_Nothing_Pulls()
    {
        var previous = Avalonia.Logging.Logger.Sink;
        var sink = new ErrorSink();
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            using var page = new Page(body: column => new Border { Classes = { AvonicContent.ScrollHostClass }, Child = column });
            Assert.Contains(sink.Errors, e => ReferenceEquals(e.Source, page.Refresher) && e.Message == "AvonicRefresher found no ScrollViewer in {Host}.");
            Assert.Single(sink.Errors, e => ReferenceEquals(e.Source, page.Refresher));
            page.Drag(100).Lift().Wait(600);
            Assert.Empty(page.Log);

            // Once: a later layout, which looks again, logs nothing more
            page.Column.Children.Add(new Border { Height = 48 });
            page.Wait(16);
            Assert.Single(sink.Errors, e => ReferenceEquals(e.Source, page.Refresher));
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    // A content whose replaced template has no scroller: logged, as getScrollElement would have nothing to scroll.
    [AvaloniaFact]
    public void B14_A_Content_With_No_Scroller_Is_Logged()
    {
        var previous = Avalonia.Logging.Logger.Sink;
        var sink = new ErrorSink();
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            var refresher = new AvonicRefresher { Content = new AvonicRefresherContent() };
            var content = new AvonicContent
            {
                FixedContent = refresher,
                Template = new FuncControlTemplate<AvonicContent>((c, scope) =>
                {
                    var slot = new ContentPresenter { Name = "PART_Fixed", [!ContentPresenter.ContentProperty] = c[!AvonicContent.FixedContentProperty] };
                    scope.Register(slot.Name, slot);
                    return new Panel { Children = { slot } };
                }),
            };
            using var scene = new Scene(content);
            Assert.True(refresher.IsAttachedToVisualTree());
            Assert.Contains(sink.Errors, e => ReferenceEquals(e.Source, refresher) && e.Message == "AvonicRefresher found no ScrollViewer in {Host}.");
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    // A host hidden when the refresher is attached has no template, so no scroller: it's logged once, and when it's shown and laid
    // out it's the scroll element, as Ionic's would be all along.
    [AvaloniaFact]
    public void B24_A_Hidden_Scroll_Host_Is_The_Scroll_Element_Once_It_Shows()
    {
        var previous = Avalonia.Logging.Logger.Sink;
        var sink = new ErrorSink();
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            var scroller = InnerScroller();
            TemplatedHost? host = null;
            using var page = new Page(styles: [TemplatedHost.Styled], body: column =>
            {
                host = new TemplatedHost(column, scroller, styled: true) { Height = 400, IsVisible = false };
                host.Classes.Add(AvonicContent.ScrollHostClass);
                return host;
            });
            page.Wait(16).Wait(16);
            Assert.Single(sink.Errors, e => ReferenceEquals(e.Source, page.Refresher));
            Assert.Empty(scroller.GetVisualDescendants());

            // Shown: the pull starts from it
            host!.IsVisible = true;
            page.Wait(16);
            ScrollsByTouch(scroller);
            page.Drag(40).Wait(16);
            Assert.Equal(["ionStart", "ionPullStart"], page.Names.Take(2));
            Assert.Single(sink.Errors, e => ReferenceEquals(e.Source, page.Refresher));
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    // querySelector searches all of the content's light DOM, so a host in the fixed slot counts too. Of a host in each slot, the
    // one in the content's own slot is taken here.
    [AvaloniaFact]
    public void B24_A_Scroll_Host_In_The_Fixed_Slot_Counts_After_One_In_The_Content()
    {
        ScrollViewer FixedHost(StackPanel? rows = null)
        {
            var scroller = InnerScroller();
            scroller.Classes.Add(AvonicContent.ScrollHostClass);
            scroller.Height = 400;
            if (rows is null)
            {
                // The fixed slot places its child at its own size, and the panel there is as high as the refresher's content
                scroller.Width = 390;
                scroller.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            }

            scroller.Content = rows ?? new StackPanel { Children = { new Border { Height = 2000, Background = Brushes.Transparent } } };
            return scroller;
        }

        // The fixed host, over the content, takes the press, and the pull starts from it
        var fixedHost = FixedHost();
        using (var page = new Page(fixedBody: refresher => new Panel { Children = { fixedHost, refresher } }))
        {
            ScrollsByTouch(fixedHost);
            page.Drag(40).Wait(16);
            Assert.Equal(["ionStart", "ionPullStart"], page.Names.Take(2));
        }

        // With one in the content too, the content's is taken: the fixed one, lower down and scrolled, doesn't stop the pull
        var second = FixedHost();
        second.Height = 100;
        second.Margin = new Thickness(0, 600, 0, 0);
        ScrollViewer? contentHost = null;
        using (var page = new Page(
                   fixedBody: refresher => new Panel { Children = { refresher, second } },
                   body: column => contentHost = FixedHost(column)))
        {
            ScrollsByTouch(second);
            ScrollsByTouch(contentHost!);
            second.Offset = new Vector(0, 100);
            page.Wait(16);
            Assert.Equal(0, contentHost!.Offset.Y);
            page.Drag(40).Wait(16);
            Assert.Equal(["ionStart", "ionPullStart"], page.Names.Take(2));
        }
    }

    // ── C1–C10: after a cancel (#167) ───────────────────────────────────────────────────────────
    // Measured in Chromium with docs/components/ion-refresher/ref/cancel.js.txt, into ref/measure-cancel.json.

    private static Rect Box(Page page, string part) => BoundsInWindow(page.Part<Control>(part), page.Scene.Window);

    /// <summary>
    /// How many of the refresher's two icons a user sees: shown, not scaled to nothing, and reaching below the content's top. Above
    /// it, the header covers an icon in Chromium, with its z-index, and the content clips it here.
    /// </summary>
    internal static int IconsSeen(Page page)
    {
        var top = BoundsInWindow(page.Content, page.Scene.Window).Y;
        return new[] { "PART_PullingIcon", "PART_RefreshingIcon" }.Count(name =>
        {
            var icon = page.Part<Border>(name);
            var box = BoundsInWindow(icon, page.Scene.Window);
            return icon.IsEffectivelyVisible && box.Width > 0.5 && box.Height > 0.5 && box.Bottom > top;
        });
    }

    [AvaloniaFact]
    public void C1_Cancelling_Shows_Both_Icons_Where_Chromium_Does_And_None_Once_It_Ends() => InstalledTheme.Run(_ =>
    {
        using var page = Refreshing();
        Assert.Equal(1, IconsSeen(page));
        page.Refresher.Cancel();
        page.Wait(32);

        // .refresher-cancelling shows .refresher-pulling, display: block, and .refresher-refreshing. The pulling icon, held at the
        // snap back's translateY(0), fills the block's 390, its spinner at the start; the column of both, 84 high, is centred on
        // the 60 px refresher at 56, so it starts 12 above it.
        Assert.Equal(new Rect(0, 44, 390, 42), Box(page, "PART_PullingIcon"));
        Assert.Equal(new Rect(9, 53, 24, 24), Box(page, "PART_PullingSpinner"));
        Assert.Equal(new Rect(174, 86, 42, 42), Box(page, "PART_RefreshingIcon"));
        Assert.Equal(2, IconsSeen(page));

        page.Wait(700);
        Assert.Contains("ionPullEnd:Cancel", page.Names);
        Assert.Equal(0, IconsSeen(page));
    });

    [AvaloniaFact]
    public void C2_A_Pull_Started_While_Cancelling_Moves_The_Icon() => InstalledTheme.Run(_ =>
    {
        // Chromium: the new pull's animation is newer than the held snap back, so it moves the icon (translateY 24.8, then 55.2)
        using var page = Refreshing();
        page.Refresher.Cancel();
        page.Wait(32 + 60);
        Assert.Equal(RefresherState.Cancelling, page.Refresher.State);

        page.Drag(8 + 180).Wait(16);
        Assert.Equal(RefresherState.Pulling, page.Refresher.State);
        var expected = RefresherPullFrame.At(page.Refresher.GetProgress(), RefresherAnimationType.Translate, 60);
        Assert.Equal(expected.IconTransform, page.Part<Border>("PART_PullingIcon").RenderTransform!.Value);
        Assert.Equal(1, IconsSeen(page));
    });

    [AvaloniaFact]
    public void C3_A_Pull_Overtaken_By_The_Cancels_End_And_Let_Go_With_No_Move_After_Leaves_The_Refresher_On() => InstalledTheme.Run(_ =>
    {
        // A deliberate difference. Measured in Chromium (F, G): resetNativeRefresher destroys the pull's animation mid-pull and sets
        // the progress to 0, so letting go with no move since plays a destroyed animation back, whose onFinish never comes, and
        // gesture.enable(true) with it, so nothing pulls again. Here the gesture comes back on as IsEnabled says. The reset has
        // already made it inactive and raised ionPullEnd, so nothing more is raised. A move past 0.4 after the reset refreshes (C10).
        using var page = Refreshing();
        page.Refresher.Cancel();
        page.Wait(32 + 60);
        page.Drag(8 + 180).Wait(16);
        page.Wait(700);
        Assert.Equal(0, IconsSeen(page));
        Assert.Equal(RefresherState.Inactive, page.Refresher.State);

        page.Lift().Wait(600);
        Assert.Single(page.Log, e => e.Name.StartsWith("ionPullEnd"));
        Assert.Equal(RefresherState.Inactive, page.Refresher.State);

        var starts = page.Names.Count(n => n == "ionStart");
        page.Drag(8 + 200).Wait(16);
        Assert.Equal(starts + 1, page.Names.Count(n => n == "ionStart"));
        Assert.Equal(1, IconsSeen(page));
        page.Lift().Wait(160);
        Assert.Equal(2, page.Names.Count(n => n == "ionRefresh"));
        Assert.Equal(RefresherState.Refreshing, page.Refresher.State);
    });

    [AvaloniaFact]
    public void C3_A_Pull_Let_Go_Short_Whose_Play_Back_The_Cancels_End_Destroys_Leaves_The_Refresher_On() => InstalledTheme.Run(_ =>
    {
        // The same bug from the other side: let go under 0.4 just before the cancel's end, the play back, about 140 ms, is
        // destroyed while it runs, and its end never comes in Ionic either.
        using var page = Refreshing();
        page.Refresher.Cancel();
        var cancelled = page.At;
        page.Wait(32 + 500);
        page.Drag(8 + 100, step: 50).Wait(16);
        page.Wait(cancelled + 32 + 700 - 50 - page.At);
        page.Lift();
        page.Wait(200);
        Assert.Equal(RefresherState.Inactive, page.Refresher.State);
        Assert.Single(page.Log, e => e.Name.StartsWith("ionPullEnd"));

        var starts = page.Names.Count(n => n == "ionStart");
        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        Assert.Equal(starts + 1, page.Names.Count(n => n == "ionStart"));
        Assert.Equal(2, page.Names.Count(n => n == "ionRefresh"));
    });

    [AvaloniaFact]
    public void C3_Disabled_While_Its_Play_Back_Runs_It_Stays_Off() => InstalledTheme.Run(_ =>
    {
        // As R3: the gesture comes back as IsEnabled says, so one disabled after letting go, before the reset destroys the play
        // back, stays off.
        using var page = Refreshing();
        page.Refresher.Cancel();
        var cancelled = page.At;
        page.Wait(32 + 500);
        page.Drag(8 + 100, step: 50).Wait(16);
        page.Wait(cancelled + 32 + 700 - 50 - page.At);
        page.Lift();
        page.Refresher.IsEnabled = false;
        page.Wait(200);

        var before = page.Log.Count;
        page.Drag(8 + 200).Wait(16).Lift().Wait(400);
        Assert.Equal(before, page.Log.Count);
    });

    [AvaloniaFact]
    public void C10_As_Ionics_A_Pull_Overtaken_By_The_Cancels_End_That_Moves_On_Past_0_4_Refreshes_When_Let_Go() => InstalledTheme.Run(_ =>
    {
        // Measured in Chromium (D): onMove goes on setting the progress after the reset, so a release past 0.4 takes the snap back,
        // which refreshes and turns the gesture on again: inactive 100 ms after letting go, refreshing at 400, and it pulls after
        using var page = Refreshing();
        page.Refresher.Cancel();
        page.Wait(32 + 60);
        page.Drag(8 + 100).Wait(16);
        page.Wait(700);
        Assert.Equal(RefresherState.Inactive, page.Refresher.State);
        Assert.Single(page.Log, e => e.Name.StartsWith("ionPullEnd"));

        page.Drag(100).Wait(16);
        Assert.True(page.Refresher.GetProgress() > RefresherPullFrame.RefreshThreshold);
        page.Lift().Wait(16 + 125 + 32);
        Assert.Equal(2, page.Names.Count(n => n == "ionRefresh"));
        Assert.Equal(RefresherState.Refreshing, page.Refresher.State);
        Assert.Equal(1, IconsSeen(page));

        page.LastRefresh!.Complete();
        page.Wait(32 + 200 + 16);
        var starts = page.Names.Count(n => n == "ionStart");
        page.Drag(8 + 40).Wait(16);
        Assert.Equal(starts + 1, page.Names.Count(n => n == "ionStart"));
    });

    [AvaloniaFact]
    public void C4_A_Short_Pull_Shows_One_Icon_As_It_Goes_Back_And_None_After() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        page.Drag(8 + 108).Wait(16);
        Assert.Equal(1, IconsSeen(page));
        page.Lift();
        for (var i = 0; i < 10; i++)
        {
            page.Wait(16);
            Assert.True(IconsSeen(page) <= 1);
            Assert.False(page.Shows("PART_Refreshing"));
        }

        page.Wait(100);
        Assert.Contains("ionPullEnd:Cancel", page.Names);
        Assert.Equal(0, IconsSeen(page));
    });

    [AvaloniaFact]
    public void C5_Cancel_With_No_Pull_Shows_Only_The_Refreshing_Icon_Where_Chromium_Does() => InstalledTheme.Run(_ =>
    {
        // The pulling icon is at its resting translateY(-52), above the content, under the header in Chromium
        using var page = new Page();
        page.Wait(0);
        page.Refresher.Cancel();
        page.Wait(32);
        Assert.Equal(new Rect(0, -8, 390, 42), Box(page, "PART_PullingIcon"));
        Assert.Equal(new Rect(174, 86, 42, 42), Box(page, "PART_RefreshingIcon"));
        Assert.Equal(1, IconsSeen(page));

        page.Wait(700);
        Assert.Equal(0, IconsSeen(page));
    });

    [AvaloniaFact]
    public void C6_After_A_Cancel_A_Refresh_Shows_One_Icon_And_None_Once_Complete() => InstalledTheme.Run(_ =>
    {
        using var page = Refreshing();
        page.Refresher.Cancel();
        page.Wait(32 + 700);
        Assert.Equal(0, IconsSeen(page));

        page.Drag(8 + 200).Wait(16);
        Assert.Equal(1, IconsSeen(page));
        page.Lift().Wait(160);
        Assert.Equal(2, page.Names.Count(n => n == "ionRefresh"));
        Assert.Equal(1, IconsSeen(page));
        Assert.Equal(new Rect(174, 65, 42, 42), Box(page, "PART_RefreshingIcon"));

        page.LastRefresh!.Complete();
        page.Wait(32 + 200 + 16);
        Assert.Equal("ionPullEnd:Complete", page.Names.Last());
        Assert.Equal(0, IconsSeen(page));
    });

    [AvaloniaFact]
    public void C7_A_Page_Closed_While_Cancelling_And_Opened_Again_Shows_What_The_State_Does() => InstalledTheme.Run(_ =>
    {
        // Opened again after the cancel's end: nothing, and a pull shows one icon
        using (var page = Refreshing())
        {
            page.Refresher.Cancel();
            page.Wait(32 + 100);
            var grid = (Grid)page.Content.Parent!;
            grid.Children.Remove(page.Content);
            page.Wait(700);
            grid.Children.Add(page.Content);
            page.Wait(16);
            Assert.Equal(0, IconsSeen(page));
            page.Drag(8 + 180).Wait(16);
            Assert.Equal(1, IconsSeen(page));
        }

        // Opened again before it: still cancelling, so both, then none
        using (var page = Refreshing())
        {
            page.Refresher.Cancel();
            page.Wait(32 + 100);
            var grid = (Grid)page.Content.Parent!;
            grid.Children.Remove(page.Content);
            page.Wait(100);
            grid.Children.Add(page.Content);
            page.Wait(16);
            Assert.Equal(2, IconsSeen(page));
            page.Wait(600);
            Assert.Equal(0, IconsSeen(page));
            Assert.Contains("ionPullEnd:Cancel", page.Names);
        }
    });

    [AvaloniaFact]
    public void C8_The_Functional_Template_Also_Stretches_The_Pulling_Icon_In_Cancelling_With_Its_Spinner_At_The_Start()
    {
        // No theme: Avonic.Components' own template, which has the structure and no sizes
        using var page = Refreshing();
        page.Refresher.Cancel();
        page.Wait(32);
        var icon = Box(page, "PART_PullingIcon");
        var container = Box(page, "PART_SpinnerArrowContainer");
        Assert.Equal(page.Refresher.Bounds.Width, icon.Width);
        Assert.Equal(icon.X, container.X);
        Assert.True(container.Width < icon.Width, $"{container.Width} of {icon.Width}");
    }

    [AvaloniaFact]
    public void C9_A_Refresh_Begun_While_Cancelling_Goes_On_Past_The_Cancels_End() => InstalledTheme.Run(_ =>
    {
        // A deliberate difference. Measured in Chromium (ref/measure-cancel.json, H): cancel() at 17132, a pull let go at 17418,
        // ionRefresh at 17548, then the cancel's reset at 17865 ends it as cancelled while the app is still refreshing. Here the
        // reset leaves a refresh begun after it alone, and that refresh's own complete() ends it.
        using var page = Refreshing();
        page.Refresher.Cancel();
        var cancelled = page.At;
        page.Wait(32 + 60);
        page.Drag(8 + 200, step: 50).Wait(16).Lift().Wait(160);
        Assert.Equal(2, page.Names.Count(n => n == "ionRefresh"));
        Assert.Equal(RefresherState.Refreshing, page.Refresher.State);
        Assert.Equal(1, IconsSeen(page));

        page.Wait(cancelled + 32 + 700 - page.At + 100);
        Assert.DoesNotContain(page.Log, e => e.Name.StartsWith("ionPullEnd"));
        Assert.Equal(RefresherState.Refreshing, page.Refresher.State);
        Assert.Equal(1, IconsSeen(page));
        Assert.Equal(new Rect(174, 65, 42, 42), Box(page, "PART_RefreshingIcon"));

        page.LastRefresh!.Complete();
        page.Wait(32 + 200 + 16);
        var end = Assert.Single(page.Log, e => e.Name.StartsWith("ionPullEnd"));
        Assert.Equal("ionPullEnd:Complete", end.Name);
        Assert.Equal(RefresherState.Inactive, page.Refresher.State);
        Assert.Equal(0, IconsSeen(page));

        page.Drag(8 + 200).Wait(16).Lift().Wait(160);
        Assert.Equal(3, page.Names.Count(n => n == "ionRefresh"));
    });

    [AvaloniaFact]
    public void C9_A_Cancel_With_No_Refresh_Since_Still_Ends_It() => InstalledTheme.Run(_ =>
    {
        // The fix only skips a reset that a refresh has begun since: a plain cancel still ends as cancelled, as B8 has it.
        using var page = Refreshing();
        page.Refresher.Cancel();
        page.Wait(32 + 700);
        Assert.Equal("ionPullEnd:Cancel", Assert.Single(page.Log, e => e.Name.StartsWith("ionPullEnd")).Name);
        Assert.Equal(RefresherState.Inactive, page.Refresher.State);
    });
}
