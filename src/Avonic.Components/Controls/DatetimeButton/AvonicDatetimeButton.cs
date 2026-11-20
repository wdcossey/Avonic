using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-datetime-button</c>: the value of an <see cref="AvonicDatetime"/>, named by <see cref="Datetime"/>, on one or two
/// buttons, as the datetime's locale writes it; a tap presents the <see cref="AvonicModal"/> or <see cref="AvonicPopover"/> the
/// datetime is in, showing the half that was tapped. See <c>docs/components/ion-datetime-button/README.md</c>. The look is in the
/// theme; the buttons' text, whether they show and their expanded state are set here, as Ionic's <c>render()</c> sets them.
/// </summary>
[TemplatePart("PART_DateButton", typeof(DatetimeButtonNative))]
[TemplatePart("PART_TimeButton", typeof(DatetimeButtonNative))]
[PseudoClasses(":date-active", ":time-active")]
public class AvonicDatetimeButton : TemplatedControl
{
    /// <summary>The style class the datetime's overlay gets, Ionic's <c>ion-datetime-button-overlay</c>.</summary>
    public const string OverlayClass = "ion-datetime-button-overlay";

    /// <summary>The theme's resources for that class: the overlay's size variables, <c>fit-content</c> (core.css).</summary>
    internal const string OverlayResourcesKey = "Avonic.DatetimeButton.Overlay";

    /// <summary>Ionic <c>color</c>: the active button's text.</summary>
    public static readonly StyledProperty<string?> ColorProperty = ColorPalette.ColorProperty.AddOwner<AvonicDatetimeButton>();

    /// <summary>Ionic <c>datetime</c>: the <c>Name</c> of the <see cref="AvonicDatetime"/>, read once, at the first attach.</summary>
    public static readonly StyledProperty<string?> DatetimeProperty =
        AvaloniaProperty.Register<AvonicDatetimeButton, string?>(nameof(Datetime));

    /// <summary>Ionic's <c>date-target</c> slot: in place of the date's text.</summary>
    public static readonly StyledProperty<object?> DateTargetContentProperty =
        AvaloniaProperty.Register<AvonicDatetimeButton, object?>(nameof(DateTargetContent));

    public static readonly StyledProperty<IDataTemplate?> DateTargetContentTemplateProperty =
        AvaloniaProperty.Register<AvonicDatetimeButton, IDataTemplate?>(nameof(DateTargetContentTemplate));

    /// <summary>Ionic's <c>time-target</c> slot: in place of the time's text.</summary>
    public static readonly StyledProperty<object?> TimeTargetContentProperty =
        AvaloniaProperty.Register<AvonicDatetimeButton, object?>(nameof(TimeTargetContent));

    public static readonly StyledProperty<IDataTemplate?> TimeTargetContentTemplateProperty =
        AvaloniaProperty.Register<AvonicDatetimeButton, IDataTemplate?>(nameof(TimeTargetContentTemplate));

    private bool _lookedUp;
    private string? _triedName;
    private bool _watching;
    private bool _ready;
    private bool _attached;
    private AvonicDatetime? _datetime;
    private AvonicOverlay? _overlay;
    private DatetimePresentation _presentation = DatetimePresentation.DateTime;
    private bool _active;
    private Selected _selected;
    private string? _dateText;
    private string? _timeText;
    private DatetimeButtonNative? _dateButton;
    private DatetimeButtonNative? _timeButton;
    private TopLevel? _layoutRoot;
    private Action? _pendingPresent;

    /// <summary>Ionic's <c>selectedButton</c>: which button is the active one while the datetime shows.</summary>
    private enum Selected
    {
        None,
        Date,
        Time,
    }

    static AvonicDatetimeButton()
    {
        // CSS's host doesn't clip: a button's focus ring is 1 px outside it.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicDatetimeButton>(false);
    }

    public string? Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    public string? Datetime { get => GetValue(DatetimeProperty); set => SetValue(DatetimeProperty, value); }

    public object? DateTargetContent { get => GetValue(DateTargetContentProperty); set => SetValue(DateTargetContentProperty, value); }

    public IDataTemplate? DateTargetContentTemplate
    {
        get => GetValue(DateTargetContentTemplateProperty);
        set => SetValue(DateTargetContentTemplateProperty, value);
    }

    public object? TimeTargetContent { get => GetValue(TimeTargetContentProperty); set => SetValue(TimeTargetContentProperty, value); }

    public IDataTemplate? TimeTargetContentTemplate
    {
        get => GetValue(TimeTargetContentTemplateProperty);
        set => SetValue(TimeTargetContentTemplateProperty, value);
    }

    /// <summary>The datetime found at load, or null.</summary>
    internal AvonicDatetime? DatetimeElement => _datetime;

    /// <summary>Ionic's <c>dateText</c>: the date button's text; null hides it.</summary>
    internal string? DateText => _dateText;

    /// <summary>Ionic's <c>timeText</c>: the time button's text; null hides it.</summary>
    internal string? TimeText => _timeText;

    /// <summary>Ionic's <c>datetimeActive</c>: the datetime is shown.</summary>
    internal bool IsDatetimeActive => _active;

    // ── Load ─────────────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        _attached = true;
        if (!_lookedUp || _datetime is null && Datetime != _triedName)
        {
            // The first attach, or a name that came while it was out of the tree after one that found nothing.
            Load();
        }
        else if (_ready && _datetime is not null)
        {
            // The listener was off while detached: the text as it would have followed.
            _datetime.ValueChange += OnValueChange;
            SetDateTimeText();
        }
        else if (_datetime is not null)
        {
            WaitForDatetime(_datetime);
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        _attached = false;
        if (_datetime is not null)
        {
            _datetime.ValueChange -= OnValueChange;
            _datetime.AttachedToLogicalTree -= OnDatetimeAttached;
        }

        CancelPendingPresent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Ionic reads datetime once, at load, where a framework has already set it. A binding here can bring it after the first
        // attach, so a name that comes while none was found is looked up then; once found, it's kept, as Ionic's is.
        if (change.Property == DatetimeProperty && _lookedUp && _datetime is null && _attached)
        {
            Load();
        }
    }

    /// <summary>Ionic's <c>componentWillLoad</c>.</summary>
    private void Load()
    {
        _lookedUp = true;
        var name = _triedName = Datetime;
        if (string.IsNullOrEmpty(name))
        {
            LogError("[ion-datetime-button] - An ID associated with an ion-datetime instance is required to function properly.");
            return;
        }

        var found = Find(name);
        if (found is null)
        {
            LogError($"[ion-datetime-button] - No ion-datetime instance found for ID '{name}'.");
            return;
        }

        if (found is not AvonicDatetime datetime)
        {
            LogError($"[ion-datetime-button] - Expected an ion-datetime instance for ID '{name}' but received '{found.GetType().Name}' instead.");
            return;
        }

        _datetime = datetime;

        // closest('ion-modal, ion-popover'), and the class that sizes it by the datetime.
        _overlay = datetime.ParentOverlay;
        if (_overlay is not null)
        {
            AddOverlayClass(_overlay);
            // Its enter and leave move the datetime by a render transform, which causes no layout pass: a check at each end.
            _overlay.DidPresent += (_, _) => CheckActive();
            _overlay.DidDismiss += (_, _) => CheckActive();
        }

        // componentOnReady: once the datetime has loaded, which it does when it's attached, after its styles, so that what an app's
        // style sets on it (its presentation, min, max) is what it loads with. One later in the tree than the button loads after it.
        WaitForDatetime(datetime);
    }

    private void WaitForDatetime(AvonicDatetime datetime)
    {
        if (((ILogical)datetime).IsAttachedToLogicalTree)
        {
            OnDatetimeReady();
        }
        else
        {
            // Not kept while the button is out of the tree, so that a datetime never attached doesn't hold on to it.
            datetime.AttachedToLogicalTree += OnDatetimeAttached;
        }
    }

    private void OnDatetimeAttached(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        _datetime!.AttachedToLogicalTree -= OnDatetimeAttached;
        OnDatetimeReady();
    }

    /// <summary>Ionic's <c>componentOnReady</c> callback: the presentation, read once, the text, and the selected button.</summary>
    private void OnDatetimeReady()
    {
        var datetime = _datetime!;
        _ready = true;
        _presentation = datetime.Presentation;
        _selected = _presentation is DatetimePresentation.TimeDate or DatetimePresentation.Time ? Selected.Time : Selected.Date;
        if (_attached)
        {
            datetime.ValueChange += OnValueChange;
        }

        datetime.AttachedToVisualTree += OnDatetimeVisualChanged;
        datetime.DetachedFromVisualTree += OnDatetimeVisualChanged;
        SetDateTimeText();
        UpdateWatch();
    }

    /// <summary>
    /// <c>document.getElementById</c>: the name in the nearest name scope, then in each one around it (a template's, a user control's,
    /// then the page's), as an id is the document's; from an overlay that's presented, on from its placeholder, where it was.
    /// </summary>
    private object? Find(string name)
    {
        for (StyledElement? element = this; element is not null;)
        {
            if (NameScope.GetNameScope(element) is { } scope && scope.Find(name) is { } found)
            {
                return found;
            }

            element = element is AvonicOverlay { Parent: OverlayHost, Placeholder: { } placeholder } ? placeholder : element.Parent;
        }

        return null;
    }

    /// <summary>
    /// <c>overlayEl.classList.add('ion-datetime-button-overlay')</c>, and core.css's rule for it: the overlay's size variables are
    /// <c>fit-content</c>, the theme's dictionary of them, each resolved, merged into the overlay's resources, so an app's own key
    /// there wins. Once per overlay, whatever the number of buttons.
    /// </summary>
    private void AddOverlayClass(AvonicOverlay overlay)
    {
        if (overlay.Classes.Contains(OverlayClass))
        {
            return;
        }

        overlay.Classes.Add(OverlayClass);
        if (this.TryFindResource(OverlayResourcesKey, out var found) && found is IResourceDictionary theme)
        {
            var copy = new ResourceDictionary();
            foreach (var key in theme.Keys)
            {
                if (theme.TryGetResource(key, null, out var value))
                {
                    copy[key] = value;
                }
            }

            overlay.Resources.MergedDictionaries.Add(copy);
        }
    }

    /// <summary>Ionic's <c>printIonError</c>. Tests read <see cref="LastError"/>.</summary>
    private void LogError(string message)
    {
        LastError = message;
        Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "{Message}", message);
    }

    /// <summary>The last error logged, for tests.</summary>
    internal string? LastError { get; private set; }

    // ── The text ─────────────────────────────────────────────────────────────────────────────

    private void OnValueChange(object? sender, DatetimeValue? value) => SetDateTimeText();

    /// <summary>Ionic's <c>setDateTimeText</c>: the datetime's value, as its locale and format options write it.</summary>
    private void SetDateTimeText()
    {
        if (_datetime is not { } datetime)
        {
            return;
        }

        var values = datetime.Value?.Values ?? [];
        List<DatetimeParts>? parsed = values.Count > 0 ? DatetimeUtils.ParseDates(values, datetime) : [datetime.GetDefaultPart()];
        if (parsed is null)
        {
            return;
        }

        // multiple with a presentation other than date: the first, so that the button doesn't look broken.
        var first = parsed[0];
        var culture = datetime.Culture;
        var options = datetime.FormatOptions;
        var hourCycle = DatetimeIntl.GetHourCycle(culture, datetime.HourCycle);
        string? dateText = null, timeText = null;
        switch (_presentation)
        {
            case DatetimePresentation.DateTime:
            case DatetimePresentation.TimeDate:
                var date = DatetimeIntl.LocalizedDateTime(culture, first, options?.Date ?? DateFormat);
                var time = DatetimeIntl.LocalizedTime(culture, first, hourCycle, options?.Time);
                if (datetime.PreferWheel)
                {
                    dateText = $"{date} {time}";
                }
                else
                {
                    dateText = date;
                    timeText = time;
                }

                break;
            case DatetimePresentation.Date:
                if (datetime.Multiple && values.Count != 1)
                {
                    dateText = $"{values.Count} days";
                    if (datetime.TitleSelectedDatesFormatter is { } formatter)
                    {
                        try
                        {
                            dateText = formatter(values);
                        }
                        catch (Exception e)
                        {
                            LogError($"[ion-datetime-button] - Exception in provided `titleSelectedDatesFormatter`: {e}");
                        }
                    }
                }
                else
                {
                    dateText = DatetimeIntl.LocalizedDateTime(culture, first, options?.Date ?? DateFormat);
                }

                break;
            case DatetimePresentation.Time:
                timeText = DatetimeIntl.LocalizedTime(culture, first, hourCycle, options?.Time);
                break;
            case DatetimePresentation.MonthYear:
                dateText = DatetimeIntl.LocalizedDateTime(culture, first, options?.Date ?? MonthYearFormat);
                break;
            // Ionic reads formatOptions.time for these two, a copy of the line above; the datetime formats them by its date
            // options, and so does this (ADR 0005, docs/infra/ionic-issues.md).
            case DatetimePresentation.Month:
                dateText = DatetimeIntl.LocalizedDateTime(culture, first, options?.Date ?? MonthFormat);
                break;
            case DatetimePresentation.Year:
                dateText = DatetimeIntl.LocalizedDateTime(culture, first, options?.Date ?? YearFormat);
                break;
        }

        _dateText = dateText;
        _timeText = timeText;
        UpdateButtons();
    }

    /// <summary>The date's default: <c>{ month: 'short', day: 'numeric', year: 'numeric' }</c>, <c>Jun 15, 2023</c>.</summary>
    private static readonly DateTimeFormatOptions DateFormat = new()
    {
        Month = DateTimeFormatMonth.Short, Day = DateTimeFormatNumber.Numeric, Year = DateTimeFormatNumber.Numeric,
    };

    private static readonly DateTimeFormatOptions MonthYearFormat = new() { Month = DateTimeFormatMonth.Long, Year = DateTimeFormatNumber.Numeric };

    private static readonly DateTimeFormatOptions MonthFormat = new() { Month = DateTimeFormatMonth.Long };

    private static readonly DateTimeFormatOptions YearFormat = new() { Year = DateTimeFormatNumber.Numeric };

    // ── The buttons ──────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_dateButton is not null)
        {
            _dateButton.Clicked -= OnDateClick;
        }

        if (_timeButton is not null)
        {
            _timeButton.Clicked -= OnTimeClick;
        }

        _dateButton = e.NameScope.Find<DatetimeButtonNative>("PART_DateButton");
        _timeButton = e.NameScope.Find<DatetimeButtonNative>("PART_TimeButton");
        if (_dateButton is not null)
        {
            _dateButton.Clicked += OnDateClick;
        }

        if (_timeButton is not null)
        {
            _timeButton.Clicked += OnTimeClick;
        }

        UpdateButtons();
    }

    /// <summary>
    /// Ionic's <c>render()</c>: a button for each text that isn't empty, with <c>aria-expanded</c>, and the host's
    /// <c>${selectedButton}-active</c> while the datetime shows.
    /// </summary>
    private void UpdateButtons()
    {
        Update(_dateButton, _dateText);
        Update(_timeButton, _timeText);
        PseudoClasses.Set(":date-active", _active && _selected == Selected.Date);
        PseudoClasses.Set(":time-active", _active && _selected == Selected.Time);

        void Update(DatetimeButtonNative? button, string? text)
        {
            if (button is null)
            {
                return;
            }

            button.Text = text;
            button.IsVisible = !string.IsNullOrEmpty(text);
            button.IsExpanded = _active;
        }
    }

    /// <summary>Ionic's <c>handleDateClick</c>: a date picker only, then the overlay.</summary>
    private void OnDateClick(object? sender, Point? at)
    {
        if (_datetime is not { } datetime)
        {
            return;
        }

        var needsPresentationChange = false;
        if (_presentation is DatetimePresentation.DateTime or DatetimePresentation.TimeDate
            && !datetime.PreferWheel && datetime.Presentation != DatetimePresentation.Date)
        {
            // The date+time wheel shows both together, so it keeps its presentation.
            datetime.SetCurrentValue(AvonicDatetime.PresentationProperty, DatetimePresentation.Date);
            // A binding or a style that puts it back: nothing to wait for.
            needsPresentationChange = datetime.Presentation == DatetimePresentation.Date;
        }

        Select(Selected.Date);
        PresentOverlay(at, needsPresentationChange, _dateButton);
    }

    /// <summary>Ionic's <c>handleTimeClick</c>: a time picker only, then the overlay.</summary>
    private void OnTimeClick(object? sender, Point? at)
    {
        if (_datetime is not { } datetime)
        {
            return;
        }

        var needsPresentationChange = false;
        if (_presentation is DatetimePresentation.DateTime or DatetimePresentation.TimeDate
            && datetime.Presentation != DatetimePresentation.Time)
        {
            datetime.SetCurrentValue(AvonicDatetime.PresentationProperty, DatetimePresentation.Time);
            needsPresentationChange = datetime.Presentation == DatetimePresentation.Time;
        }

        Select(Selected.Time);
        PresentOverlay(at, needsPresentationChange, _timeButton);
    }

    private void Select(Selected selected)
    {
        _selected = selected;
        UpdateButtons();
    }

    /// <summary>
    /// Ionic's <c>presentOverlay</c>: a popover is placed by the button tapped, its <c>ionShadowTarget</c>, once the datetime has
    /// drawn its new presentation (<c>ionRender</c>); a modal is presented as it is. With no overlay, nothing.
    /// </summary>
    private void PresentOverlay(Point? at, bool needsPresentationChange, DatetimeButtonNative? trigger)
    {
        switch (_overlay)
        {
            case AvonicPopover popover:
                var ev = new PopoverEvent(trigger, at);
                if (needsPresentationChange && _datetime is { } datetime)
                {
                    CancelPendingPresent();
                    void OnRendered(object? sender, EventArgs args)
                    {
                        CancelPendingPresent();
                        LogFault(popover.Present(ev));
                    }

                    datetime.Rendered += OnRendered;
                    _pendingPresent = () => datetime.Rendered -= OnRendered;
                }
                else
                {
                    LogFault(popover.Present(ev));
                }

                break;
            case { } overlay:
                LogFault(overlay.Present());
                break;
        }
    }

    /// <summary>A present still waiting for the datetime's <c>Rendered</c> doesn't happen: the button has gone, or another tap came.</summary>
    private void CancelPendingPresent()
    {
        _pendingPresent?.Invoke();
        _pendingPresent = null;
    }

    private void LogFault(Task task) =>
        task.ContinueWith(
            t => Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "The overlay's present failed: {Exception}", t.Exception!.InnerException),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

    // ── Active: the datetime is shown ────────────────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _layoutRoot = TopLevel.GetTopLevel(this);
        UpdateWatch();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _layoutRoot = null;
        // Not in a window, nothing is shown: collapsed, as it was before it was attached.
        UpdateWatch();
    }

    private void OnDatetimeVisualChanged(object? sender, VisualTreeAttachmentEventArgs e) => UpdateWatch();

    /// <summary>
    /// Each layout pass of the window (a scroll, a present, a dismiss, something hidden) is a check, as the observer looks at each
    /// frame, while the button and the datetime are both in a window: a datetime out of one (an overlay's unmounted content) is
    /// never shown, and costs nothing.
    /// </summary>
    private void UpdateWatch()
    {
        var watch = _layoutRoot is not null && _datetime is not null && TopLevel.GetTopLevel(_datetime) is not null;
        if (watch != _watching)
        {
            _watching = watch;
            if (watch)
            {
                LayoutUpdated += OnLayoutUpdated;
            }
            else
            {
                LayoutUpdated -= OnLayoutUpdated;
            }
        }

        CheckActive();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) => CheckActive();

    /// <summary>
    /// The <c>IntersectionObserver</c>'s <c>isIntersecting</c> at its threshold of 0.01: the datetime in this window, shown, and at
    /// least 1% of its area inside the window and every ancestor that clips it.
    /// </summary>
    private void CheckActive()
    {
        if (_datetime is null)
        {
            return;
        }

        var active = _attached && _watching && _layoutRoot is not null && IsShown(_datetime, _layoutRoot);
        if (active == _active)
        {
            return;
        }

        _active = active;
        UpdateButtons();
    }

    /// <summary>At least 1% of <paramref name="element"/>'s box is inside <paramref name="root"/> and its clipping ancestors.</summary>
    internal static bool IsShown(Visual element, TopLevel root)
    {
        if (!element.IsEffectivelyVisible || TopLevel.GetTopLevel(element) != root
            || element.TransformToVisual(root) is not { } toRoot)
        {
            return false;
        }

        var box = new Rect(element.Bounds.Size).TransformToAABB(toRoot);
        var visible = box.Intersect(new Rect(root.Bounds.Size));
        for (var ancestor = element.GetVisualParent(); ancestor is not null && ancestor != root; ancestor = ancestor.GetVisualParent())
        {
            if (ancestor.ClipToBounds && ancestor.TransformToVisual(root) is { } clipToRoot)
            {
                visible = visible.Intersect(new Rect(ancestor.Bounds.Size).TransformToAABB(clipToRoot));
            }
        }

        // A box of no area: not laid out yet, or nothing to see. Chromium has no such moment, as its layout is synchronous; the
        // next layout pass looks again.
        var area = box.Width * box.Height;
        return area > 0 && visible.Width * visible.Height / area >= ThresholdRatio;
    }

    /// <summary>Ionic's <c>threshold: 0.01</c>.</summary>
    internal const double ThresholdRatio = 0.01;
}
