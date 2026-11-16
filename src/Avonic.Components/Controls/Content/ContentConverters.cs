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
}
