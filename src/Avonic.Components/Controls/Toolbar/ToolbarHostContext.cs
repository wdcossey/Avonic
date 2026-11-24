using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>hostContext('ion-toolbar', el)</c> and <c>hostContext('ion-toolbar[color]', el)</c>, from <c>utils/theme</c>, which
/// are <c>el.closest(selector) !== null</c>: a toolbar anywhere around the control, and a toolbar anywhere around it with a
/// <c>color</c> attribute, even an empty one, and not only the nearest. Here, the control's visual ancestors, so a slot or a part
/// of a toolbar's template counts, and a toolbar whose <see cref="AvonicToolbar.Color"/> isn't null. Outside the tree, neither.
/// </summary>
/// <remarks>
/// Ionic reads it in a control's <c>render</c>, and a toolbar's color doesn't render what's in it, so each control reads it at
/// its own render: when it's attached and when one of its own props changes. <see cref="AvonicButton"/>,
/// <see cref="AvonicMenuButton"/>, <see cref="AvonicBackButton"/>, <see cref="AvonicSegment"/> and
/// <see cref="AvonicSegmentButton"/> set both classes; <see cref="AvonicBreadcrumbs"/> and <see cref="AvonicBreadcrumb"/> each set
/// only the one their md CSS reads.
/// </remarks>
internal readonly record struct ToolbarHostContext(bool InToolbar, bool InToolbarColor)
{
    public const string InToolbarPseudoClass = ":in-toolbar";

    public const string InToolbarColorPseudoClass = ":in-toolbar-color";

    /// <summary>Both, from one walk up the control's visual ancestors.</summary>
    public static ToolbarHostContext Of(Visual control)
    {
        var inToolbar = false;
        foreach (var toolbar in control.GetVisualAncestors().OfType<AvonicToolbar>())
        {
            inToolbar = true;
            if (toolbar.Color is not null)
            {
                return new ToolbarHostContext(true, true);
            }
        }

        return new ToolbarHostContext(inToolbar, false);
    }

    /// <summary><c>render</c>'s host classes <c>in-toolbar</c> and <c>in-toolbar-color</c>, as the control's pseudoclasses.</summary>
    public static void Set(IPseudoClasses classes, Visual control)
    {
        var context = Of(control);
        classes.Set(InToolbarPseudoClass, context.InToolbar);
        classes.Set(InToolbarColorPseudoClass, context.InToolbarColor);
    }
}
