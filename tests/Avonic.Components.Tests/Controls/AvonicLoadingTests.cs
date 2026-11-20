using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;
using Stage = Avonic.Components.Tests.Controls.AvonicModalTests.Stage;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-loading</c>, md, on the test clock: B1 to B15 in docs/components/ion-loading/PLAN.md, from <c>loading.js</c>,
/// <c>md.enter.js</c>, <c>md.leave.js</c>, <c>loading.md.css</c> and the measurements in <c>ref/measure.json</c>. Frames come every
/// 16 ms, so the enter animation, played at 0, shows local time t at 16 + t and finishes at the frame at 224; the leave, also
/// 200 ms, 224 after it starts on a frame.
/// </summary>
public class AvonicLoadingTests
{
    private const double Entered = 224;
    private const double Left = 224;

    private static readonly CubicBezierEasing EaseInOut = new(0.42, 0, 0.58, 1);

    internal static AvonicLoading Watch(Stage stage, AvonicLoading loading, string name = "")
    {
        string Now() => $"@{stage.Clock.Now.TotalMilliseconds}";
        loading.IonLoadingWillPresent += (_, _) => stage.Log.Add($"{name}ionLoadingWillPresent{Now()}");
        loading.WillPresent += (_, _) => stage.Log.Add($"{name}willPresent{Now()}");
        loading.IonLoadingDidPresent += (_, _) => stage.Log.Add($"{name}ionLoadingDidPresent{Now()}");
        loading.DidPresent += (_, _) => stage.Log.Add($"{name}didPresent{Now()}");
        loading.IonLoadingWillDismiss += (_, e) => stage.Log.Add($"{name}ionLoadingWillDismiss({e.Data},{e.Role}){Now()}");
        loading.WillDismiss += (_, e) => stage.Log.Add($"{name}willDismiss({e.Data},{e.Role}){Now()}");
        loading.IonLoadingDidDismiss += (_, e) => stage.Log.Add($"{name}ionLoadingDidDismiss({e.Data},{e.Role}){Now()}");
        loading.DidDismiss += (_, e) => stage.Log.Add($"{name}didDismiss({e.Data},{e.Role}){Now()}");
        return loading;
    }

    internal static AvonicLoading Shown(Stage stage, AvonicLoading loading)
    {
        loading.Present(stage.Window);
        stage.At(stage.Clock.Now.TotalMilliseconds + Entered);
        stage.Render();
        return loading;
    }

    private static double Scale(Visual visual) => visual.RenderTransform?.Value.M11 ?? 1;

    /// <summary>The box in the window, which in right to left is mirrored, so its origin is its right edge.</summary>
    private static Rect Box(Visual visual, Stage stage) => new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(stage.Window)!.Value);

    private static T Part<T>(AvonicLoading loading, string name) where T : Control =>
        loading.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static void Key(Stage stage, PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        stage.Window.KeyPressQwerty(key, modifiers);
        stage.Window.KeyReleaseQwerty(key, modifiers);
        Stage.Settle();
    }

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    /// <summary>
    /// Within 1: Avalonia rounds the layout to whole pixels at 1x, where Chromium keeps the fractions of a 2x screen, so a text
    /// 61.11 wide is 62 here, and what's centred moves by half of that.
    /// </summary>
    private static void AssertBox(Rect expected, Rect actual, double within = 1)
    {
        var close = Math.Abs(actual.X - expected.X) <= within && Math.Abs(actual.Y - expected.Y) <= within
                    && Math.Abs(actual.Width - expected.Width) <= within && Math.Abs(actual.Height - expected.Height) <= within;
        Assert.True(close, $"Expected {expected}, was {actual}.");
    }

    // ── B1, B2: md's animations ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Enter_Is_Mds_200ms_Fade_And_Scale_Down_From_1_1()
    {
        using var stage = new Stage();
        var loading = new AvonicLoading { Message = "Loading..." };
        loading.Present(stage.Window);
        Stage.Settle();
        var backdrop = loading.Backdrop!;
        var wrapper = loading.Wrapper!;

        Assert.Equal(0.01, backdrop.Opacity, 6);
        Assert.Equal(0.01, wrapper.Opacity, 6);
        Assert.Equal(1.1, Scale(wrapper), 6);
        Assert.False(backdrop.IsHitTestVisible);

        foreach (var local in new[] { 32.0, 96, 144, 192 })
        {
            stage.At(16 + local);
            var e = EaseInOut.Ease(local / 200);
            Assert.Equal(0.01 + (0.32 - 0.01) * e, backdrop.Opacity, 4);
            Assert.Equal(0.01 + 0.99 * e, wrapper.Opacity, 4);
            Assert.Equal(1.1 - 0.1 * e, Scale(wrapper), 4);
            Assert.False(backdrop.IsHitTestVisible);
        }

        // M1: Chromium's values at 25, 50, 100 and 150 ms, which the curve gives.
        Assert.Equal(0.0408029, 0.01 + 0.99 * EaseInOut.Ease(25 / 200.0), 4);
        Assert.Equal(1.08708, 1.1 - 0.1 * EaseInOut.Ease(50 / 200.0), 4);
        Assert.Equal(0.165, 0.01 + 0.31 * EaseInOut.Ease(100 / 200.0), 4);
        Assert.Equal(0.27996, 0.01 + 0.31 * EaseInOut.Ease(150 / 200.0), 4);

        stage.At(Entered);
        Assert.Equal(0.32, backdrop.Opacity, 6);
        Assert.Equal(1, wrapper.Opacity, 6);
        Assert.Equal(1, Scale(wrapper), 6);
        Assert.True(backdrop.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void B2_Leave_Is_Mds_200ms_Fade_And_Scale_To_0_9_And_The_Wrapper_Rests_Unseen()
    {
        using var stage = new Stage();
        var loading = Shown(stage, new AvonicLoading { Message = "Loading..." });
        var wrapper = loading.Wrapper!;
        var start = stage.Clock.Now.TotalMilliseconds;
        loading.Dismiss();
        Stage.Settle();
        Assert.Equal(0.99, wrapper.Opacity, 6);
        Assert.Equal(1, Scale(wrapper), 6);

        foreach (var local in new[] { 32.0, 96, 144, 192 })
        {
            stage.At(start + 16 + local);
            var e = EaseInOut.Ease(local / 200);
            Assert.Equal(0.32 * (1 - e), loading.Backdrop!.Opacity, 4);
            Assert.Equal(0.99 * (1 - e), wrapper.Opacity, 4);
            Assert.Equal(1 - 0.1 * e, Scale(wrapper), 4);
        }

        // M2 at 50 and 150 ms.
        Assert.Equal(0.86213, 0.99 * (1 - EaseInOut.Ease(50 / 200.0)), 4);
        Assert.Equal(0.912916, 1 - 0.1 * EaseInOut.Ease(150 / 200.0), 4);

        stage.After(Left);
        Assert.False(loading.IsVisible);

        // .loading-wrapper { opacity: 0 } once the animations are gone.
        Assert.Equal(0, wrapper.Opacity, 6);
    }

    [AvaloniaFact]
    public void B2_Backdrop_Opacity_Follows_The_Loadings_Key_Then_The_Global_One()
    {
        using var stage = new Stage();
        var loading = new AvonicLoading();
        stage.Window.Resources["Avonic.BackdropOpacity"] = 0.5;
        Shown(stage, loading);
        Assert.Equal(0.5, loading.Backdrop!.Opacity, 6);
        loading.Dismiss();
        stage.After(Left);

        loading.Resources["Avonic.Loading.BackdropOpacity"] = 0.2;
        Shown(stage, loading);
        Assert.Equal(0.2, loading.Backdrop!.Opacity, 6);
    }

    // ── B3: events ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_The_Loadings_Events_Come_Before_The_Generic_Ones_With_Data_And_Role()
    {
        using var stage = new Stage();
        var loading = Watch(stage, new AvonicLoading { Message = "Loading..." });
        var early = loading.Dismiss();
        Stage.Settle();
        Assert.True(early.IsCompleted);
        Assert.False(early.Result);

        loading.Present(stage.Window);
        stage.At(Entered);
        loading.Dismiss("d", "r");
        stage.After(Left);
        Assert.Equal(
            [
                "ionLoadingWillPresent@0", "willPresent@0", "ionLoadingDidPresent@224", "didPresent@224",
                "ionLoadingWillDismiss(d,r)@224", "willDismiss(d,r)@224", "ionLoadingDidDismiss(d,r)@448", "didDismiss(d,r)@448",
            ],
            stage.Log);
    }

    // ── B4: focus ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Present_Focuses_The_Loading_And_Dismiss_Gives_Focus_Back()
    {
        using var stage = new Stage();
        stage.Opener.Focus();
        var loading = Shown(stage, new AvonicLoading { Message = "Loading..." });
        Assert.Same(loading, stage.Focused);

        loading.Dismiss();
        stage.After(Left);
        Assert.Same(stage.Opener, stage.Focused);
    }

    [AvaloniaFact]
    public void B4_KeyboardClose_False_Blurs_And_Focuses_Nothing()
    {
        using var stage = new Stage();
        stage.Opener.Focus();
        Shown(stage, new AvonicLoading { Message = "Loading...", KeyboardClose = false });

        // M13: the input is blurred, and nothing has focus.
        Assert.True(stage.Focused is null || ReferenceEquals(stage.Focused, stage.Window), $"Focused: {stage.Focused}");
    }

    [AvaloniaFact]
    public void B4_Tab_Stays_On_The_Loading_As_Its_Wrapper_Has_Nothing_To_Focus()
    {
        using var stage = new Stage();
        var loading = Shown(stage, new AvonicLoading { Message = "Loading..." });
        Assert.Same(loading, stage.Focused);

        // M13: four Tabs, and still the host: the sentinel wraps to the wrapper's first focusable element, and there's none.
        for (var i = 0; i < 4; i++)
        {
            Key(stage, PhysicalKey.Tab);
            Assert.Same(loading, stage.Focused);
        }
    }

    // ── B5, B6: the backdrop, Escape and back ───────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_By_Default_A_Backdrop_Tap_Escape_And_Back_Do_Nothing()
    {
        using var stage = new Stage();
        var loading = Watch(stage, new AvonicLoading { Message = "Loading..." });
        Shown(stage, loading);
        stage.Log.Clear();
        Assert.False(loading.BackdropDismiss);
        Assert.False(loading.Backdrop!.Tappable);

        stage.Click(new Point(30, 30));
        Key(stage, PhysicalKey.Escape);
        Key(stage, PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        stage.After(Left);
        Assert.Empty(stage.Log);
        Assert.True(loading.IsVisible);
    }

    [AvaloniaFact]
    public void B5_BackdropDismiss_Lets_The_Backdrop_Escape_And_Back_Dismiss_With_Backdrop_But_Not_The_Wrapper()
    {
        using var stage = new Stage();
        var loading = Watch(stage, new AvonicLoading { Message = "Loading...", BackdropDismiss = true });
        Shown(stage, loading);
        stage.Log.Clear();

        // A tap on the wrapper, which is over the backdrop.
        var wrapper = Box(loading.Wrapper!, stage);
        stage.Click(wrapper.Center);
        stage.After(Left);
        Assert.Empty(stage.Log);

        stage.Click(new Point(30, 30));
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,backdrop)", StringComparison.Ordinal));

        Shown(stage, loading);
        stage.Log.Clear();
        Key(stage, PhysicalKey.Escape);
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,backdrop)", StringComparison.Ordinal));

        Shown(stage, loading);
        stage.Log.Clear();
        Key(stage, PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,backdrop)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B6_Without_The_Backdrop_It_Is_Transparent_And_Still_Covers_The_Page()
    {
        var under = new Button { Name = "Under", Content = "Under", Height = 40 };
        using var stage = new Stage(390, 844, under);
        var clicks = 0;
        under.Click += (_, _) => clicks++;
        stage.Render();
        var point = Box(under, stage).Center;
        stage.Click(point);
        Assert.Equal(1, clicks);

        var loading = Watch(stage, new AvonicLoading { Message = "Loading...", ShowBackdrop = false });
        Shown(stage, loading);
        stage.Log.Clear();
        Assert.False(loading.Backdrop!.Visible);

        // M9: backdrop-hide, transparent; a tap on the button under it doesn't reach it.
        stage.Click(point);
        stage.After(Left);
        Assert.Equal(1, clicks);
        Assert.Empty(stage.Log);

        // M15: and with backdropDismiss, the tap dismisses.
        loading.BackdropDismiss = true;
        stage.Click(point);
        stage.After(Left);
        Assert.Equal(1, clicks);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,backdrop)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B6_The_Backdrop_Is_Drawn_Only_With_ShowBackdrop()
    {
        using var stage = new Stage();
        var loading = Shown(stage, new AvonicLoading { Message = "Loading..." });
        var frame = stage.Window.CaptureRenderedFrame()!;
        var shown = Frames.PixelAt(frame, 30, 700);

        loading.Dismiss();
        stage.After(Left);
        loading.ShowBackdrop = false;
        Shown(stage, loading);
        var hidden = Frames.PixelAt(stage.Window.CaptureRenderedFrame()!, 30, 700);

        // Black at 0.32 over the white page, then the page itself.
        Assert.InRange(shown.R, 168, 180);
        Assert.InRange(hidden.R, 250, 255);
    }

    // ── B7: duration ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_A_Duration_Dismisses_10ms_Later_Than_Its_Length_After_The_Present_With_No_Role()
    {
        using var stage = new Stage();
        var loading = Watch(stage, new AvonicLoading { Message = "Loading...", Duration = 500 });
        Shown(stage, loading);
        stage.Log.Clear();

        // M16: setTimeout(dismiss, duration + 10) once present() has finished.
        stage.At(Entered + 509);
        Assert.Empty(stage.Log);
        stage.At(Entered + 510);
        Assert.Equal(["ionLoadingWillDismiss(,)@734", "willDismiss(,)@734"], stage.Log);
    }

    [AvaloniaFact]
    public void B7_An_Earlier_Dismiss_Stops_The_Timer_And_0_Never_Dismisses()
    {
        using var stage = new Stage();
        var loading = Watch(stage, new AvonicLoading { Message = "Loading...", Duration = 1000 });
        Shown(stage, loading);
        stage.At(600);
        loading.Dismiss();
        stage.After(Left);

        // Presented again with no duration: nothing at the old timer's time, or long after.
        loading.Duration = 0;
        Shown(stage, loading);
        stage.Log.Clear();
        stage.At(5000);
        Assert.Empty(stage.Log);
    }

    [AvaloniaFact]
    public void B7_A_Second_Present_Starts_No_Second_Timer()
    {
        using var stage = new Stage();
        var loading = Watch(stage, new AvonicLoading { Message = "Loading...", Duration = 1000 });
        Shown(stage, loading);

        // Ionic's second present() starts a timer for 1510, which dismisses the next presentation (docs/infra/ionic-issues.md).
        stage.At(500);
        loading.Present(stage.Window);
        stage.At(1234);
        Assert.Contains("willDismiss(,)@1234", stage.Log);
        stage.After(Left);

        Shown(stage, loading);
        stage.Log.Clear();
        stage.At(1510 + Entered + 100);
        Assert.Empty(stage.Log);
    }

    [AvaloniaFact]
    public void B7_A_Duration_Too_Long_For_A_Timer_Never_Comes()
    {
        using var stage = new Stage();
        var loading = Watch(stage, new AvonicLoading { Message = "Forever", Duration = double.PositiveInfinity });
        var present = loading.Present(stage.Window);
        stage.At(Entered);
        Assert.True(present.IsCompletedSuccessfully, $"The present: {present.Status}, {present.Exception?.InnerException?.Message}");
        stage.Log.Clear();
        stage.At(5000);
        Assert.Empty(stage.Log);
    }

    // ── B8: the spinner ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_The_Spinner_Defaults_To_The_Loading_Config_Then_The_Spinner_Config_Then_Crescent()
    {
        var loadingSpinner = AvonicConfig.LoadingSpinner;
        var spinner = AvonicConfig.Spinner;
        try
        {
            using var stage = new Stage();
            AvonicConfig.LoadingSpinner = null;
            AvonicConfig.Spinner = null;
            var plain = Shown(stage, new AvonicLoading());
            Assert.Equal(SpinnerName.Crescent, plain.Spinner);
            Assert.Equal(SpinnerName.Crescent, plain.SpinnerPart!.SpinnerName);
            Assert.True(Part<Border>(plain, "PART_SpinnerBox").IsVisible);

            // Read once, at the first load: a change of the config later reaches only new ones.
            AvonicConfig.Spinner = SpinnerName.Lines;
            plain.Dismiss();
            stage.After(Left);
            Shown(stage, plain);
            Assert.Equal(SpinnerName.Crescent, plain.Spinner);
            plain.Dismiss();
            stage.After(Left);

            var fromSpinner = Shown(stage, new AvonicLoading());
            Assert.Equal(SpinnerName.Lines, fromSpinner.Spinner);
            fromSpinner.Dismiss();
            stage.After(Left);

            AvonicConfig.LoadingSpinner = SpinnerName.Dots;
            var fromLoading = Shown(stage, new AvonicLoading());
            Assert.Equal(SpinnerName.Dots, fromLoading.Spinner);
            fromLoading.Dismiss();
            stage.After(Left);

            // A spinner of its own, or none, stays.
            var own = Shown(stage, new AvonicLoading { Spinner = SpinnerName.Bubbles });
            Assert.Equal(SpinnerName.Bubbles, own.Spinner);
            own.Dismiss();
            stage.After(Left);
            var none = Shown(stage, new AvonicLoading { Spinner = null });
            Assert.Null(none.Spinner);
            Assert.False(Part<Border>(none, "PART_SpinnerBox").IsVisible);
            none.Dismiss();
            stage.After(Left);

            // The config's null, ConfigSpinner.None, is no spinner.
            AvonicConfig.LoadingSpinner = ConfigSpinner.None;
            var configNone = Shown(stage, new AvonicLoading());
            Assert.Null(configNone.Spinner);
            Assert.False(Part<Border>(configNone, "PART_SpinnerBox").IsVisible);
        }
        finally
        {
            AvonicConfig.LoadingSpinner = loadingSpinner;
            AvonicConfig.Spinner = spinner;
        }
    }

    [AvaloniaFact]
    public void B8_The_Spinner_Is_The_Name_Given_In_The_Spinner_Colour_And_Hidden_From_Automation()
    {
        using var stage = new Stage();
        var loading = Shown(stage, new AvonicLoading { Message = "Loading...", Spinner = SpinnerName.Dots });
        var spinner = loading.SpinnerPart!;
        Assert.Equal(SpinnerName.Dots, spinner.SpinnerName);
        Assert.Equal(Color.Parse("#0054e9"), ColorOf(spinner.Foreground));
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(spinner));

        // --spinner-color
        loading.Resources["Avonic.Loading.SpinnerColor"] = Brushes.Red;
        Stage.Settle();
        Assert.Equal(Colors.Red, ColorOf(spinner.Foreground));

        // M8: 28 by 28 whatever the name.
        foreach (var name in Enum.GetValues<SpinnerName>())
        {
            loading.Spinner = name;
            stage.Render();
            Assert.Equal(new Size(28, 28), spinner.Bounds.Size);
        }
    }

    // ── B9, B10: the message and the layout ─────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_The_Basic_Loading_Has_Mds_Boxes()
    {
        using var stage = new Stage();
        var loading = Shown(stage, new AvonicLoading { Message = "Loading..." });

        // M4.
        AssertBox(new Rect(118.44, 382.5, 153.11, 79), Box(loading.Wrapper!, stage));
        AssertBox(new Rect(142.44, 406.5, 28, 31), Box(Part<Border>(loading, "PART_SpinnerBox"), stage));
        AssertBox(new Rect(142.44, 406.5, 28, 28), Box(loading.SpinnerPart!, stage));
        AssertBox(new Rect(186.44, 414, 61.11, 16), Box(Part<Border>(loading, "PART_Content"), stage));
    }

    [AvaloniaFact]
    public void B9_No_Message_Or_No_Spinner_Leaves_Out_Its_Part_And_The_Gap()
    {
        using var stage = new Stage();
        var noSpinner = Shown(stage, new AvonicLoading { Message = "Loading...", Spinner = null });

        // M5: no spinner.
        AssertBox(new Rect(140.44, 390, 109.11, 64), Box(noSpinner.Wrapper!, stage));
        AssertBox(new Rect(164.44, 414, 61.11, 16), Box(Part<Border>(noSpinner, "PART_Content"), stage));
        noSpinner.Dismiss();
        stage.After(Left);

        // No message: no content, no gap.
        var noMessage = Shown(stage, new AvonicLoading());
        AssertBox(new Rect(157, 382.5, 76, 79), Box(noMessage.Wrapper!, stage));
        Assert.False(Part<Border>(noMessage, "PART_Content").IsVisible);
        noMessage.Dismiss();
        stage.After(Left);

        // "": an empty .loading-content, 0 by 0, still 16 after the spinner.
        var empty = Shown(stage, new AvonicLoading { Message = "" });
        AssertBox(new Rect(149, 382.5, 92, 79), Box(empty.Wrapper!, stage));
        AssertBox(new Rect(217, 422, 0, 0), Box(Part<Border>(empty, "PART_Content"), stage));
    }

    [AvaloniaFact]
    public void B9_A_Long_Message_Wraps_Within_The_Maximum_Width()
    {
        using var stage = new Stage();
        var loading = Shown(stage, new AvonicLoading
        {
            Message = "Please wait while we load your data from the server. This could take a little while.",
        });

        // M6: 280 wide, three lines of 16; the spinner's box centred beside them.
        AssertBox(new Rect(55, 374, 280, 96), Box(loading.Wrapper!, stage));
        AssertBox(new Rect(123, 398, 188, 48), Box(Part<Border>(loading, "PART_Content"), stage));
        AssertBox(new Rect(79, 406.5, 28, 31), Box(Part<Border>(loading, "PART_SpinnerBox"), stage));
    }

    [AvaloniaFact]
    public void B9_A_Message_That_Is_Not_A_String_Is_Shown_As_It_Is()
    {
        using var stage = new Stage();
        var content = new StackPanel { Children = { new TextBlock { Text = "Uploading" }, new TextBlock { Text = "3 of 7" } } };
        var loading = Shown(stage, new AvonicLoading { Message = content });
        Assert.True(content.IsEffectivelyVisible);
        Assert.Same(loading, content.FindAncestorOfType<AvonicLoading>());
        Assert.True(Box(content, stage).Height > 30);
    }

    [AvaloniaFact]
    public void B10_The_Sizes_Come_From_The_Keys()
    {
        using var stage = new Stage();
        var loading = new AvonicLoading { Message = "Loading..." };
        loading.Resources["Avonic.Loading.Width"] = "200px";
        loading.Resources["Avonic.Loading.MinHeight"] = 150.0;
        Shown(stage, loading);

        // M7: the row centred both ways in the bigger box, where it is in M4.
        AssertBox(new Rect(95, 347, 200, 150), Box(loading.Wrapper!, stage));
        AssertBox(new Rect(142.44, 406.5, 28, 28), Box(loading.SpinnerPart!, stage));
        AssertBox(new Rect(186.44, 414, 61.11, 16), Box(Part<Border>(loading, "PART_Content"), stage));

        // --max-height: 90% of the window; --max-width, --min-width.
        loading.Resources["Avonic.Loading.Height"] = "2000px";
        loading.Resources["Avonic.Loading.MaxWidth"] = 150.0;
        loading.Resources["Avonic.Loading.MinWidth"] = "180px";
        stage.Render();
        Assert.InRange(loading.Wrapper!.Bounds.Height, 844 * 0.9 - 1, 844 * 0.9 + 1);
        Assert.Equal(180, loading.Wrapper.Bounds.Width, 1);
    }

    [AvaloniaFact]
    public void B10_In_Right_To_Left_The_Row_Is_Mirrored()
    {
        using var stage = new Stage();
        stage.Window.FlowDirection = FlowDirection.RightToLeft;
        var loading = Shown(stage, new AvonicLoading { Message = "Loading..." });

        // M12: the spinner on the right, the message on its left.
        AssertBox(new Rect(219.56, 406.5, 28, 28), Box(loading.SpinnerPart!, stage));
        AssertBox(new Rect(142.44, 414, 61.13, 16), Box(Part<Border>(loading, "PART_Content"), stage));
    }

    // ── B11: the look ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_The_Colours_Radius_And_Shadow_Are_Mds_And_The_Keys_Change_Them()
    {
        using var stage = new Stage();
        var loading = Shown(stage, new AvonicLoading { Message = "Loading..." });
        var wrapper = Assert.IsType<Border>(loading.Wrapper);
        var text = loading.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Loading...");

        // M4.
        Assert.Equal(Color.Parse("#f2f2f2"), ColorOf(wrapper.Background));
        Assert.Equal(Color.Parse("#262626"), ColorOf(text.Foreground));
        Assert.Equal(14, text.FontSize);
        Assert.Equal(new CornerRadius(2), wrapper.CornerRadius);
        Assert.Equal(new Thickness(24), wrapper.Padding);
        Assert.Equal(CssBoxShadow.Parse("0 16px 20px rgba(0, 0, 0, 0.4)"), wrapper.BoxShadow);

        // M11: an app's keys, as a cssClass's variables.
        loading.Resources["Avonic.Loading.Background"] = new SolidColorBrush(Color.Parse("#222"));
        loading.Foreground = Brushes.White;
        Stage.Settle();
        Assert.Equal(Color.Parse("#222"), ColorOf(wrapper.Background));
        Assert.Equal(Colors.White, ColorOf(text.Foreground));
    }

    // ── B12, B13: automation and classes ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_The_Loading_Is_A_Dialog_Named_By_Its_Message_Else_Its_Aria_Label()
    {
        using var stage = new Stage();
        var loading = Shown(stage, new AvonicLoading
        {
            Message = "Loading...",
            HtmlAttributes = new Dictionary<string, object?> { ["id"] = "my-loading", ["aria-label"] = "Busy" },
        });
        var peer = ControlAutomationPeer.CreatePeerForElement(loading);
        Assert.Equal(AutomationControlType.Window, peer.GetAutomationControlType());
        Assert.True(peer.IsControlElement());

        // M17: aria-labelledby the message beats aria-label.
        Assert.Equal("Loading...", peer.GetName());
        Assert.Equal("my-loading", peer.GetAutomationId());

        loading.Message = null;
        Assert.Equal("Busy", peer.GetName());
        loading.HtmlAttributes = null;
        AutomationProperties.SetName(loading, "Own");
        Assert.Equal("Own", peer.GetName());
    }

    [AvaloniaFact]
    public void B12_A_New_Message_Raises_The_Dialogs_Name_Change()
    {
        using var stage = new Stage();
        var loading = Shown(stage, new AvonicLoading { Message = "One" });
        var peer = ControlAutomationPeer.CreatePeerForElement(loading);
        Assert.Equal("One", peer.GetName());
        var changes = new List<object?>();
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == AutomationElementIdentifiers.NameProperty)
            {
                changes.Add(e.NewValue);
            }
        };

        // As the element aria-labelledby names changes its text; then aria-label, and its own name.
        loading.Message = "Two";
        loading.Message = null;
        loading.HtmlAttributes = new Dictionary<string, object?> { ["aria-label"] = "Busy" };
        loading.HtmlAttributes = null;
        AutomationProperties.SetName(loading, "Own");
        Assert.Equal(["Two", null, "Busy", null, "Own"], changes);
    }

    [AvaloniaFact]
    public void B13_CssClass_Puts_Classes_On_The_Loading_And_Takes_Them_Off()
    {
        var loading = new AvonicLoading { CssClass = "custom-loading wide" };
        Assert.Contains("custom-loading", loading.Classes);
        Assert.Contains("wide", loading.Classes);
        loading.CssClass = "other";
        Assert.DoesNotContain("custom-loading", loading.Classes);
        Assert.Equal(["other"], loading.Classes.Where(c => !c.StartsWith(':')));
    }

    // ── B14, B15: isOpen, trigger, changes, stacking ────────────────────────────────────────

    [AvaloniaFact]
    public void B14_IsOpen_And_Trigger_Present_It_And_A_Change_While_Open_Renders_Again()
    {
        using var stage = new Stage();
        var scope = new NameScope();
        scope.Register("Opener", stage.Opener);
        NameScope.SetNameScope(stage.Page, scope);
        var loading = Watch(stage, new AvonicLoading { Trigger = "Opener", Message = "Loading..." });
        stage.Page.Children.Add(loading);
        Stage.Settle();

        stage.Opener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        stage.At(Entered);
        stage.Render();
        Assert.Contains("didPresent@224", stage.Log);

        // M18: a longer message, and a new spinner, while it's up.
        loading.Message = "Almost done, a longer message";
        loading.Spinner = SpinnerName.Dots;
        stage.Render();
        Assert.Equal(280, loading.Wrapper!.Bounds.Width, 1);
        Assert.Equal(80, loading.Wrapper.Bounds.Height, 1);
        Assert.Equal(SpinnerName.Dots, loading.SpinnerPart!.SpinnerName);

        loading.Dismiss();
        stage.After(Left);
        Assert.Contains(loading, stage.Page.Children);
        stage.Log.Clear();

        // isOpen: true presents; false dismisses, with no role; it isn't set back.
        loading.IsOpen = true;
        stage.After(Entered);
        Assert.Contains(stage.Log, l => l.StartsWith("didPresent", StringComparison.Ordinal));
        loading.IsOpen = false;
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B15_The_Loading_Stacks_Over_Alerts_And_Modals_And_Under_Toasts()
    {
        using var stage = new Stage();
        var loading = Shown(stage, new AvonicLoading { Message = "Loading..." });
        var alert = new AvonicAlert { Header = "Alert" };
        alert.Present(stage.Window);
        var toast = new AvonicToast { Message = "Toast" };
        toast.Present(stage.Window);
        stage.After(500);

        Assert.Equal(40000 + loading.OverlayIndex, loading.ZIndex);
        Assert.True(loading.ZIndex > alert.ZIndex, "The loading is over an alert presented after it.");
        Assert.True(toast.ZIndex > loading.ZIndex, "A toast is over the loading.");
    }
}
