using System.ComponentModel;
using System.Globalization;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>RangeValue</c>, <c>number | { lower: number; upper: number }</c>: a number for one knob, a pair for two. A number
/// converts to one, and <c>"40"</c> or <c>"20,80"</c> in XAML.
/// </summary>
[TypeConverter(typeof(RangeValueTypeConverter))]
public readonly struct RangeValue : IEquatable<RangeValue>
{
    private readonly double _lower;
    private readonly double _upper;

    /// <summary>A number.</summary>
    public RangeValue(double value)
    {
        _upper = value;
    }

    /// <summary>A pair, <c>{ lower, upper }</c>.</summary>
    public RangeValue(double lower, double upper)
    {
        IsPair = true;
        _lower = lower;
        _upper = upper;
    }

    /// <summary><c>typeof value === 'object'</c>: a pair, not a number.</summary>
    public bool IsPair { get; }

    /// <summary>The number, or a pair's upper, as <c>getValue</c> reads a pair for one knob.</summary>
    public double Number => _upper;

    /// <summary>A pair's lower, or 0 for a number, as <c>getValue</c> reads a number for two knobs.</summary>
    public double Lower => IsPair ? _lower : 0;

    /// <summary>A pair's upper, or the number, as <c>getValue</c> reads a number for two knobs.</summary>
    public double Upper => _upper;

    public static implicit operator RangeValue(double value) => new(value);

    /// <summary>The number, or a pair's upper.</summary>
    public static explicit operator double(RangeValue value) => value.Number;

    /// <summary><c>compareValues</c>: two pairs by their ends, two numbers by value, and a number is never a pair.</summary>
    public bool Equals(RangeValue other) =>
        IsPair == other.IsPair && _upper.Equals(other._upper) && (!IsPair || _lower.Equals(other._lower));

    public override bool Equals(object? obj) => obj is RangeValue other && Equals(other);

    public override int GetHashCode() => IsPair ? HashCode.Combine(_lower, _upper) : _upper.GetHashCode();

    public static bool operator ==(RangeValue left, RangeValue right) => left.Equals(right);

    public static bool operator !=(RangeValue left, RangeValue right) => !left.Equals(right);

    /// <summary><c>"40"</c> or <c>"20, 80"</c>, which <see cref="Parse"/> reads back.</summary>
    public override string ToString() => IsPair
        ? string.Create(CultureInfo.InvariantCulture, $"{_lower}, {_upper}")
        : _upper.ToString(CultureInfo.InvariantCulture);

    /// <summary>A number, <c>"40"</c>, or a pair, <c>"20,80"</c>, in the invariant culture.</summary>
    public static RangeValue Parse(string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            1 => new RangeValue(double.Parse(parts[0], CultureInfo.InvariantCulture)),
            2 => new RangeValue(double.Parse(parts[0], CultureInfo.InvariantCulture), double.Parse(parts[1], CultureInfo.InvariantCulture)),
            _ => throw new FormatException($"'{text}' is neither a number nor a pair of them."),
        };
    }
}

/// <summary>Reads a <see cref="RangeValue"/> from XAML and bindings: a string, or a number.</summary>
public sealed class RangeValueTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || sourceType == typeof(double) || sourceType == typeof(int) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value switch
    {
        string text => RangeValue.Parse(text),
        double number => new RangeValue(number),
        int number => new RangeValue(number),
        _ => base.ConvertFrom(context, culture, value),
    };

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || destinationType == typeof(double) || base.CanConvertTo(context, destinationType);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        value is RangeValue range
            ? destinationType == typeof(double) ? range.Number : range.ToString()
            : base.ConvertTo(context, culture, value, destinationType);
}
