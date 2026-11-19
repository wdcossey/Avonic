using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Ionic's <c>datetime/utils/</c> as Avonic ports them: <c>parse.js</c>, <c>manipulation.js</c>, <c>comparison.js</c>,
/// <c>data.js</c> and <c>state.js</c>, each case read from the <c>.js</c> (B1 to B4, B11, B15, B21 in PLAN.md).
/// </summary>
public class DatetimeUtilsTests
{
    [Theory]
    [InlineData("2023-06-15T13:47:00", 2023, 6, 15, 13, 47)]
    [InlineData("2023-06-15T13:47:00Z", 2023, 6, 15, 13, 47)]
    [InlineData("2023-06-15T13:47:00+05:30", 2023, 6, 15, 13, 47)]
    [InlineData("2023-06-15T13:47:00.123-08:00", 2023, 6, 15, 13, 47)]
    [InlineData("2023-06-15", 2023, 6, 15, null, null)]
    [InlineData("2023-06", 2023, 6, null, null, null)]
    [InlineData("1994", 1994, null, null, null, null)]
    [InlineData("13:47", null, null, null, 13, 47)]
    public void B3_ParseDate_Reads_The_Wall_Clock_And_Drops_The_Zone(string value, int? year, int? month, int? day, int? hour, int? minute)
    {
        var parts = DatetimeUtils.ParseDate(value)!.Value;
        Assert.Equal((year, month, day, hour, minute), (parts.Year, parts.Month, parts.Day, parts.Hour, parts.Minute));
    }

    [Theory]
    [InlineData("")]
    [InlineData("June 15")]
    [InlineData("2023-6-15")]
    [InlineData("2023-06-15 13:47")]
    public void B2_ParseDate_Rejects_What_Isnt_Iso(string value) => Assert.Null(DatetimeUtils.ParseDate(value));

    [Fact]
    public void B2_The_AmPm_Is_Am_Before_Noon_And_Pm_With_No_Hour()
    {
        Assert.Equal("am", DatetimeUtils.ParseDate("2023-06-15T11:59")!.Value.AmPm);
        Assert.Equal("pm", DatetimeUtils.ParseDate("2023-06-15T12:00")!.Value.AmPm);
        // undefined < 12 is false in JavaScript.
        Assert.Equal("pm", DatetimeUtils.ParseDate("2023-06-15")!.Value.AmPm);
    }

    [Theory]
    [InlineData(2023, 6, 15, 13, 47, "2023-06-15T13:47:00")]
    [InlineData(2023, 6, 15, null, null, "2023-06-15")]
    [InlineData(2023, 6, null, null, null, "2023-06")]
    [InlineData(null, null, null, 9, 5, "09:05")]
    [InlineData(5, 1, 2, null, null, "0005-01-02")]
    public void B4_ConvertDataToIso_Writes_What_The_Parts_Have(int? year, int? month, int? day, int? hour, int? minute, string expected) =>
        Assert.Equal(expected, DatetimeUtils.ConvertDataToIso(new DatetimeParts(month, day, year, hour, minute)));

    [Theory]
    [InlineData("0,15,30,45", new[] { 0, 15, 30, 45 })]
    [InlineData("[1, 2, 3]", new[] { 1, 2, 3 })]
    [InlineData("6, x, 8", new[] { 6, 8 })]
    [InlineData("12abc", new[] { 12 })]
    public void ValueLists_Read_As_ConvertToArrayOfNumbers(string text, int[] expected) =>
        Assert.Equal(expected, ((DatetimeValueList)text).Values);

    [Fact]
    public void ValueLists_Take_A_Number_And_Numbers()
    {
        Assert.Equal([7], ((DatetimeValueList)7).Values);
        Assert.Equal([1, 2], ((DatetimeValueList)new[] { 1, 2 }).Values);
    }

    [Fact]
    public void Min_And_Max_Fill_What_They_Leave_Out()
    {
        var today = new DatetimeParts(6, 20, 2023);
        Assert.Equal(new DatetimeParts(12, 31, 2024, 23, 59), DatetimeUtils.ParseMaxParts("2024", today));
        Assert.Equal(new DatetimeParts(2, 29, 2024, 23, 59), DatetimeUtils.ParseMaxParts("2024-02", today));
        Assert.Equal(new DatetimeParts(1, 1, 1994, 0, 0), DatetimeUtils.ParseMinParts("1994", today));
        Assert.Equal(new DatetimeParts(6, 15, 2023, 9, 30), DatetimeUtils.ParseMinParts("2023-06-15T09:30", today));
    }

    [Fact]
    public void B5_ValidateParts_Clamps_The_Date_Cuts_The_Day_And_Moves_The_Time_On_Min_And_Max_Days()
    {
        var min = new DatetimeParts(6, 15, 2023, 9, 30);
        var max = new DatetimeParts(6, 20, 2023, 17, 15);
        Assert.Equal(min, DatetimeUtils.ValidateParts(new DatetimeParts(6, 1, 2023, 8, 0), min, max));
        Assert.Equal(new DatetimeParts(6, 15, 2023, 9, 30), DatetimeUtils.ValidateParts(new DatetimeParts(6, 15, 2023, 8, 0), min, max));
        Assert.Equal(new DatetimeParts(6, 15, 2023, 9, 30), DatetimeUtils.ValidateParts(new DatetimeParts(6, 15, 2023, 9, 10), min, max));
        Assert.Equal(new DatetimeParts(6, 20, 2023, 17, 15), DatetimeUtils.ValidateParts(new DatetimeParts(6, 20, 2023, 18, 0), min, max));
        Assert.Equal(new DatetimeParts(2, 28, 2023), DatetimeUtils.ValidateParts(new DatetimeParts(2, 31, 2023), null, null));
    }

    [Fact]
    public void ValidateParts_Keeps_A_Date_Clamped_To_Max_Whole()
    {
        // ADR 0005: Ionic cuts the 29th to February's 28 days after clamping to max, writing 2021-01-28, past max.
        var max = new DatetimeParts(1, 15, 2021, 23, 59);
        Assert.Equal(max, DatetimeUtils.ValidateParts(new DatetimeParts(2, 29, 2021, 10, 0), null, max));
    }

    [Theory]
    [InlineData("1px solid green", 1, true)]
    [InlineData("1px green", 0, false)]
    [InlineData("red", 0, false)]
    [InlineData("0 solid red", 0, true)]
    [InlineData("thin solid blue", 1, true)]
    [InlineData("solid", 3, false)]
    [InlineData("2px dashed rgb(0, 128, 0)", 2, true)]
    public void A_Highlight_Border_Reads_Css_Shorthand(string css, double width, bool hasColour)
    {
        var (thickness, brush) = DatetimeCalendarDay.ParseBorder(css);
        Assert.Equal(width, thickness);
        Assert.Equal(hasColour, brush is not null);
    }

    [Fact]
    public void B19_Day_Arithmetic_Crosses_Months_And_Years_And_Keeps_The_Day_In_The_Month()
    {
        Assert.Equal(new DatetimeParts(7, 1, 2023), DatetimeUtils.GetNextDay(new DatetimeParts(6, 30, 2023)));
        Assert.Equal(new DatetimeParts(12, 31, 2022), DatetimeUtils.GetPreviousDay(new DatetimeParts(1, 1, 2023)));
        Assert.Equal(new DatetimeParts(1, 4, 2024), DatetimeUtils.GetNextWeek(new DatetimeParts(12, 28, 2023)));
        Assert.Equal(new DatetimeParts(2, 28, 2023), DatetimeUtils.GetNextMonth(new DatetimeParts(1, 31, 2023)));
        Assert.Equal(new DatetimeParts(2, 28, 2025), DatetimeUtils.GetNextYear(new DatetimeParts(2, 29, 2024)));
        // Home and End: the start and end of the grid's row, the day of week being the column.
        Assert.Equal(new DatetimeParts(6, 11, 2023), DatetimeUtils.GetStartOfWeek(new DatetimeParts(6, 15, 2023, DayOfWeek: 4)));
        Assert.Equal(new DatetimeParts(6, 17, 2023), DatetimeUtils.GetEndOfWeek(new DatetimeParts(6, 15, 2023, DayOfWeek: 4)));
    }

    [Fact]
    public void B21_Am_And_Pm_Move_The_Hour_By_Twelve()
    {
        Assert.Equal(13, DatetimeUtils.CalculateHourFromAmPm(new DatetimeParts(6, 15, 2023, 1, 0, "am"), "pm"));
        Assert.Equal(12, DatetimeUtils.CalculateHourFromAmPm(new DatetimeParts(6, 15, 2023, 12, 0, "am"), "pm"));
        Assert.Equal(3, DatetimeUtils.CalculateHourFromAmPm(new DatetimeParts(6, 15, 2023, 15, 0, "pm"), "am"));
        Assert.Equal(0, DatetimeUtils.CalculateHourFromAmPm(new DatetimeParts(6, 15, 2023, 12, 0, "pm"), "am"));
    }

    [Fact]
    public void B11_A_Month_Has_Blanks_From_The_First_Day_Of_Week()
    {
        // June 2023 starts on a Thursday.
        Assert.Equal(4, DatetimeUtils.GetDaysOfMonth(6, 2023, 0, false).TakeWhile(c => c.Day is null).Count());
        Assert.Equal(3, DatetimeUtils.GetDaysOfMonth(6, 2023, 1, false).TakeWhile(c => c.Day is null).Count());
        // October 2023 starts on a Sunday: none.
        Assert.Equal(1, DatetimeUtils.GetDaysOfMonth(10, 2023, 0, false)[0].Day);
        Assert.Equal(42, DatetimeUtils.GetDaysOfMonth(2, 2026, 0, true).Count);
    }

    [Fact]
    public void Today_Is_Snapped_To_The_Value_Lists_Within_Min_And_Max()
    {
        var today = new DatetimeParts(6, 20, 2023, 10, 7, "am");
        var closest = DatetimeUtils.GetClosestValidDate(today, [3, 9], [1, 15, 30], [2020, 2024], [6, 18], [0, 15, 30, 45], null, null);
        // A tie (March and September are both 3 from June) keeps the first, as findClosestValue does.
        Assert.Equal((2024, 3, 15, 6, 0), (closest.Year!.Value, closest.Month!.Value, closest.Day!.Value, closest.Hour!.Value, closest.Minute!.Value));
    }

    [Fact]
    public void B21_The_Hour_Wheel_Writes_Each_Cycle()
    {
        Assert.Equal("12", DatetimeUtils.FormattedHour(0, DatetimeHourCycle.H12));
        Assert.Equal("0", DatetimeUtils.FormattedHour(0, DatetimeHourCycle.H11));
        Assert.Equal("00", DatetimeUtils.FormattedHour(0, DatetimeHourCycle.H23));
        Assert.Equal("24", DatetimeUtils.FormattedHour(0, DatetimeHourCycle.H24));
        Assert.Equal("09", DatetimeUtils.FormattedHour(9, DatetimeHourCycle.H23));
        Assert.Equal("9", DatetimeUtils.FormattedHour(9, DatetimeHourCycle.H12));
    }

    [Fact]
    public void B13_The_Previous_And_Next_Months_Are_Disabled_Outside_Min_And_Max()
    {
        var min = new DatetimeParts(6, 10, 2023);
        var max = new DatetimeParts(7, 20, 2023);
        Assert.True(DatetimeUtils.IsPrevMonthDisabled(new DatetimeParts(6, 15, 2023), min, max));
        Assert.False(DatetimeUtils.IsNextMonthDisabled(new DatetimeParts(6, 15, 2023), max));
        Assert.True(DatetimeUtils.IsNextMonthDisabled(new DatetimeParts(7, 15, 2023), max));
    }

    [Fact]
    public void Parts_Print_Without_Recursion_As_The_Out_Of_Bounds_Warning_Logs_Them() =>
        Assert.Contains("Month = 6", new DatetimeParts(6, 15, 2023).ToString(), StringComparison.Ordinal);

    [Fact]
    public void Value_Converts_From_Text_And_Lists_And_Writes_Ionics_FormatValue()
    {
        var converter = new DatetimeValueTypeConverter();
        var single = (DatetimeValue)converter.ConvertFrom("2023-06-15")!;
        Assert.False(single.IsList);
        Assert.Equal("2023-06-15", single.ToString());
        DatetimeValue list = new[] { "2023-06-05", "2023-06-09" };
        Assert.True(list.IsList);
        Assert.Equal("2023-06-05,2023-06-09", list.ToString());
        Assert.Equal("2023-06-09", list.Single);
        Assert.False(new DatetimeValue(Array.Empty<string>()).HasValue);
    }
}
