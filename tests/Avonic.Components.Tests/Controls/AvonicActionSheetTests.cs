using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;
using Stage = Avonic.Components.Tests.Controls.AvonicModalTests.Stage;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-action-sheet</c>, md, on the test clock: B1 to B17 in docs/components/ion-action-sheet/PLAN.md, from
/// <c>action-sheet.js</c>, <c>md.enter.js</c>, <c>md.leave.js</c>, <c>action-sheet.md.css</c> and the measurements in
/// <c>ref/layout.json</c> and <c>ref/behaviour.json</c>. Frames come every 16 ms, so the enter animation, played at 0, shows
/// local time t at 16 + t and finishes at the frame at 416; the leave, 450 ms, at the frame 480 after it starts on one.
/// </summary>
public class AvonicActionSheetTests
{
    private const double Entered = 416;
    private const double Left = 480;

    private static readonly CubicBezierEasing Curve = new(0.36, 0.66, 0.04, 1);

    internal static AvonicActionSheet Watch(Stage stage, AvonicActionSheet sheet, string name = "")
    {
        string Now() => $"@{stage.Clock.Now.TotalMilliseconds}";
        sheet.IonActionSheetWillPresent += (_, _) => stage.Log.Add($"{name}ionActionSheetWillPresent{Now()}");
        sheet.WillPresent += (_, _) => stage.Log.Add($"{name}willPresent{Now()}");
        sheet.IonActionSheetDidPresent += (_, _) => stage.Log.Add($"{name}ionActionSheetDidPresent{Now()}");
        sheet.DidPresent += (_, _) => stage.Log.Add($"{name}didPresent{Now()}");
        sheet.IonActionSheetWillDismiss += (_, e) => stage.Log.Add($"{name}ionActionSheetWillDismiss({e.Data},{e.Role}){Now()}");
        sheet.WillDismiss += (_, e) => stage.Log.Add($"{name}willDismiss({e.Data},{e.Role}){Now()}");
        sheet.IonActionSheetDidDismiss += (_, e) => stage.Log.Add($"{name}ionActionSheetDidDismiss({e.Data},{e.Role}){Now()}");
        sheet.DidDismiss += (_, e) => stage.Log.Add($"{name}didDismiss({e.Data},{e.Role}){Now()}");
        return sheet;
    }

    internal static AvonicActionSheet Shown(Stage stage, AvonicActionSheet sheet)
    {
        sheet.Present(stage.Window);
        stage.At(stage.Clock.Now.TotalMilliseconds + Entered);
        stage.Render();
        return sheet;
    }

    private static Point Centre(Visual visual, Stage stage) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), stage.Window)!.Value;

    private static void Tap(Stage stage, Visual visual) => stage.Click(Centre(visual, stage));

    private static void Key(Stage stage, PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        stage.Window.KeyPressQwerty(key, modifiers);
        stage.Window.KeyReleaseQwerty(key, modifiers);
        Stage.Settle();
    }

    private static double TranslateY(Visual visual) => visual.RenderTransform?.Value.M32 ?? 0;

    private static Rect Box(Visual visual, Stage stage) => new(visual.TranslatePoint(default, stage.Window)!.Value, visual.Bounds.Size);

    private static T Part<T>(AvonicActionSheet sheet, string name) where T : Control =>
        sheet.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static T Done<T>(Task<T> task)
    {
        Assert.True(task.IsCompleted, "The task hasn't finished.");
        return task.Result;
    }

    private static List<ActionSheetButton> Basic(List<string>? calls = null, params string[] falseOn) =>
    [
        new() { Text = "Delete", Role = "destructive", Data = "delete", Handler = Log(calls, "Delete", falseOn) },
        new() { Text = "Share", Data = "share", Handler = Log(calls, "Share", falseOn) },
        new() { Text = "Cancel", Role = "cancel", Data = "cancel", Handler = Log(calls, "Cancel", falseOn) },
    ];

    private static Func<object?> Log(List<string>? calls, string name, string[] falseOn) => () =>
    {
        calls?.Add(name);
        return falseOn.Contains(name) ? false : null;
    };

    private static ActionSheetButton Radio(string text, bool isChecked = false, bool disabled = false, List<string>? calls = null) => new()
    {
        Text = text,
        Disabled = disabled,
        Handler = Log(calls, text, []),
        HtmlAttributes = new Dictionary<string, object?> { ["role"] = "radio", ["aria-checked"] = isChecked ? "true" : "false" },
    };

    // ── B1, B2: md's animations ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Enter_Is_Mds_400ms_Slide_Up_From_The_Bottom()
    {
        using var stage = new Stage();
        var sheet = new AvonicActionSheet { Header = "Albums", Buttons = Basic() };
        sheet.Present(stage.Window);
        Stage.Settle();
        var backdrop = sheet.Backdrop!;
        var wrapper = sheet.Wrapper!;
        stage.Render();
        var height = wrapper.Bounds.Height;
        Assert.Equal(216, height, 1);

        Assert.Equal(0.01, backdrop.Opacity, 6);
        Assert.Equal(height, TranslateY(wrapper), 3);
        Assert.False(backdrop.IsHitTestVisible);

        foreach (var local in new[] { 32.0, 96, 192, 304, 384 })
        {
            stage.At(16 + local);
            var e = Curve.Ease(local / 400);
            Assert.Equal(0.01 + (0.32 - 0.01) * e, backdrop.Opacity, 4);
            Assert.Equal(height * (1 - e), TranslateY(wrapper), 2);
            Assert.False(backdrop.IsHitTestVisible);
        }

        // M1: at 100 ms of local time Chromium had the wrapper at 66.735 of 216 and the backdrop at 0.2242, which the curve gives.
        // Chromium's currentTime is rounded to 0.1 ms here, which is worth 0.03 px at that point of the curve.
        Assert.InRange(216 * (1 - Curve.Ease(100 / 400.0)) - 66.735, -0.05, 0.05);
        Assert.InRange(0.01 + 0.31 * Curve.Ease(100 / 400.0) - 0.2242, -0.0001, 0.0001);

        stage.At(Entered);
        Assert.Equal(0.32, backdrop.Opacity, 6);
        Assert.Equal(0, TranslateY(wrapper), 6);
        Assert.True(backdrop.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void B2_Leave_Is_Mds_450ms_Slide_Down_And_The_Wrapper_Rests_Below_The_Bottom()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = Basic() });
        var wrapper = sheet.Wrapper!;
        var height = wrapper.Bounds.Height;
        var start = stage.Clock.Now.TotalMilliseconds;
        sheet.Dismiss();
        Stage.Settle();
        Assert.Equal(0, TranslateY(wrapper), 6);
        Assert.Equal(0.32, sheet.Backdrop!.Opacity, 6);

        foreach (var local in new[] { 32.0, 144, 304, 432 })
        {
            stage.At(start + 16 + local);
            var e = Curve.Ease(local / 450);
            Assert.Equal(0.32 * (1 - e), sheet.Backdrop.Opacity, 4);
            Assert.Equal(height * e, TranslateY(wrapper), 2);
        }

        stage.After(Left);
        Assert.False(sheet.IsVisible);

        // translate3d(0, 100%, 0) once the animations are gone.
        stage.Render();
        Assert.Equal(height, TranslateY(wrapper), 3);
    }

    [AvaloniaFact]
    public void B2_Backdrop_Opacity_Follows_The_Sheets_Key_Then_The_Global_One()
    {
        using var stage = new Stage();
        var sheet = new AvonicActionSheet { Buttons = ["OK"] };
        stage.Window.Resources["Avonic.BackdropOpacity"] = 0.5;
        Shown(stage, sheet);
        Assert.Equal(0.5, sheet.Backdrop!.Opacity, 6);
        sheet.Dismiss();
        stage.After(Left);

        sheet.Resources["Avonic.ActionSheet.BackdropOpacity"] = 0.2;
        Shown(stage, sheet);
        Assert.Equal(0.2, sheet.Backdrop!.Opacity, 6);
    }

    // ── B3, B4: events and focus ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_The_Sheets_Events_Come_Before_The_Generic_Ones_With_Data_And_Role()
    {
        using var stage = new Stage();
        var sheet = Watch(stage, new AvonicActionSheet { Buttons = Basic() });
        sheet.Present(stage.Window);
        stage.At(Entered);
        sheet.Dismiss("d", "r");
        stage.After(Left);
        Assert.Equal(
            [
                "ionActionSheetWillPresent@0", "willPresent@0", "ionActionSheetDidPresent@416", "didPresent@416",
                "ionActionSheetWillDismiss(d,r)@416", "willDismiss(d,r)@416", "ionActionSheetDidDismiss(d,r)@896", "didDismiss(d,r)@896",
            ],
            stage.Log);
    }

    [AvaloniaFact]
    public void B4_Present_Focuses_The_Sheet_And_Dismiss_Gives_Focus_Back()
    {
        using var stage = new Stage();
        stage.Opener.Focus();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = Basic() });
        Assert.Same(sheet, stage.Focused);

        sheet.Dismiss();
        stage.After(Left);
        Assert.Same(stage.Opener, stage.Focused);
    }

    // ── B5, B6: buttons and the cancel handler ──────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_A_Button_Runs_Its_Handler_Then_Dismisses_With_Its_Data_And_Role()
    {
        using var stage = new Stage();
        var calls = new List<string>();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = Basic(calls) });
        var did = sheet.OnDidDismiss();
        Tap(stage, sheet.ButtonControls[0]);
        stage.After(Left);

        Assert.Equal(["Delete"], calls);
        var detail = Done(did);
        Assert.Equal("delete", detail.Data);
        Assert.Equal("destructive", detail.Role);
    }

    [AvaloniaFact]
    public void B5_A_Handler_Returning_False_Or_A_Task_Of_False_Keeps_It_Open()
    {
        using var stage = new Stage();
        var calls = new List<string>();
        var answer = new TaskCompletionSource<bool>();
        var sheet = Shown(stage, new AvonicActionSheet
        {
            Buttons =
            [
                new ActionSheetButton { Text = "Share", Handler = Log(calls, "Share", ["Share"]) },
                new ActionSheetButton { Text = "Later", Handler = () => answer.Task },
                new ActionSheetButton { Text = "Now", Role = "confirm", Data = 7, Handler = () => new ValueTask<object?>((object?)null) },
            ],
        });
        Tap(stage, sheet.ButtonControls[0]);
        stage.After(Left);
        Assert.Equal(["Share"], calls);
        Assert.True(sheet.IsVisible);

        Tap(stage, sheet.ButtonControls[1]);
        stage.After(Left);
        Assert.True(sheet.IsVisible);
        answer.SetResult(false);
        Stage.Settle();
        stage.After(Left);
        Assert.True(sheet.IsVisible);

        var did = sheet.OnDidDismiss();
        Tap(stage, sheet.ButtonControls[2]);
        stage.After(Left);
        Assert.Equal(new OverlayEventDetail(7, "confirm"), Done(did));
    }

    [AvaloniaFact]
    public void B5_A_Task_That_Says_Nothing_Dismisses()
    {
        using var stage = new Stage();
        var answer = new TaskCompletionSource();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = [new ActionSheetButton { Text = "OK", Handler = () => answer.Task }] });
        var did = sheet.OnDidDismiss();
        Tap(stage, sheet.ButtonControls[0]);
        stage.After(Left);
        Assert.False(did.IsCompleted);
        answer.SetResult();
        Stage.Settle();
        stage.After(Left);
        Assert.Null(Done(did).Role);
    }

    [AvaloniaFact]
    public void B5_And_B6_The_Cancel_Button_Dismisses_At_Once_And_Its_Handler_Runs_Once_From_The_Will_Dismiss()
    {
        using var stage = new Stage();
        var calls = new List<string>();
        var sheet = Watch(stage, Shown(stage, new AvonicActionSheet { Buttons = Basic(calls, "Cancel") }));
        sheet.IonActionSheetWillDismiss += (_, _) => calls.Add("willDismiss");
        var did = sheet.OnDidDismiss();
        Tap(stage, sheet.CancelControl!);
        stage.After(Left);

        // M16: false from the handler changes nothing; the handler runs after the event's own listeners added earlier.
        Assert.Equal("cancel", Done(did).Role);
        Assert.Equal("cancel", Done(did).Data);
        Assert.Equal(1, calls.Count(c => c == "Cancel"));
    }

    [AvaloniaFact]
    public void B6_Escape_The_Backdrop_And_A_Backdrop_Role_Call_The_Cancel_Handler_And_Other_Roles_Do_Not()
    {
        using var stage = new Stage();
        var calls = new List<string>();

        var sheet = Shown(stage, new AvonicActionSheet { Buttons = Basic(calls) });
        var did = sheet.OnDidDismiss();
        Key(stage, PhysicalKey.Escape);
        stage.After(Left);
        Assert.Equal(["Cancel"], calls);
        Assert.Equal(new OverlayEventDetail(null, OverlayRoles.Backdrop), Done(did));

        sheet = Shown(stage, new AvonicActionSheet { Buttons = Basic(calls) });
        did = sheet.OnDidDismiss();
        stage.Click(new Point(195, 100));
        stage.After(Left);
        Assert.Equal(["Cancel", "Cancel"], calls);
        Assert.Equal(OverlayRoles.Backdrop, Done(did).Role);

        // A button with the backdrop role dismisses at once, without its own handler, and the cancel handler runs.
        var backdropButton = new ActionSheetButton { Text = "Close", Role = "backdrop", Handler = Log(calls, "Close", []) };
        sheet = Shown(stage, new AvonicActionSheet { Buttons = [backdropButton, .. Basic(calls)] });
        Tap(stage, sheet.ButtonControls[0]);
        stage.After(Left);
        Assert.Equal(["Cancel", "Cancel", "Cancel"], calls);

        // A dismiss with another role doesn't.
        sheet = Shown(stage, new AvonicActionSheet { Buttons = Basic(calls) });
        sheet.Dismiss(null, "other");
        stage.After(Left);
        Assert.Equal(3, calls.Count);
    }

    // ── B7, B8: what's rendered ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_The_First_Cancel_Button_Goes_Last_On_Its_Own_And_Others_Are_Not_Rendered()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet
        {
            Buttons =
            [
                new ActionSheetButton { Text = "Cancel", Role = "cancel" },
                "Share",
                new ActionSheetButton { Text = "Second cancel", Role = "cancel" },
                new ActionSheetButton { Text = "Delete", Role = "destructive" },
            ],
        });
        Assert.Equal(["Share", "Delete"], sheet.ButtonControls.Select(b => b.Text));
        Assert.Equal("Cancel", sheet.CancelControl!.Text);
        var group = Part<Border>(sheet, "PART_Group");
        var cancelGroup = Part<Border>(sheet, "PART_CancelGroup");
        Assert.True(cancelGroup.IsVisible);
        Assert.Same(sheet.CancelControl, cancelGroup.Child);
        Assert.True(Box(cancelGroup, stage).Top >= Box(group, stage).Bottom - 0.5);
        Assert.Equal(844, Box(cancelGroup, stage).Bottom, 1);
    }

    [AvaloniaFact]
    public void B7_A_String_Is_A_Plain_Button_With_No_Role_And_No_Cancel_Group()
    {
        ActionSheetButton cancel = "Cancel";
        Assert.Null(cancel.Role);
        Assert.Equal("Cancel", cancel.Text);

        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = ["Share", "Copy link", "Cancel"] });
        Assert.Equal(3, sheet.ButtonControls.Count);
        Assert.Null(sheet.CancelControl);
        Assert.False(Part<Border>(sheet, "PART_CancelGroup").IsVisible);

        // M6: one group, 156 high, at the bottom.
        var wrapper = sheet.Wrapper!;
        Assert.Equal(688, Box(wrapper, stage).Top, 1);
        Assert.Equal(156, wrapper.Bounds.Height, 1);
    }

    [AvaloniaFact]
    public void B7_An_Icon_Is_Shown_Before_The_Text_Only_When_Set()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = [new ActionSheetButton { Text = "Delete", Icon = "trash" }, "Share"] });
        var icon = sheet.ButtonControls[0].GetVisualDescendants().OfType<AvonicIcon>().Single();
        Assert.True(icon.IsVisible);
        Assert.Equal("trash", icon.Icon);
        Assert.False(sheet.ButtonControls[1].GetVisualDescendants().OfType<AvonicIcon>().Single().IsVisible);
    }

    [AvaloniaFact]
    public void B8_Disabled_Is_Not_Clickable_Or_Focusable_And_Is_Ignored_On_The_Cancel_Button()
    {
        using var stage = new Stage();
        var calls = new List<string>();
        var sheet = Shown(stage, new AvonicActionSheet
        {
            Buttons =
            [
                new ActionSheetButton { Text = "Enabled" },
                new ActionSheetButton { Text = "Disabled", Disabled = true, Icon = "heart", Handler = Log(calls, "Disabled", []) },
                new ActionSheetButton { Text = "Cancel", Role = "cancel", Disabled = true },
            ],
        });
        var disabled = sheet.ButtonControls[1];
        Assert.False(disabled.IsEffectivelyEnabled);
        Assert.False(disabled.Focus());
        Tap(stage, disabled);
        stage.After(Left);
        Assert.Empty(calls);
        Assert.True(sheet.IsVisible);

        // Opacity 0.4 over the whole button.
        Assert.Equal(0.4, (disabled.OpacityMask as ISolidColorBrush)!.Opacity, 3);
        Assert.Null(sheet.ButtonControls[0].OpacityMask);

        // Its icon is in currentColor, the button's colour, not --color.
        var icon = disabled.GetVisualDescendants().OfType<AvonicIcon>().Single();
        Assert.Equal(Color.Parse("#262626"), ((ISolidColorBrush)icon.Foreground!).Color);
        Assert.Equal(1, ((ISolidColorBrush)icon.Foreground!).Opacity);

        Assert.True(sheet.CancelControl!.IsEffectivelyEnabled);
        var did = sheet.OnDidDismiss();
        Tap(stage, sheet.CancelControl);
        stage.After(Left);
        Assert.Equal("cancel", Done(did).Role);
    }

    [AvaloniaFact]
    public void B8_HtmlAttributes_Disabled_And_Id_Reach_Only_The_Cancel_Button()
    {
        using var stage = new Stage();
        var attributes = new Dictionary<string, object?> { ["disabled"] = true, ["id"] = "from-attributes" };
        var sheet = Shown(stage, new AvonicActionSheet
        {
            Buttons =
            [
                new ActionSheetButton { Text = "A", HtmlAttributes = attributes },
                new ActionSheetButton { Text = "Cancel", Role = "cancel", Id = "ignored", HtmlAttributes = attributes },
            ],
        });
        Assert.True(sheet.ButtonControls[0].IsEffectivelyEnabled);
        Assert.Equal($"action-sheet-button-{sheet.OverlayIndex}-0", AutomationProperties.GetAutomationId(sheet.ButtonControls[0]));
        Assert.False(sheet.CancelControl!.IsEffectivelyEnabled);
        Assert.Equal("from-attributes", AutomationProperties.GetAutomationId(sheet.CancelControl));
    }

    // ── B9: radios ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_The_Checked_Radio_Is_Selected_And_The_One_Tab_Stop()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet
        {
            Buttons = [Radio("Apple"), Radio("Banana", true), Radio("Cherry"), Radio("Durian", disabled: true), new ActionSheetButton { Text = "Cancel", Role = "cancel" }],
        });
        var radios = sheet.ButtonControls;
        Assert.Equal([false, true, false, false], radios.Take(4).Select(r => r.IsChecked));
        Assert.Equal([false, true, false, false], radios.Select(r => r.IsSelected));
        Assert.Equal([false, true, false, false], radios.Take(4).Select(KeyboardNavigation.GetIsTabStop));
        Assert.Equal(FontWeight.Bold, radios[1].FontWeight);
        Assert.Equal(FontWeight.Normal, radios[0].FontWeight);

        // M14: Tab from the sheet reaches the checked one, then Cancel, then wraps to it.
        Key(stage, PhysicalKey.Tab);
        Assert.Same(radios[1], stage.Focused);
        Key(stage, PhysicalKey.Tab);
        Assert.Same(sheet.CancelControl, stage.Focused);
        Key(stage, PhysicalKey.Tab);
        Assert.Same(radios[1], stage.Focused);
    }

    [AvaloniaFact]
    public void B9_With_None_Checked_The_First_Radio_Is_The_Tab_Stop()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = ["Plain", Radio("Apple"), Radio("Banana")] });
        Assert.Equal([true, true, false], sheet.ButtonControls.Select(KeyboardNavigation.GetIsTabStop));
        Assert.All(sheet.ButtonControls, b => Assert.False(b.IsChecked));
    }

    [AvaloniaFact]
    public void B9_Arrows_Move_Check_And_Focus_The_Next_Enabled_Radio_Wrapping_Without_A_Dismiss()
    {
        using var stage = new Stage();
        var calls = new List<string>();
        var sheet = Watch(stage, Shown(stage, new AvonicActionSheet
        {
            Buttons = [Radio("Apple", calls: calls), Radio("Banana", true, calls: calls), Radio("Cherry", calls: calls), Radio("Durian", disabled: true), "Cancel"],
        }));
        stage.Log.Clear();
        var radios = sheet.ButtonControls;
        radios[1].Focus();

        // M14: Down: Cherry; Down: Apple (Durian is disabled); Up: Cherry; Left: Banana; Right: Cherry.
        foreach (var (key, expected) in new[] { (PhysicalKey.ArrowDown, 2), (PhysicalKey.ArrowDown, 0), (PhysicalKey.ArrowUp, 2), (PhysicalKey.ArrowLeft, 1), (PhysicalKey.ArrowRight, 2) })
        {
            Key(stage, key);
            Assert.Same(radios[expected], stage.Focused);
            Assert.Equal(Enumerable.Range(0, 4).Select(i => i == expected), radios.Take(4).Select(r => r.IsChecked));
            Assert.Equal(Enumerable.Range(0, 4).Select(i => i == expected), radios.Take(4).Select(KeyboardNavigation.GetIsTabStop));
        }

        stage.After(Left);
        Assert.Empty(calls);
        Assert.Empty(stage.Log);
        Assert.True(sheet.IsVisible);

        // An arrow on a plain button does nothing.
        sheet.ButtonControls[4].Focus();
        Key(stage, PhysicalKey.ArrowDown);
        Assert.Same(sheet.ButtonControls[4], stage.Focused);
    }

    [AvaloniaFact]
    public void B9_Space_On_A_Radio_Checks_It_And_Clicks_It()
    {
        using var stage = new Stage();
        var calls = new List<string>();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = [Radio("Apple", calls: calls), Radio("Banana", true, calls: calls), "Cancel"] });
        sheet.ButtonControls[0].Focus();
        var did = sheet.OnDidDismiss();
        Key(stage, PhysicalKey.Space);
        Assert.True(sheet.ButtonControls[0].IsChecked);
        Assert.False(sheet.ButtonControls[1].IsChecked);
        stage.After(Left);
        Assert.Equal(["Apple"], calls);
        Assert.Equal(new OverlayEventDetail(null, null), Done(did));
    }

    [AvaloniaFact]
    public void B9_A_Tap_On_A_Radio_Checks_It_Then_Runs_Its_Handler()
    {
        using var stage = new Stage();
        bool? checkedInHandler = null;
        AvonicActionSheet? sheet = null;
        var cherry = Radio("Cherry");
        cherry.Handler = () =>
        {
            checkedInHandler = sheet!.ButtonControls[2].IsChecked;
            return false;
        };
        sheet = Shown(stage, new AvonicActionSheet { Buttons = [Radio("Apple"), Radio("Banana", true), cherry] });
        Tap(stage, sheet.ButtonControls[2]);
        Assert.True(checkedInHandler);
        Assert.Equal([false, false, true], sheet.ButtonControls.Select(r => r.IsSelected));
    }

    [AvaloniaFact]
    public void B9_New_Buttons_Without_A_Checked_Radio_Keep_The_Checked_Place()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = [Radio("Apple"), Radio("Banana", true)] });
        sheet.Buttons = [Radio("A"), Radio("B")];
        Assert.Equal([false, true], sheet.ButtonControls.Select(r => r.IsChecked));
        sheet.Buttons = [Radio("A", true), Radio("B")];
        Assert.Equal([true, false], sheet.ButtonControls.Select(r => r.IsChecked));
    }

    [AvaloniaFact]
    public void B9_An_Inline_Sheet_Keeps_Its_Checked_Radio_Through_Present_Unlike_Ionics()
    {
        using var stage = new Stage();
        var sheet = new AvonicActionSheet { Buttons = [Radio("Apple"), Radio("Banana", true)] };
        stage.Page.Children.Add(sheet);
        Stage.Settle();
        Shown(stage, sheet);
        Assert.Equal([false, true], sheet.ButtonControls.Select(r => r.IsChecked));
    }

    [AvaloniaFact]
    public void B9_A_Selected_Role_Is_Selected_Unless_It_Is_A_Radio()
    {
        using var stage = new Stage();
        var radio = Radio("Radio");
        radio.Role = "selected";
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = [new ActionSheetButton { Text = "Picked", Role = "selected" }, radio] });
        Assert.True(sheet.ButtonControls[0].IsSelected);
        Assert.Equal(FontWeight.Bold, sheet.ButtonControls[0].FontWeight);
        Assert.False(sheet.ButtonControls[1].IsSelected);
    }

    // ── B10, B11: keys and backdropDismiss ──────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_Tab_Goes_Through_The_Buttons_And_Wraps_And_Shift_Tab_Wraps_Back()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Header = "Albums", Buttons = ["Delete", "Share", new ActionSheetButton { Text = "Cancel", Role = "cancel" }] });
        var all = sheet.AllButtonControls.ToList();
        Assert.Same(sheet, stage.Focused);

        // M13: host, Delete, Share, Cancel, Delete.
        foreach (var expected in new[] { 0, 1, 2, 0 })
        {
            Key(stage, PhysicalKey.Tab);
            Assert.Same(all[expected], stage.Focused);
        }

        Key(stage, PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.Same(all[2], stage.Focused);
        Key(stage, PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.Same(all[1], stage.Focused);

        // Enter clicks the focused button.
        var did = sheet.OnDidDismiss();
        Key(stage, PhysicalKey.Enter);
        stage.After(Left);
        Assert.True(did.IsCompleted);
    }

    [AvaloniaFact]
    public void B11_BackdropDismiss_False_Stops_The_Tap_And_Escape()
    {
        using var stage = new Stage();
        var calls = new List<string>();
        var sheet = Watch(stage, new AvonicActionSheet { Buttons = Basic(calls), BackdropDismiss = false });
        Shown(stage, sheet);
        stage.Log.Clear();
        Assert.False(sheet.Backdrop!.Tappable);
        stage.Click(new Point(195, 100));
        Key(stage, PhysicalKey.Escape);
        stage.After(Left);
        Assert.Empty(stage.Log);
        Assert.Empty(calls);

        sheet.BackdropDismiss = true;
        stage.Click(new Point(195, 100));
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,backdrop)", StringComparison.Ordinal));
    }

    // ── B12: layout ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_The_Basic_Sheet_Sits_At_The_Bottom_With_Mds_Boxes()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Header = "Albums", Buttons = Basic() });

        // M4, ref/layout.json "basic".
        Assert.Equal(new Rect(0, 628, 390, 216), Box(sheet.Wrapper!, stage));
        Assert.Equal(new Rect(0, 628, 390, 164), Box(Part<Border>(sheet, "PART_Group"), stage));
        Assert.Equal(new Rect(0, 628, 390, 60), Box(Part<Border>(sheet, "PART_Title"), stage));
        Assert.Equal(new Rect(0, 688, 390, 52), Box(sheet.ButtonControls[0], stage));
        Assert.Equal(new Rect(0, 740, 390, 52), Box(sheet.ButtonControls[1], stage));
        Assert.Equal(new Rect(0, 792, 390, 52), Box(sheet.CancelControl!, stage));
        Assert.Equal(16, Box(Part<TextBlock>(sheet, "PART_TitleText"), stage).X, 1);
        Assert.Equal(648, Box(Part<TextBlock>(sheet, "PART_TitleText"), stage).Y, 1);
    }

    [AvaloniaFact]
    public void B12_A_Long_List_Fills_The_Window_And_Its_Group_Scrolls_Above_The_Cancel_Group()
    {
        using var stage = new Stage();
        var buttons = Enumerable.Range(1, 30).Select(i => new ActionSheetButton { Text = $"Option {i}" }).ToList();
        buttons.Add(new ActionSheetButton { Text = "Cancel", Role = "cancel" });
        var sheet = Shown(stage, new AvonicActionSheet { Header = "Pick one", Buttons = buttons });

        // M7: the wrapper 844 (max-height: 100%), the group 792 scrolling 1620, the cancel group 52.
        Assert.Equal(new Rect(0, 0, 390, 844), Box(sheet.Wrapper!, stage));
        Assert.Equal(new Rect(0, 0, 390, 792), Box(Part<Border>(sheet, "PART_Group"), stage));
        var scroller = Part<ScrollViewer>(sheet, "PART_GroupScroller");
        Assert.Equal(1620, scroller.Extent.Height, 1);
        Assert.Equal(792, scroller.Viewport.Height, 1);
        Assert.Equal(new Rect(0, 792, 390, 52), Box(sheet.CancelControl!, stage));
    }

    [AvaloniaFact]
    public void B12_On_A_Tablet_It_Is_500_Wide_And_Centred()
    {
        using var stage = new Stage(1024, 768);
        var sheet = Shown(stage, new AvonicActionSheet
        {
            Header = "Albums",
            SubHeader = "Choose an action",
            Buttons =
            [
                new ActionSheetButton { Text = "Delete", Role = "destructive", Icon = "trash" }, new ActionSheetButton { Text = "Share", Icon = "share" },
                new ActionSheetButton { Text = "Play (open modal)", Icon = "caret-forward-circle" }, new ActionSheetButton { Text = "Favorite", Icon = "heart" },
                new ActionSheetButton { Text = "Cancel", Role = "cancel", Icon = "close" },
            ],
        });

        // M8.
        Assert.Equal(new Rect(262, 420, 500, 348), Box(sheet.Wrapper!, stage));
        Assert.Equal(new Rect(262, 420, 500, 88), Box(Part<Border>(sheet, "PART_Title"), stage));
        Assert.Equal(new Rect(262, 716, 500, 52), Box(sheet.CancelControl!, stage));
    }

    [AvaloniaFact]
    public void B12_The_Safe_Areas_Pad_The_Last_Group_And_Cap_The_Height()
    {
        using var stage = new Stage();
        stage.Window.Resources["Avonic.SafeAreaTop"] = 30.0;
        stage.Window.Resources["Avonic.SafeAreaBottom"] = 20.0;
        var buttons = Enumerable.Range(1, 30).Select(i => new ActionSheetButton { Text = $"Option {i}" }).ToList();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = buttons });

        // max-height: calc(100% - 50px); the only group is the last, padded by 20 at the bottom.
        Assert.Equal(new Rect(0, 50, 390, 794), Box(sheet.Wrapper!, stage));
        Assert.Equal(20, Part<Border>(sheet, "PART_Group").Padding.Bottom);

        sheet.Buttons = [.. buttons, new ActionSheetButton { Text = "Cancel", Role = "cancel" }];
        stage.Render();
        Assert.Equal(0, Part<Border>(sheet, "PART_Group").Padding.Bottom);
        Assert.Equal(20, Part<Border>(sheet, "PART_CancelGroup").Padding.Bottom);
        Assert.Equal(844 - 20, Box(sheet.CancelControl!, stage).Bottom, 1);
    }

    [AvaloniaFact]
    public void B12_An_Apps_Size_Keys_Win()
    {
        using var stage = new Stage();
        var sheet = new AvonicActionSheet { Buttons = ["A", "B"] };
        sheet.Resources["Avonic.ActionSheet.MaxWidth"] = 300.0;
        sheet.Resources["Avonic.ActionSheet.MaxHeight"] = "80px";
        Shown(stage, sheet);
        Assert.Equal(new Rect(45, 764, 300, 80), Box(sheet.Wrapper!, stage));
        Assert.Equal(80, Part<Border>(sheet, "PART_Group").Bounds.Height, 1);
    }

    [AvaloniaFact]
    public void B12_A_Fixed_Height_Puts_The_Groups_At_Its_End()
    {
        using var stage = new Stage();
        var sheet = new AvonicActionSheet { Buttons = ["A", new ActionSheetButton { Text = "Cancel", Role = "cancel" }] };
        sheet.Resources["Avonic.ActionSheet.Height"] = "300px";
        Shown(stage, sheet);
        Assert.Equal(new Rect(0, 544, 390, 300), Box(sheet.Wrapper!, stage));
        Assert.Equal(new Rect(0, 740, 390, 52), Box(sheet.ButtonControls[0], stage));
    }

    [AvaloniaFact]
    public void B12_A_Button_Centres_Its_Line_And_Its_Icon_With_The_Text_32_After_It()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = [new ActionSheetButton { Text = "Delete", Icon = "trash" }, "Share"] });
        var withIcon = sheet.ButtonControls[0];
        var icon = withIcon.GetVisualDescendants().OfType<AvonicIcon>().Single();
        var label = withIcon.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Label");

        // M5: the icon 24 at 16, 14 down; the label's line of 16 at 72, centred in 52.
        var top = Box(withIcon, stage).Y;
        Assert.Equal(new Rect(16, top + 14, 24, 24), Box(icon, stage));
        Assert.Equal(72, Box(label, stage).X, 1);
        Assert.Equal(16, label.LineHeight);
        Assert.Equal(16, label.Bounds.Height);

        // The line box (the TextBlock's box less its half-leading margin) is 18 down in either button, within layout rounding;
        // Chromium's text there is 16 down, as its glyphs are 19 high in a line of 16.
        Assert.InRange(Box(label, stage).Y - label.Margin.Top - (top + 18), -0.5, 0.5);
        var plain = sheet.ButtonControls[1].GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Label");
        Assert.Equal(16, Box(plain, stage).X, 1);
        Assert.InRange(Box(plain, stage).Y - plain.Margin.Top - (Box(sheet.ButtonControls[1], stage).Y + 18), -0.5, 0.5);
    }

    [AvaloniaFact]
    public void B12_A_Long_Label_Wraps_In_Lines_Of_16_And_Its_Icon_Shrinks()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet
        {
            Header = "A header long enough that it has to wrap onto a second line in the sheet",
            Buttons = [new ActionSheetButton { Text = "A button whose text is long enough that it has to wrap onto another line", Icon = "heart" }, new ActionSheetButton { Text = "Cancel", Role = "cancel" }],
        });

        // M11: the title 75; the button 56, its icon 14.78 wide.
        Assert.Equal(75, Part<Border>(sheet, "PART_Title").Bounds.Height, 1);
        var button = sheet.ButtonControls[0];
        Assert.Equal(56, button.Bounds.Height, 1);
        var icon = button.GetVisualDescendants().OfType<AvonicIcon>().Single();
        Assert.Equal(14.78, icon.Bounds.Width, 0);
    }

    // ── B13: the title ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_The_Title_Shows_For_Any_Header_And_The_Sub_Title_Only_Inside_It()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = ["A"], SubHeader = "Sub" });
        var title = Part<Border>(sheet, "PART_Title");
        Assert.False(title.IsVisible);

        // An empty header still makes the title, at its minimum of 60, with no line of its own, and the sub title shows in it:
        // 20 + 16 + 16 + 17.
        sheet.Header = "";
        stage.Render();
        Assert.True(title.IsVisible);
        Assert.Equal(69, title.Bounds.Height, 1);
        sheet.SubHeader = null;
        stage.Render();
        Assert.Equal(60, title.Bounds.Height, 1);

        sheet.Header = "Albums";
        sheet.SubHeader = "Choose an action";
        stage.Render();

        // M5: 88 with the sub title, 16 + 16 at 535 when the sheet's top is 496.
        Assert.Equal(88, title.Bounds.Height, 1);
        var sub = Part<TextBlock>(sheet, "PART_SubTitle");
        Assert.True(sub.IsVisible);
        Assert.Equal(16, sub.Bounds.Height, 1);
        Assert.Equal(14, sub.FontSize);

        sheet.SubHeader = "";
        stage.Render();
        Assert.False(sub.IsVisible);
    }

    // ── B14: states ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_Mds_Colours_And_The_Focused_Highlight()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Header = "Albums", Buttons = [new ActionSheetButton { Text = "Delete", Icon = "trash" }, "Share"] });
        var button = sheet.ButtonControls[0];
        Assert.Equal(Color.Parse("#262626"), ((ISolidColorBrush)button.Foreground!).Color);
        Assert.Equal(16, button.FontSize);

        // M5: the icons, the title and the sub title in rgba(0, 0, 0, 0.54).
        var icon = button.GetVisualDescendants().OfType<AvonicIcon>().Single();
        foreach (var brush in new[] { icon.Foreground, Part<TextBlock>(sheet, "PART_TitleText").Foreground, Part<TextBlock>(sheet, "PART_SubTitle").Foreground })
        {
            var solid = Assert.IsAssignableFrom<ISolidColorBrush>(brush);
            Assert.Equal(0.54, solid.Opacity, 2);
            Assert.Equal(Colors.Black, solid.Color);
        }

        Assert.Equal(24, icon.FontSize);
        var highlight = button.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Highlight");
        Assert.Equal(0, highlight.Opacity);

        // M13: a keyboard focus shows the ::after in currentColor at 0.12.
        Key(stage, PhysicalKey.Tab);
        Assert.Same(button, stage.Focused);
        Assert.Equal(0.12, highlight.Opacity, 3);
        Assert.Equal(Color.Parse("#262626"), ((ISolidColorBrush)highlight.Background!).Color);
    }

    [AvaloniaFact]
    public void B14_A_Mouse_Over_A_Button_Shows_The_Hover_Highlight_And_A_Press_Only_The_Ripple()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = [new ActionSheetButton { Text = "A", Handler = () => false }, "B"] });
        var button = sheet.ButtonControls[0];
        var highlight = button.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Highlight");
        stage.Window.MouseMove(Centre(button, stage), RawInputModifiers.None);
        Stage.Settle();
        Assert.Equal(0.04, highlight.Opacity, 3);
        Assert.Equal(Color.Parse("#262626"), ((ISolidColorBrush)highlight.Background!).Color);

        // The press: md's activated ::after is transparent at 0, but the hover's comes after it in CSS and wins while the mouse is
        // over it, as in Chromium; a touch press has no hover, so the activated rule shows: nothing.
        stage.Window.MouseMove(new Point(195, 100), RawInputModifiers.None);
        Stage.Settle();
        Assert.Equal(0, highlight.Opacity);
        var finger = stage.Window.TouchBegin(Centre(button, stage), RawInputModifiers.None);
        Stage.Settle();
        stage.After(100);
        Assert.Contains(":activated", button.Classes);
        Assert.Equal(0, highlight.Opacity);
        stage.Window.TouchEnd(finger, Centre(button, stage), RawInputModifiers.None);
        Stage.Settle();
    }

    [AvaloniaFact]
    public void B14_The_State_Keys_Are_Read()
    {
        using var stage = new Stage();
        var sheet = new AvonicActionSheet { Buttons = [new ActionSheetButton { Text = "A", Role = "selected" }, "B"] };
        sheet.Resources["Avonic.ActionSheet.ButtonColor"] = new SolidColorBrush(Colors.Green);
        sheet.Resources["Avonic.ActionSheet.ButtonColorSelected"] = new SolidColorBrush(Colors.Red);
        sheet.Resources["Avonic.ActionSheet.ButtonBackgroundSelectedOpacity"] = 0.3;
        sheet.Resources["Avonic.ActionSheet.ButtonBackgroundFocused"] = new SolidColorBrush(Colors.Blue);
        sheet.Resources["Avonic.ActionSheet.Background"] = new SolidColorBrush(Colors.Yellow);
        Shown(stage, sheet);
        var selected = sheet.ButtonControls[0];
        var plain = sheet.ButtonControls[1];
        Assert.Equal(Colors.Red, ((ISolidColorBrush)selected.Foreground!).Color);
        Assert.Equal(Colors.Green, ((ISolidColorBrush)plain.Foreground!).Color);
        var highlight = selected.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Highlight");
        Assert.Equal(0.3, highlight.Opacity, 3);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)highlight.Background!).Color);
        Assert.Equal(Colors.Yellow, ((ISolidColorBrush)Part<Border>(sheet, "PART_Group").Background!).Color);

        Key(stage, PhysicalKey.Tab);
        Key(stage, PhysicalKey.Tab);
        Assert.Same(plain, stage.Focused);
        var plainHighlight = plain.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Highlight");
        Assert.Equal(Colors.Blue, ((ISolidColorBrush)plainHighlight.Background!).Color);
    }

    [AvaloniaFact]
    public void B14_CssClass_Reaches_The_Sheet_And_A_Button_And_A_Role_Is_A_PseudoClass()
    {
        using var stage = new Stage();
        var sheet = new AvonicActionSheet
        {
            CssClass = "mine other",
            Buttons = [new ActionSheetButton { Text = "A", CssClass = "x y", Role = "destructive" }, new ActionSheetButton { Text = "C", Role = "cancel", CssClass = "z" }],
        };
        sheet.Classes.Add("app");
        Shown(stage, sheet);
        Assert.Superset(new HashSet<string> { "mine", "other", "app" }, sheet.Classes.ToHashSet());
        Assert.Superset(new HashSet<string> { "x", "y", ":role-destructive" }, sheet.ButtonControls[0].Classes.ToHashSet());
        Assert.Superset(new HashSet<string> { "z", ":role-cancel" }, sheet.CancelControl!.Classes.ToHashSet());

        sheet.CssClass = "next";
        Assert.DoesNotContain("mine", sheet.Classes);
        Assert.Superset(new HashSet<string> { "next", "app" }, sheet.Classes.ToHashSet());
    }

    // ── B15: automation ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_Automation_Names_The_Dialog_And_Its_Buttons()
    {
        using var stage = new Stage();
        var sheet = Shown(stage, new AvonicActionSheet
        {
            Header = "H",
            SubHeader = "S",
            HtmlAttributes = new Dictionary<string, object?> { ["aria-label"] = "Label", ["id"] = "sheet-id" },
            Buttons =
            [
                new ActionSheetButton { Text = "A", Id = "my-a", HtmlAttributes = new Dictionary<string, object?> { ["aria-label"] = "Alpha" } },
                "B",
                Radio("R", true),
            ],
        });
        var peer = ControlAutomationPeer.CreatePeerForElement(sheet);
        Assert.Equal(AutomationControlType.Window, peer.GetAutomationControlType());
        Assert.Equal("H S", peer.GetName());
        Assert.Equal("sheet-id", peer.GetAutomationId());

        sheet.Header = null;
        Assert.Equal("Label", peer.GetName());

        var a = ControlAutomationPeer.CreatePeerForElement(sheet.ButtonControls[0]);
        Assert.Equal("Alpha", a.GetName());
        Assert.Equal("my-a", a.GetAutomationId());
        Assert.Equal(AutomationControlType.Button, a.GetAutomationControlType());
        var b = ControlAutomationPeer.CreatePeerForElement(sheet.ButtonControls[1]);
        Assert.Equal("B", b.GetName());
        Assert.Equal($"action-sheet-button-{sheet.OverlayIndex}-1", b.GetAutomationId());
        Assert.NotNull(b.GetProvider<IInvokeProvider>());
        Assert.Null(b.GetProvider<IToggleProvider>());

        var r = ControlAutomationPeer.CreatePeerForElement(sheet.ButtonControls[2]);
        Assert.Equal(AutomationControlType.RadioButton, r.GetAutomationControlType());
        Assert.Equal(ToggleState.On, r.GetProvider<IToggleProvider>()!.ToggleState);
        Assert.Null(r.GetProvider<IInvokeProvider>());

        // Invoking B is a click.
        var did = sheet.OnDidDismiss();
        b.GetProvider<IInvokeProvider>()!.Invoke();
        stage.After(Left);
        Assert.True(did.IsCompleted);

        // Presented again, it has a new overlay index, and its render at present gives the buttons ids with it.
        var first = sheet.OverlayIndex;
        Shown(stage, sheet);
        Assert.NotEqual(first, sheet.OverlayIndex);
        Assert.Equal($"action-sheet-button-{sheet.OverlayIndex}-1", AutomationProperties.GetAutomationId(sheet.ButtonControls[1]));
    }

    // ── B16, B17: isOpen, trigger, changes while open, scrolling ────────────────────────────

    [AvaloniaFact]
    public void B16_IsOpen_And_Trigger_Present_It()
    {
        using var stage = new Stage();
        var scope = new NameScope();
        scope.Register("Opener", stage.Opener);
        NameScope.SetNameScope(stage.Page, scope);
        var sheet = Watch(stage, new AvonicActionSheet { Trigger = "Opener", Buttons = ["OK"] });
        stage.Page.Children.Add(sheet);
        Stage.Settle();

        stage.Opener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        stage.At(Entered);
        Assert.Contains("didPresent@416", stage.Log);
        Tap(stage, sheet.ButtonControls[0]);
        stage.After(Left);
        Assert.Contains(sheet, stage.Page.Children);
        stage.Log.Clear();

        sheet.IsOpen = true;
        stage.After(Entered);
        Assert.Contains(stage.Log, l => l.StartsWith("didPresent", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B16_Buttons_Changed_While_Open_Render_Again_And_Focus_Stays_Inside()
    {
        using var stage = new Stage();
        var sheet = new AvonicActionSheet();
        sheet.Buttons!.Add("One");
        Shown(stage, sheet);
        Assert.Single(sheet.ButtonControls);
        sheet.ButtonControls[0].Focus();
        sheet.Buttons.Add("Two");
        sheet.Buttons.Add(new ActionSheetButton { Text = "Cancel", Role = "cancel" });
        Assert.Single(sheet.ButtonControls);
        Stage.Settle();
        Assert.Equal(["One", "Two"], sheet.ButtonControls.Select(b => b.Text));
        Assert.NotNull(sheet.CancelControl);
        Assert.Same(sheet.ButtonControls[0], stage.Focused);

        sheet.Buttons = ["Only"];
        Assert.Equal(["Only"], sheet.ButtonControls.Select(b => b.Text));
        Assert.Null(sheet.CancelControl);
    }

    [AvaloniaFact]
    public void B17_The_Group_Scrolls_With_The_Wheel_And_A_Touch_Drag()
    {
        using var stage = new Stage();
        var buttons = Enumerable.Range(1, 30).Select(i => new ActionSheetButton { Text = $"Option {i}" }).ToList();
        var sheet = Shown(stage, new AvonicActionSheet { Buttons = buttons });
        var scroller = Part<ScrollViewer>(sheet, "PART_GroupScroller");
        Assert.Equal(0, scroller.Offset.Y);

        stage.Window.MouseWheel(new Point(195, 400), new Vector(0, -3));
        Stage.Settle();
        Assert.True(scroller.Offset.Y > 0, $"wheel: {scroller.Offset.Y}");

        var before = scroller.Offset.Y;
        stage.Render();
        var at = new Point(195, 600);
        var finger = stage.Window.TouchBegin(at, RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            stage.Window.TouchMove(finger, at - new Point(0, i * 20), RawInputModifiers.None);
            Stage.Settle();
        }

        stage.Window.TouchEnd(finger, at - new Point(0, 200), RawInputModifiers.None);
        Stage.Settle();
        Assert.True(scroller.Offset.Y > before + 100, $"touch: {before} to {scroller.Offset.Y}");
        Assert.True(sheet.IsVisible);
    }
}
