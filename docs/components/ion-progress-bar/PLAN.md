# ion-progress-bar → `AvonicProgressBar` — Plan

Ionic 8.8.1, from `dist/collection/components/progress-bar/progress-bar.js` and `progress-bar.md.css`, and `core.json`.
Every number here was measured in Chromium, md, at 390 wide, with the bars 358 wide, by a throwaway `playwright-core` script
that paused the CSS animations with the Web Animations API (`animation.pause(); animation.currentTime = ms`) to read them at
a known time. The reference render is [`ref/`](ref/), and the comparison is [COMPARISON.md](COMPARISON.md). During the build,
the spans at 1000 ms were measured again, and so were a change of type and an interrupted transition, which this plan first
had wrong: see "Type change" below.

## What it is

A 4 px bar. Determinate: a track, a progress bar scaled to `value`, and, when `buffer` is under 1, a stream of dots after the
track's end. Indeterminate: two bars sliding across on a 2 s loop. Everything is a `transform`, with the host clipping.

## Fidelity checklist (`node tools/ionic-api.js ion-progress-bar`)

### Props (6)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `buffer` | `Buffer` | `double` | 1 | Clamped to 0..1 for display, as `clamp(0, buffer, 1)`. `:solid` when it's exactly 1, unclamped, as `buffer === 1` is |
| [x] | `color` | `Color` | `string?` | null | `ColorPalette.ColorProperty.AddOwner` |
| [ ] | `mode` | — | | | Not exposed (ADR 0001) |
| [x] | `reversed` | `Reversed` | `bool` | false | |
| [x] | `type` | `Type` | `ProgressBarType { Determinate, Indeterminate }` | `Determinate` | pseudoclasses `:type-determinate`, `:type-indeterminate` |
| [x] | `value` | `Value` | `double` | 0 | Clamped to 0..1 for display. Reported raw to accessibility, as `aria-valuenow` is |

### Events (0), methods (0), slots (0): none.

### Shadow parts (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `progress` | `PART_Progress`, and `PART_ProgressPrimary`, `PART_ProgressSecondary` | Ionic puts `part="progress"` on the determinate bar and on both indeterminate spans. Names are unique in a template, so the indeterminate two carry their class name |
| [x] | `stream` | `PART_Stream` | A `ProgressBarStream`, the template's inner part that draws the dots |
| [x] | `track` | `PART_Track` | `.progress-buffer-bar`, in both types |

### CSS custom properties (2)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.ProgressBar.Background` | brush | `rgba(var(--ion-color-primary-rgb), 0.3)`: a brush of `Avonic.Color.Primary` at opacity 0.3 |
| [x] | `--progress-background` | `Avonic.ProgressBar.ProgressBackground` | brush | `var(--ion-color-primary)`: a brush of `Avonic.Color.Primary` |

There's no `--buffer-background` in 8.8.1: `core.json`, the CSS and the JS don't have it. The dots use `--background`.

## Measured

| What | Ionic, at 358 wide |
|---|---|
| Host | `display: block`, 358 × 4, `overflow: hidden`, `contain: strict`, `direction: ltr`, no background of its own |
| `.progress` | `scaleX(value)` from the left edge: 0.25 → 89.5, 0.5 → 179, 1 → 358. `rgb(0, 84, 233)`. z 2 |
| `.progress-buffer-bar` | `scaleX(buffer)` from the left: 0.75 → 268.5. `rgba(0, 84, 233, 0.3)`, which reads 178, 203, 248 on white. z 1 |
| `.buffer-circles-container` | Outer: `translateX(buffer × 100%)`, `overflow: hidden`. Inner: `translateX(−buffer × 100%)`, so the circles stay where they were and only the window moves. What shows is `buffer × W` to `W` |
| `.buffer-circles` | `left: −10px; right: −10px` → 378 wide at x −10. A 10 × 10 tile at `5px center`, each a circle of radius 0.3 × 5√2 = 2.12, so a dot is 4.24 wide and fills the 4 px height. Dot centres at x ≡ 0 (mod 10) in the host at animation time 0, measured at 280, 290, …; at 225 ms they're at 275, 285, …: `translateX(−10px)` over 450 ms, linear, forever. Same colour as the track. z 0 |
| Transition | `transform 150ms linear` on the progress, the track and both containers. 0.2 → 0.8 measured 0.2, 0.267, 0.333, 0.4 … 0.8 one frame at a time, done 150 ms after it began. The buffer the same: 268.5 → 89.5 while the circles' x stayed at −10 |
| Clamping | `value="1.5"` draws 358 and reports `aria-valuenow="1.5"`; `value = −1` draws 0 and reports −1. `buffer = 1.5` is clamped to 1 and the host loses `progress-bar-solid` |
| Reversed | `transform: scaleX(−1)` on the host, about its centre: `value="0.3" buffer="0.6"` puts the progress at 250.6..358, the track at 143.2..358, and the dots from 0 to 143.2 |
| RTL | `document.dir = 'rtl'` reverses every bar, and a reversed one goes back: `progress-bar-reversed: document.dir === 'rtl' ? !reversed : reversed`. A `dir="rtl"` ancestor does nothing, measured. Only the document counts |
| Colour | `color="danger"`: progress `rgb(197, 0, 15)`, track and dots `rgba(197, 0, 15, 0.3)`. `--background` and `--progress-background` are still primary, unused |
| Accessibility | `role="progressbar"`, `aria-valuemin="0"`, `aria-valuemax="1"`, `aria-valuenow` = the raw value when determinate, absent when indeterminate |
| Type change | Re-renders, and Stencil keeps the first `div` and patches its class: the `.progress` becomes the indeterminate `.progress-buffer-bar`, and back. So that element eases over 150 ms from its old scale. Determinate 0.25 → indeterminate: the track goes 89.5, 149.2, 208.8, 268.5, 328.2, 358 at 33 ms steps. Indeterminate → determinate 0.25: the progress goes 358 → 89.5 the same way. The other elements are new: the indeterminate bars and `buffering` start at 0, and the determinate track is at the buffer at once |
| Interrupted transition | A new target starts a fresh 150 ms from the value shown: 0.2 → 0.8, then 1 after 65 ms, moved 42.4 px per 33 ms from 167.2. Going back to where it began is shortened, as CSS Transitions' reversing shortening factor says: back to 0.2 from 0.45 of the way took about 67 ms, not 150 |
| Indeterminate | Two bars, each 358 wide, at `left: −145.166611%` (−519.69) and `left: −54.888891%` (−196.5). `width: 100%` wins over `right: 0`. Each translates by a percentage of its own width, which is W. Inside each, a span of W scales about its **centre** (no `transform-origin` is set on `.progress-indeterminate`), so its visible part is `[centre − s × W / 2, centre + s × W / 2]`, with `centre = left + translate + W / 2` |

The indeterminate keyframes, from the CSS. An `animation-timing-function` in a keyframe eases the interval that starts there;
the animation's own is `linear`. Translate is in fractions of W, and scale is a factor.

| Primary translate | Primary scale | Secondary translate | Secondary scale |
|---|---|---|---|
| 0 → 20 %: 0, linear | 0 → 36.65 %: 0.08, linear | 0 → 25 %: 0 → 0.37651913, `(0.15, 0, 0.515058, 0.409685)` | 0 → 19.15 %: 0.08 → 0.457104, `(0.205028, 0.057051, 0.57661, 0.453971)` |
| 20 → 59.15 %: 0 → 0.8367142, `(0.5, 0, 0.701732, 0.495819)` | 36.65 → 69.15 %: 0.08 → 0.661479, `(0.334731, 0.12482, 0.785844, 1)` | 25 → 48.35 %: → 0.84386165, `(0.31033, 0.284058, 0.8, 0.733712)` | 19.15 → 44.15 %: → 0.72796, `(0.152313, 0.196432, 0.648374, 1.004315)` |
| 59.15 → 100 %: → 2.00611057, `(0.302435, 0.381352, 0.55, 0.956352)` | 69.15 → 100 %: → 0.08, `(0.06, 0.11, 0.6, 1)` | 48.35 → 100 %: → 1.60277782, `(0.4, 0.627035, 0.6, 0.902026)` | 44.15 → 100 %: → 0.08, `(0.257759, −0.003163, 0.211762, 1.38179)` |

Measured spans, in host coordinates, at 125 ms steps (the oracle for the tests; W = 358):

| ms | primary x..xEnd | primary scale | secondary x..xEnd | secondary scale |
|---|---|---|---|---|
| 0 | −355.0..−326.4 | 0.08 | −31.8..−3.2 | 0.08 |
| 250 | −355.0..−326.4 | 0.08 | −14.8..91.2 | 0.296 |
| 500 | −351.5..−322.9 | 0.08 | 19.6..215.0 | 0.546 |
| 750 | −304.0..−273.1 | 0.0865 | 76.3..325.9 | 0.697 |
| 1000 | −216.2..−109.4 | 0.299 | 178.5..417.9 | 0.669 |
| 1250 | −98.8..108.0 | 0.578 | 348.9..445.5 | 0.270 |
| 1500 | 102.7..277.5 | 0.488 | 465.5..492.3 | 0.075 |
| 1750 | 289.2..363.0 | 0.206 | 524.1..537.5 | 0.0375 |

Some scales dip under 0.08 because the last secondary segment's curve overshoots, as its control point `y = 1.38179` says.

## Behaviours

Each becomes a test in `AvonicProgressBarTests`.

| # | Behaviour | Source |
|---|---|---|
| 1 | Fills its parent's width, 4 px high in the theme, and clips. It's a `ProgressBar` to automation, with the range 0..1, read-only, reporting the raw `Value`; indeterminate drops the range pattern, as `aria-valuenow` goes | `role`, `aria-*`; measured |
| 2 | Determinate: the progress is the host's width times `Value`, from the left edge: 0.25 → 89.5 of 358 | `scaleX(finalValue)`; measured |
| 3 | `Value` and `Buffer` are clamped to 0..1 for display, and only for display: 1.5 draws full and reports 1.5. NaN keeps the scale an element had, as `style.transform` ignores the invalid `scaleX(NaN)`, and a new element is unscaled | `clamp(0, value, 1)`; measured, apart from NaN, which is from the CSSOM |
| 4 | Buffer: the track is the host's width times `Buffer`; the dots show from there to the end, every 10 px, 4.24 wide, in the track's colour, with a centre at x ≡ 0 (mod 10) at animation time 0, and none under the translucent track | `renderProgress`; measured |
| 5 | The dots stream left by 10 px every 450 ms, linear and forever, from when the bar became determinate, whether or not they were showing | `buffering`; measured |
| 6 | A change of `Value` eases linearly over 150 ms from where the bar is. A new target starts a fresh 150 ms from there, and going back is shortened by how far it got. Not on the first render | `transition: transform 150ms linear`; measured |
| 7 | A change of `Buffer` eases the same way, and the dots stay where they are while the window over them moves | measured: the circles' x stayed at −10 through the transition |
| 8 | Indeterminate: two bars on a 2 s loop, at the measured positions, from the moment the bar became indeterminate, over the full track | keyframes; measured at 125 ms steps |
| 9 | A change of type restarts the animations, eases the kept element (the progress or the indeterminate track) from its old scale, and doesn't ease the determinate track. The indeterminate bars are inside the track, so its ease scales them too: 67 ms after 0.25 → indeterminate, the track at 208.8 and the secondary bar at −17.6..6.7 | Stencil's patch; measured |
| 10 | `Reversed` mirrors the whole bar about its centre, including the stream's direction. An app's `RenderTransform` replaces it | `:host(.progress-bar-reversed) { transform: scaleX(-1) }` |
| 11 | Right-to-left reverses it, and `Reversed` in RTL puts it back. Nothing else is mirrored: the bar's internals are always LTR | `document.dir === 'rtl' ? !reversed : reversed`, `direction: ltr` |
| 12 | `Color`: the progress is the base colour, and the track and the dots are the base colour at 0.3. An unknown colour draws nothing. No colour inside a coloured ancestor keeps the defaults | `:host(.ion-color)` rules; `var()` invalid at computed-value time |
| 13 | Host classes: `:type-determinate` / `:type-indeterminate`, `:reversed` for the effective direction, `:solid` when `Buffer` is exactly 1 | `createColorClasses(...)` |
| 14 | `AvonicConfig.Animated` off: changes snap, and the stream and the indeterminate bars stand at time 0, as Ionic's `progress-paused` holds them for its own screenshots | `_testing` → `progress-paused`; Avonic's nearest public switch |
| 15 | The two keys apply: `Avonic.ProgressBar.Background` to the track and the dots, `Avonic.ProgressBar.ProgressBackground` to the progress, in both types | `--background`, `--progress-background` |
| 16 | Detaching stops the frame loop; attaching again starts the animations from 0, as a re-created element does. Hiding the bar or an ancestor does the same, as `display: none` does, and a change while hidden doesn't ease | CSS animations start when the element appears, and stop under `display: none` |
| 17 | Frames run only while something moves: a transition, the dots showing, or indeterminate. A solid determinate bar costs nothing | Avonic's own: CSS animations cost nothing on the main thread |

## Design

### Everything is a transform, as in CSS

Each part is a full-size child of one `Panel`, in paint order: stream, track, progress, primary, secondary, which is Ionic's
z-order 0, 1, 2, 2, 2. The control sets each one's `RenderTransform` from the clock. The determinate parts scale from
`RenderTransformOrigin` (0, 0), and so do the indeterminate bars, with one matrix each for the span scaled about its centre
inside a translated bar: x′ = s·x + (1 − s)·W/2 + offset·W. The control sets the origin itself, as the matrices depend on it.
Nothing is re-measured by an animation frame.

### The dots

`.buffer-circles` is a 10 × 10 tile of one circle, 20 px wider than the host, translated for the stream. A tiled brush
with a bound colour can't be written in a template (a brush isn't in the tree, so neither a `TemplateBinding` nor
`ColorPalette.Base` reaches it), so the part is a small control, `ProgressBarStream`, which draws the circles from its `Fill`
and a phase the host gives it. The theme binds `Fill` like any other brush. Its geometry, the 10 px pitch and the 2.12
radius, is Ionic's and has no variable, so it's a constant in Components, as the ripple's are: the pitch is also the
animation's period, which the code needs. The window from `buffer × W` to `W` is a clip the part draws within, which moves
while the dots don't, as the two nested containers do in Ionic. The dots under the track are cut, which shows, as the track is
translucent.

### Animations on the clock

Ionic's are CSS, so they run on the compositor and can't be observed. Here they're one frame loop on
`AvonicConfig.RequestFrame`, which `TestClock` drives at 16 ms, so a test can ask for 1000 ms and compare against the
measured span. Each segment of the keyframes is a CSS `cubic-bezier`, solved for x as Chromium does (bisection and Newton on
the unit Bézier), because Avalonia's `SplineEasing` isn't specified for a control point outside 0..1, and the last secondary
segment has `y = 1.38179`.

The transitions are on the same clock: a target, the value shown, and the time the change began. Interrupting one starts a
new one from the shown value, as CSS does, with CSS's reversing shortening factor when it goes back. A change of type keeps the
first element's transition and snaps the determinate track's, as Stencil keeps one and re-creates the other. The
indeterminate bars are children of the track in Ionic, so their matrices are multiplied by the track's scale. The loop runs only while something moves: a transition in flight, the dots
showing, or indeterminate, and only while the bar is attached and visible. Avalonia doesn't tell a control when an ancestor
hides or shows it, so a frame that finds the bar hidden stops, and the bar watches `IsVisible` on the hidden visuals in its
chain until they show. Hidden dots (`Buffer` 1) cost nothing, and their phase is still measured from when the bar became
determinate, so they come in where Ionic's would be.

### Reversed and RTL

Ionic flips the host with `transform: scaleX(-1)`, and an outer stylesheet's `transform` on the element would beat it. The
control does the same: `RenderTransform` on the host at style priority, so an app's wins. The template's root is
`FlowDirection.LeftToRight`, set by the control, because the internals are `direction: ltr`. In a right-to-left window that
root mirrors itself back, and the effective direction is computed: `FlowDirection == RightToLeft ? !Reversed : Reversed`.
`BypassFlowDirectionPolicies`, as `AvonicIcon` uses, isn't enough here: it stops the host's own mirroring, and then each child
mirrors again, as its direction differs from the host's. Ionic reads `document.dir`, the document's direction only; the nearest
thing in Avalonia is the inherited `FlowDirection`, which an ancestor can also set. That's the one deviation, recorded.

### Accessibility

A peer of type `ProgressBar` with `IRangeValueProvider`: minimum 0, maximum 1, read-only, `Value` raw. Indeterminate returns
no range provider, as `aria-valuenow` is dropped, which is also what WPF's `ProgressBarAutomationPeer` does. A change of
`Value` raises the pattern's value-changed event, and so does a change of type, from or to no value, as `aria-valuenow`
appears or goes. Name comes from
`AutomationProperties.Name`, which is `aria-label`.

### Resource keys

| Key | Value |
|---|---|
| `Avonic.ProgressBar.Background` | `SolidColorBrush` of `Avonic.Color.Primary`, opacity 0.3 |
| `Avonic.ProgressBar.ProgressBackground` | `SolidColorBrush` of `Avonic.Color.Primary` |

The host's height of 4 is a literal in the theme, as it has no variable. The progress and the two indeterminate bars share a
style class, `progress`, in the theme's template, as Ionic paints `.progress` and `.progress-indeterminate` alike. `:ion-color` reads `ColorPalette.Base` for the
progress and `ColorPalette.WithAlpha` 0.3 of `BaseColor` for the track and the dots.

## Extensions

None. Every member maps to an Ionic prop, part, class or variable.

## Deliberate differences

| Difference | Why |
|---|---|
| Right-to-left is the inherited `FlowDirection`, where Ionic reads only `document.dir`. A `dir="rtl"` ancestor does nothing in Ionic, measured, and an ancestor's `FlowDirection` does here | Avalonia has no document; an app sets `FlowDirection` on its window, which is the same thing, or on a subtree, which Ionic can't. Ionic also reads `document.dir` only when it renders; this follows a change at once |
| `AvonicConfig.Animated` off holds the stream and the indeterminate bars at time 0 and snaps changes, where Ionic's public `animated` config doesn't touch this control's CSS animations. An indeterminate bar then shows only its track, as both bars are off its left edge at time 0 | The issue asks for `AvonicConfig.Animated` to be honoured. Holding at 0 is what Ionic's private `_testing` config does to this control (`progress-paused`). It's read when a behaviour runs, as the config says: turning it back on restarts the frames at the bar's next change, attach or resize |
| The dot geometry is a constant in Components, not a theme literal | A brush with a bound colour can't be tiled in a template, and the pitch is the animation's period |
| The dots' edges are anti-aliased, where Chromium draws the gradient's hard stop at 30 % with a harder edge | Rendering, best effort (ADR 0002). Each dot is in the same place and of the same size |

## Not in this PR

| What | Goes with |
|---|---|
| `::slotted(ion-progress-bar)` in `ion-toolbar`: absolute at the bottom | the toolbar's issue |
| `progress-bar-solid` in `progress-bar.ios.css` | ADR 0001: md only. The pseudoclass is there for an app |

## Dependencies

`ColorPalette` and `Css` from `ion-item`, `AvonicConfig.RequestFrame` from `ion-content`. All done.
