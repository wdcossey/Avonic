using System.Collections;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>presentation</c>: which values the datetime picks.</summary>
public enum DatetimePresentation
{
    Date,
    DateTime,
    Month,
    MonthYear,
    Time,
    TimeDate,
    Year,
}

/// <summary>Ionic <c>size</c>: <c>fixed</c> is at most 350 wide; <c>cover</c> fills its container.</summary>
public enum DatetimeSize
{
    Cover,
    Fixed,
}

/// <summary>
/// Not Ionic's: how the calendar of a grid presentation draws its months, <see cref="AvonicDatetime.CalendarRendering"/>.
/// </summary>
public enum DatetimeCalendarRendering
{
    /// <summary>
    /// Ionic's: the previous, current and next months side by side in a scroller that snaps, which a change of month scrolls and a
    /// swipe drags (126 days).
    /// </summary>
    ScrolledMonths,

    /// <summary>
    /// One month of 42 days, drawn again in place when the month changes: the days fade out and in (none with
    /// <see cref="AvonicConfig.Animated"/> off), and a swipe past a threshold changes the month without following the finger.
    /// </summary>
    SingleGrid,
}

/// <summary>Ionic <c>hourCycle</c>, and Intl's: <c>h11</c> is 0 to 11, <c>h12</c> 1 to 12, <c>h23</c> 0 to 23, <c>h24</c> 1 to 24.</summary>
public enum DatetimeHourCycle
{
    H11,
    H12,
    H23,
    H24,
}

/// <summary>Ionic <c>DatetimeChangeEventDetail</c>: the value confirmed, or none.</summary>
public sealed class DatetimeChangeEventArgs(RoutedEvent routedEvent, DatetimeValue? value) : RoutedEventArgs(routedEvent)
{
    public DatetimeValue? Value { get; } = value;
}

/// <summary>
/// Ionic's <c>value</c>, <c>string | string[]</c>: one ISO 8601 string, or a list of them with <c>multiple</c>. A string converts
/// to one, and so does text in XAML; a <c>string[]</c> or a list converts to a list. Its <see cref="ToString"/> is Ionic's
/// <c>formatValue</c>: a list joined with commas.
/// </summary>
[TypeConverter(typeof(DatetimeValueTypeConverter))]
public readonly struct DatetimeValue : IEquatable<DatetimeValue>
{
    private readonly string? _single;
    private readonly string[]? _list;

    /// <summary>One string. A null one is no value, as Ionic's <c>undefined</c>.</summary>
    public DatetimeValue(string? value)
    {
        _single = value;
    }

    /// <summary>A list of strings.</summary>
    public DatetimeValue(IEnumerable<string> values)
    {
        _list = [.. values ?? throw new ArgumentNullException(nameof(values))];
    }

    /// <summary><c>Array.isArray(value)</c>.</summary>
    public bool IsList => _list is not null;

    /// <summary>The string, or the list's last, as Ionic shows a list's last date.</summary>
    public string? Single => _single ?? (_list is { Length: > 0 } list ? list[^1] : null);

    /// <summary>The list, or the one string as a list of one.</summary>
    public IReadOnlyList<string> Values => _list ?? (_single is null ? [] : [_single]);

    /// <summary>
    /// Ionic's <c>hasValue</c> in <c>processValue</c>: not empty, and not an empty list.
    /// </summary>
    public bool HasValue => _list is { Length: > 0 } || !string.IsNullOrEmpty(_single);

    public static implicit operator DatetimeValue(string? value) => new(value);

    /// <summary>A list; a null one is no value.</summary>
    public static implicit operator DatetimeValue(string[]? values) => values is null ? new((string?)null) : new(values);

    public static implicit operator DatetimeValue(List<string> values) => new(values);

    public bool Equals(DatetimeValue other) =>
        IsList == other.IsList && (IsList ? _list!.SequenceEqual(other._list!) : _single == other._single);

    public override bool Equals(object? obj) => obj is DatetimeValue other && Equals(other);

    public override int GetHashCode()
    {
        if (_list is null)
        {
            return _single?.GetHashCode(StringComparison.Ordinal) ?? 0;
        }

        var hash = new HashCode();
        foreach (var value in _list)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(DatetimeValue left, DatetimeValue right) => left.Equals(right);

    public static bool operator !=(DatetimeValue left, DatetimeValue right) => !left.Equals(right);

    /// <summary>Ionic's <c>formatValue</c>: the string, or the list joined with commas.</summary>
    public override string ToString() => _list is null ? _single ?? string.Empty : string.Join(',', _list);
}

/// <summary>Reads a <see cref="DatetimeValue"/> from XAML and bindings: a string is one value, a list of strings a list.</summary>
public sealed class DatetimeValueTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || typeof(IEnumerable<string>).IsAssignableFrom(sourceType) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value switch
    {
        string text => new DatetimeValue(text),
        IEnumerable<string> list => new DatetimeValue(list),
        _ => base.ConvertFrom(context, culture, value),
    };

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        destinationType == typeof(string) && value is DatetimeValue v ? v.ToString() : base.ConvertTo(context, culture, value, destinationType);
}

/// <summary>
/// Ionic's <c>dayValues</c>, <c>monthValues</c>, <c>yearValues</c>, <c>hourValues</c> and <c>minuteValues</c>, <c>number |
/// number[] | string</c>: a number, numbers, or text such as <c>"0,15,30,45"</c>, read by Ionic's <c>convertToArrayOfNumbers</c>
/// (brackets and white space removed, split at commas, what isn't a number dropped).
/// </summary>
[TypeConverter(typeof(DatetimeValueListTypeConverter))]
public readonly struct DatetimeValueList : IEquatable<DatetimeValueList>, IEnumerable<int>
{
    private readonly int[]? _values;

    public DatetimeValueList(IEnumerable<int> values)
    {
        _values = [.. values];
    }

    /// <summary>The numbers, as <c>convertToArrayOfNumbers</c> gives them.</summary>
    public IReadOnlyList<int> Values => _values ?? [];

    public static implicit operator DatetimeValueList(int value) => new([value]);

    public static implicit operator DatetimeValueList(int[] values) => new(values);

    public static implicit operator DatetimeValueList(string text) => Parse(text);

    /// <summary>Ionic's reading of a string.</summary>
    public static DatetimeValueList Parse(string text) => new(DatetimeUtils.ConvertToArrayOfNumbers(text));

    public bool Equals(DatetimeValueList other) => Values.SequenceEqual(other.Values);

    public override bool Equals(object? obj) => obj is DatetimeValueList other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in Values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(DatetimeValueList left, DatetimeValueList right) => left.Equals(right);

    public static bool operator !=(DatetimeValueList left, DatetimeValueList right) => !left.Equals(right);

    public IEnumerator<int> GetEnumerator() => Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => string.Join(',', Values);
}

/// <summary>Reads a <see cref="DatetimeValueList"/> from XAML: text, or a number.</summary>
public sealed class DatetimeValueListTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || sourceType == typeof(int) || sourceType == typeof(int[]) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value switch
    {
        string text => DatetimeValueList.Parse(text),
        int n => (DatetimeValueList)n,
        int[] list => (DatetimeValueList)list,
        _ => base.ConvertFrom(context, culture, value),
    };
}

/// <summary>
/// Ionic <c>DatetimeHighlightStyle</c>: a day's text colour, background colour and border, as CSS text (<c>"#800080"</c>,
/// <c>"rgb(255, 192, 203)"</c>, <c>"1px solid green"</c>). A colour Avalonia can't read, or a border it can't, is ignored.
/// </summary>
public class DatetimeHighlightStyle
{
    public string? TextColor { get; init; }

    public string? BackgroundColor { get; init; }

    public string? Border { get; init; }
}

/// <summary>Ionic <c>DatetimeHighlight</c>: a date, <c>"2023-06-05"</c>, and its style.</summary>
public sealed class DatetimeHighlight : DatetimeHighlightStyle
{
    public required string Date { get; init; }
}

/// <summary>
/// Ionic's <c>highlightedDates</c>, <c>DatetimeHighlight[] | ((dateIsoString: string) => DatetimeHighlightStyle | undefined)</c>:
/// a list of dates, or a callback given each day's ISO string.
/// </summary>
public readonly struct DatetimeHighlightedDates
{
    public DatetimeHighlightedDates(IReadOnlyList<DatetimeHighlight> dates)
    {
        Dates = dates ?? throw new ArgumentNullException(nameof(dates));
    }

    public DatetimeHighlightedDates(Func<string, DatetimeHighlightStyle?> callback)
    {
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public IReadOnlyList<DatetimeHighlight>? Dates { get; }

    public Func<string, DatetimeHighlightStyle?>? Callback { get; }

    public static implicit operator DatetimeHighlightedDates(DatetimeHighlight[] dates) => new(dates);

    public static implicit operator DatetimeHighlightedDates(List<DatetimeHighlight> dates) => new(dates);

    public static implicit operator DatetimeHighlightedDates(Func<string, DatetimeHighlightStyle?> callback) => new(callback);
}

/// <summary>Ionic <c>FormatOptions</c>: how the header's date (<c>Date</c>) and the time button's time (<c>Time</c>) are written.</summary>
public sealed class DatetimeFormatOptions
{
    public DateTimeFormatOptions? Date { get; init; }

    public DateTimeFormatOptions? Time { get; init; }
}

/// <summary>Intl's <c>narrow</c>, <c>short</c> and <c>long</c>, for <c>weekday</c> and <c>era</c>.</summary>
public enum DateTimeFormatText
{
    Narrow,
    Short,
    Long,
}

/// <summary>Intl's <c>numeric</c> and <c>2-digit</c>.</summary>
public enum DateTimeFormatNumber
{
    Numeric,
    TwoDigit,
}

/// <summary>Intl's <c>month</c>: <c>numeric</c>, <c>2-digit</c>, <c>narrow</c>, <c>short</c>, <c>long</c>.</summary>
public enum DateTimeFormatMonth
{
    Numeric,
    TwoDigit,
    Narrow,
    Short,
    Long,
}

/// <summary>Intl's <c>dateStyle</c> and <c>timeStyle</c>.</summary>
public enum DateTimeFormatStyle
{
    Full,
    Long,
    Medium,
    Short,
}

/// <summary>
/// Intl's <c>DateTimeFormatOptions</c>, the ones Avonic writes: each field is shown when it's set, in the locale's order, as Intl
/// does. Written by .NET's <c>CultureInfo</c>, not by Intl; see <c>docs/components/ion-datetime/README.md</c>.
/// </summary>
public sealed class DateTimeFormatOptions
{
    public DateTimeFormatText? Weekday { get; init; }

    public DateTimeFormatText? Era { get; init; }

    public DateTimeFormatNumber? Year { get; init; }

    public DateTimeFormatMonth? Month { get; init; }

    public DateTimeFormatNumber? Day { get; init; }

    public DateTimeFormatNumber? Hour { get; init; }

    public DateTimeFormatNumber? Minute { get; init; }

    public DateTimeFormatNumber? Second { get; init; }

    public bool? Hour12 { get; init; }

    public DatetimeHourCycle? HourCycle { get; init; }

    public DateTimeFormatStyle? DateStyle { get; init; }

    public DateTimeFormatStyle? TimeStyle { get; init; }

    /// <summary>Not supported, as in Ionic: a warning, and ignored.</summary>
    public string? TimeZone { get; init; }

    /// <summary>Not supported, as in Ionic: a warning, and ignored.</summary>
    public string? TimeZoneName { get; init; }

    internal bool HasDateField => Weekday is not null || Era is not null || Year is not null || Month is not null || Day is not null;

    internal bool HasTimeField => Hour is not null || Minute is not null || Second is not null;
}
