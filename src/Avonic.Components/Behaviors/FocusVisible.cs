using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avonic.Components.Behaviors;

/// <summary>
/// Ionic's <c>focus-visible</c> utility as an attached behaviour. A surface with
/// <c>FocusVisible.IsEnabled="True"</c> (Ionic: class <c>ion-focusable</c>) gets the
/// <c>:ion-focused</c> pseudoclass when focus reaches it, or anything inside it, by keyboard.
/// </summary>
/// <remarks>
/// As in <c>utils/focus-visible.js</c>, every focusable surface around the focused element gets it, and
/// a press anywhere or a key outside <c>FOCUS_KEYS</c> clears it. Ionic tracks a global keyboard mode
/// to tell how focus arrived. Avalonia reports that directly, so programmatic focus doesn't show it.
/// </remarks>
public static class FocusVisible
{
    public const string FocusedPseudoClass = ":ion-focused";

    /// <summary>Ionic class <c>ion-focusable</c>.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(FocusVisible));

    private static readonly AttachedProperty<WindowWatch?> WatchProperty =
        AvaloniaProperty.RegisterAttached<Control, WindowWatch?>("Watch", typeof(FocusVisible));

    /// <summary><c>FOCUS_KEYS</c>.</summary>
    private static readonly HashSet<Key> FocusKeys =
    [
        Key.Tab, Key.Down, Key.Space, Key.Escape, Key.LeftShift, Key.RightShift, Key.Enter,
        Key.Left, Key.Right, Key.Up, Key.Home, Key.End,
    ];

    public static bool GetIsEnabled(Control element) => element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Control element, bool value) => element.SetValue(IsEnabledProperty, value);

    static FocusVisible()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>((surface, e) =>
        {
            if (e.GetNewValue<bool>())
            {
                surface.AddHandler(InputElement.GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
                surface.AddHandler(InputElement.LostFocusEvent, OnLostFocus, RoutingStrategies.Bubble, handledEventsToo: true);
                surface.DetachedFromVisualTree += OnDetached;
            }
            else
            {
                surface.RemoveHandler(InputElement.GotFocusEvent, OnGotFocus);
                surface.RemoveHandler(InputElement.LostFocusEvent, OnLostFocus);
                surface.DetachedFromVisualTree -= OnDetached;
                Clear(surface);
            }
        });
    }

    private static void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is not Control surface)
        {
            return;
        }

        if (e.NavigationMethod is not (NavigationMethod.Tab or NavigationMethod.Directional))
        {
            Clear(surface);
            return;
        }

        ((IPseudoClasses)surface.Classes).Add(FocusedPseudoClass);

        // Only a surface showing it listens to the window, so a long list costs nothing per press.
        if (surface.GetValue(WatchProperty) is null && TopLevel.GetTopLevel(surface) is { } topLevel)
        {
            surface.SetValue(WatchProperty, new WindowWatch(surface, topLevel));
        }
    }

    private static void OnLostFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is Control surface)
        {
            Clear(surface);
        }
    }

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control surface)
        {
            Clear(surface);
        }
    }

    private static void Clear(Control surface)
    {
        ((IPseudoClasses)surface.Classes).Remove(FocusedPseudoClass);
        surface.GetValue(WatchProperty)?.Dispose();
        surface.ClearValue(WatchProperty);
    }

    /// <summary><c>pointerDown</c> and <c>onKeydown</c>, which Ionic listens for on the document.</summary>
    private sealed class WindowWatch : IDisposable
    {
        private readonly Control _surface;
        private readonly TopLevel _topLevel;

        public WindowWatch(Control surface, TopLevel topLevel)
        {
            _surface = surface;
            _topLevel = topLevel;
            topLevel.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            topLevel.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        public void Dispose()
        {
            _topLevel.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            _topLevel.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e) => Clear(_surface);

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!FocusKeys.Contains(e.Key))
            {
                Clear(_surface);
            }
        }
    }
}
