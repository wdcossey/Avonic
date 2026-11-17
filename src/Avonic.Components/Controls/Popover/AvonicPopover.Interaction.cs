using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>The popover's input, from <c>popover/utils.js</c>: its trigger actions, its dismiss interaction and its keyboard.</summary>
public partial class AvonicPopover
{
    /// <summary>How long the pointer has to stay on a <see cref="PopoverTriggerAction.Hover"/> trigger: <c>setTimeout(…, 100)</c>.</summary>
    internal static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// <c>data-ion-popover-trigger</c>: every control that's a popover's trigger, with the popovers it presents, for the keyboard's
    /// <c>isTriggerElement</c> and <c>ionPopoverActivateTrigger</c>, and the dismiss interaction's <c>closest()</c>.
    /// </summary>
    private static readonly ConditionalWeakTable<Interactive, List<AvonicPopover>> s_triggers = new();

    private IDisposable? _hoverTimeout;
    private Action? _removeKeyboard;
    private Action? _removeDismissInteraction;

    /// <summary>
    /// Ionic's <c>configureTriggerInteraction</c>, for <see cref="TriggerAction"/>: the listeners on the trigger, and its mark as one.
    /// Each action also presents it on <c>ionPopoverActivateTrigger</c>, which the keyboard of a popover around raises.
    /// </summary>
    protected internal override Action AddTriggerListeners(Interactive trigger)
    {
        var popovers = s_triggers.GetOrCreateValue(trigger);
        popovers.Add(this);
        Action remove = TriggerAction switch
        {
            PopoverTriggerAction.Hover => AddHoverListeners(trigger),
            PopoverTriggerAction.ContextMenu => AddContextMenuListeners(trigger),
            _ => AddClickListeners(trigger),
        };

        return () =>
        {
            remove();
            _hoverTimeout?.Dispose();
            _hoverTimeout = null;
            popovers.Remove(this);
            if (popovers.Count == 0)
            {
                s_triggers.Remove(trigger);
            }
        };
    }

    /// <summary>
    /// <c>click</c>: a click presents it, with the click as the event. A click Avalonia raises during a release (a button's, an
    /// item's) carries no point, so the release's is taken, as a DOM <c>click</c> has the pointer's <c>clientX</c>; one from a key
    /// has none, as Chromium gives 0, 0.
    /// </summary>
    private Action AddClickListeners(Interactive trigger)
    {
        Point? releasePoint = null;

        void OnReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (TopLevel.GetTopLevel(trigger) is { } top)
            {
                releasePoint = e.GetPosition(top);

                // The click comes while the release is routing; anything after is another input's.
                Dispatcher.UIThread.Post(() => releasePoint = null);
            }
        }

        void OnClick(object? sender, RoutedEventArgs e)
        {
            var ev = PopoverEvent.From(e);
            if (ev.ClientPosition is null && releasePoint is { } point)
            {
                ev = new PopoverEvent(ev.Target, point);
            }

            PresentFromTrigger(ev);
        }

        trigger.AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        var removeClick = OverlayTrigger.AddClickListener(trigger, OnClick);

        // A nested popover's dismiss interaction stops the click at the parent's content, as Ionic sets it up in the microtasks
        // after this listener, before the click gets there: even the click that presents it (M7). Every form of it.
        var removeStop = PopoverClick.Listen(trigger, (_, e) =>
        {
            if (_parentPopover?._content is { } parentContent)
            {
                PopoverClick.StopAt(e, parentContent);
            }
        });
        return () =>
        {
            trigger.RemoveHandler(PointerReleasedEvent, OnReleased);
            removeClick();
            removeStop();
        };
    }

    /// <summary>
    /// <c>hover</c>: the pointer coming onto the trigger presents it after 100 ms and a frame; leaving cancels the wait, and leaving
    /// for something outside this popover dismisses it alone. A click on it stops there.
    /// </summary>
    private Action AddHoverListeners(Interactive trigger)
    {
        void OnEntered(object? sender, PointerEventArgs e)
        {
            _hoverTimeout?.Dispose();
            var ev = PopoverEvent.From(e);

            // clearTimeout cancels the timeout, not the frame it asked for.
            _hoverTimeout = AvonicConfig.Schedule(() =>
            {
                AvonicConfig.RequestFrame(this, () =>
                {
                    PresentFromTrigger(ev);
                    _hoverTimeout = null;
                });
            }, HoverDelay);
        }

        void OnExited(object? sender, PointerEventArgs e)
        {
            // A finger's compatibility mouse events leave nothing until the next tap: Avalonia's leave at its release isn't one.
            if (e.Pointer.Type == PointerType.Touch)
            {
                return;
            }

            _hoverTimeout?.Dispose();

            // relatedTarget: what the pointer went onto. None, out of the window: nothing.
            if (TopLevel.GetTopLevel(trigger) is not { } top || top.InputHitTest(e.GetPosition(top)) is not Visual related)
            {
                return;
            }

            if (!ReferenceEquals(ClosestPopover(related), this))
            {
                DismissAlone();
            }
        }

        trigger.AddHandler(PointerEnteredEvent, OnEntered, handledEventsToo: true);
        trigger.AddHandler(PointerExitedEvent, OnExited, handledEventsToo: true);
        var removeClick = PopoverClick.Listen(trigger, (_, e) => PopoverClick.StopAt(e, trigger));
        return () =>
        {
            trigger.RemoveHandler(PointerEnteredEvent, OnEntered);
            trigger.RemoveHandler(PointerExitedEvent, OnExited);
            removeClick();
        };
    }

    /// <summary>
    /// <c>context-menu</c>: a context request (a right click, a long press, the menu key) presents it at its point, handled as
    /// <c>preventDefault</c> keeps the platform's menu away. A click on it stops there.
    /// </summary>
    private Action AddContextMenuListeners(Interactive trigger)
    {
        void OnContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            e.Handled = true;
            PresentFromTrigger(PopoverEvent.From(e));
        }

        trigger.AddHandler(ContextRequestedEvent, OnContextRequested, RoutingStrategies.Bubble, handledEventsToo: true);
        var removeClick = PopoverClick.Listen(trigger, (_, e) => PopoverClick.StopAt(e, trigger));
        return () =>
        {
            trigger.RemoveHandler(ContextRequestedEvent, OnContextRequested);
            removeClick();
        };
    }

    /// <summary><c>ionPopoverActivateTrigger</c> on <paramref name="trigger"/>: each popover it's a trigger of presents, and focuses inside.</summary>
    private static void ActivateTrigger(Interactive trigger)
    {
        if (s_triggers.TryGetValue(trigger, out var popovers))
        {
            foreach (var popover in popovers.ToList())
            {
                popover.PresentFromTrigger(new PopoverEvent(trigger), focusDescendant: true);
            }
        }
    }

    /// <summary><c>closest('[data-ion-popover-trigger]')</c>: the trigger the element is, or is in.</summary>
    private static Interactive? ClosestTrigger(Visual? element) =>
        element?.GetSelfAndVisualAncestors().OfType<Interactive>().FirstOrDefault(v => s_triggers.TryGetValue(v, out _));

    /// <summary><c>closest('ion-popover')</c>, through the shadow root: the popover the element is in.</summary>
    private static AvonicPopover? ClosestPopover(Visual element) =>
        element.GetSelfAndVisualAncestors().OfType<AvonicPopover>().FirstOrDefault();

    /// <summary>The <c>.popover-wrapper</c>'s <c>onClick</c>: with <see cref="DismissOnSelect"/>, a click in it dismisses it, unless one stopped it.</summary>
    private void OnWrapperClick(object? sender, RoutedEventArgs e)
    {
        if (DismissOnSelect && _wrapper is { } wrapper && !PopoverClick.IsStoppedBelow(e, wrapper))
        {
            DismissFromInput(null);
        }
    }

    // ── configureDismissInteraction ───────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's <c>configureDismissInteraction</c>, for a nested popover with a trigger, while it's presented: on the parent's
    /// content, a <c>hover</c> one goes when the pointer comes in anywhere but its trigger; a <c>click</c> or <c>context-menu</c>
    /// one goes on a click there, unless it's on its trigger, which the click then stops at.
    /// </summary>
    private void ConfigureDismissInteraction()
    {
        if (_parentPopover?._content is not { } parentContent || TriggerElement is not { } trigger)
        {
            return;
        }

        RemoveDismissInteraction();
        if (TriggerAction == PopoverTriggerAction.Hover)
        {
            void OnEntered(object? sender, PointerEventArgs e)
            {
                // document.elementFromPoint(): here the trigger or anything in it (its shadow parts and its slotted content).
                if (TopLevel.GetTopLevel(parentContent) is { } top && top.InputHitTest(e.GetPosition(top)) is Visual hit
                    && hit.GetSelfAndVisualAncestors().Contains(trigger))
                {
                    return;
                }

                DismissAlone();
            }

            parentContent.AddHandler(PointerEnteredEvent, OnEntered, handledEventsToo: true);
            _removeDismissInteraction = () => parentContent.RemoveHandler(PointerEnteredEvent, OnEntered);
        }
        else
        {
            _removeDismissInteraction = PopoverClick.Listen(parentContent, (_, e) =>
            {
                if (ReferenceEquals(ClosestTrigger(e.Source as Visual), trigger))
                {
                    PopoverClick.StopAt(e, parentContent);
                    return;
                }

                DismissAlone();
            });
        }
    }

    private void RemoveDismissInteraction()
    {
        _removeDismissInteraction?.Invoke();
        _removeDismissInteraction = null;
    }

    // ── configureKeyboardInteraction ──────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's <c>configureKeyboardInteraction</c>: a key on the popover or on an item in it, while it's presented. A tunnel
    /// handler, so that it comes before the item's own and a scroller's, as a <c>preventDefault</c> in the listener beats the
    /// browser's scrolling.
    /// </summary>
    private void ConfigureKeyboardInteraction()
    {
        RemoveKeyboardInteraction();
        AddHandler(KeyDownEvent, OnPopoverKeyDown, RoutingStrategies.Tunnel);
        _removeKeyboard = () => RemoveHandler(KeyDownEvent, OnPopoverKeyDown);
    }

    private void RemoveKeyboardInteraction()
    {
        _removeKeyboard?.Invoke();
        _removeKeyboard = null;
    }

    private void OnPopoverKeyDown(object? sender, KeyEventArgs e)
    {
        // Only for the popover itself and the items in it.
        if (e.Source is not (AvonicPopover or AvonicItem) || TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        var active = top.FocusManager?.GetFocusedElement();
        switch (e.Key)
        {
            case Key.Left:
                if (_parentPopover is not null)
                {
                    DismissAlone();
                }

                break;
            case Key.Down:
            {
                e.Handled = true;
                var items = Items();
                if (ItemAt(items, IndexOf(items, active) + 1) is { } next)
                {
                    FocusItem(next);
                }

                break;
            }
            case Key.Up:
            {
                e.Handled = true;
                var items = Items();
                if (ItemAt(items, IndexOf(items, active) - 1) is { } previous)
                {
                    FocusItem(previous);
                }

                break;
            }
            case Key.Home:
            {
                e.Handled = true;
                if (Items() is [var first, ..])
                {
                    FocusItem(first);
                }

                break;
            }
            case Key.End:
            {
                e.Handled = true;
                if (Items() is [.., var last])
                {
                    FocusItem(last);
                }

                break;
            }
            case Key.Right or Key.Space or Key.Enter:
                if (active is Interactive trigger && s_triggers.TryGetValue(trigger, out _))
                {
                    ActivateTrigger(trigger);
                }

                break;
        }
    }

    /// <summary>
    /// <c>ion-item:not(ion-popover ion-popover *):not([disabled])</c>: the items in this popover, in order, not those in a popover
    /// inside it, not disabled.
    /// </summary>
    private List<AvonicItem> Items() =>
        this.GetVisualDescendants().OfType<AvonicItem>()
            .Where(item => item.IsEnabled && ReferenceEquals(item.GetVisualAncestors().OfType<AvonicPopover>().FirstOrDefault(), this))
            .ToList();

    /// <summary><c>getIndexOfItem</c>: -1 for an element that isn't one of them.</summary>
    private static int IndexOf(List<AvonicItem> items, IInputElement? element) =>
        element is AvonicItem item ? items.IndexOf(item) : -1;

    /// <summary><c>items[i]</c>, undefined out of range.</summary>
    private static AvonicItem? ItemAt(List<AvonicItem> items, int index) =>
        index >= 0 && index < items.Count ? items[index] : null;

    /// <summary><c>focusItem</c>: the item's native button, at the next frame. A plain item has none, and isn't focused.</summary>
    private void FocusItem(AvonicItem item)
    {
        if (item.Button)
        {
            AvonicConfig.RequestFrame(this, () => item.Focus(NavigationMethod.Directional));
        }
    }
}

/// <summary>
/// A DOM <c>click</c> in Avalonia's terms, for the popover's listeners: a tap (<see cref="InputElement.TappedEvent"/>), a stock
/// <see cref="Button"/>'s <c>Click</c>, and an Avonic native button's click (<c>NativeButton.ClickInsideEvent</c>), from a pointer
/// or a key. One click can come as more than one of these; each listener's action is the same done twice. Handled ones too, as an
/// <c>addEventListener</c> hears every click. <c>stopPropagation</c> is a mark of the element it stopped at, which a listener on
/// an element around reads, as Avalonia's controls set <c>Handled</c> where the DOM doesn't stop a click.
/// </summary>
internal static class PopoverClick
{
    private static readonly ConditionalWeakTable<RoutedEventArgs, Visual> s_stoppedAt = new();

    /// <summary>Listens to a click on or in <paramref name="element"/>; returns what stops it.</summary>
    public static Action Listen(Interactive element, EventHandler<RoutedEventArgs> handler)
    {
        element.AddHandler(InputElement.TappedEvent, handler, RoutingStrategies.Bubble, handledEventsToo: true);
        element.AddHandler(Button.ClickEvent, handler, RoutingStrategies.Bubble, handledEventsToo: true);
        element.AddHandler(NativeButton.ClickInsideEvent, handler, RoutingStrategies.Bubble, handledEventsToo: true);
        return () =>
        {
            element.RemoveHandler(InputElement.TappedEvent, handler);
            element.RemoveHandler(Button.ClickEvent, handler);
            element.RemoveHandler(NativeButton.ClickInsideEvent, handler);
        };
    }


    /// <summary><c>stopPropagation()</c> at <paramref name="element"/>: listeners on elements around it don't act on the click.</summary>
    public static void StopAt(RoutedEventArgs e, Visual element) => s_stoppedAt.AddOrUpdate(e, element);

    /// <summary>Whether the click was stopped at an element inside <paramref name="element"/>, so it never got there.</summary>
    public static bool IsStoppedBelow(RoutedEventArgs e, Visual element) =>
        s_stoppedAt.TryGetValue(e, out var stoppedAt) && !ReferenceEquals(stoppedAt, element) && element.IsVisualAncestorOf(stoppedAt);
}
