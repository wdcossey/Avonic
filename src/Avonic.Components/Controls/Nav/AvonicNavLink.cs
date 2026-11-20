using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-nav-link</c>: a click anywhere in it pushes <see cref="Component"/>, sets it as the root, or pops, on the closest
/// <see cref="AvonicNav"/> around it. See <c>docs/components/ion-nav/README.md</c>.
/// </summary>
/// <remarks>
/// Its host listens to the DOM <c>click</c>, which a press on anything in it raises, and which a native button inside raises from
/// Enter and Space too: <see cref="NativeButton"/> with no keys of its own, and the clicks of the buttons inside. It isn't focusable
/// and has no role, as the host has neither.
/// </remarks>
public class AvonicNavLink : ContentControl
{
    /// <summary>Ionic <c>component</c>: the page pushed or set as the root, in any of <see cref="AvonicNav"/>'s page forms.</summary>
    public static readonly StyledProperty<object?> ComponentProperty =
        AvaloniaProperty.Register<AvonicNavLink, object?>(nameof(Component));

    /// <summary>Ionic <c>componentProps</c>: set on the page by name before it's attached.</summary>
    public static readonly StyledProperty<IReadOnlyDictionary<string, object?>?> ComponentPropsProperty =
        AvaloniaProperty.Register<AvonicNavLink, IReadOnlyDictionary<string, object?>?>(nameof(ComponentProps));

    /// <summary>Ionic <c>routerAnimation</c>: the transition, over the nav's.</summary>
    public static readonly StyledProperty<AnimationBuilder?> RouterAnimationProperty =
        AvaloniaProperty.Register<AvonicNavLink, AnimationBuilder?>(nameof(RouterAnimation));

    /// <summary>Ionic <c>routerDirection</c>: <c>Forward</c> pushes, <c>Root</c> sets the root, <c>Back</c> pops.</summary>
    public static readonly StyledProperty<NavLinkRouterDirection> RouterDirectionProperty =
        AvaloniaProperty.Register<AvonicNavLink, NavLinkRouterDirection>(nameof(RouterDirection), NavLinkRouterDirection.Forward);

    static AvonicNavLink()
    {
        // The host has no overflow rule.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicNavLink>(false);
    }

    public AvonicNavLink()
    {
        _ = new NativeButton(this, _ => _ = OnClick(), () => false, () => { }) { StockButtonsClick = true };
    }

    public object? Component
    {
        get => GetValue(ComponentProperty);
        set => SetValue(ComponentProperty, value);
    }

    public IReadOnlyDictionary<string, object?>? ComponentProps
    {
        get => GetValue(ComponentPropsProperty);
        set => SetValue(ComponentPropsProperty, value);
    }

    public AnimationBuilder? RouterAnimation
    {
        get => GetValue(RouterAnimationProperty);
        set => SetValue(RouterAnimationProperty, value);
    }

    public NavLinkRouterDirection RouterDirection
    {
        get => GetValue(RouterDirectionProperty);
        set => SetValue(RouterDirectionProperty, value);
    }

    /// <summary>
    /// Ionic's <c>navLink</c>: on the closest nav, each with <c>skipIfBusy</c>, so a second click during the transition does
    /// nothing. With no nav, or no component to go forward or to the root to, nothing happens.
    /// </summary>
    private Task<bool> OnClick()
    {
        if (!IsEffectivelyEnabled || this.FindAncestorOfType<AvonicNav>() is not { } nav)
        {
            return Task.FromResult(false);
        }

        var opts = new NavOptions { SkipIfBusy = true, AnimationBuilder = RouterAnimation };
        return RouterDirection switch
        {
            NavLinkRouterDirection.Forward when Component is { } component => nav.Push(component, ComponentProps, opts),
            NavLinkRouterDirection.Root when Component is { } component => nav.SetRoot(component, ComponentProps, opts),
            NavLinkRouterDirection.Back => nav.Pop(opts),
            _ => Task.FromResult(false),
        };
    }

    /// <summary>No role: an element of the page, whose content speaks for itself.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);
}
