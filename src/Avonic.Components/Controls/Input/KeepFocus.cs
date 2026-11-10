using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// A <c>pointerdown</c> handler's <c>preventDefault()</c>: the press doesn't move focus, so the field keeps it, while the
/// control still sees the press and clicks. Avalonia's focus manager focuses the first focusable element from the press's
/// source up, in a class handler on the source as the press tunnels to it, which runs before the source's own handlers. So
/// an ancestor's tunnel handler, which comes first, makes every element on that path unfocusable until the press has been
/// handled: the button itself, and a focusable item or card around the input too.
/// </summary>
internal static class KeepFocus
{
    public static void During(PointerPressedEventArgs e)
    {
        var frames = new List<IDisposable>();
        for (var visual = e.Source as Visual; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is InputElement { Focusable: true } element)
            {
                // Above any other value, and then gone, which leaves whatever was there.
                if (element.SetValue(InputElement.FocusableProperty, false, BindingPriority.Animation) is { } frame)
                {
                    frames.Add(frame);
                }
            }
        }

        if (frames.Count > 0)
        {
            Dispatcher.UIThread.Post(() => frames.ForEach(frame => frame.Dispose()), DispatcherPriority.Send);
        }
    }
}
