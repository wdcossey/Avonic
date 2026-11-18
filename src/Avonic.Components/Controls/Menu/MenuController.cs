using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>menuController</c> (<c>utils/menu-controller</c>): the menus that are attached, in the order they were, one open at a
/// time, the animation for each <see cref="MenuType"/>, and the back button for an open menu. One per process, as Ionic's is one per
/// document. See <c>docs/components/ion-menu/README.md</c>.
/// </summary>
/// <remarks>
/// Ionic's methods are async because they wait for every <c>ion-menu</c> in the document to be ready; a menu here is ready once it's
/// attached, so the ones that only read are synchronous. <see cref="Open"/>, <see cref="Close"/> and <see cref="Toggle"/> complete
/// when the menu's animation does.
/// </remarks>
public static class MenuController
{
    private static readonly List<AvonicMenu> s_menus = [];
    private static readonly Dictionary<MenuType, MenuAnimationBuilder> s_animations = new()
    {
        [MenuType.Reveal] = MenuAnimations.Reveal,
        [MenuType.Push] = MenuAnimations.Push,
        [MenuType.Overlay] = MenuAnimations.Overlay,
    };

    // The top levels the back button is listened to on: Ionic's one listener on the document, one per window here.
    private static readonly ConditionalWeakTable<TopLevel, object> s_listening = new();

    /// <summary>
    /// Ionic's <c>ionMenuChange</c>, which <c>ion-menu-button</c> and <c>ion-menu-toggle</c> listen to on <c>body</c>: a menu's
    /// <c>disabled</c> changed, or it loaded or was attached again. Internal, as Ionic's event is.
    /// </summary>
    internal static event EventHandler? MenuChanged;

    /// <summary>Ionic <c>open(menu)</c>: opens the menu <see cref="Get"/> finds. False if there's none, or it didn't open.</summary>
    public static Task<bool> Open(string? menu = null) =>
        Get(menu, logOnMultipleSideMenus: true) is { } found ? found.Open() : Task.FromResult(false);

    /// <summary>Ionic <c>close(menu)</c>: closes the menu <see cref="Get"/> finds, or with none given, the open one.</summary>
    public static Task<bool> Close(string? menu = null) =>
        (menu is not null ? Get(menu, logOnMultipleSideMenus: true) : GetOpen()) is { } found ? found.Close() : Task.FromResult(false);

    /// <summary>Ionic <c>toggle(menu)</c>: opens or closes the menu <see cref="Get"/> finds.</summary>
    public static Task<bool> Toggle(string? menu = null) =>
        Get(menu, logOnMultipleSideMenus: true) is { } found ? found.Toggle() : Task.FromResult(false);

    /// <summary>Ionic <c>enable(shouldEnable, menu)</c>: sets the menu's <see cref="Control.IsEnabled"/>, and returns it.</summary>
    public static AvonicMenu? Enable(bool shouldEnable, string? menu = null)
    {
        var found = Get(menu);
        found?.SetCurrentValue(InputElement.IsEnabledProperty, shouldEnable);
        return found;
    }

    /// <summary>Ionic <c>swipeGesture(shouldEnable, menu)</c>: sets the menu's <see cref="AvonicMenu.MenuSwipeGesture"/>, and returns it.</summary>
    public static AvonicMenu? SwipeGesture(bool shouldEnable, string? menu = null)
    {
        var found = Get(menu);
        found?.SetCurrentValue(AvonicMenu.MenuSwipeGestureProperty, shouldEnable);
        return found;
    }

    /// <summary>Ionic <c>isOpen(menu)</c>: whether that menu is open, or with none given, whether any is.</summary>
    public static bool IsOpen(string? menu = null) =>
        menu is not null ? Get(menu) is { } found && found.IsOpen() : GetOpen() is not null;

    /// <summary>Ionic <c>isEnabled(menu)</c>: whether the menu <see cref="Get"/> finds is enabled.</summary>
    public static bool IsEnabled(string? menu = null) => Get(menu) is { } found && found.IsEnabled;

    /// <summary>
    /// Ionic <c>get(menu)</c>. <c>start</c> or <c>end</c>: the first enabled menu on that side, else the first on that side. Any other
    /// string: the menu whose <see cref="AvonicMenu.MenuId"/> it is, or none. Null: the first enabled menu, else the first.
    /// </summary>
    public static AvonicMenu? Get(string? menu = null) => Get(menu, logOnMultipleSideMenus: false);

    /// <summary>Ionic <c>getOpen()</c>: the open menu, or null.</summary>
    public static AvonicMenu? GetOpen() => s_menus.Find(m => m.IsOpenState);

    /// <summary>Ionic <c>getMenus()</c>: every menu that's attached and registered, in the order they were.</summary>
    public static IReadOnlyList<AvonicMenu> GetMenus() => s_menus.ToArray();

    /// <summary>Ionic <c>isAnimating()</c>: whether any menu is opening or closing.</summary>
    public static bool IsAnimating() => s_menus.Exists(m => m.IsMenuAnimating);

    /// <summary>
    /// Ionic <c>registerAnimation(name, animation)</c>: the animation a menu of <paramref name="type"/> makes from now on, when it
    /// makes one: at its next open or close after a change of its type, side or width.
    /// </summary>
    public static void RegisterAnimation(MenuType type, MenuAnimationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        s_animations[type] = builder;
    }

    /// <summary>
    /// What a menu button's or a toggle's click does: <see cref="Toggle"/>, whose failure (a type with no animation, a builder that
    /// throws) is logged, as Ionic's unhandled rejection shows in the console, rather than left in a task nobody observes.
    /// </summary>
    internal static void ToggleFromClick(string? menu, object source) => _ = LogFailure(Toggle(menu), source);

    private static async Task LogFailure(Task<bool> task, object source)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(source, "[MenuController] - The menu couldn't toggle: {Error}", ex.Message);
        }
    }

    private static AvonicMenu? Get(string? menu, bool logOnMultipleSideMenus)
    {
        if (menu is "start" or "end")
        {
            // There could be more than one menu on the same side, so first the enabled one.
            var side = menu == "start" ? MenuSide.Start : MenuSide.End;
            var onSide = s_menus.FindAll(m => m.Side == side);
            var enabled = onSide.FindAll(m => m.IsEnabled);
            var candidates = enabled.Count >= 1 ? enabled : onSide;
            if (candidates.Count >= 1)
            {
                if (candidates.Count > 1 && logOnMultipleSideMenus)
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(null,
                        "[MenuController] - Queried for a menu on the \"{Side}\" side, but {Count} menus were found. The first menu reference will be used. If this is not the behavior you want then pass the MenuId of the menu instead of its side.",
                        menu, candidates.Count);
                }

                return candidates[0];
            }
        }
        else if (menu is not null)
        {
            return s_menus.Find(m => m.MenuId == menu);
        }

        return s_menus.Find(m => m.IsEnabled) ?? (s_menus.Count > 0 ? s_menus[0] : null);
    }

    // ── Internal: what the menus call ─────────────────────────────────────────────────────────

    /// <summary>Ionic <c>_register</c>: once, at the end of the list.</summary>
    internal static void Register(AvonicMenu menu, TopLevel topLevel)
    {
        if (!s_menus.Contains(menu))
        {
            s_menus.Add(menu);
        }

        ListenForBack(topLevel);
    }

    /// <summary>Ionic <c>_unregister</c>.</summary>
    internal static void Unregister(AvonicMenu menu) => s_menus.Remove(menu);

    /// <summary>Raises <see cref="MenuChanged"/>: Ionic's <c>ionMenuChange</c>, from <paramref name="menu"/>.</summary>
    internal static void RaiseMenuChanged(AvonicMenu menu) => MenuChanged?.Invoke(menu, EventArgs.Empty);

    /// <summary>Ionic <c>_getOpenSync</c>, for one top level: the open menu in it.</summary>
    internal static AvonicMenu? GetOpenIn(TopLevel topLevel) => s_menus.Find(m => m.IsOpenState && ReferenceEquals(m.RegisteredIn, topLevel));

    /// <summary>
    /// Ionic <c>_setOpen</c>: nothing while any menu is animating; to open, a different open menu is closed first, at once.
    /// </summary>
    internal static async Task<bool> SetOpen(AvonicMenu menu, bool shouldOpen, bool animated, string? role)
    {
        if (IsAnimating())
        {
            return false;
        }

        if (shouldOpen && GetOpen() is { } opened && !ReferenceEquals(opened, menu))
        {
            await opened.SetOpen(false, false);
        }

        return await menu.SetOpenCore(shouldOpen, animated, role);
    }

    /// <summary>Ionic <c>_createAnimation(type, menu)</c>: the registered builder's animation.</summary>
    /// <exception cref="InvalidOperationException">No animation is registered for the type, as Ionic throws for <c>undefined</c>.</exception>
    internal static Avonic.Components.Animation.AvonicAnimation CreateAnimation(MenuType? type, MenuAnimationContext menu) =>
        type is { } t && s_animations.TryGetValue(t, out var builder)
            ? builder(menu)
            : throw new InvalidOperationException("animation not registered");

    /// <summary>
    /// The controller's <c>ionBackButton</c> listener, on <paramref name="topLevel"/>: an open menu there registers at
    /// <see cref="HardwareBackButton.MenuBackButtonPriority"/> to close itself, and the press waits for the close. With no
    /// <see cref="AvonicApp"/> to raise it, as an overlay does, the back request itself closes it, unless an overlay that dismisses on
    /// back is presented, which takes the request at its higher priority.
    /// </summary>
    private static void ListenForBack(TopLevel topLevel)
    {
        if (s_listening.TryGetValue(topLevel, out _))
        {
            return;
        }

        s_listening.Add(topLevel, new object());
        topLevel.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) =>
        {
            if (GetOpenIn(topLevel) is { } open)
            {
                e.Register(HardwareBackButton.MenuBackButtonPriority, _ => open.Close());
            }
        });
        topLevel.AddHandler(TopLevel.BackRequestedEvent, (_, e) =>
        {
            if (HardwareBackButton.IsStarted(topLevel) || !AvonicConfig.HardwareBackButton
                || OverlayHost.PresentedOverlayIn(topLevel) is { BackdropDismissEnabled: true })
            {
                return;
            }

            if (GetOpenIn(topLevel) is { } open)
            {
                e.Handled = true;
                _ = open.Close();
            }
        });
    }
}
