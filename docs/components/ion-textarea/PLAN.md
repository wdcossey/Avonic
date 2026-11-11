# ion-textarea — Plan

A multi-line text field: `ion-input`'s label in one of five placements, start and end slots, helper and error text, a counter
and md's three looks (no fill, `solid`, `outline` with a notch), around a native `<textarea>` with `rows`, `cols`, `wrap` and
`autoGrow`. No clear button and no `type`. Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide, wrapped in
`ion-app`, with throwaway Playwright scripts (a mouse and keyboard in a desktop context for the events; a mobile context for
the boxes). The commands are at the top of [COMPARISON.md](COMPARISON.md).

It shares `ion-input`'s machinery (#24), and reuses what `Controls/Input/` makes public without editing it: `InputWrapperPanel`
for the label's five layouts, and `AvonicInput`'s converters for the padding, radius, borders and notch in the theme. What
would better move to a shared place is in the PR, under "Proposed for shared files".

## Fidelity checklist

`node tools/ionic-api.js ion-textarea`.

### Props (29)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `autoGrow` | `AutoGrow` (`bool`) | The field grows and shrinks with its lines, never under `Rows`; with it, `Cols` does nothing, as `:host([cols]:not([auto-grow]))` says |
| [x] | `autocapitalize` | `Autocapitalize` (`string`, `"none"`) | As `ion-input`'s: `on`, `sentences`, `words` → `TextInputOptions.AutoCapitalization`; `characters` → `Uppercase` |
| [x] | `autofocus` | `Autofocus` (`bool`) | As `ion-input`'s |
| [x] | `clearOnEdit` | `ClearOnEdit` (`bool`) | Unlike the input's, Enter counts as an edit, and there's no password default |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`; only the focused highlight reads it |
| [x] | `cols` | `Cols` (`int?`) | The host is as wide as its content (`width: fit-content`), the field `Cols` × the `x` glyph's advance, rounded, + 4: measured 84, 164, 324 for 10, 20, 40 (see below) |
| [x] | `counter` | `Counter` (`bool`) | Only with `Maxlength` |
| [x] | `counterFormatter` | `CounterFormatter` (`Func<int, int, string>?`) | An exception falls back to the default text |
| [x] | `debounce` | `Debounce` (`int?`, ms) | Delays `IonInput` only, on `AvonicConfig.Schedule` |
| [x] | `disabled` | `IsEnabled` | Mapping rule |
| [x] | `enterkeyhint` | `Enterkeyhint` (`TextareaEnterkeyhint?`) | → `TextInputOptions.ReturnKeyType` |
| [x] | `errorText` | `ErrorText` (`string?`) | While the textarea has both style classes `ion-touched` and `ion-invalid` |
| [x] | `fill` | `Fill` (`TextareaFill?`) | `Outline`, `Solid` |
| [x] | `helperText` | `HelperText` (`string?`) | |
| [x] | `inputmode` | `Inputmode` (`TextareaInputmode?`) | → `TextInputOptions.ContentType`. `none` isn't declared, as for the input |
| [x] | `label` | `Label` (`string?`) | Beats `LabelContent` |
| [x] | `labelPlacement` | `LabelPlacement` (`TextareaLabelPlacement`, `Start`) | `End`, `Fixed`, `Floating`, `Stacked`, `Start` |
| [x] | `maxlength` | `Maxlength` (`int?`) | `TextBox.MaxLength`; also the counter's limit |
| [ ] | `minlength` | — | Not applicable: HTML form constraint validation |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form |
| [x] | `placeholder` | `Placeholder` (`string?`) | |
| [x] | `readonly` | `Readonly` (`bool`) | `TextBox.IsReadOnly` |
| [ ] | `required` | — | Not applicable, as for the input |
| [x] | `rows` | `Rows` (`int?`) | Null is HTML's 2. The field is that many lines high (19 px each for 16 px Roboto), and scrolls past them |
| [x] | `shape` | `Shape` (`TextareaShape?`) | `Round` |
| [ ] | `spellcheck` | — | Not applicable: Avalonia's `TextBox` has no spell checker |
| [x] | `value` | `Value` (`string?`, `""`) | Two-way by default |
| [x] | `wrap` | `Wrap` (`TextareaWrap?`) | `Off` doesn't wrap, and scrolls sideways; `Soft` and `Hard` (and null) wrap. `Hard`'s only other effect is on a submitted form, which isn't here |

### Events (4), methods (2), slots (3)

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionInput` | `IonInputEvent`, `IonInput`, with `TextareaInputEventArgs` (`Value`) |
| [x] | `ionChange` | `IonChangeEvent`, `IonChange`, with `TextareaChangeEventArgs` (`Value`) |
| [x] | `ionFocus` | `IonFocusEvent`, `IonFocus` |
| [x] | `ionBlur` | `IonBlurEvent`, `IonBlur` |
| [x] | `setFocus()` | `SetFocus()` |
| [x] | `getInputElement()` | `GetInputElement()`: the native textarea, a `TextareaNativeInput` (a `TextBox`), applying the template first |
| [x] | `label` | `LabelContent`, `LabelContentTemplate` |
| [x] | `start` | `StartContent`, `StartContentTemplate` |
| [x] | `end` | `EndContent`, `EndContentTemplate` |

No shadow parts (it's scoped). Template parts, by Ionic's class: `PART_Wrapper` (`.textarea-wrapper`), `PART_Layout` (its flex
box, an `InputWrapperPanel`), `PART_Label` (`.label-text-wrapper`), `PART_LabelText`, `PART_LabelContent`, `PART_NativeWrapper`
(`.textarea-wrapper-inner`), `PART_StartWrapper`, `PART_Start`, `PART_EndWrapper`, `PART_End`, `PART_NativeInput` (the
`<textarea>`), `PART_Highlight`, `PART_OutlineContainer`, `PART_OutlineStart`, `PART_OutlineNotch`, `PART_NotchSpacer`,
`PART_OutlineEnd`, `PART_Bottom`, `PART_HelperText`, `PART_ErrorText`, `PART_Counter`.

### CSS variables (18)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Textarea.Background` | brush | `CssUnset`: transparent; solid step 50, hover step 100, focus step 150 |
| [x] | `--border-color` | `Avonic.Textarea.BorderColor` | brush | `CssUnset`: `--ion-item-border-color` (step 150); solid step 500, outline step 300, hover step 750; the highlight colour when focused and valid, or touched and invalid |
| [x] | `--border-radius` | `Avonic.Textarea.BorderRadius` | `double`, `CornerRadius` or CSS string | `CssUnset`: 0; solid and outline 4, outline round 28, round 16 |
| [ ] | `--border-style` | — | — | Not declared: Avalonia draws only a solid border |
| [x] | `--border-width` | `Avonic.Textarea.BorderWidth` | `double` | 1; outline focused: `--highlight-height` |
| [x] | `--color` | `Avonic.Textarea.Color` | brush | `CssUnset`: the inherited colour |
| [x] | `--highlight-color-focused` | `Avonic.Textarea.HighlightColorFocused` | brush | `CssUnset`: primary; a known `Color`'s base beats it |
| [x] | `--highlight-color-invalid` | `Avonic.Textarea.HighlightColorInvalid` | brush | danger |
| [x] | `--highlight-color-valid` | `Avonic.Textarea.HighlightColorValid` | brush | success |
| [x] | `--highlight-height` | `Avonic.Textarea.HighlightHeight` | `double` | 2 |
| [x] | `--padding-top` | `Avonic.Textarea.PaddingTop` | `double` | `CssUnset`: 18; 0 for floating and stacked. On the field, the slots and a row's label, not the wrapper |
| [x] | `--padding-bottom` | `Avonic.Textarea.PaddingBottom` | `double` | `CssUnset`: 8. The same parts |
| [x] | `--padding-start`, `--padding-end` | `Avonic.Textarea.PaddingStart`, `PaddingEnd` | `double` | `CssUnset`: 0; solid and outline 16, outline round 32. On the wrapper and the bottom |
| [x] | `--placeholder-color` | `Avonic.Textarea.PlaceholderColor` | brush | `CssUnset`: the field's colour |
| [x] | `--placeholder-font-style` | `Avonic.Textarea.PlaceholderFontStyle` | `FontStyle` | `CssUnset`: inherited |
| [x] | `--placeholder-font-weight` | `Avonic.Textarea.PlaceholderFontWeight` | `FontWeight` | `CssUnset`: inherited |
| [x] | `--placeholder-opacity` | `Avonic.Textarea.PlaceholderOpacity` | `double` | `CssUnset`: `--ion-placeholder-opacity`, 0.6 |

md's literals with no variable are in the theme: min heights 44 and 56, which the wrapper and `.textarea-wrapper-inner` both
inherit; the label's 16 px gap, 200 px max and 100 px fixed width; the field's and slots' 8 px (outline 12 px) above in a
column; the transforms and their 150 ms `cubic-bezier(0.4, 0, 0.2, 1)`; the highlight's 200 ms; the wrapper's 15 ms background;
the bottom's 5 px, 12 px text and step 700 colour; the counter's 16 px and letter spacing; the notch's 4 px and 8 px; 0.38
opacity when disabled.

## Behaviours

From `textarea.js` and `textarea.md.css`, each checked in Chromium. Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | Typing sets `Value` and raises `IonInput` each edit, after `IonFocus` | `onInput`; measured `type` |
| 2 | Enter types a line break and raises `IonInput`, not `IonChange` | native `change` only on blur; measured `enter` |
| 3 | Blur raises `IonChange` if the value differs from the value at focus (also when set in code while focused), then `IonBlur`; no edit raises only `IonFocus`, `IonBlur` | `onBlur`, `onChange`; measured `blur`, `focusBlurNoChange`, `grow-code` |
| 4 | A value set in code raises nothing, and shows in the field; an `IonInput` handler can replace it | `valueChanged` |
| 5 | `Debounce`: `IonInput` once, with the last value, after the wait | `debounceEvent` |
| 6 | `ClearOnEdit`: the first key after focus that isn't Tab, Shift, Meta, Alt or Control clears the value (`IonInput ""`), Enter and arrows included; once per focus | `checkClearOnEdit`; measured `coeShift`, `coeEnter` (`""` then `"\n"`), `coeArrow` |
| 7 | A click in the wrapper (label, padding) focuses the field; not on the bottom text or a button in a slot | `<label for>` |
| 8 | `SetFocus()` focuses the native textarea; `GetInputElement()` returns it | |
| 9 | In an item, a tap on the item's padding focuses the textarea | `ion-item`'s `getFirstInteractive` |
| 10 | Rows: the field is `Rows` lines high (default 2), 19 px a line for 16 px Roboto, plus `--padding-top` and `--padding-bottom` (18 + 8 in a row): 64 for 2, 102 for 4, 45 for 1. A longer value scrolls inside it | measured `a`, `h`, `r1`, `e` (scrollHeight 102 in 64) |
| 11 | `AutoGrow`: as many lines as the value, never fewer than `Rows`; grows and shrinks as it's typed or set: 45, 64, 83, 64 with `rows="1"` | `runAutoGrow`, `.native-wrapper::after`; measured `grow*` |
| 12 | `Cols` without `AutoGrow`: the host is as wide as its content, the field `Cols` × 8 + 4 for 16 px Roboto; with `AutoGrow`, full width | `:host([cols]:not([auto-grow]))`; measured `c10`, `c20`, `c40`, `c20g` |
| 13 | `Wrap`: `Off` keeps a line on one line; otherwise it wraps. A line break is a line feed, typed or pasted (CR LF and CR become LF), and an unfocused field shows its first lines | `wrap`; the HTML value's line breaks |
| 14 | Row placements: the label at the top, 18 px above and 8 below its text (45 high), 16 px from the field; end mirrored; fixed 100 px; at most 200 | md.css; measured `a`, `e`, `f` |
| 15 | Column placements: the label above (19), the field 8 px under it (outline: 12, with the label out of flow), the field's box at least 56 so the host is 75; floating sits on the field (`translateY(100%)`) until it has a value, focus or slotted content, then `translateY(50%) scale(0.75)`; the field is transparent until focus or a value | md.css; measured `b`, `c`, `d`, `j`, `k` |
| 16 | Heights: 44 at least; 56 for floating, stacked, solid and outline, on both the wrapper and `.textarea-wrapper-inner` | md.css |
| 17 | The highlight: 2 px under the wrapper, from the centre over 200 ms when focused; not in an item or with the outline | `shouldRenderHighlight`, `.textarea-highlight` |
| 18 | Highlight colours: primary, a `Color`'s base, `ion-valid` success, `ion-touched` + `ion-invalid` danger; the focused floating or stacked label, the caret, and the borders take it | md.css |
| 19 | Solid: step 50 with a 1 px step 500 bottom border and top corners 4; hover (mouse) step 100 and 750; focused step 150 and 750 | md.css |
| 20 | Outline: a 1 px step 300 border in three parts, the notch opening for a floating label; focused 2 px in the highlight; round 28 with 32 padding | md.css, `createNotchController`; measured `j`, `k` |
| 21 | Bottom: only with helper or error text or a counter; 5 px above 12 px text; error replaces helper when touched and invalid; the counter at the end | `renderBottomContent`; measured `i`, `l` |
| 22 | Slots: at the top, with the field's padding above and below them, 16 px from the field; in a column 8 px lower (outline 12). A slotted button keeps only its 4 px below (measured `0 0 4 16` at the end) | md.css `.start-slot-wrapper`, `::slotted`; measured `q` |
| 23 | Disabled: 0.38, no focus. Read-only: focusable, not editable | `textarea-disabled` |
| 24 | Placeholder: the field's colour at 0.6 | `::placeholder` |
| 25 | The hints reach the platform: `Inputmode`, `Enterkeyhint`, `Autocapitalize` set `TextInputOptions`; the field takes Return | `<textarea>` attributes |
| 26 | `Autofocus` focuses the field when it's first loaded | `autoFocus` |
| 27 | Automation: named by the label (or the host's name), described by the error text when invalid, else the helper | `inheritAriaAttributes`, `getHintTextID` |
| 28 | RTL mirrors the row, the label's origin and the outline | md.css |

### Not reproduced

- **`ionInputDidLoad` / `ionInputDidUnload`**: Ionic's scroll assist, not here.
- **`onClickCapture`**, which re-raises a click on the `<textarea>` as the host's: Avalonia has no DOM `click` to re-target.
- **`:host-context(ion-item) { align-self: baseline }`**: the textarea sits in an item as any child does. Alone in the item, as
  measured, the box is the same.
- **`:host(.in-item) .textarea-highlight { bottom: 0 }`** is dead: `textarea.js` never sets `in-item`, and draws no highlight in
  an item.
- **`.item-textarea`** on `ion-item`: no Ionic 8 textarea emits `ionStyle`, so it's dead, as `.item-input` is for the input.
- **The icon-only `ion-button` rule** (`::slotted(ion-button[slot].button-has-icon-only)`: 50% radius, 8 px padding, 40 px):
  the button keeps its own look, as in `ion-input`.
- **The soft keyboard**: as `ion-input`. Unlike the input, a value set during an IME's composition is written at once, as
  `textarea.js`'s `valueChanged` has no `isComposing` check.

## Design

`AvonicTextarea : TemplatedControl, IItemControl`, the same shape as `AvonicInput`: three slots, so plain `ContentPresenter`s;
`Value` kept in step with the native `Text` through its synchronous property change, with a guard, so only the user's edits raise
`IonInput`; a click in the wrapper through `NativeButton` with no keys.

The native `<textarea>` is `TextareaNativeInput`, a `TextBox` that takes Return, with its own minimal theme
(`Avonic.Textarea.NativeInput`). It does what Chromium's `<textarea>` does to its own size: it's `Rows` lines high (more with
`AutoGrow`, as many as its value has), and `Cols` × the rounded `x` advance + 4 wide when asked. Its lines are
`line-height: normal` as Chromium works it out, each of the font's ascent and descent rounded (19 for 16 px Roboto, where
Avalonia's own is 18.75), set at style priority, so the rows add up as in Chromium.

```
Grid (root)
├─ Grid PART_OutlineContainer          .textarea-outline-container (outline only)
├─ Border PART_Wrapper                 .textarea-wrapper: padding start and end, background, bottom border, radius, min height
│  └─ InputWrapperPanel PART_Layout    row, row-reverse or column; the label out of flow with the outline
│     ├─ Border PART_Label             at the top: padding-top and -bottom in a row
│     └─ Grid PART_NativeWrapper       .textarea-wrapper-inner: min height again
│        ├─ Border PART_StartWrapper > ContentPresenter PART_Start
│        ├─ TextareaNativeInput PART_NativeInput   padding-top and -bottom; 8 px above in a column, 12 with the outline
│        └─ Border PART_EndWrapper > ContentPresenter PART_End
├─ Border PART_Highlight
└─ Border PART_Bottom                  helper or error text, counter
```

Pseudoclasses, from Ionic's host classes and reflected attributes: `:has-value`, `:has-focus`, `:label-floating`,
`:fill-solid`, `:fill-outline`, `:shape-round`, `:label-placement-*`, `:textarea-disabled`, `:readonly`, `:auto-grow`, `:cols`,
and `:in-item`, `:label-text-wrapper-hidden`, `:has-bottom`, `:ion-hover` for the template.

## Dependencies

`ion-input` (#24), `ion-item` (#6), `ion-button` (#10): merged.
