using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Logging;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-accordion</c>: a header that opens and closes the content below it. Whether it's open is up to the
/// <see cref="AvonicAccordionGroup"/> it's in: a tap on the header asks the group, and the group's value says which are open.
/// </summary>
[TemplatePart("PART_Header", typeof(ContentPresenter))]
[TemplatePart("PART_Content", typeof(AccordionContentClip))]
[TemplatePart("PART_ContentWrapper", typeof(ContentPresenter))]
[PseudoClasses(
    ":accordion-expanding", ":accordion-expanded", ":accordion-collapsing", ":accordion-collapsed", ":expanded",
    ":accordion-next", ":accordion-previous", ":accordion-animated", ":accordion-group-expand-inset",
    ":first-in-group", ":last-in-group")]
public class AvonicAccordion : TemplatedControl
{
    /// <summary><c>transition: max-height 300ms</c> on <c>#content</c>.</summary>
    internal static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(300);

    /// <summary><c>transitionEndAsync(contentEl, 2000)</c>: how long it waits for a transition that never starts.</summary>
    internal static readonly TimeSpan TransitionEndTimeout = TimeSpan.FromMilliseconds(2000);

    /// <summary><c>cubic-bezier(0.25, 0.8, 0.5, 1)</c>.</summary>
    internal static readonly CubicBezierEasing TransitionEasing = new(0.25, 0.8, 0.5, 1);

    private static int s_accordionIds;

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>value</c>. Each accordion has its own by default, <c>ion-accordion-N</c>.</summary>
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<AvonicAccordion, string?>(nameof(Value));

    /// <summary>Ionic <c>readonly</c>: it can't be toggled, and doesn't look any different.</summary>
    public static readonly StyledProperty<bool> ReadonlyProperty =
        AvaloniaProperty.Register<AvonicAccordion, bool>(nameof(Readonly));

    /// <summary>Ionic <c>toggleIcon</c>: a name or a source, as <see cref="AvonicIcon.Icon"/> takes. Read when it first loads.</summary>
    public static readonly StyledProperty<string> ToggleIconProperty =
        AvaloniaProperty.Register<AvonicAccordion, string>(nameof(ToggleIcon), "chevron-down");

    /// <summary>Ionic <c>toggleIconSlot</c>: which slot of the header's item the icon goes in. Read when it first loads.</summary>
    public static readonly StyledProperty<AccordionToggleIconSlot> ToggleIconSlotProperty =
        AvaloniaProperty.Register<AvonicAccordion, AccordionToggleIconSlot>(nameof(ToggleIconSlot), AccordionToggleIconSlot.End);

    /// <summary>Ionic's <c>header</c> slot.</summary>
    public static readonly StyledProperty<object?> HeaderContentProperty =
        AvaloniaProperty.Register<AvonicAccordion, object?>(nameof(HeaderContent));

    public static readonly StyledProperty<IDataTemplate?> HeaderContentTemplateProperty =
        AvaloniaProperty.Register<AvonicAccordion, IDataTemplate?>(nameof(HeaderContentTemplate));

    /// <summary>Ionic's <c>content</c> slot.</summary>
    public static readonly StyledProperty<object?> ContentContentProperty =
        AvaloniaProperty.Register<AvonicAccordion, object?>(nameof(ContentContent));

    public static readonly StyledProperty<IDataTemplate?> ContentContentTemplateProperty =
        AvaloniaProperty.Register<AvonicAccordion, IDataTemplate?>(nameof(ContentContentTemplate));

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool Readonly
    {
        get => GetValue(ReadonlyProperty);
        set => SetValue(ReadonlyProperty, value);
    }

    public string ToggleIcon
    {
        get => GetValue(ToggleIconProperty);
        set => SetValue(ToggleIconProperty, value);
    }

    public AccordionToggleIconSlot ToggleIconSlot
    {
        get => GetValue(ToggleIconSlotProperty);
        set => SetValue(ToggleIconSlotProperty, value);
    }

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public IDataTemplate? HeaderContentTemplate
    {
        get => GetValue(HeaderContentTemplateProperty);
        set => SetValue(HeaderContentTemplateProperty, value);
    }

    public object? ContentContent
    {
        get => GetValue(ContentContentProperty);
        set => SetValue(ContentContentProperty, value);
    }

    public IDataTemplate? ContentContentTemplate
    {
        get => GetValue(ContentContentTemplateProperty);
        set => SetValue(ContentContentTemplateProperty, value);
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic's <c>AccordionState</c>.</summary>
    internal enum AccordionState
    {
        Collapsed = 1,
        Collapsing = 2,
        Expanded = 4,
        Expanding = 8,
    }

    private AccordionState _state = AccordionState.Collapsed;
    private bool _isNext;
    private bool _isPrevious;
    private bool _hasInteracted;
    private bool _hasEverBeenExpanded;
    private bool _hasReceivedFirstUpdate;
    private bool _didLoad;

    private ContentPresenter? _header;
    private AccordionContentClip? _content;
    private NativeButton? _headerButton;
    private IDisposable? _headerNoHit;
    private IDisposable? _contentNoHit;
    private IDisposable? _contentHidden;

    /// <summary>A release is on its way through the header, and whether something inside has clicked during it.</summary>
    private bool _releasing;
    private bool _releaseClicked;

    /// <summary>The next frame, or the timeout, that the expand or collapse waits on: Ionic's <c>currentRaf</c>.</summary>
    private IDisposable? _pending;

    /// <summary>The running transition of <c>max-height</c>, if any.</summary>
    private bool _transitioning;
    private double _from;
    private double _to;
    private TimeSpan _start;
    private Action? _onTransitionEnd;

    public AvonicAccordion()
    {
        SetValue(ValueProperty, $"ion-accordion-{s_accordionIds++}");
        Render();
    }

    /// <summary>The group it's in: <c>closest('ion-accordion-group')</c>.</summary>
    internal AvonicAccordionGroup? Group { get; private set; }

    internal AccordionState State => _state;

    private bool IsExpandedState => _state is AccordionState.Expanded or AccordionState.Expanding;

    /// <summary>Its own <c>readonly</c>, or its group's, which Ionic copies to the group's own accordions.</summary>
    private bool IsReadonly => Readonly || OwnGroup is { Readonly: true };

    /// <summary>The group it's a child of, as <c>:scope &gt; ion-accordion</c> is, whether it's in its items or made from its template.</summary>
    private AvonicAccordionGroup? OwnGroup => AvonicAccordionGroup.OwnerOf(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            UpdateState();
        }
        else if (change.Property == ReadonlyProperty)
        {
            UpdateReadonly();
        }
    }

    // ── The group ────────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        Group = this.GetLogicalAncestors().OfType<AvonicAccordionGroup>().FirstOrDefault();
        if (Group is { } group)
        {
            UpdateState();
            group.ValueChange += OnGroupValueChange;
        }

        OnGroupChanged();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        if (Group is { } group)
        {
            group.ValueChange -= OnGroupValueChange;
        }

        Group = null;

        // A transition cut short ends where it was going, so that nothing is left half open with no group to finish it.
        CancelPending();
        StopTransition();
        _state = _state switch
        {
            AccordionState.Expanding => AccordionState.Expanded,
            AccordionState.Collapsing => AccordionState.Collapsed,
            _ => _state,
        };
        _content?.ClearValue(MaxHeightProperty);
        Render();
    }

    /// <summary>
    /// The group's <c>expand</c> or <c>readonly</c> changed, its items did, or it's a new group. <c>:first-in-group</c> and
    /// <c>:last-in-group</c> are <c>ion-accordion-group &gt; ion-accordion:first-of-type</c> and <c>:last-of-type</c>.
    /// </summary>
    internal void OnGroupChanged()
    {
        var group = OwnGroup;
        PseudoClasses.Set(":accordion-group-expand-inset", group is { Expand: AccordionGroupExpand.Inset });
        var accordions = group?.Accordions().ToList();
        PseudoClasses.Set(":first-in-group", accordions is { Count: > 0 } && accordions[0] == this);
        PseudoClasses.Set(":last-in-group", accordions is { Count: > 0 } && accordions[^1] == this);
        UpdateReadonly();
    }

    /// <summary><c>accordionGroupUpdateHandler</c>: only a change after the first defined value counts as an interaction.</summary>
    private void OnGroupValueChange()
    {
        if (Group is not { } group)
        {
            return;
        }

        var value = group.Value;
        var shouldExpand = AvonicAccordionGroup.Includes(value, Value);
        if (_hasReceivedFirstUpdate && shouldExpand != IsExpandedState)
        {
            _hasInteracted = true;
        }

        if (value is not null)
        {
            _hasReceivedFirstUpdate = true;
        }

        UpdateState();
    }

    /// <summary><c>updateState</c>.</summary>
    private void UpdateState()
    {
        if (Group is not { } group)
        {
            return;
        }

        var value = group.Value;
        if (AvonicAccordionGroup.Includes(value, Value))
        {
            ExpandAccordion();
            _isNext = _isPrevious = false;
        }
        else
        {
            CollapseAccordion();

            // When inset, a closed accordion beside an open one rounds the corners on that side.
            if (Sibling(1) is { } next)
            {
                _isPrevious = AvonicAccordionGroup.Includes(value, next.Value);
            }

            if (Sibling(-1) is { } previous)
            {
                _isNext = AvonicAccordionGroup.Includes(value, previous.Value);
            }
        }

        Render();
    }

    /// <summary><c>nextElementSibling</c> or <c>previousElementSibling</c>, if it's an accordion.</summary>
    private AvonicAccordion? Sibling(int offset)
    {
        System.Collections.IList? siblings = OwnGroup is { } group ? group.Elements()
            : Parent switch
            {
                ItemsControl items => items.Items,
                Panel panel => panel.Children,
                _ => null,
            };

        if (siblings is null)
        {
            return null;
        }

        var own = siblings.IndexOf(this);
        var index = own + offset;
        return own >= 0 && index >= 0 && index < siblings.Count ? siblings[index] as AvonicAccordion : null;
    }

    // ── Expanding and collapsing ─────────────────────────────────────────────────────────────

    /// <summary><c>shouldAnimate</c>. There's no <c>prefers-reduced-motion</c> to read.</summary>
    private bool ShouldAnimate() =>
        _hasInteracted && _hasEverBeenExpanded && AvonicConfig.Animated && Group is not { Animated: false };

    /// <summary><c>expandAccordion</c>.</summary>
    private void ExpandAccordion()
    {
        if (_content is not { } content)
        {
            _state = AccordionState.Expanded;
            _hasEverBeenExpanded = true;
            return;
        }

        if (_state == AccordionState.Expanded)
        {
            // A collapse that hasn't started yet. Ionic lets it run, and closes an accordion its group has open.
            if (_pending is not null)
            {
                CancelPending();
                content.ClearValue(MaxHeightProperty);
            }

            return;
        }

        CancelPending();
        _hasEverBeenExpanded = true;
        if (ShouldAnimate())
        {
            var from = ShownHeight();
            StopTransition();
            _pending = AvonicConfig.RequestFrame(this, () =>
            {
                _state = AccordionState.Expanding;
                content.MaxHeight = from;
                Render();
                _pending = AvonicConfig.RequestFrame(this, () =>
                {
                    _pending = null;
                    var to = ContentHeight();
                    StartTransition(from, to, () =>
                    {
                        _state = AccordionState.Expanded;
                        Render();
                    });
                });
            });
        }
        else
        {
            StopTransition();
            content.ClearValue(MaxHeightProperty);
            _state = AccordionState.Expanded;
        }
    }

    /// <summary><c>collapseAccordion</c>.</summary>
    private void CollapseAccordion()
    {
        if (_content is not { } content)
        {
            _state = AccordionState.Collapsed;
            return;
        }

        if (_state == AccordionState.Collapsed)
        {
            // An expand that hasn't started yet. Ionic lets it run, and opens an accordion its group has shut.
            CancelPending();
            return;
        }

        CancelPending();
        if (ShouldAnimate())
        {
            _pending = AvonicConfig.RequestFrame(this, () =>
            {
                // offsetHeight, a frame on: a transition that was running has gone on until now, as Chromium's does.
                var from = _transitioning ? HeightAt(AvonicConfig.Now()) : ContentShownHeight();
                StopTransition();
                content.MaxHeight = from;
                _pending = AvonicConfig.RequestFrame(this, () =>
                {
                    _pending = null;
                    _state = AccordionState.Collapsing;
                    Render();
                    StartTransition(from, 0, () =>
                    {
                        _state = AccordionState.Collapsed;
                        content.ClearValue(MaxHeightProperty);
                        Render();
                    });
                });
            });
        }
        else
        {
            StopTransition();
            content.ClearValue(MaxHeightProperty);
            _state = AccordionState.Collapsed;
        }
    }

    /// <summary>The height the content shows now: the transition's, or 0 if it's hidden or held shut.</summary>
    private double ShownHeight() =>
        _transitioning ? HeightAt(AvonicConfig.Now())
        : _content is null || _state == AccordionState.Collapsed ? 0
        : double.IsFinite(_content.MaxHeight) ? _content.MaxHeight
        : 0;

    /// <summary><c>contentElWrapper.offsetHeight</c>, which lays the page out first if it has to.</summary>
    private double ContentHeight()
    {
        _content?.UpdateLayout();
        return _content?.NaturalHeight ?? 0;
    }

    /// <summary><c>contentEl.offsetHeight</c>.</summary>
    private double ContentShownHeight()
    {
        _content?.UpdateLayout();
        return _content?.Bounds.Height ?? 0;
    }

    private double HeightAt(TimeSpan now)
    {
        var progress = Math.Clamp((now - _start).Ticks / (double)TransitionDuration.Ticks, 0, 1);
        return progress >= 1 ? _to : _from + (_to - _from) * TransitionEasing.Ease(progress);
    }

    /// <summary>
    /// <c>max-height</c> eased from one height to another, then <paramref name="onEnd"/>, at the first frame after the end, as
    /// <c>transitionend</c> comes. With nothing to ease there's no transition, so <c>transitionEndAsync</c> waits out its timeout.
    /// </summary>
    private void StartTransition(double from, double to, Action onEnd)
    {
        _onTransitionEnd = onEnd;
        if (from == to || _content is null)
        {
            _pending = AvonicConfig.Schedule(EndTransition, TransitionEndTimeout);
            return;
        }

        _transitioning = true;
        _from = from;
        _to = to;
        _start = AvonicConfig.Now();
        _content.MaxHeight = from;
        _pending = AvonicConfig.RequestFrame(this, OnFrame);
    }

    private void OnFrame()
    {
        _pending = null;
        if (!_transitioning || _content is null)
        {
            return;
        }

        var now = AvonicConfig.Now();
        _content.MaxHeight = HeightAt(now);
        if (now - _start >= TransitionDuration)
        {
            EndTransition();
        }
        else
        {
            _pending = AvonicConfig.RequestFrame(this, OnFrame);
        }
    }

    private void EndTransition()
    {
        _pending = null;
        _transitioning = false;
        var onEnd = _onTransitionEnd;
        _onTransitionEnd = null;
        onEnd?.Invoke();
    }

    private void StopTransition()
    {
        _transitioning = false;
        _onTransitionEnd = null;
    }

    private void CancelPending()
    {
        _pending?.Dispose();
        _pending = null;
    }

    /// <summary>
    /// What Ionic's <c>render</c> gives the host: its classes. The animation's turns first, so that the theme's transitions are
    /// there before the values they ease change.
    /// </summary>
    private void Render()
    {
        PseudoClasses.Set(":accordion-animated", ShouldAnimate());
        PseudoClasses.Set(":accordion-expanding", _state == AccordionState.Expanding);
        PseudoClasses.Set(":accordion-expanded", _state == AccordionState.Expanded);
        PseudoClasses.Set(":accordion-collapsing", _state == AccordionState.Collapsing);
        PseudoClasses.Set(":accordion-collapsed", _state == AccordionState.Collapsed);
        PseudoClasses.Set(":expanded", IsExpandedState);
        PseudoClasses.Set(":accordion-next", _isNext);
        PseudoClasses.Set(":accordion-previous", _isPrevious);

        // :host(.accordion-collapsed) #content { display: none }
        SetStyleValue(ref _contentHidden, _content, IsVisibleProperty, _state == AccordionState.Collapsed);

        if (_content is not null && _state == AccordionState.Expanded && !_transitioning && _pending is null)
        {
            _content.ClearValue(MaxHeightProperty);
        }

        _peer?.RaiseExpandCollapseChanged();
    }

    // ── Template ─────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_header is not null)
        {
            RemoveHeaderHandlers(_header);
        }

        _headerNoHit?.Dispose();
        _headerNoHit = null;
        _contentNoHit?.Dispose();
        _contentNoHit = null;
        _contentHidden?.Dispose();
        _contentHidden = null;

        _header = e.NameScope.Find<ContentPresenter>("PART_Header");
        _content = e.NameScope.Find<AccordionContentClip>("PART_Content");

        if (_header is not null)
        {
            // #header's onClick: a press on anything in the header clicks it, as a <div> does, with no keys of its own.
            _headerButton = new NativeButton(_header, _ => OnHeaderPressClick(), () => false, () => { });
            // Around NativeButton's own release handler: one release is one click, however many controls it passes on the way up.
            _header.AddHandler(PointerReleasedEvent, OnHeaderReleaseStarts, RoutingStrategies.Tunnel, handledEventsToo: true);
            _header.AddHandler(PointerReleasedEvent, OnHeaderReleaseEnds, RoutingStrategies.Bubble, handledEventsToo: true);
            foreach (var click in InnerClicks)
            {
                _header.AddHandler(click, OnInnerClick);
            }
        }

        UpdateReadonly();
        Render();
    }

    /// <summary>The clicks a control inside can raise, from a press or from a key: what bubbles to <c>#header</c> as <c>click</c>.</summary>
    private static readonly RoutedEvent<RoutedEventArgs>[] InnerClicks =
    [
        AvonicItem.ClickEvent,
        AvonicButton.ClickEvent,
        AvonicChip.ClickEvent,
        AvonicCard.ClickEvent,
        Avalonia.Controls.Button.ClickEvent,
    ];

    private void RemoveHeaderHandlers(ContentPresenter header)
    {
        header.RemoveHandler(PointerReleasedEvent, OnHeaderReleaseStarts);
        header.RemoveHandler(PointerReleasedEvent, OnHeaderReleaseEnds);
        foreach (var click in InnerClicks)
        {
            header.RemoveHandler(click, OnInnerClick);
        }

        _headerButton?.Reset();
        _headerButton = null;
    }

    private void UpdateReadonly()
    {
        // :host(.accordion-readonly) #header, #content { pointer-events: none }
        var isReadonly = IsReadonly;
        SetStyleValue(ref _headerNoHit, _header, IsHitTestVisibleProperty, isReadonly);
        SetStyleValue(ref _contentNoHit, _content, IsHitTestVisibleProperty, isReadonly);
    }

    /// <summary>
    /// Behaviour, so it doesn't depend on the theme, which an app can replace. At style priority, as CSS is, so a theme's own
    /// value on the part wins.
    /// </summary>
    private static void SetStyleValue(ref IDisposable? frame, Control? part, StyledProperty<bool> property, bool active)
    {
        if (active && frame is null && part is not null)
        {
            frame = part.SetValue(property, false, BindingPriority.Style);
        }
        else if (!active)
        {
            frame?.Dispose();
            frame = null;
        }
    }

    // ── The header's click ───────────────────────────────────────────────────────────────────

    private void OnHeaderReleaseStarts(object? sender, PointerReleasedEventArgs e)
    {
        _releasing = true;
        _releaseClicked = false;
    }

    private void OnHeaderReleaseEnds(object? sender, PointerReleasedEventArgs e) => _releasing = false;

    /// <summary>
    /// A control inside clicked, from a release, a key or automation. A release clicks once, however many controls it passes
    /// through on the way up, and the header's own click doesn't come again after it.
    /// </summary>
    private void OnInnerClick(object? sender, RoutedEventArgs e)
    {
        if (_releasing)
        {
            if (_releaseClicked)
            {
                return;
            }

            _releaseClicked = true;
        }

        ToggleExpanded();
    }

    private void OnHeaderPressClick()
    {
        if (!_releaseClicked)
        {
            ToggleExpanded();
        }
    }

    /// <summary><c>toggleExpanded</c>: it only asks the group, which decides.</summary>
    internal void ToggleExpanded()
    {
        if (!IsEffectivelyEnabled || IsReadonly)
        {
            return;
        }

        _hasInteracted = true;
        if (Group is { } group)
        {
            var expand = _state is AccordionState.Collapsed or AccordionState.Collapsing;
            group.RequestAccordionToggle(Value, expand);
        }

        Render();
    }

    /// <summary>Whether a visual is in the header slot: <c>closest('ion-accordion [slot="header"]')</c>.</summary>
    internal bool IsInHeader(Visual visual) => _header is not null && (_header == visual || _header.IsVisualAncestorOf(visual));

    /// <summary>
    /// <c>focus()</c>, with <c>delegatesFocus</c>: the first control in it that can take focus, the header's first. False if
    /// there's none, as in a disabled accordion here, where <see cref="InputElement.IsEnabled"/> reaches the item.
    /// </summary>
    internal bool FocusFirst() =>
        this.GetVisualDescendants()
            .OfType<InputElement>()
            .FirstOrDefault(element => element.Focusable && element.IsEffectivelyEnabled && element.IsEffectivelyVisible)
            ?.Focus(NavigationMethod.Directional) == true;

    // ── When it first loads ──────────────────────────────────────────────────────────────────

    /// <summary><c>componentDidLoad</c>: once in its life, as Ionic's.</summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (_didLoad)
        {
            return;
        }

        _didLoad = true;
        SetItemDefaults();
        SlotToggleIcon();
    }

    /// <summary>
    /// The <see cref="AvonicItem"/> in the header slot: <c>getSlottedHeaderIonItem</c>, which looks at what's assigned to the slot
    /// and not inside it, so an item inside a panel there isn't it.
    /// </summary>
    internal AvonicItem? HeaderItem() => HeaderContent as AvonicItem;

    /// <summary>
    /// <c>setItemDefaults</c>: a button, for the keyboard, with no detail icon, and full lines unless it has its own. Ionic assigns
    /// them, so they beat what the app set before; a binding stays.
    /// </summary>
    private void SetItemDefaults()
    {
        if (HeaderItem() is not { } item)
        {
            return;
        }

        item.SetCurrentValue(AvonicItem.ButtonProperty, true);
        item.SetCurrentValue(AvonicItem.DetailProperty, false);
        if (item.Lines is null)
        {
            item.SetCurrentValue(AvonicItem.LinesProperty, ItemLines.Full);
        }
    }

    /// <summary>
    /// <c>slotToggleIcon</c>: an icon at the end of the item's slot, unless it already has one with the class. A slot holds one
    /// child here, so a single control there goes into a horizontal <see cref="StackPanel"/> with the icon after it.
    /// </summary>
    private void SlotToggleIcon()
    {
        if (HeaderItem() is not { } item || HasToggleIcon(item))
        {
            return;
        }

        var icon = new AvonicIcon { Icon = ToggleIcon };
        icon.Classes.Add("ion-accordion-toggle-icon");
        AutomationProperties.SetAccessibilityView(icon, AccessibilityView.Raw);

        var slot = ToggleIconSlot == AccordionToggleIconSlot.Start ? AvonicItem.StartContentProperty : AvonicItem.EndContentProperty;
        switch (item.GetValue(slot))
        {
            case null:
                item.SetCurrentValue(slot, icon);
                break;
            case Panel panel:
                panel.Children.Add(icon);
                break;
            case Control existing:
                item.SetCurrentValue(slot, null);
                if (existing.Parent is not null)
                {
                    WarnNoRoom(item);
                    item.SetCurrentValue(slot, existing);
                    break;
                }

                item.SetCurrentValue(slot, new StackPanel { Orientation = Orientation.Horizontal, Children = { existing, icon } });
                break;
            default:
                WarnNoRoom(item);
                break;
        }
    }

    private void WarnNoRoom(AvonicItem item) =>
        Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
            this, "The accordion's toggle icon has no room in {Item}'s slot, which holds content that isn't a control. Put a panel there.", item);

    /// <summary><c>ionItem.querySelector('.ion-accordion-toggle-icon')</c>: anywhere in what the item holds.</summary>
    private static bool HasToggleIcon(AvonicItem item) =>
        new[] { item.StartContent, item.Content, item.EndContent }
            .OfType<Control>()
            .SelectMany(control => control.GetSelfAndLogicalDescendants())
            .OfType<Control>()
            .Any(control => control.Classes.Contains("ion-accordion-toggle-icon"));

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    private AccordionAutomationPeer? _peer;

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new AccordionAutomationPeer(this);

    /// <summary>
    /// <c>aria-expanded</c>, which Ionic puts on the header item's button: the accordion's own peer says it here, as the item's
    /// peer is the item's. Expanding or collapsing it asks the group, as a tap does.
    /// </summary>
    private sealed class AccordionAutomationPeer(AvonicAccordion owner) : ControlAutomationPeer(owner), IExpandCollapseProvider
    {
        private ExpandCollapseState _last = ExpandCollapseState.Collapsed;

        public ExpandCollapseState ExpandCollapseState =>
            owner.IsExpandedState ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        public bool ShowsMenu => false;

        public void Expand()
        {
            if (!owner.IsExpandedState)
            {
                owner.ToggleExpanded();
            }
        }

        public void Collapse()
        {
            if (owner.IsExpandedState)
            {
                owner.ToggleExpanded();
            }
        }

        public void RaiseExpandCollapseChanged()
        {
            var state = ExpandCollapseState;
            if (state != _last)
            {
                RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, _last, state);
                _last = state;
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override bool IsControlElementCore() => true;
    }
}
