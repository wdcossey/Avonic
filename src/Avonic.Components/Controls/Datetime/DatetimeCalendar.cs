using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>.calendar-day</c>, <c>part="calendar-day"</c>: a day of the calendar grid, a native button the datetime makes for each
/// day. Styled as <c>c|DatetimeCalendarDay</c>, with Ionic's part names and classes as pseudoclasses: <c>:active</c>, <c>:today</c>,
/// <c>:day-disabled</c> (the part's <c>disabled</c>), <c>:adjacent-day</c>, <c>:padding</c> and <c>:constrained</c>. Out of the Tab
/// order (<c>tabindex="-1"</c>): the arrow keys move between days.
/// </summary>
[PseudoClasses(":active", ":today", ":day-disabled", ":adjacent-day", ":padding", ":constrained", ":highlighted", ":readonly")]
public sealed class DatetimeCalendarDay : Button
{
    /// <summary>
    /// The datetime's accent, inherited by its days: primary, or its <c>color</c>'s base. Set by the datetime's theme, as a day's
    /// own theme can't read the host's <c>:ion-color</c> (Ionic's <c>:host(.ion-color) .calendar-day</c> rules).
    /// </summary>
    public static readonly AttachedProperty<Color?> AccentColorProperty =
        AvaloniaProperty.RegisterAttached<DatetimeCalendarDay, Control, Color?>("AccentColor", inherits: true);

    /// <summary>The accent's contrast, for the selected day's text: primary's contrast, or the colour's.</summary>
    public static readonly AttachedProperty<Color?> AccentContrastColorProperty =
        AvaloniaProperty.RegisterAttached<DatetimeCalendarDay, Control, Color?>("AccentContrastColor", inherits: true);

    static DatetimeCalendarDay()
    {
        IsTabStopProperty.OverrideDefaultValue<DatetimeCalendarDay>(false);
    }

    public static Color? GetAccentColor(Control element) => element.GetValue(AccentColorProperty);

    public static void SetAccentColor(Control element, Color? value) => element.SetValue(AccentColorProperty, value);

    public static Color? GetAccentContrastColor(Control element) => element.GetValue(AccentContrastColorProperty);

    public static void SetAccentContrastColor(Control element, Color? value) => element.SetValue(AccentContrastColorProperty, value);

    protected override Type StyleKeyOverride => typeof(DatetimeCalendarDay);

    internal DatetimeParts Parts { get; private set; }

    internal bool IsPadding { get; private set; }

    internal bool IsActive { get; private set; }

    internal bool IsAdjacent { get; private set; }

    internal bool IsToday { get; private set; }

    internal bool IsDayDisabled { get; private set; }

    internal DatetimeHighlightStyle? Highlight { get; private set; }

    /// <summary>The day's state from <c>renderMonth</c>.</summary>
    internal void Update(DatetimeParts parts, string text, string? label, bool isPadding, bool isActive, bool isToday, bool isDayDisabled,
        bool isConstrained, bool isAdjacent, bool isEnabled, bool isReadonly, DatetimeHighlightStyle? highlight)
    {
        Parts = parts;
        IsPadding = isPadding;
        IsActive = isActive;
        IsAdjacent = isAdjacent;
        IsToday = isToday;
        IsDayDisabled = isDayDisabled;
        Highlight = highlight;
        if (!Equals(Content, text))
        {
            Content = text;
        }

        AutomationProperties.SetName(this, label);
        AutomationProperties.SetAccessibilityView(this, isPadding ? AccessibilityView.Raw : AccessibilityView.Default);
        IsEnabled = isEnabled;
        IsHitTestVisible = !isPadding;
        PseudoClasses.Set(":active", isActive);
        PseudoClasses.Set(":today", isToday);
        PseudoClasses.Set(":day-disabled", isDayDisabled);
        PseudoClasses.Set(":adjacent-day", isAdjacent);
        PseudoClasses.Set(":padding", isPadding);
        PseudoClasses.Set(":constrained", isConstrained);
        PseudoClasses.Set(":readonly", isReadonly);
        ApplyHighlight(highlight);
    }

    /// <summary>
    /// Ionic's inline <c>color</c>, <c>background-color</c> and <c>border</c> with <c>!important</c>: local values here, which beat
    /// every style. Cleared when the day has none.
    /// </summary>
    private void ApplyHighlight(DatetimeHighlightStyle? style)
    {
        PseudoClasses.Set(":highlighted", style is not null);
        SetOrClear(ForegroundProperty, ParseBrush(style?.TextColor));
        SetOrClear(BackgroundProperty, ParseBrush(style?.BackgroundColor));
        var (thickness, brush) = ParseBorder(style?.Border);
        // CSS's default border colour is currentColor: the highlight's text colour, or the day's.
        SetOrClear(BorderBrushProperty, brush ?? (thickness is > 0 ? ParseBrush(style?.TextColor) ?? Foreground : null));
        if (thickness is { } t)
        {
            SetValue(BorderThicknessProperty, new Thickness(t));
        }
        else
        {
            ClearValue(BorderThicknessProperty);
        }
    }

    private void SetOrClear(AvaloniaProperty<IBrush?> property, IBrush? brush)
    {
        if (brush is null)
        {
            ClearValue(property);
        }
        else
        {
            SetValue(property, brush);
        }
    }

    internal static IBrush? ParseBrush(string? text) =>
        !string.IsNullOrWhiteSpace(text) && Behaviors.CssBoxShadow.TryParseColor(text, out var color) ? new ImmutableSolidColorBrush(color) : null;

    /// <summary>
    /// CSS's <c>border</c> shorthand: a width (a length, or thin, medium, thick), a style, and a colour, in any order. The shorthand
    /// resets what it leaves out: no style is <c>none</c>, so no border; no width is medium, 3px; no colour is currentColor (null here).
    /// </summary>
    internal static (double? Thickness, IBrush? Brush) ParseBorder(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, null);
        }

        double? width = null;
        IBrush? brush = null;
        var visible = false;
        foreach (var token in Behaviors.CssBoxShadow.Split(text, ' '))
        {
            var lower = token.ToLowerInvariant();
            if (lower is "thin" or "medium" or "thick")
            {
                width = lower switch { "thin" => 1, "medium" => 3, _ => 5 };
            }
            else if (double.TryParse(lower.EndsWith("px", StringComparison.Ordinal) ? lower[..^2] : lower,
                         System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var w)
                     && (w == 0 || lower.EndsWith("px", StringComparison.Ordinal)))
            {
                width = w;
            }
            else if (lower is "none" or "hidden")
            {
                visible = false;
            }
            else if (lower is "solid" or "dashed" or "dotted" or "double" or "groove" or "ridge" or "inset" or "outset")
            {
                visible = true;
            }
            else if (ParseBrush(token) is { } b)
            {
                brush = b;
            }
        }

        return visible ? (width ?? 3, brush) : (0, null);
    }
}

/// <summary>
/// Ionic's <c>.calendar-month-grid</c>: 7 columns and 6 rows (<c>grid-template-rows: repeat(6, 1fr)</c>), each day centred in its
/// cell (<c>.calendar-day-wrapper</c>, a flex box that centres). Its 42 days are made once, when the datetime first draws the month
/// (<see cref="EnsureDays"/>), and kept; the datetime sets each. Made then, not with the panel, so that the calendar of the
/// rendering the datetime doesn't use (<see cref="DatetimeCalendarRendering"/>) costs no days.
/// </summary>
public sealed class DatetimeCalendarMonth : Panel
{
    /// <summary>The number of days a month grid holds: 7 columns, 6 rows.</summary>
    internal const int DayCount = 42;

    /// <summary>Makes the 42 days, or those not made yet. True when it made any, so that the datetime hears their clicks.</summary>
    internal bool EnsureDays() => AddDays(DayCount);

    /// <summary>Makes up to <paramref name="count"/> more days, to 42 at most. True when it made any.</summary>
    internal bool AddDays(int count)
    {
        var made = false;
        for (; count > 0 && Days.Count < DayCount; count--)
        {
            var day = new DatetimeCalendarDay();
            Days.Add(day);
            Children.Add(day);
            made = true;
        }

        return made;
    }

    /// <summary>Whether all 42 days are made.</summary>
    internal bool HasAllDays => Days.Count == DayCount;

    /// <summary>The padding of the grid: <c>padding: 4px 10px</c> in md, set by the theme.</summary>
    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<DatetimeCalendarMonth>();

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    internal List<DatetimeCalendarDay> Days { get; } = [];

    internal int Month { get; set; }

    internal int Year { get; set; }

    static DatetimeCalendarMonth()
    {
        AffectsMeasure<DatetimeCalendarMonth>(PaddingProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var cellWidth = double.IsFinite(availableSize.Width) ? Math.Max(0, (availableSize.Width - padding.Left - padding.Right) / 7) : double.PositiveInfinity;
        double row = 0;
        double widest = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(cellWidth, double.PositiveInfinity));
            row = Math.Max(row, child.DesiredSize.Height);
            widest = Math.Max(widest, child.DesiredSize.Width);
        }

        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : widest * 7 + padding.Left + padding.Right;
        return new Size(width, row * 6 + padding.Top + padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var padding = Padding;
        var cellWidth = Math.Max(0, (finalSize.Width - padding.Left - padding.Right) / 7);
        var rowHeight = Math.Max(0, (finalSize.Height - padding.Top - padding.Bottom) / 6);
        var visible = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            var column = visible % 7;
            var rowIndex = visible / 7;
            visible++;
            var size = child.DesiredSize;
            var x = padding.Left + column * cellWidth + (cellWidth - size.Width) / 2;
            var y = padding.Top + rowIndex * rowHeight + (rowHeight - size.Height) / 2;
            child.Arrange(new Rect(x, y, size.Width, size.Height));
        }

        return finalSize;
    }
}

/// <summary>
/// Ionic's <c>.calendar-body</c> content: the previous, working and next months in a row, each the calendar's width, with a snap point
/// at each that can be swiped to (<c>scroll-snap-align: start</c>; none for <c>calendar-month-disabled</c>). Its months are made when
/// the datetime first draws them (<see cref="EnsureMonths"/>), so that a datetime in <see cref="DatetimeCalendarRendering.SingleGrid"/>
/// makes none; and the days of the months either side of the working one when they're first needed (#248, spike B): until then each is
/// an empty page of the calendar's width, so the scroller's extent and snap points are the same three pages.
/// </summary>
public sealed class DatetimeCalendarBodyPanel : Panel, IScrollSnapPointsInfo
{
    private readonly bool[] _snap = [true, true, true];
    private double _pageWidth;

    /// <summary>
    /// Makes the three months if they aren't made yet, with the working month's days; the months either side get theirs later. True
    /// when it made them.
    /// </summary>
    internal bool EnsureMonths()
    {
        if (Months.Count > 0)
        {
            return false;
        }

        for (var i = 0; i < 3; i++)
        {
            var month = new DatetimeCalendarMonth();
            if (i == 1)
            {
                month.EnsureDays();
            }

            Months.Add(month);
            Children.Add(month);
        }

        return true;
    }

    /// <summary>Whether the months either side of the working one have their days.</summary>
    internal bool HasNeighbourDays => Months.Count == 3 && Months[0].HasAllDays && Months[2].HasAllDays;

    internal List<DatetimeCalendarMonth> Months { get; } = [];

    internal AvonicDatetime? Owner { get; set; }

    internal double PageWidth => _pageWidth;

    /// <summary>Which months can be swiped to.</summary>
    internal void SetSnapPages(bool[] pages)
    {
        if (pages.SequenceEqual(_snap))
        {
            return;
        }

        pages.CopyTo(_snap, 0);
        HorizontalSnapPointsChanged?.Invoke(this, new RoutedEventArgs());
    }

    private double Page(Size available)
    {
        var scroller = Parent as Visual;
        while (scroller is not null and not ScrollViewer)
        {
            scroller = scroller.GetVisualParent();
        }

        return scroller is ScrollViewer { Bounds.Width: > 0 } viewer ? viewer.Bounds.Width
            : double.IsFinite(available.Width) ? available.Width : double.NaN;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var page = Page(availableSize);
        var height = 0.0;
        var width = 0.0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.IsNaN(page) ? double.PositiveInfinity : page, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
            width = Math.Max(width, child.DesiredSize.Width);
        }

        return new Size((double.IsNaN(page) ? width : page) * Children.Count, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var page = Children.Count == 0 ? 0 : finalSize.Width / Children.Count;
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Arrange(new Rect(i * page, 0, page, finalSize.Height));
        }

        if (page != _pageWidth)
        {
            _pageWidth = page;
            HorizontalSnapPointsChanged?.Invoke(this, new RoutedEventArgs());
        }

        return finalSize;
    }

    public bool AreHorizontalSnapPointsRegular { get; set; }

    public bool AreVerticalSnapPointsRegular { get; set; }

    public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment) =>
        orientation == Orientation.Horizontal
            ? Enumerable.Range(0, Children.Count).Where(i => _snap[i]).Select(i => i * _pageWidth).ToList()
            : [];

    public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
    {
        offset = 0;
        return 0;
    }

    public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;

    public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged
    {
        add { }
        remove { }
    }
}
