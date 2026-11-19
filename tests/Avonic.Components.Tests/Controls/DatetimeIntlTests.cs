using System.Globalization;
using System.Text.Json;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-datetime</c>'s locale (B32): every <c>Intl</c> call Ionic's datetime makes, measured in Chromium for 20 locales in
/// <c>docs/components/ion-datetime/ref/intl.json</c>, against <c>DatetimeIntl</c>'s on .NET's <see cref="CultureInfo"/>. What
/// .NET can't match is listed in <see cref="Gaps"/>, and in the README's locale table, each with Chromium's and Avonic's text.
/// </summary>
public class DatetimeIntlTests
{
    private static readonly DatetimeParts June15 = new(6, 15, 2023, 13, 47);

    /// <summary>Locale and field where .NET's text differs from Chromium's, as the README records.</summary>
    internal static readonly HashSet<string> Gaps =
    [
        // .NET's long date has a comma after the weekday; CLDR's en-GB skeleton hasn't.
        "en-GB aria", "en-GB fmtDateFull",
        // .NET's Persian long date is year first; Intl's skeleton is weekday, day, month.
        "fa-IR combined", "fa-IR aria", "fa-IR fmtDateLong",
        // Intl writes a no-break space before г.; .NET's pattern a space.
        "ru-RU monthYear", "ru-RU fmtDateLong", "ru-RU fmtDateFull",
        // Intl's short weekday in brackets (ko, ja), with a dot (de), without "de" (es), without ที่ (th): CLDR skeletons .NET hasn't.
        "ko-KR combined", "ja-JP combined", "de-DE combined", "es-ES combined", "th-TH combined",
        // sv-SE's short date is year first in .NET; Intl's { month, day } puts the day first.
        "sv-SE monthFirstDay",
        // A 12-hour time asked of a 24-hour locale: .NET has no 12-hour pattern for it, so AM/PM goes after the time.
        "tr-TR time.h12", "tr-TR time.h11", "tr-TR time.h12am", "zh-CN time.h12", "zh-CN time.h11", "zh-CN time.h12am",
        "ja-JP time.h12", "ja-JP time.h11", "ja-JP time.h12am",
        // The era: .NET's long name for the Hijri era, and the Buddhist era where Intl leaves it out or puts it in.
        "ar-SA monthYear", "ar-SA year", "ar-SA fmtDateLong", "ar-SA fmtDateFull",
        "th-TH year", "th-TH fmtDateLong", "th-TH fmtDateFull",
    ];

    private static JsonElement Measured()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "docs", "components", "ion-datetime", "ref", "intl.json");
            if (File.Exists(path))
            {
                return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
            }
        }

        throw new InvalidOperationException("intl.json not found");
    }

    internal static Dictionary<string, (string Chromium, string Avonic)> Compare(string locale)
    {
        var m = Measured().GetProperty(locale);
        var c = CultureInfo.GetCultureInfo(locale);
        var result = new Dictionary<string, (string, string)>();
        void Check(string field, string chromium, string avonic) => result[field] = (chromium, avonic);

        Check("hourCycle", m.GetProperty("hourCycle").GetString()!, DatetimeIntl.GetHourCycle(c, null).ToString().ToLowerInvariant());
        Check("monthFirst", m.GetProperty("monthFirst").GetBoolean().ToString(), DatetimeIntl.IsMonthFirst(c).ToString());
        Check("monthFirstDay", m.GetProperty("monthFirstDay").GetBoolean().ToString(), DatetimeIntl.IsMonthFirst(c, againstDay: true).ToString());
        Check("dayPeriodFirst", m.GetProperty("dayPeriodFirst").GetBoolean().ToString(), DatetimeIntl.IsDayPeriodFirst(c).ToString());
        Check("monthYear", m.GetProperty("monthYear").GetString()!, DatetimeIntl.MonthAndYear(c, new DatetimeParts(6, 1, 2023)));
        Check("months", string.Join("|", m.GetProperty("months").EnumerateArray().Select(e => e.GetString())),
            string.Join("|", Enumerable.Range(1, 12).Select(i => DatetimeIntl.MonthOption(c, new DatetimeParts(i, 1, 2023)))));
        Check("daysNum", string.Join("|", m.GetProperty("daysNum").EnumerateArray().Select(e => e.GetString())),
            string.Join("|", new[] { 1, 9, 15, 31 }.Select(d => DatetimeIntl.DayOption(c, new DatetimeParts(1, d, 2023)))));
        Check("dayPart", m.GetProperty("dayPart").GetString()!, DatetimeIntl.Day(c, new DatetimeParts(6, 15, 2023)));
        Check("year", m.GetProperty("year").GetString()!, DatetimeIntl.Year(c, new DatetimeParts(6, 15, 2023)));
        Check("weekNarrow", string.Join("|", m.GetProperty("weekNarrow").EnumerateArray().Select(e => e.GetString())),
            string.Join("|", DatetimeIntl.DaysOfWeek(c, 0)));
        Check("combined", m.GetProperty("combined").GetString()!, DatetimeIntl.LocalizedDateTime(c, June15 with { Hour = null, Minute = null }, DatetimeIntl.ShortDate));
        Check("aria", m.GetProperty("aria").GetString()!, DatetimeIntl.DayAriaLabel(c, new DatetimeParts(6, 15, 2023), false));
        var time = m.GetProperty("time");
        Check("time.auto", time.GetProperty("auto").GetString()!, DatetimeIntl.LocalizedTime(c, June15, DatetimeIntl.GetHourCycle(c, null), null));
        Check("time.h12", time.GetProperty("h12").GetString()!, DatetimeIntl.LocalizedTime(c, June15, DatetimeHourCycle.H12, null));
        Check("time.h23", time.GetProperty("h23").GetString()!, DatetimeIntl.LocalizedTime(c, June15, DatetimeHourCycle.H23, null));
        Check("time.h11", time.GetProperty("h11").GetString()!, DatetimeIntl.LocalizedTime(c, June15 with { Hour = 0, Minute = 5 }, DatetimeHourCycle.H11, null));
        Check("time.h24", time.GetProperty("h24").GetString()!, DatetimeIntl.LocalizedTime(c, June15 with { Hour = 0, Minute = 5 }, DatetimeHourCycle.H24, null));
        Check("time.h12am", time.GetProperty("h12am").GetString()!, DatetimeIntl.LocalizedTime(c, June15 with { Hour = 0, Minute = 5 }, DatetimeHourCycle.H12, null));
        Check("time.h23am", time.GetProperty("h23am").GetString()!, DatetimeIntl.LocalizedTime(c, June15 with { Hour = 9, Minute = 5 }, DatetimeHourCycle.H23, null));
        Check("am", m.TryGetProperty("am", out var am) ? am.GetString()! : "AM", DatetimeIntl.DayPeriod(c, "am"));
        Check("pm", m.TryGetProperty("pm", out var pm) ? pm.GetString()! : "PM", DatetimeIntl.DayPeriod(c, "pm"));
        Check("today", m.GetProperty("today").GetString()!, DatetimeIntl.Today(c));
        Check("fmtDateLong", m.GetProperty("fmtDateLong").GetString()!, DatetimeIntl.LocalizedDateTime(c, new DatetimeParts(6, 15, 2023), new DateTimeFormatOptions { DateStyle = DateTimeFormatStyle.Long }));
        Check("fmtDateFull", m.GetProperty("fmtDateFull").GetString()!, DatetimeIntl.LocalizedDateTime(c, new DatetimeParts(6, 15, 2023), new DateTimeFormatOptions
        {
            Weekday = DateTimeFormatText.Long, Year = DateTimeFormatNumber.Numeric, Month = DateTimeFormatMonth.Long, Day = DateTimeFormatNumber.Numeric,
        }));
        Check("fmtTime2digit", m.GetProperty("fmtTime2digit").GetString()!, DatetimeIntl.LocalizedTime(c, June15 with { Hour = 13, Minute = 7 }, DatetimeIntl.GetHourCycle(c, null),
            new DateTimeFormatOptions { Hour = DateTimeFormatNumber.TwoDigit, Minute = DateTimeFormatNumber.TwoDigit }));
        return result;
    }

    public static TheoryData<string> Locales() =>
    [
        "en-US", "en-GB", "de-DE", "fr-FR", "es-ES", "pt-BR", "ru-RU", "ar-EG", "ar-SA", "he-IL", "fa-IR", "ja-JP", "zh-CN", "ko-KR",
        "hi-IN", "th-TH", "tr-TR", "nl-NL", "sv-SE", "pl-PL",
    ];

    [Theory]
    [MemberData(nameof(Locales))]
    public void Matches_Chromium_Except_The_Recorded_Gaps(string locale)
    {
        var wrong = Compare(locale)
            .Where(x => x.Value.Chromium != x.Value.Avonic && !Gaps.Contains($"{locale} {x.Key}"))
            .Select(x => $"{locale} {x.Key}: Chromium '{x.Value.Chromium}', Avonic '{x.Value.Avonic}'")
            .ToList();
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }
}
