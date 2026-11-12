# ion-item-divider

Status: done · [#9](https://github.com/wdcossey/Avonic/issues/9)

`AvonicItemDivider` is a heading row between items, with start, default and end content, that can stay at the top of what's
scrolling. The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicItemDivider Content="Section" />

<c:AvonicItemDivider Color="primary">
    <c:AvonicItemDivider.StartContent>
        <c:AvonicIcon IconName="heart" />
    </c:AvonicItemDivider.StartContent>
    <c:AvonicLabel Content="With icons" />
    <c:AvonicItemDivider.EndContent>
        <c:AvonicIcon IconName="star" />
    </c:AvonicItemDivider.EndContent>
</c:AvonicItemDivider>

<!-- Stays at the top until the end of its group. -->
<ScrollViewer>
    <c:AvonicList>
        <c:AvonicItemGroup>
            <c:AvonicItemDivider Sticky="True" Content="A" />
            <c:AvonicItem Content="Anna" />
        </c:AvonicItemGroup>
        <c:AvonicItemGroup>
            <c:AvonicItemDivider Sticky="True" Content="B" />
            <c:AvonicItem Content="Ben" />
        </c:AvonicItemGroup>
    </c:AvonicList>
</ScrollViewer>
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | |
| [x] | `sticky` | `Sticky` (`bool`) | Needs a `ScrollViewer` around it, and follows the nearest, as CSS follows the nearest scroll container. So one that's not scrolling, inside one that is, stops it. It does nothing in an inset list, as in Ionic |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Slots and parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | default slot | `Content`, `ContentTemplate` |
| [x] | `start` | `StartContent`, `StartContentTemplate` |
| [x] | `end` | `EndContent`, `EndContentTemplate` |
| [x] | `inner` | `PART_Inner` (`Border`) |
| [x] | `container` | `PART_Container` (`ContentPresenter`) |

No events or methods.

## Behaviours

Each has a test in `AvonicItemDividerTests`. See the [plan](PLAN.md#behaviours) for the source of each.

- 31 high at least, grey on the page's background, with a bottom border that darkens a colour under it.
- Icons in it are 24 and 54% text colour, or the contrast in a colour. Those in `StartContent` and `EndContent` have margins.
- A label in it has 13 above and 10 below, and acts as it does in an item, so `Avonic.Label.Color` is read.
- `TextBlock` with the class `p` is `#666666`, 14, with 2 below.
- `Sticky` moves the drawing, not the layout, and takes input where it's drawn. It stays in its parent, so a group ends
  it. In one flat list, the next sticky divider covers it.

## Differences from Ionic

- **In a flat `AvonicList` with a `VirtualizingStackPanel`, the list draws a stuck divider once the panel recycles its row** ([#68](https://github.com/wdcossey/Avonic/issues/68)).
  It's a second container for the same item, made from the list's `ItemTemplate`, so it looks the same and takes the input, but
  it's another instance: state an app sets on the divider itself doesn't carry over. Rows the panel never realised, as after a
  jump, are made from the template once to find the section's divider, and not shown; a `Sticky` set by a style there isn't
  seen, though one bound to the item is. An item that's a control can't be in two places, so it isn't drawn twice: give the
  list data and a template. In another `ItemsControl`, a group per section still keeps it.
- **An app's own `RenderTransform` stops it sticking.** Wrap it to do both.
- **`h1`–`h6` have no styles here.** Ionic's come from its global typography: see the [plan](PLAN.md#not-in-this-pr) for the numbers.

## Extensions

None.

## Resource keys

| Ionic | Key | Type | md default |
|---|---|---|---|
| `--background` | `Avonic.ItemDivider.Background` | brush | Unset → `Avonic.BackgroundColorBrush` |
| `--color` | `Avonic.ItemDivider.Color` | brush | Unset → `Avonic.TextColorStep600Brush` |
| `--padding-top`, `-end`, `-bottom` | `Avonic.ItemDivider.PaddingTop`, `…End`, `…Bottom` | `double` | 0 |
| `--padding-start` | `Avonic.ItemDivider.PaddingStart` | `double` | 16 |
| `--inner-padding-top`, `-bottom`, `-start` | `Avonic.ItemDivider.InnerPaddingTop`, `…Bottom`, `…Start` | `double` | 0 |
| `--inner-padding-end` | `Avonic.ItemDivider.InnerPaddingEnd` | `double` | 16 |
