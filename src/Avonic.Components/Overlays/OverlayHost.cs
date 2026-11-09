using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Overlays;

/// <summary>
/// Where a <see cref="TopLevel"/>'s overlays are, as Ionic appends them to <c>ion-app</c>: a panel in its
/// <see cref="OverlayLayer"/>, the layer's size, holding them in the order they were attached, which is Ionic's document order.
/// It has Ionic's three document listeners: Escape, the hardware back button and the focus trap. See
/// <c>docs/infra/overlays/PLAN.md</c>.
/// </summary>
internal sealed class OverlayHost : Panel
{
    private static readonly ConditionalWeakTable<TopLevel, OverlayHost> s_hosts = new();

    private readonly TopLevel _topLevel;
    private readonly OverlayLayer _layer;

    private OverlayHost(TopLevel topLevel, OverlayLayer layer)
    {
        _topLevel = topLevel;
        _layer = layer;
        layer.PropertyChanged += (_, e) =>
        {
            if (e.Property == BoundsProperty)
            {
                InvalidateMeasure();
            }
        };
        topLevel.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
        topLevel.AddHandler(TopLevel.BackRequestedEvent, OnBackRequested);
        topLevel.AddHandler(GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>
    /// A lifetime's <see cref="TopLevel"/>: a desktop's active window, else its main one; a phone's or a browser's main view's.
    /// </summary>
    /// <remarks>
    /// Only the desktop lifetime is in the assemblies the tests load, and the interfaces can't be implemented outside Avalonia,
    /// so the tests reach the desktop branch alone.
    /// </remarks>
    internal static TopLevel? TopLevelOf(IApplicationLifetime? lifetime) => lifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop => desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow,
        ISingleViewApplicationLifetime { MainView: { } view } => TopLevel.GetTopLevel(view),
        _ => null,
    };

    /// <exception cref="InvalidOperationException">The top level's template has no <see cref="VisualLayerManager"/>.</exception>
    internal static OverlayHost For(TopLevel topLevel)
    {
        var layer = OverlayLayer.GetOverlayLayer(topLevel) ?? throw new InvalidOperationException(
            "An overlay is shown in the TopLevel's OverlayLayer, and this one has none: its template has no VisualLayerManager. " +
            "Every base theme's Window has one, so an app usually has one from its FluentTheme or SimpleTheme.");
        if (s_hosts.TryGetValue(topLevel, out var host) && host._layer == layer)
        {
            return host;
        }

        host = new OverlayHost(topLevel, layer);
        s_hosts.AddOrUpdate(topLevel, host);
        layer.Children.Add(host);
        return host;
    }

    internal TopLevel TopLevel => _topLevel;

    internal void Attach(AvonicOverlay overlay)
    {
        if (!Children.Contains(overlay))
        {
            Children.Add(overlay);
        }
    }

    internal void Detach(AvonicOverlay overlay) => Children.Remove(overlay);

    /// <summary>
    /// Ionic <c>getPresentedOverlay</c>: the last attached that's showing (from its enter animation to the end of its dismiss),
    /// of those that <paramref name="filter"/> takes.
    /// </summary>
    internal AvonicOverlay? GetPresentedOverlay(Func<AvonicOverlay, bool>? filter = null) =>
        Children.OfType<AvonicOverlay>().LastOrDefault(o => o.IsShown && (filter is null || filter(o)));

    /// <summary>The layer is a <c>Canvas</c>, which measures a child with no limit and gives it its desired size: the layer's.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = _layer.AvailableSize;
        base.MeasureOverride(size);
        return size;
    }

    /// <summary>Ionic's <c>keydown</c> listener: Escape dismisses the top overlay with the backdrop role, if it allows it. Nothing is prevented.</summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && GetPresentedOverlay() is { BackdropDismissEnabled: true } top)
        {
            _ = top.Dismiss(null, OverlayRoles.Backdrop);
        }
    }

    /// <summary>
    /// Ionic's <c>ionBackButton</c> listener, at priority 100: the same as Escape. Handled when it dismisses, as Ionic's registered
    /// handler stops the app's own back; an app's handler that has already handled it keeps it.
    /// </summary>
    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (GetPresentedOverlay() is { BackdropDismissEnabled: true } top)
        {
            e.Handled = true;
            _ = top.Dismiss(null, OverlayRoles.Backdrop);
        }
    }

    // ── The focus trap: Ionic's trapKeyboardFocus ─────────────────────────────────────────────

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is not IInputElement target || GetPresentedOverlay(o => o.StealsFocus) is not { } last || !last.FocusTrapEnabled)
        {
            return;
        }

        var method = e.NavigationMethod;
        if (last.FocusTrapWrapper is { } wrapper)
        {
            TrapScopedFocus(last, wrapper, target, method);
        }
        else
        {
            TrapShadowFocus(last, target, method);
        }
    }

    private void TrapScopedFocus(AvonicOverlay last, Control wrapper, IInputElement target, NavigationMethod method)
    {
        if (ReferenceEquals(target, last))
        {
            last.LastFocus = null;
        }
        else if (IsInToast(target))
        {
            FocusElementInOverlay(last.LastFocus, last, method);
        }
        else
        {
            if (!Contains(last, target))
            {
                return;
            }

            if (Contains(wrapper, target) || ReferenceEquals(target, last.GetVisualDescendants().OfType<AvonicBackdrop>().FirstOrDefault()))
            {
                last.LastFocus = target;
            }
            else
            {
                // A sentinel: wrap to the first, or to the last if the first is where focus came from (Shift+Tab).
                var lastFocus = last.LastFocus;
                FocusFirstDescendant(wrapper, last, method);
                if (ReferenceEquals(lastFocus, Focused))
                {
                    FocusLastDescendant(wrapper, last, method);
                }

                last.LastFocus = Focused;
            }
        }
    }

    private void TrapShadowFocus(AvonicOverlay last, IInputElement target, NavigationMethod method)
    {
        if (Contains(last, target))
        {
            last.LastFocus = target;
        }
        else if (IsInToast(target))
        {
            FocusElementInOverlay(last.LastFocus, last, method);
        }
        else
        {
            var lastFocus = last.LastFocus;
            FocusFirstDescendant(last, last, method);
            if (ReferenceEquals(lastFocus, Focused))
            {
                FocusLastDescendant(last, last, method);
            }

            last.LastFocus = Focused;
        }
    }

    private IInputElement? Focused => _topLevel.FocusManager?.GetFocusedElement();

    /// <summary>Ionic's <c>target.tagName === 'ION-TOAST'</c>: a toast's own content is retargeted to it by its shadow root.</summary>
    private static bool IsInToast(IInputElement target) =>
        target is Visual visual && visual.GetSelfAndVisualAncestors().OfType<AvonicOverlay>().FirstOrDefault() is { StealsFocus: false };

    /// <summary>Ionic's <c>contains</c>, which includes the element itself.</summary>
    private static bool Contains(Visual container, IInputElement target) =>
        target is Visual visual && (visual == container || container.IsVisualAncestorOf(visual));

    /// <summary>Ionic's <c>focusableQueryString</c> in Avalonia's words: what Tab would stop at, in visual-tree order.</summary>
    private static IEnumerable<InputElement> Focusables(Visual container) =>
        container.GetVisualDescendants().OfType<InputElement>()
            .Where(e => e.Focusable && e.IsEffectivelyEnabled && e.IsEffectivelyVisible && KeyboardNavigation.GetIsTabStop(e));

    private static void FocusFirstDescendant(Visual container, AvonicOverlay fallback, NavigationMethod method) =>
        FocusElementInContext(Focusables(container).FirstOrDefault(), fallback, method);

    private static void FocusLastDescendant(Visual container, AvonicOverlay fallback, NavigationMethod method) =>
        FocusElementInContext(Focusables(container).LastOrDefault(), fallback, method);

    /// <summary>Ionic <c>focusElementInContext</c>: the element, with its navigation method so a Tab wrap shows the ring, else the fallback.</summary>
    private static void FocusElementInContext(IInputElement? element, AvonicOverlay fallback, NavigationMethod method)
    {
        if (element is not null)
        {
            element.Focus(method);
        }
        else
        {
            fallback.Focus();
        }
    }

    /// <summary>Ionic <c>focusElementInOverlay</c>.</summary>
    private static void FocusElementInOverlay(IInputElement? element, AvonicOverlay overlay, NavigationMethod method) =>
        FocusElementInContext(element, overlay, method);
}
