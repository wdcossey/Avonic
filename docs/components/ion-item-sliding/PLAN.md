# ion-item-sliding family — Plan

`ion-item-sliding`, `ion-item-options` and `ion-item-option` ([#31](https://github.com/wdcossey/Avonic/issues/31)): an item that
slides sideways under a finger to show buttons behind it. Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 × 844,
with touch from CDP's `Input.dispatchTouchEvent`, wrapped in `ion-app` so that `tap-click` runs. The reference page is in
[`ref/`](ref/), with its command in [`COMPARISON.md`](COMPARISON.md).

## Fidelity checklist

### ion-item-sliding — `node tools/ionic-api.js ion-item-sliding`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `disabled` | `Disabled`, default `false` | **Not `IsEnabled`**, which would disable and fade the item and the options: Ionic's only turns the gesture off, and the item stays clickable. A `disabled` that doesn't disable the children, as `AvonicReorderGroup.Disabled` |
| [x] | `ionDrag` | `IonDrag` (`ItemSlidingDragEventArgs`: `Amount`, `Ratio`) | Bubbles, as Ionic's does |
| [x] | `close()` | `Close()` | Sync: nothing to wait for |
| [x] | `closeOpened()` | `CloseOpened()` → `bool` | Closes the one open sliding item, wherever it is |
| [x] | `getOpenAmount()` | `GetOpenAmount()` → `double` | |
| [x] | `getSlidingRatio()` | `GetSlidingRatio()` → `double` | |
| [x] | `open(side)` | `Open(ItemOptionsSide? side = null)` | Returns at once, and opens at the next frame, as Ionic's `requestAnimationFrame` does |

No slots, parts or variables. Its children (an item and one or two options) have no slot in `core.json`, so they're **`Items`**,
as `ion-list`'s are.

### ion-item-options — `node tools/ionic-api.js ion-item-options`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `side` | `Side` (`ItemOptionsSide`: `End` default, `Start`) | `:options-start` or `:options-end`: the physical side, as Ionic's `item-options-start` and `-end` classes are, swapped in RTL |
| [x] | `ionSwipe` | `IonSwipe` (`ItemOptionsSwipeEventArgs`: `Side`) | Raised on the options, bubbling |
| [x] | `fireSwipeEvent()` (`@internal`) | `internal FireSwipeEvent()` | The sliding item calls it |

No slots, parts or variables. The options are **`Items`**.

### ion-item-option — `node tools/ionic-api.js ion-item-option`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `disabled` | `IsEnabled` | Mapping rule. The content at opacity 0.5, the background not, and no input |
| [x] | `expandable` | `Expandable` (`bool`) | `:expandable`. Grows over the row on a full swipe |
| [ ] | `download`, `rel`, `target` | — | Not applicable: they only apply with `href`, as on `ion-button` |
| [ ] | `href` | — | Out of scope: router, as on `ion-button`. Use `Click` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `type` | — | Not applicable: HTML form, as on `ion-button` |
| [x] | default slot | `Content` (+ `ContentTemplate`) | |
| [x] | slot `top`, `bottom`, `start`, `end`, `icon-only` | `TopContent`, `BottomContent`, `StartContent`, `EndContent`, `IconOnlyContent` (+ `…Template`) | Several slots, so a `TemplatedControl` with a `ContentPresenter` each |
| [x] | part `native` | `PART_Native` | |
| [x] | part `inner` | `PART_Inner` | |
| [x] | part `container` | `PART_Container` | |
| [x] | `--background` | `Avonic.ItemOption.Background` | Unset → primary |
| [x] | `--color` | `Avonic.ItemOption.Color` | Unset → primary contrast |

## Behaviours

Each becomes a test. Read from `item-sliding.js`, `item-options.js` and `item-option.js`, and measured.

### Sliding item

| # | Behaviour | Source |
|---|---|---|
| S1 | A horizontal drag (gesture `item-swipe`, priority 100, threshold 5) moves the item by `initialOpenAmount - deltaX`, from the capturing move: measured, a drag from 300 captured at 288 and the item was at −80 at 200 | `onStart`, `onMove` |
| S2 | It only opens towards a side that has options: with only end options the amount stays ≥ 0, with only start options ≤ 0; with none the drag does nothing | `onMove` `sides` |
| S3 | Past the options' width it's elastic: `width + (amount − width) × 0.55`. Measured: 151 wide, 155.95 at a raw 160 | `ELASTIC_FACTOR` |
| S4 | Past the width + 30 it's a full swipe (`SwipeEnd`/`SwipeStart`); measured from 181 | `SWIPE_MARGIN` |
| S5 | On release it rests open at the options' width, or closed: `(!fast && inCloseZone) \|\| (resetDirection && fast)`, fast being over 0.3 px/ms, the close zone under half the width | `onEnd`, `swipeShouldReset` |
| S6 | A full swipe released raises `IonSwipe` on that side's options, after it rests open at their width (not deleted, not closed), measured | `onEnd` |
| S7 | `IonDrag` with `Amount` and `Ratio` on every change of the amount, except to 0 | `setOpenAmount` |
| S8 | The item eases to where it rests over 500 ms, `cubic-bezier(0.36, 0.66, 0.04, 1)`; while dragged it follows at once. Measured on open: −80.2 at 89 ms, −134.6 at 189, −151 at 489 | `.item-sliding-active-slide .item`, `style.transition` |
| S9 | Closing: the options stay shown for 600 ms, taking no input, and the gesture is off until then; then everything resets. Measured: closing class gone and shown options gone at ~617 ms | `setOpenAmount(0)`, `tmr` |
| S10 | While open or closing the item takes no input: a tap on it clicks nothing in it, measured (the click went to `ion-item-options`) | `pointer-events: none` |
| S11 | Only one is open at a time: pressing another sliding item closes the open one, and the tap still clicks the other's item, measured | `canStart`, `openSlidingItem` |
| S12 | A press within 15 px of the window's left edge (right in RTL) doesn't start, for swipe-to-go-back | `canStart` |
| S13 | `Open(side)` opens that side's options, or the first found, at the next frame, with the 500 ms ease; nothing if that side is already open or has no options; it closes any other first | `open()` |
| S14 | `Close()` closes it; `CloseOpened()` closes whichever is open and says whether one was | `close()`, `closeOpened()` |
| S15 | `GetOpenAmount()` is the amount; `GetSlidingRatio()` the amount over that side's width, negative for start | `getSlidingRatio` |
| S16 | Options: hidden (`display: none`) until the item is active; then only the open side's are shown, as wide as the item, behind it | `item-options.md.css`, `item-sliding.css` |
| S17 | In RTL, start and end swap: `Side="End"` options are on the left, and a drag right opens them | `isEndSide`, `:dir(rtl)` rules |
| S18 | `Disabled` stops the gesture, and nothing else: the item stays enabled and clickable, and the close's timer leaves it off | `disabledChanged`, `setOpenAmount` |
| S19 | During the drag, an enclosing `AvonicContent`'s `ScrollY` is off, and put back as it was at the end | `disableContentScrollY` |
| S20 | Options and the item are found again when the children change (`MutationObserver`) and at each drag (`onStart`) | `updateOptions`, `onStart` |

### Options and option

| # | Behaviour | Source |
|---|---|---|
| O1 | Options are a row packed to their side, as tall as the item: measured end options 238.88 (Favorite 82.42), 321.3 (Delete 68.7) | `ion-item-options`, `justify-content` |
| O2 | A full swipe moves an expandable option to the outer end and grows it by the row's width (`padding-left: 100%`) over 600 ms `cubic-bezier(0.65, 0.05, 0.36, 1)`, its content staying put; it snaps back when the swipe ends. Measured: 118 px in, then 390 | `.item-option-expandable` rules |
| O3 | In a list with `Lines="Full"`, options have a 1 px bottom border in the item's border colour, and end options with `Inset` | `.list-md-lines-full .item-options-md` |
| O4 | The option: font 14, weight 500, uppercase; `0.7em` padding each side; content centred in a column `top`, then a row `start`, `icon-only`, default, `end`, then `bottom`. Start's margin-end 5, end's margin-start 5, icon-only 10 each side at 1.8em (25.2). Measured: icon-only option 64.78 wide, top-icon "Star" 54.06 | `item-option.md.css` |
| O5 | Primary background and contrast text; `Color` uses its base and contrast | `:host`, `:host(.ion-color)` |
| O6 | A click on release, Enter and Space, as a native button: `Click` and `Command` (extensions, as `AvonicFabButton`'s); a ripple in the text colour on press | `<button>`, `ion-activatable`, `ion-ripple-effect` |
| O7 | Disabled: content at 0.5, no input, no focus | `.item-option-disabled` |
| O8 | Automation: a button, named by its text | native `<button>` |
| O9 | The outermost option is padded by the safe area on its physical side: the left options' first (last in RTL) by `--ion-safe-area-left`, the right options' last (first in RTL) by `-right` | `item-options.md.css` |

## How it maps to Avalonia

- **Structure.** `AvonicItemSliding` and `AvonicItemOptions` are `ItemsControl`s whose children are their own containers.
  `ItemSlidingPanel` lays out the sliding item: what's in the flow (the item) stacked, the options out of it (`position:
  absolute`), as tall as the flow, at their logical side, so that RTL mirrors them; `ItemOptionsPanel` lays out the row.
- **State.** Ionic's `state` flags are ported as they are, with `setOpenAmount`, `onEnd` and the timer line for line, so its quirks
  come too (an `Open` of the other side leaves the gesture off until the next close, as Ionic's cleared timer does).
- **The item's movement.** `style.transform` is inline in Ionic, which beats any stylesheet, so it's the item's local
  `RenderTransform`, a `TranslateTransform` cleared when it rests at 0. The 500 ms transition runs on `AvonicConfig`'s clock, from
  the value shown, as the mapping rule for a transition on a computed value says. Its `pointer-events: none` and `z-index: 2` are
  `IsHitTestVisible` and `ZIndex` on the item at style priority, through `StyleValue`, bound when the item is found.
- **Options shown.** `display: none` and `visibility: hidden` both become `IsVisible` false at style priority, as neither takes
  space, input or focus here (the hidden side is under the item). `calculateOptsWidth` shows them to measure, as Ionic does.
- **Expandable.** `ItemOptionExpander`, a decorator between the option's host border and `PART_Native`, adds the expansion on the
  inner side, so the background covers it and the content stays where it was.
- **The one open item** is a static, per process, as `GestureController` is; Ionic's is per document.
- **Physical sides.** Ionic's `leftOptions` and `rightOptions` and the drag are physical; the panel works in Avalonia's logical
  coordinates, which RTL mirrors, so a physical right is the logical end in LTR and the logical start in RTL.

## Visual states and keys

`Avonic.ItemOption.Background` and `.Color`, `CssUnset`, read through `Css.Var` with primary and primary contrast after them;
`:ion-color` uses the palette. The option's font, padding and slot margins are literals in the theme, as Ionic has no variables
for them. The options' line is `Avonic.ItemBorderColor`'s chain, as the item's is.

## Dependencies

`AvonicItem` (#6), `AvonicList` (#9), `Gestures` (#4), `AvonicRippleEffect`, `AvonicContent`: all merged.

## Extensions

- `AvonicItemOption.Click`, `Command`, `CommandParameter`: the native button's click, as on `AvonicFabButton`.
- `AvonicItemOption.TextTransform`: `text-transform`, as on `AvonicButton`; the theme sets `Uppercase`.
