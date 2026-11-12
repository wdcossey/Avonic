using Avalonia.Media;
using System.Globalization;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Tests.Behaviors;

public class CssTests
{
    private static object? Convert(Avalonia.Data.Converters.IMultiValueConverter converter, object? parameter, params object?[] values) =>
        converter.Convert(values, typeof(object), parameter, CultureInfo.InvariantCulture);

    [Fact]
    public void TranslateScale_Is_A_Fraction_Of_The_Height_And_Then_A_Scale()
    {
        var matrix = Assert.IsType<Avalonia.Media.Transformation.TransformOperations>(
            Convert(Css.TranslateScale, null, 40d, 0.5, 0.75)).Value;

        // translateY(50%) scale(0.75): the translate isn't scaled.
        Assert.Equal(0.75, matrix.M11, 3);
        Assert.Equal(0.75, matrix.M22, 3);
        Assert.Equal(0, matrix.M31, 3);
        Assert.Equal(20, matrix.M32, 3);
    }

    [Fact]
    public void TranslateScale_Needs_A_Height_And_Both_Values()
    {
        Assert.Same(AvaloniaProperty.UnsetValue, Convert(Css.TranslateScale, null, null, 0.5, 0.75));
        Assert.Same(AvaloniaProperty.UnsetValue, Convert(Css.TranslateScale, null, 1d, 0.5));
    }

    [Theory]
    [InlineData("Start", FlowDirection.LeftToRight, 11, 2, 3, 4)]
    [InlineData("End", FlowDirection.LeftToRight, 1, 2, 23, 4)]
    [InlineData("Both", FlowDirection.LeftToRight, 11, 2, 23, 4)]
    [InlineData("Start", FlowDirection.RightToLeft, 21, 2, 3, 4)]
    [InlineData("End", FlowDirection.RightToLeft, 1, 2, 13, 4)]
    [InlineData("Both", FlowDirection.RightToLeft, 21, 2, 13, 4)]
    public void SafeAreaBox_Adds_At_The_Side_It_Is_Told_And_Swaps_In_RTL(string side, FlowDirection flow, double start, double top, double end, double bottom) =>
        Assert.Equal(new Thickness(start, top, end, bottom), Convert(Css.SafeAreaBox, side, 1d, 2d, 3d, 4d, 10d, 20d, flow));

    [Fact]
    public void SafeAreaBox_Needs_All_Its_Values() =>
        Assert.Same(AvaloniaProperty.UnsetValue, Convert(Css.SafeAreaBox, "Start", 1d, 2d, 3d, 4d));

    [Fact]
    public void Var_Takes_The_First_Value_That_Is_Set() =>
        Assert.Equal("c", Convert(Css.Var, null, new CssUnset(), null, AvaloniaProperty.UnsetValue, "c", "d"));

    [Fact]
    public void Var_Stops_At_A_Set_First_Value() =>
        Assert.Equal("a", Convert(Css.Var, null, "a", "b"));

    [Fact]
    public void Var_Keeps_A_Falsy_Value() =>
        Assert.Equal(0d, Convert(Css.Var, null, 0d, 1d));

    [Fact]
    public void Var_With_Nothing_Set_Is_Unset() =>
        Assert.Same(AvaloniaProperty.UnsetValue, Convert(Css.Var, null, new CssUnset(), null));

    [Fact]
    public void Em_Multiplies_By_A_Value() =>
        Assert.Equal(20d, Convert(Css.Em, null, 16d, 1.25));

    [Fact]
    public void Em_Multiplies_By_The_Parameter() =>
        Assert.Equal(24d, Convert(Css.Em, "1.5", 16d));

    [Fact]
    public void Em_Without_A_Size_Or_Factor_Is_Unset()
    {
        Assert.Same(AvaloniaProperty.UnsetValue, Convert(Css.Em, null, 16d));
        Assert.Same(AvaloniaProperty.UnsetValue, Convert(Css.Em, "1.5", AvaloniaProperty.UnsetValue));
    }

    [Fact]
    public void Box_Orders_Start_Top_End_Bottom() =>
        Assert.Equal(new Thickness(1, 2, 3, 4), Convert(Css.Box, null, 1d, 2d, 3d, 4d));

    [Fact]
    public void Box_With_A_Missing_Value_Is_Unset() =>
        Assert.Same(AvaloniaProperty.UnsetValue, Convert(Css.Box, null, 1d, 2d, 3d, AvaloniaProperty.UnsetValue));

    [Theory]
    [InlineData("Start", -18, 0, 0, 0)]
    [InlineData("Top", 0, -18, 0, 0)]
    [InlineData("End", 0, 0, -18, 0)]
    [InlineData("Bottom", 0, 0, 0, -18)]
    public void Negate_Puts_The_First_Set_Value_Negated_On_The_Side_It_Is_Told(string side, double start, double top, double end, double bottom) =>
        Assert.Equal(
            new Thickness(start, top, end, bottom),
            Css.Negate.Convert([new CssUnset(), null, 18d, 5d], typeof(Thickness), side, CultureInfo.InvariantCulture));

    [Fact]
    public void Negate_Gives_A_Number_To_A_Number() =>
        Assert.Equal(-7.5, Css.Negate.Convert([AvaloniaProperty.UnsetValue, 7.5], typeof(double), null, CultureInfo.InvariantCulture));

    [Fact]
    public void Negate_Of_Nothing_Or_Of_Something_That_Is_Not_A_Finite_Number_Is_Unset()
    {
        Assert.Same(AvaloniaProperty.UnsetValue, Css.Negate.Convert([new CssUnset(), null], typeof(Thickness), "Start", CultureInfo.InvariantCulture));
        // calc(0px - var(--a)) with --a not a length is invalid, even with a number after it, as var() takes the first that's set.
        Assert.Same(AvaloniaProperty.UnsetValue, Css.Negate.Convert(["auto", 18d], typeof(double), null, CultureInfo.InvariantCulture));
        Assert.Same(AvaloniaProperty.UnsetValue, Css.Negate.Convert([double.NaN], typeof(double), null, CultureInfo.InvariantCulture));
        // A Thickness with no side named.
        Assert.Same(AvaloniaProperty.UnsetValue, Css.Negate.Convert([18d], typeof(Thickness), null, CultureInfo.InvariantCulture));
    }

    // Avalonia facts: a brush is an AvaloniaObject, and making one on a test thread claims the dispatcher before the headless
    // session can, which fails the session's first test.
    [AvaloniaFact]
    public void Rgba_Takes_The_First_Set_Colour_At_The_Alpha_Rounded_To_A_Byte() =>
        // 0.065 × 255 = 16.575, which Chromium rounds to 17.
        Assert.Equal(Color.FromArgb(17, 1, 2, 3), Convert(Css.Rgba, "0.065", new CssUnset(), null, AvaloniaProperty.UnsetValue, Color.FromRgb(1, 2, 3), Colors.Red));

    [AvaloniaFact]
    public void Rgba_Reads_A_Solid_Brush_And_Replaces_Its_Alpha() =>
        Assert.Equal(Color.FromArgb(51, 10, 20, 30), Convert(Css.Rgba, "0.2", new SolidColorBrush(Color.FromArgb(200, 10, 20, 30), 0.5)));

    [AvaloniaFact]
    public void Rgba_Without_A_Parameter_Is_Opaque() =>
        Assert.Equal(Color.FromArgb(255, 1, 2, 3), Convert(Css.Rgba, null, Color.FromArgb(10, 1, 2, 3)));

    [AvaloniaFact]
    public void Rgba_Clamps_The_Alpha() =>
        Assert.Equal(Color.FromArgb(255, 1, 2, 3), Convert(Css.Rgba, "1.5", Color.FromRgb(1, 2, 3)));

    [AvaloniaFact]
    public void Rgba_Gives_A_Brush_To_A_Brush()
    {
        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(
            Css.Rgba.Convert([Color.FromRgb(1, 2, 3)], typeof(IBrush), "0.5", CultureInfo.InvariantCulture));

        Assert.Equal(Color.FromArgb(128, 1, 2, 3), brush.Color);
        Assert.Equal(1, brush.Opacity);
    }

    [AvaloniaFact]
    public void Rgba_Of_Something_That_Is_Not_A_Colour_Is_Unset()
    {
        // rgba(var(--a), n) with --a a gradient is invalid, even with a colour after it, as var() takes the first that's set.
        Assert.Same(AvaloniaProperty.UnsetValue, Convert(Css.Rgba, "0.5", new LinearGradientBrush(), Color.FromRgb(1, 2, 3)));
        Assert.Same(AvaloniaProperty.UnsetValue, Convert(Css.Rgba, "0.5", new CssUnset(), null));
    }

    [Fact]
    public void IsSet_Is_False_For_Null_Unset_And_CssUnset_Only()
    {
        Assert.False(Css.IsSet(null));
        Assert.False(Css.IsSet(AvaloniaProperty.UnsetValue));
        Assert.False(Css.IsSet(new CssUnset()));
        Assert.True(Css.IsSet(0d));
        Assert.True(Css.IsSet(string.Empty));
    }
}
