# ion-picker, ion-picker-column and ion-picker-column-option

Status: done · [#37](https://github.com/wdcossey/Avonic/issues/37)

`AvonicPicker` is the inline wheel picker: `AvonicPickerColumn`s side by side, each a vertical scroller of
`AvonicPickerColumnOption`s that snaps an option to its centre, with a highlight behind the centre row and fades over the rest. The
centred option is the column's `Value`. A scroll by the user that comes to rest, a click on an option or a key sets it and raises
`IonChange`; setting it in code scrolls there smoothly, with no event. `ion-datetime` builds its wheels on it. The design and the
evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicPicker>
    <c:AvonicPickerColumn Value="green" IonChange="OnColumnChange">
        <c:AvonicPickerColumnOption Value="red" Content="Red" />
        <c:AvonicPickerColumnOption Value="green" Content="Green" />
        <c:AvonicPickerColumnOption Value="blue" Content="Blue" IsEnabled="False" />
    </c:AvonicPickerColumn>
    <c:AvonicPickerColumn Value="kg" PrefixContent="Weight" SuffixContent="units">
        <c:AvonicPickerColumnOption Value="kg" Content="kg" />
        <c:AvonicPickerColumnOption Value="lb" Content="lb" />
    </c:AvonicPickerColumn>
</c:AvonicPicker>
```

## API

### `AvonicPicker` (`ion-picker`)

| Ionic | Avonic | Notes |
|---|---|---|
| default children | `Items` | Its columns. `core.json` lists no slot |
| `mode` | — | Not exposed (ADR 0001) |
| `exitInputMode()` (internal) | `internal ExitInputMode()` | For `ion-datetime` |
| `ionInputModeChange` (internal) | `internal event InputModeChanged` | Each column listens to it |

| CSS variable | Key | md default |
|---|---|---|
| `--fade-background-rgb` | `Avonic.Picker.FadeBackgroundRgb` (`Color`) | unset: the fades take `Avonic.BackgroundColor` |
| `--highlight-background` | `Avonic.Picker.HighlightBackground` (brush) | unset: transparent |
| `--highlight-border-radius` | `Avonic.Picker.HighlightBorderRadius` (`CornerRadius`) | `8` |

### `AvonicPickerColumn` (`ion-picker-column`)

| Ionic | Avonic | Notes |
|---|---|---|
| `color` | `Color` | `primary` by default, at style priority. The focused background |
| `disabled` | `IsEnabled` | No scroll, click, key or focus; options at 0.4 |
| `mode` | — | Not exposed (ADR 0001) |
| `value` | `Value` (`object?`) | Two-way. Compared with `Equals`, as `===` |
| `ionChange` | `IonChange` (`IonChangeEvent`, bubbles, `PickerColumnChangeEventArgs.Value`) | The user's changes only |
| `setFocus()` | `SetFocus()` | Focuses the column |
| `prefix`, `suffix` slots | `PrefixContent`, `SuffixContent` (+ `…Template`) | Left and right of the options |
| default slot | `Items` | Its options |
| `text-align` on the host | `HorizontalContentAlignment` | Centred; a picker's first column is at the start and its last at the end. The options follow it, as `text-align: inherit` |
| `numericInput` (internal) | `internal NumericInput` | For `ion-datetime`'s hour and minute |

### `AvonicPickerColumnOption` (`ion-picker-column-option`)

| Ionic | Avonic | Notes |
|---|---|---|
| `color` | `Color` | `primary` by default. The text when it's the active option |
| `disabled` | `IsEnabled` | 0.4; never active, not snapped to, skipped by the keys |
| `value` | `Value` (`object?`) | |
| default slot | `Content` | Its text. `aria-label` is `AutomationProperties.Name` |

## Behaviours

All measured in Chromium, or read from the `.js` where they can't be seen; see [COMPARISON.md](COMPARISON.md).

- At load the column centres its value's option, which is active (its colour). A value with no option is kept, with the first
  option at the centre and none active; set while the column is visible, it becomes the first option's, with `IonChange`.
- A value from code scrolls there smoothly (√distance / 60 s, eased), with no event. A hidden column waits until it's shown.
- While the user scrolls, the option at the centre is active; 250 ms after the last scroll it becomes the value, with one
  `IonChange`. A disabled option is never active. A wheel notch scrolls 100 px and snaps to the nearest option; a touch drag snaps
  when it ends.
- A click on an option sets the value at once and scrolls it to the centre.
- Keys on the focused column: ↓ ↑ one enabled option, PgDn PgUp five, Home End the first and last. Reversed on Android and iOS, as
  Ionic reverses them on a mobile platform.
- Focused, the column has its colour at 0.2 behind its options, except on Android and iOS (`any-hover`).
- Numeric input (internal, for `ion-datetime`): a tap in the highlight lets the user type into every numeric column (the first two
  digits the first column's, the next two the second's), or into the only one; a column focused by the keyboard takes them for
  itself, and a second's pause starts again. A tap outside the highlight, Enter, or focus leaving the picker ends it. The column
  taking digits has the `:column-active` pseudoclass (Ionic's `picker-column-active`).

## Decided on the user's behalf

- **`disabled` is `IsEnabled`** on the column and the option, as the mapping says. Ionic's column only stops its options
  (`pointer-events: none`), so a control in its prefix or suffix stays live there and is disabled here. The alternative was a
  `Disabled` property, as for #34's reorder group, but the column does stop its children, and a prefix or suffix is text in practice.
- **A click on a disabled option does nothing.** Ionic 8.8.1 sets the column to it, raises `ionChange`, then, as the value has no
  active option, sets it to the first option and raises `ionChange` again (measured: `b`, then `a`). The alternative was to copy
  that, which `IsEnabled` doesn't allow; it reads as a bug no app relies on.
- **`SetFocus()` focuses the column.** Ionic 8.8.1's focuses `assistiveFocusable`, which nothing sets, so it does nothing. Its
  documentation says it focuses the scroller, which is what this does; the alternative, a method that does nothing, would be dead.
- **Mobile is Android or iOS** (`isPlatform('mobile')` is `any-pointer: coarse`, which Avalonia doesn't report): it reverses the
  arrows and pages, and turns off the focused background (`any-hover`). `AvonicPickerColumn.IsMobile` is internal, for tests.
- **A touchpad's small scrolls snap 100 ms after the last one**, as Chromium snaps at the gesture's end, which Avalonia doesn't
  report. Not measured.

## Known gaps

- **A touch on the picker reaches gestures outside it** (Ionic stops `touchstart`, so that scrolling a wheel in a card modal doesn't
  swipe the modal). Avonic's `Gesture` listens to handled presses too, so the picker can't stop it.
- **Snapping after a touch drag is Avalonia's**: it jumps to the snap point on release, or aims a fling at one, where Chromium eases.
- **A hidden ancestor isn't noticed until the viewport changes**: Ionic's `IntersectionObserver` tells the column at once. Only the
  scroll for a value set while hidden is affected; it runs on a hidden scroller, which is harmless.
- **The first-of-type rules count every item**, not only columns, as Avalonia has no `:first-of-type`.
- **No haptics**: Ionic's are iOS only, so md has none either.

## Extensions

None.
