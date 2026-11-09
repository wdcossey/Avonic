# Plan: the overlay system and the animation utility (#5)

Ionic's `utils/overlays`, `utils/animation`, `utils/focus-trap` and the overlay half of `utils/hardware-back-button`, on Avalonia.
It's infrastructure, not a control: `ion-alert`, `ion-action-sheet`, `ion-loading`, `ion-modal`, `ion-popover`, `ion-toast` and
`ion-menu` build on it. `ion-backdrop`, its first consumer, has its own plan in
[`docs/components/ion-backdrop/PLAN.md`](../../components/ion-backdrop/PLAN.md).

Sources, `@ionic/core` 8.8.1, `dist/collection/`: `utils/overlays.js` (`prepareOverlay`, `present`, `dismiss`, `eventMethod`,
`trapKeyboardFocus`, `connectListeners`, `getPresentedOverlay`), `utils/animation/animation.js` (`createAnimation`),
`utils/animation/cubic-bezier.js` (`getTimeGivenProgression`), `utils/focus-trap.js`, `utils/hardware-back-button.js`,
`utils/lock-controller.js`, `components/loading/loading.js` as the simplest consumer, and the md `animations/md.enter.js` and
`md.leave.js` of alert, action-sheet, loading, modal, popover and toast, with `utils/menu-controller/animations/` for the menu.

## What Ionic does

### An overlay's life

Every overlay is a component that calls the shared `present()` and `dismiss()`, each inside its own `lockController`:

1. **Created** (`connectedCallback`): `prepareOverlay` gives it `overlayIndex`, a counter for the whole page starting at 1, and the
   component sets `z-index` inline to a base plus that index: 20000 for alert, action-sheet, modal, popover and picker, 40000 for
   loading, 60000 for toast. A controller appends the element to `ion-app`, or `body` without one, with `overlay-hidden`
   (`display: none !important`). The first one adds three listeners to the document: `focus` (captured) for the trap,
   `ionBackButton`, and `keydown` for Escape.
2. **`present()`**: nothing if already presented. Unless it's a toast, the focused element is blurred, and given focus back after
   the next `didDismiss` if nothing has focus then. `presented = true`; `willPresent`. The overlay shows (`overlay-hidden` goes),
   the enter animation is built (the app's `enterAnimation`, else the config's, else md's), its duration is set to 0 if the overlay's
   `animated` or the global `animated` is false, `keyboardClose` adds a `beforeAddWrite` that blurs a focused input, and it plays.
   When it finishes: `didPresent`. Then, with `keyboardClose`, the overlay itself takes focus unless focus is already inside it.
3. **`dismiss(data, role)`**: `false` if not presented. `presented = false`; `pointer-events: none` on the overlay;
   `willDismiss({ data, role })`. The leave animation plays, unless the role is `'gesture'`. `didDismiss({ data, role })`; every
   animation the overlay played is destroyed, so their held values go; `overlay-hidden` again; the element is removed. `true`.
4. **`onWillDismiss()` / `onDidDismiss()`** register a one-time listener and resolve with the next event's detail.

`present()` and `dismiss()` take the overlay's lock, so a `dismiss()` during the enter animation waits for `didPresent`.

### Which overlay is on top

`getPresentedOverlay(doc, selector?)` is the **last in document order** that isn't `overlay-hidden`, among alert, action-sheet,
loading, modal, picker, popover and toast. Document order is the order they were appended, so the last created. It isn't presented
from `willPresent` until its animation starts, and still is during its leave animation.

- **Escape** (document `keydown`): if the top overlay's `backdropDismiss` is true, `dismiss(undefined, 'backdrop')`. A toast has no
  `backdropDismiss`, so a toast on top stops Escape reaching an alert under it. Nothing is prevented.
- **Hardware back** (`ionBackButton`, which `ion-app` raises when `hardwareBackButton` is on, which by default it is only in a
  Capacitor or Cordova app): the same, registered at priority 100 (`OVERLAY_BACK_BUTTON_PRIORITY`), above the menu's 99.
- **Backdrop tap**: each overlay listens for `ionBackdropTap` and calls `dismiss(undefined, 'backdrop')`. The backdrop is
  `tappable` only when `backdropDismiss` is.

### The focus trap

On every `focus` in the document, the top overlay that isn't a toast is found. If it has `ion-disable-focus-trap` (a modal or
popover with `focusTrap: false`), nothing. Otherwise, two variants:

- **Shadow** (modal, popover): focus inside the overlay is remembered as `lastFocus`; focus outside it is moved to its first
  focusable descendant, or the last if the first is where focus came from (Shift+Tab from the first), or the overlay itself if it
  has none.
- **Scoped** (alert, action-sheet, loading, picker): focus inside `.ion-overlay-wrapper`, or on the backdrop, is remembered; focus
  on the two sentinels around the wrapper (`tabindex="0"` divs) wraps as above; **focus outside the overlay is left alone**.

### `createAnimation`

A builder over the Web Animations API. Timing (`duration`, `delay`, `easing`, `iterations`, `fill`, `direction`) falls back to the
parent animation's, then to 0, 0, `linear`, 1, `both`, `normal`. `play()` initializes on first use (the `before*` hooks, classes and
styles, then one Web Animation per element, paused), plays them and resolves when this animation and all its children have finished
(`onFinish`), or when `stop()` is called. An animation with no keyframes or no elements finishes at once. `fill: 'both'` holds the
end value after finishing, until `destroy()` cancels it: that's how an overlay's backdrop stays at `--backdrop-opacity` while it's
presented.

## Measured

Real Ionic 8.8.1, md, headless Chromium, 390 x 844, in `ion-app`, with `loadingController`, `alertController`, `modalController` and
`toastController` from `dist/esm`. Times are from the call, by `performance.now()`; frames by a `requestAnimationFrame` counter. The
raw output is in [`ref/measurements.txt`](ref/measurements.txt), from a later run of the same scripts, so its times differ from
this table's by up to a frame.

| | Case | Result |
|---|---|---|
| L1 | `loading.present()`, md enter 200 ms | `ionLoadingWillPresent`, then `willPresent`, at 0.2 ms, before the first frame. `didPresent` at 211.2 ms, frame 12; `present()` resolves after it. z-index 40001, `overlayIndex` 1, a child of `ion-app`, `overlay-hidden` until presented |
| L2 | after L1 | the backdrop's opacity is 0.32, held by a `finished` animation with `fill: both`; focus is on `ion-loading` (`keyboardClose`) |
| L3 | `dismiss({ a: 1 }, 'ok')` | `willDismiss` at 0.4 ms with `{ data: { a: 1 }, role: 'ok' }`, then `onWillDismiss()` resolves; `didDismiss` at 216.1 ms, frame 13, then `onDidDismiss()`; `dismiss()` resolves `true`; the element is gone. A second `dismiss()` resolves `false` |
| L4 | `present()` then at once `dismiss('d', 'r')` | `willDismiss` waits for `didPresent` (201 ms); `didDismiss` at 417 |
| L5 | `present()` twice at once | one `willPresent` |
| L6 | global `animated: false` | `didPresent` at 12.2 ms, **frame 0**: a zero-duration animation finishes at the next frame's animation update, before its `requestAnimationFrame` callbacks. `didDismiss` at 15.9 ms, frame 1. The order of events is the same, and the backdrop still ends at 0.32 |
| F1 | focus on a button, `alert.present()` | the button loses focus at once (`body` is focused during the animation) |
| F2 | alert, two buttons, Tab x4, Shift+Tab x4 | Tab cycles between the buttons; Shift+Tab reaches the wrapper (alert's own `tabindex`) and cycles. Focus never leaves |
| F3 | alert, `other.focus()` on a button behind it | focus stays on `other`: the scoped trap leaves focus outside the overlay alone |
| F4 | modal with buttons m1, m2, Tab x4, Shift+Tab x3 | m1, m2, m1, m2; m1, m2, m1 |
| F5 | modal, `other.focus()` with `lastFocus` m1 | focus moves to **m2**: the first descendant is m1, where focus was, so the last |
| F6 | dismiss the modal | focus returns to the opener. If something else was focused during the dismiss (in `willDismiss`, behind an alert), it stays there |
| E1 | Escape with alert on top (`backdropDismiss: true`) | dismissed with `{ role: 'backdrop' }` |
| E2 | Escape with an alert, `backdropDismiss: false`, over a modal | neither is dismissed |
| E3 | Escape with a toast over a modal | neither is dismissed |
| S1 | modal then alert | z-index 20002 and 20003, `overlayIndex` 2 and 3 |
| B1 | tap the alert's backdrop | `onDidDismiss()` resolves `{ role: 'backdrop' }` |

### Web Animations values

Sampled in Chromium by `el.animate(keyframes, timing)`, paused, at each `currentTime`, reading the computed style, on a 200 x 300
element, at local times that fall on the test clock's 16 ms frames. `AvonicAnimationTests` asserts each, opacity and scale to 1e-4
and pixels to 1e-3.

| Animation | ms: value |
|---|---|
| alert md enter: opacity 0.01 → 1, `scale(0.9)` → `scale(1)`, 150 ms, `ease-in-out` | 0: 0.01, 0.9; 32: 0.102319, 0.909325; 80: 0.561736, 0.955731; 112: 0.86861, 0.986728; 144: 0.996953, 0.999692; 160: 1, 1 |
| action-sheet md enter: `translateY(100%)` → `translateY(0%)`, 400 ms, `cubic-bezier(.36,.66,.04,1)` | 0: 300; 48: 219.68; 96: 100.87; 208: 15.8117; 400: 0 |
| modal md leave: opacity 0.99 → 0, `translateY(0px)` → `translateY(40px)`, 200 ms, `cubic-bezier(0.47,0,0.745,0.715)` | 48: 0.925564, 2.6035; 96: 0.724577, 10.7242; 144: 0.400144, 23.8326 |
| opacity 0 → 1, 100 ms, delay 50, 2 iterations, `alternate`, `ease` | 0 and 16: the element's own (fill `none`, `forwards`) or 0 (`backwards`, `both`); 80: 0.513315; 128: 0.970068; 144: 0.998007; 160: 0.994316; 240: 0.094796; 256 and 304: its own (`none`, `backwards`) or 0 (`forwards`, `both`) |
| menu md: `translateX(-288px)` → `translateX(0px)`, 300 ms, `cubic-bezier(0.0,0.0,0.2,1)`, `reverse` | 0: 0; 96: -17.3969; 288: -246.275; 304: -288 |
| opacity 0, 1, 0.5 with no offsets, 100 ms | 16: 0.32; 48: 0.96; 80: 0.7 |
| `ease-in`, `ease-out`, 100 ms | in: 16: 0.04125, 48: 0.294144; out: 16: 0.250589, 48: 0.662901 |
| `translateY(40px) scale(0.5)` → `translateY(0px) scale(1)`, 100 ms | 48: 20.8, 0.74 |
| opacity 0 → 1, 1.5 iterations, `reverse`, fill `forwards` | 48: 0.52; 112: 0.88; 144: 0.56; 160: 0.5 |

And of Ionic's `createAnimation`: a child with no timing of its own takes its parent's duration, easing and delay, and `both`;
an animation with no elements resolves `play()` at once; `stop()` resolves a pending `play()` and `isRunning()` is false after it;
`beforeStyles({ 'pointer-events': 'none' })` is an inline style during the animation and `afterClearStyles` removes it;
`beforeAddClass`, `afterRemoveClass` and `afterAddClass` change the element's classes at the start and the end; `onFinish` gets
step 1. Ionic's own `getTimeGivenProgression`, run in Node, gave the values `A8` asserts, including 0.849 at 0.5 on the menu's
symmetric curve, a quirk of its cubic solver that the port keeps.

## The design

Two namespaces in Components, `Avonic.Components.Overlays` (`src/Avonic.Components/Overlays/`) and `Avonic.Components.Animation`
(`src/Avonic.Components/Animation/`), as Ionic has two utilities.

### The host

Ionic appends overlays to `ion-app`, which Avonic doesn't have yet (#65). The host is **a panel of Avonic's own in the `TopLevel`'s
`OverlayLayer`**, one per `TopLevel`, made when the first overlay there is presented.

- `OverlayLayer` is Avalonia's layer above the page, in the `VisualLayerManager` every base theme's `Window` and single-view root
  template has. It's below `LightDismissOverlayLayer` and `PopupOverlayLayer` (z-indexes 2147483549, 2147483550 and 2147483552), so
  a popup or a `ComboBox` opened from inside an overlay shows above it, as one from inside an Ionic overlay does.
- `OverlayLayer` is a `Canvas`, which gives a child its desired size, and measures it with no limit. So the host reports the layer's
  `AvailableSize` as its size, and measures again when the layer's size changes; inside it, overlays are full-size children of a
  `Panel`, as `position: fixed; inset: 0` is in Ionic.
- An overlay's `ZIndex` is Ionic's: the base plus `overlayIndex`. The host's children are in the order they were attached, which is
  Ionic's document order, for "the top overlay".
- A `TopLevel` with no `OverlayLayer` (a template without a `VisualLayerManager`) can't show an overlay: `Present()` throws, saying
  so. The tests add `SimpleTheme`, as the sample has it.

Not `AdornerLayer`, which is below the overlay layer and is for decorations; not a `Popup`, which is a separate window on desktop
with its own focus scope; not wrapping the `TopLevel`'s content, which an app owns.

**Which `TopLevel`.** `present()` has no arguments, and a page has one document. `Present()` uses the application's: the active
window of a desktop lifetime, else its main window; a single-view lifetime's main view. `Present(TopLevel)` is an Avonic extension
for a multi-window app.

**The index** is given when the overlay is attached to the host, at `Present()`, as `connectedCallback` gives it when an element is
attached. Ionic's controller attaches at `create()`; Avonic has no `create()`, so an overlay made first and presented second is on
top, where Ionic's would be under. The controllers can attach earlier when they come.

### `AvonicOverlay`

An abstract `TemplatedControl`, the base of every overlay control. What every overlay in `core.json` has is declared here; what
only some have is a protected member the control overrides, as no property may be declared on a control it does nothing for.

| Ionic | Avonic |
|---|---|
| `animated` (all, `true`) | `Animated` |
| `keyboardClose` (`true`; toast `false`) | `KeyboardClose`; a toast overrides the default |
| `enterAnimation`, `leaveAnimation` (`AnimationBuilder`) | `EnterAnimation`, `LeaveAnimation`, `AnimationBuilder` delegate `(Control baseEl, object? opts) → AvonicAnimation` |
| `present(): Promise<void>` | `Task Present()`; `Task Present(TopLevel)` |
| `dismiss(data?, role?): Promise<boolean>` | `Task<bool> Dismiss(object? data = null, string? role = null)` |
| `onWillDismiss<T>()`, `onDidDismiss<T>()`: `Promise<OverlayEventDetail<T>>` | `Task<OverlayEventDetail> OnWillDismiss()`, `OnDidDismiss()`; `OverlayEventDetail(object? Data, string? Role)` |
| `willPresent`, `didPresent`, `willDismiss`, `didDismiss`, the generic set every overlay has in `core.json` | routed events `WillPresentEvent`, `DidPresentEvent`, `WillDismissEvent`, `DidDismissEvent` and the CLR events `WillPresent`, `DidPresent`, `WillDismiss`, `DidDismiss` (`OverlayEventArgs` with `Data`, `Role`), bubbling: the mechanical rule, which keeps an event's own name, and these have no `ion` prefix. No member of Avalonia's base types has these names. The human decided this at the PR |
| `ionAlertWillPresent`, `ionModalDidDismiss` and the rest, the per-control set | Not here: each overlay control adds its own `Ion<Name>WillPresent`, `Ion<Name>DidPresent`, `Ion<Name>WillDismiss` and `Ion<Name>DidDismiss` when it's built, raised with the generic ones |
| `backdropDismiss` (not on toast) | `protected virtual bool BackdropDismissEnabled`, `false`; the control declares the property and returns it |
| `md.enter.js` / `md.leave.js` | `protected abstract AnimationBuilder MdEnterAnimation`, `MdLeaveAnimation`; `protected virtual object? AnimationOptions` |
| z-index base | `protected virtual int ZIndexBase`, 20000 |
| "not `ION-TOAST`" | `protected virtual bool StealsFocus`, `true`: blurs and restores focus, and can trap it |
| `ion-disable-focus-trap` (`focusTrap: false`) | `protected virtual bool FocusTrapEnabled`, `true` |
| `.ion-overlay-wrapper` in a scoped overlay | `protected virtual Control? FocusTrapWrapper`, `null` for the shadow variant |
| `onIonBackdropTap` | `protected virtual void OnBackdropTap()`: `Dismiss(null, "backdrop")`, raised by any `AvonicBackdrop` in the overlay |
| `BACKDROP`, `GESTURE`, `isCancel(role)` | `OverlayRoles.Backdrop`, `.Gesture`, `.IsCancel(role)` |
| `lockController` | the base takes an async lock around `Present` and `Dismiss`, as every overlay does |

The overlay is `Focusable` and not a tab stop (`tabindex="-1"`), and hidden (`IsVisible` false) until its animation starts, as
`overlay-hidden` is. `pointer-events: none` during the dismiss is `IsHitTestVisible` false at animation priority.

**Async.** `Present`, `Dismiss`, `OnWillDismiss`, `OnDidDismiss` and `Play` are tasks that complete on Avalonia's dispatcher, with
their continuations run asynchronously, as a promise's are: a handler awaiting `OnDidDismiss()` runs after `Dismiss` has removed the
overlay, as Ionic's `restoreElementFocus` does. Everything up to the first frame is synchronous, as in Ionic: `WillPresent` is
raised inside `Present()`.

**The focus trap** is Ionic's `trapKeyboardFocus`, on the host: a `GotFocus` handler on the `TopLevel` (bubbling, handled events
too, as a captured `focus` listener sees every focus). "Inside" is a visual or a logical descendant, so a popup opened from inside
the overlay, a `ComboBox`'s drop-down, is inside, though it's in another layer or window; in Ionic, `ion-select`'s drop-down is an
overlay of its own, on top. "Focusable" is Ionic's query in Avalonia's words: `Focusable`, effectively enabled and visible, and a
tab stop, in visual-tree order. It's a flat query, as Ionic's `querySelector` is, so it doesn't follow a container's
`KeyboardNavigation.TabNavigation`, as Ionic's doesn't follow a roving `tabindex`. A wrap uses the focus's own navigation method, so a Tab wrap shows the
focus ring, as `focusVisibleElement` does. Avalonia's own Tab from the last element of the overlay goes to the first of the page,
as the browser's goes past the overlay (measured, headless), so the shadow variant catches it as Ionic's does; a scoped overlay's
template has two sentinels, as Ionic's has.

**Escape** is a `KeyDown` handler on the `TopLevel`, not marked handled, as Ionic prevents nothing. It doesn't take handled
keys: a control that handles Escape, as a `ComboBox` does to close its drop-down, has stopped it, as `stopPropagation()` keeps a
`keydown` from the document.
**Hardware back** is `TopLevel.BackRequested`, which Avalonia raises for Android's back button, and on desktop for Alt+Left and the
mouse's back button (measured, headless): Escape doesn't raise it. When the top overlay dismisses, it's marked handled, as a
registered handler stops the app's own back. The priority registry of `hardware-back-button.js` (`ionBackButton`, `register`) is
`ion-app`'s and comes with #65; the overlay's priority, 100, is recorded for it.

### `AvonicAnimation`

`AvonicAnimation.Create(string? id = null)` is `createAnimation`. Its members keep Ionic's names in PascalCase and return the
animation, so the md builders read as Ionic's do:

```csharp
var backdrop = AvonicAnimation.Create()
    .AddElement(backdropEl)
    .FromTo("opacity", 0.01, backdropOpacity)
    .BeforeStyles(new() { ["pointer-events"] = "none" })
    .AfterClearStyles(["pointer-events"]);
var wrapper = AvonicAnimation.Create()
    .AddElement(wrapperEl)
    .Keyframes([new(0) { ["opacity"] = 0.01, ["transform"] = "scale(0.9)" }, new(1) { ["opacity"] = 1, ["transform"] = "scale(1)" }]);
return AvonicAnimation.Create().AddElement(baseEl).Easing("ease-in-out").Duration(150).AddAnimation(backdrop, wrapper);
```

- **Timing**: `Duration`, `Delay`, `Easing`, `Iterations`, `Fill` (`AnimationFill`), `Direction` (`AnimationDirection`), each
  falling back to the parent's, and the getters. The model is Web Animations' (phases, active time, iteration progress, direction,
  fill), on `AvonicConfig.Now` and `AvonicConfig.RequestFrame`, so `TestClock` runs it at 16 ms. Values are read every frame, so a
  duration set while running applies, as `update()` does.
- **Easing**: `linear`, `ease`, `ease-in`, `ease-out`, `ease-in-out` and `cubic-bezier(x1, y1, x2, y2)`, solved as Chromium's
  `UnitBezier` is, by `CubicBezierEasing`, an Avalonia `Easing` too. `CubicBezier.GetTimeGivenProgression` is
  `cubic-bezier.js`, for the menu's and the sheet's gestures.
- **Playback**: as Web Animations: `Play()` applies the current values at once (an animation paused at 0 with `fill: both` shows its
  first frame) and starts at the next frame, which is its start time; it finishes at the first frame at or past its end. A
  zero-duration animation finishes at the next frame, as L6. `Play(sync: true)`, `Pause()`, `Stop()`, `Destroy()`, `IsRunning()`,
  `OnFinish(callback, oneTimeCallback)`.
- **Properties**: `opacity` (`Opacity`), a number or a numeric string, and `transform` (`RenderTransform`), a CSS string. Ionic's
  backdrop animations go to `'var(--backdrop-opacity)'`, resolved by the browser; here the builder reads the overlay's resource
  and gives the number, at present, which is when Ionic builds the animation too. `transform` takes
  `translateX`, `translateY`, `translate`, `scale`, `scaleX`, `scaleY`, `rotate` in `px`, `%` and `deg`, interpolated function by
  function, with a percentage resolved against the element's size whenever its bounds change, so a `translateY(100%)` that starts
  before the first layout is right in the first frame drawn. Values are applied at animation priority through one binding per
  element and property; a later animation on the same property wins, as a later Web Animation does.
- **Hooks**: `BeforeAddRead`, `BeforeAddWrite`, `AfterAddRead`, `AfterAddWrite`; `BeforeAddClass`, `BeforeRemoveClass`,
  `AfterAddClass`, `AfterRemoveClass` (style classes); `BeforeStyles`, `BeforeClearStyles`, `AfterStyles`, `AfterClearStyles` for
  `opacity` and `pointer-events` (`IsHitTestVisible`), as local values, as inline styles are, `""` clearing. The md animations
  style only `pointer-events`; the popover's position styles come with it.

An unsupported property throws `ArgumentException` when it's given, rather than doing nothing.

### `AvonicConfig.Animated`

Ionic's overlays read the global `animated` at every present and dismiss: an animation whose duration is set to 0, with every event
in the same order (L6). The overlay does the same, with `AvonicConfig.Animated` or its own `Animated`. Its remark in
`AvonicConfig.cs` already lists overlays, so that file is unchanged.

## Behaviours to test

Headless, on the test clock, with `SimpleTheme` for the overlay layer, in an `AvonicContent` page in a window, and real headless input
for keys and pointers. Each one has to fail without its behaviour.

| # | Behaviour | Source |
|---|---|---|
| O1 | `Present()` adds the overlay to the `TopLevel`'s overlay layer, hidden until its animation starts, full size, following the window's size; `WillPresent` at once, the animation's first values before the first frame, `DidPresent` when it finishes, then `Present()` completes; `EnterAnimation` and `LeaveAnimation` replace md's, and every builder gets `AnimationOptions` | `present`, L1 |
| O2 | `Animated` false, or `AvonicConfig.Animated` false: the same events, `DidPresent` at the next frame; the end values are shown | `overlayAnimation`, L6 |
| O3 | `Present()` while presented does nothing; two at once raise `WillPresent` once | L5 |
| O4 | `Dismiss(data, role)`: `WillDismiss` with them, the leave animation, `DidDismiss`, the overlay removed, the enter animation's held values gone, `true`; `false` when not presented; no leave animation for the `gesture` role; presented again, it's above what came since | `dismiss`, L3 |
| O5 | `OnWillDismiss()` and `OnDidDismiss()` complete with the next event's data and role, after the overlay has gone | `eventMethod` |
| O6 | `Dismiss()` during the enter animation waits for `DidPresent` | `lockController`, L4 |
| O7 | The overlay can't be hit during its dismiss | `pointer-events: none` |
| O8 | Stacking: `ZIndex` is the base plus a counter from 1, so the later is above, and hit tests reach it | `prepareOverlay`, S1 |
| O9 | Escape and hardware back dismiss the top overlay with the `backdrop` role when it allows it; not when it doesn't, even with one under it that does, nor during its leave animation, nor when a control inside has handled Escape; back is marked handled only when it dismisses, and a back the app has handled is left alone | `connectListeners`, E1 to E3 |
| O10 | A backdrop tap in the overlay dismisses it with the `backdrop` role when it's tappable; not during the enter animation, when the backdrop has `pointer-events: none` | `onIonBackdropTap`, B1 |
| O11 | Focus: blurred at present, given back after the dismiss if nothing has focus, not if something does; the overlay takes focus after `DidPresent` with `KeyboardClose`, not without, nor when focus is already inside; a focused `TextBox` is blurred as the animation starts; an overlay that doesn't steal focus leaves it; a dismiss that failed gives no focus back, then or after a later present | `restoreElementFocus`, `keyboardClose`, F1, F6 |
| O12 | The trap, shadow variant: Tab and Shift+Tab cycle inside; focus given to something behind moves inside, to the last if the first was where it came from; to the overlay itself with nothing focusable, skipping what isn't a tab stop; a popup opened from inside is inside, in both variants | `trapShadowFocus`, F4, F5 |
| O13 | The trap, scoped variant: Tab wraps through the sentinels; focus behind it is left alone | `trapScopedFocus`, F2, F3 |
| O14 | The trap: only the top overlay that steals focus traps; focus moving into one that doesn't goes back to the last focus; one with the trap disabled traps nothing | `trapKeyboardFocus` |
| O15 | `Present()` with no `TopLevel` uses the application lifetime's; throws with none, and without an overlay layer | the host |
| A1 | Easing and interpolation equal Chromium's (opacity and scale to 1e-4, pixels to 1e-3): alert, action-sheet and modal md values, the keywords, two functions | measured |
| A2 | The timing model: delay, iterations, the four directions, the four fills, computed keyframe offsets, infinite iterations | measured |
| A3 | Parent timing inherited by children; the defaults; the root finishes when all have; an animation with no elements finishes at once | `play`, `animationFinish` |
| A4 | `Play()` shows the first values at once and finishes at the first frame past the end; zero duration at the next frame; `sync`; a replay starts over; a duration set while running applies | measured |
| A5 | `Pause()` holds, `Play()` resumes; `Stop()` reverts and completes `Play()`; `Destroy()` reverts; `OnFinish` once or always; `IsRunning`; a later animation of a property wins | `animation.js` |
| A6 | Hooks: before and after read and write, classes, styles, cleared styles; unsupported properties refused | `beforeAnimation`, `afterAnimation` |
| A7 | `transform` percentages follow the element's size, from before its first layout; `none` interpolates with the identity | the host's need |
| A8 | `CubicBezier.GetTimeGivenProgression` equals Ionic's on the menu's curve and others | `cubic-bezier.js` |

## Found while building

- **An animation-priority binding that yields `UnsetValue` gives the property's default**, not the value under it (measured:
  local 0.7 under it, `Opacity` read 1). So an effect with no value (outside its active time, with no fill there) drops its
  binding, and binds again when it has a value. A track bound again is newer than any other animation's on that property, and wins
  from then on; only an animation with a delay and no backwards fill does that.
- **Hit tests read the last rendered scene, and a newly made overlay layer can take a second render tick** to reach it in a
  headless test. The overlay tests render twice.
- **`TopLevel.BackRequested`** is raised by Alt+Left and the mouse's back button headless, as on desktop, and not by Escape.
- **Avalonia's lifetimes** can't be set once the application has started, and their interfaces can't be implemented outside
  Avalonia, so `Present()`'s choice of top level is tested through `OverlayHost.TopLevelOf` with Avalonia's own (internal) desktop
  lifetime, made by reflection.

## Deliverables

| | Path |
|---|---|
| Code | `src/Avonic.Components/Overlays/`, `src/Avonic.Components/Animation/`, `src/Avonic.Components/Controls/Backdrop/` |
| Theme | `src/Avonic.Themes/Controls/AvonicBackdrop.axaml` (the backdrop's tokens are already in `Tokens/Global.axaml`) |
| Tests | `tests/Avonic.Components.Tests/Overlays/`, `tests/Avonic.Components.Tests/Animation/`, `tests/.../Controls/AvonicBackdrop*Tests.cs` |
| Sample | `samples/Avonic.Sample/Pages/OverlaysPage.axaml`, `[SamplePage("overlays", Order = 5)]`: a test overlay, a backdrop with a box on it, marked in the code as a test vehicle until the real overlays come |
| Real window | a section of `tools/real-window/drive.ps1` for the sample page, not run here |
| Docs | this plan, `README.md`; `docs/components/ion-backdrop/` |

## Deferred

What the overlay issues get from this, and what each still has to add.

| Not here | Why | Who needs it |
|---|---|---|
| Controllers (`alertController.create`, `dismiss(data, role, id)`, `getTop`) | Each overlay's issue; `getPresentedOverlay` is in the host, internal, for them | all |
| Per-control events: `Ion<Name>WillPresent`, `Ion<Name>DidPresent`, `Ion<Name>WillDismiss`, `Ion<Name>DidDismiss` (`ionAlertWillPresent` → `IonAlertWillPresent`, and the rest), raised with the base's generic `WillPresent` and the rest; `htmlAttributes`, `cssClass` | A control's own; `cssClass` is `Classes` | all |
| `isOpen`, `trigger`, inline overlays, `delegate`, `keepContentsMounted` | They teleport a control that's in a page to the root; Avalonia's logical tree has to be worked out for it | all, modal and popover |
| `canDismiss` with a handler | The modal's | modal |
| Root `aria-hidden` and `inert` (`setRootAriaHidden`), and `backdrop-no-scroll` on the body | Needs the app root, #65; the gesture controller's `IsScrollDisabled` is where no-scroll goes | all |
| The `ionBackButton` priority registry and the `hardwareBackButton` config | `ion-app`'s, #65. The overlay handles `BackRequested` itself until then | menu, nav |
| `experimentalCloseWatcher` | A browser API | none |
| Animation config keys (`alertEnter`, and the rest) in `AvonicConfig` | One per overlay; added with each | all |
| `progressStart`, `progressStep`, `progressEnd` | For gestures; the timing model keeps a current time, so they add without a rewrite | menu, sheet modal, toast swipe |
| Keyframe `easing`, `keyframes()` while running, `getWebAnimations`, other CSS properties (`top`, `left`, `transform-origin`, and the rest), styling `transform`, mismatched transform lists (which switch at the midpoint here, where CSS interpolates matrices) | Not in any md enter or leave animation but the popover's position, which its issue adds | popover |
