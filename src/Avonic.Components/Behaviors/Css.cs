using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Avonic.Components.Behaviors;

/// <summary>
/// CSS values Avalonia has no equivalent for, used in themes through a <c>MultiBinding</c>, whose values
/// can be <c>{DynamicResource}</c>. See <c>docs/components/ion-item/PLAN.md</c>.
/// </summary>
public static class Css
{
    /// <summary>
    /// <c>var(--a, var(--b, c))</c>: the first value that's set. For <c>currentColor</c>, the last
    /// value is the host's <c>Foreground</c>. An invalid variable counts as unset, as it does in CSS.
    /// </summary>
    public static readonly IMultiValueConverter Var = new VarConverter();

    /// <summary>
    /// <c>1.25em</c>: the first value is the font size, and the factor is the second value or the
    /// <c>ConverterParameter</c>.
    /// </summary>
    public static readonly IMultiValueConverter Em = new EmConverter();

    /// <summary>Four padding variables, in the order start, top, end, bottom, as a <see cref="Thickness"/>.</summary>
    public static readonly IMultiValueConverter Box = new BoxConverter();

    /// <summary>
    /// <c>calc(var(--padding-start) + var(--ion-safe-area-left, 0px))</c>: as <see cref="Box"/>, then
    /// <c>Avonic.SafeAreaLeft</c>, <c>Avonic.SafeAreaRight</c> and the <c>FlowDirection</c>, which swaps them. The
    /// <c>ConverterParameter</c> says where they're added: <c>Start</c>, <c>End</c> or <c>Both</c>. A <see cref="Thickness"/>
    /// mirrors with the flow, so its first value is the start edge in both directions.
    /// </summary>
    public static readonly IMultiValueConverter SafeAreaBox = new SafeAreaBoxConverter();

    /// <summary>
    /// <c>line-height: 1</c>. CSS splits a line's leading above and below the text, and Avalonia keeps a line with a
    /// smaller <c>LineHeight</c> at the top, which puts the text about 3 px low in a 36 px button. So this moves the
    /// text up by half the leading, without changing its box. The values are the <c>TextBlock</c>'s font family,
    /// weight, size and line height.
    /// </summary>
    public static readonly IMultiValueConverter HalfLeading = new HalfLeadingConverter();

    /// <summary>The percentage in <c>translateY(50%)</c>, as a fraction such as <c>0.5</c>. A style can ease it.</summary>
    public static readonly AttachedProperty<double> TranslateYProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, double>("TranslateY", typeof(Css));

    /// <summary>The factor in <c>scale(0.75)</c>. A style can ease it.</summary>
    public static readonly AttachedProperty<double> ScaleProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, double>("Scale", typeof(Css), 1);

    /// <summary>
    /// <c>translateY(50%) scale(0.75)</c>: the values are the element's height, which is what the
    /// percentage is of, then <see cref="TranslateYProperty"/> and <see cref="ScaleProperty"/>. Ease those
    /// two, not the result: in CSS a change of size moves the element at once, and only a change of the
    /// percentage eases.
    /// </summary>
    public static readonly IMultiValueConverter TranslateScale = new TranslateScaleConverter();


    private static bool IsSet(object? value) =>
        value is not (null or CssUnset) && !ReferenceEquals(value, AvaloniaProperty.UnsetValue);

    private sealed class TranslateScaleConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not [double height, double fraction, double scale])
            {
                return AvaloniaProperty.UnsetValue;
            }

            var builder = TransformOperations.CreateBuilder(2);
            builder.AppendScale(scale, scale);
            builder.AppendTranslate(0, height * fraction);
            return builder.Build();
        }
    }

    private sealed class VarConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (IsSet(value))
                {
                    return value;
                }
            }

            return AvaloniaProperty.UnsetValue;
        }
    }

    private sealed class EmConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var factor = values.Count > 1 ? values[1] : parameter;
            return values.Count > 0 && values[0] is double size && IsSet(factor)
                ? size * System.Convert.ToDouble(factor, CultureInfo.InvariantCulture)
                : AvaloniaProperty.UnsetValue;
        }
    }

    private sealed class SafeAreaBoxConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not [double start, double top, double end, double bottom, double left, double right, FlowDirection flow])
            {
                return AvaloniaProperty.UnsetValue;
            }

            var rtl = flow == FlowDirection.RightToLeft;
            var side = parameter as string;
            return new Thickness(
                start + (side is "Start" or "Both" ? (rtl ? right : left) : 0),
                top,
                end + (side is "End" or "Both" ? (rtl ? left : right) : 0),
                bottom);
        }
    }

    private sealed class HalfLeadingConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not [FontFamily family, FontWeight weight, double fontSize, double lineHeight] || double.IsNaN(lineHeight))
            {
                return AvaloniaProperty.UnsetValue;
            }

            var metrics = new Typeface(family, weight: weight).GlyphTypeface.Metrics;
            var natural = (double)metrics.LineSpacing / metrics.DesignEmHeight * fontSize;
            var halfLeading = (natural - lineHeight) / 2;
            return new Thickness(0, -halfLeading, 0, halfLeading);
        }
    }

    private sealed class BoxConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [double start, double top, double end, double bottom]
                ? new Thickness(start, top, end, bottom)
                : AvaloniaProperty.UnsetValue;
    }
}

/// <summary>
/// A variable that isn't set: the value of a token that falls back to something else in Ionic. An Avonic
/// key always has a value, so this is what lets <see cref="Css.Var"/> move on.
/// </summary>
public sealed class CssUnset;
