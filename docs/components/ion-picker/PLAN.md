# ion-picker family — Plan

The inline wheel picker: `ion-picker` holds `ion-picker-column`s side by side, each a vertical scroll-snap scroller of
`ion-picker-column-option`s, with the centred option selected. A highlight bar sits behind the centre row and two fades cover the
rows above and below it. One family, one PR (#37). `ion-datetime` (#46) is built on it, so the column's API and behaviour, and the
picker's internal numeric input mode, follow Ionic's `.js` exactly.

Measurements are from real Ionic 8.8.1 in Chromium, md, 390 px wide, wrapped in `ion-app`, with throwaway Playwright scripts: the
boxes and colours, `scrollTop` at load and sampled every frame during a programmatic change, a wheel notch, a click on an option, the
keys, a value with no option, a click on a disabled option and a disabled column. The page is in [COMPARISON.md](COMPARISON.md).

## Fidelity checklist

`node tools/ionic-api.js ion-picker`, `ion-picker-column`, `ion-picker-column-option`.

### ion-picker: props (1), no events, methods, slots or parts in `core.json`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [ ] | `mode` | — | Not exposed (ADR 0001) |

Its children are columns. `core.json` lists no slot, so they're **`Items`**, and the picker is an `ItemsControl`.

Internal members, which `ion-datetime` uses: `exitInputMode()` → `internal ExitInputMode()`; `ionInputModeChange` → an internal C#
event, `InputModeChanged`, as the mapping does for `@internal` events between our own controls.

### ion-picker: CSS variables (3)

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--fade-background-rgb` | `Avonic.Picker.FadeBackgroundRgb` | unset; falls back to `--background-rgb`, then `--ion-background-color-rgb` (`Avonic.BackgroundColor`) |
| [x] | `--highlight-background` | `Avonic.Picker.HighlightBackground` | unset, so transparent (measured `rgba(0, 0, 0, 0)`) |
| [x] | `--highlight-border-radius` | `Avonic.Picker.HighlightBorderRadius` | `8px` |

`--background-rgb` isn't in `core.json`: it's what an overlay around the picker sets (`ion-picker-legacy`, a modal). It's not a key.

### ion-picker-column: props (4)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`, `primary` at style priority, as Ionic's default is `'primary'`. Drives the focused background |
| [x] | `disabled` | `IsEnabled` | Mapping rule. It stops the column's own scrolling, keys and focus, and its options' input (`pointer-events: none` on them). See "Decided" for the prefix and suffix |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `value` | `Value` (`object?`) | `string \| number`; null is `undefined`. Compared with `Equals`, as `===`. Two-way, as the user changes it |

Internal: `numericInput` → `internal NumericInput`; `setValue()` and `scrollActiveItemIntoView()` → internal methods.

### ion-picker-column: events (1), methods (1), slots (2)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionChange` | `IonChange` (`IonChangeEvent`, bubbles, `PickerColumnChangeEventArgs.Value`) | From the user only: a scroll that comes to rest, a click, a key, typing. Not for a programmatic `Value` |
| [x] | `setFocus()` | `SetFocus()` | Focuses the column. Ionic 8.8.1's focuses `assistiveFocusable`, which nothing sets, so it does nothing: see "Decided" |
| [x] | `prefix` slot | `PrefixContent` (+ `PrefixContentTemplate`) | Left of the options |
| [x] | `suffix` slot | `SuffixContent` (+ `SuffixContentTemplate`) | Right of the options |

The options are the default slot, which `core.json` doesn't list: **`Items`**, and the column is an `ItemsControl`.

### ion-picker-column-option: props (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | As the column's, `primary` by default. The active option's text is its base colour |
| [x] | `disabled` | `IsEnabled` | Mapping rule. Opacity 0.4, not snapped to, skipped by the keys, never active |
| [x] | `value` | `Value` (`object?`) | `any` |

Its default slot, the text, is **`Content`**. `aria-label` is `AutomationProperties.Name`.

## Behaviours (the test list)

From `picker-column.js`, `picker-column-option.js` and `picker.js`; every number measured.

**Column**

| # | Behaviour | Ionic |
|---|---|---|
| C1 | Once it's visible, the column jumps to centre the option whose value is `value`, and that option is active (`option-active`): its text is its colour's base. No `ionChange`. Measured: `scrollTop` 121 for the 4th option, 34 × 3 + 19 | IO callback → `scrollActiveItemIntoView()`; `scroll-snap-align: center` |
| C2 | A programmatic `value` while visible scrolls smoothly to its option, √distance / 60 s, Chromium's ease in-out (measured 136 px in 200 ms), and no `ionChange` | `valueChange` → `scrollActiveItemIntoView(true)` |
| C3 | A scroll by the user moves the active option with the centre at every frame, and 250 ms after the last scroll sets `value` to the centred option and raises `ionChange` once. A disabled option at the centre is never active | `initializeScrollListener` |
| C4 | A scroll comes to rest centred on an enabled option. Measured: a 100 px wheel notch from 121 rests at 223, the nearest | `scroll-snap-type: y mandatory`; `scroll-snap-align: none` on disabled options unless the column is disabled |
| C5 | A click on an option sets `value` and raises `ionChange` at once, then scrolls it to the centre (measured: `ionChange` at 7 ms, scroll after) | option `onClick` → `setValue` |
| C6 | Disabled column: no scroll, click or key changes it; it can't be focused; its options are at opacity 0.4; the option with its value is active even if that option is disabled (measured) | `setValue`'s `disabled` check, `overflow-y: hidden`, `tabindex` |
| C7 | Disabled option: opacity 0.4; the keys skip it; not active | `activeItem`, `findNextOption` |
| C8 | Keys on the focused column: ArrowDown and ArrowUp move one enabled option, PageDown and PageUp five, Home and End the first and last option, each with `ionChange` at once. On a mobile platform the arrows and pages are reversed. Measured on desktop: 11 → ↓ 12 → ↑ 11 → PgUp 6 → Home 1 → End 12 → PgUp 7 | `onKeyDown` |
| C9 | A `value` with no option, set while the column is visible, becomes the first option's value at the next render, with `ionChange` (measured: at 71 ms). At load it stays (measured) | `componentDidRender` |
| C10 | The focused column's scroller has its colour's base at 0.2 behind it, where there's a hovering pointer (measured `rgba(0, 84, 233, 0.2)`) | `@media (any-hover: hover) :host(:focus)` |
| C11 | `SetFocus()` focuses the column | `setFocus` (see "Decided") |
| C12 | An option added later centres the column again on its value | option `componentDidLoad` |
| C13 | A column that isn't visible doesn't scroll for a new value, and centres when it's shown; its scroll listener starts a frame after it's shown, so that first jump isn't a user's scroll | `IntersectionObserver`, `raf` |

**Picker**

| # | Behaviour | Ionic |
|---|---|---|
| P1 | 200 high, columns side by side and centred, left to right whatever the direction; a highlight 34 high, 8 px in from each side, behind the centre row; fades over rows above (0–83) and below (116–200), white at 20 % / 30 % to clear at 90 % | `picker.md.css` |
| P2 | The first column's text is at the start, the last's at the end, and a column alone is centred | `::slotted(ion-picker-column:first-of-type)` |
| P3 | Numeric input (internal, for `ion-datetime`): a tap inside the highlight enters input mode for every numeric column, or the one tapped if it's the only one; digits typed select options (one column: "1", "12", a 1 s pause resets, more than two digits keep the last two; two columns: hours then minutes); a tap outside, Enter, or a user scroll that changes the selection exits it; a column focused by the keyboard enters it for that column. The active column has `picker-column-active` | `picker.js` |
| P4 | A touch on the picker doesn't reach a gesture outside it | `touchstart` `stopPropagation` |

## Visual states and keys

- Column: `font-size: 22px`, `height: 200px`; scroller `padding: 0 16px`, `min-width: 26px`, three empty 34 px rows above and below
  the options. Prefix and suffix: `padding: 0 16px`, `max-width: 200px`, one line with an ellipsis. Focused: `Avonic` colour base at
  0.2. Disabled: options at 0.4. These are literals, as Ionic has no variable for them.
- Option: 34 high, text centred in the row and aligned as the column's `text-align`; disabled 0.4; active: its colour's base.
- Picker: the three keys above; the fades use `Avonic.Picker.FadeBackgroundRgb`, falling back to `Avonic.BackgroundColor`.

## Avalonia mapping

- **Snapping** is Avalonia's `ScrollContentPresenter` with `VerticalSnapPointsType="Mandatory"` and `Center` alignment, from the
  column's panel, `PickerColumnPanel`, which stacks the options between the empty rows and gives a snap point at the centre of each
  enabled option (each option when the column is disabled), as `scroll-snap-align` does.
- **The scroll listener** is a change of the scroller's `Offset`, checked at the next `AvonicConfig.RequestFrame`, as the mapping does
  for `scroll`, and the 250 ms timeout is `AvonicConfig.Schedule`.
- **The centre option** is the option whose box holds the scroller's centre, as `elementsFromPoint` finds it.
- **Smooth scroll** is `SmoothScroll`'s timing (√distance / 60 s, `cubic-bezier(0.42, 0, 0.58, 1)`), measured again here, in a
  vertical copy in the picker's folder, as `SmoothScroll` is horizontal and internal to the segment's.
- **Visible** is `EffectiveViewportChanged` intersecting the column, as for `AvonicImg`.
- **Mobile** (`isPlatform('mobile')`, `any-pointer: coarse`) is Android or iOS; any-hover is the rest.

## Avonic extensions

None.

## Dependencies

`ColorPalette`, `AvonicConfig`'s clock, `Behaviors/StyleValue`. Nothing unbuilt.
