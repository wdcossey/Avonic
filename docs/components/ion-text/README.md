# ion-text

Status: done · [#8](https://github.com/wdcossey/Avonic/issues/8)

Colours whatever it's around, and does nothing else. HTML has one element for this, and Avalonia has two kinds, so
there are two types. The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in
[COMPARISON.md](COMPARISON.md).

| Use | Type |
|---|---|
| Words in a sentence, which flow and wrap with the words around them | `AvonicTextSpan`, in a `TextBlock` |
| Anything else: a block, several blocks, an icon, or text in an item's slot | `AvonicText` |

```xml
<TextBlock TextWrapping="Wrap">
    Some words and <c:AvonicTextSpan Color="primary">some coloured ones</c:AvonicTextSpan> in a sentence.
</TextBlock>

<c:AvonicText Color="danger">
    <StackPanel>
        <TextBlock Text="A block" />
        <c:AvonicIcon IconName="heart" />
    </StackPanel>
</c:AvonicText>

<c:AvonicItem>
    <c:AvonicText Color="danger" Content="In an item" />
</c:AvonicItem>

<!-- In a label, the TextBlock holds the class, and the span holds the colour. -->
<c:AvonicLabel>
    <TextBlock Classes="p"><c:AvonicTextSpan Color="danger">Red</c:AvonicTextSpan></TextBlock>
</c:AvonicLabel>
```

`AvonicTextSpan` isn't a control, so it only goes in a `TextBlock` or another span. Directly in a slot it shows its type's
name.

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color`, on both types | See the [colour system](../../infra/color-system/README.md) |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Content

The default slot is `Content` and `ContentTemplate` on `AvonicText`, and `Inlines` on `AvonicTextSpan`. It has no named
slots, parts, events, methods or CSS variables.

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **With no colour** it does nothing. Colour, size and font come from its parent.
- **An unknown colour** inherits, or shows a coloured ancestor's base, as in Ionic.
- **Nested,** an inner one with no colour takes the outer's, and an inner colour wins.
- **It works without the theme,** because this is what `Color` means rather than a look, though the palette comes from the theme.
- `h1`–`h6` and `p` style classes come from `AvonicLabel`, and only on a `TextBlock` directly in it or in a panel directly
  in it. So `AvonicText` around one takes them away. Put the span inside the `TextBlock`.

## Known gaps

A button item's automation name doesn't include words written as inlines, spans or not, because it reads `TextBlock.Text`.
That's the item's to fix. Until then, set `AutomationProperties.Name` on the item. See the
[plan](PLAN.md#deliberate-differences).

## Extensions

None. `AvonicTextSpan` isn't an addition: it's the inline half of `ion-text`.
