using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avonic.Components.Behaviors;

/// <summary>
/// What a browser names a button or a heading from: the text inside it as it's drawn, in order, so words in inlines count, a
/// nested button's text is in the case it's drawn, and what's hidden doesn't. Measured on <c>ion-item</c>.
/// </summary>
internal static class AccessibleText
{
    public static string Of(Control root) =>
        string.Join(
            " ",
            root.GetSelfAndVisualDescendants()
                .OfType<TextBlock>()
                .Where(block => block.IsEffectivelyVisible)
                .Select(block => block.Inlines is { Count: > 0 } inlines ? inlines.Text : block.Text)
                .Where(text => !string.IsNullOrEmpty(text)));
}
