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
| [x] | `buffer` | `Buffer` | `double` | 1 | Fills its parent's width, 4 px high in the theme, and clips. It's a `ProgressBar` to automation, with the range 0..1, read-only, reporting the raw `Value`; indeterminate drops the range pattern, as `aria-valuenow` goes | `role`, `aria-*`; measured |
| 2 | Determinate: the progress is the host's width times `Value`, from the left edge: 0.25 → 89.5 of 358 | `scaleX(finalValue)`; measured |
| 3 | `Value` and `Buffer` are clamped to 0..1 for display, and only for display: 1.5 draws full and reports 1.5. NaN draws the part unscaled, as `scaleX(NaN)` is an invalid transform | `clamp(0, value, 1)`; measured |
| 4 | Buffer: the track is the host's width times `Buffer`; the dots show from there to the end, every 10 px, 4.24 wide, in the track's colour, with a centre at x ≡ 0 (mod 10) at animation time 0, and none under the translucent track | `renderProgress`; measured |
| 5 | The dots stream left by 10 px every 450 ms, linear and forever, from when the bar became determinate, whether or not they were showing | `buffering`; measured |
| 6 | A change of `Value` eases linearly over 150 ms from where the bar is. A new target starts a fresh 150 ms from there, and going back is shortened by how far it got. Not on the first render | `transition: transform 150ms linear`; measured |
| 7 | A change of `Buffer` eases the same way, and the dots stay where they are while the window over them moves | measured: the circles' x stayed at −10 through the transition |
| 8 | Indeterminate: two bars on a 2 s loop, at the measured positions, from the moment the bar became indeterminate, over the full track | keyframes; measured at 125 ms steps |
| 9 | A change of type restarts the animations, eases the kept element (the progress or the indeterminate track) from its old scale, and doesn't ease the determinate track | Stencil's patch; measured |
| 10 | `Reversed` mirrors the whole bar about its centre, including the stream's direction. An app's `RenderTransform` replaces it | `:host(.progress-bar-reversed) { transform: scaleX(-1) }` |
| 11 | Right-to-left reverses it, and `Reversed` in RTL puts it back. Nothing else is mirrored: the bar's internals are always LTR | `document.dir === 'rtl' ? !reversed : reversed`, `direction: ltr` |
| 12 | `Color`: the progress is the base colour, and the track and the dots are the base colour at 0.3. An unknown colour draws nothing. No colour inside a coloured ancestor keeps the defaults | `:host(.ion-color)` rules; `var()` invalid at computed-value time |
| 13 | Host classes: `:type-determinate` / `:type-indeterminate`, `:reversed` for the effective direction, `:solid` when `Buffer` is exactly 1 | `createColorClasses(...)` |
| 14 | `AvonicConfig.Animated` off: changes snap, and the stream and the indeterminate bars stand at time 0, as Ionic's `progress-paused` holds them for its own screenshots | `_testing` → `progress-paused`; Avonic's nearest public switch |
| 15 | The two keys apply: `Avonic.ProgressBar.Background` to the track and the dots, `Avonic.ProgressBar.ProgressBackground` to the progress, in both types | `--background`, `--progress-background` |
| 16 | Detaching stops the frame loop; attaching again starts the animations from 0, as a re-created element does | CSS animations start when the element appears |
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
first element's transition and snaps the determinate track's, as Stencil keeps one and re-creates the other. The loop runs only while something moves: a transition in flight, the dots
showing, or indeterminate. Hidden dots (`Buffer` 1) cost nothing, and their phase is still measured from when the bar became
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
`Value` raises the pattern's value-changed event. Name comes from
`AutomationProperties.Name`, which is `aria-label`.

### Resource keys

| Key | Value |
|---|---|
| `Avonic.ProgressBar.Background` | `SolidColorBrush` of `Avonic.Color.Primary`, opacity 0.3 |
| `Avonic.ProgressBar.ProgressBackground` | `SolidColorBrush` of `Avonic.Color.Primary` |

The host's height of 4 is a literal in the theme, as it has no variable. `:ion-color` reads `ColorPalette.Base` for the
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
