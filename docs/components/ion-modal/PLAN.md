# ion-modal — Plan

`AvonicModal`: Ionic's modal, the first control built on the overlay system ([#5](../../infra/overlays/README.md)). It comes in
two PRs, as the issue allows:

1. **This one, the modal:** full screen on a phone and a centred dialog on a tablet, presented in code or inline (`IsOpen`,
   `Trigger`), `CanDismiss`, the focus trap, `KeepContentsMounted`, md's enter and leave animations, the events and methods.
2. **The sheet** ([#42](https://github.com/wdcossey/Avonic/issues/42), second PR): `breakpoints`, `initialBreakpoint`,
   `backdropBreakpoint`, `handle`, `handleBehavior`, `expandToScroll`, the drag and its snapping, `setCurrentBreakpoint()`,
   `getCurrentBreakpoint()`, `ionBreakpointDidChange` and the drag events, with `progressStart`, `progressStep` and `progressEnd`
   on `AvonicAnimation`. See [The sheet](#the-sheet) below; the first PR's sections are as they were.

Measured in real Ionic 9.0.11 in headless Chromium, md, in `ion-app`; the raw output is [`ref/measurements.txt`](ref/measurements.txt).

## Fidelity checklist

`node tools/ionic-api.js ion-modal`. **Sheet** marks a row for the second PR.

### Props (20)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `animated` | `Animated` | `bool` | `true` | The overlay's |
| [x] | `backdropBreakpoint` | `BackdropBreakpoint` | `double` | `0` | Sheet |
| [x] | `backdropDismiss` | `BackdropDismiss` | `bool` | `true` | The backdrop's `Tappable`; Escape and back (`BackdropDismissEnabled`) |
| [x] | `breakpoints` | `Breakpoints` | `AvaloniaList<double>?` | | Sheet: sorted into a copy, as Ionic sorts its array; `"0, 0.5, 1"` in XAML, as `StrokeDashArray` |
| [x] | `canDismiss` | `CanDismiss` | `ModalCanDismiss` | `true` | A `bool` or a `Func<object?, string?, Task<bool>>`, converting from either, and from `true`/`false` in XAML |
| [x] | `enterAnimation` | `EnterAnimation` | `AnimationBuilder?` | | The overlay's |
| [x] | `expandToScroll` | `ExpandToScroll` | `bool` | `true` | Sheet |
| [x] | `focusTrap` | `FocusTrap` | `bool` | `true` | `FocusTrapEnabled` |
| [x] | `handle` | `Handle` | `bool?` | | Sheet: null is Ionic's `undefined`, shown on a sheet |
| [x] | `handleBehavior` | `HandleBehavior` | `ModalHandleBehavior` | `Cycle` | Sheet: `None`, `Cycle` |
| [ ] | `htmlAttributes` | none | | | Not applicable: HTML attributes. An Avalonia app sets the control's properties, `Classes` and `AutomationProperties` directly |
| [x] | `initialBreakpoint` | `InitialBreakpoint` | `double?` | | Sheet |
| [x] | `isOpen` | `IsOpen` | `bool` | `false` | On `AvonicOverlay`, as every overlay has it |
| [x] | `keepContentsMounted` | `KeepContentsMounted` | `bool` | `false` | Mounted is in the visual tree: see the design |
| [x] | `keyboardClose` | `KeyboardClose` | `bool` | `true` | The overlay's |
| [x] | `leaveAnimation` | `LeaveAnimation` | `AnimationBuilder?` | | The overlay's |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |
| [ ] | `presentingElement` | none | | | Not applicable: ios only. md reads it nowhere: the card's animation, its swipe to close, its status bar and its safe area are all behind `mode === 'ios'` |
| [x] | `showBackdrop` | `ShowBackdrop` | `bool` | `true` | The backdrop's `Visible` |
| [x] | `trigger` | `Trigger` | `string?` | | On `AvonicOverlay`: the name of a control in the overlay's name scope, whose click presents it |

`hasController`, `overlayIndex` and `delegate` are `@internal`.

### Events (12)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `didDismiss` | `DidDismiss` | The overlay's generic event, raised after `IonModalDidDismiss` |
| [x] | `didPresent` | `DidPresent` | The same, after `IonModalDidPresent` |
| [x] | `ionBreakpointDidChange` | `IonBreakpointDidChange` | Sheet: `ModalBreakpointChangeEventArgs.Breakpoint` |
| [x] | `ionDragEnd` | `IonDragEnd` | Sheet: `ModalDragEventArgs` with `SnapBreakpoint` and `IsDismissing` (ios's card: not applicable) |
| [x] | `ionDragMove` | `IonDragMove` | Sheet: `ModalDragEventArgs` (`CurrentY`, `DeltaY`, `VelocityY`, `Progress`, `SnapBreakpoint`) |
| [x] | `ionDragStart` | `IonDragStart` | Sheet: no detail |
| [x] | `ionModalDidDismiss` | `IonModalDidDismiss` | `OverlayEventArgs` |
| [x] | `ionModalDidPresent` | `IonModalDidPresent` | |
| [x] | `ionModalWillDismiss` | `IonModalWillDismiss` | `OverlayEventArgs` |
| [x] | `ionModalWillPresent` | `IonModalWillPresent` | |
| [x] | `willDismiss` | `WillDismiss` | The overlay's generic event |
| [x] | `willPresent` | `WillPresent` | The overlay's generic event |

All bubble, as Stencil's events do.

### Methods (6)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `dismiss(data?, role?)` | `Dismiss(data, role)` | `Task<bool>`; `CanDismiss` first |
| [x] | `getCurrentBreakpoint()` | `GetCurrentBreakpoint()` | Sheet: `double?`, synchronous, as it only reads a field |
| [x] | `onDidDismiss()` | `OnDidDismiss()` | The overlay's |
| [x] | `onWillDismiss()` | `OnWillDismiss()` | The overlay's |
| [x] | `present()` | `Present()` | And `Present(TopLevel)`, the overlay's extension |
| [x] | `setCurrentBreakpoint(breakpoint)` | `SetCurrentBreakpoint(breakpoint)` | Sheet: `Task`, completing when the move has |

### Slots, parts

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | default slot | `Content` (+ `ContentTemplate`) | One slot: the modal is its content's logical parent |
| [x] | `backdrop` | `PART_Backdrop` | An `AvonicBackdrop` |
| [x] | `content` | `PART_Content` | A `ModalWrapper`, the `.modal-wrapper` |
| [x] | `handle` | `PART_Handle` | Sheet: a `ModalHandle`, the `.modal-handle` button |

### CSS custom properties (12, and 2 more)

Each is a key in `Tokens/Modal.axaml`. Those whose md value changes on a tablet (the media queries below) are `CssUnset`, with
the defaults in `Avonic.Modal.Md.*` keys, so that an app's key wins at every size, as an app's `--width` on `ion-modal` beats
the shadow root's `:host` rule inside a media query.

| ✓ | Ionic | Avonic resource key | md default |
|---|---|---|---|
| [x] | `--backdrop-opacity` | `Avonic.Modal.BackdropOpacity` | unset, falling back to `Avonic.BackdropOpacity` (`--ion-backdrop-opacity`, 0.32); read by the animations |
| [x] | `--background` | `Avonic.Modal.Background` | unset, falling back to `Avonic.BackgroundColorBrush` |
| [x] | `--border-color` | `Avonic.Modal.BorderColor` | transparent |
| [x] | `--border-radius` | `Avonic.Modal.BorderRadius` | unset: `Avonic.Modal.Md.BorderRadius` 0, `Avonic.Modal.Md.BorderRadiusInset` 2 |
| [x] | `--border-style` | `Avonic.Modal.BorderStyle` | `none`: a string, `none` or `hidden` drawing no border, any other a solid one |
| [x] | `--border-width` | `Avonic.Modal.BorderWidth` | 0 |
| [x] | `--height` | `Avonic.Modal.Height` | unset: `100%`; 500 at `:inset-small`, 600 at `:inset-large` |
| [x] | `--max-height` | `Avonic.Modal.MaxHeight` | `100%` |
| [x] | `--max-width` | `Avonic.Modal.MaxWidth` | `auto` (none) |
| [x] | `--min-height` | `Avonic.Modal.MinHeight` | `auto` |
| [x] | `--min-width` | `Avonic.Modal.MinWidth` | `auto` |
| [x] | `--width` | `Avonic.Modal.Width` | unset: `100%`; 600 at `:inset-small` |
| [x] | `--box-shadow` (not in `core.json`) | `Avonic.Modal.BoxShadow` | unset: `none`; `0 28px 48px rgba(0, 0, 0, 0.4)` at `:inset-small` |
| [ ] | `--overflow` (not in `core.json`) | none | `hidden`, which the wrapper's clip part does. An Avalonia `ClipToBounds` is a bool, and the modal's content is laid out in its box, so `visible` would draw nothing different that an app could want |

A size key takes a number (px) or a CSS string: `100%`, `600px`, `auto`, `fit-content`, `max-content`, `min-content` (all four
content sizes), `none` for a maximum.

## What Ionic does

From `modal.js`, `utils/overlays.js`, `utils/framework-delegate.js`, `animations/md.enter.js`, `md.leave.js` and `modal.md.css`.

- **Present** (`present()`): under the lock; nothing if presented. Reset the current breakpoint (sheet); `ionMount`; attach the
  content (`attachComponent`): an **inline** modal, one in the page with no controller, is **teleported** to `ion-app`, a comment
  left in its place. Then `show-modal`, the safe-area overrides, and the shared `present()`: blur the focused element to give it
  back later; `ionModalWillPresent`, `willPresent`; the enter animation; `ionModalDidPresent`, `didPresent`; with `keyboardClose`
  and focus not already inside, focus the first `[role="dialog"][tabindex]` in the shadow root, which is the wrapper, with
  `preventScroll`. Then the safe-area overrides by position, the fullscreen content padding, the sheet's gesture (sheet), the
  parent-removal observer.
- **Dismiss** (`dismiss(data, role)`): not while a sheet's gesture is dismissing it, unless the role is `gesture`. Under the
  lock: dismiss every modal nested in this one's content (role `parent-dismissed`, not awaited); unless the role is `handler`,
  `canDismiss` (a boolean, or a function of `data` and `role` that's awaited) can stop it: `false` is returned and nothing
  happens. Then the shared `dismiss()`: `pointer-events: none`, `ionModalWillDismiss`, `willDismiss`, the leave animation (not
  for `gesture`), `ionModalDidDismiss`, `didDismiss`, `overlay-hidden`, removed; an inline modal is put back where it was. Focus
  goes back to what had it, if nothing has it then.
- **Events**: the per-control event first, then the generic one, in the same task (M3).
- **`isOpen`**: `false` to `true` presents, `true` to `false` dismisses; `true` when it first loads presents at the next frame. A
  dismiss doesn't set it back (M6).
- **`trigger`**: a click on the element with that id presents it; looked up when it's connected, loaded and when `trigger`
  changes; a warning if none is found.
- **Nested modals** (`onSlotChange`, `dismissNestedModals`): a modal in another's content is tagged with its parent, and the
  parent's dismiss dismisses it with the role `parent-dismissed` (M7).
- **Parent removed** (`initParentRemovalObserver`): an inline modal whose original parent leaves the document while it's
  presented dismisses itself with the role `parent-removed` (M7).
- **Backdrop tap** (`onBackdropTap`): dismiss with `backdrop`, unless a sheet transition is running (sheet).
- **Focus**: the host is `tabindex="-1"` (0 for a sheet with a cycling handle, sheet); `focusTrap` false adds
  `ion-disable-focus-trap`, which turns the overlay's trap off; a focus on the host itself goes to a focusable handle (sheet).
- **Accessibility**: the wrapper has `role="dialog"`, `aria-modal="true"`, `tabindex="-1"`, and the host's `aria-label` and
  `role`, which it takes off the host (`inheritAttributes`).
- **Safe area** (`safe-area-utils.js`): `--ion-safe-area-*` on the host are `0px` for an edge the wrapper doesn't reach (within
  5 px), and `inherit` for one it does: predicted from the covered axes before the animation, read from the wrapper's box after
  it and on a resize (debounced 50 ms). A sheet: top, left and right 0, bottom inherit (sheet). With an `ion-content` and no
  `ion-footer` among its children or grandchildren, the content's scroll padding gets the bottom safe area
  (`--internal-content-safe-area-padding-bottom`).
- **The page lifecycle** (`onLifecycle`): `ionViewWillEnter`, `ionViewDidEnter`, `ionViewWillLeave` and `ionViewDidLeave` on the
  user's element, for a page inside a modal.
- **The look** (`modal.md.css`): the host covers the window (`inset: 0`), a flex box centring the wrapper, `color:
  var(--ion-text-color)`; the wrapper is `--width` by `--height` within the min and max, with the border, radius, background
  and shadow, `overflow: hidden`, `z-index: 10`, a flex column; `.modal-wrapper` rests at `translateY(40px)` and opacity 0.01,
  which the enter animation takes away. `@media (min-width: 768px) and (min-height: 600px)`: 600 by 500, radius 2, the shadow;
  `and (min-height: 768px)`: 600 by 600.
- **md enter**: 280 ms, `cubic-bezier(0.36, 0.66, 0.04, 1)`; the backdrop from 0.01 to `--backdrop-opacity`, `pointer-events:
  none` until it ends; the wrapper from opacity 0.01 and `translateY(40px)` to 1 and 0. **md leave**: 200 ms,
  `cubic-bezier(0.47, 0, 0.745, 0.715)`; the backdrop from `--backdrop-opacity` to 0; the wrapper from 0.99 and 0 to 0 and 40 px.

## Measured

| | Case | Result |
|---|---|---|
| M1 | Enter at 390 x 844 | 0.01 and 40 px at the call; then the backdrop 0.0468, 0.0921, 0.1508…, the wrapper 35.26, 29.40, 21.83… px; done (0.32, 1, 0) at 276.8 ms, `pointer-events` back then |
| M2 | Leave | 0.3176 / 0.9824 / 0.31 px at the first frame; 0 / 0.0001 / 39.99 at 199.7; back to the resting 0.01 and no transform when removed |
| M3 | Events | `ionModalWillPresent`, `willPresent` at the call; `ionModalDidPresent`, `didPresent` at the end; the same for dismiss, with `{data, role}` |
| M4 | After present | Focus on the host, its shadow root's on `.modal-wrapper`; host `tabindex` -1; wrapper `role="dialog"`, `aria-modal="true"`, `tabindex` -1; z-index 20000 + index |
| M5 | Box and look | 390 x 844: the wrapper 390 x 844 at 0, 0, no radius, no shadow, white, `overflow: hidden`, text black, every safe area `inherit`. 1024 x 768: 600 x 600 at 212, 84, radius 2, `0 28px 48px rgba(0,0,0,0.4)`, every safe area `0px`. 1024 x 700: 600 x 500. 767 x 1000 and 800 x 599: full screen |
| M6 | `canDismiss` | `false`: `dismiss()` false. A function: called with `(data, role)`, false keeps it; role `handler` dismisses without calling it. `isOpen` stays true after a dismiss; set false then true presents again |
| M7 | Inline | A trigger click presents; Escape dismisses with `backdrop`. In a page `div`, it moves to `ion-app` with a comment in its place, and back after. A modal inside it is tagged `data-parent-ion-modal` and dismissed with `parent-dismissed`; removing the page `div` while it's presented dismisses it with `parent-removed` |
| M8 | Focus | Focused button, present: focus on the modal; dismiss: back on the button |

## Design

`AvonicModal : AvonicOverlay`, in `Controls/Modal/`.

### What goes in `AvonicOverlay`

Every overlay in `core.json` has `isOpen` and `trigger`, and every one can be inline, so they go in the base, as its README
says what every overlay has does. The base also gets protected hooks the modal needs, which the select's alert, action sheet and
popover (#45) will use the same way. Listed in the PR under "shared files":

- `IsOpen`, `Trigger` (Ionic's `createTriggerController`, in `Overlays/OverlayTrigger.cs`): the trigger is looked up by name in
  the overlay's name scope, as `getElementById` in the document, when the overlay is attached to the logical tree and when
  `Trigger` changes, with a warning in the log if there's none. Its click is its own `Click` routed event, any type's (found in
  `RoutedEventRegistry` by name: a stock `Button`'s, an `AvonicButton`'s, a chip's…), or `Tapped` for a control with none.
- **Inline overlays** (`CoreDelegate`, in `Overlays/InlinePlacement.cs`): an overlay with a parent when it's presented is
  teleported: an invisible placeholder takes its place in its parent (a `Panel`'s `Children`, a `Decorator`'s `Child`, a
  `ContentControl`'s or `ContentPresenter`'s `Content`), and it goes back there after its dismiss. Anything else throws, saying
  so. `Present()` uses the overlay's own top level, as Ionic uses its document's `ion-app`. Its `DataContext` stays the page's,
  through a style-priority binding to the placeholder's, so its bindings keep working as a framework's do across Ionic's
  teleport; a local `DataContext` wins. A placeholder that leaves the visual tree while the overlay is presented dismisses it
  with `parent-removed`, as the modal's `MutationObserver` does. (Ionic only does that for the modal; it's done here for all, as
  the placement is shared, and recorded.)
- Hooks: the per-control events (`ControlWillPresentEvent` and the rest, raised before the generic ones); `OnPresenting()`
  (under the lock, once it's in the host), `OnDismissing(data, role)` (under the lock; false
  stops the dismiss), `OnDismissed(bool)` (after removal, under the lock); `PresentFocusTarget`, Ionic's `[role="dialog"][tabindex]`.

### The modal

- **Template**: a `Panel` with `PART_Backdrop` (`Visible` from `ShowBackdrop`, `Tappable` from `BackdropDismiss`) and a
  `ModalPanel`, which lays out `PART_Content` as the host's flex box does: `ContentWidth`, `ContentHeight` and their minimums and
  maximums in CSS's terms (a percentage of the host, px, or a content size), centred, clamped `max` then `min` as CSS does. The
  wrapper is `ModalWrapper : Border`, with the background, border, radius and shadow; inside it a clip part (`overflow: hidden`
  beside `box-shadow`) holding the content presenter.
- **`ModalWrapper`**: focusable, not a tab stop, `FocusAdorner` null (`outline: none`); its peer is a `Window`, Avalonia's
  nearest control type to `role="dialog"`, named from the host's `AutomationProperties.Name`, as Ionic moves `aria-label` there.
- **Media queries**: `:inset-small` (the top level's client size at least 768 by 600) and `:inset-large` (768 by 768), named
  after Ionic's `$modal-inset-*` SCSS variables, set while it's attached and when the size changes.
- **`CanDismiss`**: `ModalCanDismiss`, a struct holding a `bool` or a function, with implicit conversions and a type converter
  from `True`/`False`, as Ionic's union has no name. `OnDismissing` returns false when it says no, unless the role is `handler`.
- **Nested modals**: when an inline modal is teleported, it records the nearest `AvonicModal` among its logical ancestors. A
  modal's `OnDismissing` dismisses every presented modal that recorded it, with `parent-dismissed`, not awaited.
- **`KeepContentsMounted`**: Ionic's frameworks mount an inline modal's content when it's presented and unmount it after, unless
  `keepContentsMounted`. XAML builds the content when the page is built, so mounted means in the visual tree: the content
  presenter shows `Content` from `OnPresenting` to `OnDismissed`, and nothing otherwise. With `KeepContentsMounted`, it shows it
  from when the modal is attached (its template applied then), and keeps it. Ionic's own vanilla build always has it mounted;
  an app that wants that sets `KeepContentsMounted`.
- **Safe area**: after each layout of the wrapper, an edge it doesn't reach (5 px) gets `Avonic.SafeArea<Edge>` 0 in a
  dictionary the modal merges into its `Resources`, so the content reads 0 there, as Ionic's host style does; one it reaches
  inherits. Ionic predicts it before the animation and reads it after; the wrapper's layout box, untransformed, gives both.
- **Animations**: `md.enter.js` and `md.leave.js` on `AvonicAnimation`, the backdrop opacity read from the resource as the
  builder does `var(--backdrop-opacity)`.

### Not done, with why

| Not here | Why |
|---|---|
| The sheet | The second PR, [below](#the-sheet) |
| `--internal-content-safe-area-padding-bottom` on an `AvonicContent` in a full-screen modal with no footer | `AvonicContent` has no such padding; it's another control's file. Proposed in the PR. `Avonic.SafeAreaBottom` is 0 unless the app fills it |
| The page lifecycle (`ionViewWillEnter` and the rest on the user's element) | They're `ion-nav`'s events ([#130](https://github.com/wdcossey/Avonic/issues/130)), which defines them for its pages; nothing in Avonic raises or listens to them yet |
| `ionMount` | `@internal`: for a framework's delegate |
| The keyboard's `KEYBOARD_DID_OPEN` | Only the sheet's gesture listens to it: [the sheet](#the-sheet) does |
| Root `aria-hidden`, `backdrop-no-scroll` | The overlay system's deferral: there's no app root to hide (#65) |
| The router passthrough (`ion-page-overlay-passthrough`) | The router is out of scope |
| A `controller` (`modalController.create`) | The overlay system's deferral; `new AvonicModal { … }.Present()` is the same |
| `--border-style` other than none or solid | Avalonia's `Border` draws only solid lines; recorded |

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| B1 | md enter: the backdrop and wrapper at each frame, on the clock, `cubic-bezier(0.36,0.66,0.04,1)` over 280 ms; the backdrop not hit-testable until it ends | `md.enter.js`, M1 |
| B2 | md leave: 200 ms, `cubic-bezier(0.47,0,0.745,0.715)`, to 0 and 40 px; the resting values back after | `md.leave.js`, M2 |
| B3 | Events: `IonModalWillPresent` before `WillPresent`, `IonModalDidPresent` before `DidPresent`, and the dismiss pair with data and role | M3 |
| B4 | `CanDismiss` false: `Dismiss()` returns false and the modal stays; a function gets data and role, and its answer decides; `handler` skips it; Escape and the backdrop go through it | `checkCanDismiss`, M6 |
| B5 | `IsOpen`: true presents, false dismisses; true at load presents at the next frame; a dismiss doesn't clear it | `onIsOpenChange`, M6 |
| B6 | `Trigger`: a click on a named stock `Button`, an `AvonicButton`, or a tap on a `Border`, presents; a change of `Trigger` moves the listener; none found logs a warning | `createTriggerController`, M7 |
| B7 | Inline: presented in its own top level, in the overlay layer, with a placeholder in its place; back in place after; the page's `DataContext` kept | `CoreDelegate`, M7 |
| B8 | A nested inline modal is dismissed with `parent-dismissed` when its parent is | `dismissNestedModals`, M7 |
| B9 | The placeholder leaving the tree while it's presented dismisses it with `parent-removed` | `initParentRemovalObserver`, M7 |
| B10 | Backdrop tap dismisses with `backdrop`; `BackdropDismiss` false: no tap, Escape or back; `ShowBackdrop` false: transparent, still hit | `onBackdropTap`, render |
| B11 | Focus at present goes to the wrapper, not the host; back to the opener after | `present()` 8.8.19+, M4, M8 |
| B12 | `FocusTrap` false: focus can leave; true: it comes back to the first focusable | `FOCUS_TRAP_DISABLE_CLASS` |
| B13 | `KeepContentsMounted`: false, the content isn't in the visual tree before present or after dismiss; true, it is from attach on | `keepContentsMounted` |
| B14 | Layout: full screen at 390 x 844; 600 x 600 centred at 1024 x 768, 600 x 500 at 1024 x 700, radius 2 and the shadow; an app's `Avonic.Modal.Width` wins at every size; `%`, px and `auto` sizes | `modal.md.css`, M5 |
| B15 | Safe area: an app's `Avonic.SafeAreaTop` reaches the content of a full-screen modal, and is 0 in a centred one | `safe-area-utils.js`, M5 |
| B16 | The wrapper is a dialog to automation, named from the host | `role="dialog"`, `inheritAttributes` |
| B17 | The look: the wrapper's background, the text colour, border keys | `modal.md.css` |

## Dependencies

`AvonicOverlay`, `AvonicAnimation`, `AvonicBackdrop` (#5), all merged. The sheet will need `Gestures` (#4) and
`progressStart`/`progressStep`/`progressEnd` on `AvonicAnimation`, which the overlay system deferred to it.

---

# The sheet

The second PR of #42. A modal with `breakpoints` and `initialBreakpoint` is a sheet: it sits at the bottom, at a fraction of its
height, and the user drags it between those fractions, or down and away when 0 is one of them. From `modal.js`,
`gestures/sheet.js`, `gestures/utils.js`, `animations/sheet.js`, `md.enter.js`, `md.leave.js`, `ios.enter.js` (md's sheet gesture
uses it), `utils.js`, `safe-area-utils.js`, `utils/animation/animation.js` and `modal.md.css`. Measured in Ionic 9.0.11 in headless
Chromium, md, 390 x 844 and 1024 x 768, in `ion-app`; the raw output is [`ref/sheet-measurements.txt`](ref/sheet-measurements.txt).
`breakpoints [0, 0.25, 0.5, 0.75, 1]` and `initialBreakpoint 0.5` unless said.

## What Ionic does

- **A sheet** (`isSheetModal`): `breakpoints` and `initialBreakpoint` both set, read at load and again at each present. Its host
  has `modal-sheet`. `breakpoints` is sorted in place when it's set (`breakpointsChanged`). An `initialBreakpoint` that isn't in
  `breakpoints` logs a warning at load.
- **Layout** (`modal.md.css`): `:host(.modal-sheet) { --height: calc(100% - (var(--ion-modal-offset-top, 0px) + 10px)) }`, and
  the wrapper `position: absolute; bottom: 0`, centred across as the host's flex box would place it. `--ion-modal-offset-top` is the
  root's `--ion-safe-area-top`, which the modal sets on its host at present, on a resize and when the root's changes. An app's
  `--height` wins, as a `:host` rule loses to the page's. The tablet media query's 600 x 500 height loses to the sheet's.
- **Safe area**: a sheet's is fixed at present: top, left and right `0px`, bottom `inherit`.
- **The handle** (`render`, `.modal-handle`): a `<button part="handle">` first in the wrapper when `handle !== false` on a sheet,
  `aria-label="Activate to adjust the size of the dialog overlaying the screen"`, `tabindex` 0 when `handleBehavior` is `cycle`
  and -1 for `none`. 36 x 5, 5 px from the wrapper's top, centred, radius 8, `var(--ion-color-step-350,
  var(--ion-background-color-step-350, #c0c0be))`, `z-index: 11`, `cursor: pointer`; its `::before` adds a 4 px tap area around
  it. The host is `tabindex` 0 with a cycling handle, -1 otherwise.
- **Handle click** (`onHandleClick`, `moveToNextBreakpoint`): with `cycle`, and no sheet transition running, the next breakpoint
  that isn't 0, wrapping from the last to the first.
- **Present**: `currentBreakpoint` is reset to `initialBreakpoint`. md's enter is the sheet's (`createSheetEnterAnimation`), used
  whenever `initialBreakpoint` is set: 280 ms, `cubic-bezier(0.36,0.66,0.04,1)`, the wrapper from `translateY(100%)` to
  `translateY(100 - bp * 100 %)` at opacity 1; the backdrop from 0 to `--backdrop-opacity * bp` with `pointer-events: none` until
  it ends, when `backdropBreakpoint < bp`; otherwise to 0 with nothing on its `pointer-events`.
- **The gesture's animation** (`initSheetGesture`): after the present, `enterAnimation || iosEnterAnimation`, with the sheet's
  keyframes, the wrapper's inline `opacity: 1`, 500 ms and `cubic-bezier(0.32,0.72,0,1)`. Its keyframes become the full run
  (`translateY(0%)` to `100%`; the backdrop from `--backdrop-opacity` to 0.01, or to 0 at `1 - backdropBreakpoint` and on when
  that's set) and it's held at `progressStart(true, 1 - bp)`, linear. So at rest the backdrop is the linear point between those
  frames: 0.165 at 0.5, not 0.16 (S2).
- **Backdrop by breakpoint** (`enableBackdrop`, `disableBackdrop`): above `backdropBreakpoint`, with `focusTrap` and
  `showBackdrop` true, the host and backdrop take pointer events and the focus trap is on; otherwise the host and backdrop are
  `pointer-events: none` (the wrapper keeps `auto`, so only it takes input and the page behind the backdrop does too) and
  `ion-disable-focus-trap` is added. Checked at the gesture's creation and after each snap.
- **`expandToScroll`** (true): below the top breakpoint the content (`ion-content` in the modal) has `scrollY` false, so a drag on it
  moves the sheet; at the top breakpoint it's given back, at the release that snaps there. A press on content can start the
  gesture (`canStart`) only when it's scrolled to the top and has no refresher, if the sheet is at 1 or `expandToScroll` is false.
  False: `modal-no-expand-scroll`, the content keeps scrolling at every breakpoint, and the `.ion-page`'s `max-height` follows the
  visible part (`contentAnimation`, `(1 - offset) * 100%`); an upward drag inside the content doesn't move the sheet.
- **The drag** (`createGesture`): on the wrapper, `modalSheet`, priority 40, `y`, threshold 10. `onStart`: `canDismiss` blocks the
  gesture when it isn't `true` and 0 is a breakpoint; the content stops scrolling if the drag goes down; at the next frame the host
  is focused (it keeps focus, measured); `progressStart(true, 1 - bp)`; `ionDragStart`. `onMove`: `step = 1 - bp + deltaY /
  height` (the wrapper's height); past the second breakpoint with `canDismiss` blocking, `calculateSpringStep` slows it to at most
  0.95; clamped 0.0001 to 0.9999; `progressStep`; `ionDragMove` with `{ currentY, deltaY, velocityY, progress, snapBreakpoint }`,
  `progress` 0 at the lowest breakpoint and 1 at the highest. `onEnd`: the snap is the breakpoint nearest `bp - (deltaY + velocityY
  * 350) / height`, rounded to thousandths and clamped 0..1 (`FLICK_PROJECTION_MS`); `moveSheetToBreakpoint`; `ionDragEnd`, with
  `isDismissing`.
- **The snap** (`moveSheetToBreakpoint`): to the current breakpoint instead of 0 when `canDismiss` blocks; keyframes from the
  offset to the target (the backdrop by `getBackdropValueForSheet`), `progressStep(0)`, the gesture off,
  `progressEnd(1, 0, animated ? 500 : 0)`. At its end, if it stays open: at the next frame the full-run keyframes again,
  `progressStart(true, 1 - target)`, `currentBreakpoint`, `ionBreakpointDidChange` if it changed, the backdrop by breakpoint, the
  gesture on. At 0: `sheetOnDismiss`, which at the animation's end sets 0, raises `ionBreakpointDidChange` and dismisses with the
  role `gesture`, which plays no leave animation. Blocked by a `canDismiss` function: it's called with `(undefined, 'gesture')`, and
  `true` dismisses with `handler` once the snap back has finished.
- **`setCurrentBreakpoint(bp)`**: a warning and nothing if it isn't a sheet or `bp` isn't a breakpoint; nothing if it's the current
  one; otherwise the snap, with `animated`, from `1 - current`, kept as `sheetTransition` until it resolves.
  `getCurrentBreakpoint()` returns `currentBreakpoint`, undefined when not presented.
- **During a transition**: a backdrop tap does nothing; a `dismiss()` while the gesture is dismissing it returns false unless its
  role is `gesture`.
- **md leave** (`createSheetLeaveAnimation`): 200 ms, `cubic-bezier(0.47,0,0.745,0.715)`, the wrapper from `100 - bp * 100 %` to
  `100%`, the backdrop from `--backdrop-opacity * getBackdropValueForSheet(bp)` to 0 (at `backdropBreakpoint` and on, when set).
- **Dismissed**: the gesture's animation destroyed, the gesture destroyed, the content's `scrollY` given back (`resetContentScroll`).
- **Keyboard** (`KEYBOARD_DID_OPEN`): the gesture is turned off for a frame.
- **Focus** (`onModalFocus`): a focus on the host itself, from outside it, goes to a focusable handle.

## Measured

| | Case | Result |
|---|---|---|
| S1 | Box at 390 x 844 | The wrapper 390 x 834 at 0, 10 (`translateY(417px)` at 0.5); host `tabindex` 0; safe area top, left, right `0px`, bottom `inherit` |
| S1 | 1024 x 768 | 600 x 758 at x 212, radius 2, `0 28px 48px` shadow |
| S1 | `--ion-safe-area-top: 24px` on the root | `--ion-modal-offset-top: 24px`; the wrapper 810 high; the toolbar inside has no top padding |
| S2 | Enter at 0.5 | The wrapper 834 → 784.4, 723.8, 644.9 … 417 at 279; the backdrop 0 → 0.019, 0.042 … 0.160, `pointer-events: none`, then 0.165 and `auto` once the gesture holds it |
| S3 | Leave at 0.5 | 417 → 420.2, 429.9 … 833.9 at 203; the backdrop 0.16 → 0.1588 … 0 |
| S4 | Drag up 104 px from the header | The wrapper follows 1:1 (409, 401 … 313); the backdrop 0.168, 0.1709 …; `ionDragMove` `progress` 0.51, 0.519 … |
| S5 | Release there | The snap 0.75: from 313 to 208.5 over 500 ms with `cubic-bezier(0.32,0.72,0,1)`, the backdrop 0.1999 → 0.24 by `getBackdropValueForSheet`, then 0.2425 (the full run at 0.25); `ionBreakpointDidChange` 0.75 a frame after the end |
| S5 | Drag down 48 px slowly | Snaps back to 0.5 |
| S6 | Drag down 336 px | `snapBreakpoint` 0 from the first move; `ionDragEnd` `isDismissing: true`; to 834 over 500 ms; then `ionBreakpointDidChange` 0, `ionModalWillDismiss` and `ionModalDidDismiss` with `gesture`, no leave |
| S7 | The same with `canDismiss: false` | 1:1 to 609 (step 0.73), then the spring: 625.9, 629.5, 633.9 … 647.4; back to 417 over 500 ms; still presented |
| S8 | `setCurrentBreakpoint(1)` | 417 → 383.9, 347.1 … 0 at 504, the backdrop 0.16 → 0.32; the event at 520, then the promise. `0.6`: a warning, nothing. `0`: to 834, then the event and the dismiss with `gesture` |
| S9 | The handle | 36 x 5 at 177, 432 (5 below the wrapper), `rgb(192, 192, 190)`, radius 8, `tabindex` 0; hit from 4 px left of it to 4 px right, 4 above to 4 below; clicks: 0.75, 1, 0.25, 0.5, 0.75. `none`: `tabindex` -1, a click does nothing. `handle: false`: none, host `tabindex` -1 |
| S10 | `backdropBreakpoint 0.5` at 0.5 | The backdrop 0 through the enter, `pointer-events: none` on it and the host, `ion-disable-focus-trap`. To 0.75: 0.16 (0.32 x 0.5), `auto`, the class gone. To 0.25: 0, `none` |
| S11 | `expandToScroll` | True at 0.5: the content's `scrollY` false. False: `scrollY` true, `.ion-page` `max-height: 50%` (417 high) |
| S13 | Focus | After a drag from the header: the host itself, its shadow root's `activeElement` null, with a cycling handle or not |
| S16 | `initialBreakpoint` without `breakpoints` | Not a sheet (`modal-default`), no handle, but md's enter is the sheet's: the wrapper 844 high, held at `translateY(422px)`, the backdrop 0.16 |

## Design

On `AvonicModal`, with what the gesture needs in `Controls/Modal/`:

- **Properties**: `Breakpoints` (`AvaloniaList<double>?`, which XAML reads from `"0, 0.5, 1"`; a sorted copy is kept, as Ionic
  sorts its array in place), `InitialBreakpoint` (`double?`), `BackdropBreakpoint`, `Handle` (`bool?`), `HandleBehavior`
  (`ModalHandleBehavior`), `ExpandToScroll`. `:sheet` is `modal-sheet`.
- **Events and methods**: `IonBreakpointDidChange` (`ModalBreakpointChangeEventArgs`), `IonDragStart`, `IonDragMove`, `IonDragEnd`
  (`ModalDragEventArgs`), `SetCurrentBreakpoint(double)` (`Task`), `GetCurrentBreakpoint()` (`double?`).
- **`ModalSheet`** (internal): `createSheetGesture`'s closure as a class: the gesture, the animation, `moveSheetToBreakpoint`, the
  snap and progress functions, and the backdrop by breakpoint, line for line.
- **`ModalHandle`**: the `handle` part, a focusable control with `Behaviors/NativeButton` (a native `<button>`: a click on release,
  Enter and Space), its hit area 4 px wider each way (`::before`), not a tab stop with `None`, a `Button` to automation named as
  Ionic's `aria-label`.
- **Layout**: `ModalPanel.VerticalContentAlignment` (`Bottom` for `position: absolute; bottom: 0`), and `calc(a ± b)` in its sizes.
  `:sheet` sets the height from `Avonic.Modal.Height`, else `calc(100% - (offset + 10px))`, the offset being
  `Avonic.ModalOffsetTop`, which the modal keeps at the root's `Avonic.SafeAreaTop` (`--ion-modal-offset-top`), read from its
  overlay host, as the modal's own key is 0.
- **Backdrop by breakpoint**: `pointer-events: none` on the host and backdrop is the backdrop's `IsHitTestVisible` false (local, as
  an inline style): the host draws nothing and takes no input of its own, and the wrapper keeps taking it. The focus trap reads
  `FocusTrapEnabled`, false while the backdrop is off.
- **`AvonicAnimation`**: `ProgressStart(forceLinearEasing, step)`, `ProgressStep(step)`, `ProgressEnd(playTo, step, dur)`, and
  `Keyframes` on a running animation (`setKeyframes`), from `animation.js`; `max-height` as a keyframe property (a percentage of the
  parent's height), for `contentAnimation`, on the content presenter, Avonic's `.ion-page`.
- **The content**: `ContentUtils.FindClosestIonContent` from the press's source, its `ScrollY` and scroll offset, and its refresher.
  A drag up inside content that scrolls (the sheet at its top breakpoint, or `ExpandToScroll` false) can't move the sheet, and
  Ionic's passive gesture lets the browser scroll it; here the gesture lets go of it (`ReleaseAgainstDirection`, given as a
  condition read at the move, fully open as `canStart` checks 1), so the `ScrollViewer` scrolls, as the refresher does.

### Not done, with why

| Not here | Why |
|---|---|
| `swapFooterPosition` (`expandToScroll` false moves an `ion-footer` to the body while dragging) | It keeps the footer from shaking in a browser, whose layout lags the transform; Avalonia lays out and draws in the same frame, and the footer at the bottom of the content's max height is already where Ionic pins it |
| `setupChildRoutePassthrough` | The router is out of scope |
| ios's card (`presentingElement`, swipe to close) | ios only, as the first PR |

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| S1 | A sheet: `:sheet`, the wrapper at the bottom, `100% - 10` high, centred across; 600 x 758 on a tablet; the root's `Avonic.SafeAreaTop` taken off its height; an app's `Avonic.Modal.Height` wins; not a sheet without both props | `modal.md.css`, S1 |
| S2 | md's sheet enter frame by frame; the backdrop not hit-testable until it ends; then the gesture's hold (0.165) | `animations/sheet.js`, S2 |
| S3 | md's sheet leave frame by frame, from the current breakpoint | S3 |
| S4 | A drag follows the finger 1:1 on the wrapper's height, the backdrop linear; `IonDragStart`, `IonDragMove` with its detail | `onMove`, S4 |
| S5 | The release snaps to the projected breakpoint over 500 ms, `cubic-bezier(0.32,0.72,0,1)`; `IonDragEnd`; `IonBreakpointDidChange` a frame after; no drag during the snap | `onEnd`, `moveSheetToBreakpoint`, S5 |
| S6 | Down to 0: dismissed with `gesture`, no leave, `IonBreakpointDidChange` 0 first; a dismiss during it returns false | `sheetOnDismiss`, S6 |
| S7 | `CanDismiss` false: the spring, back to the breakpoint, still presented; a function is asked with `gesture`, and true dismisses with `handler` after the snap back | `handleCanDismiss`, S7 |
| S8 | `SetCurrentBreakpoint`: the move, the event, the task; nothing for the current one, one not in the list (a warning) or a modal that isn't a sheet; 0 dismisses; `Animated` false moves at once; `GetCurrentBreakpoint` | S8 |
| S9 | The handle: shown on a sheet unless `Handle` false; its box and colour; `Cycle` clicks and Enter go to the next non-zero breakpoint, wrapping, not during a move; `None` does nothing and isn't a tab stop; its name; its 4 px hit area | `render`, `onHandleClick`, S9 |
| S10 | `BackdropBreakpoint`: at or below it, the backdrop is 0 and lets clicks through to the page, and focus can leave; above it, it fades by `getBackdropValueForSheet` and blocks; `ShowBackdrop` or `FocusTrap` false never block | `enableBackdrop`, S10 |
| S11 | `ExpandToScroll`: the content doesn't scroll below the top breakpoint and does at it, and after a dismiss; false: it scrolls at every breakpoint, its height follows the sheet; a scrolled content doesn't start the drag | S11 |
| S12 | A backdrop tap during a move does nothing | `onBackdropTap` |
| S13 | Focus: the host is a tab stop with a cycling handle, and a focus on it from outside goes to the handle; a drag focuses the host | `onModalFocus`, S13 |
| S14 | Safe area: 0 at the top, left and right of a sheet, the app's at the bottom | `getInitialSafeAreaConfig` |
| S15 | Presented again, it starts at `InitialBreakpoint`; an `InitialBreakpoint` not in `Breakpoints` logs a warning | `present`, `componentWillLoad` |
| S16 | `InitialBreakpoint` alone: md's sheet enter, no handle, no gesture | S16 |
| A1–A6 | `AvonicAnimation`: `ProgressStart` holds the step, linear; `ProgressStep` moves it, clamped to 0..0.9999; `ProgressEnd(1)` plays from the step over the duration given with the easing back, `ProgressEnd(0)` plays back and finishes at step 0; `Keyframes` on a running animation keeps its time; the forced values are gone before `OnFinish` callbacks; children follow; `max-height` | `animation.js` |

## Real window

`tools/real-window/drive.ps1` gets a section for the sheet on the sample page: a drag between breakpoints with real input, the
handle clicked to cycle, and a drag down that closes it.
