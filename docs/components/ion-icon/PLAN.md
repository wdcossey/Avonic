# ion-icon → `AvonicIcon` — Plan

> Sources read: `ionicons` 8.0.13 `dist/collection/components/icon/icon.js`, `utils.js`, `request.js`,
> `validate.js` and `icon.css`, plus all 1357 files in `dist/svg/`. `ion-icon` isn't in `@ionic/core`, so
> `tools/ionic-api.js` can't print it, and this checklist comes from `icon.js`.
> Reference render: [`ref/README.md`](ref/README.md) (`node tools/ionic-ref/render.js ion-icon`).

## What it is

A box `1em` square that shows one SVG. A name is looked up in a registry and then in the bundled artwork.
It paints in the current text colour, and it can mirror itself in right-to-left layouts.

## Corrections to the issue

Measured in real Ionic 8.8.1 (md, Chromium, computed styles):

| The issue says | What Ionic does |
|---|---|
| A bare name picks the `-outline` or `-sharp` variant per mode | `getName` doesn't do this. `name="add"` is the file `add.svg`. Variants are chosen with the `ios` and `md` props |
| Render at `--ionicon-stroke-width` | It has no effect on built-in icons: `chevron-forward` stayed at `stroke-width: 48px` with the variable at `8px`. No SVG in 8.0.13 uses the `ionicon-stroke-width` class. It only reaches a custom SVG with that class, where it did give `8px` |
| Geometry resources, `Avonic.Icon.ChevronForward` | See "Artwork". A `Geometry` can't hold an icon |
| `CLAUDE.md` row and `ROADMAP.md` | Replaced by a `Status:` line since #50 |

## Fidelity checklist

### Props (11)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `name` | `IconName` | `string?` | null | **Renamed:** `StyledElement.Name` already exists, and hiding it would break `x:Name`. Ionic's own field is `iconName`. See "Open question" |
| [x] | `md` | `Md` | `string?` | null | Beats `IconName`, as `getName` does in md mode |
| [x] | `icon` | `Icon` | `string?` | null | A name, or a source if it contains `/` or `.`. Ionic also takes `{ ios, md }`: omitted, pass `Md` |
| [x] | `src` | `Src` | `string?` | null | `avares://` and `data:image/svg+xml`. See "Sources" |
| [x] | `color` | `Color` | `string?` | null | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `size` | `Size` | `IconSize?` | null | `Small` = 18, `Large` = 32. Beats `FontSize`, as `!important` does |
| [x] | `flipRtl` | `FlipRtl` | `bool?` | null | Three states matter: null auto-flips arrows and chevrons |
| [ ] | `ios` | — | | | `not applicable: mode`. `getName` ignores it in md, so it would be a dead prop |
| [ ] | `mode` | — | | | `not applicable: ADR 0001` |
| [ ] | `lazy` | — | | | `not applicable: IntersectionObserver`. It defers an HTTP fetch until visible. Artwork here is local, and only visible controls render |
| [ ] | `sanitize` | — | | | `not applicable: nothing executes`. An SVG here never runs script. The default behaviour, a blank icon, is kept; turning it off would have no purpose |

`aria-label` is not a prop in Ionic: it's inherited from the host. It maps to `AutomationProperties.Name`.

### Exported utility

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `addIcons({ name: url })` | `AvonicIcon.AddIcons(IReadOnlyDictionary<string, string>)`: camelCase keys also register in kebab-case, and the first registration of a name wins |

### Events (0), methods (0), slots (0), parts (0)

None.

### Host classes → pseudoclasses

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `.ion-color` | `:ion-color` |
| [x] | `.icon-small`, `.icon-large` | `:size-small`, `:size-large` |
| [x] | `.flip-rtl` | `:flip-rtl` |
| [ ] | `.icon-rtl` | `not applicable`: a WebKit fallback. Flipping reads `FlowDirection` directly |
| [ ] | `.md`, `.ios` | `not applicable: ADR 0001` |

### CSS variables (1)

| ✓ | Ionic | Avonic resource key | Default |
|---|---|---|---|
| [x] | `--ionicon-stroke-width` | `Avonic.Icon.IoniconStrokeWidth` | 32 |

## Behaviours

Each becomes a test.

**Naming (`getName`, `getUrl`)**

1. `src` beats everything, if it contains `/` or `.`. Otherwise it's ignored.
2. `md` beats `name`, and `name` beats `icon`.
3. `icon` is a name unless it contains `/` or `.`, in which case it's a source, used only if there's no name.
4. Names are lowercased: `HEART` works.
5. A name with anything but letters, digits and `-` resolves to nothing, as does an empty or blank one.
6. An unknown name, a missing source or no name at all leaves the box at full size and paints nothing.
7. A registered name beats the bundled artwork. Changing any of these props reloads the icon, except that a
   change resolving to no icon at all keeps the last one, because `loadIcon` only sets content `if (url)`.

**Registry (`addIcons`)**

8. `addCircle` also registers `add-circle`.
9. Registering a different icon under a used name keeps the first and warns.

**Sizing (`icon.css`)**

10. The box is `1em` square, following an inherited `FontSize`. It's `inline-block`, so it stays that size at the
    start of its parent, rather than stretching to fill it.
11. `Size` gives 18 or 32, and beats a local `FontSize`.
12. An explicit `Width` and `Height` win, and the artwork is centred and scaled to fit (`xMidYMid meet`).
13. Painting is clipped to the box (`contain: strict`, `overflow: hidden`).

**Colour**

14. Fill and stroke follow the inherited `Foreground` (`currentColor`).
15. `Color` gives `--ion-color-base` and beats a local `Foreground`, as `!important` does.
16. An unknown `Color` falls back to the inherited `Foreground`.
17. An unknown `Color` inside a coloured ancestor uses the ancestor's base.

**RTL (`render`)**

18. In RTL, arrows and chevrons flip unless `FlipRtl` is false. Others flip only if it's true.
19. The check uses the name even when `Src` supplies the artwork.
20. In LTR nothing flips, including with `FlipRtl` true.

**SVG painting (the browser's behaviour, for the features the artwork uses)**

21. `.ionicon { stroke: currentColor }` gives every element of an `ionicon` SVG a stroke, 1 unit wide by default. An SVG without the class has no default stroke.
22. Stroke defaults are SVG's: butt cap, miter join, **miter limit 4**. Avalonia's `Pen` defaults to 10.
23. Fill rule defaults to `nonzero`. Avalonia's `StreamGeometry` defaults to even-odd.
24. `fill`, `stroke` and stroke attributes inherit from the root and `<g>`, and `transform` composes.
25. `.ionicon-fill-none` removes the fill, and `.ionicon-stroke-width` takes its width from the variable, default 32.
26. An SVG containing `<script>` or an `on*` attribute is blank, and so is one that isn't an `<svg>`.

**Accessibility**

27. The host has `role="img"`, so the automation peer is `Image`. `AutomationProperties.Name` gives the label.

## Artwork

### One `Geometry` per icon can't work

| In the 1357 SVGs | Count |
|---|---|
| Icons with more than one layer | 588, up to 12 |
| Elements with their own stroke, cap, join and miter | 984 |
| Shapes besides `<path>` | 421 (`circle`, `rect`, `ellipse`) |
| Elements with a `transform` | 14 |
| Icons with a viewBox other than `0 0 512 512` | 1 (`logo-x`) |

The layers differ in paint, so merging them loses information. Keeping each icon as its SVG, and parsing it
into layers when first used, avoids a second format. It also means `Src`, `Icon` and `AddIcons` go through
the same code as every built-in icon, so the artwork tests exercise that parser.

Names stay Ionic's: `chevron-forward`, not a resource key.

### Ship all of them, compressed

Measured:

| Option | Size |
|---|---|
| All 1357, raw SVG | 758 KB |
| All 1357, stripped of `xmlns`, `class`, `px` and so on | 659 KB |
| All 1357, stripped and with 54 duplicates aliased | 645 KB |
| All 1357, **unmodified, Brotli** | **159 KB** |
| All 1357, stripped, Brotli | 158 KB |
| The 25 icons Ionic's components import, Brotli | 1 KB |

The whole set is 159 KB, and `name="anything"` then works as it does in Ionic. A subset would save 158 KB
and make every other name silently blank, and consumers would need a separate step to add the icon they
wanted. Stripping and aliasing save 1 KB, so the SVGs are stored as they are.

The file is decompressed on the first icon lookup. "Measured cost" below records what that costs.

### Generator

`tools/ionicons/generate.js` writes `src/Avonic.Components/Icons/ionicons.br`: Brotli over sorted lines of
`name<TAB>svg`. It's committed, so a build doesn't need `node_modules`. A test decompresses it and compares
it with `dist/svg/`, so an ionicons upgrade without a regenerate fails.

Ionicons is MIT, so `Icons/LICENSE` carries its notice, and it goes into the package.

### Measured cost

On a desktop in Release. A phone will be slower, and this hasn't been measured on one.

| What | Cost |
|---|---|
| `Avonic.Components.dll` | 37 KB → 229 KB: 160 KB of artwork, 33 KB of code |
| First icon lookup: decompress and index | 7 ms, 3 MB allocated, most of it short-lived |
| Kept afterwards | The 758 KB of decompressed SVG text, plus an index of the names |
| Parsing one icon | 0.045 ms on average, cached after the first use |

The bundle is held as bytes and each SVG decoded on request, because holding them as strings kept twice as
much on the heap for an app that uses a few icons. The first icon also pays for the runtime JIT-compiling
the parser, about 11 ms here.

## Sources

`Src` and a source in `Icon` accept:

| Source | Ionic | Here |
|---|---|---|
| `avares://…` | The equivalent of a relative app asset | Loaded, sync |
| `data:image/svg+xml;utf8,…`, `;base64,…`, percent-encoded | Parsed or fetched | Parsed, sync |
| `http(s)://…` | Fetched | **Blank. Deferred** |
| A relative path | Fetched from the page's origin | Blank: there's no page. Use `avares://` |

A failed load is blank, as Ionic's `safeFallback` is. Network fetching means async state, caching, timeouts
and a security review in a control that doesn't otherwise need any, so it waits until someone needs it.

**SVG support is a subset:** `path`, `circle`, `ellipse`, `rect`, `line`, `polyline`, `polygon` and `g`, with
presentation attributes, `fill` and `stroke` colours, and the classes above. Gradients, `<text>`, `<image>`,
`style=` and `<style>` aren't drawn. This covers every built-in icon, and it's documented for custom ones.

An SVG has to be well-formed XML, where Ionic's `innerHTML` is more forgiving, and more than 256 levels of nesting is
blank. A `<!DOCTYPE>` is skipped, and its entities are never expanded.

## Design

`AvonicIcon : Control` overrides `Render`. It has **no template**: Ionic's only inner element is `.icon-inner`,
which exists to carry the flip transform. So there's no stub `ControlTheme` and no Themes template.

| Piece | Job |
|---|---|
| `IconResolver` | `getName`, `getSrc`, `isSrc` and the order in `getUrl`: pure functions |
| `IconStore` | Registry, bundled artwork, cached parsed artwork |
| `SvgArtwork` | Parses one SVG to layers |
| `SvgPath` | Corrects path data before `StreamGeometry.Parse` reads it |
| `AvonicIcon` | Properties, measure, render |

### `StreamGeometry.Parse` in Avalonia 12.1.4

Comparing every icon against a browser showed that it draws some paths wrongly, and testing malformed data found
more. `SvgPath` is about 100 lines that work around each of these, by adjusting the text:

| Problem | Affects | Handling |
|---|---|---|
| After a non-curve command, SVG starts a smooth curve (`S`, `T`) at the current point. Avalonia uses the last control point, however old, and from either kind of curve. `wifi-outline` and `infinite` came out visibly warped | 39 built-in icons | A zero-length curve first makes the control point the current point |
| The default fill rule is even-odd, and SVG's is nonzero | Any icon with overlapping shapes | Sets `F1` or `F0` |
| A number straight after `Z` **loops forever** | Custom SVGs | Ends the path at the `Z`, as a browser stops at an error |
| Bad data throws, including `IndexOutOfRangeException` | Custom SVGs | Draws nothing |
| A path that doesn't start with a moveto draws from the origin | Custom SVGs | Draws nothing |
| Valid numbers like `+1` and some exponents are rejected | Custom SVGs | Draws nothing. **Not fixed** |

A full parser (385 lines) was written and then dropped. On built-in icons the workaround gives the same numbers in
the comparison. A full parser would only improve custom SVGs: a malformed path would keep the part before its error,
and `+1` would work.

**This isn't proof of no other hang.** 50 malformed inputs were tried, and numbers after `Z` was the only pattern
that hung. Treat a custom SVG from an untrusted source with care. If Avalonia fixes these, `SvgPath` can shrink.

`Tokens/Icon.axaml` holds the stroke width token and the `Design.PreviewWith` block. The control looks the
variable up from itself with a fallback of 32, so `var(--ionicon-stroke-width, 32px)` and subtree overrides
both hold, and it works with only the stubs loaded. It only subscribes if the artwork uses the variable.

**RTL.** Avalonia mirrors drawing under a right-to-left ancestor. Ionic doesn't, so the control sets
`BypassFlowDirectionPolicies` and flips itself. A pixel test covers this.

## Open question

**`name` collides with `StyledElement.Name`, and 11 other Ionic components have the same prop** (`ion-checkbox`,
`ion-input`, `ion-select` and others). `CLAUDE.md` says `disabled` is the only renamed prop, so that line has to
change either way. This plan uses `IconName`, and adds it to the mapping table for this control only. Form
controls need their own rule, and that's better decided when the first one is built.

## Differences from Ionic

- **Default size.** Ionic's `1em` is usually 16 px, from the page. Avalonia's inherited `FontSize` has its own
  default, so a bare icon is smaller unless a parent sets one. The rule (`1em`) matches, and a page-level default
  isn't this control's decision.
- **Remote and relative sources**, and the **SVG subset**, as above.
- **`Sanitize` and `Lazy`** are omitted, as above.

## Tests

One per behaviour above, plus:

- Every one of the 1357 icons parses to one layer per element, and paints.
- `ionicons.br` equals `dist/svg/`.
- Each form of the path grammar that icons use gives the same shape as its explicit spelling.
- Each `StreamGeometry.Parse` problem above, including pinning the ones not fixed.
- **A pixel comparison of all 1357 icons against Chromium**, with `tools/ionic-ref/compare-icons.js`, run for the
  compare step. The result is in [`COMPARISON.md`](COMPARISON.md).

## Sample

`Pages/IconPage`: names, variants, sizes, colours and RTL flipping. As the issue asks, the sample's back button
and the ripple page's unbounded demo use real icons.
