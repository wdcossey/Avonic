# ion-input

Status: done · [#24](https://github.com/wdcossey/Avonic/issues/24)

`AvonicInput` is a one-line text field with a label, start and end slots, a clear button, helper and error text and a counter,
in md's three looks: no fill, `Solid` and `Outline`. `AvonicInputPasswordToggle` ([its page](../ion-input-password-toggle/README.md))
goes in the end slot to show or hide a password. The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real
Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicItem>
    <c:AvonicInput Label="Email" Type="Email" Placeholder="you@example.com" />
</c:AvonicItem>

<c:AvonicInput Fill="Outline" Label="Name" LabelPlacement="Floating" HelperText="As on your card"
               Counter="True" Maxlength="40" ClearInput="True" IonChange="OnNameChange" />

<c:AvonicInput Label="Password" Type="Password">
    <c:AvonicInput.EndContent>
        <c:AvonicInputPasswordToggle />
    </c:AvonicInput.EndContent>
</c:AvonicInput>
```

Validation is the app's, as in Ionic: add the style classes `ion-touched` and `ion-invalid` (`AvonicInput.TouchedClass`,
`InvalidClass`) to show the error text in the invalid colour, or `ion-valid` (`ValidClass`) for the valid colour.

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `autocapitalize` | `Autocapitalize` (`string`) | `TextInputOptions.AutoCapitalization`, or `Uppercase` for `characters` |
| [ ] | `autocomplete` | — | Not applicable: the browser's autofill |
| [x] | `autocorrect` | `Autocorrect` | `TextInputOptions.ShowSuggestions` |
| [x] | `autofocus` | `Autofocus` | Focuses the field when it's loaded |
| [x] | `clearInput` | `ClearInput` | |
| [x] | `clearInputIcon` | `ClearInputIcon` | An icon name or source; null is `close-sharp` |
| [x] | `clearOnEdit` | `ClearOnEdit` (`bool?`) | Null is on for `Password` |
| [x] | `color` | `Color` | The focused highlight |
| [x] | `counter` | `Counter` | With `Maxlength` |
| [x] | `counterFormatter` | `CounterFormatter` (`Func<int, int, string>?`) | |
| [x] | `debounce` | `Debounce` (`int?`, ms) | Delays `IonInput` |
| [x] | `disabled` | `IsEnabled` | |
| [x] | `enterkeyhint` | `Enterkeyhint` | `TextInputOptions.ReturnKeyType` |
| [x] | `errorText` | `ErrorText` | |
| [x] | `fill` | `Fill` (`InputFill?`) | `Outline`, `Solid` |
| [x] | `helperText` | `HelperText` | |
| [x] | `inputmode` | `Inputmode` (`InputInputmode?`) | `TextInputOptions.ContentType`; no `none` |
| [x] | `label` | `Label` | Beats `LabelContent` |
| [x] | `labelPlacement` | `LabelPlacement` (`InputLabelPlacement`) | `End`, `Fixed`, `Floating`, `Stacked`, `Start` |
| [ ] | `max`, `min`, `step` | — | Not applicable: number and date constraints and spin buttons |
| [x] | `maxlength` | `Maxlength` (`int?`) | |
| [ ] | `minlength`, `multiple`, `pattern` | — | Not applicable: HTML form validation |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form |
| [x] | `placeholder` | `Placeholder` | |
| [x] | `readonly` | `Readonly` | |
| [ ] | `required` | — | Not applicable: Avalonia's automation can't say it |
| [x] | `shape` | `Shape` (`InputShape?`) | `Round` |
| [ ] | `spellcheck` | — | Not applicable: no spell checker |
| [x] | `type` | `Type` (`InputType`) | `Email`, `Number`, `Password`, `Search`, `Tel`, `Text`, `Url`; no date or time types |
| [x] | `value` | `Value` (`string?`) | Two-way by default |

### Events and methods

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionInput` | `IonInput` (`InputInputEventArgs.Value`) |
| [x] | `ionChange` | `IonChange` (`InputChangeEventArgs.Value`) |
| [x] | `ionFocus` | `IonFocus` |
| [x] | `ionBlur` | `IonBlur` |
| [x] | `setFocus()` | `SetFocus()` |
| [x] | `getInputElement()` | `GetInputElement()`: the `TextBox`, or null before the input is in a tree |

### Content

| ✓ | Ionic slot | Avonic |
|---|---|---|
| [x] | `label` | `LabelContent`, `LabelContentTemplate` |
| [x] | `start` | `StartContent`, `StartContentTemplate` |
| [x] | `end` | `EndContent`, `EndContentTemplate` |

### Resource keys

Each is in `Tokens/Input.axaml`. `CssUnset` ones fall back as Ionic's do; the fill's own default is an `Avonic.Input.Md.*` key.

| ✓ | Ionic | Key |
|---|---|---|
| [x] | `--background` | `Avonic.Input.Background` |
| [x] | `--border-color` | `Avonic.Input.BorderColor` |
| [x] | `--border-radius` | `Avonic.Input.BorderRadius` |
| [ ] | `--border-style` | — (Avalonia draws solid borders only) |
| [x] | `--border-width` | `Avonic.Input.BorderWidth` |
| [x] | `--color` | `Avonic.Input.Color` |
| [x] | `--highlight-color-focused` | `Avonic.Input.HighlightColorFocused` |
| [x] | `--highlight-color-invalid` | `Avonic.Input.HighlightColorInvalid` |
| [x] | `--highlight-color-valid` | `Avonic.Input.HighlightColorValid` |
| [x] | `--highlight-height` | `Avonic.Input.HighlightHeight` |
| [x] | `--padding-start`, `-end`, `-top`, `-bottom` | `Avonic.Input.PaddingStart`, `PaddingEnd`, `PaddingTop`, `PaddingBottom` |
| [x] | `--placeholder-color` | `Avonic.Input.PlaceholderColor` |
| [x] | `--placeholder-font-style` | `Avonic.Input.PlaceholderFontStyle` |
| [x] | `--placeholder-font-weight` | `Avonic.Input.PlaceholderFontWeight` |
| [x] | `--placeholder-opacity` | `Avonic.Input.PlaceholderOpacity` |

## Behaviours

Each has a test in `AvonicInputTests`, named after its number in [PLAN.md](PLAN.md#behaviours).

- Typing raises `IonInput` with each value; Enter, and leaving the field after an edit, raise `IonChange` once.
- A value set in code raises nothing; an `IonInput` handler can replace what was typed (masking, trimming).
- `Debounce` raises one `IonInput`, with the last value, after the wait.
- `ClearOnEdit` (on for passwords) clears on the first key that edits, once per focus.
- The clear button shows with a value, clears, raises `IonInput` with `""`, and leaves the field focused.
- A click on the label focuses the field; in an item, so does a tap on the item's padding.
- The five label placements, the floating label, the highlight, the solid and outline looks, the notch, round shapes, helper
  and error text, the counter, disabled and read-only, all measured against Chromium.
- The native input is named by the label and described by the helper or error text.

## Extensions

Not in Ionic's API, but needed by the theme:

- `HighlightBrush`: Ionic's internal `--highlight-color`, which the theme works out per state (focused, a colour, valid, invalid).
- `CounterText`: the counter's text, or null when there's none.
- The converters `PaddingConverter`, `BottomPaddingConverter`, `RadiusConverter`, `SidesConverter`, `OutlineStartWidthConverter`,
  `NotchWidthConverter` and `OrConverter`.
- `InputWrapperPanel`: the wrapper's flex box, in a row, a column, or with the outline's label out of the flow.

## Known gaps

See [COMPARISON.md](COMPARISON.md#deliberate-differences-and-gaps): a click on the label while focused keeps focus, where
Chromium blurs and refocuses; no date or time types and no number filtering; an icon-only button in a slot keeps its own look;
an app's background and border keys win at rest with a fill, where Ionic's fill wins; an input in an item's start or end slot still stretches; the notch has no
`max-width`; bottom text is 1 px taller. Not tried in a real window, with a soft keyboard, or on a device.
