using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avonic.Components.Controls.Enums;

namespace Avonic.Components.Controls;

/// <summary>
/// A container of <see cref="AvonicItemOption"/> actions revealed by swiping an <see cref="AvonicItemSliding"/>.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-item-options</c>
/// </remarks>
[PseudoClasses(":side-start", ":side-end")]
public class AvonicItemOptions : ItemsControl
{
    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<SlideSide> SideProperty =
        AvaloniaProperty.Register<AvonicItemOptions, SlideSide>(
            nameof(Side), defaultValue: SlideSide.End);

    /// <summary>Which side of the parent <see cref="AvonicItemSliding"/> this panel occupies.</summary>
    public SlideSide Side
    {
        get => GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SideProperty)
            UpdateSidePseudoClasses(change.GetNewValue<SlideSide>());
    }

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateSidePseudoClasses(Side);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void UpdateSidePseudoClasses(SlideSide side)
    {
        PseudoClasses.Set(":side-start", side == SlideSide.Start);
        PseudoClasses.Set(":side-end",   side == SlideSide.End);
    }
}
