using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
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
        topLevel.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
        topLevel.AddHandler(TopLevel.BackRequestedEvent, OnBackRequested);
        topLevel.AddHandler(HardwareBackButton.IonBackButtonEvent, OnIonBackButton);
        topLevel.AddHandler(GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>
    /// A lifetime's <see cref="TopLevel"/>: a desktop's active window, else its main one; a single view's main view's (iOS, the
    /// browser, and Android when the app sets <c>MainView</c>); under Android's <c>MainViewFactory</c>, which leaves <c>MainView</c>
    /// null, the last top level to come that's still open, which is the current activity's.
    /// </summary>
    /// <remarks>
    /// Android's lifetime is both an <see cref="ISingleViewApplicationLifetime"/> and an <see cref="IActivityApplicationLifetime"/>,
    /// so a <c>MainView</c> it's given comes first, as it's the view every activity shows; while it's out of every top level, as
    /// when an activity is recreated, the last open is still the current activity's.
    /// </remarks>
    internal static TopLevel? TopLevelOf(IApplicationLifetime? lifetime) => lifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop => desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow,
        ISingleViewApplicationLifetime { MainView: { } view } when TopLevel.GetTopLevel(view) is { } topLevel => topLevel,
        IActivityApplicationLifetime => OpenTopLevels.Last,
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

    /// <summary>
    /// Ionic <c>getPresentedOverlay(document)</c> for <paramref name="topLevel"/>, without making it a host: the top overlay there, or
    /// null. For what isn't an overlay and defers to one, as <c>ion-menu</c>'s focus trap and back button do.
    /// </summary>
    internal static AvonicOverlay? PresentedOverlayIn(TopLevel topLevel) =>
        s_hosts.TryGetValue(topLevel, out var host) && host._layer == OverlayLayer.GetOverlayLayer(topLevel)
            ? host.GetPresentedOverlay()
            : null;

    /// <summary>Ionic's <c>document.querySelector('ion-modal.show-modal')</c>, for <paramref name="topLevel"/>: a modal is presented there.</summary>
    internal static bool IsModalPresentedIn(TopLevel topLevel) =>
        s_hosts.TryGetValue(topLevel, out var host) && host._layer == OverlayLayer.GetOverlayLayer(topLevel)
        && host.Children.OfType<AvonicModal>().Any(m => m.IsShown);

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

    /// <summary>
    /// The top overlay that Escape and back are for: the top one that isn't a toast. Ionic's listeners take every kind, so a toast
    /// on top, which has no <c>backdropDismiss</c>, stops them dismissing the overlay under it; Avonic doesn't copy that
    /// (<c>docs/infra/ionic-issues.md</c>, #38).
    /// </summary>
    private AvonicOverlay? BackTarget => GetPresentedOverlay(o => o is not AvonicToast);

    /// <summary>
    /// <see cref="BackTarget"/> for <paramref name="topLevel"/>, without making it a host: what defers to an overlay on back, as
    /// <c>ion-menu</c>'s back does, takes the same one as the overlays' own back.
    /// </summary>
    internal static AvonicOverlay? BackTargetIn(TopLevel topLevel) =>
        s_hosts.TryGetValue(topLevel, out var host) && host._layer == OverlayLayer.GetOverlayLayer(topLevel)
            ? host.BackTarget
            : null;

    /// <summary>
    /// Ionic's <c>keydown</c> listener on the document: Escape dismisses the top overlay with the backdrop role, if it allows it.
    /// Nothing is prevented. A key a control has handled, as a <c>ComboBox</c> handles Escape to close its drop-down, doesn't
    /// come, as a stopped <c>keydown</c> never reaches the document.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && BackTarget is { BackdropDismissEnabled: true } top)
        {
            top.DismissFromInput(OverlayRoles.Backdrop);
        }
    }

    /// <summary>
    /// Ionic's <c>ionBackButton</c> listener: the top overlay, if its backdrop can dismiss it, registers at priority 100 to dismiss
    /// it, the same as Escape. Its dismissal isn't waited for, as Ionic's handler doesn't return it, so that a back press can still
    /// reach an alert a modal's <c>canDismiss</c> shows.
    /// </summary>
    private void OnIonBackButton(object? sender, BackButtonEventArgs e)
    {
        if (BackTarget is { BackdropDismissEnabled: true } top)
        {
            e.Register(HardwareBackButton.OverlayBackButtonPriority, _ => top.DismissFromInput(OverlayRoles.Backdrop));
        }
    }

    /// <summary>
    /// With no <see cref="AvonicApp"/> to raise <see cref="HardwareBackButton.IonBackButtonEvent"/>, the same straight from the
    /// back request, so that an overlay dismisses on back without one: handled when it dismisses, as Ionic's registered handler
    /// stops the app's own back. An app's handler that has already handled it keeps it. Nothing with
    /// <see cref="AvonicConfig.HardwareBackButton"/> off.
    /// </summary>
    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (HardwareBackButton.IsStarted(_topLevel) || !AvonicConfig.HardwareBackButton)
        {
            return;
        }

        if (BackTarget is { BackdropDismissEnabled: true } top)
        {
            e.Handled = true;
            top.DismissFromInput(OverlayRoles.Backdrop);
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
            FocusElementInContext(last.LastFocus, last, method);
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
            FocusElementInContext(last.LastFocus, last, method);
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

    private IInputElement? Focused => AvonicOverlay.Focused(_topLevel);

    /// <summary>Ionic's <c>target.tagName === 'ION-TOAST'</c>: a toast's own content is retargeted to it by its shadow root.</summary>
    private static bool IsInToast(IInputElement target) =>
        target is Visual visual && visual.GetSelfAndVisualAncestors().OfType<AvonicOverlay>().FirstOrDefault() is { StealsFocus: false };

    /// <summary>
    /// Ionic's <c>contains</c>, which includes the element itself. A popup opened from inside, such as a <c>ComboBox</c>'s
    /// drop-down, is inside too: it's in another layer or window, but its logical parent is in the container. In Ionic the
    /// drop-down of an <c>ion-select</c> is an overlay of its own, on top.
    /// </summary>
    internal static bool Contains(Visual container, IInputElement target) =>
        target is Visual visual && (visual == container || container.IsVisualAncestorOf(visual) || container.IsLogicalAncestorOf(visual));

    /// <summary>
    /// Ionic's <c>focusableQueryString</c> in Avalonia's words: what Tab would stop at, in visual-tree order. Not an
    /// <see cref="AvonicContent"/>'s scroller, which Tab stops at as Chromium's does, but which the query, of elements and
    /// <c>tabindex</c>, never finds in its shadow root.
    /// </summary>
    internal static IEnumerable<InputElement> Focusables(Visual container) =>
        container.GetVisualDescendants().OfType<InputElement>()
            .Where(e => e.Focusable && e.IsEffectivelyEnabled && e.IsEffectivelyVisible && KeyboardNavigation.GetIsTabStop(e)
                && e is not ScrollViewer { TemplatedParent: AvonicContent });

    internal static void FocusFirstDescendant(Visual container, InputElement fallback, NavigationMethod method) =>
        FocusElementInContext(Focusables(container).FirstOrDefault(), fallback, method);

    internal static void FocusLastDescendant(Visual container, InputElement fallback, NavigationMethod method) =>
        FocusElementInContext(Focusables(container).LastOrDefault(), fallback, method);

    /// <summary>Ionic <c>focusElementInContext</c>, and <c>focusElementInOverlay</c>, which is the same: the element, with its navigation method so a Tab wrap shows the ring, else the fallback.</summary>
    private static void FocusElementInContext(IInputElement? element, InputElement fallback, NavigationMethod method)
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
}
