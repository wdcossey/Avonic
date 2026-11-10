# ion-radio and ion-radio-group

Status: done · [#22](https://github.com/wdcossey/Avonic/issues/22)

`AvonicRadio` is a radio with its label, and `AvonicRadioGroup` makes the radios inside it one choice: a click, an arrow key or Space
sets its `Value` and raises `IonChange`, and each radio is checked when its `Value` is the group's. The design and evidence are in
[PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicList>
    <c:AvonicRadioGroup Value="banana" HelperText="Pick one" IonChange="OnFruitChange">
        <c:AvonicListHeader>
            <c:AvonicLabel Content="Fruit" />
        </c:AvonicListHeader>
        <c:AvonicItem>
            <c:AvonicRadio Value="apple" Content="Apple" />
        </c:AvonicItem>
        <c:AvonicItem>
            <c:AvonicRadio Value="banana" Content="Banana" />
        </c:AvonicItem>
    </c:AvonicRadioGroup>
</c:AvonicList>
```

## API

### ion-radio: `AvonicRadio`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `alignment` | `Alignment` (`RadioAlignment?`) | `Start`, `Center`: across the line. Set, the radio fills its line |
| [x] | `color` | `Color` | The mark, and the ring when checked. See the [colour system](../../infra/color-system/README.md) |
| [x] | `disabled` | `IsEnabled` | No input; label 0.38, ring 0.63; skipped by the keys. Its own: a disabled item around it fades it once, and the keys still check it |
| [x] | `justify` | `Justify` (`RadioJustify?`) | `Start`, `End`, `SpaceBetween`. Null is space-between. Set, the radio fills its line |
| [x] | `labelPlacement` | `LabelPlacement` (`RadioLabelPlacement`) | `Start` (default), `End`, `Fixed` (100 wide, ellipsis), `Stacked` (above, at 0.75) |
| [x] | `value` | `Value` (`object?`) | A unique `ion-rb-N` when not set |
| [ ] | `name` | — | Not applicable: HTML form |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `ionFocus` | `IonFocus` (`IonFocusEvent`) | The radio itself got focus |
| [x] | `ionBlur` | `IonBlur` (`IonBlurEvent`) | The radio itself lost focus |
| [x] | default slot | `Content` | The label |
| [x] | `container`, `label`, `mark` parts | `PART_Container`, `PART_Label`, `PART_Mark` | Also `PART_Wrapper` (a `RadioWrapper`), `PART_Native` and `PART_Focus` |

Checked is internal, as Ionic's `checked` is: it's the pseudoclass `:checked`, and the automation peer's `IsSelected`. Alone, a click
checks a radio and nothing unchecks it. Alone it's not a tab stop, as `tabindex="-1"` isn't.

### ion-radio-group: `AvonicRadioGroup`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `allowEmptySelection` | `AllowEmptySelection` | A click or Space on the checked radio clears the value |
| [x] | `compareWith` | `CompareWith` (`object?`) | A `Func<object?, object?, bool>`, given the group's value then the radio's, or a property name as a `string`; otherwise `Equals` |
| [x] | `errorText` | `ErrorText` | Shown instead of the helper text while the group has the classes `ion-touched` and `ion-invalid` |
| [x] | `helperText` | `HelperText` | Above the radios |
| [x] | `value` | `Value` (`object?`) | Null is nothing checked. An `IList` checks every radio whose value is in it |
| [ ] | `name` | — | Not applicable: HTML form |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `ionChange` | `IonChange` (`IonChangeEvent`, `RadioGroupChangeEventArgs.Value`) | A user's change only, never a value set in code |

`core.json` lists no slot, so the children are `Items`, as an `ItemsControl`'s are. A radio belongs to the nearest group around it,
however deep, in an item or a panel. The group's template has `PART_Top`, `PART_HelperText`, `PART_ErrorText` and `PART_ItemsPresenter`.

### Resource keys

| Ionic | Key | md default |
|---|---|---|
| `--border-radius` | `Avonic.Radio.BorderRadius` | `50%`: a string, a `CornerRadius` or a number |
| `--color` | `Avonic.Radio.Color` | `Avonic.TextColor` at 0.6 |
| `--color-checked` | `Avonic.Radio.ColorChecked` | `Avonic.Color.Primary` |
| `--inner-border-radius` | `Avonic.Radio.InnerBorderRadius` | `50%`, for the mark and the focus circle |

The group has no variables: its helper text is `Avonic.TextColorStep300Brush` and its error text `Avonic.Color.DangerBrush`.

## Behaviours

- **A click** on a radio in a group sets the group's `Value` to the radio's and raises `IonChange`. A click on the checked radio does
  nothing, unless `AllowEmptySelection`, when it clears the value. Alone, a click checks a radio.
- **The keys**, on a focused radio of the group: Down and Right go to the next enabled radio, Up and Left to the previous, wrapping,
  focus it and check it. Space checks the focused radio, or with `AllowEmptySelection` clears a value. Enter does nothing.
- **The tab stop** is the first enabled radio whose value is the group's, or the first enabled radio, so Tab goes into the group at the
  choice and out again.
- **In an item**, the radio fills the row, and the item is the cover: it ripples and shows focus, and a tap on its padding clicks the
  radio. In the start or end slot it's as wide as its content.
- **Focus by keyboard** on a radio outside an item shows a 36 px circle of the primary tint at 0.2.
- **The mark** scales in over 280 ms, `cubic-bezier(0.4, 0, 0.2, 1)`, and out again.
- **The hint** is at the top, 16 at each side in a list. The error text shows instead of the helper text while the group has both
  `ion-touched` and `ion-invalid`, which an app's validation sets.
- **Automation:** a radio is a radio button, named from its label, selected when checked. The group is a group, described by the hint it
  shows and labelled by the label in its list header, or else its divider. An item in a group isn't a list item, even in a list.

## Extensions

None.

## Known gaps

- `ionChange` has no `event`; a label is any content but null or an empty string; a radio belongs to its nearest group only; no
  `aria-invalid`; an arrow onto a radio in a disabled item checks it without focusing it. See
  [COMPARISON.md](COMPARISON.md#deliberate-differences).
- `ion-select`'s interfaces, which keep the arrows from changing the value, don't exist yet.
- Not tried in a real window, nor on a device.
