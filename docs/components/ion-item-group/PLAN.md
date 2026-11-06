# ion-item-group — Plan

The smallest control in Ionic: a `display: block` element with `role="group"`, no props, and no CSS of its own. What it does is
what it's a parent of. A sticky divider stays inside it, and its class `item` makes a label inside it act as if it's in
an item. Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide.

## Fidelity checklist

`node tools/ionic-api.js ion-item-group`: no props, events, methods, slots, parts or CSS variables.

### Internal

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `role="group"` | peer, `AutomationControlType.Group` | |
| [x] | class `item` | `Styles/ItemGroup.axaml` | The one thing that reads it is `label.md.css`'s `:host-context(.item)` |
| [x] | `display: block` | `ItemsControl` with a `StackPanel` | |

## Behaviours

Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | It stacks what's in it, with nothing of its own, and has no height when empty, and it doesn't clip. It works from a template, on the stub | `display: block`; measured `fg`, `g-empty` |
| 2 | It reads as a group | `role="group"` |
| 3 | A label in it, even in a header in it, acts as in an item: no margin, it inherits, reads `Avonic.Label.Color`, uses the font token, and `ion-text-nowrap` gives an ellipsis. Its `Color` wins | class `item`; measured `gl`, `gl2`, `gnw`, `ghl` |
| 4 | Each button item is its own tab stop, and arrow keys don't move focus | measured `t2` |

A list's lines reaching through a group, and the last item in a group, are tested with `ion-list`. A sticky divider stopping at
the end of its group is tested with `ion-item-divider`.

## Design

**An `ItemsControl`,** for the same reasons as [`AvonicList`](../ion-list/PLAN.md#design), including taking the arrow keys off.
A group can hold many items, and a data-driven section is what an `ItemTemplate` is for.

**There's no theme file.** There's nothing to draw, and `ion-item-group` has no `md.css` rule, so the stub in `Avonic.Components` is
its only template. `PROCESS.md` allows that. Its `Styles` file is only for the labels inside it.

## Deliberate differences

None of its own. A label's `Position` works here since #72: measured `gst`, `gfx`.

The default text size outside a host is an open project question. Measured `gl`: a label directly in a group is 16, from the
page. Avonic's is Avalonia's default, as it is for `ion-label` on its own.

## Dependencies

`ion-label`, in #57, which this branch is built on.
