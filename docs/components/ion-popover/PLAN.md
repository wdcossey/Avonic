# ion-popover — Plan

`AvonicPopover`: Ionic's popover on the overlay system ([#5](../../infra/overlays/README.md)), after the modal ([#42](../ion-modal/README.md)),
whose `IsOpen`, `Trigger`, inline placement and protected hooks it uses. One PR: the popover and its positioning, its trigger
actions, its keyboard navigation and nested popovers. The select (#45) presents one in code as its `popover` interface, with
itself as the event's target, so the positioning API is one code can drive: `Present(PopoverEvent)`.

Measured in real Ionic 9.0.11 in headless Chromium, md; the raw output is [`ref/measurements.txt`](ref/measurements.txt), the
reference render [`ref/`](ref/README.md).

## Fidelity checklist

`node tools/ionic-api.js ion-popover`.

### Props (23)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `alignment` | `Alignment` | `PopoverAlignment` | `Start` | `Start`, `Center`, `End`. Ionic's is `undefined` until `componentWillLoad` makes it `start` in md, before anything reads it; Avonic is md only, so it's `Start` from the start |
| [x] | `animated` | `Animated` | `bool` | `true` | The overlay's |
| [ ] | `arrow` | none | | | Not applicable: "Does not apply in md mode". md's `.popover-arrow` has no style, so it's an empty box, and `md.enter.js` passes an arrow of 0 by 0. Declared, it would do nothing (rule 3) |
| [x] | `backdropDismiss` | `BackdropDismiss` | `bool` | `true` | The backdrop's `Tappable`; Escape and back (`BackdropDismissEnabled`) |
| [ ] | `component` | none | | | Not applicable: for an app without a framework. `Content` is the slot |
| [ ] | `componentProps` | none | | | Not applicable, with `component` |
| [x] | `dismissOnSelect` | `DismissOnSelect` | `bool` | `false` | A click in the content dismisses it |
| [x] | `enterAnimation` | `EnterAnimation` | `AnimationBuilder?` | | The overlay's |
| [x] | `event` | `Event` | `PopoverEvent?` | | What `Present()` positions by when it's given none: a target (`event.target`, or `detail.ionShadowTarget`) and a point (`clientX`, `clientY`) |
| [x] | `focusTrap` | `FocusTrap` | `bool` | `true` | `FocusTrapEnabled` |
| [ ] | `htmlAttributes` | none | | | Not applicable: HTML attributes |
| [x] | `isOpen` | `IsOpen` | `bool` | `false` | On `AvonicOverlay` |
| [x] | `keepContentsMounted` | `KeepContentsMounted` | `bool` | `false` | As the modal's: mounted is in the visual tree |
| [x] | `keyboardClose` | `KeyboardClose` | `bool` | `true` | The overlay's |
| [x] | `leaveAnimation` | `LeaveAnimation` | `AnimationBuilder?` | | The overlay's |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |
| [x] | `reference` | `Reference` | `PopoverReference` | `Trigger` | `Event`, `Trigger` |
| [x] | `showBackdrop` | `ShowBackdrop` | `bool` | `true` | The backdrop's `Visible` |
| [x] | `side` | `Side` | `PopoverSide` | `Bottom` | `Top`, `Right`, `Bottom`, `Left`, `Start`, `End` |
| [x] | `size` | `Size` | `PopoverSize` | `Auto` | `Auto`, `Cover` |
| [ ] | `translucent` | none | | | Not applicable: ios only (`popover-translucent` has rules only in `popover.ios.css`) |
| [x] | `trigger` | `Trigger` | `string?` | | On `AvonicOverlay`; the popover adds its own listeners for each `TriggerAction` |
| [x] | `triggerAction` | `TriggerAction` | `PopoverTriggerAction` | `Click` | `Click`, `Hover`, `ContextMenu` |

`hasController`, `overlayIndex`, `delegate`, `cssClass` and `keyboardEvents` are `@internal`.

### Events (8)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `didDismiss` | `DidDismiss` | The overlay's generic event, after `IonPopoverDidDismiss` |
| [x] | `didPresent` | `DidPresent` | After `IonPopoverDidPresent` |
| [x] | `ionPopoverDidDismiss` | `IonPopoverDidDismiss` | `OverlayEventArgs` |
| [x] | `ionPopoverDidPresent` | `IonPopoverDidPresent` | |
| [x] | `ionPopoverWillDismiss` | `IonPopoverWillDismiss` | `OverlayEventArgs` |
| [x] | `ionPopoverWillPresent` | `IonPopoverWillPresent` | |
| [x] | `willDismiss` | `WillDismiss` | The overlay's |
| [x] | `willPresent` | `WillPresent` | The overlay's |

### Methods (4)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `dismiss(data?, role?, dismissParentPopover?)` | `Dismiss(data, role)`, `Dismiss(data, role, dismissParentPopover)` | `Task<bool>`; the two-argument one is the overlay's, with `dismissParentPopover` true |
| [x] | `onDidDismiss()` | `OnDidDismiss()` | The overlay's |
| [x] | `onWillDismiss()` | `OnWillDismiss()` | The overlay's |
| [x] | `present(event?)` | `Present()`, `Present(PopoverEvent?)` | And `Present(TopLevel)`, the overlay's extension |

`getParentPopover()` is `@internal`.

### Slots, parts

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | default slot | `Content` (+ `ContentTemplate`) | Ionic wraps an inline popover's children in `div.popover-viewport`; here the content presenter in the box's `ScrollViewer` (`PART_Scroll`), with the safe areas 0 inside |
| [ ] | `arrow` | none | ios only: see `arrow` |
| [x] | `backdrop` | `PART_Backdrop` | An `AvonicBackdrop`, not in a nested popover |
| [x] | `content` | `PART_Content` | A `Border`, the `.popover-content`, placed by `PART_Wrapper`, a `PopoverPanel` (`.popover-wrapper`) |

### CSS custom properties (11)

Each is a key in `Tokens/Popover.axaml`. A size takes a number (px) or a CSS string, read as the modal's are (`ModalPanel.Length`):
a percentage of the window, `250px`, `auto`, `none` for a maximum.

| ✓ | Ionic | Avonic resource key | md default |
|---|---|---|---|
| [x] | `--backdrop-opacity` | `Avonic.Popover.BackdropOpacity` | unset, falling back to `Avonic.BackdropOpacity` (`--ion-backdrop-opacity`), then 0.32; read by the animations |
| [x] | `--background` | `Avonic.Popover.Background` | unset, falling back to `Avonic.BackgroundColorBrush` |
| [x] | `--box-shadow` | `Avonic.Popover.BoxShadow` | `0 5px 5px -3px rgba(0, 0, 0, 0.2), 0 8px 10px 1px rgba(0, 0, 0, 0.14), 0 3px 14px 2px rgba(0, 0, 0, 0.12)` |
| [x] | `--height` | `Avonic.Popover.Height` | `auto` |
| [x] | `--max-height` | `Avonic.Popover.MaxHeight` | `90%` |
| [x] | `--max-width` | `Avonic.Popover.MaxWidth` | `auto` (none) |
| [x] | `--min-height` | `Avonic.Popover.MinHeight` | 0 |
| [x] | `--min-width` | `Avonic.Popover.MinWidth` | 0 |
| [x] | `--offset-x` | `Avonic.Popover.OffsetX` | unset: `Avonic.Popover.Md.OffsetX` 0, and `Avonic.Popover.Md.NestedOffsetX` 5 on a nested popover, signed by its side (below) |
| [x] | `--offset-y` | `Avonic.Popover.OffsetY` | 0 |
| [x] | `--width` | `Avonic.Popover.Width` | 250 |

`border-radius: 4px` has no variable: a literal in the theme, so it can be restyled.

## What Ionic does

From `popover.js`, `utils.js`, `animations/md.enter.js`, `md.leave.js`, `popover.md.css`, `utils/overlays.js`,
`utils/framework-delegate.js` and `utils/focus-trap.js`.

- **Load** (`componentWillLoad`): `parentPopover` is the closest `ion-popover` around it in the document, once; `alignment`
  undefined becomes `start` in md. `componentDidLoad`: `isOpen` presents at the next frame; a nested popover listens to its
  parent's `ionPopoverWillDismiss` and dismisses itself with no role and `dismissParentPopover` false; the trigger is set up.
- **Trigger** (`configureTriggerInteraction`, on connect, load, and a change of `trigger` or `triggerAction`): the element with
  the id, marked `data-ion-popover-trigger`, a warning if there's none. `click`: a click presents it, with the click as the event
  (no `stopPropagation`, so a second trigger's click still dismisses the first popover through its backdrop). `hover`:
  `mouseenter` presents it after a 100 ms timeout and a frame, `mouseenter` again restarts the wait; `mouseleave` cancels it, and
  if the element entered isn't in this popover (`closest('ion-popover')`), dismisses it with no role and `dismissParentPopover`
  false; its click stops there. `context-menu`: `contextmenu` presents it, with `preventDefault`; its click stops there. Each
  also presents on `ionPopoverActivateTrigger`, focusing the first focusable element in it after.
- **Present** (`present(event)`): under the lock; nothing if presented. The host takes the direction it has in the page
  (`dir`), unless it has its own, which goes after the dismiss. The content is attached: an inline popover's children are
  wrapped in `div.ion-delegate-host.popover-viewport` and the popover is teleported to `ion-app`. The keyboard and the dismiss
  interactions are set up (below). Then the shared `present()` with the options `{ event: event || this.event, size, trigger:
  triggerEl, reference, side, align }`, and focus on the first focusable element if it came from `ionPopoverActivateTrigger`.
- **Dismiss** (`dismiss(data, role, dismissParentPopover = true)`): under the lock, the parent popover is dismissed first with
  the same three, not awaited; then the shared `dismiss()` (`ionPopoverWillDismiss`, `willDismiss`, the leave animation,
  `ionPopoverDidDismiss`, `didDismiss`, focus back). After it, the keyboard and dismiss interactions go, the content is detached
  and the direction removed if it was set at present.
- **Positioning** (`md.enter.js`, `getPopoverPosition`, `calculateWindowAdjustment`): the content's box is read as it lays
  out, before any position: 250 wide by its own height up to 90%. With `size="cover"` the width is the reference element's
  (`trigger`, else `event.detail.ionShadowTarget`, else `event.target`). The reference is the trigger's box, or that element's;
  with `reference="event"` it's the event's point, 1 by 1; with neither, the popover is centred and its origin `left top`
  (`right top` in RTL). Then:
  - **Side**: `top` above (`top - height`), `bottom` below (`top + height of the reference`), both at the reference's left;
    `left` and `right` beside it at its top; `start` and `end` are `left` and `right`, swapped in RTL.
  - **Alignment**: `start` adds nothing; `center` centres it along the reference; `end` lines up the far edges.
  - **Origin**: the side's opposite edge, and `left`, `center` or `right` (top/bottom sides), or `top`, `center` or `bottom`
    (the others) for the alignment.
  - **Window**: md's body padding is 12 (0 with `cover`), and the safe area is `--ion-safe-area-*` (0 with `cover`). Left of
    `12 + safe left`: it moves to 12 (`+ safe left` in CSS), origin x `left`; else past the right edge: to `width - content - 12`
    (`- safe right`), origin x `right`. On a `top` or `bottom` side, if the reference's bottom plus its height plus the content
    passes `height - safe bottom`: it flips above (`reference bottom - content - reference height + 1`, as md's arrow is 0 and
    Ionic takes `arrowHeight - 1`) if that's at least `safe top + 12`, origin y `bottom` and the host class `popover-bottom`;
    otherwise `bottom: 12px + safe bottom`, so the box stretches from its top to there, and if its top is past that bottom edge it
    moves up to `safe top + 12`. Measured: it ignores the side for the horizontal clamp, so `side="left"` near the left edge is
    pushed right, over its reference.
  - The content gets `top: calc(T + --offset-y)`, `left: calc(L + --offset-x)`, maybe `bottom`, and `transform-origin`, as
    `beforeStyles`; `--width` (cover) and `popover-bottom` as the base animation's `beforeAddWrite`. The leave's `afterAddWrite`
    removes them all.
- **md enter**: 300 ms, `cubic-bezier(0.36,0.66,0.04,1)`; the backdrop 0.01 to `--backdrop-opacity`, `pointer-events: none`
  until it ends; `.popover-wrapper` 0.01 to 1 over its own 150 ms (on the same curve); the content `scale(0.8)` to `scale(1)` from
  its origin. Its fifth animation, `.popover-viewport` 0.01 to 1, looks in the shadow root, where the slotted viewport isn't, so it
  animates nothing (measured: the viewport is 1 throughout).
- **md leave**: 150 ms, `ease`; the backdrop to 0, the wrapper 0.99 to 0; the content isn't scaled.
- **Dismiss on select**: `.popover-wrapper`'s `onClick` dismisses with no data and no role.
- **Dismiss interaction** (`configureDismissInteraction`, a nested popover with a trigger, while it's presented), on the
  parent's `.popover-content`: for a `hover` trigger, `mouseenter` dismisses it (no role, not the parent) unless the element at
  the point is the trigger; for `click` and `context-menu`, a click dismisses it the same way, unless the click is on its trigger
  (`closest('[data-ion-popover-trigger]')`), when it stops the click, so the parent's `dismissOnSelect` doesn't dismiss the
  parent. Set up inside `present()`, which runs in the microtasks after the trigger's own `click` listener, before the click
  reaches the parent's content: so even the click that presents it stops there (measured: a click trigger in a
  `dismiss-on-select` parent opens the child and leaves the parent open).
- **Keyboard** (`configureKeyboardInteraction`, while presented, unless `keyboardEvents`): a `keydown` on the host whose target
  is the popover or an `ion-item`. The items are the `ion-item`s in it, not in a popover inside it, not `disabled`. `ArrowDown`:
  the item after the focused one (the first, if the focused element isn't an item); `ArrowUp`: the one before (none from the
  host); `Home`, `End`: the first, the last; each focuses the item's native `button`, at the next frame, and prevents the default
  (scrolling). `ArrowRight`, Space, Enter: if the focused element is a popover trigger, `ionPopoverActivateTrigger` on it.
  `ArrowLeft`: a nested popover dismisses itself (no role, not the parent).
- **Nested** (`popover-nested`): no backdrop; the host doesn't take pointer events, the wrapper does, so the parent's backdrop
  still takes a click outside; `--offset-x` is 5 px on side `left` (`start` in LTR, `end` in RTL) and -5 on `right` (`end` in
  LTR, `start` in RTL).
- **The look** (`popover.md.css`): the host covers the window, `color: var(--ion-text-color)`, `z-index` 20000 + index;
  `.popover-wrapper` `opacity: 0` (the enter animation's), `z-index: 10`; `.popover-content` absolute, a flex column, `--width`
  by `--height` within the min and max, the background, the shadow, radius 4, `overflow: auto`; the viewport a flex column with
  the safe areas 0 inside. An `ion-content` in it sizes to its content (`readSizeToContent`), and shrinks to scroll in the box
  (`min-height: 0`); anything else taller than the box makes the box scroll.
- **Focus**: present focuses the host (it has no `[role="dialog"][tabindex]`), with `keyboardClose`; the focus trap is the
  shadow variant; `focusTrap` false turns it off.

## Measured

| | Case | Result |
|---|---|---|
| M1 | Enter, a 100 by 40 trigger at 145, 400 | At the call the wrapper 0.01, the content `scale(0.8)`; at 22 ms the backdrop 0.0443, the wrapper 0.2518, scale 0.82212; the wrapper 1 by 156 ms; the backdrop 0.32 and scale 1 at 305 ms, the backdrop's `pointer-events` back after. The viewport 1 throughout |
| M1b | Enter, `animated` false | The backdrop at 0.32 and the scale 1 at the first frame; the wrapper 0.2506 at 29 ms and 1 by 163, as animated |
| M2 | Leave | 150 ms, `ease`: the backdrop 0.2840 and the wrapper 0.8786 at 14 ms; 0 at 147; the content's `top` stays until the end |
| M3 | Events | `ionPopoverWillDismiss`, `willDismiss` at the call; `ionPopoverDidDismiss`, `didDismiss` at the end (164 ms) |
| M4 | Box | 250 wide, the content's height (120), radius 4, the shadow above, white, `overflow: auto`, `max-height: 90%`; a list of five button items 256 high; host z-index 20000 + index, text black |
| M5 | Positions | 108 cases in `measurements.txt`: every side and alignment at six places, cover, no event, tall content, the event's point, RTL, offsets. Each matches the maths above to the pixel |
| M6 | Keyboard | From the host, `ArrowDown` the first item; `End` the last enabled one (the disabled fourth skipped); `ArrowUp` at the first and `ArrowDown` at the last stay; `ArrowRight` on a trigger item opens its popover and focuses its first item; `ArrowLeft` there dismisses it alone, focus back on the trigger item; Escape then dismisses the parent with `backdrop`, focus back on the opener |
| M7 | Nested | No backdrop; at 123, 160 beside an item at 18, 160 (clamped right to 128, then `--offset-x` -5); Escape: the child with `backdrop`, then the parent; the parent's backdrop: the parent with `backdrop`, then the child with none; a click in the child (both `dismiss-on-select`): the child, then the parent, no role |
| M8 | Hover | Presented 108 ms after the pointer enters; leaving after 40 ms presents nothing; once it's open the pointer is over its backdrop, so leaving the trigger doesn't dismiss it |
| M9 | Context menu | A right click at 247.25, 41 with `reference="event"`: the content at 128, 42 (clamped right; the point's top plus 1) |

## Design

`AvonicPopover : AvonicOverlay`, in `Controls/Popover/`.

- **Template**: a `Panel` with `PART_Backdrop` (`Visible` from `ShowBackdrop`, `Tappable` from `BackdropDismiss`, hidden in a
  nested popover) and `PART_Wrapper`, a `PopoverPanel` (`.popover-wrapper`, opacity 0 in the theme) that places `PART_Content`,
  a `Border` with the background, shadow and radius; inside it a clip part to the radius and a `ScrollViewer` (`PART_Scroll`,
  `overflow: auto`) holding the content presenter.
- **`PopoverPanel`**: the content's width, height and their limits in CSS's terms, as `ModalPanel` reads them; placed at the
  `left`, `top` and maybe `bottom` the popover gives it, in the top level's coordinates, plus `OffsetX` and `OffsetY`; with no
  position, centred, as an absolute box with no inset sits where the host's flex box centres it. A mirrored (RTL) popover gets
  the same physical place.
- **The viewport**: `ion-content` alone has `min-height: 0` in the viewport's flex column, so it shrinks to the box and scrolls
  itself; anything else is as tall as it is, and the box (`overflow: auto`) scrolls it. So the scroller doesn't scroll vertically
  when the content is an `AvonicContent`, and does otherwise. The safe areas are 0 inside (`Avonic.SafeArea*` in a dictionary the
  popover merges), and read from the overlay host for the position.
- **Positioning**: `PopoverPosition`, internal and pure: `getPopoverPosition` and `calculateWindowAdjustment` line for line, so
  tests check it against the 108 measured cases. The md enter builder reads the boxes after a layout pass (the forced reflow of
  `getBoundingClientRect`) in the top level's coordinates, and gives the panel the result in a `BeforeAddWrite`, as Ionic's
  `beforeStyles`; the leave's `AfterAddWrite` takes it away.
- **`PopoverEvent`**: Ionic's `event` is any DOM event; what the popover reads of it is a target and a point. `PopoverEvent`
  holds both; `PopoverEvent.From(RoutedEventArgs)` takes the source, a breadcrumb's `IonShadowTarget`, and a pointer's, tap's or
  context request's position. The select (#45) will present with `new PopoverEvent(itself)`.
- **Trigger actions**: `AvonicOverlay` gets one virtual, the listeners a trigger gets (a click by default), which the popover
  overrides for its three actions; and the trigger element, and a call to set it up again (for `TriggerAction`). A DOM `click`
  here is `Tapped`, a stock `Button`'s `Click` and an Avonic native button's click from a key; `stopPropagation` of one is a mark
  the popover's own listeners read, so it doesn't depend on `Handled`, which Avalonia's controls set more freely than the DOM
  stops a click. `hover` is `PointerEntered`, `AvonicConfig.Schedule` for the 100 ms and `RequestFrame`; `mouseleave`'s related
  target is what the top level's hit test finds at the pointer. `context-menu` is `ContextRequested` (a right click, a long
  press, the menu key), handled.
- **`dismissParentPopover`** and the present's event are per call, as Ionic's arguments are, carried to `OnDismissing` and
  `OnPresenting` through an `AsyncLocal` of the popover's, so the base's `Dismiss(data, role)`, which Escape, the backdrop and
  `IsOpen` call, keeps Ionic's default.
- **Nested**: the parent is the nearest `AvonicPopover` among the logical ancestors at the first attach to the logical tree
  (`componentWillLoad`), which for one in another's `Content` is before either is shown. The child listens to its parent's
  `IonPopoverWillDismiss`. A click on a nested popover's trigger stops at the parent's content, whether the child was open or
  not, as M7's microtasks make it. Ionic's `dismiss()` awaits its lock before its events, so the parent's dismiss from the
  child's, and the child's from the parent's will-dismiss, are posted, to come after the events already on their way (M7's
  order). The trigger is looked up through a presented overlay's placeholder (a change to `OverlayTrigger`), as the parent's
  teleport moves the child's place out of the page's name scope, where `getElementById` sees the whole document.
- **Not animated**: `animation.duration(0)` sets the parent's duration; the wrapper's own 150 ms stays, as a child with its own
  duration keeps it (`getDuration`), so a popover with `animated` false still fades in over 150 ms (M1b). `AvonicAnimation`
  does the same.
- **Keyboard**: a tunnel `KeyDown` handler on the popover while it's presented, for a key whose source is the popover or an
  `AvonicItem`, so the item's own handlers and a `ScrollViewer`'s arrows come after it, as a `preventDefault` beats the
  browser's scrolling. The items are the enabled `AvonicItem`s in the popover's visual tree, not in another popover's; focusing
  one is focusing a button item at the next frame (`AvonicConfig.RequestFrame`), with `NavigationMethod.Directional` so it shows
  the focus ring.
- **Direction**: at present the popover takes its place's `FlowDirection` at style priority, unless it has one set, and gives
  it back after the dismiss.

### Not done, with why

| Not here | Why |
|---|---|
| `arrow`, `translucent`, `PART_Arrow` | ios only |
| The page lifecycle (`onLifecycle`: `ionViewWillEnter` and the rest on the user's element) | `ion-nav`'s events (#130), as the modal records |
| `recalculateContentOnHeaderReady` | Recalculates an `ion-content`'s offsets once its header has a height, for the browser's layout; Avalonia lays out the content again whenever the header's size changes |
| `ionMount`, `keyboardEvents` | `@internal`, for a framework's delegate and `ion-select-popover`, which #45 can add |
| `popover-desktop`, `popover-bottom` host classes | No md rule reads them |
| Root `aria-hidden`, `backdrop-no-scroll`, a controller | The overlay system's deferrals |
| CSS `zoom` (`getElementCSSZoom`) | Avalonia has no CSS zoom; a scaled top level's coordinates are already its own |

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| B1 | md enter on the clock: the backdrop 0.01 to 0.32 and not hit-testable until it ends, the wrapper 0.01 to 1 over 150 ms, the content 0.8 to 1 from its origin, over 300 ms on `cubic-bezier(0.36,0.66,0.04,1)` | `md.enter.js`, M1 |
| B2 | md leave: 150 ms `ease`, the backdrop to 0 and the wrapper 0.99 to 0; the position and the origin gone after | `md.leave.js`, M2 |
| B3 | Events: `IonPopoverWillPresent` before `WillPresent` and the rest, with data and role; they bubble | M3 |
| B4 | Positioning maths: every measured case, from `PopoverPosition` | `utils.js`, M5 |
| B5 | Placement on screen: the content where the maths puts it beside a trigger, at each edge, flipped, stretched to the bottom padding; origin per side and alignment | `md.enter.js`, M5 |
| B6 | `Reference`: the trigger's box, the event's target (or `IonShadowTarget`), or the event's point; none: centred | `getPopoverPosition`, M5, M9 |
| B7 | `Size` cover: as wide as the reference; auto: `Avonic.Popover.Width` | `getPopoverDimensions` |
| B8 | `--offset-x`, `--offset-y` move it; a nested popover's ±5 by side and direction; an app's key wins | `popover.md.css` |
| B9 | `TriggerAction`: click; hover after 100 ms and a frame, cancelled by leaving, dismissed by leaving to outside it; context menu at its point, handled; a change moves the listeners | `configureTriggerInteraction`, M8, M9 |
| B10 | `DismissOnSelect`: a tap or a key's click in the content dismisses it, with no role; not a click a hover or context-menu trigger stopped | `onClick`, M7 |
| B11 | Backdrop: `ShowBackdrop`, `BackdropDismiss` (tap, Escape, back); none in a nested popover | `render()` |
| B12 | Keyboard: arrows, Home, End over the enabled items, at the next frame, handled; not for another source; `ArrowRight` on a trigger presents its popover and focuses inside; `ArrowLeft` dismisses a nested one alone | `configureKeyboardInteraction`, M6 |
| B13 | Nested: the parent's will-dismiss dismisses the child; the child's dismiss dismisses the parent, unless `dismissParentPopover` is false; a click in the parent's content dismisses the child alone; a hover child is dismissed when the pointer enters the parent's content off the trigger; the click on its trigger never dismisses the parent | `componentDidLoad`, `dismiss`, `configureDismissInteraction`, M7 |
| B14 | Focus: the host at present; the first focusable after a keyboard activation; `FocusTrap` false lets it go; back after | `present`, `focusTrap` |
| B15 | `KeepContentsMounted`, as the modal's | `keepContentsMounted` |
| B16 | Direction: the page's `FlowDirection` at present; `Start` and `End` follow it | `dirSetOnPresent`, `isRTL` |
| B17 | `Present(PopoverEvent)` and `Event`: the argument wins, then the property | `present(event)` |
| B18 | The look: 250 wide, the content's height up to 90%, radius 4, the shadow, the background and text keys; long content scrolls in the box, an `AvonicContent` fits it | `popover.md.css`, M4 |

## Extensions

None. `Present(TopLevel)` is the overlay's.

## Dependencies

`AvonicOverlay`, `AvonicBackdrop`, `AvonicAnimation` (done); `AvonicItem` for the keyboard (done); `AvonicContent`, `AvonicList`
in the sample (done).
