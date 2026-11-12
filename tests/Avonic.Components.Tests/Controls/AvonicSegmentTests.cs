using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ion-segment's behaviours, from <c>segment.js</c>, each measured in Chromium: see <c>docs/components/ion-segment/PLAN.md</c>,
/// whose numbers these tests carry.
/// </summary>
public class AvonicSegmentTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────────────────────

    private static AvonicSegmentButton Button(string value, string label) => AvonicSegmentCaptureTests.Button(value, label);

    private static AvonicSegment Segment(string? value, params AvonicSegmentButton[] buttons)
    {
        var segment = AvonicSegmentCaptureTests.Segment(value ?? "", buttons);
        segment.Value = value;
        segment.VerticalAlignment = VerticalAlignment.Top;
        return segment;
    }

    private static AvonicSegment FriendsEnemies() => Segment("a", Button("a", "Friends"), Button("b", "Enemies"));

    private static List<object?> Changes(AvonicSegment segment)
    {
        var changes = new List<object?>();
        segment.IonChange += (_, e) => changes.Add(e.Value);
        return changes;
    }

    private static Rect Box(Visual visual, Visual to) => new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(to)!.Value);

    private static Point Centre(Visual visual, Scene scene) => Box(visual, scene.Window).Center;

    private static AvonicSegmentButton[] Buttons(AvonicSegment segment) => segment.Items.OfType<AvonicSegmentButton>().ToArray();

    private static Control Indicator(AvonicSegmentButton button) => button.GetVisualDescendants().OfType<Control>().First(c => c.Name == "PART_Indicator");

    private static void Render() => GestureTests.Render();

    private static void Down(Scene scene, Point at)
    {
        Render();
        scene.Window.MouseMove(at, RawInputModifiers.None);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Move(Scene scene, Point at)
    {
        scene.Window.MouseMove(at, RawInputModifiers.LeftMouseButton);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Up(Scene scene, Point at)
    {
        Render();
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Tap(Scene scene, Point at)
    {
        Down(scene, at);
        Up(scene, at);
    }

    private static void Key(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static Control? Focused(Scene scene) => scene.Window.FocusManager?.GetFocusedElement() as Control;

    // ── The segment ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S1_A_Button_Is_Checked_When_Its_Value_Is_The_Segments_And_Code_Raises_No_IonChange()
    {
        var segment = FriendsEnemies();
        var changes = Changes(segment);
        using var scene = new Scene(segment);
        var (a, b) = (Buttons(segment)[0], Buttons(segment)[1]);
        Assert.True(a.IsChecked);
        Assert.False(b.IsChecked);

        segment.Value = "b";
        Assert.False(a.IsChecked);
        Assert.True(b.IsChecked);

        b.Value = "c";
        Assert.False(b.IsChecked);
        a.Value = "b";
        Assert.True(a.IsChecked);

        // ===, not a conversion: the number 1 isn't the string "1".
        segment.Value = 1;
        a.Value = "1";
        Assert.False(a.IsChecked);
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void S2_A_Tap_Checks_The_Button_At_The_Release_With_One_IonChange()
    {
        using var clock = new TestClock();
        var segment = FriendsEnemies();
        var changes = Changes(segment);
        using var scene = new Scene(segment);
        var b = Buttons(segment)[1];

        Down(scene, Centre(b, scene));
        Assert.Equal("a", segment.Value);
        Up(scene, Centre(b, scene));

        Assert.Equal("b", segment.Value);
        Assert.Equal(["b"], changes);
    }

    [AvaloniaFact]
    public void S3_Dragging_The_Checked_Button_Moves_It_Past_Each_Edge_With_IonChange_Only_At_The_Release()
    {
        using var clock = new TestClock();
        var segment = FriendsEnemies();
        segment.Value = "b";
        var changes = Changes(segment);
        using var scene = new Scene(segment);
        var y = Centre(segment, scene).Y;

        Down(scene, new Point(292, y));
        Assert.Contains(":segment-activated", segment.Classes);
        foreach (var (x, expected) in new[] { (280.0, "b"), (250, "b"), (200, "b"), (196, "b"), (190, "a"), (150, "a"), (100, "a") })
        {
            Move(scene, new Point(x, y + 6));
            clock.At(clock.Now.TotalMilliseconds + 40);
            Assert.Equal(expected, segment.Value);
        }

        Assert.Empty(changes);
        Up(scene, new Point(100, y + 6));
        Assert.Equal(["a"], changes);
        Assert.DoesNotContain(":segment-activated", segment.Classes);
    }

    [AvaloniaFact]
    public void S3_A_Disabled_Neighbour_Stops_The_Drag_And_The_Release_Checks_Where_It_Ends()
    {
        using var clock = new TestClock();
        var two = Button("2", "Two");
        two.IsEnabled = false;
        var segment = Segment("1", Button("1", "One"), two, Button("3", "Three"));
        var changes = Changes(segment);
        using var scene = new Scene(segment);
        var y = Centre(segment, scene).Y;

        Down(scene, new Point(65, y));
        foreach (var x in new[] { 120.0, 140, 200, 262, 300 })
        {
            Move(scene, new Point(x, y));
            clock.At(clock.Now.TotalMilliseconds + 40);
            Assert.Equal("1", segment.Value);
        }

        Up(scene, new Point(300, y));
        Assert.Equal("3", segment.Value);
        Assert.Equal(["3"], changes);
    }

    [AvaloniaFact]
    public void S4_A_Drag_From_An_Unchecked_Button_Moves_Nothing_And_The_Release_Below_Checks_By_Its_X()
    {
        using var clock = new TestClock();
        var segment = FriendsEnemies();
        var changes = Changes(segment);
        using var scene = new Scene(new StackPanel { Children = { segment, new Border { Height = 200 } } });
        var y = Centre(segment, scene).Y;

        Down(scene, new Point(292, y));
        Move(scene, new Point(100, y));
        clock.At(40);
        Assert.Equal("a", segment.Value);
        clock.At(80);
        Move(scene, new Point(300, y + 60));
        Up(scene, new Point(300, y + 60));

        Assert.Equal("b", segment.Value);
        Assert.Equal(["b"], changes);
    }

    [AvaloniaFact]
    public void S5_A_Release_Beyond_The_End_Changes_Nothing()
    {
        using var clock = new TestClock();
        var segment = FriendsEnemies();
        segment.Width = 300;
        segment.HorizontalAlignment = HorizontalAlignment.Left;
        var changes = Changes(segment);
        using var scene = new Scene(segment);
        var y = Centre(segment, scene).Y;

        // From the unchecked button, released past the segment's end.
        Down(scene, new Point(220, y));
        clock.At(40);
        Move(scene, new Point(350, y + 100));
        Up(scene, new Point(350, y + 100));

        Assert.Equal("a", segment.Value);
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void S6_With_Nothing_Checked_The_Press_Checks_And_IonChange_Waits_For_The_Release()
    {
        using var clock = new TestClock();
        var segment = Segment(null, Button("a", "Friends"), Button("b", "Enemies"));
        var changes = Changes(segment);
        using var scene = new Scene(segment);
        var b = Buttons(segment)[1];

        Down(scene, Centre(b, scene));
        Assert.Equal("b", segment.Value);
        Assert.Empty(changes);
        Up(scene, Centre(b, scene));
        Assert.Equal(["b"], changes);
    }

    // ion-activatable, ion-activatable-instant and ion-focusable are behaviour at style priority, given before any style, so that
    // an app's plain style wins.
    [AvaloniaFact]
    public void S6_An_Apps_Style_For_Activatable_And_FocusVisible_Wins()
    {
        var button = new AvonicSegmentButton { Value = "a", Items = { new AvonicLabel { Content = "A" } } };
        using var scene = Scene.Styled(button, Scene.AppStyle<AvonicSegmentButton>(Activatable.IsEnabledProperty, false),
            Scene.AppStyle<AvonicSegmentButton>(Activatable.IsInstantProperty, false),
            Scene.AppStyle<AvonicSegmentButton>(FocusVisible.IsEnabledProperty, false));

        Assert.False(Activatable.GetIsEnabled(button));
        Assert.False(Activatable.GetIsInstant(button));
        Assert.False(FocusVisible.GetIsEnabled(button));
    }

    [AvaloniaFact]
    public void S7_A_Press_On_A_Disabled_Button_Does_Nothing()
    {
        using var clock = new TestClock();
        var two = Button("2", "Two");
        two.IsEnabled = false;
        var segment = Segment("1", Button("1", "One"), two, Button("3", "Three"));
        var changes = Changes(segment);
        using var scene = new Scene(segment);

        Tap(scene, Centre(two, scene));

        Assert.Equal("1", segment.Value);
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void S8_With_The_Gesture_Off_A_Click_Checks_With_The_Slide() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var segment = FriendsEnemies();
        segment.SegmentSwipeGesture = false;
        var changes = Changes(segment);
        using var scene = new Scene(segment);
        var (a, b) = (Buttons(segment)[0], Buttons(segment)[1]);

        // No gesture: a press on the checked button doesn't hold the indicator, and a release elsewhere is no click.
        Down(scene, Centre(a, scene));
        Move(scene, Centre(b, scene));
        clock.At(40);
        Up(scene, Centre(b, scene));
        Assert.Equal("a", segment.Value);

        // The click comes from the button, at the release.
        Down(scene, Centre(b, scene));
        Move(scene, Centre(b, scene) + new Point(5, 0));
        clock.At(80);
        Assert.Equal("a", segment.Value);
        Up(scene, Centre(b, scene) + new Point(5, 0));
        Assert.Equal("b", segment.Value);
        Assert.Equal(["b"], changes);

        clock.At(80 + 16);
        Assert.True(b.Slide!.IsRunning);
    });
}
