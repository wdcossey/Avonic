# ion-progress-bar

Status: done · [#18](https://github.com/wdcossey/Avonic/issues/18)

`AvonicProgressBar` is a 4 px bar that shows how far a process has got, with an optional buffer and its stream of dots, or,
when that isn't known, two bars sliding across. The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real
Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicProgressBar Value="0.4" />
<c:AvonicProgressBar Value="0.3" Buffer="0.6" />
<c:AvonicProgressBar Type="Indeterminate" Color="danger" />
<c:AvonicProgressBar Value="0.4" Reversed="True" />
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `buffer` | `Buffer` (`double`, 1) | Under 1, the track ends there and dots fill the rest. Drawn clamped to 0..1 |
| [x] | `color` | `Color` | See the [colour system](../../infra/color-system/README.md) |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `reversed` | `Reversed` (`bool`) | Fills from the end. In right-to-left, from the left again |
| [x] | `type` | `Type` (`ProgressBarType`: `Determinate`, `Indeterminate`) | |
| [x] | `value` | `Value` (`double`, 0) | Drawn clamped to 0..1, and reported to accessibility as it is |

No events, methods or slots, as in Ionic.

### Parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `progress` | `PART_Progress`, and `PART_ProgressPrimary`, `PART_ProgressSecondary` for the two indeterminate bars |
| [x] | `stream` | `PART_Stream`, a `ProgressBarStream`, which draws the dots in its `Fill` |
| [x] | `track` | `PART_Track` |

The control moves the parts with their `RenderTransform` and shows or hides them, so a theme only paints them. Each part fills the
template's panel, in the order stream, track, progress, primary, secondary.

### Pseudoclasses

Ionic's host classes: `:type-determinate`, `:type-indeterminate`, `:reversed` (the direction drawn, after right-to-left), and
`:solid` (`Buffer` exactly 1), with `:ion-color` from `Color`.

### Resource keys

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--background` | `Avonic.ProgressBar.Background` | `Avonic.Color.Primary` at 0.3: the track and the dots |
| [x] | `--progress-background` | `Avonic.ProgressBar.ProgressBackground` | `Avonic.Color.Primary`: the progress and the indeterminate bars |

The height of 4 is a literal in the theme, as Ionic has no variable for it. `--buffer-background` isn't in Ionic 8.8.1.

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **A change of `Value` or `Buffer` eases** over 150 ms, linearly, as Ionic's `transition` does, and an interrupted one
  carries on from where it is. Not before the bar's first frame, as Stencil renders a new element at the next frame.
- **The dots stream left** by 10 px every 450 ms, and the indeterminate bars loop every 2 s, both from the moment the bar
  appeared or changed type, as CSS animations start when the element does.
- **Right-to-left reverses the bar,** and `Reversed` puts it back, as `document.dir` does in Ionic. The bar's own parts are always
  laid out left to right.
- **`AvonicConfig.Animated` doesn't stop it.** Ionic's `animated` config reaches the ripple (`tap-click`) and the transitions of
  the accordion, the menu, `ion-nav`, `ion-router-outlet`, overlays and the searchbar, and the skeleton text's shimmer, not
  this control's CSS. A progress bar is the user's only sign that something is happening, so with it off the changes still
  ease, and the stream and the indeterminate bars still move.
- **To automation** it's a `ProgressBar` with a read-only range from 0 to 1, whose value is the raw `Value`, and which
  announces a change of it. Indeterminate, it has no range, as `aria-valuenow` goes. A change of type raises no event, as
  WPF's progress bar raises none when it becomes indeterminate.
- **Frames run only while something moves.** A determinate bar with no buffer is still.

## Extensions

None.

## Known gaps and deliberate differences

Each is in the [plan](PLAN.md#deliberate-differences), with its measurement.

- **Right-to-left is the inherited `FlowDirection`,** so an ancestor set right to left reverses the bar. Ionic reads only
  `document.dir`, and a `dir="rtl"` ancestor does nothing there, measured.
- **The dots' edges are anti-aliased,** where Chromium draws the gradient's hard stop. Each dot is in the same place and of the
  same size.
- **In a toolbar,** Ionic places the bar at the bottom with `::slotted(ion-progress-bar)`. That goes with `ion-toolbar`.
