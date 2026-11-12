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
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-picker-column</c>: a wheel of <see cref="AvonicPickerColumnOption"/>s in a vertical scroller that snaps an option
/// to its centre. The centred option is the column's <see cref="Value"/>: a scroll by the user that comes to rest, a click on an
/// option, or a key, sets it and raises <see cref="IonChange"/>; setting it in code scrolls there smoothly, with no event. Its
/// options are <see cref="ItemsControl.Items"/>, as <c>core.json</c> lists only the <c>prefix</c> and <c>suffix</c> slots. See
/// <c>docs/components/ion-picker/README.md</c>.
/// </summary>
[TemplatePart("PART_Scroll", typeof(ScrollViewer))]
[PseudoClasses(ColumnActive, NumericInputClass, AnyHover, InPicker)]
public class AvonicPickerColumn : ItemsControl
{
    /// <summary>Ionic's <c>picker-column-active</c>: the column takes typed digits.</summary>
    internal const string ColumnActive = ":column-active";

    /// <summary>Ionic's <c>picker-column-numeric-input</c>.</summary>
    internal const string NumericInputClass = ":numeric-input";

    /// <summary><c>@media (any-hover: hover)</c>, which the focused background needs.</summary>
    internal const string AnyHover = ":any-hover";

    /// <summary>A direct child of a picker, which <c>::slotted(ion-picker-column)</c> styles.</summary>
    internal const string InPicker = ":in-picker";

    /// <summary>Chromium's scroll for one notch of a mouse wheel, after which the column snaps to the nearest option.</summary>
    internal const double WheelStep = 100;

    /// <summary>
    /// How long after a touchpad's last small scroll the column snaps, as Chromium snaps when the gesture ends, which Avalonia
    /// doesn't report. Not measured: Chromium's gesture end comes from the touchpad itself.
    /// </summary>
    internal static readonly TimeSpan WheelSnapDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>The time after the last scroll that the centred option becomes the value, as Ionic's timeout.</summary>
    internal static readonly TimeSpan ScrollEndDelay = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Ionic's <c>isPlatform('mobile')</c>, <c>any-pointer: coarse</c>: Android and iOS here, as Avalonia has no such query. It
    /// reverses the arrows and pages, and a platform without it has a hovering pointer, which the focused background needs.
    /// </summary>
    internal static Func<bool> IsMobile = () => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    /// <summary>Ionic <c>color</c>: the focused column's background. <c>primary</c> unless set, as Ionic's default is.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicPickerColumn>();

    /// <summary>Ionic <c>value</c>: the selected option's value. Null is <c>undefined</c>.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicPickerColumn, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Ionic's <c>prefix</c> slot: left of the options.</summary>
    public static readonly StyledProperty<object?> PrefixContentProperty =
        AvaloniaProperty.Register<AvonicPickerColumn, object?>(nameof(PrefixContent));

    public static readonly StyledProperty<IDataTemplate?> PrefixContentTemplateProperty =
        AvaloniaProperty.Register<AvonicPickerColumn, IDataTemplate?>(nameof(PrefixContentTemplate));

    /// <summary>Ionic's <c>suffix</c> slot: right of the options.</summary>
    public static readonly StyledProperty<object?> SuffixContentProperty =
        AvaloniaProperty.Register<AvonicPickerColumn, object?>(nameof(SuffixContent));

    public static readonly StyledProperty<IDataTemplate?> SuffixContentTemplateProperty =
        AvaloniaProperty.Register<AvonicPickerColumn, IDataTemplate?>(nameof(SuffixContentTemplate));

    /// <summary>
    /// The column's <c>text-align</c>, which its options inherit: centred, or at the start and end for the first and last column
    /// of a picker. <c>ion-datetime</c> sets it on its columns, as Ionic's comment on <c>.picker-opts</c> says.
    /// </summary>
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<AvonicPickerColumn>();

    /// <summary>Ionic <c>ionChange</c>: the user changed the value. Not raised when code sets <see cref="Value"/>.</summary>
    public static readonly RoutedEvent<PickerColumnChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicPickerColumn, PickerColumnChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    private readonly StyleValue<string?> _defaultColor = new();
    private readonly HiddenChainWatcher _hiddenChain;
    private ScrollViewer? _scroll;
    private PickerSmoothScroll? _smooth;
    private AvonicPicker? _parent;
    private Rect? _viewport;
    private bool _attached;
    private bool _isColumnVisible;
    private bool _listening;
    private IDisposable? _listenFrame;
    private IDisposable? _scrollFrame;
    private IDisposable? _scrollTimeout;
    private IDisposable? _renderFrame;
    private AvonicPickerColumnOption? _listenerActive;
    private bool _isScrolling;
    private bool _canExitInputMode = true;
    private Action? _scrollEndCallback;
    private bool _isActive;
    private bool _numericInput;
    private bool _centrePosted;
    private IDisposable? _wheelSnap;
    private string? _peerValue;

    static AvonicPickerColumn()
    {
        // tabindex="0" on .picker-opts, and none when disabled, which IsEnabled gives.
        FocusableProperty.OverrideDefaultValue<AvonicPickerColumn>(true);
        ValueProperty.Changed.AddClassHandler<AvonicPickerColumn>((column, _) => column.OnValueChanged());
        ColorProperty.Changed.AddClassHandler<AvonicPickerColumn>((column, _) => column.RequestRender());
    }

    public AvonicPickerColumn()
    {
        // display: none on the column or an ancestor ends its intersection, and showing it starts one.
        _hiddenChain = new HiddenChainWatcher(CheckVisible);
        // color = 'primary', at style priority so that an app's style still wins.
        Bind(ColorProperty, _defaultColor, BindingPriority.Style);
        _defaultColor.Set("primary");
        PseudoClasses.Set(AnyHover, !IsMobile());
        // Ahead of the scroller's own wheel, which would step to the next snap point whatever the distance.
        AddHandler(PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    public object? PrefixContent
    {
        get => GetValue(PrefixContentProperty);
        set => SetValue(PrefixContentProperty, value);
    }

    public IDataTemplate? PrefixContentTemplate
    {
        get => GetValue(PrefixContentTemplateProperty);
        set => SetValue(PrefixContentTemplateProperty, value);
    }

    public object? SuffixContent
    {
        get => GetValue(SuffixContentProperty);
        set => SetValue(SuffixContentProperty, value);
    }

    public IDataTemplate? SuffixContentTemplate
    {
        get => GetValue(SuffixContentTemplateProperty);
        set => SetValue(SuffixContentTemplateProperty, value);
    }

    public event EventHandler<PickerColumnChangeEventArgs>? IonChange
    {
        add => AddHandler(IonChangeEvent, value);
        remove => RemoveHandler(IonChangeEvent, value);
    }

    /// <summary>
    /// Ionic's internal <c>numericInput</c>: tapping the picker lets the user type digits into this column, as
    /// <c>ion-datetime</c>'s hour and minute columns do.
    /// </summary>
    internal bool NumericInput
    {
        get => _numericInput;
        set
        {
            if (_numericInput == value)
            {
                return;
            }

            _numericInput = value;
            PseudoClasses.Set(NumericInputClass, value);
            RequestRender();
        }
    }

    /// <summary>Ionic's <c>isActive</c>: the column takes typed digits.</summary>
    internal bool IsInputModeActive => _isActive;

    /// <summary>Whether a scroll is under way, until 250 ms after its last frame, as Ionic's <c>isScrolling</c>.</summary>
    internal bool IsScrolling => _isScrolling;

    /// <summary>Ionic's <c>isColumnVisible</c>.</summary>
    internal bool IsColumnVisible => _isColumnVisible;

    internal ScrollViewer? Scroller => _scroll;

    private PickerColumnPanel? Panel => ItemsPanelRoot as PickerColumnPanel;

    /// <summary>The options, in order, as <c>querySelectorAll('ion-picker-column-option')</c>.</summary>
    internal IEnumerable<AvonicPickerColumnOption> Options =>
        Enumerable.Range(0, ItemCount).Select(ContainerFromIndex).OfType<AvonicPickerColumnOption>();

    /// <summary>
    /// Ionic's <c>activeItem</c>: the first option whose value is the column's. A disabled option isn't, unless the whole column is
    /// disabled, as the value should then still appear.
    /// </summary>
    internal AvonicPickerColumnOption? ActiveItem =>
        Options.FirstOrDefault(option => (!IsColumnDisabled ? option.IsEnabled : true) && Equals(option.Value, Value));

    private bool IsColumnDisabled => !IsEffectivelyEnabled;

    /// <summary>Ionic <c>setFocus()</c>: focuses the column, which is the scroller's focusable element here.</summary>
    public void SetFocus() => Focus(NavigationMethod.Unspecified);

    /// <summary>A control is its own row; anything else, from <c>ItemsSource</c>, gets an option to show it, whose value it is.</summary>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Control>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new AvonicPickerColumnOption();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is AvonicPickerColumnOption option && item is not Control)
        {
            option.SetCurrentValue(AvonicPickerColumnOption.ValueProperty, item);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _smooth?.Stop();
        if (_scroll is not null)
        {
            _scroll.PropertyChanged -= OnScrollPropertyChanged;
        }

        _scroll = e.NameScope.Find<ScrollViewer>("PART_Scroll");
        _smooth = _scroll is null ? null : new PickerSmoothScroll(_scroll);
        if (_scroll is not null)
        {
            _scroll.PropertyChanged += OnScrollPropertyChanged;
        }
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        PseudoClasses.Set(InPicker, Parent is AvonicPicker);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        // componentWillLoad: el.closest('ion-picker'), and its ionInputModeChange.
        _parent = this.FindLogicalAncestorOfType<AvonicPicker>();
        if (_parent is not null)
        {
            _parent.InputModeChanged += OnInputModeChange;
        }

        EffectiveViewportChanged += OnEffectiveViewportChanged;
        // A new listener hears nothing until a layout pass.
        InvalidateArrange();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        EffectiveViewportChanged -= OnEffectiveViewportChanged;
        if (_parent is not null)
        {
            _parent.InputModeChanged -= OnInputModeChange;
            _parent = null;
        }

        _viewport = null;
        SetColumnVisible(false);
        _hiddenChain.Dispose();
        _smooth?.Stop();
        // Avonic's own snap for a touchpad goes with the column. Ionic's scroll-end timeout outlives the element, so it stays.
        _wheelSnap?.Dispose();
        _wheelSnap = null;
        _renderFrame?.Dispose();
        _renderFrame = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        CheckVisible();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEffectivelyEnabledProperty)
        {
            Panel?.UpdateSnapPoints();
            RequestRender();
        }
        else if (change.Property == IsVisibleProperty)
        {
            CheckVisible();
        }
    }

    // ── Visibility: the IntersectionObserver ─────────────────────────────────────────────────

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        _viewport = e.EffectiveViewport;
        CheckVisible();
    }

    /// <summary>
    /// Ionic observes the column with the picker as the root, so inside a picker it's visible whenever it's shown with a size; on its
    /// own, the root is the viewport, and an edge counts.
    /// </summary>
    private void CheckVisible()
    {
        if (!_attached || (_parent is null && _viewport is null))
        {
            return;
        }

        if (!IsEffectivelyVisible)
        {
            _hiddenChain.Watch(this);
        }

        var size = Bounds.Size;
        var shown = IsEffectivelyVisible && size.Width > 0 && size.Height > 0;
        SetColumnVisible(shown && (_parent is not null || AvonicImg.Intersects(_viewport!.Value, size)));
    }

    /// <summary>
    /// Whether it's visible now. Avalonia doesn't tell a control when an ancestor hides it, so a column that thought it was visible
    /// looks before it acts, as the observer would have told it.
    /// </summary>
    private bool IsVisibleNow()
    {
        if (_isColumnVisible && !IsEffectivelyVisible)
        {
            SetColumnVisible(false);
            _hiddenChain.Watch(this);
        }

        return _isColumnVisible;
    }

    private void SetColumnVisible(bool visible)
    {
        if (visible == _isColumnVisible)
        {
            return;
        }

        _isColumnVisible = visible;
        if (visible)
        {
            // The first call has to come before the scroll listener, so the active option is marked here, and an old one cleared.
            // Ionic looks for the old one in the column's shadow root, where slotted options aren't, so it never finds it: see
            // "Decided" in the README.
            foreach (var option in Options.Where(option => option.IsActive))
            {
                option.SetActive(false);
            }

            var active = ActiveItem;
            ScrollActiveItemIntoView();
            active?.SetActive(true);
            if (active is null)
            {
                Resnap();
            }
            InitializeScrollListener();
        }
        else
        {
            DestroyScrollListener();
        }
    }

    // ── Scrolling ────────────────────────────────────────────────────────────────────────────

    /// <summary>The listener starts a frame later, so that the first jump into view isn't a user's scroll.</summary>
    private void InitializeScrollListener()
    {
        _listenFrame?.Dispose();
        _listenerActive = ActiveItem;
        _listenFrame = AvonicConfig.RequestFrame(this, () =>
        {
            _listenFrame = null;
            _listening = true;
        });
    }

    private void DestroyScrollListener()
    {
        _listenFrame?.Dispose();
        _listenFrame = null;
        _listening = false;
        _scrollFrame?.Dispose();
        _scrollFrame = null;
    }

    private void OnScrollPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty && _listening && _scrollFrame is null)
        {
            // scroll → raf(scrollCallback).
            _scrollFrame = AvonicConfig.RequestFrame(this, OnScrollFrame);
        }
    }

    /// <summary>Ionic's <c>scrollCallback</c>: the option at the centre is active, and becomes the value 250 ms after the last scroll.</summary>
    private void OnScrollFrame()
    {
        _scrollFrame = null;
        _scrollTimeout?.Dispose();
        _scrollTimeout = null;
        _isScrolling = true;

        var newActive = OptionAtCentre();
        _listenerActive?.SetActive(false);
        if (newActive is null || !newActive.IsEnabled)
        {
            return;
        }

        if (newActive != _listenerActive && _canExitInputMode)
        {
            // Only a change of the selected option by the user's scroll leaves input mode.
            ExitInputMode();
        }

        _listenerActive = newActive;
        newActive.SetActive(true);
        _scrollTimeout = AvonicConfig.Schedule(() =>
        {
            _scrollTimeout = null;
            _isScrolling = false;
            var callback = _scrollEndCallback;
            _scrollEndCallback = null;
            callback?.Invoke();
            _canExitInputMode = true;
            SetValue(newActive.Value);
        }, ScrollEndDelay);
    }

    /// <summary>
    /// <c>elementsFromPoint</c> at the scroller's centre, finding an option. A disabled column's options are
    /// <c>pointer-events: none</c>, which <c>elementsFromPoint</c> skips.
    /// </summary>
    internal AvonicPickerColumnOption? OptionAtCentre()
    {
        if (_scroll is null || Panel is not { } panel || IsColumnDisabled)
        {
            return null;
        }

        var centre = _scroll.Offset.Y + _scroll.Viewport.Height / 2;
        foreach (var child in panel.Children)
        {
            if (child is AvonicPickerColumnOption option && option.IsVisible && option.Bounds.Top <= centre && centre <= option.Bounds.Bottom)
            {
                return option;
            }
        }

        return null;
    }

    /// <summary>
    /// A wheel over the options: Chromium scrolls 100 px a notch and then snaps to the nearest option, as
    /// <c>scroll-snap-type: y mandatory</c> does; a touchpad's smaller steps scroll as they come, and snap once they stop. At the
    /// end of the options the wheel goes on to whatever scrolls outside, as it does in a browser.
    /// </summary>
    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (_scroll is null || _smooth is null || IsColumnDisabled || e.Delta.Y == 0
            || e.Source is not Visual source || (source != _scroll && !_scroll.IsVisualAncestorOf(source)))
        {
            return;
        }

        var max = Math.Max(0, _scroll.Extent.Height - _scroll.Viewport.Height);
        var target = Math.Clamp(_scroll.Offset.Y - e.Delta.Y * WheelStep, 0, max);
        if (Math.Abs(target - _scroll.Offset.Y) < 0.5)
        {
            return;
        }

        _smooth.Stop();
        _wheelSnap?.Dispose();
        _wheelSnap = null;
        if (Math.Abs(e.Delta.Y) >= 1)
        {
            _scroll.Offset = new Vector(_scroll.Offset.X, Snap(target));
        }
        else
        {
            _scroll.Offset = new Vector(_scroll.Offset.X, target);
            _wheelSnap = AvonicConfig.Schedule(() =>
            {
                _wheelSnap = null;
                if (_scroll is not null)
                {
                    _smooth?.To(Snap(_scroll.Offset.Y), smooth: true);
                }
            }, WheelSnapDelay);
        }

        e.Handled = true;
    }

    /// <summary>Ionic's <c>scrollActiveItemIntoView</c>.</summary>
    internal void ScrollActiveItemIntoView(bool smooth = false)
    {
        if (ActiveItem is { } active)
        {
            CenterPickerItemInView(active, smooth, canExitInputMode: false);
        }
    }

    /// <summary>
    /// Ionic's <c>centerPickerItemInView</c>: three empty rows up from the option, and half a row down, where the scroll then snaps
    /// to the nearest snap point, as <c>scroll-snap-type: mandatory</c> does to a programmatic scroll.
    /// </summary>
    internal void CenterPickerItemInView(AvonicPickerColumnOption target, bool smooth = true, bool canExitInputMode = true)
    {
        if (!IsVisibleNow() || _scroll is null || _smooth is null)
        {
            return;
        }

        var height = target.Bounds.Height;
        var top = target.Bounds.Y - PickerColumnPanel.EmptyRows * height + height / 2;
        if (_scroll.Offset.Y != top)
        {
            _canExitInputMode = canExitInputMode;
            _smooth.To(Snap(top), smooth);
        }
    }

    /// <summary>
    /// A scroll at rest stays on a snap point after the layout changes, as <c>scroll-snap-type: mandatory</c> keeps it: with no
    /// option for the value, the column still shows the first option at its centre (measured: <c>scrollTop</c> 19).
    /// </summary>
    private void Resnap()
    {
        if (_isColumnVisible && _scroll is not null && _smooth is { IsRunning: false })
        {
            _smooth.To(Snap(_scroll.Offset.Y), smooth: false);
        }
    }

    /// <summary>The snap position closest to <paramref name="top"/>, within what the scroller can reach.</summary>
    internal double Snap(double top)
    {
        if (_scroll is null)
        {
            return top;
        }

        var max = Math.Max(0, _scroll.Extent.Height - _scroll.Viewport.Height);
        var half = _scroll.Viewport.Height / 2;
        var best = Math.Clamp(top, 0, max);
        var bestDistance = double.PositiveInfinity;
        foreach (var centre in Panel?.SnapCentres ?? [])
        {
            var position = Math.Clamp(centre - half, 0, max);
            var distance = Math.Abs(position - top);
            if (distance < bestDistance)
            {
                best = position;
                bestDistance = distance;
            }
        }

        return best;
    }

    // ── Value ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's internal <c>setValue</c>: the value and <c>ionChange</c>, for what the user did. Nothing when the column is disabled
    /// or the value is the same.
    /// </summary>
    internal void SetValue(object? value)
    {
        if (IsColumnDisabled || Equals(Value, value))
        {
            return;
        }

        SetCurrentValue(ValueProperty, value);
        RaiseEvent(new PickerColumnChangeEventArgs(IonChangeEvent, value));
    }

    /// <summary>Ionic's <c>valueChange</c> watcher: only a visible column scrolls to its value.</summary>
    private void OnValueChanged()
    {
        if (IsVisibleNow())
        {
            ScrollActiveItemIntoView(smooth: true);
        }

        RequestRender();
    }

    /// <summary>Stencil renders again at the next frame after a prop or state changes, and then runs <c>componentDidRender</c>.</summary>
    private void RequestRender()
    {
        if (_renderFrame is not null || !_attached)
        {
            return;
        }

        _renderFrame = AvonicConfig.RequestFrame(this, () =>
        {
            _renderFrame = null;
            PseudoClasses.Set(ColumnActive, _isActive);
            ComponentDidRender();
            UpdatePeerValue();
        });
    }

    /// <summary>
    /// Ionic's <c>componentDidRender</c>: a visible column with no option for its value takes the first option's, with
    /// <c>ionChange</c>, so that the value matches what's centred.
    /// </summary>
    private void ComponentDidRender()
    {
        if (IsVisibleNow() && ActiveItem is null && Options.FirstOrDefault() is { } first && !Equals(first.Value, Value))
        {
            SetValue(first.Value);
        }
    }

    /// <summary>An option's or the column's <c>disabled</c> changed, or an option came or went: what snaps changes.</summary>
    internal void OnOptionChanged() => Panel?.InvalidateArrange();

    /// <summary>An option's <c>componentDidLoad</c>: centre the value again, once it's laid out.</summary>
    internal void OnOptionLoaded()
    {
        OnOptionChanged();
        if (_centrePosted)
        {
            return;
        }

        _centrePosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _centrePosted = false;
            ScrollActiveItemIntoView();
            if (ActiveItem is null)
            {
                Resnap();
            }
        }, DispatcherPriority.Loaded);
    }

    // ── Keys ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic's <c>onKeyDown</c>, on the focused scroller. On a mobile platform the arrows and pages are reversed.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Source != this)
        {
            base.OnKeyDown(e);
            return;
        }

        var mobile = IsMobile();
        Control? newOption = e.Key switch
        {
            Key.Down => mobile ? FindPreviousOption() : FindNextOption(),
            Key.Up => mobile ? FindNextOption() : FindPreviousOption(),
            Key.PageUp => mobile ? FindNextOption(5) : FindPreviousOption(5),
            Key.PageDown => mobile ? FindPreviousOption(5) : FindNextOption(5),
            Key.Home => Options.FirstOrDefault(),
            Key.End => Options.LastOrDefault(),
            _ => null,
        };

        if (newOption is null)
        {
            base.OnKeyDown(e);
            return;
        }

        SetValue((newOption as AvonicPickerColumnOption)?.Value);
        e.Handled = true;
    }

    /// <summary>
    /// Ionic's <c>findNextOption</c>: the enabled option <paramref name="stride"/> after the active one, or the next enabled after
    /// that; failing both, the last element, whatever it is.
    /// </summary>
    internal Control? FindNextOption(int stride = 1) => FindOption(stride, +1);

    /// <summary>Ionic's <c>findPreviousOption</c>, the other way.</summary>
    internal Control? FindPreviousOption(int stride = 1) => FindOption(stride, -1);

    private Control? FindOption(int stride, int step)
    {
        if (ActiveItem is not { } active)
        {
            return null;
        }

        Control last = active;
        for (var i = IndexFromContainer(active) + step; i >= 0 && i < ItemCount; i += step)
        {
            if (ContainerFromIndex(i) is not { } node)
            {
                continue;
            }

            if (stride > 0)
            {
                stride--;
            }

            if (node is AvonicPickerColumnOption { IsEnabled: true } && stride == 0)
            {
                return node;
            }

            last = node;
        }

        return last;
    }

    // ── Input mode, for the picker ───────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's <c>exitInputMode</c> on the column: tells the picker, and drops <c>picker-column-active</c> at once, though the state
    /// waits for the scroll to end.
    /// </summary>
    private void ExitInputMode()
    {
        if (_parent is null)
        {
            return;
        }

        _parent.ExitInputMode();
        PseudoClasses.Set(ColumnActive, false);
    }

    /// <summary>Ionic's <c>inputModeChange</c>: whether this column is the one taking digits.</summary>
    private void OnInputModeChange(object? sender, PickerInputModeChange e)
    {
        if (!NumericInput)
        {
            return;
        }

        var isColumnActive = e.InputModeColumn is null || e.InputModeColumn == this;
        SetInputModeActive(e.UseInputMode && isColumnActive);
    }

    /// <summary>Ionic's <c>setInputModeActive</c>: not in the middle of a scroll, which a render would make jump.</summary>
    private void SetInputModeActive(bool state)
    {
        if (_isScrolling)
        {
            _scrollEndCallback = () => SetIsActive(state);
            return;
        }

        SetIsActive(state);
    }

    private void SetIsActive(bool state)
    {
        if (_isActive == state)
        {
            return;
        }

        _isActive = state;
        RequestRender();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new PickerColumnPeer(this);

    /// <summary>The render's <c>aria-valuetext</c>, which a screen reader announces when it changes.</summary>
    private void UpdatePeerValue()
    {
        var now = ValueText();
        if (now == _peerValue)
        {
            return;
        }

        var old = _peerValue;
        _peerValue = now;
        if (ControlAutomationPeer.FromElement(this) is { } peer)
        {
            peer.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, old, now);
        }
    }

    /// <summary>Ionic's <c>getOptionValueText</c>: the active option's <c>aria-label</c>, or its text.</summary>
    internal string ValueText() => ActiveItem is { } item
        ? AutomationProperties.GetName(item) is { Length: > 0 } label ? label : AvonicPicker.OptionText(item)
        : string.Empty;

    /// <summary>
    /// <c>.picker-opts</c>' <c>role="slider"</c>, named by <c>aria-label</c> or "Select a value", with the active option's text as
    /// its <c>aria-valuetext</c>.
    /// </summary>
    private sealed class PickerColumnPeer(AvonicPickerColumn owner) : ControlAutomationPeer(owner), IValueProvider
    {
        private AvonicPickerColumn Column => (AvonicPickerColumn)Owner;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

        protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : "Select a value";

        public bool IsReadOnly => true;

        public string? Value => Column.ValueText();

        public void SetValue(string? value) => throw new InvalidOperationException("The column's value is set by choosing an option.");
    }
}
