# ion-grid

Status: done · [#14](https://github.com/wdcossey/Avonic/issues/14)

The grid family: `AvonicGrid`, `AvonicRow` and `AvonicCol`. A grid is rows of columns that share the width out of 12, with
sizes, offsets, pushes and pulls that can change at each breakpoint of the screen's width. The design and evidence are in
[PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicGrid>
    <c:AvonicRow>
        <c:AvonicCol Content="Shares the rest" />
        <c:AvonicCol Size="6" Content="Half" />
        <c:AvonicCol Size="auto" Content="As wide as this" />
    </c:AvonicRow>
    <c:AvonicRow>
        <!-- Full width on a phone, half from 576, a third from 768 -->
        <c:AvonicCol Size="12" SizeSm="6" SizeMd="4" Content="A" />
        <c:AvonicCol Size="12" SizeSm="6" SizeMd="4" Content="B" />
        <c:AvonicCol Size="12" SizeSm="6" SizeMd="4" Content="C" />
    </c:AvonicRow>
</c:AvonicGrid>

<!-- At most 540, 720, 960 or 1140 wide, centred, with no padding on the grid or its columns, and 6 columns -->
<c:AvonicGrid Fixed="True" Classes="ion-no-padding">
    <c:AvonicGrid.Resources>
        <x:Double x:Key="Avonic.GridColumns">6</x:Double>
    </c:AvonicGrid.Resources>
    <c:AvonicRow>
        <c:AvonicCol Size="2" Content="2 of 6" />
        <c:AvonicCol Size="3" Offset="1" Content="3 of 6, after 1" />
    </c:AvonicRow>
</c:AvonicGrid>
```

## API

### `AvonicGrid`

An `ItemsControl` of rows, in a vertical `StackPanel`.

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `fixed` | `Fixed` (`bool`) | `:fixed` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `class="ion-no-padding"` | `Classes="ion-no-padding"` | No padding on the grid, nor on any column inside it, a nested grid's too |

### `AvonicRow`

An `ItemsControl` of columns. Its panel is `RowPanel`: `display: flex; flex-wrap: wrap`. No props.

### `AvonicCol`

A `ContentControl`: one cell, whose `Content` is what's in it. Every prop is a `string?`, as in Ionic: a number of columns,
which can be a fraction, or `"auto"` for `Size`. `null` is unset, and `""` at a breakpoint goes back to the default there.

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `size`, `sizeXs` … `sizeXl` | `Size`, `SizeXs`, `SizeSm`, `SizeMd`, `SizeLg`, `SizeXl` |
| [x] | `offset`, `offsetXs` … `offsetXl` | `Offset`, `OffsetXs`, `OffsetSm`, `OffsetMd`, `OffsetLg`, `OffsetXl` |
| [x] | `push`, `pushXs` … `pushXl` | `Push`, `PushXs`, `PushSm`, `PushMd`, `PushLg`, `PushXl` |
| [x] | `pull`, `pullXs` … `pullXl` | `Pull`, `PullXs`, `PullSm`, `PullMd`, `PullLg`, `PullXl` |
| [ ] | `mode` | — (ADR 0001) |

A column's own `VerticalAlignment` is `align-self`: unset, it's as tall as the tallest on its line. `Classes="ion-no-padding"`
takes its padding off, as `padding.css` does.

### Breakpoints

`xs` from 0, `sm` from 576, `md` from 768, `lg` from 992 and `xl` from 1200, of the **viewport**: the `TopLevel`'s client
width, which is the screen on a phone and the window on the desktop, whatever the grid is in. A popup uses the window it
belongs to. For each prop, the last breakpoint at or below the viewport that has a value wins, and `xs` beats the plain one.

The grid and the columns have `:breakpoint-sm`, `:breakpoint-md`, `:breakpoint-lg` and `:breakpoint-xl`, each from that width
up, as `min-width` media queries are. The theme uses them for the padding and the fixed width.

### Resource keys

Set any of them in the `Resources` of the grid, or anything above it, as a CSS variable is set on any ancestor. All are
lengths, as `double`, except the count. Unset (`CssUnset`) keys fall back as in Ionic: the breakpoint's key, then the
general one, then the md default.

| Ionic | Key | Default |
|---|---|---|
| `--ion-grid-padding`, `-xs` … `-xl` | `Avonic.GridPadding`, `Avonic.GridPaddingXs` … `Avonic.GridPaddingXl` | `Avonic.Grid.Md.Padding`: 5 |
| `--ion-grid-width`, `-xs` … `-xl` | `Avonic.GridWidth`, `Avonic.GridWidthXs` … `Avonic.GridWidthXl` | `Avonic.Grid.Md.WidthXs` … `WidthXl`: `Infinity` (100%), 540, 720, 960, 1140 |
| `--ion-grid-column-padding`, `-xs` … `-xl` | `Avonic.GridColumnPadding`, `Avonic.GridColumnPaddingXs` … `Avonic.GridColumnPaddingXl` | `Avonic.Grid.Md.ColumnPadding`: 5 |
| `--ion-grid-columns` | `Avonic.GridColumns` | 12, in `AvonicCol.DefaultColumns`. A number of any type; anything that isn't a positive number is 12 |

## Behaviours

All 26 in [PLAN.md](PLAN.md#behaviours), each tested in `AvonicGridTests`. In short: columns with no size share the row,
and none is narrower than its longest word; sizes, offsets, pushes and pulls are fractions of the row; columns wrap when
they don't fit; a line is as tall as its tallest column; everything follows the viewport's breakpoint as it changes; the
grid fills an item's row, as `flex: 1` does; the arrow keys don't move focus; and none of the three has a role.

## Extensions

None.

## Known gaps

- **A grid in a `Popup` isn't tested.** The headless platform has no popup windows. The code reads the `TopLevel` the
  popup belongs to.
- **`min-content` is estimated** from a wrapping `TextBlock`'s longest word, as an item's label does, so a control that
  could shrink in CSS some other way doesn't here.
- **In a horizontally scrolling `ScrollViewer`,** such as `AvonicContent ScrollX="True"`, columns are as wide as their
  content, on one line, with no offset, push or pull. Avalonia measures there with no width, and laying out again at the
  arranged width would loop.
- **Not tried in a real window.**
- The utility classes of `flex-utils.css` (`ion-justify-content-*`, `ion-align-items-*`, `ion-nowrap` …) aren't provided,
  as `.ion-padding` isn't. A column's `VerticalAlignment` does `align-self`.
- The deliberate differences, each measured, are in [COMPARISON.md](COMPARISON.md#known-differences-in-behaviour): a resize
  needs no frame to reach the columns, and RTL is each control's `FlowDirection` rather than the document's.
