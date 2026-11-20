using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Extension: the hardware back button pops the nav. Ionic's core nav doesn't listen to <c>ionBackButton</c> (measured): its
/// router outlets do, in the frameworks and <c>ion-router</c>, at priority 0. Avonic has no router, so the nav does it.
/// </summary>
public partial class AvonicNav
{
    /// <summary>
    /// Extension: whether a back request (Android's back, a mouse's back button, Alt+Left) pops this nav while it can go back.
    /// Default true. Off, it leaves the request to the app. An app can also take the request first by registering on
    /// <see cref="HardwareBackButton.IonBackButtonEvent"/> above priority 0.
    /// </summary>
    public static readonly StyledProperty<bool> HandleBackButtonProperty =
        AvaloniaProperty.Register<AvonicNav, bool>(nameof(HandleBackButton), true);

    /// <summary>The priority the nav takes a back request at: Ionic's router's, under an overlay's 100 and a menu's 99.</summary>
    public const int BackButtonPriority = 0;

    private static readonly ConditionalWeakTable<TopLevel, BackListener> s_listeners = new();
    private TopLevel? _backTopLevel;

    public bool HandleBackButton
    {
        get => GetValue(HandleBackButtonProperty);
        set => SetValue(HandleBackButtonProperty, value);
    }

    /// <summary>Whether this nav takes a back request now: on, shown, and with a page to go back to.</summary>
    private bool TakesBack => HandleBackButton && IsEffectivelyVisible && CanGoBack();

    private void AttachBack(TopLevel? topLevel)
    {
        DetachBack();
        if (topLevel is null)
        {
            return;
        }

        _backTopLevel = topLevel;
        s_listeners.GetValue(topLevel, top => new BackListener(top)).Navs.Add(this);
    }

    private void DetachBack()
    {
        if (_backTopLevel is { } topLevel && s_listeners.TryGetValue(topLevel, out var listener))
        {
            listener.Navs.Remove(this);
        }

        _backTopLevel = null;
    }

    /// <summary>
    /// One per top level: of the navs in it that take a back request, the innermost, as an inner nav's outlet registers after its
    /// outer one's and Ionic keeps the later of equal priorities. Inside an <see cref="AvonicApp"/>, it registers at <see cref="BackButtonPriority"/>;
    /// without one, it takes the back request itself, unless an overlay or a menu would.
    /// </summary>
    private sealed class BackListener
    {
        private readonly TopLevel _topLevel;

        public BackListener(TopLevel topLevel)
        {
            _topLevel = topLevel;
            topLevel.AddHandler(HardwareBackButton.IonBackButtonEvent, OnIonBackButton);
            topLevel.AddHandler(TopLevel.BackRequestedEvent, OnBackRequested);
        }

        public List<AvonicNav> Navs { get; } = [];

        /// <summary>
        /// The innermost nav that takes the request: the last attached of those, as a nav inside another's page is attached after
        /// it (a parent attaches before its children), and is removed when it's detached. While an overlay is presented (a toast
        /// aside, as for the overlays' own back), only a nav inside it: one behind it never pops, even under an overlay that takes
        /// no back request itself (<c>BackdropDismiss</c> off), where Ionic's router would go back behind it.
        /// </summary>
        private AvonicNav? Target()
        {
            var overlay = OverlayHost.BackTargetIn(_topLevel);
            return Navs.LastOrDefault(nav => nav.TakesBack && (overlay is null || overlay.IsVisualAncestorOf(nav)));
        }

        private void OnIonBackButton(object? sender, BackButtonEventArgs e)
        {
            if (Target() is { } nav)
            {
                e.Register(BackButtonPriority, _ => nav.Pop(new NavOptions { SkipIfBusy = true }));
            }
        }

        private void OnBackRequested(object? sender, RoutedEventArgs e)
        {
            if (e.Handled || HardwareBackButton.IsStarted(_topLevel) || !AvonicConfig.HardwareBackButton
                || OverlayHost.BackTargetIn(_topLevel) is { BackdropDismissEnabled: true }
                || MenuController.GetOpenIn(_topLevel) is not null
                || Target() is not { } nav)
            {
                return;
            }

            e.Handled = true;
            _ = nav.Pop(new NavOptions { SkipIfBusy = true });
        }
    }
}
