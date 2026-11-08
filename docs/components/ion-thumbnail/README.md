# ion-thumbnail

Status: done · [#16](https://github.com/wdcossey/Avonic/issues/16)

`AvonicThumbnail` is a square picture, often at the start of an [`AvonicItem`](../ion-item/README.md). It was built with
[`ion-badge`](../ion-badge/README.md) and [`ion-avatar`](../ion-avatar/README.md), and the three share a
[PLAN.md](../ion-badge/PLAN.md) and a [COMPARISON.md](../ion-badge/COMPARISON.md).

```xml
<c:AvonicItem>
    <c:AvonicItem.StartContent>
        <c:AvonicThumbnail>
            <Image Source="avares://MyApp/Assets/photo.png" />
        </c:AvonicThumbnail>
    </c:AvonicItem.StartContent>
    <c:AvonicLabel Content="A photo" />
</c:AvonicItem>
```

## API

It has no properties, events or methods of its own. The default slot is `Content` and `ContentTemplate`.

### Resource keys

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--size` | `Avonic.Thumbnail.Size` | Unset, so it's `Avonic.Thumbnail.Md.Size`, 48, or `Avonic.Thumbnail.Md.SizeInItem`, 56, in an item's slot |
| [x] | `--border-radius` | `Avonic.Thumbnail.BorderRadius` | 0 |

The radius takes a `CornerRadius`, a number, or a string as CSS writes `border-radius`: one to four lengths or percentages, where a
percentage is of the thumbnail's size. Anything else is no radius. The size takes a number. Put a key in one thumbnail's `Resources` to change only that thumbnail. `Avonic.Thumbnail.Size`
set anywhere wins in an item too, as a page's rule beats the item's `::slotted` one in CSS.

## Behaviours

Everything is listed and measured in [PLAN.md](../ion-badge/PLAN.md#thumbnail). In short:

- **It's 48 × 48,** at the left, as a block is.
- **An `Image` in it fills it,** cropped from the middle, with square corners, or cut to the radius. Anything else in it isn't cut.
- **In an item's or a divider's slot** it's 56 × 56, with 8 above and below and 16 towards the content, so a row of 72. Beside a
  stacked label it's 7 above and below. Its own `Width` and `Height` beat the 56. A panel directly in a slot stands for the slot.

`ion-img` doesn't exist yet ([#17](https://github.com/wdcossey/Avonic/issues/17)). Its PR adds the same rule for itself.

## Extensions

None.

## Known gaps

None of its own. At `50%`, see [ion-avatar](../ion-avatar/README.md#known-gaps).
