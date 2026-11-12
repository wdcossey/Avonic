# ion-input-otp

Status: done · [#27](https://github.com/wdcossey/Avonic/issues/27)

`AvonicInputOtp` is a row of one-character boxes for a one-time code. Typing fills a box and moves on, Backspace walks back, a
paste or a soft keyboard's suggested code spreads over the boxes, and `Value` is the boxes joined. Its content is the description
under the boxes. The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in
[COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicInputOtp Length="6" Separators="3" IonComplete="OnCodeComplete">
    Didn't get a code? Resend
</c:AvonicInputOtp>

<c:AvonicInputOtp Fill="Solid" Shape="Soft" Size="Small" Type="Text" Value="{Binding Code}" />
```

Validation is the app's, as in Ionic: add the style classes `ion-touched` and `ion-invalid` (`AvonicInputOtp.TouchedClass`,
`InvalidClass`) to border every box in the invalid colour, or `ion-valid` (`ValidClass`) for the valid colour while it's focused.

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `autocapitalize` | `Autocapitalize` (`string`, `"off"`) | `TextInputOptions.AutoCapitalization`, or `Uppercase` for `characters`, on each box |
| [x] | `color` | `Color` | The focused highlight, and the outline's border |
| [x] | `disabled` | `IsEnabled` | |
| [x] | `fill` | `Fill` (`InputOtpFill`) | `Outline` (default), `Solid` |
| [x] | `inputmode` | `Inputmode` (`InputOtpInputmode?`) | `TextInputOptions.ContentType`; null is numeric for `Number`, text for `Text`. No `none` |
| [x] | `length` | `Length` (`int`, 4) | |
| [x] | `pattern` | `Pattern` (`string?`) | One character's regular expression, .NET's syntax; null is `[\p{N}]` or `[\p{L}\p{N}]` by type |
| [x] | `readonly` | `Readonly` | Typing does nothing; Backspace and paste still edit, as in Ionic |
| [x] | `separators` | `Separators` (`string?`) | `"all"` or positions such as `"1,3"`; Ionic's array is the same numbers with commas |
| [x] | `shape` | `Shape` (`InputOtpShape`) | `Rectangular`, `Round` (default), `Soft` |
| [x] | `size` | `Size` (`InputOtpSize`) | `Large`, `Medium` (default), `Small` |
| [x] | `type` | `Type` (`InputOtpType`) | `Number` (default), `Text` |
| [x] | `value` | `Value` (`string?`, `""`) | Two-way by default; rewritten to what the boxes hold |

### Events, methods, slots

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionInput` | `IonInput` (`InputOtpInputEventArgs.Value`), each edit |
| [x] | `ionChange` | `IonChange` (`InputOtpChangeEventArgs.Value`), after `IonBlur`, if the value changed since focus |
| [x] | `ionComplete` | `IonComplete` (`InputOtpCompleteEventArgs.Value`), each edit that leaves every box filled |
| [x] | `ionFocus` | `IonFocus`, when focus enters the group |
| [x] | `ionBlur` | `IonBlur`, when it leaves |
| [x] | `setFocus(index?)` | `SetFocus(int? index = null)` |
| [x] | default slot | `Content`, `ContentTemplate`: the description |

### Template parts

`PART_Group` (an `InputOtpGroupPanel`, which the control fills with an `InputOtpBox` and an `InputOtpSeparator` per box) and
`PART_Description` (a `ContentPresenter`). The box and the separator have `ControlTheme`s of their own, and each box carries the
host's state as pseudoclasses (`:fill-*`, `:shape-*`, `:size-*`, `:has-focus`, `:readonly`, `:ion-color`, `:ion-valid`,
`:ion-invalid`, `:ion-touched`) beside its own `:focus` and `:disabled`, so a theme styles it as `input-otp.md.css` does.

## Behaviours

Each has a test in `AvonicInputOtpTests`, named by its number:

1. The boxes show `Value` cut to `Length`, with refused characters dropped, and `Value` is rewritten to match.
2. Typing in an empty box fills it, raises `IonInput` and focuses the next box.
3. A character the pattern refuses changes nothing.
4. `IonComplete` follows `IonInput` whenever an edit leaves every box filled.
5. Typing in a filled box inserts and shifts right while the last box is empty, and otherwise replaces it.
6. Backspace removes a filled box's character and shifts the rest left; in an empty box it moves back and deletes there.
7. Right moves on only from a filled box, Left moves back, swapped in RTL.
8. A click or `SetFocus` lands on the box if it's filled, else the first empty box; not after Tab was pressed in a box.
9. One box is the tab stop; empty boxes past it are hidden from automation.
10. `IonFocus` and `IonBlur` are the group's; `IonChange` follows `IonBlur` when the value changed.
11. Paste fills from the first box and focuses the next empty one.
12. An autofill (several characters at once) spreads from the first box, and focus follows 20 ms later.
13. `SetFocus(index)` clamps; `SetFocus()` focuses the tab stop.
14. A value set in code raises nothing.
15. Separators show after the listed boxes, never after the last.
16. Disabled boxes can't be focused; read-only ones can, and ignore typing.
17. `Type`, `Inputmode` and `Autocapitalize` reach each box.
18. The group is named "One-time password input", or by the host's `AutomationProperties.Name`; each box "Input n of length".
19. The description shows only with content.
20. The layout by size, the gap, shrinking and overflow, and the separators, measured.
21. The colours of each state, measured.

## Resource keys

| Key | md default |
|---|---|
| `Avonic.InputOtp.Background` | unset: transparent (outline), step 50 (solid, or outline disabled or read-only), step 100 (solid disabled or read-only) |
| `Avonic.InputOtp.BorderColor` | unset: step 300 (outline), step 50 (solid), step 100 (disabled; solid read-only) |
| `Avonic.InputOtp.BorderRadius` | unset: 16 round, 8 soft, 0 rectangular. A `CornerRadius`, number or CSS string such as `"50%"` |
| `Avonic.InputOtp.BorderWidth` | 1 (2 on the focused box) |
| `Avonic.InputOtp.Color` | unset: the inherited text colour; text step 650 disabled |
| `Avonic.InputOtp.Height`, `Avonic.InputOtp.Width` | unset: 40 small, 48 medium, 56 large |
| `Avonic.InputOtp.HighlightColorFocused` | unset: `Avonic.Color.PrimaryBrush`; a `Color`'s base beats it |
| `Avonic.InputOtp.HighlightColorInvalid` | unset: `Avonic.Color.DangerBrush` |
| `Avonic.InputOtp.HighlightColorValid` | unset: `Avonic.Color.SuccessBrush` |
| `Avonic.InputOtp.MarginTop`, `…End`, `…Bottom`, `…Start` | 0 |
| `Avonic.InputOtp.MinWidth` | 40 |
| `Avonic.InputOtp.PaddingTop`, `…End`, `…Bottom`, `…Start` | 16, 0, 16, 0 |
| `Avonic.InputOtp.SeparatorBorderRadius` | 999 |
| `Avonic.InputOtp.SeparatorColor` | unset: `Avonic.BackgroundColorStep150Brush` |
| `Avonic.InputOtp.SeparatorHeight` | unset: the separator's width |
| `Avonic.InputOtp.SeparatorWidth` | 8 |

The per-size, per-shape and outline defaults are `Avonic.InputOtp.Md.SizeSmall`, `SizeMedium`, `SizeLarge`, `BorderRadiusRound`,
`BorderRadiusSoft`, `BorderRadiusRectangular` and `Background`.

## Extensions

- `HighlightBrush` (`IBrush?`): Ionic's private `--highlight-color`, public so that the theme can set it per state and each
  box can read it, as `AvonicInput`'s is. Ionic has no such member.

## Known gaps

- `autocomplete="one-time-code"`: Avalonia 12 has no content type for it, so a phone won't offer a code from a message by itself.
- Read-only Backspace and paste edit the value, as Ionic's do: see COMPARISON.md.
- A paste reads the clipboard asynchronously, so a key typed during a slow read comes first.
- Tried in a real window with mouse, touch and keys by `tools/real-window`; not on a device.
