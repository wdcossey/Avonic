using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-breadcrumbs in Chromium, at 390 px wide, and the tests are named after the behaviours in
/// docs/components/ion-breadcrumbs/PLAN.md. Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub.
/// </summary>
public class AvonicBreadcrumbsTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color PrimaryShade = Color.Parse("#004ACD");
    private static readonly Color Danger = Color.Parse("#C5000F");
    private static readonly Color DangerShade = Color.Parse("#AD000D");
    private static readonly Color Plain = Color.Parse("#677483");
    private static readonly Color ActiveText = Color.Parse("#03060B");
    private static readonly Color FocusedText = Color.Parse("#35404E");
    private static readonly Color SeparatorText = Color.Parse("#73849A");

    private static AvonicBreadcrumb Crumb(string text, string? href = null) => AvonicBreadcrumbsCaptureTests.Crumb(text, href);

    private static AvonicBreadcrumbs Trail(params string[] texts) => AvonicBreadcrumbsCaptureTests.Trail(texts);

    private static AvonicBreadcrumbs Trail(params AvonicBreadcrumb[] breadcrumbs) => AvonicBreadcrumbsCaptureTests.Trail(breadcrumbs);

    private static List<AvonicBreadcrumb> Crumbs(AvonicBreadcrumbs trail) =>
        [.. trail.GetLogicalDescendantsOfType()];

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static Control Separator(AvonicBreadcrumb breadcrumb) => Part<Control>(breadcrumb, "PART_Separator");

    private static Button Indicator(AvonicBreadcrumb breadcrumb) => Part<Button>(breadcrumb, "PART_CollapsedIndicator");

    private static Border Native(AvonicBreadcrumb breadcrumb) => Part<Border>(breadcrumb, "PART_Native");

    /// <summary>
    /// What Ionic renders, as the measuring script printed it: the text, then A if active, I if the indicator shows, S if the
    /// separator does, H if the native part is hidden, and the separator prop.
    /// </summary>
    private static string State(AvonicBreadcrumbs trail)
    {
        Dispatcher.UIThread.RunJobs();
        return string.Join(" | ", Crumbs(trail).Select(breadcrumb =>
            $"{breadcrumb.Content}:{(breadcrumb.Active ? "A" : "")}{(Indicator(breadcrumb).IsVisible ? "I" : "")}" +
            $"{(Separator(breadcrumb).IsVisible ? "S" : "")}{(Native(breadcrumb).IsVisible ? "" : "H")}" +
            $"sep={breadcrumb.Separator?.ToString().ToLowerInvariant() ?? "undefined"}"));
    }

    private static Rect Bounds(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void AssertBox(double x, double y, double width, double height, Rect actual, double tolerance = 0.5)
    {
        Assert.InRange(actual.X, x - tolerance, x + tolerance);
        Assert.InRange(actual.Y, y - tolerance, y + tolerance);
        Assert.InRange(actual.Width, width - tolerance, width + tolerance);
        Assert.InRange(actual.Height, height - tolerance, height + tolerance);
    }

    private static Color Rgba(IBrush? brush)
    {
        if (brush is null)
        {
            return Colors.Transparent;
        }

        var solid = Assert.IsAssignableFrom<ISolidColorBrush>(brush);
        return Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B);
    }

    private static void AssertColor(Color expected, IBrush? brush, double alpha = 1)
    {
        var actual = Rgba(brush);
        Assert.Equal((expected.R, expected.G, expected.B), (actual.R, actual.G, actual.B));
        Assert.InRange(actual.A, alpha * 255 - 1, alpha * 255 + 1);
    }

    private static Scene Show(Control content)
    {
        var scene = new Scene(content);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();
        return scene;
    }

    private static void Hover(Scene scene, Control control)
    {
        scene.Window.MouseMove(Bounds(control, scene.Window).Center, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void PressKey(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Click(Scene scene, Point point)
    {
        scene.Window.MouseMove(point, RawInputModifiers.None);
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    // ── 1, 2: separators, and the last made active ───────────────────────────────────────────

    [AvaloniaFact]
    public void B1_B2_Every_One_But_The_Last_Is_Separated_And_The_Last_Is_Active()
    {
        var trail = Trail("A", "B", "C");
        using var scene = Show(trail);

        // initial: A:sep=true | B:sep=true | C:ALsep=undefined
        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Asep=undefined", State(trail));
    }

    [AvaloniaFact]
    public void B1_A_Separator_Set_On_A_Breadcrumb_Stays_And_The_Last_Never_Shows_One()
    {
        var a = Crumb("A");
        a.Separator = false;
        var c = Crumb("C");
        c.Separator = true;
        var trail = Trail(a, Crumb("B"), c);
        using var scene = Show(trail);

        // separator false/-/true: A:sep=false | B:sep=true | C:ALsep=true, shown [false, true, false]
        Assert.Equal("A:sep=false | B:Ssep=true | C:Asep=true", State(trail));
    }

    [AvaloniaFact]
    public void B2_An_Active_Breadcrumb_Keeps_The_Last_From_Being_Made_Active()
    {
        var b = Crumb("B");
        b.Active = true;
        var trail = Trail(Crumb("A"), b, Crumb("C"));
        using var scene = Show(trail);

        Assert.Equal("A:Ssep=true | B:ASsep=true | C:sep=undefined", State(trail));
    }

    [AvaloniaFact]
    public void B2_The_Breadcrumbs_Assigns_The_Props_So_A_Binding_Survives_And_A_Later_App_Value_Wins()
    {
        var c = Crumb("C");
        var source = new ObservableValue { Value = false };
        c.Bind(AvonicBreadcrumb.ActiveProperty, new Avalonia.Data.Binding(nameof(ObservableValue.Value)) { Source = source });
        var trail = Trail(Crumb("A"), c);
        using var scene = Show(trail);
        Assert.True(c.Active);

        source.Value = true;
        source.Value = false;

        Assert.False(c.Active);
    }

    private sealed class ObservableValue : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _value;

        public bool Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    // ── 3: changes ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Adding_And_Removing_Children_Moves_The_Last_And_The_Active()
    {
        var trail = Trail("A", "B", "C");
        using var scene = Show(trail);

        trail.Items.Add(Crumb("D"));
        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Ssep=true | D:Asep=undefined", State(trail));

        trail.Items.RemoveAt(3);
        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Asep=true", State(trail));
    }

    [AvaloniaFact]
    public void B3_A_Change_Undoes_The_First_Active_One_If_The_Breadcrumbs_Ever_Made_One_Active()
    {
        var trail = Trail("A", "B", "C");
        using var scene = Show(trail);
        var crumbs = Crumbs(trail);

        // B.active=true, then append E: C stays active, as Ionic undoes B, the first active one.
        crumbs[1].Active = true;
        trail.Items.Add(Crumb("E"));

        Assert.Equal("A:Ssep=true | B:Ssep=true | C:ASsep=true | E:sep=undefined", State(trail));
    }

    [AvaloniaFact]
    public void B3_Several_Changes_At_Once_Are_One_Change()
    {
        var trail = Trail("A", "B", "C");
        using var scene = Show(trail);

        // One slotchange for both: C is undone, and X is already active, so Y isn't made active. One at a time, X would be undone.
        var x = Crumb("X");
        x.Active = true;
        trail.Items.Add(x);
        trail.Items.Add(Crumb("Y"));

        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Ssep=true | X:ASsep=true | Y:sep=undefined", State(trail));
    }

    [AvaloniaFact]
    public void B3_A_Child_That_Is_No_Breadcrumb_Is_A_Change_Too()
    {
        var trail = Trail("A", "B", "C");
        using var scene = Show(trail);
        var crumbs = Crumbs(trail);

        // B set active as well as C, which the breadcrumbs chose: a slotchange undoes B, the first.
        crumbs[1].Active = true;
        trail.Items.Add(new TextBlock { Text = "Not a breadcrumb" });

        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Asep=undefined", State(trail));
    }

    [AvaloniaFact]
    public void B3_It_Is_Worked_Out_When_It_Is_Attached_Before_It_Is_Laid_Out()
    {
        var holder = new StackPanel();
        using var scene = Show(holder);
        var trail = Trail("A", "B");

        // componentWillLoad, before the first render: no layout or dispatcher job has run.
        holder.Children.Add(trail);
        var crumbs = Crumbs(trail);

        Assert.Equal(true, crumbs[0].Separator);
        Assert.True(crumbs[1].Active);
    }

    [AvaloniaFact]
    public void B2_The_Breadcrumbs_Value_Gives_Way_To_An_Apps_Style_Set_Later()
    {
        var trail = Trail("A", "B");
        trail.Styles.Add(new Style(x => x.OfType<AvonicBreadcrumb>().Class("off")) { Setters = { new Setter(AvonicBreadcrumb.ActiveProperty, false) } });
        using var scene = Show(trail);
        var b = Crumbs(trail)[1];
        Assert.True(b.Active);

        b.Classes.Add("off");

        Assert.False(b.Active);
    }

    [AvaloniaFact]
    public void B3_Changes_While_It_Is_Out_Of_The_Window_Still_Count()
    {
        var trail = Trail("A", "B", "C");
        var holder = new StackPanel { Children = { trail } };
        using var scene = Show(holder);

        holder.Children.Remove(trail);
        trail.Items.Add(Crumb("D"));
        Dispatcher.UIThread.RunJobs();
        holder.Children.Add(trail);

        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Ssep=true | D:Asep=undefined", State(trail));
    }

    [AvaloniaFact]
    public void B3_Showing_It_Makes_The_Last_Active_Once()
    {
        var trail = Trail("A", "B", "C");
        var c = Crumbs(trail)[2];
        var changes = new List<bool>();
        c.PropertyChanged += (_, e) =>
        {
            if (e.Property == AvonicBreadcrumb.ActiveProperty)
            {
                changes.Add(c.Active);
            }
        };

        using var scene = Show(trail);

        Assert.Equal([true], changes);
    }

    [AvaloniaFact]
    public void B3_Before_Anything_Is_Shown_The_Children_Are_Read_Once_As_Ionic_Loads_Them()
    {
        var active = Crumb("Active");
        active.Active = true;
        var trail = Trail(Crumb("A"), active, Crumb("Last"));
        using var scene = Show(trail);

        Assert.Equal("A:Ssep=true | Active:ASsep=true | Last:sep=undefined", State(trail));
    }

    // ── 4, 5: collapsing ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_More_Than_MaxItems_Collapse_The_Middle_Behind_The_Indicator()
    {
        var trail = Trail("A", "B", "C", "D", "E");
        using var scene = Show(trail);

        trail.MaxItems = 3;
        Assert.Equal("A:Ssep=true | B:ISHsep=true | C:Hsep=true | D:Hsep=true | E:Asep=undefined", State(trail));

        trail.ItemsBeforeCollapse = 2;
        Assert.Equal("A:Ssep=true | B:Ssep=true | C:ISHsep=true | D:Hsep=true | E:Asep=undefined", State(trail));

        // 3 + 1 > 3: nothing collapses.
        trail.ItemsBeforeCollapse = 3;
        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Ssep=true | D:Ssep=true | E:Asep=undefined", State(trail));
    }

    [AvaloniaFact]
    public void B4_No_MaxItems_Or_Few_Enough_Breadcrumbs_Collapse_Nothing()
    {
        var trail = Trail("A", "B", "C");
        using var scene = Show(trail);
        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Asep=undefined", State(trail));

        trail.MaxItems = 3;
        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Asep=undefined", State(trail));
    }

    [AvaloniaFact]
    public void B5_With_None_After_The_Indicator_Its_Breadcrumb_Is_The_Last()
    {
        var trail = Trail("A", "B", "C", "D", "E");
        trail.MaxItems = 3;
        using var scene = Show(trail);

        // before=1 after=0: A:sep=true | B:ACLIsep=true | C:Csep=true | D:Csep=true | E:Csep=true
        trail.ItemsAfterCollapse = 0;
        Assert.Equal("A:Ssep=true | B:AIHsep=true | C:Hsep=true | D:Hsep=true | E:Hsep=true", State(trail));

        // maxItems=undefined: E is last again.
        trail.MaxItems = null;
        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Ssep=true | D:Ssep=true | E:Asep=true", State(trail));

        // maxItems=10 after=0: B is last though nothing collapses, and E shows the separator it was given.
        trail.MaxItems = 10;
        Assert.Equal("A:Ssep=true | B:Asep=true | C:Ssep=true | D:Ssep=true | E:Ssep=true", State(trail));
    }

    [AvaloniaFact]
    public void B5_Zero_Everywhere_Collapses_All_Behind_The_First()
    {
        var trail = Trail("A", "B");
        trail.MaxItems = 0;
        trail.ItemsBeforeCollapse = 0;
        trail.ItemsAfterCollapse = 0;
        using var scene = Show(trail);

        // max=0 before=0 after=0: A:ACLIsep=undefined | B:Csep=true
        Assert.Equal("A:AIHsep=undefined | B:Hsep=true", State(trail));
    }

    // ── 6: anywhere inside ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Breadcrumbs_Inside_Another_Child_Count_In_Order()
    {
        var nested = Crumb("Nested");
        var trail = new AvonicBreadcrumbs();
        trail.Items.Add(new StackPanel { Children = { nested } });
        trail.Items.Add(Crumb("B"));
        using var scene = Show(trail);

        Assert.Equal(true, nested.Separator);
        Assert.False(nested.Active);
        Assert.True(((AvonicBreadcrumb)trail.Items[1]!).Active);
    }

    [AvaloniaFact]
    public void B6_Breadcrumbs_Made_From_An_ItemTemplate_Count()
    {
        var trail = new AvonicBreadcrumbs
        {
            ItemTemplate = new FuncDataTemplate<string>((text, _) => new AvonicBreadcrumb { Content = text }),
            ItemsSource = new ObservableCollection<string> { "A", "B", "C" },
        };
        using var scene = Show(trail);

        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Asep=undefined", State(trail));

        ((ObservableCollection<string>)trail.ItemsSource).Add("D");
        Assert.Equal("A:Ssep=true | B:Ssep=true | C:Ssep=true | D:Asep=undefined", State(trail));
    }

    // ── 7: the indicator's click ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_The_Indicator_Hands_Over_The_Collapsed_Breadcrumbs_And_Itself()
    {
        var trail = Trail("A", "B", "C", "D");
        trail.MaxItems = 2;
        using var scene = Show(trail);
        var crumbs = Crumbs(trail);
        var events = new List<BreadcrumbCollapsedClickEventArgs>();
        var bubbled = 0;
        trail.IonCollapsedClick += (_, e) => events.Add(e);
        scene.Window.AddHandler(AvonicBreadcrumbs.IonCollapsedClickEvent, (_, _) => bubbled++);

        Click(scene, Bounds(Indicator(crumbs[1]), scene.Window).Center);

        var e = Assert.Single(events);
        Assert.Same(trail, e.Source);
        Assert.Equal([crumbs[1], crumbs[2]], e.CollapsedBreadcrumbs);
        Assert.Same(Indicator(crumbs[1]), e.IonShadowTarget);
        Assert.Equal(1, bubbled);
    }

    [AvaloniaTheory]
    [InlineData(PhysicalKey.Enter)]
    [InlineData(PhysicalKey.Space)]
    public void B7_Enter_And_Space_On_The_Indicator_Click_It(PhysicalKey key)
    {
        var trail = Trail("A", "B", "C", "D");
        trail.MaxItems = 2;
        using var scene = Show(trail);
        var clicks = 0;
        trail.IonCollapsedClick += (_, _) => clicks++;

        Indicator(Crumbs(trail)[1]).Focus(NavigationMethod.Tab);
        PressKey(scene, key);

        Assert.Equal(1, clicks);
    }

    // ── 8, 9: the row ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_A_Row_That_Wraps_With_Each_Breadcrumb_Whole_And_Centred() => InstalledTheme.Run(_ =>
    {
        var trail = Trail("One", "Two", "A really long breadcrumb label");
        var holder = new StackPanel { Width = 200, HorizontalAlignment = HorizontalAlignment.Left, Children = { trail } };
        using var scene = Show(holder);
        var crumbs = Crumbs(trail);

        // wrap 200px: [0,0,79,36] [79,0,78,36] [0,36,239,36]. Text here measures a little differently. The long one's box is held
        // to the row's 200, as Avalonia's WrapPanel does, but what it draws doesn't shrink: its native part is 239, past the edge.
        AssertBox(0, 0, crumbs[0].Bounds.Width, 36, Bounds(crumbs[0], holder));
        Assert.Equal(0, Bounds(crumbs[1], holder).Y);
        AssertBox(0, 36, 239, 36, Bounds(Native(crumbs[2]), holder), 3);
    });

    [AvaloniaFact]
    public void B8_A_Shorter_Breadcrumb_Sits_In_The_Middle_Of_Its_Line() => InstalledTheme.Run(_ =>
    {
        var trail = Trail("A", "B", "C", "D");
        trail.MaxItems = 2;
        using var scene = Show(trail);

        // element 10: the indicator's breadcrumb is 23 tall, at y + 6.5 in a 36 row.
        var indicatorCrumb = Crumbs(trail)[1];
        Assert.Equal(23, indicatorCrumb.Bounds.Height, 0.5);
        Assert.Equal(6.5, Bounds(indicatorCrumb, trail).Y, 0.6);
    });

    [AvaloniaFact]
    public void B9_In_A_Toolbar_It_Has_Padding_And_In_A_Coloured_One_The_Contrast() => InstalledTheme.Run(_ =>
    {
        var plain = Trail("A", "B");
        var coloured = Trail("A", "B");
        var page = new StackPanel
        {
            Children = { new AvonicToolbar { Content = plain }, new AvonicToolbar { Color = "primary", Content = coloured } },
        };
        using var scene = Show(page);

        Assert.Equal(new Thickness(8, 0), plain.Padding);
        Assert.Equal(new Thickness(8, 0), coloured.Padding);
        AssertColor(Colors.White, coloured.Foreground);
        Assert.Equal(new Thickness(0), Trail("A").Padding);
    });

    // ── 10–14: a breadcrumb's look ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_The_Box_Is_36_Tall_With_A_Slash_10_Either_Side() => InstalledTheme.Run(_ =>
    {
        var trail = Trail("Home", "Electronics");
        using var scene = Show(trail);
        var home = Crumbs(trail)[0];
        var native = Native(home);
        var separator = Separator(home);

        Assert.Equal(16, home.FontSize);
        Assert.Equal(FontWeight.Normal, home.FontWeight);
        Assert.Equal(36, home.Bounds.Height, 0.5);
        Assert.Equal(new Thickness(12, 6), native.Padding);
        Assert.Equal(36, native.Bounds.Height, 0.5);

        // element 0: native 67.06 wide, separator at native + 10, 6.61 by 24, at y + 5.5.
        var nativeBox = Bounds(native, trail);
        var separatorBox = Bounds(separator, trail);
        Assert.Equal(nativeBox.Right + 10, separatorBox.X, 0.5);
        Assert.Equal(24, separatorBox.Height, 0.5);
        Assert.Equal(5.5, separatorBox.Y, 0.6);
        Assert.Equal(separatorBox.Right + 10, home.Bounds.Width, 0.5);
        Assert.Equal("/", separator.GetVisualDescendants().OfType<TextBlock>().Single(t => t.IsEffectivelyVisible).Text);
    });

    [AvaloniaFact]
    public void B10_Separator_Content_Replaces_The_Slash() => InstalledTheme.Run(_ =>
    {
        var arrow = new AvonicIcon { IconName = "arrow-forward" };
        var trail = Trail(new AvonicBreadcrumb { Content = "Custom", SeparatorContent = arrow }, Crumb("End"));
        using var scene = Show(trail);
        var separator = Separator(Crumbs(trail)[0]);

        Assert.True(arrow.IsEffectivelyVisible);
        Assert.DoesNotContain(separator.GetVisualDescendants().OfType<TextBlock>(), t => t.IsEffectivelyVisible);

        // element 26: an 18 by 18 separator at y + 8.5, which layout rounds at a scale of 1.
        AssertBox(Bounds(separator, trail).X, 8.5, 18, 18, Bounds(separator, trail), 1);
    });

    [AvaloniaFact]
    public void B10_Separator_Content_Replaces_The_Slash_In_The_Stub_Too()
    {
        var trail = Trail(new AvonicBreadcrumb { Content = "Custom", SeparatorContent = "to" }, Crumb("End"));
        using var scene = Show(trail);

        var shown = Separator(Crumbs(trail)[0]).GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible);
        Assert.Equal(["to"], shown.Select(t => t.Text));
    }

    [AvaloniaFact]
    public void B10_Each_Slot_Takes_Its_Template() => EachSlotTakesItsTemplate();

    [AvaloniaFact]
    public void B10_Each_Slot_Takes_Its_Template_In_The_Theme() => InstalledTheme.Run(_ => EachSlotTakesItsTemplate());

    private static void EachSlotTakesItsTemplate()
    {
        static FuncDataTemplate<string> Upper() => new((text, _) => new TextBlock { Text = text.ToUpperInvariant() });
        var breadcrumb = new AvonicBreadcrumb
        {
            StartContent = "s", StartContentTemplate = Upper(),
            Content = "c", ContentTemplate = Upper(),
            EndContent = "e", EndContentTemplate = Upper(),
            SeparatorContent = "x", SeparatorContentTemplate = Upper(),
        };
        var trail = Trail(breadcrumb, Crumb("End"));
        using var scene = Show(trail);

        var shown = breadcrumb.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible).Select(t => t.Text);
        Assert.Equal(["S", "C", "E", "X"], shown);
    }

    [AvaloniaFact]
    public void B11_B12_Collapsed_Only_The_Indicator_And_Its_Separator_Show() => InstalledTheme.Run(_ =>
    {
        var trail = Trail("Home", "Electronics", "Photography", "Cameras");
        trail.MaxItems = 2;
        using var scene = Show(trail);
        var crumbs = Crumbs(trail);

        // element 10: 86.61 by 23 at y + 6.5, the indicator 32 by 18 at + 14 and y + 9, which layout rounds at a scale of 1.
        AssertBox(Bounds(crumbs[1], trail).X, 6.5, 86.61, 23, Bounds(crumbs[1], trail), 1);
        AssertBox(Bounds(crumbs[1], trail).X + 14, 9, 32, 18, Bounds(Indicator(crumbs[1]), trail), 1);
        Assert.Equal(0, crumbs[2].Bounds.Width);
        Assert.False(Native(crumbs[1]).IsVisible);
    });

    [AvaloniaFact]
    public void B13_Active_Is_Color_Active_At_500() => InstalledTheme.Run(_ =>
    {
        var trail = Trail("Home", "Cameras");
        using var scene = Show(trail);
        var crumbs = Crumbs(trail);

        AssertColor(Plain, crumbs[0].Foreground);
        AssertColor(ActiveText, crumbs[1].Foreground);
        Assert.Equal(FontWeight.Medium, crumbs[1].FontWeight);
        AssertColor(SeparatorText, Separator(crumbs[0]).GetValue(TextElement.ForegroundProperty));
    });

    [AvaloniaFact]
    public void B13_The_Variables_Beat_The_Md_Values() => InstalledTheme.Run(_ =>
    {
        var trail = Trail("Home", "Cameras");
        trail.Resources["Avonic.Breadcrumb.Color"] = new SolidColorBrush(Colors.Green);
        trail.Resources["Avonic.Breadcrumb.ColorActive"] = new SolidColorBrush(Colors.Orange);
        using var scene = Show(trail);
        var crumbs = Crumbs(trail);

        AssertColor(Colors.Green, crumbs[0].Foreground);
        AssertColor(Colors.Orange, crumbs[1].Foreground);
    });

    [AvaloniaFact]
    public void B14_Colours_Follow_Ionic_Order() => InstalledTheme.Run(_ =>
    {
        var danger = new AvonicBreadcrumb { Content = "Danger", Color = "danger" };
        var dangerActive = new AvonicBreadcrumb { Content = "Danger active", Color = "danger", Active = true };
        var plainTrail = Trail(danger, dangerActive, Crumb("Last"));

        var inPrimary = Trail("Home", "Primary");
        inPrimary.Color = "primary";

        var inToolbar = Trail("T", "U");
        var successInToolbar = Trail(Crumb("V"), new AvonicBreadcrumb { Content = "W", Color = "success" });
        successInToolbar.Color = "danger";

        var page = new StackPanel
        {
            Children =
            {
                plainTrail,
                inPrimary,
                new AvonicToolbar { Color = "primary", Content = inToolbar },
                new AvonicToolbar { Color = "primary", Content = successInToolbar },
            },
        };
        using var scene = Show(page);

        AssertColor(Danger, danger.Foreground);
        AssertColor(Danger, dangerActive.Foreground);

        // In a coloured breadcrumbs: the base, the separator too, and active as well.
        AssertColor(Primary, Crumbs(inPrimary)[0].Foreground);
        AssertColor(Primary, Separator(Crumbs(inPrimary)[0]).GetValue(TextElement.ForegroundProperty));
        AssertColor(Primary, Crumbs(inPrimary)[1].Foreground);

        // In a coloured toolbar: the contrast at 0.8, separator too; active, the contrast. hover u: white, w: success's contrast.
        AssertColor(Colors.White, Crumbs(inToolbar)[0].Foreground, 0.8);
        AssertColor(Colors.White, Separator(Crumbs(inToolbar)[0]).GetValue(TextElement.ForegroundProperty), 0.8);
        AssertColor(Colors.White, Crumbs(inToolbar)[1].Foreground);
        AssertColor(Colors.White, Crumbs(successInToolbar)[0].Foreground, 0.8);
        AssertColor(Colors.Black, Crumbs(successInToolbar)[1].Foreground);
    });

    [AvaloniaFact]
    public void B14_The_Context_Is_Read_When_The_Breadcrumb_Renders_Not_When_The_Breadcrumbs_Changes() => InstalledTheme.Run(_ =>
    {
        var trail = Trail("A", "B");
        using var scene = Show(trail);
        var a = Crumbs(trail)[0];

        trail.Color = "primary";
        Dispatcher.UIThread.RunJobs();
        AssertColor(Plain, a.Foreground);

        // Its next render, from a prop of its own.
        a.Href = "#a";
        Dispatcher.UIThread.RunJobs();
        AssertColor(Primary, a.Foreground);
    });

    [AvaloniaFact]
    public void B14_A_Prop_Of_Its_Own_Renders_It_Again_And_Reads_A_Toolbar_Coloured_Since() => InstalledTheme.Run(_ =>
    {
        var trail = Trail("A", "B");
        var toolbar = new AvonicToolbar { Content = trail };
        using var scene = Show(toolbar);
        var a = Crumbs(trail)[0];

        toolbar.Color = "primary";
        Dispatcher.UIThread.RunJobs();
        AssertColor(Plain, a.Foreground);

        // Its own colour: in-toolbar-color comes after ion-color, so the contrast at 0.8.
        a.Color = "danger";
        Dispatcher.UIThread.RunJobs();
        AssertColor(Colors.White, a.Foreground, 0.8);
    });

    // ── 15, 16: a link ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_A_Link_Is_A_Tab_Stop_And_A_Span_Is_Not()
    {
        var before = new Button { Content = "x" };
        var a = Crumb("A", "#a");
        var span = Crumb("Span");
        var disabled = Crumb("Dis", "#c");
        disabled.IsEnabled = false;
        var d = Crumb("D", "#d");
        var trail = Trail(a, span, disabled, d);
        trail.MaxItems = 2;
        using var scene = Show(new StackPanel { Children = { before, trail } });
        before.Focus();

        // tab: A, then the indicator in Span, then D. Dis is collapsed and disabled.
        PressKey(scene, PhysicalKey.Tab);
        Assert.True(a.IsFocused);
        PressKey(scene, PhysicalKey.Tab);
        Assert.True(Indicator(span).IsFocused);
        PressKey(scene, PhysicalKey.Tab);
        Assert.True(d.IsFocused);
    }

    [AvaloniaFact]
    public void B15_A_Collapsed_Link_Is_No_Tab_Stop()
    {
        var hidden = Crumb("Hidden", "#h");
        var trail = Trail(Crumb("A"), Crumb("B"), hidden, Crumb("D"));
        trail.MaxItems = 2;
        using var scene = Show(trail);

        Assert.False(hidden.Focusable);
        trail.MaxItems = null;
        Assert.True(hidden.Focusable);
    }

    [AvaloniaFact]
    public void B15_A_Focused_Link_That_Collapses_Loses_Focus()
    {
        var hidden = Crumb("Hidden", "#h");
        var trail = Trail(Crumb("A"), Crumb("B"), hidden, Crumb("D"));
        using var scene = Show(trail);
        var blurs = 0;
        hidden.IonBlur += (_, _) => blurs++;
        hidden.Focus(NavigationMethod.Tab);
        Assert.True(hidden.IsFocused);

        trail.MaxItems = 2;

        Assert.False(hidden.IsFocused);
        Assert.Equal(1, blurs);
    }

    [AvaloniaFact]
    public void B15_Enter_Clicks_A_Link_And_Space_Does_Not()
    {
        var a = Crumb("A", "#a");
        var trail = Trail(a, Crumb("B"));
        using var scene = Show(trail);
        var clicks = 0;
        a.Click += (_, _) => clicks++;
        a.Focus(NavigationMethod.Tab);

        PressKey(scene, PhysicalKey.Space);
        Assert.Equal(0, clicks);
        PressKey(scene, PhysicalKey.Enter);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B15_Enter_Does_Nothing_On_A_Span()
    {
        var span = Crumb("Span");
        span.Focusable = true;
        var trail = Trail(span, Crumb("B"));
        using var scene = Show(trail);
        var clicks = 0;
        span.Click += (_, _) => clicks++;
        span.Focus(NavigationMethod.Tab);

        PressKey(scene, PhysicalKey.Enter);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B15_Hovering_A_Link_Gives_Color_Hover_And_The_Shade_Over_A_Colour() => InstalledTheme.Run(_ =>
    {
        var link = Crumb("Link", "#a");
        var span = Crumb("Span");
        var danger = new AvonicBreadcrumb { Content = "Danger", Href = "#x", Color = "danger" };
        var inPrimaryLink = Crumb("P", "#p");
        var inPrimary = Trail(inPrimaryLink, Crumb("Q"));
        inPrimary.Color = "primary";
        var inToolbarLink = Crumb("T", "#t");
        var page = new StackPanel
        {
            Children =
            {
                Trail(link, span, danger, Crumb("Last")),
                inPrimary,
                new AvonicToolbar { Color = "primary", Content = Trail(inToolbarLink, Crumb("U")) },
            },
        };
        using var scene = Show(page);

        Hover(scene, link);
        AssertColor(ActiveText, link.Foreground);
        Hover(scene, span);
        AssertColor(Plain, span.Foreground);
        Hover(scene, danger);
        AssertColor(DangerShade, danger.Foreground);
        Hover(scene, inPrimaryLink);
        AssertColor(PrimaryShade, inPrimaryLink.Foreground);

        // hover t: rgba(255, 255, 255, 0.8) -> rgb(3, 6, 11)
        Hover(scene, inToolbarLink);
        AssertColor(ActiveText, inToolbarLink.Foreground);
    });

    [AvaloniaFact]
    public void B16_Focused_By_Keyboard_Shows_Color_Background_Radius_And_Shadow() => InstalledTheme.Run(_ =>
    {
        var link = Crumb("Focused", "#f");
        var trail = Trail(link, Crumb("Last"));
        using var scene = Show(trail);
        var native = Native(link);
        Assert.Equal(default, native.CornerRadius);

        link.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();

        // element 4: rgb(53, 64, 78), white, 4px, the two shadows.
        AssertColor(FocusedText, link.Foreground);
        AssertColor(Colors.White, native.Background);
        Assert.Equal(new CornerRadius(4), native.CornerRadius);
        Assert.Equal(2, native.BoxShadow.Count);

        // overflow: visible, so the shadow isn't cut off by the breadcrumb or the row.
        Assert.False(link.ClipToBounds);
        Assert.False(trail.ClipToBounds);

        // A press anywhere ends it, as focus-visible's pointerdown does.
        Click(scene, new Point(380, 830));
        AssertColor(Plain, link.Foreground);
        Assert.Equal(default, native.CornerRadius);
    });

    // outline: none, on the link and on the indicator. The adorner comes from the app's base theme, which TestApp doesn't load, so
    // this adds it underneath, as the sample does. FocusAdorner reads null either way, so only the layer shows what's drawn.
    [AvaloniaFact]
    public void B16_B20_There_Is_No_Focus_Adorner_Over_A_Base_Theme()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var link = Crumb("A", "#a");
                var trail = Trail(link, Crumb("B"), Crumb("C"), Crumb("D"));
                trail.MaxItems = 3;
                var other = new Button { Content = "x" };
                using var scene = Show(new StackPanel { Children = { trail, other } });
                var layer = Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(link)!;

                PressKey(scene, PhysicalKey.Tab);
                Assert.True(link.IsFocused);
                Assert.Empty(layer.Children);
                PressKey(scene, PhysicalKey.Tab);
                Assert.True(Indicator(Crumbs(trail)[1]).IsFocused);
                Assert.Empty(layer.Children);

                // Proves the base theme really does give a focused control one.
                PressKey(scene, PhysicalKey.Tab);
                Assert.True(other.IsFocused);
                Assert.Single(layer.Children);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    [AvaloniaFact]
    public void B17_IonFocus_And_IonBlur_Follow_The_Breadcrumb_Not_Its_Indicator()
    {
        var a = Crumb("A", "#a");
        var b = Crumb("B", "#b");
        var trail = Trail(a, b, Crumb("C"), Crumb("D"));
        trail.MaxItems = 3;
        using var scene = Show(trail);
        var log = new List<string>();
        foreach (var crumb in Crumbs(trail))
        {
            crumb.IonFocus += (s, _) => log.Add("focus " + ((AvonicBreadcrumb)s!).Content);
            crumb.IonBlur += (s, _) => log.Add("blur " + ((AvonicBreadcrumb)s!).Content);
        }

        a.Focus(NavigationMethod.Tab);
        Indicator(b).Focus(NavigationMethod.Tab);

        Assert.Equal(["focus A", "blur A"], log);
    }

    // ── 18: disabled ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_Disabled_Is_Half_Opacity_With_No_Input_And_No_Focus() => InstalledTheme.Run(_ =>
    {
        var disabled = Crumb("Dis", "#d");
        disabled.IsEnabled = false;
        var trail = Trail(disabled, Crumb("Last"));
        using var scene = Show(trail);
        var clicks = 0;
        disabled.Click += (_, _) => clicks++;

        Assert.Equal(0.5, disabled.Opacity);
        Assert.True(Separator(disabled).IsEffectivelyVisible);
        Assert.False(disabled.Focus(NavigationMethod.Tab));
        Click(scene, Bounds(disabled, scene.Window).Center);
        Assert.Equal(0, clicks);
    });

    // ── 19: slotted icons ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_Slotted_Icons_Are_18_In_Their_Colour_With_8_Beside_The_Text() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "home" };
        var end = new AvonicIcon { IconName = "folder" };
        var activeIcon = new AvonicIcon { IconName = "document" };
        var trail = Trail(
            new AvonicBreadcrumb { StartContent = start, Content = "Home" },
            new AvonicBreadcrumb { Content = "Files", EndContent = end },
            new AvonicBreadcrumb { StartContent = activeIcon, Content = "Report" });
        using var scene = Show(trail);

        Assert.Equal(18, start.Bounds.Height);
        Assert.Equal(new Thickness(0, 0, 8, 0), start.Margin);
        Assert.Equal(new Thickness(8, 0, 0, 0), end.Margin);
        AssertColor(Color.Parse("#7D8894"), start.Foreground);
        AssertColor(Color.Parse("#222D3A"), activeIcon.Foreground);
    });

    [AvaloniaFact]
    public void B19_An_Apps_Plain_Style_Beats_The_Slotted_Margin() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "home" };
        var trail = Trail(new AvonicBreadcrumb { StartContent = start, Content = "Home" });
        trail.Styles.Add(new Style(x => x.OfType<AvonicIcon>()) { Setters = { new Setter(Layoutable.MarginProperty, new Thickness(3)) } });
        using var scene = Show(trail);

        Assert.Equal(new Thickness(3), start.Margin);
    });

    // ── 20: the indicator ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B20_The_Indicator_Look_Hover_And_Focus() => InstalledTheme.Run(_ =>
    {
        var trail = Trail("A", "B", "C", "D");
        trail.MaxItems = 2;
        using var scene = Show(trail);
        var indicator = Indicator(Crumbs(trail)[1]);
        var icon = indicator.GetVisualDescendants().OfType<AvonicIcon>().Single();

        Assert.Equal(new CornerRadius(2), indicator.CornerRadius);
        Assert.Equal(new Thickness(14, 0), indicator.Margin);
        AssertColor(Color.Parse("#EEF1F3"), indicator.Background);
        AssertColor(Color.Parse("#73849A"), icon.Foreground);
        Assert.Equal("ellipsis-horizontal", icon.IconName);
        Assert.Equal(22, icon.Bounds.Height);
        Assert.Equal(22, icon.FontSize);
        Assert.NotNull(indicator.Cursor);

        Hover(scene, indicator);
        Assert.Equal(0.7, indicator.Opacity);
        scene.Window.MouseMove(new Point(380, 830), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, indicator.Opacity);

        // :focus, by any means.
        indicator.Focus(NavigationMethod.Pointer);
        Dispatcher.UIThread.RunJobs();
        AssertColor(Color.Parse("#DFE5E8"), indicator.Background);
    });

    // ── Extension: Click and Command ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void E1_A_Press_Clicks_Any_Breadcrumb_Link_Or_Not()
    {
        var span = Crumb("Span");
        var link = Crumb("Link", "#l");
        var trail = Trail(span, link);
        using var scene = Show(trail);
        var clicked = new List<object?>();
        span.Click += (_, _) => clicked.Add(span.Content);
        link.Click += (_, _) => clicked.Add(link.Content);

        Click(scene, Bounds(span, scene.Window).Center);
        Click(scene, Bounds(link, scene.Window).Center);
        Click(scene, Bounds(link, scene.Window).Center);

        Assert.Equal(["Span", "Link", "Link"], clicked);
    }

    [AvaloniaFact]
    public void E1_The_Indicator_Clicks_Its_Breadcrumb_Once_After_IonCollapsedClick()
    {
        var trail = Trail("A", "B", "C", "D");
        trail.MaxItems = 2;
        using var scene = Show(trail);
        var b = Crumbs(trail)[1];
        var log = new List<string>();
        trail.IonCollapsedClick += (_, _) => log.Add("ionCollapsedClick");
        b.Click += (_, _) => log.Add("click");

        Click(scene, Bounds(Indicator(b), scene.Window).Center);
        Indicator(b).Focus(NavigationMethod.Tab);
        PressKey(scene, PhysicalKey.Enter);

        Assert.Equal(["ionCollapsedClick", "click", "ionCollapsedClick", "click"], log);
    }

    [AvaloniaFact]
    public void E1_A_Key_On_The_Indicator_Leaves_Nothing_Behind_For_The_Next_Click()
    {
        var trail = Trail("A", "B", "C", "D");
        trail.MaxItems = 2;
        using var scene = Show(trail);
        var b = Crumbs(trail)[1];
        b.Href = "#b";
        var clicks = 0;
        b.Click += (_, _) => clicks++;

        Indicator(b).Focus(NavigationMethod.Tab);
        PressKey(scene, PhysicalKey.Space);
        Assert.Equal(1, clicks);

        // Expanded, B is a link, and a screen reader's activation clicks it.
        trail.MaxItems = null;
        ControlAutomationPeer.CreatePeerForElement(b).GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void E1_A_Press_On_The_Indicator_That_Loses_Its_Capture_Leaves_Keys_Working()
    {
        var trail = Trail("A", "B", "C", "D");
        trail.MaxItems = 2;
        using var scene = Show(trail);
        var b = Crumbs(trail)[1];
        var indicator = Indicator(b);
        var clicks = 0;
        b.Click += (_, _) => clicks++;

        // A gesture takes the pointer mid-press: no release comes.
        var finger = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        var centre = Bounds(indicator, scene.Window).Center;
        indicator.RaiseEvent(new PointerPressedEventArgs(indicator, finger, scene.Window, centre, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        finger.Capture(indicator);
        finger.Capture(scene.Window);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, clicks);

        indicator.Focus(NavigationMethod.Tab);
        PressKey(scene, PhysicalKey.Enter);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void E1_A_Finger_That_Moves_Too_Far_On_The_Indicator_Clicks_Nothing()
    {
        var trail = Trail("A", "B", "C", "D");
        trail.MaxItems = 2;
        using var scene = Show(trail);
        var b = Crumbs(trail)[1];
        var indicator = Indicator(b);
        var log = new List<string>();
        trail.IonCollapsedClick += (_, _) => log.Add("ionCollapsedClick");
        b.Click += (_, _) => log.Add("click");

        // 20 px across the 32 px button, past Chromium's 15, and released still on it: the button would click, Chromium doesn't.
        var finger = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        var left = Bounds(indicator, scene.Window).Center - new Point(10, 0);
        var right = left + new Point(20, 0);
        var properties = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed);
        indicator.RaiseEvent(new PointerPressedEventArgs(indicator, finger, scene.Window, left, 0, properties, KeyModifiers.None));
        indicator.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, indicator, finger, scene.Window, right, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
        indicator.RaiseEvent(new PointerReleasedEventArgs(indicator, finger, scene.Window, right, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(log);
    }

    [AvaloniaFact]
    public void E1_Command_Runs_Unless_Click_Is_Handled_And_One_That_Cannot_Execute_Disables()
    {
        var link = Crumb("Link", "#l");
        var runs = 0;
        var canExecute = true;
        var command = new TestCommand(() => runs++, () => canExecute);
        link.Command = command;
        link.CommandParameter = "p";
        var trail = Trail(link, Crumb("Last"));
        using var scene = Show(trail);

        Click(scene, Bounds(link, scene.Window).Center);
        Assert.Equal(1, runs);
        Assert.Equal("p", command.Parameter);

        EventHandler<RoutedEventArgs> handle = (_, e) => e.Handled = true;
        link.Click += handle;
        Click(scene, Bounds(link, scene.Window).Center);
        Assert.Equal(1, runs);
        Assert.Equal("p", command.Parameter);
        link.Click -= handle;

        canExecute = false;
        command.Raise();
        Assert.False(link.IsEffectivelyEnabled);
    }

    private sealed class TestCommand(Action execute, Func<bool> canExecute) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute();

        public object? Parameter { get; private set; }

        public void Execute(object? parameter)
        {
            Parameter = parameter;
            execute();
        }

        public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Accessibility ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A1_A_Link_Is_A_Named_Hyperlink_That_Invokes_And_A_Span_Has_No_Role()
    {
        var link = Crumb("Electronics", "#e");
        var span = Crumb("Cameras");
        var trail = Trail(link, span);
        using var scene = Show(trail);
        var clicks = 0;
        link.Click += (_, _) => clicks++;

        var peer = ControlAutomationPeer.CreatePeerForElement(link);
        Assert.Equal(AutomationControlType.Hyperlink, peer.GetAutomationControlType());
        Assert.Equal("Electronics", peer.GetName());
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(1, clicks);

        // A disabled one has its href taken away: no link.
        link.IsEnabled = false;
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());

        var spanPeer = ControlAutomationPeer.CreatePeerForElement(span);
        Assert.Equal(AutomationControlType.Group, spanPeer.GetAutomationControlType());
        Assert.False(spanPeer.IsContentElement());
        Assert.Equal(AutomationControlType.Group, ControlAutomationPeer.CreatePeerForElement(trail).GetAutomationControlType());
    }

    [AvaloniaFact]
    public void A2_The_Separator_Is_Hidden_And_The_Indicator_Is_A_Named_Button()
    {
        var trail = Trail("A", "B", "C", "D");
        trail.MaxItems = 2;
        using var scene = Show(trail);
        var crumbs = Crumbs(trail);

        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(Separator(crumbs[0])));
        var indicator = ControlAutomationPeer.CreatePeerForElement(Indicator(crumbs[1]));
        Assert.Equal(AutomationControlType.Button, indicator.GetAutomationControlType());
        Assert.Equal("Show more breadcrumbs", indicator.GetName());
    }
}

file static class BreadcrumbTestExtensions
{
    public static IEnumerable<AvonicBreadcrumb> GetLogicalDescendantsOfType(this AvonicBreadcrumbs trail) =>
        Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(trail).OfType<AvonicBreadcrumb>();
}
