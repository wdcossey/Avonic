# ion-textarea

Status: done · [#25](https://github.com/wdcossey/Avonic/issues/25)

`AvonicTextarea` is a multi-line text field with `ion-input`'s label, start and end slots, helper and error text and counter, in
md's three looks: no fill, `Solid` and `Outline`. Its field is `Rows` lines high, or grows with its lines with `AutoGrow`. The
design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicItem>
    <c:AvonicTextarea Label="Notes" Placeholder="Anything else?" />
</c:AvonicItem>

<c:AvonicTextarea Fill="Outline" Label="Comment" LabelPlacement="Floating" AutoGrow="True"
                  HelperText="Be kind" Counter="True" Maxlength="200" IonChange="OnCommentChange" />

<c:AvonicTextarea Label="Log" Rows="6" Wrap="Off" Readonly="True" />
```

Validation is the app's, as in Ionic: add the style classes `ion-touched` and `ion-invalid` (`AvonicTextarea.TouchedClass`,
`InvalidClass`) to show the error text in the invalid colour, or `ion-valid` (`ValidClass`) for the valid colour.

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `autoGrow` | `AutoGrow` | As many lines as the value, never fewer than `Rows` |
| [x] | `autocapitalize` | `Autocapitalize` (`string`, `"none"`) | `TextInputOptions.AutoCapitalization`, or `Uppercase` for `characters` |
| [x] | `autofocus` | `Autofocus` | Focuses the field when it's first loaded |
| [x] | `clearOnEdit` | `ClearOnEdit` | The first key after focus, Enter included, clears the value |
| [x] | `color` | `Color` | The focused highlight |
| [x] | `cols` | `Cols` (`int?`) | Without `AutoGrow`, the host is as wide as its content |
| [x] | `counter` | `Counter` | With `Maxlength` |
| [x] | `counterFormatter` | `CounterFormatter` (`Func<int, int, string>?`) | |
| [x] | `debounce` | `Debounce` (`int?`, ms) | Delays `IonInput` |
| [x] | `disabled` | `IsEnabled` | |
| [x] | `enterkeyhint` | `Enterkeyhint` (`TextareaEnterkeyhint?`) | `TextInputOptions.ReturnKeyType` |
| [x] | `errorText` | `ErrorText` | |
| [x] | `fill` | `Fill` (`TextareaFill?`) | `Outline`, `Solid` |
| [x] | `helperText` | `HelperText` | |
| [x] | `inputmode` | `Inputmode` (`TextareaInputmode?`) | `TextInputOptions.ContentType`; no `none` |
| [x] | `label` | `Label` | Beats `LabelContent` |
| [x] | `labelPlacement` | `LabelPlacement` (`TextareaLabelPlacement`) | `End`, `Fixed`, `Floating`, `Stacked`, `Start` |
| [x] | `maxlength` | `Maxlength` (`int?`) | |
| [ ] | `minlength` | — | Not applicable: HTML form validation |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form |
| [x] | `placeholder` | `Placeholder` | |
| [x] | `readonly` | `Readonly` | |
| [ ] | `required` | — | Not applicable: Avalonia's automation can't say it |
| [x] | `rows` | `Rows` (`int?`) | Null is 2 |
| [x] | `shape` | `Shape` (`TextareaShape?`) | `Round` |
| [ ] | `spellcheck` | — | Not applicable: no spell checker |
| [x] | `value` | `Value` (`string?`) | Two-way by default |
| [x] | `wrap` | `Wrap` (`TextareaWrap?`) | `Off` doesn't wrap; `Soft`, `Hard` and null do |

### Events and methods

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionInput` | `IonInput` (`TextareaInputEventArgs.Value`) |
| [x] | `ionChange` | `IonChange` (`TextareaChangeEventArgs.Value`): on leaving the field after a change |
| [x] | `ionFocus` | `IonFocus` |
| [x] | `ionBlur` | `IonBlur` |
| [x] | `setFocus()` | `SetFocus()` |
| [x] | `getInputElement()` | `GetInputElement()`: the `TextareaNativeInput`, a `TextBox`, or null before the textarea is in a tree |

### Content

| ✓ | Ionic slot | Avonic |
|---|---|---|
| [x] | `label` | `LabelContent`, `LabelContentTemplate` |
| [x] | `start` | `StartContent`, `StartContentTemplate` |
| [x] | `end` | `EndContent`, `EndContentTemplate` |

### Resource keys

Each is in `Tokens/Textarea.axaml`. `CssUnset` ones fall back as Ionic's do; md's own default is an `Avonic.Textarea.Md.*` key.

| ✓ | Ionic | Key |
|---|---|---|
| [x] | `--background` | `Avonic.Textarea.Background` |
| [x] | `--border-color` | `Avonic.Textarea.BorderColor` |
| [x] | `--border-radius` | `Avonic.Textarea.BorderRadius` |
| [ ] | `--border-style` | — (Avalonia draws solid borders only) |
| [x] | `--border-width` | `Avonic.Textarea.BorderWidth` |
| [x] | `--color` | `Avonic.Textarea.Color` |
| [x] | `--highlight-color-focused` | `Avonic.Textarea.HighlightColorFocused` |
| [x] | `--highlight-color-invalid` | `Avonic.Textarea.HighlightColorInvalid` |
| [x] | `--highlight-color-valid` | `Avonic.Textarea.HighlightColorValid` |
| [x] | `--highlight-height` | `Avonic.Textarea.HighlightHeight` |
| [x] | `--padding-start`, `-end`, `-top`, `-bottom` | `Avonic.Textarea.PaddingStart`, `PaddingEnd`, `PaddingTop`, `PaddingBottom` |
| [x] | `--placeholder-color` | `Avonic.Textarea.PlaceholderColor` |
| [x] | `--placeholder-font-style` | `Avonic.Textarea.PlaceholderFontStyle` |
| [x] | `--placeholder-font-weight` | `Avonic.Textarea.PlaceholderFontWeight` |
| [x] | `--placeholder-opacity` | `Avonic.Textarea.PlaceholderOpacity` |

## Behaviours

Each has a test in `AvonicTextareaTests`, named after its number in [PLAN.md](PLAN.md#behaviours).

- Typing raises `IonInput` with each value; Enter types a line break. Leaving the field after a change raises `IonChange` once.
- A value set in code raises nothing; an `IonInput` handler can replace what was typed.
- `Debounce` raises one `IonInput`, with the last value, after the wait.
- `ClearOnEdit` clears on the first key that edits, Enter included, once per focus.
- A click on the label focuses the field; in an item, so does a tap on the item's padding.
- `Rows`, `AutoGrow`, `Cols` and `Wrap` size the field as Chromium sizes a `<textarea>`: 19 px a line for 16 px Roboto.
- The five label placements, the floating label, the highlight, the solid and outline looks, the notch, round shapes, helper
  and error text, the counter, disabled and read-only, all measured against Chromium.
- The native textarea is named by the label and described by the helper or error text.

## Extensions

Not in Ionic's API, but needed by the theme:

- `HighlightBrush`: Ionic's internal `--highlight-color`, which the theme works out per state.
- `CounterText`: the counter's text, or null when there's none.
- `PlacementConverter` and `VerticalPaddingConverter`, for the theme. It also uses `ion-input`'s `InputWrapperPanel` and converters.
- `TextareaNativeInput`: the native `<textarea>`, a `TextBox` with `Rows`, `Cols` and `AutoGrow`, and Chromium's `line-height: normal`.

## Known gaps

See [COMPARISON.md](COMPARISON.md#deliberate-differences-and-gaps): `cols` follows Chromium on Windows in a phone viewport, which
Chromium on Android would make wider; a click on the label while focused keeps focus; an icon-only button in a slot keeps its own
look; an app's background, border and top padding keys win where Ionic's host classes would; no `align-self: baseline` in an item;
the notch has no `max-width`; bottom text is 1 px taller. Not tried in a real window, with a soft keyboard, or on a device.
