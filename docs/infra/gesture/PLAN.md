# Plan: the gesture utility (#4)

Ionic's `utils/gesture` on Avalonia: `createGesture`, the pan recognizer's threshold, direction and angle, the gesture
controller's priority and capture arbitration, and its scroll blocker. It's infrastructure, not a control: `ion-item-sliding`,
`ion-range`, `ion-toggle`, `ion-refresher`, `ion-reorder-group`, `ion-menu`, `ion-picker-column` and the sheet modal are built on it.

Sources, `@ionic/core` 8.8.1, `dist/collection/utils/gesture/`: `index.js` (createGesture), `gesture-controller.js`,
`recognizers.js` (createPanRecognizer), `pointer-events.js`, `listener.js`; `utils/tap-click/index.js` for what a captured
gesture does to a press; `css/core.css` for `body.backdrop-no-scroll`; and every `createGesture` call in `components/` for the
options the controls really use.

## What Ionic does

`createGesture(config)` attaches to an element and tracks one pointer at a time:

1. **Down** (`touchstart` on the element, or `mousedown` within 2 s of no touch): fill the detail, ask `canStart`, then the
   controller's `start()`. Not allowed while another gesture is captured, or while a blocker disables this gesture's name.
   With `threshold: 0` it captures at once; otherwise the pan recognizer starts.
2. **Move while undecided:** the detail is recalculated on every move (velocity included). Once the distance from the down point
   reaches `threshold`, the recognizer decides once: a pan if the angle to the gesture's axis is under `maxAngle` (the cosine is
   compared, `cos > cos(maxAngle)`), either way along the axis. A pan tries to capture; anything else aborts: `notCaptured`, and
   the gesture stops listening to this press.
3. **Capture:** the controller grants it only to the started gesture with the highest priority, `gesturePriority * 1000000 + id`,
   so a tie goes to the gesture created later. A loser is dropped from the started set and aborts. The winner resets `startX`,
   `startY` and `startTime` to the capturing move, so a large threshold doesn't make the first `onMove` jump, but `deltaX` and
   `deltaY` keep the capturing move's values until the next move recalculates them. Then `onWillStart` (awaited) and `onStart`.
   The controller dispatches `ionGestureCaptured` on the document, which `tap-click` answers by cancelling the active press, and
   with `disableScroll` adds `backdrop-no-scroll` to `body`, whose only rule is `overflow: hidden` on the body. `ion-content` is
   untouched by it.
4. **Move while captured:** the first move of a frame recalculates the detail and queues `onMove` for the next animation frame;
   **further moves in that frame are dropped**, not merged. Velocity is an exponential average, `0.7 × (Δ / Δt) + 0.3 × previous`,
   skipped when `Δt` is 0 or 100 ms or more.
5. **Up** (`touchend`, `touchcancel`, `mouseup`): recalculate, release, then `onEnd` if captured, else `notCaptured`. Nothing fires
   while `onWillStart` is still pending; when it settles, `onStart` fires anyway, after the up. `enable(false)` while captured is an
   up with no event. A gesture starts disabled: every control calls `enable()`.

The controller is one object per document, `GESTURE_CONTROLLER`. A `blocker` can disable gestures by name and disable scroll
without being a gesture; `ion-menu` holds one for its backdrop.

### What the controls use

| Control | name | priority | direction | threshold | other |
|---|---|---|---|---|---|
| item-sliding | `item-swipe` | 100 | x | 5 | `canStart` |
| range | `range` | 100 | x | 10 | |
| toggle | `toggle` | 100 | x | 5 | |
| refresher | `refresher` | 31 | y | 5 (native), 20 | `canStart: scrollTop === 0` |
| menu | `menu-swipe` | 30 | x | 10 | `canStart`, `onWillStart`, `blurOnStart`, on `document` |
| reorder-group | `reorder` | 110 | y | 0 | `canStart` |

## Measured: Ionic's numbers

The real `createGesture` from `dist/cjs` ran in Node, on a fake DOM, with events at exact timestamps, and the callbacks' details were
recorded. `requestAnimationFrame` ran 16 ms after it was asked for, before the next event, as the tests' clock runs
`AvonicConfig.RequestFrame`, so the tests assert the same numbers: every `onMove` time in `GestureTests` is Ionic's. Timestamps are ms; velocities px/ms.

| Case | Result |
|---|---|
| x, defaults; down at (100, 100) t=1000; move to 109.9 at 1016; 110 at 1032; (130, 105) at 1048; up at 1064 | 9.9 px does nothing. At 10 px: captured, `onStart` with start (110, 100, 1032), **`deltaX` still 10**, `velocityX` 0.134312 (the 9.9 px move counted). `onMove` at 1048: delta (20, 5), velocity (0.915294, 0.21875). `onEnd` at 1064: delta (20, 5), velocity (0.274588, 0.065625), the up's zero movement decaying it by 0.3 |
| x, defaults; one 20 px move at 39°, 41° | 39°: captured. 41°: `notCaptured` at that move, delta (15.094192, 13.121181). 40° itself is a floating-point coin toss, so it isn't tested |
| x, defaults; move to (100, 115), then (150, 115) | `notCaptured` at the first move, velocity (0, 0.65625); the second move does nothing |
| y, threshold 5; moves to (101, 96), (101, 94), (101, 60) | 4.1 px nothing; 6.1 px captured, start (101, 94), delta (1, −6); `onMove` delta (0, −34), velocity (0.003938, −1.5295) |
| threshold 0; down (100, 100) at 1000; 8 px moves at 1004, 1008, 1012, 1016, then 140 at 1024, 148 at 1048, (168, 110) at 1168, 173 and 178 both at 1200; up at 1240 | `onStart` at the down, delta 0. First `onMove` at 1020 (the frame asked for at 1004): `currentX` **108**, velocity 1.4: the other three moves of that frame were dropped, not merged. Then 140 at 1040 (1.54), 148 at 1064 (0.695333); the 120 ms gap keeps 0.695333; 173 (0.317975), and 178 at the same time is dropped by the frame. `onEnd` at 1240: 178, velocity 0.182893 |
| x, defaults; moves to 103 at 1016, 106 at 1016, 107 at 1200; up at 1216 | `notCaptured` at the up, velocity 0.039375: the 0 ms move and the 184 ms one were left out of the average |
| inner (100, threshold 20) in outer (30, threshold 5); moves to 110 then 125 | outer `notCaptured` at 110; inner `onStart` at 125, delta 25 |
| equal priority 50, `first` on the inner element, `second` created later on the outer | `first` `notCaptured`, `second` captures |
| `a` captured at its down (threshold 0); `b` pressed with another finger | `b`'s `canStart` is called, then nothing: the controller refuses |
| `canStart` returns false | nothing at all, not even `notCaptured` |
| up at (105, 101) before the threshold | `notCaptured` with the up's position, delta (5, 1) |
| `enable(false)` while captured | `onEnd` at once, with the last move's detail; later moves and the up do nothing; after `enable()` the next press works |
| `disableScroll` | `backdrop-no-scroll` added at capture, removed at release; `release()` also runs at every down |
| blocker `disable: ['a']` | `a`'s `canStart` runs, then the controller refuses; `isScrollDisabled()` true while blocked |
| `onWillStart` pending at the up | nothing at the up; `onStart` fires when it settles |

## Measured: a gesture and `ion-content`'s scroll in Chromium

Real Ionic in headless Chromium, md, 390 × 844, touch through CDP `Input.dispatchTouchEvent` in 8 px steps 16 ms apart, in a
scrollable `ion-content` (40 rows). Ionic can't stop the browser scrolling, as its listeners are passive, so this is Chromium's
touch model with Ionic's recogniser on top.

| | Drag | Result |
|---|---|---|
| C1 | item-sliding, 80 px left | open 74 px, `scrollTop` 0 |
| C2 | item-sliding, 150 px up | closed, `scrollTop` 135: the first scroll event after a 16 px move scrolled 1 px, so Chromium's slop is about 15 px, above Ionic's 5 |
| C3 | item-sliding, 60 px left then 200 px up | open 74, **`scrollTop` 0**: once a touch has gone sideways, Chromium never scrolls it |
| C4 | item-sliding, 60 px up then 200 px left | closed, `scrollTop` 45: aborted at the threshold, and the gesture ignores the rest of the touch |
| C5 | item-sliding, 120 px at 30° from x | open 74, `scrollTop` 0: Chromium locks to the primary axis at its slop, so under 45° there's no scroll |
| C6 | item-sliding, 120 px at 50° | closed, `scrollTop` 80 |
| C7 | range knob: 80 px right; 150 px up; 60 px right then 200 px up | value 75, `scrollTop` 0; value 50, `scrollTop` 135; value 69, `scrollTop` 0. `touch-action: pan-y` on the slider |
| C8 | a raw x gesture, threshold 10, on a div; 150 px up | `notCaptured` at 16 px; `touchmove` keeps arriving through the scroll, `cancelable: false`; no `touchcancel` |
| C9 | raw x gesture; 60 px left then 200 px up | captured; `onMove` reaches `deltaY` −200 with `scrollTop` 0 |
| C10 | a raw **y** gesture on the div; 150 px up | **both**: `onStart`, `onMove` to `deltaY` −134, and `scrollTop` 135 |

Chromium also fires `pointercancel` at the first `touchmove` in every case, which is why `tap-click` never completes a press
that moved.

## The design

Namespace `Avonic.Components.Gestures`, folder `src/Avonic.Components/Gestures/`.

| Ionic | Avonic |
|---|---|
| `createGesture(config): Gesture` | `Gesture.Create(GestureConfig)` |
| `GestureConfig.el` | `GestureConfig.Element` (`InputElement`, required) |
| `gestureName`, `gesturePriority`, `disableScroll`, `threshold`, `maxAngle`, `blurOnStart` | `GestureName`, `GesturePriority` (0), `DisableScroll`, `Threshold` (10), `MaxAngle` (40), `BlurOnStart` |
| `direction: 'x' \| 'y'` | `GestureDirection { X, Y }`, `Direction` (`X`) |
| `canStart(detail): boolean` | `Func<GestureDetail, bool>? CanStart` |
| `onWillStart(detail): Promise<void>` | `Func<GestureDetail, Task>? OnWillStart` |
| `onStart`, `onMove`, `onEnd`, `notCaptured` | `Action<GestureDetail>?` of the same names |
| `passive` | Not applicable: Avalonia has no passive listeners, and a gesture never prevents a default. See [Not here](#not-here) |
| `Gesture.enable(enable = true)`, `destroy()` | `Enable(bool enable = true)`, `Destroy()`; `IDisposable` is `Destroy` |
| `GestureDetail` | `GestureDetail`: `Type` (`"pan"`), `StartX`, `StartY`, `StartTime`, `CurrentX`, `CurrentY`, `VelocityX`, `VelocityY`, `DeltaX`, `DeltaY`, `CurrentTime`, `Event` (`PointerEventArgs?`), `Data` (`object?`). Times are ms, from `AvonicConfig.Now` |
| `GESTURE_CONTROLLER` | `GestureController.Instance` |
| `createBlocker({ disable, disableScroll })`, `block()`, `unblock()`, `destroy()` | `CreateBlocker(IEnumerable<string>? disable = null, bool disableScroll = false)` → `GestureBlocker`: `Block()`, `Unblock()`, `Destroy()` |
| `isCaptured()`, `isScrollDisabled()` | `IsCaptured`, `IsScrollDisabled`, as properties |
| `ionGestureCaptured` on `document` | `GestureController.Instance.IonGestureCaptured`, `GestureCapturedEventArgs.GestureName`. The `Ion` prefix is kept, as for any Ionic event |
| `body.backdrop-no-scroll` | `ScrollDisabledChanged`, and `IsScrollDisabled`. Nothing in Avonic scrolls a body, so nothing reads it yet. See [Not here](#not-here) |
| `createGesture`'s delegate, `GestureDelegate`, `canStart`, `start`, `capture`, `release` | internal |

The algorithms are a line-for-line port: `PanRecognizer` (internal), `Gesture` holds `createGesture`'s closure state,
`GestureController` the maps. `requestAnimationFrame(fireOnMove)` is `AvonicConfig.RequestFrame`, so the coalescing is the same
and the test clock runs it at 16 ms. The time is `AvonicConfig.Now()`, Ionic's own `Date.now()` fallback, rather than
`PointerEventArgs.Timestamp`: Avalonia's is in the platform's units with no fixed origin, and the tests' clock couldn't drive it.
Positions are in the top level's coordinates, `e.GetPosition(null)`, as `pageX` is the page's; the controls only use deltas.

### Pointer events

Ionic listens on the element in the bubble phase, with no regard to a child having handled the event (the DOM has no "handled").
So the gesture adds `PointerPressed`, `PointerMoved`, `PointerReleased` and `PointerCaptureLost` on `Element`, bubbling,
`handledEventsToo`, because Avalonia's `Button` handles a left press, and a tunnelling `PointerMoved` for the scroller (below). One pointer at a time: a second touch's down is ignored, as
`hasStartedPan` ignores it in Ionic; its moves are ignored too, where Ionic reads `changedTouches[0]` of whatever finger moved.
Any mouse button starts a gesture, as `mousedown` does. `MOUSE_WAIT` has no equivalent: Avalonia doesn't synthesize mouse events
from a touch.

Avalonia implicitly captures the pointer to the element it hit, so a press inside `Element` keeps routing through it after the
finger leaves its bounds, as the browser's implicit touch capture and Ionic's `document` mouse listeners do.

### Capture

**Capturing** is `e.Pointer.Capture(null)`, then `e.Pointer.Capture(Element)`, then `e.PreventGestureRecognition()`, on the move or
press that captured.

- Letting go first raises `PointerCaptureLost` on the element that held the pointer and every ancestor, as Ionic's `ionGestureCaptured`
  on `document` reaches `tap-click` wherever the pressed surface is. A `Button` lets go of its press, and `Activatable` cancels its
  surface, whether the surface is inside `Element`, is `Element`, or is around it (a toggle's track inside a button item). Capturing
  straight to `Element` would raise it only below the common parent, so a surface around `Element` would stay activated. So
  `Activatable` doesn't need to change: capture leaving its holder is already its cancel.
- Gestures that hear the capture lost on the way are checked once it has arrived: one around `Element` still gets the pointer and
  carries on, as Ionic's does, and is refused at its own threshold. One inside `Element` will never get another move or the release,
  so it's ended at once, with `notCaptured`. Ionic's gets more moves and is refused at its threshold: the same callback, sooner.
- `PreventGestureRecognition` sets a flag on the pointer that every `GestureRecognizer` in the route checks, until the press ends.
  `ScrollViewer`'s `ScrollGestureRecognizer` then gets nothing: the scroller can't take the pointer away, and nothing scrolls for the
  rest of the press, which is C3 and C9. The gesture owns the pointer until the up. The release to null also raises it on the
  scroller's presenter, which ends the recogniser's tracking.

**Losing** it: when a `ScrollGestureRecognizer` captures, Avalonia routes every later move and the release to the recogniser alone,
and raises `PointerCaptureLost` on the element that held the capture, and its ancestors. The same when the platform cancels the touch,
and when `Element` leaves the tree. So `PointerCaptureLost` with the capture now outside `Element` (another element, a recogniser,
or nothing) is Ionic's `touchcancel`: an up with the last detail, `onEnd` if captured and `notCaptured` if started. A capture that
stays inside `Element`, a child taking it on its own press, is the same press, as `Activatable` treats it.

### The scroller

Chromium decides whether a touch scrolls on its own, at about 15 px, by the primary axis; Ionic's gesture decides separately, at its
threshold (5 to 10 px) and `maxAngle`. Avalonia's `ScrollGestureRecognizer` decides at 5 px on its own axis (`ScrollStartDistance`,
half the touch tap size, in headless and by default), with no axis lock: a 30° drag that moved 6 px down scrolls. Left alone it would
decide before most gestures and cancel them, so:

- **While a gesture is undecided,** each of its moves sets the pointer's skip flag in the tunnel phase, before the move reaches
  any recogniser, around `Element` or inside it (`ion-menu`'s gesture is on the document, around the content). The flag is
  `Pointer.IsGestureRecognitionSkipped`, public and settable. Recognisers still see the press, so they're tracking.
- **When the gesture aborts** (wrong direction, or lost the priority contest), and no other gesture on that pointer is still undecided
  or has captured, the flag is cleared, unless it was set before any gesture held it. The recogniser has been tracking since the press, so the next move scrolls, from the press point less its 5 px.
- **When it captures,** the flag stays, through `PreventGestureRecognition`.
- A gesture that doesn't start (`canStart` false, controller refused, disabled) touches nothing, and the scroller behaves as if the
  gesture weren't there.

| Case | Ionic in Chromium | Avonic |
|---|---|---|
| C1 sideways | pan, no scroll | the same |
| C2 up | scroll, `notCaptured` at the threshold; `scrollTop` 135 | the same; the scroll starts at the gesture's threshold, from the press less Avalonia's 5 px, so 145 |
| C3 sideways then up | pan, never scrolls | the same |
| C4 up then sideways | scroll, the gesture is out for the rest of the touch; 45 | the same; 55, for the same reason |
| C5 30° | pan, no scroll | the same, at any threshold, because the gesture decides first |
| C6 50° | scroll | the same |
| 40° to 45° | neither: Ionic aborts, Chromium's primary axis is x | scrolls, as the recogniser has no axis lock. A difference that needs a drag held within 5° for 10 px |
| C10 a y gesture in a y scroller | **both** | **the gesture alone.** Avalonia delivers a pointer to one owner, and a captured recogniser gets everything. A control that wants both, `ion-refresher`, has to decide in `CanStart`, or watch the scroller instead |

A mouse never scrolls a `ScrollViewer` by dragging, so none of this touches the mouse.

### `disableScroll`

The state and the events are there: `DisableScroll` on a gesture adds the blocker at capture and removes it at release; a `GestureBlocker`
adds it at `Block()`. `IsScrollDisabled` reports it and `ScrollDisabledChanged` says when. Ionic's only effect is `overflow: hidden` on
the body, and nothing in Avonic is the body: `AvonicContent` is `ion-content`, which Ionic leaves alone. When `ion-app` (#65) or
`ion-menu` need a root that stops scrolling, they read it here.

## Behaviours to test

Real headless pointer input, on the test clock, each one failing without its behaviour. Expected numbers are the measured ones above.

| # | Behaviour | Source |
|---|---|---|
| G1 | A gesture starts disabled; `Enable()` makes a press start it; `Enable(false)` while captured ends it with `OnEnd` and the last detail, and later input in that press does nothing | `enable()` |
| G2 | Below the threshold nothing fires; at it, along the axis, the gesture captures: `OnStart` with the start reset to the capturing move and `DeltaX` still the move's; `OnMove` then measures from the new start | `tryToCapturePan` |
| G3 | Direction: 39° captures, 41° doesn't; the wrong axis aborts at once with `NotCaptured`, and the rest of that press is ignored; `Direction.Y` with its own threshold; negative movement along the axis counts | `createPanRecognizer` |
| G4 | Threshold 0 captures at the press | `pointerDown` |
| G5 | Velocity: the exponential average, gaps of 100 ms or more and of 0 ms skipped, the first move of a frame counted and the rest of the frame dropped, `OnMove` on the next frame, the up's recalculation | `calcGestureData`, `pointerMove` |
| G6 | An up before the threshold is `NotCaptured` with the up's position | `pointerUp` |
| G7 | `CanStart` false: nothing, not even `NotCaptured`; the controller refuses while another gesture is captured, and `CanStart` is still consulted first | `pointerDown`, `GestureController.canStart` |
| G8 | Priority: the lower priority reaching its threshold first loses and aborts; the higher captures later. Equal priorities: the later-created wins | `GestureController.capture` |
| G9 | Capture handoff: capturing moves Avalonia's capture from the pressed child to the element, which cancels a `Button`'s press and an `Activatable` surface's activation, with the surface the element, inside it or around it; moves outside the element's bounds keep arriving; a gesture around the one that captured carries on and is refused at its threshold; one inside it is `NotCaptured` at once, and can start on the next press | `ionGestureCaptured`, `tap-click` |
| G10 | Cancel on capture lost: another element taking the pointer, the platform cancelling the touch, or the element leaving the tree, ends a captured gesture with `OnEnd`, and an undecided one with `NotCaptured`; a child's own capture inside the element doesn't. A scroller can't take it while the gesture decides (G11), so that path is the same `PointerCaptureLost` | `touchcancel` |
| G11 | Scroll blocking: a captured x gesture inside `AvonicContent` and inside a plain `ScrollViewer` stops the scroll for the rest of the press, including after a 200 px vertical drag (C3); a vertical drag on the same element scrolls and the gesture is `NotCaptured` (C2); drags at 39° (threshold 10) and 30° (threshold 20) in 2 px steps, whose vertical movement passes the recogniser's 5 px before the threshold, pan and don't scroll (C5); 50° scrolls (C6); a scroller inside the element waits from the first move; two gestures deciding hold the scroller until both give up; a y gesture in a y scroller has the pointer to itself (C10) | measured |
| G12 | `DisableScroll`: `IsScrollDisabled` true from capture to release, `ScrollDisabledChanged` raised; a blocker's `Block()`/`Unblock()` the same, and `disable` by name stops a gesture starting | `gesture-controller.js` |
| G13 | `OnWillStart`: `OnStart` waits for the task; an up before it settles fires nothing at the up, and `OnStart` once it settles | `tryToCapturePan`, `pointerUp` |
| G14 | `BlurOnStart` clears the focused element at `OnStart` | `blurActiveElement` |
| G15 | `IonGestureCaptured` is raised with the name at every capture; `IsCaptured` is true until the release | `capture()` |
| G16 | `Destroy()` releases the controller's state and stops listening; a second pointer while one is tracked is ignored | `destroy()`, `hasStartedPan` |

## Deliverables

| | Path |
|---|---|
| Code | `src/Avonic.Components/Gestures/`: `Gesture.cs`, `GestureConfig.cs`, `GestureDetail.cs`, `GestureDirection.cs`, `GestureController.cs`, `GestureBlocker.cs`, `GestureCapturedEventArgs.cs`, `PanRecognizer.cs`, `RecognitionHold.cs` (the scroller's skip flag) |
| Tests | `tests/Avonic.Components.Tests/Gestures/GestureTests.cs`, `GestureScrollTests.cs` |
| Sample | `samples/Avonic.Sample/Pages/GesturePage.axaml`: a draggable box in a scrolling list, showing the detail, `[SamplePage("gesture", Order = 4)]` |
| Docs | this plan, `README.md` with the API and how a control uses it |

Nothing visual: no theme, tokens or styles.

## Not here

- **`passive`.** A gesture in Avalonia never prevents anything; a captured one owns the pointer through capture. Declaring the option would be a dead one.
- **`body.backdrop-no-scroll`'s effect.** Recorded above: the state exists, nothing is the body.
- **A controller per `TopLevel`.** Ionic's is per document. One per process is simpler, and a user has one pointer; two windows
  dragging at once would arbitrate across windows. Listed for the PR.
- **A threshold-0 gesture that loses at the press.** Measured: Ionic's `pointerDown` returns false with `hasStartedPan` still set,
  and the gesture never starts again until it's disabled (its `canStart` isn't even called on later presses). Avonic lets go, as a
  refused press does, so the next press works. A deliberate difference, tested by `G8_A_Threshold_Zero_Gesture_That_Loses_At_The_Press_Can_Start_Again`.
- **A cancel's detail.** Ionic's `touchcancel` recalculates from the cancel's touch; `PointerCaptureLost` has no position, so the
  detail is the last move's, as `enable(false)`'s is.
- **Pointer events of a second finger** are ignored while one is tracked. Ionic reads whichever finger moved, which is a bug there,
  not a behaviour.
