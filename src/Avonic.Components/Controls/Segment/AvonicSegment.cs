using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Gestures;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-segment</c>: a row of <see cref="AvonicSegmentButton"/>s, one of them checked: the one whose value is
/// <see cref="Value"/>. A tap, a drag of the indicator, or the keys change it and raise <see cref="IonChange"/>, and the indicator
/// slides from the last one. With buttons that have a <c>ContentId</c>, it pages an <see cref="AvonicSegmentView"/>, and follows
/// it. See <c>docs/components/ion-segment/README.md</c>.
/// </summary>
/// <remarks>
/// Its children are <see cref="ItemsControl.Items"/>, as <c>core.json</c> lists no slot. It finds its buttons among its logical
/// descendants, in tree order, as <c>querySelectorAll('ion-segment-button')</c> does.
/// </remarks>
[TemplatePart("PART_Scroll", typeof(ScrollViewer))]
[PseudoClasses(":in-toolbar", ":in-toolbar-color", ":scrollable", ":segment-activated")]
public class AvonicSegment : ItemsControl, IItemControl
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>color</c>. Its buttons show it, through <c>:in-segment-color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicSegment>();

    /// <summary>Ionic <c>scrollable</c>: the buttons at their content's width, scrolling sideways, and no drag gesture.</summary>
    public static readonly StyledProperty<bool> ScrollableProperty =
        AvaloniaProperty.Register<AvonicSegment, bool>(nameof(Scrollable));

    /// <summary>Ionic <c>selectOnFocus</c>: the arrows, Home and End check the button they focus.</summary>
    public static readonly StyledProperty<bool> SelectOnFocusProperty =
        AvaloniaProperty.Register<AvonicSegment, bool>(nameof(SelectOnFocus));

    /// <summary>Ionic <c>swipeGesture</c>: a drag moves the indicator between the buttons. True by default.</summary>
    public static readonly StyledProperty<bool> SegmentSwipeGestureProperty =
        AvaloniaProperty.Register<AvonicSegment, bool>(nameof(SegmentSwipeGesture), true);

    /// <summary>Ionic <c>value</c>. Null is <c>undefined</c>. Two-way by default, as the user changes it.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicSegment, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic <c>ionChange</c>: the user changed the value, and any dragging pointer has been released. Not raised when
    /// <see cref="Value"/> is set in code.
    /// </summary>
    public static readonly RoutedEvent<SegmentChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicSegment, SegmentChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    public event EventHandler<SegmentChangeEventArgs>? IonChange
    {
        add => AddHandler(IonChangeEvent, value);
        remove => RemoveHandler(IonChangeEvent, value);
    }

    /// <summary>Ionic's internal <c>ionSelect</c>, raised on every change of the value, which each button listens to.</summary>
    internal event Action? Select;

    /// <summary>Ionic's <c>ionStyle</c> as the buttons hear it on the segment: render again.</summary>
    internal event Action? StyleChanged;

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private ScrollViewer? _scroll;
    private SmoothScroll? _smooth;
    private Gesture? _gesture;
    private AvonicSegmentView? _segmentView;
    private TopLevel? _topLevel;
    private bool _loaded;
    private bool _didLoad;
    private bool _activated;
    private object? _valueBeforeGesture;
    private int? _lastNextIndex;
    private bool? _triggerScrollOnValueChange;

    static AvonicSegment()
    {
        // Every button is a tab stop, as each native <button> is.
        KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<AvonicSegment>(KeyboardNavigationMode.Continue);

        ValueProperty.Changed.AddClassHandler<AvonicSegment>((segment, e) => segment.ValueChanged(e.NewValue, e.OldValue));
        ColorProperty.Changed.AddClassHandler<AvonicSegment>((segment, e) => segment.ColorChanged(e.NewValue, e.OldValue));
        SegmentSwipeGestureProperty.Changed.AddClassHandler<AvonicSegment>((segment, _) => segment.GestureChanged());
        ScrollableProperty.Changed.AddClassHandler<AvonicSegment>((segment, _) => segment.ScrollableChanged());
        IsEnabledProperty.Changed.AddClassHandler<AvonicSegment>((segment, _) => segment.DisabledChanged());
    }

    public AvonicSegment()
    {
        Items.CollectionChanged += OnSlottedItemsChange;
        Loaded += (_, _) => OnLoaded();
        Unloaded += (_, _) => OnUnloaded();
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool Scrollable
    {
        get => GetValue(ScrollableProperty);
        set => SetValue(ScrollableProperty, value);
    }

    public bool SelectOnFocus
    {
        get => GetValue(SelectOnFocusProperty);
        set => SetValue(SelectOnFocusProperty, value);
    }

    public bool SegmentSwipeGesture
    {
        get => GetValue(SegmentSwipeGestureProperty);
        set => SetValue(SegmentSwipeGestureProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    ItemControlType IItemControl.ItemControlType => ItemControlType.Segment;

    /// <summary>An item never taps a segment: it isn't in <c>getFirstInteractive</c>'s list.</summary>
    void IItemControl.OnItemTap()
    {
    }

    /// <summary><c>getButtons</c>: every button inside, in tree order. Not inside a button, whose label and icon it skips.</summary>
    internal List<AvonicSegmentButton> Buttons()
    {
        var buttons = new List<AvonicSegmentButton>();
        Collect(this);
        return buttons;

        void Collect(ILogical parent)
        {
            foreach (var child in parent.LogicalChildren)
            {
                if (child is AvonicSegmentButton button)
                {
                    buttons.Add(button);
                }
                else
                {
                    Collect(child);
                }
            }
        }
    }

    /// <summary>The <c>checked</c> getter: the button whose value is the segment's.</summary>
    private AvonicSegmentButton? Checked => Value is { } value ? Buttons().Find(button => Equals(button.Value, value)) : null;

    private bool IsRtl => FlowDirection == Avalonia.Media.FlowDirection.RightToLeft;

    // ── Life ─────────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _smooth?.Stop();
        _scroll = e.NameScope.Find<ScrollViewer>("PART_Scroll");
        _smooth = _scroll is null ? null : new SmoothScroll(_scroll);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // connectedCallback: emitStyle, getSegmentView. And the ionSegmentViewScroll listener on body.
        RaiseEvent(new IonStyleEventArgs(new Dictionary<string, bool> { ["segment"] = true }));
        _segmentView = GetSegmentView();
        UpdateClasses();
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(AvonicSegmentView.IonSegmentViewScrollEvent, HandleSegmentViewScroll);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(AvonicSegmentView.IonSegmentViewScrollEvent, HandleSegmentViewScroll);
        _topLevel = null;
        _segmentView = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// <c>componentDidLoad</c>, the first time, and the gesture every time, as leaving the tree destroys it here. Ionic's runs
    /// once for an element, so a segment that comes back doesn't snap its view again.
    /// </summary>
    private void OnLoaded()
    {
        _loaded = true;
        _gesture?.Destroy();
        _gesture = Gesture.Create(new GestureConfig
        {
            Element = this,
            GestureName = "segment",
            GesturePriority = 100,
            Threshold = 0,
            OnStart = OnStart,
            OnMove = detail => SetNextIndex(detail),
            OnEnd = OnEnd,
        });
        GestureChanged();
        if (_didLoad)
        {
            return;
        }

        _didLoad = true;
        _segmentView = GetSegmentView();
        SetCheckedClasses();

        // Once the buttons are laid out, snap the checked one into view, rather than scroll to it.
        AvonicConfig.RequestFrame(this, () => ScrollActiveButtonIntoView(false));

        if (!IsEnabled)
        {
            DisabledChanged();
        }

        // The view, from the first value, with no scroll.
        UpdateSegmentView(false);
    }

    private void OnUnloaded()
    {
        _loaded = false;
        // Destroying the gesture mid-drag never ends it, so the held indicator lets go here.
        if (_activated)
        {
            SetActivated(false);
        }

        _gesture?.Destroy();
        _gesture = null;
        _smooth?.Stop();
    }

    /// <summary>The classes from <c>render</c>: <c>hostContext('ion-toolbar')</c>, and <c>[color]</c> on it.</summary>
    private void UpdateClasses()
    {
        bool inToolbar = false, inToolbarColor = false;
        foreach (var toolbar in this.GetVisualAncestors().OfType<AvonicToolbar>())
        {
            inToolbar = true;
            inToolbarColor |= toolbar.Color is not null;
        }

        PseudoClasses.Set(":in-toolbar", inToolbar);
        PseudoClasses.Set(":in-toolbar-color", inToolbarColor);
        PseudoClasses.Set(":scrollable", Scrollable);
    }

    /// <summary>The width the segment was given, which its grid's fr columns share, as the scroller inside doesn't pass it on.</summary>
    internal double RowWidth { get; private set; } = double.PositiveInfinity;

    protected override Size MeasureOverride(Size availableSize)
    {
        // The scroller measures the grid with an infinite width whatever the segment's, so it has to be told.
        if (RowWidth != availableSize.Width)
        {
            RowWidth = availableSize.Width;
            ItemsPanelRoot?.InvalidateMeasure();
        }

        return base.MeasureOverride(availableSize);
    }

    // ── Watchers ─────────────────────────────────────────────────────────────────────────────

    private void ColorChanged(object? value, object? oldValue)
    {
        // Set after not having been set, or the other way: the buttons render again for in-segment-color.
        if ((oldValue is null) != (value is null))
        {
            EmitStyle();
        }
    }

    private void EmitStyle()
    {
        RaiseEvent(new IonStyleEventArgs(new Dictionary<string, bool> { ["segment"] = true }));
        StyleChanged?.Invoke();
    }

    /// <summary>
    /// Ionic has no watcher on <c>scrollable</c>, so its gesture keeps what it read at load. Here it follows, along with the
    /// buttons' <c>min-width</c>.
    /// </summary>
    private void ScrollableChanged()
    {
        UpdateClasses();
        StyleChanged?.Invoke();
        GestureChanged();
        InvalidateMeasure();
        ItemsPanelRoot?.InvalidateMeasure();
    }

    private void ValueChanged(object? value, object? oldValue)
    {
        // Force a value to exist if we're using a segment view.
        if (_segmentView is not null && value is null)
        {
            if (Buttons().FirstOrDefault() is { } first)
            {
                SetCurrentValue(ValueProperty, first.Value);
            }

            return;
        }

        if (oldValue is not null && value is not null)
        {
            var buttons = Buttons();
            var previous = buttons.Find(button => Equals(button.Value, oldValue));
            var current = buttons.Find(button => Equals(button.Value, value));
            if (previous is not null && current is not null)
            {
                if (_segmentView is null)
                {
                    CheckButton(previous, current);
                }
                else if (_triggerScrollOnValueChange != false)
                {
                    UpdateSegmentView();
                }
            }
        }
        else if (value is not null && oldValue is null && _segmentView is not null)
        {
            UpdateSegmentView();
        }

        // ionSelect, on every change, internal or external, for the buttons.
        Select?.Invoke();

        // The view's scroll listener scrolls the checked button into view, when there's a view.
        if (_segmentView is null)
        {
            ScrollActiveButtonIntoView();
        }

        _triggerScrollOnValueChange = null;
    }

    private void DisabledChanged()
    {
        GestureChanged();
        if (_segmentView is null)
        {
            foreach (var button in Buttons())
            {
                button.SetCurrentValue(IsEnabledProperty, IsEnabled);
            }
        }
        else
        {
            _segmentView.SetCurrentValue(IsEnabledProperty, IsEnabled);
        }
    }

    private void GestureChanged() => _gesture?.Enable(!Scrollable && IsEnabled && SegmentSwipeGesture);

    private void OnSlottedItemsChange(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // New buttons are checked if the value matches theirs.
        if (_loaded)
        {
            ValueChanged(Value, null);
        }
    }

    // ── The gesture ──────────────────────────────────────────────────────────────────────────

    private void OnStart(GestureDetail detail)
    {
        _valueBeforeGesture = Value;
        Activate(detail);
    }

    private void OnEnd(GestureDetail detail)
    {
        SetActivated(false);
        SetNextIndex(detail, true);
        if (detail.Event is { } ev)
        {
            ev.Handled = true;
        }

        if (Value is { } value && !Equals(_valueBeforeGesture, value))
        {
            EmitValueChange();
            UpdateSegmentView();
        }

        _valueBeforeGesture = null;
    }

    /// <summary>The button an event landed on, or null for the segment itself, as a disabled button lets the press through.</summary>
    private AvonicSegmentButton? ButtonAt(object? source)
    {
        for (var visual = source as Visual; visual is not null && visual != this; visual = visual.GetVisualParent())
        {
            if (visual is AvonicSegmentButton button)
            {
                return button.IsEffectivelyEnabled ? button : null;
            }
        }

        return null;
    }

    private void Activate(GestureDetail detail)
    {
        // Only a press on a segment button activates, as a disabled one gives the press to the segment.
        var clicked = ButtonAt(detail.Event?.Source);
        if (clicked is null)
        {
            return;
        }

        // With no checked button, the pressed one is checked.
        if (Checked is null)
        {
            SetCurrentValue(ValueProperty, clicked.Value);
            SetCheckedClasses();
        }

        // A press on the checked button holds the indicator.
        if (Equals(Value, clicked.Value))
        {
            SetActivated(true);
        }
    }

    /// <summary>Activate the segment and its buttons.</summary>
    private void SetActivated(bool activated)
    {
        foreach (var button in Buttons())
        {
            ((IPseudoClasses)button.Classes).Set(":segment-button-activated", activated);
        }

        _activated = activated;
        PseudoClasses.Set(":segment-activated", activated);
    }

    private void SetNextIndex(GestureDetail detail, bool isEnd = false)
    {
        var rtl = IsRtl;
        var activated = _activated;
        var buttons = Buttons();
        var index = buttons.FindIndex(button => Equals(button.Value, Value));
        if (index == -1 || TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        var previous = buttons[index];
        AvonicSegmentButton? current = null;

        // The checked button's box, and the element under the pointer's x at its middle, as the pointer can leave the segment.
        if (previous.TransformToVisual(top) is not { } matrix)
        {
            return;
        }

        var rect = new Rect(previous.Bounds.Size).TransformToAABB(matrix);
        var currentX = detail.CurrentX;
        var decreaseIndex = rtl ? currentX > rect.Right : currentX < rect.Left;
        var increaseIndex = rtl ? currentX < rect.Left : currentX > rect.Right;

        // Holding the indicator, it moves to the next button whenever the pointer passes the checked one's edge.
        if (activated && !isEnd)
        {
            int? nextIndex = null;
            if (decreaseIndex && index - 1 >= 0)
            {
                nextIndex = index - 1;
            }
            else if (increaseIndex && index + 1 < buttons.Count)
            {
                nextIndex = index + 1;
            }

            if (nextIndex is { } next && buttons[next].IsEnabled)
            {
                current = buttons[next];
            }
        }

        // Not holding it, the release checks the button where it ended.
        if (!activated && isEnd)
        {
            current = top.InputHitTest(new Point(currentX, rect.Center.Y)) is Visual hit && this.IsVisualAncestorOf(hit)
                ? ButtonAt(hit)
                : null;
        }

        if (current is not null && current != previous)
        {
            CheckButton(previous, current);
        }
    }

    // ── Clicks and keys ──────────────────────────────────────────────────────────────────────

    /// <summary><c>onClick</c>, for a click on one of the buttons, which has checked that it's enabled.</summary>
    internal void OnButtonClick(AvonicSegmentButton current)
    {
        var previous = Checked;
        SetCurrentValue(ValueProperty, current.Value);
        if (current != previous)
        {
            EmitValueChange();
        }

        if (_segmentView is not null)
        {
            UpdateSegmentView();
            if (Scrollable && previous is not null)
            {
                CheckButton(previous, current);
            }
        }
        else if (Scrollable || !SegmentSwipeGesture)
        {
            if (previous is not null)
            {
                CheckButton(previous, current);
            }
            else
            {
                SetCheckedClasses();
            }
        }
    }

    /// <summary><c>getSegmentButton</c>: among the enabled buttons, from the focused one.</summary>
    private AvonicSegmentButton? GetSegmentButton(string selector)
    {
        var buttons = Buttons().Where(button => button.IsEnabled).ToList();
        if (buttons.Count == 0)
        {
            return null;
        }

        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        var index = buttons.FindIndex(button => ReferenceEquals(button, focused));
        return selector switch
        {
            "first" => buttons[0],
            "last" => buttons[^1],
            "next" => index + 1 < buttons.Count ? buttons[index + 1] : buttons[0],
            "previous" => index - 1 >= 0 ? buttons[index - 1] : buttons[^1],
            _ => null,
        };
    }

    /// <summary>
    /// <c>onKeyDown</c>. The base class isn't called: an <see cref="ItemsControl"/> moves focus between its containers with the
    /// arrows, which Ionic's segment does its own way.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var rtl = IsRtl;
        var keyDownSelectsButton = SelectOnFocus;
        AvonicSegmentButton? current;
        switch (e.Key)
        {
            case Key.Right:
                e.Handled = true;
                current = GetSegmentButton(rtl ? "previous" : "next");
                break;
            case Key.Left:
                e.Handled = true;
                current = GetSegmentButton(rtl ? "next" : "previous");
                break;
            case Key.Home:
                e.Handled = true;
                current = GetSegmentButton("first");
                break;
            case Key.End:
                e.Handled = true;
                current = GetSegmentButton("last");
                break;
            case Key.Space:
            case Key.Enter:
                e.Handled = true;
                current = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as AvonicSegmentButton;
                keyDownSelectsButton = true;
                break;
            default:
                return;
        }

        if (current is null)
        {
            return;
        }

        if (keyDownSelectsButton)
        {
            var previous = Checked;
            CheckButton(previous ?? current, current);
            if (current != previous)
            {
                EmitValueChange();
            }
        }

        current.Focus(NavigationMethod.Directional);
    }

    // ── Checking ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>emitValueChange</c>: <c>ionChange</c>, for a change the user committed.
    /// </summary>
    private void EmitValueChange() => RaiseEvent(new SegmentChangeEventArgs(IonChangeEvent, Value));

    /// <summary>
    /// <c>checkButton</c>: the new button's indicator starts on top of the last one's, scaled to its width, and slides back, in
    /// the next frame as Ionic's <c>writeTask</c>; and the value is the new button's.
    /// </summary>
    private void CheckButton(AvonicSegmentButton previous, AvonicSegmentButton current)
    {
        // Ionic's indicators are always in the shadow root. Here one isn't until its button's template is applied, which an
        // invisible button's isn't: then it doesn't slide, but the value still changes, as nothing in Ionic stops it.
        if (previous.Indicator is { } previousIndicator && current.Indicator is { } currentIndicator
            && previousIndicator.TransformToVisual(this) is { } previousMatrix
            && currentIndicator.TransformToVisual(this) is { } currentMatrix)
        {
            // As getBoundingClientRect, with any transform they have. In the segment's own space, which RTL mirrors as it does
            // the indicator's, so the transform is right either way.
            var previousRect = new Rect(previousIndicator.Bounds.Size).TransformToAABB(previousMatrix);
            var currentRect = new Rect(currentIndicator.Bounds.Size).TransformToAABB(currentMatrix);
            var widthDelta = previousRect.Width / currentRect.Width;
            var xPosition = previousRect.Left - currentRect.Left;

            AvonicConfig.RequestFrame(this, () =>
            {
                current.Slide?.Start(xPosition, widthDelta, current);
                ScrollActiveButtonIntoView(true);
            });
        }

        SetCurrentValue(ValueProperty, current.Value);
        SetCheckedClasses();
    }

    /// <summary><c>setCheckedClasses</c>: the button after the checked one, or the first when none is checked.</summary>
    private void SetCheckedClasses()
    {
        var buttons = Buttons();
        var index = buttons.FindIndex(button => Equals(button.Value, Value));
        for (var i = 0; i < buttons.Count; i++)
        {
            ((IPseudoClasses)buttons[i].Classes).Set(":after-checked", i == index + 1);
        }
    }

    // ── The view ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>getSegmentView</c>: the view around the content that the first button with a content id names, anywhere in the
    /// window, as <c>document.querySelector</c> looks.
    /// </summary>
    private AvonicSegmentView? GetSegmentView()
    {
        if (Buttons().Find(button => button.ContentId is not null)?.ContentId is not { } id
            || TopLevel.GetTopLevel(this) is not { } top)
        {
            return null;
        }

        // The name scope first, which is where XAML puts a content beside its segment, then the window.
        var content = this.FindNameScope()?.Find<AvonicSegmentContent>(id)
                      ?? top.GetVisualDescendants().OfType<AvonicSegmentContent>().FirstOrDefault(content => content.Name == id);
        return content?.FindAncestorOfType<AvonicSegmentView>() ?? content?.FindLogicalAncestorOfType<AvonicSegmentView>();
    }

    /// <summary><c>updateSegmentView</c>: the view shows the checked button's content.</summary>
    private void UpdateSegmentView(bool smoothScroll = true)
    {
        if (Checked?.ContentId is { } contentId)
        {
            _segmentView?.SetContent(contentId, smoothScroll);
        }
    }

    /// <summary><c>handleSegmentViewScroll</c>: a person scrolling the view moves the checked button with it.</summary>
    private void HandleSegmentViewScroll(object? sender, SegmentViewScrollEventArgs e)
    {
        if (!e.IsManualScroll || _segmentView is not { } view || e.Source is not Visual from)
        {
            return;
        }

        // Only from this segment's view, or a view that holds the segment.
        if (from != view && !view.IsVisualAncestorOf(from) && !from.IsVisualAncestorOf(this))
        {
            return;
        }

        var buttons = Buttons();
        var index = buttons.FindIndex(button => Equals(button.Value, Value));
        if (buttons.Count == 0 || index < 0 || !double.IsFinite(e.ScrollRatio))
        {
            return;
        }

        var nextIndex = (int)Math.Round(e.ScrollRatio * (buttons.Count - 1), MidpointRounding.AwayFromZero);
        if (nextIndex < 0 || nextIndex >= buttons.Count)
        {
            return;
        }

        if (_lastNextIndex != nextIndex)
        {
            _lastNextIndex = nextIndex;
            _triggerScrollOnValueChange = false;
            CheckButton(buttons[index], buttons[nextIndex]);
            EmitValueChange();
        }
    }

    // ── Scrolling ────────────────────────────────────────────────────────────────────────────

    /// <summary><c>scrollActiveButtonIntoView</c>: when scrollable, the checked button is centred, as far as it can be.</summary>
    private void ScrollActiveButtonIntoView(bool smoothScroll = true)
    {
        if (!Scrollable || _scroll is null || _smooth is null || Checked is not { } active || ItemsPanelRoot is not { } panel
            || active.TransformToVisual(panel) is not { } matrix)
        {
            return;
        }

        var box = new Rect(active.Bounds.Size).TransformToAABB(matrix);
        _smooth.To(box.Left - _scroll.Viewport.Width / 2 + box.Width / 2, smoothScroll);
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new SegmentPeer(this);

    /// <summary><c>role="tablist"</c>.</summary>
    private sealed class SegmentPeer(AvonicSegment owner) : ControlAutomationPeer(owner), ISelectionProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        public bool CanSelectMultiple => false;

        public bool IsSelectionRequired => false;

        public IReadOnlyList<AutomationPeer> GetSelection() =>
            owner.Checked is { } button && CreatePeerForElement(button) is { } peer ? [peer] : [];
    }
}
