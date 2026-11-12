using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The behaviours in docs/components/ion-accordion/PLAN.md, from accordion.js and accordion-group.js, and the numbers measured on
/// real Ionic in Chromium at 390 px wide. Tests with <c>InstalledTheme</c> check the look; the others run on the functional stub.
/// The content is a box 51 px tall, as Ionic's <c>&lt;div style="padding:16px"&gt;</c> of text measured.
/// </summary>
public class AvonicAccordionTests
{
    private const double ContentHeight = 51;

    /// <summary>cubic-bezier(0.25, 0.8, 0.5, 1), from accordion.md.css, not the control's own constant.</summary>
    private static readonly Avonic.Components.Animation.CubicBezierEasing Ease = new(0.25, 0.8, 0.5, 1);

    private static AvonicItem Item(string text) => new() { Content = new AvonicLabel { Content = text } };

    private static AvonicAccordion Accordion(string value) => new()
    {
        Value = value,
        HeaderContent = Item(value),
        ContentContent = new Border { Height = ContentHeight },
    };

    private static AvonicAccordionGroup Group(object? value, params AvonicAccordion[] accordions)
    {
        var group = new AvonicAccordionGroup { Value = value };
        foreach (var accordion in accordions)
        {
            group.Items.Add(accordion);
        }

        return group;
    }

    private static Scene Show(Control control)
    {
        var scene = new Scene(new StackPanel { Children = { control } });
        Dispatcher.UIThread.RunJobs();
        return scene;
    }

    private static T Part<T>(TemplatedControl control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == control);

    private static AccordionContentClip Content(AvonicAccordion accordion) => Part<AccordionContentClip>(accordion, "PART_Content");

    private static ContentPresenter Header(AvonicAccordion accordion) => Part<ContentPresenter>(accordion, "PART_Header");

    /// <summary>How tall the content shows, after a layout pass; 0 when it's hidden.</summary>
    private static double Shown(Scene scene, AvonicAccordion accordion)
    {
        scene.Window.UpdateLayout();
        var content = Content(accordion);
        return content.IsEffectivelyVisible ? content.Bounds.Height : 0;
    }

    private static Point CentreOf(Scene scene, Visual visual) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), scene.Window)!.Value;

    /// <summary>Where the header is now: one above may be opening or closing.</summary>
    private static void Tap(Scene scene, AvonicAccordion accordion)
    {
        scene.Window.UpdateLayout();
        Click(scene, CentreOf(scene, Header(accordion)));
    }

    private static void Click(Scene scene, Point point)
    {
        scene.Window.CaptureRenderedFrame();
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static List<object?> Changes(AvonicAccordionGroup group)
    {
        var changes = new List<object?>();
        group.IonChange += (_, e) => changes.Add(e.Value);
        return changes;
    }

    // ── B1, B5, B21: the group's value ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_The_Group_Value_Opens_The_Matching_Accordion_Only()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b"), c = Accordion("c");
        using var scene = Show(Group("b", a, b, c));

        Assert.Equal(0, Shown(scene, a));
        Assert.Equal(ContentHeight, Shown(scene, b));
        Assert.Equal(0, Shown(scene, c));
    }

    [AvaloniaFact]
    public void B1_With_Multiple_Each_Listed_Accordion_Opens()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b"), c = Accordion("c");
        var group = Group(new[] { "a", "c" }, a, b, c);
        group.Multiple = true;
        using var scene = Show(group);

        Assert.Equal(ContentHeight, Shown(scene, a));
        Assert.Equal(0, Shown(scene, b));
        Assert.Equal(ContentHeight, Shown(scene, c));
    }

    [AvaloniaFact]
    public void B5_Setting_The_Value_Raises_No_IonChange_And_The_Accordions_Follow()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        var changes = Changes(group);
        using var scene = Show(group);
        using var clock = new TestClock();

        group.Value = "b";
        clock.At(1000);

        Assert.Empty(changes);
        Assert.Equal(0, Shown(scene, a));
        Assert.Equal(ContentHeight, Shown(scene, b));
    }

    [AvaloniaFact]
    public void B21_Changing_An_Accordions_Value_Works_Its_State_Out_Again()
    {
        var a = Accordion("a");
        using var scene = Show(Group("x", a));
        Assert.Equal(0, Shown(scene, a));

        a.Value = "x";

        Assert.Equal(ContentHeight, Shown(scene, a));
    }

    [AvaloniaFact]
    public void Value_Defaults_To_An_Id_Of_Its_Own()
    {
        AvonicAccordion a = new(), b = new();

        Assert.StartsWith("ion-accordion-", a.Value);
        Assert.StartsWith("ion-accordion-", b.Value);
        Assert.NotEqual(a.Value, b.Value);
    }

    // ── B2, B3, B4: a tap ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_A_Tap_On_A_Closed_One_Opens_It_Raises_IonChange_And_Closes_The_Other()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        var changes = Changes(group);
        using var scene = Show(group);
        using var clock = new TestClock();

        Tap(scene, b);
        clock.At(1000);

        Assert.Equal("b", group.Value);
        Assert.Equal(["b"], changes);
        Assert.Equal(0, Shown(scene, a));
        Assert.Equal(ContentHeight, Shown(scene, b));
    }

    [AvaloniaFact]
    public void B3_A_Tap_On_An_Open_One_Clears_The_Value()
    {
        var a = Accordion("a");
        var group = Group("a", a);
        var changes = Changes(group);
        using var scene = Show(group);
        using var clock = new TestClock();

        Tap(scene, a);
        clock.At(1000);

        Assert.Null(group.Value);
        Assert.Equal([null], changes);
        Assert.Equal(0, Shown(scene, a));
    }

    [AvaloniaFact]
    public void B4_With_Multiple_A_Tap_Adds_Or_Removes_Its_Value_As_A_New_List()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var start = new[] { "a" };
        var group = Group(start, a, b);
        group.Multiple = true;
        var changes = Changes(group);
        using var scene = Show(group);
        using var clock = new TestClock();

        Tap(scene, b);
        clock.At(1000);
        var added = Assert.IsAssignableFrom<IEnumerable<string>>(group.Value);
        Assert.Equal(["a", "b"], added);
        Assert.NotSame(start, group.Value);
        Assert.Equal(ContentHeight, Shown(scene, a));
        Assert.Equal(ContentHeight, Shown(scene, b));

        Tap(scene, a);
        clock.At(2000);
        Assert.Equal(["b"], Assert.IsAssignableFrom<IEnumerable<string>>(group.Value));
        Assert.Equal(2, changes.Count);
        Assert.Equal(0, Shown(scene, a));
    }

    [AvaloniaFact]
    public void B4_A_Second_Tap_Before_It_Opens_Adds_Nothing()
    {
        var a = Accordion("a");
        var group = Group(null, a);
        group.Multiple = true;
        var changes = Changes(group);
        using var scene = Show(group);
        using var clock = new TestClock();

        // It's still closed until a frame has passed, so the second tap asks to open it again.
        Tap(scene, a);
        Tap(scene, a);

        Assert.Equal(["a"], Assert.IsAssignableFrom<IEnumerable<string>>(group.Value));
        Assert.Single(changes);
    }

    [AvaloniaFact]
    public void B1_A_Value_That_Is_Not_A_String_Is_Read_The_Same_Way_By_A_Tap()
    {
        var a = Accordion("1");
        var group = Group(new object[] { 1 }, a);
        group.Multiple = true;
        using var scene = Show(group);
        using var clock = new TestClock();

        Assert.Equal(ContentHeight, Shown(scene, a));
        Tap(scene, a);
        clock.At(1000);

        Assert.Equal(0, Shown(scene, a));
    }

    [AvaloniaFact]
    public void B6_An_Array_Without_Multiple_Logs_A_Warning()
    {
        var sink = new WarningSink();
        var previous = Avalonia.Logging.Logger.Sink;
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            var group = new AvonicAccordionGroup { Value = new[] { "a", "b" } };
            Assert.Contains(sink.Messages, m => m.Contains("multiple is \"false\""));

            sink.Messages.Clear();
            group.Multiple = true;
            group.Value = new[] { "a" };
            Assert.DoesNotContain(sink.Messages, m => m.Contains("multiple is \"false\""));
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    private sealed class WarningSink : Avalonia.Logging.ILogSink
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(Avalonia.Logging.LogEventLevel level, string area) => level >= Avalonia.Logging.LogEventLevel.Warning;

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate) => Messages.Add(messageTemplate);

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Messages.Add(messageTemplate);
    }

    // ── B7, B8, B9: what stops a tap ─────────────────────────────────────────────────────────

    // ion-accordion::part(header) { pointer-events: auto } beats the shadow root's own rule, whenever readonly turns on. A value
    // given at style priority when it turns on would come after the app's style, and win.
    [AvaloniaFact]
    public void B7_An_Apps_Style_On_A_Part_Survives_Readonly_Turning_On_After_It()
    {
        // The app's styles are there before the accordion's template is, as an app's styles are.
        var a = Accordion("a");
        var window = new Window
        {
            Content = Group("a", a),
            Styles =
            {
                new Style(x => x.OfType<AvonicAccordion>().Template().OfType<ContentPresenter>().Name("PART_Header"))
                {
                    Setters = { new Setter(InputElement.IsHitTestVisibleProperty, true) },
                },
                new Style(x => x.OfType<AvonicAccordion>().Template().OfType<AccordionContentClip>().Name("PART_Content"))
                {
                    Setters = { new Setter(InputElement.IsHitTestVisibleProperty, true) },
                },
            },
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        a.Readonly = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(Header(a).IsHitTestVisible);
        Assert.True(Content(a).IsHitTestVisible);
        window.Close();
    }

    // An app's own template may have a stock presenter as the header: readonly still takes its input.
    [AvaloniaFact]
    public void B7_Readonly_Takes_Input_Off_A_Stock_Presenter_As_The_Header()
    {
        var a = Accordion("a");
        a.Theme = new ControlTheme(typeof(AvonicAccordion))
        {
            Setters =
            {
                new Setter(TemplatedControl.TemplateProperty, new Avalonia.Controls.Templates.FuncControlTemplate<AvonicAccordion>((accordion, scope) =>
                {
                    var header = new ContentPresenter { Name = "PART_Header", Content = accordion.HeaderContent };
                    var content = new AccordionContentClip { Name = "PART_Content" };
                    scope.Register(header.Name, header);
                    scope.Register(content.Name, content);
                    return new StackPanel { Children = { header, content } };
                })),
            },
        };
        using var scene = Show(Group("a", a));

        a.Readonly = true;
        Dispatcher.UIThread.RunJobs();

        Assert.IsNotType<AccordionHeaderPresenter>(Header(a));
        Assert.False(Header(a).IsHitTestVisible);
    }

    [AvaloniaFact]
    public void B7_Readonly_Stops_A_Tap_And_Takes_Pointer_Input_Off_The_Header_And_Content()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        b.Readonly = true;
        b.ContentContent = new Border { Height = ContentHeight, Background = Brushes.Transparent };
        var group = Group("a", a, b);
        var changes = Changes(group);
        using var scene = Show(group);

        var centre = CentreOf(scene, Header(b));
        Tap(scene, b);

        Assert.Equal("a", group.Value);
        Assert.Empty(changes);
        scene.Window.CaptureRenderedFrame();
        var hit = scene.Window.InputHitTest(centre) as Visual;
        Assert.False(hit is not null && Header(b).IsVisualAncestorOf(hit));

        // The content too: open it from the group, and nothing in it takes the pointer.
        group.Animated = false;
        group.Value = "b";
        Dispatcher.UIThread.RunJobs();
        Assert.True(Content(b).IsEffectivelyVisible);
        scene.Window.CaptureRenderedFrame();
        var inContent = scene.Window.InputHitTest(CentreOf(scene, Content(b))) as Visual;
        Assert.False(inContent is not null && Content(b).IsVisualAncestorOf(inContent));
    }

    [AvaloniaFact]
    public void B7_A_Readonly_Group_Stops_A_Tap_And_Its_Accordions_Take_No_Pointer()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        group.Readonly = true;
        var changes = Changes(group);
        using var scene = Show(group);

        Tap(scene, b);
        Assert.Equal("a", group.Value);
        Assert.Empty(changes);
        Assert.False(Header(b).IsHitTestVisible);

        group.Readonly = false;
        Assert.True(Header(b).IsHitTestVisible);
        Tap(scene, b);
        Assert.Equal("b", group.Value);
    }

    [AvaloniaFact]
    public void B7_A_Readonly_Group_Refuses_An_Accordion_Further_In()
    {
        // Not the group's own child, so the group doesn't make it readonly: the group refuses its request instead.
        var inner = Accordion("inner");
        var group = new AvonicAccordionGroup { Readonly = true };
        group.Items.Add(new StackPanel { Children = { inner } });
        var changes = Changes(group);
        using var scene = Show(group);

        Assert.True(Header(inner).IsHitTestVisible);
        Tap(scene, inner);

        Assert.Null(group.Value);
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void B7_Readonly_Stops_The_Keyboard_Too()
    {
        var a = Accordion("a");
        a.Readonly = true;
        var group = Group(null, a);
        using var scene = Show(group);

        var item = (AvonicItem)a.HeaderContent!;
        item.Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        Assert.Null(group.Value);
    }

    [AvaloniaFact]
    public void B8_Disabled_Stops_A_Tap()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        b.IsEnabled = false;
        var group = Group("a", a, b);
        using var scene = Show(group);

        Tap(scene, b);

        Assert.Equal("a", group.Value);
    }

    [AvaloniaFact]
    public void B8_Disabled_Stops_Automation_Too()
    {
        var a = Accordion("a");
        a.IsEnabled = false;
        var group = Group(null, a);
        using var scene = Show(group);

        ((IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(HeaderOf(a))).Expand();

        Assert.Null(group.Value);
    }

    [AvaloniaFact]
    public void B8_A_Disabled_Group_Stops_A_Tap()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        group.IsEnabled = false;
        using var scene = Show(group);

        Tap(scene, b);

        Assert.Equal("a", group.Value);
    }

    [AvaloniaFact]
    public void B8_Disabled_Dims_The_Header_And_The_Content_Not_The_Host() => InstalledTheme.Run(_ =>
    {
        var a = Accordion("a");
        a.IsEnabled = false;
        using var scene = Show(Group("a", a));

        Assert.Equal(0.4, Header(a).Opacity);
        Assert.Equal(0.4, Content(a).Opacity);
        Assert.Equal(1, a.Opacity);
        // Ionic's item isn't disabled itself, so only the header's 0.4 dims it, measured. One disabled itself keeps its own 0.3.
        Assert.Equal(1, ((AvonicItem)a.HeaderContent!).Opacity);
        var b = Accordion("b");
        b.IsEnabled = false;
        ((AvonicItem)b.HeaderContent!).IsEnabled = false;
        using var other = Show(Group(null, b));
        Assert.Equal(0.3, ((AvonicItem)b.HeaderContent!).Opacity);
    });

    [AvaloniaFact]
    public void B18_The_Group_Does_Not_Clip_The_Last_Shadow()
    {
        Assert.False(new AvonicAccordionGroup().ClipToBounds);
    }

    [AvaloniaFact]
    public void B9_Outside_A_Group_A_Tap_Opens_Nothing()
    {
        var a = Accordion("a");
        using var scene = Show(a);

        Tap(scene, a);

        Assert.Equal(0, Shown(scene, a));
    }

    // ── B10, B11: the header's item ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_The_Header_Item_Becomes_A_Button_With_No_Detail_And_Full_Lines()
    {
        var a = Accordion("a");
        var item = (AvonicItem)a.HeaderContent!;
        item.Detail = true;
        using var scene = Show(Group(null, a));

        Assert.True(item.Button);
        Assert.False(item.Detail);
        Assert.Equal(ItemLines.Full, item.Lines);
    }

    [AvaloniaFact]
    public void B10_An_Items_Own_Lines_Stay()
    {
        var a = Accordion("a");
        var item = (AvonicItem)a.HeaderContent!;
        item.Lines = ItemLines.Inset;
        using var scene = Show(Group(null, a));

        Assert.Equal(ItemLines.Inset, item.Lines);
    }

    [AvaloniaFact]
    public void B10_An_Item_Inside_A_Panel_In_The_Header_Is_Left_Alone()
    {
        // Ionic looks at what's assigned to the slot, and a <div> there isn't an ion-item.
        var item = Item("a");
        var a = new AvonicAccordion { Value = "a", HeaderContent = new StackPanel { Children = { item } } };
        using var scene = Show(Group(null, a));

        Assert.False(item.Button);
        Assert.Null(item.EndContent);
    }

    [AvaloniaFact]
    public void B10_An_Item_From_The_Header_Template_Counts()
    {
        var a = new AvonicAccordion
        {
            Value = "a",
            HeaderContent = "From a view model",
            HeaderContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => Item(text)),
        };
        using var scene = Show(Group(null, a));

        var item = Assert.IsType<AvonicItem>(Header(a).Child);
        Assert.True(item.Button);
        Assert.IsType<AvonicIcon>(item.EndContent);
    }

    [AvaloniaFact]
    public void B11_A_Toggle_Icon_Goes_In_The_Items_End_Slot()
    {
        var a = Accordion("a");
        var item = (AvonicItem)a.HeaderContent!;
        using var scene = Show(Group(null, a));

        var icon = Assert.IsType<AvonicIcon>(item.EndContent);
        Assert.Contains("ion-accordion-toggle-icon", icon.Classes);
        Assert.Equal("chevron-down", icon.Icon);
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(icon));
    }

    [AvaloniaFact]
    public void B11_ToggleIcon_And_ToggleIconSlot_Choose_The_Icon_And_The_Slot()
    {
        var a = Accordion("a");
        a.ToggleIcon = "caret-down";
        a.ToggleIconSlot = AccordionToggleIconSlot.Start;
        var item = (AvonicItem)a.HeaderContent!;
        using var scene = Show(Group(null, a));

        var icon = Assert.IsType<AvonicIcon>(item.StartContent);
        Assert.Equal("caret-down", icon.Icon);
        Assert.Null(item.EndContent);
    }

    [AvaloniaFact]
    public void B11_No_Icon_Is_Added_When_The_Item_Has_One_With_The_Class()
    {
        var a = Accordion("a");
        var own = new AvonicIcon { Icon = "add" };
        own.Classes.Add("ion-accordion-toggle-icon");
        var item = (AvonicItem)a.HeaderContent!;
        item.StartContent = new StackPanel { Children = { own } };
        using var scene = Show(Group(null, a));

        Assert.Null(item.EndContent);
    }

    [AvaloniaFact]
    public void B11_The_Icon_Goes_After_What_The_Slot_Holds()
    {
        var a = Accordion("a");
        var item = (AvonicItem)a.HeaderContent!;
        var note = new AvonicNote { Content = "3" };
        item.EndContent = note;
        var b = Accordion("b");
        var panelItem = (AvonicItem)b.HeaderContent!;
        var badge = new AvonicBadge { Content = "1" };
        var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { badge } };
        panelItem.EndContent = panel;
        using var scene = Show(Group(null, a, b));

        var wrapper = Assert.IsType<StackPanel>(item.EndContent);
        Assert.Equal(Avalonia.Layout.Orientation.Horizontal, wrapper.Orientation);
        Assert.Same(note, wrapper.Children[0]);
        Assert.Contains("ion-accordion-toggle-icon", wrapper.Children[1].Classes);
        Assert.Same(panel, panelItem.EndContent);
        // A row stands for the slot; any other panel is one control in it, as a vertical one would put the icon underneath.
        Assert.Same(badge, panel.Children[0]);
        Assert.Contains("ion-accordion-toggle-icon", panel.Children[1].Classes);
    }

    [AvaloniaFact]
    public void B11_A_Panel_That_Is_Not_A_Row_Goes_Beside_The_Icon()
    {
        var a = Accordion("a");
        var item = (AvonicItem)a.HeaderContent!;
        var column = new StackPanel { Children = { new AvonicNote { Content = "1" } } };
        item.EndContent = column;
        using var scene = Show(Group(null, a));

        var row = Assert.IsType<StackPanel>(item.EndContent);
        Assert.Equal(Avalonia.Layout.Orientation.Horizontal, row.Orientation);
        Assert.Same(column, row.Children[0]);
        Assert.Contains("ion-accordion-toggle-icon", row.Children[1].Classes);
    }

    [AvaloniaFact]
    public void B12_The_Toggle_Icon_Turns_While_Open() => InstalledTheme.Run(_ =>
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        using var scene = Show(Group("a", a, b));

        static double Turn(AvonicAccordion accordion)
        {
            var icon = ((AvonicItem)accordion.HeaderContent!).GetLogicalDescendantsAndSelfIcons().First();
            return icon.RenderTransform is { } transform ? transform.Value.M11 : 1;
        }

        Assert.Equal(-1, Turn(a), 6);
        Assert.Equal(1, Turn(b), 6);
    });

    [AvaloniaFact]
    public void B12_The_Turn_Eases_Once_Animated() => InstalledTheme.Run(_ =>
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        using var scene = Show(Group("a", a, b));
        var icon = ((AvonicItem)b.HeaderContent!).GetLogicalDescendantsAndSelfIcons().First();
        Assert.Null(icon.Transitions);

        using var clock = new TestClock();
        Tap(scene, b);
        clock.At(16);

        Assert.NotNull(icon.Transitions);
        // Avalonia's transition runs on real time, so just after the change it's barely begun: not yet turned.
        Assert.True(icon.RenderTransform is null || icon.RenderTransform.Value.M11 > 0.5);
    });

    // ── B13: the header's click ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_Enter_On_The_Header_Item_Toggles()
    {
        var a = Accordion("a");
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);

        ((AvonicItem)a.HeaderContent!).Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        Assert.Equal("a", group.Value);
        Assert.Equal(["a"], changes);
    }

    [AvaloniaFact]
    public void B13_A_Click_On_A_Button_In_The_Header_Item_Toggles_Once()
    {
        var a = Accordion("a");
        var button = new AvonicButton { Content = "Go", Width = 60, Height = 36 };
        ((AvonicItem)a.HeaderContent!).StartContent = button;
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);

        Click(scene, CentreOf(scene, button));

        Assert.Equal(["a"], changes);
    }

    [AvaloniaFact]
    public void B13_A_Header_That_Is_Not_An_Item_Toggles_On_A_Click()
    {
        var a = new AvonicAccordion { Value = "a", HeaderContent = new Border { Height = 48, Background = Brushes.Transparent } };
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);

        Tap(scene, a);
        Tap(scene, a);

        Assert.Equal(["a", null], changes);
    }

    /// <summary>A click from a key or from automation, on each kind of control that raises one: each bubbles to the header.</summary>
    [AvaloniaFact]
    public void B13_A_Click_From_Any_Control_Inside_Toggles()
    {
        var button = new AvonicButton { Content = "Go" };
        var chip = new AvonicChip { Items = { "Chip" } };
        var card = new AvonicCard { Button = true, Content = "Card" };
        var stock = new Button { Content = "Stock" };
        var item = Item("a");
        item.EndContent = new StackPanel { Children = { button, chip, card, stock } };
        var a = new AvonicAccordion { Value = "a", HeaderContent = item };
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);

        using var clock = new TestClock();
        var now = 0;
        foreach (var control in new Control[] { item, button, chip, card, stock })
        {
            ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(control)).Invoke();
            clock.At(now += 1000);
        }

        Assert.Equal(["a", null, "a", null, "a"], changes);
    }

    /// <summary>
    /// A stock button in a chip, from a key and from a press when it clicks on the press: the button's click is the chip's, one
    /// DOM click, which toggles once.
    /// </summary>
    [AvaloniaFact]
    public void B13_A_Stock_Button_In_A_Chip_Toggles_Once()
    {
        var keyed = new Button { Content = "Key" };
        var pressed = new Button
        {
            ClickMode = ClickMode.Press,
            Width = 60,
            Height = 36,
            Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Transparent }),
        };
        var item = Item("a");
        item.EndContent = new StackPanel { Children = { new AvonicChip { Items = { keyed } }, new AvonicChip { Items = { pressed } } } };
        var a = new AvonicAccordion { Value = "a", HeaderContent = item };
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);
        using var clock = new TestClock();

        keyed.Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(["a"], changes);

        clock.At(1000);
        Click(scene, CentreOf(scene, pressed));
        Assert.Equal(["a", null], changes);
    }

    /// <summary>
    /// A chip in a chip, both clicked by one key click on a stock button inside, toggles once; a chip that stops its click stops
    /// that one click before it reaches the header.
    /// </summary>
    [AvaloniaFact]
    public void B13_A_Key_Click_Through_Two_Chips_Toggles_Once_And_Not_At_All_When_Stopped()
    {
        var nested = new Button { Content = "Nested" };
        var stopped = new Button { Content = "Stopped" };
        var stopping = new AvonicChip { Items = { stopped } };
        stopping.Click += (_, e) => e.Handled = true;
        var item = Item("a");
        item.EndContent = new StackPanel { Children = { new AvonicChip { Items = { new AvonicChip { Items = { nested } } } }, stopping } };
        var a = new AvonicAccordion { Value = "a", HeaderContent = item };
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);
        using var clock = new TestClock();

        nested.Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        clock.At(1000);
        stopped.Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        Assert.Equal(["a"], changes);
    }

    /// <summary>
    /// The same for any other control with a native button, which the header hears as one event rather than a list of them:
    /// a fab button's and a breadcrumb's clicks bubble to <c>#header</c> too. A handled one doesn't.
    /// </summary>
    [AvaloniaFact]
    public void B13_A_Click_From_Any_Native_Button_Inside_Toggles_Unless_Stopped()
    {
        var fab = new AvonicFabButton { Content = "Fab" };
        var breadcrumb = new AvonicBreadcrumb { Content = "Home", Href = "#home" };
        var stopped = new AvonicFabButton { Content = "Stopped" };
        stopped.Click += (_, e) => e.Handled = true;
        var item = Item("a");
        item.EndContent = new StackPanel { Children = { fab, breadcrumb, stopped } };
        var a = new AvonicAccordion { Value = "a", HeaderContent = item };
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);

        using var clock = new TestClock();
        var now = 0;
        foreach (var control in new Control[] { fab, breadcrumb, stopped })
        {
            ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(control)).Invoke();
            clock.At(now += 1000);
        }

        Assert.Equal(["a", null], changes);
    }

    [AvaloniaFact]
    public void B13_Each_Tap_On_The_Header_Item_Toggles()
    {
        var a = Accordion("a");
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);
        using var clock = new TestClock();

        Tap(scene, a);
        clock.At(1000);
        Tap(scene, a);

        Assert.Equal(["a", null], changes);
    }

    [AvaloniaFact]
    public void B13_After_A_Tap_A_Key_Still_Toggles()
    {
        var a = Accordion("a");
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);

        Tap(scene, a);
        ((AvonicItem)a.HeaderContent!).Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        Assert.Equal(["a", null], changes);
    }

    [AvaloniaFact]
    public void B13_A_RepeatButtons_Click_Does_Not_Toggle()
    {
        var repeat = new RepeatButton { Content = "+" };
        var item = Item("a");
        item.EndContent = repeat;
        var a = new AvonicAccordion { Value = "a", HeaderContent = item };
        var group = Group(null, a);
        using var scene = Show(group);

        repeat.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        Assert.Null(group.Value);
    }

    [AvaloniaFact]
    public void B13_A_Button_That_Clicks_On_Press_Toggles_Once()
    {
        // TestApp has no base theme, so a stock button needs a template of its own to be hit.
        var button = new Button
        {
            ClickMode = ClickMode.Press,
            Width = 60,
            Height = 36,
            Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Transparent }),
        };
        var a = new AvonicAccordion { Value = "a", HeaderContent = button };
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);

        Click(scene, CentreOf(scene, button));

        Assert.Equal(["a"], changes);
    }

    [AvaloniaFact]
    public void B13_After_A_Press_Mode_Click_The_Next_Press_Elsewhere_Toggles()
    {
        var button = new Button
        {
            ClickMode = ClickMode.Press,
            Width = 60,
            Height = 36,
            Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Transparent }),
        };
        var plain = new Border { Height = 48, Background = Brushes.Transparent };
        var a = new AvonicAccordion { Value = "a", HeaderContent = new StackPanel { Children = { button, plain } } };
        var group = Group(null, a);
        var changes = Changes(group);
        using var scene = Show(group);
        using var clock = new TestClock();

        Click(scene, CentreOf(scene, button));
        clock.At(1000);
        Click(scene, CentreOf(scene, plain));

        Assert.Equal(["a", null], changes);
    }

    [AvaloniaFact]
    public void B13_A_Handled_Click_Inside_Does_Not_Toggle()
    {
        var a = Accordion("a");
        var item = (AvonicItem)a.HeaderContent!;
        item.Click += (_, e) => e.Handled = true;
        var group = Group(null, a);
        using var scene = Show(group);

        Tap(scene, a);

        Assert.Null(group.Value);
    }

    // ── B14, B15, B16: the animation ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_Expanding_Waits_A_Frame_Shows_At_Zero_Then_Eases_To_Its_Height_Over_300ms()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        using var scene = Show(Group("a", a, b));
        using var clock = new TestClock();

        Tap(scene, b);
        Assert.Equal(0, Shown(scene, b));

        clock.At(16);
        Assert.True(Content(b).IsVisible);
        Assert.Equal(0, Shown(scene, b));

        // The second frame starts the transition.
        clock.At(32);
        Assert.Equal(0, Shown(scene, b));
        clock.At(32 + 144);
        var expected = ContentHeight * Ease.Ease(144 / 300.0);
        Assert.Equal(expected, Content(b).MaxHeight, 6);
        // Laid out on whole pixels, rounded up.
        Assert.Equal(Math.Ceiling(expected), Shown(scene, b));
        // The content keeps its own height while the box around it grows.
        Assert.Equal(ContentHeight, Content(b).Child!.Bounds.Height);

        clock.At(32 + 288);
        Assert.True(Content(b).MaxHeight < ContentHeight);

        // The first frame at or after the end finishes it, and the box is free to follow its content again.
        clock.At(336);
        Assert.Equal(ContentHeight, Shown(scene, b));
        Assert.True(double.IsPositiveInfinity(Content(b).MaxHeight));
    }

    /// <summary>Measured: a collapse 112 ms into an expand eases to 0 from where the expand had got to a frame on.</summary>
    [AvaloniaFact]
    public void B14_A_Collapse_During_An_Expand_Starts_From_The_Height_It_Had_Reached()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        using var scene = Show(group);
        using var clock = new TestClock();

        Tap(scene, b);
        clock.At(32 + 96);
        Tap(scene, b);
        Assert.Null(group.Value);

        // The tick that was due at 144 doesn't come; the collapse's first frame reads where the expand had got to by then.
        clock.At(32 + 112);
        Assert.Equal(ContentHeight * Ease.Ease(112 / 300.0), Content(b).MaxHeight, 6);
    }

    [AvaloniaFact]
    public void B14_Collapsing_Reads_The_Height_Then_Eases_To_Zero_And_Hides()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        using var scene = Show(Group("a", a, b));
        using var clock = new TestClock();

        Tap(scene, b);
        clock.At(16);
        Assert.Equal(ContentHeight, Content(a).MaxHeight);
        Assert.Equal(ContentHeight, Shown(scene, a));

        clock.At(32 + 144);
        var expected = ContentHeight * (1 - Ease.Ease(144 / 300.0));
        Assert.Equal(expected, Content(a).MaxHeight, 6);
        Assert.Equal(Math.Ceiling(expected), Shown(scene, a));

        clock.At(336);
        Assert.False(Content(a).IsVisible);
        Assert.True(double.IsPositiveInfinity(Content(a).MaxHeight));
    }

    [AvaloniaFact]
    public void B14_The_Content_Keeps_Its_Own_Height_While_The_Box_Grows()
    {
        // A scroller is as tall as it's let be, so it shows whether the content is measured at its own height, as a block is.
        var a = new AvonicAccordion
        {
            Value = "a",
            HeaderContent = Item("a"),
            ContentContent = new ScrollViewer { Content = new Border { Height = ContentHeight } },
        };
        var b = Accordion("b");
        using var scene = Show(Group("b", a, b));
        using var clock = new TestClock();

        Tap(scene, a);
        clock.At(32 + 144);
        scene.Window.UpdateLayout();

        Assert.True(Content(a).Bounds.Height < ContentHeight);
        Assert.Equal(ContentHeight, Content(a).Child!.Bounds.Height);
    }

    [AvaloniaFact]
    public void B14_An_Accordion_Never_Opened_Is_Not_Animated() => InstalledTheme.Run(_ =>
    {
        // A tap the group refuses is still an interaction, but it has never been open, so its host has no transitions.
        var inner = Accordion("inner");
        var group = new AvonicAccordionGroup { Readonly = true };
        group.Items.Add(new StackPanel { Children = { inner } });
        using var scene = Show(group);

        Tap(scene, inner);

        Assert.Null(inner.Transitions);
    });

    [AvaloniaFact]
    public void B14_Nothing_Animates_Before_An_Interaction()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group(null, a, b);
        using var scene = Show(group);
        using var clock = new TestClock();

        // The first value an accordion hears of isn't an interaction.
        group.Value = "a";
        Assert.Equal(ContentHeight, Shown(scene, a));
    }

    [AvaloniaFact]
    public void B14_With_Nothing_To_Ease_It_Waits_Out_Transitionends_Timeout()
    {
        var a = new AvonicAccordion { Value = "a", HeaderContent = Item("a"), ContentContent = new Border { Height = 0 } };
        var b = Accordion("b");
        var group = Group("b", a, b);
        using var scene = Show(group);
        using var clock = new TestClock();

        // a has been open once, so its next expansion animates.
        group.Value = "a";
        clock.At(1000);
        group.Value = "b";
        clock.At(2000);

        Tap(scene, a);
        clock.At(2000 + 32 + 1000);
        Assert.Equal(AvonicAccordion.AccordionState.Expanding, a.State);
        clock.At(2000 + 32 + 2000);
        Assert.Equal(AvonicAccordion.AccordionState.Expanded, a.State);
    }

    [AvaloniaFact]
    public void B14_Leaving_The_Group_Mid_Collapse_Finishes_It()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        var panel = new StackPanel();
        using var scene = new Scene(new StackPanel { Children = { group, panel } });
        using var clock = new TestClock();

        Tap(scene, b);
        clock.At(32 + 100);
        Assert.True(double.IsFinite(Content(a).MaxHeight));

        group.Items.Remove(a);
        panel.Children.Add(a);

        // At once: with no group, nothing would be waiting on it.
        Assert.Equal(0, Shown(scene, a));
        Assert.True(double.IsPositiveInfinity(Content(a).MaxHeight));
        clock.At(1000);

        Assert.Equal(0, Shown(scene, a));
        Assert.True(double.IsPositiveInfinity(Content(a).MaxHeight));
    }

    [AvaloniaFact]
    public void B14_A_Padded_Content_Box_Eases_To_Its_Whole_Height()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        using var scene = Show(Group("a", a, b));
        using var clock = new TestClock();
        Content(b).Padding = new Thickness(0, 8);

        Tap(scene, b);
        clock.At(32 + 288);

        Assert.Equal((ContentHeight + 16) * Ease.Ease(288 / 300.0), Content(b).MaxHeight, 6);
    }

    [AvaloniaFact]
    public void B14_A_New_Template_Mid_Transition_Finishes_It()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        using var scene = Show(Group("a", a, b));
        using var clock = new TestClock();

        Tap(scene, b);
        clock.At(32 + 100);
        foreach (var accordion in new[] { a, b })
        {
            var template = accordion.Template;
            accordion.Template = null;
            accordion.ApplyTemplate();
            accordion.Template = template;
            accordion.ApplyTemplate();
        }

        // Finished at once, and the old template's frames leave the new content box alone.
        Assert.True(double.IsPositiveInfinity(Content(a).MaxHeight));
        Assert.True(double.IsPositiveInfinity(Content(b).MaxHeight));
        clock.At(1000);
        Assert.Equal(ContentHeight, Shown(scene, b));
        Assert.Equal(0, Shown(scene, a));
        Assert.True(double.IsPositiveInfinity(Content(a).MaxHeight));
        Assert.True(double.IsPositiveInfinity(Content(b).MaxHeight));
    }

    [AvaloniaFact]
    public void B15_Animated_Off_On_The_Group_Means_No_Animation()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        group.Animated = false;
        using var scene = Show(group);
        using var clock = new TestClock();

        Tap(scene, b);

        Assert.Equal(0, Shown(scene, a));
        Assert.Equal(ContentHeight, Shown(scene, b));
    }

    [AvaloniaFact]
    public void B15_The_Global_Animated_Off_Means_No_Animation()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        using var scene = Show(Group("a", a, b));
        using var clock = new TestClock();
        AvonicConfig.Animated = false;
        try
        {
            Tap(scene, b);

            Assert.Equal(0, Shown(scene, a));
            Assert.Equal(ContentHeight, Shown(scene, b));
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    }

    [AvaloniaFact]
    public void B16_A_Later_Change_Of_The_Value_Animates_Too()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group(null, a, b);
        using var scene = Show(group);
        using var clock = new TestClock();

        group.Value = "a";
        group.Value = "b";
        Assert.Equal(0, Shown(scene, b));
        Assert.Equal(ContentHeight, Shown(scene, a));

        clock.At(32 + 144);
        Assert.InRange(Shown(scene, b), 1, ContentHeight - 1);
        Assert.InRange(Shown(scene, a), 1, ContentHeight - 1);
    }

    // ── B17, B18, B19: inset, and the item's border ──────────────────────────────────────────

    [AvaloniaFact]
    public void B18_Inset_Margins_Corners_And_Shadows() => InstalledTheme.Run(_ =>
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b"), c = Accordion("c");
        var group = Group("b", a, b, c);
        group.Expand = AccordionGroupExpand.Inset;
        using var scene = Show(group);
        using var clock = new TestClock();

        // Measured: the group at 16,212 358 wide; a 8 8 6 6, b 6 with 16 above and below, c 6 6 8 8.
        Assert.Equal(new Thickness(16), group.Margin);
        Assert.Equal(new CornerRadius(8, 8, 6, 6), a.CornerRadius);
        Assert.Equal(new CornerRadius(6), b.CornerRadius);
        Assert.Equal(new CornerRadius(6, 6, 8, 8), c.CornerRadius);
        Assert.Equal(new Thickness(0), a.Margin);
        Assert.Equal(new Thickness(0, 16), b.Margin);
        Assert.Equal(new Thickness(0), c.Margin);
        Assert.Equal(358, a.Bounds.Width);
        Assert.Equal(3, Part<Border>(a, "PART_Host").BoxShadow.Count);

        // A closed one with no open neighbour has square corners; the first open one has no margin above.
        // Avalonia's transitions run on real time: this checks where they end.
        foreach (var accordion in new[] { a, b, c })
        {
            accordion.Transitions = [];
        }

        group.Value = "a";
        clock.At(1000);
        Assert.Equal(new CornerRadius(6, 6, 0, 0), b.CornerRadius);
        Assert.Equal(new CornerRadius(0, 0, 8, 8), c.CornerRadius);
        Assert.Equal(new Thickness(0, 0, 0, 16), a.Margin);

        // One in the middle with no open neighbour is square.
        group.Value = null;
        clock.At(2000);
        Assert.Equal(new CornerRadius(0), b.CornerRadius);
    });

    [AvaloniaFact]
    public void B18_Compact_Has_No_Margin_Corners_Or_Shadow() => InstalledTheme.Run(_ =>
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        using var scene = Show(group);

        Assert.Equal(new Thickness(0), group.Margin);
        Assert.Equal(new Thickness(0), a.Margin);
        Assert.Equal(new CornerRadius(0), a.CornerRadius);
        // background-color: var(ion-background-color), and the host doesn't clip, so an inset shadow shows.
        Assert.Equal(Colors.White, Assert.IsAssignableFrom<ISolidColorBrush>(a.Background).Color);
        Assert.False(a.ClipToBounds);
        Assert.Equal(0, Part<Border>(a, "PART_Host").BoxShadow.Count);
    });

    [AvaloniaFact]
    public void B18_Inset_Margin_Eases_Once_Animated() => InstalledTheme.Run(_ =>
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b"), c = Accordion("c");
        var group = Group("a", a, b, c);
        group.Expand = AccordionGroupExpand.Inset;
        using var scene = Show(group);
        using var clock = new TestClock();

        Tap(scene, b);
        clock.At(16);

        Assert.NotNull(b.Transitions);
        // Avalonia's transition runs on real time, so just after the change the margin has barely begun to grow.
        Assert.True(b.Margin.Top < 8);
    });

    [AvaloniaFact]
    public void B18_Changing_Expand_Reaches_The_Accordions() => InstalledTheme.Run(_ =>
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        using var scene = Show(group);
        Assert.Equal(new CornerRadius(0), a.CornerRadius);

        group.Expand = AccordionGroupExpand.Inset;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new CornerRadius(8, 8, 6, 6), a.CornerRadius);
        Assert.Equal(3, Part<Border>(b, "PART_Host").BoxShadow.Count);
    });

    [AvaloniaFact]
    public void An_Accordion_Among_The_Items_Is_Not_Put_Through_The_Item_Template()
    {
        var a = Accordion("a");
        var group = Group("a", a);
        group.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<object>((_, _) => new TextBlock { Text = "templated" });
        using var scene = Show(group);

        Assert.Contains(a, group.GetVisualDescendants());
        Assert.DoesNotContain(group.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "templated");
    }

    /// <summary>Accordions made from <c>ItemsSource</c> and a template are the group's own too, in presenters.</summary>
    [AvaloniaFact]
    public void B18_Accordions_From_ItemsSource_Are_The_Groups_Own() => InstalledTheme.Run(_ =>
    {
        var group = new AvonicAccordionGroup
        {
            Expand = AccordionGroupExpand.Inset,
            Value = "b",
            ItemsSource = new[] { "a", "b", "c" },
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((value, _) => Accordion(value)),
        };
        var changes = Changes(group);
        using var scene = Show(group);
        var accordions = group.GetVisualDescendants().OfType<AvonicAccordion>().ToList();
        Assert.Equal(3, accordions.Count);
        AvonicAccordion a = accordions[0], b = accordions[1], c = accordions[2];

        // First, before the open one; the open one; last, after it. And the last's line goes.
        Assert.Equal(new CornerRadius(8, 8, 6, 6), a.CornerRadius);
        Assert.Equal(new CornerRadius(6), b.CornerRadius);
        Assert.Equal(new CornerRadius(6, 6, 8, 8), c.CornerRadius);
        Assert.Equal(new Thickness(0), NativeBorder(c));

        // Keys move between them, and readonly reaches them.
        ((AvonicItem)a.HeaderContent!).Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.Same(b.HeaderContent, scene.Window.FocusManager!.GetFocusedElement());
        group.Readonly = true;
        Dispatcher.UIThread.RunJobs();
        Assert.False(Header(a).IsHitTestVisible);
        Assert.Empty(changes);
    });

    private static Thickness NativeBorder(AvonicAccordion accordion) =>
        Part<Border>((AvonicItem)accordion.HeaderContent!, "PART_Native").BorderThickness;

    [AvaloniaFact]
    public void B19_The_Header_Items_Border_Goes_When_Open_And_On_The_Last() => InstalledTheme.Run(_ =>
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b"), c = Accordion("c");
        using var scene = Show(Group("a", a, b, c));

        // Measured: 0, 1px, 0.
        Assert.Equal(new Thickness(0), NativeBorder(a));
        Assert.Equal(new Thickness(0, 0, 0, 1), NativeBorder(b));
        Assert.Equal(new Thickness(0), NativeBorder(c));
    });

    [AvaloniaFact]
    public void B17_B19_Inset_A_Closed_One_Before_An_Open_One_Loses_Both_Borders() => InstalledTheme.Run(_ =>
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b"), c = Accordion("c"), d = Accordion("d");
        foreach (var accordion in new[] { a, b, c, d })
        {
            ((AvonicItem)accordion.HeaderContent!).Lines = ItemLines.Inset;
        }

        var group = Group("c", a, b, c, d);
        group.Expand = AccordionGroupExpand.Inset;
        using var scene = Show(group);

        Thickness Inner(AvonicAccordion accordion) => Part<Border>((AvonicItem)accordion.HeaderContent!, "PART_Inner").BorderThickness;
        Assert.Equal(new Thickness(0), Inner(b));
        Assert.Equal(new Thickness(0, 0, 0, 1), Inner(a));

        // Open, it's no longer the one before an open one, so its inner line is back: only border-width goes while open.
        group.Animated = false;
        group.Value = "b";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(0, 0, 0, 1), Inner(b));
    });

    // ── B20: keys ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B20_Arrow_Keys_Home_And_End_Move_Focus_Between_The_Headers_Wrapping()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b"), c = Accordion("c");
        using var scene = Show(Group(null, a, b, c));
        AvonicItem Item(AvonicAccordion accordion) => (AvonicItem)accordion.HeaderContent!;
        IInputElement? Focused() => scene.Window.FocusManager!.GetFocusedElement();
        void Press(PhysicalKey key)
        {
            scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
            scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        }

        Item(a).Focus();
        Press(PhysicalKey.ArrowDown);
        Assert.Same(Item(b), Focused());
        Press(PhysicalKey.ArrowDown);
        Press(PhysicalKey.ArrowDown);
        Assert.Same(Item(a), Focused());
        Press(PhysicalKey.ArrowUp);
        Assert.Same(Item(c), Focused());
        Press(PhysicalKey.Home);
        Assert.Same(Item(a), Focused());
        Press(PhysicalKey.End);
        Assert.Same(Item(c), Focused());
    }

    [AvaloniaFact]
    public void B20_A_Group_Inside_Moves_Focus_Among_Its_Own_Only()
    {
        AvonicAccordion x = Accordion("x"), y = Accordion("y");
        var inner = Group(null, x, y);
        var outer = Accordion("outer");
        outer.ContentContent = inner;
        var other = Accordion("other");
        using var scene = Show(Group("outer", outer, other));

        ((AvonicItem)x.HeaderContent!).Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);

        Assert.Same(y.HeaderContent, scene.Window.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    public void B20_A_Disabled_Accordion_Is_Passed_Over()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b"), c = Accordion("c");
        c.IsEnabled = false;
        using var scene = Show(Group(null, a, b, c));

        ((AvonicItem)b.HeaderContent!).Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Assert.Same(a.HeaderContent, scene.Window.FocusManager!.GetFocusedElement());

        scene.Window.KeyPressQwerty(PhysicalKey.End, RawInputModifiers.None);
        Assert.Same(b.HeaderContent, scene.Window.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    public void B20_Home_On_The_First_Leaves_Focus_Where_It_Is()
    {
        var a = Accordion("a");
        var button = new Button { Content = "In the header" };
        ((AvonicItem)a.HeaderContent!).StartContent = button;
        using var scene = Show(Group(null, a, Accordion("b")));

        button.Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.Home, RawInputModifiers.None);

        Assert.Same(button, scene.Window.FocusManager!.GetFocusedElement());
    }

    [AvaloniaFact]
    public void B20_Keys_In_The_Content_Are_Left_Alone()
    {
        var a = Accordion("a");
        var box = new Button { Content = "In the content" };
        a.ContentContent = box;
        var b = Accordion("b");
        using var scene = Show(Group("a", a, b));

        box.Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);

        Assert.Same(box, scene.Window.FocusManager!.GetFocusedElement());
    }

    // ── B22: automation ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B22_A_Peer_Made_While_Open_Announces_No_Change()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group(new[] { "a" }, a, b);
        group.Multiple = true;
        using var scene = Show(group);
        var peer = ControlAutomationPeer.CreatePeerForElement(HeaderOf(a));
        var changes = 0;
        peer.PropertyChanged += (_, e) => changes += e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty ? 1 : 0;

        // a is told of the new value, and renders, still open.
        group.Value = new[] { "a", "b" };

        Assert.Equal(0, changes);
    }

    [AvaloniaFact]
    public void B22_The_Peer_Says_Whether_It_Is_Expanded_And_Asks_The_Group()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        using var scene = Show(group);
        using var clock = new TestClock();

        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(HeaderOf(b));
        Assert.Equal(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);

        peer.Expand();
        Assert.Equal("b", group.Value);
        // Ionic sets aria-expanded as it renders the new state, a frame later.
        clock.At(16);
        Assert.Equal(ExpandCollapseState.Expanded, peer.ExpandCollapseState);
    }

    // setAria puts aria-expanded on the header item's button, so the item says it, and the accordion and other items don't.
    [AvaloniaFact]
    public void B22_The_Header_Item_Says_It_And_Nothing_Else_Does()
    {
        var a = Accordion("a");
        var other = Item("other");
        var group = Group("a", a);
        using var scene = Show(new StackPanel { Children = { group, other } });

        Assert.NotNull(ControlAutomationPeer.CreatePeerForElement(HeaderOf(a)).GetProvider<IExpandCollapseProvider>());
        Assert.Null(ControlAutomationPeer.CreatePeerForElement(a).GetProvider<IExpandCollapseProvider>());
        Assert.Null(ControlAutomationPeer.CreatePeerForElement(other).GetProvider<IExpandCollapseProvider>());
    }

    [AvaloniaFact]
    public void B22_The_Header_Item_Announces_A_Change()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        using var scene = Show(group);
        using var clock = new TestClock();
        var peer = ControlAutomationPeer.CreatePeerForElement(HeaderOf(b));
        _ = ((IExpandCollapseProvider)peer).ExpandCollapseState;
        var changes = new List<object?>();
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
            {
                changes.Add(e.NewValue);
            }
        };

        group.Value = "b";
        clock.At(16);

        Assert.Equal([ExpandCollapseState.Expanded], changes);
    }

    // With no animation the accordion renders once, already in the new state: the peer still announces it.
    [AvaloniaFact]
    public void B22_The_Header_Item_Announces_A_Change_With_No_Animation()
    {
        AvonicAccordion a = Accordion("a"), b = Accordion("b");
        var group = Group("a", a, b);
        group.Animated = false;
        using var scene = Show(group);
        var peer = ControlAutomationPeer.CreatePeerForElement(HeaderOf(b));
        var changes = new List<object?>();
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
            {
                changes.Add(e.NewValue);
            }
        };

        group.Value = "b";
        group.Value = null;

        Assert.Equal([ExpandCollapseState.Expanded, ExpandCollapseState.Collapsed], changes);
    }

    private static AvonicItem HeaderOf(AvonicAccordion accordion) => (AvonicItem)accordion.HeaderContent!;
}

internal static class AccordionTestExtensions
{
    /// <summary>The toggle icons in what an item holds.</summary>
    public static IEnumerable<AvonicIcon> GetLogicalDescendantsAndSelfIcons(this AvonicItem item) =>
        new[] { item.StartContent, item.Content, item.EndContent }
            .OfType<Control>()
            .SelectMany(c => Avalonia.LogicalTree.LogicalExtensions.GetSelfAndLogicalDescendants(c))
            .OfType<AvonicIcon>()
            .Where(icon => icon.Classes.Contains("ion-accordion-toggle-icon"));
}
