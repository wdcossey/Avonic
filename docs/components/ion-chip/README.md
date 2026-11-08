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
- **In an item's or a divider's slot** the slot sets the side towards the content, 16, and the chip keeps its own 4 on the
  other three, so `4,4,16,4` at the start and `16,4,4,4` at the end, and `16,7,4,7` beside a stacked or floating label. An
  app's plain `Margin` style beats it, as a page's rule beats `::slotted`. The rules are in the theme's `Styles/Item.axaml`
  and `Styles/ItemDivider.axaml`, after the item's rule for any child in a slot.

## Extensions

Ionic's chip has no click of its own: an app listens to the element's own DOM `click`. These stand for it.

| Member | What |
|---|---|
| `Click` | A routed event, on the release of every press on the chip, as the DOM `click`: a quick second tap counts, where `Tapped` raises `DoubleTapped` instead. A finger that moves more than 15 px, a release off the chip, another mouse button, a second finger, a child that handles the press and a disabled chip don't click. Enter and Space don't either, as the chip has no native button. Automation's `Invoke`, a screen reader's activation, does. Through `Behaviors/NativeButton`, as `AvonicCard`'s button mode |
| `Command`, `CommandParameter` | Run after `Click`, unless a handler set `Handled`. A command that can't execute disables the chip, as on `AvonicButton` |

```xml
<c:AvonicChip Click="OnFilterClick" Command="{Binding ToggleFilter}" CommandParameter="recent">
    <c:AvonicLabel Content="Recent" />
</c:AvonicChip>
```

## Known gaps

| Gap | Why |
|---|---|
| An avatar child isn't sized | `chip.md.css`'s `::slotted(ion-avatar)`; it goes in `Styles/Chip.axaml`, in a PR of its own |
| A disabled panel fades the chip, a flex parent doesn't stretch it, and a taller parent centres it | Deliberate; see [COMPARISON.md](COMPARISON.md#deliberate-differences) |
| Content that can't shrink, such as a label with `ion-text-nowrap`, is clipped at the container, where Ionic's chip grows past it | Avalonia arranges a child within its space |
| A chip whose margin an app changes in a `ControlTheme` still has 4 on the other three sides in an item's or a divider's slot, where a CSS `:host` margin would follow | The slot rules are a `Style`, which beats any `ControlTheme`, and Avalonia can't set one side of a `Margin`. A plain `Style` for the margin beats them, as a page's rule does in CSS |
| Not tried in a real window | Headless capture only |
