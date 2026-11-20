using Avalonia.Controls;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>NavDirection</c>: the way a transition goes.</summary>
public enum NavDirection
{
    /// <summary><c>'back'</c>: the leaving page goes, uncovering the entering one.</summary>
    Back,

    /// <summary><c>'forward'</c>: the entering page comes in over the leaving one.</summary>
    Forward,
}

/// <summary>Ionic <c>ion-nav-link</c>'s <c>routerDirection</c>: what a click does to the closest <see cref="AvonicNav"/>.</summary>
public enum NavLinkRouterDirection
{
    /// <summary><c>'back'</c>: pops.</summary>
    Back,

    /// <summary><c>'forward'</c>: pushes the component.</summary>
    Forward,

    /// <summary><c>'root'</c>: sets the component as the root.</summary>
    Root,
}

/// <summary>
/// Ionic <c>NavOptions</c>, with <c>RouterOutletOptions</c>: how one call to <see cref="AvonicNav"/> transitions. Unset members
/// take the nav's own behaviour. <c>easing</c> is read only by Ionic's iOS transition, <c>keyboardClose</c> by nothing, and
/// <c>mode</c> isn't exposed (ADR 0001), so none of them is here.
/// </summary>
public sealed class NavOptions
{
    /// <summary>Ionic <c>animated</c>: false turns the transition off. <see cref="AvonicNav.SetPages"/> takes only true.</summary>
    public bool? Animated { get; set; }

    /// <summary>Ionic <c>animationBuilder</c>: the transition, over the nav's <see cref="AvonicNav.Animation"/> and md's.</summary>
    public AnimationBuilder? AnimationBuilder { get; set; }

    /// <summary>Ionic <c>duration</c>, in ms: replaces the transition's own when it isn't 0. 0 is no animation.</summary>
    public double? Duration { get; set; }

    /// <summary>Ionic <c>direction</c>: back when the call removes pages, forward when it adds the top one, unless this says.</summary>
    public NavDirection? Direction { get; set; }

    /// <summary>Ionic <c>skipIfBusy</c>: while a transition runs, the call returns false at once and nothing is queued.</summary>
    public bool SkipIfBusy { get; set; }

    /// <summary>Ionic <c>showGoBack</c>: whether the entering page gets <c>:can-go-back</c>, over what the stack says.</summary>
    public bool? ShowGoBack { get; set; }

    /// <summary>
    /// Ionic <c>deepWait</c>: whether the transition waits for the pages to be laid out before it starts. Animated, it does unless
    /// this is false; with no animation, only if this is true.
    /// </summary>
    public bool? DeepWait { get; set; }

    /// <summary>Ionic <c>viewIsReady</c>: awaited, with the entering page, before the transition starts.</summary>
    public Func<Control, Task>? ViewIsReady { get; set; }

    /// <summary>Ionic <c>progressAnimation</c>: the transition is held for a gesture to drive, as the swipe back does.</summary>
    public bool ProgressAnimation { get; set; }

    internal NavOptions Clone() => (NavOptions)MemberwiseClone();
}

/// <summary>Ionic <c>NavComponentWithProps</c>: a page and its props, for <see cref="AvonicNav.InsertPages"/> and <see cref="AvonicNav.SetPages"/>.</summary>
/// <param name="Component">A page form: see <see cref="AvonicNav"/>.</param>
/// <param name="ComponentProps">Set on the page by name before it's attached.</param>
public sealed record NavComponentWithProps(object Component, IReadOnlyDictionary<string, object?>? ComponentProps = null);

/// <summary>
/// Ionic <c>TransitionDoneFn</c>: called when a call to <see cref="AvonicNav"/> ends, before its task completes; with
/// <c>(false, false, …)</c> when it fails, the reason in <paramref name="error"/>.
/// </summary>
public delegate void TransitionDoneFn(
    bool hasCompleted, bool requiresTransition, ViewController? enteringView, ViewController? leavingView, NavDirection? direction,
    Exception? error);

/// <summary>
/// Ionic's <c>animationOpts</c>: what <see cref="AvonicNav"/> gives a transition's <see cref="AnimationBuilder"/>, as its
/// <c>options</c>, with the nav as its base element.
/// </summary>
public sealed class NavTransitionOptions
{
    internal NavTransitionOptions(Control enteringElement, Control? leavingElement, NavDirection direction, double? duration, bool showGoBack)
    {
        EnteringElement = enteringElement;
        LeavingElement = leavingElement;
        Direction = direction;
        Duration = duration;
        ShowGoBack = showGoBack;
    }

    /// <summary>Ionic <c>enteringEl</c>: the page that becomes the top.</summary>
    public Control EnteringElement { get; }

    /// <summary>Ionic <c>leavingEl</c>: the page that was the top.</summary>
    public Control? LeavingElement { get; }

    /// <summary>Ionic <c>direction</c>.</summary>
    public NavDirection Direction { get; }

    /// <summary>Ionic <c>duration</c>: the call's, or null for the transition's own.</summary>
    public double? Duration { get; }

    /// <summary>Ionic <c>showGoBack</c>: whether the entering page has one under it.</summary>
    public bool ShowGoBack { get; }
}
