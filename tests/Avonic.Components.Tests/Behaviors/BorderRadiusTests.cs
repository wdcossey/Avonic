using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Tests.Behaviors;

public class BorderRadiusTests
{
    private static object? VarToCornerRadius(params object?[] values) =>
        BorderRadius.VarToCornerRadius.Convert(values, typeof(CornerRadius), null, CultureInfo.InvariantCulture);

    [Fact]
    public void VarToCornerRadius_Takes_The_First_Value_That_Is_Set() =>
        // var(--border-radius, 8px): the app's key unset, so the shape's.
        Assert.Equal(new CornerRadius(8), VarToCornerRadius(new CssUnset(), null, AvaloniaProperty.UnsetValue, 8d, 4d, new Rect(0, 0, 48, 56), FlowDirection.LeftToRight));

    [Fact]
    public void VarToCornerRadius_Stops_At_A_Set_First_Value_And_Reads_Its_Percentage_Of_The_Bounds() =>
        // 50% of a 40 × 40 box, though the fallback says 8.
        Assert.Equal(new CornerRadius(20), VarToCornerRadius("50%", 8d, new Rect(0, 0, 40, 40), FlowDirection.LeftToRight));

    [Fact]
    public void VarToCornerRadius_Swaps_The_Corners_In_RTL() =>
        Assert.Equal(new CornerRadius(2, 1, 4, 3), VarToCornerRadius(new CssUnset(), "1px 2px 3px 4px", new Rect(0, 0, 40, 40), FlowDirection.RightToLeft));

    [Fact]
    public void VarToCornerRadius_With_Nothing_Set_Is_Zero() =>
        Assert.Equal(new CornerRadius(0), VarToCornerRadius(new CssUnset(), null, new Rect(0, 0, 40, 40), FlowDirection.LeftToRight));

    [Fact]
    public void VarToCornerRadius_Needs_A_Value_And_The_Bounds_And_The_Direction() =>
        Assert.Same(AvaloniaProperty.UnsetValue, VarToCornerRadius(8d, new Rect(0, 0, 40, 40)));
}
