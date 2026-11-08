# Gesture utility

Status: done · [#4](https://github.com/wdcossey/Avonic/issues/4)

Ionic's `utils/gesture` on Avalonia, in `Avonic.Components.Gestures`: `createGesture`, the pan recogniser's threshold, direction and
angle, the gesture controller's priority and capture arbitration, and its blockers. It's what `ion-item-sliding`, `ion-range`,
`ion-toggle`, `ion-refresher`, `ion-reorder-group`, `ion-menu`, `ion-picker-column` and the sheet modal are built on. The design,
the measurements and the deviations are in [PLAN.md](PLAN.md).

## Using it in a control

Create the gesture when the control is in the tree, enable it, and destroy it when the control leaves, as Ionic's components do in
`connectedCallback` and `disconnectedCallback`. A gesture starts disabled.

```csharp
using Avonic.Components.Gestures;

private Gesture? _gesture;

protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnAttachedToVisualTree(e);
    _gesture = Gesture.Create(new GestureConfig
    {
        Element = this,
        GestureName = "item-swipe",
        GesturePriority = 100,
        Threshold = 5,
        CanStart = detail => !IsOpening,
        OnStart = detail => BeginDrag(),
        OnMove = detail => DragTo(detail.DeltaX),
        OnEnd = detail => EndDrag(detail.VelocityX),
    });
    _gesture.Enable(IsEnabled);
}

protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    _gesture?.Destroy();
    _gesture = null;
    base.OnDetachedFromVisualTree(e);
}
```

`Enable(false)` while the gesture is captured ends it with `OnEnd`, as Ionic's does, so a control that disables itself mid-drag gets
its end callback.

## API

| Ionic | Avonic |
|---|---|
| `createGesture(config)` | `Gesture.Create(GestureConfig)` |
| `gesture.enable(enable = true)`, `destroy()` | `Enable(bool enable = true)`, `Destroy()`, and `Dispose()` is `Destroy()` |
| `el` | `Element`, an `InputElement`, required. Presses on anything inside it count |
| `gestureName` | `GestureName`, required |
| `gesturePriority` | `GesturePriority`, 0 |
| `direction: 'x' \| 'y'` | `Direction`, `GestureDirection.X` or `.Y`; `X` |
| `threshold` | `Threshold`, 10 px. 0 captures at the press |
| `maxAngle` | `MaxAngle`, 40 degrees |
| `disableScroll` | `DisableScroll`: `GestureController.IsScrollDisabled` from the capture to the release |
| `blurOnStart` | `BlurOnStart`: clears the focus before `OnStart` |
| `canStart(detail): boolean` | `Func<GestureDetail, bool>? CanStart`. `false` ignores the press, with no `NotCaptured` |
| `onWillStart(detail): Promise<void>` | `Func<GestureDetail, Task>? OnWillStart`. Moves are dropped until it completes, and a release before then fires nothing; `OnStart` still fires when it completes. A faulted task is rethrown on the UI thread, and `OnStart` doesn't fire |
| `onStart`, `onMove`, `onEnd`, `notCaptured` | `Action<GestureDetail>?` `OnStart`, `OnMove`, `OnEnd`, `NotCaptured` |
| `passive` | Not here: an Avalonia listener can't be passive, and a captured gesture already keeps the scroller off the pointer. To stop something else seeing an event, set `detail.Event.Handled` |
| `GestureDetail` | `GestureDetail`, one object updated in place: `Type` (`"pan"`), `StartX`, `StartY`, `StartTime`, `CurrentX`, `CurrentY`, `CurrentTime`, `DeltaX`, `DeltaY`, `VelocityX`, `VelocityY`, `Event` (`PointerEventArgs?`), and `Data`, which a control sets |
| `GESTURE_CONTROLLER` | `GestureController.Instance`, one per process |
| `isCaptured()`, `isScrollDisabled()` | `IsCaptured`, `IsScrollDisabled` |
| `createBlocker({ disable, disableScroll })` | `CreateBlocker(IEnumerable<string>? disable, bool disableScroll)`, a `GestureBlocker` with `Block()`, `Unblock()`, `Destroy()` |
| `ionGestureCaptured` on `document` | `GestureController.Instance.IonGestureCaptured`, with `GestureCapturedEventArgs.GestureName` |
| `body.backdrop-no-scroll` | `ScrollDisabledChanged`, raised when `IsScrollDisabled` changes |

Positions are in the top level's coordinates, as Ionic's `pageX` is the page's. Times are milliseconds on Avonic's clock
(`AvonicConfig.Now`), and velocities pixels per millisecond: `0.7` of the last move's speed and `0.3` of what it was, leaving out moves
0 ms or 100 ms or more after the last. `OnMove` runs once a frame, through `AvonicConfig.RequestFrame`, with the first move of the
frame; the others are dropped, not merged.

## How it decides

1. **A press** on `Element` fills the detail and asks `CanStart`, then the controller, which refuses while another gesture is
   captured or a blocker disables this one's name. One pointer at a time: a second finger is ignored while one is tracked.
2. **Moves** recalculate the detail. Once the pointer is `Threshold` from the press, the gesture decides, once: a pan if it's within
   `MaxAngle` of the axis, either way along it. A pan asks the controller to capture; anything else is `NotCaptured`, and the rest of
   the press is ignored.
3. **The controller** lets only the started gesture with the highest priority capture, and on a tie the one created last. A loser
   is `NotCaptured`. The winner's start resets to the capturing move, then `OnWillStart` and `OnStart`.
4. **The release** recalculates, then `OnEnd` if captured, or `NotCaptured` if not.

## With Avalonia's pointer and a scroller

- **Capturing takes Avalonia's pointer capture** to `Element`, by way of releasing it, so every element that held the press hears it
  lost: a `Button` lets go of its press, and an `Activatable` surface is cancelled whether it's inside `Element`, is `Element` or is
  around it, which is Ionic's `ionGestureCaptured` reaching `tap-click`. Moves outside `Element`'s bounds keep arriving until the
  release. A gesture around the one that captured still gets the pointer, and is refused at its own threshold; one inside it gets no
  more of the pointer, so it's `NotCaptured` at once.
- **Losing the capture** to anything outside `Element` (another element, the platform cancelling the touch, `Element` leaving the
  tree) is Ionic's `touchcancel`: `OnEnd` if captured, `NotCaptured` if not, with the last move's detail, as there's no position.
- **A `ScrollViewer` waits** while a gesture on the pointer decides. Avalonia's `ScrollGestureRecognizer` starts at 5 px, before most
  thresholds, with no axis lock, so each undecided move sets `Pointer.IsGestureRecognitionSkipped` before any recogniser sees it, inside
  `Element` or around it. If every gesture gives up, the flag is cleared and the scroller, which has tracked the press, scrolls from the
  next move. A captured gesture keeps the scroller off the pointer until the release. A mouse never drags a `ScrollViewer`.

| Drag on a horizontal gesture inside a scroller | Ionic in Chromium | Avonic |
|---|---|---|
| sideways | pans | pans |
| up | scrolls, from about 15 px | scrolls, from the gesture's threshold |
| sideways, then up | pans, never scrolls | the same |
| up, then sideways | scrolls, and the gesture is out | the same |
| under `MaxAngle` from the axis | pans | pans |
| between `MaxAngle` and 45° | neither | scrolls |
| over 45° | scrolls | scrolls |

A vertical gesture in a vertical scroller is the one real difference: Chromium scrolls and Ionic's passive gesture runs too, where
Avalonia gives the pointer to one owner, the gesture. `ion-refresher` decides in `CanStart` for that reason.

## Known gaps

- **`IsScrollDisabled` has no effect yet.** Ionic's is `overflow: hidden` on `body`, and nothing in Avonic is the body, as
  `AvonicContent` is `ion-content`, which Ionic leaves alone. `ion-app` (#65) or `ion-menu` will read it.
- **One controller per process,** where Ionic's is one per document: two windows dragging at once would arbitrate with each other.
- **A second finger's moves are ignored** while one is tracked. Ionic reads `changedTouches[0]` of whichever finger moved.
- **Deviations,** each in [PLAN.md](PLAN.md): a threshold-0 gesture that loses at the press can start again, where Ionic's never does;
  a cancel's detail is the last move's; a gesture inside the one that captures is `NotCaptured` at the capture, not at its threshold.
- Not tried in a real window, or on a phone.
