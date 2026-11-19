namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>DatetimeParts</c>: the pieces of a date and time the datetime works with. Every field is optional, as Ionic's are: a
/// time alone has no year, month or day; a cell before the 1st has no day. JavaScript tells <c>null</c> (a padding day) from
/// <c>undefined</c> (not given), but every comparison Ionic makes treats them alike, so both are <c>null</c> here.
/// </summary>
internal readonly record struct DatetimeParts(
    int? Month,
    int? Day,
    int? Year,
    int? Hour = null,
    int? Minute = null,
    string? AmPm = null,
    int? DayOfWeek = null,
    bool IsAdjacentDay = false)
{
    /// <summary>Ionic's spread, <c>{ ...this, ...other }</c>, of the fields <paramref name="other"/> has.</summary>
    public DatetimeParts With(DatetimeParts other) => this with
    {
        Month = other.Month ?? Month,
        Day = other.Day ?? Day,
        Year = other.Year ?? Year,
        Hour = other.Hour ?? Hour,
        Minute = other.Minute ?? Minute,
        AmPm = other.AmPm ?? AmPm,
        DayOfWeek = other.DayOfWeek ?? DayOfWeek,
        IsAdjacentDay = other.IsAdjacentDay,
    };

    /// <summary>The date only, <c>{ month, day, year }</c>, as Ionic's calendar day gives.</summary>
    public DatetimeParts DateOnly => new(Month, Day, Year);
}
