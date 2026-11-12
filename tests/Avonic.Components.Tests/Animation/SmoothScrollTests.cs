using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Avonic.Components.Animation;

namespace Avonic.Components.Tests.Animation;

/// <summary>
/// <see cref="SmoothScroll"/>, Chromium's programmatic smooth scroll, along each axis: √distance / 60 s, eased in and out. The
/// segment's and the picker's tests pin it through their controls; these pin the shared class itself.
/// </summary>
public class SmoothScrollTests
{
    private static ScrollViewer Scroller()
    {
        var scroller = new ScrollViewer
        {
            Theme = (ControlTheme)Application.Current!.FindResource("AvonicContentScrollViewer")!,
            Width = 200,
            Height = 200,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = new Border { Width = 1200, Height = 1200 },
        };
        return scroller;
    }

    private static void Run(TestClock clock, double ms)
    {
        clock.At(ms);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void Vertical_Scrolls_Top_Eased_Over_The_Square_Root_Of_The_Distance_And_Leaves_Left()
    {
        using var clock = new TestClock();
        var scroller = Scroller();
        using var scene = new Scene(scroller);
        scroller.Offset = new Vector(40, 100);
        var smooth = new SmoothScroll(scroller, Orientation.Vertical);

        var start = clock.Now.TotalMilliseconds;
        smooth.To(500, smooth: true);
        Assert.True(smooth.IsRunning);
        Assert.Equal(new Vector(40, 100), scroller.Offset);

        // 400 px down in √400 / 60 s = 333 ms
        var duration = Math.Sqrt(400) / 60 * 1000;
        Run(clock, start + 16 * 10);
        Assert.Equal(100 + 400 * SmoothScroll.Easing.Ease(16 * 10 / duration), scroller.Offset.Y, 3);
        Assert.Equal(40, scroller.Offset.X);

        Run(clock, start + duration + 16);
        Assert.Equal(new Vector(40, 500), scroller.Offset);
        Assert.False(smooth.IsRunning);
    }

    [AvaloniaFact]
    public void Vertical_Clamps_To_The_Bottom_And_Jumps_When_Not_Smooth()
    {
        using var clock = new TestClock();
        var scroller = Scroller();
        using var scene = new Scene(scroller);
        var smooth = new SmoothScroll(scroller, Orientation.Vertical);

        smooth.To(5000, smooth: false);
        Assert.Equal(new Vector(0, 1000), scroller.Offset);
        Assert.False(smooth.IsRunning);
    }

    [AvaloniaFact]
    public void Horizontal_Is_The_Default_And_Leaves_Top()
    {
        using var clock = new TestClock();
        var scroller = Scroller();
        using var scene = new Scene(scroller);
        scroller.Offset = new Vector(0, 70);
        var smooth = new SmoothScroll(scroller);

        var start = clock.Now.TotalMilliseconds;
        smooth.To(390, smooth: true);
        var duration = Math.Sqrt(390) / 60 * 1000;
        Run(clock, start + 16 * 5);
        Assert.Equal(390 * SmoothScroll.Easing.Ease(16 * 5 / duration), scroller.Offset.X, 3);
        Assert.Equal(70, scroller.Offset.Y);
        Run(clock, start + duration + 16);
        Assert.Equal(new Vector(390, 70), scroller.Offset);
    }

    [AvaloniaFact]
    public void Vertical_Stops_When_Something_Else_Scrolls_It()
    {
        using var clock = new TestClock();
        var scroller = Scroller();
        using var scene = new Scene(scroller);
        var smooth = new SmoothScroll(scroller, Orientation.Vertical);

        var start = clock.Now.TotalMilliseconds;
        smooth.To(600, smooth: true);
        Run(clock, start + 32);
        scroller.Offset = new Vector(0, 20);
        Run(clock, start + 48);
        Assert.False(smooth.IsRunning);
        Run(clock, start + 1000);
        Assert.Equal(new Vector(0, 20), scroller.Offset);
    }
}
