# ion-modal — Plan

`AvonicModal`: Ionic's modal, the first control built on the overlay system ([#5](../../infra/overlays/README.md)). It comes in
two PRs, as the issue allows:

1. **This one, the modal:** full screen on a phone and a centred dialog on a tablet, presented in code or inline (`IsOpen`,
   `Trigger`), `CanDismiss`, the focus trap, `KeepContentsMounted`, md's enter and leave animations, the events and methods.
2. **The sheet**, on this branch: `breakpoints`, `initialBreakpoint`, `backdropBreakpoint`, `handle`, `handleBehavior`,
   `expandToScroll`, the drag and its snapping, `setCurrentBreakpoint()`, `getCurrentBreakpoint()`, `ionBreakpointDidChange` and
   the drag events. None of these is declared here (rule 3: no dead props).

Measured in real Ionic 9.0.11 in headless Chromium, md, in `ion-app`; the raw output is [`ref/measurements.txt`](ref/measurements.txt).

## Fidelity checklist

`node tools/ionic-api.js ion-modal`. **Sheet** marks a row for the second PR.

### Props (20)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `animated` | `Animated` | `bool` | `true` | The overlay's |
| [ ] | `backdropBreakpoint` | | | `0` | Sheet |
| [x] | `backdropDismiss` | `BackdropDismiss` | `bool` | `true` | The backdrop's `Tappable`; Escape and back (`BackdropDismissEnabled`) |
| [ ] | `breakpoints` | | | | Sheet |
| [x] | `canDismiss` | `CanDismiss` | `ModalCanDismiss` | `true` | A `bool` or a `Func<object?, string?, Task<bool>>`, converting from either, and from `true`/`false` in XAML |
| [x] | `enterAnimation` | `EnterAnimation` | `AnimationBuilder?` | | The overlay's |
| [ ] | `expandToScroll` | | | `true` | Sheet |
| [x] | `focusTrap` | `FocusTrap` | `bool` | `true` | `FocusTrapEnabled` |
| [ ] | `handle` | | | | Sheet |
| [ ] | `handleBehavior` | | | `'cycle'` | Sheet |
| [ ] | `htmlAttributes` | none | | | Not applicable: HTML attributes. An Avalonia app sets the control's properties, `Classes` and `AutomationProperties` directly |
| [ ] | `initialBreakpoint` | | | | Sheet |
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
| [ ] | `ionBreakpointDidChange` | | Sheet |
| [ ] | `ionDragEnd` | | Sheet (and ios's card, not applicable) |
| [ ] | `ionDragMove` | | Sheet |
| [ ] | `ionDragStart` | | Sheet |
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
| [ ] | `getCurrentBreakpoint()` | | Sheet |
| [x] | `onDidDismiss()` | `OnDidDismiss()` | The overlay's |
| [x] | `onWillDismiss()` | `OnWillDismiss()` | The overlay's |
| [x] | `present()` | `Present()` | And `Present(TopLevel)`, the overlay's extension |
| [ ] | `setCurrentBreakpoint(breakpoint)` | | Sheet |

### Slots, parts

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | default slot | `Content` (+ `ContentTemplate`) | One slot: the modal is its content's logical parent |
| [x] | `backdrop` | `PART_Backdrop` | An `AvonicBackdrop` |
| [x] | `content` | `PART_Content` | A `ModalWrapper`, the `.modal-wrapper` |
| [ ] | `handle` | | Sheet |

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
| The sheet | The second PR |
| `--internal-content-safe-area-padding-bottom` on an `AvonicContent` in a full-screen modal with no footer | `AvonicContent` has no such padding; it's another control's file. Proposed in the PR. `Avonic.SafeAreaBottom` is 0 unless the app fills it |
| The page lifecycle (`ionViewWillEnter` and the rest on the user's element) | They're `ion-nav`'s events ([#130](https://github.com/wdcossey/Avonic/issues/130)), which defines them for its pages; nothing in Avonic raises or listens to them yet |
| `ionMount` | `@internal`: for a framework's delegate |
| The keyboard's `KEYBOARD_DID_OPEN` | Only the sheet's gesture listens to it |
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
