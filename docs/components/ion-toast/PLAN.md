# ion-toast — Plan

`AvonicToast`: Ionic's toast, md, on the overlay system ([#5](../../infra/overlays/README.md)), its inline overlays (`IsOpen`,
`Trigger`, [#42](../ion-modal/README.md)) and the gesture utility ([#4](../../infra/gesture/README.md)) for `swipeGesture`. Its buttons
are plain data, `ToastButton`, as Ionic's are and as `AlertButton` and `ActionSheetButton` are, rendered as `ToastNativeButton`s.

Measured in real Ionic 9.0.11 in headless Chromium, md, in `ion-app`, on `render.js`'s phone (390 x 844 at 2x, mobile and touch,
Roboto from `tools/fonts/roboto/`) and at 1024 x 768; the raw output is [`ref/layout.json`](ref/layout.json) (every case's boxes,
colours, fonts and animations), [`ref/behaviour.json`](ref/behaviour.json) (events and their times, real keys, taps, hovers and
drags) and [`ref/extra.json`](ref/extra.json) (Tab, a tap through, a render while presented), from the script COMPARISON.md
describes, with a frame of each case in [`ref/`](ref/).

## How a toast differs from the other overlays

Read in `utils/overlays.js` and `toast.js`, and measured:

- **No backdrop, nothing blocked.** The host covers the window with `pointer-events: none`; only the wrapper takes input. A tap
  beside it reaches the page (measured: the button under the host clicked).
- **It doesn't steal focus.** `present()` skips `restoreElementFocus` for `ION-TOAST`: nothing is blurred at present and nothing is
  given back after (measured: the opener keeps focus). `keyboardClose` is `false` by default, unlike every other overlay; true,
  the host is focused after the enter, and nothing is given back (measured: `body`).
- **No focus trap.** `trapKeyboardFocus` reads `NON_TOAST_OVERLAYS`; focus moved into a toast over another overlay goes back to
  that overlay's last focus (measured over an alert). `AvonicOverlay.StealsFocus` false and `OverlayHost.IsInToast` already do this.
- **No `backdropDismiss`.** The toast doesn't declare it, so Escape and back never dismiss a toast. Ionic's Escape and
  `ionBackButton` listeners read the top overlay of every kind, toasts included, so a toast over an alert or a modal stops Escape
  and back from dismissing it too (measured: Escape over an alert with a toast on top did nothing). Avonic doesn't copy that (below).
- **No `aria-hidden` on the app root**, and no body scroll lock (`locksAppRoot`).
- **z-index 60000** + the overlay index, over every other overlay (alert 20000).
- **No controller here**, as for every overlay (the overlay system's deferral): `new AvonicToast { … }.Present()` is
  `toastController.create(…).present()`.

## Fidelity checklist

`node tools/ionic-api.js ion-toast`.

### Props (20)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `animated` | `Animated` | `bool` | `true` | The overlay's |
| [x] | `buttons` | `Buttons` | `IList<ToastButton>?` | empty | `ToastButton` converts from a `string`, Ionic's string form: `{ text }`, no role (measured: `"Cancel"` is a plain button) |
| [x] | `color` | `Color` | `string?` | | `ColorPalette.ColorProperty.AddOwner`; `:ion-color` |
| [x] | `cssClass` | `CssClass` | `string?` | | Style classes on the toast, space-separated (`AlertClasses`); Ionic's array form is the same string |
| [x] | `duration` | `Duration` | `double` (ms) | `AvonicConfig.ToastDuration`, 0 | Ionic's `config.getNumber('toastDuration', 0)` in the constructor: the config at style priority, so an app's value wins |
| [x] | `enterAnimation` | `EnterAnimation` | `AnimationBuilder?` | | The overlay's; given a `ToastPresentOptions` |
| [x] | `header` | `Header` | `string?` | | `.toast-header`, when not null |
| [x] | `htmlAttributes` | `HtmlAttributes` | `IReadOnlyDictionary<string, object?>?` | | Spread on the host: `id` is its `AutomationId`, `aria-label` its name. Others have nothing to do in Avalonia, and are ignored |
| [x] | `icon` | `Icon` | `string?` | | `.toast-icon`, an `AvonicIcon.Icon` (an ionicons name, or what it takes) |
| [x] | `isOpen` | `IsOpen` | `bool` | `false` | `AvonicOverlay`'s |
| [x] | `keyboardClose` | `KeyboardClose` | `bool` | **`false`** | The overlay's, its default overridden for the toast |
| [x] | `layout` | `Layout` | `ToastLayout` | `Baseline` | `Baseline`, `Stacked` |
| [x] | `leaveAnimation` | `LeaveAnimation` | `AnimationBuilder?` | | The overlay's |
| [x] | `message` | `Message` | `string?` | | `.toast-message`, when not null. Plain text: Ionic parses HTML only with `innerHTMLTemplatesEnabled`, off by default, which Avonic has no counterpart for (as the alert) |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |
| [x] | `position` | `Position` | `ToastPosition` | `Bottom` | `Top`, `Bottom`, `Middle` |
| [x] | `positionAnchor` | `PositionAnchor` | `object?` | | A `Control`, or the `Name` of one (Ionic's id), looked up at each present: in the toast's name scope, else the first control of that name in its top level, as `getElementById` searches the document. Ignored with `Middle`; a name not found is a warning and no anchor, as Ionic's |
| [x] | `swipeGesture` | `ToastSwipeGesture` | `ToastSwipeGestureDirection?` | null | `Vertical`. Renamed by the mapping rule, as `InputElement.SwipeGesture` is an event |
| [ ] | `translucent` | none | | `false` | Not applicable: it only adds `.toast-translucent`, which only `toast.ios.css` reads; md draws nothing different, so it would be a dead prop (rule 3), as for the alert and the action sheet |
| [x] | `trigger` | `Trigger` | `string?` | | `AvonicOverlay`'s |

`overlayIndex`, `delegate` and `hasController` are `@internal`.

### `ToastButton` (Ionic's interface)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `text` | `Text` | Uppercased by the theme (`text-transform`) |
| [x] | `icon` | `Icon` | Before the text, 19.6 px (`1.4em`); with no text, the button is icon-only: 36 x 36, round, an unbounded ripple |
| [x] | `side` | `Side` | `ToastButtonSide?`: `Start` goes in the start group, before the icon; anything else in the end group |
| [x] | `role` | `Role` | Any role is the dismiss's, and `:role-<role>` on the control (`toast-button-<role>`); `cancel` is coloured as one; `cancel` and `backdrop` (`isCancel`) dismiss at once, and are part `button cancel` (`:cancel`) |
| [x] | `htmlAttributes` | `HtmlAttributes` | Spread on the `<button>`: `aria-label` names it. Others are ignored |
| [x] | `handler` | `Handler` | `Func<object?>`: `false` keeps the toast open; a `Task` or `ValueTask` is awaited and its result read the same way (`OverlayHandler.Run`); an exception is logged and the toast dismisses, as Ionic's `printIonError` |

A toast button has no `data`, `cssClass` or `id` in Ionic's interface, so none here: its dismiss has no data (measured: `{role}`).

### Events (8)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionToastWillPresent` | `IonToastWillPresent` | Through `ControlWillPresentEvent`, before `WillPresent` |
| [x] | `ionToastDidPresent` | `IonToastDidPresent` | |
| [x] | `ionToastWillDismiss` | `IonToastWillDismiss` | `OverlayEventArgs` |
| [x] | `ionToastDidDismiss` | `IonToastDidDismiss` | `OverlayEventArgs` |
| [x] | `willPresent`, `didPresent`, `willDismiss`, `didDismiss` | the overlay's | |

### Methods (4)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `present()` | `Present()` | And `Present(TopLevel)` |
| [x] | `dismiss(data?, role?)` | `Dismiss(data, role)` | |
| [x] | `onDidDismiss()`, `onWillDismiss()` | the overlay's | `OverlayEventDetail(Data, Role)` |

### Slots (0)

None.

### Shadow parts (8)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `wrapper` | `PART_Wrapper` | `.toast-wrapper`: a `Border`, placed by `ToastPanel` (`PART_Panel`) |
| [x] | `container` | `PART_Container` | `.toast-container`: a `ToastContainer`, the flex row that wraps when stacked |
| [x] | `content` | `PART_Content` | `.toast-content`, the live region: a `ToastContent` |
| [x] | `header` | `PART_Header` | A `TextBlock` |
| [x] | `message` | `PART_Message` | A `TextBlock` |
| [x] | `icon` | `PART_Icon` | An `AvonicIcon` |
| [x] | `button` | `ToastNativeButton` | Made in code, one per button, in `PART_StartButtons` or `PART_EndButtons`: a type of its own with its own `ControlTheme`, as `/template/` can't reach what code makes |
| [x] | `button cancel` | `ToastNativeButton:cancel` | A pseudoclass, as the part is a state of the button (the mapping rule for `part="header expanded"`) |

### CSS custom properties (17)

| ✓ | Ionic | Avonic resource key | md default |
|---|---|---|---|
| [x] | `--background` | `Avonic.Toast.Background` | unset, following `Avonic.BackgroundColorStep800Brush` (`#333333`) |
| [x] | `--border-color` | `Avonic.Toast.BorderColor` | unset: `initial`, `currentColor`, the toast's `Foreground` |
| [x] | `--border-radius` | `Avonic.Toast.BorderRadius` | 4 |
| [x] | `--border-style` | `Avonic.Toast.BorderStyle` | `none`: no border is drawn, whatever its width, as CSS's `none` |
| [x] | `--border-width` | `Avonic.Toast.BorderWidth` | 0 |
| [x] | `--box-shadow` | `Avonic.Toast.BoxShadow` | md's three shadows, through `{b:CssShadow}` |
| [x] | `--button-color` | `Avonic.Toast.ButtonColor` | unset, following `Avonic.Color.PrimaryBrush` |
| [x] | `--color` | `Avonic.Toast.Color` | unset, following `Avonic.TextColorStep950Brush` (`#f2f2f2`) |
| [x] | `--end` | `Avonic.Toast.End` | 8 |
| [x] | `--height` | `Avonic.Toast.Height` | `auto` |
| [x] | `--max-height` | `Avonic.Toast.MaxHeight` | `auto` (no maximum: `auto` is invalid for `max-height`, so `none`) |
| [x] | `--max-width` | `Avonic.Toast.MaxWidth` | 700 |
| [x] | `--min-height` | `Avonic.Toast.MinHeight` | `auto` |
| [x] | `--min-width` | `Avonic.Toast.MinWidth` | `auto` |
| [x] | `--start` | `Avonic.Toast.Start` | 8 |
| [x] | `--white-space` | `Avonic.Toast.WhiteSpace` | `normal`: a `string` read by `ToastContent.WhiteSpace` (`normal`, `nowrap`, `pre`, `pre-wrap`, `pre-line`, `break-spaces`) |
| [x] | `--width` | `Avonic.Toast.Width` | `auto` |

A size takes a number (px) or a CSS string, read by `AlertPanel.Length` (#39), as the action sheet's and the modal's do.

md's own values with no variable, kept as keys so that they can be restyled (the mapping rule for a literal): the cancel button's
colour, `Avonic.Toast.Md.ButtonCancelColor` (`--ion-color-step-100`, `--ion-text-color-step-900`: `#e6e6e6`), and the hover
backgrounds, `Avonic.Toast.Md.ButtonBackgroundHover` (`rgba(--ion-color-primary-rgb, 0.08)`) and
`Avonic.Toast.Md.ButtonCancelBackgroundHover` (`rgba(--ion-background-color-rgb, 0.08)`).

## What Ionic does

From `toast.js`, `animations/md.enter.js`, `md.leave.js`, `animations/utils.js`, `gestures/swipe-to-dismiss.js`, `toast.md.css`
(with the base part of the md file) and `utils/overlays.js`.

- **Present**: the lock, then the anchor (`getAnchorElement`, by id each time) and the animation's position
  (`getAnimationPosition`), cached for the leave; the shared `present()` with `{position, top, bottom}`; then the content is
  revealed to screen readers; then, with `duration > 0`, a timer that dismisses with the role `timeout`; then, with
  `swipeGesture: 'vertical'`, the swipe gesture.
- **The position** (md): 8 px from the edge, `calc(8px + var(--ion-safe-area-top, 0px))` at the top and
  `calc(-8px - var(--ion-safe-area-bottom, 0px))` at the bottom. With an anchor (not in the middle), plus the anchor's
  `getBoundingClientRect()`: at the top `8 + box.bottom`; at the bottom `-8 - (innerHeight - box.top)`, with no safe area.
- **md enter** (400 ms, `cubic-bezier(.36,.66,.04,1)`): sets the wrapper's inline `transform: translateY(<top>)` (top) or
  `translateY(<bottom>)` (bottom), not animated, or its inline `top` to `floor(host.clientHeight / 2 - wrapper.clientHeight / 2)`
  (middle); and animates only the wrapper's opacity from 0.01 to 1. **md leave** (300 ms, the same curve): opacity 0.99 to 0. At
  rest the wrapper is `opacity: 0.01` and `translate3d(0, ±100%, 0)` (top and bottom). The inline values stay after the dismiss.
- **Dismiss**: the lock, the timer cleared, the shared `dismiss()` with the cached position, the swipe gesture destroyed.
- **Buttons** (`render`): start buttons (`side: 'start'`) in `.toast-button-group-start`, before the icon; the others in
  `.toast-button-group-end`, after the content; each a `<button type="button" tabindex="0">` with `toast-button`,
  `toast-button-icon-only` (an icon and no text), `toast-button-<role>`, `ion-focusable`, `ion-activatable`, its
  `htmlAttributes` spread, part `button` or `button cancel` (`isCancel`); inside, `.toast-button-inner` with the icon
  (`aria-hidden`, `slot="icon-only"` with no text) and the text; md's ripple, unbounded for an icon-only button.
- **A button** (`buttonClick`): a `cancel` or `backdrop` role dismisses with that role at once; otherwise the handler is awaited
  (`safeCall`): `false` keeps the toast, anything else (an exception too, logged) dismisses with the button's role, and no data.
- **`dispatchCancelHandler`**: the host's `ionToastWillDismiss` with a cancel role calls the first `cancel` button's handler, not
  awaited, its result ignored. So a cancel button's handler runs once, from the will-dismiss, whether it was clicked or the toast was
  dismissed in code with `cancel` or `backdrop`.
- **Swipe** (`createSwipeToDismissGesture`, on the wrapper, priority 39, `direction: 'y'`, threshold 10): an animation of the
  wrapper's transform held by `progressStart(true, …)` and moved by `progressStep`, linear, so the wrapper follows the finger 1:1.
  Bottom: from `translateY(<bottom>)` to `translateY(100%)` over `host.clientHeight - wrapper.top`; top: from `translateY(<top>)` to
  `translateY(-100%)` over `wrapper.bottom` (the drag inverted); middle: from `-(top + h)` through 0 to `+(top + h)`, started at 0.5,
  over `host.clientHeight + h`. At the release: `(deltaY + velocityY × 1000) / distance` (inverted at the top) at least 0.5 dismisses
  (middle: at least 0.25 either way); the gesture is off; the animation plays to the end (dismiss) or back, for
  `min(remaining / |velocity|, 200)` ms; then a dismiss with the role `gesture` (no leave animation), or the animation held again and
  the gesture back on. `swipeGesture` changed while presented destroys and makes the gesture.
- **Accessibility**: the host has `tabindex="-1"`; `.toast-content` is `role="status"`, `aria-live="polite"`, `aria-atomic="true"`;
  until the enter has finished the header and message are rendered with `aria-hidden="true"`, then again without, as new elements,
  so that a screen reader announces them once they're shown; the icons are `aria-hidden`.
- **Look** (`toast.md.css`): the host is the window, `font-size: 0.875rem`, `color: var(--color)`, `font-family`; the wrapper
  `position: absolute`, `inset-inline: var(--start) var(--end)`, `margin-inline: auto`, so with `width: auto` it fills between them
  up to `--max-width` 700 and is centred past it; `--background`, `--border-radius` 4, md's shadow, the border variables; the container a
  flex row, `align-items: center`, inheriting the wrapper's heights; the content `flex: 1`, padding 14 16, a column centred when
  baseline; the header 500, the header and message `line-height: 1.25rem`, the header 2 px above the message; the icon 1.4em with 16
  before it; the groups' margins (baseline: 8 at the start of the start group and the end of the end group; stacked: the groups 100%
  wide, `justify-content: end`, 8 at the end, and 8 above the start group and below the end group); each button padding 10 15, 14 px,
  500, `letter-spacing: 0.84px`, uppercase, `--button-color`, transparent, `line-height: normal`; cancel `#e6e6e6`; icon-only 36 x 36,
  padding 9, round. With `any-hover: hover`, a hovered button's background is `rgba(primary, 0.08)`, a cancel button's
  `rgba(background, 0.08)`, and its cursor a pointer. With `.ion-color`: the wrapper the colour, the text its contrast, every button
  (cancel too) the text's colour.

## Measured

| | Case | Result |
|---|---|---|
| M1 | Enter | 400 ms `cubic-bezier(0.36, 0.66, 0.04, 1)`, `fill: both`, one animation: the wrapper's opacity 0.01 to 1 (0.2894 at 50 ms, 0.7798 at 116.7, as the curve gives); the transform `translateY(-8px)` inline from the first frame, never animated; middle: `top: 398px` inline, no transform |
| M2 | Leave | 300 ms, opacity 0.99 to 0, the same curve; the enter's animation still held under it until the end; then `display: none`, the inline transform kept |
| M3 | Events | `ionToastWillPresent`, `willPresent` at 3; `ionToastDidPresent`, `didPresent` at 432; the dismiss pair with `{data, role}`, the did ones 300 later |
| M4 | Basic, bottom | wrapper 8, 788, 374 x 48; message at 24, 802, 342 x 20; `#333333`, radius 4, md's shadow; text `#f2f2f2`, 14 px Roboto 400 |
| M5 | Header, icon, Undo, Dismiss (cancel) | wrapper 8, 766, 374 x 70; icon 19.59 at 24, 791.2; content 43.59 to 212.13; header at 59.59, 780 (500), message at 802; end group 212.13, 783, 161.88 x 36, 8 from the end; Undo 71.25 x 36, its text 41.25 x 16 at 15 in, `#0054e9`; Dismiss 90.63, `#e6e6e6`; uppercase, 0.84 px apart |
| M6 | Stacked | wrapper 8, 702, 374 x 134; content the whole row, 90 (a message of two lines); the end group a line of its own, 8, 792, 366 x 36, the buttons at its end; 8 below it |
| M7 | Top; middle | top: 8, 8; middle: 8, 398 (`floor(844 / 2 - 48 / 2)`) |
| M8 | Icon-only cancel | 36 x 36 at 338, 794, padding 9, radius 50%; its icon 18 x 19.59 (squeezed by the padding); `aria-label` on the `<button>` |
| M9 | `color` | primary: wrapper `#0054e9`, text and both buttons `#fff`; danger with an icon: `#c5000f`, the icon `#fff` |
| M10 | Start buttons | baseline: the start group at 16 (8 from the wrapper's start), the content after it, the end group 8 from the end; stacked: the start group a line of its own above, 8 above it, its button at the end of the line |
| M11 | A long message | 4 lines, wrapper 108 high; the OK button centred beside it (764) |
| M12 | `--white-space: nowrap` | one line, overflowing the content (cut by the wrapper's end) |
| M13 | Tablet 1024 x 768 | 700 wide, centred: 162, 712; top: 162, 8 |
| M14 | Anchors | header 0, 0, 390 x 56; footer 0, 788, 390 x 56. Bottom on the footer: `translateY(-64px)`, at 732; top under the header: `translateY(64px)`, at 64; middle with an anchor: the middle, a warning; a name not found: as no anchor, a warning |
| M15 | Safe areas 30 / 20 | bottom: `translateY(-28px)`, at 768; top: 38 |
| M16 | `--width: 200px`, `--start: 20px`, `--end: 40px`, a 2 px border, `--min-height: 80px` | 83, 752, 204 x 84: content-box sizes, centred between 20 and 350; the content 48 high, centred in the container's 80 |
| M17 | RTL, `--start: 20px`, `--end: 40px` | the wrapper 40 to 370; the start group on the right, the icon after it, the end group on the left |
| M18 | `duration: 1000` | did present at 432, `ionToastWillDismiss {role: "timeout"}` at 1438, about 1000 after `present()` resolved |
| M19 | Focus | the opener keeps focus at present; Tab from the page never reaches the toast's buttons (below); Escape doesn't dismiss; after a dismiss the opener still has focus. `keyboardClose: true`: the host, then `body` after |
| M20 | Buttons | a handler returning false: still open; a role `go`: `{role: "go"}`, no data; Cancel: `{role: "cancel"}`, its handler once, though it returned false; `dismiss('data', 'cancel')` and `'backdrop'` in code: the cancel handler once; `'other'`: not; strings: plain buttons; a handler that throws: an error logged, dismissed; an async handler resolving to false: still open |
| M21 | Pointer | a tap at 20, 410, beside a bottom toast, clicked the page's button there |
| M22 | Hover (a desktop context) | Undo and an icon-only button: `rgba(0, 84, 233, 0.08)`, Dismiss `rgba(255, 255, 255, 0.08)`, cursor pointer; a press: the ripple in the button's colour |
| M23 | Over an alert | z-index 60004 over 20002; the alert's button keeps focus at the toast's present; Escape: nothing dismisses; a click on the toast's button: focus back on the alert's button; once the toast has gone, Escape dismisses the alert |
| M24 | Two toasts | the same place, the second over the first |
| M25 | Inline, `trigger` and `isOpen` | moved to `ion-app` at present; dismissed, still there, hidden, the inline transform kept; `position: 'top'` and `isOpen` true: at the top |
| M26 | Accessibility | during the enter, the header and message `aria-hidden="true"`; after, none |
| M27 | Swipe, bottom | a 20 px drag: nothing until 10, then 1:1 (-8 to 2); released still: back to -8 in 200 ms; 50 px: 1:1 to 32, then out to 48 (the wrapper's height) in 200 ms and `{role: "gesture"}` at once, all four events together; up: nothing; a fast 20 px flick: out, in 176 ms |
| M28 | Swipe, top and middle | top: the same, inverted; middle: 1:1 either way, 100 px back to 0 in 200 ms, 300 px out to ±446 (`398 + 48`) |
| M29 | Swipe off, then on while presented | no `swipeGesture`: a drag does nothing; set to `vertical` while presented: the next drag dismisses |
| M30 | A render while presented | message, header, buttons, icon, colour and layout change at once; a new `position` moves the wrapper by its class, but keeps the inline transform of the present (top 0 with `translateY(-8px)`) |

## Not copied from Ionic

Each recorded in `docs/infra/ionic-issues.md`:

- **Tab skips the toast's buttons.** The host has `tabindex="-1"`, and Chromium leaves out of sequential navigation everything in
  the shadow tree of a host with a negative `tabindex`, so the buttons, which Ionic gives `tabindex="0"` and `ion-focusable`, are
  never reached by Tab or Shift+Tab (measured both ways); a focused one (by a tap, or `focus()`) does Tab to the next one, and then
  out. Avonic's buttons are tab stops, after the page's last, as Ionic's `tabindex="0"` asks.
- **A toast on top stops Escape and back.** `NON_TOAST_OVERLAYS` excludes toasts from the close watcher's condition, but the
  Escape and `ionBackButton` listeners take `getPresentedOverlay(document)`, toasts included, and a toast has no `backdropDismiss`.
  With an `ion-app` the back press then falls through to a handler below, such as the router's, which navigates under the modal.
  Avonic's Escape and back read the top overlay that isn't a toast. **A change to shared code** (`OverlayHost`, three lines).
- **A re-present after a change of `position`** keeps the other position's inline value: Ionic's middle sets only `top` and the
  others only `transform`, so a toast presented in the middle and then at the bottom is over-constrained at `top: 398px`. Avonic's
  middle clears the transform, and its panel reads the middle offset only in the middle.

## Design

`AvonicToast : AvonicOverlay`, in `Controls/Toast/`: `StealsFocus` false, `ZIndexBase` 60000, `KeyboardClose` false by default,
no `BackdropDismissEnabled` (the base's `false`).

- **Template**: a `ToastPanel` (`PART_Panel`), which places the wrapper as `.toast-wrapper` is placed: between `Start` and `End`
  (CSS lengths, logical, so mirrored in RTL by Avalonia's flow direction), `ContentWidth` within `ContentMinWidth` and
  `ContentMaxWidth`, content-box sizes as CSS's, centred when it doesn't fill; as tall as its content within the height keys; at
  the top, the bottom, or `MiddleTop` in the middle (static position, 0, until the enter sets it). The wrapper is a `Border`
  (background, radius, border, shadow, the opacity at rest, `RenderTransform` from `Css.TranslateY` ±1 at rest), holding a
  `ToastContainer` (`PART_Container`): the flex row of `toast.md.css`, the start group, the icon, the content (`flex: 1`) and the
  end group, centred across, that wraps when `IsStacked`, a group then taking a line of its own, as `width: 100%` does. The content
  is a `ToastContent` (a `Border` with a live region's peer) with a `StackPanel` of the header and message `TextBlock`s.
- **`ToastNativeButton`**: the `<button>`, a type of its own: `NativeButton`, `Activatable`, `FocusVisible`, md's ripple (bounded,
  unbounded icon-only), an icon and a label in a `FlexRow`; `:role-<role>`, `:cancel`, `:icon-only`; to automation a button named by
  its text or `aria-label`.
- **The animations**: `MdEnterAnimation` sets the wrapper's local `RenderTransform` (top, bottom) or the panel's `MiddleTop` (middle)
  from the `ToastPresentOptions`, as Ionic's inline style, then animates the opacity; `MdLeaveAnimation` the opacity. The position
  is worked out at present (`OnPresenting`), as `getAnimationPosition`, in px: Ionic's `calc(… var(--ion-safe-area-…))` keeps
  following the variable while presented; Avonic reads `Avonic.SafeAreaTop` and `Avonic.SafeAreaBottom` once, at present.
- **The swipe**: `Gesture.Create` on the wrapper, priority 39, `Y`, and an `AvonicAnimation` of its transform, driven by
  `ProgressStart`, `ProgressStep` and `ProgressEnd`, as `swipe-to-dismiss.js`. `Keyframes` replaced at the release in the middle.
- **Duration**: `AvonicConfig.Schedule` once the present has finished; disposed at the start of a dismiss.
- **The data**: `ToastButton` is a plain class; `Buttons` a list the toast follows while it's in a tree, rendered again after a change
  (posted, as Stencil renders once for many), keeping each button's control by its place in its group, so focus and a press survive.
- **Shared code**: `AvonicConfig.ToastDuration` (Ionic's `toastDuration`); `OverlayHost`'s Escape and back skip toasts (above).
  Nothing else; `OverlayHandler.Run` and `AlertClasses` are reused.

### Not done, with why

| Not here | Why |
|---|---|
| `translucent`, ios's animations | ios only |
| `innerHTMLTemplatesEnabled`, `IonicSafeString` | Plain text, as the alert (#39) |
| The `toastEnter` and `toastLeave` configs | No overlay here has its animation config; `EnterAnimation` and `LeaveAnimation` are there |
| A controller (`toastController.create`, `getTop`) | The overlay system's deferral |
| `aria-atomic` | No counterpart in Avalonia: the live region is the content as a whole already |
| Ionic's printed warnings (stacked with buttons on both sides) | Logged by Avalonia's logger only for the anchor; the layout one is advice, and not copied |
| `cssClass` as CSS | Style classes here, for an app's styles |

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| B1 | md enter: 400 ms on the curve, the wrapper's opacity 0.01 to 1 frame by frame; the transform set from the first frame and not animated: -8 at the bottom, 8 at the top; middle: `floor(H / 2 - h / 2)`, no transform | `md.enter.js`, M1, M7 |
| B2 | md leave: 300 ms, opacity 0.99 to 0; at rest (before a present) the wrapper is at 0.01 and `translateY(±100%)`; the transform kept after | `md.leave.js`, M2 |
| B3 | Events, in order, with data and role; `Present()` finishes after the did-present | M3 |
| B4 | Placement: bottom, top, middle; the insets 8; 700 at most, centred on a tablet; `--width` centred between the insets; content-box sizes with a border; `--min-height` centring the content; RTL swaps the insets | `toast.md.css`, M4, M7, M13, M16, M17 |
| B5 | Safe areas: `Avonic.SafeAreaBottom` and `Avonic.SafeAreaTop` at present | `getAnimationPosition`, M15 |
| B6 | `PositionAnchor`: a control or a name; bottom above its top by 8, top below its bottom by 8, with no safe area; ignored in the middle; a name not found is as none | `getAnchorElement`, M14 |
| B7 | `Duration`: a dismiss with `timeout`, `Duration` after the present finished; none with 0; a dismiss before then cancels it; `AvonicConfig.ToastDuration` is the default, and an app's value wins | `present`, M18 |
| B8 | Buttons: the two groups by `Side`; strings plain; `:role-<role>`, `:cancel` for `cancel` and `backdrop`, `:icon-only`; the icon before the text | `render`, M5, M8, M10, M20 |
| B9 | A button: `cancel` or `backdrop` dismisses at once with its role; otherwise the handler: false (or a task of it) keeps it, else a dismiss with the role and no data; an exception dismisses | `buttonClick`, M20 |
| B10 | A dismiss with a cancel role, from a button or code, runs the first cancel button's handler once; another role doesn't | `dispatchCancelHandler`, M20 |
| B11 | Focus: nothing blurred at present, nothing given back after; `KeyboardClose` false by default, true focuses the toast; a click on a toast's button over an alert gives focus back to the alert | `present`, M19, M23 |
| B12 | Not blocking: a tap beside the wrapper reaches the page; the host and the panel aren't hit; Escape and back don't dismiss a toast, and do dismiss an alert under it (not copied) | M21, M23 |
| B13 | Above the other overlays: `ZIndex` 60000 + its index; two toasts in the same place, the later on top | M23, M24 |
| B14 | Swipe, bottom and top: nothing before 10 px, then 1:1; a release under the threshold animates back in 200 ms (or the velocity's time) and the gesture works again; over it, out to `translateY(±100%)` and a dismiss with `gesture`, with no leave animation; the other way does nothing | `swipe-to-dismiss.js`, M27, M28 |
| B15 | Swipe, middle: either way, threshold 0.25; back, or out to `±(top + h)` | M28 |
| B16 | `ToastSwipeGesture`: none while null; set while presented, the gesture comes; cleared, it goes; none after a dismiss | `swipeGestureChanged`, M29 |
| B17 | Layout: baseline (one row, centred across) and stacked (the groups on lines of their own, at the end); the groups' margins; a long message wraps, the buttons centred beside it | `toast.md.css`, M5, M6, M10, M11 |
| B18 | Colours: the keys' defaults; `:ion-color`: the wrapper the base, the text, the buttons and the cancel button the contrast | M4, M5, M9 |
| B19 | Hover: a button's and a cancel button's backgrounds with a mouse, none with a touch; md's ripple in each, unbounded icon-only | M22 |
| B20 | White space: `normal` wraps and collapses runs of spaces and line breaks, `nowrap` doesn't wrap, `pre` keeps them and doesn't wrap, `pre-line` keeps the breaks | `--white-space`, M12 |
| B21 | Automation: the content is a polite live region; the header and message hidden from it until the did-present, then shown; the icons hidden; a button named by its text or `aria-label`; the toast not a tab stop, `htmlAttributes`' `id` and `aria-label`; the buttons are tab stops after the page (not copied) | render, M19, M26 |
| B22 | `IsOpen` and `Trigger` through the base; an inline toast back in its place after | M25 |
| B23 | A render while presented: message, header, buttons, icon, colour and layout follow; a list changed in place renders once after | watchers, M30 |
| B24 | `CssClass` on the toast; `Header` and `Message` shown only when not null | `render` |

## Extensions

None. `Present(TopLevel)` is the overlay's.

## Dependencies

`AvonicOverlay`, `AvonicAnimation`, `OverlayHandler`, `AlertClasses`, `AlertPanel.Length` (#5, #39), the gesture utility (#4),
`AvonicIcon` (#3), `AvonicRippleEffect`, `Activatable`, `FocusVisible`, `NativeButton`, `FlexRow`, `ColorPalette`: all merged.
