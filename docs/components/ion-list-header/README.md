# ion-list-header

Status: done · [#9](https://github.com/wdcossey/Avonic/issues/9)

`AvonicListHeader` is a heading row for an [`AvonicList`](../ion-list/README.md). The design and evidence are in [PLAN.md](PLAN.md),
and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicListHeader Content="Settings" />

<c:AvonicListHeader Color="primary" Lines="Full" Content="A colour, and a full line" />

<!-- A label and something else. The star is what Ionic's flex: 1 1 auto does. -->
<c:AvonicListHeader>
    <Grid ColumnDefinitions="*,Auto">
        <c:AvonicLabel Content="Recent" />
        <Button Grid.Column="1" Content="More" />
    </Grid>
</c:AvonicListHeader>
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | |
| [x] | `lines` | `Lines` (`ListHeaderLines?`) | `Full`, `Inset`, `None`. A list's `Lines` doesn't reach a header |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Slots and parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | default slot | `Content`, `ContentTemplate` |
| [x] | `inner` | `PART_Inner` (`Border`) |

No events or methods.

## Behaviours

Each has a test in `AvonicListHeaderTests`. See the [plan](PLAN.md#behaviours) for the source of each.

- At least 45 high, with 16 of padding at the start. A full line makes it 46, and an inset one stays inside the 45.
- Both borders use the border colour, even in a colour, and the background shows through them.
- A label fills the row, with no margin, and inherits. `Avonic.Label.Color` isn't read here, unless the header is in a group.

## Differences from Ionic

- **One child in the slot,** where Ionic's is a flex row. Use a `Grid`.
- **An explicit `Lines` beats a token,** as for `ion-item`.
- **A label's `Position` does nothing here.** Measured in Ionic, a stacked label's transform applies and pushes it out of the row.

## Extensions

None.

## Resource keys

| Ionic | Key | Type | md default |
|---|---|---|---|
| `--background` | `Avonic.ListHeader.Background` | brush | transparent |
| `--border-color` | `Avonic.ListHeader.BorderColor` | brush | Unset → `Avonic.ItemBorderColorBrush` |
| `--border-width` | `Avonic.ListHeader.BorderWidth` | `Thickness` | 0 |
| `--color` | `Avonic.ListHeader.Color` | brush | Unset → `Avonic.TextColorBrush` |
| `--inner-border-width` | `Avonic.ListHeader.InnerBorderWidth` | `Thickness` | 0 |

`--border-style` isn't applicable: Avalonia's `Border` only draws solid.
