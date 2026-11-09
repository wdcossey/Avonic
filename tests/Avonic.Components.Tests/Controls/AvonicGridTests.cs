using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-grid, ion-row and ion-col in Chromium, md, in ion-app, at a 390 viewport unless a test says
/// otherwise, with Roboto at 16 px, a web page's default. See docs/components/ion-grid/PLAN.md, whose behaviours these are
/// numbered after. The padding is in the theme, so nearly every test installs it.
/// </summary>
public class AvonicGridTests
{
    private static void Run() => Dispatcher.UIThread.RunJobs();

    private static Rect Box(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    // Avalonia rounds layout to device pixels, and at 1x that moves an edge by up to half a pixel and rounds a width up.
    private static void Near(double expected, double actual, double tolerance = 1) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);

    private static AvonicCol Col(string? size = null, string text = "x") => new() { Size = size, Content = text };

    private static AvonicRow Row(params Control[] children)
    {
        var row = new AvonicRow();
        foreach (var child in children)
        {
            row.Items.Add(child);
        }

        return row;
    }

    private static AvonicGrid Grid(params Control[] rows)
    {
        var grid = new AvonicGrid();
        foreach (var row in rows)
        {
            grid.Items.Add(row);
        }

        return grid;
    }

    /// <summary>The page's text is 16 px, as a web page's is.</summary>
    private static Scene Show(Control content, double width = 390)
    {
        var page = new StackPanel { Children = { content } };
        TextElement.SetFontSize(page, 16);
        return new Scene(page, width);
    }

    private static double X(Visual visual, Scene scene) => Box(visual, scene.Window).X;

    private static double Width(Visual visual) => visual.Bounds.Width;

    // ── 1. Equal columns ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Columns_With_No_Size_Share_The_Row() => InstalledTheme.Run(_ =>
    {
        AvonicCol a = Col(text: "1"), b = Col(text: "2"), c = Col(text: "3");
        using var scene = Show(Grid(Row(a, b, c)));

        Near(5, X(a, scene));
        Near(131.67, X(b, scene));
        Near(258.33, X(c, scene));
        foreach (var col in new[] { a, b, c })
        {
            Near(126.67, Width(col));
        }
    });

    // ── 2. Sizes ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_A_Size_Is_Its_Share_Of_Twelve_And_The_Rest_Share_What_Is_Left() => InstalledTheme.Run(_ =>
    {
        AvonicCol six = Col("6"), rest = Col(text: "rest"), three = Col("3");
        using var scene = Show(Grid(Row(six, rest, three)));

        Near(5, X(six, scene));
        Near(190, Width(six));
        Near(195, X(rest, scene));
        Near(95, Width(rest));
        Near(290, X(three, scene));
        Near(95, Width(three));
    });

    [AvaloniaFact]
    public void B2_A_Column_From_An_Item_Template_Is_Laid_Out_As_One() => InstalledTheme.Run(_ =>
    {
        var row = new AvonicRow
        {
            ItemsSource = new[] { "a", "b", "c" },
            ItemTemplate = new FuncDataTemplate<string>((text, _) => new AvonicCol { Size = "4", Content = text }),
        };
        using var scene = Show(Grid(row));

        var columns = row.GetVisualDescendants().OfType<AvonicCol>().ToList();
        Assert.Equal(3, columns.Count);
        Near(5, X(columns[0], scene));
        Near(131.67, X(columns[1], scene));
        Near(258.33, X(columns[2], scene));
        Assert.All(columns, column => Near(126.67, Width(column)));
    });

    [AvaloniaFact]
    public void B2_A_Templated_Columns_Margin_Is_Outside_Its_Width() => InstalledTheme.Run(_ =>
    {
        // flex: 0 0 50% is the border box, so a margin makes the two too wide for one line, as it does a column of the row's own.
        var row = new AvonicRow
        {
            ItemsSource = new[] { "a", "b" },
            ItemTemplate = new FuncDataTemplate<string>((text, _) => new AvonicCol { Size = "6", Margin = new Thickness(10, 0, 10, 0), Content = text }),
        };
        using var scene = Show(Grid(row));

        var columns = row.GetVisualDescendants().OfType<AvonicCol>().ToList();
        Near(190, Width(columns[0]));
        Near(15, X(columns[1], scene));
        Near(Box(columns[0], scene.Window).Bottom, Box(columns[1], scene.Window).Y);
    });

    [AvaloniaFact]
    public void B2_A_Templated_Item_That_Grows_Fills_The_Row() => InstalledTheme.Run(_ =>
    {
        // Not a column: flex: 0 1 auto, unless it has a flex of its own, as a grid's flex: 1 does.
        var row = new AvonicRow
        {
            ItemsSource = new[] { "a" },
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new Border { Height = 10, [FlexRow.FlexProperty] = 1d }),
        };
        using var scene = Show(Grid(row));

        Near(380, Width(row.GetVisualDescendants().OfType<Border>().Single(border => border.Height == 10)));
    });

    [AvaloniaFact]
    public void B2_A_Change_Of_Size_Lays_The_Row_Out_Again() => InstalledTheme.Run(_ =>
    {
        AvonicCol changed = Col("4."), rest = Col();
        using var scene = Show(Grid(Row(changed, rest)));

        changed.Size = "6";
        Run();

        Near(190, Width(changed));
        Near(195, X(rest, scene));
        Near(190, Width(rest));
    });

    [AvaloniaFact]
    public void B2_A_Hidden_Templated_Column_Takes_No_Space() => InstalledTheme.Run(_ =>
    {
        var row = new AvonicRow
        {
            ItemsSource = new[] { true, false, true },
            ItemTemplate = new FuncDataTemplate<bool>((shown, _) => new AvonicCol { Size = "6", IsVisible = shown, Content = "x" }),
        };
        using var scene = Show(Grid(row));

        var shown = row.GetVisualDescendants().OfType<AvonicCol>().Where(column => column.IsVisible).ToList();
        Assert.Equal(2, shown.Count);
        Near(5, X(shown[0], scene));
        Near(195, X(shown[1], scene));
        Near(Box(shown[0], scene.Window).Y, Box(shown[1], scene.Window).Y);

        // Shown again, it takes its half, and the last one wraps.
        row.GetVisualDescendants().OfType<AvonicCol>().Single(column => !column.IsVisible).IsVisible = true;
        Run();
        Near(5, X(shown[1], scene));
        Near(Box(shown[0], scene.Window).Bottom, Box(shown[1], scene.Window).Y);
    });

    // ── 3. Auto ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Auto_Is_As_Wide_As_Its_Content_And_The_Rest_Fill() => InstalledTheme.Run(_ =>
    {
        AvonicCol auto = Col("auto", "auto content"), rest = Col(text: "rest");
        using var scene = Show(Grid(Row(auto, rest)));

        Near(99.83, Width(auto));
        Near(5 + Width(auto), X(rest, scene));
        Near(385 - X(rest, scene), Width(rest));
    });

    [AvaloniaFact]
    public void B3_Auto_Is_Never_Wider_Than_The_Row_And_The_Next_Column_Wraps() => InstalledTheme.Run(_ =>
    {
        var auto = Col("auto", "auto with a very long text that should certainly be wider than the row itself is");
        var next = Col(text: "x");
        using var scene = Show(Grid(Row(auto, next)));

        Near(380, Width(auto));
        Assert.True(auto.Bounds.Height > next.Bounds.Height * 1.5, $"{auto.Bounds.Height} isn't two lines");
        Near(5, X(next, scene));
        Near(380, Width(next));
        Near(Box(auto, scene.Window).Bottom, Box(next, scene.Window).Y);
    });

    // ── 4. Wrapping ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Columns_That_Do_Not_Fit_Wrap_To_The_Next_Line() => InstalledTheme.Run(_ =>
    {
        AvonicCol first = Col("8"), second = Col("8");
        using var scene = Show(Grid(Row(first, second)));

        Near(253.33, Width(first));
        Near(253.33, Width(second));
        Near(5, X(second, scene));
        Near(Box(first, scene.Window).Bottom, Box(second, scene.Window).Y);
    });

    // ── 5. Offset ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Offset_Is_A_Margin_At_The_Start() => InstalledTheme.Run(_ =>
    {
        var offset = new AvonicCol { Size = "3", Offset = "3", Content = "off3" };
        var next = Col("3");
        using var scene = Show(Grid(Row(offset, next)));

        Near(100, X(offset, scene));
        Near(95, Width(offset));
        Near(195, X(next, scene));
    });

    [AvaloniaFact]
    public void B5_A_Column_With_An_Offset_And_No_Size_Still_Grows() => InstalledTheme.Run(_ =>
    {
        var offset = new AvonicCol { Offset = "2", Content = "offset no size" };
        var next = Col();
        using var scene = Show(Grid(Row(offset, next)));

        Near(68.33, X(offset, scene));
        Near(158.33, Width(offset));
        Near(226.67, X(next, scene));
        Near(158.33, Width(next));
    });

    // ── 6. Push and pull ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Push_And_Pull_Move_A_Column_Without_Moving_The_Others() => InstalledTheme.Run(_ =>
    {
        var pushed = new AvonicCol { Size = "9", Push = "3", Content = "push3" };
        var pulled = new AvonicCol { Size = "3", Pull = "9", Content = "pull9" };
        using var scene = Show(Grid(Row(pushed, pulled)));

        Near(100, X(pushed, scene));
        Near(285, Width(pushed));
        Near(5, X(pulled, scene));
        Near(95, Width(pulled));
    });

    [AvaloniaFact]
    public void B6_Push_Wins_Over_Pull_Even_At_Zero_And_An_Invalid_Push_Leaves_Pull() => InstalledTheme.Run(_ =>
    {
        var both = new AvonicCol { Size = "3", Push = "3", Pull = "6" };
        var invalid = new AvonicCol { Size = "3", Push = "abc", Pull = "1" };
        var zero = new AvonicCol { Size = "3", Push = "0", Pull = "1" };
        using var scene = Show(Grid(Row(both), Row(invalid, zero)));

        Near(100, X(both, scene));
        Near(-26.67, X(invalid, scene));
        Near(100, X(zero, scene));
    });

    // ── 7. Breakpoints ───────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(390, 380, 131.67)]
    [InlineData(576, 283, 193.67)]
    [InlineData(768, 252.67, 447.17)]
    [InlineData(992, 245.5, 659.67)]
    [InlineData(1200, 198.33, 798.33)]
    public void B7_The_Last_Breakpoint_At_Or_Below_The_Viewport_With_A_Value_Wins(double viewport, double width, double x) =>
        InstalledTheme.Run(_ =>
        {
            var sized = new AvonicCol { Size = "12", SizeSm = "6", SizeMd = "4", SizeLg = "3", SizeXl = "2" };
            var xs = new AvonicCol { Size = "6", SizeXs = "4" };
            var positioned = new AvonicCol { Size = "3", OffsetMd = "3", PushLg = "1", PullXl = "1" };
            using var scene = Show(Grid(Row(sized), Row(xs, positioned)), viewport);

            Near(width, Width(sized));
            Near((viewport - 10) / 3, Width(xs));
            Near(x, X(positioned, scene));
        });

    public static TheoryData<string, string, double> BreakpointProps()
    {
        var data = new TheoryData<string, string, double>();
        foreach (var kind in new[] { "Offset", "Push", "Pull" })
        {
            foreach (var (name, width) in new[] { ("Xs", 0d), ("Sm", 576d), ("Md", 768d), ("Lg", 992d), ("Xl", 1200d) })
            {
                data.Add(kind, name, width);
            }
        }

        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(BreakpointProps))]
    public void B7_Each_Offset_Push_And_Pull_Applies_From_Its_Breakpoint_Up(string kind, string breakpoint, double from) =>
        InstalledTheme.Run(_ =>
        {
            foreach (var viewport in from == 0 ? new[] { 390d } : new[] { from - 1, from })
            {
                var col = Col("3");
                var property = (StyledProperty<string?>)AvaloniaPropertyRegistry.Instance
                    .FindRegistered(typeof(AvonicCol), kind + breakpoint)!;
                col.SetValue(property, "3");
                using var scene = Show(Grid(Row(col)), viewport);

                var quarter = (viewport - 10) / 4;
                var expected = viewport < from ? 5 : kind == "Pull" ? 5 - quarter : 5 + quarter;
                Near(expected, X(col, scene));
            }
        });

    // ── 8. An empty value ────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(390, 126.67)]
    [InlineData(768, 758)]
    [InlineData(1200, 1190)]
    public void B8_An_Empty_Value_At_A_Breakpoint_Goes_Back_To_No_Size(double viewport, double width) => InstalledTheme.Run(_ =>
    {
        var col = new AvonicCol { Size = "4", SizeMd = "" };
        using var scene = Show(Grid(Row(col)), viewport);

        Near(width, Width(col));
    });

    // ── 9. The viewport ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_The_Breakpoint_Is_The_Viewport_Not_The_Container() => InstalledTheme.Run(_ =>
    {
        var col = new AvonicCol { Size = "12", SizeMd = "6" };
        var box = new Border { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Child = Grid(Row(col)) };
        using var scene = Show(box, 768);

        Near(145, Width(col));
    });

    // ── 10. Resizing ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_A_Change_Of_The_Viewport_Lays_The_Columns_And_The_Grid_Out_Again() => InstalledTheme.Run(_ =>
    {
        var col = new AvonicCol { Size = "12", SizeMd = "6" };
        var grid = Grid(Row(col));
        grid.Resources["Avonic.GridPaddingMd"] = 20d;
        using var scene = Show(grid);

        Near(380, Width(col));
        Assert.Equal(new Thickness(5), grid.Padding);

        scene.Window.Width = 800;
        Run();

        Near(380, Width(col));
        Assert.Equal(new Thickness(20), grid.Padding);
        Near(20, X(col, scene));

        scene.Window.Width = 390;
        Run();

        Near(380, Width(col));
        Assert.Equal(new Thickness(5), grid.Padding);
        Near(5, X(col, scene));
    });

    [AvaloniaTheory]
    [InlineData(390, 5)]
    [InlineData(576, 30)]
    [InlineData(1200, 30)]
    public void B10_A_Restyle_From_A_Breakpoint_Up_Applies_At_Every_Wider_One(double viewport, double padding) => InstalledTheme.Run(_ =>
    {
        // @media (min-width: 576px) in an app's own CSS still matches at 1200.
        var grid = Grid(Row(Col()));
        var style = new Style(x => x.OfType<AvonicGrid>().Class(":breakpoint-sm"))
        {
            Setters = { new Setter(TemplatedControl.PaddingProperty, new Thickness(30)) },
        };
        using var scene = Show(grid, viewport);
        scene.Window.Styles.Add(style);
        Run();

        Assert.Equal(new Thickness(padding), grid.Padding);
    });

    // ── 11. Odd sizes ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Fractions_Zero_Negatives_And_More_Than_Twelve() => InstalledTheme.Run(_ =>
    {
        AvonicCol half = Col("4.5"), zero = Col("0"), negative = Col("-2"), rest = Col();
        AvonicCol over = Col("13"), afterOver = Col();
        using var scene = Show(Grid(Row(half, zero, negative, rest), Row(over, afterOver)));

        Near(142.5, Width(half));
        Near(10, Width(zero));
        Near(10, Width(negative));
        Near(411.67, Width(over));
        Near(5, X(afterOver, scene));
        Near(380, Width(afterOver));
        Near(Box(over, scene.Window).Bottom, Box(afterOver, scene.Window).Y);
    });

    [AvaloniaFact]
    public void B11_A_Size_That_Is_Not_A_Number_Makes_A_Plain_Flex_Item() => InstalledTheme.Run(_ =>
    {
        AvonicCol invalid = Col("abc", "abc"), rest = Col();
        AvonicCol dot = Col("4.", "4."), afterDot = Col();
        using var scene = Show(Grid(Row(invalid, rest), Row(dot, afterDot)));

        Near(36.06, Width(invalid));
        Near(5 + Width(invalid), X(rest, scene));
        Near(380 - Width(invalid), Width(rest));
        Near(23.22, Width(dot));
        Near(356.78, Width(afterDot));
    });

    [AvaloniaFact]
    public void B11_A_Number_Can_Start_With_A_Point_Or_Have_An_Exponent() => InstalledTheme.Run(_ =>
    {
        AvonicCol point = Col(".5", ".5"), exponent = Col("1e1", "1e1");
        using var scene = Show(Grid(Row(point, exponent)));

        Near(15.83, Width(point));
        Near(316.67, Width(exponent));
    });

    // ── 12. Odd offsets ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_Odd_Offsets_And_Pushes() => InstalledTheme.Run(_ =>
    {
        var zero = new AvonicCol { Size = "2", Offset = "0" };
        var back = new AvonicCol { Size = "2", Push = "-1" };
        var far = new AvonicCol { Size = "2", Offset = "11" };
        var wrapped = Col("2");
        var invalid = new AvonicCol { Size = "2", Offset = "abc", Push = "x", Pull = "" };
        using var scene = Show(Grid(Row(zero, back), Row(far, wrapped), Row(invalid)));

        Near(5, X(zero, scene));
        Near(36.67, X(back, scene));
        Near(353.33, X(far, scene));
        Near(5, X(wrapped, scene));
        Near(Box(far, scene.Window).Bottom, Box(wrapped, scene.Window).Y);
        Near(5, X(invalid, scene));
    });

    // ── 13 to 15. Variables ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_B14_B15_The_Column_Count_And_Both_Paddings_Come_From_Resources() => InstalledTheme.Run(_ =>
    {
        var three = Col("3", "3 of 6");
        var offset = new AvonicCol { Offset = "1", Content = "off1" };
        var grid = Grid(Row(three, offset));
        grid.Resources["Avonic.GridColumns"] = 6d;
        grid.Resources["Avonic.GridPadding"] = 12d;
        grid.Resources["Avonic.GridColumnPadding"] = 2d;
        grid.Resources["Avonic.GridColumnPaddingXs"] = 7d;
        using var scene = Show(grid);

        Assert.Equal(new Thickness(12), grid.Padding);
        Assert.Equal(new Thickness(7), three.Padding);
        Near(12, X(three, scene));
        Near(183, Width(three));
        Near(256, X(offset, scene));
        Near(122, Width(offset));
    });

    [AvaloniaFact]
    public void B13_A_Change_Of_The_Column_Count_Lays_The_Columns_Out_Again() => InstalledTheme.Run(_ =>
    {
        var col = Col("3");
        var grid = Grid(Row(col));
        using var scene = Show(grid);

        Near(95, Width(col));

        grid.Resources["Avonic.GridColumns"] = 6d;
        Run();
        Near(190, Width(col));

        grid.Resources["Avonic.GridColumns"] = 4;
        Run();
        Near(285, Width(col));

        grid.Resources["Avonic.GridColumns"] = 0d;
        Run();
        Near(95, Width(col));
    });

    [AvaloniaTheory]
    [InlineData(390, 5)]
    [InlineData(768, 9)]
    [InlineData(1200, 5)]
    public void B14_Each_Breakpoint_Reads_Its_Own_Column_Padding(double viewport, double padding) => InstalledTheme.Run(_ =>
    {
        var col = Col();
        var grid = Grid(Row(col));
        grid.Resources["Avonic.GridColumnPaddingMd"] = 9d;
        using var scene = Show(grid, viewport);

        Assert.Equal(new Thickness(padding), col.Padding);
    });

    [AvaloniaFact]
    public void B14_B15_The_Defaults_Are_5() => InstalledTheme.Run(_ =>
    {
        var col = Col();
        var grid = Grid(Row(col));
        using var scene = Show(grid);

        Assert.Equal(new Thickness(5), grid.Padding);
        Assert.Equal(new Thickness(5), col.Padding);
        Near(39, grid.Bounds.Height - col.Bounds.Height + 29, 0.01);
    });

    [AvaloniaTheory]
    [InlineData(390, 7)]
    [InlineData(768, 0)]
    public void B14_A_Padding_Variable_That_Is_Not_A_Length_Is_No_Padding(double viewport, double padding) => InstalledTheme.Run(_ =>
    {
        // var() substitutes it, and padding: x is invalid at computed-value time, so it's padding: 0, not the rule below.
        var col = Col();
        var grid = Grid(Row(col));
        grid.Resources["Avonic.GridColumnPadding"] = 7d;
        grid.Resources["Avonic.GridColumnPaddingMd"] = 'x';
        using var scene = Show(grid, viewport);

        Assert.Equal(new Thickness(padding), col.Padding);
    });

    [AvaloniaFact]
    public void B14_A_Negative_Padding_Variable_Is_No_Padding() => InstalledTheme.Run(_ =>
    {
        var col = Col();
        var grid = Grid(Row(col));
        grid.Resources["Avonic.GridColumnPadding"] = -5d;
        using var scene = Show(grid);

        Assert.Equal(new Thickness(0), col.Padding);
    });

    // ── 16. Fixed ────────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(390, 0, 390)]
    [InlineData(576, 18, 540)]
    [InlineData(768, 24, 720)]
    [InlineData(992, 16, 960)]
    [InlineData(1200, 30, 1140)]
    public void B16_A_Fixed_Grid_Has_A_Width_Per_Breakpoint_And_Is_Centred(double viewport, double x, double width) =>
        InstalledTheme.Run(_ =>
        {
            var fixedGrid = new AvonicGrid { Fixed = true, Items = { Row(Col()) } };
            var plain = Grid(Row(Col()));
            using var scene = Show(new StackPanel { Children = { fixedGrid, plain } }, viewport);

            Near(x, X(fixedGrid, scene));
            Near(width, Width(fixedGrid));
            Near(viewport, Width(plain));
        });

    [AvaloniaFact]
    public void B16_The_Width_Variables_Set_It_And_It_Is_Never_Wider_Than_Its_Container() => InstalledTheme.Run(_ =>
    {
        var general = new AvonicGrid { Fixed = true, Items = { Row(Col()) } };
        general.Resources["Avonic.GridWidth"] = 300d;
        var xs = new AvonicGrid { Fixed = true, Items = { Row(Col()) } };
        xs.Resources["Avonic.GridWidth"] = 300d;
        xs.Resources["Avonic.GridWidthXs"] = 200d;
        var narrow = new AvonicGrid { Fixed = true, Items = { Row(Col()) } };
        narrow.Resources["Avonic.GridWidth"] = 300d;
        var notFixed = Grid(Row(Col()));
        notFixed.Resources["Avonic.GridWidth"] = 300d;
        var box = new Border { Width = 150, HorizontalAlignment = HorizontalAlignment.Left, Child = narrow };
        using var scene = Show(new StackPanel { Children = { general, xs, box, notFixed } });

        Near(45, X(general, scene));
        Near(300, Width(general));
        Near(95, X(xs, scene));
        Near(200, Width(xs));
        Near(150, Width(narrow));
        Near(390, Width(notFixed));
    });

    // ── 17. ion-no-padding ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_No_Padding_Takes_It_Off_The_Grid_And_Its_Columns_At_Any_Depth() => InstalledTheme.Run(_ =>
    {
        var innerCol = Col(text: "inner");
        var inner = Grid(Row(innerCol));
        var outerCol = new AvonicCol { Content = inner };
        var outer = Grid(Row(outerCol));
        outer.Classes.Add("ion-no-padding");
        using var scene = Show(outer);

        Assert.Equal(new Thickness(0), outer.Padding);
        Assert.Equal(new Thickness(0), outerCol.Padding);
        Assert.Equal(new Thickness(5), inner.Padding);
        Assert.Equal(new Thickness(0), innerCol.Padding);
        Near(5, X(innerCol, scene));
        Near(380, Width(innerCol));

        outer.Classes.Remove("ion-no-padding");
        Run();

        Assert.Equal(new Thickness(5), outer.Padding);
        Assert.Equal(new Thickness(5), outerCol.Padding);
        Assert.Equal(new Thickness(5), innerCol.Padding);
    });

    [AvaloniaTheory]
    [InlineData(390, 3)]
    [InlineData(768, 0)]
    public void B17_An_Apps_Own_Variable_On_The_Grid_Still_Wins(double viewport, double padding) => InstalledTheme.Run(_ =>
    {
        var col = Col();
        var grid = Grid(Row(col));
        grid.Classes.Add("ion-no-padding");
        grid.Resources["Avonic.GridColumnPaddingXs"] = 3d;
        using var scene = Show(grid, viewport);

        Assert.Equal(new Thickness(padding), col.Padding);
    });

    [AvaloniaFact]
    public void B17_No_Padding_Survives_Resources_Set_After_The_Class() => InstalledTheme.Run(_ =>
    {
        // XAML sets Classes before a Resources element.
        var col = Col();
        var grid = Grid(Row(col));
        grid.Classes.Add("ion-no-padding");
        grid.Resources = new ResourceDictionary { ["Avonic.GridPadding"] = 3d };
        using var scene = Show(grid);

        Assert.Equal(new Thickness(0), col.Padding);
    });

    [AvaloniaFact]
    public void B17_No_Padding_On_A_Column_Takes_Its_Own_Off() => InstalledTheme.Run(_ =>
    {
        var col = Col(text: "no padding");
        col.Classes.Add("ion-no-padding");
        using var scene = Show(Grid(Row(col)), 800);

        Assert.Equal(new Thickness(0), col.Padding);
        Near(790, Width(col));
    });

    // ── 18. Stretch ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_Columns_On_A_Line_Are_As_Tall_As_The_Tallest_Unless_They_Align_Themselves() => InstalledTheme.Run(_ =>
    {
        var block = new Border { Background = Brushes.Gray, Child = new TextBlock { Text = "block" } };
        AvonicCol tall = Tall(), stretched = new() { Content = block };
        var top = new AvonicCol { Content = "start", VerticalAlignment = VerticalAlignment.Top };
        var centre = new AvonicCol { Content = "center", VerticalAlignment = VerticalAlignment.Center };
        var other = Col(text: "short");
        var second = Row(Tall(), top, centre, other);
        using var scene = Show(Grid(Row(tall, stretched), second));

        Assert.True(tall.Bounds.Height > 50, $"{tall.Bounds.Height}");
        Near(tall.Bounds.Height, stretched.Bounds.Height, 0.01);
        Near(tall.Bounds.Height, other.Bounds.Height, 0.01);

        // What's in a column is a block, at the top, as tall as itself: 19 in a column of 67.
        Near(Box(stretched, scene.Window).Y + 5, Box(block, scene.Window).Y);
        Assert.True(block.Bounds.Height < 25, $"{block.Bounds.Height}");

        // Their own height, one line and the padding, at the top and in the middle of the line.
        Assert.True(top.Bounds.Height < 40, $"{top.Bounds.Height}");
        Near(top.Bounds.Height, centre.Bounds.Height, 0.01);
        var lineTop = Box(other, scene.Window).Y;
        Near(lineTop, Box(top, scene.Window).Y, 0.01);
        Near(lineTop + ((other.Bounds.Height - centre.Bounds.Height) / 2), Box(centre, scene.Window).Y, 0.5);
    });

    private static AvonicCol Tall() => new()
    {
        Content = new StackPanel { Children = { new TextBlock { Text = "one" }, new TextBlock { Text = "two" }, new TextBlock { Text = "three" } } },
    };

    // ── 19. Longest word ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_A_Column_With_No_Size_Is_Never_Narrower_Than_Its_Longest_Word() => InstalledTheme.Run(_ =>
    {
        AvonicCol word = Col(text: "Supercalifragilisticexpialidocious"), b = Col(text: "b"), c = Col(text: "c"), d = Col(text: "d");
        using var scene = Show(Grid(Row(word, b, c, d)));

        Near(244.11, Width(word));
        Near((380 - Width(word)) / 3, Width(b));
        Near((380 - Width(word)) / 3, Width(c));
        Near((380 - Width(word)) / 3, Width(d));
        Near(Box(word, scene.Window).Y, Box(d, scene.Window).Y);
    });

    [AvaloniaFact]
    public void B19_A_Clipping_Part_Inside_A_Column_Keeps_Its_Content_In_The_Minimum() => InstalledTheme.Run(_ =>
    {
        // overflow: hidden on a descendant doesn't change the column's min-content. A button's PART_Clip clips.
        var button = new AvonicButton { Content = "Supercalifragilistic" };
        AvonicCol col = new() { Content = button }, b = Col(text: "b"), c = Col(text: "c");
        using var scene = Show(Grid(Row(col, b, c)));

        Near(215.67, Width(col));
        Near(82.17, Width(b));
        Near(82.17, Width(c));
    });

    [AvaloniaFact]
    public void B19_A_Grid_In_A_Column_Wraps_Rather_Than_Widen_It() => InstalledTheme.Run(_ =>
    {
        AvonicCol i1 = Col(text: "alpha beta"), i2 = Col(text: "gamma delta"), i3 = Col(text: "epsilon zeta");
        var outer = new AvonicCol { Content = Grid(Row(i1, i2, i3)) };
        AvonicCol b = Col(text: "b"), c = Col(text: "c");
        using var scene = Show(Grid(Row(outer, b, c)));

        Near(126.67, Width(outer));
        Near(126.67, Width(b));
        Near(106.67, Width(i1));
        Near(15, X(i3, scene));
        Near(Box(i2, scene.Window).Bottom, Box(i3, scene.Window).Y);
    });

    // ── 20. Not a column ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B20_Something_That_Is_Not_A_Column_Is_As_Wide_As_Its_Content() => InstalledTheme.Run(_ =>
    {
        // A div's text wraps, so its content is one line and its min-content a word.
        var plain = new TextBlock { Text = "plain div", TextWrapping = TextWrapping.Wrap };
        var col = Col(text: "col");
        using var scene = Show(Grid(Row(plain, col)));

        Near(58.94, Width(plain));
        Near(5 + Width(plain), X(col, scene));
        Near(380 - Width(plain), Width(col));
    });

    // ── 21. RTL ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B21_In_RTL_Columns_Start_At_The_Right_And_Offset_Push_And_Pull_Mirror() => InstalledTheme.Run(_ =>
    {
        var both = new AvonicCol { Size = "3", Push = "3", Pull = "6" };
        var offset = new AvonicCol { Offset = "2" };
        var next = Col();
        var half = Col("4.5");
        var back = new AvonicCol { Size = "2", Push = "-1" };
        var pulled = new AvonicCol { Size = "3", Pull = "9" };
        var grid = Grid(Row(both), Row(offset, next), Row(half), Row(Col("2"), back), Row(Col("9"), pulled));
        using var scene = Show(grid);
        scene.Window.FlowDirection = FlowDirection.RightToLeft;
        Run();

        Near(195, X(both, scene));
        Near(163.33, X(offset, scene));
        Near(5, X(next, scene));
        Near(242.5, X(half, scene));
        Near(290, X(back, scene));
        Near(290, X(pulled, scene));
    });

    // ── 22. Rows in a grid ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B22_Rows_Stack_And_Fill_The_Grid_Inside_Its_Padding() => InstalledTheme.Run(_ =>
    {
        AvonicRow first = Row(Col()), second = Row(Col());
        var empty = new AvonicGrid();
        using var scene = Show(new StackPanel { Children = { Grid(first, second), empty } });

        Near(5, X(first, scene));
        Near(380, Width(first));
        Near(5, X(second, scene));
        Near(Box(first, scene.Window).Bottom, Box(second, scene.Window).Y);
        Near(10, empty.Bounds.Height);
    });

    [AvaloniaFact]
    public void B22_An_Empty_Column_With_No_Padding_Is_1_High() => InstalledTheme.Run(_ =>
    {
        var empty = new AvonicCol();
        var grid = Grid(Row(empty));
        grid.Classes.Add("ion-no-padding");
        using var scene = Show(grid);

        Assert.Equal(1, empty.Bounds.Height);
        Assert.Equal(1, grid.Bounds.Height);
    });

    // ── 23. Wrapping text, and no clipping ───────────────────────────────────────────────────

    [AvaloniaFact]
    public void B23_Text_In_A_Column_Wraps() => InstalledTheme.Run(_ =>
    {
        var wrapped = Col("6", "A sentence that is long enough to wrap in half a row");
        var other = Col();
        using var scene = Show(Grid(Row(wrapped, other)));

        Near(190, Width(wrapped));
        Assert.True(wrapped.Bounds.Height > 60, $"{wrapped.Bounds.Height}");
        Near(wrapped.Bounds.Height, other.Bounds.Height, 0.01);
    });

    [AvaloniaFact]
    public void B23_A_Column_Past_The_Row_Shows() => InstalledTheme.Run(_ =>
    {
        var over = new AvonicCol { Size = "13", Background = Brushes.Red, Content = "13" };
        var box = new Border { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Child = Grid(Row(over)) };
        using var scene = Show(box);
        scene.Window.Background = Brushes.White;
        Run();

        Assert.False(over.ClipToBounds);
        var frame = scene.Window.CaptureRenderedFrame()!;
        Assert.Equal(Colors.Red, Frames.PixelAt(frame, 310, (int)Box(over, scene.Window).Center.Y));
    });

    // ── 24. flex: 1 ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B24_In_An_Item_The_Grid_Fills_What_Is_Left() => InstalledTheme.Run(_ =>
    {
        AvonicCol a = Col(text: "a"), b = Col(text: "b");
        var grid = Grid(Row(a, b));
        using var scene = Show(new AvonicItem { Content = grid });

        Near(16, X(grid, scene), 0.5);
        Near(358, Width(grid), 0.5);
        Near(174, Width(a), 0.5);
        Near(174, Width(b), 0.5);
    });

    [AvaloniaFact]
    public void B24_In_A_Row_The_Grid_Fills_What_Is_Left() => InstalledTheme.Run(_ =>
    {
        var span = new TextBlock { Text = "span" };
        var inner = Grid(Row(Col(text: "grid in a row")));
        using var scene = Show(Grid(Row(span, inner)));

        Near(34.78, Width(span));
        Near(5 + Width(span), X(inner, scene));
        Near(380 - Width(span), Width(inner));
    });

    // ── 25. Arrow keys ───────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(PhysicalKey.ArrowRight)]
    [InlineData(PhysicalKey.ArrowDown)]
    [InlineData(PhysicalKey.ArrowLeft)]
    [InlineData(PhysicalKey.ArrowUp)]
    public void B25_Arrow_Keys_Do_Not_Move_Focus(PhysicalKey key) => InstalledTheme.Run(_ =>
    {
        AvonicButton one = new() { Content = "One" }, two = new() { Content = "Two" }, three = new() { Content = "Three" };
        var middle = new AvonicCol { Content = two };
        using var scene = Show(Grid(Row(new AvonicCol { Content = one }, middle), Row(new AvonicCol { Content = three })));

        two.Focus();
        Run();
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        Run();

        Assert.True(two.IsFocused);
    });

    [AvaloniaTheory]
    [InlineData(PhysicalKey.ArrowDown)]
    [InlineData(PhysicalKey.ArrowUp)]
    public void B25_Arrow_Keys_Do_Not_Move_Focus_Between_Controls_Directly_In_A_Grid(PhysicalKey key) => InstalledTheme.Run(_ =>
    {
        AvonicButton one = new() { Content = "One" }, two = new() { Content = "Two" }, three = new() { Content = "Three" };
        using var scene = Show(Grid(one, two, three));

        two.Focus();
        Run();
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        Run();

        Assert.True(two.IsFocused);
    });

    // ── 26. Automation ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B26_A_Grid_Row_And_Column_Have_No_Role() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Inside" };
        var col = new AvonicCol { Content = button };
        var row = Row(col);
        var grid = Grid(row);
        using var scene = Show(grid);

        foreach (var control in new Control[] { grid, row, col })
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(control);
            Assert.NotEqual(AutomationControlType.List, peer.GetAutomationControlType());
            Assert.NotEqual(AutomationControlType.ListItem, peer.GetAutomationControlType());
            Assert.False(peer.IsControlElement());
        }

        Assert.Contains(ControlAutomationPeer.CreatePeerForElement(button), Descendants(ControlAutomationPeer.CreatePeerForElement(scene.Window)));
    });

    private static IEnumerable<AutomationPeer> Descendants(AutomationPeer peer)
    {
        foreach (var child in peer.GetChildren())
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    // ── Without the theme ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Layout_Needs_No_Theme()
    {
        AvonicCol six = Col("6"), rest = Col();
        using var scene = Show(Grid(Row(six, rest)));

        Near(0, X(six, scene));
        Near(195, Width(six));
        Near(195, Width(rest));
    }
}
