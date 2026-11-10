# ion-input-password-toggle

Status: done · [#24](https://github.com/wdcossey/Avonic/issues/24)

`AvonicInputPasswordToggle` is a round, clear `AvonicButton` for an `AvonicInput`'s end slot. A click flips the input's `Type`
between `Password` and `Text`, and its icon between `eye` and `eye-off`. It's built with `ion-input`, so the plan, the behaviours
and the comparison are in [ion-input](../ion-input/README.md); its own Ionic frame is in [`ref/`](ref/).

```xml
<c:AvonicInput Label="Password" Type="Password">
    <c:AvonicInput.EndContent>
        <c:AvonicInputPasswordToggle Color="medium" />
    </c:AvonicInput.EndContent>
</c:AvonicInput>
```

## API

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | The button's |
| [x] | `hideIcon` | `HideIcon` | Shown while the password is visible; null is `eye-off` |
| [x] | `showIcon` | `ShowIcon` | Shown while it's hidden; null is `eye` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

No events, methods, slots or variables.

## Behaviours

Tested in `AvonicInputTests` (B22, B27):

- A click flips the input's `Type`, which an app binding to it sees, and the icon; the button is named "Show password" or
  "Hide password", as Ionic's `aria-label`.
- A press doesn't take focus from the field, as Ionic's `pointerdown` handler prevents it.
- A change of the input's `Type` in code updates the icon.
- Hidden (`visibility: hidden`) while the input is disabled or read-only.
- In an item it keeps the default size, as Ionic's button is in the toggle's shadow root, where the item can't reach it:
  44 × 48, measured.

## Extensions

`Icon` and `ButtonLabel`, read-only: what the button shows and says now, for the template.

## Known gaps

`aria-pressed` (Avalonia's automation has no pressed state for a plain button), and Ionic's warning for an input type other
than text or password.
