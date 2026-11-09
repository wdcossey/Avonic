using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>Values the grid's themes need that <see cref="Css"/> doesn't have.</summary>
public static class GridCss
{
    /// <summary>
    /// <c>padding: var(--a, var(--b, 5px))</c>: <see cref="Css.Var"/>, as one length on all four sides, which is how
    /// <c>grid.css</c> and <c>col.css</c> use their variables.
    /// </summary>
    public static readonly IMultiValueConverter Padding = new PaddingConverter();

    private sealed class PaddingConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            Css.Var.Convert(values, typeof(object), parameter, culture) switch
            {
                IConvertible length when length.GetTypeCode() is >= TypeCode.SByte and <= TypeCode.Decimal
                    && length.ToDouble(CultureInfo.InvariantCulture) is >= 0 and var value => new Thickness(value),

                // Not a length, or a negative one, is invalid, and an unset binding is the default padding, 0, as CSS has it. Measured: the
                // style below doesn't come through.
                _ => AvaloniaProperty.UnsetValue,
            };
    }
}
