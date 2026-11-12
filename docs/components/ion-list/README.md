# ion-list

Status: done · [#9](https://github.com/wdcossey/Avonic/issues/9)

`AvonicList` is a block of [`AvonicItem`](../ion-item/README.md)s that can share their lines and sit inset from the page, usually
with an [`AvonicListHeader`](../ion-list-header/README.md), [`AvonicItemDivider`](../ion-item-divider/README.md)s and
[`AvonicItemGroup`](../ion-item-group/README.md)s. The design and evidence are in [PLAN.md](PLAN.md), and the comparison
with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicList Lines="Full">
    <c:AvonicListHeader Content="Settings" />
    <c:AvonicItem Content="Takes the list's lines" />
    <c:AvonicItem Content="Its own lines win" Lines="None" />
</c:AvonicList>

<c:AvonicList Inset="True">
    <c:AvonicItem Button="True" Detail="True" Content="Rounded, with a margin" />
    <c:AvonicItem Content="The last item has no border" />
</c:AvonicList>

<!-- A long list. It doesn't scroll by itself, so it goes in what does. -->
<ScrollViewer>
    <c:AvonicList ItemsSource="{Binding People}">
        <c:AvonicList.ItemsPanel>
            <ItemsPanelTemplate>
                <VirtualizingStackPanel />
            </ItemsPanelTemplate>
        </c:AvonicList.ItemsPanel>
        <c:AvonicList.ItemTemplate>
            <DataTemplate>
                <c:AvonicItem Content="{Binding Name}" />
            </DataTemplate>
        </c:AvonicList.ItemTemplate>
    </c:AvonicList>
</ScrollViewer>
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `inset` | `Inset` (`bool`) | Margin 16, radius 2, and the last item loses its border |
| [x] | `lines` | `Lines` (`ListLines?`) | `Full`, `Inset`, `None`. An item's own `Lines` beats it |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

`Items`, `ItemsSource`, `ItemTemplate` and `ItemsPanel` come from `ItemsControl`.

### Methods

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [ ] | `closeSlidingItems()` | — | Comes with `ion-item-sliding` (#31). It could only return `false` until then |

No events, slots, parts or CSS variables.

### Pseudoclasses

`:lines-full`, `:lines-inset`, `:lines-none`, `:inset`, and `:after-inset` for an inset list straight after another.

## Behaviours

Each has a test in `AvonicListTests`. See the [plan](PLAN.md#behaviours) for the source of each.

- Items stack at full width in 8 of padding, on `Avonic.ItemBackgroundBrush`.
- `Lines` reaches items at any depth, unless they set their own, and beats their other borders.
- Between nested lists, inset beats full, which beats none, whichever is inside.
- `Inset` adds a margin and rounded corners that clip, and takes the border off the last item in each parent, whatever
  its lines. An inset list straight after one has no top margin.
- Each button item is its own tab stop, and the arrow keys do nothing.
- It's a `List` to automation, and a plain item in it is a `ListItem`.

## Differences from Ionic

The full list is in the [plan](PLAN.md#deliberate-differences). The ones you may notice:

- **The last item is the last child, not the last of its type.** `[header, item]` loses the border here and keeps it in Ionic.
- **A button item is a `Button` to automation,** not a `ListItem`.
- **`role="listitem"` is in the theme,** so it goes if you replace the theme.
- **Margins don't collapse.**
- **Inset lists as rows of a `VirtualizingStackPanel`** are 16 apart, as in Ionic. The top row, while the item before it isn't
  realised, takes that item to be a list like itself: where it isn't, the row moves by 16 as it's realised.
- **A last item in a `Border` keeps its border.** A `StackPanel` works.

## Extensions

None. Virtualising comes from `ItemsControl`.

## Resource keys

None of its own, as in Ionic. It reads `Avonic.ItemBackgroundBrush`.
