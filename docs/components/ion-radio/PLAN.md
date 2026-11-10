# ion-radio and ion-radio-group — Plan

A radio and the group that makes its radios one choice. Ionic 8's radio has no native input: it's a `div` with `role="radio"`,
and its `checked` is internal state, set from the group's `value`. The group does the rest: a click on a radio inside it, the
arrow keys and Space, `ionChange`, which radio is the tab stop, and helper or error text at the top. One family, one PR (#22).

Measurements are from real Ionic 8.8.1 in Chromium, md, 390 px wide, wrapped in `ion-app`. The page is in
[COMPARISON.md](COMPARISON.md), and the behaviours were measured with a throwaway Playwright script on the same page: clicks,
a tap on an item's padding, the keys, focus and blur, and the mark's transition sampled every 70 ms.

## Fidelity checklist

`node tools/ionic-api.js ion-radio`, `node tools/ionic-api.js ion-radio-group`.

### ion-radio: props (8)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `alignment` | `Alignment` (`RadioAlignment?`: `Start`, `Center`) | `align-items` of the wrapper; with `Justify`, makes the radio `display: block` |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`: the mark and a checked ring in the colour's base |
| [x] | `disabled` | `IsEnabled` | Mapping rule. `aria-disabled`, no input, label at 0.38, the ring at 0.63, skipped by the keys and the tab stop |
| [x] | `justify` | `Justify` (`RadioJustify?`: `Start`, `End`, `SpaceBetween`) | `justify-content` of the wrapper. Null is `space-between` from `.radio-wrapper`, without `display: block` |
| [x] | `labelPlacement` | `LabelPlacement` (`RadioLabelPlacement`: `Start`, `End`, `Fixed`, `Stacked`; default `Start`) | |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form. Nothing in the radio reads it; it's submitted with a form, which Avalonia doesn't have |
| [x] | `value` | `Value` (`object?`) | Defaults to a unique `ion-rb-N` when not set, on attach, as `connectedCallback` does |

### ion-radio: events (2)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionBlur` | `IonBlur` (`IonBlurEvent`, bubbles) | When the radio itself loses focus |
| [x] | `ionFocus` | `IonFocus` (`IonFocusEvent`, bubbles) | When the radio itself gets focus: a click, Tab, or an arrow key in the group |

### ion-radio: slots and parts

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | default slot | `Content` | The label. `ContentControl`, as there's one slot |
| [x] | `container` part | `PART_Container` | The 20 px ring |
| [x] | `label` part | `PART_Label` | The label's wrapper. Hidden with no content, as `label-text-wrapper-hidden` is |
| [x] | `mark` part | `PART_Mark` | The dot |

Also `PART_Wrapper` (`.radio-wrapper`, a `RadioWrapper` panel) and `PART_Native` (`.native-wrapper`), which aren't parts in
Ionic, and `PART_Focus`, the `::after` focus circle.

### ion-radio: CSS variables (4)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--border-radius` | `Avonic.Radio.BorderRadius` | `string`, `CornerRadius` or number, read by `BorderRadius.ToCornerRadius` | `50%` |
| [x] | `--color` | `Avonic.Radio.Color` | brush | `rgb(var(--ion-text-color-rgb), 0.6)`: `Avonic.TextColor` at opacity 0.6 |
| [x] | `--color-checked` | `Avonic.Radio.ColorChecked` | brush | `var(--ion-color-primary)`: follows `Avonic.Color.Primary` |
| [x] | `--inner-border-radius` | `Avonic.Radio.InnerBorderRadius` | as `BorderRadius` | `50%` |

`radio.md.css` also has `--border-width: 0.125rem` and `--border-style: solid`, which aren't in `core.json`: a literal 2 in the
theme, as a visual constant with no public variable is.

### ion-radio-group: props (6)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `allowEmptySelection` | `AllowEmptySelection` (`bool`) | A click or Space on the checked radio clears the value |
| [x] | `compareWith` | `CompareWith` (`object?`) | A `Func<object?, object?, bool>`, or a property name as a `string`, as Ionic's function or string. Anything else is strict equality, as `undefined` is |
| [x] | `errorText` | `ErrorText` (`string?`) | At the top, when the group has the classes `ion-touched` and `ion-invalid` |
| [x] | `helperText` | `HelperText` (`string?`) | At the top, otherwise |
| [ ] | `name` | — | Not applicable: HTML form. It names the hidden `<input>` that `renderHiddenInput` adds for a form, and the label's id for `aria-labelledby`, which `LabeledBy` gives here without one |
| [x] | `value` | `Value` (`object?`) | Null is `undefined`: nothing checked |

### ion-radio-group: events (1), no methods, slots, parts or variables

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionChange` | `IonChange` (`IonChangeEvent`, bubbles, `RadioGroupChangeEventArgs.Value`) | On a user's change only, never when `Value` is set in code. Ionic's detail also carries the DOM `event`, which isn't kept: see Deliberate differences |

The group's children: `core.json` lists no slot, so they're **`Items`**, as `ion-list`'s are, and the group is an
`ItemsControl`. Its internal `ionValueChange` is a C# event the radios listen to, and its internal `setFocus` isn't needed
without `ion-select`.

## Behaviours

Each becomes a test. Measured values are from the page in COMPARISON.md.

### The radio

| # | Behaviour | Source |
|---|---|---|
| R1 | Alone, a click checks it, and a second click leaves it checked. No `ionChange`, as there's no group | `onClick`; measured `solo1`, `solo2` |
| R2 | In a group, it's checked when its `Value` is the group's, and follows the group's `Value` and its own as they change in code | `updateState` on `ionValueChange`, `valueChanged`, `connectedCallback` |
| R3 | `CompareWith`: a function, or a property name compared on both values; an array (here an `IList`, as `Array.isArray` isn't true of a string or a dictionary) as the group's value checks every radio whose value is in it | `isOptionSelected`, `compareOptions` |
| R4 | A radio without a `Value` gets a unique one when it's attached, so two of them in a group are two choices | `connectedCallback`: `this.value = this.inputId` |
| R5 | Disabled: a click does nothing, the keys skip it, it's never the tab stop. Its own `disabled`: one in a disabled item is faded only by the item, and the keys still move to it | `onClick`, `pointer-events: none`, `onKeydown`'s filter, `setRadioTabindex`, all on `radio.disabled` |
| R6 | `IonFocus` and `IonBlur` when the radio itself gets and loses focus; a click focuses it | `onFocus`, `onBlur`; measured `log1` `focus`, `clickA` `blur`, `focus` |
| R7 | Not a tab stop alone (`tabindex="-1"`). In a group, the first enabled radio whose value is the group's (strictly) is the one tab stop, or the first enabled radio if none is, set on load and whenever the group's value changes | `buttonTabindex = -1`, `setRadioTabindex`; measured `0-1-1…`, `tab`, `shtab` |
| R8 | In an item: `:in-item`, not activatable or focusable itself (the item is, as a cover), fills the item's row, and a tap on the item's padding clicks it. The label has 10 above and below | `inItem`, `:host(.in-item)`; item.js `getFirstInteractive`, `hasCover`; measured `padTap` `change:b` |
| R9 | Alone, it's activatable and focusable: `:ion-focused` by keyboard shows a 36 px circle of the primary tint at 0.2 behind the ring | `ion-activatable`, `ion-focusable`, `.ion-focused .radio-icon::after` |
| R10 | Accessibility: a radio button, selected when checked, named from its label, disabled when disabled. Selecting it from automation is a click | `role="radio"`, `aria-checked`, `aria-disabled` |
| R11 | No content: the label's wrapper is hidden, with its margin | `label-text-wrapper-hidden`; measured standalone radios 20 by 20 |

### The look

| # | Behaviour | Source |
|---|---|---|
| V1 | A 20 px ring, border 2 in `--color`, radius `--border-radius`; the mark 10 px (50% of the content box + the border), radius `--inner-border-radius`, `--color-checked`, centred | `.radio-icon`, `.radio-inner`; measured 20 by 20, mark 10 by 10 |
| V2 | Checked: the ring is `--color-checked`, the mark scales from 0 to 1 over 280 ms, `cubic-bezier(0.4, 0, 0.2, 1)`, and back when unchecked. No transition on the first frame | `.radio-checked`; sampled about every 70 ms: 0, 0.21, 0.74, 0.97, then 1 at 280 ms, the curve's shape at the sampling's timing |
| V3 | `Color`: the mark is the base, and the checked ring too | `:host(.ion-color)` |
| V4 | Disabled: the label at 0.38, the ring and mark at 0.63 | `.radio-disabled` |
| V5 | `LabelPlacement`: start puts the label first with 16 after it; end puts it last with 16 before it; fixed makes it 100 wide with an ellipsis; stacked puts it above, scaled to 0.75, with 16 below it, from its centre, or from its top with `Alignment`, left in LTR for start | `.radio-label-placement-*`; measured `a`, `d`, `e`, `f`, `g` |
| V6 | `Justify`: null and space-between put them at the two ends of a full row, start packs them at the start, end at the end. `Justify` or `Alignment` makes the radio fill its line (`display: block`); otherwise it's as wide as its content | `.radio-justify-*`, `display: block`; measured `h`, `i`, `16` (74.69 wide) |
| V7 | `Alignment` in a row: start puts both at the top, center (and null) in the middle | `.radio-alignment-*` |
| V8 | In an item: 47 high in a 48 row, label 10 above and below; stacked: 75 high, label 10 above and 16 below, ring 10 above the bottom | `:host(.in-item…)`; measured `a`, `f` |
| V9 | RTL mirrors it | measured `r`, `s` |
| V10 | Label text: one line, an ellipsis when it doesn't fit, in the host's colour | `.label-text-wrapper` |

### The group

| # | Behaviour | Source |
|---|---|---|
| G1 | A click on an enabled radio in it sets `Value` to the radio's and raises `IonChange` once. Clicking the checked radio does nothing, unless `AllowEmptySelection`, when it clears `Value` and raises `IonChange` | `onClick`; measured `clickA`, `clickA2` |
| G2 | Setting `Value` in code checks the radio and raises no `IonChange` | `@Watch('value')`, `emitValueChange` only from user input |
| G3 | Down and Right focus the next enabled radio, wrapping, check it and raise `IonChange`; Up and Left the previous. Focus by key shows `:ion-focused` on the focusable surface around it (the item) | `onKeydown`; measured `down` `d` (Cherry, disabled, skipped), `up` item `ion-focused` |
| G4 | Space checks the focused radio and raises `IonChange` if that changed it; with `AllowEmptySelection` and a value, it clears the value instead, and raises `IonChange` every time. Enter does nothing | `onKeydown`; measured `space`, `enter` |
| G5 | Only a key on a radio of the group counts | `radios.includes(ev.target)` |
| G6 | Helper text at the top, 16 px on a 24 px line, `#4d4d4d`; error text instead, `--ion-color-danger`, when the group has `ion-touched` and `ion-invalid`; nothing when neither is set. 16 at each side in a list | `.radio-group-top`, `checkInvalidState`; measured helper 358 by 24 at x 16, error `rgb(197, 0, 15)` |
| G7 | Accessibility: a group, described by the hint it shows, labelled by the label in a list header or divider in it | `role="radiogroup"`, `aria-describedby`, `aria-labelledby` |
| G8 | An item in a group isn't a list item, even in a list | item.js `inList = hostContext('ion-list') && !hostContext('ion-radio-group')` |

## Design

### The radio

`AvonicRadio : ContentControl, IItemControl`. A tap is a DOM `click` on a `div`, so `NativeButton` with no keys, as the chip
has. The click goes to the group if there is one (`closest('ion-radio-group')` among the logical ancestors, found on attach),
which does `onClick`; the radio's own `checked` toggling in `onClick` only matters alone, as the group's value decides in a
group. A radio in a group listens to the group's value; the group finds its radios as its visual descendants, in tree order,
for the keys and the tab stop, as `querySelectorAll('ion-radio')` does.

Checked is the pseudoclass `:checked`, Ionic's `radio-checked`. There's no public `Checked`: it's `@State`, not a prop. The
automation peer's `IsSelected` shows it, as `aria-checked` does.

The tab stop is `IsTabStop`, at style priority so an app's own value wins, and `false` by default, as `tabindex="-1"` is.

`:in-item` is worked out from the visual ancestors on attach, as `hostContext('ion-item')` is, and turns off `Activatable` and
`FocusVisible` on the radio.

### The wrapper

`.radio-wrapper` is a flex row or column with two children, the label and the native wrapper, whose order, `justify-content` and
`align-items` change with the three props. A `Grid` can't shrink the label to an ellipsis while packing it at the start, and
`FlexRow` has no `justify-content` or column, so `RadioWrapper`, a two-child panel in the radio's folder, does what those rules
do and no more:

- Row (start, fixed, end): the native wrapper keeps its 20, and the label takes what's left, down to nothing, as `overflow:
  hidden` gives it no minimum. End is `row-reverse`. `justify-content: start` and `end` are the line's start and end even
  reversed; space-between puts the first in flex order at the main start, which for `row-reverse` is the right.
- Column (stacked): the label's box is at most `100% / 0.75` wide, and `align-items` centres it or puts it at the start; the theme
  scales it to 0.75.
- Its own size is its content's, so the radio is as wide as its content unless it's stretched, which `Justify`, `Alignment` and
  an item do.

### The group

`AvonicRadioGroup : ItemsControl`, with the hint above the `ItemsPresenter`. Its `Value` is two-way by default, as the user changes
it. Its `TabNavigation` is `Continue`, where an `ItemsControl`'s is `Once`: Tab goes through what's inside, as in a page, and the
radios' own tab stops do the rest. It overrides `OnKeyDown` without calling the base, which would move focus between containers
with the arrows. `:in-list` comes from the logical ancestors on
attach, for the 16 at each side, so the rule is in the group's own theme and costs nothing elsewhere. `ion-touched` and
`ion-invalid` are style classes, as classes an app sets are.

The item rule (G8) is `AutomationProperties.ControlTypeOverride`, in `Styles/RadioGroup.axaml`, as the mapping says for a role
from `hostContext` on a control that isn't ours: `c|AvonicRadioGroup c|AvonicItem:not(:button)` → `Group`, the item's own type
outside a list. A button item stays a button, as it does in a list.

## Extensions

None.

## Deliberate differences

- `ionChange`'s detail has no `event`. Ionic passes the DOM event that caused it; a click here comes from `NativeButton`, which
  has no one event to pass, and an app has the routed event's `Source`.
- `name` on both isn't declared (HTML form).

## Not in this PR

- `ion-select`'s popover and modal, which stop the arrow keys from changing the value (`inSelectInterface`). They don't exist yet.

## Dependencies

`ion-item` (#6), `ion-label` (#7), `ion-list` (#9), `ion-list-header` (#10) and `ion-item-divider`: all merged.
