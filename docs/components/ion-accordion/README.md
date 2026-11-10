# ion-accordion and ion-accordion-group

Status: done · [#32](https://github.com/wdcossey/Avonic/issues/32)

`AvonicAccordionGroup` holds which of its `AvonicAccordion`s are open. Each accordion has a header, usually an `AvonicItem`,
and content that slides open below it when the header is tapped. The design and evidence are in [PLAN.md](PLAN.md), and the
comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicAccordionGroup Value="first" IonChange="OnIonChange">
    <c:AvonicAccordion Value="first">
        <c:AvonicAccordion.HeaderContent>
            <c:AvonicItem><c:AvonicLabel Content="First Accordion" /></c:AvonicItem>
        </c:AvonicAccordion.HeaderContent>
        <c:AvonicAccordion.ContentContent>
            <TextBlock Margin="16" Text="First Content" />
        </c:AvonicAccordion.ContentContent>
    </c:AvonicAccordion>
</c:AvonicAccordionGroup>

<c:AvonicAccordionGroup Expand="Inset" Multiple="True" Value="{Binding Open}">…</c:AvonicAccordionGroup>
```

## API

### AvonicAccordion

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `disabled` | `IsEnabled` | No toggle; header and content at 0.4 |
| [x] | `readonly` | `Readonly` | No toggle, and no pointer input in the header or the content; looks the same |
| [x] | `toggleIcon` | `ToggleIcon` (`"chevron-down"`) | A name or a source. Read when it first loads |
| [x] | `toggleIconSlot` | `ToggleIconSlot` (`AccordionToggleIconSlot.End`) | `Start` or `End` of the header item. Read when it first loads |
| [x] | `value` | `Value` | `ion-accordion-N` by default |
| [x] | slot `header` | `HeaderContent`, `HeaderContentTemplate` | |
| [x] | slot `content` | `ContentContent`, `ContentContentTemplate` | |
| [x] | part `header` | `PART_Header` | |
| [x] | part `content` | `PART_Content` (`AccordionContentClip`), with `PART_ContentWrapper` inside | |
| [x] | part `expanded` | `:expanded` | `::part(header expanded)` → `c\|AvonicAccordion:expanded /template/ ContentPresenter#PART_Header` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

Pseudoclasses, from Ionic's host classes: `:accordion-expanding`, `:accordion-expanded`, `:accordion-collapsing`,
`:accordion-collapsed`, `:accordion-next`, `:accordion-previous`, `:accordion-animated`; `:accordion-group-expand-inset` for
`ion-accordion-group.accordion-group-expand-inset > ion-accordion`; and `:first-in-group` and `:last-in-group` for
`ion-accordion-group > ion-accordion:first-of-type` and `:last-of-type`, which hold for accordions made from `ItemsSource` too.

### AvonicAccordionGroup

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `animated` | `Animated` (`true`) | |
| [x] | `disabled` | `IsEnabled` | |
| [x] | `expand` | `Expand` (`AccordionGroupExpand.Compact`) | `:expand-compact`, `:expand-inset` |
| [x] | `multiple` | `Multiple` | |
| [x] | `readonly` | `Readonly` | |
| [x] | `value` | `Value` (`object?`) | A `string`, an `IEnumerable<string>` with `Multiple`, or null |
| [x] | `ionChange` | `IonChange`, `IonChangeEvent` (`AccordionGroupChangeEventArgs.Value`) | Only from a tap or a key |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

Its accordions are its `Items`: it's an `ItemsControl`, and an accordion is its own container. Accordions made from
`ItemsSource` and an `ItemTemplate` are its own too.

### Resource keys

Neither has a CSS variable. These are md literals, so that they can be restyled:

| Key | Value | From |
|---|---|---|
| `Avonic.AccordionGroup.Md.InsetBoxShadow` | `0 3 1 -2 #33000000, 0 2 2 0 #24000000, 0 1 5 0 #1F000000` | `accordion-group.md.css` |
| `Avonic.Accordion.Md.Transition` | margin and corners, 300 ms, `cubic-bezier(0.25, 0.8, 0.5, 1)` | `:host(.accordion-animated) { transition: all … }` |
| `Avonic.Accordion.Md.ToggleIconTransition` | `RenderTransform`, 300 ms, the same curve | `core.css` |

The accordion's background is `Avonic.BackgroundColorBrush` (`--ion-background-color`).

### For themes

`AccordionCorners.Top` and `.Bottom` are an inset accordion's top and bottom radii, which Ionic sets apart; the template makes
`CornerRadius` from them with `AccordionCorners.ToCornerRadius`. `AccordionContentClip` is `#content`: it measures its child at
its own height and clips it, so the content slides rather than squeezes.

## Behaviours

- The group's `Value` opens the accordion whose `Value` equals it, or each one in the list with `Multiple`.
- A tap anywhere in an accordion's header, or Enter or Space on its item, asks the group to toggle it. The group sets `Value`
  and raises `IonChange`. Setting `Value` in code raises nothing.
- A readonly or disabled accordion or group can't be toggled. An accordion outside a group never opens.
- When it first loads, an accordion makes the `AvonicItem` in its header slot, given or from `HeaderContentTemplate` (not one
  inside a panel there, as Ionic
  only looks at what's assigned to the slot) a button, with no detail icon and full lines unless it
  has its own, and adds a toggle icon to its end slot (or start), unless the item already holds a control with the style class
  `ion-accordion-toggle-icon`. The icon turns 180° while open.
- Nothing animates until the first interaction. After it, the content's height eases over 300 ms, on `AvonicConfig`'s clock,
  unless `Animated` on the group or `AvonicConfig.Animated` is off.
- Inset: the group has a 16 px margin, each accordion a shadow, and an open one 16 px above and below and rounded corners. The
  header item's line goes while open, on the last accordion, and inset on one before an open one.
- With focus in a header, ArrowDown and ArrowUp move it to the next or previous accordion, wrapping, and Home and End to the first
  and the last. A disabled accordion can't take focus here, so it's passed over.
- The accordion's automation peer has `ExpandCollapse`, for `aria-expanded`.

## Extensions

None.

## Known gaps

- `prefers-reduced-motion` isn't read: Avalonia has no equivalent.
- `IsEnabled` reaches the controls in a disabled accordion, so one in the content that dims itself when disabled dims twice. The
  header's item is kept at its opacity.
- `aria-expanded` is on the accordion's peer, not the header item's; `role="region"` on the content isn't mapped. Proposed: the
  item's peer reports `ExpandCollapse` when it's an accordion's header.
- The host's transitions and the icon's turn run on real time, as Avalonia's transitions do.
- From a key, only the `Click` of an `AvonicItem`, `AvonicButton`, `AvonicChip`, `AvonicCard` or stock `Button` in the header
  toggles it. A pointer press on anything there does.
- Not tried in a real window, and not tried on a device.
