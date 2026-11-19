using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Logging;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>datetime/utils/comparison.js</c>, <c>helpers.js</c> (its date half), <c>parse.js</c> and <c>manipulation.js</c>,
/// one for one. JavaScript's comparisons with <c>undefined</c> are false, which <see cref="Lt"/> and <see cref="Gt"/> keep.
/// </summary>
internal static partial class DatetimeUtils
{
    internal static bool Lt(int? a, int? b) => a.HasValue && b.HasValue && a.Value < b.Value;

    internal static bool Gt(int? a, int? b) => a.HasValue && b.HasValue && a.Value > b.Value;

    // ── comparison.js ────────────────────────────────────────────────────────────────────────────────

    public static bool IsSameDay(DatetimeParts a, DatetimeParts b) => a.Month == b.Month && a.Day == b.Day && a.Year == b.Year;

    public static bool IsBefore(DatetimeParts a, DatetimeParts b) =>
        Lt(a.Year, b.Year)
        || (a.Year == b.Year && Lt(a.Month, b.Month))
        || (a.Year == b.Year && a.Month == b.Month && a.Day is not null && Lt(a.Day, b.Day));

    public static bool IsAfter(DatetimeParts a, DatetimeParts b) =>
        Gt(a.Year, b.Year)
        || (a.Year == b.Year && Gt(a.Month, b.Month))
        || (a.Year == b.Year && a.Month == b.Month && a.Day is not null && Gt(a.Day, b.Day));

    /// <summary><c>warnIfValueOutOfBounds</c>.</summary>
    public static void WarnIfValueOutOfBounds(IReadOnlyList<DatetimeParts> values, DatetimeParts? min, DatetimeParts? max, object? source)
    {
        foreach (var value in values)
        {
            if ((min is { } lo && IsBefore(value, lo)) || (max is { } hi && IsAfter(value, hi)))
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(source,
                    "[ion-datetime] - The value provided to ion-datetime is out of bounds. Min: {Min} Max: {Max} Value: {Value}", min, max, value);
                break;
            }
        }
    }

    // ── helpers.js ───────────────────────────────────────────────────────────────────────────────────

    public static bool IsLeapYear(int year) => (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;

    /// <summary><c>getNumDaysInMonth</c>. A month or year that isn't known reads as JavaScript's <c>undefined</c> would.</summary>
    public static int GetNumDaysInMonth(int? month, int? year) =>
        month is 4 or 6 or 9 or 11 ? 30
        : month == 2 ? (year is { } y && IsLeapYear(y) ? 29 : 28)
        : 31;

    // ── parse.js ─────────────────────────────────────────────────────────────────────────────────────

    [GeneratedRegex(@"^(\d{4}|[+\-]\d{6})(?:-(\d{2})(?:-(\d{2}))?)?(?:T(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{3}))?)?(?:(Z)|([+\-])(\d{2})(?::(\d{2}))?)?)?$", RegexOptions.CultureInvariant)]
    private static partial Regex Iso8601();

    [GeneratedRegex(@"^((\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{3}))?)?(?:(Z)|([+\-])(\d{2})(?::(\d{2}))?)?)?$", RegexOptions.CultureInvariant)]
    private static partial Regex TimeOnly();

    /// <summary>
    /// <c>convertToArrayOfNumbers</c>: a string has its brackets and white space removed and is split at commas; each piece read
    /// as <c>parseInt</c> reads it, and the ones that aren't numbers dropped.
    /// </summary>
    public static int[] ConvertToArrayOfNumbers(string input)
    {
        var pieces = Regex.Replace(input, @"\[|\]|\s", string.Empty).Split(',');
        var values = new List<int>();
        foreach (var piece in pieces)
        {
            if (ParseInt(piece) is { } n)
            {
                values.Add(n);
            }
        }

        return [.. values];
    }

    /// <summary>JavaScript's <c>parseInt(s, 10)</c>: an optional sign and the leading digits, or nothing.</summary>
    internal static int? ParseInt(string s)
    {
        s = s.TrimStart();
        var i = 0;
        if (i < s.Length && (s[i] == '+' || s[i] == '-'))
        {
            i++;
        }

        var start = i;
        while (i < s.Length && char.IsAsciiDigit(s[i]))
        {
            i++;
        }

        return i == start ? null : int.TryParse(s.AsSpan(0, i), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    /// <summary>
    /// <c>parseDate</c> for one string: a time alone (<c>HH:mm</c>) first, then ISO 8601. The zone, if any, is read and dropped:
    /// the parts are the wall clock as written. Null, with Ionic's warning, if neither matches.
    /// </summary>
    public static DatetimeParts? ParseDate(string? value, object? source = null)
    {
        Match? match = null;
        int?[] parts = new int?[8];
        if (!string.IsNullOrEmpty(value))
        {
            match = TimeOnly().Match(value);
            if (match.Success)
            {
                // The time's groups sit two along, as Ionic's unshift(undefined, undefined) puts them.
                parts[4] = Group(match, 2);
                parts[5] = Group(match, 3);
            }
            else
            {
                match = Iso8601().Match(value);
                if (match.Success)
                {
                    for (var i = 1; i < 6; i++)
                    {
                        parts[i] = Group(match, i);
                    }
                }
            }
        }

        if (match is not { Success: true })
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(source,
                "[ion-datetime] - Unable to parse date string: {Value}. Please provide a valid ISO 8601 datetime string.", value);
            return null;
        }

        // ampm: parse[4] < 12 ? 'am' : 'pm', where undefined < 12 is false.
        return new DatetimeParts(parts[2], parts[3], parts[1], parts[4], parts[5], parts[4] is { } h && h < 12 ? "am" : "pm");
    }

    private static int? Group(Match match, int index) => match.Groups[index].Success ? ParseInt(match.Groups[index].Value) : null;

    /// <summary><c>parseDate</c> for a list: every string, or null if one doesn't parse.</summary>
    public static List<DatetimeParts>? ParseDates(IEnumerable<string> values, object? source = null)
    {
        var list = new List<DatetimeParts>();
        foreach (var value in values)
        {
            if (ParseDate(value, source) is not { } parts)
            {
                return null;
            }

            list.Add(parts);
        }

        return list;
    }

    /// <summary><c>clampDate</c>: by the date only, min first.</summary>
    public static DatetimeParts ClampDate(DatetimeParts parts, DatetimeParts? min, DatetimeParts? max) =>
        min is { } lo && IsBefore(parts, lo) ? lo
        : max is { } hi && IsAfter(parts, hi) ? hi
        : parts;

    /// <summary><c>parseAmPm</c>: undefined is <c>'am'</c>, as <c>undefined >= 12</c> is false.</summary>
    public static string ParseAmPm(int? hour) => hour is >= 12 ? "pm" : "am";

    /// <summary><c>parseMaxParts</c>: what's missing is the end of the year, the month or the day.</summary>
    public static DatetimeParts? ParseMaxParts(string max, DatetimeParts today, object? source = null)
    {
        if (ParseDate(max, source) is not { } result)
        {
            return null;
        }

        var year = result.Year ?? today.Year;
        var month = result.Month ?? 12;
        return new DatetimeParts(month, result.Day ?? GetNumDaysInMonth(month, year), year, result.Hour ?? 23, result.Minute ?? 59);
    }

    /// <summary><c>parseMinParts</c>: what's missing is the start of the year, the month or the day.</summary>
    public static DatetimeParts? ParseMinParts(string min, DatetimeParts today, object? source = null)
    {
        if (ParseDate(min, source) is not { } result)
        {
            return null;
        }

        return new DatetimeParts(result.Month ?? 1, result.Day ?? 1, result.Year ?? today.Year, result.Hour ?? 0, result.Minute ?? 0);
    }

    // ── manipulation.js ──────────────────────────────────────────────────────────────────────────────

    private static string TwoDigit(int? value) => Math.Abs(value ?? 0).ToString("00", CultureInfo.InvariantCulture)[^2..];

    private static string FourDigit(int? value)
    {
        var s = "000" + Math.Abs(value ?? 0).ToString(CultureInfo.InvariantCulture);
        return s[^4..];
    }

    /// <summary>
    /// <c>convertDataToISO</c>: <c>YYYY-MM-DDTHH:mm:00</c>, shortened to what the parts have; a time alone is <c>HH:mm</c>.
    /// </summary>
    public static string ConvertDataToIso(DatetimeParts data)
    {
        var rtn = string.Empty;
        if (data.Year is not null)
        {
            rtn = FourDigit(data.Year);
            if (data.Month is not null)
            {
                rtn += "-" + TwoDigit(data.Month);
                if (data.Day is not null)
                {
                    rtn += "-" + TwoDigit(data.Day);
                    if (data.Hour is not null)
                    {
                        rtn += $"T{TwoDigit(data.Hour)}:{TwoDigit(data.Minute)}:00";
                    }
                }
            }
        }
        else if (data.Hour is not null)
        {
            rtn = TwoDigit(data.Hour) + ":" + TwoDigit(data.Minute);
        }

        return rtn;
    }

    public static int? Convert12HourTo24Hour(int? hour, string? ampm) =>
        ampm is null ? hour
        : ampm == "am" ? (hour == 12 ? 0 : hour)
        : hour == 12 ? 12 : hour + 12;

    public static DatetimeParts GetStartOfWeek(DatetimeParts parts) =>
        SubtractDays(parts, parts.DayOfWeek ?? throw new InvalidOperationException("No day of week provided"));

    public static DatetimeParts GetEndOfWeek(DatetimeParts parts) =>
        AddDays(parts, 6 - (parts.DayOfWeek ?? throw new InvalidOperationException("No day of week provided")));

    public static DatetimeParts GetNextDay(DatetimeParts parts) => AddDays(parts, 1);

    public static DatetimeParts GetPreviousDay(DatetimeParts parts) => SubtractDays(parts, 1);

    public static DatetimeParts GetPreviousWeek(DatetimeParts parts) => SubtractDays(parts, 7);

    public static DatetimeParts GetNextWeek(DatetimeParts parts) => AddDays(parts, 7);

    /// <summary><c>subtractDays</c>: by at most a month, as Ionic only moves by a week.</summary>
    public static DatetimeParts SubtractDays(DatetimeParts parts, int numDays)
    {
        var day = (parts.Day ?? throw new InvalidOperationException("No day provided")) - numDays;
        var month = parts.Month ?? 0;
        var year = parts.Year ?? 0;
        if (day < 1)
        {
            month -= 1;
        }

        if (month < 1)
        {
            month = 12;
            year -= 1;
        }

        if (day < 1)
        {
            day = GetNumDaysInMonth(month, year) + day;
        }

        return new DatetimeParts(month, day, year);
    }

    /// <summary><c>addDays</c>.</summary>
    public static DatetimeParts AddDays(DatetimeParts parts, int numDays)
    {
        var day = (parts.Day ?? throw new InvalidOperationException("No day provided")) + numDays;
        var month = parts.Month ?? 0;
        var year = parts.Year ?? 0;
        var daysInMonth = GetNumDaysInMonth(month, year);
        if (day > daysInMonth)
        {
            day -= daysInMonth;
            month += 1;
        }

        if (month > 12)
        {
            month = 1;
            year += 1;
        }

        return new DatetimeParts(month, day, year);
    }

    private static int? ClampDay(int month, int year, int? day)
    {
        var n = GetNumDaysInMonth(month, year);
        return Lt(n, day) ? n : day;
    }

    public static DatetimeParts GetPreviousMonth(DatetimeParts parts)
    {
        var month = parts.Month == 1 ? 12 : (parts.Month ?? 0) - 1;
        var year = parts.Month == 1 ? (parts.Year ?? 0) - 1 : parts.Year ?? 0;
        return new DatetimeParts(month, ClampDay(month, year, parts.Day), year);
    }

    public static DatetimeParts GetNextMonth(DatetimeParts parts)
    {
        var month = parts.Month == 12 ? 1 : (parts.Month ?? 0) + 1;
        var year = parts.Month == 12 ? (parts.Year ?? 0) + 1 : parts.Year ?? 0;
        return new DatetimeParts(month, ClampDay(month, year, parts.Day), year);
    }

    private static DatetimeParts ChangeYear(DatetimeParts parts, int delta)
    {
        var month = parts.Month ?? 0;
        var year = (parts.Year ?? 0) + delta;
        return new DatetimeParts(month, ClampDay(month, year, parts.Day), year);
    }

    public static DatetimeParts GetPreviousYear(DatetimeParts parts) => ChangeYear(parts, -1);

    public static DatetimeParts GetNextYear(DatetimeParts parts) => ChangeYear(parts, 1);

    public static int? GetInternalHourValue(int hour, bool use24Hour, string? ampm) => use24Hour ? hour : Convert12HourTo24Hour(hour, ampm);

    /// <summary><c>calculateHourFromAMPM</c>.</summary>
    public static int? CalculateHourFromAmPm(DatetimeParts current, string newAmPm)
    {
        var hour = current.Hour;
        if (current.AmPm == "am" && newAmPm == "pm")
        {
            return Convert12HourTo24Hour(hour, "pm");
        }

        if (current.AmPm == "pm" && newAmPm == "am")
        {
            return hour is { } h ? Math.Abs(h - 12) : null;
        }

        return hour;
    }

    /// <summary>
    /// <c>validateParts</c>: clamped by date, the day cut to its month, and on min's or max's own day the time raised or lowered to it.
    /// </summary>
    public static DatetimeParts ValidateParts(DatetimeParts parts, DatetimeParts? min, DatetimeParts? max)
    {
        var copy = ClampDate(parts, min, max);
        var numDays = GetNumDaysInMonth(parts.Month, parts.Year);
        if (parts.Day is { } day && numDays < day)
        {
            copy = copy with { Day = numDays };
        }

        if (min is { } lo && IsSameDay(copy, lo) && copy.Hour is not null && lo.Hour is not null)
        {
            if (copy.Hour < lo.Hour)
            {
                copy = copy with { Hour = lo.Hour, Minute = lo.Minute };
            }
            else if (copy.Hour == lo.Hour && copy.Minute is not null && lo.Minute is not null && copy.Minute < lo.Minute)
            {
                copy = copy with { Minute = lo.Minute };
            }
        }

        // Ionic compares the parts as given, not the clamped copy, with max.
        if (max is { } hi && IsSameDay(parts, hi) && copy.Hour is not null && hi.Hour is not null)
        {
            if (copy.Hour > hi.Hour)
            {
                copy = copy with { Hour = hi.Hour, Minute = hi.Minute };
            }
            else if (copy.Hour == hi.Hour && copy.Minute is not null && hi.Minute is not null && copy.Minute > hi.Minute)
            {
                copy = copy with { Minute = hi.Minute };
            }
        }

        return copy;
    }

    /// <summary><c>getClosestValidDate</c>: today moved to the nearest year, month, day, hour and minute the lists allow.</summary>
    public static DatetimeParts GetClosestValidDate(DatetimeParts reference, int[]? monthValues, int[]? dayValues, int[]? yearValues,
        int[]? hourValues, int[]? minuteValues, DatetimeParts? min, DatetimeParts? max)
    {
        var copy = reference with { DayOfWeek = null };
        if (yearValues is not null)
        {
            var years = yearValues.Where(y => !(min is { } lo && Lt(y, lo.Year)) && !(max is { } hi && Gt(y, hi.Year))).ToArray();
            copy = copy with { Year = FindClosestValue(reference.Year, years) };
        }

        if (monthValues is not null)
        {
            var months = monthValues.Where(m => !(min is { } lo && copy.Year == lo.Year && Lt(m, lo.Month))
                                                && !(max is { } hi && copy.Year == hi.Year && Gt(m, hi.Month))).ToArray();
            copy = copy with { Month = FindClosestValue(reference.Month, months) };
        }

        if (reference.Day is not null && dayValues is not null)
        {
            var days = dayValues.Where(d => !(min is { } lo && IsBefore(copy with { Day = d }, lo))
                                            && !(max is { } hi && IsAfter(copy with { Day = d }, hi))).ToArray();
            copy = copy with { Day = FindClosestValue(reference.Day, days) };
        }

        if (reference.Hour is not null && hourValues is not null)
        {
            var hours = hourValues.Where(h => !(min is { Hour: not null } lo && IsSameDay(copy, lo) && h < lo.Hour)
                                              && !(max is { Hour: not null } hi && IsSameDay(copy, hi) && h > hi.Hour)).ToArray();
            var hour = FindClosestValue(reference.Hour, hours);
            copy = copy with { Hour = hour, AmPm = ParseAmPm(hour) };
        }

        if (reference.Minute is not null && minuteValues is not null)
        {
            var minutes = minuteValues.Where(m => !(min is { Minute: not null } lo && IsSameDay(copy, lo) && copy.Hour == lo.Hour && m < lo.Minute)
                                                  && !(max is { Minute: not null } hi && IsSameDay(copy, hi) && copy.Hour == hi.Hour && m > hi.Minute)).ToArray();
            copy = copy with { Minute = FindClosestValue(reference.Minute, minutes) };
        }

        return copy;
    }

    /// <summary><c>findClosestValue</c>: the first of the nearest; undefined (null) when there are none.</summary>
    private static int? FindClosestValue(int? reference, int[] values)
    {
        if (values.Length == 0)
        {
            return null;
        }

        var closest = values[0];
        var rank = Math.Abs(closest - (reference ?? 0));
        for (var i = 1; i < values.Length; i++)
        {
            var r = Math.Abs(values[i] - (reference ?? 0));
            if (r < rank)
            {
                closest = values[i];
                rank = r;
            }
        }

        return closest;
    }
}
