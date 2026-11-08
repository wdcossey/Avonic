# ion-chip

Status: done · [#19](https://github.com/wdcossey/Avonic/issues/19)

`AvonicChip` is a small rounded box holding a row of children, usually an icon and a label, that ripples when it's pressed.
The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicChip>
    <c:AvonicLabel Content="Default" />
</c:AvonicChip>

<c:AvonicChip Color="danger" Outline="True">
    <c:AvonicIcon IconName="heart" />
    <c:AvonicLabel Content="Favourite" />
</c:AvonicChip>
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | See the [colour system](../../infra/color-system/README.md) |
| [x] | `disabled` | `IsEnabled` | 0.4, and a tap goes to what's behind it |
| [x] | `outline` | `Outline` | A border, and no background |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Content

`core.json` lists no slot, and the default slot holds several children, so they're `Items`, as an `ItemsControl`'s are
([ADR 0002](../../decisions/0002-decisions-from-the-first-swarm.md)). A string is a child too, shown as text, and like a text node it isn't counted for the first and last child's margins, nor is any value shown as its text. One a template makes into a control is counted. The chip is an `ItemsControl`, so
it also has `ItemsSource` and `ItemTemplate`, and an icon made by the template is slotted too. It has no events, methods or parts in Ionic. Its template has `PART_Native`
(`Border`: background, border, radius, clip) and `PART_Ripple`.

The children sit in a row, centred, and the chip is as wide as they are, or as wide as there's room for when they don't fit,
when a label wraps and an icon beside it shrinks.

### Resource keys

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Chip.Background` | brush | Unset, so text at 0.12, and 0.16 on hover or focus, 0.2 pressed |
| [x] | `--color` | `Avonic.Chip.Color` | brush | Unset, so text at 0.87 |

Set, `Avonic.Chip.Background` is the background of a plain chip in every state, as a variable set from outside beats the
states in Ionic. An outline chip ignores it, and a coloured chip ignores both. `Avonic.Chip.Md.*` holds the defaults, which have
no variable in Ionic. They aren't Ionic's variables: override the keys above instead.

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **Plain** is the text colour at 0.12 behind text at 0.87. **Outline** is transparent with a 1 px border at 0.32. **`Color`** is
  the colour at 0.08 behind its shade, and an outline is the colour at 0.32. An unknown colour is transparent with inherited text.
- **A press** activates it after 100 ms, or at once on a quick tap, with a ripple in the text colour, clipped to the radius.
- **With a mouse or pen**, hover shows 0.16, or 0.04 on an outline and 0.12 on a colour, and still shows while pressed, as in
  Ionic. A finger shows the press: 0.2, 0.08 or 0.16.
- **It isn't a tab stop,** and a click doesn't focus it: focus stays where it was, where Chromium would move it to the page. Set `Focusable="True"`, as `tabindex` would, and any focus,
  a click's too, shows the hover values. No focus rectangle.
- **A slotted icon** is 1.43 em, so 20, at 0.54 of the text colour or the chip's shade. The first child has margins −4 at the
  start and 8 at the end, the last 8 and −4, so a leading icon sits 8 from the edge. One nested deeper gets none of it.
- **Keys do nothing:** no click on Enter or Space, and the arrows don't move focus between the children.

There's no click in Ionic's chip: an app listens to the element's own `click`, which every press and release on it raises.
Here `Tapped` is the nearest, though a second quick tap raises `DoubleTapped` instead, and `PointerReleased` sees every one.

## Extensions

None.

## Known gaps

| Gap | Why |
|---|---|
| A chip in an item's start or end slot has 0 margin on the other three sides, where Ionic keeps 4 | `ion-item`'s slot rule sets all four sides. Proposed in the PR |
| An avatar child isn't sized | `ion-avatar` is #16; its rule goes in `Styles/Chip.axaml` when it exists |
| A disabled panel fades the chip, a flex parent doesn't stretch it, and a taller parent centres it | Deliberate; see [COMPARISON.md](COMPARISON.md#deliberate-differences) |
| Content that can't shrink, such as a label with `ion-text-nowrap`, is clipped at the container, where Ionic's chip grows past it | Avalonia arranges a child within its space |
| Not tried in a real window | Headless capture only |
