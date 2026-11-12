using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;

namespace Avonic.Components.Behaviors;

/// <summary>
/// A DOM element's <c>textContent</c>, as Ionic reads it to hide an empty label: every piece of text inside, shown or not, where
/// <see cref="AccessibleText"/> is what's drawn. Data is in the DOM as what its template makes.
/// </summary>
internal static class TextContent
{
    /// <summary>
    /// <c>textContent !== ''</c>: any text inside, shown or not, stopping at the first. Data is what <paramref name="presenter"/>'s
    /// template made of it, which is the presenter's child, or its text when it has none yet.
    /// </summary>
    public static bool HasText(object? content, ContentPresenter? presenter)
    {
        if (content is not (null or string or Control) && presenter?.Child is { } made)
        {
            content = made;
        }

        return Has(content);
    }

    private static bool Has(object? content) => content switch
    {
        null => false,
        string value => value.Length > 0,
        TextBlock block => (block.Inlines is { Count: > 0 } inlines ? inlines.Text : block.Text) is { Length: > 0 },
        ContentControl control => Has(control.Content),
        ILogical logical => logical.LogicalChildren.Any(Has),
        _ => content.ToString() is { Length: > 0 },
    };
}
