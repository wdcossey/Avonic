# ion-accordion and ion-accordion-group — Plan

One family: `ion-accordion-group` holds the value, and each `ion-accordion` works out from it whether it's open, and animates
its content open and shut. Read from Ionic 8.8.1 (`accordion.js`, `accordion-group.js`, `accordion.md.css`,
`accordion-group.md.css`, and the accordion rules in `css/core.css`), and measured in Chromium, md, at 390 px wide, with the
page in [COMPARISON.md](COMPARISON.md) and throwaway Playwright scripts against it.

## Fidelity checklist

### ion-accordion — `node tools/ionic-api.js ion-accordion`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `disabled` | `IsEnabled` | Mapping rule. A group's `disabled` reaches it through `IsEnabled`'s inheritance |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `readonly` | `Readonly` (`bool`) | No toggle, and `pointer-events: none` on the header and the content. A group's `readonly` counts for its own children |
| [x] | `toggleIcon` | `ToggleIcon` (`string`, `"chevron-down"`) | A name or a source, as `AvonicIcon.Icon` takes. Read once, when the accordion first loads, as Ionic does |
| [x] | `toggleIconSlot` | `ToggleIconSlot` (`AccordionToggleIconSlot { Start, End }`, `End`) | Read once, as above |
| [x] | `value` | `Value` (`string`, `ion-accordion-N`) | Each accordion counts up from 0 in the process, as Ionic's module counter does |
| [x] | slot `header` | `HeaderContent` (+ `HeaderContentTemplate`) | |
| [x] | slot `content` | `ContentContent` (+ `ContentContentTemplate`) | Mapping rule: named slot `foo` → `FooContent` |
| [x] | part `header` | `PART_Header` | The header slot's `ContentPresenter` |
| [x] | part `content` | `PART_Content` | An `AccordionContentClip`, the clipping box whose height animates; `PART_ContentWrapper` inside it is `#content-wrapper` |
| [x] | part `expanded` | pseudoclass `:expanded` | A part name that the other two parts carry while open isn't an element of its own: `::part(header expanded)` → `:expanded /template/ ContentPresenter#PART_Header` |

No events, methods or CSS variables.

### ion-accordion-group — `node tools/ionic-api.js ion-accordion-group`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `animated` | `Animated` (`bool`, `true`) | |
| [x] | `disabled` | `IsEnabled` | Mapping rule |
| [x] | `expand` | `Expand` (`AccordionGroupExpand { Compact, Inset }`, `Compact`) | Pseudoclasses `:expand-compact`, `:expand-inset` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `multiple` | `Multiple` (`bool`) | |
| [x] | `readonly` | `Readonly` (`bool`) | |
| [x] | `value` | `Value` (`object?`) | A `string`, or an `IEnumerable<string>` with `Multiple`; null is none. The union has no single CLR type |
| [x] | `ionChange` | `IonChange` / `IonChangeEvent`, `AccordionGroupChangeEventArgs.Value` | Only from a tap or a key, never from setting `Value` |

No methods, slots, parts or CSS variables. Its children have no slot in `core.json`, so they're **`Items`**: the group is an
`ItemsControl`, as `AvonicList` is. An accordion is its own container, so it's the panel's child.

`ionValueChange` is `@internal`: it's an internal event here, and `requestAccordionToggle` and `getAccordions` are internal methods.

## Behaviours

| # | Behaviour | Source |
|---|---|---|
| B1 | The group's `value` opens the accordion whose `value` equals it, and only that one; with `multiple`, each one the list holds | `updateState` |
| B2 | A tap on a closed accordion's header sets the group's value to its own, raises `ionChange` with it, and closes the one that was open | `toggleExpanded`, `requestAccordionToggle`, `setValue` |
| B3 | A tap on an open one sets the value to `undefined` (null), and raises `ionChange` | same |
| B4 | With `multiple`, a tap adds the accordion's value to the list, or takes it out, as a new list | same |
| B5 | Setting `value` raises no `ionChange`, and the accordions follow it | `valueChanged` emits only `ionValueChange` |
| B6 | An array with `multiple` off logs a warning | `valueChanged` |
| B7 | `readonly` on the accordion, or on its group, stops a tap, and takes pointer input off the header and the content | `toggleExpanded`, `requestAccordionToggle`, `.accordion-readonly` |
| B8 | `disabled` on the accordion or the group stops a tap, and the header and the content are at opacity 0.4 | same, `.accordion-disabled` |
| B9 | An accordion outside a group never opens: a tap only asks a group | `toggleExpanded` |
| B10 | When it first loads, an `ion-item` in the header becomes a button, loses its detail icon, and gets `lines="full"` unless it has lines of its own | `setItemDefaults` |
| B11 | When it first loads, a toggle icon is added to that item's `end` slot (`toggleIconSlot`), with class `ion-accordion-toggle-icon`, unless the item already has an element with that class | `slotToggleIcon` |
| B12 | The toggle icon turns 180° while expanding or expanded, eased over 300 ms once animated | `core.css` |
| B13 | Any click in the header toggles, once, including Enter or Space on the item's button | `#header`'s `onClick` |
| B14 | No animation until the first interaction; after a tap, the content's height eases from 0 to its own over 300 ms, `cubic-bezier(0.25, 0.8, 0.5, 1)`, and back to 0 to close, two frames after the request | `shouldAnimate`, `expandAccordion`, `collapseAccordion` |
| B15 | `animated` off on the group, or Ionic's global `animated` off, means no animation | `shouldAnimate` |
| B16 | A change of the group's value that doesn't come from a tap animates too, except the first defined value an accordion hears of | `accordionGroupUpdateHandler` |
| B17 | A closed accordion knows whether the one after it (`accordion-previous`) or before it (`accordion-next`) is open, for the inset corners | `updateState` |
| B18 | Inset: the group has a 16 px margin; each accordion a shadow; an open one a 16 px margin above and below (not above the first) and 6 px corners; the first's top corners and the last's bottom ones 8 px; a closed one's corners beside an open one 6 px | `accordion-group.md.css`, `core.css` |
| B19 | The header item's border goes while the accordion is open, on the last accordion in a group, and, inset, on one before an open one | `accordion.md.css`, `core.css` |
| B20 | With focus in a header, ArrowDown and ArrowUp move it to the next or the previous accordion in the group, wrapping, and Home and End to the first and the last | `onKeydown` |
| B21 | Changing an accordion's `value` works its state out again | `valueChanged` watcher |
| B22 | The accordion says whether it's expanded, as `aria-expanded` on the header's button does | `setAria` |

## Design

- **The clock.** The expand and collapse run on `AvonicConfig.RequestFrame` and `AvonicConfig.Now`, so tests assert the
  timeline. Expand: a frame, then `accordion-expanding` (the content shows at height 0); a frame, then the height eases to the
  wrapper's own. Collapse: a frame to read the height, a frame, then `accordion-collapsing` and the ease to 0. The first frame
  at or after the 300 ms end finishes it, as `transitionend` comes then. With nothing to ease, Ionic waits for
  `transitionEndAsync`'s 2 s timeout, and so does this.
- **The content's box** is `AccordionContentClip`, a `Decorator` that measures its child with no limit on its height and clips
  it, as `#content` with `overflow: hidden` and a `max-height` does: the child keeps its full height while the box shrinks.
- **Readonly** is behaviour: the control takes hit testing off `PART_Header` and `PART_Content`, at style priority.
- **The header's click.** `NativeButton`, with no keys, on `PART_Header`, for a press on anything there; and the `Click` of an
  `AvonicItem`, `AvonicButton`, `AvonicChip`, `AvonicCard` or `Button` inside, for a key. A press that has already clicked
  something inside doesn't toggle twice.
- **The toggle icon** goes in the item's `EndContent` (or `StartContent`) if it's empty; at the end of a `Panel` there; and
  beside a single control there, in a horizontal `StackPanel`, as Ionic's `appendChild` puts it after what's there.
- **Theme.** `Themes/Controls/AvonicAccordion.axaml` and `AvonicAccordionGroup.axaml`: the background, the inset shadow,
  margins and corners, opacity when disabled, and the host's `transition: all` as transitions of the margin and the corners.
  `Styles/Accordion.axaml`: the header item's border and the toggle icon's turn, which reach into slotted content.
  The inset corners differ at the top and the bottom, so the theme sets `AccordionCorners.Top` and `.Bottom`, and the template
  makes the `CornerRadius` from them.

## Deliberate differences (expected)

- `prefers-reduced-motion` has no equivalent in Avalonia, so it isn't read.
- An accordion inside a disabled one is disabled too, through `IsEnabled`; in Ionic it only looks it, under the 0.4 opacity.
- Group `readonly` set back to false doesn't clear an accordion's own `readonly`, as Ionic's assignment would.
- Arrow keys that move focus are handled, so that Avalonia's own directional navigation doesn't move it again.

## Dependencies

`AvonicItem`, `AvonicIcon`, `AvonicLabel`: all done.
