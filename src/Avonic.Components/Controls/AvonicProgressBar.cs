using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avonic.Components.Controls.Enums;

namespace Avonic.Components.Controls;

/// <summary>
/// A horizontal progress bar with determinate and indeterminate variants.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-progress-bar</c><br/>
/// For determinate mode, <see cref="Value"/> drives the fill width and
/// <see cref="Buffer"/> drives a secondary buffer indicator behind the fill.<br/>
/// For indeterminate mode, a looping animation is used regardless of <see cref="Value"/>.
/// </remarks>
[PseudoClasses(":determinate", ":indeterminate", ":reversed")]
public class AvonicProgressBar : TemplatedControl
{
    // ── Backing field ────────────────────────────────────────────────────────

    private double _value;

    // ── Template parts ───────────────────────────────────────────────────────

    private Border? _progressBar;
    private Border? _bufferBar;

    // ── DirectProperty ───────────────────────────────────────────────────────

    public static readonly DirectProperty<AvonicProgressBar, double> ValueProperty =
        AvaloniaProperty.RegisterDirect<AvonicProgressBar, double>(
            nameof(Value),
            o => o._value,
            (o, v) => o.Value = v);

    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<double> BufferProperty =
        AvaloniaProperty.Register<AvonicProgressBar, double>(nameof(Buffer), defaultValue: 1.0);

    public static readonly StyledProperty<ProgressBarType> TypeProperty =
        AvaloniaProperty.Register<AvonicProgressBar, ProgressBarType>(
            nameof(Type), defaultValue: ProgressBarType.Determinate);

    public static readonly StyledProperty<bool> ReversedProperty =
        AvaloniaProperty.Register<AvonicProgressBar, bool>(nameof(Reversed), defaultValue: false);

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Progress value in the range [0, 1]. Clamped on assignment.</summary>
    public double Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            SetAndRaise(ValueProperty, ref _value, clamped);
        }
    }

    /// <summary>Buffer amount in the range [0, 1] — shown as a secondary indicator behind the fill.</summary>
    public double Buffer
    {
        get => GetValue(BufferProperty);
        set => SetValue(BufferProperty, Math.Clamp(value, 0.0, 1.0));
    }

    /// <summary>Animation type: <see cref="ProgressBarType.Determinate"/> (default) or <see cref="ProgressBarType.Indeterminate"/>.</summary>
    public ProgressBarType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    /// <summary>When <see langword="true"/>, reverses the fill direction.</summary>
    public bool Reversed
    {
        get => GetValue(ReversedProperty);
        set => SetValue(ReversedProperty, value);
    }

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TypeProperty)
            UpdateTypePseudoClasses(change.GetNewValue<ProgressBarType>());

        if (change.Property == ReversedProperty)
            PseudoClasses.Set(":reversed", change.GetNewValue<bool>());

        if (change.Property == ValueProperty || change.Property == BufferProperty)
            UpdateBarWidths();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _progressBar = e.NameScope.Find<Border>("PART_Progress");
        _bufferBar   = e.NameScope.Find<Border>("PART_Buffer");

        UpdateTypePseudoClasses(Type);
        PseudoClasses.Set(":reversed", Reversed);
        UpdateBarWidths();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);
        UpdateBarWidths();
        return arranged;
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void UpdateTypePseudoClasses(ProgressBarType type)
    {
        PseudoClasses.Set(":determinate",   type == ProgressBarType.Determinate);
        PseudoClasses.Set(":indeterminate", type == ProgressBarType.Indeterminate);
    }

    private void UpdateBarWidths()
    {
        if (_progressBar != null)
            _progressBar.Width = Bounds.Width * Value;

        if (_bufferBar != null)
            _bufferBar.Width = Bounds.Width * Buffer;
    }
}
