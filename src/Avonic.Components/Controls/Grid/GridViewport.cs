using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's breakpoints, from <c>utils/media.js</c>: <c>matchMedia</c> and <c>@media (min-width)</c> read the window's width,
/// whatever the grid is in. Here that's the <see cref="TopLevel"/>'s client width: the screen on a phone, the window on the
/// desktop. A <see cref="PopupRoot"/> is a <see cref="TopLevel"/> too, so a popup uses the one it belongs to, as a browser has
/// one window. One of these per <see cref="TopLevel"/> holds the breakpoint, and says when it changes, so a resize that
/// doesn't cross one costs nothing.
/// </summary>
internal sealed class GridViewport
{
    /// <summary><c>SIZE_TO_MEDIA</c>: the minimum widths of <c>xs</c>, <c>sm</c>, <c>md</c>, <c>lg</c> and <c>xl</c>.</summary>
    internal static readonly double[] MinWidths = [0, 576, 768, 992, 1200];

    /// <summary>
    /// Set from each breakpoint up, as <c>min-width</c> queries match, so a theme's later rule wins as the later <c>@media</c>
    /// does. <c>xs</c> always matches, so it has none: it's the plain rule.
    /// </summary>
    internal static readonly string[] PseudoClasses = [":breakpoint-sm", ":breakpoint-md", ":breakpoint-lg", ":breakpoint-xl"];

    private static readonly ConditionalWeakTable<TopLevel, GridViewport> Viewports = new();

    private GridViewport(TopLevel topLevel)
    {
        Breakpoint = BreakpointFor(topLevel.ClientSize.Width);
        topLevel.PropertyChanged += (_, e) =>
        {
            if (e.Property == TopLevel.ClientSizeProperty)
            {
                var breakpoint = BreakpointFor(topLevel.ClientSize.Width);
                if (breakpoint != Breakpoint)
                {
                    Breakpoint = breakpoint;
                    Changed?.Invoke();
                }
            }
        };
    }

    /// <summary>0 for <c>xs</c>, up to 4 for <c>xl</c>.</summary>
    public int Breakpoint { get; private set; }

    public event Action? Changed;

    /// <summary>The viewport <paramref name="control"/> is in, or null if it isn't in one.</summary>
    public static GridViewport? Of(Visual control)
    {
        var topLevel = TopLevel.GetTopLevel(control);
        while (topLevel is PopupRoot { ParentTopLevel: { } parent })
        {
            topLevel = parent;
        }

        return topLevel is null ? null : Viewports.GetValue(topLevel, key => new GridViewport(key));
    }

    public static int BreakpointFor(double width)
    {
        var breakpoint = 0;
        for (var i = 1; i < MinWidths.Length; i++)
        {
            if (width >= MinWidths[i])
            {
                breakpoint = i;
            }
        }

        return breakpoint;
    }

    public static void SetPseudoClasses(IPseudoClasses classes, int breakpoint)
    {
        for (var i = 0; i < PseudoClasses.Length; i++)
        {
            classes.Set(PseudoClasses[i], breakpoint > i);
        }
    }
}
