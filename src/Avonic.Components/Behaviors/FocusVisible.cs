using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

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
            // A ring SetFocus gave stays through focus moved by code, as Ionic's focusin does nothing outside keyboard mode.
            if (!IsSetByCode(surface))
            {
                Clear(surface);
            }

            return;
        }

        ((IPseudoClasses)surface.Classes).Add(FocusedPseudoClass);

        // Only a surface showing it listens to the window, so a long list costs nothing per press. One that SetFocus gave it
        // becomes a keyboard one now, so that focus leaving it clears it, as Ionic's focusin in keyboard mode replaces the set.
        if (surface.GetValue(WatchProperty) is not { SetByCode: false } && TopLevel.GetTopLevel(surface) is { } topLevel)
        {
            surface.GetValue(WatchProperty)?.Dispose();
            surface.SetValue(WatchProperty, new WindowWatch(surface, topLevel));
        }
    }

    private static void OnLostFocus(object? sender, FocusChangedEventArgs e)
    {
        // A ring SetFocus gave goes only when focus moves by keyboard or leaves everything, which its window watch hears.
        if (sender is Control surface && !IsSetByCode(surface))
        {
            Clear(surface);
        }
    }

    private static bool IsSetByCode(Control surface) => surface.GetValue(WatchProperty) is { SetByCode: true };

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

    /// <summary>
    /// Ionic's <c>setFocus(elements)</c> from <c>startFocusVisible</c>, which <c>ion-app</c>'s <c>setFocus</c> calls:
    /// <c>:ion-focused</c> comes off every element in <paramref name="topLevel"/> that has it, and goes on each of
    /// <paramref name="elements"/>, focusable surface or not, without moving focus. They keep it until a press, a key outside
    /// <c>FOCUS_KEYS</c>, focus moving by keyboard (Ionic's keyboard-mode <c>focusin</c>), focus leaving everything
    /// (<c>focusout</c> to <c>body</c>), or the next call. Focus moved by code leaves it, as Ionic's <c>focusin</c> does outside
    /// keyboard mode.
    /// </summary>
    internal static void SetFocus(TopLevel topLevel, IReadOnlyList<Control> elements)
    {
        foreach (var surface in Showing(topLevel))
        {
            Clear(surface);
        }

        foreach (var element in elements)
        {
            ((IPseudoClasses)element.Classes).Add(FocusedPseudoClass);
            element.GetValue(WatchProperty)?.Dispose();
            element.SetValue(WatchProperty, new WindowWatch(element, topLevel, setByCode: true));
        }
    }

    /// <summary>What shows <c>:ion-focused</c> in <paramref name="topLevel"/>: Ionic's <c>currentFocus</c>.</summary>
    private static List<Control> Showing(TopLevel topLevel) =>
        s_showing.Select(pair => pair.Key).Where(c => c.GetValue(WatchProperty) is { } watch && watch.TopLevel == topLevel).ToList();

    /// <summary>Every surface with a watch, held weakly, so that <see cref="SetFocus"/> can find them.</summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, object?> s_showing = new();

    /// <summary><c>pointerDown</c> and <c>onKeydown</c>, which Ionic listens for on the document.</summary>
    private sealed class WindowWatch : IDisposable
    {
        private readonly Control _surface;
        private readonly TopLevel _topLevel;

        private readonly bool _setByCode;

        public WindowWatch(Control surface, TopLevel topLevel, bool setByCode = false)
        {
            _surface = surface;
            _topLevel = topLevel;
            _setByCode = setByCode;
            s_showing.AddOrUpdate(surface, null);
            topLevel.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            topLevel.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            if (setByCode)
            {
                // A surface's own focus handlers clear it when it has focus; one given by SetFocus needs the window's.
                topLevel.AddHandler(InputElement.GotFocusEvent, OnAnyGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
                topLevel.AddHandler(InputElement.LostFocusEvent, OnAnyLostFocus, RoutingStrategies.Bubble, handledEventsToo: true);
                surface.DetachedFromVisualTree += OnSurfaceDetached;
            }
        }

        public TopLevel TopLevel => _topLevel;

        public bool SetByCode => _setByCode;

        public void Dispose()
        {
            s_showing.Remove(_surface);
            _topLevel.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            _topLevel.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            if (_setByCode)
            {
                _topLevel.RemoveHandler(InputElement.GotFocusEvent, OnAnyGotFocus);
                _topLevel.RemoveHandler(InputElement.LostFocusEvent, OnAnyLostFocus);
                _surface.DetachedFromVisualTree -= OnSurfaceDetached;
            }
        }

        /// <summary>Ionic's keyboard-mode <c>focusin</c>: <c>setFocus</c> with the new focus's surfaces, which their own handlers add.</summary>
        private void OnAnyGotFocus(object? sender, FocusChangedEventArgs e)
        {
            if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional
                && !(e.Source is Visual source && source.GetSelfAndVisualAncestors().Contains(_surface) && GetIsEnabled(_surface)))
            {
                Clear(_surface);
            }
        }

        /// <summary>Ionic's <c>focusout</c> when <c>document.activeElement</c> is then the body: focus has left everything.</summary>
        private void OnAnyLostFocus(object? sender, RoutedEventArgs e)
        {
            if (_topLevel.FocusManager?.GetFocusedElement() is null)
            {
                Clear(_surface);
            }
        }

        private void OnSurfaceDetached(object? sender, VisualTreeAttachmentEventArgs e) => Clear(_surface);

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
