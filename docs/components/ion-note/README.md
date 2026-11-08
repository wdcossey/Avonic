# ion-note

Status: done · [#8](https://github.com/wdcossey/Avonic/issues/8)

`AvonicNote` is a small grey piece of text, usually a date or a count at the end of an
[`AvonicItem`](../ion-item/README.md). The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real
Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicNote Content="A note on its own" />

<c:AvonicItem>
    <c:AvonicLabel Content="Inbox" />
    <c:AvonicItem.EndContent>
        <c:AvonicNote Color="danger" Content="12 unread" />
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
| [x] | `--color` | `Avonic.Note.Color` | Unset, so it's `Avonic.TextColorStep400Brush`, `#666666` |

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **On its own** it's 14 px and grey, whatever's around it.
- **Directly in an item's or a divider's slot** it's 11 px, at the top of the row however tall the row is. In the start
  or end slot it has 18 above and 10 below, so its text lines up with a one-line label beside it.
- **A coloured item doesn't change it,** so it stays grey on the item's colour, as in Ionic. Set `Color` for a note that
  shows up there.
- **It's as wide as its text,** and a tap on it doesn't reach the item's first control.

A slot holds one control, so a panel directly in a slot stands for the slot: a note in it is slotted too, and the rest of
the panel stays centred. A vertical `StackPanel` there is centred as a group. Wrap a note in a `Border` to get what
`<div slot="end"><ion-note>` does in Ionic: 14 px and centred.

## Extensions

None.
