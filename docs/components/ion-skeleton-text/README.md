# ion-skeleton-text

Status: done · [#17](https://github.com/wdcossey/Avonic/issues/17)

`AvonicSkeletonText` is a grey bar that stands in for a line of text while it loads, with an optional shimmer. In an avatar or a
thumbnail it fills the box. The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in
[COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicSkeletonText />
<c:AvonicSkeletonText Animated="True" Width="200" HorizontalAlignment="Left" />

<c:AvonicItem>
    <c:AvonicItem.StartContent>
        <c:AvonicThumbnail>
            <c:AvonicSkeletonText Animated="True" />
        </c:AvonicThumbnail>
    </c:AvonicItem.StartContent>
    <c:AvonicLabel>
        <StackPanel>
            <c:AvonicSkeletonText Animated="True" Width="230" HorizontalAlignment="Left" />
            <c:AvonicSkeletonText Animated="True" Width="170" HorizontalAlignment="Left" />
        </StackPanel>
    </c:AvonicLabel>
</c:AvonicItem>
```

A bar with a `Width` needs `HorizontalAlignment="Left"` to start at the left, as a block with a width does in CSS, since
Avalonia centres it. Ionic's `width: 80%` is a fixed `Width`, or a `Grid`'s star columns.

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `animated` | `Animated` (`bool`, false) | The shimmer, while `AvonicConfig.Animated` was on when it was read |

No events, methods, slots or parts, as in Ionic.

### Pseudoclasses

Ionic's host classes: `:animated` (`skeleton-text-animated`: `Animated` and the config both on) and `:in-media` (`in-media`: in an
`AvonicAvatar` or `AvonicThumbnail`). Both are worked out when it's first attached and when `Animated` changes, as Ionic works them
out when it renders.

### Template parts

`PART_Shimmer` is painted with the band by the theme, and moved by the control. The control makes it 800 wider than the host and
moves it with its `RenderTransform`, so a theme only paints it, in a tile 800 wide that repeats.

### Resource keys

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--background` | `Avonic.SkeletonText.Background` | `CssUnset`: `BackgroundRgb` at 0.065. A brush. Not read while animated, as in Ionic |
| [x] | `--background-rgb` | `Avonic.SkeletonText.BackgroundRgb` | `CssUnset`: `Avonic.TextColor`. A `Color` |
| [x] | `--border-radius` | `Avonic.SkeletonText.BorderRadius` | `CssUnset`: the parent's radius. A `CornerRadius`, a number or a CSS string such as `50%` |

The 10 px line, the 4 px margins, and the band's stops (8 %, 18 %, 33 %), alphas (0.065, 0.135) and 800 px tile are literals in the
theme, as Ionic has no variables for them. `AvonicSkeletonText.Rgba` and `AvonicSkeletonText.RadiusOrInherit` are the converters
the theme uses for `rgba(var(…), n)` and `var(--border-radius, inherit)`.

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **10 high, with 4 above and below, as wide as its parent,** whatever the font, or the height of a parent that has one, as
  `height: inherit` does.
- **The shimmer** moves a lighter band across it, 800 px a second, from the same place on every bar that starts together.
- **`AvonicConfig.Animated` off keeps it still,** if it's off when the bar is first attached or when `Animated` changes, as
  Ionic reads `animated` from its config when it renders. Ionic's `animated` reaches skeleton text, so this one is read.
- **In an avatar or a thumbnail,** at any depth, it fills the box with no margin, and takes the avatar's circle.
- **In an item it tells the item,** through `IonStyle`, so that the item's label clips, as Ionic's `item-skeleton-text` does.
- **A press goes through it,** as `pointer-events: none`.
- **The shimmer runs on `AvonicConfig`'s clock,** only while it's animated, attached and shown, and starts again from the
  beginning when it's attached or shown again, as a CSS animation does.
- **Automation doesn't see it,** as a screen reader doesn't read the `&nbsp;` inside.

## Extensions

None.

## Known gaps and deliberate differences

- **Margins don't collapse:** two bars in a `StackPanel` are 18 apart, where CSS has them 14 apart. Set `Margin="0,0,0,4"` on the
  second to match.
- **A bar standing in for a heading or a paragraph in a label** doesn't get the `h3`'s or the `p`'s margins, as it isn't in one.
- **No percentages:** use a `Width`, or a `Grid`'s star columns.

All of them, with their measurements, are in [PLAN.md](PLAN.md#deliberate-differences).
