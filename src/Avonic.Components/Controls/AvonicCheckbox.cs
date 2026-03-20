namespace Avonic.Components.Controls;

/// <summary>
/// A touch-first checkbox with optional indeterminate state.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-checkbox</c>
/// </remarks>
[PseudoClasses(":indeterminate")]
public class AvonicCheckbox : AvonicCheckableBase
{
    // ── Direct Properties ────────────────────────────────────────────────────

    private bool _isIndeterminate;

    public static readonly DirectProperty<AvonicCheckbox, bool> IsIndeterminateProperty =
        AvaloniaProperty.RegisterDirect<AvonicCheckbox, bool>(
            nameof(IsIndeterminate),
            o => o._isIndeterminate,
            (o, v) => o.IsIndeterminate = v);

    /// <summary>
    /// When <see langword="true"/> the checkbox displays a dash rather than a tick.
    /// The first user interaction clears indeterminate and checks the control.
    /// </summary>
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => SetAndRaise(IsIndeterminateProperty, ref _isIndeterminate, value);
    }

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsIndeterminateProperty)
            PseudoClasses.Set(":indeterminate", change.GetNewValue<bool>());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        PseudoClasses.Set(":indeterminate", IsIndeterminate);
    }

    // ── Toggle override ──────────────────────────────────────────────────────

    /// <summary>
    /// If indeterminate, clears the state and moves to checked.
    /// Otherwise, delegates to the standard toggle logic.
    /// </summary>
    protected override void ToggleChecked()
    {
        if (IsIndeterminate)
        {
            IsIndeterminate = false;
            IsChecked       = true;
            RaiseEvent(new CheckedChangedEventArgs(CheckedChangedEvent, this, true, Value));
            return;
        }

        base.ToggleChecked();
    }
}
