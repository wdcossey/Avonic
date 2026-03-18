using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avonic.Components.Controls.Enums;

namespace Avonic.Components.Controls;

/// <summary>
/// An animated loading spinner with multiple visual variants.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-spinner</c>
/// </remarks>
[PseudoClasses(
    ":spinner-circular", ":spinner-crescent",
    ":spinner-dots", ":spinner-lines",
    ":spinner-bubbles", ":spinner-circles")]
public class AvonicSpinner : TemplatedControl
{
    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<SpinnerName> VariantProperty =
        AvaloniaProperty.Register<AvonicSpinner, SpinnerName>(
            nameof(Variant), defaultValue: SpinnerName.Circular);

    public static readonly StyledProperty<double> DurationProperty =
        AvaloniaProperty.Register<AvonicSpinner, double>(nameof(Duration), defaultValue: 0);

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Visual variant of the spinner. Default is <see cref="SpinnerName.Circular"/>.</summary>
    public SpinnerName Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>
    /// Override for the animation duration in milliseconds. When 0 (default), each variant
    /// uses its own natural duration.
    /// </summary>
    public double Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty)
            UpdateVariantPseudoClasses(change.GetNewValue<SpinnerName>());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateVariantPseudoClasses(Variant);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void UpdateVariantPseudoClasses(SpinnerName variant)
    {
        PseudoClasses.Set(":spinner-circular",  variant == SpinnerName.Circular);
        PseudoClasses.Set(":spinner-crescent",  variant == SpinnerName.Crescent);
        PseudoClasses.Set(":spinner-dots",      variant == SpinnerName.Dots);
        PseudoClasses.Set(":spinner-lines",     variant == SpinnerName.Lines);
        PseudoClasses.Set(":spinner-bubbles",   variant == SpinnerName.Bubbles);
        PseudoClasses.Set(":spinner-circles",   variant == SpinnerName.Circles);
    }
}
