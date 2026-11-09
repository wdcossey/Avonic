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

        public void OpenAPage()
        {
            var index = (StackPanel)Shell.FindControl<ContentControl>("Host")!.Content!;
            index.Children.OfType<Button>().First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.True(Shell.IsShowingPage);
        }

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

    [AvaloniaFact]
    public void Back_From_A_Page_Shows_The_List_And_Is_Handled()
    {
        using var stage = new Stage();
        stage.OpenAPage();

        Assert.True(stage.Back());
        Assert.False(stage.Shell.IsShowingPage);
        Assert.IsType<StackPanel>(stage.Shell.FindControl<ContentControl>("Host")!.Content);
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
    public void The_Header_Pads_Itself_By_The_Safe_Area_And_The_Bottom_Line_By_Its_Bottom()
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
        Assert.Equal(new Thickness(8, 2), line.Margin);
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
