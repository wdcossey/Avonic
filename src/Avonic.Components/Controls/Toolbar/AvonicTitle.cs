using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-title</c>: a toolbar's title, on one line with an ellipsis. The look is in the theme. See
/// <c>docs/components/ion-toolbar/README.md</c>.
/// </summary>
[PseudoClasses(":size-small")]
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // title-small. title-large has rules in ios only, so it needs no pseudoclass.
        if (change.Property == SizeProperty)
        {
            PseudoClasses.Set(":size-small", Size == TitleSize.Small);
        }
    }
}
