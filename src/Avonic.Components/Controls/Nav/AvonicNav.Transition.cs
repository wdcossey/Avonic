using Avalonia.Controls;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>transition</c> (<c>nav.js</c> and <c>utils/transition</c>) and md's page transition.</summary>
public partial class AvonicNav
{
    /// <summary>md's <c>OFF_BOTTOM</c>: how far below its place a page starts, or ends going back.</summary>
    internal const double MdOffBottom = 40;

    /// <summary>md's forward duration, in ms.</summary>
    internal const double MdForwardDuration = 280;

    /// <summary>md's back duration, in ms.</summary>
    internal const double MdBackDuration = 200;

    /// <summary>md's forward easing.</summary>
    internal const string MdForwardEasing = "cubic-bezier(0.36,0.66,0.04,1)";

    /// <summary>md's back easing.</summary>
    internal const string MdBackEasing = "cubic-bezier(0.47,0,0.745,0.715)";

    // Ionic's setZIndex: the entering page over the leaving one going forward, under it going back.
    internal const int EnteringForwardZIndex = 101;
    internal const int EnteringBackZIndex = 99;
    internal const int LeavingZIndex = 100;

    /// <summary>The swipe's animation, while a gesture drives it (Ionic's <c>sbAni</c>).</summary>
    private AvonicAnimation? _sbAni;

    private bool _gestureOrAnimationInProgress;

    /// <summary>Ionic <c>transition</c> in <c>nav.js</c>: the options for the page transition, then <c>transitionFinish</c>.</summary>
    private async Task<NavResult> Transition(ViewController enteringView, ViewController? leavingView, Instruction ti)
    {
        var opts = ti.Opts;
        Action<AvonicAnimation?>? progressCallback = opts.ProgressAnimation ? OnSwipeAnimation : null;

        var animated = opts.Animated ?? (Animated && AvonicConfig.Animated);
        var showGoBack = opts.ShowGoBack ?? CanGoBack(enteringView);
        var builder = opts.AnimationBuilder ?? Animation;
        var hasCompleted = await PageTransition(
            enteringView, leavingView, opts.Direction ?? NavDirection.Forward, animated, opts.Duration, showGoBack, builder, opts,
            progressCallback);
        return TransitionFinish(hasCompleted, enteringView, leavingView, opts);
    }

    /// <summary>Ionic <c>transitionFinish</c>: an incomplete transition leaves the leaving view the active one.</summary>
    private NavResult TransitionFinish(bool hasCompleted, ViewController enteringView, ViewController? leavingView, NavOptions opts)
    {
        var activeView = hasCompleted ? enteringView : leavingView;
        if (activeView is not null)
        {
            UnmountInactiveViews(activeView);
        }

        return new NavResult(hasCompleted, true, enteringView, leavingView, opts.Direction);
    }

    /// <summary>
    /// Ionic <c>transition</c> in <c>utils/transition</c>: the pages set up (<c>beforeTransition</c>), the animation or none, then
    /// the pages put back (<c>afterTransition</c>).
    /// </summary>
    private async Task<bool> PageTransition(
        ViewController enteringView, ViewController? leavingView, NavDirection direction, bool animated, double? duration,
        bool showGoBack, AnimationBuilder? builder, NavOptions opts, Action<AvonicAnimation?>? progressCallback)
    {
        var enteringEl = enteringView.Element!;
        var leavingEl = leavingView?.Element;
        BeforeTransition(enteringView, leavingView, direction, showGoBack);
        try
        {
            // getAnimationBuilder: none without a leaving page, with animations off, or a duration of 0.
            var animationBuilder = leavingEl is null || !animated || duration == 0 ? null : builder ?? MdTransitionAnimation;
            if (animationBuilder is null)
            {
                // noAnimation: deep only with Ionic's focus manager, which is off, unless the call says.
                await WaitForReady(opts, defaultDeep: false, enteringEl);
                FireWillEvents(enteringEl, leavingEl);
                FireDidEvents(enteringEl, leavingEl);
                return true;
            }

            // animation: every page laid out first, so that the animation's first frame is its start however long that took.
            await WaitForReady(opts, defaultDeep: true, enteringEl);
            var trans = animationBuilder(this, new NavTransitionOptions(enteringEl, leavingEl, direction, duration, showGoBack));
            FireWillEvents(enteringEl, leavingEl);
            bool didComplete;
            try
            {
                didComplete = await PlayTransition(trans, progressCallback);
                progressCallback?.Invoke(null);
                if (didComplete)
                {
                    FireDidEvents(enteringEl, leavingEl);
                }
            }
            finally
            {
                trans.Destroy();
            }

            return didComplete;
        }
        finally
        {
            AfterTransition(enteringView, leavingView);
        }
    }

    /// <summary>
    /// Ionic <c>beforeTransition</c>: <c>z-index</c>, <c>can-go-back</c>, both pages shown and taking no input until the end.
    /// </summary>
    private static void BeforeTransition(ViewController enteringView, ViewController? leavingView, NavDirection direction, bool showGoBack)
    {
        enteringView.SetZIndex(direction == NavDirection.Back ? EnteringBackZIndex : EnteringForwardZIndex);
        leavingView?.SetZIndex(LeavingZIndex);
        ((IPseudoClasses)enteringView.Element!.Classes).Set(CanGoBackPseudoClass, showGoBack);
        enteringView.SetHidden(false);
        enteringView.SetTransitioning(true);
        if (leavingView is not null)
        {
            leavingView.SetHidden(false);
            leavingView.SetTransitioning(true);
        }
    }

    /// <summary>Ionic <c>afterTransition</c>: <c>ion-page-invisible</c> and <c>pointer-events</c> off both pages.</summary>
    private static void AfterTransition(ViewController enteringView, ViewController? leavingView)
    {
        enteringView.Element?.Classes.Remove(ViewController.InvisibleClass);
        enteringView.SetTransitioning(false);
        if (leavingView is not null)
        {
            leavingView.Element?.Classes.Remove(ViewController.InvisibleClass);
            leavingView.SetTransitioning(false);
        }
    }

    /// <summary>
    /// Ionic <c>waitForReady</c>: <c>deepReady</c> on both pages, which in Avalonia is a layout pass that includes them (their
    /// templates applied and measured), then the call's <c>viewIsReady</c>.
    /// </summary>
    private async Task WaitForReady(NavOptions opts, bool defaultDeep, Control enteringEl)
    {
        if (opts.DeepWait ?? defaultDeep)
        {
            UpdateLayout();
        }

        if (opts.ViewIsReady is { } viewIsReady)
        {
            await viewIsReady(enteringEl);
        }
    }

    /// <summary>Ionic <c>playTransition</c>: played, or held for the swipe; true when it finishes at its end.</summary>
    private static Task<bool> PlayTransition(AvonicAnimation trans, Action<AvonicAnimation?>? progressCallback)
    {
        var finished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        trans.OnFinish((step, _) => finished.TrySetResult(step == 1));
        if (progressCallback is not null)
        {
            trans.ProgressStart(true);
            progressCallback(trans);
        }
        else
        {
            _ = trans.Play();
        }

        return finished.Task;
    }

    private static void FireWillEvents(Control enteringEl, Control? leavingEl)
    {
        Lifecycle(leavingEl, IonViewWillLeaveEvent);
        Lifecycle(enteringEl, IonViewWillEnterEvent);
    }

    private static void FireDidEvents(Control enteringEl, Control? leavingEl)
    {
        Lifecycle(enteringEl, IonViewDidEnterEvent);
        Lifecycle(leavingEl, IonViewDidLeaveEvent);
    }

    /// <summary>
    /// Ionic <c>mdTransitionAnimation</c>: forward, the entering page rises 40 px and fades in over 280 ms; back, the leaving page
    /// sinks 40 px and fades out over 200 ms, then isn't shown, over the entering page, which doesn't move.
    /// </summary>
    internal static AvonicAnimation MdTransitionAnimation(Control baseElement, object? options)
    {
        var opts = (NavTransitionOptions)options!;
        var backDirection = opts.Direction == NavDirection.Back;
        var duration = opts.Duration is { } d && d != 0 ? d : (double?)null;
        var rootTransition = AvonicAnimation.Create();
        rootTransition.AddElement(opts.EnteringElement).Fill(AnimationFill.Both).BeforeRemoveClass(ViewController.InvisibleClass);
        if (backDirection)
        {
            rootTransition.Duration(duration ?? MdBackDuration).Easing(MdBackEasing);
        }
        else
        {
            rootTransition
                .Duration(duration ?? MdForwardDuration)
                .Easing(MdForwardEasing)
                .FromTo("transform", $"translateY({MdOffBottom}px)", "translateY(0px)")
                .FromTo("opacity", 0.01, 1);
        }

        // The entering toolbar's animation has no keyframes in md: it only joins the timeline, as Ionic's does.
        if (opts.LeavingElement is { } leavingEl && backDirection)
        {
            rootTransition.Duration(duration ?? MdBackDuration).Easing(MdBackEasing);
            var leavingPage = AvonicAnimation.Create();
            leavingPage
                .AddElement(leavingEl)
                .OnFinish((currentStep, animation) =>
                {
                    if (currentStep == 1 && animation.Elements.Count > 0 && baseElement is AvonicNav nav)
                    {
                        nav.ViewOf(leavingEl)?.SetDisplayNone();
                    }
                })
                .FromTo("transform", "translateY(0px)", $"translateY({MdOffBottom}px)")
                .FromTo("opacity", 1, 0);
            rootTransition.AddAnimation(leavingPage);
        }

        return rootTransition;
    }

    /// <summary>
    /// Ionic's <c>progressCallback</c>: the swipe's animation, held for the gesture to drive. A gesture that has already ended by
    /// the time the animation comes plays it back to its start at once, so that the transition's task completes.
    /// </summary>
    private void OnSwipeAnimation(AvonicAnimation? ani)
    {
        if (ani is not null && !_gestureOrAnimationInProgress)
        {
            _gestureOrAnimationInProgress = true;
            ani.OnFinish((_, _) => _gestureOrAnimationInProgress = false, oneTimeCallback: true);
            ani.ProgressEnd(0, 0, 0);
        }
        else
        {
            _sbAni = ani;
        }
    }

    /// <summary>Ionic's <c>componentWillLoad</c> for <c>swipeGesture</c>: the swipe back, when it's built.</summary>
    partial void LoadSwipeGesture();

    /// <summary>The view whose page is <paramref name="page"/>.</summary>
    internal ViewController? ViewOf(Control page) => _views.Find(v => ReferenceEquals(v.Element, page));
}
