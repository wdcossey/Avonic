using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avonic.Components.Behaviors;

/// <summary>
/// What a browser names a button or a heading from: the text inside it as it's drawn, in order, so words in inlines count, a
/// nested button's text is in the case it's drawn, and what's hidden doesn't. Something inside with a name of its own counts as
/// that, as <c>aria-label</c> does, and something raw doesn't count, as <c>aria-hidden</c> doesn't. Measured on <c>ion-item</c>.
/// </summary>
internal static class AccessibleText
{
    public static string Of(Control root)
    {
        var parts = new List<string>();
        Collect(root, root, parts);
        return string.Join(" ", parts);
    }

    private static void Collect(Visual visual, Control root, List<string> parts)
    {
        if (!visual.IsVisible)
        {
            return;
        }

        if (visual != root && visual is Control control)
        {
            if (AutomationProperties.GetAccessibilityView(control) == AccessibilityView.Raw)
            {
                return;
            }

            if (AutomationProperties.GetName(control) is { Length: > 0 } name)
            {
                parts.Add(name);
                return;
            }
        }

        if (visual is TextBlock block)
        {
            var text = block.Inlines is { Count: > 0 } inlines ? inlines.Text : block.Text;
            if (!string.IsNullOrEmpty(text))
            {
                parts.Add(text);
            }
        }

        foreach (var child in visual.GetVisualChildren())
        {
            Collect(child, root, parts);
        }
    }
}
