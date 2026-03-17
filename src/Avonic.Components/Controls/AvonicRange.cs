using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avonic.Components.Controls.Enums;

namespace Avonic.Components.Controls;

/// <summary>
/// A touch-first range slider with optional snapping, tick marks, and a label slot.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-range</c><br/>
/// The drag gesture computes a ratio from the pointer position relative to the
/// transparent <c>PART_TrackHitArea</c> overlay. When <see cref="Snaps"/> is
/// <c>true</c>, the value is rounded to the nearest <see cref="Step"/> increment
/// on every drag update.<br/>
/// <c>PART_ActiveBar</c> width and <c>PART_Knob</c> left margin are set in
/// <see cref="ArrangeOverride"/> after each layout pass so the values stay in
/// sync with the track's rendered width without requiring Reactive subscriptions.
/// </remarks>
[PseudoClasses(
    ":dragging",
    ":show-pin",
    ":show-ticks",
    ":label-start", ":label-end", ":label-stacked", ":label-fixed")]
public class AvonicRange : ContentControl
{
    // ── Backing field ────────────────────────────────────────────────────────

    private double _value;

    // ── DirectProperty ────────────────────────────────────────────────────────

    public static readonly DirectProperty<AvonicRange, double> ValueProperty =
        AvaloniaProperty.RegisterDirect<AvonicRange, double>(
            nameof(Value),
            o => o._value,
            (o, v) => o.Value = v);

    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<double> MinProperty =
        AvaloniaProperty.Register<AvonicRange, double>(nameof(Min), defaultValue: 0.0);

    public static readonly StyledProperty<double> MaxProperty =
        AvaloniaProperty.Register<AvonicRange, double>(nameof(Max), defaultValue: 100.0);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<AvonicRange, double>(nameof(Step), defaultValue: 1.0);

    public static readonly StyledProperty<bool> ShowPinProperty =
        AvaloniaProperty.Register<AvonicRange, bool>(nameof(ShowPin), defaultValue: false);

    public static readonly StyledProperty<bool> SnapsProperty =
        AvaloniaProperty.Register<AvonicRange, bool>(nameof(Snaps), defaultValue: false);

    public static readonly StyledProperty<bool> ShowTicksProperty =
        AvaloniaProperty.Register<AvonicRange, bool>(nameof(ShowTicks), defaultValue: false);

    public static readonly StyledProperty<RangeLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<AvonicRange, RangeLabelPlacement>(
            nameof(LabelPlacement), defaultValue: RangeLabelPlacement.Start);

    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicRange, object?>(nameof(StartContent));

    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicRange, object?>(nameof(EndContent));

    // ── Routed Events ────────────────────────────────────────────────────────

    public static readonly RoutedEvent<RangeValueChangedEventArgs> ValueChangedEvent =
        RoutedEvent.Register<AvonicRange, RangeValueChangedEventArgs>(
            nameof(ValueChanged), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> KnobMoveStartEvent =
        RoutedEvent.Register<AvonicRange, RoutedEventArgs>(
            nameof(KnobMoveStart), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> KnobMoveEndEvent =
        RoutedEvent.Register<AvonicRange, RoutedEventArgs>(
            nameof(KnobMoveEnd), RoutingStrategies.Bubble);

    /// <summary>Raised when the user changes the value by dragging or tapping the track.</summary>
    public event EventHandler<RangeValueChangedEventArgs>? ValueChanged
    {
        add    => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>Raised when the user begins a knob drag.</summary>
    public event EventHandler<RoutedEventArgs>? KnobMoveStart
    {
        add    => AddHandler(KnobMoveStartEvent, value);
        remove => RemoveHandler(KnobMoveStartEvent, value);
    }

    /// <summary>Raised when the user finishes a knob drag.</summary>
    public event EventHandler<RoutedEventArgs>? KnobMoveEnd
    {
        add    => AddHandler(KnobMoveEndEvent, value);
        remove => RemoveHandler(KnobMoveEndEvent, value);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>The current value. Clamped to [<see cref="Min"/>, <see cref="Max"/>] on set.</summary>
    public double Value
    {
        get => _value;
        set => SetAndRaise(ValueProperty, ref _value, Math.Clamp(value, Min, Max));
    }

    public double Min
    {
        get => GetValue(MinProperty);
        set => SetValue(MinProperty, value);
    }

    public double Max
    {
        get => GetValue(MaxProperty);
        set => SetValue(MaxProperty, value);
    }

    /// <summary>Value granularity. Used for snapping when <see cref="Snaps"/> is <c>true</c>.</summary>
    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    /// <summary>Show a pin with the current value above the knob while dragging.</summary>
    public bool ShowPin
    {
        get => GetValue(ShowPinProperty);
        set => SetValue(ShowPinProperty, value);
    }

    /// <summary>Round value to the nearest <see cref="Step"/> increment while dragging.</summary>
    public bool Snaps
    {
        get => GetValue(SnapsProperty);
        set => SetValue(SnapsProperty, value);
    }

    /// <summary>
    /// Show tick marks at each <see cref="Step"/> position.
    /// Only meaningful when <see cref="Snaps"/> is <c>true</c>.
    /// </summary>
    public bool ShowTicks
    {
        get => GetValue(ShowTicksProperty);
        set => SetValue(ShowTicksProperty, value);
    }

    /// <summary>Where the label is rendered relative to the slider.</summary>
    public RangeLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    /// <summary>Content placed to the left of the slider track. Ionic <c>start</c> slot.</summary>
    public object? StartContent
    {
        get => GetValue(StartContentProperty);
        set => SetValue(StartContentProperty, value);
    }

    /// <summary>Content placed to the right of the slider track. Ionic <c>end</c> slot.</summary>
    public object? EndContent
    {
        get => GetValue(EndContentProperty);
        set => SetValue(EndContentProperty, value);
    }

    // ── Template parts ───────────────────────────────────────────────────────

    private Border?    _trackHitArea;
    private Border?    _activeBar;
    private Border?    _knob;
    private TextBlock? _pinLabel;

    // ── Drag state ───────────────────────────────────────────────────────────

    private bool _isDragging;

    // ── Template ─────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_trackHitArea != null)
        {
            _trackHitArea.PointerPressed     -= OnTrackPointerPressed;
            _trackHitArea.PointerMoved       -= OnTrackPointerMoved;
            _trackHitArea.PointerReleased    -= OnTrackPointerReleased;
            _trackHitArea.PointerCaptureLost -= OnTrackPointerCaptureLost;
        }

        _trackHitArea = e.NameScope.Find<Border>("PART_TrackHitArea");
        _activeBar    = e.NameScope.Find<Border>("PART_ActiveBar");
        _knob         = e.NameScope.Find<Border>("PART_Knob");
        _pinLabel     = e.NameScope.Find<TextBlock>("PART_PinLabel");

        if (_trackHitArea != null)
        {
            _trackHitArea.PointerPressed     += OnTrackPointerPressed;
            _trackHitArea.PointerMoved       += OnTrackPointerMoved;
            _trackHitArea.PointerReleased    += OnTrackPointerReleased;
            _trackHitArea.PointerCaptureLost += OnTrackPointerCaptureLost;
        }

        UpdatePseudoClasses();
    }

    // UpdateVisuals is called from ArrangeOverride so that _trackHitArea.Bounds
    // and _knob.Bounds are guaranteed to reflect the current layout pass.
    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);
        UpdateVisuals();
        return result;
    }

    // ── Property change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty ||
            change.Property == MinProperty   ||
            change.Property == MaxProperty)
            UpdateVisuals();

        if (change.Property == LabelPlacementProperty)
            UpdateLabelPlacementPseudoClasses(change.GetNewValue<RangeLabelPlacement>());

        if (change.Property == ShowPinProperty)
            PseudoClasses.Set(":show-pin", change.GetNewValue<bool>());

        if (change.Property == ShowTicksProperty)
            PseudoClasses.Set(":show-ticks", change.GetNewValue<bool>());
    }

    // ── Visuals ──────────────────────────────────────────────────────────────

    private void UpdateVisuals()
    {
        if (_trackHitArea == null) return;

        var ratio      = ComputeRatio(Value);
        var trackWidth = _trackHitArea.Bounds.Width;
        var knobHalf   = (_knob?.Bounds.Width ?? 0.0) / 2.0;

        if (_activeBar != null)
            _activeBar.Width = trackWidth * ratio;

        if (_knob != null)
            _knob.Margin = new Thickness(trackWidth * ratio - knobHalf, 0, 0, 0);

        if (_pinLabel != null)
            _pinLabel.Text = ((int)Math.Round(Value)).ToString();
    }

    // ── Pseudoclasses ─────────────────────────────────────────────────────────

    private void UpdatePseudoClasses()
    {
        UpdateLabelPlacementPseudoClasses(LabelPlacement);
        PseudoClasses.Set(":show-pin",   ShowPin);
        PseudoClasses.Set(":show-ticks", ShowTicks);
    }

    private void UpdateLabelPlacementPseudoClasses(RangeLabelPlacement placement)
    {
        PseudoClasses.Set(":label-start",   placement == RangeLabelPlacement.Start);
        PseudoClasses.Set(":label-end",     placement == RangeLabelPlacement.End);
        PseudoClasses.Set(":label-stacked", placement == RangeLabelPlacement.Stacked);
        PseudoClasses.Set(":label-fixed",   placement == RangeLabelPlacement.Fixed);
    }

    // ── Gesture ───────────────────────────────────────────────────────────────

    private void OnTrackPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsEffectivelyEnabled || !e.GetCurrentPoint(_trackHitArea).Properties.IsLeftButtonPressed)
            return;

        e.Pointer.Capture(_trackHitArea);
        _isDragging = true;
        PseudoClasses.Set(":dragging", true);

        SetValueFromPointer(e.GetPosition(_trackHitArea));
        RaiseEvent(new RoutedEventArgs(KnobMoveStartEvent, this));
        e.Handled = true;
    }

    private void OnTrackPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;

        SetValueFromPointer(e.GetPosition(_trackHitArea));
        e.Handled = true;
    }

    private void OnTrackPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;

        _isDragging = false;
        PseudoClasses.Set(":dragging", false);

        e.Pointer.Capture(null);
        RaiseEvent(new RoutedEventArgs(KnobMoveEndEvent, this));
        e.Handled = true;
    }

    private void OnTrackPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_isDragging) return;

        _isDragging = false;
        PseudoClasses.Set(":dragging", false);
    }

    // ── Value computation ─────────────────────────────────────────────────────

    private void SetValueFromPointer(Point position)
    {
        if (_trackHitArea == null || _trackHitArea.Bounds.Width <= 0) return;

        var ratio    = Math.Clamp(position.X / _trackHitArea.Bounds.Width, 0.0, 1.0);
        var newValue = RatioToValue(ratio);

        if (newValue == _value) return;

        var oldValue = _value;
        Value = newValue;
        RaiseEvent(new RangeValueChangedEventArgs(ValueChangedEvent, this, oldValue, newValue));
    }

    private double ComputeRatio(double value)
    {
        var range = Max - Min;
        return range <= 0 ? 0.0 : Math.Clamp((value - Min) / range, 0.0, 1.0);
    }

    private double RatioToValue(double ratio)
    {
        var raw = Min + ratio * (Max - Min);

        if (Snaps && Step > 0)
            raw = Math.Round(raw / Step) * Step;

        return Math.Clamp(raw, Min, Max);
    }
}
