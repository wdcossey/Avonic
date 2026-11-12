using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Gestures;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-item-sliding</c>: an <see cref="AvonicItem"/> that a finger slides sideways to show the
/// <see cref="AvonicItemOptions"/> behind it. The item and the options are its items. See
/// <c>docs/components/ion-item-sliding/README.md</c>.
/// </summary>
/// <remarks>
/// <c>item-sliding.js</c> ported line for line: its <c>state</c> flags, <c>setOpenAmount</c>, the gesture's callbacks and the
/// 600 ms timer. Ionic's amounts and its <c>leftOptions</c> and <c>rightOptions</c> are physical; <see cref="ItemSlidingPanel"/>
/// works in Avalonia's logical coordinates, which RTL mirrors. The pseudoclasses are Ionic's host classes:
/// <c>:active-slide</c> is <c>item-sliding-active-slide</c>, and so on.
/// </remarks>
[PseudoClasses(":active-slide", ":active-options-end", ":active-options-start", ":active-swipe-end", ":active-swipe-start", ":closing")]
public class AvonicItemSliding : ItemsControl
{
    /// <summary><c>SWIPE_MARGIN</c>: how far past the options' width a drag is a full swipe.</summary>
    internal const double SwipeMargin = 30;

    /// <summary><c>ELASTIC_FACTOR</c>: how much of a drag past the options' width moves the item.</summary>
    internal const double ElasticFactor = 0.55;

    /// <summary><c>canStart</c>'s 15 px from the window's edge, left for swipe to go back.</summary>
    internal const double EdgeWidth = 15;

    /// <summary><c>onEnd</c>'s velocity, in px per ms, above which a release is fast.</summary>
    internal const double FastVelocity = 0.3;

    /// <summary>The closing timer's 600 ms, after which the options hide and the gesture is back on.</summary>
    internal static readonly TimeSpan CloseDelay = TimeSpan.FromMilliseconds(600);

    /// <summary><c>.item-sliding-active-slide .item</c>'s <c>transition: transform 500ms</c>.</summary>
    internal static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(500);

    /// <summary>Its <c>cubic-bezier(0.36, 0.66, 0.04, 1)</c>.</summary>
    internal static readonly CubicBezierEasing TransitionEasing = new(0.36, 0.66, 0.04, 1);

    /// <summary>Ionic's module-level <c>openSlidingItem</c>: one per process here, as <see cref="GestureController"/> is.</summary>
    private static AvonicItemSliding? s_openSlidingItem;

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionDrag</c>: the item moved, by a drag or to where it rests. Not raised for 0, when it closes.</summary>
    public static readonly RoutedEvent<ItemSlidingDragEventArgs> IonDragEvent =
        RoutedEvent.Register<AvonicItemSliding, ItemSlidingDragEventArgs>(nameof(IonDrag), RoutingStrategies.Bubble);

    public event EventHandler<ItemSlidingDragEventArgs>? IonDrag
    {
        add => AddHandler(IonDragEvent, value);
        remove => RemoveHandler(IonDragEvent, value);
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    [Flags]
    private enum SlidingState
    {
        Disabled = 2,
        Enabled = 4,
        End = 8,
        Start = 16,
        SwipeEnd = 32,
        SwipeStart = 64,
    }

    [Flags]
    private enum ItemSide
    {
        None = 0,
        Start = 1,
        End = 2,
        Both = 3,
    }

    private AvonicItem? _item;
    private double _openAmount;
    private double _initialOpenAmount;
    private double _optsWidthRightSide;
    private double _optsWidthLeftSide;
    private ItemSide _sides;
    private bool _optsDirty = true;
    private AvonicContent? _contentEl;
    private bool _initialContentScrollY = true;
    private bool _contentScrollDisabled;
    private SlidingState _state = SlidingState.Disabled;
    private bool _closing;
    private AvonicItemOptions? _leftOptions;
    private AvonicItemOptions? _rightOptions;
    private Gesture? _gesture;
    private IDisposable? _tmr;
    private bool _attached;

    /// <summary>The item's <c>style.transition = 'none'</c>, from a drag's start until <c>setOpenAmount(…, true)</c>.</summary>
    private bool _transitionNone;

    private readonly SlideTransition _transition = new(TransitionEasing);
    private IDisposable? _transitionFrame;
    private TranslateTransform? _translate;

    // pointer-events: none and z-index: 2 on the item, while active, at style priority.
    private readonly StyleValue<bool> _itemHitTest = new();
    private readonly StyleValue<int> _itemZIndex = new();
    private readonly List<IDisposable> _itemBindings = [];

    // display: none, visibility: hidden and pointer-events: none on each options, at style priority.
    private readonly Dictionary<AvonicItemOptions, OptionsValues> _options = [];

    private sealed class OptionsValues
    {
        public readonly StyleValue<bool> Visible = new();
        public readonly StyleValue<bool> HitTest = new();
        public IDisposable[] Bindings = [];
    }

    public AvonicItemSliding()
    {
        // Bound as the children arrive, before an app's styles reach them at attach, so that an app's style wins over these
        // values, as a later rule of the same weight does in CSS.
        Items.CollectionChanged += (_, _) =>
        {
            if (!_attached)
            {
                SetItem(FindItem());
            }

            OptionsChanged();
        };
    }

    // ── Methods ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>getOpenAmount()</c>: how far the item is open, in pixels, positive with the right options showing.</summary>
    public double GetOpenAmount() => _openAmount;

    /// <summary>
    /// Ionic <c>getSlidingRatio()</c>: the open amount over the width of the options it shows. Positive for the right options,
    /// negative for the left; over 1 is open further than the options.
    /// </summary>
    public double GetSlidingRatio() => GetSlidingRatioSync();

    /// <summary>
    /// Ionic <c>open(side)</c>: opens the options on <paramref name="side"/>, or the first found, at the next frame. Nothing if that
    /// side is already open, or has no options. Any other open sliding item closes first.
    /// </summary>
    public void Open(ItemOptionsSide? side = null)
    {
        if (_item is null)
        {
            SetItem(FindItem());
        }

        if (_item is null)
        {
            return;
        }

        var optionsToOpen = GetOptions(side);
        if (optionsToOpen is null)
        {
            return;
        }

        // Ionic infers the side from the physical options, then reads it through isEndSide: so in RTL, with no side given, the
        // left options open to the right. Kept, as Ionic does it.
        side ??= optionsToOpen == _leftOptions ? ItemOptionsSide.Start : ItemOptionsSide.End;
        var openRight = IsEndSide(side.Value);

        var isStartOpen = _openAmount < 0;
        var isEndOpen = _openAmount > 0;
        if (isStartOpen && optionsToOpen == _leftOptions)
        {
            return;
        }

        if (isEndOpen && optionsToOpen == _rightOptions)
        {
            return;
        }

        CloseOpened();
        SetState(SlidingState.Enabled);
        AvonicConfig.RequestFrame(this, () =>
        {
            CalculateOptsWidth();
            var width = openRight ? _optsWidthRightSide : -_optsWidthLeftSide;
            s_openSlidingItem = this;
            SetOpenAmount(width, false);
            SetState(openRight ? SlidingState.End : SlidingState.Start);
        });
    }

    /// <summary>Ionic <c>close()</c>.</summary>
    public void Close() => SetOpenAmount(0, true);

    /// <summary>Ionic <c>closeOpened()</c>: closes whichever sliding item is open, and says whether there was one.</summary>
    public bool CloseOpened()
    {
        if (s_openSlidingItem is { } open)
        {
            open.Close();
            s_openSlidingItem = null;
            return true;
        }

        return false;
    }

    /// <summary>Ionic <c>getOptions(side)</c>: <c>'start'</c> is the physical left, as Ionic has it.</summary>
    private AvonicItemOptions? GetOptions(ItemOptionsSide? side) => side switch
    {
        null => _leftOptions ?? _rightOptions,
        ItemOptionsSide.Start => _leftOptions,
        _ => _rightOptions,
    };

    /// <summary>Ionic <c>isEndSide(side)</c>, with this control's <see cref="Visual.FlowDirection"/> for the document's <c>dir</c>.</summary>
    private bool IsEndSide(ItemOptionsSide side) => (side == ItemOptionsSide.End) != IsRtl;

    private bool IsRtl => FlowDirection == FlowDirection.RightToLeft;

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        SetItem(FindItem());
        _contentEl = this.FindAncestorOfType<AvonicContent>();
        UpdateOptions();
        _gesture = Gesture.Create(new GestureConfig
        {
            Element = this,
            GestureName = "item-swipe",
            GesturePriority = 100,
            Threshold = 5,
            CanStart = CanStart,
            OnStart = _ => OnStart(),
            OnMove = OnMove,
            OnEnd = OnEnd,
        });
        _gesture.Enable(IsEnabled);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        _gesture?.Destroy();
        _gesture = null;

        // Destroy() ends a drag with no OnEnd, as Ionic's does, which would leave the content unable to scroll: put it back.
        RestoreContentScroll();
        SetItem(null);
        _leftOptions = _rightOptions = null;
        if (s_openSlidingItem == this)
        {
            s_openSlidingItem = null;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsEnabledProperty)
        {
            // disabledChanged
            _gesture?.Enable(change.GetNewValue<bool>());
        }
        else if (change.Property == FlowDirectionProperty)
        {
            UpdateOptions();
            ApplyTranslate(_transition.At(AvonicConfig.Now()));
        }
    }

    /// <summary>Ionic's <c>MutationObserver</c> on the subtree: any change of the children, here or in an options, finds them again.</summary>
    internal void OptionsChanged() => UpdateOptions();

    /// <summary><c>el.querySelector('ion-item')</c>: the first item among the light DOM's descendants, in document order.</summary>
    private AvonicItem? FindItem() => this.GetLogicalDescendants().OfType<AvonicItem>().FirstOrDefault();

    /// <summary>Ionic <c>updateOptions()</c>: every options inside, the last one on each physical side winning.</summary>
    private void UpdateOptions()
    {
        var found = this.GetLogicalDescendants().OfType<AvonicItemOptions>().ToList();
        var sides = ItemSide.None;
        _leftOptions = _rightOptions = null;
        foreach (var options in found)
        {
            if (options.IsRight)
            {
                _rightOptions = options;
                sides |= ItemSide.End;
            }
            else
            {
                _leftOptions = options;
                sides |= ItemSide.Start;
            }
        }

        foreach (var gone in _options.Keys.Except(found).ToList())
        {
            foreach (var binding in _options[gone].Bindings)
            {
                binding.Dispose();
            }

            _options.Remove(gone);
        }

        foreach (var options in found.Where(o => !_options.ContainsKey(o)))
        {
            var values = new OptionsValues();
            values.Bindings =
            [
                options.Bind(IsVisibleProperty, values.Visible, BindingPriority.Style),
                options.Bind(IsHitTestVisibleProperty, values.HitTest, BindingPriority.Style),
            ];
            _options[options] = values;
        }

        _optsDirty = true;
        _sides = sides;
        UpdateClasses();
    }

    // ── Gesture ──────────────────────────────────────────────────────────────────────────────

    private bool CanStart(GestureDetail detail)
    {
        // Very close to the window's start, the left options don't open, so that swipe to go back still works.
        var width = TopLevel.GetTopLevel(this)?.ClientSize.Width ?? 0;
        var atEdge = IsRtl ? width - detail.StartX < EdgeWidth : detail.StartX < EdgeWidth;
        if (atEdge)
        {
            return false;
        }

        var selected = s_openSlidingItem;
        if (selected is not null && selected != this)
        {
            CloseOpened();
        }

        return _rightOptions is not null || _leftOptions is not null;
    }

    private void OnStart()
    {
        // The item is found again at every drag: an app may swap it.
        SetItem(FindItem());
        if (_contentEl is { } content)
        {
            // disableContentScrollY
            _initialContentScrollY = content.ScrollY;
            content.SetCurrentValue(AvonicContent.ScrollYProperty, false);
            _contentScrollDisabled = true;
        }

        s_openSlidingItem = this;
        _tmr?.Dispose();
        _tmr = null;
        if (_openAmount == 0)
        {
            _optsDirty = true;
            SetState(SlidingState.Enabled);
        }

        _initialOpenAmount = _openAmount;
        if (_item is not null)
        {
            // style.transition = 'none', which ends a running transition where it was going.
            _transitionNone = true;
            _transitionFrame?.Dispose();
            _transitionFrame = null;
            _transition.Snap(_transition.Target);
            ApplyTranslate(_transition.Target);
        }
    }

    private void OnMove(GestureDetail detail)
    {
        if (_optsDirty)
        {
            CalculateOptsWidth();
        }

        var openAmount = _initialOpenAmount - detail.DeltaX;
        switch (_sides)
        {
            case ItemSide.End:
                openAmount = Math.Max(0, openAmount);
                break;
            case ItemSide.Start:
                openAmount = Math.Min(0, openAmount);
                break;
            case ItemSide.Both:
                break;
            default:
                return;
        }

        if (openAmount > _optsWidthRightSide)
        {
            var optsWidth = _optsWidthRightSide;
            openAmount = optsWidth + (openAmount - optsWidth) * ElasticFactor;
        }
        else if (openAmount < -_optsWidthLeftSide)
        {
            var optsWidth = -_optsWidthLeftSide;
            openAmount = optsWidth + (openAmount - optsWidth) * ElasticFactor;
        }

        SetOpenAmount(openAmount, false);
    }

    private void OnEnd(GestureDetail detail)
    {
        RestoreContentScroll();

        var velocity = detail.VelocityX;
        var restingPoint = _openAmount > 0 ? _optsWidthRightSide : -_optsWidthLeftSide;

        // Did the drag clear the buttons' midpoint, or move fast enough to swipe open?
        var isResetDirection = _openAmount > 0 == !(velocity < 0);
        var isMovingFast = Math.Abs(velocity) > FastVelocity;
        var isOnCloseZone = Math.Abs(_openAmount) < Math.Abs(restingPoint / 2);
        if (SwipeShouldReset(isResetDirection, isMovingFast, isOnCloseZone))
        {
            restingPoint = 0;
        }

        var state = _state;
        SetOpenAmount(restingPoint, true);
        if ((state & SlidingState.SwipeEnd) != 0 && _rightOptions is not null)
        {
            _rightOptions.FireSwipeEvent();
        }
        else if ((state & SlidingState.SwipeStart) != 0 && _leftOptions is not null)
        {
            _leftOptions.FireSwipeEvent();
        }
    }

    /// <summary><c>resetContentScrollY</c>: the content's <c>ScrollY</c> back to what it was when the drag started.</summary>
    private void RestoreContentScroll()
    {
        if (_contentScrollDisabled && _contentEl is { } content)
        {
            content.SetCurrentValue(AvonicContent.ScrollYProperty, _initialContentScrollY);
        }

        _contentScrollDisabled = false;
    }

    /// <summary>Ionic's truth table, solved: close if slow in the close zone, or fast towards closed.</summary>
    private static bool SwipeShouldReset(bool isResetDirection, bool isMovingFast, bool isOnResetZone) =>
        (!isMovingFast && isOnResetZone) || (isResetDirection && isMovingFast);

    /// <summary>
    /// Ionic <c>calculateOptsWidth()</c>: each side's options shown for a moment and measured as wide as they want
    /// (<c>offsetWidth</c> with <c>display: flex</c>), as tall as the item.
    /// </summary>
    private void CalculateOptsWidth()
    {
        _optsWidthRightSide = MeasureOptions(_rightOptions);
        _optsWidthLeftSide = MeasureOptions(_leftOptions);
        _optsDirty = false;
    }

    private double MeasureOptions(AvonicItemOptions? options)
    {
        if (options is null || !_options.TryGetValue(options, out var values))
        {
            return 0;
        }

        values.Visible.Clear();
        var shown = options.IsVisible;
        options.Measure(new Size(double.PositiveInfinity, Bounds.Height));
        var width = shown ? options.DesiredSize.Width : 0;
        UpdateClasses();
        return width;
    }

    // ── setOpenAmount ────────────────────────────────────────────────────────────────────────

    private void SetOpenAmount(double openAmount, bool isFinal)
    {
        _tmr?.Dispose();
        _tmr = null;
        if (_item is null)
        {
            return;
        }

        _openAmount = openAmount;
        if (isFinal)
        {
            _transitionNone = false;
        }

        if (openAmount > 0)
        {
            SetState(openAmount >= _optsWidthRightSide + SwipeMargin
                ? SlidingState.End | SlidingState.SwipeEnd
                : SlidingState.End);
        }
        else if (openAmount < 0)
        {
            SetState(openAmount <= -_optsWidthLeftSide - SwipeMargin
                ? SlidingState.Start | SlidingState.SwipeStart
                : SlidingState.Start);
        }
        else
        {
            // The options take no input while the item closes, and the gesture can't interrupt it, which would let two be open.
            _closing = true;
            _gesture?.Enable(false);

            // Enable(false) mid-drag ends the drag, and its OnEnd closes again, re-entrantly, and has set a timer already.
            _tmr?.Dispose();
            _tmr = AvonicConfig.Schedule(() =>
            {
                _tmr = null;
                _closing = false;
                SetState(SlidingState.Disabled);
                _gesture?.Enable(IsEnabled);
            }, CloseDelay);
            s_openSlidingItem = null;
            UpdateClasses();
            MoveItem(0);
            return;
        }

        MoveItem(-openAmount);
        RaiseEvent(new ItemSlidingDragEventArgs(IonDragEvent, this, openAmount, GetSlidingRatioSync()));
    }

    private double GetSlidingRatioSync() => _openAmount switch
    {
        > 0 => _openAmount / _optsWidthRightSide,
        < 0 => _openAmount / _optsWidthLeftSide,
        _ => 0,
    };

    private void SetState(SlidingState state)
    {
        _state = state;
        UpdateClasses();
    }

    /// <summary>Ionic's <c>render()</c> classes, and what <c>item-sliding.css</c> and <c>item-options.md.css</c> do with them.</summary>
    private void UpdateClasses()
    {
        var active = _state != SlidingState.Disabled;
        var end = (_state & SlidingState.End) != 0;
        var start = (_state & SlidingState.Start) != 0;
        var swipeEnd = (_state & SlidingState.SwipeEnd) != 0;
        var swipeStart = (_state & SlidingState.SwipeStart) != 0;
        PseudoClasses.Set(":active-slide", active);
        PseudoClasses.Set(":active-options-end", end);
        PseudoClasses.Set(":active-options-start", start);
        PseudoClasses.Set(":active-swipe-end", swipeEnd);
        PseudoClasses.Set(":active-swipe-start", swipeStart);
        PseudoClasses.Set(":closing", _closing);

        // .item-sliding-active-slide .item: pointer-events: none; z-index: 2
        _itemHitTest.SetWhen(active, false);
        _itemZIndex.SetWhen(active, 2);

        foreach (var (options, values) in _options)
        {
            // display: none until active; then visibility: hidden, except the open side's, which is width: 100%.
            var right = options.IsRight;
            values.Visible.Set(active && (right ? end : start));
            // .item-sliding-closing ion-item-options: pointer-events: none
            values.HitTest.SetWhen(_closing, false);
            options.SetSwiping(right ? swipeEnd : swipeStart);
        }
    }

    // ── The item ─────────────────────────────────────────────────────────────────────────────

    private void SetItem(AvonicItem? item)
    {
        if (item == _item)
        {
            return;
        }

        foreach (var binding in _itemBindings)
        {
            binding.Dispose();
        }

        _itemBindings.Clear();
        if (_item is not null && _translate is not null && ReferenceEquals(_item.RenderTransform, _translate))
        {
            _item.ClearValue(RenderTransformProperty);
        }

        _item = item;
        if (item is not null)
        {
            _itemBindings.Add(item.Bind(IsHitTestVisibleProperty, _itemHitTest, BindingPriority.Style));
            _itemBindings.Add(item.Bind(ZIndexProperty, _itemZIndex, BindingPriority.Style));
            ApplyTranslate(_transition.At(AvonicConfig.Now()));
        }
    }

    /// <summary>
    /// <c>style.transform</c>: <c>translate3d(-openAmount px, 0, 0)</c>, through the 500 ms transition unless a drag turned it off.
    /// The transition only runs while <c>.item-sliding-active-slide</c> is on, which it always is when this is called with one.
    /// </summary>
    private void MoveItem(double x)
    {
        var now = AvonicConfig.Now();
        if (_transitionNone || _state == SlidingState.Disabled)
        {
            _transition.Snap(x);
        }
        else
        {
            _transition.Start(x, now, TransitionDuration);
        }

        OnTransitionFrame();
    }

    private void OnTransitionFrame()
    {
        _transitionFrame?.Dispose();
        _transitionFrame = null;
        var now = AvonicConfig.Now();
        ApplyTranslate(_transition.At(now));
        if (_transition.IsRunning(now))
        {
            _transitionFrame = AvonicConfig.RequestFrame(this, OnTransitionFrame);
        }
    }

    /// <summary>
    /// Physical pixels to the item's <c>RenderTransform</c>, a local value as an inline style is. Avalonia mirrors an RTL subtree,
    /// so a physical move left is a logical move right there. Taken away at 0, as <c>style.transform = ''</c> is.
    /// </summary>
    private void ApplyTranslate(double x)
    {
        if (_item is not { } item)
        {
            return;
        }

        if (x == 0)
        {
            if (_translate is not null && ReferenceEquals(item.RenderTransform, _translate))
            {
                item.ClearValue(RenderTransformProperty);
            }

            return;
        }

        _translate ??= new TranslateTransform();
        _translate.X = IsRtl ? -x : x;
        if (!ReferenceEquals(item.RenderTransform, _translate))
        {
            item.RenderTransform = _translate;
        }
    }

    // ── Containers and automation ────────────────────────────────────────────────────────────

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Control>(item, out recycleKey);

    /// <summary>No role in Ionic: a plain element, where an <see cref="ItemsControl"/>'s own peer would call it a list.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new GroupPeer(this);

    internal sealed class GroupPeer(Control owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override bool IsContentElementCore() => false;

        protected override bool IsControlElementCore() => false;
    }
}
