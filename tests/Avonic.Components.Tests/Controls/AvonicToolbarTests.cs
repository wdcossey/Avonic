using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicToolbarCaptureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>Helpers shared by the toolbar family's tests.</summary>
internal static class ToolbarTest
{
    public static readonly Color Toolbar = Color.Parse("#424242");
    public static readonly Color Primary = Color.Parse("#0054E9");
    public static readonly Color Danger = Color.Parse("#C5000F");

    public static Color Solid(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    public static Rect Box(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    public static void Near(double expected, double actual, double tolerance = 0.5) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);

    public static T Part<T>(TemplatedControl control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

    public static Scene Show(Control content)
    {
        var scene = new Scene(new StackPanel { Children = { content } });
        Dispatcher.UIThread.RunJobs();
        return scene;
    }

    public static void Render(Scene scene)
    {
        Dispatcher.UIThread.RunJobs();
        scene.Window.CaptureRenderedFrame();
    }

    /// <summary>An app's own rule, such as <c>c|AvonicTitle:size-large { Opacity: 0.25 }</c>.</summary>
    public static Style AppStyle<T>(string pseudoClass, AvaloniaProperty property, object value) where T : Control =>
        new(selector => selector.OfType<T>().Class(pseudoClass)) { Setters = { new Setter(property, value) } };

    /// <summary>Ionic's dark palette merged into the app, as an app does, while <paramref name="test"/> runs.</summary>
    public static void InDark(Action test)
    {
        var app = Application.Current!;
        var dark = new ResourceInclude(new Uri("avares://Avonic.Themes/"))
        {
            Source = new Uri("avares://Avonic.Themes/Tokens/Palettes/Dark.axaml"),
        };
        app.Resources.MergedDictionaries.Add(dark);
        try
        {
            Dispatcher.UIThread.RunJobs();
            test();
        }
        finally
        {
            app.Resources.MergedDictionaries.Remove(dark);
        }
    }

    public static void PressKey(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }
}

/// <summary>
/// <c>ion-toolbar</c>, <c>ion-header</c> and <c>ion-footer</c>. The numbers are from real Ionic in Chromium at 390 px wide, and
/// the behaviour numbers are from docs/components/ion-toolbar/PLAN.md.
/// </summary>
public class AvonicToolbarTests
{
    private static Border Background(AvonicToolbar toolbar) => ToolbarTest.Part<Border>(toolbar, "PART_Background");

    private static Border Container(AvonicToolbar toolbar) => ToolbarTest.Part<Border>(toolbar, "PART_Container");

    private static Border ContentPart(AvonicToolbar toolbar) => ToolbarTest.Part<Border>(toolbar, "PART_Content");

    // ── T1: the box ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T1_56_High_Full_Width_Grey_Text_On_The_Background() => InstalledTheme.Run(_ =>
    {
        var toolbar = new AvonicToolbar { Content = new AvonicTitle { Content = "Title" } };
        using var scene = ToolbarTest.Show(toolbar);

        Assert.Equal(new Size(390, 56), toolbar.Bounds.Size);
        Assert.Equal(new Thickness(0), toolbar.Padding);
        Assert.Equal(ToolbarTest.Toolbar, ToolbarTest.Solid(toolbar.Foreground));
        Assert.Equal(Colors.White, ToolbarTest.Solid(Background(toolbar).Background));
        Assert.Equal(new Rect(0, 0, 390, 56), ToolbarTest.Box(Background(toolbar), toolbar));
    });

    // ── T2: md's slot order ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T2_Start_Content_Then_End_Centred() => InstalledTheme.Run(_ =>
    {
        var start = Buttons(IconButton("menu"));
        var edit = new AvonicButton { Content = "Edit" };
        var end = Buttons(edit, IconButton("search"));
        var toolbar = new AvonicToolbar { StartContent = start, Content = new AvonicTitle { Content = "Header" }, EndContent = end };
        using var scene = ToolbarTest.Show(toolbar);

        // Measured: start 4–52, content 52–282.75, end 282.75–386, the icon button 48 high at 4, Edit 32 high at 12.
        var startBox = ToolbarTest.Box(start, toolbar);
        var content = ToolbarTest.Box(ContentPart(toolbar), toolbar);
        var endBox = ToolbarTest.Box(end, toolbar);
        Assert.Equal(new Rect(4, 4, 48, 48), startBox);
        ToolbarTest.Near(52, content.X);
        ToolbarTest.Near(endBox.X, content.Right);
        ToolbarTest.Near(386, endBox.Right);
        ToolbarTest.Near(12, ToolbarTest.Box(edit, toolbar).Y);
        ToolbarTest.Near(103.25, endBox.Width, 3);
    });

    [AvaloniaFact]
    public void T2_Secondary_Comes_After_The_Content_And_Before_Primary() => InstalledTheme.Run(_ =>
    {
        var secondary = Buttons(IconButton("star"));
        var primary = Buttons(new AvonicButton { Content = "Primary" });
        var toolbar = new AvonicToolbar
        {
            Color = "primary", SecondaryContent = secondary, PrimaryContent = primary, Content = new AvonicTitle { Content = "Primary" },
        };
        using var scene = ToolbarTest.Show(toolbar);

        // Measured: content 0–248.58, secondary 248.58–296.58, primary 296.58–386.
        var content = ToolbarTest.Box(ContentPart(toolbar), toolbar);
        var secondaryBox = ToolbarTest.Box(secondary, toolbar);
        var primaryBox = ToolbarTest.Box(primary, toolbar);
        Assert.Equal(0, content.X);
        ToolbarTest.Near(secondaryBox.X, content.Right);
        Assert.Equal(48, secondaryBox.Width);
        ToolbarTest.Near(primaryBox.X, secondaryBox.Right);
        ToolbarTest.Near(386, primaryBox.Right);
    });

    [AvaloniaFact]
    public void T2_The_Content_Takes_What_Is_Left_And_Can_Shrink_To_Nothing() => InstalledTheme.Run(_ =>
    {
        var wide = new Border { Width = 500, Height = 10 };
        var toolbar = new AvonicToolbar { StartContent = wide, Content = new AvonicTitle { Content = "Title" } };
        using var scene = ToolbarTest.Show(toolbar);

        Assert.Equal(0, ContentPart(toolbar).Bounds.Width);

        wide.Width = 100;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(290, ContentPart(toolbar).Bounds.Width);
    });

    // ── T3: RTL ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T3_RTL_Puts_Start_On_The_Right() => InstalledTheme.Run(_ =>
    {
        var start = Buttons(new AvonicButton { Content = "Start" });
        var end = Buttons(new AvonicButton { Content = "End" });
        var toolbar = new AvonicToolbar
        {
            FlowDirection = FlowDirection.RightToLeft, StartContent = start, Content = new AvonicTitle { Content = "RTL" }, EndContent = end,
        };
        using var scene = ToolbarTest.Show(toolbar);

        // Measured: start ends at 386, end starts at 4.
        ToolbarTest.Near(386, ToolbarTest.Box(start, scene.Window).Right);
        ToolbarTest.Near(4, ToolbarTest.Box(end, scene.Window).X);
    });

    // ── T4: the keyboard follows Ionic's slot order ─────────────────────────────────────────

    [AvaloniaFact]
    public void T4_Tab_Goes_Start_Secondary_Default_Primary_End() => InstalledTheme.Run(_ =>
    {
        var before = new Button { Content = "before" };
        AvonicButton start = new() { Content = "Sta" }, secondary = new() { Content = "Sec" }, inDefault = new() { Content = "Def" },
            primary = new() { Content = "Pri" }, end = new() { Content = "End" };
        var after = new Button { Content = "after" };
        var toolbar = new AvonicToolbar
        {
            EndContent = Buttons(end), PrimaryContent = Buttons(primary), Content = inDefault,
            SecondaryContent = Buttons(secondary), StartContent = Buttons(start),
        };
        using var scene = new Scene(new StackPanel { Children = { before, toolbar, after } });
        before.Focus();

        var order = new List<object?>();
        for (var i = 0; i < 6; i++)
        {
            ToolbarTest.PressKey(scene, PhysicalKey.Tab);
            order.Add(scene.Window.FocusManager!.GetFocusedElement());
        }

        Assert.Equal<object?>([start, secondary, inDefault, primary, end, after], order);
    });

    // ── T5: a long title ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T5_A_Long_Title_Ends_With_An_Ellipsis_Inside_The_Content() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicTitle { Content = "A title that is far too long to fit in the toolbar on one line" };
        var toolbar = new AvonicToolbar { StartContent = Buttons(IconButton("menu")), Content = title };
        using var scene = ToolbarTest.Show(toolbar);

        var text = title.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(ContentPart(toolbar).Bounds.Width, title.Bounds.Width);
        Assert.Single(text.TextLayout.TextLines);
        Assert.True(text.TextLayout.TextLines[0].HasCollapsed);
        Assert.True(ToolbarTest.Box(text, title).Right <= title.Bounds.Width - 20 + 0.5);
    });

    // ── T6: color ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T6_Color_Paints_Base_And_Contrast() => InstalledTheme.Run(_ =>
    {
        var toolbar = new AvonicToolbar { Color = "danger", Content = "Danger" };
        using var scene = ToolbarTest.Show(toolbar);

        Assert.Equal(ToolbarTest.Danger, ToolbarTest.Solid(Background(toolbar).Background));
        Assert.Equal(Colors.White, ToolbarTest.Solid(toolbar.Foreground));
    });

    [AvaloniaFact]
    public void T6_An_Unknown_Color_Is_Transparent_And_Inherits_The_Text() => InstalledTheme.Run(_ =>
    {
        var toolbar = new AvonicToolbar { Color = "nonexistent", Content = "Unknown" };
        var holder = new StackPanel { Children = { toolbar } };
        TextElement.SetForeground(holder, Brushes.Orange);
        using var scene = new Scene(holder);

        Assert.Null(Background(toolbar).Background);
        Assert.Equal(Colors.Orange, ToolbarTest.Solid(toolbar.Foreground));
    });

    // ── T7: the variables ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T7_The_Variables_Apply() => InstalledTheme.Run(_ =>
    {
        var toolbar = new AvonicToolbar
        {
            Content = "x",
            Resources =
            {
                ["Avonic.Toolbar.Background"] = Brushes.Yellow,
                ["Avonic.Toolbar.Color"] = Brushes.Purple,
                ["Avonic.Toolbar.BorderColor"] = Brushes.Green,
                ["Avonic.Toolbar.BorderWidth"] = new Thickness(0, 0, 0, 2),
                ["Avonic.Toolbar.MinHeight"] = 70d,
                ["Avonic.Toolbar.Opacity"] = 0.5,
                ["Avonic.Toolbar.PaddingStart"] = 1d,
                ["Avonic.Toolbar.PaddingTop"] = 2d,
                ["Avonic.Toolbar.PaddingEnd"] = 3d,
                ["Avonic.Toolbar.PaddingBottom"] = 4d,
            },
        };
        using var scene = ToolbarTest.Show(toolbar);

        var background = Background(toolbar);
        Assert.Equal(Colors.Yellow, ToolbarTest.Solid(background.Background));
        Assert.Equal(Colors.Green, ToolbarTest.Solid(background.BorderBrush));
        Assert.Equal(new Thickness(0, 0, 0, 2), background.BorderThickness);
        Assert.Equal(0.5, background.Opacity);
        Assert.Equal(Colors.Purple, ToolbarTest.Solid(toolbar.Foreground));
        Assert.Equal(new Thickness(1, 2, 3, 4), Container(toolbar).Padding);
        Assert.Equal(70, toolbar.Bounds.Height);
    });

    [AvaloniaFact]
    public void T7_The_Border_Defaults_To_Ionic_Grey_At_No_Width() => InstalledTheme.Run(_ =>
    {
        var toolbar = new AvonicToolbar { Content = "x" };
        using var scene = ToolbarTest.Show(toolbar);

        Assert.Equal(Color.Parse("#C1C4CD"), ToolbarTest.Solid(Background(toolbar).BorderBrush));
        Assert.Equal(new Thickness(0), Background(toolbar).BorderThickness);
        Assert.Equal(1, Background(toolbar).Opacity);
    });

    // ── T8: the global tokens ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T8_Global_Tokens_Apply_And_The_Toolbar_Variables_Beat_Them() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicToolbar { Content = "x" };
        var own = new AvonicToolbar
        {
            Content = "y",
            Resources =
            {
                ["Avonic.Toolbar.Background"] = Brushes.Yellow,
                ["Avonic.Toolbar.Color"] = Brushes.Purple,
                ["Avonic.Toolbar.BorderColor"] = Brushes.Green,
            },
        };
        var page = new StackPanel
        {
            Children = { plain, own },
            Resources =
            {
                ["Avonic.ToolbarBackgroundBrush"] = Brushes.Black,
                ["Avonic.ToolbarColorBrush"] = Brushes.Red,
                ["Avonic.ToolbarBorderColorBrush"] = Brushes.Blue,
            },
        };
        using var scene = new Scene(page);

        Assert.Equal(Colors.Black, ToolbarTest.Solid(Background(plain).Background));
        Assert.Equal(Colors.Red, ToolbarTest.Solid(plain.Foreground));
        Assert.Equal(Colors.Blue, ToolbarTest.Solid(Background(plain).BorderBrush));
        Assert.Equal(Colors.Yellow, ToolbarTest.Solid(Background(own).Background));
        Assert.Equal(Colors.Purple, ToolbarTest.Solid(own.Foreground));
        Assert.Equal(Colors.Green, ToolbarTest.Solid(Background(own).BorderBrush));
    });

    // ── T10: the dark palette ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T10_In_The_Dark_Palette_The_Text_Is_White_And_The_Border_Step_150() => InstalledTheme.Run(_ => ToolbarTest.InDark(() =>
    {
        var title = new AvonicTitle { Content = "Title" };
        var solid = new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid };
        var clear = new AvonicButton { Content = "Clear" };
        var toolbar = new AvonicToolbar { StartContent = Buttons(solid, clear), Content = title };
        using var scene = ToolbarTest.Show(toolbar);

        // Measured with dark.always.css: color #ffffff (ion-text-color), border-color #363636 (ion-background-color-step-150),
        // on #1f1f1f. A solid button in ion-buttons is the toolbar's text colour, with the toolbar's background as its text.
        Assert.Equal(Colors.White, ToolbarTest.Solid(toolbar.Foreground));
        Assert.Equal(Colors.White, ToolbarTest.Solid(title.Foreground));
        Assert.Equal(Color.Parse("#363636"), ToolbarTest.Solid(Background(toolbar).BorderBrush));
        Assert.Equal(Color.Parse("#1F1F1F"), ToolbarTest.Solid(Background(toolbar).Background));
        var native = ToolbarTest.Part<Border>(solid, "PART_Native");
        Assert.Equal(Colors.White, ToolbarTest.Solid(native.Background));
        Assert.Equal(Color.Parse("#1F1F1F"), ToolbarTest.Solid(TextElement.GetForeground(native)));
        Assert.Equal(Colors.White, ToolbarTest.Solid(TextElement.GetForeground(ToolbarTest.Part<Border>(clear, "PART_Native"))));
    }));

    [AvaloniaFact]
    public void T10_In_The_Dark_Palette_The_Toolbar_Follows_The_Text_And_Border_Colours() => InstalledTheme.Run(_ => ToolbarTest.InDark(() =>
    {
        // var(ion-text-color) and var(ion-border-color, …), which dark leaves to an app to change.
        var app = Application.Current!;
        app.Resources["Avonic.TextColor"] = Colors.Orange;
        app.Resources["Avonic.BorderColor"] = Colors.Purple;
        try
        {
            var toolbar = new AvonicToolbar { Content = "x" };
            using var scene = ToolbarTest.Show(toolbar);

            Assert.Equal(Colors.Orange, ToolbarTest.Solid(toolbar.Foreground));
            Assert.Equal(Colors.Purple, ToolbarTest.Solid(Background(toolbar).BorderBrush));
        }
        finally
        {
            app.Resources.Remove("Avonic.TextColor");
            app.Resources.Remove("Avonic.BorderColor");
        }
    }));

    [AvaloniaFact]
    public void T10_The_Dark_Palette_Still_Gives_Way_To_The_Toolbar_Tokens() => InstalledTheme.Run(_ => ToolbarTest.InDark(() =>
    {
        var toolbar = new AvonicToolbar
        {
            Content = "x",
            Resources = { ["Avonic.ToolbarColorBrush"] = Brushes.Red, ["Avonic.ToolbarBorderColorBrush"] = Brushes.Blue },
        };
        using var scene = ToolbarTest.Show(toolbar);

        Assert.Equal(Colors.Red, ToolbarTest.Solid(toolbar.Foreground));
        Assert.Equal(Colors.Blue, ToolbarTest.Solid(Background(toolbar).BorderBrush));
    }));

    // ── T11: ionStyle ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T11_A_Title_Gives_The_Toolbar_Its_Size_As_A_Host_Class() => InstalledTheme.Run(_ =>
    {
        // toolbar-title-default, -small and -large, as measured in Chromium. md has no rule on them, but an app's can.
        var title = new AvonicTitle { Content = "Title", Size = TitleSize.Large };
        var toolbar = new AvonicToolbar { Content = title };
        var page = new StackPanel { Children = { toolbar } };
        page.Styles.Add(ToolbarTest.AppStyle<AvonicToolbar>(":toolbar-title-large", Visual.OpacityProperty, 0.25));
        page.Styles.Add(ToolbarTest.AppStyle<AvonicToolbar>(":toolbar-title-small", Visual.OpacityProperty, 0.5));
        page.Styles.Add(ToolbarTest.AppStyle<AvonicToolbar>(":toolbar-title-default", Visual.OpacityProperty, 0.75));
        using var scene = ToolbarTest.Show(page);
        Assert.Equal(0.25, toolbar.Opacity);

        // sizeChanged emits again, and the toolbar drops what the title said before.
        title.Size = TitleSize.Small;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.5, toolbar.Opacity);

        title.Size = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.75, toolbar.Opacity);
    });

    [AvaloniaFact]
    public void T11_A_Title_Added_Later_Tells_The_Toolbar() => InstalledTheme.Run(_ =>
    {
        // connectedCallback emits.
        var toolbar = new AvonicToolbar();
        var page = new StackPanel { Children = { toolbar } };
        page.Styles.Add(ToolbarTest.AppStyle<AvonicToolbar>(":toolbar-title-large", Visual.OpacityProperty, 0.25));
        using var scene = ToolbarTest.Show(page);
        Assert.Equal(1, toolbar.Opacity);

        toolbar.Content = new AvonicTitle { Content = "Title", Size = TitleSize.Large };
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.25, toolbar.Opacity);
    });

    [AvaloniaFact]
    public void T11_Each_Type_Of_Child_Keeps_What_It_Said() => InstalledTheme.Run(_ =>
    {
        // childrenStyles is a map by tag name, merged at render: a label doesn't replace the title's.
        var toolbar = new AvonicToolbar
        {
            StartContent = new AvonicLabel { Content = "Label" },
            Content = new AvonicTitle { Content = "Title", Size = TitleSize.Large },
        };
        var page = new StackPanel { Children = { toolbar } };
        page.Styles.Add(ToolbarTest.AppStyle<AvonicToolbar>(":toolbar-title-large", Visual.OpacityProperty, 0.25));
        page.Styles.Add(ToolbarTest.AppStyle<AvonicToolbar>(":toolbar-label", Layoutable.MarginProperty, new Thickness(3)));
        using var scene = ToolbarTest.Show(page);

        Assert.Equal(0.25, toolbar.Opacity);
        Assert.Equal(new Thickness(3), toolbar.Margin);
    });

    [AvaloniaFact]
    public void T11_The_Toolbar_Stops_Ion_Style_So_An_Item_Around_It_Hears_Nothing() => InstalledTheme.Run(_ =>
    {
        // childrenStyle calls stopPropagation: a stacked label in a toolbar in an item doesn't make it a stacked label's item,
        // which is 55 high at least. Beside it, one that is.
        var inToolbar = new AvonicItem { Content = new AvonicToolbar { Content = new AvonicLabel { Content = "In a toolbar", Position = LabelPosition.Stacked } } };
        var direct = new AvonicItem { Content = new AvonicLabel { Content = "Direct", Position = LabelPosition.Stacked } };
        using var scene = ToolbarTest.Show(new StackPanel { Children = { inToolbar, direct } });

        Assert.Equal(55, ToolbarTest.Part<Border>(direct, "PART_Native").MinHeight);
        Assert.NotEqual(55, ToolbarTest.Part<Border>(inToolbar, "PART_Native").MinHeight);
    });

    // ── T9: the safe area's sides ───────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void T9_Safe_Area_Left_And_Right_Are_Physical_Padding(FlowDirection flow) => InstalledTheme.Run(_ =>
    {
        var toolbar = new AvonicToolbar
        {
            FlowDirection = flow,
            Content = "x",
            Resources = { ["Avonic.SafeAreaLeft"] = 7d, ["Avonic.SafeAreaRight"] = 5d },
        };
        using var scene = ToolbarTest.Show(toolbar);

        // Measured: container at 7, 378 wide; the background still 0–390.
        Assert.Equal(new Rect(7, 0, 378, 56), ToolbarTest.Box(Container(toolbar), scene.Window));
        Assert.Equal(new Rect(0, 0, 390, 56), ToolbarTest.Box(Background(toolbar), scene.Window));
    });

    // ── T10: buttons-first-slot and buttons-last-slot ───────────────────────────────────────

    [AvaloniaFact]
    public void T10_The_First_Start_And_Last_End_Buttons_Get_4() => InstalledTheme.Run(_ =>
    {
        AvonicButtons s1 = Buttons(new AvonicButton { Content = "A" }), s2 = Buttons(new AvonicButton { Content = "B" });
        AvonicButtons e1 = Buttons(new AvonicButton { Content = "C" }), e2 = Buttons(new AvonicButton { Content = "D" });
        var toolbar = new AvonicToolbar
        {
            StartContent = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { s1, s2 } },
            EndContent = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { e1, e2 } },
        };
        using var scene = ToolbarTest.Show(toolbar);

        Assert.Equal(new Thickness(4, 0, 0, 0), s1.Margin);
        Assert.Equal(new Thickness(0), s2.Margin);
        Assert.Equal(new Thickness(0), e1.Margin);
        Assert.Equal(new Thickness(0, 0, 4, 0), e2.Margin);
    });

    [AvaloniaFact]
    public void T10_Last_Falls_Back_To_Primary_Then_Secondary_And_Follows_A_Change() => InstalledTheme.Run(_ =>
    {
        AvonicButtons secondary = Buttons(new AvonicButton { Content = "S" }), primary = Buttons(new AvonicButton { Content = "P" });
        var end = Buttons(new AvonicButton { Content = "E" });
        var toolbar = new AvonicToolbar { SecondaryContent = secondary };
        using var scene = ToolbarTest.Show(toolbar);
        Assert.Equal(new Thickness(0, 0, 4, 0), secondary.Margin);

        toolbar.PrimaryContent = primary;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(0), secondary.Margin);
        Assert.Equal(new Thickness(0, 0, 4, 0), primary.Margin);

        toolbar.EndContent = end;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(0), primary.Margin);
        Assert.Equal(new Thickness(0, 0, 4, 0), end.Margin);
    });

    [AvaloniaFact]
    public void T10_A_New_Start_Takes_The_4_From_The_Old() => InstalledTheme.Run(_ =>
    {
        AvonicButtons old = Buttons(new AvonicButton { Content = "Old" }), replacement = Buttons(new AvonicButton { Content = "New" });
        var toolbar = new AvonicToolbar { StartContent = old };
        using var scene = ToolbarTest.Show(toolbar);
        Assert.Equal(new Thickness(4, 0, 0, 0), old.Margin);

        toolbar.StartContent = replacement;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(0), old.Margin);
        Assert.Equal(new Thickness(4, 0, 0, 0), replacement.Margin);
    });

    [AvaloniaFact]
    public void T10_Buttons_A_Template_Makes_Count_Too() => InstalledTheme.Run(_ =>
    {
        var template = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => Buttons(new AvonicButton { Content = text }));
        var toolbar = new AvonicToolbar { StartContent = "Start", StartContentTemplate = template, EndContent = "End", EndContentTemplate = template };
        using var scene = ToolbarTest.Show(toolbar);

        var made = toolbar.GetVisualDescendants().OfType<AvonicButtons>().ToList();
        Assert.Equal(2, made.Count);
        Assert.Equal(new Thickness(4, 0, 0, 0), made[0].Margin);
        Assert.Equal(new Thickness(0, 0, 4, 0), made[1].Margin);
    });

    // ── H1, F1: stacking ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void H1_F1_A_Header_And_Footer_Stack_Their_Toolbars() => InstalledTheme.Run(_ =>
    {
        AvonicToolbar one = new() { Content = "One" }, two = new() { Content = "Two" }, three = new() { Content = "Three" };
        var header = new AvonicHeader { Items = { one, two } };
        var footer = new AvonicFooter { Items = { three } };
        using var scene = new Scene(new StackPanel { Children = { header, footer } });

        Assert.Equal(new Size(390, 112), header.Bounds.Size);
        Assert.Equal(new Rect(0, 56, 390, 56), ToolbarTest.Box(two, header));
        Assert.Equal(new Size(390, 56), footer.Bounds.Size);
    });

    // ── H2, F1: the shadow ──────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void H2_F1_The_Md_Shadow_And_Ion_No_Border(bool footer) => InstalledTheme.Run(_ =>
    {
        ItemsControl host = footer ? new AvonicFooter { Items = { new AvonicToolbar() } } : new AvonicHeader { Items = { new AvonicToolbar() } };
        using var scene = ToolbarTest.Show(host);

        var native = (Border)host.GetVisualDescendants().First(v => v is Border { Name: "PART_Native" });
        Assert.Equal(3, native.BoxShadow.Count);
        Assert.Equal(new BoxShadow { OffsetX = 0, OffsetY = 2, Blur = 4, Spread = -1, Color = Color.Parse("#33000000") }, native.BoxShadow[0]);
        Assert.Equal(new BoxShadow { OffsetY = 4, Blur = 5, Color = Color.Parse("#24000000") }, native.BoxShadow[1]);
        Assert.Equal(new BoxShadow { OffsetY = 1, Blur = 10, Color = Color.Parse("#1F000000") }, native.BoxShadow[2]);

        host.Classes.Add("ion-no-border");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, native.BoxShadow.Count);
    });

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void H2_F1_The_Shadow_Draws_Over_What_Comes_After(bool footer) => InstalledTheme.Run(_ =>
    {
        ItemsControl host = footer ? new AvonicFooter { Items = { new AvonicToolbar() } } : new AvonicHeader { Items = { new AvonicToolbar() } };
        var content = new Border { Background = Brushes.White, Height = 100 };
        using var scene = new Scene(new StackPanel { Children = { host, content } });
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();

        using var frame = scene.Window.CaptureRenderedFrame()!;
        var below = Pixel(frame, 195, 58);
        Assert.True(below.R < 235, $"2 below the header is {below}, so the content covered the shadow");
    });

    [AvaloniaFact]
    public void H2_F1_In_A_Page_The_Shadows_Are_Over_The_Content() => InstalledTheme.Run(_ =>
    {
        var list = new StackPanel { Background = Brushes.White };
        for (var i = 0; i < 40; i++)
        {
            list.Children.Add(new TextBlock { Text = $"Row {i}", Height = 40 });
        }

        var header = new AvonicHeader { Items = { new AvonicToolbar { Content = new AvonicTitle { Content = "Header" } } } };
        var footer = new AvonicFooter { Items = { new AvonicToolbar { Content = new AvonicTitle { Content = "Footer" } } } };
        var content = new AvonicContent { Content = list };
        Grid.SetRow(content, 1);
        Grid.SetRow(footer, 2);
        var page = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Children = { header, content, footer } };
        using var scene = new Scene(page);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Rect(0, 0, 390, 56), header.Bounds);
        Assert.Equal(new Rect(0, 788, 390, 56), footer.Bounds);
        using var frame = scene.Window.CaptureRenderedFrame()!;
        Assert.True(Pixel(frame, 195, 58).R < 235, "the content covered the header's shadow");
    });

    [AvaloniaFact]
    public void B2_Buttons_Do_Not_Clip_What_Overflows_Them() => InstalledTheme.Run(_ =>
    {
        // ion-buttons has no overflow: hidden, so a child drawn past it shows. This one is 20 high and 10 of it is above.
        var child = new Border { Width = 20, Height = 20, Margin = new Thickness(0, -10, 0, 0), Background = Brushes.Red };
        var buttons = new AvonicButtons { Margin = new Thickness(0, 20, 0, 0), Items = { child } };
        using var scene = new Scene(new StackPanel { Children = { buttons } });
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(20, ToolbarTest.Box(buttons, scene.Window).Y);
        using var frame = scene.Window.CaptureRenderedFrame()!;
        var above = Pixel(frame, (int)ToolbarTest.Box(child, scene.Window).Center.X, 15);
        Assert.True(above.G < 50, $"above the buttons is {above}, so they clipped their child");
    });

    [AvaloniaFact]
    public void T2_The_Container_Clips_At_The_Safe_Area() => InstalledTheme.Run(_ =>
    {
        // .toolbar-container { overflow: hidden }, inside the host's padding, which is the safe area.
        var wide = new Border { Width = 300, Height = 20, Background = Brushes.Red };
        var toolbar = new AvonicToolbar
        {
            Width = 200, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, StartContent = wide,
            Resources = { ["Avonic.SafeAreaRight"] = 30d, ["Avonic.ToolbarBackgroundBrush"] = Brushes.White },
        };
        using var scene = new Scene(new StackPanel { Children = { toolbar } });
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();

        using var frame = scene.Window.CaptureRenderedFrame()!;
        Assert.True(Pixel(frame, 160, 28).G < 50, "the content isn't drawn");
        var inPadding = Pixel(frame, 185, 28);
        Assert.True(inPadding.G > 200, $"the safe area is {inPadding}, so the container didn't clip");
    });

    private static Color Pixel(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        unsafe
        {
            var pixel = ((uint*)buffer.Address)[(y * buffer.RowBytes / 4) + x];
            return Color.FromUInt32(pixel);
        }
    }

    // ── H3, F2: the safe area's top and bottom ──────────────────────────────────────────────

    [AvaloniaFact]
    public void H3_The_First_Toolbar_In_A_Header_Has_The_Top_Safe_Area() => InstalledTheme.Run(_ =>
    {
        AvonicToolbar first = new() { Content = "One" }, second = new() { Content = "Two" };
        var header = new AvonicHeader
        {
            Items = { first, second },
            Resources = { ["Avonic.SafeAreaTop"] = 20d, ["Avonic.SafeAreaLeft"] = 7d, ["Avonic.SafeAreaRight"] = 5d },
        };
        using var scene = ToolbarTest.Show(header);

        // Measured s2 #t1: padding 20 5 0 7, keeping the sides.
        Assert.Equal(new Thickness(7, 20, 5, 0), first.Padding);

        // Measured: 76 high, the container at 20, the background from 0. The second toolbar has none.
        Assert.Equal(76, first.Bounds.Height);
        Assert.Equal(new Rect(7, 20, 378, 56), ToolbarTest.Box(Container(first), first));
        Assert.Equal(new Rect(0, 0, 390, 76), ToolbarTest.Box(Background(first), first));
        Assert.Equal(56, second.Bounds.Height);
        Assert.Equal(132, header.Bounds.Height);
    });

    [AvaloniaFact]
    public void F2_The_Last_Toolbar_In_A_Footer_Has_The_Bottom_Safe_Area() => InstalledTheme.Run(_ =>
    {
        AvonicToolbar first = new() { Content = "One" }, last = new() { Content = "Two" };
        var footer = new AvonicFooter
        {
            Items = { first, last },
            Resources = { ["Avonic.SafeAreaBottom"] = 10d, ["Avonic.SafeAreaLeft"] = 7d, ["Avonic.SafeAreaRight"] = 5d },
        };
        using var scene = ToolbarTest.Show(footer);

        // Measured s2 #t6: padding 0 5 10 7.
        Assert.Equal(new Thickness(7, 0, 5, 10), last.Padding);

        // Measured: 66 high, the container at the top.
        Assert.Equal(56, first.Bounds.Height);
        Assert.Equal(66, last.Bounds.Height);
        Assert.Equal(new Rect(7, 0, 378, 56), ToolbarTest.Box(Container(last), last));
    });

    [AvaloniaFact]
    public void H3_F2_Toolbars_A_Template_Makes_Get_The_Safe_Area_Too() => InstalledTheme.Run(_ =>
    {
        var template = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => new AvonicToolbar { Content = text });
        var header = new AvonicHeader { ItemsSource = new[] { "One", "Two" }, ItemTemplate = template };
        var footer = new AvonicFooter { ItemsSource = new[] { "Three", "Four" }, ItemTemplate = template };
        var page = new StackPanel
        {
            Children = { header, footer },
            Resources = { ["Avonic.SafeAreaTop"] = 20d, ["Avonic.SafeAreaBottom"] = 10d },
        };
        using var scene = new Scene(page);

        var heights = page.GetVisualDescendants().OfType<AvonicToolbar>().Select(toolbar => toolbar.Bounds.Height);
        Assert.Equal([76d, 56d, 56d, 66d], heights);
    });

    // ── H5: collapse ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void H5_A_Condense_Header_Is_Hidden_And_No_Banner_In_Md() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicHeader { Collapse = HeaderCollapse.Condense, Items = { new AvonicToolbar { Content = "Large" } } };
        using var scene = ToolbarTest.Show(header);

        // .header-md.header-collapse-condense { display: none }; getRoleType gives none.
        Assert.False(header.IsVisible);
        Assert.Null(ControlAutomationPeer.CreatePeerForElement(header).GetLandmarkType());

        header.Collapse = null;
        Dispatcher.UIThread.RunJobs();
        Assert.True(header.IsVisible);
        Assert.Equal(AutomationLandmarkType.Banner, ControlAutomationPeer.CreatePeerForElement(header).GetLandmarkType());
    });

    [AvaloniaFact]
    public void H5_A_Fade_Header_Looks_Like_The_Default_In_Md() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicHeader { Collapse = HeaderCollapse.Fade, Items = { new AvonicToolbar { Content = "Fade" } } };
        using var scene = ToolbarTest.Show(header);

        Assert.True(header.IsVisible);
        Assert.Equal(56, header.Bounds.Height);
        Assert.Equal(AutomationLandmarkType.Banner, ControlAutomationPeer.CreatePeerForElement(header).GetLandmarkType());
    });

    [AvaloniaFact]
    public void H5_A_Fade_Header_Has_Ionics_Host_Class_For_An_App_To_Style() => InstalledTheme.Run(_ =>
    {
        // header-collapse-fade, as measured in Chromium. md has no rule on it, but an app's can.
        var fade = new AvonicHeader { Collapse = HeaderCollapse.Fade, Items = { new AvonicToolbar { Content = "Fade" } } };
        var plain = new AvonicHeader { Items = { new AvonicToolbar { Content = "Plain" } } };
        var page = new StackPanel { Children = { fade, plain } };
        page.Styles.Add(ToolbarTest.AppStyle<AvonicHeader>(":collapse-fade", Visual.OpacityProperty, 0.25));
        page.Styles.Add(ToolbarTest.AppStyle<AvonicHeader>(":collapse-none", Layoutable.MarginProperty, new Thickness(3)));
        using var scene = ToolbarTest.Show(page);

        // And header-collapse-none with no collapse.
        Assert.Equal(0.25, fade.Opacity);
        Assert.Equal(new Thickness(0), fade.Margin);
        Assert.Equal(1, plain.Opacity);
        Assert.Equal(new Thickness(3), plain.Margin);

        fade.Collapse = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, fade.Opacity);
        Assert.Equal(new Thickness(3), fade.Margin);
    });

    // ── H4, F3: landmarks ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void H4_F3_A_Header_Is_The_Banner_And_A_Footer_The_Content_Info()
    {
        var header = new AvonicHeader();
        var footer = new AvonicFooter();
        using var scene = new Scene(new StackPanel { Children = { header, footer } });

        Assert.Equal(AutomationLandmarkType.Banner, ControlAutomationPeer.CreatePeerForElement(header).GetLandmarkType());
        Assert.Equal(AutomationLandmarkType.ContentInfo, ControlAutomationPeer.CreatePeerForElement(footer).GetLandmarkType());
    }
}
