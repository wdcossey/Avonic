using System.Globalization;
using Avalonia.Logging;

namespace Avonic.Components.Controls;

/// <summary>An option of a wheel the datetime makes: its text, value, and whether it's disabled.</summary>
internal readonly record struct DatetimeOption(string Text, object Value, bool Disabled = false);

/// <summary>A cell of the calendar grid: <c>getDaysOfMonth</c>'s day (null for the padding before the 1st).</summary>
internal readonly record struct DatetimeCalendarCell(int? Day, int? DayOfWeek, bool IsAdjacentDay);

/// <summary>Ionic's <c>datetime/utils/data.js</c> and <c>state.js</c>, one for one.</summary>
internal static partial class DatetimeUtils
{
    private static readonly int[] Hour11 = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
    private static readonly int[] Hour23 = [.. Enumerable.Range(0, 24)];
    private static readonly int[] Hour24 = [.. Enumerable.Range(1, 23), 0];
    private static readonly int[] Minutes = [.. Enumerable.Range(0, 60)];

    /// <summary><c>getToday</c>, as parts: the local wall clock.</summary>
    public static DatetimeParts PartsOf(DateTime now) =>
        new(now.Month, now.Day, now.Year, now.Hour, now.Minute, ParseAmPm(now.Hour));

    /// <summary>
    /// <c>getDaysOfMonth</c>: blank cells before the 1st, from <paramref name="firstDayOfWeek"/>; with adjacent days, 42 days with
    /// the previous and next months'. A day's <c>DayOfWeek</c> is its column, as Ionic's <c>data-day-of-week</c> is.
    /// </summary>
    public static List<DatetimeCalendarCell> GetDaysOfMonth(int month, int year, int firstDayOfWeek, bool showAdjacentDays)
    {
        var numDays = GetNumDaysInMonth(month, year);
        var previousNumDays = month == 1 ? GetNumDaysInMonth(12, year - 1) : GetNumDaysInMonth(month - 1, year);
        var firstOfMonth = (int)new DateTime(year, month, 1).DayOfWeek;
        var offset = firstOfMonth >= firstDayOfWeek ? firstOfMonth - (firstDayOfWeek + 1) : 6 - (firstDayOfWeek - firstOfMonth);
        var days = new List<DatetimeCalendarCell>();
        for (var i = 1; i <= numDays; i++)
        {
            days.Add(new DatetimeCalendarCell(i, Mod(offset + i, 7), false));
        }

        if (showAdjacentDays)
        {
            for (var i = 0; i <= offset; i++)
            {
                days.Insert(0, new DatetimeCalendarCell(previousNumDays - i, Mod(previousNumDays - i, 7), true));
            }

            var positiveOffset = 41 - (numDays + offset);
            for (var i = 0; i < positiveOffset; i++)
            {
                days.Add(new DatetimeCalendarCell(i + 1, Mod(numDays + offset + i, 7), true));
            }
        }
        else
        {
            for (var i = 0; i <= offset; i++)
            {
                days.Insert(0, new DatetimeCalendarCell(null, null, false));
            }
        }

        return days;
    }

    /// <summary>JavaScript's <c>%</c> keeps the sign; Ionic's offsets of -1 give a day of week of 0 at most once, as here.</summary>
    private static int Mod(int a, int b) => a % b;

    /// <summary><c>generateTime</c>: the hours and minutes the cycle, the lists and min and max allow, and whether AM and PM are.</summary>
    public static (int[] Hours, int[] Minutes, bool Am, bool Pm) GenerateTime(DatetimeParts reference, DatetimeHourCycle hourCycle,
        DatetimeParts? min, DatetimeParts? max, int[]? hourValues, int[]? minuteValues)
    {
        var use24Hour = DatetimeIntl.Is24Hour(hourCycle);
        IEnumerable<int> hours = hourCycle switch
        {
            DatetimeHourCycle.H11 or DatetimeHourCycle.H12 => Hour11,
            DatetimeHourCycle.H23 => Hour23,
            _ => Hour24,
        };
        IEnumerable<int> minutes = Minutes;
        var am = true;
        var pm = true;
        if (hourValues is not null)
        {
            hours = hours.Where(hourValues.Contains);
        }

        if (minuteValues is not null)
        {
            minutes = minutes.Where(minuteValues.Contains);
        }

        int Converted(int hour) => use24Hour ? hour : reference.AmPm == "pm" ? (hour + 12) % 24 : hour;

        if (min is { } lo)
        {
            if (IsSameDay(reference, lo))
            {
                if (lo.Hour is { } minHour)
                {
                    hours = hours.Where(h => Converted(h) >= minHour).ToArray();
                    am = minHour < 13;
                }

                if (lo.Minute is { } minMinute)
                {
                    var isPastMinHour = lo.Hour is not null && reference.Hour is not null && reference.Hour > lo.Hour;
                    minutes = minutes.Where(m => isPastMinHour || m >= minMinute).ToArray();
                }
            }
            else if (IsBefore(reference, lo))
            {
                hours = [];
                minutes = [];
                am = pm = false;
            }
        }

        if (max is { } hi)
        {
            if (IsSameDay(reference, hi))
            {
                if (hi.Hour is { } maxHour)
                {
                    hours = hours.Where(h => Converted(h) <= maxHour).ToArray();
                    pm = maxHour >= 12;
                }

                if (hi.Minute is { } maxMinute && reference.Hour == hi.Hour)
                {
                    minutes = minutes.Where(m => m <= maxMinute).ToArray();
                }
            }
            else if (IsAfter(reference, hi))
            {
                hours = [];
                minutes = [];
                am = pm = false;
            }
        }

        return ([.. hours], [.. minutes], am, pm);
    }

    /// <summary><c>generateMonths</c>: the previous, working and next months, or the forced month in place of one of the sides.</summary>
    public static DatetimeParts[] GenerateMonths(DatetimeParts reference, DatetimeParts? forced = null)
    {
        var current = new DatetimeParts(reference.Month, reference.Day, reference.Year);
        if (forced is { } f && (reference.Month != f.Month || reference.Year != f.Year))
        {
            var forcedMonth = new DatetimeParts(f.Month, f.Day, f.Year);
            return IsBefore(forcedMonth, current)
                ? [forcedMonth, current, GetNextMonth(reference)]
                : [GetPreviousMonth(reference), current, forcedMonth];
        }

        return [GetPreviousMonth(reference), current, GetNextMonth(reference)];
    }

    /// <summary><c>getMonthColumnData</c>.</summary>
    public static List<DatetimeOption> GetMonthColumnData(CultureInfo culture, DatetimeParts reference, DatetimeParts? min, DatetimeParts? max, int[]? monthValues)
    {
        var year = reference.Year;
        var months = new List<DatetimeOption>();
        IEnumerable<int> processed;
        if (monthValues is not null)
        {
            processed = monthValues;
            if (max?.Month is { } maxMonth)
            {
                processed = processed.Where(m => m <= maxMonth);
            }

            if (min?.Month is { } minMonth)
            {
                processed = processed.Where(m => m >= minMonth);
            }
        }
        else
        {
            var maxMonth = max is { } hi && hi.Year == year ? hi.Month ?? 12 : 12;
            var minMonth = min is { } lo && lo.Year == year ? lo.Month ?? 1 : 1;
            processed = Enumerable.Range(minMonth, Math.Max(0, maxMonth - minMonth + 1));
        }

        foreach (var month in processed)
        {
            months.Add(new DatetimeOption(DatetimeIntl.MonthOption(culture, new DatetimeParts(month, 1, year)), month));
        }

        return months;
    }

    /// <summary><c>getDayColumnData</c>, with the combined column's format when <paramref name="combined"/> is set.</summary>
    public static List<(DatetimeOption Option, int Day)> GetDayColumnData(CultureInfo culture, DatetimeParts reference, DatetimeParts? min,
        DatetimeParts? max, int[]? dayValues, bool combined = false)
    {
        var (month, year) = (reference.Month, reference.Year);
        var numDays = GetNumDaysInMonth(month, year);
        var maxDay = max is { Day: { } hd } hi && hi.Year == year && hi.Month == month ? hd : numDays;
        var minDay = min is { Day: { } ld } lo && lo.Year == year && lo.Month == month ? ld : 1;
        IEnumerable<int> days = dayValues is not null
            ? dayValues.Where(d => d >= minDay && d <= maxDay)
            : Enumerable.Range(minDay, Math.Max(0, maxDay - minDay + 1));
        return [.. days.Select(day =>
        {
            var parts = new DatetimeParts(month, day, year);
            var text = combined ? DatetimeIntl.LocalizedDateTime(culture, parts, DatetimeIntl.ShortDate) : DatetimeIntl.DayOption(culture, parts);
            return (new DatetimeOption(text, day), day);
        })];
    }

    /// <summary><c>getYearColumnData</c>: the list within min and max, or 100 years before the reference's to its year.</summary>
    public static List<DatetimeOption> GetYearColumnData(CultureInfo culture, DatetimeParts reference, DatetimeParts? min, DatetimeParts? max, int[]? yearValues)
    {
        IEnumerable<int> years;
        if (yearValues is not null)
        {
            years = yearValues;
            if (max?.Year is { } maxYear)
            {
                years = years.Where(y => y <= maxYear);
            }

            if (min?.Year is { } minYear)
            {
                years = years.Where(y => y >= minYear);
            }
        }
        else
        {
            var year = reference.Year ?? 2023;
            var maxYear = max?.Year ?? year;
            var minYear = min?.Year ?? year - 100;
            years = Enumerable.Range(minYear, Math.Max(0, maxYear - minYear + 1));
        }

        return [.. years.Select(y => new DatetimeOption(DatetimeIntl.Year(culture, new DatetimeParts(reference.Month, reference.Day, y)), y))];
    }

    /// <summary>
    /// <c>getCombinedDateColumnData</c>: every day from <paramref name="min"/>'s month to <paramref name="max"/>'s, within them, as
    /// <c>Thu, Jun 15</c>, today as the locale's "Today"; each value <c>"2023-6-15"</c>.
    /// </summary>
    public static (List<DatetimeOption> Items, List<DatetimeParts> Parts) GetCombinedDateColumnData(CultureInfo culture, DatetimeParts today,
        DatetimeParts min, DatetimeParts max, int[]? dayValues, int[]? monthValues)
    {
        var items = new List<DatetimeOption>();
        var parts = new List<DatetimeParts>();
        var months = new List<DatetimeParts>();
        var current = min;
        // getAllMonthsInRange, bounded so that a min after max can't run away.
        for (var guard = 0; guard < 2400; guard++)
        {
            months.Add(current);
            if (current.Month == max.Month && current.Year == max.Year)
            {
                break;
            }

            current = GetNextMonth(current);
        }

        if (monthValues is not null)
        {
            months = [.. months.Where(m => m.Month is { } mm && monthValues.Contains(mm))];
        }

        foreach (var month in months)
        {
            var reference = new DatetimeParts(month.Month, null, month.Year);
            foreach (var (option, day) in GetDayColumnData(culture, reference, min, max, dayValues, combined: true))
            {
                var isToday = IsSameDay(reference with { Day = day }, today);
                items.Add(new DatetimeOption(isToday ? DatetimeIntl.Today(culture) : option.Text, $"{month.Year}-{month.Month}-{day}"));
                parts.Add(new DatetimeParts(month.Month, day, month.Year));
            }
        }

        return (items, parts);
    }

    /// <summary><c>getTimeColumnsData</c>: the hour, minute and AM/PM wheels' options.</summary>
    public static (List<DatetimeOption> Hours, List<DatetimeOption> Minutes, List<DatetimeOption> DayPeriods) GetTimeColumnsData(
        CultureInfo culture, DatetimeParts reference, DatetimeHourCycle hourCycle, DatetimeParts? min, DatetimeParts? max,
        int[]? hourValues, int[]? minuteValues)
    {
        var use24Hour = DatetimeIntl.Is24Hour(hourCycle);
        var (hours, minutes, am, pm) = GenerateTime(reference, hourCycle, min, max, hourValues, minuteValues);
        var hourItems = hours.Select(h => new DatetimeOption(FormattedHour(h, hourCycle), (object?)GetInternalHourValue(h, use24Hour, reference.AmPm) ?? h)).ToList();
        var minuteItems = minutes.Select(m => new DatetimeOption(m.ToString("00", CultureInfo.InvariantCulture), m)).ToList();
        var periods = new List<DatetimeOption>();
        if (am && !use24Hour)
        {
            periods.Add(new DatetimeOption(DatetimeIntl.DayPeriod(culture, "am"), "am"));
        }

        if (pm && !use24Hour)
        {
            periods.Add(new DatetimeOption(DatetimeIntl.DayPeriod(culture, "pm"), "pm"));
        }

        return (hourItems, minuteItems, periods);
    }

    /// <summary><c>getFormattedHour</c>: 0 is <c>0</c>, <c>12</c>, <c>00</c> or <c>24</c> by the cycle; 24-hour hours have two digits.</summary>
    public static string FormattedHour(int hour, DatetimeHourCycle hourCycle)
    {
        if (hour == 0)
        {
            return hourCycle switch
            {
                DatetimeHourCycle.H11 => "0",
                DatetimeHourCycle.H12 => "12",
                DatetimeHourCycle.H23 => "00",
                _ => "24",
            };
        }

        return DatetimeIntl.Is24Hour(hourCycle) ? hour.ToString("00", CultureInfo.InvariantCulture) : hour.ToString(CultureInfo.InvariantCulture);
    }

    // ── state.js ─────────────────────────────────────────────────────────────────────────────────────

    public static bool IsYearDisabled(int? year, DatetimeParts? min, DatetimeParts? max) =>
        (min is { } lo && Gt(lo.Year, year)) || (max is { } hi && Lt(hi.Year, year));

    /// <summary><c>isDayDisabled</c>: a padding cell, a day not in the list, before min or after max.</summary>
    public static bool IsDayDisabled(DatetimeParts parts, DatetimeParts? min, DatetimeParts? max, int[]? dayValues = null) =>
        parts.Day is not { } day
        || (dayValues is not null && !dayValues.Contains(day))
        || (min is { } lo && IsBefore(parts, lo))
        || (max is { } hi && IsAfter(parts, hi));

    /// <summary><c>isMonthDisabled</c>.</summary>
    public static bool IsMonthDisabled(DatetimeParts parts, DatetimeParts? min, DatetimeParts? max) =>
        IsYearDisabled(parts.Year, min, max) || (min is { } lo && IsBefore(parts, lo)) || (max is { } hi && IsAfter(parts, hi));

    public static bool IsPrevMonthDisabled(DatetimeParts parts, DatetimeParts? min, DatetimeParts? max) =>
        IsMonthDisabled(GetPreviousMonth(parts) with { Day = null }, min, max);

    public static bool IsNextMonthDisabled(DatetimeParts parts, DatetimeParts? max) =>
        IsMonthDisabled(GetNextMonth(parts) with { Day = null }, null, max);

    /// <summary><c>getHighlightStyles</c>: the list's entry for the date, or the callback's style; an exception is logged.</summary>
    public static DatetimeHighlightStyle? GetHighlightStyles(DatetimeHighlightedDates highlighted, string dateIso, object? source)
    {
        if (highlighted.Dates is { } dates)
        {
            var date = dateIso.Split('T')[0];
            return dates.FirstOrDefault(d => d.Date == date);
        }

        try
        {
            return highlighted.Callback?.Invoke(dateIso);
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(source,
                "[ion-datetime] - Exception thrown from provided `highlightedDates` callback. Please check your function and try again. {Exception}", e);
            return null;
        }
    }
}
