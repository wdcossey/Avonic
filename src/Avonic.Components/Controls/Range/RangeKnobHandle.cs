using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>.range-knob-handle</c> in <c>ion-range</c>'s shadow root: a knob's container, which takes focus and the arrow keys, and holds
/// its pin and its knob. <see cref="AvonicRange"/> sets its state. Its automation peer is the knob's <c>role="slider"</c>.
/// </summary>
[PseudoClasses(":range-knob-pressed", ":ion-focused", ":lower", ":upper", ":range-knob-min", ":range-knob-max", ":activated", ":ion-hover")]
public sealed class RangeKnobHandle : Panel
{
    /// <summary>Which knob this is: A, or B, which only two knobs have.</summary>
    public static readonly StyledProperty<RangeKnob> KnobProperty =
        AvaloniaProperty.Register<RangeKnobHandle, RangeKnob>(nameof(Knob));

    /// <summary>The knob's value: <c>valA</c> or <c>valB</c>, its <c>aria-valuenow</c>.</summary>
    public static readonly DirectProperty<RangeKnobHandle, double> ValueProperty =
        AvaloniaProperty.RegisterDirect<RangeKnobHandle, double>(nameof(Value), handle => handle.Value);

    /// <summary>What the pin shows: <c>pinFormatter(value)</c>.</summary>
    public static readonly DirectProperty<RangeKnobHandle, string?> PinTextProperty =
        AvaloniaProperty.RegisterDirect<RangeKnobHandle, string?>(nameof(PinText), handle => handle.PinText);

    private double _value;
    private string? _pinText;

    static RangeKnobHandle()
    {
        // tabindex="0", unless disabled, which Avalonia already leaves out.
        FocusableProperty.OverrideDefaultValue<RangeKnobHandle>(true);
    }

    public RangeKnobHandle()
    {
        // ion-activatable: tap-click's :activated, and :ion-hover. At style priority, here, before any style, so that an app's
        // style wins.
        SetValue(Activatable.IsEnabledProperty, true, BindingPriority.Style);
    }

    public RangeKnob Knob
    {
        get => GetValue(KnobProperty);
        set => SetValue(KnobProperty, value);
    }

    public double Value
    {
        get => _value;
        internal set => SetAndRaise(ValueProperty, ref _value, value);
    }

    public string? PinText
    {
        get => _pinText;
        internal set => SetAndRaise(PinTextProperty, ref _pinText, value);
    }

    internal void SetState(string pseudoClass, bool on) => PseudoClasses.Set(pseudoClass, on);

    private AvonicRange? Range => TemplatedParent as AvonicRange;

    protected override AutomationPeer OnCreateAutomationPeer() => new KnobPeer(this);

    /// <summary><c>role="slider"</c>, with <c>aria-valuemin</c>, <c>aria-valuemax</c>, <c>aria-valuenow</c> and <c>aria-disabled</c>.</summary>
    private sealed class KnobPeer : ControlAutomationPeer, IRangeValueProvider
    {
        private readonly RangeKnobHandle _owner;

        public KnobPeer(RangeKnobHandle owner)
            : base(owner)
        {
            _owner = owner;
            owner.PropertyChanged += (_, e) =>
            {
                if (e.Property == ValueProperty)
                {
                    RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, e.OldValue, e.NewValue);
                }
            };
        }

        public bool IsReadOnly => !_owner.IsEffectivelyEnabled;

        public double Minimum => _owner.Range?.Min ?? 0;

        public double Maximum => _owner.Range?.Max ?? 100;

        public double Value => _owner.Value;

        public double SmallChange => _owner.Range?.Step is > 0 and var step ? step : 1;

        public double LargeChange => SmallChange;

        /// <summary>
        /// A screen reader's increment or decrement, which Chromium makes an arrow key for a <c>role="slider"</c>: one step toward the
        /// value asked for.
        /// </summary>
        public void SetValue(double value)
        {
            if (!IsReadOnly && value != _owner.Value)
            {
                _owner.Range?.HandleKeyboard(_owner.Knob, value > _owner.Value);
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

        protected override string? GetNameCore() => _owner.Range?.AccessibleName(_owner.Knob) ?? base.GetNameCore();

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }
}
