using System.Globalization;
using Avalonia;
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
}
