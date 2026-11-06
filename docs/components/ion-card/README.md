# ion-card

Status: done · [#13](https://github.com/wdcossey/Avonic/issues/13)

The card family: `AvonicCard`, `AvonicCardHeader`, `AvonicCardTitle`, `AvonicCardSubtitle` and `AvonicCardContent`. A
card is a raised box for content, and it can be tappable. The design and evidence are in [PLAN.md](PLAN.md), and the
comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicCard>
    <StackPanel>
        <c:AvonicCardHeader>
            <StackPanel>
                <c:AvonicCardTitle Content="Title" />
                <c:AvonicCardSubtitle Content="Subtitle" />
            </StackPanel>
        </c:AvonicCardHeader>
        <c:AvonicCardContent Content="Text wraps, and the card grows to fit it." />
    </StackPanel>
</c:AvonicCard>

<c:AvonicCard Button="True" Click="OnCardClick" Color="danger">
    <c:AvonicCardContent>
        <StackPanel>
            <TextBlock Classes="h2" Text="Heading" />
            <TextBlock Classes="p" Text="Paragraph" />
        </StackPanel>
    </c:AvonicCardContent>
</c:AvonicCard>

<!-- CSS collapses the margins between cards to 10. Avalonia adds them up to 20, so take the difference off. -->
<StackPanel Spacing="-10">
    <c:AvonicCard Content="One" />
    <c:AvonicCard Content="Two" />
</StackPanel>
```

## API

### `AvonicCard`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `button` | `Button` | Activatable, with a ripple, a `Hand` cursor and a tab stop |
| [x] | `color` | `Color` | See the [colour system](../../infra/color-system/README.md) |
| [x] | `disabled` | `IsEnabled` | Opacity 0.3, no input. It also disables what's inside, as `IsEnabled` does |
| [ ] | `href`, `routerAnimation`, `routerDirection` | — | Out of scope: router. Use `Button` and `Click` |
| [ ] | `download`, `rel`, `target` | — | Not applicable: only apply with `href` |
| [ ] | `type` | — | Not applicable: HTML form |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `native` part | `PART_Native` | Other parts: `PART_Host` draws the shadow outside the clip, `PART_Ripple`, and `PART_Focus` shows keyboard focus (see Extensions) |

### `AvonicCardHeader`, `AvonicCardTitle`, `AvonicCardSubtitle`, `AvonicCardContent`

| ✓ | Control | Ionic | Avonic | Notes |
|---|---|---|---|---|
| [x] | header, title, subtitle | `color` | `Color` | A header paints base and contrast, and the others paint base |
| [ ] | header | `translucent` | — | Not applicable: only applies in ios |
| [ ] | all | `mode` | — | Not exposed (ADR 0001) |

### Content

All five have one slot, `Content` and `ContentTemplate`. A slot holds one child, so to have several, wrap them in a panel.
A panel directly inside stands for the slot, which matters in two places:

- **A title or subtitle has 8 above it unless it's first** in a panel directly in the header.
- **`h1`–`h6` and `p`** are style classes on a `TextBlock`, directly in the content or in a panel directly in it. Deeper
  down, set the values yourself. See the [plan](PLAN.md#deliberate-differences).

**Content straight after a header has no top padding,** when both are in the same panel. `:after-card-header` is that state.
A hidden header still counts, as it does in CSS.

**Inside a coloured card or coloured header,** a header, title or subtitle inherits the text colour, at any depth, even
over its own `Color`. A coloured header keeps its background. This is Ionic's `.ion-inherit-color`.

### Resource keys

| Ionic | Key | Type | md default |
|---|---|---|---|
| `ion-card` `--background` | `Avonic.Card.Background` | brush | Unset → `Avonic.CardBackgroundBrush` |
| `ion-card` `--color` | `Avonic.Card.Color` | brush | Unset → `Avonic.CardColorBrush` |
| `ion-card-title` `--color` | `Avonic.CardTitle.Color` | brush | Unset → `Avonic.TextColorStep150Brush` |
| `ion-card-subtitle` `--color` | `Avonic.CardSubtitle.Color` | brush | Unset → `Avonic.TextColorStep450Brush` |
| `--ion-card-background` | `Avonic.CardBackgroundBrush` | brush | Follows `Avonic.BackgroundColor` |
| `--ion-card-color` | `Avonic.CardColorBrush` | brush | Follows `Avonic.TextColorStep450` |
| none: `box-shadow` | `Avonic.Card.BoxShadow` | `BoxShadows` | CSS's three layers. See [COMPARISON.md](COMPARISON.md) for how they draw above 1× |

`Background` and `CornerRadius` are the card's own properties, and `Foreground` is what CSS calls `color`, so all can be
set directly.

## Behaviours

These are in [PLAN.md](PLAN.md#behaviours), numbered, each with tests named after its number. The ones that surprise:

- A `Button` card has **no hover state**, as in Ionic.
- **Keyboard focus shows on a `Button` card, which it doesn't in Ionic.** See [Extensions](#extensions).
- **Cards in a column are 20 apart unless the panel has `Spacing="-10"`,** where CSS collapses their margins to 10. For bound cards, put it on the `StackPanel` in `ItemsPanel`.
- A tap on a child also clicks a `Button` card. A nested `Button` card raises one `Click` that reaches both.
- `Click` comes on release, every time, and not with the secondary button.

## Extensions

Not in Ionic, and named so that they can't collide with it:

- **`Click`** on `AvonicCard`, a routed event. A native `<button>` gives Ionic this for free. It's only raised by a `Button`
  card, so use `Tapped` for others.

- **Keyboard focus** on a `Button` card itself shows the text colour at 0.12 over the card, and clears as an item's does. In Ionic it's invisible, which is an accessibility cost. `PART_Focus` is that overlay, and it's only in the theme.

`Command` isn't included. It follows #10.

## Known gaps

- **An item around a `Button` card, at any depth, also reacts to focus,** where Ionic's doesn't.
- **The shadow's offset doesn't scale above 1×,** which is Avalonia's rendering and accepted as best effort. See [COMPARISON.md](COMPARISON.md).
- **A mouse drag within the card** doesn't click, where it does in Ionic. A finger that slides shouldn't click in either, though this was only run with a mouse.
- **A child that handles `Tapped`** stops only the first of two quick taps. To stop both, handle `DoubleTapped` too.
- **`--ion-item-background` and `--ion-item-color`** don't move a card. See the [plan](PLAN.md#deliberate-differences).
