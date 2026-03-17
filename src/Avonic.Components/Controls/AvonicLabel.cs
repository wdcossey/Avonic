using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Avonic.Components.Controls;

/// <summary>
/// A text label for use inside <see cref="AvonicItem"/> or standalone.
/// Supports fixed, stacked, and floating layout positions.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-label</c>
/// </remarks>
[PseudoClasses(":fixed", ":stacked", ":floating")]
public class AvonicLabel : ContentControl
{
    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<LabelPosition> PositionProperty =
        AvaloniaProperty.Register<AvonicLabel, LabelPosition>(
            nameof(Position),
            defaultValue: LabelPosition.Default);

    /// <summary>
    /// Controls the label's layout position within an <see cref="AvonicItem"/>.
    /// </summary>
    public LabelPosition Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    // ── Property Change Handlers ─────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PositionProperty)
            UpdatePositionPseudoClasses(change.GetNewValue<LabelPosition>());
    }

    private void UpdatePositionPseudoClasses(LabelPosition position)
    {
        PseudoClasses.Set(":fixed",    position == LabelPosition.Fixed);
        PseudoClasses.Set(":stacked",  position == LabelPosition.Stacked);
        PseudoClasses.Set(":floating", position == LabelPosition.Floating);
    }
}
