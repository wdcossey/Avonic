using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>queueTrns</c>, <c>runTransition</c> and the stack's bookkeeping.</summary>
public partial class AvonicNav
{
    /// <summary>Ionic <c>TransitionInstruction</c>.</summary>
    private sealed class Instruction(NavOptions? opts, bool clone = true)
    {
        public NavOptions Opts { get; } = (clone ? opts?.Clone() : opts) ?? new NavOptions();

        public int? InsertStart { get; set; }

        public List<object>? InsertViews { get; set; }

        public List<ViewController>? Views { get; set; }

        public ViewController? RemoveView { get; set; }

        public int? RemoveStart { get; set; }

        public int? RemoveCount { get; set; }

        public bool LeavingRequiresTransition { get; set; }

        public bool EnteringRequiresTransition { get; set; }

        public TaskCompletionSource<bool> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TransitionDoneFn? Done { get; set; }
    }

    /// <summary>Ionic's <c>NavResult</c>.</summary>
    private readonly record struct NavResult(
        bool HasCompleted, bool RequiresTransition, ViewController? EnteringView = null, ViewController? LeavingView = null,
        NavDirection? Direction = null);

    /// <summary>
    /// Ionic <c>queueTrns</c>: <c>skipIfBusy</c> during a transition is false at once; otherwise queued, and started if nothing
    /// is running. Before the nav has loaded, it waits for the load, as Stencil holds a method until then.
    /// </summary>
    private Task<bool> QueueTrns(Instruction ti, TransitionDoneFn? done)
    {
        if (_isTransitioning && ti.Opts.SkipIfBusy)
        {
            return Task.FromResult(false);
        }

        ti.Done = done;
        if (ti.InsertViews is { Count: 0 })
        {
            ti.InsertViews = null;
        }

        if (!_didLoad)
        {
            _beforeLoad.Add(ti);
            return ti.Result.Task;
        }

        _transInstr.Add(ti);
        NextTrns();
        return ti.Result.Task;
    }

    /// <summary>Ionic <c>nextTrns</c>: one transition at a time.</summary>
    private void NextTrns()
    {
        if (_isTransitioning || _transInstr.Count == 0)
        {
            return;
        }

        var ti = _transInstr[0];
        _transInstr.RemoveAt(0);
        _ = RunTransition(ti);
    }

    /// <summary>Ionic <c>runTransition</c>.</summary>
    private async Task RunTransition(Instruction ti)
    {
        try
        {
            // Busy before the event, unlike Ionic, whose handler could start a second transition alongside this one
            // (docs/infra/ionic-issues.md).
            _isTransitioning = true;
            RaiseEvent(new RoutedEventArgs(IonNavWillChangeEvent, this));
            PrepareTI(ti);
            var leavingView = GetActive();
            var enteringView = GetEnteringView(ti, leavingView);
            if (leavingView is null && enteringView is null)
            {
                throw new InvalidOperationException("no views in the stack to be removed");
            }

            if (enteringView is { State: ViewController.StateNew })
            {
                // attachComponent, then componentOnReady: a page that isn't one of Ionic's own is ready at the next frame.
                enteringView.Init(this);
                await NextFrame();
            }

            PostViewInit(enteringView, leavingView, ti);

            var requiresTransition = (ti.EnteringRequiresTransition || ti.LeavingRequiresTransition) && enteringView != leavingView;
            if (requiresTransition && leavingView is not null)
            {
                // Going back uses the entering page's animation, unless the call gives one.
                if (ti.Opts.Direction == NavDirection.Back)
                {
                    ti.Opts.AnimationBuilder ??= enteringView?.AnimationBuilder;
                }

                leavingView.AnimationBuilder = ti.Opts.AnimationBuilder;
            }

            var result = requiresTransition
                ? await Transition(enteringView!, leavingView, ti)
                : new NavResult(true, false);
            Success(result, ti);
            RaiseEvent(new RoutedEventArgs(IonNavDidChangeEvent, this));
        }
        catch (Exception exception)
        {
            Failed(exception, ti);
        }

        _isTransitioning = false;
        NextTrns();
    }

    private Task NextFrame()
    {
        var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        AvonicConfig.RequestFrame(this, () => frame.TrySetResult());
        return frame.Task;
    }

    private static void Success(NavResult result, Instruction ti)
    {
        ti.Done?.Invoke(result.HasCompleted, result.RequiresTransition, result.EnteringView, result.LeavingView, result.Direction, null);
        ti.Result.TrySetResult(result.HasCompleted);
    }

    /// <summary>
    /// Ionic <c>failed</c>: the rest of the queue is dropped, and the call fails. Ionic's dropped calls never settle, and an
    /// <c>await</c> on one waits for ever; each completes false here (ADR 0005, docs/infra/ionic-issues.md).
    /// </summary>
    private void Failed(Exception reason, Instruction ti)
    {
        var dropped = _transInstr.ToArray();
        _transInstr.Clear();
        foreach (var other in dropped)
        {
            other.Done?.Invoke(false, false, null, null, null, null);
            other.Result.TrySetResult(false);
        }

        ti.Done?.Invoke(false, false, null, null, null, reason);
        ti.Result.TrySetException(reason);
    }

    /// <summary>Ionic <c>prepareTI</c>: the instruction's indexes made absolute, and its pages made views.</summary>
    private void PrepareTI(Instruction ti)
    {
        var viewsLength = _views.Count;
        if (ti.RemoveView is not null)
        {
            var index = _views.IndexOf(ti.RemoveView);
            if (index < 0)
            {
                throw new InvalidOperationException("removeView was not found");
            }

            ti.RemoveStart += index;
        }

        if (ti.RemoveStart is { } removeStart)
        {
            if (removeStart < 0)
            {
                ti.RemoveStart = viewsLength - 1;
            }

            if (ti.RemoveCount < 0)
            {
                ti.RemoveCount = viewsLength - ti.RemoveStart;
            }

            ti.LeavingRequiresTransition = ti.RemoveCount > 0 && ti.RemoveStart + ti.RemoveCount == viewsLength;
        }

        if (ti.InsertViews is not null)
        {
            if (ti.InsertStart is not { } start || start < 0 || start > viewsLength)
            {
                ti.InsertStart = viewsLength;
            }

            ti.EnteringRequiresTransition = ti.InsertStart == viewsLength;
        }

        if (ti.InsertViews is null)
        {
            return;
        }

        var viewControllers = ConvertToViews(ti.InsertViews);
        if (viewControllers.Count == 0)
        {
            throw new InvalidOperationException("invalid views to insert");
        }

        foreach (var view in viewControllers)
        {
            if (view.Nav is { } nav && nav != this)
            {
                throw new InvalidOperationException("inserted view was already inserted");
            }

            if (view.State == ViewController.StateDestroyed)
            {
                throw new InvalidOperationException("inserted view was already destroyed");
            }
        }

        ti.Views = viewControllers;
    }

    /// <summary>
    /// Ionic <c>convertToViews</c>: a view as it is, a component with props, or a bare page form. Ionic throws a <c>TypeError</c>
    /// on a bare string, which its type allows; any form is taken here (ADR 0005).
    /// </summary>
    private static List<ViewController> ConvertToViews(IEnumerable<object> pages)
    {
        var views = new List<ViewController>();
        foreach (var page in pages)
        {
            switch (page)
            {
                case null:
                    break;
                case ViewController view:
                    views.Add(view);
                    break;
                case NavComponentWithProps { Component: ViewController view }:
                    views.Add(view);
                    break;
                case NavComponentWithProps withProps when withProps.Component is not null:
                    views.Add(new ViewController(withProps.Component, withProps.ComponentProps));
                    break;
                case NavComponentWithProps:
                    break;
                default:
                    views.Add(new ViewController(page));
                    break;
            }
        }

        return views;
    }

    /// <summary>Ionic <c>getEnteringView</c>: the last inserted, or the last view that stays and isn't the one leaving.</summary>
    private ViewController? GetEnteringView(Instruction ti, ViewController? leavingView)
    {
        if (ti.Views is { } insertViews)
        {
            return insertViews[^1];
        }

        if (ti.RemoveStart is { } removeStart)
        {
            var removeEnd = removeStart + (ti.RemoveCount ?? 0);
            for (var i = _views.Count - 1; i >= 0; i--)
            {
                var view = _views[i];
                if ((i < removeStart || i >= removeEnd) && view != leavingView)
                {
                    return view;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Ionic <c>postViewInit</c>: views removed from below the top leave at once, with their events; the inserted ones go in;
    /// the direction defaults; a stack left empty fails.
    /// </summary>
    private void PostViewInit(ViewController? enteringView, ViewController? leavingView, Instruction ti)
    {
        var opts = ti.Opts;
        List<ViewController>? destroyQueue = null;
        if (ti.RemoveStart is { } removeStart && ti.RemoveCount is { } removeCount)
        {
            destroyQueue = [];
            for (var i = removeStart; i < removeStart + removeCount; i++)
            {
                if (i >= 0 && i < _views.Count && _views[i] is var view && view != enteringView && view != leavingView)
                {
                    destroyQueue.Add(view);
                }
            }

            opts.Direction ??= NavDirection.Back;
        }

        var finalNumViews = _views.Count + (ti.Views?.Count ?? 0) - (ti.RemoveCount ?? 0);
        if (finalNumViews == 0)
        {
            Warn(this, "[ion-nav] - You can't remove all the pages in the navigation stack. nav.pop() is probably called too many times.");
            throw new InvalidOperationException("navigation stack needs at least one root page");
        }

        if (ti.Views is { } insertViews)
        {
            var insertIndex = ti.InsertStart ?? _views.Count;
            foreach (var view in insertViews)
            {
                InsertViewAt(view, insertIndex);
                insertIndex++;
            }

            if (ti.EnteringRequiresTransition)
            {
                opts.Direction ??= NavDirection.Forward;
            }
        }

        if (destroyQueue is { Count: > 0 })
        {
            foreach (var view in destroyQueue)
            {
                Lifecycle(view.Element, IonViewWillLeaveEvent);
                Lifecycle(view.Element, IonViewDidLeaveEvent);
                Lifecycle(view.Element, IonViewWillUnloadEvent);
            }

            foreach (var view in destroyQueue)
            {
                DestroyView(view);
            }
        }
    }

    /// <summary>Ionic <c>insertViewAt</c>: a view already in the stack moves.</summary>
    private void InsertViewAt(ViewController view, int index)
    {
        var existing = _views.IndexOf(view);
        if (existing > -1)
        {
            _views.RemoveAt(existing);
            _views.Insert(Math.Min(index, _views.Count), view);
        }
        else
        {
            view.Nav = this;
            _views.Insert(Math.Min(index, _views.Count), view);
        }
    }

    /// <summary>Ionic <c>destroyView</c>.</summary>
    private void DestroyView(ViewController view)
    {
        view.Destroy();
        _views.Remove(view);
    }

    /// <summary>
    /// Ionic <c>unmountInactiveViews</c>: the pages above the active one leave the nav; the ones under it are hidden. A view with
    /// no page yet, as <c>insertPages</c> leaves all but the last, is left alone.
    /// </summary>
    private void UnmountInactiveViews(ViewController activeView)
    {
        var activeViewIndex = _views.IndexOf(activeView);
        for (var i = _views.Count - 1; i >= 0; i--)
        {
            var view = _views[i];
            if (view.Element is not { } element)
            {
                continue;
            }

            if (i > activeViewIndex)
            {
                Lifecycle(element, IonViewWillUnloadEvent);
                DestroyView(view);
            }
            else if (i < activeViewIndex)
            {
                view.SetHidden(true);
            }
        }
    }
}
