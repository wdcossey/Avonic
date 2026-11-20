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
        NavSampleView.WillEnter = _ => _readyMs = _timer.Elapsed.TotalMilliseconds;
        Nav.IonNavDidChange += (_, _) => UpdateStatus();

        PopToRootButton.Click += (_, _) => _ = Nav.PopToRoot();
        PushDraftButton.Click += (_, _) => _ = Nav.Push(() => new NavDraftView());
        AnimatedToggle.IonChange += (_, e) => Nav.Animated = e.Checked;
    }

    private void UpdateStatus()
    {
        var names = Enumerable.Range(0, Nav.GetLength())
            .Select(i => Nav.GetByIndex(i)?.Element is { } page ? (page as NavSampleView)?.Title ?? page.GetType().Name : "(not built)");
        NavStatus.Text = $"Stack: {string.Join(" > ", names)} ({Nav.GetLength()}). Done: ready at {_readyMs:0} ms, all {_timer.Elapsed.TotalMilliseconds:0} ms";
    }
}
