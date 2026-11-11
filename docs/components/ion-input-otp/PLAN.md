# ion-input-otp — Plan

A row of one-character boxes for a one-time code: `length` native inputs in a flex row with a gap, optional separator dots
between them, and a description slot underneath. Typing fills a box and moves on, Backspace walks back, a paste or an autofill
spreads over the boxes, and the value is the boxes joined. Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px
wide, wrapped in `ion-app`, with throwaway Playwright scripts (a mouse and keyboard in a desktop context for the behaviours; the
frame in a mobile one). The commands are at the top of [COMPARISON.md](COMPARISON.md).

## Fidelity checklist

`node tools/ionic-api.js ion-input-otp`.

### Props (13)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `autocapitalize` | `Autocapitalize` (`string`, `"off"`) | As `AvonicInput`'s: `on`, `sentences`, `words` → `TextInputOptions.AutoCapitalization`; `characters` → `Uppercase`; anything else neither. On every box |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`. The focused highlight, and the boxes' border with `outline` (and focused with `solid`) |
| [x] | `disabled` | `IsEnabled` | Mapping rule. The boxes can't be focused; `:disabled` styles them |
| [x] | `fill` | `Fill` (`InputOtpFill`, `Outline`) | `Outline`, `Solid`. Ionic's type also allows `undefined`, which leaves neither class and draws an unstyled box; not declared |
| [x] | `inputmode` | `Inputmode` (`InputOtpInputmode?`) | → `TextInputOptions.ContentType` on each box: `Decimal` (`Number`), `Email`, `Numeric` and `Tel` (`Digits`), `Search`, `Text` (`Normal`), `Url`. Null is `numeric` for `Type == Number`, else `text`, as `getInputmode`. `none` isn't declared, as for `AvonicInput`: Avalonia can't keep the soft keyboard away from a focused `TextBox` |
| [x] | `length` | `Length` (`int`, 4) | The number of boxes. Ionic throws for a negative length (`Array(-1)`); here it's no boxes |
| [x] | `pattern` | `Pattern` (`string?`) | A regular expression for one character, tested as `^pattern$`, as Ionic's `validKeyPattern`. Null is `[\p{N}]` for `Number` and `[\p{L}\p{N}]` for `Text`. .NET's syntax, which shares `\p{…}` and classes with JavaScript's; a pattern .NET can't read logs a warning and accepts nothing, where Ionic's handlers throw |
| [x] | `readonly` | `Readonly` (`bool`) | Each box's `IsReadOnly`: typing does nothing. Ionic's own Backspace and paste handlers don't check it, so they still edit (measured); reproduced, see "For you to decide" in the PR |
| [x] | `separators` | `Separators` (`string?`) | `"all"`, or positions such as `"3"` or `"1,3"`: a dot after those boxes. Ionic's array form is the same numbers joined with commas. A string that isn't a list of numbers logs a warning and shows none; duplicates warn; positions past `length` warn and are ignored, as `processSeparators` |
| [x] | `shape` | `Shape` (`InputOtpShape`, `Round`) | `Rectangular`, `Round`, `Soft` |
| [x] | `size` | `Size` (`InputOtpSize`, `Medium`) | `Large`, `Medium`, `Small` |
| [x] | `type` | `Type` (`InputOtpType`, `Number`) | `Number`, `Text`: the default pattern and keyboard. The boxes are always text, as Ionic's `<input type="text">` |
| [x] | `value` | `Value` (`string?`, `""`) | Two-way by default, as Ionic's is mutable. Rewritten to what the boxes hold, as `initializeValues` does: cut to `length`, then characters the pattern refuses dropped. A number is the app's to format |

### Events (5), methods (1), slots, parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionInput` | `IonInputEvent`, `IonInput`, with `InputOtpInputEventArgs` (`Value`) |
| [x] | `ionChange` | `IonChangeEvent`, `IonChange`, with `InputOtpChangeEventArgs` (`Value`) |
| [x] | `ionComplete` | `IonCompleteEvent`, `IonComplete`, with `InputOtpCompleteEventArgs` (`Value`) |
| [x] | `ionFocus` | `IonFocusEvent`, `IonFocus` |
| [x] | `ionBlur` | `IonBlurEvent`, `IonBlur` |
| [x] | `setFocus(index?)` | `SetFocus(int? index = null)` |
| [x] | default `<slot>` | `Content`, `ContentTemplate`: the description under the boxes. `core.json` lists no slot, but `render()` has one, inside `.input-otp-description` |

No shadow parts (it's scoped). Template parts, by Ionic's class: `PART_Group` (`.input-otp-group`, an `InputOtpGroupPanel`, which
the control fills) and `PART_Description` (`.input-otp-description`, a `ContentPresenter`). In the group, the control makes one
`InputOtpBox` (`.native-input`, a `TextBox`) per box, and one `InputOtpSeparator` (`.input-otp-separator`) after each, shown where
`showSeparator` says. `.native-wrapper` has no element of its own: its `min-width` is the box's, which is all it does.

### CSS variables (23)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.InputOtp.Background` | brush | `CssUnset`. Outline: none (transparent); solid: step 50; outline disabled or read-only: step 50; solid disabled or read-only: step 100 |
| [x] | `--border-color` | `Avonic.InputOtp.BorderColor` | brush | `CssUnset`. Outline: step 300; solid: step 50; outline disabled: step 100; solid disabled or read-only: step 100. The highlight colour on the focused box, on every box when focused and valid, and when touched and invalid |
| [x] | `--border-radius` | `Avonic.InputOtp.BorderRadius` | `CornerRadius`, number or CSS string | `CssUnset`. Round 16, soft 8, rectangular 0. Read by `BorderRadius.ToCornerRadius`, so a percentage works |
| [x] | `--border-width` | `Avonic.InputOtp.BorderWidth` | `double` | 1; 2 on the focused box (a literal on `.native-input:focus`, which beats the key) |
| [x] | `--color` | `Avonic.InputOtp.Color` | brush | `CssUnset`: `initial` falls back to the inherited text colour; disabled: text step 650 |
| [x] | `--height` | `Avonic.InputOtp.Height` | `double` | `CssUnset`. Small 40, medium 48, large 56 |
| [x] | `--highlight-color-focused` | `Avonic.InputOtp.HighlightColorFocused` | brush | `CssUnset`, falling back to `Avonic.Color.PrimaryBrush`; a known `Color`'s base beats it |
| [x] | `--highlight-color-invalid` | `Avonic.InputOtp.HighlightColorInvalid` | brush | `CssUnset`, falling back to `Avonic.Color.DangerBrush` |
| [x] | `--highlight-color-valid` | `Avonic.InputOtp.HighlightColorValid` | brush | `CssUnset`, falling back to `Avonic.Color.SuccessBrush` |
| [x] | `--margin-bottom`, `--margin-end`, `--margin-start`, `--margin-top` | `Avonic.InputOtp.Margin*` | `double` | 0, on the group |
| [x] | `--min-width` | `Avonic.InputOtp.MinWidth` | `double` | 40: what a box shrinks to when the row is too narrow |
| [x] | `--padding-bottom`, `--padding-end`, `--padding-start`, `--padding-top` | `Avonic.InputOtp.Padding*` | `double` | 16 top and bottom, 0 at the sides, on the group |
| [x] | `--separator-border-radius` | `Avonic.InputOtp.SeparatorBorderRadius` | as `--border-radius` | 999 |
| [x] | `--separator-color` | `Avonic.InputOtp.SeparatorColor` | brush | `CssUnset`, falling back to `Avonic.BackgroundColorStep150Brush` |
| [x] | `--separator-height` | `Avonic.InputOtp.SeparatorHeight` | `double` | `CssUnset`, falling back to `--separator-width` |
| [x] | `--separator-width` | `Avonic.InputOtp.SeparatorWidth` | `double` | 8 |
| [x] | `--width` | `Avonic.InputOtp.Width` | `double` | `CssUnset`. Small 40, medium 48, large 56: the box's flex basis |

md's literals with no variable are in the theme: the host's 14 px font, the gap (8 small, 12 otherwise), the box's 1 × 2 px padding
(Chromium's for an `<input>`), its centred text, and the description's 12 px font, 20 px line height, step 700 colour (text step 300)
and centred text. The per-size, per-shape and per-fill defaults are `Avonic.InputOtp.Md.*` keys.

## Behaviours

From `input-otp.js` and `input-otp.md.css`, each checked in Chromium (scripts `behave.js`, `behave2.js`, `measure.js`; the traces
are in COMPARISON.md). Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | `Length` boxes. `Value` is cut to `Length` and its characters the pattern refuses are dropped, and the value is rewritten to that; each box shows its character. `"1a2"` → boxes `1`, `2` and value `"12"` | `initializeValues`, `valueChanged`; measured |
| 2 | Typing a valid character in an empty box fills it, raises `IonInput` with the value, and focuses the next box | `onInput` case 3, `focusNext`; measured `type 1` |
| 3 | A character the pattern refuses changes nothing and raises nothing. `Number` takes digits, `Text` letters and digits, and `Pattern` replaces both | `onInput` case 2, `getPattern`; measured `type a`, `text type -`, `text é` |
| 4 | When an edit leaves every box filled, `IonComplete` follows `IonInput`, every time | `updateValue`; measured `type 34`, `type 5 in full last box` |
| 5 | Typing in a filled box inserts there and shifts the rest right while the last box is empty, and keeps focus; with the last box filled, it replaces the box's character with the one typed (by the caret) | `onInput` cases 4 and 5; measured `type 9`, `type 7 caret start`, `box2 caret 0 type 7 full` |
| 6 | Backspace in a filled box removes its character and shifts the rest left, whatever the caret, and keeps focus. In an empty box, focus moves back, and the key then deletes the character there, as Chromium's default action follows focus. Delete, or Backspace over a selection, edits natively, and an emptied box moves focus on, which the redirect of 8 brings back | `onKeyDown`, `onInput` case 3; measured `backspace on last`, `backspace again`, `box1 caret0 backspace`, `box0 caret0 Delete` |
| 7 | Right moves to the next box only from a filled box; Left moves to the previous one; neither moves the caret. In RTL they swap | `onKeyDown`; measured `ArrowLeft`, `ArrowRight` |
| 8 | Focus that isn't from Tab or an arrow lands on the box if it's filled, else on the first empty box, or the last if all are filled. Tab in a box sets the flag, so the next focus, wherever it comes from, isn't redirected | `onFocus`, `isKeyboardNavigation`; measured `click empty box 2`, `click filled box 0`, `click empty box 3 after Tab` |
| 9 | One box is a tab stop: the focused one, and after an edit or a blur, the first empty box, or the last. An empty box that isn't the tab stop is hidden from automation | `onFocus`, `updateTabIndexes`; measured `tab`, `hidden` |
| 10 | `IonFocus` once when focus enters the group, `IonBlur` when it leaves, then `IonChange` if the value isn't what it was at focus, even if code set it. Moving between boxes raises neither | `onFocus`, `onBlur`; measured `blur to other`, `value 43 while focused` |
| 11 | Paste: the valid characters from the first box on, the value set to them (so a paste with none clears it), `IonInput`, `IonComplete` if full, and focus on the next empty box or the last. An empty clipboard only raises `IonInput` | `onPaste`; measured `paste "x1 2-3"`, `paste "ab"`, `paste "987654"` |
| 12 | Autofill: an edit that adds more than one character spreads the valid ones from the first box, and focuses the next empty box (or the last) 20 ms later | `onInput` case 1; measured `insertText 56`, `insertText 8a9` |
| 13 | `SetFocus(index)` focuses that box, clamped, which 8 then redirects; `SetFocus()` focuses the tab stop | `setFocus`; measured `setFocus(1)`, `setFocus(3) on empty` |
| 14 | `Value` set in code raises nothing, shows in the boxes and moves the tab stop | `valueChanged`; measured `value set in code` |
| 15 | Separators: a dot after each listed box, not after the last; `"all"` between every pair | `processSeparators`, `showSeparator`; measured scenes |
| 16 | Disabled: no box takes focus. Read-only: a box takes focus, typing does nothing, and Backspace and paste still edit, as in Ionic | `disabled`, `readOnly`; measured `readonly *`, `click disabled` |
| 17 | `Type`, `Inputmode` and `Autocapitalize` reach each box's `TextInputOptions` | `inputmode`, `autoCapitalize` |
| 18 | Automation: the group is a `Group` named "One-time password input", or the host's own name, which `inheritAriaAttributes` moves to it; each box is named "Input n of length" | `render` |
| 19 | The description: under the group, 12 px with a 20 px line, centred, text step 300; nothing when there's no content | `.input-otp-description`; measured |
| 20 | Layout: boxes `width` × `height` by size (40, 48, 56), gap 12 (8 small), 16 px above and below, centred; too narrow, boxes shrink to `min-width` 40, and past that the row overflows both sides, centred. Separators 8 × 8, round, step 150, with the gap on both sides | md.css; measured `REST` |
| 21 | Look by state: radius by shape; outline step 300 border; solid step 50; disabled and read-only colours; the focused box 2 px in the highlight colour, which is the caret's too; `ion-valid` while focused and `ion-touched ion-invalid` border every box in it; with a `Color`, outline boxes are bordered in its base at 0.6, at 0.3 disabled, danger when `ion-invalid` and success when `ion-valid` | md.css; measured `REST`, `FOCUS` |

### Not reproduced

- **The `requestAnimationFrame` clear after an autofill with no valid characters**: it undoes a browser's autofill writing every
  `autocomplete="one-time-code"` input, which nothing does here. The boxes are already cleared.
- **`autocomplete="one-time-code"`**: Avalonia 12's `TextInputOptions` has no content type for a one-time code.
- **`cursor: not-allowed`** when disabled: a desktop pointer only, and a disabled Avalonia control gets no pointer.
- **The `input-otp-description-hidden` class's first render**: Ionic looks for the description before it exists, so it's never
  hidden at first, and an empty one is 0 high anyway. Here it's hidden when there's no content, or only white space.
- **Stencil's patching of `tabIndex`**, which can leave two boxes at 0 after an autofill (measured `-1,0,0,-1`): one tab stop here.
- **Chromium's undo** in a box: the boxes have none, as Chromium's history goes when the value is set in code.

## Design

`AvonicInputOtp : ContentControl`, as it has one slot. The boxes are made in code, `Length` of them, so the template can't hold
them, and a `ControlTheme`'s `/template/` rules can't reach them. So the box and the separator are types of their own,
`InputOtpBox : TextBox` and `InputOtpSeparator : Border`, each with its own `ControlTheme`, and the host copies its state to each box
as pseudoclasses: `:fill-*`, `:shape-*`, `:size-*`, `:has-focus`, `:readonly`, `:ion-color`, and `:ion-valid`, `:ion-invalid`,
`:ion-touched` from the app's classes. The box's own `:focus` and `:disabled` are Avalonia's. Each md rule on `.native-input` then
reads as it does in CSS, and costs nothing in `Styles/`. `--highlight-color` is `HighlightBrush` on the host, as `AvonicInput`'s,
which the host's theme works out per state and each box reads.

```
StackPanel (root)
├─ InputOtpGroupPanel PART_Group        .input-otp-group: margin, padding, a centred flex row with a gap
│  ├─ InputOtpBox, InputOtpSeparator    one pair per box; a separator shows only where showSeparator says
│  └─ …
└─ ContentPresenter PART_Description   .input-otp-description: the default slot
```

`InputOtpGroupPanel` is `display: flex; justify-content: center; align-items: center; gap`: each box's basis is
`InputOtpGroupPanel.FlexBasis` (`--width`), as a `Width` would stop it shrinking; boxes shrink in proportion to it down to their
`MinWidth`, separators don't (`flex-shrink: 0`), and a row that still doesn't fit overflows both sides. `FlexRow` has no gap, no
centring and no basis of its own, so it isn't reused.

The box's input is handled as Chromium hands it to Ionic: text input (typing, a soft keyboard, an IME's commit) in a tunnel
handler, which works out what the native input would hold and its caret, and runs `onInput` with them; Backspace and the arrows in a
tunnel key handler, as `onKeyDown`; paste through `PastingFromClipboard`, handled, and the clipboard's text read; and any other
native edit (a selection deleted, Delete, cut) through the text's change, as an `input` event. Focus and blur are the boxes'
`GotFocus` and `LostFocus`, with `NewFocusedElement` as `relatedTarget`.

Reused from `Controls/Input/` without changing it: nothing public fits (its enums and event args are `ion-input`'s, and Ionic's
names differ), so `InputOtp*` types of the same shape are added, by the mapping rule.

## Dependencies

`ion-input` (#24): merged. Nothing else.
