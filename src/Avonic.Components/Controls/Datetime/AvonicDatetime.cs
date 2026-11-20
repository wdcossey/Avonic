using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-datetime</c>: a calendar grid, or wheels, to pick a date, a time, a month or a year. See
/// <c>docs/components/ion-datetime/README.md</c>. The look is in the theme; the parts are made and kept up to date in code, as
/// Ionic's <c>render()</c> makes them.
/// </summary>
[TemplatePart("PART_DatetimeHeader", typeof(Control))]
[TemplatePart("PART_DatetimeTitle", typeof(Control))]
[TemplatePart("PART_DatetimeSelectedDate", typeof(TextBlock))]
[TemplatePart("PART_CalendarHeader", typeof(Control))]
[TemplatePart("PART_MonthYearButton", typeof(Button))]
[TemplatePart("PART_MonthYearText", typeof(TextBlock))]
[TemplatePart("PART_PreviousButton", typeof(AvonicButton))]
[TemplatePart("PART_NextButton", typeof(AvonicButton))]
[TemplatePart("PART_CalendarDaysOfWeek", typeof(Panel))]
[TemplatePart("PART_CalendarBody", typeof(ScrollViewer))]
[TemplatePart("PART_CalendarMonths", typeof(DatetimeCalendarBodyPanel))]
[TemplatePart("PART_CalendarGrid", typeof(DatetimeCalendarMonth))]
[TemplatePart("PART_MonthYearWheel", typeof(AvonicPicker))]
[TemplatePart("PART_TimeButton", typeof(Button))]
[TemplatePart("PART_TimeText", typeof(TextBlock))]
[TemplatePart("PART_TimePopover", typeof(AvonicPopover))]
[TemplatePart("PART_TimeWheel", typeof(AvonicPicker))]
[TemplatePart("PART_Wheel", typeof(AvonicPicker))]
[TemplatePart("PART_CancelButton", typeof(AvonicButton))]
[TemplatePart("PART_ClearButton", typeof(AvonicButton))]
[TemplatePart("PART_ConfirmButton", typeof(AvonicButton))]
[PseudoClasses(":presentation-date", ":presentation-date-time", ":presentation-time-date", ":presentation-time")]
[PseudoClasses(":presentation-month", ":presentation-month-year", ":presentation-year", ":size-fixed", ":size-cover")]
[PseudoClasses(":prefer-wheel", ":grid", ":show-month-and-year", ":month-year-picker-open", ":readonly", ":ion-color")]
[PseudoClasses(":has-header", ":has-selected-date", ":has-time", ":has-wheel", ":has-footer", ":has-default-buttons")]
[PseudoClasses(":has-clear-button", ":has-buttons", ":has-time-label", ":wheel-in-time", ":calendar-single-grid")]
public partial class AvonicDatetime : TemplatedControl, IItemControl
{
    /// <summary>Ionic's <c>CANCEL_ROLE</c>, the parent overlay's dismiss role for <see cref="Cancel"/>.</summary>
    public const string CancelRole = "datetime-cancel";

    /// <summary>Ionic's <c>CONFIRM_ROLE</c>, the parent overlay's dismiss role for <see cref="Confirm"/>.</summary>
    public const string ConfirmRole = "datetime-confirm";

    /// <summary>The style class of each wheel the datetime makes, Ionic's <c>part="wheel"</c>.</summary>
    public const string WheelClass = "wheel";

    /// <summary>The style class of each wheel's option, Ionic's <c>part="wheel-item"</c>.</summary>
    public const string WheelItemClass = "wheel-item";

    /// <summary>The style class of the working value's option, Ionic's <c>part="wheel-item active"</c>.</summary>
    public const string ActiveClass = "active";

    /// <summary>The style class of the previous and next buttons, Ionic's <c>part="navigation-button"</c>.</summary>
    public const string NavigationButtonClass = "navigation-button";

    /// <summary>The title's <c>text-transform: uppercase</c>, for a string; other content as it is.</summary>
    public static readonly Avalonia.Data.Converters.IValueConverter UppercaseText =
        new Avalonia.Data.Converters.FuncValueConverter<object?, object?>(value => value is string text ? text.ToUpperInvariant() : value);

    /// <summary>Ionic's <c>getToday()</c>: the clock. Tests pin it.</summary>
    internal static Func<DateTime> Now { get; set; } = () => DateTime.Now;

    /// <summary>The scroll's rest before the month changes: Ionic's <c>setTimeout(updateActiveMonth, 50)</c>.</summary>
    internal static readonly TimeSpan ScrollRest = TimeSpan.FromMilliseconds(50);

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>value</c>. Null is no value. Two-way by default, as the user changes it.</summary>
    public static readonly StyledProperty<DatetimeValue?> ValueProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeValue?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> CancelTextProperty =
        AvaloniaProperty.Register<AvonicDatetime, string>(nameof(CancelText), "Cancel");

    public static readonly StyledProperty<string> ClearTextProperty =
        AvaloniaProperty.Register<AvonicDatetime, string>(nameof(ClearText), "Clear");

    public static readonly StyledProperty<string> DoneTextProperty =
        AvaloniaProperty.Register<AvonicDatetime, string>(nameof(DoneText), "Done");

    /// <summary>Ionic <c>color</c>: the selected day, today, the header, the wheels and the buttons.</summary>
    public static readonly StyledProperty<string?> ColorProperty = ColorPalette.ColorProperty.AddOwner<AvonicDatetime>();

    public static readonly StyledProperty<DatetimeValueList?> DayValuesProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeValueList?>(nameof(DayValues));

    public static readonly StyledProperty<DatetimeValueList?> MonthValuesProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeValueList?>(nameof(MonthValues));

    public static readonly StyledProperty<DatetimeValueList?> YearValuesProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeValueList?>(nameof(YearValues));

    public static readonly StyledProperty<DatetimeValueList?> HourValuesProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeValueList?>(nameof(HourValues));

    public static readonly StyledProperty<DatetimeValueList?> MinuteValuesProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeValueList?>(nameof(MinuteValues));

    /// <summary>Ionic <c>firstDayOfWeek</c>: 0 is Sunday. Read <c>% 7</c>.</summary>
    public static readonly StyledProperty<int> FirstDayOfWeekProperty =
        AvaloniaProperty.Register<AvonicDatetime, int>(nameof(FirstDayOfWeek));

    public static readonly StyledProperty<DatetimeFormatOptions?> FormatOptionsProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeFormatOptions?>(nameof(FormatOptions));

    public static readonly StyledProperty<DatetimeHighlightedDates?> HighlightedDatesProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeHighlightedDates?>(nameof(HighlightedDates));

    /// <summary>Ionic <c>hourCycle</c>. Null is the locale's.</summary>
    public static readonly StyledProperty<DatetimeHourCycle?> HourCycleProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeHourCycle?>(nameof(HourCycle));

    public static readonly StyledProperty<Func<string, bool>?> IsDateEnabledProperty =
        AvaloniaProperty.Register<AvonicDatetime, Func<string, bool>?>(nameof(IsDateEnabled));

    /// <summary>Ionic <c>locale</c>: a culture name; <c>"default"</c> is the current culture.</summary>
    public static readonly StyledProperty<string> LocaleProperty =
        AvaloniaProperty.Register<AvonicDatetime, string>(nameof(Locale), "default");

    public static readonly StyledProperty<string?> MaxProperty =
        AvaloniaProperty.Register<AvonicDatetime, string?>(nameof(Max));

    public static readonly StyledProperty<string?> MinProperty =
        AvaloniaProperty.Register<AvonicDatetime, string?>(nameof(Min));

    public static readonly StyledProperty<bool> MultipleProperty =
        AvaloniaProperty.Register<AvonicDatetime, bool>(nameof(Multiple));

    public static readonly StyledProperty<bool> PreferWheelProperty =
        AvaloniaProperty.Register<AvonicDatetime, bool>(nameof(PreferWheel));

    public static readonly StyledProperty<DatetimePresentation> PresentationProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimePresentation>(nameof(Presentation), DatetimePresentation.DateTime);

    public static readonly StyledProperty<bool> ReadonlyProperty =
        AvaloniaProperty.Register<AvonicDatetime, bool>(nameof(Readonly));

    public static readonly StyledProperty<bool> ShowAdjacentDaysProperty =
        AvaloniaProperty.Register<AvonicDatetime, bool>(nameof(ShowAdjacentDays));

    public static readonly StyledProperty<bool> ShowClearButtonProperty =
        AvaloniaProperty.Register<AvonicDatetime, bool>(nameof(ShowClearButton));

    public static readonly StyledProperty<bool> ShowDefaultButtonsProperty =
        AvaloniaProperty.Register<AvonicDatetime, bool>(nameof(ShowDefaultButtons));

    public static readonly StyledProperty<bool> ShowDefaultTimeLabelProperty =
        AvaloniaProperty.Register<AvonicDatetime, bool>(nameof(ShowDefaultTimeLabel), true);

    public static readonly StyledProperty<bool> ShowDefaultTitleProperty =
        AvaloniaProperty.Register<AvonicDatetime, bool>(nameof(ShowDefaultTitle));

    public static readonly StyledProperty<DatetimeSize> SizeProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeSize>(nameof(Size), DatetimeSize.Fixed);

    public static readonly StyledProperty<Func<IReadOnlyList<string>, string>?> TitleSelectedDatesFormatterProperty =
        AvaloniaProperty.Register<AvonicDatetime, Func<IReadOnlyList<string>, string>?>(nameof(TitleSelectedDatesFormatter));

    /// <summary>Ionic's <c>title</c> slot. Its default is <c>"Select Date"</c>.</summary>
    public static readonly StyledProperty<object?> TitleContentProperty =
        AvaloniaProperty.Register<AvonicDatetime, object?>(nameof(TitleContent));

    public static readonly StyledProperty<IDataTemplate?> TitleContentTemplateProperty =
        AvaloniaProperty.Register<AvonicDatetime, IDataTemplate?>(nameof(TitleContentTemplate));

    /// <summary>Ionic's <c>time-label</c> slot. Its default is <c>"Time"</c>.</summary>
    public static readonly StyledProperty<object?> TimeLabelContentProperty =
        AvaloniaProperty.Register<AvonicDatetime, object?>(nameof(TimeLabelContent));

    public static readonly StyledProperty<IDataTemplate?> TimeLabelContentTemplateProperty =
        AvaloniaProperty.Register<AvonicDatetime, IDataTemplate?>(nameof(TimeLabelContentTemplate));

    /// <summary>Ionic's <c>buttons</c> slot: in place of the default buttons; a selection then waits for <see cref="Confirm"/>.</summary>
    public static readonly StyledProperty<object?> ButtonsContentProperty =
        AvaloniaProperty.Register<AvonicDatetime, object?>(nameof(ButtonsContent));

    public static readonly StyledProperty<IDataTemplate?> ButtonsContentTemplateProperty =
        AvaloniaProperty.Register<AvonicDatetime, IDataTemplate?>(nameof(ButtonsContentTemplate));

    /// <summary>The title shown: <see cref="TitleContent"/>, or Ionic's default <c>"Select Date"</c>.</summary>
    public static readonly DirectProperty<AvonicDatetime, object?> ShownTitleProperty =
        AvaloniaProperty.RegisterDirect<AvonicDatetime, object?>(nameof(ShownTitle), o => o.ShownTitle);

    /// <summary>The time label shown: <see cref="TimeLabelContent"/>, or Ionic's default <c>"Time"</c>.</summary>
    public static readonly DirectProperty<AvonicDatetime, object?> ShownTimeLabelProperty =
        AvaloniaProperty.RegisterDirect<AvonicDatetime, object?>(nameof(ShownTimeLabel), o => o.ShownTimeLabel);

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    public static readonly RoutedEvent<DatetimeChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicDatetime, DatetimeChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> IonCancelEvent =
        RoutedEvent.Register<AvonicDatetime, RoutedEventArgs>(nameof(IonCancel), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicDatetime, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicDatetime, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    static AvonicDatetime()
    {
        FocusableProperty.OverrideDefaultValue<AvonicDatetime>(false);
    }

    public AvonicDatetime()
    {
        // Ionic stops its inner components' ionFocus and ionBlur at the datetime, so that an app hears only the datetime's.
        AddHandler(AvonicButton.IonFocusEvent, (_, e) => e.Handled = true);
        AddHandler(AvonicButton.IonBlurEvent, (_, e) => e.Handled = true);

        // CalendarRendering's default, read once here, as a prop's default from the config is. At style priority, and before any
        // style, so that an app's value, set or in a style, wins.
        if (AvonicConfig.DatetimeCalendarRendering != DatetimeCalendarRendering.ScrolledMonths)
        {
            SetValue(CalendarRenderingProperty, AvonicConfig.DatetimeCalendarRendering, BindingPriority.Style);
        }
    }

    public DatetimeValue? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string CancelText { get => GetValue(CancelTextProperty); set => SetValue(CancelTextProperty, value); }

    public string ClearText { get => GetValue(ClearTextProperty); set => SetValue(ClearTextProperty, value); }

    public string DoneText { get => GetValue(DoneTextProperty); set => SetValue(DoneTextProperty, value); }

    public string? Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    public DatetimeValueList? DayValues { get => GetValue(DayValuesProperty); set => SetValue(DayValuesProperty, value); }

    public DatetimeValueList? MonthValues { get => GetValue(MonthValuesProperty); set => SetValue(MonthValuesProperty, value); }

    public DatetimeValueList? YearValues { get => GetValue(YearValuesProperty); set => SetValue(YearValuesProperty, value); }

    public DatetimeValueList? HourValues { get => GetValue(HourValuesProperty); set => SetValue(HourValuesProperty, value); }

    public DatetimeValueList? MinuteValues { get => GetValue(MinuteValuesProperty); set => SetValue(MinuteValuesProperty, value); }

    public int FirstDayOfWeek { get => GetValue(FirstDayOfWeekProperty); set => SetValue(FirstDayOfWeekProperty, value); }

    public DatetimeFormatOptions? FormatOptions { get => GetValue(FormatOptionsProperty); set => SetValue(FormatOptionsProperty, value); }

    public DatetimeHighlightedDates? HighlightedDates { get => GetValue(HighlightedDatesProperty); set => SetValue(HighlightedDatesProperty, value); }

    public DatetimeHourCycle? HourCycle { get => GetValue(HourCycleProperty); set => SetValue(HourCycleProperty, value); }

    public Func<string, bool>? IsDateEnabled { get => GetValue(IsDateEnabledProperty); set => SetValue(IsDateEnabledProperty, value); }

    public string Locale { get => GetValue(LocaleProperty); set => SetValue(LocaleProperty, value); }

    public string? Max { get => GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    public string? Min { get => GetValue(MinProperty); set => SetValue(MinProperty, value); }

    public bool Multiple { get => GetValue(MultipleProperty); set => SetValue(MultipleProperty, value); }

    public bool PreferWheel { get => GetValue(PreferWheelProperty); set => SetValue(PreferWheelProperty, value); }

    public DatetimePresentation Presentation { get => GetValue(PresentationProperty); set => SetValue(PresentationProperty, value); }

    public bool Readonly { get => GetValue(ReadonlyProperty); set => SetValue(ReadonlyProperty, value); }

    public bool ShowAdjacentDays { get => GetValue(ShowAdjacentDaysProperty); set => SetValue(ShowAdjacentDaysProperty, value); }

    public bool ShowClearButton { get => GetValue(ShowClearButtonProperty); set => SetValue(ShowClearButtonProperty, value); }

    public bool ShowDefaultButtons { get => GetValue(ShowDefaultButtonsProperty); set => SetValue(ShowDefaultButtonsProperty, value); }

    public bool ShowDefaultTimeLabel { get => GetValue(ShowDefaultTimeLabelProperty); set => SetValue(ShowDefaultTimeLabelProperty, value); }

    public bool ShowDefaultTitle { get => GetValue(ShowDefaultTitleProperty); set => SetValue(ShowDefaultTitleProperty, value); }

    public DatetimeSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    public Func<IReadOnlyList<string>, string>? TitleSelectedDatesFormatter
    {
        get => GetValue(TitleSelectedDatesFormatterProperty);
        set => SetValue(TitleSelectedDatesFormatterProperty, value);
    }

    public object? TitleContent { get => GetValue(TitleContentProperty); set => SetValue(TitleContentProperty, value); }

    public IDataTemplate? TitleContentTemplate { get => GetValue(TitleContentTemplateProperty); set => SetValue(TitleContentTemplateProperty, value); }

    public object? TimeLabelContent { get => GetValue(TimeLabelContentProperty); set => SetValue(TimeLabelContentProperty, value); }

    public IDataTemplate? TimeLabelContentTemplate { get => GetValue(TimeLabelContentTemplateProperty); set => SetValue(TimeLabelContentTemplateProperty, value); }

    public object? ButtonsContent { get => GetValue(ButtonsContentProperty); set => SetValue(ButtonsContentProperty, value); }

    public IDataTemplate? ButtonsContentTemplate { get => GetValue(ButtonsContentTemplateProperty); set => SetValue(ButtonsContentTemplateProperty, value); }

    public object? ShownTitle => TitleContent ?? "Select Date";

    public object? ShownTimeLabel => TimeLabelContent ?? "Time";

    public event EventHandler<DatetimeChangeEventArgs>? IonChange
    {
        add => AddHandler(IonChangeEvent, value);
        remove => RemoveHandler(IonChangeEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonCancel
    {
        add => AddHandler(IonCancelEvent, value);
        remove => RemoveHandler(IonCancelEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonFocus
    {
        add => AddHandler(IonFocusEvent, value);
        remove => RemoveHandler(IonFocusEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonBlur
    {
        add => AddHandler(IonBlurEvent, value);
        remove => RemoveHandler(IonBlurEvent, value);
    }

    /// <summary>Ionic's internal <c>ionValueChange</c>, after every change of <see cref="Value"/>, for <c>ion-datetime-button</c>.</summary>
    internal event EventHandler<DatetimeValue?>? ValueChange;

    /// <summary>Ionic's internal <c>ionRender</c>, a frame after the presentation changes, for <c>ion-datetime-button</c>.</summary>
    internal event EventHandler? Rendered;

    ItemControlType IItemControl.ItemControlType => ItemControlType.Datetime;

    void IItemControl.OnItemTap()
    {
    }

    /// <summary>The culture <see cref="Locale"/> names.</summary>
    internal CultureInfo Culture => DatetimeIntl.Culture(Locale);

    protected override AutomationPeer OnCreateAutomationPeer() => new DatetimeAutomationPeer(this);

    private static void Warn(object source, string message) =>
        Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(source, message);
}

/// <summary>The datetime as a group, named by nothing of its own, as Ionic's host has no role.</summary>
internal sealed class DatetimeAutomationPeer(AvonicDatetime owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override bool IsContentElementCore() => false;
}
