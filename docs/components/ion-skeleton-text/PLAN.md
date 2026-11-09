# ion-skeleton-text → `AvonicSkeletonText` — Plan

Ionic 8.8.1, from `dist/collection/components/skeleton-text/skeleton-text.js`, `skeleton-text.css` (there's no
`skeleton-text.md.css`), `label.md.css` and `core.json`. Every number here was measured in Chromium, md, at 390 wide, by
throwaway `playwright-core` scripts, which paused the shimmer with the Web Animations API (`animation.pause();
animation.currentTime = ms`) to read it at a known time. The reference render is [`ref/`](ref/), and the comparison is
[COMPARISON.md](COMPARISON.md). It's built with `ion-img`, in one PR, for #17.

## What it is

A grey bar that stands in for a line of text while it loads: a block, as wide as its parent, 10 high, with 4 px above and
below. `animated` sweeps a lighter band across it, once a second. In an avatar or a thumbnail it fills the box instead.

## Fidelity checklist (`node tools/ionic-api.js ion-skeleton-text`)

### Props (1)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `animated` | `Animated` | `bool` | false | The shimmer, only while `AvonicConfig.Animated` is also true when it's read: see "The global animated config" |

### Events (0), methods (0), slots (0), parts (0): none.

### CSS custom properties (3)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.SkeletonText.Background` | brush | `CssUnset` → `rgba(BackgroundRgb, 0.065)`. Not read while animated, as the gradient replaces `background`: measured `s7` |
| [x] | `--background-rgb` | `Avonic.SkeletonText.BackgroundRgb` | `Color` | `CssUnset` → `Avonic.TextColor`, as `var(--ion-text-color-rgb, 0, 0, 0)` |
| [x] | `--border-radius` | `Avonic.SkeletonText.BorderRadius` | `CornerRadius`, number or CSS string | `CssUnset` → `inherit`: the parent's corner radius |

### Internal events

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionStyle` | `IonStyle.IonStyleEvent` | `{ skeleton-text: true }`, once, before the first render. An item turns it into `:item-skeleton-text`, which `Styles/Label.axaml` already reads to clip a label |

### Host classes

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `skeleton-text-animated` | `:animated` | `animated && config.getBoolean('animated', true)` |
| [x] | `in-media` | `:in-media` | `hostContext('ion-avatar') \|\| hostContext('ion-thumbnail')` |

## Measured

| What | Chromium |
|---|---|
| Box | `display: block`, 358 × 10 in a 358 wide parent, margin `4px 0`. 10 high at 16, 40 and 8 px, in Roboto and in serif: `line-height: 10px` on a `&nbsp;` |
| `height: inherit` | In a parent 50 high: 50. In a flex row 40 high: 40. In a `<span>`: 10. Its own `height: 30px; width: 100px`: 100 × 30 |
| Background | `rgba(0, 0, 0, 0.067)`: 0.065 rounded to 17/255. `--background-rgb: 255,0,0` → `rgba(255, 0, 0, 0.067)`. `--background` beats it, when it isn't animated |
| Radius | 0 by default. `border-radius: inherit`: 12 in a parent with 12. `--border-radius: 6px`: 6 |
| Animated | `background: linear-gradient(to right, rgba(rgb, 0.065) 8%, rgba(rgb, 0.135) 18%, rgba(rgb, 0.065) 33%)`, `background-size: 800px 104px`, `background-position` from −400 to 400 px over 1 s, linear, forever. `--background` is ignored, `--background-rgb` isn't (`s7`) |
| The band, on 358 × 20, one pixel row, red channel on white | 0 ms: 238 everywhere. 250 ms: 229 at x 0, 234 at 32, 238 from 64. 500 ms: 221 at 142 to 144, 230 at 100, 229 at 200, 238 at 0 and from 264. 750 ms: 230 at 300, 223 at 357 |
| In a thumbnail in an item | `in-media`, 56 × 56, margin 0, radius 0 |
| In an avatar in an item | `in-media`, 40 × 40, margin 0, radius 50%, inherited from the avatar |
| In an item | The item gets `item-skeleton-text`, and its `ion-label` gets `overflow: hidden`. Every skeleton raises `ionStyle` `{"skeleton-text":true}`, a skeleton straight in an item too |
| Hit testing | `pointer-events: none`, `user-select: none` |
| Global `animated: false` at start | No `skeleton-text-animated`. `config.set('animated', true)` later: still none, until `animated` changes: false then true gives it. `config.set('animated', false)` then removing it and appending it again: it keeps the class. So the config is read when it renders, as the mapping row for global config says: the first attach and each change of `animated` |
| `in-media` | Moved out of a thumbnail: keeps `in-media` (and is 390 × 10 with no margin). One appended outside and moved into a thumbnail: no `in-media`. So it's read at render too |
| Animations | One, `shimmer`, 1000 ms |
| Right to left | With `dir="rtl"`, at 250 ms, the same pixels from the left as left to right: `background-position` and `to right` are physical |

## Behaviours

Each becomes a test in `AvonicSkeletonTextTests`.

| # | Behaviour | Source |
|---|---|---|
| 1 | A block as wide as its parent, 10 high, with 4 above and below, whatever the font | `:host`; measured |
| 2 | A parent with a height gives it that height; its own `Height` and `Width` win | `height: inherit`; measured `s3`, `s8`, `s10` |
| 3 | Background `rgba(text, 0.065)`; `BackgroundRgb` changes the colour; `Background` replaces it | `--background`, `--background-rgb`; measured |
| 4 | Radius 0, or the parent's; `BorderRadius` sets it, a percentage of its own size included | `border-radius: var(--border-radius, inherit)`; measured `s4`, `s5`, `av` |
| 5 | `Animated`: the band moves left to right, 800 px a second, linear, from −400 at the start, in the `BackgroundRgb` colour, in right-to-left too; `Background` is ignored | `:host(.skeleton-text-animated)`, `@keyframes shimmer`; measured at 0, 250, 500 and 750 ms |
| 6 | `AvonicConfig.Animated` off when it first attaches, or when `Animated` changes, keeps it still; turning the config on later changes nothing until `Animated` changes | `config.getBoolean('animated', true)` in `render()`; measured |
| 7 | In an `AvonicAvatar` or `AvonicThumbnail`, at any depth, when it first attaches: no margin, and it fills the box, a circle in an avatar. Moved later, it keeps what it had | `:host(.in-media)`, `hostContext`; measured |
| 8 | It raises `IonStyle` `{ skeleton-text: true }` once, before it first renders, so an item clips its label | `componentWillLoad`; measured |
| 9 | Not hit-testable: a press goes to what's under it | `pointer-events: none` |
| 10 | The shimmer runs on `AvonicConfig`'s clock, only while it's animated, attached and visible. Detaching or hiding it stops the frames; attaching or showing it again starts the band from −400, as a CSS animation restarts. A hide and show between two frames restarts nothing. `Animated` off and on again restarts it | CSS animations; CLAUDE.md's rows for transitions and `display: none` |
| 11 | Not in the automation tree, as a `&nbsp;` in a `span` isn't announced | Chromium's accessibility tree has no node for it |

## Design

### The global animated config

`render()` reads `this.animated && config.getBoolean('animated', true)`. Ionic's `animated` reaches skeleton text, so
`AvonicConfig.Animated` applies, read when Ionic reads it: at the first attach, and at each change of `Animated`. Its doc in
`AvonicConfig.cs` already names the skeleton text's shimmer, so it needs no change.

### The shimmer

`background-position` moves an 800 px tile of a gradient across the host, which clips it. Here the theme draws the tile in a
part, `PART_Shimmer`, as a `LinearGradientBrush` from 0 to 800 in absolute units, repeating, which is `background-size: 800px`
and `background-repeat: repeat`. The part is 800 wider than the host, and the control moves it with a `RenderTransform` from
`AvonicConfig`'s clock, so that the tile's origin is at `−400 + 800 t` modulo 800, as the keyframes put it. Nothing is
re-measured by a frame. The part's parent clips to the host's corners. The part is left to right whatever the app's direction,
as the gradient and its position are physical in CSS, and in right-to-left its transform moves it the other way, as Avalonia lays
it out from the right and mirrors its transform. The 800, the keyframes and the second are constants in
the control, `ShimmerTile`, `ShimmerFrom` and `ShimmerDuration`; the gradient's stops and alphas are the theme's.

The frames follow `AvonicProgressBar`'s: one is requested only while it moves, and the time is measured from when it started.
Every skeleton that starts in one frame shares that frame's time on a test's clock, as CSS animations started together do.

### Inherit

`border-radius: inherit` takes the parent's radius. The parent is the visual parent, and its radius is
`$parent[Control].(Border.CornerRadius)`, the property `Border`, `ContentPresenter` and `TemplatedControl` share, which is 0 on
a panel, as on a `div`. In an avatar that's the avatar's presenter, which has its radius. A converter on the control,
`AvonicSkeletonText.BorderRadius`, takes the key, that, the size and the direction, and gives the key's radius if it's set, or
the parent's.

### Resource keys

| Key | Type | Default |
|---|---|---|
| `Avonic.SkeletonText.Background` | brush | `CssUnset` |
| `Avonic.SkeletonText.BackgroundRgb` | `Color` | `CssUnset` |
| `Avonic.SkeletonText.BorderRadius` | `CornerRadius`, number or CSS string | `CssUnset` |

The 10 px line, the 4 px margins, the gradient's stops (8 %, 18 %, 33 %) and alphas (0.065, 0.135), and the 800 px tile have no
variable, so they're literals in the theme. `AvonicSkeletonText.Rgba`, a converter on the control (since the P1 roll-up, `Css.Rgba`), makes `rgba(var(--a,
var(--b)), n)` from the keys: the first that's set, a `Color` or a solid brush's colour, at the alpha in its parameter.
`AvonicSkeletonText.BackgroundOrRgba` gives the `Background` key if it's set, and that otherwise.

## Extensions

None.

## Deliberate differences

| Difference | Why |
|---|---|
| A skeleton in place of text in a label isn't in an `<h3>` or a `<p>`, so it doesn't get their margins: 2 px above and below an `h3` in a label | Avalonia's `h3` is a `TextBlock`, which can't hold a control. An app adds the margin |
| Two bars one above the other in a `StackPanel` are 18 apart, where CSS collapses the 4 below one and the 4 above the next, so they're 14 apart (measured `s1`, `s2`) | Avalonia has no margin collapsing. An app that wants 14 sets the next one's `Margin` to `0,0,0,4` |
| A bar with a `Width` and no `HorizontalAlignment` is centred, where a block with a width starts at the left | Avalonia's `Stretch` centres what's narrower than its slot, for every control. An app sets `HorizontalAlignment="Left"` |
| `width: 80%` has no equivalent; an app sets `Width`, or uses a `Grid`'s star columns | Avalonia has no percentages |
| `in-media` and the config are read at the first attach, which is Ionic's first render; a skeleton made but never attached has neither | Avalonia has no render before attach |

## Dependencies

`AvonicItem` (`IonStyle`, `:item-skeleton-text`), `AvonicLabel` (`Styles/Label.axaml` clips it), `AvonicAvatar`,
`AvonicThumbnail`, `Css`, `BorderRadius` and `AvonicConfig.RequestFrame`. All done.
