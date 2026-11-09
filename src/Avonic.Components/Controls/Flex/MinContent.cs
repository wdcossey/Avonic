using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// CSS's <c>min-content</c> and <c>min-width: auto</c>, for <see cref="FlexRow"/> and <see cref="RowPanel"/>. <c>Measure</c> clamps to
/// what's available, so it can't report a minimum: these work it out from the one-line sizes just measured. Text can wrap, and
/// nothing else can shrink.
/// </summary>
internal static class MinContent
{
    internal static double Horizontal(Thickness thickness) => thickness.Left + thickness.Right;

    /// <summary>What a box of width 0 still takes: margin, padding and borders.</summary>
    internal static double OwnBox(Control control) => Horizontal(control.Margin) + control switch
    {
        ContentPresenter presenter => Horizontal(presenter.Padding) + Horizontal(presenter.BorderThickness),
        Border border => Horizontal(border.Padding) + Horizontal(border.BorderThickness),
        TemplatedControl templated => Horizontal(templated.Padding) + Horizontal(templated.BorderThickness),
        _ => 0,
    };

    /// <summary>
    /// What's in a slot, or the container an <c>ItemTemplate</c> made, which is the row's item. A hidden one is
    /// <c>display: none</c>, so it isn't one.
    /// </summary>
    internal static Control? Slotted(Control child) => child is ContentPresenter { Child: { IsVisible: true } slotted } ? slotted : null;

    /// <summary><see cref="OwnBox"/>, and a slotted child's.</summary>
    internal static double Box(Control control) =>
        OwnBox(control) + (Slotted(control) is { } child ? OwnBox(child) : 0);

    /// <summary><c>min-width: auto</c> on one of a row's items. Needs it measured on one line first.</summary>
    internal static double OfItem(Control child)
    {
        var item = Slotted(child) ?? child;

        // contain: strict. Size containment leaves it no minimum, so it shrinks with the rest. Measured.
        if (item is AvonicIcon)
        {
            return Box(child);
        }

        // overflow: hidden, which also takes it away. A TemplatedControl clips by default, so only a set value counts.
        if (item.ClipToBounds && item.IsSet(Visual.ClipToBoundsProperty))
        {
            return Box(child);
        }

        return Of(child);
    }

    /// <summary><c>min-content</c>, from the one-line sizes just measured.</summary>
    internal static double Of(Control control)
    {
        if (!double.IsNaN(control.Width))
        {
            return control.DesiredSize.Width;
        }

        if (control is TextBlock text)
        {
            return Math.Min(control.DesiredSize.Width, Horizontal(text.Margin) + Horizontal(text.Padding) + LongestWord(text));
        }

        var children = control.GetVisualChildren().OfType<Control>().Where(child => child.IsVisible).ToList();
        if (children.Count == 0)
        {
            return control.DesiredSize.Width;
        }

        // ion-row wraps, so its narrowest is its widest item's narrowest, however wide it was on one line.
        if (control is RowPanel)
        {
            return Horizontal(control.Margin) + Math.Max(control.MinWidth, children.Max(Of));
        }

        // Not Grid.ColumnDefinitions: reading it makes the collection, and a Grid measured without one then throws when it arranges.
        var adds = control is FlexRow or ButtonInner or DockPanel or StackPanel { Orientation: Orientation.Horizontal }
            || (control is Grid && children.Any(child => Grid.GetColumn(child) > 0));
        var oneLine = adds ? children.Sum(child => child.DesiredSize.Width) : children.Max(child => child.DesiredSize.Width);
        var least = adds ? children.Sum(Of) : children.Max(Of);

        return Math.Max(Horizontal(control.Margin) + control.MinWidth, control.DesiredSize.Width - oneLine + least);
    }

    /// <summary>The widest word, as a line breaks between words and keeps each whole. Infinite if it can't wrap.</summary>
    internal static double LongestWord(TextBlock text)
    {
        if (text.TextWrapping == TextWrapping.NoWrap)
        {
            return double.PositiveInfinity;
        }

        var content = text.Inlines is { Count: > 0 } inlines ? inlines.Text : text.Text;
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        using var layout = new TextLayout(
            content,
            new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch),
            text.FontSize,
            null,
            textWrapping: TextWrapping.WrapWithOverflow,
            maxWidth: 1,
            letterSpacing: text.LetterSpacing);
        return layout.Width;
    }
}
