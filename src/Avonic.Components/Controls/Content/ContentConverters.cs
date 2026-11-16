using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>Converters for the content's scroll part's template.</summary>
internal static class ContentConverters
{
    /// <summary>
    /// A brush becomes transparent, and none stays none: the presenter takes input where the scroller has a background, as before,
    /// while a part under the focus ring paints it, as an element's own background is under its outline in CSS.
    /// </summary>
    public static readonly IValueConverter TransparentIfSet =
        new FuncValueConverter<IBrush?, IBrush?>(brush => brush is null ? null : Brushes.Transparent);

    /// <summary>
    /// Whether an axis draws no scroll bar, so its indicator shows: a bar is drawn only where it's <see cref="ScrollBarVisibility.Auto"/>
    /// or <see cref="ScrollBarVisibility.Visible"/>, and a browser draws one or the other, never both.
    /// </summary>
    public static readonly IValueConverter NoScrollBar =
        new FuncValueConverter<ScrollBarVisibility, bool>(visibility => visibility is ScrollBarVisibility.Hidden or ScrollBarVisibility.Disabled);
}
