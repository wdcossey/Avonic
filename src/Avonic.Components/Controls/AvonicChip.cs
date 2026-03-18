using Avalonia;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avonic.Components.Primitives;

namespace Avonic.Components.Controls;

/// <summary>
/// A compact interactive chip/tag element, often used for filters, selections, or labels.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-chip</c>
/// </remarks>
[PseudoClasses(":outline")]
public class AvonicChip : AvonicPressable
{
    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<bool> OutlineProperty =
        AvaloniaProperty.Register<AvonicChip, bool>(nameof(Outline), defaultValue: false);

    /// <summary>When <see langword="true"/>, renders as an outline chip with transparent background.</summary>
    public bool Outline
    {
        get => GetValue(OutlineProperty);
        set => SetValue(OutlineProperty, value);
    }

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OutlineProperty)
            PseudoClasses.Set(":outline", change.GetNewValue<bool>());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        PseudoClasses.Set(":outline", Outline);
    }
}
