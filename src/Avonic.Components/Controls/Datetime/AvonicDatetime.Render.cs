using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avalonia.Reactive;

namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>render()</c>: the parts kept in step with the state.</summary>
public partial class AvonicDatetime
{
    private Control? _header;
    private TextBlock? _selectedDate;
    private Button? _monthYearButton;
    private TextBlock? _monthYearText;
    private AvonicIcon? _monthYearIcon;
    private AvonicButton? _previousButton;
    private AvonicButton? _nextButton;
    private Panel? _daysOfWeek;
    private ScrollViewer? _calendarBody;
    private DatetimeCalendarBodyPanel? _calendarMonths;
    private AvonicPicker? _monthYearWheel;
    private Button? _timeButton;
    private TextBlock? _timeText;
    private AvonicPopover? _timePopover;
    private AvonicPicker? _timeWheel;
    private AvonicPicker? _wheel;
    private AvonicButton? _cancelButton;
    private AvonicButton? _clearButton;
    private AvonicButton? _confirmButton;
    private SmoothScroll? _smooth;
    private IDisposable? _scrollRest;
    private bool _rendering;
    private bool _renderAgain;
    private IDisposable? _wheelVariables;

    internal ScrollViewer? CalendarBody => _calendarBody;

    internal DatetimeCalendarBodyPanel? CalendarMonths => _calendarMonths;

    internal AvonicPicker? Wheel => _wheel;

    internal AvonicPicker? MonthYearWheel => _monthYearWheel;

    internal AvonicPicker? TimeWheel => _timeWheel;

    internal AvonicPopover? TimePopover => _timePopover;

    /// <summary>Ionic's <c>datetime-ready</c> for the grid: the calendar is laid out, so it can scroll (or, as a single grid, fade).</summary>
    private bool IsCalendarReady => UsesSingleGrid ? IsGridReady : _calendarBody is { Viewport.Width: > 0 } && IsEffectivelyVisible;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Detach();
        var scope = e.NameScope;
        _header = scope.Find<Control>("PART_DatetimeHeader");
        _selectedDate = scope.Find<TextBlock>("PART_DatetimeSelectedDate");
        _monthYearButton = scope.Find<Button>("PART_MonthYearButton");
        _monthYearText = scope.Find<TextBlock>("PART_MonthYearText");
        _monthYearIcon = scope.Find<AvonicIcon>("PART_MonthYearIcon");
        _previousButton = scope.Find<AvonicButton>("PART_PreviousButton");
        _nextButton = scope.Find<AvonicButton>("PART_NextButton");
        _daysOfWeek = scope.Find<Panel>("PART_CalendarDaysOfWeek");
        _calendarBody = scope.Find<ScrollViewer>("PART_CalendarBody");
        _calendarMonths = scope.Find<DatetimeCalendarBodyPanel>("PART_CalendarMonths");
        AttachGrid(scope.Find<DatetimeCalendarMonth>("PART_CalendarGrid"));
        _monthYearWheel = scope.Find<AvonicPicker>("PART_MonthYearWheel");
        _timeButton = scope.Find<Button>("PART_TimeButton");
        _timeText = scope.Find<TextBlock>("PART_TimeText");
        _timePopover = scope.Find<AvonicPopover>("PART_TimePopover");
        _timeWheel = scope.Find<AvonicPicker>("PART_TimeWheel");
        _wheel = scope.Find<AvonicPicker>("PART_Wheel");
        _cancelButton = scope.Find<AvonicButton>("PART_CancelButton");
        _clearButton = scope.Find<AvonicButton>("PART_ClearButton");
        _confirmButton = scope.Find<AvonicButton>("PART_ConfirmButton");

        if (_monthYearButton is not null)
        {
            _monthYearButton.Click += OnMonthYearClick;
        }

        if (_previousButton is not null)
        {
            _previousButton.Click += OnPreviousClick;
        }

        if (_nextButton is not null)
        {
            _nextButton.Click += OnNextClick;
        }

        if (_timeButton is not null)
        {
            _timeButton.Click += OnTimeButtonClick;
        }

        if (_timePopover is not null)
        {
            _timePopover.IonPopoverWillPresent += OnTimePopoverWillPresent;
        }

        if (_cancelButton is not null)
        {
            _cancelButton.Click += OnCancelClick;
        }

        if (_clearButton is not null)
        {
            _clearButton.Click += OnClearClick;
        }

        if (_confirmButton is not null)
        {
            _confirmButton.Click += OnConfirmClick;
        }

        if (_calendarBody is not null)
        {
            _smooth = new SmoothScroll(_calendarBody);
            _calendarBody.PropertyChanged += OnCalendarScroll;
            _calendarBody.AddHandler(KeyDownEvent, OnCalendarKeyDown, RoutingStrategies.Tunnel);
            _calendarBody.GotFocus += OnCalendarFocus;
            _calendarBody.SizeChanged += OnCalendarBodySize;
        }

        if (_calendarMonths is not null)
        {
            _calendarMonths.Owner = this;
        }

        _wheelVariables?.Dispose();
        _wheelVariables = ForwardWheelVariables([_wheel, _monthYearWheel, _timeWheel]);
        EnsureLoaded();
        Render();
    }

    /// <summary>
    /// datetime.md.css's <c>ion-picker { --highlight-background: var(--wheel-highlight-background); … }</c>: each of the datetime's
    /// wheel variables, when set, as its pickers' own; unset, the pickers' own apply.
    /// </summary>
    private IDisposable ForwardWheelVariables(AvonicPicker?[] pickers)
    {
        var subscriptions = new List<IDisposable>();
        foreach (var (from, to) in new[]
                 {
                     ("Avonic.Datetime.WheelHighlightBackground", "Avonic.Picker.HighlightBackground"),
                     ("Avonic.Datetime.WheelHighlightBorderRadius", "Avonic.Picker.HighlightBorderRadius"),
                     ("Avonic.Datetime.WheelFadeBackgroundRgb", "Avonic.Picker.FadeBackgroundRgb"),
                 })
        {
            subscriptions.Add(this.GetResourceObservable(from).Subscribe(new AnonymousObserver<object?>(value =>
            {
                foreach (var picker in pickers.OfType<AvonicPicker>())
                {
                    if (Css.IsSet(value))
                    {
                        picker.Resources[to] = value;
                    }
                    else
                    {
                        picker.Resources.Remove(to);
                    }
                }
            })));
        }

        return new Subscriptions(subscriptions);
    }

    private sealed class Subscriptions(List<IDisposable> list) : IDisposable
    {
        public void Dispose() => list.ForEach(s => s.Dispose());
    }

    private void Detach()
    {
        if (_monthYearButton is not null)
        {
            _monthYearButton.Click -= OnMonthYearClick;
        }

        if (_previousButton is not null)
        {
            _previousButton.Click -= OnPreviousClick;
        }

        if (_nextButton is not null)
        {
            _nextButton.Click -= OnNextClick;
        }

        if (_timeButton is not null)
        {
            _timeButton.Click -= OnTimeButtonClick;
        }

        if (_timePopover is not null)
        {
            _timePopover.IonPopoverWillPresent -= OnTimePopoverWillPresent;
        }

        if (_cancelButton is not null)
        {
            _cancelButton.Click -= OnCancelClick;
        }

        if (_clearButton is not null)
        {
            _clearButton.Click -= OnClearClick;
        }

        if (_confirmButton is not null)
        {
            _confirmButton.Click -= OnConfirmClick;
        }

        if (_calendarBody is not null)
        {
            _calendarBody.PropertyChanged -= OnCalendarScroll;
            _calendarBody.RemoveHandler(KeyDownEvent, OnCalendarKeyDown);
            _calendarBody.GotFocus -= OnCalendarFocus;
            _calendarBody.SizeChanged -= OnCalendarBodySize;
        }

        _smooth?.Stop();
        _smooth = null;
        DetachGrid();
    }

    /// <summary>
    /// The host is a block, <c>width: auto</c>: as wide as its line, up to its <c>max-width</c>, unless it's a wheel that's as wide as
    /// its content (<c>size="fixed"</c> with <c>preferWheel</c>: <c>max-width: max-content</c>).
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = base.MeasureOverride(availableSize);
        var contentWide = Size == DatetimeSize.Fixed && IsCalendarPicker && PreferWheel;
        return !contentWide && double.IsFinite(availableSize.Width) && HorizontalAlignment != Avalonia.Layout.HorizontalAlignment.Stretch
            ? desired.WithWidth(Math.Max(desired.Width, availableSize.Width))
            : desired;
    }

    // ── Render ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic's <c>firstDayOfWeek % 7</c>, kept in 0 to 6: a negative one would give a month more than 42 cells.</summary>
    private int FirstDayOfWeekIndex => ((FirstDayOfWeek % 7) + 7) % 7;

    private bool IsMonthAndYearPresentation =>
        Presentation is DatetimePresentation.Year or DatetimePresentation.Month or DatetimePresentation.MonthYear;

    /// <summary>Ionic's <c>render()</c>: every part from the state. Cheap enough to run after each change.</summary>
    internal void Render()
    {
        if (!_loaded)
        {
            return;
        }

        if (_rendering)
        {
            // Asked for during a render: run again once this one ends, so that the parts show the state as it is then.
            _renderAgain = true;
            return;
        }

        _rendering = true;
        try
        {
            RenderHost();
            // Without a template (never shown) there are no parts to draw, but the presentation check below still runs: Ionic's
            // datetime renders while hidden, so ion-datetime-button's wait for ionRender ends (#79).
            if (Template is not null && (_calendarMonths is not null || _wheel is not null))
            {
                var culture = Culture;
                RenderHeader(culture);
                if (IsGridStyle)
                {
                    RenderCalendar(culture);
                    RenderMonthYearWheel(culture);
                    RenderTime(culture);
                }

                RenderWheel(culture);
                RenderFooter();
            }
        }
        finally
        {
            _rendering = false;
        }

        if (_renderAgain)
        {
            _renderAgain = false;
            Render();
            return;
        }

        if (_prevPresentation is { } previous && previous != Presentation)
        {
            _prevPresentation = Presentation;
            // componentDidRender: the month/year picker isn't in the other presentations, and ionRender a frame later.
            if (_showMonthAndYear)
            {
                _showMonthAndYear = false;
                Render();
            }

            AvonicConfig.RequestFrame(this, () => Rendered?.Invoke(this, EventArgs.Empty));
        }

        _prevPresentation = Presentation;
    }

    private void RenderHost()
    {
        var presentation = Presentation;
        var classes = PseudoClasses;
        classes.Set(":presentation-date", presentation == DatetimePresentation.Date);
        classes.Set(":presentation-date-time", presentation == DatetimePresentation.DateTime);
        classes.Set(":presentation-time-date", presentation == DatetimePresentation.TimeDate);
        classes.Set(":presentation-time", presentation == DatetimePresentation.Time);
        classes.Set(":presentation-month", presentation == DatetimePresentation.Month);
        classes.Set(":presentation-month-year", presentation == DatetimePresentation.MonthYear);
        classes.Set(":presentation-year", presentation == DatetimePresentation.Year);
        classes.Set(":size-fixed", Size == DatetimeSize.Fixed);
        classes.Set(":size-cover", Size == DatetimeSize.Cover);
        classes.Set(":prefer-wheel", IsCalendarPicker && PreferWheel);
        classes.Set(":grid", IsGridStyle);
        classes.Set(":show-month-and-year", _showMonthAndYear || IsMonthAndYearPresentation);
        classes.Set(":month-year-picker-open", _showMonthAndYear && !IsMonthAndYearPresentation);
        classes.Set(":readonly", Readonly);
        classes.Set(":calendar-single-grid", UsesSingleGrid);

        var hasHeader = TitleContent is not null || ShowDefaultTitle;
        classes.Set(":has-header", hasHeader);
        classes.Set(":has-selected-date", hasHeader && IsGridStyle);
        classes.Set(":has-time", IsGridStyle && presentation is DatetimePresentation.DateTime or DatetimePresentation.TimeDate);
        classes.Set(":has-wheel", !IsGridStyle);
        classes.Set(":wheel-in-time", presentation == DatetimePresentation.Time);
        classes.Set(":has-time-label", TimeLabelContent is not null || ShowDefaultTimeLabel);
        classes.Set(":has-footer", HasSlottedButtons || ShowDefaultButtons || ShowClearButton);
        classes.Set(":has-buttons", HasSlottedButtons);
        classes.Set(":has-default-buttons", !HasSlottedButtons && ShowDefaultButtons);
        classes.Set(":has-clear-button", !HasSlottedButtons && ShowClearButton);
    }

    /// <summary>Ionic's <c>getHeaderSelectedDateText</c>.</summary>
    internal string HeaderSelectedDateText(CultureInfo culture)
    {
        if (Multiple && _activeIsList && _active.Count != 1)
        {
            var text = $"{_active.Count} days";
            if (TitleSelectedDatesFormatter is { } formatter)
            {
                try
                {
                    text = formatter([.. _active.Select(DatetimeUtils.ConvertDataToIso)]);
                }
                catch (Exception e)
                {
                    LogError(this, "[ion-datetime] - Exception in provided `titleSelectedDatesFormatter`:", e);
                }
            }

            return text;
        }

        return DatetimeIntl.LocalizedDateTime(culture, ActivePartWithFallback, FormatOptions?.Date ?? DatetimeIntl.ShortDate);
    }

    private void RenderHeader(CultureInfo culture)
    {
        if (_selectedDate is not null && IsGridStyle && (TitleContent is not null || ShowDefaultTitle))
        {
            _selectedDate.Text = HeaderSelectedDateText(culture);
        }
    }

    // ── The calendar ─────────────────────────────────────────────────────────────────────────

    private void RenderCalendar(CultureInfo culture)
    {
        var disabled = !IsEnabled;
        if (_monthYearText is not null)
        {
            _monthYearText.Text = DatetimeIntl.MonthAndYear(culture, _working);
        }

        if (_monthYearIcon is not null)
        {
            _monthYearIcon.Icon = _showMonthAndYear ? "caret-up-sharp" : "caret-down-sharp";
        }

        if (_monthYearButton is not null)
        {
            AutomationProperties.SetName(_monthYearButton, _showMonthAndYear ? "Hide year picker" : "Show year picker");
        }

        _previousButton?.SetCurrentValue(IsEnabledProperty, !(disabled || DatetimeUtils.IsPrevMonthDisabled(_working, _minParts, _maxParts)));
        _nextButton?.SetCurrentValue(IsEnabledProperty, !(disabled || DatetimeUtils.IsNextMonthDisabled(_working, _maxParts)));

        if (_daysOfWeek is not null)
        {
            var names = DatetimeIntl.DaysOfWeek(culture, FirstDayOfWeekIndex);
            while (_daysOfWeek.Children.Count < 7)
            {
                _daysOfWeek.Children.Add(new TextBlock { Classes = { "day-of-week" }, HorizontalAlignment = HorizontalAlignment.Center });
            }

            for (var i = 0; i < 7; i++)
            {
                ((TextBlock)_daysOfWeek.Children[i]).Text = names[i];
            }
        }

        if (UsesSingleGrid)
        {
            // The working month only: a forced one is shown when the fade reaches it (SwapGridMonth).
            if (_calendarGrid!.EnsureDays())
            {
                HearDayClicks(_calendarGrid);
            }

            RenderMonth(culture, _calendarGrid, _working.Month!.Value, _working.Year!.Value);
            return;
        }

        if (_calendarMonths is null)
        {
            return;
        }

        if (_calendarMonths.EnsureMonths())
        {
            foreach (var month in _calendarMonths.Months)
            {
                HearDayClicks(month);
            }
        }

        var months = DatetimeUtils.GenerateMonths(_working, _forceRenderDate);
        var pageEnabled = new bool[3];
        for (var i = 0; i < 3; i++)
        {
            pageEnabled[i] = RenderMonth(culture, _calendarMonths.Months[i], months[i].Month!.Value, months[i].Year!.Value);
        }

        _calendarMonths.SetSnapPages(pageEnabled);
    }

    private void HearDayClicks(DatetimeCalendarMonth month)
    {
        foreach (var day in month.Days)
        {
            day.Click -= OnDayClick;
            day.Click += OnDayClick;
        }
    }

    /// <summary>Ionic's <c>renderMonth</c>. True when the month can be swiped to.</summary>
    private bool RenderMonth(CultureInfo culture, DatetimeCalendarMonth monthPanel, int month, int year)
    {
        var disabled = !IsEnabled;
        var yearAllowed = _yearValues is null || _yearValues.Contains(year);
        var monthAllowed = _monthValues is null || _monthValues.Contains(month);
        var isCalMonthDisabled = !yearAllowed || !monthAllowed;
        var isDatetimeDisabled = disabled || Readonly;
        var swipeDisabled = disabled || DatetimeUtils.IsMonthDisabled(new DatetimeParts(month, null, year),
            _minParts is { } lo ? lo with { Day = null } : null, _maxParts is { } hi ? hi with { Day = null } : null);
        var isWorkingMonth = _working.Month == month && _working.Year == year;
        AutomationProperties.SetAccessibilityView(monthPanel, isWorkingMonth ? AccessibilityView.Default : AccessibilityView.Raw);
        monthPanel.Month = month;
        monthPanel.Year = year;

        var cells = DatetimeUtils.GetDaysOfMonth(month, year, FirstDayOfWeekIndex, ShowAdjacentDays);
        for (var index = 0; index < monthPanel.Days.Count; index++)
        {
            var dayControl = monthPanel.Days[index];
            if (index >= cells.Count)
            {
                dayControl.IsVisible = false;
                continue;
            }

            dayControl.IsVisible = true;
            var cell = cells[index];
            var (cellMonth, cellYear) = (month, year);
            if (ShowAdjacentDays && cell.IsAdjacentDay && cell.Day is { } adjacent)
            {
                if (adjacent > 20)
                {
                    (cellMonth, cellYear) = month == 1 ? (12, year - 1) : (month - 1, year);
                }
                else if (adjacent < 15)
                {
                    (cellMonth, cellYear) = month == 12 ? (1, year + 1) : (month + 1, year);
                }
            }

            var reference = new DatetimeParts(cellMonth, cell.Day, cellYear, IsAdjacentDay: cell.IsAdjacentDay);
            var isPadding = cell.Day is null;
            var isActive = _active.Any(p => DatetimeUtils.IsSameDay(reference, p));
            var isToday = DatetimeUtils.IsSameDay(reference, _today);
            var isDayDisabled = DatetimeUtils.IsDayDisabled(reference, _minParts, _maxParts, _dayValues);
            var dateIso = DatetimeUtils.ConvertDataToIso(reference);
            var isCalDayDisabled = isCalMonthDisabled || isDayDisabled;
            if (!isCalDayDisabled && IsDateEnabled is { } isDateEnabled)
            {
                try
                {
                    isCalDayDisabled = !isDateEnabled(dateIso);
                }
                catch (Exception e)
                {
                    LogError(this, "[ion-datetime] - Exception thrown from provided `isDateEnabled` function. Please check your function and try again.", e);
                }
            }

            DatetimeHighlightStyle? style = null;
            if (HighlightedDates is { } highlighted && !isActive && !isPadding && !cell.IsAdjacentDay)
            {
                style = DatetimeUtils.GetHighlightStyles(highlighted, dateIso, this);
            }

            dayControl.Update(
                reference with { DayOfWeek = cell.DayOfWeek },
                isPadding ? string.Empty : DatetimeIntl.Day(culture, reference),
                isPadding ? null : DatetimeIntl.DayAriaLabel(culture, reference, isToday),
                isPadding: isPadding,
                isActive: isActive,
                isToday: isToday && !isPadding,
                isDayDisabled: isCalDayDisabled && !isPadding,
                isConstrained: isCalDayDisabled && isDatetimeDisabled,
                isAdjacent: cell.IsAdjacentDay,
                isEnabled: !(isCalDayDisabled || isDatetimeDisabled),
                isReadonly: Readonly,
                highlight: style);
        }

        return isWorkingMonth || !swipeDisabled;
    }

    private void OnDayClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not DatetimeCalendarDay day || day.IsPadding)
        {
            return;
        }

        var reference = day.Parts with { DayOfWeek = null };
        var activePart = ActivePartWithFallback;
        if (day.IsAdjacent)
        {
            var shown = _working;
            // Ionic blurs, sets the active parts and confirms at once, past the default buttons. Avonic selects the day as any other
            // (ADR 0005, docs/infra/ionic-issues.md), then shows its month.
            if (Multiple)
            {
                SetActiveParts(reference with { IsAdjacentDay = false }, day.IsActive);
            }
            else
            {
                SetActiveParts(activePart.With(reference) with { IsAdjacentDay = false });
            }

            // The scroll starts from the month that was shown, as Ionic's leaves the working parts to it.
            _working = shown;
            AnimateToDate(reference with { IsAdjacentDay = false });
            return;
        }

        _working = _working.With(reference);
        if (Multiple)
        {
            SetActiveParts(reference, day.IsActive);
        }
        else
        {
            SetActiveParts(activePart.With(reference));
        }
    }

    private void OnMonthYearClick(object? sender, RoutedEventArgs e)
    {
        _showMonthAndYear = !_showMonthAndYear;
        Render();
    }

    private void OnPreviousClick(object? sender, RoutedEventArgs e) => PrevMonth();

    private void OnNextClick(object? sender, RoutedEventArgs e) => NextMonth();

    /// <summary>Ionic's <c>nextMonth</c>: a smooth scroll to the last month, at once when <see cref="AvonicConfig.Animated"/> is off.</summary>
    internal void NextMonth()
    {
        if (UsesSingleGrid)
        {
            ChangeGridMonth(next: true);
            return;
        }

        if (_calendarBody is null || _smooth is null)
        {
            ResolveForcedWithoutScroll();
            return;
        }

        _smooth.To(_calendarBody.Viewport.Width * 2, AvonicConfig.Animated);
    }

    /// <summary>Ionic's <c>prevMonth</c>.</summary>
    internal void PrevMonth()
    {
        if (UsesSingleGrid)
        {
            ChangeGridMonth(next: false);
            return;
        }

        if (_calendarBody is null || _smooth is null)
        {
            ResolveForcedWithoutScroll();
            return;
        }

        _smooth.To(0, AvonicConfig.Animated);
    }

    /// <summary>With no calendar to scroll, a forced month is the working month at once.</summary>
    private void ResolveForcedWithoutScroll()
    {
        if (_forceRenderDate is { } forced)
        {
            _working = _working with { Month = forced.Month, Day = forced.Day, Year = forced.Year };
            _forceDateScrolling?.TrySetResult();
        }
    }

    /// <summary>Each month is the calendar's width (width: 100%), which a horizontal scroller doesn't pass on as it measures.</summary>
    private void OnCalendarBodySize(object? sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
        {
            _calendarMonths?.InvalidateMeasure();
        }
    }

    /// <summary>The scroll listener: a change of the offset; and, as the calendar gets its size, the working month put in view.</summary>
    private void OnCalendarScroll(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_calendarBody is null)
        {
            return;
        }

        if (e.Property == ScrollViewer.ExtentProperty || e.Property == ScrollViewer.ViewportProperty)
        {
            // Ionic's scrollLeft = month width, once the calendar has its size: the working month in view.
            if (_smooth?.IsRunning != true)
            {
                _calendarBody.Offset = new Vector(_calendarBody.Viewport.Width, 0);
            }

            return;
        }

        if (e.Property != ScrollViewer.OffsetProperty || e.GetOldValue<Vector>().X == e.GetNewValue<Vector>().X)
        {
            return;
        }

        _scrollRest?.Dispose();
        _scrollRest = AvonicConfig.Schedule(UpdateActiveMonth, ScrollRest);
    }

    /// <summary>
    /// Ionic's <c>updateActiveMonth</c>: when the scroll rests on the first or last month, lined up within 2 px, that month is the
    /// working one and the scroller goes back to the middle.
    /// </summary>
    private void UpdateActiveMonth()
    {
        _scrollRest = null;
        if (_calendarBody is null)
        {
            return;
        }

        var width = _calendarBody.Viewport.Width;
        var x = _calendarBody.Offset.X;
        var atStart = x <= 2;
        if (atStart ? Math.Abs(x) > 2 : Math.Abs(2 * width - x) > 2)
        {
            return;
        }

        DatetimeParts next;
        if (_forceRenderDate is { } forced)
        {
            next = forced;
        }
        else
        {
            next = atStart ? DatetimeUtils.GetPreviousMonth(_working) : DatetimeUtils.GetNextMonth(_working);
        }

        if (DatetimeUtils.IsMonthDisabled(new DatetimeParts(next.Month, null, next.Year),
                _minParts is { } lo ? lo with { Day = null } : null, _maxParts is { } hi ? hi with { Day = null } : null))
        {
            return;
        }

        var dayHadFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is DatetimeCalendarDay focused
                          && focused.FindAncestorOfType<AvonicDatetime>() == this;
        _working = _working with { Month = next.Month, Day = next.Day, Year = next.Year };
        Render();
        _calendarBody.Offset = new Vector(width, 0);
        _forceDateScrolling?.TrySetResult();
        if (dayHadFocus)
        {
            _calendarBody.Focus();
        }
    }

    // ── The keyboard ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's focus handling: the calendar reached with the keyboard hands focus to the working day; the days themselves are out of
    /// the Tab order.
    /// </summary>
    private void OnCalendarFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source == sender && e.NavigationMethod == NavigationMethod.Tab)
        {
            FocusWorkingDay();
        }
    }

    /// <summary>Ionic's <c>focusWorkingDay</c>: the working day of the middle month, or of the single grid.</summary>
    internal void FocusWorkingDay()
    {
        var month = UsesSingleGrid ? _calendarGrid : _calendarMonths?.Months.ElementAtOrDefault(1);
        if (month is null || _working.Day is null)
        {
            return;
        }

        var day = month.Days.FirstOrDefault(d => d.IsVisible && !d.IsPadding && !d.IsAdjacent && d.Parts.Day == _working.Day);
        day?.Focus(NavigationMethod.Directional);
    }

    /// <summary>Ionic's calendar <c>keydown</c>: the arrows, Home, End and the pages move the focused day.</summary>
    private void OnCalendarKeyDown(object? sender, KeyEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not DatetimeCalendarDay day || day.IsPadding)
        {
            return;
        }

        var parts = day.Parts;
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        DatetimeParts? target = e.Key switch
        {
            Key.Down => DatetimeUtils.GetNextWeek(parts),
            Key.Up => DatetimeUtils.GetPreviousWeek(parts),
            Key.Right => DatetimeUtils.GetNextDay(parts),
            Key.Left => DatetimeUtils.GetPreviousDay(parts),
            Key.Home => DatetimeUtils.GetStartOfWeek(parts),
            Key.End => DatetimeUtils.GetEndOfWeek(parts),
            Key.PageUp => shift ? DatetimeUtils.GetPreviousYear(parts) : DatetimeUtils.GetPreviousMonth(parts),
            Key.PageDown => shift ? DatetimeUtils.GetNextYear(parts) : DatetimeUtils.GetNextMonth(parts),
            _ => null,
        };

        if (target is not { } to)
        {
            return;
        }

        e.Handled = true;
        if (DatetimeUtils.IsDayDisabled(to, _minParts, _maxParts))
        {
            return;
        }

        // A grid's change on its way ends first, as the key moves from the day it was pressed on.
        if (UsesSingleGrid)
        {
            FinishGridChange();
        }

        SetWorkingParts(_working with { Month = to.Month, Day = to.Day, Year = to.Year });
        AvonicConfig.RequestFrame(this, FocusWorkingDay);
    }

    // ── The month/year wheel ─────────────────────────────────────────────────────────────────

    private void RenderMonthYearWheel(CultureInfo culture)
    {
        if (_monthYearWheel is not null && _showMonthAndYear)
        {
            RenderWheelColumns(_monthYearWheel, culture, DatetimePresentation.MonthYear);
        }
    }

    // ── The time ─────────────────────────────────────────────────────────────────────────────

    private void RenderTime(CultureInfo culture)
    {
        if (Presentation is not (DatetimePresentation.DateTime or DatetimePresentation.TimeDate))
        {
            return;
        }

        if (_timeText is not null)
        {
            _timeText.Text = DatetimeIntl.LocalizedTime(culture, ActivePartWithFallback, DatetimeIntl.GetHourCycle(culture, HourCycle), FormatOptions?.Time);
        }

        if (_timeButton is not null)
        {
            ((IPseudoClasses)_timeButton.Classes).Set(":active", _isTimePopoverOpen);
        }

        if (_timeWheel is not null && _isTimePopoverOpen)
        {
            RenderWheelColumns(_timeWheel, culture, DatetimePresentation.Time);
        }
    }

    private async void OnTimeButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_timePopover is null || _timeButton is null)
        {
            return;
        }

        _isTimePopoverOpen = true;
        Render();
        var dismissed = _timePopover.OnWillDismiss();
        await _timePopover.Present(new PopoverEvent(_timeButton));
        await dismissed;
        _isTimePopoverOpen = false;
        Render();
    }

    /// <summary>Ionic's <c>onWillPresent</c>: each time column's value scrolled into view before the popover shows.</summary>
    private void OnTimePopoverWillPresent(object? sender, RoutedEventArgs e)
    {
        foreach (var column in _timeWheel?.Items.OfType<AvonicPickerColumn>() ?? [])
        {
            column.ScrollActiveItemIntoView();
        }
    }

    // ── The wheels ───────────────────────────────────────────────────────────────────────────

    private void RenderWheel(CultureInfo culture)
    {
        if (_wheel is null || IsGridStyle)
        {
            return;
        }

        RenderWheelColumns(_wheel, culture, Presentation);
    }

    /// <summary>
    /// Ionic's <c>renderWheelPicker</c>: the columns for <paramref name="presentation"/>, in Ionic's order, each kept from render to
    /// render (keyed by its label) with its options rebuilt only when they change.
    /// </summary>
    private void RenderWheelColumns(AvonicPicker picker, CultureInfo culture, DatetimePresentation presentation)
    {
        var columns = new List<AvonicPickerColumn>();
        var existing = picker.Items.OfType<AvonicPickerColumn>().ToDictionary(c => AutomationProperties.GetName(c) ?? string.Empty);
        var date = DateColumns(culture, presentation, existing);
        var time = TimeColumns(culture, presentation, existing);
        columns.AddRange(presentation == DatetimePresentation.TimeDate ? [.. time, .. date] : [.. date, .. time]);

        // order: -1 on the day period column, where the locale's hour starts with it (Korean): first of the whole picker.
        if (columns.FirstOrDefault(c => AutomationProperties.GetName(c) == "Select a day period") is { } period && DatetimeIntl.IsDayPeriodFirst(culture))
        {
            columns.Remove(period);
            columns.Insert(0, period);
        }

        // Year first where the locale writes the year first (wheel-order-year-first): the year, the month, then the day.
        if (!DatetimeIntl.IsMonthFirst(culture) && presentation is DatetimePresentation.Date or DatetimePresentation.MonthYear or DatetimePresentation.Month or DatetimePresentation.Year)
        {
            columns = [.. columns.OrderBy(c => c.Classes.Contains("year-column") ? 0 : c.Classes.Contains("month-column") ? 1 : c.Classes.Contains("day-column") ? 2 : 3)];
            foreach (var column in columns)
            {
                // .wheel-order-year-first: the year at the start, the month and the day at the end, at style priority so an app's wins.
                SetYearFirstAlignment(column, column.Classes.Contains("year-column") ? HorizontalAlignment.Left : HorizontalAlignment.Right);
            }
        }
        else
        {
            foreach (var column in columns)
            {
                SetYearFirstAlignment(column, null);
            }
        }

        foreach (var gone in picker.Items.OfType<AvonicPickerColumn>().Except(columns).ToList())
        {
            SetYearFirstAlignment(gone, null);
        }

        if (!picker.Items.OfType<AvonicPickerColumn>().SequenceEqual(columns))
        {
            picker.Items.Clear();
            foreach (var column in columns)
            {
                picker.Items.Add(column);
            }
        }
    }

    private List<AvonicPickerColumn> DateColumns(CultureInfo culture, DatetimePresentation presentation, Dictionary<string, AvonicPickerColumn> existing)
    {
        var result = new List<AvonicPickerColumn>();
        if (presentation == DatetimePresentation.Time)
        {
            return result;
        }

        if (presentation is DatetimePresentation.DateTime or DatetimePresentation.TimeDate)
        {
            // renderCombinedDatePickerColumn: three months around the working date, narrowed by min and max.
            var months = DatetimeUtils.GenerateMonths(_working);
            var first = months[0] with { Day = 1 };
            var last = months[2] with { Day = DatetimeUtils.GetNumDaysInMonth(months[2].Month, months[2].Year) };
            var min = _minParts is { } lo && DatetimeUtils.IsAfter(lo, first) ? lo : first;
            var max = _maxParts is { } hi && DatetimeUtils.IsBefore(hi, last) ? hi : last;
            var (items, parts) = DatetimeUtils.GetCombinedDateColumnData(culture, _today, min, max, _dayValues, _monthValues);
            if (IsDateEnabled is { } isDateEnabled)
            {
                items = [.. items.Select((item, i) =>
                {
                    try
                    {
                        return item with { Disabled = !isDateEnabled(DatetimeUtils.ConvertDataToIso(parts[i])) };
                    }
                    catch (Exception e)
                    {
                        LogError(this, "[ion-datetime] - Exception thrown from provided `isDateEnabled` function. Please check your function and try again.", e);
                        return item;
                    }
                })];
            }

            var reference = _working.Day is not null ? _working : _default;
            var value = $"{reference.Year}-{reference.Month}-{reference.Day}";
            result.Add(Column(existing, "Select a date", "date-column", items, value, v =>
            {
                var found = parts.FirstOrDefault(p => $"{p.Year}-{p.Month}-{p.Day}" == v as string);
                var active = ActivePartWithFallback;
                _working = _working.With(found);
                SetActiveParts(active.With(found));
            }));
            return result;
        }

        var monthsData = presentation is not DatetimePresentation.Year
            ? DatetimeUtils.GetMonthColumnData(culture, _working, _minParts, _maxParts, _monthValues)
            : [];
        var daysData = presentation == DatetimePresentation.Date
            ? DatetimeUtils.GetDayColumnData(culture, _working, _minParts, _maxParts, _dayValues).Select(d => d.Option).ToList()
            : [];
        if (IsDateEnabled is { } enabled)
        {
            daysData = [.. daysData.Select(d =>
            {
                try
                {
                    return d with { Disabled = !enabled(DatetimeUtils.ConvertDataToIso(new DatetimeParts(_working.Month, (int)d.Value, _working.Year))) };
                }
                catch (Exception e)
                {
                    LogError(this, "[ion-datetime] - Exception thrown from provided `isDateEnabled` function. Please check your function and try again.", e);
                    return d;
                }
            })];
        }

        var yearsData = presentation is not DatetimePresentation.Month
            ? DatetimeUtils.GetYearColumnData(culture, _default, _minParts, _maxParts, _yearValues)
            : [];

        AvonicPickerColumn? MonthColumn() => monthsData.Count == 0 ? null : Column(existing, "Select a month", "month-column", monthsData, _working.Month, v =>
        {
            var active = ActivePartWithFallback;
            _working = _working with { Month = (int?)v };
            if (!Multiple)
            {
                SetActiveParts(active with { Month = (int?)v });
            }
            else
            {
                Render();
            }
        });

        AvonicPickerColumn? DayColumn()
        {
            if (daysData.Count == 0)
            {
                return null;
            }

            var dayValue = _working.Day ?? _default.Day;
            return Column(existing, "Select a day", "day-column", daysData, dayValue, v =>
            {
                var active = ActivePartWithFallback;
                _working = _working with { Day = (int?)v };
                SetActiveParts(active with { Day = (int?)v });
            });
        }

        AvonicPickerColumn? YearColumn() => yearsData.Count == 0 ? null : Column(existing, "Select a year", "year-column", yearsData, _working.Year, v =>
        {
            var active = ActivePartWithFallback;
            _working = _working with { Year = (int?)v };
            if (!Multiple)
            {
                SetActiveParts(active with { Year = (int?)v });
            }
            else
            {
                Render();
            }
        });

        var monthFirst = DatetimeIntl.IsMonthFirst(culture, againstDay: true);
        var ordered = monthFirst ? new[] { MonthColumn(), DayColumn(), YearColumn() } : [DayColumn(), MonthColumn(), YearColumn()];
        result.AddRange(ordered.OfType<AvonicPickerColumn>());
        return result;
    }

    private List<AvonicPickerColumn> TimeColumns(CultureInfo culture, DatetimePresentation presentation, Dictionary<string, AvonicPickerColumn> existing)
    {
        var result = new List<AvonicPickerColumn>();
        if (presentation is DatetimePresentation.Date or DatetimePresentation.Month or DatetimePresentation.MonthYear or DatetimePresentation.Year)
        {
            return result;
        }

        // Min and max limit the time once a date is selected, even unconfirmed.
        var userHasSelectedDate = ActivePart is not null;
        var hourCycle = DatetimeIntl.GetHourCycle(culture, HourCycle);
        var (hours, minutes, periods) = DatetimeUtils.GetTimeColumnsData(culture, _working, hourCycle,
            userHasSelectedDate ? _minParts : null, userHasSelectedDate ? _maxParts : null, _hourValues, _minuteValues);
        var active = ActivePartWithFallback;
        if (hours.Count > 0)
        {
            result.Add(Column(existing, "Select an hour", null, hours, active.Hour, v =>
            {
                _working = _working with { Hour = (int?)v };
                SetActiveParts(ActivePartWithFallback with { Hour = (int?)v });
            }, numericInput: true));
        }

        if (minutes.Count > 0)
        {
            result.Add(Column(existing, "Select a minute", null, minutes, active.Minute, v =>
            {
                _working = _working with { Minute = (int?)v };
                SetActiveParts(ActivePartWithFallback with { Minute = (int?)v });
            }, numericInput: true));
        }

        if (periods.Count > 0)
        {
            var column = Column(existing, "Select a day period", null, periods, active.AmPm, v =>
            {
                var current = _working;
                var ampm = (string)v!;
                var hour = DatetimeUtils.CalculateHourFromAmPm(current, ampm);
                _working = current with { AmPm = ampm, Hour = hour };
                SetActiveParts(ActivePartWithFallback with { AmPm = ampm, Hour = hour });
            });
            result.Add(column);
        }

        return result;
    }

    private readonly Dictionary<AvonicPickerColumn, IDisposable> _yearFirstAlignment = [];

    private void SetYearFirstAlignment(AvonicPickerColumn column, HorizontalAlignment? alignment)
    {
        if (_yearFirstAlignment.Remove(column, out var previous))
        {
            previous.Dispose();
        }

        if (alignment is { } value && column.SetValue(AvonicPickerColumn.HorizontalContentAlignmentProperty, value, Avalonia.Data.BindingPriority.Style) is { } frame)
        {
            _yearFirstAlignment[column] = frame;
        }
    }

    /// <summary>A column kept from the last render, or a new one, with these options and this value.</summary>
    private AvonicPickerColumn Column(Dictionary<string, AvonicPickerColumn> existing, string label, string? cssClass,
        List<DatetimeOption> options, object? value, Action<object?> onChange, bool numericInput = false)
    {
        if (!existing.TryGetValue(label, out var column))
        {
            column = new AvonicPickerColumn { Classes = { WheelClass } };
            AutomationProperties.SetName(column, label);
            if (cssClass is not null)
            {
                column.Classes.Add(cssClass);
            }

            column.NumericInput = numericInput;
            column.IonChange += (_, e) =>
            {
                e.Handled = true;
                if (column.Tag is Action<object?> change)
                {
                    change(e.Value);
                }
            };
        }

        column.Tag = onChange;
        column.Color = Color;
        column.IsEnabled = IsEnabled;
        var current = column.Items.OfType<AvonicPickerColumnOption>().ToList();
        var same = current.Count == options.Count && current.Zip(options).All(x =>
            Equals(x.First.Value, x.Second.Value) && (x.First.Content as string) == x.Second.Text && x.First.IsEnabled == !x.Second.Disabled);
        if (!same)
        {
            column.Items.Clear();
            foreach (var option in options)
            {
                column.Items.Add(new AvonicPickerColumnOption { Value = option.Value, Content = option.Text, IsEnabled = !option.Disabled, Classes = { WheelItemClass } });
            }

            current = column.Items.OfType<AvonicPickerColumnOption>().ToList();
        }

        foreach (var option in current)
        {
            option.Color = Color;
            option.Classes.Set(ActiveClass, Equals(option.Value, value));
        }

        if (!Equals(column.Value, value))
        {
            column.Value = value;
        }

        return column;
    }

    // ── The footer ───────────────────────────────────────────────────────────────────────────

    private void RenderFooter()
    {
        var buttonEnabled = IsEnabled && !Readonly;
        foreach (var (button, text) in new[] { (_cancelButton, CancelText), (_clearButton, ClearText), (_confirmButton, DoneText) })
        {
            if (button is null)
            {
                continue;
            }

            button.Content = text;
            button.Color = Color;
            button.SetCurrentValue(IsEnabledProperty, buttonEnabled);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Cancel(true);

    private void OnClearClick(object? sender, RoutedEventArgs e) => Clear();

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Confirm(true);

    // ── Focus ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's <c>onFocus</c> and <c>onBlur</c> on the host: when focus comes into the datetime and leaves it, not as it moves
    /// inside (measured). The inner buttons' own <c>ionFocus</c> and <c>ionBlur</c> stop here, as Ionic stops them.
    /// </summary>
    private void OnKeyboardFocusWithinChanged(bool within) =>
        RaiseEvent(new RoutedEventArgs(within ? IonFocusEvent : IonBlurEvent));
}
