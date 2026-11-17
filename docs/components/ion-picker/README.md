# ion-picker, ion-picker-column and ion-picker-column-option

Status: done · [#37](https://github.com/wdcossey/Avonic/issues/37)

`AvonicPicker` is the inline wheel picker: `AvonicPickerColumn`s side by side, each a vertical scroller of
`AvonicPickerColumnOption`s that snaps an option to its centre, with a highlight behind the centre row and fades over the rest. The
centred option is the column's `Value`. A scroll by the user that comes to rest, a click on an option or a key sets it and raises
`IonChange`; setting it in code scrolls there smoothly, with no event. `ion-datetime` builds its wheels on it. The design and the
evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md). It follows Ionic
9.0.11.

> **Changed in the Ionic 9 migration** ([#202](https://github.com/wdcossey/Avonic/issues/202)): `AvonicPickerColumn.Color` and
> `AvonicPickerColumnOption.Color` are unset by default, as Ionic 9.0 removed their default `'primary'`: they read null, and
> `:ion-color` is off until an app sets one. The look is the same, as the active option and the focused column fall back to
> primary. A press outside the column during the user's scroll now commits the centred option at once, and a column detached
> during a scroll commits nothing (9.0.1, 9.0.11).

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
| `color` | `Color` | Unset by default. The focused background, primary's without one (`Avonic.PickerColumn.FocusBackground`) |
| `disabled` | `IsEnabled` | No scroll, click, key or focus; options at 0.4 |
| `mode` | — | Not exposed (ADR 0001) |
| `value` | `Value` (`object?`) | Two-way. Compared with `Equals`, as `===` |
| `ionChange` | `IonChange` (`IonChangeEvent`, bubbles, `PickerColumnChangeEventArgs.Value`) | The user's changes only |
| `setFocus()` | `SetFocus()` | Focuses the column |
| `prefix`, `suffix` slots | `PrefixContent`, `SuffixContent` (+ `…Template`) | Left and right of the options |
| default slot | `Items` | Its options |
| `numericInput` (internal) | `internal NumericInput` | For `ion-datetime`'s hour and minute |

### `AvonicPickerColumnOption` (`ion-picker-column-option`)

| Ionic | Avonic | Notes |
|---|---|---|
| `color` | `Color` | Unset by default. The text when it's the active option, primary without one |
| `disabled` | `IsEnabled` | 0.4; never active, not snapped to, skipped by the keys |
| `value` | `Value` (`object?`) | |
| default slot | `Content` | Its text. `aria-label` is `AutomationProperties.Name` |

## Behaviours

All measured in Chromium, or read from the `.js` where they can't be seen; see [COMPARISON.md](COMPARISON.md).

- At load the column centres its value's option, which is active (its colour). A value with no option is kept, with the first
  option at the centre and none active; set while the column is visible, it becomes the first option's, with `IonChange`.
- A value from code scrolls there smoothly (√distance / 60 s, eased), with no event. A hidden column waits until it's shown, and
  then only its value's option is active: an old one is cleared, as Ionic 8.8.19 does (8.8.1 looked for it in the column's shadow
  root, where slotted options aren't, and left it coloured; Avonic cleared it from the start, as Ionic meant).
- While the user scrolls, the option at the centre is active; 250 ms after the last scroll it becomes the value, with one
  `IonChange`. A disabled option is never active, and the active option stays until an enabled one is at the centre. A wheel
  notch scrolls 100 px and snaps to the nearest option; a touch drag snaps when it ends.
- During a scroll the user started, a press anywhere outside the column and its picker stops the column on the centred option and
  commits it at once, before the press reaches what it's on, so a Save button reads the new value. A press in the same picker
  leaves the column scrolling. A column detached or hidden during a scroll commits nothing.
- A click on an option sets the value at once and scrolls it to the centre.
- Keys on the focused column: ↓ ↑ one enabled option, PgDn PgUp five, Home End the first and last. Reversed on Android and iOS, as
  Ionic reverses them on a mobile platform.
- Focused, the column has its colour (primary without one) at 0.2 behind its options, except on Android and iOS (`any-hover`).
- Numeric input (internal, for `ion-datetime`): a tap in the highlight lets the user type into every numeric column (the first two
  digits the first column's, the next two the second's), or into the only one; a column focused by the keyboard takes them for
  itself, and a second's pause starts again. A tap outside the highlight, Enter, or focus leaving the picker ends it. The column
  taking digits has the `:column-active` pseudoclass (Ionic's `picker-column-active`).
- A touch on the picker starts no gesture around it, as Ionic stops `touchstart` there, so that scrolling a wheel doesn't swipe a
  sliding item or a card modal (`Gesture.StopsTouchStart`, #131). A mouse press isn't stopped, as only `touchstart` is.

## Decided on the user's behalf

- **`disabled` is `IsEnabled`** on the column and the option, as the mapping says. Ionic's column only stops its options
  (`pointer-events: none`), so a control in its prefix or suffix stays live there and is disabled here. The alternative was a
  `Disabled` property, as for #34's reorder group, but the column does stop its children, and a prefix or suffix is text in practice.
- **A click on a disabled option does nothing.** Ionic 8.8.19 (and 9.0.11, whose option code is the same) sets the column to it, raises `ionChange`, then, as the value has no
  active option, sets it to the first option and raises `ionChange` again (measured: `b`, then `a`). The alternative was to copy
  that, which `IsEnabled` doesn't allow; it reads as a bug no app relies on.
- **`SetFocus()` focuses the column.** Ionic 8.8.19's (and 9.0.11's) focuses `assistiveFocusable`, which nothing sets, so it does nothing. Its
  documentation says it focuses the scroller, which is what this does; the alternative, a method that does nothing, would be dead.
- **Mobile is Android or iOS** (`isPlatform('mobile')` is `any-pointer: coarse`, which Avalonia doesn't report): it reverses the
  arrows and pages, and turns off the focused background (`any-hover`). `AvonicPickerColumn.IsMobile` is internal, for tests.
- **With one numeric column, a tap on an option in the highlight takes digits for that column.** Ionic passes the tapped element,
  the option, as the input mode's column, so no column becomes active and typing selects nothing. The alternative was to copy
  that; `ion-datetime` always has two numeric columns, where both agree.
- **Items from `ItemsSource` that aren't controls get an `AvonicPickerColumnOption`** with the item as its `Content` and
  `Value`, as a framework's template would make one for each.
- **A touchpad's small scrolls snap 100 ms after the last one**, as Chromium snaps at the gesture's end, which Avalonia doesn't
  report. Not measured.

## Known gaps

- **Snapping after a touch drag is Avalonia's**: it jumps to the snap point on release, or aims a fling at one, where Chromium eases.
- **A hidden ancestor is noticed when the column next acts** (a value, a render), not at once as Ionic's `IntersectionObserver`
  does, as Avalonia doesn't say when an ancestor hides. Showing it again is noticed at once (`HiddenChainWatcher`).
- **A press outside doesn't stop a touch fling's inertia.** It halts a wheel's or a touchpad's scroll and a smooth scroll, and
  commits at once, but Avalonia's `ScrollGestureRecognizer` goes on with a fling it has started (its timer stops only for a new
  touch on its own control, as read from Avalonia's source, not tried); once it moves more than 2 px past the halt, the column scrolls again and commits where it rests, with a second
  `IonChange`. Chromium's `scrollTo` ends the fling. Not yet tried on a device.
- **The first-of-type rules count every item**, not only columns, as Avalonia has no `:first-of-type`.
- **No haptics**: Ionic's are iOS only, so md has none either.

## Extensions

- **`AvonicPickerColumn.HorizontalContentAlignment`**: the column's `text-align`, which isn't an Ionic prop but CSS on the host.
  Centred; a picker's first column is at the start and its last at the end (`picker.md.css`). The options follow it, as
  `text-align: inherit`. `ion-datetime` sets it on its columns, as Ionic's comment on `.picker-opts` says.
