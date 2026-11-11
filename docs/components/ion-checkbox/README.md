# ion-checkbox

Status: done · [#20](https://github.com/wdcossey/Avonic/issues/20)

`AvonicCheckbox` is a box with a drawn mark and its own label, which is its content. A click anywhere on it, or Space, toggles it.
The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicCheckbox Content="Remember me" Checked="{Binding Remember}" />

<c:AvonicCheckbox Content="I agree to the terms" LabelPlacement="End" Justify="Start"
                  HelperText="Required to continue" ErrorText="You have to agree" Required="True"
                  IonChange="OnTermsChange" />

<c:AvonicItem>
    <c:AvonicCheckbox Content="In an item" />
</c:AvonicItem>
```

An app marks a field `ion-touched` and `ion-invalid` with style classes, as an Ionic form does, and the error text then replaces the
helper text:

```csharp
terms.Classes.Set("ion-touched", true);
terms.Classes.Set("ion-invalid", !terms.Checked);
```

## API

### Properties

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `alignment` | `Alignment` | `CheckboxAlignment?` (`Start`, `Center`) | null | Cross axis. Set, the checkbox fills its width |
| [x] | `checked` | `Checked` | `bool` | false | Two-way by default. A click sets it with `SetCurrentValue` |
| [x] | `color` | `Color` | `string?` | null | The checked background, border and mark |
| [x] | `disabled` | `IsEnabled` | `bool` | true | Mapping rule |
| [x] | `errorText` | `ErrorText` | `string?` | null | Shown while it has the `ion-touched` and `ion-invalid` classes |
| [x] | `helperText` | `HelperText` | `string?` | null | Shown otherwise |
| [x] | `indeterminate` | `Indeterminate` | `bool` | false | Two-way by default. A click clears it, after `IonChange` |
| [x] | `justify` | `Justify` | `CheckboxJustify?` (`Start`, `End`, `SpaceBetween`) | null | Main axis. Set, the checkbox fills its width |
| [x] | `labelPlacement` | `LabelPlacement` | `CheckboxLabelPlacement` (`Start`, `End`, `Fixed`, `Stacked`) | `Start` | |
| [ ] | `mode` | — | | | Not exposed (ADR 0001) |
| [ ] | `name` | — | | | Not applicable: HTML form |
| [x] | `required` | `Required` | `bool` | false | `AutomationProperties.IsRequiredForForm` |
| [x] | `value` | `Value` | `object?` | `"on"` | In `IonChange` |

### Events

| ✓ | Ionic | Avonic | Args |
|---|---|---|---|
| [x] | `ionBlur` | `IonBlur` | `RoutedEventArgs` |
| [x] | `ionChange` | `IonChange` | `CheckboxChangeEventArgs` (`Checked`, `Value`): from a click, Space, an item's tap or automation, never from code |
| [x] | `ionFocus` | `IonFocus` | `RoutedEventArgs` |

### Content and parts

The default slot is `Content`, the label. With no text in it, the label and its helper text aren't shown.

| ✓ | Ionic part | Template part |
|---|---|---|
| [x] | `container` | `PART_Container` (`Border`) |
| [x] | `label` | `PART_Label` |
| [x] | `mark` | `PART_Mark` (`CheckboxMark`) |
| [x] | `supporting-text` | `PART_HelperText` and `PART_ErrorText` |
| [x] | `helper-text` | `PART_HelperText` |
| [x] | `error-text` | `PART_ErrorText` |

`LabelledControlPanel` (`PART_Wrapper`), shared with the radio and the toggle, lays out the label and the box for each placement, and `CheckboxMark` draws md's tick or bar.
Pseudoclasses for a theme: `:checked`, `:indeterminate`, `:label-placement-*`, `:justify-*`, `:alignment-*`, `:in-item`,
`:slot-start`, `:slot-end`, `:ion-color`, and `:drawn` from the first frame, for the transition.

### Resource keys

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--border-color` | `Avonic.Checkbox.BorderColor` | brush | text colour at 0.6 |
| [x] | `--border-color-checked` | `Avonic.Checkbox.BorderColorChecked` | brush | unset: the colour's base, or primary |
| [x] | `--border-radius` | `Avonic.Checkbox.BorderRadius` | `CornerRadius`, number or CSS string | unset: an eighth of the size |
| [ ] | `--border-style` | — | | Not applicable: `Border` only draws solid |
| [x] | `--border-width` | `Avonic.Checkbox.BorderWidth` | `Thickness` | 2 |
| [x] | `--checkbox-background` | `Avonic.Checkbox.CheckboxBackground` | brush | unset: `Avonic.ItemBackgroundBrush` |
| [x] | `--checkbox-background-checked` | `Avonic.Checkbox.CheckboxBackgroundChecked` | brush | unset: the colour's base, or primary |
| [x] | `--checkmark-color` | `Avonic.Checkbox.CheckmarkColor` | brush | unset: the colour's contrast, or primary's |
| [x] | `--checkmark-width` | `Avonic.Checkbox.CheckmarkWidth` | `double` | 3, of 24 units |
| [x] | `--size` | `Avonic.Checkbox.Size` | `double` | 18 |
| [x] | `--transition` | `Avonic.Checkbox.Transition` | `Transitions` | background, 180 ms `cubic-bezier(0.4, 0, 0.2, 1)` |

## Behaviours

- A click anywhere on it toggles it once and raises `IonChange`; Space does on every key down, repeats too; Enter doesn't.
- Clicking an indeterminate checkbox checks it, and clears `Indeterminate` after `IonChange`.
- A tab stop. Disabled: no click, no Space, no focus, label 0.38 and box 0.63.
- Label start, end, fixed (100 wide, ellipsis) or stacked (above at 0.75); `Justify` and `Alignment` fill the width.
- Checking draws the mark after 90 ms over 90 ms, and the background eases over 180 ms; unchecking removes the mark at once. Not
  on the first frame.
- In an item it fills the row, with 10 above and below; in a start or end slot it's its own size. The item makes itself
  activatable, and a tap on its padding toggles the checkbox.
- Automation: a check box with `On`, `Off` or `Indeterminate`, named by its label, described by the helper or the error.

## Extensions

None.

## Known gaps

- Chromium's own focus ring on keyboard focus is the base theme's focus adorner here.
- `aria-invalid` has no Avalonia equivalent.
- The few pixels an `inline-block` line box adds around a standalone checkbox aren't there.
- A standalone label longer than the page ends in an ellipsis, where Ionic's `inline-block` overflows the page with its box off screen.
- A parent that arranges it narrower than it measured it clips the label without an ellipsis.
- Not tried in a real window or on a device.
