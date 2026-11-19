using System.Globalization;
using System.Text;
using Avalonia.Logging;

namespace Avonic.Components.Controls;

/// <summary>
/// What Ionic's datetime asks <c>Intl</c> for, from .NET's <see cref="CultureInfo"/> (ADR 0004: measure where they differ;
/// <c>docs/components/ion-datetime/PLAN.md</c> has the measurements). .NET has fixed patterns where Intl builds one from the fields
/// asked for, so <see cref="Format"/> takes the locale's pattern that has those fields and drops the others with the literals that
/// belong to them. It writes each field itself, so that it has what .NET's format strings don't: the hour cycles <c>h11</c> and
/// <c>h24</c>, narrow day names, and the locale's own digits.
/// </summary>
internal static class DatetimeIntl
{
    /// <summary>Ionic's <c>locale</c>: a culture name, or <c>"default"</c> for the current culture, as Ionic's is the browser's.</summary>
    public static CultureInfo Culture(string? locale)
    {
        if (string.IsNullOrEmpty(locale) || locale == "default")
        {
            return CultureInfo.CurrentCulture;
        }

        try
        {
            return CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(null, "[ion-datetime] - Unknown locale {Locale}; using the current culture.", locale);
            return CultureInfo.CurrentCulture;
        }
    }

    // ── helpers.js ───────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>getHourCycle</c>: the one given, or the locale's, from its short time pattern.</summary>
    public static DatetimeHourCycle GetHourCycle(CultureInfo culture, DatetimeHourCycle? hourCycle) =>
        hourCycle ?? LocaleHourCycle(culture);

    private static DatetimeHourCycle LocaleHourCycle(CultureInfo culture)
    {
        foreach (var token in Tokenize(culture.DateTimeFormat.ShortTimePattern))
        {
            if (token.Field == 'h')
            {
                return DatetimeHourCycle.H12;
            }

            if (token.Field == 'H')
            {
                return DatetimeHourCycle.H23;
            }
        }

        return DatetimeHourCycle.H12;
    }

    public static bool Is24Hour(DatetimeHourCycle hourCycle) => hourCycle is DatetimeHourCycle.H23 or DatetimeHourCycle.H24;

    /// <summary>
    /// <c>isMonthFirstLocale</c>: whether the month comes before the year (<c>{ month: 'numeric', year: 'numeric' }</c>), or before
    /// the day (<c>{ month: 'numeric', day: 'numeric' }</c>), in the locale's short date.
    /// </summary>
    public static bool IsMonthFirst(CultureInfo culture, bool againstDay = false)
    {
        var other = againstDay ? 'd' : 'y';
        foreach (var token in Tokenize(culture.DateTimeFormat.ShortDatePattern))
        {
            if (token.Field == 'M')
            {
                return true;
            }

            if (token.Field == other)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary><c>isLocaleDayPeriodRTL</c>: whether the locale's hour starts with AM or PM (Korean).</summary>
    public static bool IsDayPeriodFirst(CultureInfo culture)
    {
        foreach (var token in Tokenize(culture.DateTimeFormat.ShortTimePattern))
        {
            if (token.Field is 't')
            {
                return true;
            }

            if (token.Field is 'h' or 'H')
            {
                return false;
            }
        }

        return false;
    }

    // ── format.js and data.js ────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic's <c>getNormalizedDate</c>: the parts as a date, 1 for a missing month or day, 2023 for a missing year.</summary>
    public static DateTime Normalize(DatetimeParts parts)
    {
        var year = Math.Clamp(parts.Year ?? 2023, 1, 9999);
        var month = Math.Clamp(parts.Month ?? 1, 1, 12);
        var day = Math.Clamp(parts.Day ?? 1, 1, DateTime.DaysInMonth(year, month));
        var date = new DateTime(year, month, day);
        return parts.Hour is { } h && parts.Minute is { } m ? date.AddHours(h).AddMinutes(m) : date;
    }

    /// <summary><c>getMonthAndYear</c>: <c>{ month: 'long', year: 'numeric' }</c>, as <c>June 2023</c>.</summary>
    public static string MonthAndYear(CultureInfo culture, DatetimeParts parts) =>
        Format(culture, Normalize(parts), new DateTimeFormatOptions { Month = DateTimeFormatMonth.Long, Year = DateTimeFormatNumber.Numeric });

    /// <summary><c>getDaysOfWeek</c> in md: the narrow names from <paramref name="firstDayOfWeek"/>.</summary>
    public static string[] DaysOfWeek(CultureInfo culture, int firstDayOfWeek)
    {
        var names = culture.DateTimeFormat.ShortestDayNames;
        return [.. Enumerable.Range(firstDayOfWeek, 7).Select(i => Digits(culture, names[((i % 7) + 7) % 7]))];
    }

    /// <summary><c>getDay</c>: the day number alone, as the day part of <c>{ day: 'numeric' }</c>, for a calendar cell.</summary>
    public static string Day(CultureInfo culture, DatetimeParts parts)
    {
        var date = Normalize(parts);
        return Digits(culture, culture.DateTimeFormat.Calendar.GetDayOfMonth(date).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>A day wheel's option: <c>{ day: 'numeric' }</c>, with a suffix where the locale has one (<c>15日</c>).</summary>
    public static string DayOption(CultureInfo culture, DatetimeParts parts) =>
        Format(culture, Normalize(parts), new DateTimeFormatOptions { Day = DateTimeFormatNumber.Numeric });

    /// <summary>A month wheel's option: <c>{ month: 'long' }</c>.</summary>
    public static string MonthOption(CultureInfo culture, DatetimeParts parts) =>
        Format(culture, Normalize(parts), new DateTimeFormatOptions { Month = DateTimeFormatMonth.Long });

    /// <summary><c>getYear</c>: <c>{ year: 'numeric' }</c>.</summary>
    public static string Year(CultureInfo culture, DatetimeParts parts) =>
        Format(culture, Normalize(parts), new DateTimeFormatOptions { Year = DateTimeFormatNumber.Numeric });

    /// <summary>The date wheel's option and the header's default: <c>{ weekday: 'short', month: 'short', day: 'numeric' }</c>.</summary>
    public static readonly DateTimeFormatOptions ShortDate = new()
    {
        Weekday = DateTimeFormatText.Short, Month = DateTimeFormatMonth.Short, Day = DateTimeFormatNumber.Numeric,
    };

    /// <summary><c>generateDayAriaLabel</c>: <c>{ weekday: 'long', month: 'long', day: 'numeric' }</c>, after <c>Today, </c> for today.</summary>
    public static string DayAriaLabel(CultureInfo culture, DatetimeParts parts, bool isToday)
    {
        var label = Format(culture, Normalize(parts), new DateTimeFormatOptions
        {
            Weekday = DateTimeFormatText.Long, Month = DateTimeFormatMonth.Long, Day = DateTimeFormatNumber.Numeric,
        });
        return isToday ? $"Today, {label}" : label;
    }

    /// <summary><c>getLocalizedDateTime</c>.</summary>
    public static string LocalizedDateTime(CultureInfo culture, DatetimeParts parts, DateTimeFormatOptions options) =>
        Format(culture, Normalize(parts), options);

    /// <summary><c>getLocalizedTime</c>: <c>{ hour: 'numeric', minute: 'numeric' }</c> or the given options, with the hour cycle.</summary>
    public static string LocalizedTime(CultureInfo culture, DatetimeParts parts, DatetimeHourCycle hourCycle, DateTimeFormatOptions? options)
    {
        if (parts.Hour is not { } hour || parts.Minute is not { } minute)
        {
            return "Invalid Time";
        }

        options ??= new DateTimeFormatOptions { Hour = DateTimeFormatNumber.Numeric, Minute = DateTimeFormatNumber.Numeric };
        var withCycle = new DateTimeFormatOptions
        {
            Weekday = options.Weekday, Era = options.Era, Year = options.Year, Month = options.Month, Day = options.Day,
            Hour = options.Hour, Minute = options.Minute, Second = options.Second, Hour12 = options.Hour12,
            DateStyle = options.DateStyle, TimeStyle = options.TimeStyle,
            // Ionic passes hourCycle after the options, so it wins over theirs, as Intl takes hourCycle over hour12 only when
            // hour12 isn't set.
            HourCycle = options.Hour12 is null ? hourCycle : options.HourCycle,
        };
        return Format(culture, new DateTime(2023, 1, 1, hour % 24, minute % 60, 0), withCycle);
    }

    /// <summary>
    /// <c>getLocalizedDayPeriod</c>: the locale's AM or PM where its hour shows one, else <c>AM</c> and <c>PM</c>, Ionic's fallback.
    /// </summary>
    public static string DayPeriod(CultureInfo culture, string dayPeriod)
    {
        var info = culture.DateTimeFormat;
        var shows = Tokenize(info.ShortTimePattern).Any(t => t.Field == 't');
        var text = dayPeriod == "am" ? info.AMDesignator : info.PMDesignator;
        return shows && !string.IsNullOrEmpty(text) ? text : dayPeriod.ToUpperInvariant();
    }

    /// <summary>
    /// <c>getTodayLabel</c>: <c>Intl.RelativeTimeFormat</c>'s "today", capitalised, which .NET doesn't have: the languages measured
    /// in Chromium, and English otherwise.
    /// </summary>
    public static string Today(CultureInfo culture) => TodayWords.TryGetValue(culture.TwoLetterISOLanguageName, out var word) ? word : "Today";

    private static readonly Dictionary<string, string> TodayWords = new()
    {
        ["en"] = "Today", ["de"] = "Heute", ["fr"] = "Aujourd’hui", ["es"] = "Hoy", ["pt"] = "Hoje", ["ru"] = "Сегодня",
        ["ar"] = "اليوم", ["he"] = "היום", ["fa"] = "امروز", ["ja"] = "今日", ["zh"] = "今天", ["ko"] = "오늘", ["hi"] = "आज",
        ["th"] = "วันนี้", ["tr"] = "Bugün", ["nl"] = "Vandaag", ["sv"] = "I dag", ["pl"] = "Dzisiaj", ["it"] = "Oggi",
    };

    // ── The formatter ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>A piece of a .NET pattern: a field (its letter and how many) or literal text.</summary>
    internal readonly record struct Token(char Field, int Count, string Text)
    {
        public bool IsField => Field != '\0';
    }

    private const string FieldLetters = "dMyghHmstfFzK";

    /// <summary>Splits a .NET date pattern into fields and literals, the separators <c>/</c> and <c>:</c> as the culture's.</summary>
    internal static List<Token> Tokenize(string pattern, DateTimeFormatInfo? info = null)
    {
        var tokens = new List<Token>();
        var literal = new StringBuilder();
        void Flush()
        {
            if (literal.Length > 0)
            {
                tokens.Add(new Token('\0', 0, literal.ToString()));
                literal.Clear();
            }
        }

        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c is '\'' or '"')
            {
                var end = pattern.IndexOf(c, i + 1);
                end = end < 0 ? pattern.Length : end;
                literal.Append(pattern, i + 1, end - i - 1);
                i = end;
            }
            else if (c == '\\' && i + 1 < pattern.Length)
            {
                literal.Append(pattern[++i]);
            }
            else if (c == '%')
            {
            }
            else if (c == '/')
            {
                literal.Append(info?.DateSeparator ?? "/");
            }
            else if (c == ':')
            {
                literal.Append(info?.TimeSeparator ?? ":");
            }
            else if (FieldLetters.Contains(c))
            {
                var count = 1;
                while (i + count < pattern.Length && pattern[i + count] == c)
                {
                    count++;
                }

                Flush();
                tokens.Add(new Token(c, count, string.Empty));
                i += count - 1;
            }
            else
            {
                literal.Append(c);
            }
        }

        Flush();
        return tokens;
    }

    /// <summary>
    /// Writes <paramref name="date"/> with the fields <paramref name="options"/> asks for, in the locale's order: Intl's
    /// <c>format</c>, as near as .NET's patterns allow.
    /// </summary>
    public static string Format(CultureInfo culture, DateTime date, DateTimeFormatOptions options)
    {
        var info = culture.DateTimeFormat;
        string? datePart = null;
        string? timePart = null;
        if (options.DateStyle is not null || options.TimeStyle is not null)
        {
            if (options.DateStyle is { } ds)
            {
                datePart = ds switch
                {
                    DateTimeFormatStyle.Full => Render(culture, date, Tokenize(info.LongDatePattern, info), null),
                    DateTimeFormatStyle.Long => Render(culture, date, Subset(Tokenize(info.LongDatePattern, info), "Mdyg"), null),
                    DateTimeFormatStyle.Medium => Render(culture, date, Widen(Subset(Tokenize(info.LongDatePattern, info), "Mdyg"), month: DateTimeFormatMonth.Short), null),
                    _ => Render(culture, date, Tokenize(info.ShortDatePattern, info), null),
                };
            }

            if (options.TimeStyle is { } ts)
            {
                var pattern = ts == DateTimeFormatStyle.Short ? info.ShortTimePattern : info.LongTimePattern;
                timePart = Render(culture, date, Tokenize(pattern, info), CycleFor(culture, options));
            }
        }
        else
        {
            if (options.HasDateField || !options.HasTimeField)
            {
                datePart = Render(culture, date, DateTokens(culture, options), null);
            }

            if (options.HasTimeField)
            {
                timePart = Render(culture, date, TimeTokens(culture, options), CycleFor(culture, options));
            }
        }

        var text = datePart is null ? timePart ?? string.Empty : timePart is null ? datePart : $"{datePart}, {timePart}";
        return Digits(culture, text.Trim());
    }

    private static DatetimeHourCycle CycleFor(CultureInfo culture, DateTimeFormatOptions options)
    {
        var locale = LocaleHourCycle(culture);
        return options.Hour12 switch
        {
            true => locale == DatetimeHourCycle.H11 ? DatetimeHourCycle.H11 : DatetimeHourCycle.H12,
            false => DatetimeHourCycle.H23,
            _ => options.HourCycle ?? locale,
        };
    }

    /// <summary>The date's fields from the locale's pattern that has them.</summary>
    private static List<Token> DateTokens(CultureInfo culture, DateTimeFormatOptions options)
    {
        var info = culture.DateTimeFormat;
        var asked = new StringBuilder();
        if (options.Weekday is not null)
        {
            asked.Append("dddd");
        }

        if (options.Day is not null)
        {
            asked.Append('d');
        }

        if (options.Month is not null)
        {
            asked.Append('M');
        }

        if (options.Year is not null)
        {
            asked.Append('y');
        }

        if (options.Era is not null)
        {
            asked.Append('g');
        }

        if (asked.Length == 0)
        {
            // Intl's default: year, month and day, numeric.
            return Tokenize(info.ShortDatePattern, info);
        }

        var hasWeekday = options.Weekday is not null;
        var hasDay = options.Day is not null;
        var hasMonth = options.Month is not null;
        var hasYear = options.Year is not null;
        var numericMonth = options.Month is DateTimeFormatMonth.Numeric or DateTimeFormatMonth.TwoDigit;

        List<Token> tokens;
        if (hasWeekday && !hasDay && !hasMonth && !hasYear)
        {
            tokens = [new Token('d', 4, string.Empty)];
        }
        else if (hasDay && !hasMonth && !hasYear && !hasWeekday)
        {
            // The day alone, with the letters the locale writes right after it (15日, 15일).
            tokens = [new Token('d', 1, string.Empty)];
            var monthDay = Tokenize(info.MonthDayPattern, info);
            var at = monthDay.FindIndex(t => t.Field == 'd');
            if (at >= 0 && at + 1 < monthDay.Count && !monthDay[at + 1].IsField)
            {
                var suffix = new string(monthDay[at + 1].Text.TakeWhile(char.IsLetter).ToArray());
                if (suffix.Length > 0)
                {
                    tokens.Add(new Token('\0', 0, suffix));
                }
            }
        }
        else if (hasMonth && !hasDay && !hasWeekday && !hasYear)
        {
            tokens = [new Token('M', 4, string.Empty)];
        }
        else if (!hasDay && !hasWeekday && !numericMonth)
        {
            // Month and year, or the year alone: the locale's year-month pattern.
            tokens = Subset(Tokenize(info.YearMonthPattern, info), asked.ToString());
            if (!hasMonth && tokens.Count > 0 && !tokens[^1].IsField)
            {
                // The year alone keeps only the letters stuck to it (2023年), not a word after a space (2023 г.).
                var suffix = Split(tokens[^1].Text).Suffix;
                tokens[^1] = tokens[^1] with { Text = suffix };
            }
        }
        else if (numericMonth && !hasWeekday)
        {
            tokens = Subset(Tokenize(info.ShortDatePattern, info), asked.ToString());
        }
        else
        {
            tokens = Subset(Tokenize(info.LongDatePattern, info), asked.ToString());
            if (!tokens.Any(t => t.Field == 'd' && t.Count >= 3) && hasWeekday)
            {
                tokens.Insert(0, new Token('\0', 0, ", "));
                tokens.Insert(0, new Token('d', 4, string.Empty));
            }
        }

        return Widen(tokens, options.Weekday, options.Month, options.Day, options.Year, options.Era);
    }

    /// <summary>The time's fields from the locale's short or long time pattern.</summary>
    private static List<Token> TimeTokens(CultureInfo culture, DateTimeFormatOptions options)
    {
        var info = culture.DateTimeFormat;
        var pattern = options.Second is not null ? info.LongTimePattern : info.ShortTimePattern;
        var asked = new StringBuilder();
        if (options.Hour is not null)
        {
            asked.Append("hHt");
        }

        if (options.Minute is not null)
        {
            asked.Append('m');
        }

        if (options.Second is not null)
        {
            asked.Append('s');
        }

        var tokens = Subset(Tokenize(pattern, info), asked.ToString());
        return [.. tokens.Select(t => t.Field switch
        {
            'h' or 'H' when options.Hour == DateTimeFormatNumber.TwoDigit => t with { Count = 2 },
            'm' when options.Hour is not null || options.Minute == DateTimeFormatNumber.TwoDigit => t with { Count = 2 },
            'm' => t with { Count = 1 },
            's' when options.Minute is not null || options.Second == DateTimeFormatNumber.TwoDigit => t with { Count = 2 },
            's' => t with { Count = 1 },
            _ => t,
        })];
    }

    /// <summary>Sets each field's width to what's asked for.</summary>
    private static List<Token> Widen(List<Token> tokens, DateTimeFormatText? weekday = null, DateTimeFormatMonth? month = null,
        DateTimeFormatNumber? day = null, DateTimeFormatNumber? year = null, DateTimeFormatText? era = null) =>
        [.. tokens.Select(t => t.Field switch
        {
            'd' when t.Count >= 3 && weekday is { } w => t with { Count = w switch { DateTimeFormatText.Long => 4, DateTimeFormatText.Short => 3, _ => 5 } },
            'd' when t.Count < 3 && day is { } d => t with { Count = d == DateTimeFormatNumber.TwoDigit ? 2 : 1 },
            'M' when month is { } m && !(t.Count <= 2 && m is DateTimeFormatMonth.Long or DateTimeFormatMonth.Short or DateTimeFormatMonth.Narrow) => t with
            {
                Count = m switch
                {
                    DateTimeFormatMonth.Numeric => 1, DateTimeFormatMonth.TwoDigit => 2, DateTimeFormatMonth.Short => 3,
                    DateTimeFormatMonth.Long => 4, _ => 5,
                },
            },
            'y' when year is { } y => t with { Count = y == DateTimeFormatNumber.TwoDigit ? 2 : 1 },
            'g' when era is { } e => t with { Count = e == DateTimeFormatText.Long ? 2 : 1 },
            _ => t,
        })];

    /// <summary>
    /// The fields whose letters are in <paramref name="keep"/> (<c>dddd</c> for the weekday, <c>d</c> for the day, and so on), with
    /// the literals that belong to them: between two kept fields, the separator; after a kept field, the letters stuck to it (日, 年);
    /// before one, the letters stuck to it after a space (Hebrew's ב); at the end, all of it.
    /// </summary>
    internal static List<Token> Subset(List<Token> tokens, string keep)
    {
        bool Kept(Token t) => t.Field switch
        {
            'd' when t.Count >= 3 => keep.Contains("dddd", StringComparison.Ordinal),
            'd' => keep.Replace("dddd", string.Empty, StringComparison.Ordinal).Contains('d'),
            'g' => keep.Contains('g') || keep.Contains('y'),
            _ => keep.Contains(t.Field),
        };

        var fields = tokens.Select((t, i) => (t, i)).Where(x => x.t.IsField).Select(x => x.i).ToList();
        var result = new List<Token>();
        int? lastKept = null;
        string? pendingSeparator = null;
        for (var f = 0; f < fields.Count; f++)
        {
            var index = fields[f];
            var token = tokens[index];
            var before = index > 0 && !tokens[index - 1].IsField ? tokens[index - 1].Text : null;
            var after = index + 1 < tokens.Count && !tokens[index + 1].IsField ? tokens[index + 1].Text : null;
            var isLastField = f == fields.Count - 1;
            if (!Kept(token))
            {
                if (lastKept is not null && pendingSeparator is null && after is not null)
                {
                    pendingSeparator = Split(after).Separator;
                }

                continue;
            }

            if (lastKept is null)
            {
                // A literal at the very start belongs to the first field.
                if (f == 0 && before is not null)
                {
                    result.Add(new Token('\0', 0, before));
                }
                else if (before is not null)
                {
                    var prefix = Split(before).Prefix;
                    if (prefix.Length > 0)
                    {
                        result.Add(new Token('\0', 0, prefix));
                    }
                }
            }
            else
            {
                var between = before is not null ? Split(before) : default;
                var adjacent = fields[f - 1] == lastKept;
                var separator = adjacent ? between.Separator : pendingSeparator ?? between.Separator;
                if (!string.IsNullOrEmpty(separator))
                {
                    result.Add(new Token('\0', 0, separator));
                }

                if (!string.IsNullOrEmpty(between.Prefix))
                {
                    result.Add(new Token('\0', 0, between.Prefix));
                }
            }

            result.Add(token);
            lastKept = index;
            pendingSeparator = null;
            if (after is not null)
            {
                var suffix = isLastField ? after : Split(after).Suffix;
                if (suffix.Length > 0)
                {
                    result.Add(new Token('\0', 0, suffix));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// A literal between two fields: the letters stuck to the field before it, the separator, and the letters stuck to the field
    /// after it (only after white space, so that 年 in 2023年6月 belongs to the year).
    /// </summary>
    private static (string Suffix, string Separator, string Prefix) Split(string literal)
    {
        var suffixLength = 0;
        while (suffixLength < literal.Length && (char.IsLetter(literal[suffixLength]) || literal[suffixLength] == '.' && suffixLength > 0))
        {
            suffixLength++;
        }

        var rest = literal[suffixLength..];
        var lastSpace = rest.LastIndexOfAny([' ', ' ', ' ']);
        var prefix = lastSpace >= 0 && rest[(lastSpace + 1)..].All(char.IsLetter) ? rest[(lastSpace + 1)..] : string.Empty;
        var separator = rest[..(rest.Length - prefix.Length)];
        return (literal[..suffixLength], separator, prefix);
    }

    /// <summary>Writes each field: names from the culture, numbers from its calendar, the hour in its cycle.</summary>
    private static string Render(CultureInfo culture, DateTime date, List<Token> tokens, DatetimeHourCycle? hourCycle)
    {
        var info = culture.DateTimeFormat;
        var calendar = info.Calendar;
        var hasDay = tokens.Any(t => t.Field == 'd' && t.Count <= 2);
        var tokensWithCycle = hourCycle is { } cycle ? ApplyCycle(tokens, cycle) : tokens;
        var text = new StringBuilder();
        foreach (var token in tokensWithCycle)
        {
            if (!token.IsField)
            {
                text.Append(token.Text);
                continue;
            }

            var n = token.Count;
            switch (token.Field)
            {
                case 'd' when n >= 5:
                    text.Append(info.ShortestDayNames[(int)calendar.GetDayOfWeek(date)]);
                    break;
                case 'd' when n == 4:
                    text.Append(info.DayNames[(int)calendar.GetDayOfWeek(date)]);
                    break;
                case 'd' when n == 3:
                    text.Append(info.AbbreviatedDayNames[(int)calendar.GetDayOfWeek(date)]);
                    break;
                case 'd':
                    text.Append(Number(calendar.GetDayOfMonth(date), n));
                    break;
                case 'M':
                {
                    var month = calendar.GetMonth(date);
                    text.Append(n switch
                    {
                        >= 5 => MonthName(info, month, hasDay, abbreviated: true)[..1],
                        4 => MonthName(info, month, hasDay, abbreviated: false),
                        3 => MonthName(info, month, hasDay, abbreviated: true),
                        _ => Number(month, n),
                    });
                    break;
                }

                case 'y':
                {
                    var year = calendar.GetYear(date);
                    text.Append(n == 2 ? Number(year % 100, 2) : year.ToString(CultureInfo.InvariantCulture));
                    break;
                }

                case 'g':
                    text.Append(info.GetEraName(calendar.GetEra(date)));
                    break;
                case 'h':
                {
                    var hour = date.Hour % 12;
                    text.Append(Number(hour == 0 ? 12 : hour, n));
                    break;
                }

                case 'K':
                    text.Append(Number(date.Hour % 12, n));
                    break;
                case 'H':
                    text.Append(Number(date.Hour, n));
                    break;
                case 'k':
                    text.Append(Number(date.Hour == 0 ? 24 : date.Hour, n));
                    break;
                case 'm':
                    text.Append(Number(date.Minute, n));
                    break;
                case 's':
                    text.Append(Number(date.Second, n));
                    break;
                case 't':
                    text.Append(date.Hour < 12 ? info.AMDesignator : info.PMDesignator);
                    break;
            }
        }

        return text.ToString();
    }

    private static string MonthName(DateTimeFormatInfo info, int month, bool withDay, bool abbreviated)
    {
        var names = abbreviated
            ? withDay ? info.AbbreviatedMonthGenitiveNames : info.AbbreviatedMonthNames
            : withDay ? info.MonthGenitiveNames : info.MonthNames;
        var name = month - 1 < names.Length ? names[month - 1] : string.Empty;
        return string.IsNullOrEmpty(name) ? (abbreviated ? info.AbbreviatedMonthNames : info.MonthNames)[month - 1] : name;
    }

    /// <summary>The hour field in the cycle asked for, with AM or PM added or removed.</summary>
    private static List<Token> ApplyCycle(List<Token> tokens, DatetimeHourCycle cycle)
    {
        var twelve = !Is24Hour(cycle);
        var letter = cycle switch { DatetimeHourCycle.H11 => 'K', DatetimeHourCycle.H12 => 'h', DatetimeHourCycle.H23 => 'H', _ => 'k' };
        var hasHour = tokens.Any(t => t.Field is 'h' or 'H');
        var result = new List<Token>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Field is 'h' or 'H')
            {
                // Intl's numeric hour: one digit when a 24-hour pattern turns 12-hour, two when a 12-hour one turns 24-hour.
                var changed = (t.Field == 'h') == !twelve;
                result.Add(t with { Field = letter, Count = changed ? (twelve ? 1 : 2) : t.Count });
            }
            else if (t.Field == 't' && !twelve)
            {
                // Drop AM/PM and the space between it and the time.
                if (result.Count > 0 && !result[^1].IsField && string.IsNullOrWhiteSpace(result[^1].Text))
                {
                    result.RemoveAt(result.Count - 1);
                }
                else if (i + 1 < tokens.Count && !tokens[i + 1].IsField && string.IsNullOrWhiteSpace(tokens[i + 1].Text))
                {
                    i++;
                }
            }
            else
            {
                result.Add(t);
            }
        }

        if (twelve && hasHour && !result.Any(t => t.Field == 't'))
        {
            result.Add(new Token('\0', 0, " "));
            result.Add(new Token('t', 2, string.Empty));
        }

        return result;
    }

    private static string Number(int value, int digits) =>
        value.ToString(digits >= 2 ? "00" : "0", CultureInfo.InvariantCulture);

    /// <summary>The locale's own digits, where .NET says it substitutes them (Arabic and Persian), as Intl writes them.</summary>
    internal static string Digits(CultureInfo culture, string text)
    {
        var number = culture.NumberFormat;
        if (number.DigitSubstitution == DigitShapes.None || number.NativeDigits is not { Length: 10 } native || native[0] == "0")
        {
            return text;
        }

        var result = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsAsciiDigit(c))
            {
                result.Append(native[c - '0']);
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
