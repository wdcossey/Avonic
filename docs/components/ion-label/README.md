# ion-label

Status: done · [#7](https://github.com/wdcossey/Avonic/issues/7)

`AvonicLabel` is text that fits an [`AvonicItem`](../ion-item/README.md): it takes the item's colour, has the right
margins and sizes for headings and paragraphs, and can sit above the item's content or float over it. The design and
evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicItem>
    <c:AvonicLabel Content="A row" />
</c:AvonicItem>

<c:AvonicItem>
    <c:AvonicLabel Color="danger" Classes="ion-text-nowrap" Content="Red, on one line, with an ellipsis" />
</c:AvonicItem>

<c:AvonicItem>
    <c:AvonicLabel>
        <StackPanel>
            <TextBlock Classes="h2" Text="Title" />
            <TextBlock Classes="p" Text="Detail" />
        </StackPanel>
    </c:AvonicLabel>
</c:AvonicItem>

<!-- A stacked label over something else: the star row is what Ionic's flex: 1 does. -->
<c:AvonicItem>
    <Grid RowDefinitions="*,Auto">
        <c:AvonicLabel Position="Stacked" Content="Name" />
        <TextBox Grid.Row="1" />
    </Grid>
</c:AvonicItem>
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | See the [colour system](../../infra/color-system/README.md) |
| [x] | `position` | `Position` | `Fixed`, `Stacked`, `Floating`, or null for the default. Inside an item only, as in Ionic |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Content

The default slot is `Content` and `ContentTemplate`. A slot holds one child, so to have several, wrap them in a panel.
A panel directly inside the label stands for the slot, so a `p` in it is still a direct child.

| Ionic | Avonic |
|---|---|
| `<h1>` to `<h6>` | `<TextBlock Classes="h1" />` to `h6` |
| `<p>` | `<TextBlock Classes="p" />` |
| `class="ion-text-wrap"` | `Classes="ion-text-wrap"` |
| `class="ion-text-nowrap"` | `Classes="ion-text-nowrap"` |

### Resource keys

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--color` | `Avonic.Label.Color` | Unset, so the text inherits. Only read inside an item, and focus beats it |

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **Outside an item** it inherits its colour and size. `Position` and `Avonic.Label.Color` do nothing.
- **In an item** it tells the item about itself, which is what makes a stacked or floating item 55 high.
- **Stacked** sits at 75% size, moved down by half its height, and turns primary when the item has keyboard focus.
- **Floating** sits at full size, moved down by 96% of its height, and rises to where a stacked label sits on keyboard
  focus or when something in the item reports a value. It doesn't animate in its first second.
- **A tap on a label** doesn't reach the item's first control, but it does click a `Button` item.

Ionic 8's own controls don't report `has-focus`, `has-value`, `has-placeholder`, `input` or `textarea`. The label's rules
for them work, and a custom control can raise them through `IonStyle`.

## Extensions

None.
