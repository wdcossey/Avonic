# ion-backdrop — Plan

`AvonicBackdrop`: the dark layer under an overlay that a tap goes to. It's the first consumer of the overlay system (#5), whose
plan is [`docs/infra/overlays/PLAN.md`](../../infra/overlays/PLAN.md). On its own it's nearly invisible: md's opacity is 0.01, and
each overlay's enter animation takes it to `--backdrop-opacity`. Measurements are from real Ionic 8.8.1 in Chromium, md, 390 x 844,
in `ion-app`.

## Fidelity checklist

`node tools/ionic-api.js ion-backdrop`.

### Props

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `stopPropagation` | `StopPropagation` | `bool` | `true` | The tap stops at the backdrop |
| [x] | `tappable` | `Tappable` | `bool` | `true` | Raises `IonBackdropTap`; the cursor is a hand |
| [x] | `visible` | `Visible` | `bool` | `true` | False: the background is transparent, and the backdrop still takes the tap |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |

### Events

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionBackdropTap` | `IonBackdropTap` | Routed, bubbling, as it's `bubbles: true, composed: true`; `RoutedEventArgs` |

No methods, slots, parts or CSS variables.

## What Ionic does

`backdrop.js`: a `click` listener on the host, in the capture phase. With `stopPropagation`, `preventDefault()` and
`stopPropagation()`; with `tappable`, `ionBackdropTap`. The host is `aria-hidden="true"`, with the classes `backdrop-hide` when not
`visible` and `backdrop-no-tappable` when not `tappable`.

`backdrop.md.css`: `inset: 0; position: absolute; display: block; opacity: 0.01; z-index: 2; cursor: pointer;
touch-action: none; contain: strict; transform: translateZ(0); background-color: var(--ion-backdrop-color, #000)`;
`.backdrop-hide { background: transparent }`; `.backdrop-no-tappable { cursor: auto }`.

## Measured

A 200 x 100 backdrop over a button, in a `div` with a `click`, `pointerdown` and `pointerup` listener (`[bp-]` in
`ref/measurements.json`).

| | Case | Result |
|---|---|---|
| M1 | computed style | opacity 0.01, background `rgb(0, 0, 0)`, cursor `pointer`, z-index 2, `position: absolute`, the parent's size, `aria-hidden="true"`, `touch-action: none` |
| M2 | `visible = false` | background `rgba(0, 0, 0, 0)`, opacity still 0.01 |
| M3 | `tappable = false` | cursor `auto` |
| M4 | mouse click, and a mouse drag of 60 px | `ionBackdropTap` (not `defaultPrevented`, composed); with `stopPropagation` the parent's `click` doesn't come, without it it does. `pointerdown` and `pointerup` reach the parent either way |
| M5 | touch tap; a touch moving 5, 10, 14, 15 px; 16, 20, 40 px | a tap up to 15 px; none from 16: the browser's own click, which `NativeButton` already measures |
| M6 | `tappable = false` | no `ionBackdropTap`; the button under it gets no click either: the backdrop covers it |

## Design

`AvonicBackdrop : TemplatedControl`, in `Controls/Backdrop/`, with a stub template of one `Border` that draws `Background`.

- **The tap** is `NativeButton`, with no keys, as the chip has it for a plain DOM `click`: a click on release, any movement with a
  mouse and up to 15 px with a finger, the left button only. On it: with `StopPropagation`, the click goes no further, which in
  Avalonia means an `AvonicButton`, item or chip around the backdrop doesn't click (their `NativeButton` hears a stopped click from
  inside), and `Tapped` from the press is handled on the backdrop, so a handler on a parent that doesn't ask for handled events
  doesn't see it. `preventDefault()` has no counterpart: a `div` has no default. Then with `Tappable`, `IonBackdropTap`.
- **The look**, in Themes: `Opacity` 0.01, `Background` `Avonic.BackdropColorBrush` (`--ion-backdrop-color`, `#000`, already in
  `Tokens/Global.axaml`), `ZIndex` 2 and `Cursor` `Hand`. `Visible` false: `Transparent`, which Avalonia hit-tests, as CSS's
  `transparent` does, where `null` wouldn't. `Tappable` false: the default cursor. Both are pseudoclasses, `:backdrop-hide` and
  `:backdrop-no-tappable`, Ionic's class names, as they aren't a prop's value.
- **`aria-hidden`**: the default peer, `NoneAutomationPeer`, which is neither a control nor content.
- **Size**: it stretches, as `inset: 0` fills the parent.
- Not applicable: `touch-action: none` (nothing scrolls it: an overlay isn't in a `ScrollViewer`), `contain: strict` and
  `translateZ(0)` (rendering hints).

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| B1 | A mouse click raises `IonBackdropTap` once; a drag of any length too; a right click doesn't | `onMouseDown`, M4 |
| B2 | A touch tap raises it; a touch that moves 16 px doesn't | M5 |
| B3 | `Tappable` false: no `IonBackdropTap`; the control under it gets nothing | M6 |
| B4 | `StopPropagation`: an `AvonicButton` around it doesn't click and a parent's `Tapped` handler doesn't run; false: both do | M4 |
| B5 | The event bubbles to the overlay, which dismisses with the `backdrop` role | `ionBackdropTap`, overlays B1 |
| B6 | The look: opacity 0.01, the backdrop colour, z-index 2, a hand cursor; `Visible` false transparent and still hit; `Tappable` false the default cursor | M1 to M3 |
| B7 | Not in the automation tree as a control or content | `aria-hidden` |
| B8 | It fills its parent in an overlay and in a page | `inset: 0` |

## Dependencies

`NativeButton` (Behaviors), the overlay system (this PR). No other control.
