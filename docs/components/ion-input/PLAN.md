# ion-input + ion-input-password-toggle — Plan

A one-line text field: a label in one of five placements, an Avalonia `TextBox` as the native `<input>`, start and end slots, a
clear button, helper and error text, a counter, and md's three looks (no fill, `solid`, `outline` with a notch). The password
toggle is a round clear `ion-button` in the end slot that flips the input between `password` and `text`. Measurements are from
real Ionic 8.8.1 in Chromium, md, at 390 px wide, wrapped in `ion-app`, with throwaway Playwright scripts (a mouse and keyboard
in a desktop context; the frame in a mobile one). The commands are at the top of [COMPARISON.md](COMPARISON.md).

## Fidelity checklist: ion-input

`node tools/ionic-api.js ion-input`.

### Props (35)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `autocapitalize` | `Autocapitalize` (`string`, `"off"`) | Ionic's type is `string`, so it stays one. `on`, `sentences`, `words` → `TextInputOptions.AutoCapitalization`; `characters` → `TextInputOptions.Uppercase`; `off`, `none` and anything else → neither. Avalonia has no words-only mode |
| [ ] | `autocomplete` | — | Not applicable: the browser's autofill. Avalonia has none |
| [x] | `autocorrect` | `Autocorrect` (`InputAutocorrect`, `Off`) | `On` → `TextInputOptions.ShowSuggestions`, which is what Android's autocorrect is |
| [x] | `autofocus` | `Autofocus` (`bool`) | The native input takes focus when it's first loaded, as Chromium focuses an inserted `autofocus` element |
| [x] | `clearInput` | `ClearInput` (`bool`) | |
| [x] | `clearInputIcon` | `ClearInputIcon` (`string?`) | An `AvonicIcon.Icon`: a name or a source. Null is md's `close-sharp` |
| [x] | `clearOnEdit` | `ClearOnEdit` (`bool?`) | Null is `Type == Password` |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`. Only the focused highlight reads it |
| [x] | `counter` | `Counter` (`bool`) | Only with `Maxlength` |
| [x] | `counterFormatter` | `CounterFormatter` (`Func<int, int, string>?`) | An exception falls back to the default text, as Ionic's `try` does |
| [x] | `debounce` | `Debounce` (`int?`, ms) | Delays `IonInput` only, on `AvonicConfig.Schedule`, as `setTimeout`. 0 is still a delay, as in Ionic |
| [x] | `disabled` | `IsEnabled` | Mapping rule |
| [x] | `enterkeyhint` | `Enterkeyhint` (`InputEnterkeyhint?`) | → `TextInputOptions.ReturnKeyType`: `Done`, `Enter` (`Return`), `Go`, `Next`, `Previous`, `Search`, `Send` |
| [x] | `errorText` | `ErrorText` (`string?`) | Shown instead of the helper text while the input has both style classes `ion-touched` and `ion-invalid` |
| [x] | `fill` | `Fill` (`InputFill?`) | `Outline`, `Solid`; null is the default |
| [x] | `helperText` | `HelperText` (`string?`) | |
| [x] | `inputmode` | `Inputmode` (`InputInputmode?`) | → `TextInputOptions.ContentType`, beating `Type`'s: `Decimal` (`Number`), `Email`, `Numeric` (`Digits`), `Search`, `Tel` (`Digits`), `Text` (`Normal`), `Url`. `none` isn't declared: Avalonia can't keep the soft keyboard away from a focused `TextBox` |
| [x] | `label` | `Label` (`string?`) | Beats `LabelContent`, as Ionic renders the slot only when `label` is undefined. `""` still counts as a label, as in Ionic |
| [x] | `labelPlacement` | `LabelPlacement` (`InputLabelPlacement`, `Start`) | `End`, `Fixed`, `Floating`, `Stacked`, `Start` |
| [ ] | `max`, `min`, `step` | — | Not applicable: only for `type="number"` and the date types, as form constraints and the spin buttons, which a phone doesn't show. See `Type` |
| [x] | `maxlength` | `Maxlength` (`int?`) | `TextBox.MaxLength`: typing stops there. Also the counter's limit |
| [ ] | `minlength`, `multiple`, `pattern` | — | Not applicable: HTML form constraint validation, which only sets `:invalid` (`box-shadow: none` in md) and `validity`. Ionic's own validity is the app's `ion-invalid` class |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form |
| [x] | `placeholder` | `Placeholder` (`string?`) | `TextBox.PlaceholderText` |
| [x] | `readonly` | `Readonly` (`bool`) | `TextBox.IsReadOnly`; also hides the clear button and the password toggle |
| [x] | `required` | `Required` (`bool`) | `AutomationProperties.IsRequiredForForm` on the native input, as the mapping rule for `aria-required` says, and as `AvonicTextarea`; form validation isn't here |
| [x] | `shape` | `Shape` (`InputShape?`) | `Round`; null is the default |
| [ ] | `spellcheck` | — | Not applicable: Avalonia's `TextBox` has no spell checker to turn on |
| [x] | `type` | `Type` (`InputType`, `Text`) | `Email`, `Number`, `Password`, `Search`, `Tel`, `Text`, `Url`: the soft keyboard's `ContentType`, and `Password` masks with `•` and sets `IsSensitive`. The date and time types (`date`, `datetime-local`, `month`, `time`, `week`) aren't declared: Chromium gives them its own picker and segmented field, which is `ion-datetime`'s job here. Chromium also drops a non-number from a `number` input; this doesn't (a known gap) |
| [x] | `value` | `Value` (`string?`, `""`) | Two-way by default, as Ionic's is `mutable` and the input sets it. A number is the app's to format |

### Events (4), methods (2), slots (3)

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionInput` | `IonInputEvent`, `IonInput`, with `InputInputEventArgs` (`Value`) |
| [x] | `ionChange` | `IonChangeEvent`, `IonChange`, with `InputChangeEventArgs` (`Value`) |
| [x] | `ionFocus` | `IonFocusEvent`, `IonFocus` |
| [x] | `ionBlur` | `IonBlurEvent`, `IonBlur` |
| [x] | `setFocus()` | `SetFocus()` |
| [x] | `getInputElement()` | `GetInputElement()`, which returns the `TextBox` (`PART_NativeInput`), applying the template first if it hasn't been |
| [x] | `label` | `LabelContent`, `LabelContentTemplate` |
| [x] | `start` | `StartContent`, `StartContentTemplate` |
| [x] | `end` | `EndContent`, `EndContentTemplate` |

No shadow parts (it's scoped). Template parts, by Ionic's class: `PART_Wrapper` (`.input-wrapper`, a `Border`), `PART_Layout`
(the wrapper's flex box, an `InputWrapperPanel`), `PART_Label` (`.label-text-wrapper`), `PART_LabelText` (`.label-text`),
`PART_LabelContent` (the `label` slot), `PART_NativeWrapper` (`.native-wrapper`), `PART_Start`, `PART_End`, `PART_NativeInput`
(the `<input>`, a `TextBox`), `PART_ClearButton` (`.input-clear-icon`), `PART_Highlight` (`.input-highlight`),
`PART_OutlineStart`, `PART_OutlineNotch`, `PART_NotchSpacer`, `PART_OutlineEnd` (`.input-outline-*`), `PART_Bottom`
(`.input-bottom`), `PART_HelperText`, `PART_ErrorText` and `PART_Counter`.

### CSS variables (18)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Input.Background` | brush | `CssUnset`. Falls back to transparent; solid: step 50 (`#f2f2f2`), hover step 100, focus step 150 |
| [x] | `--border-color` | `Avonic.Input.BorderColor` | brush | `CssUnset`. Falls back to `--ion-item-border-color` → `--ion-border-color` → step 150 (`rgba(0,0,0,.13)`); solid step 500, outline step 300, hover and solid focus step 750; the highlight colour when focused and valid, or touched and invalid |
| [x] | `--border-radius` | `Avonic.Input.BorderRadius` | `double` | `CssUnset`. Falls back to 0; solid and outline 4, outline round 28, round 16 |
| [ ] | `--border-style` | — | — | Not declared: Avalonia draws only a solid border |
| [x] | `--border-width` | `Avonic.Input.BorderWidth` | `double` | 1; outline focused: `--highlight-height` |
| [x] | `--color` | `Avonic.Input.Color` | brush | `CssUnset`: `initial` falls back to the inherited text colour |
| [x] | `--highlight-color-focused` | `Avonic.Input.HighlightColorFocused` | brush | `CssUnset`, falling back to `Avonic.Color.PrimaryBrush`; a known `Color`'s base beats it |
| [x] | `--highlight-color-invalid` | `Avonic.Input.HighlightColorInvalid` | brush | `Avonic.Color.DangerBrush` |
| [x] | `--highlight-color-valid` | `Avonic.Input.HighlightColorValid` | brush | `Avonic.Color.SuccessBrush` |
| [x] | `--highlight-height` | `Avonic.Input.HighlightHeight` | `double` | 2 |
| [x] | `--padding-bottom`, `--padding-end`, `--padding-start`, `--padding-top` | `Avonic.Input.Padding*` | `double` | `CssUnset`, falling back to 0; solid and outline 16 at the start and end, outline round 32 |
| [x] | `--placeholder-color` | `Avonic.Input.PlaceholderColor` | brush | `CssUnset`: `initial` falls back to the input's own colour (measured black) |
| [x] | `--placeholder-font-style` | `Avonic.Input.PlaceholderFontStyle` | `FontStyle` | `CssUnset`: inherited |
| [x] | `--placeholder-font-weight` | `Avonic.Input.PlaceholderFontWeight` | `FontWeight` | `CssUnset`: inherited |
| [x] | `--placeholder-opacity` | `Avonic.Input.PlaceholderOpacity` | `double` | `--ion-placeholder-opacity`, 0.6 |

md's literals with no variable are in the theme: min heights 44 and 56, the label's 16 px gap, 200 px max and 100 px fixed width,
the transforms and their 150 ms `cubic-bezier(0.4, 0, 0.2, 1)`, the highlight's 200 ms `ease`, the wrapper's 15 ms background
transition, the clear button's 30 px, 22 px icon, step 600 colour and 0.5 focused opacity, the bottom's 5 px, 12 px text and
step 700 colour, the counter's 16 px gap and letter spacing, the notch's 4 px and 8 px, and 0.38 opacity when disabled, as one (an `OpacityMask`, `Avonic.Input.Md.DisabledMask`, as the parts overlap).

## Fidelity checklist: ion-input-password-toggle

`node tools/ionic-api.js ion-input-password-toggle`.

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | Passed to the button |
| [x] | `hideIcon` | `HideIcon` (`string?`) | Null is `eye-off` |
| [x] | `showIcon` | `ShowIcon` (`string?`) | Null is `eye` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `type` (internal) | internal | Read from the input around it, as Ionic's `connectedCallback` and the input's `onTypeChange` set it |

No events, methods, slots, parts or variables. Template part: `PART_Button` (the `ion-button`) with `PART_Icon`.

## Behaviours

From `input.js`, `input.md.css`, `input-password-toggle.js` and `utils/forms`, each checked in Chromium. Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | Typing sets `Value` and raises `IonInput` with it, each edit. Focus raises `IonFocus` first | `onInput`; measured `type`: `ionFocus`, `ionInput "h"`, `ionInput "hi"` |
| 2 | Enter raises `IonChange` with the value if it changed since focus or the last change; again, nothing | native `change`, `onChange`; measured `enter`, `enter2` |
| 3 | Blur raises `IonChange` if the value differs from the value at focus (or the last change), then `IonBlur`. A focus and blur with no edit raises only `IonFocus` and `IonBlur` | `onBlur`, `focusedValue`; measured `blur`, `focusBlurNoChange` |
| 4 | Setting `Value` in code raises nothing, and shows in the field. A value set in an `IonInput` handler replaces what was typed | `valueChanged`; measured `prog` |
| 5 | `Debounce`: `IonInput` waits that long after the last edit, and is raised once with the last value | `debounceEvent`; measured `debounce`: one `ionInput "abc"` |
| 6 | `ClearOnEdit` (default on for `Password`): the first key that isn't Enter, Tab, Shift, Meta, Alt or Control after focus clears the value (an `IonInput` with `""`) and then types; once per focus. An ignored key first doesn't use it up | `checkClearOnEdit`; measured `pShift`, `pArrow`, `pz`, `pTab` |
| 7 | `ClearInput`: a button shown only while there's a value, and not when read-only or disabled. A click clears the value, raises `IonInput` with `""`, and focuses the input (`IonFocus` first if it wasn't focused); a press on it doesn't take focus. Enter on it clears too; with keyboard focus it's at 0.5 opacity | `clearTextInput`, `onPointerDown`; measured `clear`, `clearBlur`, `clearUnfocused`, `tabTo`, `enterOnClear` |
| 8 | A click on the label or anywhere in the wrapper focuses the input. Not on the bottom text, and not on a button in a slot | `<label for>`, `onLabelClick`; measured `labelFocus` |
| 9 | `SetFocus()` focuses the native input; `GetInputElement()` returns it | `setFocus`, `getInputElement` |
| 10 | In an item, a tap on the item's padding focuses the input. The item counts it as an input | `ion-item`'s `getFirstInteractive`; measured `itemPadTap` |
| 11 | Label placements: start (label, 16 px, field), end (mirrored), fixed (label 100 px), each label at most 200 px and trimmed with an ellipsis; stacked and floating are a column, the label above at 0.75 scale and `translateY(50%)`; floating sits on the field (`translateY(100%)`, scale 1) until it has a value, focus, or anything in a slot. No label hides the label part and its gap | md.css; measured `scene1`, `scene3` |
| 12 | Floating: the field is transparent until the label floats (opacity, 150 ms) | `.input-label-placement-floating input { opacity: 0 }` |
| 13 | Heights: 44 at least; 56 for floating, stacked, solid and outline | md.css; measured |
| 14 | Focus outside an item, and not outline: a 2 px highlight at the bottom grows from the centre over 200 ms in the highlight colour. Not in an item, and not with `outline` | `shouldRenderHighlight`, `.input-highlight`; measured `focus_s`, `focus_n`, `focus_sep`, `focus_f` |
| 15 | The highlight colour is primary, or a known `Color`'s base; `ion-valid` makes it success, and `ion-touched` + `ion-invalid` danger, which wins. Focused floating or stacked labels take it, as do the caret, the bottom border (focused and valid, or invalid), and the outline when focused | md.css; measured `focus_*`, `l` |
| 16 | Solid: background step 50, a 1 px bottom border step 500, top corners 4. Hover (mouse): step 100 and step 750. Focused: step 150 and step 750 | md.css; measured `f`, `focus_s`, `hoverSolid` |
| 17 | Outline: a 1 px border step 300 with radius 4 (round 28, padding 32), drawn as start, notch and end; when the label floats, the notch's top border goes, and the notch is as wide as the label at 0.75 plus 8. The floating label sits on the border (`translateY(-32%)`) and takes no room. Focused: 2 px in the highlight colour. Hover: step 750 | md.css, `createNotchController`; measured `g`, `h`, `j`, `k`, `l`, `focus_o`, `hoverOutline` |
| 18 | Round shape: radius 16 (solid: top corners only), 28 with outline | md.css; measured `n`, `h` |
| 19 | Bottom: shown only with helper text, error text or a counter. 5 px above 12 px text; a top border with no fill; helper step 700; while touched and invalid the error text in the invalid colour replaces the helper. The counter `len / max` at the end, 16 px from the text | `renderBottomContent`, `getCounterText`; measured `f`, `i`, `k`, `l` |
| 20 | The clear button sits at the end of the field, 30 × 30, a 22 px `close-sharp` in step 600, or the item's colour in a coloured item | md.css; measured `j` |
| 21 | Start and end slots sit beside the field, 16 px from it | md.css `::slotted`; measured `g` |
| 22 | Disabled: 0.38 opacity, as one (an `OpacityMask`, as the parts overlap), no focus, no clear button, the password toggle hidden. Read-only: focusable, not editable, no clear button, the toggle hidden | `input-disabled`, md.css; measured `m` |
| 23 | Placeholder: the input's colour at 0.6 | `::placeholder`; measured `ph` |
| 24 | Type and the hints reach the platform: `Password` masks and is sensitive; `Type`, `Inputmode`, `Enterkeyhint`, `Autocapitalize`, `Autocorrect` set `TextInputOptions` | `type`, `inputmode`, `enterKeyHint`, ... on `<input>` |
| 25 | `Autofocus` focuses the field when it's loaded | `autoFocus` |
| 26 | Automation: the native input is named by the label (or the host's `AutomationProperties.Name`, which beats it, as `aria-label` does), and described by the error text when invalid and set, else the helper text; `Required` says it's required | `getLabelledById`, `getHintTextID`, `inheritAriaAttributes`, `required` |
| 27 | Password toggle: a click flips the input between `Password` and `Text` (setting `Type`), and the icon between `eye` and `eye-off`, named "Show password" / "Hide password". A press on it doesn't take focus from the field. A change of the input's `Type` in code updates it | `togglePasswordVisibility`, `onTypeChange`; measured `toggle0`, `toggle1` |
| 28 | RTL mirrors the row, the label's origin and the notch | measured `q` |

### Not reproduced

- **The native date and time pickers**, and number filtering: see `Type`.
- **`ionInputDidLoad` / `ionInputDidUnload`**: internal document events for Ionic's scroll assist, which isn't here.
- **The password toggle's warning** for a type other than text or password, and **`aria-pressed`**: Avalonia's automation has
  no pressed state for a plain button.
- **Composition**: `isComposing` stops Ionic writing the value back mid-IME; Avalonia's `TextBox` owns its composition.
- **A click on the label while focused**: Chromium blurs and focuses again (measured `labelClickFocused`: `ionBlur`, `ionFocus`,
  and an `ionChange` if edited); here the field keeps focus.

## Design

`AvonicInput : TemplatedControl, IItemControl`. Three slots, so a `TemplatedControl` with plain `ContentPresenter`s. The native
input is a `TextBox` with its own minimal `ControlTheme` (`Avonic.Input.NativeInput`, functional in Components, so a test with no
base theme can type into it): no border, no padding, no background, the placeholder and the presenter. The control keeps
`Value` and the `TextBox`'s `Text` in step with a guard, so only the user's edits raise `IonInput`.

```
Panel (root)
├─ Border PART_Wrapper                 .input-wrapper: padding, background, bottom border, radius
│  └─ Panel
│     ├─ Grid (outline only)            .input-outline-container: PART_OutlineStart, PART_OutlineNotch (PART_NotchSpacer), PART_OutlineEnd
│     ├─ InputWrapperPanel PART_Layout  row, row-reverse, column, or column with the label taking no room (outline)
│     │  ├─ Border PART_Label           .label-text-wrapper: TextBlock PART_LabelText or ContentPresenter PART_LabelContent
│     │  └─ DockPanel PART_NativeWrapper   PART_Start | PART_End | PART_ClearButton | PART_NativeInput
│     └─ Border PART_Highlight          .input-highlight, 1 px below the wrapper
└─ DockPanel PART_Bottom                .input-bottom: PART_Counter, PART_HelperText, PART_ErrorText
```

`InputWrapperPanel` is the flex box of `.input-wrapper` for the five placements, each measured: in a row the label takes its
width up to its `MaxWidth` and the field the rest; in a column the label's available width is `(width − inset) / scale`, as the
`max-width: calc(100% / 0.75)` and the outline's `calc((100% − padding − 8px) / 0.75)` say. `FlexRow` has neither a column nor
`max-width`.

Host classes become pseudoclasses: `:has-value`, `:has-focus`, `:label-floating`, `:fill-solid`, `:fill-outline`, `:shape-round`,
`:label-placement-*`, `:in-item`, `:in-item-color`, `:input-disabled`, `:label-text-wrapper-hidden`, and `:ion-hover` (a mouse or
pen over it, for `@media (any-hover: hover)`). `ion-touched`, `ion-invalid` and `ion-valid` are the app's style classes, read as they
are. `--highlight-color`, which the theme works out per state, is `HighlightBrush` on the control, so each part reads one value.

A click on the wrapper is `NativeButton` with no keys (a `<label>`'s activation on release); a press on a slotted button or the
clear button is that button's, which `NativeButton` already arranges. The clear button is a `Border` with its own
`NativeButton` (Enter and Space), as Ionic's is a native `<button>`.

## Dependencies

`ion-item` (#6), `ion-label` (#7), `ion-button` (#10), `ion-icon` (#3): all merged.
