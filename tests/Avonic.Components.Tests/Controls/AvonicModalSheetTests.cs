using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;
using static Avonic.Components.Tests.Gestures.GestureTests;
using Stage = Avonic.Components.Tests.Controls.AvonicModalTests.Stage;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ion-modal's sheet, md, on the test clock: S1 to S16 in docs/components/ion-modal/PLAN.md, "The sheet", from <c>modal.js</c>,
/// <c>gestures/sheet.js</c>, <c>animations/sheet.js</c> and the measurements in <c>ref/sheet-measurements.txt</c>. Breakpoints
/// 0, 0.25, 0.5, 0.75 and 1, opening at 0.5, at 390 x 844 unless said: the wrapper is 834 high, 10 below the top, and 0.5 holds it
/// 417 down. The present's enter (280 ms, played at 0) finishes at the frame at 304, and the gesture takes over then.
/// </summary>
public class AvonicModalSheetTests
{
    private const double Entered = 304;
    private const double Height = 834;

    private static readonly CubicBezierEasing EnterEasing = new(0.36, 0.66, 0.04, 1);
    private static readonly CubicBezierEasing LeaveEasing = new(0.47, 0, 0.745, 0.715);
    private static readonly CubicBezierEasing SnapEasing = new(0.32, 0.72, 0, 1);

    /// <summary>A header 56 high, then a content holding 40 rows, as the measured page has.</summary>
    private static AvonicModal Sheet(double? initial = 0.5, AvaloniaList<double>? breakpoints = null, Action<AvonicModal>? set = null)
    {
        var rows = new StackPanel();
        for (var i = 0; i < 40; i++)
        {
            rows.Children.Add(new Border { Height = 48, Background = Brushes.Transparent, Focusable = true, Name = $"Row{i}" });
        }

        var content = new AvonicContent { Content = rows };
        var header = new AvonicHeader { Height = 56, Name = "Header" };
        var page = new DockPanel { Children = { header, content } };
        DockPanel.SetDock(header, Dock.Top);
        var modal = new AvonicModal
        {
            Breakpoints = breakpoints ?? [0, 0.25, 0.5, 0.75, 1],
            InitialBreakpoint = initial,
            Content = page,
        };
        set?.Invoke(modal);
        return modal;
    }

    private static AvonicModal Presented(Stage stage, AvonicModal? modal = null)
    {
        modal ??= Sheet();
        modal.Present(stage.Window);
        stage.At(Entered);
        stage.After(16);
        return modal;
    }

    private static double TranslateY(Visual element) => element.RenderTransform?.Value.M32 ?? 0;

    private static AvonicContent ContentOf(AvonicModal modal) => ((DockPanel)modal.Content!).Children.OfType<AvonicContent>().Single();

    private static void Near(double expected, double actual, double tolerance = 1e-3) =>
        Assert.True(Math.Abs(expected - actual) <= tolerance, $"expected {expected}, was {actual}");

    /// <summary>A finger on the sheet, 8 px a move, 16 ms apart, from (195, <paramref name="y"/>).</summary>
    private sealed class Drag
    {
        private readonly Stage _stage;
        private readonly Finger _finger;
        private double _y;

        public Drag(Stage stage, double y)
        {
            _stage = stage;
            _y = y;
            At = stage.Clock.Now.TotalMilliseconds + 16;
            _finger = new Finger(stage.Scene, stage.Clock).Down(195, y, At);
        }

        public double At { get; private set; }

        public Drag By(double dy, double step = 8)
        {
            var target = _y + dy;
            while (Math.Abs(target - _y) > 1e-9)
            {
                _y += Math.Sign(dy) * Math.Min(step, Math.Abs(target - _y));
                At += 16;
                _finger.Move(195, _y, At);
            }

            return this;
        }

        /// <summary>Holds still for a frame, so the last move's frame has run.</summary>
        public Drag Hold()
        {
            At += 16;
            _stage.At(At);
            return this;
        }

        public double Lift()
        {
            At += 16;
            _finger.Up(195, _y, At);
            Stage.Settle();
            return At;
        }
    }

    // ── S1: a sheet's box ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S1_A_Sheet_Sits_At_The_Bottom_10_Below_The_Top_At_Its_Breakpoint()
    {
        using var stage = new Stage();
        var modal = Presented(stage);

        Assert.True(((IPseudoClasses)modal.Classes).Contains(AvonicModal.Sheet));
        var wrapper = modal.Wrapper!;
        Assert.Equal(new Rect(0, 10, 390, Height), wrapper.Bounds);
        Near(417, TranslateY(wrapper));
        Assert.Equal(0.5, modal.GetCurrentBreakpoint());
    }

    [AvaloniaFact]
    public void S1_On_A_Tablet_It_Is_600_Wide_Centred_And_758_High()
    {
        using var stage = new Stage(1024, 768);
        var modal = Presented(stage);

        Assert.Equal(new Rect(212, 10, 600, 758), modal.Wrapper!.Bounds);
        Near(379, TranslateY(modal.Wrapper!));
        Assert.Equal(new CornerRadius(2), modal.Wrapper!.CornerRadius);
    }

    [AvaloniaFact]
    public void S1_The_Roots_Top_Safe_Area_Is_Left_Out_Of_Its_Height_And_Followed()
    {
        using var stage = new Stage();
        stage.Window.Resources["Avonic.SafeAreaTop"] = 24d;
        var modal = Presented(stage);

        Assert.Equal(new Rect(0, 34, 390, 810), modal.Wrapper!.Bounds);
        stage.Window.Resources["Avonic.SafeAreaTop"] = 30d;
        stage.Window.UpdateLayout();
        Assert.Equal(new Rect(0, 40, 390, 804), modal.Wrapper!.Bounds);
    }

    [AvaloniaFact]
    public void S1_An_Apps_Height_Wins_And_Without_Both_Props_It_Is_Not_A_Sheet()
    {
        using var stage = new Stage();
        var modal = Sheet();
        modal.Resources["Avonic.Modal.Height"] = 500d;
        Presented(stage, modal);
        Assert.Equal(new Rect(0, 344, 390, 500), modal.Wrapper!.Bounds);
        modal.Dismiss();
        stage.After(240);

        var plain = Sheet(set: m => m.Breakpoints = null);
        Presented(stage, plain);
        Assert.False(((IPseudoClasses)plain.Classes).Contains(AvonicModal.Sheet));
        Assert.Equal(new Rect(0, 0, 390, 844), plain.Wrapper!.Bounds);
        Assert.False(plain.HandlePart!.IsVisible);
    }

    // ── S2, S3: md's sheet animations ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S2_The_Enter_Rises_To_The_Breakpoint_With_The_Backdrop_And_The_Gesture_Then_Holds_It()
    {
        using var stage = new Stage();
        var modal = Sheet();
        modal.Present(stage.Window);
        Stage.Settle();
        var wrapper = modal.Wrapper!;
        var backdrop = modal.Backdrop!;
        Near(Height, TranslateY(wrapper));
        Assert.Equal(1, wrapper.Opacity, 6);
        Assert.Equal(0, backdrop.Opacity, 6);

        // Measured at 29.1 ms (local 13.4): 784.4 and 0.019.
        foreach (var local in new[] { 16.0, 64, 128, 224 })
        {
            stage.At(16 + local);
            var e = EnterEasing.Ease(local / 280);
            Near(Height * (1 - 0.5 * e), TranslateY(wrapper));
            Near(0.16 * e, backdrop.Opacity, 1e-4);
            Assert.False(backdrop.IsHitTestVisible);
        }

        // Then the gesture's animation holds it, linear from 0.32 to 0.01: 0.165 at 0.5, as measured.
        stage.At(Entered);
        stage.After(16);
        Near(417, TranslateY(wrapper));
        Assert.Equal(0.165, backdrop.Opacity, 4);
        Assert.True(backdrop.IsHitTestVisible);
        Assert.Equal(1, wrapper.Opacity, 6);
    }

    [AvaloniaFact]
    public void S3_The_Leave_Falls_From_The_Breakpoint_With_Mds_Curve()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var start = stage.Clock.Now.TotalMilliseconds;
        modal.Dismiss();
        Stage.Settle();
        Near(417, TranslateY(modal.Wrapper!));
        Assert.Equal(0.16, modal.Backdrop!.Opacity, 4);

        foreach (var local in new[] { 48.0, 112, 176 })
        {
            stage.At(start + 16 + local);
            var e = LeaveEasing.Ease(local / 200);
            Near(417 + 417 * e, TranslateY(modal.Wrapper!));
            Near(0.16 * (1 - e), modal.Backdrop!.Opacity, 1e-4);
        }

        stage.At(start + 240);
        Assert.Null(modal.GetVisualParent());
        Assert.Null(modal.GetCurrentBreakpoint());
    }

    // ── S4, S5: the drag and the snap ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S4_A_Drag_Moves_The_Sheet_With_The_Finger_And_Raises_The_Drag_Events()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var events = new List<string>();
        var moves = new List<ModalDragEventArgs>();
        modal.IonDragStart += (_, _) => events.Add("start");
        modal.IonDragMove += (_, e) => moves.Add(e);

        // From the header, 20 below the sheet's top; the gesture captures past 10 px, at 16 up.
        var drag = new Drag(stage, 447).By(-16);
        Assert.Equal(["start"], events);
        drag.By(-40).Hold();

        var wrapper = modal.Wrapper!;
        var delta = moves[^1].DeltaY;
        Near(-40, delta);
        Near(417 + delta, TranslateY(wrapper));

        // The backdrop is linear on the full run, 0.32 to 0.01.
        var step = 0.5 + delta / Height;
        Near(0.32 + (0.01 - 0.32) * step, modal.Backdrop!.Opacity, 1e-4);
        Near(Math.Round(0.5 - delta / Height, 3), moves[^1].Progress);

        // The snap projects the speed 350 ms on: 0.5 px/ms up, so 0.75 already.
        var projected = Math.Round(0.5 - (delta + moves[^1].VelocityY * 350) / Height, 3);
        Assert.Equal(new[] { 0, 0.25, 0.5, 0.75, 1 }.MinBy(b => Math.Abs(b - projected)), moves[^1].SnapBreakpoint);
        Assert.Equal(0.75, moves[^1].SnapBreakpoint);
        Assert.Null(moves[^1].IsDismissing);
        Assert.Equal(modal, moves[^1].Source);
        drag.Lift();
    }

    [AvaloniaFact]
    public void S5_The_Release_Snaps_To_The_Projected_Breakpoint_Over_500ms_And_Then_Says_So()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        ModalDragEventArgs? end = null;
        var changes = new List<(double Breakpoint, double At)>();
        modal.IonDragEnd += (_, e) => end = e;
        modal.IonBreakpointDidChange += (_, e) => changes.Add((e.Breakpoint, stage.Clock.Now.TotalMilliseconds));

        var drag = new Drag(stage, 447).By(-120);
        var released = drag.Lift();
        Assert.NotNull(end);

        // The snap is the breakpoint nearest where the drag would be 350 ms on.
        var projected = 0.5 - (end!.DeltaY + end.VelocityY * 350) / Height;
        var snap = new[] { 0, 0.25, 0.5, 0.75, 1 }.MinBy(b => Math.Abs(b - Math.Round(projected, 3)));
        Assert.Equal(snap, end.SnapBreakpoint);
        Assert.Equal(0.75, snap);
        Assert.False(end.IsDismissing);

        // It plays from where it was let go, at the next frame.
        var from = 417 + end.DeltaY;
        var to = Height * 0.25;
        Near(from, TranslateY(modal.Wrapper!));
        foreach (var local in new[] { 96.0, 240, 400 })
        {
            stage.At(released + 16 + local);
            Near(from + (to - from) * SnapEasing.Ease(local / 500), TranslateY(modal.Wrapper!));
        }

        stage.At(released + 16 + 496);
        Assert.Empty(changes);
        stage.At(released + 16 + 512 + 16);
        Assert.Equal([(0.75, released + 16 + 512 + 16)], changes);
        Assert.Equal(0.75, modal.GetCurrentBreakpoint());
        Near(to, TranslateY(modal.Wrapper!));

        // Held at the new breakpoint: 0.32 + (0.01 - 0.32) * 0.25 = 0.2425, as measured.
        Assert.Equal(0.2425, modal.Backdrop!.Opacity, 4);
    }

    [AvaloniaFact]
    public void S5_A_Short_Slow_Drag_Snaps_Back_And_Changes_Nothing()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var changes = 0;
        modal.IonBreakpointDidChange += (_, _) => changes++;

        var drag = new Drag(stage, 447).By(48, step: 4);
        drag.Hold().Hold().Hold().Hold();
        var released = drag.Lift();
        stage.At(released + 600);

        Assert.Equal(0.5, modal.GetCurrentBreakpoint());
        Near(417, TranslateY(modal.Wrapper!));
        Assert.Equal(0, changes);
    }

    [AvaloniaFact]
    public void S5_The_Gesture_Is_Off_While_It_Snaps()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var starts = 0;
        modal.IonDragStart += (_, _) => starts++;

        var released = new Drag(stage, 447).By(-120).Lift();
        stage.At(released + 100);
        var second = new Drag(stage, 300).By(40);
        Assert.Equal(1, starts);
        second.Lift();
    }

    // ── S6, S7: down to 0 ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S6_Down_To_0_Dismisses_With_The_Gesture_Role_And_No_Leave_Animation()
    {
        using var stage = new Stage();
        var modal = stage.Watch(Presented(stage));
        ModalDragEventArgs? end = null;
        var log = stage.Log;
        modal.IonDragEnd += (_, e) => end = e;
        modal.IonBreakpointDidChange += (_, e) => log.Add($"ionBreakpointDidChange({e.Breakpoint})@{stage.Clock.Now.TotalMilliseconds}");
        log.Clear();

        var released = new Drag(stage, 447).By(336, step: 24).Lift();
        Assert.Equal(0, end!.SnapBreakpoint);
        Assert.True(end.IsDismissing);

        // A dismiss on its way down is refused.
        var refused = modal.Dismiss(null, "cancel");
        stage.At(released + 100);
        Assert.True(refused.IsCompletedSuccessfully);
        Assert.False(refused.Result);

        stage.At(released + 16 + 512);
        var finished = released + 16 + 512;
        Assert.Equal(
        [
            $"ionBreakpointDidChange(0)@{finished}", $"ionModalWillDismiss(,gesture)@{finished}", $"willDismiss(,gesture)@{finished}",
            $"ionModalDidDismiss(,gesture)@{finished}", $"didDismiss(,gesture)@{finished}",
        ], log);
        Assert.Null(modal.GetVisualParent());
    }

    [AvaloniaFact]
    public void S7_CanDismiss_False_Springs_Past_The_Second_Breakpoint_And_Snaps_Back()
    {
        using var stage = new Stage();
        var modal = Presented(stage, Sheet(set: m => m.CanDismiss = false));
        var moves = new List<ModalDragEventArgs>();
        modal.IonDragMove += (_, e) => moves.Add(e);

        var drag = new Drag(stage, 447).By(400, step: 24).Hold();
        var delta = moves[^1].DeltaY;

        // Past 1 - 0.25 = 0.75 the spring: 0.75 + calculateSpringStep((step - 0.75) / 0.2).
        var step = 0.5 + delta / Height;
        Assert.True(step > 0.75);
        var expected = 0.75 + ModalSheet.CalculateSpringStep((step - 0.75) / (0.95 - 0.75));
        Near(Height * expected, TranslateY(modal.Wrapper!));
        Assert.True(TranslateY(modal.Wrapper!) < Height * 0.95);

        var released = drag.Lift();
        stage.At(released + 600);
        Assert.Equal(0.5, modal.GetCurrentBreakpoint());
        Near(417, TranslateY(modal.Wrapper!));
        Assert.NotNull(modal.GetVisualParent());
    }

    [AvaloniaFact]
    public void S7_A_CanDismiss_Function_Is_Asked_With_Gesture_And_True_Dismisses_With_Handler_After_The_Snap_Back()
    {
        using var stage = new Stage();
        var asked = new List<string?>();
        var answer = new TaskCompletionSource<bool>();
        var modal = stage.Watch(Presented(stage, Sheet(set: m => m.CanDismiss = new ModalCanDismiss((_, role) =>
        {
            asked.Add(role);
            return answer.Task;
        }))));
        stage.Log.Clear();

        var released = new Drag(stage, 447).By(400, step: 24).Lift();
        Assert.Equal(["gesture"], asked);
        answer.SetResult(true);
        Stage.Settle();

        // Not until the snap back has finished, then the md leave from where it is.
        stage.At(released + 400);
        Assert.Empty(stage.Log);
        stage.At(released + 16 + 512 + 240);
        Assert.Contains($"ionModalWillDismiss(,handler)@{released + 16 + 512}", stage.Log);
        Assert.Null(modal.GetVisualParent());
    }

    // ── S8: setCurrentBreakpoint ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S8_SetCurrentBreakpoint_Moves_There_Over_500ms_And_Completes_After_The_Event()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var order = new List<string>();
        modal.IonBreakpointDidChange += (_, e) => order.Add($"event {e.Breakpoint}@{stage.Clock.Now.TotalMilliseconds}");
        var start = stage.Clock.Now.TotalMilliseconds;

        var moved = modal.SetCurrentBreakpoint(1);
        _ = moved.ContinueWith(_ => order.Add("done"), TaskScheduler.FromCurrentSynchronizationContext());
        Stage.Settle();
        Near(417, TranslateY(modal.Wrapper!));
        Assert.Equal(0.16, modal.Backdrop!.Opacity, 4);

        // Measured: 383.9, 347.1 at 21 and 38 ms; here at the clock's frames.
        foreach (var local in new[] { 16.0, 128, 256 })
        {
            stage.At(start + 16 + local);
            var e = SnapEasing.Ease(local / 500);
            Near(417 * (1 - e), TranslateY(modal.Wrapper!));
            Near(0.16 + 0.16 * e, modal.Backdrop!.Opacity, 1e-4);
        }

        stage.At(start + 16 + 512 + 16);
        Assert.Equal([$"event 1@{start + 544}", "done"], order);
        Assert.Equal(1, modal.GetCurrentBreakpoint());
        Assert.Equal(0.32, modal.Backdrop!.Opacity, 4);
    }

    [AvaloniaFact]
    public void S8_SetCurrentBreakpoint_Does_Nothing_For_The_Current_One_One_Not_Listed_Or_A_Modal_That_Is_Not_A_Sheet()
    {
        var sink = new WarningSink();
        var previous = Logger.Sink;
        Logger.Sink = sink;
        try
        {
            using var stage = new Stage();
            var modal = Presented(stage);
            Assert.True(modal.SetCurrentBreakpoint(0.5).IsCompleted);
            Assert.True(modal.SetCurrentBreakpoint(0.6).IsCompleted);
            stage.After(600);
            Assert.Equal(0.5, modal.GetCurrentBreakpoint());
            Assert.Contains(sink.Messages, m => m.Contains("invalid breakpoint value 0.6"));

            var plain = new AvonicModal { Content = new TextBlock() };
            Assert.True(plain.SetCurrentBreakpoint(0.5).IsCompleted);
            Assert.Contains(sink.Messages, m => m.Contains("only supported on sheet modals"));
        }
        finally
        {
            Logger.Sink = previous;
        }
    }

    [AvaloniaFact]
    public void S8_SetCurrentBreakpoint_0_Dismisses_With_Gesture_And_Not_Animated_Moves_At_Once()
    {
        using var stage = new Stage();
        var modal = Presented(stage, Sheet(set: m => m.Animated = false));
        var start = stage.Clock.Now.TotalMilliseconds;
        modal.SetCurrentBreakpoint(0.25);
        stage.At(start + 16);
        Near(Height * 0.75, TranslateY(modal.Wrapper!));
        stage.At(start + 48);
        Assert.Equal(0.25, modal.GetCurrentBreakpoint());

        var roles = new List<string?>();
        modal.IonModalDidDismiss += (_, e) => roles.Add(e.Role);
        modal.SetCurrentBreakpoint(0);
        stage.After(48);
        Assert.Equal(["gesture"], roles);
        Assert.Null(modal.GetCurrentBreakpoint());
    }

    // ── S9: the handle ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S9_The_Handle_Is_36_By_5_At_The_Top_Centred_In_Its_Colour()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var handle = modal.HandlePart!;

        Assert.True(handle.IsEffectivelyVisible);
        var box = handle.TransformToVisual(stage.Window)!.Value;
        var rect = new Rect(handle.Bounds.Size).TransformToAABB(box);
        Assert.Equal(new Rect(177, 432, 36, 5), rect);
        Assert.Equal(new CornerRadius(8), handle.CornerRadius);
        Assert.Equal(Color.Parse("#C0C0BE"), ((ISolidColorBrush)handle.Background!).Color);

        var peer = ControlAutomationPeer.CreatePeerForElement(handle);
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("Activate to adjust the size of the dialog overlaying the screen", peer.GetName());
    }

    [AvaloniaFact]
    public void S9_Handle_False_Hides_It()
    {
        using var stage = new Stage();
        var modal = Presented(stage, Sheet(set: m => m.Handle = false));
        Assert.False(modal.HandlePart!.IsVisible);
        Assert.False(KeyboardNavigation.GetIsTabStop(modal));
    }

    [AvaloniaFact]
    public void S9_Clicking_The_Handle_Cycles_Through_The_Breakpoints_Other_Than_0()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var seen = new List<double>();
        foreach (var _ in Enumerable.Range(0, 5))
        {
            var handle = modal.HandlePart!;
            var centre = handle.TranslatePoint(new Point(18, 2.5), stage.Window)!.Value;
            stage.Click(centre);
            stage.After(560);
            seen.Add(modal.GetCurrentBreakpoint()!.Value);
        }

        Assert.Equal([0.75, 1, 0.25, 0.5, 0.75], seen);
    }

    [AvaloniaFact]
    public void S9_Its_Tap_Area_Is_4_Px_Around_It()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var handle = modal.HandlePart!;
        stage.Render();

        foreach (var (x, y, hit) in new[] { (-3.5, 2.0, true), (39.5, 2, true), (18, -3.5, true), (18, 8.5, true), (-4.5, 2, false), (40.5, 2, false), (18, -4.5, false), (18, 9.5, false) })
        {
            var point = handle.TranslatePoint(new Point(x, y), stage.Window)!.Value;
            var found = stage.Window.InputHitTest(point) as Visual;
            Assert.True(hit == (found is not null && (found == handle || handle.IsVisualAncestorOf(found))), $"{x}, {y}: {found}");
        }
    }

    [AvaloniaFact]
    public void S9_Enter_On_The_Handle_Cycles_And_None_Does_Nothing_And_Is_No_Tab_Stop()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        Assert.True(KeyboardNavigation.GetIsTabStop(modal.HandlePart!));
        Assert.True(KeyboardNavigation.GetIsTabStop(modal));
        modal.HandlePart!.Focus();
        stage.Key(PhysicalKey.Enter);
        stage.After(560);
        Assert.Equal(0.75, modal.GetCurrentBreakpoint());

        modal.HandleBehavior = ModalHandleBehavior.None;
        Assert.False(KeyboardNavigation.GetIsTabStop(modal.HandlePart!));
        Assert.False(KeyboardNavigation.GetIsTabStop(modal));
        var centre = modal.HandlePart!.TranslatePoint(new Point(18, 2.5), stage.Window)!.Value;
        stage.Click(centre);
        stage.After(560);
        Assert.Equal(0.75, modal.GetCurrentBreakpoint());
    }

    [AvaloniaFact]
    public void S9_A_Click_On_The_Handle_While_It_Moves_Does_Nothing()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var start = stage.Clock.Now.TotalMilliseconds;
        modal.SetCurrentBreakpoint(1);
        stage.At(start + 200);
        var centre = modal.HandlePart!.TranslatePoint(new Point(18, 2.5), stage.Window)!.Value;
        stage.Click(centre);
        stage.After(1200);
        Assert.Equal(1, modal.GetCurrentBreakpoint());
    }

    // ── S10: backdropBreakpoint ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S10_At_Or_Below_The_Backdrop_Breakpoint_The_Page_Takes_Input_And_Focus_Can_Leave()
    {
        using var stage = new Stage();
        var clicks = 0;
        stage.Opener.Click += (_, _) => clicks++;
        var modal = Sheet(set: m => m.BackdropBreakpoint = 0.5);
        modal.Present(stage.Window);
        stage.At(64);

        // The enter: no backdrop, and nothing on its pointer-events.
        Assert.Equal(0, modal.Backdrop!.Opacity, 6);
        stage.At(Entered);
        stage.After(16);
        Assert.Equal(0, modal.Backdrop!.Opacity, 6);
        Assert.False(modal.Backdrop!.IsHitTestVisible);

        stage.Click(new Point(30, 20));
        Assert.Equal(1, clicks);
        Assert.NotNull(modal.GetVisualParent());
        stage.Opener.Focus();
        Stage.Settle();
        Assert.Same(stage.Opener, stage.Focused);

        // Above it: the backdrop at 0.32 x getBackdropValueForSheet(0.75, 0.5) = 0.16, and it takes the click.
        var start = stage.Clock.Now.TotalMilliseconds;
        modal.SetCurrentBreakpoint(0.75);
        stage.At(start + 560);
        Assert.Equal(0.16, modal.Backdrop!.Opacity, 4);
        Assert.True(modal.Backdrop!.IsHitTestVisible);
        stage.Click(new Point(30, 20));
        Assert.Equal(1, clicks);
        stage.After(240);
        Assert.Null(modal.GetVisualParent());
    }

    [AvaloniaFact]
    public void S10_Below_The_Backdrop_Breakpoint_The_Focus_Trap_Is_Off_And_Above_It_On()
    {
        using var stage = new Stage();
        var modal = Presented(stage, Sheet(set: m => m.BackdropBreakpoint = 0.5));
        Assert.False(modal.FocusTrapEnabled);
        var start = stage.Clock.Now.TotalMilliseconds;
        modal.SetCurrentBreakpoint(0.75);
        stage.At(start + 560);
        Assert.True(modal.FocusTrapEnabled);
    }

    [AvaloniaFact]
    public void S10_ShowBackdrop_False_Never_Blocks_The_Page()
    {
        using var stage = new Stage();
        var clicks = 0;
        stage.Opener.Click += (_, _) => clicks++;
        var modal = Presented(stage, Sheet(1, set: m => m.ShowBackdrop = false));
        Assert.False(modal.Backdrop!.IsHitTestVisible);
        Assert.False(modal.FocusTrapEnabled);
        stage.Click(new Point(30, 5));
        Assert.Equal(1, clicks);
    }

    // ── S11: expandToScroll ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S11_The_Content_Scrolls_Only_At_The_Top_Breakpoint_And_As_It_Was_After()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var content = ContentOf(modal);
        Assert.False(content.ScrollY);

        var start = stage.Clock.Now.TotalMilliseconds;
        modal.SetCurrentBreakpoint(1);
        Stage.Settle();
        Assert.True(content.ScrollY);
        stage.At(start + 560);

        // A move down in code doesn't take it away: only a drag does, as in Ionic.
        modal.SetCurrentBreakpoint(0.5);
        stage.After(560);
        Assert.True(content.ScrollY);
        var drag = new Drag(stage, 447).By(24).Hold();
        Assert.False(content.ScrollY);
        drag.Lift();
        stage.After(600);
        Assert.False(content.ScrollY);

        // The dismiss gives it back.
        modal.Dismiss();
        stage.After(240);
        Assert.True(content.ScrollY);
    }

    [AvaloniaFact]
    public void S11_A_Drag_On_The_Content_Below_The_Top_Expands_The_Sheet()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var released = new Drag(stage, 700).By(-200).Lift();
        stage.At(released + 600);
        Assert.True(modal.GetCurrentBreakpoint() > 0.5);
    }

    [AvaloniaFact]
    public void S11_Scrolled_Content_At_The_Top_Breakpoint_Scrolls_And_Does_Not_Start_A_Drag()
    {
        using var stage = new Stage();
        var modal = Presented(stage, Sheet(1));
        var content = ContentOf(modal);
        var starts = 0;
        modal.IonDragStart += (_, _) => starts++;
        content.GetScrollElement()!.Offset = new Vector(0, 100);
        stage.Render();

        var released = new Drag(stage, 400).By(80).Lift();
        stage.At(released + 600);
        Assert.Equal(0, starts);
        Assert.Equal(1, modal.GetCurrentBreakpoint());
    }

    [AvaloniaFact]
    public void S11_ExpandToScroll_False_The_Content_Scrolls_At_Every_Breakpoint_And_Is_As_Tall_As_What_Shows()
    {
        using var stage = new Stage();
        var modal = Presented(stage, Sheet(set: m => m.ExpandToScroll = false));
        var content = ContentOf(modal);
        Assert.True(content.ScrollY);
        stage.Window.UpdateLayout();

        // .ion-page's max-height: 50%, 417 high, at the top of the wrapper.
        var page = (Control)modal.Content!;
        Assert.Equal(417, page.Bounds.Height, 3);
        Assert.Equal(new Point(0, 427), page.TranslatePoint(default, stage.Window));

        var start = stage.Clock.Now.TotalMilliseconds;
        modal.SetCurrentBreakpoint(0.75);
        stage.At(start + 560);
        stage.Window.UpdateLayout();
        Assert.Equal(625.5, page.Bounds.Height, 0);
        Assert.True(content.ScrollY);
    }

    // ── S12, S13, S14, S15, S16 ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S12_A_Backdrop_Tap_While_It_Moves_Does_Nothing()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var start = stage.Clock.Now.TotalMilliseconds;
        modal.SetCurrentBreakpoint(0.25);
        stage.At(start + 200);
        stage.Click(new Point(30, 20));
        stage.After(800);
        Assert.NotNull(modal.GetVisualParent());

        stage.Click(new Point(30, 20));
        stage.After(240);
        Assert.Null(modal.GetVisualParent());
    }

    [AvaloniaFact]
    public void S13_A_Focus_On_The_Host_From_Outside_Goes_To_The_Handle_And_A_Drag_Focuses_The_Host()
    {
        using var stage = new Stage();
        var modal = Presented(stage, Sheet(set: m => m.FocusTrap = false));
        stage.Opener.Focus();
        Stage.Settle();
        modal.Focus();
        Stage.Settle();
        Assert.Same(modal.HandlePart, stage.Focused);

        // From inside, the host keeps it: the drag's start focuses it at the next frame.
        modal.Wrapper!.Focus();
        var drag = new Drag(stage, 447).By(-40).Hold();
        Assert.Same(modal, stage.Focused);
        drag.Lift();
    }

    [AvaloniaFact]
    public void S14_A_Sheets_Safe_Area_Is_0_At_The_Top_And_Sides_And_The_Apps_At_The_Bottom()
    {
        using var stage = new Stage();
        foreach (var (key, value) in new[] { ("Avonic.SafeAreaTop", 24d), ("Avonic.SafeAreaBottom", 30d), ("Avonic.SafeAreaLeft", 5d), ("Avonic.SafeAreaRight", 6d) })
        {
            stage.Window.Resources[key] = value;
        }

        var modal = Presented(stage);
        var inside = (Control)modal.Content!;
        Assert.True(inside.TryFindResource("Avonic.SafeAreaTop", out var top));
        Assert.True(inside.TryFindResource("Avonic.SafeAreaBottom", out var bottom));
        Assert.True(inside.TryFindResource("Avonic.SafeAreaLeft", out var left));
        Assert.True(inside.TryFindResource("Avonic.SafeAreaRight", out var right));
        Assert.Equal((0d, 30d, 0d, 0d), ((double)top!, (double)bottom!, (double)left!, (double)right!));
    }

    [AvaloniaFact]
    public void S15_Presented_Again_It_Starts_At_Its_Initial_Breakpoint_And_A_Missing_One_Warns()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var start = stage.Clock.Now.TotalMilliseconds;
        modal.SetCurrentBreakpoint(1);
        stage.At(start + 560);
        modal.Dismiss();
        stage.After(240);
        Assert.Null(modal.GetCurrentBreakpoint());

        modal.Present(stage.Window);
        stage.After(Entered + 16);
        Assert.Equal(0.5, modal.GetCurrentBreakpoint());
        Near(417, TranslateY(modal.Wrapper!));

        var sink = new WarningSink();
        var previous = Logger.Sink;
        Logger.Sink = sink;
        try
        {
            stage.Page.Children.Add(Sheet(0.6));
            Stage.Settle();
            Assert.Contains(sink.Messages, m => m.Contains("must include the initialBreakpoint"));
        }
        finally
        {
            Logger.Sink = previous;
        }
    }

    [AvaloniaFact]
    public void S16_An_Initial_Breakpoint_Alone_Enters_As_A_Sheet_But_Has_No_Handle_Or_Gesture()
    {
        using var stage = new Stage();
        var modal = Presented(stage, Sheet(set: m => m.Breakpoints = null));
        Assert.False(((IPseudoClasses)modal.Classes).Contains(AvonicModal.Sheet));
        Assert.Equal(new Rect(0, 0, 390, 844), modal.Wrapper!.Bounds);
        Near(422, TranslateY(modal.Wrapper!));
        Assert.Equal(0.16, modal.Backdrop!.Opacity, 4);
        Assert.False(modal.HandlePart!.IsVisible);
        Assert.Null(modal.SheetGesture);
    }

    [AvaloniaFact]
    public void S17_A_Keyboard_Opening_Turns_The_Gesture_Off_For_A_Frame()
    {
        using var stage = new Stage();
        var modal = Presented(stage);
        var starts = 0;
        modal.IonDragStart += (_, _) => starts++;
        var drag = new Drag(stage, 447).By(-24);
        Assert.Equal(1, starts);

        var ends = 0;
        modal.IonDragEnd += (_, _) => ends++;
        stage.Window.RaiseEvent(new KeyboardDidShowEventArgs(KeyboardAssist.IonKeyboardDidShowEvent, stage.Window, 300));
        Stage.Settle();
        drag.By(-16);
        Assert.Equal(1, ends);
        drag.Lift();
    }

    private sealed class WarningSink : ILogSink
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate) => Messages.Add(messageTemplate);

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Messages.Add(messageTemplate + " " + string.Join(" ", propertyValues));
    }
}
