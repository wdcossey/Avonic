using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-list-header</c>: a heading row for a list, with its own lines and colour.</summary>
[TemplatePart("PART_Inner", typeof(Border))]
[PseudoClasses(":lines-full", ":lines-inset", ":lines-none")]
public class AvonicListHeader : ContentControl
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicListHeader>();

    /// <summary>Ionic <c>lines</c>: how the bottom border is displayed on the header. Null is no border.</summary>
    public static readonly StyledProperty<ListHeaderLines?> LinesProperty =
        AvaloniaProperty.Register<AvonicListHeader, ListHeaderLines?>(nameof(Lines));

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public ListHeaderLines? Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LinesProperty)
        {
            var lines = Lines;
            PseudoClasses.Set(":lines-full", lines == ListHeaderLines.Full);
            PseudoClasses.Set(":lines-inset", lines == ListHeaderLines.Inset);
            PseudoClasses.Set(":lines-none", lines == ListHeaderLines.None);
        }
    }
}
