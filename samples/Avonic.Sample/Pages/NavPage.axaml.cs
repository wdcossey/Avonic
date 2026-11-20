using System.Diagnostics;
using Avalonia.Controls;
using Avonic.Sample.Pages.Nav;

namespace Avonic.Sample.Pages;

[SamplePage("ion-nav", Order = 130)]
public partial class NavPage : UserControl
{
    private readonly Stopwatch _timer = new();
    private double _readyMs;

    public NavPage()
    {
        InitializeComponent();

        // How long each transition takes: from its start to the entering page's will-enter (built, laid out, ready to move),
        // then to its end.
        Nav.IonNavWillChange += (_, _) =>
        {
            _timer.Restart();
            _readyMs = 0;
        };
        Nav.IonNavDidChange += (_, _) => UpdateStatus();

        PopToRootButton.Click += (_, _) => _ = Nav.PopToRoot();
        PushDraftButton.Click += (_, _) => _ = Nav.Push(() => new NavDraftView());
        AnimatedToggle.IonChange += (_, e) => Nav.Animated = e.Checked;
        SwipeToggle.IonChange += (_, e) => Nav.NavSwipeGesture = e.Checked;
    }

    // The views report their will-enter through a static hook, held only while this page is shown, so a closed page isn't kept.
    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        NavSampleView.WillEnter = OnWillEnter;
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (NavSampleView.WillEnter == OnWillEnter)
        {
            NavSampleView.WillEnter = null;
        }
    }

    private void OnWillEnter(NavSampleView view) => _readyMs = _timer.Elapsed.TotalMilliseconds;

    private void UpdateStatus()
    {
        var names = Enumerable.Range(0, Nav.GetLength())
            .Select(i => Nav.GetByIndex(i)?.Element is { } page ? (page as NavSampleView)?.Title ?? page.GetType().Name : "(not built)");
        NavStatus.Text = $"Stack: {string.Join(" > ", names)} ({Nav.GetLength()}). Done: ready at {_readyMs:0} ms, all {_timer.Elapsed.TotalMilliseconds:0} ms";
    }
}
