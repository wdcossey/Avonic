using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
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
