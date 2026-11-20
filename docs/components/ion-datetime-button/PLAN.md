# ion-datetime-button — Plan

`AvonicDatetimeButton`: Ionic's compact way to show a datetime on a phone, md. One or two buttons with the value of an
`AvonicDatetime` ([#46](../ion-datetime/README.md)) as its locale writes it; a tap presents the `AvonicModal` or `AvonicPopover`
the datetime is in ([#42](../ion-modal/README.md), [#43](../ion-popover/README.md)), showing only the half that was tapped. The text
comes from the datetime's own locale helpers (`DatetimeIntl`), as Ionic's comes from the datetime's `utils/format.js`.

Measured in real Ionic 9.0.11 in headless Chromium, md, in `ion-app`, on `render.js`'s phone (390 x 844 at 2x, mobile and touch,
Roboto from `tools/fonts/roboto/`), with the clock pinned to 2023-06-20 10:00 UTC (Playwright's `clock.setFixedTime`), the time
zone UTC and the locale `en-US`, and the datetime's value `2023-06-15T13:47:00` unless a case says otherwise. Each page is an
`ion-app` with a 16 px padded stage, the button first, and the datetime in an `ion-modal` or `ion-popover` with
`keep-contents-mounted`, or in the page. The raw output is [`ref/behaviour.json`](ref/behaviour.json) (73 cases: each button's
text, box, colour, `aria-expanded`, the host's classes, the datetime's presentation and box, the overlay's classes and wrapper box,
before and after each step, and the event timelines of a present and a dismiss), with frames in [`ref/frames/`](ref/frames/), and
[`ref/`](ref/)'s `render.js` output. The script isn't in the repo, as the swarm brief asks; COMPARISON.md describes it.

## Fidelity checklist

`node tools/ionic-api.js ion-datetime-button`.

### Props (4)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `color` | `Color` | `string?` | | `ColorPalette.ColorProperty.AddOwner`; `:ion-color`. Only the active button's text reads it |
| [x] | `datetime` | `Datetime` | `string?` | | The `Name` of the `AvonicDatetime`, looked up in the button's name scope, as an overlay's `Trigger` is (`OverlayTrigger`), at the first attach, as Ionic reads it once in `componentWillLoad` |
| [x] | `disabled` | `IsEnabled` | `bool` | `true` | The mapping rule |
| [ ] | `mode` | — | | | Not exposed (ADR 0001) |

### Events (0), methods (0)

None.

### Slots (2)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `date-target` | `DateTargetContent` (+ `DateTargetContentTemplate`) | In place of the date text, inside the date button, shown only while there's a date text |
| [x] | `time-target` | `TimeTargetContent` (+ `TimeTargetContentTemplate`) | The same for the time |

### Shadow parts (1)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `native` | `PART_DateButton`, `PART_TimeButton`, each a `DatetimeButtonNative` with the style class `native` | Both buttons carry the part, and a template can't name two parts alike, so they're named by their ids (`date-button`, `time-button`) and the part is the class, as the datetime's `navigation-button` is |

### CSS custom properties (0)

None. The look is `datetime-button.md.css`'s literals and globals, as resource keys (below).

## What Ionic does

Read in `datetime-button.js` and measured.

### Load (`componentWillLoad`)

1. **No `datetime`**: `printIonError("An ID associated with an ion-datetime instance is required to function properly.")`, and
   nothing renders: the host is 0 high (`noId`).
2. **`document.getElementById(datetime)`**; none: `"No ion-datetime instance found for ID 'x'."` (`wrongId`); not an
   `ion-datetime`: `"Expected an ion-datetime instance for ID 'x' but received 'div' instead."` (`notDatetime`). Nothing renders.
3. **An `IntersectionObserver` on the datetime, threshold 0.01**: `datetimeActive = isIntersecting`. It's never disconnected.
4. **The overlay**: `datetimeEl.closest('ion-modal, ion-popover')`, kept; it gets the class `ion-datetime-button-overlay`, never
   taken away.
5. **When the datetime is ready** (`componentOnReady`): `datetimePresentation = datetimeEl.presentation || 'date-time'`, read once
   and never again; `setDateTimeText()`; a listener for `ionValueChange`; `selectedButton` is `date` for `date-time`, `date`,
   `month-year`, `month`, `year`, and `time` for `time-date`, `time`.

### The text (`setDateTimeText`)

Reads the datetime's `value`, `locale`, `formatOptions`, `hourCycle`, `preferWheel`, `multiple` and `titleSelectedDatesFormatter`
each time:

- **The values**: `undefined`/`null` → none; an array as it is; a string → one. None → `[await datetimeEl.getDefaultPart()]`
  (today, snapped to the value lists: `hour-values="8,14" minute-values="20,40"` gives `8:20 AM`, `defaultValues`); else
  `parseDate(values)`, and if any doesn't parse, **nothing changes**, the old text stays (`setValue`: `nonsense` keeps
  `Feb 29, 2024`). The first parsed value is the one shown.
- **`date-time`, `time-date`**: the date `getLocalizedDateTime(locale, first, formatOptions?.date ?? { month: 'short', day:
  'numeric', year: 'numeric' })` (`Jun 15, 2023`; `{ weekday: 'short', month: 'long', day: '2-digit' }` → `Fri, June 16`) and the
  time `getLocalizedTime(locale, first, getHourCycle(locale, hourCycle), formatOptions?.time)` (`1:47 PM`; `{ hour: '2-digit',
  minute: '2-digit' }` → `09:05 AM`; a date alone → `Invalid Time`, `dateOnlyValue`; a time alone → `Jan 1, 2023`,
  `timeValueInDateTime`). With `preferWheel`, one date button with both, `"${date} ${time}"`: `Jun 15, 2023 1:47 PM`.
- **`date`**: with `multiple` and not exactly one value, `` `${n} days` `` (`3 days`; `0 days` with none), or
  `titleSelectedDatesFormatter(values)`, given the strings, whose exception is printed and leaves `n days` (`x_multiple`); else
  the date as above (one value: `Jun 5, 2023`).
- **`time`**: the time only. **`month-year`**: `formatOptions?.date ?? { month: 'long', year: 'numeric' }` (`June 2023`;
  `{ month: 'short', year: '2-digit' }` → `Jul 24`). **`month`**: `formatOptions?.time ?? { month: 'long' }` (`June`). **`year`**:
  `formatOptions?.time ?? { year: 'numeric' }` (`2023`). The last two read `time`: an Ionic bug (below).
- **Updated on `ionValueChange`** only: after a confirmed choice, a value set in code, `undefined` (today again). A selection waiting
  for Done changes nothing (`chooseButtons`). A change of `locale` or `presentation` alone changes nothing; the next value change
  uses the new locale and still the first presentation (`setValue`: `1. März 2024`, `07:05`, both buttons).

Measured in other locales (`locales`): `2023年6月15日` `13:47`, `15 Jun 2023` `13:47`, `15. Juni 2023` `13:47`,
`2023년 6월 15일` `오후 1:47`, `١٥ يونيو ٢٠٢٣` `١:٤٧ م`, `15 juin 2023` `13:47`; `hour-cycle="h23"`: `13:47`.

### The buttons (`render`)

- A `<button part="native" class="ion-activatable">` for each text that's truthy: `#date-button` with
  `<slot name="date-target">{dateText}</slot>`, `#time-button` with `time-target`, and an `ion-ripple-effect` in each in md.
  `aria-expanded` on both is `datetimeActive`; `disabled` on both is the host's.
- **The host** has `${selectedButton}-active` while `datetimeActive`, `datetime-button-disabled` while disabled, and the colour
  classes.
- **A click on the date button**: for `date-time` and `time-date`, unless `preferWheel`, the datetime's `presentation` becomes
  `date` if it isn't. `selectedButton = 'date'`; then the overlay is presented (below). **On the time button** the same with
  `time`, `preferWheel` or not (it has no time button with `preferWheel`). Measured: `modalDate` `date-time` → `date`,
  `modalTime` → `time`, `modalWheel` and `x_wheelTimeDate` unchanged. In the page (`inline`), with no overlay, the presentation
  still changes (`time`, the datetime 200 high) and the time button is the active one at once.
- **The overlay**: none, nothing. A popover: if the presentation changed, it waits for the datetime's `ionRender` (a frame after it
  renders the new presentation), then `present({ ...ev, detail: { ionShadowTarget: button } })`, so the popover is placed by the
  button tapped (`popoverDate`: `ionRender` at 70 ms, `ionPopoverWillPresent` at 72). A modal: `present()`.
- **Active**: the observer's first callback after the overlay shows (`modalDate`: `ionModalWillPresent` at 49 ms, `date-active` and
  `aria-expanded="true"` at 62), and off just after it has gone (`ionModalDidDismiss` at 1104, off at 1120). In the page, from the
  start. At 1% of the datetime's area: 4 px of its 391 in a scroller is active, 3 isn't, and back again the same (`y_ratio`).
  Hidden by `display: none` around it, inactive; shown, active (`inlineHidden`).

### The overlay (`core.css`)

```css
.ion-datetime-button-overlay { --width: fit-content; --height: fit-content }
.ion-datetime-button-overlay ion-datetime.datetime-grid { width: 320px; min-height: 320px }
```

The modal's wrapper is the datetime's size, centred: 320 x 344 at 35, 250 for `date` (`modalDate`), 181.56 x 200 at 104.22, 322 for
`time`, 350 x 200 for the wheels. The popover's content too, under the button tapped: 320 x 344 at 58, 47 (the date button at 93.78,
the popover kept 12 in from the right), the time 181.56 x 200 at 196.44, 47; a time-only button at 153.55 gives 153.55, 47.

### The look (`datetime-button.md.css`)

| CSS | Measured | Key |
|---|---|---|
| `:host { display: flex; flex-wrap: wrap; align-items: center; justify-content: center }` | 358 x 31 in the stage; two lines at 150 wide (`narrow`, 148 x 62); in an item's end slot as wide as its buttons (206.42, `x_item`); in RTL the date on the right (`x_rtl`) | |
| `button { border-radius: 8px; margin-inline: 2px; padding: 6px 12px; border: none; font-size: 1rem; font-family: inherit; overflow: hidden }` | date 115.53 x 31, time 82.89 x 31, 4 apart | literals in the theme |
| `background: var(--ion-color-step-300, var(--ion-background-color-step-300, #edeef0))` | `rgb(237, 238, 240)` | `Avonic.DatetimeButton.Md.Background`; dark follows `Avonic.BackgroundColorStep300` |
| `color: var(--ion-text-color, #000)` | black | `Avonic.TextColorBrush` |
| `transition: 150ms color ease-in-out` | the active colour fades in over 150 ms even when the button is first drawn (`x_firstColor`: black at 13 ms, the class at 29, `rgb(0, 84, 233)` at 173) | `BrushTransition`, `{a:CubicBezier 0.42, 0, 0.58, 1}` |
| `:host(.date-active) #date-button`, `:host(.time-active) #time-button { color: var(--ion-color-primary) }` | `rgb(0, 84, 233)` | `Avonic.Color.PrimaryBrush` |
| `:host(.ion-color.date-active) #date-button { color: var(--ion-color-base) }` | `rgb(197, 0, 15)` for `danger` (`x_colorActive`); inactive, black | `ColorPalette.Base` |
| `:host(.datetime-button-disabled) { pointer-events: none }`, `button { opacity: 0.4 }` | 0.4 each | `Avonic.DatetimeButton.Md.DisabledMask` |
| `cursor: pointer` | | `Hand` |
| (Chromium's own) `:focus-visible { outline: auto }` | Tab: the date button, then the time button, each with a 2 px orange ring from 1 outside to 1 inside, rounded (`keyboard-focus.png`, `#E59700`) | `Avonic.DatetimeButton.FocusRingBrush`, as `AvonicContent`'s |

The ripple, pressed (`x_tapPress`): `ion-activated` on the button, the ripple in the text colour over the background, clipped to the
radius. Enter and Space click (`keyboard`, `x_space`); Escape then gives focus back to the button, with its ring.

### Accessibility

Each `<button>`: a button named by its text (or the slotted content's), `aria-expanded` from `datetimeActive`, disabled with the
host. The host has no role.

## Not copied from Ionic ([ADR 0005](../../decisions/0005-dont-copy-an-ionic-bug.md))

- **`month` and `year` read `formatOptions.date`**, where Ionic reads `formatOptions.time` (a copy of the line above, evidently: the
  datetime itself formats those presentations by `formatOptions.date` and warns when it's missing). Measured: `month` with
  `{ date: { month: 'short' }, time: { hour: '2-digit', minute: '2-digit' } }` shows `01:47 PM` in Ionic, `Jul` here; `year` with
  `{ date: { year: '2-digit' } }` shows `2024` in Ionic (the date ignored), `24` here. Recorded in `docs/infra/ionic-issues.md`.

## Design

- **`AvonicDatetimeButton : TemplatedControl`** with `Color`, `Datetime`, `DateTargetContent`/`Template` and
  `TimeTargetContent`/`Template`; pseudoclasses `:date-active` and `:time-active` (Ionic's host classes; `datetime-button-disabled`
  is `:disabled`); `ClipToBounds` off, as CSS's host doesn't clip, so the focus ring can go outside it.
- **`DatetimeButtonNative : Button`**, a type of its own for the two `<button>`s: its own `ControlTheme`, a `Text` that is the slot's
  fallback (shown while there's no content), `Activatable` (tap-click) and a ripple in its template, and a peer with
  `IExpandCollapseProvider` for `aria-expanded`, named by its text through `AccessibleText`. The host sets `Text`, `IsVisible` and
  the expanded state in code, as Ionic's `render()` does; the slot's content comes through a template binding.
- **The look**: `src/Avonic.Themes/Controls/AvonicDatetimeButton.axaml` (the host, a `WrapPanel` with `ItemsAlignment="Center"`
  for `flex-wrap` and `justify-content: center`, the items centred in their line, and `DatetimeButtonNative`'s theme), and
  `Tokens/DatetimeButton.axaml`.
- **Finding the datetime**: on the first attach to the logical tree, the name in the nearest name scope, through
  `OverlayTrigger.FindNameScope` (made `internal`, the only change to `Overlays/`), which goes on from a presented overlay to its
  placeholder, as `getElementById` sees the whole document. Ionic's three errors are logged at `Error`, and nothing renders.
- **`componentOnReady`**: `AvonicDatetime.GetDefaultPart()`, which loads the datetime if it hasn't loaded yet (its
  `componentWillLoad`), so its `ValueChange` comes; then the presentation, the text and the selected button. The listener is on
  while the button is attached, and off when it isn't, as a .NET event would otherwise keep a removed button alive; attached again,
  the text is worked out again, as Ionic's never-removed listener would have kept it.
- **The text**: `DatetimeIntl.LocalizedDateTime`, `LocalizedTime` and `GetHourCycle`, with the datetime's `Culture`, as the
  datetime's own header and time button use them; `DatetimeUtils.ParseDates`.
- **The overlay**: the nearest `AvonicModal` or `AvonicPopover` among the datetime's logical ancestors (the datetime's own
  `ParentOverlay`). It gets the style class `ion-datetime-button-overlay`, and a copy of the theme's
  `Avonic.DatetimeButton.Overlay` dictionary in its `Resources.MergedDictionaries` (`Avonic.Modal.Width`, `Avonic.Modal.Height`,
  `Avonic.Popover.Width`, `Avonic.Popover.Height` = `fit-content`), merged, so an app's own key on the overlay still wins, as the
  mapping rule for a class that sets variables has it. The `ion-datetime.datetime-grid` rule is one style in
  `Styles/DatetimeButton.axaml`: `.ion-datetime-button-overlay c|AvonicDatetime:grid { Width: 320; MinHeight: 320 }` (one
  alternative more in the style budget).
- **The click**: the presentation by `SetCurrentValue`, as a container sets a child's prop, so a binding stays. The modal by
  `Present()`; the popover by `Present(new PopoverEvent(button, point))`, the point the tap's, after the datetime's `Rendered` if the
  presentation changed. A failed present is logged, as `PresentFromInput` does.
- **The observer**: `IntersectionObserver` has no Avalonia counterpart that tells a hide or a scroll. On each layout pass while the
  button is attached (`LayoutUpdated`, which a scroll, a present, a dismiss and an ancestor hidden all cause) the button checks:
  the datetime in the same visual root, effectively visible, and at least 1% of its area inside the root and every clipping
  ancestor (`ClipToBounds`), its box transformed to the root. A box of no area that touches counts as 1, as Chromium's ratio does.
- **`AvonicDatetime` (#46), one change**: `Rendered` is raised a frame after a render that changed the presentation even when the
  datetime has no template yet (never shown), as Ionic's datetime renders while hidden. Without it, a tap on a datetime-button whose
  popover has never been shown would wait for good. Nothing else in the datetime changes.

### Not done, with why

- `mode`: ADR 0001.
- Ionic's `IntersectionObserver` is never disconnected, and its `ionValueChange` listener never removed; here both stop while the
  button is detached (above). Nothing an app sees differs.

## Behaviours to test

In `AvonicDatetimeButtonTests` (on the test clock, today pinned to 2023-06-20 10:00, the culture `en-US`) and
`AvonicDatetimeButtonCaptureTests` (frames and boxes against `ref/`):

- **B1** the datetime found by name at load; the three errors; nothing rendered without it.
- **B2** the text of each presentation, with and without `formatOptions`, `preferWheel` together.
- **B3** the values: a list's first; none is the datetime's default part; one that doesn't parse leaves the text; a date alone has
  `Invalid Time`.
- **B4** `multiple`: `n days`, `0 days`, one value formatted, the formatter given the strings, its exception logged.
- **B5** updated on the datetime's value change (code, confirm), not on an unconfirmed selection, nor on a change of locale or
  presentation alone; the presentation read once.
- **B6** a tap on the date or time button changes the presentation as Ionic's does (not with `preferWheel`), and presents the modal,
  or the popover placed by that button after `Rendered`; in the page only the presentation; nothing without an overlay.
- **B7** active while the datetime shows: the selected button's colour and `:date-active`/`:time-active`, `aria-expanded` on both;
  the threshold of 1%; hidden, inactive; in an overlay, on from the present and off after the dismiss.
- **B8** the overlay's class and its fit-content size; a grid datetime 320 wide and at least 320 high in it; an app's key wins.
- **B9** disabled: no click, 0.4.
- **B10** the slots, in place of the text, only in a button that shows.
- **B11** the ripple, Tab order, Enter and Space, the focus ring by keyboard only.
- **B12** the layout: boxes against Chromium's, the wrap, RTL.
- **B13** the ADR 0005 difference: `month` and `year` by `formatOptions.date`.
- **B14** a popover's datetime never shown before still opens (the `Rendered` change).
- **B15** the colour: primary, the colour's base, eased over 150 ms.

## Extensions

None.

## Dependencies

`ion-datetime` (#46), `ion-modal` (#42), `ion-popover` (#43), `ion-ripple-effect`: all merged.
