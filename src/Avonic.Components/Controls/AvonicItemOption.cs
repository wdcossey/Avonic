using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avonic.Components.Controls.Enums;
using Avonic.Components.Primitives;

namespace Avonic.Components.Controls;

/// <summary>
/// A single swipe action button within an <see cref="AvonicItemOptions"/> panel.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-item-option</c>
/// </remarks>
[PseudoClasses(
    ":color-default", ":color-primary", ":color-secondary",
    ":color-success", ":color-warning", ":color-danger",
    ":icon-only")]
public class AvonicItemOption : AvonicPressable
{
    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<ItemOptionColor> ColorProperty =
        AvaloniaProperty.Register<AvonicItemOption, ItemOptionColor>(
            nameof(Color), defaultValue: ItemOptionColor.Default);

    public static readonly StyledProperty<bool> IconOnlyProperty =
        AvaloniaProperty.Register<AvonicItemOption, bool>(nameof(IconOnly), defaultValue: false);

    // ── Start / End Icon Slots ───────────────────────────────────────────────

    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicItemOption, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItemOption, IDataTemplate?>(nameof(StartContentTemplate));

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Background colour variant for the option.</summary>
    public ItemOptionColor Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>When <see langword="true"/>, hides the label and expands the icon.</summary>
    public bool IconOnly
    {
        get => GetValue(IconOnlyProperty);
        set => SetValue(IconOnlyProperty, value);
    }

    /// <summary>Content rendered above the label (icon slot).</summary>
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

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ColorProperty)
            UpdateColorPseudoClasses(change.GetNewValue<ItemOptionColor>());

        if (change.Property == IconOnlyProperty)
            PseudoClasses.Set(":icon-only", change.GetNewValue<bool>());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateColorPseudoClasses(Color);
        PseudoClasses.Set(":icon-only", IconOnly);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void UpdateColorPseudoClasses(ItemOptionColor color)
    {
        PseudoClasses.Set(":color-default",   color == ItemOptionColor.Default);
        PseudoClasses.Set(":color-primary",   color == ItemOptionColor.Primary);
        PseudoClasses.Set(":color-secondary", color == ItemOptionColor.Secondary);
        PseudoClasses.Set(":color-success",   color == ItemOptionColor.Success);
        PseudoClasses.Set(":color-warning",   color == ItemOptionColor.Warning);
        PseudoClasses.Set(":color-danger",    color == ItemOptionColor.Danger);
    }
}
