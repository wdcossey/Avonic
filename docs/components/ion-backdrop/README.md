# ion-backdrop

Status: done · [#5](https://github.com/wdcossey/Avonic/issues/5)

`AvonicBackdrop`, Ionic's `ion-backdrop`: the layer under an overlay that a tap goes to. On its own it's all but invisible, at md's
opacity of 0.01; an overlay's enter animation takes it to its backdrop opacity, `Avonic.BackdropOpacity` (0.32). It came with the
[overlay system](../../infra/overlays/README.md). Plan: [PLAN.md](PLAN.md); comparison: [COMPARISON.md](COMPARISON.md).

```xml
<Panel>
    <TextBlock Text="Under it" />
    <c:AvonicBackdrop Tappable="True" IonBackdropTap="OnBackdropTap" />
</Panel>
```

In an overlay's template, bind `Tappable` to the overlay's `BackdropDismiss`: the overlay hears `IonBackdropTap` and dismisses with
the `backdrop` role.

## Checklist

| ✓ | Ionic | Avonic | Default | |
|---|---|---|---|---|
| [x] | `stopPropagation` | `StopPropagation` | `true` | The tap goes no further: an `AvonicButton`, item or chip around it doesn't click, and a parent's `Tapped` isn't raised |
| [x] | `tappable` | `Tappable` | `true` | Raises `IonBackdropTap`; a hand cursor. False: nothing, and it still covers what's under it |
| [x] | `visible` | `Visible` | `true` | False: a transparent background, still taking the tap |
| [x] | `ionBackdropTap` | `IonBackdropTap` | | Routed, bubbling |
| [ ] | `mode` | none | | Not exposed (ADR 0001) |

No methods, slots, parts or CSS variables of its own.

## Behaviour

- A tap is a browser's `click`, as `NativeButton` has it: on release, the left button, any distance with a mouse and up to 15 px
  with a finger.
- Not in the automation tree as a control or content, as `aria-hidden="true"`.
- It fills its parent, as `inset: 0` does.

## Resource keys

Both are Ionic's global variables, in `Tokens/Global.axaml`:

| Key | Ionic | md |
|---|---|---|
| `Avonic.BackdropColor`, `Avonic.BackdropColorBrush` | `--ion-backdrop-color` | `#000` |
| `Avonic.BackdropOpacity` | `--ion-backdrop-opacity`, which overlays animate the backdrop to | 0.32 |

The theme, `Controls/AvonicBackdrop.axaml`, has `backdrop.md.css`: opacity 0.01, z-index 2, the hand cursor, and the
pseudoclasses `:backdrop-hide` (transparent) and `:backdrop-no-tappable` (the default cursor), Ionic's host classes.

## Known gaps

- Opacity is blended one level of 255 differently from Chromium (253 against 252 at 0.01, 174 against 173 at 0.32). Measured in
  [COMPARISON.md](COMPARISON.md).
- Not tried in a real window.
