using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Themes;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-split-pane</c>, md: B1 to B19 in docs/components/ion-split-pane/PLAN.md, from <c>split-pane.js</c>, <c>menu.js</c>,
/// <c>split-pane.md.css</c>, <c>menu.md.css</c> and the measurements in <c>ref/measurements.txt</c> (S1 to S23). A window holds a
/// split pane with a menu, then the main content named <c>Main</c>, as Ionic's examples put them.
/// </summary>
public class AvonicSplitPaneTests
{
    /// <summary>A split pane with a menu and the main content, in a window with <c>SimpleTheme</c> and <c>AvonicTheme</c>.</summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Avalonia.Themes.Simple.SimpleTheme _simple = new();
        private readonly AvonicTheme _theme = new();

        public Stage(double width = 390, double height = 844, Action<Stage>? set = null, bool attach = true)
        {
            Application.Current!.Styles.Insert(0, _simple);
            Application.Current.Styles.Add(_theme);
            Clock = new TestClock();
            Main = new Border { Name = "Main", Background = Brushes.White, Child = new TextBlock { Text = "Main content" } };
            Menu = new AvonicMenu { ContentId = "Main", Content = new TextBlock { Text = "Menu content" } };
            Pane = new AvonicSplitPane { ContentId = "Main", Items = { Menu, Main } };
            Root = new Panel { Children = { Pane } };
            Scope = new NameScope();
            NameScope.SetNameScope(Root, Scope);
            Scope.Register("Main", Main);
            Pane.IonSplitPaneVisible += (_, e) => Log.Add($"ionSplitPaneVisible({e.Visible})");
            Menu.IonDidClose += (_, e) => Log.Add($"ionDidClose({e.Role})");
            set?.Invoke(this);
            Scene = new Scene(attach ? new Panel { Children = { Root } } : new Panel(), width, height);
            Settle();
        }

        public TestClock Clock { get; }

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public Panel Root { get; }

        public NameScope Scope { get; }

        public AvonicSplitPane Pane { get; }

        public AvonicMenu Menu { get; }

        public Border Main { get; }

        public List<string> Log { get; } = [];

        public SplitPanePanel Panel => (SplitPanePanel)Pane.ItemsPanelRoot!;

        public void Attach()
        {
            ((Panel)Window.Content!).Children.Add(Root);
            Settle();
        }

        public void Detach()
        {
            ((Panel)Window.Content!).Children.Remove(Root);
            Settle();
        }

        public void Resize(double width, double height = double.NaN)
        {
            Window.Width = width;
            if (!double.IsNaN(height))
            {
                Window.Height = height;
            }

            Settle();
        }

        /// <summary>A control's box in the window.</summary>
        public Rect Box(Visual element) => new Rect(element.Bounds.Size).TransformToAABB(element.TransformToVisual(Window)!.Value);

        public static void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            Dispatcher.UIThread.RunJobs();
        }

        public void At(double ms)
        {
            while (Clock.Now.TotalMilliseconds < ms)
            {
                Clock.At(Math.Min(Math.Floor(Clock.Now.TotalMilliseconds) + 1, ms));
                Settle();
            }

            Settle();
        }

        public void Render()
        {
            for (var i = 0; i < 2; i++)
            {
                Settle();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            Settle();
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
            Application.Current!.Styles.Remove(_theme);
            Application.Current.Styles.Remove(_simple);
        }
    }

    private static void Near(double expected, double actual, double tolerance = 0.01) =>
        Assert.True(Math.Abs(expected - actual) <= tolerance, $"expected {expected}, was {actual}");

    private static void Box(Rect actual, double x, double width, double tolerance = 0.01)
    {
        Near(x, actual.X, tolerance);
        Near(width, actual.Width, tolerance);
    }

    // ── B1: the default breakpoint and the row ───────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_By_Default_It_Is_Hidden_On_A_Phone_And_The_Menu_Is_An_Ordinary_One()
    {
        // S1: at 390 the menu is display: none, the main fills the pane.
        using var stage = new Stage();
        Assert.DoesNotContain(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
        Assert.False(stage.Menu.IsVisible);
        Assert.True(stage.Menu.IsActive());
        Assert.Contains(AvonicMenu.SplitPaneSide, stage.Menu.Classes);
        Assert.DoesNotContain(AvonicMenu.MenuPaneVisible, stage.Menu.Classes);
        Box(stage.Box(stage.Main), 0, 390);
    }

    [AvaloniaFact]
    public void B1_At_1024_The_Menu_Is_Beside_The_Main_Content()
    {
        // S2: the menu 28% of 1024, 286.72, its container 285.72, then the main from 286.72. Avalonia rounds a layout to the
        // device's pixels, here whole ones: 287, the menu 286 and its border at 286.
        using var stage = new Stage(1024, 768);
        Assert.Contains(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
        Assert.Contains(AvonicMenu.MenuPaneVisible, stage.Menu.Classes);
        Assert.True(stage.Menu.IsVisible);
        Assert.Equal(0, stage.Menu.ZIndex);
        Box(stage.Box(stage.Menu), 0, 286);
        Box(stage.Box(stage.Main), 287, 737);
        Assert.Equal(new Rect(286, 0, 1, 768), stage.Panel.BorderRects().Single());

        // .menu-inner fills the menu, with no transform: left: 0; right: 0; width: auto; transform: none.
        var container = stage.Menu.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Container");
        Box(stage.Box(container), 0, 286);
        Assert.Equal(768, stage.Box(container).Height, 2);
    }

    [AvaloniaFact]
    public void B1_The_Default_When_Is_Ionics_Query()
    {
        Assert.Equal("(min-width: 992px)", new AvonicSplitPane().When.Query);
        Assert.False(new AvonicSplitPane().When.IsBoolean);
    }

    // ── B2: the event ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_IonSplitPaneVisible_Comes_When_The_State_Changes_And_Only_Then()
    {
        // S5, md: shown at 768 with one event, nothing at 800, hidden at 500 with one event.
        using var stage = new Stage(767, 600, s => s.Pane.When = "md");
        Assert.Empty(stage.Log);

        // The log listens on the split pane, which hears it before the menu does, on the top level.
        stage.Resize(768);
        Assert.Equal(["ionSplitPaneVisible(True)", "ionDidClose(gesture)"], stage.Log);

        stage.Resize(800);
        Assert.Equal(2, stage.Log.Count);

        stage.Resize(500);
        Assert.Equal(["ionSplitPaneVisible(True)", "ionDidClose(gesture)", "ionSplitPaneVisible(False)"], stage.Log);
    }

    [AvaloniaFact]
    public void B2_It_Comes_At_The_First_Attach_When_It_Shows()
    {
        // S2: Stencil's watcher is ready before connectedCallback, so the state set there raises it.
        using var stage = new Stage(1024, 768, attach: false);
        var heard = new List<bool>();
        stage.Pane.IonSplitPaneVisible += (_, e) => heard.Add(e.Visible);
        stage.Attach();
        Assert.Equal([true], heard);
    }

    [AvaloniaFact]
    public void B2_It_Bubbles()
    {
        using var stage = new Stage(1024, 768, attach: false);
        var heard = 0;
        stage.Root.AddHandler(AvonicSplitPane.IonSplitPaneVisibleEvent, (_, _) => heard++);
        stage.Attach();
        Assert.Equal(1, heard);
    }

    // ── B3: shortcuts ────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData("xs", 0)]
    [InlineData("sm", 576)]
    [InlineData("md", 768)]
    [InlineData("lg", 992)]
    [InlineData("xl", 1200)]
    public void B3_A_Shortcut_Shows_It_From_Its_Breakpoint(string when, double from)
    {
        using var stage = new Stage(Math.Max(1, from - 1), 600, s => s.Pane.When = when);
        Assert.Equal(from == 0, stage.Pane.Classes.Contains(AvonicSplitPane.SplitPaneVisible));
        stage.Resize(from + 1);
        Assert.Contains(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
        stage.Resize(from == 0 ? 2 : from);
        Assert.Contains(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
    }

    [AvaloniaFact]
    public void B3_Never_Hides_It()
    {
        using var stage = new Stage(1400, 768, s => s.Pane.When = "never");
        Assert.DoesNotContain(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
    }

    // ── B4: media queries ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_A_Media_Query_Follows_The_Window()
    {
        // S6: visible at 600 x 800 (portrait), hidden at 800 x 600 (landscape).
        using var stage = new Stage(600, 800, s => s.Pane.When = "(min-width: 500px) and (orientation: portrait)");
        Assert.Contains(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
        stage.Resize(800, 600);
        Assert.DoesNotContain(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
        Assert.Equal(["ionSplitPaneVisible(True)", "ionSplitPaneVisible(False)"], stage.Log.Where(l => l.StartsWith("ionSplit", StringComparison.Ordinal)));
    }

    /// <summary>Every query of ref/media-queries.txt that Avonic reads, with Chromium's answer at 600 x 800.</summary>
    [Theory]
    [InlineData("(min-width: 500px)", true)]
    [InlineData("(max-width: 599px)", false)]
    [InlineData("(max-width: 600px)", true)]
    [InlineData("(min-width: 600px)", true)]
    [InlineData("(min-width: 600.5px)", false)]
    [InlineData("(width: 600px)", true)]
    [InlineData("screen and (min-width: 500px)", true)]
    [InlineData("print", false)]
    [InlineData("not print", true)]
    [InlineData("not (min-width: 700px)", true)]
    [InlineData("not screen and (min-width: 700px)", true)]
    [InlineData("(min-width: 900px), (min-width: 500px)", true)]
    [InlineData("only screen and (orientation: portrait)", true)]
    [InlineData("(orientation: landscape)", false)]
    [InlineData("(width >= 600px)", true)]
    [InlineData("(width > 600px)", false)]
    [InlineData("(400px < width < 700px)", true)]
    [InlineData("(700px > width)", true)]
    [InlineData("(width < 600px)", false)]
    [InlineData("(600px < width)", false)]
    [InlineData("(width <= 600px)", true)]
    [InlineData("(600px <= width < 601px)", true)]
    [InlineData("(width > 599px)", true)]
    [InlineData("(width >= 600.5px)", false)]
    [InlineData("(width = 600px)", true)]
    [InlineData("(min-aspect-ratio: 1/1)", false)]
    [InlineData("(max-aspect-ratio: 1/1)", true)]
    [InlineData("(aspect-ratio: 3/4)", true)]
    [InlineData("(min-width: 30em)", true)]
    [InlineData("(min-width: 37.5em)", true)]
    [InlineData("(min-width: 37.6em)", false)]
    [InlineData("(min-width: 37.5rem)", true)]
    [InlineData("(prefers-color-scheme: dark)", false)]
    [InlineData("(hover: hover)", false)]
    [InlineData("(foo: bar)", false)]
    [InlineData("never", false)]
    [InlineData("true", false)]
    [InlineData("garbage(((", false)]
    [InlineData("(min-width)", false)]
    [InlineData("(width)", true)]
    [InlineData("all", true)]
    [InlineData("", true)]
    [InlineData("(min-height: 800px)", true)]
    [InlineData("(min-height: 801px)", false)]
    [InlineData("not all and (min-width: 700px)", true)]
    [InlineData("(min-width: 500px) or (max-width: 100px)", true)]
    [InlineData("(min-width: 700px) and (max-width: 100px)", false)]
    [InlineData("((min-width: 500px))", true)]
    [InlineData("not ((min-width: 700px) or (max-width: 100px))", true)]
    [InlineData("(min-width: 0)", true)]
    [InlineData("(min-width: 0px)", true)]
    [InlineData("(min-width: 500)", false)]
    [InlineData("SCREEN AND (MIN-WIDTH: 500PX)", true)]
    [InlineData("  (min-width:500px)  ", true)]
    [InlineData("(min-width: 500px) and (foo: bar)", false)]
    [InlineData("(min-width: 500px), (foo: bar)", true)]
    [InlineData("not (foo: bar)", false)]
    [InlineData("(min-width: 10cm)", true)]
    [InlineData("tv", false)]
    [InlineData("speech", false)]
    [InlineData("not tv", true)]
    public void B4_A_Media_Query_Matches_As_Chromiums_Does(string query, bool chromium) =>
        Assert.Equal(chromium, MediaQuery.Matches(query, new Size(600, 800)));

    /// <summary>The rows of ref/media-queries.txt measured at 992 x 800: brackets that don't parse, and exponents.</summary>
    [Theory]
    [InlineData("(min-width: 500px) or (hover hover)", true)]
    [InlineData("(hover hover)", false)]
    [InlineData("not (hover hover)", false)]
    [InlineData("(min-width: 9.92e2px)", true)]
    [InlineData("(min-width: 5e2px)", true)]
    [InlineData("(min-width) or (min-width: 500px)", true)]
    [InlineData("(min-width: 500px), (min-width)", true)]
    [InlineData("(width < )", false)]
    [InlineData("(foo) or (min-width: 500px)", true)]
    [InlineData("(min-width: 500px) and (hover hover)", false)]
    public void B4_A_Bracket_That_Doesnt_Parse_Is_Unknown_As_In_Chromium(string query, bool chromium) =>
        Assert.Equal(chromium, MediaQuery.Matches(query, new Size(992, 800)));

    /// <summary>The rows of ref/media-queries.txt it doesn't read as Chromium does: features of the device, and calc().</summary>
    [Theory]
    [InlineData("(min-width: calc(400px + 100px))")]
    [InlineData("(min-device-width: 100px)")]
    [InlineData("(min-resolution: 2dppx)")]
    [InlineData("(-webkit-min-device-pixel-ratio: 2)")]
    [InlineData("(display-mode: browser)")]
    public void B4_Gaps_Chromium_Matches_These_And_Avonic_Doesnt(string query) =>
        Assert.False(MediaQuery.Matches(query, new Size(600, 800)));

    // ── B5: when ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_When_Takes_A_Boolean_And_Strings()
    {
        // S8: true, false, never, xs, '', garbage.
        using var stage = new Stage();
        bool Visible() => stage.Pane.Classes.Contains(AvonicSplitPane.SplitPaneVisible);

        stage.Pane.When = true;
        Assert.True(Visible());
        stage.Pane.When = false;
        Assert.False(Visible());
        stage.Pane.When = "never";
        Assert.False(Visible());
        stage.Pane.When = "xs";
        Assert.True(Visible());
        stage.Pane.When = "";
        Assert.False(Visible());
        stage.Pane.When = "xs";
        stage.Pane.When = (string?)null;
        Assert.False(Visible());
        stage.Pane.When = "garbage(((";
        Assert.False(Visible());
    }

    [Fact]
    public void B5_Xaml_True_And_False_Are_Booleans_And_Any_Other_Text_A_Query()
    {
        var converter = new SplitPaneWhenConverter();
        Assert.Equal(new SplitPaneWhen(true), converter.ConvertFrom(null, null, "True"));
        Assert.Equal(new SplitPaneWhen(false), converter.ConvertFrom(null, null, " false "));
        Assert.Equal(new SplitPaneWhen("md"), converter.ConvertFrom(null, null, "md"));
        Assert.True(((SplitPaneWhen)converter.ConvertFrom(null, null, "TRUE")!).IsBoolean);
    }

    [AvaloniaFact]
    public void B5_A_Change_Of_When_Stops_Following_The_Old_Query()
    {
        using var stage = new Stage(390, 844, s => s.Pane.When = "(min-width: 500px)");
        stage.Pane.When = false;
        stage.Resize(600);
        Assert.DoesNotContain(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
        Assert.Empty(stage.Log);
    }

    // ── B6: disabled ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Disabled_Hides_It_And_Enabling_Reads_The_Window_Again()
    {
        using var stage = new Stage(1024, 768);
        stage.Log.Clear();
        stage.Pane.Disabled = true;
        Assert.DoesNotContain(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
        Assert.True(stage.Menu.IsActive());
        Assert.Equal(["ionSplitPaneVisible(False)"], stage.Log);

        // Its children stay enabled, where IsEnabled false would disable the app.
        Assert.True(stage.Main.IsEffectivelyEnabled);

        // A resize while disabled changes nothing.
        stage.Resize(1100);
        Assert.DoesNotContain(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);

        stage.Pane.Disabled = false;
        Assert.Contains(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
        Assert.False(stage.Menu.IsActive());
    }

    // ── B7: the main content ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_ContentId_Marks_The_Main_Content_When_It_Is_Attached()
    {
        using var stage = new Stage(attach: false);
        Assert.DoesNotContain(AvonicSplitPane.SplitPaneMain, stage.Main.Classes);
        stage.Attach();
        Assert.Contains(AvonicSplitPane.SplitPaneMain, stage.Main.Classes);
        Assert.DoesNotContain(AvonicSplitPane.SplitPaneMain, stage.Menu.Classes);
    }

    [AvaloniaFact]
    public void B7_The_Mark_Moves_When_It_Is_Attached_Again_With_Another_ContentId()
    {
        // S20: Ionic leaves the old main marked, so two share the room; one main is what it means.
        var other = new Border { Name = "Other" };
        using var stage = new Stage(390, 844, s =>
        {
            s.Pane.When = "xs";
            s.Pane.Items.Add(other);
        });
        stage.Pane.ContentId = "Other";
        Assert.Contains(AvonicSplitPane.SplitPaneMain, stage.Main.Classes);
        stage.Detach();
        stage.Attach();
        Assert.DoesNotContain(AvonicSplitPane.SplitPaneMain, stage.Main.Classes);
        Assert.Contains(AvonicSplitPane.SplitPaneMain, other.Classes);
    }

    [AvaloniaFact]
    public void B7_A_Main_Removed_From_The_Items_Loses_Its_Mark()
    {
        // So that a menu in it, used elsewhere, isn't taken for a menu in a main content.
        using var stage = new Stage(1024, 768);
        Assert.Contains(AvonicSplitPane.SplitPaneMain, stage.Main.Classes);
        Assert.True(stage.Main.ClipToBounds);
        stage.Pane.Items.Remove(stage.Main);
        Stage.Settle();
        Assert.DoesNotContain(AvonicSplitPane.SplitPaneMain, stage.Main.Classes);
        Assert.False(stage.Main.ClipToBounds);
    }

    [AvaloniaFact]
    public void B7_No_Main_Found_Warns()
    {
        var sink = new WarningSink();
        var old = Logger.Sink;
        Logger.Sink = sink;
        try
        {
            using var stage = new Stage(390, 844, s => s.Pane.ContentId = "Nothing");
            Assert.Contains(sink.Messages, m => m.Contains("Does not have a specified main node", StringComparison.Ordinal));
        }
        finally
        {
            Logger.Sink = old;
        }
    }

    private sealed class WarningSink : ILogSink
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate) => Messages.Add(messageTemplate);

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Messages.Add(messageTemplate);
    }

    // ── B8: the side's width ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_At_390_The_Minimum_Beats_28_Percent()
    {
        // S3: 270 (28% is 109.2), its container 269; the main 120 from 270.
        using var stage = new Stage(390, 844, s => s.Pane.When = "xs");
        Box(stage.Box(stage.Menu), 0, 269);
        Box(stage.Box(stage.Main), 270, 120);
    }

    [AvaloniaTheory]
    [InlineData(992, 277.76)]
    [InlineData(1200, 336)]
    [InlineData(1400, 392)]
    public void B8_Wider_It_Is_28_Percent(double width, double side)
    {
        // S13: 277.75 (Chromium's layout units), 336, 392; rounded to whole pixels here.
        using var stage = new Stage(width, 768);
        Box(stage.Box(stage.Main), Math.Round(side), width - Math.Round(side));
    }

    [AvaloniaFact]
    public void B8_The_Keys_Set_The_Width_The_Maximum_Then_The_Minimum()
    {
        // S12: 200px within 100px and 50% of 390: 195. 40% within 0 and 100%: 156.
        using var stage = new Stage(390, 844, s => s.Pane.When = "xs");
        stage.Pane.Resources["Avonic.SplitPane.SideWidth"] = 200d;
        stage.Pane.Resources["Avonic.SplitPane.SideMinWidth"] = 100d;
        stage.Pane.Resources["Avonic.SplitPane.SideMaxWidth"] = "50%";
        Stage.Settle();
        Box(stage.Box(stage.Main), 195, 195);

        // The default 100%, with no limits: the whole row.
        stage.Pane.Resources["Avonic.SplitPane.SideMinWidth"] = 0d;
        stage.Pane.Resources["Avonic.SplitPane.SideMaxWidth"] = "100%";
        stage.Pane.Resources.Remove("Avonic.SplitPane.SideWidth");
        Stage.Settle();
        Box(stage.Box(stage.Main), 390, 0);

        stage.Pane.Resources["Avonic.SplitPane.SideWidth"] = "40%";
        stage.Pane.Resources["Avonic.SplitPane.SideMinWidth"] = 0d;
        stage.Pane.Resources["Avonic.SplitPane.SideMaxWidth"] = "100%";
        Stage.Settle();
        Box(stage.Box(stage.Main), 156, 234);
    }

    [AvaloniaFact]
    public void B8_With_No_Side_Width_It_Is_The_Menus_Own()
    {
        // S12c: var(side-width, var(width)) with side-width unset: the menu's 304.
        using var stage = new Stage(390, 844, s => s.Pane.When = "xs");
        stage.Pane.Resources["Avonic.SplitPane.SideWidth"] = new Avonic.Components.Behaviors.CssUnset();
        stage.Pane.Resources["Avonic.SplitPane.SideMinWidth"] = 0d;
        stage.Pane.Resources["Avonic.SplitPane.SideMaxWidth"] = "100%";
        Stage.Settle();
        Box(stage.Box(stage.Main), 304, 86);
    }

    [AvaloniaFact]
    public void B8_A_Side_Width_Of_Auto_Is_The_Sides_Own()
    {
        // var(side-width, var(width)) with side-width: auto: the side's content width, here the menu's text.
        using var stage = new Stage(390, 844, s => s.Pane.When = "xs");
        stage.Pane.Resources["Avonic.SplitPane.SideWidth"] = "auto";
        stage.Pane.Resources["Avonic.SplitPane.SideMinWidth"] = 0d;
        stage.Pane.Resources["Avonic.SplitPane.SideMaxWidth"] = "100%";
        Stage.Settle();
        Assert.InRange(stage.Box(stage.Main).X, 40, 200);
    }

    // ── B9: the border ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_The_Border_Is_One_Pixel_Inside_The_Side_At_Its_Inline_End()
    {
        using var stage = new Stage(390, 844, s => s.Pane.When = "xs");
        var line = Assert.Single(stage.Panel.BorderRects());
        Assert.Equal(new Rect(269, 0, 1, 844), line);
        stage.Render();

        // rgba(0, 0, 0, 0.13) over the white window: 222.
        using var frame = stage.Window.CaptureRenderedFrame()!;
        var pixel = Pixel(frame, 269, 400);
        Assert.InRange(pixel.R, 218, 226);
        Assert.Equal(255, Pixel(frame, 271, 400).R);
    }

    [AvaloniaFact]
    public void B9_The_Border_Colour_And_Width_Are_Keys()
    {
        using var stage = new Stage(390, 844, s => s.Pane.When = "xs");
        stage.Pane.Resources["Avonic.SplitPane.BorderWidth"] = 4d;
        stage.Pane.Resources["Avonic.SplitPane.BorderColor"] = Brushes.Red;
        Stage.Settle();
        Assert.Equal(new Rect(266, 0, 4, 844), Assert.Single(stage.Panel.BorderRects()));
        Box(stage.Box(stage.Menu), 0, 266);
        Assert.Equal(Brushes.Red, stage.Panel.BorderBrush);
    }

    [AvaloniaFact]
    public void B9_A_Sides_Margin_Leaves_The_Border_At_The_Edge_Of_Its_Room()
    {
        using var stage = new Stage(390, 844, s =>
        {
            s.Pane.When = "xs";
            s.Menu.Margin = new Thickness(0, 0, 8, 0);
        });
        Assert.Equal(new Rect(269, 0, 1, 844), Assert.Single(stage.Panel.BorderRects()));
    }

    [AvaloniaFact]
    public void B9_In_Right_To_Left_The_Start_Side_Is_At_The_Right_With_Its_Border_At_Its_Left()
    {
        // S11: the menu 120 to 390, the border at 120, the main 0 to 120.
        using var stage = new Stage(390, 844, s =>
        {
            s.Pane.When = "xs";
            s.Root.FlowDirection = FlowDirection.RightToLeft;
        });
        Box(stage.Box(stage.Menu), 121, 269);
        Box(stage.Box(stage.Main), 0, 120);
        stage.Render();
        using var frame = stage.Window.CaptureRenderedFrame()!;
        Assert.InRange(Pixel(frame, 120, 400).R, 218, 226);
        Assert.Equal(255, Pixel(frame, 119, 400).R);
    }

    private static Color Pixel(Avalonia.Media.Imaging.WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        unsafe
        {
            var p = (byte*)buffer.Address + (y * buffer.RowBytes) + (x * 4);
            return Color.FromArgb(p[3], p[2], p[1], p[0]);
        }
    }

    // ── B10: end sides ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_An_End_Side_Is_After_The_Main_Content_With_Its_Border_At_Its_Start()
    {
        // S4: the main 0 to 120, the menu 120 to 390, the border at 120, the container from 121.
        using var stage = new Stage(390, 844, s =>
        {
            s.Pane.When = "xs";
            s.Menu.Side = MenuSide.End;
        });
        Box(stage.Box(stage.Main), 0, 120);
        Box(stage.Box(stage.Menu), 121, 269);
        Assert.Equal(new Rect(120, 0, 1, 844), Assert.Single(stage.Panel.BorderRects()));
    }

    [AvaloniaFact]
    public void B10_Two_Sides_Go_Either_Side_Of_The_Main_Content_Whatever_Their_Order()
    {
        // S17, at 1024: 286.72 each, the main 450.56 between them. Here the end menu comes first in the items.
        var end = new AvonicMenu { ContentId = "Main", Side = MenuSide.End, MenuId = "end" };
        using var stage = new Stage(1024, 768, s => s.Pane.Items.Insert(0, end));
        Box(stage.Box(stage.Menu), 0, 286);
        Box(stage.Box(stage.Main), 287, 450);
        Box(stage.Box(end), 738, 286);
    }

    // ── B11: the menu in the pane isn't active ───────────────────────────────────────────────

    [AvaloniaFact]
    public async Task B11_An_Open_Menu_Closes_When_The_Pane_Shows_And_Cant_Open_There()
    {
        // S9: ionDidClose(gesture), then the event; isOpen() false, isActive() false, open() false; active again at 390.
        using var stage = new Stage(390, 844, s => s.Pane.When = "(min-width: 600px)");
        var opened = stage.Menu.Open(false);
        Stage.Settle();
        Assert.True(opened.IsCompleted && opened.Result);
        Assert.True(stage.Menu.IsOpen());
        stage.Log.Clear();

        // An app listening on the top level, as Ionic's test did on document, hears the event after the menu has closed (S9).
        var closedFirst = false;
        stage.Window.AddHandler(AvonicSplitPane.IonSplitPaneVisibleEvent, (_, _) => closedFirst = stage.Log.Contains("ionDidClose(gesture)"));
        stage.Resize(700);
        Assert.Equal(["ionSplitPaneVisible(True)", "ionDidClose(gesture)"], stage.Log);
        Assert.True(closedFirst);
        Assert.False(stage.Menu.IsOpen());
        Assert.False(stage.Menu.IsActive());
        Assert.True(stage.Menu.IsVisible);
        Assert.DoesNotContain(AvonicMenu.ShowMenu, stage.Menu.Classes);
        Assert.True(stage.Main.IsHitTestVisible);
        Assert.False(await stage.Menu.Open(false));
        Assert.False(await MenuController.Toggle());

        stage.Resize(390);
        Assert.True(stage.Menu.IsActive());
        Assert.False(stage.Menu.IsOpen());
        Assert.False(stage.Menu.IsVisible);
    }

    [AvaloniaFact]
    public void B11_The_Swipe_Is_Off_In_The_Pane_And_On_Again_Below_It()
    {
        using var stage = new Stage(1024, 768);
        stage.Menu.IonWillOpen += (_, _) => stage.Log.Add("ionWillOpen");
        stage.Log.Clear();
        Swipe(stage, 10, 210, 1000);
        stage.At(1500);
        Assert.Empty(stage.Log);

        stage.Resize(390);
        stage.Log.Clear();
        Swipe(stage, 10, 210, 2000);
        Assert.Contains("ionWillOpen", stage.Log);
    }

    /// <summary>A finger from <paramref name="x"/> to <paramref name="to"/> at 400 high, 8 px every 16 ms from <paramref name="at"/>.</summary>
    private static void Swipe(Stage stage, double x, double to, double at)
    {
        stage.Render();
        stage.At(at);
        var finger = new Gestures.GestureTests.Finger(stage.Scene, stage.Clock).Down(x, 400, at);
        while (x < to)
        {
            x += 8;
            at += 16;
            finger.Move(x, 400, at);
            stage.At(at + 16);
        }

        finger.Up(x, 400, at + 16);
        Stage.Settle();
    }

    // ── B12: a disabled menu ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_A_Disabled_Menu_Stays_Hidden_And_The_Main_Fills_The_Pane()
    {
        // S10: display: none; the main 390.
        using var stage = new Stage(390, 844, s =>
        {
            s.Pane.When = "xs";
            s.Menu.IsEnabled = false;
        });
        Assert.False(stage.Menu.IsVisible);
        Box(stage.Box(stage.Main), 0, 390);
        Assert.Empty(stage.Panel.BorderRects());

        stage.Menu.IsEnabled = true;
        Stage.Settle();
        Assert.True(stage.Menu.IsVisible);
        Box(stage.Box(stage.Main), 270, 120);
    }

    // ── B13: a menu in the main content ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_A_Menu_Inside_The_Main_Content_Is_An_Ordinary_Menu()
    {
        // S16: no split-pane-side, still active, and the pane's row is the side and the main.
        var inner = new AvonicMenu { ContentId = "Inner", MenuId = "inner" };
        var innerContent = new Border { Name = "Inner" };
        using var stage = new Stage(390, 844, s =>
        {
            s.Pane.When = "xs";
            s.Main.Child = new Panel { Children = { inner, innerContent } };
            s.Scope.Register("Inner", innerContent);
        });
        Assert.DoesNotContain(AvonicMenu.SplitPaneSide, inner.Classes);
        Assert.DoesNotContain(AvonicMenu.MenuPaneVisible, inner.Classes);
        Assert.True(inner.IsActive());
        Assert.Contains(AvonicMenu.SplitPaneSide, stage.Menu.Classes);
        Assert.False(stage.Menu.IsActive());
    }

    [AvaloniaFact]
    public void B13_A_Split_Pane_That_Is_Another_Ones_Main_Content_Has_Its_Own_Side()
    {
        // getSplitPane: a split pane can be the main content of an outer one, so only this one's main content counts.
        var innerMenu = new AvonicMenu { ContentId = "InnerMain", MenuId = "innerSide" };
        var innerMain = new Border { Name = "InnerMain" };
        var innerPane = new AvonicSplitPane { Name = "Nested", ContentId = "InnerMain", When = true, Items = { innerMenu, innerMain } };
        using var stage = new Stage(1024, 768, s =>
        {
            s.Pane.Items.Remove(s.Main);
            s.Pane.Items.Add(innerPane);
            s.Pane.ContentId = "Nested";
            s.Scope.Register("Nested", innerPane);
            s.Scope.Register("InnerMain", innerMain);
            s.Menu.ContentId = "Nested";
        });
        Assert.Contains(AvonicMenu.SplitPaneSide, innerMenu.Classes);
        Assert.Contains(AvonicMenu.MenuPaneVisible, innerMenu.Classes);
        Assert.Contains(innerMenu, innerPane.Sides);
        Assert.DoesNotContain(innerMenu, stage.Pane.Sides);
    }

    [AvaloniaFact]
    public void B13_A_Menu_Takes_The_State_Of_Its_Own_Split_Pane_Only()
    {
        // onSplitPaneChanged: splitPane === ev.target. Another split pane in the window showing changes nothing for this menu.
        var otherMenu = new AvonicMenu { ContentId = "OtherMain", MenuId = "other" };
        var otherMain = new Border { Name = "OtherMain" };
        var other = new AvonicSplitPane { ContentId = "OtherMain", When = false, Items = { otherMenu, otherMain } };
        using var stage = new Stage(390, 844, s =>
        {
            s.Root.Children.Add(other);
            s.Scope.Register("OtherMain", otherMain);
        });
        stage.Pane.When = true;
        Assert.False(stage.Menu.IsActive());
        Assert.True(otherMenu.IsActive());
        Assert.DoesNotContain(AvonicMenu.MenuPaneVisible, otherMenu.Classes);
    }

    // ── B14: menu buttons and toggles ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_A_Menu_Button_And_A_Toggle_Hide_While_The_Pane_Shows()
    {
        // S2, S19: hidden with the pane, the toggle with AutoHide off stays; both back below it.
        var button = new AvonicMenuButton();
        var toggle = new AvonicMenuToggle { Content = new Button { Content = "Toggle" } };
        var kept = new AvonicMenuToggle { AutoHide = false, Content = new Button { Content = "Kept" } };
        using var stage = new Stage(390, 844, s =>
        {
            s.Pane.When = "(min-width: 600px)";
            s.Main.Child = new StackPanel { Children = { button, toggle, kept } };
        });
        Assert.True(button.IsVisible);
        Assert.True(toggle.IsVisible);

        stage.Resize(700);
        Assert.False(button.IsVisible);
        Assert.False(toggle.IsVisible);
        Assert.True(kept.IsVisible);

        stage.Resize(390);
        Assert.True(button.IsVisible);
        Assert.True(toggle.IsVisible);
    }

    [AvaloniaFact]
    public void B14_A_Menu_Button_Attached_With_The_Pane_Already_Shown_Is_Hidden()
    {
        var button = new AvonicMenuButton();
        using var stage = new Stage(1024, 768, s => s.Main.Child = button);
        Assert.False(button.IsVisible);
    }

    // ── B15: the main content clips ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_The_Main_Content_Clips_While_The_Pane_Shows()
    {
        // overflow: hidden on .split-pane-main while visible. A Border doesn't clip by itself.
        using var stage = new Stage(390, 844, s => s.Pane.When = "(min-width: 600px)");
        Assert.False(stage.Main.ClipToBounds);
        stage.Resize(700);
        Assert.True(stage.Main.ClipToBounds);
        stage.Resize(390);
        Assert.False(stage.Main.ClipToBounds);
    }

    [AvaloniaFact]
    public void B15_An_Apps_Own_Style_For_The_Main_Content_Wins()
    {
        // An app's rule beats ::slotted: the split pane's value is bound before the main is styled, so the app's style wins.
        var app = new Avalonia.Styling.Style(x => Avalonia.Styling.Selectors.Name(Avalonia.Styling.Selectors.OfType(x, typeof(Border)), "Main"))
        {
            Setters = { new Avalonia.Styling.Setter(Visual.ClipToBoundsProperty, false) },
        };
        Application.Current!.Styles.Add(app);
        try
        {
            using var stage = new Stage(1024, 768);
            Assert.Contains(AvonicSplitPane.SplitPaneVisible, stage.Pane.Classes);
            Assert.False(stage.Main.ClipToBounds);
        }
        finally
        {
            Application.Current.Styles.Remove(app);
        }
    }

    // ── B16: other children ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B16_Another_Child_Sits_In_The_Row_At_Its_Own_Width()
    {
        // S15: a plain div after the menu at its content's width, the main what's left.
        var other = new Border { Width = 40 };
        using var stage = new Stage(390, 844, s =>
        {
            s.Pane.When = "xs";
            s.Pane.Items.Insert(1, other);
        });
        Box(stage.Box(other), 270, 40);
        Box(stage.Box(stage.Main), 310, 80);

        // Only the main content clips.
        Assert.True(stage.Main.ClipToBounds);
        Assert.False(other.ClipToBounds);

        // Hidden, every child fills the pane.
        stage.Pane.When = false;
        Stage.Settle();
        Box(stage.Box(stage.Main), 0, 390);
    }

    [AvaloniaFact]
    public void B16_Another_Child_Shrinks_To_The_Room_Left()
    {
        // flex: 0 1 auto: a wrapping text that would be wider than the row shrinks to the 120 the side leaves.
        var other = new TextBlock { Text = string.Join(' ', Enumerable.Repeat("words", 40)), TextWrapping = TextWrapping.Wrap };
        using var stage = new Stage(390, 844, s =>
        {
            s.Pane.When = "xs";
            s.Pane.Items.Insert(1, other);
        });
        Assert.InRange(stage.Box(other).Width, 1, 120);
        Assert.InRange(stage.Box(stage.Main).X, 270, 390);
    }

    // ── B17: a wrapped menu ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_A_Menu_Wrapped_In_Another_Control_Is_Still_The_Side()
    {
        // menu.md.css keeps the side's rules on the menu so that a menu wrapped in a component gets them.
        using var stage = new Stage(390, 844, s =>
        {
            s.Pane.When = "xs";
            s.Pane.Items.Remove(s.Menu);
            s.Pane.Items.Insert(0, new UserControl { Content = s.Menu });
        });
        Assert.Contains(AvonicMenu.SplitPaneSide, stage.Menu.Classes);
        Assert.False(stage.Menu.IsActive());
        Box(stage.Box(stage.Menu), 0, 269);
        Box(stage.Box(stage.Main), 270, 120);
    }

    // ── B18: attached again ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_A_Menu_Attached_Again_Reads_The_Pane_Again()
    {
        // Detached while the pane hides, the menu hears nothing; at its next attach it reads the state.
        using var stage = new Stage(1024, 768);
        var host = (Panel)stage.Window.Content!;
        stage.Pane.Items.Remove(stage.Menu);
        Stage.Settle();
        stage.Resize(390);
        stage.Pane.Items.Insert(0, stage.Menu);
        Stage.Settle();
        Assert.True(stage.Menu.IsActive());
        Assert.False(stage.Menu.IsVisible);

        stage.Pane.Items.Remove(stage.Menu);
        Stage.Settle();
        stage.Resize(1024);
        stage.Pane.Items.Insert(0, stage.Menu);
        Stage.Settle();
        Assert.False(stage.Menu.IsActive());
        Assert.True(stage.Menu.IsVisible);
        Assert.NotNull(host);
    }

    [AvaloniaFact]
    public void B18_A_Detached_Split_Pane_Stops_Following_The_Window()
    {
        // disconnectedCallback removes the listener: a resize while detached raises nothing, and the next attach reads the window.
        using var stage = new Stage(700, 844, s => s.Pane.When = "(min-width: 600px)");
        stage.Log.Clear();
        stage.Detach();
        stage.Resize(390);
        Assert.Empty(stage.Log);
        stage.Attach();
        Assert.Equal(["ionSplitPaneVisible(False)"], stage.Log);
    }

    [AvaloniaFact]
    public void B18_A_Menu_With_No_Content_Found_Still_Shows_In_The_Pane_When_Attached_Again()
    {
        // render: menu-pane-visible and split-pane-side show it whether or not connectedCallback found its content.
        using var stage = new Stage(390, 844, s => s.Pane.When = "(min-width: 600px)");
        stage.Menu.ContentId = "Nothing";
        stage.Pane.Items.Remove(stage.Menu);
        Stage.Settle();
        stage.Resize(700);
        stage.Pane.Items.Insert(0, stage.Menu);
        Stage.Settle();
        Assert.True(stage.Menu.IsVisible);
        Assert.Equal(270, stage.Box(stage.Main).X, 1);
    }

    // ── B19: automation ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_It_Has_No_Role_Of_Its_Own()
    {
        using var stage = new Stage(1024, 768);
        Assert.IsType<NoneAutomationPeer>(ControlAutomationPeer.CreatePeerForElement(stage.Pane));
    }
}
