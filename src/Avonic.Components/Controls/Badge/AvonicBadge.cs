using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-badge</c>: a small coloured count or label, often at the end of an <see cref="AvonicItem"/>. Its look is in
/// the theme's <c>Controls/AvonicBadge.axaml</c>.
/// </summary>
public class AvonicBadge : ContentControl
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicBadge>();

    private readonly StyleValue<bool> _hidden = new();

    static AvonicBadge()
    {
        // vertical-align: baseline, on an inline-block, which never stretches to its line. A default, not a theme's value, as
        // FlexSlot would take that for align-self, and an item centres it.
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicBadge>(VerticalAlignment.Center);
    }

    public AvonicBadge()
    {
        // Bound once, before any style, so that an app's style still wins when the badge empties after it.
        Bind(IsVisibleProperty, _hidden, BindingPriority.Style);
        UpdateEmpty();
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            UpdateEmpty();
        }
    }

    /// <summary>
    /// <c>:host(:empty) { display: none }</c>. A framework renders no text node for an empty string, so that's empty too. It's
    /// at style priority, so that it survives a replaced theme and an app's own <c>IsVisible</c> wins.
    /// </summary>
    private void UpdateEmpty() => _hidden.SetWhen(Content is null or "", false);
}
