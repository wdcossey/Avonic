using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Threading;

namespace Avonic.Components.Controls;

/// <summary>
/// A <c>pointerdown</c> handler's <c>preventDefault()</c>: the press doesn't move focus, so the field keeps it, while the
/// control still sees the press and clicks. Avalonia focuses the first focusable element from the press's source up, when the
/// press reaches it, so the control stops being focusable until the press has been handled, and no further: an ancestor
/// that's focusable would take it instead, which none of the input's is.
/// </summary>
internal static class KeepFocus
{
    public static void During(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not InputElement element || !element.Focusable)
        {
            return;
        }

        // Above any other value, and then gone, which leaves whatever was there.
        var frame = element.SetValue(InputElement.FocusableProperty, false, BindingPriority.Animation);
        Dispatcher.UIThread.Post(() => frame?.Dispose(), DispatcherPriority.Send);
    }
}
