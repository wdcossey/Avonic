using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>Ionic's state: the working and active parts, and what changes them (<c>processValue</c>, <c>setActiveParts</c>, the methods).</summary>
public partial class AvonicDatetime
{
    private bool _loaded;
    private DatetimeParts _working = new(5, 28, 2021, 13, 52, "pm");

    /// <summary>Ionic's <c>activeParts</c>: one set of parts, or a list (<see cref="_activeIsList"/>), which starts empty.</summary>
    private List<DatetimeParts> _active = [];
    private bool _activeIsList = true;
    private DatetimeParts _default;
    private DatetimeParts _today;
    private DatetimeParts? _minParts;
    private DatetimeParts? _maxParts;
    private int[]? _dayValues;
    private int[]? _monthValues;
    private int[]? _yearValues;
    private int[]? _hourValues;
    private int[]? _minuteValues;
    private bool _hasUnconfirmedSelection;
    private bool _showMonthAndYear;
    private bool _isTimePopoverOpen;
    private DatetimeParts? _forceRenderDate;
    private TaskCompletionSource? _forceDateScrolling;
    private AvonicOverlayHook? _overlayHook;
    private DatetimePresentation? _prevPresentation;

    /// <summary>What's shown: the month, the wheels' positions.</summary>
    internal DatetimeParts WorkingParts => _working;

    /// <summary>What's selected.</summary>
    internal IReadOnlyList<DatetimeParts> ActiveParts => _active;

    internal bool ShowMonthAndYear => _showMonthAndYear;

    internal bool HasUnconfirmedSelection => _hasUnconfirmedSelection;

    /// <summary>Ionic's <c>isGridStyle</c>: a date presentation without <see cref="PreferWheel"/>.</summary>
    internal bool IsGridStyle => IsCalendarPicker && !PreferWheel;

    /// <summary>Ionic's <c>isCalendarPicker</c>: <c>date</c>, <c>date-time</c> or <c>time-date</c>.</summary>
    internal bool IsCalendarPicker => Presentation is DatetimePresentation.Date or DatetimePresentation.DateTime or DatetimePresentation.TimeDate;

    /// <summary>Ionic's <c>getActivePart</c>: the first selected, or none.</summary>
    internal DatetimeParts? ActivePart => _active.Count > 0 ? _active[0] : null;

    /// <summary>Ionic's <c>getActivePartsWithFallback</c>.</summary>
    internal DatetimeParts ActivePartWithFallback => ActivePart ?? _default;

    /// <summary>Ionic's internal <c>getDefaultPart()</c>, for <c>ion-datetime-button</c>: today, snapped to the constraints.</summary>
    internal DatetimeParts GetDefaultPart()
    {
        EnsureLoaded();
        return _default;
    }

    private bool HasSlottedButtons => ButtonsContent is not null;

    /// <summary>Ionic's <c>componentWillLoad</c>, on the first attach or the first call that needs it.</summary>
    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        if (Multiple)
        {
            if (Presentation != DatetimePresentation.Date)
            {
                Warn(this, "[ion-datetime] - Multiple date selection is only supported for presentation=\"date\".");
            }

            if (PreferWheel)
            {
                Warn(this, "[ion-datetime] - Multiple date selection is not supported with preferWheel=\"true\".");
            }
        }

        if (HighlightedDates is not null)
        {
            if (!IsCalendarPicker)
            {
                Warn(this, "[ion-datetime] - The highlightedDates property is only supported with the date, date-time, and time-date presentations.");
            }

            if (PreferWheel)
            {
                Warn(this, "[ion-datetime] - The highlightedDates property is not supported with preferWheel=\"true\".");
            }
        }

        CheckFormatOptions();
        _hourValues = HourValues?.Values.ToArray();
        _minuteValues = MinuteValues?.Values.ToArray();
        _monthValues = MonthValues?.Values.ToArray();
        _yearValues = YearValues?.Values.ToArray();
        _dayValues = DayValues?.Values.ToArray();
        _today = DatetimeUtils.PartsOf(Now());
        // min and max read the default parts for a missing year; at load those are today's, as they become.
        _default = _today;
        ProcessMinParts();
        ProcessMaxParts();
        _default = DatetimeUtils.GetClosestValidDate(_today, _monthValues, _dayValues, _yearValues, _hourValues, _minuteValues, _minParts, _maxParts);
        ProcessValue(Value);
        EmitStyle();
    }

    private void ProcessMinParts() => _minParts = Min is null ? null : DatetimeUtils.ParseMinParts(Min, _default, this);

    private void ProcessMaxParts() => _maxParts = Max is null ? null : DatetimeUtils.ParseMaxParts(Max, _default, this);

    /// <summary><c>formatOptionsChanged</c>: <c>checkForPresentationFormatMismatch</c> and <c>warnIfTimeZoneProvided</c>.</summary>
    private void CheckFormatOptions(bool presentationOnly = false)
    {
        if (FormatOptions is not { } options)
        {
            return;
        }

        if (!presentationOnly && (options.Date?.TimeZone is not null || options.Date?.TimeZoneName is not null
            || options.Time?.TimeZone is not null || options.Time?.TimeZoneName is not null))
        {
            Warn(this, "[ion-datetime] - \"timeZone\" and \"timeZoneName\" are not supported in \"formatOptions\".");
        }

        switch (Presentation)
        {
            case DatetimePresentation.Date or DatetimePresentation.MonthYear or DatetimePresentation.Month or DatetimePresentation.Year
                when options.Date is null:
                Warn(this, $"[ion-datetime] - The '{PresentationName(Presentation)}' presentation requires a date object in formatOptions.");
                break;
            case DatetimePresentation.Time when options.Time is null:
                Warn(this, "[ion-datetime] - The 'time' presentation requires a time object in formatOptions.");
                break;
            case DatetimePresentation.DateTime or DatetimePresentation.TimeDate when options.Date is null && options.Time is null:
                Warn(this, $"[ion-datetime] - The '{PresentationName(Presentation)}' presentation requires either a date or time object (or both) in formatOptions.");
                break;
        }
    }

    internal static string PresentationName(DatetimePresentation presentation) => presentation switch
    {
        DatetimePresentation.DateTime => "date-time",
        DatetimePresentation.TimeDate => "time-date",
        DatetimePresentation.MonthYear => "month-year",
        _ => presentation.ToString().ToLowerInvariant(),
    };

    /// <summary><c>emitStyle</c>: what an item around reads, <c>item-interactive</c>, <c>item-datetime</c> and <c>item-interactive-disabled</c>.</summary>
    private void EmitStyle() =>
        RaiseEvent(new IonStyleEventArgs(new Dictionary<string, bool> { ["interactive"] = true, ["datetime"] = true, ["interactive-disabled"] = !IsEnabled }));

    /// <summary>Ionic's <c>setWorkingParts</c>.</summary>
    internal void SetWorkingParts(DatetimeParts parts)
    {
        _working = parts;
        Render();
    }

    /// <summary>Ionic's <c>setValue</c>: the value, and <c>ionChange</c>, every time.</summary>
    private void SetValue(DatetimeValue? value)
    {
        SetCurrentValue(ValueProperty, value);
        RaiseEvent(new DatetimeChangeEventArgs(IonChangeEvent, value));
    }

    /// <summary>
    /// Ionic's <c>setActiveParts</c>: a selection. Nothing while read-only; the parts validated against min and max; then, unless
    /// the default buttons or the buttons slot wait for <see cref="Confirm"/>, confirmed.
    /// </summary>
    internal void SetActiveParts(DatetimeParts parts, bool removeDate = false)
    {
        if (Readonly)
        {
            // The working parts the caller set still show, as Ionic's setWorkingParts renders before this returns.
            Render();
            return;
        }

        _hasUnconfirmedSelection = true;
        var validated = DatetimeUtils.ValidateParts(parts, _minParts, _maxParts);
        _working = validated;
        if (Multiple)
        {
            var list = _activeIsList ? _active : [.. _active];
            _active = removeDate ? [.. list.Where(p => !DatetimeUtils.IsSameDay(p, validated))] : [.. list, validated];
            _activeIsList = true;
        }
        else
        {
            _active = [validated];
            _activeIsList = false;
        }

        Render();
        if (HasSlottedButtons || ShowDefaultButtons)
        {
            return;
        }

        Confirm();
    }

    /// <summary>
    /// Ionic's <c>processValue</c>: a value from code, <see cref="Reset"/> or the present of a parent overlay. It selects the value
    /// (or nothing) and shows its month: animated in the grid when the month changes and the calendar is shown.
    /// </summary>
    private void ProcessValue(DatetimeValue? value, bool animated = true)
    {
        _hasUnconfirmedSelection = false;
        var hasValue = value is { HasValue: true };
        List<DatetimeParts>? list = null;
        DatetimeParts? single = null;
        if (hasValue)
        {
            if (value!.Value.IsList)
            {
                list = DatetimeUtils.ParseDates(value.Value.Values, this);
                if (list is null)
                {
                    return;
                }
            }
            else
            {
                single = DatetimeUtils.ParseDate(value.Value.Single, this);
                if (single is null)
                {
                    return;
                }
            }
        }
        else
        {
            single = _default;
        }

        if (!Multiple && value is { IsList: true })
        {
            Warn(this, $"[ion-datetime] - An array of values was passed, but multiple is \"false\". This is incorrect usage and may result in unexpected behaviors. To dismiss this warning, pass a string to the \"value\" property when multiple=\"false\". Value Passed: [{string.Join(", ", value.Value.Values.Select(v => $"'{v}'"))}]");
        }

        if (hasValue)
        {
            DatetimeUtils.WarnIfValueOutOfBounds(list ?? [single!.Value], _minParts, _maxParts, this);
        }

        var target = DatetimeUtils.ClampDate(list is { Count: > 0 } ? list[^1] : single!.Value, _minParts, _maxParts);
        var ampm = DatetimeUtils.ParseAmPm(target.Hour);
        if (hasValue)
        {
            if (list is not null)
            {
                _active = [.. list];
                _activeIsList = true;
            }
            else
            {
                _active = [new DatetimeParts(target.Month, target.Day, target.Year, target.Hour, target.Minute, ampm)];
                _activeIsList = false;
            }
        }
        else
        {
            _active = [];
            _activeIsList = true;
        }

        var didChangeMonth = (target.Month is not null && target.Month != _working.Month) || (target.Year is not null && target.Year != _working.Year);
        if (IsGridStyle && didChangeMonth && IsCalendarReady && !_showMonthAndYear && animated)
        {
            AnimateToDate(target);
        }
        else
        {
            // A time alone has no month for the grid to show (Ionic draws "Invalid Date" there): the default parts' month shows.
            _working = new DatetimeParts(target.Month ?? _default.Month, target.Year is null ? _default.Day : target.Day, target.Year ?? _default.Year,
                target.Hour, target.Minute, ampm);
            Render();
        }
    }

    /// <summary>
    /// Ionic's <c>animateToDate</c>: the target month drawn in place of the next (or previous) one, scrolled to, and made the working
    /// month when the scroll rests.
    /// </summary>
    private async void AnimateToDate(DatetimeParts target)
    {
        _forceRenderDate = target;
        var done = new TaskCompletionSource();
        _forceDateScrolling = done;
        Render();
        if (DatetimeUtils.IsBefore(target, _working))
        {
            PrevMonth();
        }
        else
        {
            NextMonth();
        }

        await done.Task;
        if (_forceDateScrolling == done)
        {
            _forceDateScrolling = null;
        }

        _forceRenderDate = null;
        Render();
    }

    /// <summary>Ionic <c>confirm()</c>: the selection as the value, with <c>ionChange</c>; and the parent overlay dismissed if asked.</summary>
    public void Confirm(bool closeOverlay = false)
    {
        EnsureLoaded();
        if (_activeIsList && _active.Count == 0)
        {
            // Ionic writes the working parts only with preferWheel; Avonic for every presentation that shows a wheel, as its
            // comment means (ADR 0005, docs/infra/ionic-issues.md).
            SetValue(PreferWheel || !IsGridStyle ? new DatetimeValue(DatetimeUtils.ConvertDataToIso(_working)) : (DatetimeValue?)null);
        }
        else
        {
            SetValue(_activeIsList
                ? new DatetimeValue(_active.Select(DatetimeUtils.ConvertDataToIso))
                : DatetimeUtils.ConvertDataToIso(_active[0]));
        }

        _hasUnconfirmedSelection = false;
        if (closeOverlay)
        {
            CloseParentOverlay(ConfirmRole);
        }
    }

    /// <summary>Ionic <c>reset()</c>: the state to <paramref name="startDate"/>, or to nothing selected and today's month; the value stays.</summary>
    public void Reset(string? startDate = null)
    {
        EnsureLoaded();
        ProcessValue(startDate is null ? null : (DatetimeValue?)new DatetimeValue(startDate));
    }

    /// <summary>
    /// Ionic <c>cancel()</c>: <c>ionCancel</c>; then the parent overlay dismissed if asked (its next present discards the
    /// selection), or the selection discarded now.
    /// </summary>
    public void Cancel(bool closeOverlay = false)
    {
        EnsureLoaded();
        RaiseEvent(new RoutedEventArgs(IonCancelEvent));
        if (!closeOverlay || !CloseParentOverlay(CancelRole))
        {
            ProcessValue(Value);
        }
    }

    /// <summary>The nearest <see cref="AvonicModal"/> or <see cref="AvonicPopover"/> around the datetime, as <c>closest('ion-modal, ion-popover')</c>.</summary>
    internal AvonicOverlay? ParentOverlay =>
        this.GetLogicalAncestors().OfType<AvonicOverlay>().FirstOrDefault(o => o is AvonicModal or AvonicPopover);

    private bool CloseParentOverlay(string role)
    {
        if (ParentOverlay is not { } overlay)
        {
            return false;
        }

        _ = overlay.Dismiss(null, role);
        return true;
    }

    /// <summary>Ionic's <c>clearButtonClick</c>.</summary>
    private void Clear()
    {
        Reset();
        SetValue(null);
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        EnsureLoaded();
        _overlayHook?.Dispose();
        _overlayHook = ParentOverlay is { } overlay ? new AvonicOverlayHook(this, overlay) : null;
    }

    /// <summary>An item around hears <c>ionStyle</c> once the datetime is in the visual tree, which routes the event to it.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EmitStyle();
        UpdateGridGesture(inTree: true);
    }

    /// <summary>A grid's fade on its way ends at its month, and its swipe stops listening, so that nothing goes on once it's gone.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        FinishGridChange();
        UpdateGridGesture(inTree: false);
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _overlayHook?.Dispose();
        _overlayHook = null;
    }

    /// <summary>
    /// Ionic's <c>initializeOverlayPresentListener</c>: the parent overlay's own will-present discards an unconfirmed selection,
    /// with no month animation, as the overlay is still hidden; and, as the datetime was hidden, the month/year wheel is closed.
    /// </summary>
    private sealed class AvonicOverlayHook : IDisposable
    {
        private readonly AvonicDatetime _owner;
        private readonly AvonicOverlay _overlay;
        private readonly RoutedEvent<RoutedEventArgs> _event;

        public AvonicOverlayHook(AvonicDatetime owner, AvonicOverlay overlay)
        {
            _owner = owner;
            _overlay = overlay;
            _event = overlay is AvonicModal ? AvonicModal.IonModalWillPresentEvent : AvonicPopover.IonPopoverWillPresentEvent;
            overlay.AddHandler(_event, OnPresent);
        }

        private void OnPresent(object? sender, RoutedEventArgs e)
        {
            if (e.Source != _overlay)
            {
                return;
            }

            // The datetime was hidden with its overlay: Ionic's hidden observer closes the month/year wheel.
            if (_owner._showMonthAndYear)
            {
                _owner._showMonthAndYear = false;
                _owner.Render();
            }

            if (_owner._hasUnconfirmedSelection)
            {
                _owner.ProcessValue(_owner.Value, animated: false);
            }
        }

        public void Dispose() => _overlay.RemoveHandler(_event, OnPresent);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsKeyboardFocusWithinProperty)
        {
            OnKeyboardFocusWithinChanged(change.GetNewValue<bool>());
        }

        if (change.Property == TitleContentProperty)
        {
            RaisePropertyChanged(ShownTitleProperty, change.GetOldValue<object?>() ?? "Select Date", ShownTitle);
        }
        else if (change.Property == TimeLabelContentProperty)
        {
            RaisePropertyChanged(ShownTimeLabelProperty, change.GetOldValue<object?>() ?? "Time", ShownTimeLabel);
        }

        if (change.Property == CalendarRenderingProperty)
        {
            OnCalendarRenderingChanged();
            return;
        }

        if (!_loaded)
        {
            return;
        }

        if (change.Property == ValueProperty)
        {
            var value = change.GetNewValue<DatetimeValue?>();
            if (value is not null)
            {
                ProcessValue(value);
            }

            EmitStyle();
            ValueChange?.Invoke(this, value);
        }
        else if (change.Property == IsEnabledProperty)
        {
            EmitStyle();
            Render();
        }
        else if (change.Property == MinProperty)
        {
            ProcessMinParts();
            Render();
        }
        else if (change.Property == MaxProperty)
        {
            ProcessMaxParts();
            Render();
        }
        else if (change.Property == FormatOptionsProperty)
        {
            CheckFormatOptions();
            Render();
        }
        else if (change.Property == PresentationProperty)
        {
            CheckFormatOptions(presentationOnly: true);
            Render();
        }
        else if (change.Property == YearValuesProperty)
        {
            _yearValues = YearValues?.Values.ToArray();
            Render();
        }
        else if (change.Property == MonthValuesProperty)
        {
            _monthValues = MonthValues?.Values.ToArray();
            Render();
        }
        else if (change.Property == DayValuesProperty)
        {
            _dayValues = DayValues?.Values.ToArray();
            Render();
        }
        else if (change.Property == HourValuesProperty)
        {
            _hourValues = HourValues?.Values.ToArray();
            Render();
        }
        else if (change.Property == MinuteValuesProperty)
        {
            _minuteValues = MinuteValues?.Values.ToArray();
            Render();
        }
        else if (change.Property == ColorProperty || change.Property == LocaleProperty || change.Property == HourCycleProperty
                 || change.Property == FirstDayOfWeekProperty || change.Property == IsDateEnabledProperty
                 || change.Property == HighlightedDatesProperty || change.Property == MultipleProperty
                 || change.Property == PreferWheelProperty || change.Property == ReadonlyProperty
                 || change.Property == ShowAdjacentDaysProperty || change.Property == ShowClearButtonProperty
                 || change.Property == ShowDefaultButtonsProperty || change.Property == ShowDefaultTimeLabelProperty
                 || change.Property == ShowDefaultTitleProperty || change.Property == SizeProperty
                 || change.Property == TitleSelectedDatesFormatterProperty || change.Property == TitleContentProperty
                 || change.Property == TimeLabelContentProperty || change.Property == ButtonsContentProperty
                 || change.Property == CancelTextProperty || change.Property == ClearTextProperty || change.Property == DoneTextProperty)
        {
            Render();
        }
    }

    private static void LogError(object source, string message, Exception e) =>
        Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(source, message + " {Exception}", e);
}
