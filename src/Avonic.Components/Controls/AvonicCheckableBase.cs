using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avonic.Components.Controls.Enums;
using Avonic.Components.Primitives;

namespace Avonic.Components.Controls;

/// <summary>
/// Abstract base for all checkable Avonic controls (Checkbox, Toggle, Radio).
/// Inherits <see cref="AvonicPressable"/> for touch interaction, adding checked state,
/// label placement, semantic value, and a group-notification event.
/// </summary>
/// <remarks>
/// Ionic reference: shared shape of <c>ion-checkbox</c>, <c>ion-toggle</c>, <c>ion-radio</c>.
/// </remarks>
[PseudoClasses(
    ":checked",
    ":label-end", ":label-start", ":label-stacked",
    ":justify-space-between", ":justify-start", ":justify-end",
    ":alignment-center", ":alignment-start")]
public abstract class AvonicCheckableBase : AvonicPressable
{
    // ── Direct Properties ────────────────────────────────────────────────────

    private bool _isChecked;

    public static readonly DirectProperty<AvonicCheckableBase, bool> IsCheckedProperty =
        AvaloniaProperty.RegisterDirect<AvonicCheckableBase, bool>(
            nameof(IsChecked),
            o => o._isChecked,
            (o, v) => o.IsChecked = v);

    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicCheckableBase, object?>(nameof(Value));

    public static readonly StyledProperty<LabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<AvonicCheckableBase, LabelPlacement>(
            nameof(LabelPlacement), defaultValue: LabelPlacement.End);

    public static readonly StyledProperty<CheckableJustify> JustifyProperty =
        AvaloniaProperty.Register<AvonicCheckableBase, CheckableJustify>(
            nameof(Justify), defaultValue: CheckableJustify.SpaceBetween);

    public static readonly StyledProperty<CheckableAlignment> AlignmentProperty =
        AvaloniaProperty.Register<AvonicCheckableBase, CheckableAlignment>(
            nameof(Alignment), defaultValue: CheckableAlignment.Center);

    // ── Routed Events ────────────────────────────────────────────────────────

    public static readonly RoutedEvent<CheckedChangedEventArgs> CheckedChangedEvent =
        RoutedEvent.Register<AvonicCheckableBase, CheckedChangedEventArgs>(
            nameof(CheckedChanged), RoutingStrategies.Bubble);

    /// <summary>
    /// Raised when the user changes the checked state. Not raised by programmatic
    /// <see cref="IsChecked"/> assignment.
    /// </summary>
    public event EventHandler<CheckedChangedEventArgs>? CheckedChanged
    {
        add    => AddHandler(CheckedChangedEvent, value);
        remove => RemoveHandler(CheckedChangedEvent, value);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Whether this control is currently checked.</summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => SetAndRaise(IsCheckedProperty, ref _isChecked, value);
    }

    /// <summary>
    /// Semantic value associated with this control.
    /// Used by <see cref="AvonicRadioGroup"/> for group selection.
    /// </summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Where the label is rendered relative to the control element.</summary>
    public LabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    /// <summary>How the label and control are packed within a line.</summary>
    public CheckableJustify Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    /// <summary>Cross-axis alignment of the label and control pair.</summary>
    public CheckableAlignment Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    protected AvonicCheckableBase()
    {
        // Wire to own ReleasedEvent so ToggleChecked fires on every confirmed press.
        // This is added in the instance constructor (not a static ClassHandler) so
        // subclass overrides of ToggleChecked() are always dispatched correctly.
        AddHandler(ReleasedEvent, OnSelfReleased);
    }

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
            PseudoClasses.Set(":checked", change.GetNewValue<bool>());

        if (change.Property == LabelPlacementProperty)
            UpdateLabelPlacementPseudoClasses(change.GetNewValue<LabelPlacement>());

        if (change.Property == JustifyProperty)
            UpdateJustifyPseudoClasses(change.GetNewValue<CheckableJustify>());

        if (change.Property == AlignmentProperty)
            UpdateAlignmentPseudoClasses(change.GetNewValue<CheckableAlignment>());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        PseudoClasses.Set(":checked", IsChecked);
        UpdateLabelPlacementPseudoClasses(LabelPlacement);
        UpdateJustifyPseudoClasses(Justify);
        UpdateAlignmentPseudoClasses(Alignment);
    }

    // ── Toggle logic ─────────────────────────────────────────────────────────

    /// <summary>
    /// Inverts <see cref="IsChecked"/> and raises <see cref="CheckedChangedEvent"/>.
    /// Called on every confirmed release. Subclasses may override to alter behaviour
    /// (e.g. <c>AvonicRadio</c> only allows checking, never unchecking).
    /// </summary>
    protected virtual void ToggleChecked()
    {
        IsChecked = !IsChecked;
        RaiseEvent(new CheckedChangedEventArgs(CheckedChangedEvent, this, IsChecked, Value));
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void OnSelfReleased(object? sender, RoutedEventArgs e)
        => ToggleChecked();

    private void UpdateLabelPlacementPseudoClasses(LabelPlacement placement)
    {
        PseudoClasses.Set(":label-end",     placement == LabelPlacement.End);
        PseudoClasses.Set(":label-start",   placement == LabelPlacement.Start);
        PseudoClasses.Set(":label-stacked", placement == LabelPlacement.Stacked);
    }

    private void UpdateJustifyPseudoClasses(CheckableJustify justify)
    {
        PseudoClasses.Set(":justify-space-between", justify == CheckableJustify.SpaceBetween);
        PseudoClasses.Set(":justify-start",         justify == CheckableJustify.Start);
        PseudoClasses.Set(":justify-end",           justify == CheckableJustify.End);
    }

    private void UpdateAlignmentPseudoClasses(CheckableAlignment alignment)
    {
        PseudoClasses.Set(":alignment-center", alignment == CheckableAlignment.Center);
        PseudoClasses.Set(":alignment-start",  alignment == CheckableAlignment.Start);
    }
}
