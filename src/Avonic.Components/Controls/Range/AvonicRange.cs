using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Gestures;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-range</c>: a slider, with one knob or two, moved by a drag or a tap on the bar, or the arrow keys on a knob. The look
/// is in the theme. See <c>docs/components/ion-range/README.md</c>.
/// </summary>
/// <remarks>
/// The state and its methods are Ionic's: <c>ratioA</c> and <c>ratioB</c> are where the knobs are, and the value follows them as the
/// user moves one, or they follow the value when it's set. The drag is <see cref="Gesture"/> on <c>PART_Slider</c>, with Ionic's
/// numbers, and a tap is the slider's release for a press the gesture didn't capture. RTL is the range's
/// <see cref="Visual.FlowDirection"/>: the slider is mirrored, so a press read in its coordinates already counts from the right.
/// </remarks>
[TemplatePart("PART_Slider", typeof(RangeSlider))]
[TemplatePart("PART_Bar", typeof(Control))]
[TemplatePart("PART_BarActive", typeof(Control))]
[TemplatePart("PART_Ticks", typeof(RangeTicks))]
[TemplatePart("PART_KnobHandleA", typeof(RangeKnobHandle))]
[TemplatePart("PART_KnobHandleB", typeof(RangeKnobHandle))]
[TemplatePart("PART_Label", typeof(Control))]
[PseudoClasses(":in-item", ":range-disabled", ":range-dual-knobs", ":range-pressed", ":range-has-pin")]
[PseudoClasses(":range-pressed-a", ":range-pressed-b", ":range-pressed-lower", ":range-pressed-upper")]
[PseudoClasses(":range-item-start-adjustment", ":range-item-end-adjustment", ":range-value-min", ":range-value-max")]
[PseudoClasses(":label-placement-end", ":label-placement-fixed", ":label-placement-stacked", ":label-placement-start")]
[PseudoClasses(":label-text-wrapper-hidden", ":start-icon", ":end-icon")]
public class AvonicRange : TemplatedControl, IItemControl
{
    /// <summary><c>createGesture</c>'s <c>threshold</c>: a drag decides 10 px from the press, as it might be a scroll.</summary>
    public const double GestureThreshold = 10;

    /// <summary><c>createGesture</c>'s <c>gesturePriority</c>.</summary>
    public const int GesturePriority = 100;

    // ── Ionic's props ────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>activeBarStart</c>: where the active bar starts, with one knob. Clamped into <see cref="Min"/> to <see cref="Max"/>.</summary>
    public static readonly StyledProperty<double?> ActiveBarStartProperty =
        AvaloniaProperty.Register<AvonicRange, double?>(nameof(ActiveBarStart));

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicRange>();

    /// <summary>Ionic <c>debounce</c>: milliseconds to wait after each change before <see cref="IonInput"/>.</summary>
    public static readonly StyledProperty<int?> DebounceProperty =
        AvaloniaProperty.Register<AvonicRange, int?>(nameof(Debounce));

    /// <summary>Ionic <c>dualKnobs</c>: two knobs, and a <see cref="RangeValue"/> pair.</summary>
    public static readonly StyledProperty<bool> DualKnobsProperty =
        AvaloniaProperty.Register<AvonicRange, bool>(nameof(DualKnobs));

    /// <summary>Ionic <c>label</c>: plain label text, which beats <see cref="LabelContent"/>.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<AvonicRange, string?>(nameof(Label));

    /// <summary>Ionic <c>labelPlacement</c>.</summary>
    public static readonly StyledProperty<RangeLabelPlacement> LabelPlacementProperty =
        RangePanel.LabelPlacementProperty.AddOwner<AvonicRange>();

    /// <summary>Ionic <c>max</c>. One that isn't a finite number is 100.</summary>
    public static readonly StyledProperty<double> MaxProperty =
        AvaloniaProperty.Register<AvonicRange, double>(nameof(Max), 100, coerce: (_, value) => double.IsFinite(value) ? value : 100);

    /// <summary>Ionic <c>min</c>. One that isn't a finite number is 0.</summary>
    public static readonly StyledProperty<double> MinProperty =
        AvaloniaProperty.Register<AvonicRange, double>(nameof(Min), 0, coerce: (_, value) => double.IsFinite(value) ? value : 0);

    /// <summary>Ionic <c>pin</c>: a pin with the value over the knob while it's pressed or focused.</summary>
    public static readonly StyledProperty<bool> PinProperty =
        AvaloniaProperty.Register<AvonicRange, bool>(nameof(Pin));

    /// <summary>Ionic <c>pinFormatter</c>: the pin's text for a value. By default JavaScript's <c>Math.round</c>.</summary>
    public static readonly StyledProperty<Func<double, string>> PinFormatterProperty =
        AvaloniaProperty.Register<AvonicRange, Func<double, string>>(nameof(PinFormatter), DefaultPinFormatter);

    /// <summary>Ionic <c>snaps</c>: the knob snaps to a step as it moves.</summary>
    public static readonly StyledProperty<bool> SnapsProperty =
        AvaloniaProperty.Register<AvonicRange, bool>(nameof(Snaps));

    /// <summary>Ionic <c>step</c>. One that isn't a finite number is 1.</summary>
    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<AvonicRange, double>(nameof(Step), 1, coerce: (_, value) => double.IsFinite(value) ? value : 1);

    /// <summary>Ionic <c>ticks</c>: a tick per step, with <see cref="Snaps"/>.</summary>
    public static readonly StyledProperty<bool> TicksProperty =
        AvaloniaProperty.Register<AvonicRange, bool>(nameof(Ticks), true);

    /// <summary>Ionic <c>value</c>. Two-way by default, as the range sets it itself.</summary>
    public static readonly StyledProperty<RangeValue> ValueProperty =
        AvaloniaProperty.Register<AvonicRange, RangeValue>(nameof(Value), new RangeValue(0), defaultBindingMode: BindingMode.TwoWay);

    // ── Slots ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic's <c>start</c> slot: before the slider.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicRange, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicRange, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic's <c>end</c> slot: after the slider.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicRange, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicRange, IDataTemplate?>(nameof(EndContentTemplate));

    /// <summary>Ionic's <c>label</c> slot, shown while <see cref="Label"/> is null.</summary>
    public static readonly StyledProperty<object?> LabelContentProperty =
        AvaloniaProperty.Register<AvonicRange, object?>(nameof(LabelContent));

    public static readonly StyledProperty<IDataTemplate?> LabelContentTemplateProperty =
        AvaloniaProperty.Register<AvonicRange, IDataTemplate?>(nameof(LabelContentTemplate));

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionChange</c>: the user committed a value, on release or a key. Not when <see cref="Value"/> is set in code.</summary>
    public static readonly RoutedEvent<RangeChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicRange, RangeChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionInput</c>: the value changed, as the user drags, or in code, after <see cref="Debounce"/>.</summary>
    public static readonly RoutedEvent<RangeChangeEventArgs> IonInputEvent =
        RoutedEvent.Register<AvonicRange, RangeChangeEventArgs>(nameof(IonInput), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionKnobMoveStart</c>.</summary>
    public static readonly RoutedEvent<RangeKnobMoveEventArgs> IonKnobMoveStartEvent =
        RoutedEvent.Register<AvonicRange, RangeKnobMoveEventArgs>(nameof(IonKnobMoveStart), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionKnobMoveEnd</c>.</summary>
    public static readonly RoutedEvent<RangeKnobMoveEventArgs> IonKnobMoveEndEvent =
        RoutedEvent.Register<AvonicRange, RangeKnobMoveEventArgs>(nameof(IonKnobMoveEnd), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionFocus</c>: focus entered the range.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicRange, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>: focus left the range.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicRange, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    // ── For the theme ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>margin-inline-start: calc(0px - var(--knob-handle-size) / 2)</c>, with the handle twice the knob: the value is the knob's
    /// size, and the margin is minus that, at the start.
    /// </summary>
    public static readonly IMultiValueConverter KnobHandleMarginConverter =
        new FuncMultiValueConverter<object?, Thickness>(values =>
            new Thickness(values.FirstOrDefault() is double size && double.IsFinite(size) ? -size : 0, 0, 0, 0));

    private RangeSlider? _slider;
    private Control? _barActive;
    private RangeTicks? _ticks;
    private RangeKnobHandle? _handleA;
    private RangeKnobHandle? _handleB;
    private Gesture? _gesture;
    private AvonicContent? _content;
    private IDisposable? _debounce;

    private double _ratioA;
    private double _ratioB;
    private RangeKnob? _pressedKnob;
    private RangeKnob? _focusedKnob;
    private bool _hasFocus;
    private bool _noUpdate;
    private bool _inItem;
    private bool _loaded;
    private int? _places;
    private (double Min, double Max, double Step, bool Show) _tickKey;
    private double[] _tickRatios = [];
    private bool _captured;
    private bool _initialContentScrollY = true;
    private bool _contentScrollDisabled;

    static AvonicRange()
    {
        // user-select: none, and the knob's halo hangs over the slider, which Ionic doesn't clip.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicRange>(false);
    }

    public AvonicRange()
    {
        // onFocusin and onFocusout are on the host, so focus on anything inside counts, the slots' content too.
        AddHandler(GotFocusEvent, (_, _) => OnFocus(), RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(LostFocusEvent, (_, e) =>
        {
            // A knob's blur is onKnobBlur's: Chromium raises no focusout on the host as focus moves inside the shadow root.
            if (e.Source is not RangeKnobHandle)
            {
                OnBlur();
            }
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        UpdateRatio();
        Render();
    }

    public double? ActiveBarStart
    {
        get => GetValue(ActiveBarStartProperty);
        set => SetValue(ActiveBarStartProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public int? Debounce
    {
        get => GetValue(DebounceProperty);
        set => SetValue(DebounceProperty, value);
    }

    public bool DualKnobs
    {
        get => GetValue(DualKnobsProperty);
        set => SetValue(DualKnobsProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public RangeLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public double Max
    {
        get => GetValue(MaxProperty);
        set => SetValue(MaxProperty, value);
    }

    public double Min
    {
        get => GetValue(MinProperty);
        set => SetValue(MinProperty, value);
    }

    public bool Pin
    {
        get => GetValue(PinProperty);
        set => SetValue(PinProperty, value);
    }

    public Func<double, string> PinFormatter
    {
        get => GetValue(PinFormatterProperty);
        set => SetValue(PinFormatterProperty, value);
    }

    public bool Snaps
    {
        get => GetValue(SnapsProperty);
        set => SetValue(SnapsProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public bool Ticks
    {
        get => GetValue(TicksProperty);
        set => SetValue(TicksProperty, value);
    }

    public RangeValue Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public object? StartContent
    {
        get => GetValue(StartContentProperty);
        set => SetValue(StartContentProperty, value);
    }

    public IDataTemplate? StartContentTemplate
    {
        get => GetValue(StartContentTemplateProperty);
        set => SetValue(StartContentTemplateProperty, value);
    }

    public object? EndContent
    {
        get => GetValue(EndContentProperty);
        set => SetValue(EndContentProperty, value);
    }

    public IDataTemplate? EndContentTemplate
    {
        get => GetValue(EndContentTemplateProperty);
        set => SetValue(EndContentTemplateProperty, value);
    }

    public object? LabelContent
    {
        get => GetValue(LabelContentProperty);
        set => SetValue(LabelContentProperty, value);
    }

    public IDataTemplate? LabelContentTemplate
    {
        get => GetValue(LabelContentTemplateProperty);
        set => SetValue(LabelContentTemplateProperty, value);
    }

    public event EventHandler<RangeChangeEventArgs>? IonChange
    {
        add => AddHandler(IonChangeEvent, value);
        remove => RemoveHandler(IonChangeEvent, value);
    }

    public event EventHandler<RangeChangeEventArgs>? IonInput
    {
        add => AddHandler(IonInputEvent, value);
        remove => RemoveHandler(IonInputEvent, value);
    }

    public event EventHandler<RangeKnobMoveEventArgs>? IonKnobMoveStart
    {
        add => AddHandler(IonKnobMoveStartEvent, value);
        remove => RemoveHandler(IonKnobMoveStartEvent, value);
    }

    public event EventHandler<RangeKnobMoveEventArgs>? IonKnobMoveEnd
    {
        add => AddHandler(IonKnobMoveEndEvent, value);
        remove => RemoveHandler(IonKnobMoveEndEvent, value);
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

    /// <summary>JavaScript's <c>Math.round</c>, which rounds a half up, as text.</summary>
    private static string DefaultPinFormatter(double value) => Math.Floor(value + 0.5).ToString(CultureInfo.InvariantCulture);

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_slider is not null)
        {
            _slider.RemoveHandler(PointerReleasedEvent, OnSliderReleased);
            _slider.RemoveHandler(PointerPressedEvent, OnSliderPressed);
        }

        foreach (var handle in new[] { _handleA, _handleB })
        {
            if (handle is not null)
            {
                handle.KeyDown -= OnKnobKeyDown;
                handle.GotFocus -= OnKnobGotFocus;
                handle.LostFocus -= OnKnobLostFocus;
            }
        }

        _slider = e.NameScope.Find<RangeSlider>("PART_Slider");
        _barActive = e.NameScope.Find<Control>("PART_BarActive");
        _ticks = e.NameScope.Find<RangeTicks>("PART_Ticks");
        _handleA = e.NameScope.Find<RangeKnobHandle>("PART_KnobHandleA");
        _handleB = e.NameScope.Find<RangeKnobHandle>("PART_KnobHandleB");

        if (_slider is not null)
        {
            _slider.AddHandler(PointerPressedEvent, OnSliderPressed, RoutingStrategies.Bubble, handledEventsToo: true);
            _slider.AddHandler(PointerReleasedEvent, OnSliderReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        foreach (var handle in new[] { _handleA, _handleB })
        {
            if (handle is not null)
            {
                // ion-activatable: tap-click's :activated, and :ion-hover.
                Activatable.SetIsEnabled(handle, true);
                handle.KeyDown += OnKnobKeyDown;
                handle.GotFocus += OnKnobGotFocus;
                handle.LostFocus += OnKnobLostFocus;
            }
        }

        SetupGesture();
        Render();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // connectedCallback: updateRatio, activeBarStartChanged, then the gesture and the closest ion-content. Before the first
        // load the knobs start at 0, as Ionic's do, whatever order the props were set in, so that A is the lower of two.
        if (!_loaded)
        {
            (_ratioA, _ratioB) = (0, 0);
        }

        UpdateRatio();
        ActiveBarStartChanged();
        SetupGesture();
        _content = this.FindAncestorOfType<AvonicContent>();

        // hostContext('ion-item'), at render. Read again here, as a moved range is attached again.
        _inItem = this.FindAncestorOfType<AvonicItem>() is not null;
        Render();

        // componentDidLoad: from here, a change of the value is a watch's.
        _loaded = true;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // disconnectedCallback. A drag that's cut off gives the content its scroll back.
        _gesture?.Destroy();
        _gesture = null;
        RestoreContentScroll();
        _pressedKnob = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary><c>setupGesture</c>: on the slider, once it exists and the range is in the tree. Then <c>disabledChanged</c>.</summary>
    private void SetupGesture()
    {
        _gesture?.Destroy();
        _gesture = null;
        if (_slider is null || !this.IsAttachedToVisualTree())
        {
            return;
        }

        _gesture = Gesture.Create(new GestureConfig
        {
            Element = _slider,
            GestureName = "range",
            GesturePriority = GesturePriority,
            Threshold = GestureThreshold,
            OnStart = _ =>
            {
                _captured = true;
                OnStart();
            },
            OnMove = OnMove,
            OnEnd = OnEnd,
        });
        _gesture.Enable(IsEffectivelyEnabled);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsEffectivelyEnabledProperty)
        {
            // disabledChanged
            _gesture?.Enable(IsEffectivelyEnabled);
        }
        else if (change.Property == ValueProperty)
        {
            // valueChanged. A watch isn't called for the value a component loads with.
            if (_loaded)
            {
                EmitInput();
            }

            // While the knobs set the value, the caller renders once they've moved.
            if (!_noUpdate)
            {
                UpdateRatio();
                Render();
            }
        }
        else if (change.Property == MinProperty || change.Property == MaxProperty)
        {
            // minChanged, maxChanged
            _places = null;
            if (!_noUpdate)
            {
                UpdateRatio();
            }

            Render();
        }
        else if (change.Property == ActiveBarStartProperty)
        {
            // A watch, which isn't called before load: connectedCallback checks it once every prop is set.
            if (_loaded)
            {
                ActiveBarStartChanged();
            }

            Render();
        }
        else if (change.Property == DebounceProperty)
        {
            // debounceChanged: back to the original emitter. A pending one still fires, as Ionic's timer does.
            _debounce = null;
        }
        else if (change.Property == StepProperty)
        {
            _places = null;
            Render();
        }
        else if (change.Property == IsEnabledProperty || change.Property == DualKnobsProperty || change.Property == PinProperty
                 || change.Property == PinFormatterProperty || change.Property == SnapsProperty
                 || change.Property == TicksProperty || change.Property == LabelProperty || change.Property == LabelContentProperty
                 || change.Property == LabelPlacementProperty || change.Property == StartContentProperty
                 || change.Property == EndContentProperty)
        {
            Render();
        }
    }

    /// <summary><c>activeBarStartChanged</c>: one outside <see cref="Min"/> to <see cref="Max"/> is moved to the nearer end.</summary>
    private void ActiveBarStartChanged()
    {
        if (ActiveBarStart is not { } start)
        {
            return;
        }

        if (start > Max)
        {
            SetCurrentValue(ActiveBarStartProperty, Max);
        }
        else if (start < Min)
        {
            SetCurrentValue(ActiveBarStartProperty, Min);
        }
    }

    // ── Value and ratios ─────────────────────────────────────────────────────────────────────

    private double ClampBounds(double value) => Math.Max(Min, Math.Min(value, Max));

    /// <summary><c>ensureValueInBounds</c>.</summary>
    private RangeValue EnsureValueInBounds(RangeValue value) =>
        DualKnobs ? new RangeValue(ClampBounds(value.Lower), ClampBounds(value.Upper)) : new RangeValue(ClampBounds(value.Number));

    /// <summary><c>getValue</c>: a pair for two knobs, a number for one.</summary>
    private RangeValue GetRangeValue()
    {
        var value = Value;
        return DualKnobs ? new RangeValue(value.Lower, value.Upper) : new RangeValue(value.Number);
    }

    private double ValA => RatioToValue(_ratioA);

    private double ValB => RatioToValue(_ratioB);

    /// <summary>The most decimals of <see cref="Min"/>, <see cref="Max"/> and <see cref="Step"/>, which a value is rounded to.</summary>
    private int Places => _places ??= Math.Max(DecimalPlaces(Min), Math.Max(DecimalPlaces(Max), DecimalPlaces(Step)));

    private double RatioLower => DualKnobs
        ? Math.Min(_ratioA, _ratioB)
        : ActiveBarStart is { } start ? ValueToRatio(start, Min, Max) : 0;

    private double RatioUpper => DualKnobs ? Math.Max(_ratioA, _ratioB) : _ratioA;

    /// <summary><c>updateRatio</c>: two knobs take the lower and upper in whichever way moves them least, so they don't swap.</summary>
    private void UpdateRatio()
    {
        var value = GetRangeValue();
        if (DualKnobs)
        {
            var lowerRatio = ValueToRatio(value.Lower, Min, Max);
            var upperRatio = ValueToRatio(value.Upper, Min, Max);
            if (Math.Abs(_ratioA - lowerRatio) + Math.Abs(_ratioB - upperRatio)
                <= Math.Abs(_ratioA - upperRatio) + Math.Abs(_ratioB - lowerRatio))
            {
                _ratioA = lowerRatio;
                _ratioB = upperRatio;
            }
            else
            {
                _ratioA = upperRatio;
                _ratioB = lowerRatio;
            }
        }
        else
        {
            _ratioA = ValueToRatio(value.Number, Min, Max);
        }
    }

    /// <summary><c>updateValue</c>: the value from the knobs, without moving them back.</summary>
    private void UpdateValue()
    {
        _noUpdate = true;
        try
        {
            var (valA, valB) = (ValA, ValB);
            SetCurrentValue(ValueProperty, DualKnobs ? new RangeValue(Math.Min(valA, valB), Math.Max(valA, valB)) : new RangeValue(valA));
        }
        finally
        {
            _noUpdate = false;
        }
    }

    /// <summary>
    /// <c>emitValueChange</c>: the value, in bounds, then <c>ionChange</c>. Ionic sets a new object for two knobs, which its watch
    /// always sees, and puts the knobs on their values; a number that's the same isn't a change.
    /// </summary>
    private void EmitValueChange()
    {
        var bounded = EnsureValueInBounds(Value);
        if (bounded != Value)
        {
            SetCurrentValue(ValueProperty, bounded);
        }
        else if (DualKnobs)
        {
            UpdateRatio();
            Render();
        }

        RaiseEvent(new RangeChangeEventArgs(IonChangeEvent, this, Value));
    }

    /// <summary><c>ionInput.emit</c>, through <c>debounceEvent</c> when <see cref="Debounce"/> is set.</summary>
    private void EmitInput()
    {
        var args = new RangeChangeEventArgs(IonInputEvent, this, Value);
        if (Debounce is not { } wait)
        {
            RaiseEvent(args);
            return;
        }

        _debounce?.Dispose();
        _debounce = AvonicConfig.Schedule(() => RaiseEvent(args), TimeSpan.FromMilliseconds(Math.Max(0, wait)));
    }

    /// <summary><c>ratioToValue</c>: Ionic adds <c>min</c> only with a step, and rounds to the decimals of the three.</summary>
    private double RatioToValue(double ratio)
    {
        var (min, max, step) = (Min, Max, Step);
        var value = (max - min) * ratio;
        if (step > 0)
        {
            // Math.round: a half goes up, toward positive infinity.
            value = Math.Floor(value / step + 0.5) * step + min;
        }

        return RoundToMaxDecimalPlaces(Math.Max(min, Math.Min(value, max)), Places);
    }

    /// <summary><c>valueToRatio</c>.</summary>
    internal static double ValueToRatio(double value, double min, double max) =>
        Math.Max(0, Math.Min((value - min) / (max - min), 1));

    /// <summary><c>roundToMaxDecimalPlaces</c>: <c>toFixed</c> to the most decimals any of the references has.</summary>
    private static double RoundToMaxDecimalPlaces(double value, int places)
    {
        if (!double.IsFinite(value))
        {
            return 0;
        }

        return double.Parse(value.ToString("F" + places.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    /// <summary><c>getDecimalPlaces</c>: the digits after the point in the shortest text for the number, as JavaScript's.</summary>
    private static int DecimalPlaces(double value)
    {
        if (!double.IsFinite(value) || value % 1 == 0)
        {
            return 0;
        }

        // .NET writes 1E-05 where JavaScript writes 0.00001, down to 1e-6: the digits, less the exponent.
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        var exponentAt = text.IndexOf('E');
        var mantissa = exponentAt < 0 ? text : text[..exponentAt];
        var exponent = exponentAt < 0 ? 0 : int.Parse(text[(exponentAt + 1)..], CultureInfo.InvariantCulture);
        var point = mantissa.IndexOf('.');
        var digits = point < 0 ? 0 : mantissa.Length - point - 1;
        return Math.Max(0, digits - exponent);
    }

    // ── The gesture and taps ─────────────────────────────────────────────────────────────────

    /// <summary><c>onStart</c>.</summary>
    private void OnStart() => RaiseEvent(new RangeKnobMoveEventArgs(IonKnobMoveStartEvent, this, EnsureValueInBounds(Value)));

    /// <summary><c>onMove</c>: the first move picks the knob and turns the content's scroll off.</summary>
    private void OnMove(GestureDetail detail)
    {
        var x = SliderX(detail.CurrentX, detail.CurrentY);
        if (_pressedKnob is null)
        {
            DisableContentScroll();
            SetPressedKnob(x);
        }

        Update(x);
    }

    /// <summary><c>onEnd</c>, for the gesture's release.</summary>
    private void OnEnd(GestureDetail detail) => OnEnd(SliderX(detail.CurrentX, detail.CurrentY));

    /// <summary><c>onEnd</c>: the knob goes to the release, and the value is committed.</summary>
    private void OnEnd(double x)
    {
        if (_pressedKnob is null)
        {
            SetPressedKnob(x);
        }

        RestoreContentScroll();
        Update(x);
        _pressedKnob = null;
        EmitValueChange();
        RaiseEvent(new RangeKnobMoveEventArgs(IonKnobMoveEndEvent, this, EnsureValueInBounds(Value)));
        Render();
    }

    private void OnSliderPressed(object? sender, PointerPressedEventArgs e) => _captured = false;

    /// <summary>
    /// <c>onPointerUp</c>: a release the gesture didn't capture, a tap or a move under its threshold, moves the knob there. Ionic tells
    /// them apart by <c>pressedKnob</c>, which is set by the first move after a capture; here by the capture itself, so the order
    /// in which the gesture and this hear the release doesn't matter.
    /// </summary>
    private void OnSliderReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_captured || _pressedKnob is not null || _slider is null || !IsEffectivelyEnabled)
        {
            _captured = false;
            return;
        }

        OnStart();
        OnEnd(e.GetPosition(_slider).X);
    }

    /// <summary>A position in the top level, as the gesture gives it, in the slider's own coordinates, which RTL mirrors.</summary>
    private double SliderX(double x, double y)
    {
        if (_slider is null || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return 0;
        }

        return topLevel.TranslatePoint(new Point(x, y), _slider)?.X ?? 0;
    }

    private double RatioAt(double x)
    {
        var width = _slider?.Bounds.Width ?? 0;
        return width > 0 ? Math.Max(0, Math.Min(x / width, 1)) : 0;
    }

    /// <summary><c>update</c>: the pressed knob to the pointer, snapped if <see cref="Snaps"/>, and the value from it.</summary>
    private void Update(double x)
    {
        var ratio = RatioAt(x);
        if (Snaps)
        {
            ratio = ValueToRatio(RatioToValue(ratio), Min, Max);
        }

        if (_pressedKnob == RangeKnob.A)
        {
            _ratioA = ratio;
        }
        else
        {
            _ratioB = ratio;
        }

        UpdateValue();
        Render();
    }

    /// <summary><c>setPressedKnob</c>: the nearer knob, and B on a tie.</summary>
    private void SetPressedKnob(double x)
    {
        var ratio = RatioAt(x);
        _pressedKnob = !DualKnobs || Math.Abs(_ratioA - ratio) < Math.Abs(_ratioB - ratio) ? RangeKnob.A : RangeKnob.B;
    }

    /// <summary><c>disableContentScrollY</c>, once a drag starts.</summary>
    private void DisableContentScroll()
    {
        if (_content is null || _contentScrollDisabled)
        {
            return;
        }

        _initialContentScrollY = _content.ScrollY;
        _content.SetCurrentValue(AvonicContent.ScrollYProperty, false);
        _contentScrollDisabled = true;
    }

    /// <summary><c>resetContentScrollY</c>, for a drag that turned it off.</summary>
    private void RestoreContentScroll()
    {
        if (_content is null || !_contentScrollDisabled)
        {
            return;
        }

        _content.SetCurrentValue(AvonicContent.ScrollYProperty, _initialContentScrollY);
        _contentScrollDisabled = false;
    }

    // ── Keys and focus ───────────────────────────────────────────────────────────────────────

    private void OnKnobKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not RangeKnobHandle handle || e.Handled)
        {
            return;
        }

        // ev.preventDefault(); ev.stopPropagation()
        switch (e.Key)
        {
            case Key.Left or Key.Down:
                HandleKeyboard(handle.Knob, false);
                e.Handled = true;
                break;
            case Key.Right or Key.Up:
                HandleKeyboard(handle.Knob, true);
                e.Handled = true;
                break;
        }
    }

    /// <summary><c>handleKeyboard</c>: a step of the ratio, which the value then rounds.</summary>
    internal void HandleKeyboard(RangeKnob knob, bool isIncrease)
    {
        var step = Step > 0 ? Step : 1;
        step /= Max - Min;
        if (!isIncrease)
        {
            step *= -1;
        }

        if (knob == RangeKnob.A)
        {
            _ratioA = Math.Max(0, Math.Min(_ratioA + step, 1));
        }
        else
        {
            _ratioB = Math.Max(0, Math.Min(_ratioB + step, 1));
        }

        RaiseEvent(new RangeKnobMoveEventArgs(IonKnobMoveStartEvent, this, EnsureValueInBounds(Value)));
        UpdateValue();
        Render();
        EmitValueChange();
        RaiseEvent(new RangeKnobMoveEventArgs(IonKnobMoveEndEvent, this, EnsureValueInBounds(Value)));
    }

    /// <summary>
    /// <c>onKnobFocus</c>: a knob focused by a press isn't shown focused, only by the keyboard or in code. Ionic tells them apart
    /// by a flag it sets on <c>pointerdown</c>; Avalonia says how focus came.
    /// </summary>
    private void OnKnobGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is not RangeKnobHandle handle || !ReferenceEquals(e.Source, handle))
        {
            return;
        }

        _focusedKnob = e.NavigationMethod == NavigationMethod.Pointer ? null : handle.Knob;
        OnFocus();
        Render();
    }

    /// <summary><c>onKnobBlur</c>: after a turn, unless focus went to the other knob.</summary>
    private void OnKnobLostFocus(object? sender, FocusChangedEventArgs e)
    {
        AvonicConfig.Schedule(() =>
        {
            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            if (focused is RangeKnobHandle knob && (knob == _handleA || knob == _handleB))
            {
                return;
            }

            OnBlur();
        }, TimeSpan.Zero);
    }

    /// <summary><c>onFocus</c>.</summary>
    private void OnFocus()
    {
        if (!_hasFocus)
        {
            _hasFocus = true;
            RaiseEvent(new RoutedEventArgs(IonFocusEvent, this));
        }
    }

    /// <summary><c>onBlur</c>.</summary>
    private void OnBlur()
    {
        if (_hasFocus)
        {
            _hasFocus = false;
            _focusedKnob = null;
            RaiseEvent(new RoutedEventArgs(IonBlurEvent, this));
            Render();
        }
    }

    // ── Render ───────────────────────────────────────────────────────────────────────────────

    /// <summary><c>hasLabel</c>: <see cref="Label"/>, even empty, or something in the label slot.</summary>
    private bool HasLabel => Label is not null || LabelContent is not null;

    /// <summary><c>getKnobPosition</c>: on a tie A is the lower.</summary>
    private bool IsLower(RangeKnob knob) =>
        !DualKnobs || (_ratioA == _ratioB ? knob == RangeKnob.A : knob == RangeKnob.A ? _ratioA < _ratioB : _ratioB < _ratioA);

    /// <summary><c>render</c> and <c>renderRangeSlider</c>: the host's classes, the parts' places, the knobs' state.</summary>
    private void Render()
    {
        var dual = DualKnobs;
        var (min, max) = (Min, Max);
        var (valA, valB) = (ValA, ValB);
        var placement = LabelPlacement;
        var hasLabel = HasLabel;
        var pressed = _pressedKnob;

        // render's host classes.
        var hasStartContent = (hasLabel && placement is RangeLabelPlacement.Start or RangeLabelPlacement.Fixed) || StartContent is not null;
        var hasEndContent = (hasLabel && placement == RangeLabelPlacement.End) || EndContent is not null;
        PseudoClasses.Set(":in-item", _inItem);
        PseudoClasses.Set(":range-disabled", !IsEnabled);
        PseudoClasses.Set(":range-dual-knobs", dual);
        PseudoClasses.Set(":range-pressed", pressed is not null);
        PseudoClasses.Set(":range-pressed-a", dual && pressed == RangeKnob.A);
        PseudoClasses.Set(":range-pressed-b", dual && pressed == RangeKnob.B);
        PseudoClasses.Set(":range-pressed-lower", dual && pressed is { } p && IsLower(p));
        PseudoClasses.Set(":range-pressed-upper", dual && pressed is { } q && !IsLower(q));
        PseudoClasses.Set(":range-has-pin", Pin);
        PseudoClasses.Set(":label-placement-end", placement == RangeLabelPlacement.End);
        PseudoClasses.Set(":label-placement-fixed", placement == RangeLabelPlacement.Fixed);
        PseudoClasses.Set(":label-placement-stacked", placement == RangeLabelPlacement.Stacked);
        PseudoClasses.Set(":label-placement-start", placement == RangeLabelPlacement.Start);
        PseudoClasses.Set(":range-item-start-adjustment", _inItem && !hasStartContent);
        PseudoClasses.Set(":range-item-end-adjustment", _inItem && !hasEndContent);
        PseudoClasses.Set(":range-value-min", dual ? valA == min || valB == min : valA == min);
        PseudoClasses.Set(":range-value-max", dual ? valA == max || valB == max : valA == max);
        PseudoClasses.Set(":label-text-wrapper-hidden", !hasLabel);

        // ::slotted(ion-icon[slot]) { font-size: 24px }, for what's directly in each slot.
        PseudoClasses.Set(":start-icon", StartContent is AvonicIcon);
        PseudoClasses.Set(":end-icon", EndContent is AvonicIcon);

        // The active bar: from the lower ratio to the upper, or with one knob below activeBarStart, from the knob to it.
        var (ratioLower, ratioUpper) = (Finite(RatioLower), Finite(RatioUpper));
        var (barStart, barEnd) = (ratioLower, 1 - ratioUpper);
        if (!dual && valA < (ActiveBarStart ?? min))
        {
            (barStart, barEnd) = (ratioUpper, 1 - ratioLower);
        }

        if (_barActive is not null)
        {
            RangeSlider.SetStart(_barActive, barStart);
            RangeSlider.SetEnd(_barActive, barEnd);
        }

        // Ticks: one per step from min to max, with snaps and ticks. Ionic's loop never ends with a step of 0.
        if (_ticks is not null)
        {
            var key = (min, max, Step, Snaps && Ticks && Step > 0);
            if (key != _tickKey)
            {
                _tickKey = key;
                _tickRatios = key.Item4 ? TickRatios(min, max, Step) : [];
            }

            var (low, high) = (Math.Min(ratioLower, ratioUpper), Math.Max(ratioLower, ratioUpper));
            var ticks = new RangeTick[_tickRatios.Length];
            for (var i = 0; i < ticks.Length; i++)
            {
                var ratio = _tickRatios[i];
                ticks[i] = new RangeTick(ratio, ratio >= low && ratio <= high);
            }

            _ticks.Items = ticks;
        }

        RenderKnob(_handleA, RangeKnob.A, Finite(_ratioA), valA);
        RenderKnob(_handleB, RangeKnob.B, Finite(_ratioB), valB);
    }

    /// <summary>Where the ticks are: one per step from min to max, as Ionic's loop adds them up.</summary>
    private static double[] TickRatios(double min, double max, double step)
    {
        var ratios = new List<double>();
        for (var value = min; value <= max; value += step)
        {
            ratios.Add(ValueToRatio(value, min, max));

            // A step too small to change a value this large would loop for ever, as Ionic's does.
            if (value + step == value)
            {
                break;
            }
        }

        return [.. ratios];
    }

    /// <summary>A ratio of NaN, from a <see cref="Min"/> equal to <see cref="Max"/>, is an invalid <c>left</c>, which CSS ignores.</summary>
    private static double Finite(double ratio) => double.IsFinite(ratio) ? ratio : 0;

    /// <summary><c>renderKnob</c>.</summary>
    private void RenderKnob(RangeKnobHandle? handle, RangeKnob knob, double ratio, double value)
    {
        if (handle is null)
        {
            return;
        }

        var dual = DualKnobs;
        RangeSlider.SetStart(handle, ratio);
        handle.Value = value;
        handle.PinText = (PinFormatter ?? DefaultPinFormatter)(value);
        handle.SetState(":range-knob-pressed", _pressedKnob == knob);
        handle.SetState(":ion-focused", _focusedKnob == knob);
        handle.SetState(":lower", dual && IsLower(knob));
        handle.SetState(":upper", dual && !IsLower(knob));
        handle.SetState(":range-knob-min", value == Min);
        handle.SetState(":range-knob-max", value == Max);
    }

    // ── Item ─────────────────────────────────────────────────────────────────────────────────

    ItemControlType IItemControl.ItemControlType => ItemControlType.Range;

    /// <summary>An item never forwards a tap to a range: <c>getFirstInteractive</c> doesn't list it.</summary>
    void IItemControl.OnItemTap()
    {
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A knob's name: <c>aria-label</c>, moved from the host to each knob, or else <c>aria-labelledby</c> the <c>&lt;label&gt;</c>
    /// around everything, which Chromium names from its text: the label, the start slot, the knob's own pin, the other knob's value
    /// (an embedded slider) and the end slot. Measured.
    /// </summary>
    internal string AccessibleName(RangeKnob knob)
    {
        if (AutomationProperties.GetName(this) is { Length: > 0 } name)
        {
            return name;
        }

        var parts = new List<string?>
        {
            Label ?? TextOf(LabelContent),
            TextOf(StartContent),
        };

        foreach (var handle in new[] { _handleA, _handleB })
        {
            if (handle is null || (handle == _handleB && !DualKnobs))
            {
                continue;
            }

            parts.Add(handle.Knob == knob
                ? Pin ? handle.PinText : null
                : handle.Value.ToString(CultureInfo.InvariantCulture));
        }

        parts.Add(TextOf(EndContent));
        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }

    private static string? TextOf(object? content) => content switch
    {
        null => null,
        string text => text,
        Control control => AccessibleText.Of(control),
        _ => content.ToString(),
    };
}
