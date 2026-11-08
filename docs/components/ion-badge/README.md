# ion-badge

Status: done · [#16](https://github.com/wdcossey/Avonic/issues/16)

`AvonicBadge` is a small coloured count or label, often at the end of an [`AvonicItem`](../ion-item/README.md). It was built
with [`ion-avatar`](../ion-avatar/README.md) and [`ion-thumbnail`](../ion-thumbnail/README.md), and the three share a
[PLAN.md](PLAN.md) and a [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicBadge Content="22" />
<c:AvonicBadge Color="danger" Content="99+" />

<c:AvonicItem>
    <c:AvonicLabel Content="Inbox" />
    <c:AvonicItem.EndContent>
        <c:AvonicBadge Content="22" />
    </c:AvonicItem.EndContent>
</c:AvonicItem>
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | See the [colour system](../../infra/color-system/README.md) |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Content

The default slot is `Content` and `ContentTemplate`. It has no named slots, parts, events or methods.

### Resource keys

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--background` | `Avonic.Badge.Background` | Unset, so it's `Avonic.Color.PrimaryBrush` |
| [x] | `--color` | `Avonic.Badge.Color` | Unset, so it's `Avonic.Color.PrimaryContrastBrush` |
| [x] | `--padding-top` | `Avonic.Badge.PaddingTop` | 3 |
| [x] | `--padding-end` | `Avonic.Badge.PaddingEnd` | 4 |
| [x] | `--padding-bottom` | `Avonic.Badge.PaddingBottom` | 4 |
| [x] | `--padding-start` | `Avonic.Badge.PaddingStart` | 4 |

Put a key in one badge's `Resources` to change only that badge, as a `style="--background: …"` would.

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **It's 13 px bold, primary on white, 20 high,** with a radius of 4, whatever is around it, and as wide as its text plus 8,
  at least 10. It never wraps.
- **`Color` paints the colour behind its contrast.** An unknown colour has no background and the text of its parent, or a
  coloured item's colours, as in Ionic. A coloured item doesn't change a badge without one.
- **Empty, it's hidden,** with `Content` null or `""`, and takes no space or margin, even in a slot.
- **In an item's slot** it has the slot's margin of 16, 7 above and below beside a stacked label, and it's centred in the row.

## Extensions

None.

## Known gaps

- A badge wider than its parent is cut there, where Ionic's runs off the page. See [COMPARISON.md](COMPARISON.md#deliberate-differences-and-known-gaps).
- Outside an item, it's centred down in what it's given rather than sitting on the text's baseline.
- `ion-tab-button`'s and `ion-chip`'s rules for a badge go with those controls.
