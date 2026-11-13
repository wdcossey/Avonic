using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Overlays;
using Avonic.Sample;
using Avonic.Sample.Diagnostics;
using Avonic.Sample.Pages;

namespace Avonic.Components.Tests.Sample;

/// <summary>
/// The sample's shell, which a phone shows as its main view (#66): Android's back, which Avalonia raises as
/// <see cref="TopLevel.BackRequestedEvent"/>, takes a page back to the list once nothing else wants it, and the header pads itself by
/// the safe area's keys. See docs/infra/android/PLAN.md.
/// </summary>
public class SampleShellTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>The shell in a window with <c>SimpleTheme</c>, whose template has the overlay layer, showing its first page.</summary>
    private sealed class Stage : IDisposable
    {
        private readonly Avalonia.Themes.Simple.SimpleTheme _theme = new();

        public Stage()
        {
            Application.Current!.Styles.Insert(0, _theme);
            Shell = new SampleShell();
            Scene = new Scene(Shell);
        }

        public SampleShell Shell { get; }

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public ScrollViewer Index => Assert.IsType<ScrollViewer>(Shell.FindControl<ContentControl>("Host")!.Content);

        public void Open(Func<Button, bool> which)
        {
            ((StackPanel)Index.Content!).Children.OfType<Button>().First(which).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.True(Shell.IsShowingPage);
        }

        public void OpenAPage() => Open(_ => true);

        /// <summary>What Avalonia does for the platform's back: raises it on the <see cref="TopLevel"/>, and reads <c>Handled</c>.</summary>
        public bool Back(Interactive? from = null)
        {
            var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
            (from ?? Window).RaiseEvent(e);
            Settle();
            return e.Handled;
        }

        public void Dispose()
        {
            Scene.Dispose();
            Application.Current!.Styles.Remove(_theme);
        }
    }

    // A Debug build's monitor hears every measurement in the process, so a shell gone from its window mustn't leave one.
    [AvaloniaFact]
    public void A_Shell_That_Has_Left_Its_Window_Stops_Listening()
    {
        var before = PerformanceMonitor.Listening;
        using (var stage = new Stage())
        {
            stage.OpenAPage();
#if DEBUG
            Assert.Equal(before + 1, PerformanceMonitor.Listening);
#endif
        }

        Assert.Equal(before, PerformanceMonitor.Listening);
    }

    [AvaloniaFact]
    public void Back_From_A_Page_Shows_The_List_And_Is_Handled()
    {
        using var stage = new Stage();
        stage.OpenAPage();

        Assert.True(stage.Back());
        Assert.False(stage.Shell.IsShowingPage);
        Assert.Same(stage.Index, stage.Shell.FindControl<ContentControl>("Host")!.Content);
    }

    // Unhandled, Android goes back from the activity, and leaves the app.
    [AvaloniaFact]
    public void Back_On_The_List_Is_Left_Unhandled()
    {
        using var stage = new Stage();

        Assert.False(stage.Back());
        Assert.False(stage.Shell.IsShowingPage);
    }

    // Alt+Left is Avalonia's back on desktop, through the same event as Android's back button.
    [AvaloniaFact]
    public void The_Platforms_Own_Back_Key_Goes_Back()
    {
        using var stage = new Stage();
        stage.OpenAPage();

        stage.Window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        stage.Window.KeyReleaseQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        Settle();

        Assert.False(stage.Shell.IsShowingPage);
    }

    // An overlay's handler is added when the first one is presented, after the shell's, so the shell has to come after it anyway.
    [AvaloniaFact]
    public void A_Back_That_A_Later_Handler_Took_Keeps_The_Page()
    {
        using var stage = new Stage();
        stage.OpenAPage();
        stage.Window.AddHandler(TopLevel.BackRequestedEvent, (_, e) => e.Handled = true);

        Assert.True(stage.Back());
        Assert.True(stage.Shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void A_Back_From_Inside_The_Window_Goes_Back_Too()
    {
        using var stage = new Stage();
        stage.OpenAPage();

        Assert.True(stage.Back(from: stage.Shell.FindControl<TextBlock>("PageTitle")));
        Assert.False(stage.Shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void A_Back_In_Another_Window_Is_Left_Alone()
    {
        using var stage = new Stage();
        stage.OpenAPage();
        using var other = new Scene(new Panel());

        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        other.Window.RaiseEvent(e);
        Settle();

        Assert.False(e.Handled);
        Assert.True(stage.Shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void Back_Dismisses_An_Overlay_First_And_The_Next_Back_Shows_The_List()
    {
        using var stage = new Stage();
        using var clock = new TestClock();
        stage.OpenAPage();
        var overlay = new TestOverlay();
        var dismissed = false;
        overlay.DidDismiss += (_, _) => dismissed = true;
        overlay.Present(stage.Window);
        Run(clock, 200);

        Assert.True(stage.Back());
        Run(clock, 400);
        Assert.True(dismissed);
        Assert.True(stage.Shell.IsShowingPage);

        Assert.True(stage.Back());
        Assert.False(stage.Shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void A_Detached_Shell_Stops_Listening()
    {
        using var stage = new Stage();
        stage.OpenAPage();
        stage.Window.Content = null;
        Settle();

        Assert.False(stage.Back());
        Assert.True(stage.Shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void The_Header_And_The_List_Pad_Themselves_By_The_Safe_Area_And_The_Bottom_Line_By_Its_Bottom()
    {
        using var stage = new Stage();
        var header = stage.Shell.FindControl<Border>("Header")!;
        var line = stage.Shell.FindControl<TextBlock>("PerformanceLine")!;
        Assert.Equal(default, header.Padding);
        Assert.Equal(56, header.Bounds.Height);
        Assert.Equal(new Thickness(8, 2), line.Margin);

        var resources = Application.Current!.Resources;
        SafeAreaMode.Fill(resources, new Thickness(10, 30, 20, 40));
        try
        {
            Settle();
            Assert.Equal(new Thickness(10, 0, 20, 40), stage.Index.Padding);
            Assert.Equal(new Thickness(10, 30, 20, 0), header.Padding);
            Assert.Equal(56 + 30, header.Bounds.Height);
            Assert.Equal(new Thickness(18, 2, 28, 42), line.Margin);
        }
        finally
        {
            SafeAreaMode.Clear(resources);
        }

        Settle();
        Assert.Equal(default, header.Padding);
        Assert.Equal(default, stage.Index.Padding);
        Assert.Equal(new Thickness(8, 2), line.Margin);
    }

    // A phone can be shorter than the list.
    [AvaloniaFact]
    public void The_List_Scrolls()
    {
        using var stage = new Stage();
        stage.Window.Height = 200;
        Settle();

        Assert.True(stage.Index.Extent.Height > stage.Index.Viewport.Height);
    }

    [AvaloniaFact]
    public void Filling_The_Safe_Area_Reaches_An_Item_And_Clearing_It_Puts_The_Themes_Zero_Back() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = "x" };
        using var scene = new Scene(item);
        FlexRow Layout() => item.GetVisualDescendants().OfType<FlexRow>().Single(part => part.Name == "PART_Layout");

        var resources = Application.Current!.Resources;
        SafeAreaMode.Fill(resources, new Thickness(10, 0, 0, 0));
        try
        {
            Settle();
            Assert.Equal(26, Layout().Margin.Left);
        }
        finally
        {
            SafeAreaMode.Clear(resources);
        }

        Settle();
        Assert.Equal(16, Layout().Margin.Left);
    });

    // A desktop has no insets, so Ionic's way can't be turned on, and the main view keeps Avalonia's padding.
    [AvaloniaFact]
    public void With_No_Insets_The_Safe_Area_Switch_Changes_Nothing()
    {
        using var stage = new Stage();
        Assert.Null(stage.Window.InsetsManager);

        Assert.False(SafeAreaMode.Set(stage.Window, ionic: true));
        Assert.False(SafeAreaMode.IsIonic);
        Assert.True(TopLevel.GetAutoSafeAreaPadding(stage.Shell));
        Assert.False(Application.Current!.Resources.ContainsKey(SafeAreaMode.TopKey));
    }

    // The page a phone is checked with. On desktop there are no insets, so it says so; captured for the docs.
    [AvaloniaFact]
    public void The_Device_Page_Opens_From_The_List_And_Says_There_Are_No_Insets_Here() => InstalledTheme.Run(_ =>
    {
        using var stage = new Stage();
        stage.Window.SetRenderScaling(2);
        stage.Open(b => Equals(b.Content, "device"));

        var page = Assert.IsType<DevicePage>(stage.Shell.FindControl<ContentControl>("Host")!.Content);
        Assert.Equal("No insets on this platform, so the switch does nothing.", page.FindControl<TextBlock>("Insets")!.Text);
        Assert.Contains(" × ", page.FindControl<TextBlock>("Screen")!.Text, StringComparison.Ordinal);

        for (var i = 0; i < 2; i++)
        {
            Settle();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        Frames.Save(stage.Window.CaptureRenderedFrame()!, "avonic-sample-device-page.png");
    });

    // From the button, through the shell's host, with small counts: the whole path a phone takes, under the installed theme.
    [AvaloniaFact]
    public void The_Device_Page_Runs_The_Benchmark_And_Shows_The_Row() => InstalledTheme.Run(_ =>
    {
        var make = DevicePage.MakeBenchmark;
        DevicePage.MakeBenchmark = host => new StyleCostRunner(host, elements: 10, depth: 2, warmUpRounds: 0, rounds: 1);
        try
        {
            using var stage = new Stage();
            stage.Open(b => Equals(b.Content, "device"));
            var page = (DevicePage)stage.Shell.FindControl<ContentControl>("Host")!.Content!;
            var row = page.FindControl<TextBox>("BenchmarkRow")!;

            page.FindControl<AvonicButton>("RunBenchmark")!.RaiseEvent(new RoutedEventArgs(AvonicButton.ClickEvent));
            var until = DateTime.UtcNow.AddSeconds(30);
            while (!row.IsVisible && DateTime.UtcNow < until)
            {
                Settle();
            }

            Assert.True(row.IsVisible, page.FindControl<TextBlock>("BenchmarkStatus")!.Text);
            Assert.StartsWith("| ", row.Text, StringComparison.Ordinal);
            Assert.Empty(stage.Shell.BenchmarkArea.Children);
        }
        finally
        {
            DevicePage.MakeBenchmark = make;
        }
    });

    // Back during a run: the page goes, and so does the run, with the theme back where it was. The back comes from inside the
    // run's first attach, as the dispatcher would run the whole of it in one go here.
    [AvaloniaFact]
    public void Leaving_The_Device_Page_Stops_Its_Benchmark() => InstalledTheme.Run(theme =>
    {
        var make = DevicePage.MakeBenchmark;
        var styles = Application.Current!.Styles;
        using var stage = new Stage();
        var index = styles.IndexOf(theme);
        var attaches = 0;
        Control Build()
        {
            if (++attaches == 1)
            {
                Assert.True(stage.Back());
            }

            return new Border();
        }

        DevicePage.MakeBenchmark = host => new StyleCostRunner(host, elements: 1, depth: 0, warmUpRounds: 0, rounds: 100, kinds: [Build]);
        try
        {
            stage.Open(b => Equals(b.Content, "device"));
            var page = (DevicePage)stage.Shell.FindControl<ContentControl>("Host")!.Content!;

            page.FindControl<AvonicButton>("RunBenchmark")!.RaiseEvent(new RoutedEventArgs(AvonicButton.ClickEvent));
            Settle();

            Assert.False(stage.Shell.IsShowingPage);
            Assert.Equal("Stopped: the page was left.", page.FindControl<TextBlock>("BenchmarkStatus")!.Text);
            Assert.Equal(2, attaches);
            Assert.Same(theme, styles[index]);
        }
        finally
        {
            DevicePage.MakeBenchmark = make;
        }
    });

    /// <summary>The test clock a millisecond at a time, so that an overlay's animation runs to its end.</summary>
    private static void Run(TestClock clock, double ms)
    {
        while (clock.Now.TotalMilliseconds < ms)
        {
            clock.At(Math.Min(Math.Floor(clock.Now.TotalMilliseconds) + 1, ms));
            Settle();
        }

        Settle();
    }
}
