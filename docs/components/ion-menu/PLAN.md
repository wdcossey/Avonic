# ion-menu family — Plan

`AvonicMenu`, `AvonicMenuButton`, `AvonicMenuToggle` and `MenuController`: Ionic's side menu, its hamburger button, its toggle
and the `menuController` utility they share, in one PR ([#44](https://github.com/wdcossey/Avonic/issues/44)). The menu isn't an
overlay in Ionic: it's an element of the page, with its own backdrop, animations, gesture and focus trap, which the controller
keeps to one open at a time. So it isn't an `AvonicOverlay`; it reuses what the overlays share where Ionic does
(`AvonicBackdrop`, `AvonicAnimation`, `Gesture`, `GestureController`'s blocker, `HardwareBackButton`, the focus-trap helpers).

Measured in real Ionic 9.0.11 in headless Chromium, md, on `render.js`'s phone (390 x 844 at 2x, mobile and touch); the raw output
is [`ref/measurements.txt`](ref/measurements.txt) and [`ref/extra.txt`](ref/extra.txt), from the scripts named in
[COMPARISON.md](COMPARISON.md).

## Fidelity checklist

`node tools/ionic-api.js ion-menu`, `ion-menu-button`, `ion-menu-toggle`.

### ion-menu: props (7)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `contentId` | `ContentId` | `string?` | | The `Name` of the main content, looked up in the menu's name scope when it's attached, as `getElementById` at `connectedCallback`. None found: an error logged, and the menu isn't registered with the controller and has no gesture, as Ionic's |
| [x] | `disabled` | `IsEnabled` (inverted) | `bool` | `true` | `menuController.enable()` sets it. The menu's own, not inherited: Ionic's `_isActive()` reads its own prop |
| [x] | `maxEdgeStart` | `MaxEdgeStart` | `double` | `50` | |
| [x] | `menuId` | `MenuId` | `string?` | | |
| [x] | `side` | `Side` | `MenuSide` | `Start` | `Start`, `End` |
| [x] | `swipeGesture` | `MenuSwipeGesture` | `bool` | `true` | Not `SwipeGesture`, which `InputElement` has as an event, as `ion-segment`'s became `SegmentSwipeGesture` |
| [x] | `type` | `Type` | `MenuType?` | null | `Overlay`, `Reveal`, `Push`. Null, as Ionic's `undefined`, takes `AvonicConfig.MenuType` (Ionic's `menuType`, md's `overlay`) when it's first attached, if it's still null then (`ConfigOnLoad.IfUnset`) |

### ion-menu: events (4), methods (6), slot, parts

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionWillOpen` | `IonWillOpen` | Bubbles |
| [x] | `ionDidOpen` | `IonDidOpen` | Bubbles |
| [x] | `ionWillClose` | `IonWillClose` | `MenuCloseEventArgs`, its `Role` |
| [x] | `ionDidClose` | `IonDidClose` | `MenuCloseEventArgs` |
| [x] | `ionMenuChange` (`@internal`) | `MenuController.MenuChanged`, internal | What the button and the toggle listen to on `body`: one static event for every menu, as theirs hears every menu's |
| [x] | `isOpen()` | `IsOpen()` | `bool`, synchronous |
| [x] | `isActive()` | `IsActive()` | `bool`: enabled (and in no split pane, which Avonic doesn't have yet, #78) |
| [x] | `open(animated?)` | `Open(bool animated = true)` | `Task<bool>`: it completes when the animation does |
| [x] | `close(animated?, role?)` | `Close(bool animated = true, string? role = null)` | `Task<bool>` |
| [x] | `toggle(animated?)` | `Toggle(bool animated = true)` | `Task<bool>` |
| [x] | `setOpen(shouldOpen, animated?, role?)` | `SetOpen(bool shouldOpen, bool animated = true, string? role = null)` | `Task<bool>` |
| [x] | default slot | `Content` (+ `ContentTemplate`) | Inside `.menu-inner` |
| [x] | `container` | `PART_Container` | A `Border`: the background and, for `overlay`, the shadow; the content clipped inside it, as `contain: strict` |
| [x] | `backdrop` | `PART_Backdrop` | An `AvonicBackdrop`, `Tappable` and `StopPropagation` false, as Ionic renders it |

### ion-menu: CSS custom properties (7)

Each is a key in `Tokens/Menu.axaml`. A size takes a number (px) or a CSS string (`304px`, `80%`, `auto`, `calc()`), read as the
alert's are (`AlertPanel.Length`), its percentage of the menu's host, the containing block of `.menu-inner`.

| ✓ | Ionic | Avonic resource key | md default |
|---|---|---|---|
| [x] | `--background` | `Avonic.Menu.Background` | unset, falling back to `Avonic.BackgroundColorBrush` (`--ion-background-color`) |
| [x] | `--width` | `Avonic.Menu.Width` | 304; below 341 wide, `Avonic.Menu.Md.WidthSmall` (264) whatever this is, as `.menu-inner`'s own `--width` beats the host's |
| [x] | `--min-width` | `Avonic.Menu.MinWidth` | `auto` |
| [x] | `--max-width` | `Avonic.Menu.MaxWidth` | `auto` |
| [x] | `--height` | `Avonic.Menu.Height` | `100%` |
| [x] | `--min-height` | `Avonic.Menu.MinHeight` | `auto` |
| [x] | `--max-height` | `Avonic.Menu.MaxHeight` | `auto` |

No variable: the overlay type's `box-shadow: 4px 0px 16px rgba(0, 0, 0, 0.18)` on `.menu-inner` (`Avonic.Menu.Md.BoxShadow`), and
core.css's `.md .menu-content-push`, `.md .menu-content-reveal` `box-shadow` of the same value on the main content
(`Avonic.Menu.Md.ContentBoxShadow`), both literals in `Tokens/`, as they hold a colour.

### ion-menu-button: props (6), slot, parts, variables (13)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `autoHide` | `AutoHide` | `bool`, `true` |
| [x] | `color` | `Color` | `ColorPalette` |
| [x] | `disabled` | `IsEnabled` (inverted) | |
| [x] | `menu` | `Menu` | `string?`: a `MenuId`, or `start` or `end` |
| [ ] | `mode` | none | Not exposed (ADR 0001) |
| [ ] | `type` | none | Not applicable: HTML form, as `ion-button`'s |
| [x] | default slot | `Content` (+ `ContentTemplate`) | Empty, it shows the icon, as the slot's fallback content |
| [x] | `native` | `PART_Native` | A `Border` |
| [x] | `icon` | `PART_Icon` | An `AvonicIcon`, `AvonicConfig.MenuIcon` or md's `menu-sharp` |
| [x] | `--background` | `Avonic.MenuButton.Background` | transparent |
| [x] | `--background-focused` | `Avonic.MenuButton.BackgroundFocused` | unset: `currentColor` |
| [x] | `--background-focused-opacity` | `Avonic.MenuButton.BackgroundFocusedOpacity` | 0.12 |
| [x] | `--background-hover` | `Avonic.MenuButton.BackgroundHover` | unset: `currentColor` |
| [x] | `--background-hover-opacity` | `Avonic.MenuButton.BackgroundHoverOpacity` | 0.04 |
| [x] | `--border-radius` | `Avonic.MenuButton.BorderRadius` | `50%` |
| [x] | `--color` | `Avonic.MenuButton.Color` | unset (`initial`): the host's colour is inherited |
| [x] | `--color-focused` | `Avonic.MenuButton.ColorFocused` | unset: `currentColor` |
| [x] | `--color-hover` | `Avonic.MenuButton.ColorHover` | unset: md leaves it undefined, so the colour is inherited |
| [x] | `--padding-start`, `--padding-end` | `Avonic.MenuButton.PaddingStart`, `.PaddingEnd` | 8 |
| [x] | `--padding-top`, `--padding-bottom` | `Avonic.MenuButton.PaddingTop`, `.PaddingBottom` | 0 |

`width: 3rem; height: 3rem; font-size: 1.5rem` (48, 48, 24) have no variable: literals in the theme.

### ion-menu-toggle: props (2), slot

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `autoHide` | `AutoHide` | `bool`, `true` |
| [x] | `menu` | `Menu` | `string?` |
| [x] | default slot | `Content` | A `ContentControl`; no look of its own (`menu-toggle.css` has only `.menu-toggle-hidden { display: none }`) |

### menuController (`utils/menu-controller`)

Not in `core.json`, but exported by `@ionic/core` and documented as Ionic's menu controller: a static class `MenuController`.

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `open(menu?)`, `close(menu?)`, `toggle(menu?)` | `Open`, `Close`, `Toggle` | `Task<bool>`. `Close()` with no menu closes the open one |
| [x] | `enable(shouldEnable, menu?)`, `swipeGesture(shouldEnable, menu?)` | `Enable`, `SwipeGesture` | The menu found, or null |
| [x] | `isOpen(menu?)`, `isEnabled(menu?)` | `IsOpen`, `IsEnabled` | `bool` |
| [x] | `get(menu?)`, `getOpen()`, `getMenus()` | `Get`, `GetOpen`, `GetMenus` | Synchronous: Ionic's wait for every `ion-menu` to be ready (`waitUntilReady`) is a menu that has been attached here |
| [x] | `isAnimating()` | `IsAnimating` | |
| [x] | `registerAnimation(name, builder)` | `RegisterAnimation(MenuType, builder)` | Replaces a type's animation; the builder is given a `MenuAnimationContext` (the menu, its parts, its width and side), Ionic's `MenuI` |

## What Ionic does

From `menu.js`, `menu.md.css`, `menu-button.js`, `menu-button.md.css`, `menu-toggle.js`, `menu-toggle-util.js`,
`utils/menu-controller/index.js` and `animations/{base,overlay,push,reveal}.js`, `utils/hardware-back-button.js`,
`utils/focus-trap.js`, and core.css's `.menu-content*` rules.

**The menu**
- **Connect** (`connectedCallback`, every attach): `type` undefined takes `config.get('menuType', 'overlay')`. The content is
  `getElementById(contentId)`; none: `printIonError` and nothing else, so the menu isn't registered and has no gesture. The
  content gets `menu-content` and `menu-content-<type>`; the menu registers with the controller, emits `ionMenuChange` if it
  has loaded before, creates its gesture on `document` (`menu-swipe`, priority 30, threshold 10, `blurOnStart`) and calls
  `updateState`. **Load** (`componentDidLoad`, once): `didLoad`, then `ionMenuChange { disabled, open }` and `updateState`.
- **`updateState`**: the gesture is enabled when the menu is active and `swipeGesture` is on. A menu that isn't active is closed
  at once with `afterAnimation(false, 'gesture')`, which emits `ionDidClose { role: 'gesture' }` even when it was closed already
  (measured: a menu disabled at load emits `ionDidClose` twice, at connect and at load, around its `ionMenuChange`); one disabled
  mid-animation marks the operation cancelled, so its `setOpen` returns false.
- **Watchers**: `disabled` → `updateState`, then `ionMenuChange`; `side` → `isEndSide`, and the animation is made again;
  `swipeGesture` → `updateState`; `type` → the content's and the inner's inline styles go, the content's class follows, and the
  animation is made again.
- **`setOpen(shouldOpen, animated = true, role)`**: blurs `document.activeElement`, then the controller's `_setOpen`: false if any
  menu is animating; to open, a different open menu is closed first without animation (`setOpen(false, false)`, so no role);
  then the menu's own: false if it isn't active, is animating, or is already in that state. `beforeAnimation`, `loadAnimation`,
  `startAnimation`; false if the operation was cancelled; `afterAnimation`; true.
- **`beforeAnimation(shouldOpen, role)`**: `show-menu` (the host is shown), `tabindex="0"`, `show-backdrop`, the content gets
  `menu-content-open` and `aria-hidden="true"`; the blocker blocks scrolling; `isAnimating`; `ionWillOpen`, or
  `ionWillClose { role }`. On Android, `aria-hidden` on the menu until it's open (a TalkBack workaround).
- **`loadAnimation`**: the inner's `offsetWidth` and `isEndSide` (`side`, and the direction: start is the end in RTL); a new
  animation if either changed, or there's none: the type's builder from the controller, `duration(0)` with `animated` off in the
  config, `fill('both')`.
- **`startAnimation(shouldOpen, animated)`**: direction `reverse` and easing `cubic-bezier(0.4, 0, 0.6, 1)` to close, `normal`
  and `cubic-bezier(0.0, 0.0, 0.2, 1)` to open; played and awaited, or with `animated` false `play({ sync: true })` not awaited;
  the direction set back to normal.
- **Animations** (base: 300 ms in md). `overlay`: the inner `translateX(-(width + 8)px)` to 0 (`+` at the end side), the backdrop
  opacity 0.01 to 0.25. `push`: the inner `translateX(∓width)` to 0, the content 0 to `±width`, the backdrop 0.01 to 0.32 (but
  it isn't displayed for push). `reveal`: the content 0 to `±width`; the inner stays at 0.
- **`afterAnimation(isOpen, role)`**: `_isOpen`, not animating. Open: `ionDidOpen`; focus moves to the menu itself unless focus
  is already in it; the focus trap starts. Closed: the blocker unblocks; `show-menu`, `tabindex`, `show-backdrop`,
  `menu-content-open` and the content's `aria-hidden` go; the animation is stopped (its values go); `ionDidClose { role }`; the
  trap stops. Focus isn't given back (measured: `body` after a close).
- **Focus trap** (`document` `focus` in the capture phase, while open), unless the top presented overlay doesn't contain the
  menu: focus inside is remembered; focus outside goes to the first focusable element in the menu, or the last if the first is
  where it was (Shift+Tab), or the menu itself if there's none (measured: Tab from the menu with one button inside stays on it).
- **Backdrop** (a `click` listener on the host, in the capture phase): open, at least 100 ms after a gesture's end, and the click
  not in `.menu-inner`: `preventDefault`, `stopPropagation`, `close(undefined, 'backdrop')`. The backdrop is shown only for
  `overlay`; for push and reveal the content has `pointer-events: none` while open, so a click there reaches the host behind it.
- **Escape** (`keydown` on the host, as `experimentalCloseWatcher` is off): `close(undefined, 'backdrop')`.
- **Gesture**: `canStart`: no `ion-modal.show-modal` in the document, `swipeGesture`, not animating, active; if open, any press
  starts it; if another menu is open, none; else only a press within `maxEdgeStart` of the window's edge on the menu's side
  (`currentX <= maxEdgeStart`, or `>= innerWidth - maxEdgeStart` at the end side; measured: 49 opens, 51 doesn't).
  `onWillStart`: `beforeAnimation(!isOpen, 'gesture')`, `loadAnimation`. `onStart`: `progressStart(true, isOpen ? 1 : 0)`.
  `onMove`: `delta = max(0, isOpen != isEndSide ? -deltaX : deltaX)`, step `delta / width`, `progressStep(isOpen ? 1 - step :
  step)`. `onEnd`: complete if the velocity is past 0.2 px/ms the right way, or the drag past half the width (`z`); the step
  through `getTimeGivenProgression` of `cubic-bezier(0.4, 0, 0.6, 1)`, ±0.001; `progressEnd` over 300 ms on that easing, then
  `afterAnimation(shouldOpen, 'gesture')`; `lastOnEnd` is the end's time.
- **Disconnect**: `close(false)`, the blocker destroyed, unregistered, the animation and gesture destroyed.
- **Host**: `role="navigation"`, `aria-label` the host's own or `menu`; classes `menu-type-<type>`, `menu-enabled`,
  `menu-side-<side>`.

**The controller**: `get(menu)`: `start` or `end`: the first enabled menu on that side, else the first on that side; another
string: the menu with that `menuId`, or none; nothing: the first enabled menu, else the first. `_setOpen` as above. On
`ionBackButton`, an open menu registers at `MENU_BACK_BUTTON_PRIORITY` (99), to close; the handler returns the close's promise.

**The button**: a click toggles `menuController.toggle(menu)`. `visible` is `get(menu)` existing and active, read at load and at
every `ionMenuChange` (and `ionSplitPaneVisible`); hidden (`display: none`, `aria-hidden`) when `autoHide` and not visible.
`menu-button-disabled`: `aria-disabled`, the native button disabled, opacity 0.5, no pointer events. The native `<button>` is
named `aria-label` from the host or `menu`. The icon is `config.get('menuIcon', menuSharp)` read in `render()`; an unbounded
ripple in md; `in-toolbar`, `in-toolbar-color` from `hostContext`; `ion-activatable`, `ion-focusable`.

**The toggle**: the same click and `autoHide` (read at every connect and every `ionMenuChange`), hidden with `display: none` and
`aria-hidden`. Its click is the DOM `click` on the host, from anything inside.

## Measured

| | Case | Result |
|---|---|---|
| M1 | Closed | The host `display: none`, z-index 1000 (overlay). The button 48 x 48 at 4, 4 in the toolbar's start buttons; its icon 24 at 16, 16; colour the toolbar's (66, 66, 66); `aria-label="menu"` on the native button |
| M2 | Open, overlay | The inner 0, 0, 304 x 844, white, `4px 0px 16px rgba(0,0,0,0.18)`; the backdrop over the host, 0.25; animations 300 ms `cubic-bezier(0, 0, 0.2, 1)`, fill both: the inner `translateX(-312px)` to 0, the backdrop 0.01 to 0.25. The content: `pointer-events: none`, `aria-hidden`. `ionWillOpen` at the call, `ionDidOpen` about 316 ms later. Focus on `ion-menu` |
| M3 | Push | The inner from -304; the content 0 to `translateX(304px)` with its shadow; the backdrop hidden (0.32 animated, `display: none`); host z-index `auto` |
| M4 | Reveal | Only the content moves, 0 to 304; the inner at 0 the whole time; host z-index 0 |
| M5 | End side | The inner at 86 (390 - 304), from `translateX(312px)` |
| M6 | 320 x 640 | The inner 264 wide, from -272 |
| M7 | A click outside the inner | `ionWillClose`, `ionDidClose` with `role: 'backdrop'`, 300 ms apart; Escape the same |
| M8 | Opening a second menu while one is open | The first closes at once (`ionWillClose`, `ionDidClose` with no role, same ms), then the second opens |
| M9 | `open()` on one menu, then `close()` and another's `open()` at once | `[true, false, true]`: both menus opened, as the controller's `isAnimating` check runs before either has started (its `await getOpen()`); see the deliberate difference below |
| M10 | Disabling an open menu | `ionDidClose { role: 'gesture' }`, then `ionMenuChange { disabled: true }`; no `ionWillClose`. A closed menu disabled: the same. Disabled at load: `ionDidClose`, `ionMenuChange`, `ionDidClose`. `open()` on it: false |
| M11 | Focus | After open, `ion-menu` (the button that had it blurred); Tab: the one button in the menu, again and again; after close, `body` |
| M12 | Swipe from the left edge | Captured at the 10 px threshold; the inner then at `-312 + 312 × delta / 304` (linear), the backdrop with it; from 49 px opens, from 51 nothing; `max-edge-start="100"`: from 90 opens; the end side from 380 leftwards opens |
| M13 | Swipe to close an open menu | From 280 leftwards; the first move at `-0.0312` (the step clamped to 0.9999), then linear; released moving left: `ionWillClose { role: 'gesture' }`, `ionDidClose { role: 'gesture' }` |
| M14 | A short swipe on an open menu's backdrop | `ionWillClose { role: 'gesture' }` then `ionDidOpen`: it goes back open |
| M15 | The button | Disabled: opacity 0.5, `pointer-events: none`, `aria-disabled`; `color="danger"`: the native colour (197, 0, 15); in a primary toolbar: white; focused (Tab): `::after` the colour at 0.12; the menu disabled: the button `display: none`, `aria-hidden`; with `auto-hide="false"`, shown |
| M16 | No `content-id` | The error logged; the button hidden (the menu isn't registered); `open()` on the menu still opens it |
| M17 | Removed while open | No events reach `document`; the content's `menu-content-open` goes; the button isn't hidden (no `ionMenuChange` on unregister) |
| M18 | The button clicked twice 50 ms apart | Opens once: the second toggle is refused while it's animating |

## Design

**`AvonicMenu : ContentControl`**, in `Controls/Menu/`.

- **Template**: a `Panel` (transparent, so the whole host takes a click, as the host behind push and reveal content does) holding
  `PART_Backdrop`, an `AvonicBackdrop` (`Tappable` and `StopPropagation` false); `PART_ContentShadow`, a `Border` the menu
  places over the main content's box, with its shadow, for push and reveal; and a `MenuPanel` that places `PART_Container`, the
  `.menu-inner` `Border`, at the menu's side with the CSS sizes. Inside the container, a clipping part and the content
  presenter.
- **Direction**: `.menu-inner`'s `translateX` and its `box-shadow` are physical in Ionic, and an Avalonia `RenderTransform` in a
  right-to-left subtree is mirrored. So the template's root is laid out left to right, and the content presenter takes the
  menu's own `FlowDirection` back: the inner's placement, transform and shadow are physical, as `isEndSide` is, and what's in the
  menu is laid out in its direction. The main content's `translateX` is physical in Ionic too: the menu negates it when the
  content's parent is mirrored.
- **Shown** (`show-menu`): the host's `IsVisible` and `Focusable` at style priority through `StyleValue`s bound in the
  constructor, so an app's style still wins. Its `ZIndex` per type, at style priority: 1000 for overlay, 0 otherwise.
- **Pseudoclasses**: `:type-overlay`, `:type-push`, `:type-reveal`, `:side-start`, `:side-end`, `:show-menu`, and `:small-width`
  (Ionic's `@media (max-width: 340px)`, from the top level's client width, as the modal's queries).
- **The content** (`menu-content-open`): `IsHitTestVisible` false at style priority while the menu is shown, and
  `AutomationProperties.IsOffscreenBehavior` `Offscreen` for `aria-hidden` (see the gaps). Its `translateX` for push and reveal
  is the animation's, at animation priority, gone when it stops.
- **Safe area**: the side of the inner away from the screen's edge gets `Avonic.SafeAreaLeft` or `Avonic.SafeAreaRight` 0 in the
  container's resources, as `--ion-safe-area-right: 0px` (LTR start) does.
- **Gesture**: `Gesture.Create` on the `TopLevel`, Ionic's `document`, with Ionic's config; `canStart` reads the top level's
  client width for `innerWidth`, and a presented `AvonicModal` in its overlay host for `ion-modal.show-modal`.
- **Animations**: `AvonicAnimation`, through the controller's registry; the gesture drives them with `ProgressStart`,
  `ProgressStep` and `ProgressEnd`, and `CubicBezier.GetTimeGivenProgression`. `loadAnimation`'s `offsetWidth` is the container's
  width after `UpdateLayout()`, the menu having just been shown.
- **Backdrop click**: `NativeButton` with no keys on the host, as `AvonicBackdrop` has for its DOM `click`: a click whose source
  isn't in `PART_Container` closes it with `backdrop`, at least 100 ms after a gesture's end by the events' own times
  (`AvonicConfig.EventTime`), and is handled.
- **Focus**: `setOpen` clears the focus; after opening, the menu takes it unless it's already inside; the trap is a handler on the
  top level's `GotFocus` while open, which defers to an overlay on top, through the overlay host's helpers (made `internal` for
  this: `OverlayHost.Focusables`, `FocusFirstDescendant`, `FocusLastDescendant`, `GetPresentedOverlay` without creating a host).
- **Back**: the controller adds an `IonBackButtonEvent` handler to each top level a menu is registered in: an open menu there
  registers at `HardwareBackButton.MenuBackButtonPriority` (99) to close it, returning the close's task. With no `AvonicApp`, as an
  overlay does, the menu takes `BackRequested` itself, unless an overlay that dismisses on back is presented, which takes it.
- **Automation**: `LandmarkType` `Navigation` by `OverrideDefaultValue`, and the peer's name `menu` unless the app names it.

**`AvonicMenuButton : TemplatedControl`**: `NativeButton` (click, Enter, Space), `Activatable` and `FocusVisible` at style priority,
an unbounded `AvonicRippleEffect`; `:in-toolbar`, `:in-toolbar-color` worked out from its ancestors at attach and at each prop
change, as `AvonicButton`'s; hidden with `IsVisible` false at style priority. A `Button` automation peer named `menu` unless the
app names it.

**`AvonicMenuToggle : ContentControl`**: `NativeButton` with no keys and `StockButtonsClick`, as `AvonicChip`'s DOM `click`, so a
click on an `AvonicButton` or a stock `Button` inside toggles; hidden with `IsVisible` false at style priority.

**`MenuController`**: a static class; menus register at attach and unregister at detach, in attach order.

### Not done, with why

| Not here | Why |
|---|---|
| `ion-split-pane` (`isPaneVisible`, `menu-pane-visible`, `split-pane-side`, `ionSplitPaneVisible`) | `ion-split-pane` is #78. `IsActive()` is `IsEnabled` until then |
| `aria-hidden` on the menu during its animation on Android | A TalkBack focus-ring workaround for Chromium's transforms |
| `experimentalCloseWatcher` | A browser API; Escape is the host's `keydown`, as Ionic's default |
| `menu-content` (`translate3d(0,0,0)`), `cursor: pointer`, `touch-action: manipulation` on the content | Hints to the browser's renderer and its touch handling |
| `overflow-y: hidden` and `--overflow: hidden` on the open content | With `IsHitTestVisible` false, the content takes no pointer or wheel, and focus is trapped in the menu |
| The button's `type` | HTML form |

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| B1 | Closed: the host hidden; `Open()` shows it, `IonWillOpen` at the call and `IonDidOpen` when the 300 ms animation ends; `true` | `setOpen`, M2 |
| B2 | Overlay animation on the clock: the inner from `-(width + 8)` to 0 and the backdrop 0.01 to 0.25 on `cubic-bezier(0, 0, 0.2, 1)`; closing reverses on `cubic-bezier(0.4, 0, 0.6, 1)`; the host hidden after | overlay.js, M2 |
| B3 | Push: the inner from -width, the content 0 to +width; reveal: the content alone; the content's transform gone after the close | push.js, reveal.js, M3, M4 |
| B4 | The end side: the inner at the far edge, from +(width + 8); RTL: start is the far edge, physical | `isEndSide`, M5 |
| B5 | Width: `Avonic.Menu.Width` 304, the inner sized and placed by the size keys; 264 below 341 wide | menu.md.css, M6 |
| B6 | Events and their roles: `Close(true, "cancel")`; backdrop; Escape; gesture; no role when another menu opens | M7, M8 |
| B7 | One at a time: opening a second closes the first without animation; `Open`/`Close`/`Toggle` while any menu animates return false; an open or closed menu's `Open`/`Close` returns false | controller, M8, M18 |
| B8 | Disabled: `IsEnabled` false closes it at once with `IonDidClose` `gesture` and no will event, and `Open()` returns false; cancelled mid-animation returns false | `updateState`, M10 |
| B9 | Backdrop click: a click outside the inner closes it with `backdrop`, handled; a click inside doesn't; nothing within 100 ms of a gesture's end; push and reveal: a click on the content's place (not hit-testable) closes it | `onBackdropClick`, M7 |
| B10 | The content while open: not hit-testable, offscreen to automation; back after the close | `beforeAnimation`, M2 |
| B11 | Escape on the menu closes it with `backdrop` | `onKeydown` |
| B12 | Focus: `SetOpen` clears it; the menu has it once open; the trap keeps Tab in the menu; an overlay on top keeps its own; not given back after | `afterAnimation`, `handleFocus`, M11 |
| B13 | Back: with `AvonicApp`, an open menu registers at 99 and closes; an overlay at 100 comes first; without one, back closes it unless an overlay takes it | controller, hardware-back-button.js |
| B14 | Swipe: from within `MaxEdgeStart` of the window's edge on the menu's side it opens, following the finger linearly, past half or a flick completing; from further in nothing; the end side; `SwipeGesture` false or disabled: nothing; another menu open: nothing; a modal presented: nothing | `canStart`, `onMove`, `onEnd`, M12 |
| B15 | Swipe an open menu closed, from anywhere; a short one goes back open (`IonWillClose` then `IonDidOpen`) | M13, M14 |
| B16 | Type change: the animation made again; config `MenuType` read at first attach when `Type` is null | `typeChanged`, `connectedCallback` |
| B17 | Detach while open: closed without animation, unregistered, the content's state gone | `disconnectedCallback`, M17 |
| B18 | `ContentId` not found: error logged, not registered (the button hidden), but `Open()` still opens it | M16 |
| B19 | Controller lookup: `start`, `end`, a `MenuId`, none; enabled first; `Enable`, `SwipeGesture`, `IsOpen`, `IsEnabled`, `GetOpen`, `GetMenus`, `IsAnimating`; `RegisterAnimation` replaces a type's | index.js |
| B20 | Button: a click toggles its menu (`Menu` by id or side), Enter and Space too; hidden when `AutoHide` and its menu isn't active, following `MenuChanged`; shown with `AutoHide` false; disabled: no click, 0.5 | menu-button.js, M15 |
| B21 | Button look: 48 x 48, the icon 24 centred, `menu-sharp` or `AvonicConfig.MenuIcon`; a slotted content replaces it; colour inherited, the toolbar's, `Color`'s base; focused overlay 0.12, hover 0.04; ripple unbounded | menu-button.md.css, M1, M15 |
| B22 | Button automation: a button named `menu`, or the app's name; invoking it toggles | `aria-label` |
| B23 | Toggle: a click inside (a stock `Button`, an `AvonicButton`, from a key) toggles; hidden like the button | menu-toggle.js |
| B24 | Menu automation: a `Navigation` landmark named `menu`, or the app's name | `render()` |

## Deliberate differences

- **M9's race.** Ionic's controller checks `isAnimating` before an `await`, so two menus asked to open in the same tick both open.
  Here the check and the start are synchronous, so the second is refused, as the check means it to be.
- **`Type` set to null after load** is Ionic's `undefined`: opening throws "animation not registered", as Ionic's controller does.

## Extensions

None. `MenuController` is Ionic's `menuController`; `MenuAnimationContext` is its `MenuI` as the animations see it.

## Dependencies

`AvonicBackdrop`, `AvonicAnimation`, `Gesture`, `GestureController`, `HardwareBackButton`, the overlay host's focus helpers,
`AvonicIcon`, `AvonicRippleEffect`, `Activatable`, `FocusVisible`, `NativeButton`, `ColorPalette` (all done). `AvonicToolbar`,
`AvonicButtons`, `AvonicContent`, `AvonicHeader`, `AvonicList`, `AvonicItem` in the sample (done). `ion-split-pane` (#78) isn't.
