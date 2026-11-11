# ion-checkbox — Plan

A box with a drawn mark and a label beside it, or above it. Ionic 8's checkbox is its own label: the default slot is the
label text, and `labelPlacement`, `justify` and `alignment` lay the two out. It's the first form control here, so it's the
first with a user-mutable value (`checked`), helper and error text, and the `ion-touched`/`ion-invalid` classes.

Measurements are from real Ionic 8.8.1 in Chromium, md, 390 px wide, wrapped in `ion-app`, with Roboto from `tools/fonts`.
The scene and the commands are in [COMPARISON.md](COMPARISON.md).

## Fidelity checklist

`node tools/ionic-api.js ion-checkbox`.

### Props (13)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `alignment` | `Alignment` (`CheckboxAlignment?`: `Start`, `Center`) | Cross-axis alignment of the label and the box. Set, it makes the host `display: block`. Pseudoclasses `:alignment-start`, `:alignment-center` |
| [x] | `checked` | `Checked` (`bool`, two-way by default) | Mutable: a click sets it with `SetCurrentValue`, so a binding survives. Pseudoclass `:checked` |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`: the checked background, border and mark |
| [x] | `disabled` | `IsEnabled` | Mapping rule. No click, no Space, not a tab stop, label 0.38, box 0.63 |
| [x] | `errorText` | `ErrorText` (`string?`) | Under the label, while the checkbox has the `ion-touched` and `ion-invalid` classes |
| [x] | `helperText` | `HelperText` (`string?`) | Under the label, otherwise |
| [x] | `indeterminate` | `Indeterminate` (`bool`, two-way by default) | Mutable: a click clears it, after `IonChange`. Pseudoclass `:indeterminate` |
| [x] | `justify` | `Justify` (`CheckboxJustify?`: `Start`, `End`, `SpaceBetween`) | Packing on the main axis. Set, it makes the host `display: block`. Pseudoclasses `:justify-*` |
| [x] | `labelPlacement` | `LabelPlacement` (`CheckboxLabelPlacement`: `Start`, `End`, `Fixed`, `Stacked`) | Default `Start`. Pseudoclasses `:label-placement-*` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form. Its only use is the hidden `<input>`'s name for form submission (`renderHiddenInput`), and Avalonia has no form. The mapping would be `CheckboxName`; see "For you to decide" in the PR |
| [x] | `required` | `Required` (`bool`) | `aria-required`: sets `AutomationProperties.IsRequiredForForm` at style priority. Accessibility only, as in Ionic |
| [x] | `value` | `Value` (`object?`, default `"on"`) | Carried in `IonChange`'s detail. Its form use is not applicable |

### Events (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionBlur` | `IonBlur` | The host losing focus |
| [x] | `ionChange` | `IonChange` (`CheckboxChangeEventArgs`: `Checked`, `Value`) | On a click, Space, an item's forwarded tap, or automation's `Toggle`. Not when `Checked` is set in code |
| [x] | `ionFocus` | `IonFocus` | The host getting focus |

### Methods (0)

`setFocus()` is `@internal`; Avalonia's `Focus()` is the same thing.

### Slots (1)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | default | `Content` | The label. No text content hides the label's box (`label-text-wrapper-hidden`), helper text too |

### Shadow parts (6)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `container` | `PART_Container` (`Border`) | The box: size, border, radius, background |
| [x] | `label` | `PART_Label` (`StackPanel`) | `.label-text-wrapper`: the slot and the supporting text |
| [x] | `mark` | `PART_Mark` (`CheckboxMark`) | The drawn path |
| [x] | `supporting-text` | `PART_HelperText` and `PART_ErrorText` | Ionic gives both divs this part name; Avalonia names are unique, so it's the pair |
| [x] | `helper-text` | `PART_HelperText` (`TextBlock`) | |
| [x] | `error-text` | `PART_ErrorText` (`TextBlock`) | |

The template also has `PART_Wrapper` (`LabelledControlPanel`, `CheckboxWrapper` until the P2 roll-up, Ionic's `<label class="checkbox-wrapper">`), `PART_Bottom`
(`.checkbox-bottom`), `PART_NativeWrapper` (`.native-wrapper`) and `PART_Content` (the slot). The hidden `<input>` isn't
recreated: it's `display: none`, and only there for forms.

### CSS variables (11)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--border-color` | `Avonic.Checkbox.BorderColor` | brush | `rgb(text, 0.6)`: `Avonic.TextColor` at 0.6 |
| [x] | `--border-color-checked` | `Avonic.Checkbox.BorderColorChecked` | brush | `CssUnset` → the colour's base, or `Avonic.Color.PrimaryBrush` |
| [x] | `--border-radius` | `Avonic.Checkbox.BorderRadius` | `CornerRadius`, number or CSS string | `CssUnset` → `calc(var(--size) * .125)`: 2.25 |
| [ ] | `--border-style` | — | | Not applicable: Avalonia's `Border` only draws solid, as in the button and the item |
| [x] | `--border-width` | `Avonic.Checkbox.BorderWidth` | `Thickness` | 2 |
| [x] | `--checkbox-background` | `Avonic.Checkbox.CheckboxBackground` | brush | `CssUnset` → `Avonic.ItemBackgroundBrush`, which follows the background colour |
| [x] | `--checkbox-background-checked` | `Avonic.Checkbox.CheckboxBackgroundChecked` | brush | `CssUnset` → the colour's base, or primary |
| [x] | `--checkmark-color` | `Avonic.Checkbox.CheckmarkColor` | brush | `CssUnset` → the colour's contrast, or `Avonic.Color.PrimaryContrastBrush` |
| [x] | `--checkmark-width` | `Avonic.Checkbox.CheckmarkWidth` | `double` | 3, in the 24-unit view box |
| [x] | `--size` | `Avonic.Checkbox.Size` | `double` | 18 |
| [x] | `--transition` | `Avonic.Checkbox.Transition` | `Transitions` | `background 180ms cubic-bezier(0.4, 0, 0.2, 1)` on the box |

The three `-checked` keys and the mark's colour are `CssUnset`, because `:host(.ion-color)` sets the variable inside the
shadow root, and an app's value from outside beats it there.

## Behaviours

Each becomes a test. Ids are the checkboxes in the measurement scene.

| # | Behaviour | Source |
|---|---|---|
| 1 | A click anywhere on the host toggles `Checked` once and raises `IonChange` with `{ checked, value }`: on the box, the label or the gap of a space-between row | `onClick` → `toggleChecked`; the label's own click is stopped (`onDivLabelClick`) and the synthetic input click is `preventDefault`ed; measured `c1` label, `c7` gap, one `ionChange` each |
| 2 | Clicking an indeterminate checkbox: `Checked` becomes `!Checked`, `IonChange` is raised while `Indeterminate` is still true, and then it's cleared | `toggleChecked`: `setChecked`, then `indeterminate = false`; measured `c3` → `{checked: true}` |
| 3 | Setting `Checked` in code raises no `IonChange` | Docs; measured `c4`: none |
| 4 | Space toggles, on key down, every key down, auto-repeat too. Enter does nothing | `onKeyDown` checks `' '` only; measured Enter none, Space press 1, key down alone 1, a `repeat` key down 1 |
| 5 | Focus: a tab stop, a click focuses it, `IonFocus` and `IonBlur` | `tabindex: 0`, `onFocus`, `onBlur`; measured `c1:focus` on click |
| 6 | Disabled: no click, no Space, not a tab stop; label opacity 0.38, box 0.63 | `onClick` returns, `pointer-events: none`, `tabindex` unset; measured `c8` |
| 7 | Layout, label start: label, 16 gap, box; label 19 high, the box 18 centred on it; the host as wide as both (`inline-block`) | `.checkbox-wrapper`, `:host(.checkbox-label-placement-start)`; measured `c1` 109.89 × 19, box at 91.89, y + 0.5 |
| 8 | Label end: box, 16 gap, label, packed at the start | `row-reverse`, `justify-content: start`; measured `c4` box at 0, label at 34 |
| 9 | Label fixed: the label is 100 wide, then 16, then the box, and a long label ends in an ellipsis | `flex: 0 0 100px`; measured `c5` 134 wide, box at 116 |
| 10 | Label stacked: the label above at 0.75 scale, 16 below it, the box under it; centred by default; the host as wide as the unscaled label | `flex-direction: column`, `scale(0.75)`, `max-width: calc(100% / 0.75)`; measured `c6` 57.28 × 53, label box 42.96 × 14.25 at x + 7.16, box at x + 19.64, y + 35 |
| 11 | `Justify` or `Alignment` makes the host a block, full width; `SpaceBetween` puts the box at the far end | `display: block`; measured `c7` 390 wide, box at 372 |
| 12 | `Alignment` start: on the cross axis, at the start; stacked, at the left, scaled from its top left | `align-items: start`, `transform-origin: left top`; measured `i4` label at x 16 |
| 13 | Checked or indeterminate: box background and border the checked colour, the mark drawn; unchecked: `Avonic.Checkbox.CheckboxBackground` with a 2 px `rgb(0,0,0,0.6)` border, no mark | `.checkbox-checked .checkbox-icon`; measured `c1` and `c2`, `rgb(0, 84, 233)` |
| 14 | The mark: md's paths, `M1.73,12.91 8.1,19.28 22.79,4.59` checked and `M2 12H22` indeterminate, in a 24-unit view box filling the box's content (14 × 14), stroke `--checkmark-color`, width 3 units | `getSVGPath('md', indeterminate)`; measured `c2`, `c3` |
| 15 | Checking draws the mark: nothing for 90 ms, then the dash grows linearly to the end over 90 ms; unchecking removes it at once. Programmatic too. Not at first render | `stroke-dashoffset: 30 → 0`, `transition: stroke-dashoffset 90ms linear 90ms` only on the checked rule; measured: 30 until 80 ms, 26.67 at 97, 0 at 181; uncheck 30 at the first frame |
| 16 | The box background eases 180 ms `cubic-bezier(0.4, 0, 0.2, 1)` both ways; the border changes at once | `--transition`; measured 48 ms `rgb(177, 203, 248)`, border `rgb(0, 84, 233)` at 0 |
| 17 | `Color` colours the checked background, border and mark; an app's key beats it | `:host(.ion-color)`; measured `c7` `rgb(197, 0, 15)` |
| 18 | No text content hides the label, so the host is the box alone, 18 × 18 | `label-text-wrapper-hidden`; measured `c11`, `i3` |
| 19 | Helper text under the label, 4 below it, 12 px, `--ion-text-color-step-300`; error text instead, in danger, while it has both `ion-touched` and `ion-invalid`; `ion-invalid` alone shows the helper. Classes changed later are followed | `renderHintText`, `checkInvalidState`, the `MutationObserver`; measured `c9` helper `rgb(77, 77, 77)` 12px at y + 23, `c10` error `rgb(197, 0, 15)`; `c9` after classes, invalid only |
| 20 | Stacked supporting text is 16 px (12 after the scale) | `:host(.checkbox-label-placement-stacked) .checkbox-bottom` |
| 21 | In an item: the host fills the item's default slot, full width and height (`flex: 1 1 0`); label and box get 10 above and below, so the label is at y + 14 and the box at x 356 in a 48 row | `:host(.in-item)`, margins; measured `i1` 358 × 47 |
| 22 | In an item's start or end slot: content-sized, and the box gets no margins | `:host([slot=start])`, `:not([slot])`; measured `i3` 18 × 18 at y + 15 |
| 23 | Stacked in an item: label 10 above and 16 below, box 10 below | measured `i4` 73 high, box at y + 45 |
| 24 | An item with one checkbox forwards a tap on its padding to it: toggles, `IonChange` | `ion-item` `getFirstInteractive` → `click()`; measured `i1` at x 5. `ItemControlType.Checkbox` in `IItemControl` |
| 25 | Automation: a check box, `On`, `Off` or `Indeterminate`; named by the label text unless `AutomationProperties.Name` is set; help text from the shown hint (the error while invalid with error text, else the helper); `Toggle` is a click; required | `role="checkbox"`, `aria-checked` `mixed`, `aria-labelledby`, `aria-describedby` `hintTextId`, `aria-required` |
| 26 | The helper is a polite live region, and the error an assertive one | `aria-live="polite"`, `role="alert"` |

## Design

- **`AvonicCheckbox : ContentControl, IItemControl`**. One slot, so a `ContentControl`. The click is `NativeButton` with no
  keys, as the chip's DOM `click`; Space is the control's own `KeyDown`, which Ionic handles on key down, not as a button's
  keyup. `ItemControlType.Checkbox` tells an item it's a cover and its first interactive control, which the item already
  does.
- **`CheckboxWrapper`** (since the P2 roll-up, the shared `LabelledControlPanel` in `Controls/Flex/`), a panel for `.checkbox-wrapper`: two children, label and native wrapper, in a row, a reversed row,
  or a column, with `justify-content` and `align-items` from `Justify` and `Alignment`. `FlexRow` has no justify, reverse or
  column. Stacked, the label is measured at the width over its scale (`Css.Scale`), as `max-width: calc(100% / 0.75)`.
- **`CheckboxMark`**, a control that draws md's path into a 24-unit view box, and runs the dash's transition on
  `AvonicConfig`'s clock, as the progress bar does: a constant 90 ms delay and 90 ms linear, in Components, as Ionic's md CSS
  hard-codes them. A theme can replace the part.
- **`:in-item`**, from any visual ancestor `AvonicItem` on attach (`hostContext('ion-item')`), and **`:slot-start`** and
  **`:slot-end`**, from the parent presenter's name, `PART_Start` or `PART_End` (`[slot=start]`, `[slot=end]`). All three are
  the checkbox's own, so the rules are in its `ControlTheme`, at no cost to the style budget.
- **Helper and error**: the control sets each part's text and visibility, as `render` does, since `isInvalid` is exactly the
  CSS's `.ion-touched.ion-invalid`. `Classes.CollectionChanged` is the `MutationObserver`.
- **Font**: `Avonic.FontFamily` and 16, as `body`'s, since Avalonia's default is 14.

## Extensions

None.

## Dependencies

`ion-item` (#6) and `ion-label` (#7), both merged. The item's side (`IItemControl`, covers, first interactive) exists.
