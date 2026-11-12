using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// The converters the themes of <see cref="AvonicInput"/>, <see cref="AvonicTextarea"/> and <see cref="AvonicSearchbar"/> read: a
/// field's padding, borders, corners and notch from its CSS variables, and its default icons. Not in Ionic's API.
/// </summary>
public static class FieldConverters
{
    /// <summary>The value, or else the <c>ConverterParameter</c>: a default such as md's clear icon.</summary>
    public static readonly IValueConverter Or =
        new FuncValueConverter<object?, object?, object?>((value, parameter) => value is null or "" ? parameter : value);

    /// <summary>
    /// The searchbar's cancel icon's <c>flipRtl</c>: false for the default, which Ionic imports as SVG with no name to flip by,
    /// else the icon's own rule.
    /// </summary>
    public static readonly IValueConverter FlipUnlessDefault =
        new FuncValueConverter<object?, bool?>(value => value is null or "" ? false : null);

    /// <summary>The textarea's placement as <see cref="InputWrapperPanel"/> takes it, which lays out the label the same five ways.</summary>
    public static readonly IValueConverter TextareaPlacement =
        new FuncValueConverter<TextareaLabelPlacement, InputLabelPlacement>(LabelledTextField.Placement);

    /// <summary>
    /// The wrapper's padding: the values are <c>--padding-start</c>, <c>-top</c>, <c>-end</c> and <c>-bottom</c>, then the fill's
    /// own start and end default for an unset start or end. An unset top or bottom is 0.
    /// </summary>
    public static readonly IMultiValueConverter Padding =
        new FuncMultiValueConverter<object?, Thickness>(values =>
        {
            var list = values.ToList();
            var fallback = Number(list.ElementAtOrDefault(4));
            return new Thickness(
                NumberOr(list.ElementAtOrDefault(0), fallback),
                Number(list.ElementAtOrDefault(1)),
                NumberOr(list.ElementAtOrDefault(2), fallback),
                Number(list.ElementAtOrDefault(3)));
        });

    /// <summary>
    /// The textarea's <c>padding-top: var(--padding-top)</c> and <c>padding-bottom: var(--padding-bottom)</c>, on the field, a
    /// row's label and the slots. The values are the two variables, then the default for an unset bottom, then for an unset top
    /// (0 if there's none, as in a column). The <c>ConverterParameter</c> is what's added above: a slotted child's
    /// <c>margin-top</c> in a column.
    /// </summary>
    public static readonly IMultiValueConverter VerticalPadding = new VerticalPaddingConverter();

    /// <summary>
    /// <c>border-radius</c>: the first value that's set, a number or a <see cref="CornerRadius"/>, on the corners the
    /// <c>ConverterParameter</c> names: <c>Top</c> (solid's), <c>Start</c> or <c>End</c> (the outline's ends), or all of them.
    /// </summary>
    public static readonly IMultiValueConverter Radius = new RadiusConverter();

    /// <summary>
    /// A border of the width in the value on the sides the <c>ConverterParameter</c> names, from <c>s</c>tart, <c>t</c>op,
    /// <c>e</c>nd and <c>b</c>ottom, such as <c>stb</c> for the outline's start.
    /// </summary>
    public static readonly IMultiValueConverter Sides = new SidesConverter();

    /// <summary>
    /// <c>.input-bottom</c>'s padding: the values are <c>--padding-start</c> and <c>--padding-end</c>, then the fill's own default
    /// for either that's unset. The <c>ConverterParameter</c> is the padding above; there's none below.
    /// </summary>
    public static readonly IMultiValueConverter BottomPadding = new BottomPaddingConverter();

    /// <summary><c>calc(var(--padding-start) - 4px)</c>: the outline's start. The values are the variable and the fill's default.</summary>
    public static readonly IMultiValueConverter OutlineStartWidth =
        new FuncMultiValueConverter<object?, double>(values =>
        {
            var list = values.ToList();
            return Math.Max(0, NumberOr(list.ElementAtOrDefault(0), Number(list.ElementAtOrDefault(1))) - 4);
        });

    /// <summary>
    /// The notch for a slotted label: <c>createNotchController</c> sets the spacer's width to the label's width at 0.75. The
    /// values are the field's <c>Label</c>, which leaves the spacer to its own text when it's set, and the slotted label's width.
    /// </summary>
    public static readonly IMultiValueConverter NotchWidth =
        new FuncMultiValueConverter<object?, double>(values =>
        {
            var list = values.ToList();
            return list.ElementAtOrDefault(0) is string ? double.NaN : Number(list.ElementAtOrDefault(1)) * 0.75;
        });

    private static double Number(object? value) => value is double number && double.IsFinite(number) ? number : 0;

    private static double NumberOr(object? value, double fallback) =>
        value is double number && double.IsFinite(number) ? number : fallback;

    private static double Parameter(object? parameter) =>
        double.TryParse(parameter as string, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;

    private sealed class VerticalPaddingConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var extra = Parameter(parameter);
            var top = NumberOr(values.ElementAtOrDefault(0), Number(values.ElementAtOrDefault(3)));
            var bottom = NumberOr(values.ElementAtOrDefault(1), Number(values.ElementAtOrDefault(2)));
            return new Thickness(0, top + extra, 0, bottom);
        }
    }

    private sealed class SidesConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var width = Number(values.FirstOrDefault());
            var sides = parameter as string ?? "";
            return new Thickness(
                sides.Contains('s') ? width : 0, sides.Contains('t') ? width : 0,
                sides.Contains('e') ? width : 0, sides.Contains('b') ? width : 0);
        }
    }

    private sealed class BottomPaddingConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var fallback = Number(values.ElementAtOrDefault(2));
            var top = Parameter(parameter);
            return new Thickness(NumberOr(values.ElementAtOrDefault(0), fallback), top, NumberOr(values.ElementAtOrDefault(1), fallback), 0);
        }
    }

    private sealed class RadiusConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // The extras: the element's Bounds and FlowDirection, for a CSS string's percentages.
            var extras = values.Where(value => value is Rect or Size or FlowDirection).ToList();
            var r = values.FirstOrDefault(value => value is double or CornerRadius or string) switch
            {
                double number => new CornerRadius(number),
                CornerRadius radius => radius,
                string css => Behaviors.BorderRadius.ToCornerRadius.Convert([css, .. extras], typeof(CornerRadius), null, culture)
                    is CornerRadius parsed ? parsed : default,
                _ => default,
            };
            return parameter switch
            {
                "Top" => new CornerRadius(r.TopLeft, r.TopRight, 0, 0),
                "Start" => new CornerRadius(r.TopLeft, 0, 0, r.BottomLeft),
                "End" => new CornerRadius(0, r.TopRight, r.BottomRight, 0),
                _ => r,
            };
        }
    }
}
