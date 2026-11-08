using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Gestures;
using static Avonic.Components.Tests.Gestures.GestureTests;

namespace Avonic.Components.Tests.Gestures;

/// <summary>
/// A gesture inside a scroller, and a scroller inside a gesture. The cases are what real Ionic did in Chromium, under "Measured: a
/// gesture and ion-content's scroll in Chromium" in docs/infra/gesture/PLAN.md: a finger in 8 px steps, 16 ms apart.
/// </summary>
public class GestureScrollTests
{
    /// <summary>A row 60 high at 300, in a content 2,300 high, in a window 844 high.</summary>
    private sealed class Page : IDisposable
    {
        public Page(GestureClock clock, bool simpleScrollViewer = false, bool rowOutside = false)
        {
            Clock = clock;
            Row = new Border { Height = 60, Background = Brushes.Transparent };
            var column = new StackPanel { Children = { new Border { Height = 300 }, Row, new Border { Height = 2000 } } };
            if (simpleScrollViewer)
            {
                Viewer = new ScrollViewer { Content = column, IsScrollInertiaEnabled = false };
                Root = Viewer;
            }
            else
            {
                var content = new AvonicContent { Content = column };
                Root = content;
            }

            if (rowOutside)
            {
                Outer = new Border { Background = Brushes.Transparent, Child = Root };
                Root = Outer;
            }

            Scene = new Scene(Root);
            Viewer ??= Root.GetVisualDescendants().OfType<ScrollViewer>().First();
            Viewer.IsScrollInertiaEnabled = false;
            Log = new Log(clock);
        }

        public GestureClock Clock { get; }

        public Border Row { get; }

        public Border? Outer { get; }

        public Control Root { get; }

        public ScrollViewer Viewer { get; }

        public Scene Scene { get; }

        public Log Log { get; }

        public double ScrollTop => Viewer.Offset.Y;

        /// <summary>
        /// A drag from (200, 330), or on from where the last one ended, by (dx, dy): steps of <paramref name="step"/> px along it, 16 ms
        /// apart, and what's left as the last step.
        /// </summary>
        public Finger Drag(double dx, double dy, double step = 8, Finger? finger = null, double from = 1000)
        {
            var at = from;
            if (finger is null)
            {
                finger = new Finger(Scene, Clock).Down(200, 330, at);
                _x = 200;
                _y = 330;
            }

            var length = Math.Sqrt(dx * dx + dy * dy);
            var (x0, y0) = (_x, _y);
            for (var done = step; ; done += step)
            {
                var part = Math.Min(done, length) / length;
                at += 16;
                _x = x0 + dx * part;
                _y = y0 + dy * part;
                finger.Move(_x, _y, at);
                if (done >= length)
                {
                    break;
                }
            }

            _at = at;
            return finger;
        }

        public void Lift(Finger finger) => finger.Up(_x, _y, _at += 16);

        public double At => _at;

        private double _x;
        private double _y;
        private double _at;

        public void Dispose() => Scene.Dispose();
    }

    [AvaloniaFact]
    public void C1_A_Sideways_Drag_Pans_And_Does_Not_Scroll()
    {
        using var clock = new GestureClock();
        using var page = new Page(clock);
        using var gesture = Make(page.Row, page.Log, threshold: 5);

        page.Lift(page.Drag(-80, 0));

        Assert.Equal(["g.canStart", "g.onStart", "g.onMove", "g.onEnd"], page.Log.Names.Distinct());
        Assert.Equal(0, page.ScrollTop);
    }

    [AvaloniaFact]
    public void C2_An_Upward_Drag_Scrolls_And_The_Gesture_Is_NotCaptured_At_Its_Threshold()
    {
        using var clock = new GestureClock();
        using var page = new Page(clock);
        using var gesture = Make(page.Row, page.Log);

        page.Lift(page.Drag(0, -150));

        Assert.Equal(["g.canStart", "g.notCaptured"], page.Log.Names);
        Assert.Equal(-16, page.Log.Single("g.notCaptured").DeltaY);
        // From the press, less Avalonia's 5 px start distance. Chromium's slop is about 15 px, so Ionic's page is at 135.
        Assert.Equal(145, page.ScrollTop);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void C3_C9_Sideways_Then_Up_Never_Scrolls_And_The_Gesture_Follows(bool simpleScrollViewer)
    {
        using var clock = new GestureClock();
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        if (simpleScrollViewer)
        {
            styles.Insert(0, baseTheme);
        }

        try
        {
            using var page = new Page(clock, simpleScrollViewer);
            using var gesture = Make(page.Row, page.Log);

            var finger = page.Drag(-60, 0);
            page.Drag(0, -200, finger: finger, from: page.At);
            clock.At(page.At + 16);

            Assert.Equal(0, page.ScrollTop);
            Assert.Equal(-200, page.Log.All.Last(seen => seen.Name == "g.onMove").DeltaY);
            page.Lift(finger);
            Assert.Equal("g.onEnd", page.Log.All[^1].Name);
            Assert.Equal(0, page.ScrollTop);
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    [AvaloniaFact]
    public void C4_Up_Then_Sideways_Scrolls_And_The_Gesture_Ignores_The_Rest()
    {
        using var clock = new GestureClock();
        using var page = new Page(clock);
        using var gesture = Make(page.Row, page.Log, threshold: 5);

        var finger = page.Drag(0, -60);
        page.Drag(-200, 0, finger: finger, from: page.At);
        page.Lift(finger);

        Assert.Equal(["g.canStart", "g.notCaptured"], page.Log.Names);
        // Ionic's page is at 45, from Chromium's slop.
        Assert.Equal(55, page.ScrollTop);
    }

    [AvaloniaTheory]
    [InlineData(30, 20)]
    [InlineData(39, 10)]
    public void C5_A_Drag_Within_MaxAngle_Pans_And_Does_Not_Scroll_Even_After_The_Scroller_Would_Have_Started(double degrees, double threshold)
    {
        // In 2 px steps, the vertical movement passes Avalonia's 5 px before the gesture's threshold.
        using var clock = new GestureClock();
        using var page = new Page(clock);
        using var gesture = Make(page.Row, page.Log, threshold: threshold);

        var angle = degrees * Math.PI / 180;
        page.Lift(page.Drag(-120 * Math.Cos(angle), -120 * Math.Sin(angle), step: 2));

        Assert.Equal(["g.canStart", "g.onStart", "g.onMove", "g.onEnd"], page.Log.Names.Distinct());
        Assert.Equal(0, page.ScrollTop);
    }

    [AvaloniaFact]
    public void C6_A_Drag_Past_MaxAngle_Scrolls()
    {
        using var clock = new GestureClock();
        using var page = new Page(clock);
        using var gesture = Make(page.Row, page.Log, threshold: 5);

        var angle = 50 * Math.PI / 180;
        page.Lift(page.Drag(-120 * Math.Cos(angle), -120 * Math.Sin(angle)));

        Assert.Equal(["g.canStart", "g.notCaptured"], page.Log.Names);
        Assert.True(page.ScrollTop > 80, $"{page.ScrollTop}");
    }

    [AvaloniaFact]
    public void C10_A_Captured_Y_Gesture_In_A_Y_Scroller_Has_The_Pointer_To_Itself()
    {
        // A deliberate difference: Ionic's passive gesture and Chromium's scroll both run. Avalonia gives a pointer to one owner.
        using var clock = new GestureClock();
        using var page = new Page(clock);
        using var gesture = Make(page.Row, page.Log, direction: GestureDirection.Y);

        var finger = page.Drag(0, -150);
        clock.At(page.At + 16);

        Assert.Equal(-134, page.Log.All.Last(seen => seen.Name == "g.onMove").DeltaY);
        Assert.Equal(0, page.ScrollTop);
        page.Lift(finger);
    }

    [AvaloniaFact]
    public void A_Gesture_That_Does_Not_Start_Leaves_The_Scroller_Alone()
    {
        using var clock = new GestureClock();
        using var page = new Page(clock);
        using var gesture = Make(page.Row, page.Log, threshold: 20, canStart: _ => false);

        page.Lift(page.Drag(0, -150));

        Assert.Equal(["g.canStart"], page.Log.Names);
        Assert.Equal(145, page.ScrollTop);
    }

    [AvaloniaFact]
    public void A_Scroller_Inside_The_Element_Waits_For_The_Gesture_From_The_First_Move()
    {
        // ion-menu's gesture is on the document, around the content.
        using var clock = new GestureClock();
        using var page = new Page(clock, rowOutside: true);
        using var gesture = Make(page.Outer!, page.Log);

        // 6 px down and 4 across: the scroller would start at once, and the gesture hasn't decided.
        var finger = new Finger(page.Scene, clock).Down(200, 330, 1000).Move(196, 324, 1016).Move(186, 324, 1032).Move(150, 320, 1048);
        finger.Up(150, 320, 1064);

        Assert.Equal(["g.canStart", "g.onStart", "g.onMove", "g.onEnd"], page.Log.Names);
        Assert.Equal(0, page.ScrollTop);
    }

    [AvaloniaFact]
    public void Two_Gestures_Deciding_Hold_The_Scroller_Until_Both_Give_Up()
    {
        using var clock = new GestureClock();
        using var page = new Page(clock);
        var inner = new Border { Background = Brushes.Transparent, Height = 60 };
        page.Row.Child = inner;
        using var outer = Make(page.Row, page.Log, name: "outer", threshold: 30);
        using var first = Make(inner, page.Log, name: "inner", threshold: 10);

        var finger = page.Drag(0, -16);
        Assert.Equal(["inner.canStart", "outer.canStart", "inner.notCaptured"], page.Log.Names);
        Assert.Equal(0, page.ScrollTop);

        page.Drag(0, -24, finger: finger, from: page.At);
        Assert.Equal(["inner.canStart", "outer.canStart", "inner.notCaptured", "outer.notCaptured"], page.Log.Names);
        page.Lift(finger);
        Assert.True(page.ScrollTop > 30, $"{page.ScrollTop}");
    }

    [AvaloniaFact]
    public void A_Captured_Gesture_Keeps_The_Scroller_Off_After_A_Gesture_Around_It_Gives_Up()
    {
        using var clock = new GestureClock();
        using var page = new Page(clock);
        var inner = new Border { Background = Brushes.Transparent, Height = 60 };
        page.Row.Child = inner;
        using var outer = Make(page.Row, page.Log, name: "outer", threshold: 40);
        using var first = Make(inner, page.Log, name: "inner", threshold: 10);

        // The inner one captures at 16 px; the outer one is refused at 40, and the drag goes on upwards.
        var finger = page.Drag(-48, 0);
        page.Drag(0, -200, finger: finger, from: page.At);
        page.Lift(finger);

        Assert.Contains("outer.notCaptured", page.Log.Names);
        Assert.Equal("inner.onEnd", page.Log.All[^1].Name);
        Assert.Equal(0, page.ScrollTop);
    }

    [AvaloniaFact]
    public void A_Gesture_Captured_At_The_Press_Keeps_The_Scroller_Around_It_Off_The_Press()
    {
        // ion-reorder-group's gesture: threshold 0, along the scroller's own axis.
        using var clock = new GestureClock();
        using var page = new Page(clock);
        using var gesture = Make(page.Row, page.Log, direction: GestureDirection.Y, threshold: 0);

        var finger = page.Drag(0, -150);
        clock.At(page.At + 16);
        page.Lift(finger);

        Assert.Equal(-150, page.Log.All.Last(seen => seen.Name == "g.onMove").DeltaY);
        Assert.Equal("g.onEnd", page.Log.All[^1].Name);
        Assert.Equal(0, page.ScrollTop);
    }

    [AvaloniaFact]
    public void A_Gesture_That_Gives_Up_Leaves_An_Apps_Own_PreventGestureRecognition_Alone()
    {
        using var clock = new GestureClock();
        using var page = new Page(clock);
        using var gesture = Make(page.Row, page.Log);
        // After the scroller has seen the press, the app keeps recognisers off this pointer.
        page.Root.AddHandler(InputElement.PointerPressedEvent, (_, e) => e.PreventGestureRecognition(), handledEventsToo: true);

        page.Lift(page.Drag(0, -150));

        Assert.Equal(["g.canStart", "g.notCaptured"], page.Log.Names);
        Assert.Equal(0, page.ScrollTop);
    }
}
