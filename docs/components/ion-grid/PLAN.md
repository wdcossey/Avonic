# ion-grid family — Plan

Three controls in one PR: `AvonicGrid`, `AvonicRow` and `AvonicCol`. They only make sense together, so they share this folder,
as the card family does. Measurements are from real Ionic 8.8.1 in Chromium, md, wrapped in `ion-app`, with scripts kept outside
the repo. Every number below was measured unless it says otherwise. Widths are CSS px, and Avalonia's are device-independent
pixels, which are the same unit.

## Fidelity checklist

`node tools/ionic-api.js ion-grid`, then `ion-row` and `ion-col`.

### `ion-grid` props (1)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `fixed` | `Fixed` (`bool`) | Pseudoclass `:fixed` (`grid-fixed`) |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### `ion-row` props (0)

None. `ion-row` is `display: flex; flex-wrap: wrap` and nothing else.

### `ion-col` props (24)

All `string?`, as Ionic's are `string | undefined`. `null` is `undefined`, and `""` is the attribute with no value.

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `size`, `sizeXs`, `sizeSm`, `sizeMd`, `sizeLg`, `sizeXl` | `Size`, `SizeXs`, `SizeSm`, `SizeMd`, `SizeLg`, `SizeXl` | A number of columns, or `"auto"` |
| [x] | `offset`, `offsetXs`, `offsetSm`, `offsetMd`, `offsetLg`, `offsetXl` | `Offset`, `OffsetXs`, … `OffsetXl` | |
| [x] | `push`, `pushXs`, `pushSm`, `pushMd`, `pushLg`, `pushXl` | `Push`, `PushXs`, … `PushXl` | |
| [x] | `pull`, `pullXs`, `pullSm`, `pullMd`, `pullLg`, `pullXl` | `Pull`, `PullXs`, … `PullXl` | |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Slots, parts, events, methods

None in `core.json` for any of the three. Each has a default `<slot>`. The grid's and the row's hold many children, so the
mapping rule "many children, with no slot in `core.json`" makes `AvonicGrid` and `AvonicRow` `ItemsControl`s (`Items`). A
column is one cell, and its slot holds the cell's content, so `AvonicCol` is a `ContentControl` (`Content`), as the card's parts are.

### CSS variables (19)

All are global (`--ion-grid-*`), so the keys are `Avonic.Grid*`. Ionic reads them where they're inherited, so an app sets
them in `Resources` anywhere above, as it would set the variable on any ancestor. The md defaults are the `var()`
fallbacks, kept as `Avonic.Grid.Md.*` keys, as `ion-button`'s per-class defaults are.

| ✓ | Ionic | Key | Type | Default |
|---|---|---|---|---|
| [x] | `--ion-grid-padding` | `Avonic.GridPadding` | `double` | Unset → `Avonic.Grid.Md.Padding` (5) |
| [x] | `--ion-grid-padding-xs` … `-xl` | `Avonic.GridPaddingXs` … `Avonic.GridPaddingXl` | `double` | Unset → `Avonic.GridPadding` |
| [x] | `--ion-grid-width` | `Avonic.GridWidth` | `double` | Unset → the breakpoint's `Avonic.Grid.Md.Width*` |
| [x] | `--ion-grid-width-xs` … `-xl` | `Avonic.GridWidthXs` … `Avonic.GridWidthXl` | `double` | Unset → `Avonic.GridWidth` → `Avonic.Grid.Md.WidthXs` (∞, for `100%`), `Sm` 540, `Md` 720, `Lg` 960, `Xl` 1140 |
| [x] | `--ion-grid-column-padding` | `Avonic.GridColumnPadding` | `double` | Unset → `Avonic.Grid.Md.ColumnPadding` (5) |
| [x] | `--ion-grid-column-padding-xs` … `-xl` | `Avonic.GridColumnPaddingXs` … `Avonic.GridColumnPaddingXl` | `double` | Unset → `Avonic.GridColumnPadding` |
| [x] | `--ion-grid-columns` | `Avonic.GridColumns` | number | Unset → 12, a constant in `AvonicCol`, as it's in `col.js` |

## Behaviours

Each becomes a test, named after its number. Widths are at a 390 viewport unless a width is given.

| # | Behaviour | Source |
|---|---|---|
| 1 | Columns with no size share the row equally, padding included | `col.css` `flex-basis: 0; flex-grow: 1`; measured three at 126.67 |
| 2 | `Size` n is n / 12 of the row, border box. The columns with no size share what's left | `calculateSize`; measured `6`, rest, `3`: 190, 95, 95 |
| 3 | `Size="auto"`: as wide as its content, and the rest fill. Never wider than the row: the text wraps, and the next column goes to the next line | `flex: 0 0 auto; width: auto`. `max-width: auto` is invalid, so `col.css`'s `100%` stays; measured 99.83, and a long one at 380 × 48 |
| 4 | Columns that don't fit on the line wrap to the next one | `row.css` `flex-wrap: wrap`; measured two `8`s at 253.33, one under the other |
| 5 | `Offset` n is a margin at the start of n / 12. A column with no size still grows into the rest | `calculateOffset`; measured x = 100; with no size, 68.33 and 158.34 wide |
| 6 | `Push` n moves the column n / 12 towards the end, and `Pull` towards the start, without moving the others. With both, `Push` wins, even `"0"`. An invalid `Push` leaves `Pull` | `calculatePush`, `calculatePull`: `left` and `right` on `position: relative`; measured x = 100 and 5, `push3 pull6` 100, `push="abc" pull="1"` −26.66, `push="0" pull="1"` no move |
| 7 | Per breakpoint: the last breakpoint at or below the viewport with a value wins, `xs` beats the plain prop, and each prop is looked up on its own | `getColumns`, `matchBreakpoint`; measured `size="12" size-sm="6" size-md="4" size-lg="3" size-xl="2"` at 390, 576, 768, 992, 1200: 380, 283, 252.66, 245.5, 198.33; `size="6" size-xs="4"`: 4; `offset-md`, `push-lg`, `pull-xl` |
| 8 | An empty value at a breakpoint goes back to no size there | `if (!columns \|\| columns === '') return`; measured `size-md=""` |
| 9 | The breakpoint is the viewport's width, not the container's | `matchMedia`; measured `size-md="6"` in a 300 box at 768: 145 |
| 10 | A change of the viewport's width re-lays the columns and the grid | `@Listen('resize', { target: 'window' })` → `forceUpdate`, and `@media` in the CSS; measured |
| 11 | Odd sizes: `"4.5"` is 4.5 columns; `"0"` and negatives leave only the padding; more than 12 overflows the row and the next column wraps; anything that isn't a number makes it a plain flex item, as wide as its content | Invalid at computed-value time unsets `flex`, `width`, `max-width`; measured 142.5, 10, 10, 411.66, `abc` 36.06 |
| 12 | Odd offsets: `"0"` is none, a negative push moves towards the start, an offset past the row overflows it, and anything not a number is ignored | measured `off0`, `push="-1"` 36.67, `offset="11"` at 353.33 |
| 13 | `Avonic.GridColumns` changes the count, from any ancestor's resources | `var(--ion-grid-columns, 12)`; measured `6`: size 3 is 183, offset 1 is 61 |
| 14 | Column padding: 5, or `Avonic.GridColumnPadding`, with the breakpoint's key winning | `col.css`; measured 2 and `-xs` 7: 7 |
| 15 | Grid padding: 5, or `Avonic.GridPadding`, with the breakpoint's key winning | `grid.css`; measured 12, and `-md` 20 at 800 |
| 16 | `Fixed`: 100%, 540, 720, 960 and 1140 wide from each breakpoint, centred, and never wider than its container. `Avonic.GridWidth` and the breakpoint's key set it | `grid.css`; measured at 576 x = 18 w = 540, 768 w = 720, 992 w = 960; 300 in a 150 box: 150; `--ion-grid-width: 300px` 300; `-xs: 200px` 200 |
| 17 | `Classes="ion-no-padding"` on a grid: no padding on the grid or on its columns, at any depth. A grid inside keeps its own, and its columns have none | `:host(.ion-no-padding)` sets the column variables, and `padding.css` the grid's; measured `gnest` |
| 18 | Columns on a line are as tall as the tallest. One with its own `VerticalAlignment` keeps its height and aligns, as `align-self` | `align-items: normal` (stretch); measured 67, 67; `ion-align-self-start` 29 at the top, `-center` y + 19 |
| 19 | A column with no size is never narrower than its longest word, and the others share the rest | `min-width: auto`; measured `Supercalifragilisticexpialidocious` 244.11 and three of 45.3 |
| 20 | Something in a row that isn't a column is as wide as its content, and the columns share the rest | `flex: 0 1 auto`; measured a `div` at 58.94 |
| 21 | RTL: columns start at the right, and offset, push and pull mirror | measured with `dir="rtl"` on the document |
| 22 | Rows stack in the grid, each as wide as the grid inside its padding. A grid with nothing in it is its padding | `display: block`; measured 380 at x = 5; empty 10 high |
| 23 | Text in a column wraps, and nothing is clipped: a column wider than the row shows | `white-space: normal`, `overflow: visible`; measured |
| 24 | In a flex row, as in an item, the grid fills what's left | `:host { flex: 1 }`; measured 358 in an item, and 355.22 after a `span` |
| 25 | The arrow keys don't move focus between controls in a grid | measured: focus stays on the button for all four |
| 26 | Automation: no role. A grid isn't a list and a column isn't a list item | `role` is never set; measured: neither is in the accessibility tree |

## Design

**`AvonicGrid` and `AvonicRow` are `ItemsControl`s, and `AvonicCol` a `ContentControl`.** The grid's panel is the default
vertical `StackPanel`, which is `display: block` for rows. The row's is `RowPanel`, which is `display: flex; flex-wrap: wrap`.
`FlexRow` can't be reused: it has one line and no percentages. A column made by an `ItemTemplate` arrives in a
`ContentPresenter`, and `RowPanel` looks through it, as `AvonicList.ContainerOf` does.

**What a column's props mean is worked out in `AvonicCol`, and `RowPanel` does the layout.** The column turns its props, the
breakpoint and the column count into one of four kinds — grow (no size), fixed (a fraction), auto, or content (a size that
isn't a number) — with an offset and a shift, all as fractions of the row. When any of those change, it invalidates the panel.
`RowPanel` follows the flexbox algorithm for those four, as `col.css` and the inline style give them:

| Kind | `flex` | width | max | min |
|---|---|---|---|---|
| grow | `1 1 0` | — | the row | `min-content` |
| fixed | `0 0 n%` | `n%`, at least its padding | `n%` | — |
| auto | `0 0 auto` | content | the row | — |
| content, and anything that isn't a column | `0 1 auto` | content | — | `min-content` |

An offset is a margin at the start, counted when the line breaks. A shift is added when arranging, so it moves nothing else.
Columns on a line are arranged as tall as the line, so a column's own `VerticalAlignment` is `align-self`, with no code.
`min-content` is worked out as `FlexRow` does it, from the text's longest word, because `Measure` clamps to what's available
and can't report it. `FlexRow`'s is private, so `RowPanel` has a copy, and the PR proposes sharing it.

**The viewport is the `TopLevel`'s client width.** `matchMedia` and `@media (min-width)` read the window's width, whatever the
grid is in (behaviour 9). On a phone the `TopLevel` is the screen, and on the desktop it's the window, which is what a browser's
viewport is. A `PopupRoot` is also a `TopLevel`, so a grid in a popup uses the `TopLevel` the popup belongs to, as a browser has one
window. Watching the grid's own width instead would be a container query, which Ionic doesn't do. One watcher per `TopLevel`
holds the breakpoint, and controls subscribe on attach, so a window resize that doesn't cross a breakpoint costs nothing.
Thresholds are 576, 768, 992 and 1200, from `SIZE_TO_MEDIA`.

**Breakpoints are cumulative pseudoclasses: `:breakpoint-sm`, `:breakpoint-md`, `:breakpoint-lg`, `:breakpoint-xl`.** Each is
set from that width up, as `min-width` media queries match, so a theme's later rule wins as the later `@media` does. The grid
and the column have them, for the padding and the fixed width. There's no `:breakpoint-xs`: it's the plain rule.

**Everything is in `ControlTheme`s.** Nothing goes in `Styles/`, so the style budget doesn't move. The column's padding, the
grid's padding and the fixed width are `Css.Var` chains in the themes. Padding is one length on four sides, so the themes use
`GridCss.Padding`, a converter in the grid's folder that does `var()` and makes a `Thickness`.

**`Fixed` is `MaxWidth`.** `width: 540px; max-width: 100%; margin: auto` is a box at most 540 wide, centred, and a
stretched Avalonia control with a `MaxWidth` is exactly that. `100%` is `Infinity`.

**`ion-no-padding` on a grid sets the column variables, as resources.** In Ionic the grid's `:host(.ion-no-padding)` sets the
six `--ion-grid-column-padding*` variables to 0, and they inherit. Here the grid watches its `Classes` and puts a dictionary
with the six keys at 0 in its own `Resources.MergedDictionaries`. Columns read them through `DynamicResource` as they would
any ancestor's, a nested grid's columns too, and an app's own key on the same grid still wins, as an app's style beats a
`:host` rule. The grid's own padding is `padding.css`, which `ionic.bundle.css` loads: a `^.ion-no-padding` rule last in the
grid's theme.

**`flex: 1` on the grid** is `FlexRow.Flex = 1` in its theme, so an item's slot grows it (mapping rule). `RowPanel` also reads
`FlexRow.Flex` on a child that isn't a column, so a grid in a row grows too.

**Overflow is visible.** A `TemplatedControl` clips by default, so all three override `ClipToBounds` to `false`, as the button does.

**Text wraps in a column:** `TextBlock.TextWrapping="Wrap"` on the column, in the theme, as on the card. Avalonia's `Wrap` also
breaks a word that doesn't fit, which is what Ionic's `overflow-wrap: break-word` on `body` does.

**No arrow keys and no list role.** An `ItemsControl` moves focus with the arrow keys, so the grid and the row override
`OnKeyDown`, as `AvonicList` does. Their automation peers are plain `Group`s, not lists.

### Extensions

None.

## Deliberate differences

| Difference | Why |
|---|---|
| A column follows a resize in the same layout pass, where Ionic's lags a frame: the grid's padding changes in the `resize` event, and the column's size one frame later (measured 45 ms and 60 ms) | Stencil schedules the re-render. A frame with the two out of step is a flicker, not a behaviour |
| RTL is the column's `FlowDirection`. Ionic reads `document.dir`, so under a `dir="rtl"` element in an LTR page its offset lands at the end and push and pull go the wrong way (measured: offset 3 left the column at the start, x = 290; push moved it to 385) | Avalonia has one direction per control, and the page-wide case, which is what an RTL app is, matches |
| A size, offset, push or pull must be a CSS number. Ionic pastes the string into `calc()`, so `"3 + 1"` would compute something | An expression in a column count isn't something anyone means |
| `Avonic.GridWidth*` are lengths. CSS takes a percentage too | `Infinity` stands for the default `100%`. Set `MaxWidth` for anything else |
| `Avonic.GridColumns` that isn't a positive number counts as unset, so 12 | CSS would divide by it. 0 is a division by zero there |
| `min-content` is estimated as `FlexRow` does: the longest word of a wrapping `TextBlock`, and the sum or maximum of what's in a panel | `Measure` can't report it. The same estimate as an item's label |
| A column outside a row is a plain block, as wide as its container. Ionic's is `display: inline` there (measured 41.45) | It isn't how a column is used, and an inline box isn't something Avalonia has |

## Not in this PR

| What | Why |
|---|---|
| `flex-utils.css`: `ion-justify-content-*`, `ion-align-items-*`, `ion-align-self-*`, `ion-nowrap`, `ion-wrap` | Global utility classes, not part of these components, as `.ion-padding` isn't. A column's `VerticalAlignment` does `align-self` |
| `.ion-no-padding`, `.ion-padding` on a column or a row | The same. `Padding` on the control does it |
| The default text size outside a host: Avalonia's, where a web page's is 16 | Its own decision, already raised at `ion-icon`. The capture sets the page to 16, as `ion-text`'s does |

## Dependencies

None to build on. `FlexRow` (for `FlexRow.Flex`, and the `min-content` approach) and `Css.Var` exist. Tests put a grid in an
`AvonicItem` and an `AvonicButton` in columns, and both exist.
