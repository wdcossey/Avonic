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
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Components.Behaviors;
using Avonic.Components.Overlays;
using Stage = Avonic.Components.Tests.Controls.AvonicModalTests.Stage;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-alert</c>, md, on the test clock: B1 to B21 in docs/components/ion-alert/PLAN.md, from <c>alert.js</c>,
/// <c>md.enter.js</c>, <c>md.leave.js</c>, <c>alert.md.css</c> and the measurements in <c>ref/layout.json</c> and
/// <c>ref/behaviour.json</c>. Frames come every 16 ms, so the enter animation, played at 0, shows local time t at 16 + t and
/// finishes at the frame at 176; the leave, also 150 ms, 176 after it starts on a frame.
/// </summary>
public class AvonicAlertTests
{
    private const double Entered = 176;
    private const double Left = 176;

    private static readonly CubicBezierEasing EaseInOut = new(0.42, 0, 0.58, 1);

    private static AvonicAlert Watch(Stage stage, AvonicAlert alert, string name = "")
    {
        string Now() => $"@{stage.Clock.Now.TotalMilliseconds}";
        alert.IonAlertWillPresent += (_, _) => stage.Log.Add($"{name}ionAlertWillPresent{Now()}");
        alert.WillPresent += (_, _) => stage.Log.Add($"{name}willPresent{Now()}");
        alert.IonAlertDidPresent += (_, _) => stage.Log.Add($"{name}ionAlertDidPresent{Now()}");
        alert.DidPresent += (_, _) => stage.Log.Add($"{name}didPresent{Now()}");
        alert.IonAlertWillDismiss += (_, e) => stage.Log.Add($"{name}ionAlertWillDismiss({e.Data},{e.Role}){Now()}");
        alert.WillDismiss += (_, e) => stage.Log.Add($"{name}willDismiss({e.Data},{e.Role}){Now()}");
        alert.IonAlertDidDismiss += (_, e) => stage.Log.Add($"{name}ionAlertDidDismiss({e.Data},{e.Role}){Now()}");
        alert.DidDismiss += (_, e) => stage.Log.Add($"{name}didDismiss({e.Data},{e.Role}){Now()}");
        return alert;
    }

    private static AvonicAlert Shown(Stage stage, AvonicAlert alert)
    {
        alert.Present(stage.Window);
        stage.At(stage.Clock.Now.TotalMilliseconds + Entered);
        stage.Render();
        return alert;
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

    private static double Scale(Visual visual) => visual.RenderTransform?.Value.M11 ?? 1;

    private static T Done<T>(Task<T> task)
    {
        Assert.True(task.IsCompleted, "The task hasn't finished.");
        return task.Result;
    }

    private static List<AlertInput> Radios(params string[] values) =>
        values.Select(v => new AlertInput { Type = AlertInputType.Radio, Label = v, Value = v }).ToList();

    private static List<AlertInput> Checkboxes(params string[] values) =>
        values.Select(v => new AlertInput { Type = AlertInputType.Checkbox, Label = v, Value = v }).ToList();

    // ── B1, B2: md's animations ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Enter_Is_Mds_150ms_Ease_In_Out_With_The_Wrapper_Scaling_Up()
    {
        using var stage = new Stage();
        var alert = new AvonicAlert { Header = "Alert", Buttons = ["OK"] };
        alert.Present(stage.Window);
        Stage.Settle();
        var backdrop = alert.Backdrop!;
        var wrapper = alert.Wrapper!;

        Assert.Equal(0.01, backdrop.Opacity, 6);
        Assert.Equal(0.01, wrapper.Opacity, 6);
        Assert.Equal(0.9, Scale(wrapper), 4);
        Assert.False(backdrop.IsHitTestVisible);

        foreach (var local in new[] { 32.0, 64, 96, 128 })
        {
            stage.At(16 + local);
            var e = EaseInOut.Ease(local / 150);
            Assert.Equal(0.01 + (0.32 - 0.01) * e, backdrop.Opacity, 4);
            Assert.Equal(0.01 + 0.99 * e, wrapper.Opacity, 4);
            Assert.Equal(0.9 + 0.1 * e, Scale(wrapper), 4);
            Assert.False(backdrop.IsHitTestVisible);
        }

        stage.At(Entered);
        Assert.Equal(0.32, backdrop.Opacity, 6);
        Assert.Equal(1, wrapper.Opacity, 6);
        Assert.Equal(1, Scale(wrapper), 4);
        Assert.True(backdrop.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void B2_Leave_Is_Mds_150ms_Fade_And_The_Wrapper_Rests_At_0()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert { Header = "Alert", Buttons = ["OK"] });
        var start = stage.Clock.Now.TotalMilliseconds;
        alert.Dismiss();
        Stage.Settle();
        Assert.Equal(0.99, alert.Wrapper!.Opacity, 6);
        Assert.Equal(0.32, alert.Backdrop!.Opacity, 6);

        foreach (var local in new[] { 32.0, 80, 128 })
        {
            stage.At(start + 16 + local);
            var e = EaseInOut.Ease(local / 150);
            Assert.Equal(0.32 * (1 - e), alert.Backdrop.Opacity, 4);
            Assert.Equal(0.99 * (1 - e), alert.Wrapper.Opacity, 4);
        }

        stage.After(Left);
        Assert.False(alert.IsVisible);

        // .alert-wrapper's opacity: 0, and the backdrop's 0.01, once the animations are gone.
        Assert.Equal(0, alert.Wrapper.Opacity, 6);
        Assert.Equal(0.01, alert.Backdrop.Opacity, 6);
    }

    [AvaloniaFact]
    public void B2_Backdrop_Opacity_Follows_The_Alerts_Key_Then_The_Global_One()
    {
        using var stage = new Stage();
        var alert = new AvonicAlert { Buttons = ["OK"] };
        stage.Window.Resources["Avonic.BackdropOpacity"] = 0.5;
        Shown(stage, alert);
        Assert.Equal(0.5, alert.Backdrop!.Opacity, 6);
        alert.Dismiss();
        stage.After(Left);

        alert.Resources["Avonic.Alert.BackdropOpacity"] = 0.2;
        Shown(stage, alert);
        Assert.Equal(0.2, alert.Backdrop!.Opacity, 6);
    }

    // ── B3: events ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_The_Alerts_Events_Come_Before_The_Generic_Ones_With_Data_And_Role()
    {
        using var stage = new Stage();
        var alert = Watch(stage, new AvonicAlert { Header = "Alert", Buttons = ["OK"] });
        alert.Present(stage.Window);
        stage.At(Entered);
        alert.Dismiss("d", "r");
        stage.After(Left);
        Assert.Equal(
            [
                "ionAlertWillPresent@0", "willPresent@0", "ionAlertDidPresent@176", "didPresent@176",
                "ionAlertWillDismiss(d,r)@176", "willDismiss(d,r)@176", "ionAlertDidDismiss(d,r)@352", "didDismiss(d,r)@352",
            ],
            stage.Log);
    }

    // ── B4: focus at present ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Present_Focuses_The_Wrapper_And_Dismiss_Gives_Focus_Back()
    {
        using var stage = new Stage();
        stage.Opener.Focus();
        var alert = Shown(stage, new AvonicAlert { Header = "Alert", Buttons = ["Cancel", "OK"] });
        Assert.Same(alert.Wrapper, stage.Focused);

        alert.Dismiss();
        stage.After(Left);
        Assert.Same(stage.Opener, stage.Focused);
    }

    [AvaloniaFact]
    public void B4_One_Button_And_No_Inputs_Focuses_The_Button_Even_Without_KeyboardClose()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert { Header = "Alert", Buttons = ["OK"], KeyboardClose = false });
        Assert.Same(alert.ButtonControls[0], stage.Focused);
        alert.Dismiss();
        stage.After(Left);

        // An input as well: the wrapper.
        var withInput = Shown(stage, new AvonicAlert { Buttons = ["OK"], Inputs = [new AlertInput()], KeyboardClose = false });
        Assert.Same(withInput.Wrapper, stage.Focused);
    }

    // ── B5, B6, B7: buttons and values ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_A_Button_Dismisses_With_The_Values_And_Its_Role_After_Its_Handler()
    {
        using var stage = new Stage();
        object? given = "not called";
        var alert = Watch(stage, new AvonicAlert
        {
            Buttons = [new AlertButton { Text = "Delete", Role = "destructive", Handler = v => { given = v; return null; } }],
            Inputs = Radios("a", "b"),
        });
        Shown(stage, alert);
        var did = alert.OnDidDismiss();
        Tap(stage, alert.InputControls[1]);
        Tap(stage, alert.ButtonControls[0]);
        stage.After(Left);

        Assert.Equal("b", given);
        var detail = Done(did);
        Assert.Equal("destructive", detail.Role);
        var data = Assert.IsType<AlertDismissData>(detail.Data);
        Assert.Equal("b", data.Values);
        Assert.Equal(["values"], data.Keys);
    }

    [AvaloniaFact]
    public void B5_A_Handler_Returning_False_Keeps_It_Open_And_A_Dictionary_Joins_The_Data()
    {
        using var stage = new Stage();
        var keep = true;
        var calls = 0;
        var alert = new AvonicAlert
        {
            Buttons =
            [
                new AlertButton
                {
                    Text = "Save",
                    Handler = _ =>
                    {
                        calls++;
                        return keep ? false : new Dictionary<string, object?> { ["extra"] = 1 };
                    },
                },
            ],
            Inputs = [new AlertInput { Name = "first", Value = "Ann" }],
        };
        Shown(stage, alert);
        Tap(stage, alert.ButtonControls[0]);
        stage.After(Left);
        Assert.Equal(1, calls);
        Assert.True(alert.IsVisible);

        keep = false;
        var did = alert.OnDidDismiss();
        Tap(stage, alert.ButtonControls[0]);
        stage.After(Left);
        var data = Assert.IsType<AlertDismissData>(Done(did).Data);
        Assert.Equal(1, data["extra"]);
        Assert.Equal(new Dictionary<string, object?> { ["first"] = "Ann" }, data.Values);
        Assert.Null(Done(did).Role);
    }

    [AvaloniaFact]
    public void B5_A_Handlers_Task_Is_Awaited_And_Its_Result_Read()
    {
        using var stage = new Stage();
        var answer = new TaskCompletionSource<object?>();
        var alert = new AvonicAlert { Buttons = [new AlertButton { Text = "OK", Handler = _ => answer.Task }] };
        Shown(stage, alert);
        var did = alert.OnDidDismiss();
        Tap(stage, alert.ButtonControls[0]);
        stage.After(Left);
        Assert.True(alert.IsVisible);

        answer.SetResult(new Dictionary<string, object?> { ["late"] = true });
        Stage.Settle();
        stage.After(Left);
        Assert.Equal(true, ((AlertDismissData)Done(did).Data!)["late"]);
    }

    [AvaloniaFact]
    public void B5_A_Cancel_Button_Dismisses_At_Once_And_Its_Handler_Gets_No_Values()
    {
        using var stage = new Stage();
        var given = new List<object?>();
        var alert = new AvonicAlert
        {
            Buttons = [new AlertButton { Text = "Cancel", Role = "cancel", Handler = v => { given.Add(v ?? "null"); return false; } }, "OK"],
            Inputs = [new AlertInput { Name = "x", Value = "v" }],
        };
        Shown(stage, alert);
        var did = alert.OnDidDismiss();
        Tap(stage, alert.ButtonControls[0]);
        stage.After(Left);

        // M15: called once, from the will-dismiss listener, with undefined; false changes nothing.
        Assert.Equal(["null"], given);
        var detail = Done(did);
        Assert.Equal("cancel", detail.Role);
        Assert.Equal(new Dictionary<string, object?> { ["x"] = "v" }, ((AlertDismissData)detail.Data!).Values);
    }

    [AvaloniaFact]
    public void B5_A_String_Cancel_Is_The_Cancel_Role()
    {
        AlertButton cancel = "CANCEL";
        AlertButton ok = "OK";
        Assert.Equal("cancel", cancel.Role);
        Assert.Null(ok.Role);
        Assert.Null(new AlertButton { Text = "Cancel" }.Role);
    }

    [AvaloniaFact]
    public void B6_Escape_And_The_Backdrop_Call_The_Cancel_Buttons_Handler()
    {
        using var stage = new Stage();
        var calls = 0;
        AvonicAlert Make() => new()
        {
            Buttons = [new AlertButton { Text = "Cancel", Role = "cancel", Handler = _ => { calls++; return false; } }, "OK"],
        };

        var alert = Shown(stage, Make());
        var did = alert.OnDidDismiss();
        Key(stage, PhysicalKey.Escape);
        stage.After(Left);
        Assert.Equal(1, calls);
        Assert.Equal(OverlayRoles.Backdrop, Done(did).Role);

        alert = Shown(stage, Make());
        stage.Click(new Point(10, 10));
        stage.After(Left);
        Assert.Equal(2, calls);

        // A dismiss with another role doesn't.
        alert = Shown(stage, Make());
        alert.Dismiss(null, "other");
        stage.After(Left);
        Assert.Equal(2, calls);
    }

    [AvaloniaFact]
    public void B7_Values_Of_Each_Kind()
    {
        using var stage = new Stage();

        var none = Shown(stage, new AvonicAlert { Buttons = ["OK"] });
        Assert.Null(none.GetValues());
        none.Dismiss();
        stage.After(Left);

        var radios = Radios("a", "b");
        radios[1].Checked = true;
        var radio = Shown(stage, new AvonicAlert { Inputs = radios });
        Assert.Equal("b", radio.GetValues());
        radio.Dismiss();
        stage.After(Left);

        var unchecked_ = Shown(stage, new AvonicAlert { Inputs = Radios("a") });
        Assert.Null(unchecked_.GetValues());
        unchecked_.Dismiss();
        stage.After(Left);

        var boxes = Checkboxes("a", "b", "c");
        boxes[0].Checked = boxes[2].Checked = true;
        var checkbox = Shown(stage, new AvonicAlert { Inputs = boxes });
        Assert.Equal(new object?[] { "a", "c" }, checkbox.GetValues());
        checkbox.Dismiss();
        stage.After(Left);

        var text = Shown(stage, new AvonicAlert
        {
            Inputs = [new AlertInput { Name = "first", Value = "Ann" }, new AlertInput(), new AlertInput { Type = AlertInputType.Textarea, Value = "t" }],
        });
        Assert.Equal(new Dictionary<string, object?> { ["first"] = "Ann", ["1"] = "", ["2"] = "t" }, text.GetValues());
        text.Dismiss();
        stage.After(Left);

        // i.value || '': every falsy value is ''.
        var falsy = Shown(stage, new AvonicAlert
        {
            Inputs = [new AlertInput { Name = "zero", Value = 0 }, new AlertInput { Name = "no", Value = false }, new AlertInput { Name = "one", Value = 1 }],
        });
        Assert.Equal(new Dictionary<string, object?> { ["zero"] = "", ["no"] = "", ["one"] = 1 }, falsy.GetValues());
    }

    // ── B8: radios ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_A_Tap_Checks_One_Radio_And_Calls_Its_Handler_With_The_Copy()
    {
        using var stage = new Stage();
        var inputs = Radios("a", "b", "c");
        inputs[0].Checked = true;
        AlertInput? handled = null;
        inputs[2].Handler = i => handled = i;
        var alert = Shown(stage, new AvonicAlert { Inputs = inputs });
        var options = alert.InputControls.Cast<AlertRadioButton>().ToList();
        Assert.True(options[0].IsChecked);

        Tap(stage, options[2]);
        Assert.Equal([false, false, true], options.Select(o => o.IsChecked));
        Assert.NotNull(handled);
        Assert.NotSame(inputs[2], handled);
        Assert.True(handled!.Checked);

        // The app's objects aren't changed.
        Assert.True(inputs[0].Checked);
        Assert.False(inputs[2].Checked);
    }

    [AvaloniaFact]
    public void B8_Only_The_Checked_Or_First_Enabled_Radio_Is_A_Tab_Stop()
    {
        using var stage = new Stage();
        var inputs = Radios("a", "b", "c");
        inputs[0].Disabled = true;
        var alert = Shown(stage, new AvonicAlert { Inputs = inputs });
        var options = alert.InputControls.Cast<AlertRadioButton>().ToList();
        Assert.Equal([false, true, false], options.Select(KeyboardNavigation.GetIsTabStop));

        Tap(stage, options[2]);
        Assert.Equal([false, false, true], options.Select(KeyboardNavigation.GetIsTabStop));
        alert.Dismiss();
        stage.After(Left);

        inputs[2].Checked = true;
        alert = Shown(stage, new AvonicAlert { Inputs = inputs });
        Assert.Equal([false, false, true], alert.InputControls.Select(KeyboardNavigation.GetIsTabStop));
    }

    [AvaloniaFact]
    public void B8_Arrows_Check_And_Focus_The_Next_Enabled_Radio_Wrapping()
    {
        using var stage = new Stage();
        var inputs = Radios("red", "blue", "off", "green");
        inputs[1].Checked = true;
        inputs[2].Disabled = true;
        var alert = Shown(stage, new AvonicAlert { Buttons = ["Cancel", "OK"], Inputs = inputs });
        var options = alert.InputControls.Cast<AlertRadioButton>().ToList();

        // M12, M13: Tab from the wrapper goes to the checked one; Down skips the disabled one; Down wraps; Up wraps back.
        Key(stage, PhysicalKey.Tab);
        Assert.Same(options[1], stage.Focused);
        Key(stage, PhysicalKey.ArrowDown);
        Assert.Same(options[3], stage.Focused);
        Assert.True(options[3].IsChecked);
        Key(stage, PhysicalKey.ArrowDown);
        Assert.Same(options[0], stage.Focused);
        Assert.Equal("red", alert.GetValues());
        Key(stage, PhysicalKey.ArrowUp);
        Assert.Same(options[3], stage.Focused);
        Key(stage, PhysicalKey.ArrowRight);
        Assert.Same(options[0], stage.Focused);
        Key(stage, PhysicalKey.ArrowLeft);
        Assert.Same(options[3], stage.Focused);
        Assert.Equal("green", alert.GetValues());
    }

    [AvaloniaFact]
    public void B8_Enter_And_Space_Check_A_Radio()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert { Inputs = Radios("a", "b") });
        var options = alert.InputControls.Cast<AlertRadioButton>().ToList();
        options[1].Focus();
        Key(stage, PhysicalKey.Enter);
        Assert.Equal("b", alert.GetValues());
        options[0].Focus();
        Key(stage, PhysicalKey.Space);
        Assert.Equal("a", alert.GetValues());
    }

    // ── B9: checkboxes ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_An_Input_Handlers_Exception_Is_Logged_And_The_Options_Still_Show_The_Pick()
    {
        var sink = new Sink();
        var previous = Avalonia.Logging.Logger.Sink;
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            using var stage = new Stage();
            var inputs = Radios("a", "b");
            inputs[1].Handler = _ => throw new InvalidOperationException("app");
            var alert = Shown(stage, new AvonicAlert { Inputs = inputs });
            Tap(stage, alert.InputControls[1]);
            Assert.True(((AlertRadioButton)alert.InputControls[1]).IsChecked);
            Assert.Contains(sink.Messages, m => m.Contains("input handler failed", StringComparison.Ordinal));
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    [AvaloniaFact]
    public void B9_A_Tap_Or_Space_Toggles_A_Checkbox_And_Enter_Does_Not()
    {
        using var stage = new Stage();
        var inputs = Checkboxes("a", "b");
        var handled = new List<bool>();
        inputs[0].Handler = i => handled.Add(i.Checked);
        var alert = Shown(stage, new AvonicAlert { Buttons = ["OK"], Inputs = inputs });
        var options = alert.InputControls.Cast<AlertCheckboxButton>().ToList();

        Tap(stage, options[0]);
        Assert.True(options[0].IsChecked);
        Assert.Equal([true], handled);

        options[0].Focus();
        Key(stage, PhysicalKey.Enter);
        Assert.True(options[0].IsChecked);
        Key(stage, PhysicalKey.Space);
        Assert.False(options[0].IsChecked);
        Assert.Equal([true, false], handled);

        // Every checkbox is a tab stop.
        Assert.All(options, o => Assert.True(KeyboardNavigation.GetIsTabStop(o)));
    }

    // ── B10, B13: text fields ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_Typing_Sets_The_Value_And_The_Field_Takes_The_Inputs_Settings()
    {
        using var stage = new Stage();
        var source = new AlertInput { Name = "name", Placeholder = "Name", Id = "name-field" };
        var alert = Shown(stage, new AvonicAlert
        {
            Buttons = ["OK"],
            Inputs =
            [
                source,
                new AlertInput { Type = AlertInputType.Password, Placeholder = "Secret" },
                new AlertInput { Attributes = new Dictionary<string, object?> { ["maxlength"] = 3, ["readonly"] = true, ["aria-label"] = "Code" } },
                new AlertInput { Disabled = true },
                new AlertInput { Disabled = true, Attributes = new Dictionary<string, object?> { ["disabled"] = false } },
                new AlertInput { Type = AlertInputType.Textarea },
            ],
        });
        var fields = alert.InputControls.Cast<AlertTextField>().ToList();
        Assert.IsType<AlertTextareaInput>(fields[5]);
        Assert.IsType<TextareaNativeInput>(fields[5].Native);

        fields[0].Native!.Focus();
        stage.Window.KeyTextInput("Ann");
        Stage.Settle();
        Assert.Equal("Ann", ((Dictionary<string, object?>)alert.GetValues()!)["name"]);
        Assert.Null(source.Value);
        Assert.Equal("Name", fields[0].Native!.PlaceholderText);
        Assert.Equal("name-field", AutomationProperties.GetAutomationId(fields[0].Native!));
        Assert.Equal("Name", AutomationProperties.GetName(fields[0].Native!));

        Assert.Equal(NativeTextField.PasswordMask, fields[1].Native!.PasswordChar);
        Assert.Equal(3, fields[2].Native!.MaxLength);
        Assert.True(fields[2].Native!.IsReadOnly);
        Assert.Equal("Code", AutomationProperties.GetName(fields[2].Native!));
        Assert.False(fields[3].IsEnabled);
        Assert.True(fields[4].IsEnabled);

        // ::placeholder: var(ion-placeholder-color), on the input and the textarea alike.
        var placeholder = ((ISolidColorBrush)alert.FindResource("Avonic.PlaceholderColorBrush")!).Color;
        Assert.Equal(placeholder, ((ISolidColorBrush)fields[0].Native!.PlaceholderForeground!).Color);
        Assert.Equal(placeholder, ((ISolidColorBrush)fields[5].Native!.PlaceholderForeground!).Color);
    }

    [AvaloniaFact]
    public void B13_Enter_In_A_Text_Field_Does_Not_Dismiss()
    {
        using var stage = new Stage();
        var alert = Watch(stage, new AvonicAlert { Buttons = ["OK"], Inputs = [new AlertInput { Name = "x" }] });
        Shown(stage, alert);
        stage.Log.Clear();
        ((AlertTextField)alert.InputControls[0]).Native!.Focus();
        Key(stage, PhysicalKey.Enter);
        stage.After(Left);
        Assert.Empty(stage.Log);
        Assert.True(alert.IsVisible);
    }

    // ── B11: Tab ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Tab_Goes_Through_The_Inputs_And_Buttons_And_Wraps_And_Shift_Tab_On_The_Wrapper_Goes_To_The_Last_Button()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert { Buttons = ["One", "Two"], Inputs = [new AlertInput { Placeholder = "x" }] });
        var field = ((AlertTextField)alert.InputControls[0]).Native!;
        Assert.Same(alert.Wrapper, stage.Focused);

        Key(stage, PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.Same(alert.ButtonControls[1], stage.Focused);
        Key(stage, PhysicalKey.Tab);
        Assert.Same(field, stage.Focused);
        Key(stage, PhysicalKey.Tab);
        Assert.Same(alert.ButtonControls[0], stage.Focused);

        // Back: the field, the wrapper (a tab stop of its own), then the last button again.
        Key(stage, PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.Same(field, stage.Focused);
        Key(stage, PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.Same(alert.Wrapper, stage.Focused);
        Key(stage, PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.Same(alert.ButtonControls[1], stage.Focused);
    }

    // ── B12: backdropDismiss ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_BackdropDismiss_False_Stops_The_Tap_And_Escape()
    {
        using var stage = new Stage();
        var alert = Watch(stage, new AvonicAlert { Buttons = ["OK"], BackdropDismiss = false });
        Shown(stage, alert);
        stage.Log.Clear();
        Assert.False(alert.Backdrop!.Tappable);
        stage.Click(new Point(10, 10));
        Key(stage, PhysicalKey.Escape);
        stage.After(Left);
        Assert.Empty(stage.Log);

        alert.BackdropDismiss = true;
        stage.Click(new Point(10, 10));
        stage.After(Left);
        Assert.Contains(stage.Log, l => l.StartsWith("didDismiss(,backdrop)", StringComparison.Ordinal));
    }

    // ── B14: the button group ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_The_Group_Is_A_Column_For_Three_Buttons_Or_Two_That_Wrap_Until_The_Buttons_Change()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert { Buttons = ["Cancel", "OK"] });
        Assert.False(alert.ButtonGroup!.Vertical);
        alert.Buttons = ["A", "B", "C"];
        Stage.Settle();
        Assert.True(alert.ButtonGroup.Vertical);

        alert.Buttons = ["A rather long cancel button", "Confirm the long thing"];
        stage.Render();
        Assert.False(alert.ButtonGroup.Vertical);
        stage.After(16);
        stage.Render();
        Assert.True(alert.ButtonGroup.Vertical);

        // Latched: the column fits, and stays.
        stage.After(32);
        stage.Render();
        Assert.True(alert.ButtonGroup.Vertical);

        alert.Buttons = ["Cancel", "OK"];
        stage.After(32);
        stage.Render();
        Assert.False(alert.ButtonGroup.Vertical);
    }

    [AvaloniaFact]
    public void B14_A_Row_Sits_At_The_End_And_Wraps_Upwards()
    {
        var group = new AlertButtonGroup { Width = 200, Padding = new Thickness(8) };
        group.Children.Add(new Border { Width = 120, Height = 40 });
        group.Children.Add(new Border { Width = 100, Height = 40 });
        group.Measure(new Size(200, double.PositiveInfinity));
        group.Arrange(new Rect(0, 0, 200, group.DesiredSize.Height));

        // wrap-reverse: the first at the bottom; flex-end: each line at the end.
        Assert.Equal(96, group.DesiredSize.Height);
        Assert.Equal(new Rect(72, 48, 120, 40), group.Children[0].Bounds);
        Assert.Equal(new Rect(92, 8, 100, 40), group.Children[1].Bounds);
    }

    [AvaloniaFact]
    public void B14_A_Narrow_Pass_On_The_Way_Does_Not_Make_A_Column()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert { Buttons = ["Cancel", "OK"] });

        // A pass too narrow for the row, then the real layout: the frame's check reads the row again, which fits.
        alert.ButtonGroup!.Measure(new Size(60, double.PositiveInfinity));
        alert.ButtonGroup.InvalidateMeasure();
        stage.Render();
        stage.After(32);
        stage.Render();
        Assert.False(alert.ButtonGroup.Vertical);
    }

    [AvaloniaFact]
    public void B21_Focus_On_A_Button_Stays_In_The_Alert_When_The_Buttons_Change()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert { Buttons = ["Cancel", "OK"] });
        alert.ButtonControls[1].Focus();
        alert.Buttons = ["Cancel", "OK", "More"];
        Stage.Settle();
        Assert.Same(alert.ButtonControls[1], stage.Focused);
    }

    [AvaloniaFact]
    public void B5_A_Handlers_ValueTask_False_Keeps_It_Open()
    {
        using var stage = new Stage();
        var alert = new AvonicAlert { Buttons = [new AlertButton { Text = "OK", Handler = _ => new ValueTask<bool>(false) }] };
        Shown(stage, alert);
        Tap(stage, alert.ButtonControls[0]);
        stage.After(Left);
        Assert.True(alert.IsVisible);
    }

    [AvaloniaFact]
    public void B18_CssClass_Leaves_The_Apps_Own_Classes()
    {
        var alert = new AvonicAlert();
        alert.Classes.Add("wide");
        alert.CssClass = "wide custom";
        alert.CssClass = "custom";
        Assert.Contains("wide", alert.Classes);
        alert.CssClass = null;
        Assert.DoesNotContain("custom", alert.Classes);
    }

    [AvaloniaFact]
    public void B10_A_Number_Value_Is_Written_As_JavaScript_Does_Whatever_The_Culture()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            using var stage = new Stage();
            var alert = Shown(stage, new AvonicAlert { Inputs = [new AlertInput { Type = AlertInputType.Number, Value = 1.5 }] });
            Assert.Equal("1.5", ((AlertTextField)alert.InputControls[0]).Native!.Text);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    // ── B15: layout ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_Vw_And_Vh_Are_The_Windows_Width_And_Height_Whatever_The_Axis()
    {
        var viewport = new Size(400, 800);
        Assert.Equal(400, CssLength.Read("50vh", 400, viewport));
        Assert.Equal(320, CssLength.Read("80vw", 800, viewport));
    }

    [AvaloniaFact]
    public void B15_In_A_Narrow_Window_A_Long_Header_Makes_The_Wrapper_As_Wide_As_The_Room()
    {
        // fit-content: min(max-content, room). Measured only at the room, wrapped text would be its widest line, narrower.
        using var stage = new Stage(266, 844);
        var alert = Shown(stage, new AvonicAlert { Header = "A header long enough to wrap onto lines of its own", Buttons = ["OK"] });
        Assert.Equal(266, alert.Wrapper!.Bounds.Width, 3);
    }

    [AvaloniaFact]
    public void B15_The_Wrappers_Max_Height_Shrinks_The_Scrollers_In_Proportion()
    {
        var column = new AlertColumn();
        var head = new Border { Height = 60 };
        var message = new Border { Child = new Border { Height = 300 } };
        var group = new Border { Child = new Border { Height = 100 } };
        AlertColumn.SetShrinks(message, true);
        AlertColumn.SetShrinks(group, true);
        column.Children.AddRange([head, message, group]);
        column.Measure(new Size(250, 260));
        column.Arrange(new Rect(0, 0, 250, 260));
        Assert.Equal(60, head.Bounds.Height);
        Assert.Equal(150, message.Bounds.Height);
        Assert.Equal(50, group.Bounds.Height);
        Assert.Equal(210, group.Bounds.Y);
    }

    [AvaloniaFact]
    public void B15_An_Apps_Width_Keys_Win()
    {
        using var stage = new Stage();
        var alert = new AvonicAlert { Header = "Alert", Buttons = ["OK"] };
        alert.Resources["Avonic.Alert.Width"] = "300px";
        alert.Resources["Avonic.Alert.MaxWidth"] = 320d;
        Shown(stage, alert);
        Assert.Equal(300, alert.Wrapper!.Bounds.Width);

        alert.Resources["Avonic.Alert.Width"] = "auto";
        alert.Resources["Avonic.Alert.MinWidth"] = 100d;
        stage.Render();
        Assert.True(alert.Wrapper.Bounds.Width < 200);
    }

    [AvaloniaTheory]
    [InlineData("min(100vw - 96px, 560px)", 1024d, 560d)]
    [InlineData("min(100vw - 96px, 560px)", 600d, 504d)]
    [InlineData("90%", 800d, 720d)]
    [InlineData("12px", 800d, 12d)]
    [InlineData("100% + 4px", 100d, 104d)]
    [InlineData("100% - 10px + 4px", 100d, 94d)]
    [InlineData("calc(100% - 32px)", 400d, 368d)]
    [InlineData("auto", 100d, null)]
    [InlineData("none", 100d, null)]
    [InlineData("calc(min(80px, 300px) - min(10px, 4px))", 100d, 76d)]
    [InlineData("4px + min(100vw - 96px, 560px)", 600d, 508d)]
    [InlineData("min(min(1px, 2px), 3px)", 100d, 1d)]
    [InlineData("max(10px, 20%)", 200d, 40d)]
    [InlineData("clamp(250px, calc(100% - 40px), 400px)", 1024d, 400d)]
    [InlineData("clamp(250px, calc(100% - 40px), 400px)", 200d, 250d)]
    [InlineData("calc(100% - (10px + 4px))", 100d, 86d)]
    [InlineData("calc(100% * 2)", 100d, null)]
    [InlineData("min(10px, auto)", 100d, null)]
    [InlineData("calc(100%\n  - 32px)", 400d, 368d)]
    [InlineData("infinity", 100d, null)]
    [InlineData("calc(100% - NaNpx)", 100d, null)]
    [InlineData("100% - -10px", 100d, 110d)]
    public void B15_Sizes_Read_As_Css(string value, double basis, double? expected) =>
        Assert.Equal(expected, CssLength.Read(value, basis));

    // ── B16, B17: head and message ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B16_The_Title_And_Sub_Title_Show_When_Set_And_The_Head_Keeps_Its_Padding()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert { Message = "Only a message.", Buttons = ["OK", "Cancel"] });
        var head = alert.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Head");
        var title = alert.GetVisualDescendants().OfType<TextBlock>().First(b => b.Name == "PART_Title");
        var sub = alert.GetVisualDescendants().OfType<TextBlock>().First(b => b.Name == "PART_SubTitle");

        // M6: the head is 35 with neither.
        Assert.False(title.IsVisible);
        Assert.False(sub.IsVisible);
        Assert.Equal(35, head.Bounds.Height);

        alert.SubHeader = "Sub";
        stage.Render();
        Assert.True(sub.IsVisible);
        Assert.Equal(2, AutomationProperties.GetHeadingLevel(sub));
        alert.Header = "Head";
        stage.Render();
        Assert.True(title.IsVisible);
        Assert.Equal(3, AutomationProperties.GetHeadingLevel(sub));
        Assert.Equal(2, AutomationProperties.GetHeadingLevel(title));
    }

    [AvaloniaFact]
    public void B17_A_String_Message_Is_Text_A_Control_Is_Content_And_None_Takes_No_Room()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert { Header = "H", Message = "Text", Buttons = ["OK"] });
        var message = alert.GetVisualDescendants().OfType<ScrollViewer>().First(b => b.Name == "PART_Message");
        Assert.Contains(message.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Text");
        Assert.Equal(39, message.Bounds.Height);

        var own = new Border { Width = 10, Height = 50 };
        alert.Message = own;
        stage.Render();
        Assert.True(own.IsAttachedToVisualTree());
        Assert.Equal(70, message.Bounds.Height);

        alert.Message = null;
        stage.Render();
        Assert.False(message.IsVisible);
        alert.Message = "";
        stage.Render();
        Assert.False(message.IsVisible);
    }

    // ── B18, B19: classes and automation ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_CssClass_Reaches_The_Alert_Its_Buttons_And_Inputs_And_A_Role_Is_A_PseudoClass()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert
        {
            CssClass = "one two",
            Buttons = ["cancel", new AlertButton { Text = "Delete", Role = "destructive", CssClass = "x y" }],
            Inputs = [new AlertInput { CssClass = "field", Attributes = new Dictionary<string, object?> { ["class"] = "extra" } }],
        });
        Assert.Contains("one", alert.Classes);
        Assert.Contains("two", alert.Classes);
        alert.CssClass = "three";
        Assert.DoesNotContain("one", alert.Classes);
        Assert.Contains("three", alert.Classes);

        Assert.Contains(":role-cancel", alert.ButtonControls[0].Classes);
        Assert.Contains(":role-destructive", alert.ButtonControls[1].Classes);
        Assert.Contains("x", alert.ButtonControls[1].Classes);
        Assert.Contains("y", alert.ButtonControls[1].Classes);
        Assert.Contains("field", alert.InputControls[0].Classes);
        Assert.Contains("extra", alert.InputControls[0].Classes);
    }

    [AvaloniaFact]
    public void B19_Automation_Names_The_Dialog_Its_Buttons_And_Options()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert
        {
            Header = "Head",
            SubHeader = "Sub",
            Message = "Body",
            HtmlAttributes = new Dictionary<string, object?> { ["aria-label"] = "Label", ["id"] = "dialog" },
            Buttons =
            [
                new AlertButton { Text = "OK", Id = "ok", HtmlAttributes = new Dictionary<string, object?> { ["aria-label"] = "Accept" } },
                new AlertButton { Text = "Off", HtmlAttributes = new Dictionary<string, object?> { ["disabled"] = true } },
            ],
            Inputs = [new AlertInput { Type = AlertInputType.Checkbox, Label = "Box", Checked = true, Id = "box" }],
        });
        var wrapper = ControlAutomationPeer.CreatePeerForElement(alert.Wrapper!);
        Assert.Equal(AutomationControlType.Window, wrapper.GetAutomationControlType());
        Assert.Equal("Head Sub", wrapper.GetName());
        Assert.Equal("Body", wrapper.GetHelpText());
        Assert.Equal("dialog", wrapper.GetAutomationId());
        alert.Header = alert.SubHeader = null;
        Assert.Equal("Label", wrapper.GetName());

        var ok = ControlAutomationPeer.CreatePeerForElement(alert.ButtonControls[0]);
        Assert.Equal(AutomationControlType.Button, ok.GetAutomationControlType());
        Assert.Equal("Accept", ok.GetName());
        Assert.Equal("ok", ok.GetAutomationId());
        Assert.Equal("Off", ControlAutomationPeer.CreatePeerForElement(alert.ButtonControls[1]).GetName());
        Assert.False(alert.ButtonControls[1].IsEnabled);

        var box = ControlAutomationPeer.CreatePeerForElement((Control)alert.InputControls[0]);
        Assert.Equal(AutomationControlType.CheckBox, box.GetAutomationControlType());
        Assert.Equal("Box", box.GetName());
        Assert.Equal("box", box.GetAutomationId());
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(box);
        Assert.Equal(ToggleState.On, toggle.ToggleState);
        toggle.Toggle();
        Assert.Equal(ToggleState.Off, toggle.ToggleState);
    }

    [AvaloniaFact]
    public void B19_Default_Input_Ids_Use_The_Overlay_Index()
    {
        using var stage = new Stage();
        var alert = Shown(stage, new AvonicAlert { Inputs = Radios("a", "b") });
        var index = alert.OverlayIndex;
        Assert.Equal($"alert-input-{index}-1", AutomationProperties.GetAutomationId((Control)alert.InputControls[1]));
    }

    // ── B20, B21: isOpen, trigger, changes while open ───────────────────────────────────────

    [AvaloniaFact]
    public void B20_IsOpen_And_Trigger_Present_It()
    {
        using var stage = new Stage();
        var scope = new NameScope();
        scope.Register("Opener", stage.Opener);
        NameScope.SetNameScope(stage.Page, scope);
        var alert = Watch(stage, new AvonicAlert { Trigger = "Opener", Buttons = ["OK"] });
        stage.Page.Children.Add(alert);
        Stage.Settle();

        stage.Opener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        stage.At(Entered);
        Assert.Contains("didPresent@176", stage.Log);
        Tap(stage, alert.ButtonControls[0]);
        stage.After(Left);
        Assert.Contains(alert, stage.Page.Children);
        stage.Log.Clear();

        alert.IsOpen = true;
        stage.After(Entered);
        Assert.Contains(stage.Log, l => l.StartsWith("didPresent", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void B21_Buttons_And_Inputs_Changed_While_Open_Render_Again()
    {
        using var stage = new Stage();
        // XAML adds to the lists the alert starts with, which it follows; a list of the app's is followed if it can say it changed.
        var alert = new AvonicAlert { Inputs = Radios("a") };
        alert.Buttons!.Add("OK");
        Shown(stage, alert);
        Assert.Single(alert.ButtonControls);
        alert.Buttons.Add("Cancel");
        Stage.Settle();
        Assert.Equal(2, alert.ButtonControls.Count);
        alert.Inputs = new Avalonia.Collections.AvaloniaList<AlertInput>(Radios("x", "y", "z"));
        Assert.Equal(3, alert.InputControls.Count);
        alert.Inputs.Add(new AlertInput { Type = AlertInputType.Radio, Label = "w" });
        alert.Inputs.Add(new AlertInput { Type = AlertInputType.Radio, Label = "v" });
        Assert.Equal(3, alert.InputControls.Count);
        Stage.Settle();
        Assert.Equal(5, alert.InputControls.Count);
        Assert.Equal("w", ((AlertRadioButton)alert.InputControls[3]).Label);
    }

    [AvaloniaFact]
    public void Mixed_Radios_And_Checkboxes_Warn_And_Render_As_The_First()
    {
        var sink = new Sink();
        var previous = Avalonia.Logging.Logger.Sink;
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            using var stage = new Stage();
            var alert = Shown(stage, new AvonicAlert
            {
                Inputs = [new AlertInput { Type = AlertInputType.Radio, Label = "r" }, new AlertInput { Type = AlertInputType.Checkbox, Label = "c" }],
            });
            Assert.Contains(sink.Messages, m => m.Contains("cannot mix input types", StringComparison.Ordinal));
            Assert.All(alert.InputControls, c => Assert.IsType<AlertRadioButton>(c));
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    private sealed class Sink : Avalonia.Logging.ILogSink
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(Avalonia.Logging.LogEventLevel level, string area) => level >= Avalonia.Logging.LogEventLevel.Warning;

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate) => Messages.Add(messageTemplate);

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Messages.Add(messageTemplate);
    }

    // ── The look ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Look_A_Radios_Dot_Grows_Through_Its_Transition_But_Not_At_First()
    {
        using var stage = new Stage();
        var inputs = Radios("a", "b");
        inputs[1].Checked = true;
        var alert = Shown(stage, new AvonicAlert { Inputs = inputs });
        var dots = alert.GetVisualDescendants().OfType<Border>().Where(b => b.Name == "PART_Dot").ToList();

        // Checked from the start: drawn at full size, as CSS doesn't transition a first style.
        Assert.Equal(1, Scale(dots[1]), 3);

        // transition: transform 280ms: a tap starts it from 0. Transitions run on real time here, so only its start is read.
        Tap(stage, alert.InputControls[0]);
        Assert.True(Scale(dots[0]) < 0.5, $"The dot is at {Scale(dots[0])} at once.");
    }

    [AvaloniaFact]
    public void Look_Checked_Radio_Shows_Its_Dot_In_The_Primary_And_A_Focused_Button_Is_Grey()
    {
        using var stage = new Stage();
        var inputs = Radios("a", "b");
        inputs[1].Checked = true;
        var alert = Shown(stage, new AvonicAlert { Buttons = ["OK", "Cancel"], Inputs = inputs });
        var dots = alert.GetVisualDescendants().OfType<Border>().Where(b => b.Name == "PART_Dot").ToList();
        Assert.Equal(0, Scale(dots[0]), 3);
        Assert.Equal(1, Scale(dots[1]), 3);
        var primary = (Color)alert.FindResource("Avonic.Color.Primary")!;
        Assert.Equal(primary, ((ISolidColorBrush)dots[1].Background!).Color);

        Key(stage, PhysicalKey.Tab);
        Key(stage, PhysicalKey.Tab);
        Assert.Same(alert.ButtonControls[0], stage.Focused);
        Assert.Equal((Color)alert.FindResource("Avonic.BackgroundColorStep100")!, ((ISolidColorBrush)alert.ButtonControls[0].Background!).Color);
    }
}
