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
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;
using Stage = Avonic.Components.Tests.Controls.AvonicPopoverTests.Stage;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-select</c>'s interfaces, md, on the test clock: B4 to B7 and B17 to B25 in docs/components/ion-select/PLAN.md, from
/// <c>select.js</c>, <c>select-popover.js</c>, <c>select-modal.js</c> and the measurements in <c>ref/</c>. Each overlay's own
/// animations are its own tests'; here they're waited out.
/// </summary>
public class AvonicSelectInterfaceTests
{
    /// <summary>Longer than any interface's enter or leave: the popover's 300 ms, the modal's 280.</summary>
    private const double Settled = 600;

    /// <summary>A select of the four options in a list on the stage's page, and a log of its events.</summary>
    private static AvonicSelect Mount(Stage stage, Action<AvonicSelect>? set = null, bool inItem = true)
    {
        var select = AvonicSelectCaptureTests.Select("s", set);
        select.Label ??= "Fruit";
        var page = new StackPanel { Width = 390 };
        page.Children.Add(inItem
            ? new AvonicList { Items = { new AvonicItem { Content = select } } }
            : new Border { Padding = new Thickness(16, 0), Child = select });

        stage.Page.Children.Add(page);
        select.IonChange += (_, e) => stage.Log.Add($"ionChange({Show(e.Value)})");
        select.IonCancel += (_, _) => stage.Log.Add("ionCancel");
        select.IonDismiss += (_, _) => stage.Log.Add("ionDismiss");
        Stage.Settle();
        stage.Render();
        return select;
    }

    private static string Show(object? value) => value switch
    {
        null => "null",
        object?[] values => "[" + string.Join(",", values.Select(Show)) + "]",
        _ => value.ToString()!,
    };

    private static Point Centre(Visual visual, Stage stage) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), stage.Window)!.Value;

    private static void Tap(Stage stage, Visual visual)
    {
        stage.Render();
        stage.Click(Centre(visual, stage));
    }

    /// <summary>Taps the select, and waits for its interface to be in.</summary>
    private static T Opened<T>(Stage stage, AvonicSelect select) where T : AvonicOverlay
    {
        Tap(stage, select);
        stage.After(Settled);
        stage.Render();
        return Assert.IsType<T>(select.Overlay);
    }

    private static void Key(Stage stage, PhysicalKey key)
    {
        stage.Window.KeyPressQwerty(key, RawInputModifiers.None);
        Stage.Settle();
        stage.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Stage.Settle();
    }

    private static List<AvonicItem> Rows(AvonicOverlay overlay) =>
        overlay.GetVisualDescendants().OfType<AvonicItem>().Where(item => item.Classes.Contains(AvonicSelect.OptionClass)).ToList();

    // ── B17: the alert ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_Alert_Has_The_Label_As_Header_A_Radio_Per_Option_And_Cancel_And_OK()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => s.Value = "apple");
        var alert = Opened<AvonicAlert>(stage, select);

        Assert.Equal("Fruit", alert.Header);
        Assert.Equal("select-alert single-select-alert", alert.CssClass);
        var inputs = alert.Inputs!;
        Assert.Equal(["Apple", "Banana", "Cherry", "Grape"], inputs.Select(i => i.Label));
        Assert.All(inputs, i => Assert.Equal(AlertInputType.Radio, i.Type));
        Assert.Equal([true, false, false, false], inputs.Select(i => i.Checked));
        Assert.Equal([false, false, true, false], inputs.Select(i => i.Disabled));
        Assert.All(inputs, i => Assert.Equal(AvonicSelect.OptionClass, i.CssClass));
        Assert.Equal(["Cancel", "OK"], alert.Buttons!.Select(b => b.Text));
        Assert.Equal("cancel", alert.Buttons![0].Role);
    }

    [AvaloniaFact]
    public void B17_OK_Sets_The_Value_With_IonChange_Then_IonDismiss_And_Focus_Comes_Back()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => s.Value = "apple");
        var alert = Opened<AvonicAlert>(stage, select);

        Tap(stage, alert.InputControls[1]);
        Tap(stage, alert.ButtonControls[1]);
        stage.After(Settled);

        Assert.Equal("banana", select.Value);
        Assert.Equal(["ionChange(banana)", "ionDismiss"], stage.Log);
        Assert.Null(select.Overlay);
        Assert.Same(select, stage.Focused);
        Assert.Equal("Banana", select.DisplayText);
    }

    [AvaloniaFact]
    public void B17_Cancel_And_The_Backdrop_Raise_IonCancel_And_No_Change()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => s.Value = "apple");
        var alert = Opened<AvonicAlert>(stage, select);
        Tap(stage, alert.InputControls[1]);
        Tap(stage, alert.ButtonControls[0]);
        stage.After(Settled);
        Assert.Equal(["ionCancel", "ionDismiss"], stage.Log);
        Assert.Equal("apple", select.Value);

        stage.Log.Clear();
        alert = Opened<AvonicAlert>(stage, select);
        Key(stage, PhysicalKey.Escape);
        stage.After(Settled);
        Assert.Equal(["ionCancel", "ionDismiss"], stage.Log);
    }

    [AvaloniaFact]
    public void B17_Multiple_Uses_Checkboxes_And_Sets_A_List()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Multiple = true; s.Value = new object?[] { "grape" }; });
        var alert = Opened<AvonicAlert>(stage, select);

        Assert.Equal("select-alert multiple-select-alert", alert.CssClass);
        Assert.All(alert.Inputs!, i => Assert.Equal(AlertInputType.Checkbox, i.Type));
        Assert.Equal([false, false, false, true], alert.Inputs!.Select(i => i.Checked));
        Tap(stage, alert.InputControls[0]);
        Tap(stage, alert.ButtonControls[1]);
        stage.After(Settled);

        Assert.Equal(["ionChange([apple,grape])", "ionDismiss"], stage.Log);
        Assert.Equal("Apple, Grape", select.DisplayText);
    }

    [AvaloniaFact]
    public void B7_The_Same_Value_Again_Raises_No_IonChange()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Multiple = true; s.Value = new object?[] { "grape", "apple" }; });
        var alert = Opened<AvonicAlert>(stage, select);
        Tap(stage, alert.ButtonControls[1]);
        stage.After(Settled);

        // isValueEqual: the same values in another order.
        Assert.Equal(["ionDismiss"], stage.Log);
    }

    // ── B18: the action sheet ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_Action_Sheet_Has_A_Radio_Button_Per_Option_And_Cancel()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.ActionSheet; s.Value = "banana"; });
        var sheet = Opened<AvonicActionSheet>(stage, select);

        Assert.Equal("select-action-sheet", sheet.CssClass);
        Assert.Equal(["Apple", "Banana", "Cherry", "Grape", "Cancel"], sheet.Buttons!.Select(b => b.Text));
        Assert.Equal(["false", "true", "false", "false"], sheet.Buttons!.Take(4).Select(b => (string?)b.HtmlAttributes!["aria-checked"]));
        Assert.All(sheet.Buttons!.Take(4), b => Assert.Equal("radio", b.HtmlAttributes!["role"]));
        Assert.True(sheet.Buttons![2].Disabled);

        Tap(stage, sheet.ButtonControls[3]);
        stage.After(Settled);
        Assert.Equal(["ionChange(grape)", "ionDismiss"], stage.Log);

        stage.Log.Clear();
        sheet = Opened<AvonicActionSheet>(stage, select);
        Tap(stage, sheet.AllButtonControls.Last());
        stage.After(Settled);
        Assert.Equal(["ionCancel", "ionDismiss"], stage.Log);
    }

    // ── B5: which interface ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_An_Action_Sheet_With_Multiple_Is_An_Alert_And_A_Popover_With_No_Event_Is_An_Alert()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.ActionSheet; s.Multiple = true; });
        Opened<AvonicAlert>(stage, select);
        Key(stage, PhysicalKey.Escape);
        stage.After(Settled);

        select.Multiple = false;
        select.Interface = SelectInterface.Popover;
        var task = select.Open();
        stage.After(Settled);
        Assert.True(task.IsCompleted);
        Assert.IsType<AvonicAlert>(task.Result);
    }

    // ── B19, B21, B22: the popover ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_Popover_Is_Centred_At_The_Container_With_No_Backdrop()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Value = "apple"; });
        var popover = Opened<AvonicPopover>(stage, select);

        Assert.Equal(PopoverAlignment.Center, popover.Alignment);
        Assert.Equal(PopoverSize.Auto, popover.Size);
        Assert.False(popover.ShowBackdrop);
        Assert.Contains("select-popover", popover.Classes);
        Assert.IsType<AvonicSelectPopover>(popover.Content);

        // ref/popover.json: 250 wide at 128, 41, four 48-high items from 49.
        var box = AvonicPopoverTests.Box(popover, stage.Window);
        AvonicSelectCaptureTests.AssertBox(128, 41, 250, 208, box, 1);
        var rows = Rows(popover);
        Assert.Equal(4, rows.Count);
        AvonicSelectCaptureTests.AssertBox(128, 49, 250, 48, AvonicSelectCaptureTests.Box(rows[0], stage.Window), 1);
    }

    [AvaloniaFact]
    public void B19_A_Fill_Or_A_Floating_Label_Covers_The_Select()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Fill = SelectFill.Outline; s.LabelPlacement = SelectLabelPlacement.Floating; s.Value = "banana"; }, inItem: false);
        var popover = Opened<AvonicPopover>(stage, select);

        Assert.Equal(PopoverSize.Cover, popover.Size);
        // As wide as the select, under it.
        var box = AvonicPopoverTests.Box(popover, stage.Window);
        var selectBox = AvonicSelectCaptureTests.Box(select, stage.Window);
        Assert.Equal(selectBox.Width, box.Width, 0.6);
        Assert.Equal(selectBox.X, box.X, 0.6);
    }

    [AvaloniaFact]
    public void B22_A_Radio_Sets_The_Value_And_Dismisses()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Value = "apple"; });
        var popover = Opened<AvonicPopover>(stage, select);

        Tap(stage, Rows(popover)[1]);
        stage.After(Settled);
        Assert.Equal(["ionChange(banana)", "ionDismiss"], stage.Log);
        Assert.Null(select.Overlay);
    }

    [AvaloniaFact]
    public void B22_The_Checked_Radio_Dismisses_With_No_Change()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Value = "apple"; });
        var popover = Opened<AvonicPopover>(stage, select);

        Tap(stage, Rows(popover)[0]);
        stage.After(Settled);
        Assert.Equal(["ionDismiss"], stage.Log);
    }

    [AvaloniaFact]
    public void B22_A_Checkbox_Sets_Every_Checked_Value_And_Stays_Open()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Multiple = true; s.Value = new object?[] { "apple" }; });
        var popover = Opened<AvonicPopover>(stage, select);
        var rows = Rows(popover);
        Assert.Contains(SelectOptionList.CheckboxCheckedClass, rows[0].Classes);

        Tap(stage, rows[3]);
        stage.After(Settled);
        Assert.Equal(["ionChange([apple,grape])"], stage.Log);
        Assert.Same(popover, select.Overlay);
        Assert.Contains(SelectOptionList.CheckboxCheckedClass, rows[3].Classes);

        Tap(stage, rows[0]);
        Assert.Equal("ionChange([grape])", stage.Log.Last());
        Assert.DoesNotContain(SelectOptionList.CheckboxCheckedClass, rows[0].Classes);
    }

    [AvaloniaFact]
    public void B22_Arrows_Move_Focus_Only_Enter_Commits_And_Dismisses_On_Its_Key_Up()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Value = "apple"; });
        var popover = Opened<AvonicPopover>(stage, select);
        var radios = Rows(popover).Select(row => (AvonicRadio)row.Content!).ToList();
        Assert.Same(radios[0], stage.Focused);

        // Down skips the disabled Cherry; nothing is chosen.
        Key(stage, PhysicalKey.ArrowDown);
        Key(stage, PhysicalKey.ArrowDown);
        Assert.Same(radios[3], stage.Focused);
        Assert.Empty(stage.Log);
        Assert.Same(popover, select.Overlay);

        stage.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Stage.Settle();
        Assert.Equal(["ionChange(grape)"], stage.Log);
        stage.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        stage.After(Settled);
        Assert.Equal(["ionChange(grape)", "ionDismiss"], stage.Log);
    }

    [AvaloniaFact]
    public void B22_Space_Chooses_And_Dismisses()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Value = "apple"; });
        Opened<AvonicPopover>(stage, select);
        Key(stage, PhysicalKey.ArrowDown);
        Key(stage, PhysicalKey.Space);
        stage.After(Settled);
        Assert.Equal(["ionChange(banana)", "ionDismiss"], stage.Log);
    }

    [AvaloniaFact]
    public void B21_The_Checked_Radios_Item_Has_Primary_At_8_Percent_And_No_Mark_Or_Inner_Border()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Value = "banana"; });
        var popover = Opened<AvonicPopover>(stage, select);
        var rows = Rows(popover);

        Assert.Contains(SelectOptionList.RadioCheckedClass, rows[1].Classes);
        Assert.DoesNotContain(SelectOptionList.RadioCheckedClass, rows[0].Classes);
        Assert.True(rows[1].TryFindResource("Avonic.Item.Background", out var background));
        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(background);
        Assert.Equal(0.08, brush.Opacity, 3);
        Assert.False(rows[0].TryFindResource("Avonic.Item.Background", out var plain) && plain is ISolidColorBrush);
        Assert.True(rows[0].TryFindResource("Avonic.Item.InnerBorderWidth", out var inner));
        Assert.Equal(new Thickness(0), inner);

        var radio = (AvonicRadio)rows[0].Content!;
        Assert.False(radio.GetVisualDescendants().OfType<Control>().Single(c => c.Name == "PART_Container").IsVisible);
        // md's defaults: the label after the control, packed at the start.
        Assert.Equal(RadioLabelPlacement.End, radio.LabelPlacement);
        Assert.Equal(RadioJustify.Start, radio.Justify);
        // ref/popover.json: the label at 144, 14.5 down the 48-high row.
        var label = radio.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Apple");
        var labelBox = AvonicSelectCaptureTests.Box(label, stage.Window);
        var rowBox = AvonicSelectCaptureTests.Box(rows[0], stage.Window);
        Assert.Equal(16, labelBox.X - rowBox.X, 0.6);
        Assert.InRange(labelBox.Y - rowBox.Y, 13.9, 15.1);
    }

    [AvaloniaFact]
    public void B23_The_Selected_Option_Is_Focused_And_With_None_The_First_Enabled()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Value = "grape"; });
        var popover = Opened<AvonicPopover>(stage, select);
        Assert.Same(Rows(popover)[3].Content, stage.Focused);
        Key(stage, PhysicalKey.Escape);
        stage.After(Settled);

        select.Value = null;
        select.Items[0].IsEnabled = false;
        popover = Opened<AvonicPopover>(stage, select);
        Assert.Same(Rows(popover)[1].Content, stage.Focused);
    }

    [AvaloniaFact]
    public void B21_Header_Sub_Header_And_Message_Come_From_The_Interface_Options()
    {
        using var stage = new Stage();
        var select = Mount(stage, s =>
        {
            s.Interface = SelectInterface.Popover;
            s.InterfaceOptions = new Dictionary<string, object?> { ["header"] = "Fruits", ["subHeader"] = "Pick one", ["message"] = "Only one" };
        });
        var popover = Opened<AvonicPopover>(stage, select);
        var content = (AvonicSelectPopover)popover.Content!;

        Assert.Equal("Fruits", content.Header);
        Assert.Equal("Fruits", popover.GetVisualDescendants().OfType<AvonicListHeader>().Single().Content);
        var texts = popover.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Contains("Pick one", texts);
        Assert.Contains("Only one", texts);
    }

    [AvaloniaFact]
    public void B21_A_Description_Is_Under_The_Text_And_Widens_The_Popover()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Items[1].Description = "Yellow and long"; s.Value = "apple"; });
        var popover = Opened<AvonicPopover>(stage, select);

        Assert.Contains("select-popover-rich-content", popover.Classes);
        // clamp(250px, 100vw - 40px, 400px) at 390: 350, as ref/description.json's 56 to 406... less the 16 md padding at the edge.
        Assert.Equal(350, AvonicPopoverTests.Box(popover, stage.Window).Width, 0.6);
        var description = popover.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Yellow and long");
        Assert.Equal(12, description.FontSize);
    }

    [AvaloniaFact]
    public void B22_An_Enter_Held_From_Opening_Commits_Nothing_Until_It_Comes_Up()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.Value = "apple"; });
        select.Focus(NavigationMethod.Tab);
        stage.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        stage.After(Settled);
        var popover = Assert.IsType<AvonicPopover>(select.Overlay);

        // The key repeats on the focused radio: not a new press. Its key up dismisses nothing.
        Key(stage, PhysicalKey.ArrowDown);
        stage.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Stage.Settle();
        stage.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        stage.After(Settled);
        Assert.Empty(stage.Log);
        Assert.Same(popover, select.Overlay);

        // A new press does.
        Key(stage, PhysicalKey.Enter);
        stage.After(Settled);
        Assert.Equal(["ionChange(banana)", "ionDismiss"], stage.Log);
    }

    [AvaloniaFact]
    public void B21_An_Options_Placement_And_Justify_Reach_Its_Checkbox()
    {
        using var stage = new Stage();
        var select = Mount(stage, s =>
        {
            s.Interface = SelectInterface.Popover;
            s.Multiple = true;
            s.Items[0].LabelPlacement = SelectOptionLabelPlacement.Start;
            s.Items[0].Justify = SelectOptionJustify.SpaceBetween;
        });
        var popover = Opened<AvonicPopover>(stage, select);
        var rows = Rows(popover);
        var first = (AvonicCheckbox)rows[0].Content!;
        var second = (AvonicCheckbox)rows[1].Content!;
        Assert.Equal(CheckboxLabelPlacement.Start, first.LabelPlacement);
        Assert.Equal(CheckboxJustify.SpaceBetween, first.Justify);
        Assert.Equal(CheckboxLabelPlacement.End, second.LabelPlacement);
        Assert.Equal(CheckboxJustify.Start, second.Justify);
    }

    [AvaloniaFact]
    public void B23_The_Action_Sheets_Selected_Button_Is_Focused_A_Frame_After_It_Will_Present()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.ActionSheet; s.Value = "grape"; });
        var sheet = Opened<AvonicActionSheet>(stage, select);
        Assert.Same(sheet.ButtonControls[3], stage.Focused);
    }

    // ── B20: the modal ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B20_Modal_Has_The_Header_The_Close_Button_And_The_Options()
    {
        using var stage = new Stage();
        var select = Mount(stage, s =>
        {
            s.Interface = SelectInterface.Modal;
            s.Value = "apple";
            s.InterfaceOptions = new Dictionary<string, object?> { ["header"] = "Fruits" };
        });
        var modal = Opened<AvonicModal>(stage, select);
        var content = Assert.IsType<AvonicSelectModal>(modal.Content);

        Assert.Contains("select-modal", modal.Classes);
        Assert.Equal("Fruits", content.Header);
        Assert.Equal("Cancel", content.CancelText);
        var rows = Rows(modal);
        Assert.Equal(4, rows.Count);
        Assert.All(rows, row => Assert.Equal(ItemLines.None, row.Lines));

        // Close dismisses, with no ionCancel.
        var close = modal.GetVisualDescendants().OfType<AvonicButton>().Single(b => b.Name == "PART_Cancel");
        Tap(stage, close);
        stage.After(Settled);
        Assert.Equal(["ionDismiss"], stage.Log);
    }

    [AvaloniaFact]
    public void B20_A_Modal_Radio_Sets_The_Value_And_Dismisses()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Modal; s.Value = "apple"; });
        var modal = Opened<AvonicModal>(stage, select);
        Tap(stage, Rows(modal)[1]);
        stage.After(Settled);
        Assert.Equal(["ionChange(banana)", "ionDismiss"], stage.Log);
    }

    // ── B6, B4: expanded, opening ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Open_Turns_The_Icon_Floats_The_Label_And_A_Second_Open_Does_Nothing()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.Interface = SelectInterface.Popover; s.LabelPlacement = SelectLabelPlacement.Floating; }, inItem: false);
        Assert.DoesNotContain(":label-floating", select.Classes);

        Opened<AvonicPopover>(stage, select);
        Assert.Contains(":select-expanded", select.Classes);
        Assert.Contains(":label-floating", select.Classes);
        // Done at once, with nothing: never .Result on a task that isn't, which would hang the UI thread.
        var again = select.Open(new PopoverEvent(select));
        Assert.True(again.IsCompleted);
        Assert.Null(again.Result);

        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(select);
        Assert.Equal(ExpandCollapseState.Expanded, peer.ExpandCollapseState);

        Key(stage, PhysicalKey.Escape);
        stage.After(Settled);
        Assert.DoesNotContain(":select-expanded", select.Classes);
        Assert.Equal(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
    }

    [AvaloniaFact]
    public void B6_After_A_Dismiss_The_Select_Has_Focus_Even_Opened_From_Code()
    {
        using var stage = new Stage();
        var select = Mount(stage);
        var other = new Button { Content = "Other" };
        Canvas.SetTop(other, 400);
        stage.Page.Children.Add(other);
        Stage.Settle();
        other.Focus();

        // The overlay gives focus back to what had it; then onDidDismiss's setFocus moves it to the select.
        _ = select.Open(new PopoverEvent(select));
        stage.After(Settled);
        Key(stage, PhysicalKey.Escape);
        stage.After(Settled);
        Assert.Same(select, stage.Focused);
    }

    [AvaloniaFact]
    public void B6_An_Expanded_Icon_Replaces_The_Toggle_Icon_While_Open()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => { s.ToggleIcon = "add"; s.ExpandedIcon = "remove"; });
        Assert.Equal("add", select.SelectIcon);
        Opened<AvonicAlert>(stage, select);
        Assert.Equal("remove", select.SelectIcon);
        Key(stage, PhysicalKey.Escape);
        stage.After(Settled);
        Assert.Equal("add", select.SelectIcon);
    }

    [AvaloniaFact]
    public void B4_Disabled_Opens_Nothing()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => s.IsEnabled = false);
        // Done at once, with nothing: never .Result on a task that isn't, which would hang the UI thread.
        var again = select.Open(new PopoverEvent(select));
        Assert.True(again.IsCompleted);
        Assert.Null(again.Result);
        Assert.Null(select.Overlay);
    }

    [AvaloniaFact]
    public void B4_Enter_On_The_Focused_Select_Opens_It()
    {
        using var stage = new Stage();
        var select = Mount(stage);
        select.Focus(NavigationMethod.Tab);
        Key(stage, PhysicalKey.Enter);
        stage.After(Settled);
        Assert.IsType<AvonicAlert>(select.Overlay);
    }

    [AvaloniaFact]
    public void B4_A_Button_In_The_End_Slot_Doesnt_Open_It_But_An_Icon_Does()
    {
        using var stage = new Stage();
        var button = new Button { Content = "x", Width = 30, Height = 30 };
        var select = Mount(stage, s => s.EndContent = button, inItem: false);
        Tap(stage, button);
        stage.After(Settled);
        Assert.Null(select.Overlay);

        var icon = new AvonicIcon { IconName = "heart", Width = 20, Height = 20 };
        select.EndContent = icon;
        stage.Render();
        Tap(stage, icon);
        stage.After(Settled);
        Assert.NotNull(select.Overlay);
    }

    [AvaloniaFact]
    public void B16_A_Tap_On_The_Items_Padding_Opens_It()
    {
        using var stage = new Stage();
        var select = Mount(stage);
        var item = select.FindAncestorOfType<AvonicItem>()!;
        var box = AvonicSelectCaptureTests.Box(item, stage.Window);
        stage.Click(new Point(box.X + 4, box.Y + box.Height / 2));
        stage.After(Settled);
        Assert.IsType<AvonicAlert>(select.Overlay);
    }

    // ── B24, B25: interface options, changes while open ─────────────────────────────────────

    [AvaloniaFact]
    public void B24_Interface_Options_Set_The_Overlay_By_Name_And_Add_Classes()
    {
        using var stage = new Stage();
        var select = Mount(stage, s =>
        {
            s.Interface = SelectInterface.Popover;
            s.InterfaceOptions = new Dictionary<string, object?>
            {
                ["size"] = "cover", ["Side"] = PopoverSide.Top, ["cssClass"] = new[] { "mine", "yours" }, ["nonsense"] = 1,
            };
        });
        var popover = Opened<AvonicPopover>(stage, select);
        Assert.Equal(PopoverSize.Cover, popover.Size);
        Assert.Equal(PopoverSide.Top, popover.Side);
        Assert.Contains("mine", popover.Classes);
        Assert.Contains("yours", popover.Classes);
        Key(stage, PhysicalKey.Escape);
        stage.After(Settled);

        select.Interface = SelectInterface.Alert;
        select.InterfaceOptions = new Dictionary<string, object?> { ["header"] = "Mine", ["subHeader"] = "Sub", ["cssClass"] = "a b" };
        var alert = Opened<AvonicAlert>(stage, select);
        Assert.Equal("Mine", alert.Header);
        Assert.Equal("Sub", alert.SubHeader);
        Assert.Equal("select-alert a b single-select-alert", alert.CssClass);
    }

    [AvaloniaFact]
    public void B25_A_New_Option_While_Open_Is_In_The_Interface()
    {
        using var stage = new Stage();
        var select = Mount(stage, s => s.Interface = SelectInterface.Popover);
        var popover = Opened<AvonicPopover>(stage, select);
        select.Items.Add(new AvonicSelectOption { Value = "kiwi", Content = "Kiwi" });
        stage.Render();
        Assert.Equal(5, Rows(popover).Count);
    }

    [AvaloniaFact]
    public void B25_A_Removed_Option_While_Open_Leaves_The_Alert()
    {
        using var stage = new Stage();
        var select = Mount(stage);
        var alert = Opened<AvonicAlert>(stage, select);
        select.Items.RemoveAt(0);
        Assert.Equal(3, alert.Inputs!.Count);
    }
}
