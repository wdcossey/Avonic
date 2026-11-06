using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-card-subtitle</c>. Its look is in the theme's <c>Styles/Card.axaml</c>.</summary>
public class AvonicCardSubtitle : ContentControl
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicCardSubtitle>();

    /// <summary><c>aria-level="3"</c>.</summary>
    static AvonicCardSubtitle() => AutomationProperties.HeadingLevelProperty.OverrideDefaultValue<AvonicCardSubtitle>(3);

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new CardHeadingAutomationPeer(this);
}
