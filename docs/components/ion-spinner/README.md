# `AvonicSpinner` — ion-spinner

Status: done · [#15](https://github.com/wdcossey/Avonic/issues/15)

Ionic reference: [`ion-spinner`](https://ionicframework.com/docs/api/spinner) · [Plan](PLAN.md) · [Comparison](COMPARISON.md) ·
reference render in [`ref/`](ref/)

An indeterminate progress indicator, 28 px square: one of nine sets of shapes, each animated, in the current text colour.

## Usage

```xml
xmlns:c="using:Avonic.Components.Controls"

<c:AvonicSpinner />
<c:AvonicSpinner SpinnerName="Lines" />
<c:AvonicSpinner SpinnerName="Crescent" Color="primary" />
<c:AvonicSpinner SpinnerName="Dots" Duration="1500" Paused="{Binding IsIdle}" />
<c:AvonicSpinner Width="48" Height="48" />
```

**Note `SpinnerName`, not `Name`.** `Name` is Avalonia's own element name, so Ionic's `name` prop can't use it. Its type is
the enum `SpinnerName`: `Bubbles`, `Circles`, `Circular`, `Crescent`, `Dots`, `Lines`, `LinesSmall`, `LinesSharp`,
`LinesSharpSmall`, for Ionic's `bubbles` … `lines-sharp-small`.

**With no name it's `Circular`,** md's default.

**It sits at the start of its parent.** It's `inline-block` in Ionic, so it doesn't stretch. It defaults to
`HorizontalAlignment="Left"` and `VerticalAlignment="Top"`, so set `Center` to centre it.

**A size that isn't square** scales the shapes by the smaller side, centred, as an SVG does, while the ring and the dots
are spread by each side on its own, as Ionic's percentages are.

**It needs `AvonicTheme`** for its size and colour. Without it, give it a `Width`, a `Height` and a `Foreground`.

## Fidelity checklist

### Props

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | The palette's base colour. An unknown colour inherits |
| [x] | `duration` | `Duration` (`double?`, ms) | Only over 10 counts, otherwise the name's own: 1000 for `bubbles`, `circles` and the lines, 1400 for `circular`, 750 for `crescent` and `dots`. A change carries on from the same elapsed time |
| [x] | `name` | `SpinnerName` (`SpinnerName?`) | Renamed, see above |
| [x] | `paused` | `Paused` | Stops where it is. Clearing it carries on from there |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Events, methods, slots, shadow parts

None in Ionic, none here.

### Host classes

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `.ion-color` | `:ion-color` | From `ColorPalette` |
| [ ] | `.spinner-<name>`, `.spinner-paused` | — | Not added: only Ionic's own shadow DOM rules use them, and those are drawn in code here. See the PR's "For you to decide" |

### CSS variables

| ✓ | Ionic | Avonic resource key | Default |
|---|---|---|---|
| [x] | `--color` | `Avonic.Spinner.Color` | unset, so the colour inherits. `Color` beats it, as `:host(.ion-color)` does |

### Global config

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [ ] | `spinner` | — | Not yet: `AvonicConfig` is a shared file. Proposed in the PR. A spinner with no name is `circular` |
| [x] | `_testing` | `AvonicConfig.Animated` off | Pauses every spinner at its next frame. Turned back on, a spinner carries on within 250 ms |

## Behaviours

From [PLAN.md](PLAN.md), each a test in `AvonicSpinnerTests`:

1. With no name it's `circular`.
2. Each name has exactly its elements from `spinner-configs.js`, with `spinner.css`'s strokes, caps and fills.
3. Each name has its own duration.
4. A duration over 10 ms replaces it, and a change keeps the elapsed time.
5. The ring and the lines are staggered `duration / n` apart, and the brightest moves clockwise.
6. `dots` pulse 110 ms apart, scale 1 → 0.4 → 1 and opacity 0.9 → 0.3 → 0.9.
7. `crescent` is a 101.4° arc from 3 o'clock, turning clockwise.
8. `circular` doesn't turn. Its arc grows from a point at 3 o'clock to 100 units by halfway, then runs off the end of the
   circle, `ease-in-out` in each half.
9. `Paused` freezes it, and clearing it carries on.
10. A change of name keeps the elements that keep their keyframes, and starts the rest again, as Ionic's re-render does.
11. Leaving the tree, or being hidden, which is `IsVisible` false on it or an ancestor, stops it, and coming back starts again.
12. The colour inherits, follows `Color`, and follows `Avonic.Spinner.Color`, and an app's own `Foreground` beats both.
13. 28 px square, scaling by the smaller side.
14. Not mirrored right to left.
15. A progress bar to automation, not focusable, at the start of its parent, and its whole box takes input.
16. Frames are asked for only while it runs, and each one redraws.
17. `AvonicConfig.Animated` off pauses it, and on again carries on.
18. It draws that frame, in its colour.

## Extensions

None.

## Resource keys

| Key | Type | Ionic |
|---|---|---|
| `Avonic.Spinner.Color` | `IBrush`, or `CssUnset` | `--color` |

The size, 28, is a literal in the theme, `Width` and `Height`, which an app's own value or style beats. The shapes, strokes,
offsets and timings are each name's definition, which no Ionic app can reach, so they're constants in `SpinnerConfigs`.

## Known gaps

- **`config.spinner`** isn't read, as `AvonicConfig` is shared. Waiting on the proposal in the PR.
- **Not tried in a real window.** Compared headless at 350 ms; see [COMPARISON.md](COMPARISON.md).
