using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Styling;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ion-segment's indicator, keys, scrolling and layout, ion-segment-button's, and ion-segment-view's behaviours, from their
/// <c>.js</c> and <c>.md.css</c>, measured in Chromium: see <c>docs/components/ion-segment/PLAN.md</c>.
/// </summary>
public class AvonicSegmentFamilyTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────────────────────

    private static AvonicSegmentButton Button(string value, string label, string? icon = null, SegmentButtonLayout layout = SegmentButtonLayout.IconTop) =>
        AvonicSegmentCaptureTests.Button(value, label, icon, layout);

    private static AvonicSegment Segment(string? value, params AvonicSegmentButton[] buttons)
    {
        var segment = AvonicSegmentCaptureTests.Segment(value ?? "", buttons);
        segment.Value = value;
        segment.VerticalAlignment = VerticalAlignment.Top;
        return segment;
    }

    private static List<object?> Changes(AvonicSegment segment)
    {
        var changes = new List<object?>();
        segment.IonChange += (_, e) => changes.Add(e.Value);
        return changes;
    }

    private static Rect Box(Visual visual, Visual to) => new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(to)!.Value);

    private static Point Centre(Visual visual, Scene scene) => Box(visual, scene.Window).Center;

    private static AvonicSegmentButton[] Buttons(AvonicSegment segment) => segment.Items.OfType<AvonicSegmentButton>().ToArray();

    private static T Part<T>(Visual control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Tap(Scene scene, Point at)
    {
        GestureTests.Render();
        scene.Window.MouseMove(at, RawInputModifiers.None);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        Settle();
        GestureTests.Render();
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        Settle();
    }

    private static void Key(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Settle();
    }

    private static object? FocusedValue(Scene scene) => (scene.Window.FocusManager?.GetFocusedElement() as AvonicSegmentButton)?.Value;

    private static (double X, double Scale) Transform(AvonicSegmentButton button) => button.Slide!.Current;

    // ── The indicator ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S9_The_Indicator_Slides_From_The_Last_Button_By_The_Themes_Transition() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var segment = Segment("a", Button("a", "Friends"), Button("b", "Enemies"));
        using var scene = new Scene(segment);
        Settle();
        var b = Buttons(segment)[1];

        segment.Value = "b";
        // Ionic's writeTask: the transform is set in the next frame, translateX(previous.left - current.left) scaleX(1).
        clock.At(16);
        Assert.Equal((-195.0, 1.0), Transform(b));

        // transform 250ms cubic-bezier(0.4, 0, 0.2, 1), towards none. Chromium had −185.3 at 54 ms and −24.2 at 162: the same
        // curve, sampled a frame late.
        var easing = new CubicBezierEasing(0.4, 0, 0.2, 1);
        foreach (var at in new[] { 32.0, 112, 160, 240 })
        {
            clock.At(16 + at);
            Assert.Equal(-195 * (1 - easing.Ease(at / 250)), Transform(b).X, 3);
        }

        // The first frame after it ends.
        clock.At(16 + 250 + 16);
        Assert.Null(Part<Control>(b, "PART_Indicator").RenderTransform);
    });

    [AvaloniaFact]
    public void S9_The_Indicator_Scales_From_The_Last_Ones_Width()
    {
        using var clock = new TestClock();
        var wide = Button("a", "Friends");
        wide.Width = 200;
        var narrow = Button("b", "Enemies");
        narrow.Width = 100;
        var segment = Segment("a", wide, narrow);
        segment.Resources["Avonic.SegmentButton.IndicatorTransition"] = new Avalonia.Animation.Transitions
        {
            new Avalonia.Animation.TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = TimeSpan.FromMilliseconds(250) },
        };
        using var scene = new Scene(segment);
        Settle();

        segment.Value = "b";
        clock.At(16);

        var expected = Box(Part<Control>(wide, "PART_Indicator"), segment);
        Assert.Equal(expected, Box(Part<Control>(narrow, "PART_Indicator"), segment));
    }

    [AvaloniaFact]
    public void S9_Without_A_Transition_The_Indicator_Jumps()
    {
        using var clock = new TestClock();
        var segment = Segment("a", Button("a", "Friends"), Button("b", "Enemies"));
        using var scene = new Scene(segment);
        Settle();
        var b = Buttons(segment)[1];

        segment.Value = "b";
        clock.At(16);

        Assert.Null(Part<Control>(b, "PART_Indicator").RenderTransform);
        Assert.True(b.IsChecked);
    }

    // ── Keys ─────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S10_The_Arrows_Focus_The_Next_Enabled_Button_Wrapping_And_Enter_And_Space_Check_It()
    {
        var two = Button("2", "Two");
        two.IsEnabled = false;
        var segment = Segment("1", Button("1", "One"), two, Button("3", "Three"));
        var changes = Changes(segment);
        using var scene = new Scene(segment);
        Settle();
        Buttons(segment)[0].Focus(NavigationMethod.Tab);

        Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal("3", FocusedValue(scene));
        Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal("1", FocusedValue(scene));
        Key(scene, PhysicalKey.ArrowLeft);
        Assert.Equal("3", FocusedValue(scene));
        Assert.Equal("1", segment.Value);
        Assert.Empty(changes);

        Key(scene, PhysicalKey.Enter);
        Assert.Equal("3", segment.Value);
        Key(scene, PhysicalKey.Home);
        Assert.Equal("1", FocusedValue(scene));
        Key(scene, PhysicalKey.Space);
        Assert.Equal("1", segment.Value);
        Key(scene, PhysicalKey.End);
        Assert.Equal("3", FocusedValue(scene));

        Assert.Equal(["3", "1"], changes);
        Assert.Contains(FocusVisible.FocusedPseudoClass, Buttons(segment)[2].Classes);
    }

    [AvaloniaFact]
    public void S10_SelectOnFocus_Checks_What_The_Keys_Focus_And_RTL_Swaps_The_Arrows()
    {
        var segment = Segment("a", Button("a", "A"), Button("b", "B"), Button("c", "C"));
        segment.SelectOnFocus = true;
        segment.FlowDirection = FlowDirection.RightToLeft;
        var changes = Changes(segment);
        using var scene = new Scene(segment);
        Settle();
        Buttons(segment)[0].Focus(NavigationMethod.Tab);

        Key(scene, PhysicalKey.ArrowLeft);
        Assert.Equal("b", FocusedValue(scene));
        Assert.Equal("b", segment.Value);
        Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal("a", segment.Value);
        Key(scene, PhysicalKey.End);
        Assert.Equal("c", segment.Value);

        Assert.Equal(["b", "a", "c"], changes);
    }

    [AvaloniaFact]
    public void S10_Each_Button_Is_A_Tab_Stop()
    {
        var segment = Segment("a", Button("a", "A"), Button("b", "B"));
        using var scene = new Scene(new StackPanel { Children = { new Button { Content = "before" }, segment } });
        Settle();

        Key(scene, PhysicalKey.Tab);
        Key(scene, PhysicalKey.Tab);
        Assert.Equal("a", FocusedValue(scene));
        Key(scene, PhysicalKey.Tab);
        Assert.Equal("b", FocusedValue(scene));
    }

    // ── Layout and scrolling ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S13_Columns_Grow_Equally_From_Each_Min_Width_To_360_Centred() => InstalledTheme.Run(_ =>
    {
        var one = Segment("a", Button("a", "Only"));
        var five = Segment("a", Button("a", "One"), Button("b", "Two"), Button("c", "Three"), Button("d", "Four"), Button("e", "Five long label"));
        var wide = Button("a", "Wide");
        wide.MinWidth = 150;
        var mixed = Segment("a", wide, Button("b", "B"));
        var two = Segment("a", Button("a", "Friends"), Button("b", "Enemies"));
        using var scene = new Scene(new StackPanel { Children = { one, five, mixed, two } });
        Settle();

        static (double X, double W)[] Columns(AvonicSegment segment) =>
            Buttons(segment).Select(b => Box(b, segment)).Select(r => (Math.Round(r.X, 2), Math.Round(r.Width, 2))).ToArray();

        // Measured in Chromium: 360 at x 15; 90 each from x −30; 225 and 165; 195 each.
        Assert.Equal([(15.0, 360.0)], Columns(one));
        Assert.Equal([(-30.0, 90.0), (60, 90), (150, 90), (240, 90), (330, 90)], Columns(five));
        Assert.Equal([(0.0, 225.0), (225, 165)], Columns(mixed));
        Assert.Equal([(0.0, 195.0), (195, 195)], Columns(two));
        Assert.All(Buttons(two), b => Assert.Equal(48, b.Bounds.Height));
    });

    [AvaloniaFact]
    public void S11_Scrollable_Columns_Are_Their_Contents_Width_Or_A_Share_And_The_Checked_One_Is_Centred_At_Load() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var shares = Segment("a", Button("a", "A"), Button("b", "Longer label"));
        shares.Scrollable = true;
        var many = Segment("s4", Button("s1", "First"), Button("s2", "Second"), Button("s3", "Third"), Button("s4", "Fourth"), Button("s5", "Fifth"), Button("s6", "Sixth"));
        many.Scrollable = true;
        using var scene = new Scene(new StackPanel { Children = { shares, many } });
        Settle();
        clock.At(16);
        GestureTests.Render();

        // Measured: A and Longer label share 390 at 195 each; the six are their text and 32 wide, and Fourth is centred.
        Assert.All(Buttons(shares), b => Assert.Equal(195, b.Bounds.Width, 1));
        var widths = Buttons(many).Select(b => b.Bounds.Width).ToArray();
        Assert.All(widths, w => Assert.InRange(w, 70, 95));
        var fourth = Box(Buttons(many)[3], scene.Window);
        Assert.Equal(195, fourth.Center.X, 0.6);
    });

    [AvaloniaFact]
    public void S11_S12_A_Change_Scrolls_Smoothly_For_The_Root_Of_The_Distance_Over_60_Seconds() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var many = Segment("s4", Button("s1", "First"), Button("s2", "Second"), Button("s3", "Third"), Button("s4", "Fourth"), Button("s5", "Fifth"), Button("s6", "Sixth"));
        many.Scrollable = true;
        using var scene = new Scene(many);
        Settle();
        clock.At(16);
        var scroll = Part<ScrollViewer>(many, "PART_Scroll");
        var from = scroll.Offset.X;
        Assert.True(from > 0);

        many.Value = "s1";
        clock.At(32);
        // The target is centring First, which is as far as it goes: 0.
        var duration = Math.Sqrt(from) / 60 * 1000;
        var start = clock.Now.TotalMilliseconds - 16;
        clock.At(start + duration / 2);
        Assert.Equal(from / 2, scroll.Offset.X, 0);
        clock.At(start + duration + 16);
        Assert.Equal(0, scroll.Offset.X);
    });

    [AvaloniaFact]
    public void S12_The_Smooth_Scroll_Is_Chromiums_Ease_In_Out()
    {
        Assert.Equal(465.5, SmoothScroll.DurationFor(780).TotalMilliseconds, 1);
        Assert.Equal(329.1, SmoothScroll.DurationFor(390).TotalMilliseconds, 1);
        // Chromium, 0 to 780: 392 at 222 ms, with its start about 12 ms before the first sample.
        Assert.InRange(780 * SmoothScroll.Easing.Ease((222 + 12) / 465.5), 392 - 3, 392 + 3);
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S14_Disabling_The_Segment_Disables_Every_Button_And_Enabling_It_Enables_Them_All()
    {
        var two = Button("2", "Two");
        two.IsEnabled = false;
        var segment = Segment("1", Button("1", "One"), two);
        using var scene = new Scene(segment);
        Settle();

        segment.IsEnabled = false;
        Assert.All(Buttons(segment), b => Assert.False(b.IsEffectivelyEnabled));
        segment.IsEnabled = true;
        Assert.True(two.IsEnabled);
    }

    [AvaloniaFact]
    public void S15_The_Button_After_The_Checked_One_Is_Marked_At_Load_And_When_It_Slides()
    {
        using var clock = new TestClock();
        var segment = Segment("a", Button("a", "A"), Button("b", "B"), Button("c", "C"));
        using var scene = new Scene(segment);
        Settle();
        var buttons = Buttons(segment);
        Assert.Equal([false, true, false], buttons.Select(b => b.Classes.Contains(":after-checked")));

        segment.Value = "b";
        Assert.Equal([false, false, true], buttons.Select(b => b.Classes.Contains(":after-checked")));
    }

    [AvaloniaFact]
    public void S16_In_A_Toolbar_It_Takes_The_Toolbars_Height() => InstalledTheme.Run(_ =>
    {
        var segment = Segment("a", Button("a", "Inbox"), Button("b", "Sent"));
        var colored = Segment("a", Button("a", "Inbox"), Button("b", "Sent"));
        using var scene = new Scene(new StackPanel
        {
            Children = { new AvonicToolbar { Content = segment }, new AvonicToolbar { Color = "primary", Content = colored } },
        });
        Settle();

        Assert.Equal(56, segment.Bounds.Height);
        Assert.All(Buttons(segment), b => Assert.Equal(56, b.Bounds.Height));
        Assert.Contains(":in-toolbar", segment.Classes);
        Assert.Contains(":in-toolbar-color", colored.Classes);
        Assert.DoesNotContain(":in-toolbar-color", segment.Classes);
    });

    [AvaloniaFact]
    public void S17_A_Button_Added_Later_Is_Checked_If_Its_Value_Matches()
    {
        var segment = Segment("c", Button("a", "A"));
        using var scene = new Scene(segment);
        Settle();

        var c = Button("c", "C");
        segment.Items.Add(c);
        Settle();

        Assert.True(c.IsChecked);
    }

    [AvaloniaFact]
    public void S18_It_Tells_An_Item_It_Is_A_Segment()
    {
        var segment = Segment("a", Button("a", "A"));
        var item = new AvonicItem { Content = segment };
        using var scene = new Scene(item);
        Settle();

        Assert.Contains(":item-segment", item.Classes);
        Assert.Equal(ItemControlType.Segment, ((IItemControl)segment).ItemControlType);
    }

    // ── The button ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_The_Layout_Puts_The_Icon_First_In_Its_Direction_Or_Hides_One() => InstalledTheme.Run(_ =>
    {
        var layouts = Enum.GetValues<SegmentButtonLayout>();
        var buttons = layouts.Select(l => Button(l.ToString(), "Label", "call", l)).ToArray();
        var page = new StackPanel();
        foreach (var button in buttons)
        {
            page.Children.Add(Segment(button.Value as string, button));
        }

        using var scene = new Scene(page);
        Settle();

        foreach (var button in buttons)
        {
            var label = button.Items.OfType<AvonicLabel>().Single();
            var icon = button.Items.OfType<AvonicIcon>().Single();
            var (l, i) = (Box(label, button), Box(icon, button));
            switch (button.Layout)
            {
                case SegmentButtonLayout.IconTop:
                    Assert.True(i.Bottom <= l.Top, "icon above");
                    break;
                case SegmentButtonLayout.IconBottom:
                    Assert.True(l.Bottom <= i.Top, "icon below");
                    break;
                case SegmentButtonLayout.IconStart:
                    Assert.Equal(8, l.Left - i.Right, 1);
                    break;
                case SegmentButtonLayout.IconEnd:
                    Assert.Equal(8, i.Left - l.Right, 1);
                    break;
                case SegmentButtonLayout.IconHide:
                    Assert.False(icon.IsEffectivelyVisible);
                    break;
                case SegmentButtonLayout.LabelHide:
                    Assert.False(label.IsEffectivelyVisible);
                    break;
            }
        }
    });

    [AvaloniaFact]
    public void L1_A_Label_Is_22_High_12_From_The_Top_And_An_Icon_And_Label_Make_70() => InstalledTheme.Run(_ =>
    {
        var labelOnly = Button("a", "Friends");
        var both = Button("call", "Call", "call");
        var start = Button("wifi", "Wi-Fi", "wifi", SegmentButtonLayout.IconStart);
        var iconOnly = new AvonicSegmentButton { Value = "i", Items = { new AvonicIcon { IconName = "call" } } };
        using var scene = new Scene(new StackPanel
        {
            Children = { Segment("a", labelOnly), Segment("call", both), Segment("wifi", start), Segment("i", iconOnly) },
        });
        Settle();

        var label = Box(labelOnly.Items.OfType<AvonicLabel>().Single(), labelOnly);
        Assert.Equal((13.0, 22.0), (label.Top, label.Height));
        Assert.Equal(48, labelOnly.Bounds.Height);
        Assert.Equal(70, both.Bounds.Height);
        Assert.Equal(48, start.Bounds.Height);
        var icon = Box(iconOnly.Items.OfType<AvonicIcon>().Single(), iconOnly);
        Assert.Equal((12.0, 24.0), (icon.Top, icon.Height));
    });

    [AvaloniaFact]
    public void B7_A_Label_Is_Upper_Case_On_One_Line_With_An_Ellipsis() => InstalledTheme.Run(_ =>
    {
        var button = Button("a", "A label far too long to fit in its column at all");
        var segment = Segment("a", button, Button("b", "B"), Button("c", "C"), Button("d", "D"));
        using var scene = new Scene(segment);
        Settle();

        var text = button.Items.OfType<AvonicLabel>().Single().GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal("A LABEL FAR TOO LONG TO FIT IN ITS COLUMN AT ALL", text.Text);
        Assert.Equal(TextTrimming.CharacterEllipsis, text.TextTrimming);
        Assert.Equal(TextWrapping.NoWrap, text.TextWrapping);
        Assert.True(Box(text, button).Right <= button.Bounds.Width - 16 + 0.5);
    });

    [AvaloniaFact]
    public void B2_A_Button_Without_A_Value_Has_A_Unique_One()
    {
        var (a, b) = (new AvonicSegmentButton(), new AvonicSegmentButton());

        Assert.StartsWith("ion-sb-", (string)a.Value!);
        Assert.NotEqual(a.Value, b.Value);
    }

    [AvaloniaFact]
    public void B3_The_Press_Ripples_And_The_Capture_Cancels_It_So_A_Held_Press_Does_Not_Keep_It()
    {
        using var clock = new TestClock();
        var segment = Segment("a", Button("a", "Friends"), Button("b", "Enemies"));
        using var scene = new Scene(segment);
        Settle();
        var b = Buttons(segment)[1];
        var ripple = Part<AvonicRippleEffect>(b, "PART_Ripple");

        GestureTests.Render();
        scene.Window.MouseMove(Centre(b, scene), RawInputModifiers.None);
        scene.Window.MouseDown(Centre(b, scene), MouseButton.Left, RawInputModifiers.None);
        Settle();

        // Instant: activated and rippling at the press, then cancelled by the gesture's capture, as ionGestureCaptured cancels
        // tap-click: measured, the ripple gone by 600 ms with the press still held.
        Assert.DoesNotContain(Activatable.ActivatedPseudoClass, b.Classes);
        clock.At(600);
        ripple.Tick();
        Assert.Empty(ripple.Ripples);
        scene.Window.MouseUp(Centre(b, scene), MouseButton.Left, RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void B5_A_Disabled_Button_Is_At_0_3() => InstalledTheme.Run(_ =>
    {
        var two = Button("2", "Two");
        two.IsEnabled = false;
        var segment = Segment("1", Button("1", "One"), two);
        using var scene = new Scene(segment);
        Settle();

        Assert.Equal(0.3, two.Opacity);
        Assert.Equal(1, Buttons(segment)[0].Opacity);
    });

    [AvaloniaFact]
    public void B6_A_Button_Is_A_Tab_Item_Selected_When_Checked_And_Named_By_Its_Text()
    {
        var segment = Segment("a", Button("a", "Friends"), Button("b", "Enemies"));
        using var scene = new Scene(segment);
        Settle();
        var b = Buttons(segment)[1];
        var peer = ControlAutomationPeer.CreatePeerForElement(b);

        Assert.Equal(AutomationControlType.TabItem, peer.GetAutomationControlType());
        Assert.Equal("Enemies", peer.GetName());
        var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(peer);
        Assert.False(selection.IsSelected);
        selection.Select();
        Assert.True(selection.IsSelected);
        Assert.Equal(AutomationControlType.Tab, ControlAutomationPeer.CreatePeerForElement(segment).GetAutomationControlType());
    }

    [AvaloniaFact]
    public void B8_A_Button_With_A_Content_Id_Cannot_Be_Disabled()
    {
        var button = Button("a", "A");
        button.ContentId = "a";
        button.IsEnabled = false;
        using var scene = new Scene(Segment("a", button));

        Assert.True(button.IsEnabled);
    }

    // ── The look ─────────────────────────────────────────────────────────────────────────────

    private static Color ColorOf(IBrush? brush) => brush is ISolidColorBrush s ? Color.FromArgb((byte)Math.Round(s.Color.A * s.Opacity), s.Color.R, s.Color.G, s.Color.B) : default;

    [AvaloniaFact]
    public void L2_L3_L4_Text_And_Indicator_Colours() => InstalledTheme.Run(_ =>
    {
        var plain = Segment("a", Button("a", "A"), Button("b", "B"));
        var danger = Segment("a", Button("a", "A"), Button("b", "B"));
        danger.Color = "danger";
        var toolbar = Segment("a", Button("a", "A"), Button("b", "B"));
        using var scene = new Scene(new StackPanel
        {
            Children = { plain, danger, new AvonicToolbar { Color = "primary", Content = toolbar } },
        });
        foreach (var button in new[] { plain, danger, toolbar }.SelectMany(Buttons))
        {
            button.Transitions = [];
        }

        Settle();

        Color Text(AvonicSegment s, int i) => ColorOf(Buttons(s)[i].Foreground);
        Color Bar(AvonicSegment s, int i) => ColorOf(Part<Border>(Buttons(s)[i], "PART_IndicatorBackground").Background);

        // Measured: rgb(0, 84, 233) and rgba(0, 0, 0, 0.6); in danger, rgb(197, 0, 15); in the coloured toolbar, white and
        // rgba(255, 255, 255, 0.6), with a white indicator.
        Assert.Equal(Color.FromRgb(0, 84, 233), Text(plain, 0));
        Assert.Equal(Color.FromArgb(153, 0, 0, 0), Text(plain, 1));
        Assert.Equal(Color.FromRgb(0, 84, 233), Bar(plain, 0));
        Assert.Equal(2, Part<Border>(Buttons(plain)[0], "PART_IndicatorBackground").Bounds.Height);
        Assert.Equal(1, Part<Border>(Buttons(plain)[0], "PART_Indicator").Opacity);
        Assert.Equal(0, Part<Border>(Buttons(plain)[1], "PART_Indicator").Opacity);

        Assert.Equal(Color.FromRgb(197, 0, 15), Text(danger, 0));
        Assert.Equal(Color.FromArgb(153, 0, 0, 0), Text(danger, 1));
        Assert.Equal(Color.FromRgb(197, 0, 15), Bar(danger, 0));

        Assert.Equal(Colors.White, Text(toolbar, 0));
        Assert.Equal(Color.FromArgb(153, 255, 255, 255), Text(toolbar, 1));
        Assert.Equal(Colors.White, Bar(toolbar, 0));
    });

    [AvaloniaFact]
    public void L5_Hover_And_Focus_Show_The_Checked_Colour_At_0_04_And_0_12() => InstalledTheme.Run(_ =>
    {
        var segment = Segment("a", Button("a", "A"), Button("b", "B"));
        using var scene = new Scene(segment);
        Settle();
        var (a, b) = (Buttons(segment)[0], Buttons(segment)[1]);

        GestureTests.Render();
        scene.Window.MouseMove(Centre(b, scene), RawInputModifiers.None);
        Settle();
        var overlay = Part<Border>(b, "PART_Overlay");
        Assert.Equal(0.04, overlay.Opacity);
        Assert.Equal(Color.FromRgb(0, 84, 233), ColorOf(overlay.Background));

        a.Focus(NavigationMethod.Tab);
        Settle();
        Assert.Equal(0.12, Part<Border>(a, "PART_Overlay").Opacity);
    });

    // ── Found in review ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R1_A_Scrollable_Segments_Shares_Follow_Its_Width() => InstalledTheme.Run(_ =>
    {
        var segment = Segment("a", Button("a", "A"), Button("b", "Longer label"));
        segment.Scrollable = true;
        var host = new Border { Width = 390, HorizontalAlignment = HorizontalAlignment.Left, Child = segment };
        using var scene = new Scene(host);
        Settle();
        Assert.All(Buttons(segment), b => Assert.Equal(195, b.Bounds.Width, 1));

        host.Width = 300;
        Settle();
        Assert.All(Buttons(segment), b => Assert.Equal(150, b.Bounds.Width, 1));
    });

    [AvaloniaFact]
    public void R2_A_Hidden_Button_Still_Takes_The_Value_From_The_Keys()
    {
        var hidden = Button("c", "C");
        hidden.IsVisible = false;
        var segment = Segment("a", Button("a", "A"), Button("b", "B"), hidden);
        segment.SelectOnFocus = true;
        using var scene = new Scene(segment);
        Settle();
        Buttons(segment)[0].Focus(NavigationMethod.Tab);

        Key(scene, PhysicalKey.End);

        Assert.Equal("c", segment.Value);
    }

    [AvaloniaFact]
    public void R3_A_Segment_Enabled_While_Away_Comes_Back_With_Its_Buttons_Enabled()
    {
        var segment = Segment("a", Button("a", "A"), Button("b", "B"));
        var host = new Border { Child = segment };
        using var scene = new Scene(host);
        Settle();

        segment.IsEnabled = false;
        host.Child = null;
        Settle();
        segment.IsEnabled = true;
        host.Child = segment;
        Settle();

        Assert.All(Buttons(segment), b => Assert.True(b.IsEffectivelyEnabled));
    }

    [AvaloniaFact]
    public void R4_A_Segment_Unloaded_Mid_Drag_Lets_The_Indicator_Go()
    {
        using var clock = new TestClock();
        var segment = Segment("a", Button("a", "A"), Button("b", "B"));
        var host = new Border { Child = segment };
        using var scene = new Scene(host);
        Settle();
        GestureTests.Render();
        var at = Centre(Buttons(segment)[0], scene);
        scene.Window.MouseMove(at, RawInputModifiers.None);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        Settle();
        Assert.Contains(":segment-activated", segment.Classes);

        host.Child = null;
        Settle();

        Assert.DoesNotContain(":segment-activated", segment.Classes);
        Assert.All(Buttons(segment), b => Assert.DoesNotContain(":segment-button-activated", b.Classes));
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void R5_A_Row_With_Nothing_To_Shrink_But_An_Empty_Text_Lays_Out()
    {
        var button = new AvonicSegmentButton { Value = "a", Layout = SegmentButtonLayout.IconStart, Width = 20, Items = { new AvonicIcon { IconName = "call", Width = 40 }, "" } };
        using var scene = new Scene(Segment("a", button));
        Settle();

        Assert.True(button.IsArrangeValid);
    }

    [AvaloniaFact]
    public void R9_A_Style_Can_Give_A_Button_Its_Value()
    {
        var button = new AvonicSegmentButton { Classes = { "second" }, Items = { new AvonicLabel { Content = "B" } } };
        var segment = Segment("b", Button("a", "A"), button);
        segment.Styles.Add(new Style(x => x.OfType<AvonicSegmentButton>().Class("second"))
        {
            Setters = { new Setter(AvonicSegmentButton.ValueProperty, "b") },
        });
        using var scene = new Scene(segment);
        Settle();

        Assert.True(button.IsChecked);
    }

    [AvaloniaFact]
    public void R10_With_No_Width_To_Fill_Each_Column_Grows_To_360() => InstalledTheme.Run(_ =>
    {
        var segment = Segment("a", Button("a", "Notifications"), Button("b", "B"));
        using var scene = new Scene(new StackPanel { Orientation = Orientation.Horizontal, Children = { segment } });
        Settle();

        Assert.All(Buttons(segment), b => Assert.Equal(360, b.Bounds.Width));
    });

    [AvaloniaFact]
    public void R11_Assistive_Technology_Hears_The_Selected_Tab_Change()
    {
        var segment = Segment("a", Button("a", "A"), Button("b", "B"));
        using var scene = new Scene(segment);
        Settle();
        var peer = ControlAutomationPeer.CreatePeerForElement(Buttons(segment)[1]);
        var heard = new List<object?>();
        peer.PropertyChanged += (_, e) => heard.Add(e.NewValue);

        segment.Value = "b";

        Assert.Equal([true], heard);
    }
}
