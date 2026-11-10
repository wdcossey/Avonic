# ion-segment family — Plan

A row of tabs and the pages they switch: `ion-segment` holds `ion-segment-button`s, one of them checked, with an indicator
under it that slides from the last one; `ion-segment-view` holds `ion-segment-content`s side by side in a horizontal scroller,
and a button with a `contentId` pages it. One family, one PR (#28).

Measurements are from real Ionic 8.8.1 in Chromium, md, 390 px wide, wrapped in `ion-app`. The page is in
[COMPARISON.md](COMPARISON.md). The behaviours were measured with throwaway Playwright scripts on the same page: mouse taps and
drags, held presses, the keys, the indicator's transform sampled during its transition, and `scrollLeft` sampled every frame
during each smooth scroll.

## Fidelity checklist

`node tools/ionic-api.js ion-segment`, `ion-segment-button`, `ion-segment-content`, `ion-segment-view`.

### ion-segment: props (7)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`. Its buttons get `:in-segment-color` |
| [x] | `disabled` | `IsEnabled` | Mapping rule. Writes every button's `IsEnabled`, or the view's when there's one, as `disabledChanged` does |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `scrollable` | `Scrollable` (`bool`) | `:scrollable`. Columns at their content's width, scrolling sideways; turns the gesture off |
| [x] | `selectOnFocus` | `SelectOnFocus` (`bool`) | The arrows, Home and End check the button they focus |
| [x] | `swipeGesture` | `SwipeGesture` (`bool`, `true`) | The drag gesture |
| [x] | `value` | `Value` (`object?`) | Null is `undefined`. Two-way, as the user changes it |

### ion-segment: events (1), no methods, slots or parts

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionChange` | `IonChange` (`IonChangeEvent`, bubbles, `SegmentChangeEventArgs.Value`) | On a user's change only. Its internal `ionSelect` is a C# event the buttons listen to; `ionStyle` is raised as `IonStyle` |

The segment's children: `core.json` lists no slot, so they're **`Items`**, and the segment is an `ItemsControl`.

### ion-segment: CSS variables (1)

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--background` | `Avonic.Segment.Background` | `transparent` |

### ion-segment-button: props (6)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `contentId` | `ContentId` (`string?`) | The `Name` of an `AvonicSegmentContent`, as Ionic's is its `id`. A button with one can't be disabled: it's enabled on attach, with a warning |
| [x] | `disabled` | `IsEnabled` | Mapping rule. Opacity 0.3; skipped by the keys and the drag |
| [x] | `layout` | `Layout` (`SegmentButtonLayout`: `IconBottom`, `IconEnd`, `IconHide`, `IconStart`, `IconTop`, `LabelHide`; default `IconTop`) | `:layout-icon-top` and so on |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `type` | — | Not applicable: HTML form. It's the native button's `type`, which only a `<form>` reads |
| [x] | `value` | `Value` (`object?`) | A unique `ion-sb-N` until it's set, as Ionic's field initialiser gives |

### ion-segment-button: parts (3), no events, methods or slots

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `native` | `PART_Native` | The button's box: margins, colour, the overlay and the ripple |
| [x] | `indicator` | `PART_Indicator` | Along the bottom; what slides. Opacity 0 unless checked |
| [x] | `indicator-background` | `PART_IndicatorBackground` | The bar inside it: height, colour, transform and shadow |

Also `PART_Overlay` (`.button-native::after`), `PART_Ripple` and `PART_Inner` (`.button-inner`, a `SegmentButtonPanel`).
`core.json` lists no slot, so the label and the icon are **`Items`**: the button is an `ItemsControl` whose panel is
`.button-inner`'s flex box.

### ion-segment-button: CSS variables (28)

All `Avonic.SegmentButton.<Name>`. md defaults from `segment-button.md.css`, resolved in Chromium (`ref/README.md`):

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--background` | `Background` | `none`: unset |
| [x] | `--background-checked` | `BackgroundChecked` | `none` |
| [x] | `--background-focused` | `BackgroundFocused` | unset: `var(--color-checked)` |
| [x] | `--background-focused-opacity` | `BackgroundFocusedOpacity` | 0.12 |
| [x] | `--background-hover` | `BackgroundHover` | unset: `var(--color-checked)` |
| [x] | `--background-hover-opacity` | `BackgroundHoverOpacity` | 0.04 |
| [x] | `--border-color` | `BorderColor` | unset |
| [x] | `--border-radius` | `BorderRadius` | unset: 0 |
| [ ] | `--border-style` | — | Avalonia draws solid borders only. A theme sets `BorderWidth` to 0 for `none` |
| [x] | `--border-width` | `BorderWidth` | unset: 0, as `border-style` is `none` |
| [x] | `--color` | `Color` | `rgba(text, 0.6)` |
| [x] | `--color-checked` | `ColorChecked` | `var(--ion-color-primary)` |
| [x] | `--color-focused` | `ColorFocused` | unset: the host's colour |
| [x] | `--color-hover` | `ColorHover` | unset: `var(--color)` |
| [x] | `--indicator-box-shadow` | `IndicatorBoxShadow` | `none` |
| [x] | `--indicator-color` | `IndicatorColor` | unset: `var(--color-checked)` |
| [x] | `--indicator-height` | `IndicatorHeight` | 2 |
| [x] | `--indicator-transform` | `IndicatorTransform` | `none` |
| [x] | `--indicator-transition` | `IndicatorTransition` | `transform 250ms cubic-bezier(0.4, 0, 0.2, 1)`: a `Transitions` with one `TransformOperationsTransition`, whose `Duration`, `Delay` and `Easing` the segment runs on `AvonicConfig`'s clock |
| [x] | `--margin-bottom`, `--margin-end`, `--margin-start`, `--margin-top` | `MarginBottom`, … | unset: 0 |
| [x] | `--padding-bottom`, `--padding-end`, `--padding-start`, `--padding-top` | `PaddingBottom`, … | 0, 16, 0, 16 |
| [x] | `--transition` | `Transition` | `color 0.15s linear, opacity 0.15s linear`: a `Transitions` on the part |

`--ion-toolbar-segment-*`, which the button reads in a toolbar, are `Avonic.ToolbarSegment*Brush` keys, unset, in this
family's tokens, as only it reads them.

### ion-segment-view: props (2), events (1)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `disabled` | `IsEnabled` | Mapping rule. No swiping; opacity 0.3 |
| [x] | `swipeGesture` | `SwipeGesture` (`bool`, `true`) | `false`: no swiping. Code still pages it |
| [x] | `ionSegmentViewScroll` | `IonSegmentViewScroll` (`SegmentViewScrollEventArgs`: `ScrollRatio`, `IsManualScroll`) | On every scroll |

Its internal `setContent(id, smoothScroll)` is the internal `SetContent`. Its children are `Items`, as `core.json` lists no slot.

### ion-segment-content

No props, events, methods, slots, parts or variables. `AvonicSegmentContent`, a `ContentControl`: a page as wide as the view,
scrolling vertically (`overflow-y: scroll`) with no scroll bar.

## Behaviours

Each becomes a test. "Measured" is from the scripts above.

### The segment

| # | Behaviour | Source |
|---|---|---|
| S1 | A button is checked when its `Value` equals the segment's, by `Equals` as `===`; both can change in code, with no `IonChange` | `valueChanged`, `updateState` on `ionSelect` |
| S2 | A tap: the press captures (threshold 0); at the release the button under the pointer's x, at the checked button's middle y, is checked, and `IonChange` is raised once. Nothing changes at the press | `onStart`, `onEnd`, `setNextIndex(…, true)`; measured `after down a`, `after up b` |
| S3 | A drag from the checked button: `:segment-activated` on the segment while held; the checked button moves one to the left or right whenever the pointer passes its edge, but never onto a disabled one; `IonChange` once, at the release | `activate`, `setNextIndex`; measured x 196 → `b`, 190 → `a`; across `Two` (disabled) nothing moves until the release |
| S4 | A drag from an unchecked button moves nothing; the release checks the button under its x, even below the segment | measured `unchecked drag mid a`, `end b` |
| S5 | A release beyond the segment's end changes nothing | measured `drag out` |
| S6 | With nothing checked, the press checks the pressed button, and `IonChange` comes at the release | `activate`, `onEnd` |
| S7 | A press on a disabled button does nothing | `activate`'s target check; measured `tap disabled` |
| S8 | With the gesture off (`Scrollable`, `SwipeGesture="False"`): a click checks the button, with the indicator's slide, and raises `IonChange` | `onClick` |
| S9 | The indicator slides: the new one starts at `translateX(previous.left − current.left) scaleX(previous.width / current.width)` from its left and transitions to none by `--indicator-transition`: 250 ms, `cubic-bezier(0.4, 0, 0.2, 1)` | `checkButton`; measured −185.3 at 54 ms, −75.2 at 112, −24.2 at 162, −9.2 at 208, 0 at 270 |
| S10 | Keys on a focused button: Right and Left focus the next or previous enabled button, wrapping, swapped in RTL; Home and End the first and last; Enter and Space check the focused one, with `IonChange`. With `SelectOnFocus`, the first four check too. Each button is a tab stop | `onKeyDown`, `getSegmentButton`; measured `right`, `right (wrap)`, `enter`, `selectOnFocus left` |
| S11 | `Scrollable`: each column is its content's width at least (buttons' `min-width: auto`), sharing what's left as `1fr`; it scrolls sideways; the checked button is centred at load, at once, and smoothly when it changes | `segment-scrollable`, `scrollActiveButtonIntoView`; measured `s4` centred at `scrollLeft` 92 |
| S12 | A smooth scroll takes √distance / 60 s, `cubic-bezier(0.42, 0, 0.58, 1)`, as Chromium's programmatic one | measured 84 px in ~150 ms, 390 in ~330, 780 in ~460; the fit is 2.5 px off on average |
| S13 | Not scrollable: columns `minmax(auto, 360px)`, each from its button's `min-width` (90), growing equally to 360, centred; too many overflow on both sides | measured: 2 → 195 each; 1 → 360 at x 15; 5 → 90 each from x −30; `min-width: 150` → 225 and 165 |
| S14 | `IsEnabled="False"` disables every button, and enabling it again enables every button, as Ionic writes each button's `disabled` | `disabledChanged` |
| S15 | The button after the checked one has `:after-checked`, set at load and when the indicator slides | `setCheckedClasses` |
| S16 | In a toolbar: `:in-toolbar`, `:in-toolbar-color`, and the toolbar's min height (56) | `hostContext`, `:host(.in-toolbar)`; measured 56 high |
| S17 | Buttons added later are checked if their value matches | `onSlottedItemsChange` |
| S18 | It raises `IonStyle` with `segment`, and counts as an input to an item | `emitStyle`; item.js `totalNestedInputs` |

### The button

| # | Behaviour | Source |
|---|---|---|
| B1 | `Layout`: icon-top is a column, icon-bottom a reversed column, icon-start a row, icon-end a reversed row; icons go first in any of them (`order: -1`); icon-hide hides icons, label-hide labels | `.button-native` flex, `::slotted(ion-icon)` |
| B2 | `:has-label`, `:has-icon`, `:has-label-only`, `:has-icon-only` from an `AvonicLabel` or `AvonicIcon` inside it | `hasLabel`, `hasIcon` |
| B3 | Activatable, instantly, with a ripple; the capture at the press cancels it, so a held press doesn't keep it | `ion-activatable-instant`; measured: ripple at 0.16, gone by 600 ms while held |
| B4 | Focusable, with `:ion-focused` from the keyboard | `ion-focusable` |
| B5 | Disabled, or in a disabled segment: opacity 0.3, no input | `.segment-button-disabled` |
| B6 | Automation: a tab item, selected when checked, named by its text | `role="tab"`, `aria-selected` |
| B7 | Labels are one line with an ellipsis, 22 high, upper case; the icon 24 | `::slotted(ion-label)`, `::slotted(ion-icon)` |

### The view

| # | Behaviour | Source |
|---|---|---|
| V1 | `SetContent`: scrolls to the content's index × its width, smoothly (S12) or at once; no hit testing while it does, until 100 ms after the last scroll | `setContent`, `segment-view-scroll-disabled`, `checkForScrollEnd`; measured class on during, off after |
| V2 | `IonSegmentViewScroll` on each scroll: `ScrollRatio` = offset / (extent − viewport), `IsManualScroll` false during `SetContent` | `handleScroll` |
| V3 | A user's scroll moves the segment's checked button to `round(ratio × (n − 1))`, with `IonChange`, without scrolling back; the first one always raises it | `handleSegmentViewScroll`, `lastNextIndex`; measured `manual 600` → `v3` |
| V4 | A segment whose buttons have a `ContentId` finds the view; at load the view jumps to the checked button's content; when the value changes, by tap, drag, key or code, it scrolls smoothly; a null value becomes the first button's | `getSegmentView`, `updateSegmentView`, `valueChanged`; measured `view click`, `view programmatic` |
| V5 | A touch drag pages, one content at a time, snapping to the nearest; `SwipeGesture="False"` or disabled stops it, and code still pages | `scroll-snap-type: x mandatory`, `scroll-snap-stop: always`, `overflow-x: hidden` |
| V6 | Each content is the view's width and the view's height, and scrolls vertically | `segment-content.css` |
| V7 | Disabling the segment disables its view | `disabledChanged` |

### The look

| # | Behaviour | Source |
|---|---|---|
| L1 | Label only: 48 high, the label 22 high at y 13; icon and label: 70; icon-start: 48 with the label 8 after the icon | measured |
| L2 | Unchecked text `rgba(text, 0.6)`, checked the primary; the indicator 2 high in `--indicator-color` | measured |
| L3 | `Color` on the segment: checked text, the indicator and the ripple in its base | `.in-segment-color` |
| L4 | In a coloured toolbar: text the contrast at 0.6, checked the contrast, indicator the contrast | `.in-toolbar-color`; measured `t1` white |
| L5 | Hover: an overlay of `--background-hover` at 0.04; focus: `--background-focused` at 0.12 | `::after` |
| L6 | RTL mirrors the row | logical CSS |

## Design

- **`AvonicSegment : ItemsControl`**, with `SegmentPanel`, Ionic's `display: grid` with one row. The panel works out columns as
  Chromium did above: `minmax(auto, MaxColumnWidth)` from each button's `MinWidth`, or `minmax(min-content, 1fr)` when
  scrollable. `MaxColumnWidth` is infinite in the stub and 360 in the theme, as `minmax(auto, 360px)` is md's. The template
  holds it in a `ScrollViewer` with no bars, which scrolls only when `Scrollable`.
- **The gesture** is `Gesture.Create` with Ionic's config: `segment`, priority 100, threshold 0, enabled while not
  scrollable, enabled and swiping. "Under the pointer" is a hit test at the checked button's middle y.
- **The indicator's slide** is a CSS transition, run by the segment on `AvonicConfig`'s clock, on `PART_Indicator`'s
  `RenderTransform`, with its origin at the left. The duration, delay and easing are read from
  `Avonic.SegmentButton.IndicatorTransition` on the button; with none, it jumps, as `transition: none` does.
- **The smooth scrolls** (the scrollable segment and the view) are one helper on `AvonicConfig`'s clock (S12). A user's
  scroll in the middle stops it, as in Chromium.
- **`AvonicSegmentButton : ItemsControl`**, with `NativeButton` for the click when the gesture is off (no keys: the segment
  takes them, as Ionic's `onKeyDown` prevents the native button's), `Activatable` (instant) and `FocusVisible`. Its panel,
  `SegmentButtonPanel`, is `.button-inner`'s flex box for the six layouts.
- **`AvonicSegmentView : ItemsControl`**, a `ScrollViewer` with mandatory single snap points from `SegmentViewPanel`, which
  sizes each content to the view. Avalonia's `ScrollGestureRecognizer` is the touch scroll.
- **Slotted rules** (the label's and icon's margins and sizes, `display: none`) go in `Styles/SegmentButton.axaml`. The
  scrollable segment's `min-width: auto` on its buttons is the button's own `:in-segment-scrollable`, which the segment sets,
  so that it costs nothing in the style budget.

## Deliberate differences (each recorded in COMPARISON.md)

- **A vertical drag that starts on a segment doesn't scroll the page around it.** The gesture captures at the press, as
  Ionic's does with threshold 0, and Avonic's gesture keeps a scroller off a captured pointer, where Chromium scrolls anyway.
- **The indicator's starting transform is applied at once,** not in the next frame (`writeTask`), so that no frame shows it at
  its end before it starts.
- **`prefers-reduced-motion`** isn't read: Avalonia has no such setting.
- **`Scrollable` changing after load turns the gesture on or off.** Ionic has no watcher on it, so its gesture keeps what
  it read at load.
- **Snapping after a fling** is Avalonia's `MandatorySingle`, not Chromium's.

## Extensions

None.

## Dependencies

`ion-label`, `ion-icon`, `ion-ripple-effect`, `ion-toolbar`, `ion-item` (done); the gesture utility (done).
