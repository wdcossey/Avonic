using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;
using Stage = Avonic.Components.Tests.Controls.AvonicModalTests.Stage;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-toast</c>, md, on the test clock: B1 to B24 in docs/components/ion-toast/PLAN.md, from <c>toast.js</c>, <c>md.enter.js</c>,
/// <c>md.leave.js</c>, <c>swipe-to-dismiss.js</c>, <c>toast.md.css</c> and the measurements in <c>ref/</c>. Frames come every 16 ms,
/// so the enter animation, played at 0, shows local time t at 16 + t and finishes at the frame at 416; the leave, 300 ms, at the frame
/// 320 after it starts on one.
/// </summary>
public class AvonicToastTests
{
    private const double Entered = 416;
    private const double Left = 320;

    private static readonly CubicBezierEasing Curve = new(0.36, 0.66, 0.04, 1);

    private static AvonicToast Watch(Stage stage, AvonicToast toast, string name = "")
    {
        string Now() => $"@{stage.Clock.Now.TotalMilliseconds}";
        toast.IonToastWillPresent += (_, _) => stage.Log.Add($"{name}ionToastWillPresent{Now()}");
        toast.WillPresent += (_, _) => stage.Log.Add($"{name}willPresent{Now()}");
        toast.IonToastDidPresent += (_, _) => stage.Log.Add($"{name}ionToastDidPresent{Now()}");
        toast.DidPresent += (_, _) => stage.Log.Add($"{name}didPresent{Now()}");
        toast.IonToastWillDismiss += (_, e) => stage.Log.Add($"{name}ionToastWillDismiss({e.Data},{e.Role}){Now()}");
        toast.WillDismiss += (_, e) => stage.Log.Add($"{name}willDismiss({e.Data},{e.Role}){Now()}");
        toast.IonToastDidDismiss += (_, e) => stage.Log.Add($"{name}ionToastDidDismiss({e.Data},{e.Role}){Now()}");
        toast.DidDismiss += (_, e) => stage.Log.Add($"{name}didDismiss({e.Data},{e.Role}){Now()}");
        return toast;
    }

    private static AvonicToast Shown(Stage stage, AvonicToast toast)
    {
        _ = toast.Present(stage.Window);
        stage.At(stage.Clock.Now.TotalMilliseconds + Entered);
        stage.Render();
        return toast;
    }

    private static void Gone(Stage stage) => stage.At(stage.Clock.Now.TotalMilliseconds + Left + 16);

    private static Rect Box(Visual visual, Stage stage) => AvonicToastCaptureTests.Box(visual, stage.Window);

    private static Point Centre(Visual visual, Stage stage)
    {
        var box = Box(visual, stage);
        return new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
    }

    private static double TranslateY(Visual visual) => visual.RenderTransform?.Value.M32 ?? 0;

    private static void Near(double[] expected, Rect actual, double tolerance = 0.5)
    {
        var got = new[] { actual.X, actual.Y, actual.Width, actual.Height };
        for (var i = 0; i < 4; i++)
        {
            Assert.True(Math.Abs(expected[i] - got[i]) <= tolerance,
                $"Expected [{string.Join(", ", expected)}], got [{string.Join(", ", got.Select(v => Math.Round(v, 2)))}]");
        }
    }

    private static T Part<T>(AvonicToast toast, string name) where T : Control => AvonicToastCaptureTests.Part<T>(toast, name);

    private static Func<object?> Log(List<string> calls, string name, object? result = null) => () =>
    {
        calls.Add(name);
        return result;
    };

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    // ── B1 to B3: the animations and the events ─────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Enter_Fades_The_Wrapper_In_Over_400ms_With_Its_Place_Set_At_Once()
    {
        using var stage = new Stage();
        var toast = new AvonicToast { Message = "Hello World" };
        _ = toast.Present(stage.Window);
        Stage.Settle();
        var wrapper = toast.Wrapper!;

        // The transform is the inline style, set before the first frame and never animated.
        Assert.Equal(-8, TranslateY(wrapper), 6);
        Assert.Equal(0.01, wrapper.Opacity, 6);
        foreach (var local in new[] { 32.0, 48, 112, 208, 384 })
        {
            stage.At(16 + local);
            Assert.Equal(0.01 + 0.99 * Curve.Ease(local / 400), wrapper.Opacity, 4);
            Assert.Equal(-8, TranslateY(wrapper), 6);
        }

        // M1: Chromium had 0.289413 at 50.061 ms and 0.779794 at 116.661.
        Assert.InRange(0.01 + 0.99 * Curve.Ease(50.061 / 400) - 0.289413, -0.0005, 0.0005);
        Assert.InRange(0.01 + 0.99 * Curve.Ease(116.661 / 400) - 0.779794, -0.0005, 0.0005);

        stage.At(Entered);
        stage.Render();
        Assert.Equal(1, wrapper.Opacity, 6);
        Near([8, 788, 374, 48], Box(wrapper, stage));
    }

    [AvaloniaFact]
    public void B1_Top_Is_8_Down_And_The_Middle_Is_Floor_Of_Half_The_Rest_With_No_Transform()
    {
        using var stage = new Stage();
        var top = Shown(stage, new AvonicToast { Message = "Top", Position = ToastPosition.Top });
        Assert.Equal(8, TranslateY(top.Wrapper!), 6);
        Near([8, 8, 374, 48], Box(top.Wrapper!, stage));
        _ = top.Dismiss();
        Gone(stage);

        // The same toast again, in the middle: the top's transform goes (not copied: Ionic keeps it).
        top.Position = ToastPosition.Middle;
        var middle = Shown(stage, top);
        Assert.Equal(398, middle.Panel!.MiddleTop);
        Assert.Equal(0, TranslateY(middle.Wrapper!), 6);
        Near([8, 398, 374, 48], Box(middle.Wrapper!, stage));
        Assert.Equal(398, AvonicToast.OffsetForMiddle(844, 48));
        Assert.Equal(397, AvonicToast.OffsetForMiddle(844, 49.4));
    }

    [AvaloniaFact]
    public void B2_Leave_Fades_From_099_To_0_Over_300ms_And_Rests_Off_The_Edge()
    {
        using var stage = new Stage();
        var toast = Shown(stage, new AvonicToast { Message = "Bye" });
        var wrapper = toast.Wrapper!;
        stage.At(1008);
        _ = toast.Dismiss();
        Stage.Settle();
        Assert.Equal(0.99, wrapper.Opacity, 6);
        foreach (var local in new[] { 48.0, 144, 272 })
        {
            stage.At(1008 + 16 + local);
            Assert.Equal(0.99 * (1 - Curve.Ease(local / 300)), wrapper.Opacity, 4);
        }

        stage.At(1008 + Left + 16);
        Assert.Null(toast.GetVisualParent());

        // The inline transform stays, as Ionic's inline style does; the opacity is back to its rest, 0.01.
        Assert.Equal(-8, TranslateY(wrapper), 6);
        Assert.Equal(0.01, wrapper.Opacity, 6);
    }

    [AvaloniaFact]
    public void B2_At_Rest_The_Wrapper_Is_Faint_And_Off_Its_Edge()
    {
        using var stage = new Stage();
        var toast = new AvonicToast { Message = "Rest" };
        stage.Page.Children.Add(toast);
        toast.ApplyTemplate();
        var wrapper = Part<Border>(toast, "PART_Wrapper");
        Assert.Equal(0.01, wrapper.Opacity);
        Assert.Equal(1, wrapper.GetValue(Css.TranslateYProperty));
        toast.Position = ToastPosition.Top;
        Assert.Equal(-1, wrapper.GetValue(Css.TranslateYProperty));
        toast.Position = ToastPosition.Middle;
        Assert.Equal(0, wrapper.GetValue(Css.TranslateYProperty));
    }

    [AvaloniaFact]
    public void B3_Events_Come_In_Ionics_Order_And_Present_Finishes_After_Them()
    {
        using var stage = new Stage();
        var toast = Watch(stage, new AvonicToast { Message = "Events" });
        var present = toast.Present(stage.Window);
        Stage.Settle();
        Assert.Equal(["ionToastWillPresent@0", "willPresent@0"], stage.Log);
        stage.At(Entered - 1);
        Assert.False(present.IsCompleted);
        stage.At(Entered);
        Assert.True(present.IsCompleted);
        Assert.Equal(["ionToastWillPresent@0", "willPresent@0", "ionToastDidPresent@416", "didPresent@416"], stage.Log);
        stage.Log.Clear();

        stage.At(1000);
        var dismiss = toast.Dismiss("data", "role");
        stage.At(1000 + Left);
        Assert.True(dismiss.IsCompleted);
        Assert.True(dismiss.Result);
        Assert.Equal(["ionToastWillDismiss(data,role)@1000", "willDismiss(data,role)@1000", "ionToastDidDismiss(data,role)@1320", "didDismiss(data,role)@1320"], stage.Log);
    }

    // ── B4 to B6: where it is ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Sizes_Are_Content_Box_Centred_Between_The_Insets()
    {
        using var stage = new Stage();
        var toast = new AvonicToast { Message = "Custom" };
        toast.Resources["Avonic.Toast.Width"] = "200px";
        toast.Resources["Avonic.Toast.Start"] = 20.0;
        toast.Resources["Avonic.Toast.End"] = 40.0;
        toast.Resources["Avonic.Toast.BorderWidth"] = 2.0;
        toast.Resources["Avonic.Toast.BorderStyle"] = "solid";
        toast.Resources["Avonic.Toast.MinHeight"] = 80.0;
        Shown(stage, toast);

        // M16: 83, 752, 204 x 84; the content 48 high, centred in the container's 80.
        Near([83, 752, 204, 84], Box(toast.Wrapper!, stage));
        Near([85, 770, 200, 48], Box(toast.Content!, stage));
    }

    [AvaloniaFact]
    public void B4_The_Width_Stops_At_700_And_Is_Centred_On_A_Tablet()
    {
        using var stage = new Stage(1024, 768);
        var toast = Shown(stage, new AvonicToast { Message = "Tablet" });
        Near([162, 712, 700, 48], Box(toast.Wrapper!, stage));
    }

    [AvaloniaFact]
    public void B5_The_Safe_Areas_Are_Read_At_Present()
    {
        using var stage = new Stage();
        stage.Window.Resources["Avonic.SafeAreaBottom"] = 20.0;
        stage.Window.Resources["Avonic.SafeAreaTop"] = 30.0;
        var bottom = Shown(stage, new AvonicToast { Message = "Bottom" });
        Near([8, 768, 374, 48], Box(bottom.Wrapper!, stage));
        Assert.Equal(-28, bottom.LastPresentedPosition!.Bottom);
        _ = bottom.Dismiss();
        Gone(stage);

        var top = Shown(stage, new AvonicToast { Message = "Top", Position = ToastPosition.Top });
        Near([8, 38, 374, 48], Box(top.Wrapper!, stage));
    }

    [AvaloniaFact]
    public void B6_An_Anchor_Puts_It_8_Beyond_The_Anchors_Edge()
    {
        var header = new Border { Name = "Hdr", Height = 56 };
        var spacer = new Border { Height = 844 - 56 - 40 - 56 };
        var footer = new Border { Name = "Ftr", Height = 56 };
        using var stage = new Stage(390, 844, header, spacer, footer);
        stage.Render();
        Near([0, 788, 390, 56], Box(footer, stage));

        // M14: on the footer by name, 732 (-64); under the header by reference, 8 below its bottom (the stage's opener is above it).
        var bottom = Shown(stage, new AvonicToast { Message = "Above the footer", PositionAnchor = "Ftr" });
        Assert.Equal(-64, TranslateY(bottom.Wrapper!), 6);
        Near([8, 732, 374, 48], Box(bottom.Wrapper!, stage));
        Assert.Equal(96, Box(header, stage).Bottom, 6);
        _ = bottom.Dismiss();
        Gone(stage);

        var top = Shown(stage, new AvonicToast { Message = "Below the header", Position = ToastPosition.Top, PositionAnchor = header });
        Assert.Equal(104, TranslateY(top.Wrapper!), 6);
        Near([8, 104, 374, 48], Box(top.Wrapper!, stage));
        _ = top.Dismiss();
        Gone(stage);

        // Ignored in the middle; a name not found is as no anchor.
        var middle = Shown(stage, new AvonicToast { Message = "Middle", Position = ToastPosition.Middle, PositionAnchor = "Hdr" });
        Near([8, 398, 374, 48], Box(middle.Wrapper!, stage));
        _ = middle.Dismiss();
        Gone(stage);
        var missing = Shown(stage, new AvonicToast { Message = "Missing", PositionAnchor = "Nope" });
        Near([8, 788, 374, 48], Box(missing.Wrapper!, stage));
    }

    // ── B7: duration ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_A_Duration_Dismisses_With_Timeout_That_Long_After_The_Present()
    {
        using var stage = new Stage();
        var toast = Watch(stage, new AvonicToast { Message = "Timed", Duration = 1000 });
        Shown(stage, toast);
        stage.Log.Clear();
        stage.At(Entered + 999);
        Assert.Empty(stage.Log);
        stage.At(Entered + 1000);
        Assert.Equal(["ionToastWillDismiss(,timeout)@1416", "willDismiss(,timeout)@1416"], stage.Log);
    }

    [AvaloniaFact]
    public void B7_An_Earlier_Dismiss_Cancels_The_Timer_And_0_Never_Dismisses()
    {
        using var stage = new Stage();
        var toast = Watch(stage, new AvonicToast { Message = "Timed", Duration = 1000 });
        Shown(stage, toast);
        stage.At(600);
        _ = toast.Dismiss();
        Gone(stage);

        // Presented again with no duration: nothing at the old timer's time, or long after.
        toast.Duration = 0;
        Shown(stage, toast);
        stage.Log.Clear();
        stage.At(5000);
        Assert.Empty(stage.Log);
    }

    [AvaloniaFact]
    public void B7_The_Config_Is_The_Default_And_An_Apps_Value_Wins()
    {
        try
        {
            AvonicConfig.ToastDuration = 1500;
            var toast = new AvonicToast();
            Assert.Equal(1500, toast.Duration);
            toast.Duration = 200;
            Assert.Equal(200, toast.Duration);
            toast.ClearValue(AvonicToast.DurationProperty);
            Assert.Equal(1500, toast.Duration);
        }
        finally
        {
            AvonicConfig.ToastDuration = 0;
        }

        Assert.Equal(0, new AvonicToast().Duration);
    }

    // ── B8 to B10: buttons ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_Buttons_Go_In_Their_Groups_With_Their_State()
    {
        using var stage = new Stage();
        var toast = Shown(stage, new AvonicToast
        {
            Message = "Buttons",
            Buttons =
            [
                new ToastButton { Text = "Left", Side = ToastButtonSide.Start },
                "Cancel",
                new ToastButton { Text = "Go", Role = "go" },
                new ToastButton { Icon = "close", Role = "backdrop" },
                new ToastButton { Text = "Dismiss", Role = "cancel", Icon = "close" },
            ],
        });

        Assert.Equal(["Left"], toast.StartButtonControls.Select(b => b.Text));
        Assert.Equal(["Cancel", "Go", null, "Dismiss"], toast.EndButtonControls.Select(b => b.Text));
        var (plain, go, backdrop, cancel) = (toast.EndButtonControls[0], toast.EndButtonControls[1], toast.EndButtonControls[2], toast.EndButtonControls[3]);

        // A string is a plain button, with no role.
        Assert.DoesNotContain(ToastNativeButton.Cancel, plain.Classes);
        Assert.Contains(":role-go", go.Classes);
        Assert.Contains(ToastNativeButton.Cancel, backdrop.Classes);
        Assert.Contains(ToastNativeButton.IconOnly, backdrop.Classes);
        Assert.Contains(ToastNativeButton.Cancel, cancel.Classes);
        Assert.Contains(":role-cancel", cancel.Classes);
        Assert.DoesNotContain(ToastNativeButton.IconOnly, cancel.Classes);

        // The icon before the text, 19.6 for 1.4em of 14.
        var icon = cancel.GetVisualDescendants().OfType<AvonicIcon>().Single();
        var label = cancel.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Label");
        Assert.True(Box(icon, stage).Right <= Box(label, stage).Left + 0.5);
        Assert.Equal(19.6, icon.FontSize, 6);
        Assert.Equal("DISMISS", label.Text);

        // The groups: the start group before the content, the end group after it.
        Assert.True(Box(toast.StartButtonControls[0], stage).Right <= Box(toast.Content!, stage).Left + 0.5);
        Assert.True(Box(toast.Content!, stage).Right <= Box(plain, stage).Left + 0.5);
    }

    [AvaloniaFact]
    public async Task B9_A_Handler_Keeps_Or_Dismisses_And_A_Cancel_Role_Dismisses_At_Once()
    {
        using var stage = new Stage();
        var calls = new List<string>();
        var keep = new ToastButton { Text = "Keep", Handler = Log(calls, "keep", false) };
        var go = new ToastButton { Text = "Go", Role = "go", Handler = Log(calls, "go") };
        var toast = Watch(stage, new AvonicToast { Message = "Buttons", Buttons = [keep, go] });
        Shown(stage, toast);
        stage.Log.Clear();

        stage.Click(Centre(toast.EndButtonControls[0], stage));
        Assert.Equal(["keep"], calls);
        Assert.Empty(stage.Log);

        stage.Click(Centre(toast.EndButtonControls[1], stage));
        Assert.Equal(["keep", "go"], calls);
        Assert.Equal(["ionToastWillDismiss(,go)@416", "willDismiss(,go)@416"], stage.Log);
        Gone(stage);

        // A task of false keeps it; an exception dismisses.
        var gate = new TaskCompletionSource<object?>();
        var later = new ToastButton { Text = "Later", Handler = () => gate.Task };
        var boom = new ToastButton { Text = "Boom", Handler = () => throw new InvalidOperationException("boom") };
        var second = Watch(stage, new AvonicToast { Message = "Async", Buttons = [later, boom] }, "2:");
        Shown(stage, second);
        stage.Log.Clear();
        stage.Click(Centre(second.EndButtonControls[0], stage));
        gate.SetResult(false);
        await Task.Yield();
        Stage.Settle();
        Assert.Empty(stage.Log);
        stage.Click(Centre(second.EndButtonControls[1], stage));
        Assert.Contains(stage.Log, l => l.StartsWith("2:willDismiss(,)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B10_Any_Cancel_Role_Dismiss_Runs_The_First_Cancel_Buttons_Handler_Once()
    {
        using var stage = new Stage();
        var calls = new List<string>();
        AvonicToast Make() => Watch(stage, new AvonicToast
        {
            Message = "Cancel",
            Buttons =
            [
                new ToastButton { Text = "Cancel", Role = "cancel", Handler = Log(calls, "cancel", false) },
                new ToastButton { Text = "Second", Role = "cancel", Handler = Log(calls, "second") },
            ],
        });

        // Its click: dismissed at once, though the handler says false, which runs once, from the will-dismiss.
        var clicked = Shown(stage, Make());
        stage.Log.Clear();
        stage.Click(Centre(clicked.EndButtonControls[0], stage));
        Assert.Equal(["cancel"], calls);
        Assert.Equal(["ionToastWillDismiss(,cancel)@416", "willDismiss(,cancel)@416"], stage.Log);
        Gone(stage);

        foreach (var (role, expected) in new[] { ("cancel", 1), ("backdrop", 1), ("other", 0) })
        {
            calls.Clear();
            var toast = Shown(stage, Make());
            _ = toast.Dismiss("data", role);
            Gone(stage);
            Assert.Equal(expected, calls.Count);
        }
    }

    // ── B11 to B13: focus, input and stacking ───────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_It_Takes_No_Focus_Unless_KeyboardClose_And_Gives_None_Back()
    {
        using var stage = new Stage();
        stage.Opener.Focus();
        var toast = Shown(stage, new AvonicToast { Message = "Focus", Buttons = ["Undo"] });
        Assert.False(toast.KeyboardClose);
        Assert.Same(stage.Opener, stage.Focused);
        _ = toast.Dismiss();
        Gone(stage);
        Stage.Settle();
        Assert.Same(stage.Opener, stage.Focused);

        // KeyboardClose: the toast, then nothing is given back.
        var keyboard = Shown(stage, new AvonicToast { Message = "Focus", KeyboardClose = true });
        Assert.Same(keyboard, stage.Focused);
        _ = keyboard.Dismiss();
        Gone(stage);
        Stage.Settle();
        Assert.NotSame(stage.Opener, stage.Focused);
    }

    [AvaloniaFact]
    public void B11_A_Click_On_A_Toasts_Button_Over_An_Alert_Gives_Focus_Back_To_The_Alert()
    {
        using var stage = new Stage();
        var alert = new AvonicAlert { Header = "Alert", Buttons = ["OK"] };
        _ = alert.Present(stage.Window);
        stage.At(500);
        stage.Render();
        var ok = alert.GetVisualDescendants().OfType<AlertActionButton>().First();
        ok.Focus(NavigationMethod.Tab);
        Stage.Settle();
        var toast = new AvonicToast { Message = "Over the alert", Buttons = [new ToastButton { Text = "Undo", Handler = () => false }] };
        _ = toast.Present(stage.Window);
        stage.At(1000);
        stage.Render();
        Assert.Same(ok, stage.Focused);
        stage.Click(Centre(toast.EndButtonControls[0], stage));
        Assert.Same(ok, stage.Focused);
    }

    [AvaloniaFact]
    public void B12_A_Tap_Beside_It_Reaches_The_Page_And_Escape_Leaves_It()
    {
        var under = new Button { Content = "Under", Height = 40 };
        using var stage = new Stage(390, 844, new Border { Height = 360 }, under);
        var clicks = 0;
        under.Click += (_, _) => clicks++;
        var toast = Watch(stage, new AvonicToast { Message = "Pointer", Position = ToastPosition.Top });
        Shown(stage, toast);
        stage.Log.Clear();
        stage.Click(Centre(under, stage));
        Assert.Equal(1, clicks);

        // The toast's own surface is only its wrapper.
        Assert.Same(under, (stage.Window.InputHitTest(Centre(under, stage)) as Visual)?.GetSelfAndVisualAncestors().OfType<Button>().FirstOrDefault());
        stage.Key(PhysicalKey.Escape);
        stage.After(400);
        Assert.Empty(stage.Log);
    }

    [AvaloniaFact]
    public void B12_A_Toast_On_Top_Doesnt_Stop_Escape_Dismissing_The_Alert_Under_It()
    {
        using var stage = new Stage();
        var alert = new AvonicAlert { Header = "Alert", Buttons = ["OK"] };
        var dismissed = new List<string?>();
        alert.WillDismiss += (_, e) => dismissed.Add(e.Role);
        _ = alert.Present(stage.Window);
        stage.At(500);
        var toast = Shown(stage, new AvonicToast { Message = "On top" });

        // Not copied from Ionic, where the toast on top stops it (M23).
        stage.Key(PhysicalKey.Escape);
        Assert.Equal(["backdrop"], dismissed);
        Assert.True(toast.IsShown);
    }

    [AvaloniaFact]
    public void B13_It_Is_Over_Every_Other_Overlay_And_A_Later_Toast_Over_An_Earlier()
    {
        using var stage = new Stage();
        var alert = new AvonicAlert { Header = "Alert", Buttons = ["OK"] };
        _ = alert.Present(stage.Window);
        stage.At(500);
        var first = Shown(stage, new AvonicToast { Message = "First, longer" });
        var second = Shown(stage, new AvonicToast { Message = "Second" });
        Assert.True(first.ZIndex > alert.ZIndex);
        Assert.Equal(60000 + first.OverlayIndex, first.ZIndex);
        Assert.True(second.ZIndex > first.ZIndex);
        Assert.Equal(Box(first.Wrapper!, stage), Box(second.Wrapper!, stage));
    }

    // ── B14 to B16: the swipe ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A slow mouse drag from the wrapper's centre, <paramref name="dy"/> in steps of <paramref name="step"/> px, one every 120 ms,
    /// so that no velocity is measured (Ionic counts only moves under 100 ms apart), then released where it is.
    /// </summary>
    private static void Drag(Stage stage, AvonicToast toast, double dy, double step = 2)
    {
        stage.Render();
        var start = Centre(toast.Wrapper!, stage);
        stage.Window.MouseDown(start, MouseButton.Left);
        Stage.Settle();
        var steps = (int)Math.Round(Math.Abs(dy) / step);
        for (var i = 1; i <= steps; i++)
        {
            stage.After(120);
            stage.Window.MouseMove(start + new Point(0, Math.Sign(dy) * step * i));
            Stage.Settle();
            stage.After(16);
        }

        stage.After(120);
        stage.Window.MouseUp(start + new Point(0, dy), MouseButton.Left);
        Stage.Settle();
    }

    [AvaloniaFact]
    public void B14_A_Short_Swipe_Down_Follows_The_Pointer_And_Goes_Back()
    {
        using var stage = new Stage();
        var toast = Watch(stage, new AvonicToast { Message = "Swipe me", ToastSwipeGesture = ToastSwipeGestureDirection.Vertical });
        Shown(stage, toast);
        stage.Log.Clear();
        stage.Render();
        var start = Centre(toast.Wrapper!, stage);
        stage.Window.MouseDown(start, MouseButton.Left);
        for (var i = 1; i <= 10; i++)
        {
            stage.After(120);
            stage.Window.MouseMove(start + new Point(0, 2 * i));
            Stage.Settle();
            stage.After(16);

            // Nothing until 10 px; then 1:1 from where it captured (M27: -8 to 2).
            Assert.Equal(i <= 5 ? -8 : -8 + 2 * (i - 5), TranslateY(toast.Wrapper!), 3);
        }

        stage.After(120);
        stage.Window.MouseUp(start + new Point(0, 20), MouseButton.Left);
        Stage.Settle();

        // 10 of 56 is under half: back to -8, played over a 200 ms animation from where it is, so 36 ms from its first frame; then
        // the gesture works again.
        stage.After(32);
        Assert.InRange(TranslateY(toast.Wrapper!), -7.9, 1.9);
        stage.After(64);
        Assert.Equal(-8, TranslateY(toast.Wrapper!), 3);
        Assert.Empty(stage.Log);

        Drag(stage, toast, 50);
        stage.After(250);
        Assert.Contains(stage.Log, l => l.StartsWith("ionToastWillDismiss(,gesture)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B14_A_Long_Swipe_Down_Goes_Off_And_Dismisses_With_Gesture_And_No_Leave()
    {
        using var stage = new Stage();
        var toast = Watch(stage, new AvonicToast { Message = "Swipe me", ToastSwipeGesture = ToastSwipeGestureDirection.Vertical });
        Shown(stage, toast);
        stage.Log.Clear();
        Drag(stage, toast, 50);
        var released = stage.Clock.Now.TotalMilliseconds;

        // 40 of 56: out to translateY(100%), 48, played over a 200 ms animation from 0.714 of it, so 57 ms (M27: Chromium's
        // currentTime 176 of 200 after 40 ms).
        Assert.Equal(32, TranslateY(toast.Wrapper!), 3);
        stage.After(32);
        Assert.InRange(TranslateY(toast.Wrapper!), 32.1, 47.9);
        Assert.Empty(stage.Log);
        stage.After(64);

        // All four events at once: no leave animation.
        Assert.Equal(4, stage.Log.Count);
        Assert.All(stage.Log, l => Assert.Contains("(,gesture)", l));
        Assert.Single(stage.Log.Select(l => l[(l.IndexOf('@') + 1)..]).Distinct());
        Assert.True(double.Parse(stage.Log[0][(stage.Log[0].IndexOf('@') + 1)..], System.Globalization.CultureInfo.InvariantCulture) <= released + 96);
        Assert.Null(toast.GetVisualParent());
        Assert.Null(toast.SwipeGestureInstance);
    }

    [AvaloniaFact]
    public void B14_Up_At_The_Bottom_Does_Nothing_And_Up_At_The_Top_Dismisses()
    {
        using var stage = new Stage();
        var bottom = Watch(stage, new AvonicToast { Message = "Bottom", ToastSwipeGesture = ToastSwipeGestureDirection.Vertical });
        Shown(stage, bottom);
        stage.Log.Clear();
        Drag(stage, bottom, -40);
        stage.After(300);
        Assert.Empty(stage.Log);
        Assert.Equal(-8, TranslateY(bottom.Wrapper!), 3);
        _ = bottom.Dismiss();
        Gone(stage);
        stage.Log.Clear();

        var top = Watch(stage, new AvonicToast { Message = "Top", Position = ToastPosition.Top, ToastSwipeGesture = ToastSwipeGestureDirection.Vertical });
        Shown(stage, top);
        stage.Log.Clear();
        Drag(stage, top, -50);
        Assert.Equal(-32, TranslateY(top.Wrapper!), 3);
        stage.After(250);
        Assert.Contains(stage.Log, l => l.StartsWith("ionToastWillDismiss(,gesture)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B15_The_Middle_Swipes_Either_Way_With_A_Quarter_Threshold()
    {
        using var stage = new Stage();
        var toast = Watch(stage, new AvonicToast { Message = "Middle", Position = ToastPosition.Middle, ToastSwipeGesture = ToastSwipeGestureDirection.Vertical });
        Shown(stage, toast);
        stage.Log.Clear();

        // 100 px: 90 of 892 is under a quarter, so back to 0.
        Drag(stage, toast, 100, 10);
        Assert.Equal(90, TranslateY(toast.Wrapper!), 3);
        stage.After(250);
        Assert.Equal(0, TranslateY(toast.Wrapper!), 3);
        Assert.Empty(stage.Log);

        // 300 px up: out to -446, then dismissed.
        Drag(stage, toast, -300, 10);
        Assert.Equal(-290, TranslateY(toast.Wrapper!), 3);
        stage.After(100);
        Assert.InRange(TranslateY(toast.Wrapper!), -445, -291);
        stage.After(130);
        Assert.Contains(stage.Log, l => l.StartsWith("ionToastWillDismiss(,gesture)", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B16_The_Gesture_Comes_And_Goes_With_The_Property_While_Presented()
    {
        using var stage = new Stage();
        var toast = Watch(stage, new AvonicToast { Message = "No swipe" });
        Shown(stage, toast);
        stage.Log.Clear();
        Assert.Null(toast.SwipeGestureInstance);
        Drag(stage, toast, 50);
        stage.After(300);
        Assert.Empty(stage.Log);
        Assert.Equal(-8, TranslateY(toast.Wrapper!), 3);

        toast.ToastSwipeGesture = ToastSwipeGestureDirection.Vertical;
        Assert.NotNull(toast.SwipeGestureInstance);
        toast.ToastSwipeGesture = null;
        Assert.Null(toast.SwipeGestureInstance);
        toast.ToastSwipeGesture = ToastSwipeGestureDirection.Vertical;
        Drag(stage, toast, 50);
        stage.After(300);
        Assert.Contains(stage.Log, l => l.StartsWith("ionToastWillDismiss(,gesture)", StringComparison.Ordinal));

        // Not before a present.
        var later = new AvonicToast { Message = "Later", ToastSwipeGesture = ToastSwipeGestureDirection.Vertical };
        Assert.Null(later.SwipeGestureInstance);
    }

    // ── B17 to B20: the look ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_Stacked_Puts_The_Buttons_On_A_Line_Of_Their_Own_At_The_End()
    {
        using var stage = new Stage();
        var toast = Shown(stage, new AvonicToast { Message = "Short", Layout = ToastLayout.Stacked, Buttons = ["Undo"] });
        var content = Box(toast.Content!, stage);
        var undo = Box(toast.EndButtonControls[0], stage);
        Assert.Equal(374, content.Width, 1);
        Assert.True(undo.Top >= content.Bottom - 0.5);
        Assert.Equal(374, undo.Right, 1);
        Assert.Equal(Box(toast.Wrapper!, stage).Bottom - 8, undo.Bottom, 1);

        // Baseline: one row, the button 8 from the end, centred across.
        toast.Layout = ToastLayout.Baseline;
        stage.Render();
        undo = Box(toast.EndButtonControls[0], stage);
        Near([8, 788, 374, 48], Box(toast.Wrapper!, stage));
        Assert.Equal(374, undo.Right, 1);
        Assert.Equal(812, undo.Top + undo.Height / 2, 1);
    }

    [AvaloniaFact]
    public void B18_The_Colours_Are_The_Keys_And_A_Color_Takes_Over()
    {
        using var stage = new Stage();
        var toast = Shown(stage, new AvonicToast { Message = "Colours", Buttons = [new ToastButton { Text = "Undo" }, new ToastButton { Text = "Dismiss", Role = "cancel" }] });
        var wrapper = Part<Border>(toast, "PART_Wrapper");
        Assert.Equal(Color.Parse("#333333"), ColorOf(wrapper.Background));
        Assert.Equal(Color.Parse("#f2f2f2"), ColorOf(toast.Foreground));
        Assert.Equal(Color.Parse("#0054e9"), ColorOf(toast.EndButtonControls[0].Foreground));
        Assert.Equal(Color.Parse("#e6e6e6"), ColorOf(toast.EndButtonControls[1].Foreground));
        Assert.Equal(new CornerRadius(4), wrapper.CornerRadius);
        Assert.Equal(3, wrapper.BoxShadow.Count);

        toast.Color = "primary";
        Stage.Settle();
        Assert.Equal(Color.Parse("#0054e9"), ColorOf(wrapper.Background));
        Assert.Equal(Colors.White, ColorOf(toast.Foreground));
        Assert.Equal(Colors.White, ColorOf(toast.EndButtonControls[0].Foreground));
        Assert.Equal(Colors.White, ColorOf(toast.EndButtonControls[1].Foreground));

        // The variables, set on the toast.
        toast.Color = null;
        toast.Resources["Avonic.Toast.Background"] = new SolidColorBrush(Colors.Teal);
        toast.Resources["Avonic.Toast.ButtonColor"] = new SolidColorBrush(Colors.Orange);
        toast.Resources["Avonic.Toast.Color"] = new SolidColorBrush(Colors.Yellow);
        Stage.Settle();
        Assert.Equal(Colors.Teal, ColorOf(wrapper.Background));
        Assert.Equal(Colors.Orange, ColorOf(toast.EndButtonControls[0].Foreground));
        Assert.Equal(Colors.Yellow, ColorOf(toast.Foreground));

        // A colour from the start reaches the buttons it renders.
        _ = toast.Dismiss();
        Gone(stage);
        var danger = Shown(stage, new AvonicToast { Message = "Danger", Color = "danger", Buttons = [new ToastButton { Text = "Retry" }, new ToastButton { Text = "Close", Role = "cancel" }] });
        Assert.All(danger.ButtonControls, b => Assert.Equal(Colors.White, ColorOf(b.Foreground)));
    }

    [AvaloniaFact]
    public void B19_A_Mouse_Over_A_Button_Tints_It_And_An_Icon_Only_Ripple_Is_Unbounded()
    {
        using var stage = new Stage();
        var toast = Shown(stage, new AvonicToast { Message = "Hover", Buttons = [new ToastButton { Text = "Undo" }, new ToastButton { Text = "Dismiss", Role = "cancel" }, new ToastButton { Icon = "close" }] });
        var (undo, dismiss, close) = (toast.EndButtonControls[0], toast.EndButtonControls[1], toast.EndButtonControls[2]);
        Assert.Equal(Colors.Transparent, ColorOf(undo.Background));

        stage.Window.MouseMove(Centre(undo, stage));
        Stage.Settle();
        var hover = Assert.IsAssignableFrom<ISolidColorBrush>(undo.Background);
        Assert.Equal(Color.Parse("#0054e9"), hover.Color);
        Assert.Equal(0.08, hover.Opacity, 6);

        stage.Window.MouseMove(Centre(dismiss, stage));
        Stage.Settle();
        Assert.Equal(Colors.Transparent, ColorOf(undo.Background));
        var cancelHover = Assert.IsAssignableFrom<ISolidColorBrush>(dismiss.Background);
        Assert.Equal(Colors.White, cancelHover.Color);
        Assert.Equal(0.08, cancelHover.Opacity, 6);

        Assert.Equal(RippleEffectType.Bounded, undo.GetVisualDescendants().OfType<AvonicRippleEffect>().Single().Type);
        Assert.Equal(RippleEffectType.Unbounded, close.GetVisualDescendants().OfType<AvonicRippleEffect>().Single().Type);
        Assert.Equal(new Size(36, 36), close.Bounds.Size);
    }

    [AvaloniaFact]
    public void B20_White_Space_Collapses_And_Wraps_As_CSS_Says()
    {
        Assert.Equal("a b c", ToastContent.WhiteSpaceText.Convert(["  a \t b\n\nc  ", "normal"], typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("a b c", ToastContent.WhiteSpaceText.Convert(["a  b\nc", null], typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("a b\nc", ToastContent.WhiteSpaceText.Convert(["a   b \n  c", "pre-line"], typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("a   b \n c", ToastContent.WhiteSpaceText.Convert(["a   b \n c", "pre"], typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));

        using var stage = new Stage();
        var toast = Shown(stage, new AvonicToast { Message = "Line one\nLine  two", Header = "  The   header " });
        var message = Part<TextBlock>(toast, "PART_Message");
        Assert.Equal("Line one Line two", message.Text);
        Assert.Equal("The header", Part<TextBlock>(toast, "PART_Header").Text);
        Assert.Equal(TextWrapping.Wrap, message.TextWrapping);

        toast.Resources["Avonic.Toast.WhiteSpace"] = "nowrap";
        Stage.Settle();
        Assert.Equal(TextWrapping.NoWrap, message.TextWrapping);
        toast.Resources["Avonic.Toast.WhiteSpace"] = "pre-line";
        Stage.Settle();
        Assert.Equal("Line one\nLine two", message.Text);
        Assert.Equal(TextWrapping.Wrap, message.TextWrapping);
    }

    // ── B21: automation and keys ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B21_The_Content_Is_A_Live_Region_Shown_To_Automation_Once_It_Has_Entered()
    {
        using var stage = new Stage();
        var toast = new AvonicToast { Header = "H", Message = "M", Icon = "globe" };
        _ = toast.Present(stage.Window);
        Stage.Settle();
        var header = Part<TextBlock>(toast, "PART_Header");
        var message = Part<TextBlock>(toast, "PART_Message");
        Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(toast.Content!));
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(header));
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(message));
        stage.At(Entered);
        Assert.Equal(AccessibilityView.Content, AutomationProperties.GetAccessibilityView(header));
        Assert.Equal(AccessibilityView.Content, AutomationProperties.GetAccessibilityView(message));
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(Part<AvonicIcon>(toast, "PART_Icon")));

        _ = toast.Dismiss();
        Gone(stage);
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(message));
    }

    [AvaloniaFact]
    public void B21_Buttons_Are_Named_And_Invokable_And_Tab_Reaches_Them_After_The_Page()
    {
        using var stage = new Stage();
        var calls = new List<string>();
        var toast = Shown(stage, new AvonicToast
        {
            Message = "Keys",
            HtmlAttributes = new Dictionary<string, object?> { ["id"] = "my-toast", ["aria-label"] = "A toast" },
            Buttons =
            [
                new ToastButton { Text = "Undo", Handler = Log(calls, "undo", false) },
                new ToastButton { Icon = "close", HtmlAttributes = new Dictionary<string, object?> { ["aria-label"] = "Close the toast" }, Handler = Log(calls, "close", false) },
            ],
        });
        var (undo, close) = (toast.EndButtonControls[0], toast.EndButtonControls[1]);
        var undoPeer = ControlAutomationPeer.CreatePeerForElement(undo);
        Assert.Equal("Undo", undoPeer.GetName());
        Assert.Equal(AutomationControlType.Button, undoPeer.GetAutomationControlType());
        Assert.Equal("Close the toast", ControlAutomationPeer.CreatePeerForElement(close).GetName());
        ((IInvokeProvider)undoPeer).Invoke();
        Stage.Settle();
        Assert.Equal(["undo"], calls);

        var toastPeer = ControlAutomationPeer.CreatePeerForElement(toast);
        Assert.Equal("A toast", toastPeer.GetName());
        Assert.Equal("my-toast", toastPeer.GetAutomationId());
        Assert.False(KeyboardNavigation.GetIsTabStop(toast));

        // Tab from the page's last stop: the toast's buttons, as their tabindex="0" asks (not copied: Chromium skips them).
        stage.Opener.Focus(NavigationMethod.Tab);
        stage.Key(PhysicalKey.Tab);
        Assert.Same(undo, stage.Focused);
        Assert.Contains(FocusVisible.FocusedPseudoClass, undo.Classes);
        stage.Key(PhysicalKey.Tab);
        Assert.Same(close, stage.Focused);
        stage.Key(PhysicalKey.Enter);
        Assert.Equal(["undo", "close"], calls);
    }

    // ── B22 to B24: inline, a render while presented, classes ──────────────────────────────

    [AvaloniaFact]
    public void B22_An_Inline_Toast_Opens_From_Its_Trigger_And_Goes_Back_In_Its_Place()
    {
        using var stage = new Stage();
        var scope = new NameScope();
        scope.Register("Opener", stage.Opener);
        NameScope.SetNameScope(stage.Page, scope);
        var toast = new AvonicToast { Message = "Inline", Trigger = "Opener" };
        stage.Page.Children.Add(toast);
        stage.Render();
        Assert.False(toast.IsShown);
        stage.Click(Centre(stage.Opener, stage));
        stage.After(Entered);
        Assert.True(toast.IsShown);
        Assert.IsNotType<StackPanel>(toast.Parent);
        _ = toast.Dismiss();
        Gone(stage);
        Assert.Same(stage.Page, toast.Parent);

        // IsOpen, at the top.
        toast.Position = ToastPosition.Top;
        toast.IsOpen = true;
        stage.After(Entered);
        stage.Render();
        Near([8, 8, 374, 48], Box(toast.Wrapper!, stage));
        toast.IsOpen = false;
        Gone(stage);
        Assert.False(toast.IsShown);
    }

    [AvaloniaFact]
    public void B23_A_Change_While_Presented_Renders_Again()
    {
        using var stage = new Stage();
        var buttons = new Avalonia.Collections.AvaloniaList<ToastButton> { "One" };
        var toast = Shown(stage, new AvonicToast { Message = "Before", Header = "H", Buttons = buttons });
        var first = toast.EndButtonControls[0];
        first.Focus();
        buttons.Add("Two");
        buttons.Add(new ToastButton { Text = "Three", Side = ToastButtonSide.Start });
        Assert.Single(toast.EndButtonControls);
        Stage.Settle();
        Assert.Equal(["One", "Two"], toast.EndButtonControls.Select(b => b.Text));
        Assert.Equal(["Three"], toast.StartButtonControls.Select(b => b.Text));
        Assert.Same(first, toast.EndButtonControls[0]);
        Assert.Same(first, stage.Focused);

        toast.Message = "After";
        toast.Header = null;
        toast.Icon = "globe";
        toast.Color = "success";
        toast.Layout = ToastLayout.Stacked;
        stage.Render();
        Assert.Equal("After", Part<TextBlock>(toast, "PART_Message").Text);
        Assert.False(Part<TextBlock>(toast, "PART_Header").IsVisible);
        Assert.True(Part<AvonicIcon>(toast, "PART_Icon").IsVisible);
        Assert.All(toast.ButtonControls, b => Assert.Contains(ToastNativeButton.InColor, b.Classes));
        Assert.True(Box(toast.EndButtonControls[0], stage).Top >= Box(toast.Content!, stage).Bottom - 0.5);
        Assert.True(Box(toast.StartButtonControls[0], stage).Bottom <= Box(toast.Content!, stage).Top + 0.5);

        // A new position moves the wrapper by its class, and keeps the transform of the present, as Ionic's inline style.
        toast.Position = ToastPosition.Top;
        stage.Render();
        Assert.Equal(-8, TranslateY(toast.Wrapper!), 6);
        Assert.Equal(-8, Box(toast.Wrapper!, stage).Top, 1);
    }

    [AvaloniaFact]
    public void B24_CssClass_Is_Style_Classes_And_Header_And_Message_Show_Only_When_Set()
    {
        using var stage = new Stage();
        var toast = Shown(stage, new AvonicToast { CssClass = "one two" });
        Assert.Contains("one", toast.Classes);
        Assert.Contains("two", toast.Classes);
        toast.CssClass = "three";
        Assert.DoesNotContain("one", toast.Classes);
        Assert.Contains("three", toast.Classes);

        // No header or message: the content is its padding, 28.
        Assert.False(Part<TextBlock>(toast, "PART_Header").IsVisible);
        Assert.False(Part<TextBlock>(toast, "PART_Message").IsVisible);
        Assert.False(Part<AvonicIcon>(toast, "PART_Icon").IsVisible);
        Near([8, 808, 374, 28], Box(toast.Wrapper!, stage));
        toast.Message = "Now";
        stage.Render();
        Near([8, 788, 374, 48], Box(toast.Wrapper!, stage));
    }
}
