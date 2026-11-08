using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-title</c>: a toolbar's title, on one line with an ellipsis. The look is in the theme. See
/// <c>docs/components/ion-toolbar/README.md</c>.
/// </summary>
[PseudoClasses(":size-default", ":size-small", ":size-large")]
public class AvonicTitle : ContentControl
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicTitle>();

    /// <summary>Ionic <c>size</c>. Null is Ionic's <c>default</c>.</summary>
    public static readonly StyledProperty<TitleSize?> SizeProperty =
        AvaloniaProperty.Register<AvonicTitle, TitleSize?>(nameof(Size));

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public TitleSize? Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public AvonicTitle()
    {
        PseudoClasses.Set(":size-default", true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // title-small, and title-default and title-large, which md has no rules for but an app can style, as in Ionic.
        // sizeChanged emits the style again.
        if (change.Property == SizeProperty)
        {
            PseudoClasses.Set(":size-default", Size is null);
            PseudoClasses.Set(":size-small", Size == TitleSize.Small);
            PseudoClasses.Set(":size-large", Size == TitleSize.Large);
            EmitStyle();
        }
    }

    /// <summary><c>connectedCallback</c> emits the style.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EmitStyle();
    }

    /// <summary><c>emitStyle</c>: <c>title-default</c>, <c>title-small</c> or <c>title-large</c>, which a toolbar takes as <c>:toolbar-title-*</c>.</summary>
    private void EmitStyle()
    {
        var size = Size is { } value ? value.ToString().ToLowerInvariant() : "default";
        RaiseEvent(new IonStyleEventArgs(new Dictionary<string, bool> { ["title-" + size] = true }));
    }
}
