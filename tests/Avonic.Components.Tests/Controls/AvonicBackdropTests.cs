using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-backdrop</c>'s tap, with real headless input. The cases are M1 to M6 in docs/components/ion-backdrop/PLAN.md: a mouse
/// click of any length, a finger up to 15 px, the left button only, and what <c>stopPropagation</c> stops.
/// </summary>
public class AvonicBackdropTests
{
    private sealed class Fixture : IDisposable
    {
        public Fixture(AvonicBackdrop? backdrop = null)
        {
            Backdrop = backdrop ?? new AvonicBackdrop();
            Under = new Button { Content = "Under", HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            Parent = new Panel { Width = 300, Height = 200, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { Under, Backdrop } };
            Scene = new Scene(Parent);
            Backdrop.IonBackdropTap += (_, e) =>
            {
                Assert.Same(Backdrop, e.Source);
                Taps++;
            };
            Under.Click += (_, _) => UnderClicks++;
            Parent.Tapped += (_, _) => ParentTaps++;
        }

        public AvonicBackdrop Backdrop { get; }

        public Button Under { get; }

        public Panel Parent { get; }

        public Scene Scene { get; }

        public int Taps { get; private set; }

        public int UnderClicks { get; private set; }

        public int ParentTaps { get; private set; }

        public void Render()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        public void Click(Point at, MouseButton button = MouseButton.Left, Point? to = null)
        {
            Render();
            Scene.Window.MouseDown(at, button);
            if (to is { } end)
            {
                Scene.Window.MouseMove(end);
            }

            Scene.Window.MouseUp(to ?? at, button);
            Dispatcher.UIThread.RunJobs();
        }

        public void Touch(Point at, double moveX = 0)
        {
            Render();
            var finger = Scene.Window.TouchBegin(at, RawInputModifiers.None);
            var end = new Point(at.X + moveX, at.Y);
            if (moveX != 0)
            {
                Scene.Window.TouchMove(finger, end, RawInputModifiers.None);
            }

            Scene.Window.TouchEnd(finger, end, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose() => Scene.Dispose();
    }

    [AvaloniaFact]
    public void B1_A_Mouse_Click_Of_Any_Length_Raises_IonBackdropTap_And_A_Right_Click_Does_Not()
    {
        using var f = new Fixture();
        f.Click(new Point(100, 100));
        Assert.Equal(1, f.Taps);

        f.Click(new Point(20, 20), to: new Point(280, 180));
        Assert.Equal(2, f.Taps);

        f.Click(new Point(100, 100), MouseButton.Right);
        Assert.Equal(2, f.Taps);
    }

    [AvaloniaFact]
    public void B2_A_Finger_Taps_Up_To_15_Px_And_Not_From_16()
    {
        using var f = new Fixture();
        f.Touch(new Point(100, 100));
        f.Touch(new Point(100, 100), moveX: 14);
        Assert.Equal(2, f.Taps);

        f.Touch(new Point(100, 100), moveX: 16);
        Assert.Equal(2, f.Taps);
    }

    [AvaloniaFact]
    public void B3_Not_Tappable_Raises_Nothing_And_Still_Covers_What_Is_Under_It()
    {
        using var f = new Fixture(new AvonicBackdrop { Tappable = false });
        f.Click(new Point(100, 100));

        Assert.Equal(0, f.Taps);
        Assert.Equal(0, f.UnderClicks);
    }

    [AvaloniaFact]
    public void B4_StopPropagation_Stops_The_Tap_At_The_Backdrop()
    {
        using var f = new Fixture();
        f.Click(new Point(100, 100));
        Assert.Equal(1, f.Taps);
        Assert.Equal(0, f.ParentTaps);

        // Far from the first, which would make a double tap, and no Tapped.
        f.Backdrop.StopPropagation = false;
        f.Click(new Point(250, 170));
        Assert.Equal(2, f.Taps);
        Assert.Equal(1, f.ParentTaps);
    }

    [AvaloniaFact]
    public void B4_StopPropagation_Stops_A_Button_Around_It_From_Clicking()
    {
        var backdrop = new AvonicBackdrop { Width = 100, Height = 40 };
        var button = new AvonicButton { Content = backdrop, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var clicks = 0;
        var taps = 0;
        button.Click += (_, _) => clicks++;
        backdrop.IonBackdropTap += (_, _) => taps++;
        using var scene = new Scene(button);

        void Click()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var centre = backdrop.TranslatePoint(new Point(50, 20), scene.Window)!.Value;
            scene.Window.MouseDown(centre, MouseButton.Left);
            scene.Window.MouseUp(centre, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        Click();
        Assert.Equal((1, 0), (taps, clicks));

        backdrop.StopPropagation = false;
        Click();
        Assert.Equal((2, 1), (taps, clicks));
    }

    [AvaloniaFact]
    public void B5_IonBackdropTap_Bubbles()
    {
        using var f = new Fixture();
        var seen = 0;
        f.Parent.AddHandler(AvonicBackdrop.IonBackdropTapEvent, (_, _) => seen++);
        f.Click(new Point(100, 100));
        Assert.Equal(1, seen);
    }

    [AvaloniaFact]
    public void B6_The_Md_Look_Is_Nearly_Invisible_Black_Over_What_Is_Under_It_With_A_Hand_Cursor() => InstalledTheme.Run(_ =>
    {
        using var f = new Fixture();
        f.Render();

        Assert.Equal(0.01, f.Backdrop.Opacity);
        Assert.Equal(Colors.Black, ((ISolidColorBrush)f.Backdrop.Background!).Color);
        Assert.Equal(2, f.Backdrop.ZIndex);
        Assert.Equal(new Cursor(StandardCursorType.Hand).ToString(), f.Backdrop.Cursor?.ToString());
        Assert.Equal(new Rect(0, 0, 300, 200), f.Backdrop.Bounds);
    });

    [AvaloniaFact]
    public void B6_Not_Visible_Is_Transparent_And_Still_Takes_The_Tap() => InstalledTheme.Run(_ =>
    {
        using var f = new Fixture(new AvonicBackdrop { Visible = false });
        f.Render();
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)f.Backdrop.Background!).Color);

        f.Click(new Point(100, 100));
        Assert.Equal(1, f.Taps);
        Assert.Equal(0, f.UnderClicks);

        f.Backdrop.Visible = true;
        Assert.Equal(Colors.Black, ((ISolidColorBrush)f.Backdrop.Background!).Color);
    });

    [AvaloniaFact]
    public void B6_Not_Tappable_Has_The_Default_Cursor() => InstalledTheme.Run(_ =>
    {
        using var f = new Fixture(new AvonicBackdrop { Tappable = false });
        f.Render();
        Assert.Equal(new Cursor(StandardCursorType.Arrow).ToString(), f.Backdrop.Cursor?.ToString());
    });

    [AvaloniaFact]
    public void B7_It_Is_Hidden_From_Assistive_Technology()
    {
        using var f = new Fixture();
        var peer = ControlAutomationPeer.CreatePeerForElement(f.Backdrop);
        Assert.False(peer.IsControlElement());
        Assert.False(peer.IsContentElement());
    }

    [AvaloniaFact]
    public void B8_It_Fills_Its_Parent()
    {
        using var f = new Fixture();
        f.Render();
        Assert.Equal(new Rect(0, 0, 300, 200), f.Backdrop.Bounds);
    }
}
