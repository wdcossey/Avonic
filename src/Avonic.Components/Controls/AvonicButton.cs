using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avonic.Components.Primitives;

namespace Avonic.Components.Controls;

/// <summary>
/// A touch-first button with fill, shape, size, and expand variants.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-button</c>
/// </remarks>
[PseudoClasses(
    ":fill-solid", ":fill-outline", ":fill-clear",
    ":expand-block", ":expand-full",
    ":shape-round",
    ":size-small", ":size-default", ":size-large",
    ":strong")]
public class AvonicButton : AvonicPressable
{
    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<ButtonFill> FillProperty =
        AvaloniaProperty.Register<AvonicButton, ButtonFill>(nameof(Fill), defaultValue: ButtonFill.Solid);

    public static readonly StyledProperty<ButtonExpand?> ExpandProperty =
        AvaloniaProperty.Register<AvonicButton, ButtonExpand?>(nameof(Expand));

    public static readonly StyledProperty<ButtonShape> ShapeProperty =
        AvaloniaProperty.Register<AvonicButton, ButtonShape>(nameof(Shape), defaultValue: ButtonShape.Default);

    public static readonly StyledProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.Register<AvonicButton, ButtonSize>(nameof(Size), defaultValue: ButtonSize.Default);

    public static readonly StyledProperty<bool> StrongProperty =
        AvaloniaProperty.Register<AvonicButton, bool>(nameof(Strong), defaultValue: false);

    // ── Start / End Slots ────────────────────────────────────────────────────

    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicButton, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicButton, IDataTemplate?>(nameof(StartContentTemplate));

    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicButton, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicButton, IDataTemplate?>(nameof(EndContentTemplate));

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Visual fill style — Solid (default), Outline, or Clear.</summary>
    public ButtonFill Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Expand to Block (full-width, rounded) or Full (full-width, square).</summary>
    public ButtonExpand? Expand
    {
        get => GetValue(ExpandProperty);
        set => SetValue(ExpandProperty, value);
    }

    /// <summary>Shape variant — Default or Round (pill-shaped corner radius).</summary>
    public ButtonShape Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    /// <summary>Size variant — Small, Default, or Large.</summary>
    public ButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>When <see langword="true"/>, renders the label with a heavier font weight.</summary>
    public bool Strong
    {
        get => GetValue(StrongProperty);
        set => SetValue(StrongProperty, value);
    }

    /// <summary>Content rendered to the left of the label.</summary>
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

    /// <summary>Content rendered to the right of the label.</summary>
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

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FillProperty)
            UpdateFillPseudoClasses(change.GetNewValue<ButtonFill>());

        if (change.Property == ExpandProperty)
            UpdateExpandPseudoClasses(change.GetNewValue<ButtonExpand?>());

        if (change.Property == ShapeProperty)
            UpdateShapePseudoClasses(change.GetNewValue<ButtonShape>());

        if (change.Property == SizeProperty)
            UpdateSizePseudoClasses(change.GetNewValue<ButtonSize>());

        if (change.Property == StrongProperty)
            PseudoClasses.Set(":strong", change.GetNewValue<bool>());
    }

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateFillPseudoClasses(Fill);
        UpdateExpandPseudoClasses(Expand);
        UpdateShapePseudoClasses(Shape);
        UpdateSizePseudoClasses(Size);
        PseudoClasses.Set(":strong", Strong);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void UpdateFillPseudoClasses(ButtonFill fill)
    {
        PseudoClasses.Set(":fill-solid",   fill == ButtonFill.Solid);
        PseudoClasses.Set(":fill-outline", fill == ButtonFill.Outline);
        PseudoClasses.Set(":fill-clear",   fill == ButtonFill.Clear);
    }

    private void UpdateExpandPseudoClasses(ButtonExpand? expand)
    {
        PseudoClasses.Set(":expand-block", expand == ButtonExpand.Block);
        PseudoClasses.Set(":expand-full",  expand == ButtonExpand.Full);
    }

    private void UpdateShapePseudoClasses(ButtonShape shape)
    {
        PseudoClasses.Set(":shape-round", shape == ButtonShape.Round);
    }

    private void UpdateSizePseudoClasses(ButtonSize size)
    {
        PseudoClasses.Set(":size-small",   size == ButtonSize.Small);
        PseudoClasses.Set(":size-default", size == ButtonSize.Default);
        PseudoClasses.Set(":size-large",   size == ButtonSize.Large);
    }
}
