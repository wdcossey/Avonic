using Avalonia;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Each form of the SVG path grammar has to give the same shape as its explicit spelling. Bounds are
/// enough to tell them apart here: each pair is chosen so a wrong reading changes them.
/// </summary>
public class SvgPathTests
{
    private static Rect Bounds(string data) => SvgPath.Parse(data, evenOdd: false)?.Bounds ?? default;

    private static void AssertSame(string data, string explicitForm)
    {
        var actual = Bounds(data);
        var expected = Bounds(explicitForm);
        Assert.True(actual != default, $"{data} drew nothing");
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
        Assert.Equal(expected.Width, actual.Width, 3);
        Assert.Equal(expected.Height, actual.Height, 3);
    }

    // ── Smooth curves ────────────────────────────────────────────────────────────────────────

    // The bug in StreamGeometry.Parse: it reflects the control point from before the line.
    [AvaloniaFact]
    public void A_Smooth_Curve_After_A_Line_Starts_At_The_Current_Point() =>
        AssertSame("M0 0C0 900 100 900 100 0L200 0S300 100 400 0", "M0 0C0 900 100 900 100 0L200 0C200 0 300 100 400 0");

    [AvaloniaFact]
    public void A_Smooth_Curve_After_A_Moveto_Starts_At_The_Current_Point() =>
        AssertSame("M0 0C0 900 100 900 100 0M200 0S300 100 400 0", "M0 0C0 900 100 900 100 0M200 0C200 0 300 100 400 0");

    [AvaloniaFact]
    public void A_Smooth_Curve_After_A_Curve_Reflects_Its_Control_Point() =>
        AssertSame("M0 0C0 100 100 100 100 0S200 -100 200 0", "M0 0C0 100 100 100 100 0C100 -100 200 -100 200 0");

    [AvaloniaFact]
    public void A_Smooth_Curve_After_A_Smooth_Curve_Reflects_Its_Control_Point() =>
        AssertSame("M0 0C0 50 50 50 50 0S100 -50 100 0S150 50 150 0", "M0 0C0 50 50 50 50 0C50 -50 100 -50 100 0C100 50 150 50 150 0");

    [AvaloniaFact]
    public void A_Smooth_Quadratic_After_A_Line_Starts_At_The_Current_Point() =>
        AssertSame("M0 0Q50 900 100 0L200 0T300 0", "M0 0Q50 900 100 0L200 0Q200 0 300 0");

    [AvaloniaFact]
    public void A_Smooth_Quadratic_After_A_Quadratic_Reflects_Its_Control_Point() =>
        AssertSame("M0 0Q50 100 100 0T200 0", "M0 0Q50 100 100 0Q150 -100 200 0");

    [AvaloniaFact]
    public void A_Smooth_Quadratic_After_A_Smooth_Quadratic_Reflects_Again() =>
        AssertSame("M0 0Q50 100 100 0T200 0T300 0", "M0 0Q50 100 100 0Q150 -100 200 0Q250 100 300 0");

    [AvaloniaFact]
    public void A_Smooth_Cubic_After_A_Quadratic_Starts_At_The_Current_Point() =>
        AssertSame("M0 0Q50 900 100 0S200 100 300 0", "M0 0Q50 900 100 0C100 0 200 100 300 0");

    [AvaloniaFact]
    public void A_Smooth_Quadratic_After_A_Cubic_Starts_At_The_Current_Point() =>
        AssertSame("M0 0C0 900 100 900 100 0T300 0", "M0 0C0 900 100 900 100 0Q100 0 300 0");

    [AvaloniaFact]
    public void A_Smooth_Cubic_After_A_Quadratic_And_A_Line_Starts_At_The_Current_Point() =>
        AssertSame("M0 0Q50 900 100 0L200 0S300 100 400 0", "M0 0Q50 900 100 0L200 0C200 0 300 100 400 0");

    // ── Relative and shorthand forms ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Relative_Commands_Are_From_The_Current_Point() =>
        AssertSame("M100 100l10 20h30v40c10 0 20 10 20 20s10 20 20 20q5 5 10 0t10 0", "M100 100L110 120H140V160C150 160 160 170 160 180S170 200 180 200Q185 205 190 200T200 200");

    [AvaloniaFact]
    public void A_Leading_Relative_Moveto_Is_Absolute() => AssertSame("m100 100L200 200", "M100 100L200 200");

    [AvaloniaFact]
    public void Coordinates_After_A_Moveto_Are_Linetos() => AssertSame("M0 0 100 100 200 0", "M0 0L100 100L200 0");

    [AvaloniaFact]
    public void Coordinates_After_A_Relative_Moveto_Are_Relative_Linetos() => AssertSame("m10 10 100 100 100-100", "M10 10L110 110L210 10");

    [AvaloniaFact]
    public void A_Command_Repeats_With_More_Coordinates() =>
        AssertSame("M0 0L10 10 20 0H30 40V10 20C1 2 3 4 5 6 7 8 9 10 11 12", "M0 0L10 10L20 0H30H40V10V20C1 2 3 4 5 6C7 8 9 10 11 12");

    [AvaloniaFact]
    public void Closepath_Returns_To_The_Subpath_Start() => AssertSame("M100 100H200V200Zl50 50", "M100 100H200V200Z M100 100L150 150");

    [AvaloniaFact]
    public void A_Lowercase_Closepath_Also_Closes() => AssertSame("M100 100H200V200zl50 50", "M100 100H200V200Z M100 100L150 150");

    // ── Arcs ─────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void An_Arc_Draws_A_Half_Circle()
    {
        var bounds = Bounds("M0 0A50 50 0 0 1 100 0");

        Assert.Equal(100, bounds.Width, 1);
        Assert.Equal(50, bounds.Height, 1);
    }

    // Ionicons' own minified form: a1 1 0 01.5.5
    [AvaloniaFact]
    public void Arc_Flags_Can_Run_Together() => AssertSame("M0 0a50 50 0 01100 0", "M0 0A50 50 0 0 1 100 0");

    [AvaloniaFact]
    public void The_Sweep_Flag_Picks_The_Side()
    {
        Assert.True(Bounds("M0 0A50 50 0 0 1 100 0").Y < 0 != Bounds("M0 0A50 50 0 0 0 100 0").Y < 0);
    }

    [AvaloniaFact]
    public void An_Arc_With_A_Zero_Radius_Is_A_Line() => AssertSame("M0 0A0 50 0 0 1 100 100", "M0 0L100 100");

    [AvaloniaFact]
    public void An_Arc_To_The_Same_Point_Is_Omitted() => AssertSame("M0 0L10 10A50 50 0 0 1 10 10", "M0 0L10 10");

    // ── Numbers ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData("M10-20L30-40", "M10 -20L30 -40")]
    [InlineData("M.5.5L1.5.5", "M0.5 0.5L1.5 0.5")]
    [InlineData("M1,2L3,4", "M1 2L3 4")]
    [InlineData("M 1 , 2 L 3 , 4", "M1 2L3 4")]
    public void Numbers_Are_Read_As_In_Svg(string data, string explicitForm) => AssertSame(data, explicitForm);

    // An exponent's e isn't a command, so it can't count as the command before a smooth curve.
    [AvaloniaTheory]
    [InlineData("M0 0C1e1 2 3 4 5 6S7 8 9 10")]
    [InlineData("M0 0C1E1 2 3 4 5 6S7 8 9 10")]
    [InlineData("M0 0Q1e1 2 3 4T5 6")]
    public void An_Exponent_Is_Not_A_Command(string data) => Assert.Equal(data, SvgPath.Prepare(data));

    // Here the reflection is what SVG asks for, so a reset would be wrong.
    [AvaloniaTheory]
    [InlineData("M0 0C1 1 2 2 3 3S4 4 5 5")]
    [InlineData("M0 0c1 1 2 2 3 3s4 4 5 5")]
    [InlineData("M0 0C1 1 2 2 3 3S4 4 5 5S6 6 7 7")]
    [InlineData("M0 0c1 1 2 2 3 3s4 4 5 5s6 6 7 7")]
    [InlineData("M0 0Q1 1 2 2T3 3")]
    [InlineData("M0 0q1 1 2 2t3 3")]
    [InlineData("M0 0Q1 1 2 2T3 3T4 4")]
    [InlineData("M0 0q1 1 2 2t3 3t4 4")]
    public void A_Smooth_Curve_Straight_After_Its_Own_Kind_Is_Unchanged(string data) => Assert.Equal(data, SvgPath.Prepare(data));

    // Nothing has gone stale yet, so the path stays as it is.
    [AvaloniaTheory]
    [InlineData("M0 0L1 1S2 2 3 3")]
    [InlineData("M0 0S2 2 3 3")]
    [InlineData("M0 0L1 1T2 2")]
    public void A_Smooth_Curve_With_No_Earlier_Curve_Is_Unchanged(string data) => Assert.Equal(data, SvgPath.Prepare(data));

    [AvaloniaFact]
    public void Data_That_Needs_No_Change_Is_Unchanged() => Assert.Equal("M0 0C1 2 3 4 5 6S7 8 9 10Z", SvgPath.Prepare("M0 0C1 2 3 4 5 6S7 8 9 10Z"));

    // ── Errors: known differences from a browser, in custom SVGs ─────────────────────────────

    // A browser keeps the path up to the error. Avalonia's parser gives nothing back, so this draws nothing.
    [AvaloniaTheory]
    [InlineData("M0 0L100 100X200 200")]
    [InlineData("M0 0L100 100L200")]
    [InlineData("M0 0L100 100C1 2")]
    [InlineData("M0 0L100 100A1 1 0 2 0 5 5")]
    [InlineData("M1e")] // IndexOutOfRangeException
    public void An_Error_Draws_Nothing_And_Is_Not_Fatal(string data) => Assert.Null(SvgPath.Parse(data, evenOdd: false));

    // Valid SVG that Avalonia's parser rejects. None of the bundled icons use these.
    [AvaloniaTheory]
    [InlineData("M+1 +2L+3 +4")]
    [InlineData("M1e2 1E2L2e+2 2e2")]
    public void Some_Valid_Numbers_Draw_Nothing(string data) => Assert.Null(SvgPath.Parse(data, evenOdd: false));

    [AvaloniaTheory]
    [InlineData("L100 100")]
    [InlineData("100 100")]
    [InlineData("not a path")]
    [InlineData("Z")]
    [InlineData("Z5")]
    public void A_Path_Without_A_Leading_Moveto_Draws_Nothing(string data) => Assert.Equal(default, Bounds(data));

    // These make StreamGeometry.Parse loop forever, so a regression hangs here rather than failing.
    [AvaloniaTheory]
    [InlineData("M0 0L10 10Z 50 50", "M0 0L10 10Z")]
    [InlineData("M0 0L10 10Z5", "M0 0L10 10Z")]
    [InlineData("M0 0L10 10z5 5", "M0 0L10 10z")]
    [InlineData("M0 0L10 10Z,5", "M0 0L10 10Z")]
    [InlineData("M0 0L10 10Z-5", "M0 0L10 10Z")]
    [InlineData("M0 0L10 10Z.5", "M0 0L10 10Z")]
    public void Numbers_After_A_Closepath_End_The_Path(string data, string expected) => Assert.Equal(expected, SvgPath.Prepare(data));

    // The reset has to survive the path being cut short.
    [AvaloniaFact]
    public void Ending_At_A_Closepath_Keeps_Earlier_Resets() =>
        Assert.Equal("M0 0C1 1 2 2 3 3L1 1c0 0 0 0 0 0S2 2 3 3Z", SvgPath.Prepare("M0 0C1 1 2 2 3 3L1 1S2 2 3 3Z 5 5"));
}
