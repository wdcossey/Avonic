# ion-spinner → `AvonicSpinner` — Plan

> Sources read: `@ionic/core` 8.8.1 `dist/collection/components/spinner/spinner.js`, `spinner-configs.js` and
> `spinner.css` (there is no `spinner.md.css`: the spinner has one stylesheet for both modes), `utils/theme.js`
> `createColorClasses`, and `@ionic/docs` `core.json`. Reference render: [`ref/README.md`](ref/README.md).
> Measurements are from real Ionic in Chromium (md, 390 wide, 2×), wrapped in `ion-app`, with every animation paused
> and seeked to a known time through the Web Animations API, so that each number below is a computed style at that
> instant, not a reading off an image. The exact command for the frames is at the top of [COMPARISON.md](COMPARISON.md).

## What it is

An indeterminate progress indicator, 28 px square, drawn as a set of `<svg>` elements, each holding one `<circle>` or
one `<line>`, with a CSS keyframe animation on each. The `name` chooses one of nine sets from `spinner-configs.js`.
It has no slots, no parts, no events and one CSS variable. In Ionic the animation is CSS; here it's drawn in code
from the same configs on `AvonicConfig`'s clock, as `AvonicRippleEffect` is, so that a test can assert the timeline
to the millisecond.

## Fidelity checklist (`node tools/ionic-api.js ion-spinner`)

### Props (4)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `color` | `Color` | `string?` | null | `ColorPalette.ColorProperty.AddOwner`. `:host(.ion-color) { color: var(--ion-color-base) }`; an unknown colour inherits, measured `rgb(0, 0, 0)` for `color="nonsense"` in a black parent |
| [x] | `duration` | `Duration` | `double?` | null | Milliseconds. `typeof duration === 'number' && duration > 10 ? duration : spinner.dur`: measured `duration="2000"` → 2 s and `duration="10"` → the default 750 ms |
| [x] | `name` | `SpinnerName` | `SpinnerName?` | null | **Renamed**, as `StyledElement.Name` exists ([ADR 0002](../../decisions/0002-decisions-from-the-first-swarm.md) names it). The enum is `SpinnerName` too, by the string-union rule `<Component><Prop>` with the component's prefix: `RippleEffectType`, `IconSize`, `ButtonFill` — the prop is `name`, so `SpinnerName`, which is also the property's name, as `Color Color` is. Null resolves to `config.spinner`, then the mode default, `circular` on md. See "Default name" |
| [x] | `paused` | `Paused` | `bool` | false | Host class `spinner-paused` → `animation-play-state: paused` on the host, every `svg` and every `circle` |
| [ ] | `mode` | — | | | Not exposed (ADR 0001) |

### Events (0), methods (0), slots (0), shadow parts (0)

None in Ionic, none here.

### Host classes → pseudoclasses

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `.spinner-<name>` | `:name-<name>` | By the mapping rule for a host class from a prop, as `ion-icon`'s `icon-small` is `:size-small`: `:name-bubbles` … `:name-lines-sharp-small`, for `getName()`, so `:name-circular` with no name. Ionic's own styles use it only inside the shadow DOM, which is drawn in code here, so the theme doesn't select it, but an app's `ion-spinner.spinner-lines { … }` can, as `c\|AvonicSpinner:name-lines` |
| [x] | `.spinner-paused` | `:paused` | `paused`. Ionic's `_testing` isn't mapped. (Until #87, `AvonicConfig.Animated` off stood in for it) |
| [x] | `.ion-color`, `.ion-color-<name>` | `:ion-color` | From `ColorPalette` |
| [ ] | `.md`, `.ios` | — | Not applicable (ADR 0001) |

### CSS variables (1)

| ✓ | Ionic | Avonic resource key | Default | Notes |
|---|---|---|---|---|
| [x] | `--color` | `Avonic.Spinner.Color` | `CssUnset` | `color: var(--color)`: with nothing set it's invalid at computed-value time, so `color` inherits. Measured: `rgb(1, 2, 3)` in a `rgb(1, 2, 3)` parent; `style="--color: rgb(18,52,86)"` → that colour. So `Foreground` is `Css.Var` of the key, then `$parent[Control].(TextElement.Foreground)` |

### Global config

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `config.get('spinner')` | `AvonicConfig.Spinner` | Added in #87. See "Default name" |
| [ ] | `config.getBoolean('_testing')` | — | Not applicable: Ionic's test hook, which pauses every spinner. Until #87, `AvonicConfig.Animated` off stood in for it, which the human reversed: `spinner.js` doesn't read `animated`, and a spinner is the user's only sign that something is happening |

## The nine spinners, from `spinner-configs.js` and `spinner.css`

Every number below is from the source and confirmed by a computed style. Element `i` of `n`, with the default
duration `dur`. `%` is of the host's own width (`left`) or height (`top`): the `svg` is absolutely positioned at
that offset, as big as the host, so in a 28 px host `32%` is 8.96 px. The shape sits at the `svg`'s centre and
scales with the smaller side of the box (`preserveAspectRatio` `xMidYMid meet`): measured in a 56×28 host, the dot
radius stayed `6 × 28 / 64` and the offset was `32%` of 56.

| Name | `dur` | Elements | Shape (viewBox `0 0 64 64` unless said) | Offset | Delay of `i` | Animation on the `svg` |
|---|---|---|---|---|---|---|
| `bubbles` | 1000 | 9 circles | `r` 5, filled | `(32 cos θ, 32 sin θ)%`, θ = 2πi/9 | `dur·i/n − dur` | `spinner-scale-out`: `scale(1)` → `scale(0)`, linear |
| `circles` | 1000 | 8 circles | `r` 5, filled | as above, θ = 2πi/8 | `dur·i/n − dur` | `spinner-fade-out`: opacity 1 → 0, linear |
| `circular` | 1400 | 1 circle | `r` 20 at (48, 48), viewBox `24 24 48 48`, stroke 5.6, no fill | none | 0 | `spinner-circular` on the `svg` (see below) and `spinner-circular-inner` on the `circle` |
| `crescent` | 750 | 1 circle | `r` 26, stroke 4, `stroke-dasharray: 128px`, `stroke-dashoffset: 82px`, fill transparent | none | 0 | `spinner-rotate`: 0° → 360°, linear |
| `dots` | 750 | 3 circles | `r` 6, filled, `stroke-width: 0` | `left: (32 − 32i)%` | `−110·i` ms | `spinner-dots`: scale 1 → 0.4 → 1 and opacity 0.9 → 0.3 → 0.9, linear |
| `lines` | 1000 | 8 lines | `y1` 14 to `y2` 26, stroke 7, round caps | none | `dur·i/n − dur` | `spinner-fade-out`; the `svg` is rotated `45i + 180`° |
| `lines-small` | 1000 | 8 lines | `y1` 12 to `y2` 20, stroke 7, round caps | none | `dur·i/n − dur` | as `lines` |
| `lines-sharp` | 1000 | 12 lines | `y1` 17 to `y2` 29, stroke 4, round caps | none | `dur·i/n − dur` | `spinner-fade-out`; rotated `30i + 180`° |
| `lines-sharp-small` | 1000 | 12 lines | `y1` 12 to `y2` 20, stroke 4, round caps | none | `dur·i/n − dur` | as `lines-sharp` |

Every `animation-duration` is the resolved duration, set inline on each `svg` (and on `circular`'s `circle`), which
beats the `1s` in the stylesheet. So the phase of element `i` at time `t` is `frac((t − delay) / dur)`:

- ring spinners and lines: `frac(t/dur − i/n)`. At `t = 0` the opacities of `lines` are `1, ⅛, ¼, … ⅞` and the scales
  of `bubbles` are `1, ⅑, … ⅑·8`, measured exactly. The brightest element moves to `i + 1` every `dur/n`, which is
  clockwise: `rotate(45i + 180)` puts line 0 at 12 o'clock and line 1 at half past one. Measured: brightest line
  1 → 2 → 3 over time
- `dots`: `frac((t + 110i) / dur)`. Measured at 0 ms: scales `1, 0.824, 0.648`, opacities `0.9, 0.724, 0.548`;
  at 250 ms: scales `0.6, 0.424, 0.552`
- `crescent`: `frac(t/dur)`. Measured at 100 ms: `rotate(48deg)`, as a clockwise matrix
- `circular`: `frac(t/dur)`, 1400 ms

**`circular` does not rotate.** `:host(.spinner-circular) svg { animation: spinner-circular linear infinite }` with
`@keyframes spinner-circular { 100% { transform: rotate(360deg) } }` has an implicit 0 % frame at the underlying
value, `transform: translateZ(0)` from `svg { … }`. A `translateZ` and a `rotate` are different functions, so the
browser interpolates their matrices, and `rotate(360deg)` is the identity matrix, so the `svg`'s computed transform
is `matrix(1, 0, 0, 1, 0, 0)` at every phase. Measured at 0, 100, 175, 250, 350, 375, 500, 700, 750, 1000, 1050 and
1400 ms. The arc's travel is all in `spinner-circular-inner` on the `circle`, `ease-in-out` between keyframes:

| Progress | `stroke-dasharray` | `stroke-dashoffset` |
|---|---|---|
| 0 % | `1px, 200px` | `0px` |
| 50 % | `100px, 200px` | `−15px` |
| 100 % | `100px, 200px` | `−125px` |

Measured: at 100 ms `5.04952px, 200px` / `−0.613563px`, which is `cubic-bezier(0.42, 0, 0.58, 1)` of `100/700` applied
to each segment; at 350 ms `50.5px` / `−7.5px`; at 750 ms `100px` / `−16.0941px`; at 1050 ms `−70px`. The dash
starts at distance `−offset` along the circle, which begins at 3 o'clock and runs clockwise, and **it ends at the end
of the path, with no wrap**: at 1050 ms the visible arc is from 70 to the circumference, 125.66, which the frame
shows as an arc over the top ending at 3 o'clock, not a 285° one. The gap of 200 is longer than the circle, so there is
never a second dash. `crescent`'s dash of 128 with an offset of 82 is visible from 0 to 46, which is 101.4°, then
rotated by the `svg`.

### What's a constant, and what's a resource key

Ionic exposes `--color` and nothing else. Its host is `width: 28px; height: 28px`, which an app can restyle with
`ion-spinner { width: … }`, so that is a **literal in the theme**, as ADR 0002 says: `Width` and `Height` setters,
which an app's own value or style beats. Everything inside the shadow DOM — radii, stroke widths, dashes, the 32 %
ring, the delays and the keyframes — no Ionic app can reach, as there are no parts, so those are **constants in
`Avonic.Components`**, where the ripple's are: they are the definition of each named spinner, and the code needs
them to draw. Nothing in them is a theme choice.

## Behaviours

Each becomes a test in `AvonicSpinnerTests`, named after its number.

| # | Behaviour | Source |
|---|---|---|
| 1 | With no name, the spinner is `circular`. A name draws that spinner | `getName`; measured `spinner-circular` on `<ion-spinner>` |
| 2 | Each name has exactly its elements, radii, stroke widths, caps and fills from the table | `spinner-configs.js`, `spinner.css`; measured per element |
| 3 | Each name has its default duration | `dur`; measured `animation-duration` |
| 4 | `Duration` over 10 ms replaces it, 10 or less doesn't, and a change keeps the clock running: the phase becomes `elapsed / new` | `render`; measured `2000`, `10`, and a change to 3000 on a running animation keeping its `currentTime` |
| 5 | Ring and line elements are staggered `dur/n` apart, so that at `t = 0` `lines` is `1, ⅛ … ⅞` and `bubbles` scales `1, ⅑ … ⅑·8`, and the brightest moves clockwise | `animation-delay`; measured at 0, 175, 250 ms |
| 6 | `dots` run 110 ms apart through scale 1 → 0.4 → 1 and opacity 0.9 → 0.3 → 0.9 | `spinner-dots`; measured at 0, 250, 375 ms |
| 7 | `crescent` is a 101.4° arc from 3 o'clock, turning clockwise once per period | dash maths; measured `rotate(48deg)` at 100 ms |
| 8 | `circular` doesn't turn; its arc grows from a point at 3 o'clock to 100 units by half time, then runs off the end of the circle, `ease-in-out` in each half | measured at every phase above |
| 9 | `Paused` freezes the phase, and clearing it continues from there, not from 0 | `animation-play-state`; measured `currentTime` 216.6 before and after a 300 ms pause, then 299.9 |
| 10 | A change of name re-renders the same `svg`s in order. One that's kept with the same keyframes carries on; a new one, or one whose keyframes change, starts again | Stencil reuses unkeyed children by position, and a CSS animation whose name doesn't change carries on. Measured 100 ms after a change made 400 ms in: `lines` → `lines-small` all 517 ms; `circles` → `lines` all 517; `lines` → `lines-sharp` the first 8 at 517 and the other 4 at 83; `dots` → `bubbles`, `bubbles` → `circles`, `crescent` ↔ `circular` all 83 |
| 11 | Leaving the tree, or being hidden, stops the frames; coming back, or being shown, starts from 0. `IsVisible` false on the spinner or an ancestor is CSS's `display: none`. A hidden window isn't: a hidden page doesn't cancel its animations | a re-inserted element's animations restart; measured `currentTime` null while `display: none` on the host or an ancestor, then 100 ms after showing it 100 ms later |
| 12 | The colour is `Foreground`: `Color` paints the palette's base, an unknown colour inherits, `Avonic.Spinner.Color` sets it, and the default inherits | measured `rgb(197, 0, 15)`, `rgb(0, 0, 0)`, `rgb(18, 52, 86)`, `rgb(1, 2, 3)` |
| 13 | 28 px square by default. A set size scales the drawing by the smaller side, centred, and the offsets by each axis | measured 56×28 |
| 14 | Not mirrored in RTL | `top`/`left` by design, and the `transform-origin: calc(100% - center)` rules are invalid; measured identical matrices and positions under `dir="rtl"` |
| 15 | `role="progressbar"`; not focusable; sits at the start and top of its slot, as an inline block does | `render`; `display: inline-block` |
| 16 | Frames are requested while attached and running, and not while paused, and each one redraws | the equivalent of a CSS animation's cost |
| 17 | `AvonicConfig.Animated` off changes nothing: it keeps turning (#87, which replaced "off pauses it") | `spinner.js` doesn't read `animated` |
| 18 | It draws that frame, in its colour: each shape where the maths says, at its opacity | the frames are drawn in `Render`, so this is the pixels |
| 19 | The host has a class for its name and one for paused, which an app's style can select | `render`'s `class`; measured `md spinner-circular` with no name |
| 20 | `AvonicConfig.Spinner` names a spinner with none, read when it's first attached and at each change of a prop, not when only the config changes or it's moved, hidden or shown (#87) | `getName()` in `render()`; measured, see COMPARISON |

## Avalonia mapping

| Ionic | Avonic |
|---|---|
| host, `display: inline-block`, 28 px | `AvonicSpinner : Control`, drawing itself in `Render`, as `AvonicIcon` does. `HorizontalAlignment` `Left` and `VerticalAlignment` `Top` as default values, so a slot's alignment still wins. `Width`/`Height` 28 in the theme |
| `color`, `currentColor` | `Foreground` (`TextElement.ForegroundProperty.AddOwner`), inherited |
| `--color`, `.ion-color` | `Avonic.Spinner.Color` as `CssUnset`; `Foreground` = `Css.Var(key, parent Foreground)`; `^:ion-color` = `Css.Var(ColorPalette.Base, parent Foreground)` |
| the `svg`s and their keyframes | `SpinnerConfigs`: one record per name, mirroring `spinner-configs.js` plus that name's CSS rules. `SpinnerFrame.Compute(name, duration, elapsed, size)` gives every element's opacity, scale, rotation, shape and visible arc for an instant, as pure maths, which the tests pin to the measured values and `Render` draws |
| `stroke-dasharray` / `stroke-dashoffset` | the visible interval `[−offset, −offset + dash] ∩ [0, circumference]`, drawn as an arc with butt caps. Not `DashStyle`, whose units and start point are the backend's |
| `animation-play-state`, re-insertion | a start time per element on `AvonicConfig.Now()`: set on attach, and on a name change for each element that starts again, and moved forward by the time spent paused |
| the compositor's frames | `AvonicConfig.RequestFrame` while running; `TestClock` drives it at 16 ms |
| `role="progressbar"` | an automation peer of type `ProgressBar` |
| RTL | `BypassFlowDirectionPolicies`, as the icon |

## Default name

`getName()` is `this.name || config.get('spinner')`, then `'circular'` on md. In #87 the config became
`AvonicConfig.Spinner`, and the control resolves `SpinnerName ?? AvonicConfig.Spinner ?? SpinnerName.Circular`. Ionic calls
`getName()` in `render()`, so the config is read when the spinner is first attached and at each change of a prop, as
measured (see [COMPARISON.md](COMPARISON.md)): behaviour 20.

## Extensions

None.

## Dependencies

`ColorPalette`, `Css` and `AvonicConfig`'s clock, all present. No other control.

## Deliverables

| | Path |
|---|---|
| Control | `src/Avonic.Components/Controls/Spinner/AvonicSpinner.cs`, `SpinnerName.cs`, `SpinnerConfigs.cs`, `SpinnerFrame.cs`. No stub `.axaml`: it draws itself and has no template, as `AvonicIcon` doesn't |
| Theme | `src/Avonic.Themes/Controls/AvonicSpinner.axaml` (size and colour, with `Design.PreviewWith`), `src/Avonic.Themes/Tokens/Spinner.axaml` |
| Tests | `tests/Avonic.Components.Tests/Controls/AvonicSpinnerTests.cs` (1–20), `AvonicSpinnerCaptureTests.cs` (frames at known phases for the compare step) |
| Sample | `samples/Avonic.Sample/Pages/SpinnerPage.axaml*`, `[SamplePage("ion-spinner", Order = 15)]` |
| Docs | this plan, `COMPARISON.md`, `README.md` |
