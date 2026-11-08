# ion-avatar

Status: done · [#16](https://github.com/wdcossey/Avonic/issues/16)

`AvonicAvatar` is a round picture, often at the start of an [`AvonicItem`](../ion-item/README.md). It was built with
[`ion-badge`](../ion-badge/README.md) and [`ion-thumbnail`](../ion-thumbnail/README.md), and the three share a
[PLAN.md](../ion-badge/PLAN.md) and a [COMPARISON.md](../ion-badge/COMPARISON.md).

```xml
<c:AvonicItem>
    <c:AvonicItem.StartContent>
        <c:AvonicAvatar>
            <Image Source="avares://MyApp/Assets/person.png" />
        </c:AvonicAvatar>
    </c:AvonicItem.StartContent>
    <c:AvonicLabel Content="Ada Lovelace" />
</c:AvonicItem>

<c:AvonicAvatar Background="{DynamicResource Avonic.Color.PrimaryBrush}">
    <TextBlock Text="AL" HorizontalAlignment="Center" VerticalAlignment="Center" />
</c:AvonicAvatar>
```

## API

It has no properties, events or methods of its own. The default slot is `Content` and `ContentTemplate`.

### Resource keys

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--border-radius` | `Avonic.Avatar.BorderRadius` | `50%` |

The key takes a `CornerRadius`, a number, or a string: a percentage, which is of the avatar's size, or a length such as `8px`.
Anything else is no radius. Put it in one avatar's `Resources` to change only that avatar.

## Behaviours

Everything is listed and measured in [PLAN.md](../ion-badge/PLAN.md#avatar). In short:

- **It's 64 × 64,** at the left, as a block is.
- **An `Image` in it fills it,** cropped from the middle, and is cut to the radius: a circle. Anything else in it isn't cut, and
  a `Background` on it follows the radius.
- **In an item's or a divider's slot** it's 40 × 40, with 8 above and below and 16 towards the content, so a row of 56. Beside a
  stacked label it's 7 above and below. Its own `Width` and `Height` beat the 40. A panel directly in a slot stands for the slot.

`ion-img` doesn't exist yet ([#17](https://github.com/wdcossey/Avonic/issues/17)). Its PR adds the same rule for itself.

## Extensions

None.

## Known gaps

- At `50%`, a background on an avatar that isn't square is a stadium, where Ionic's is an ellipse. An image in it is cut to the
  ellipse. A `CornerRadius` has one radius per corner.
- `ion-chip`'s rules for an avatar go with `ion-chip`.
