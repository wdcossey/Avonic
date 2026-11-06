using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-card-title</c>. Its look is in the theme's <c>Styles/Card.axaml</c>.</summary>
public class AvonicCardTitle : ContentControl
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicCardTitle>();

    /// <summary><c>aria-level="2"</c>.</summary>
    static AvonicCardTitle() => AutomationProperties.HeadingLevelProperty.OverrideDefaultValue<AvonicCardTitle>(2);

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new CardHeadingAutomationPeer(this);
}
