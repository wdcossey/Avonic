# ion-list — Plan

The family's container. It draws very little: nearly all of `list.md.css` reaches into the items around it, so this PR is
mostly how a container's rules reach many children, including ones a virtualising panel creates later. Its siblings have
their own plans: [`ion-list-header`](../ion-list-header/PLAN.md), [`ion-item-divider`](../ion-item-divider/PLAN.md) and
[`ion-item-group`](../ion-item-group/PLAN.md). Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide.

## Fidelity checklist

`node tools/ionic-api.js ion-list`.

### Props (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `inset` | `Inset` (`bool`) | `:inset` |
| [x] | `lines` | `Lines` (`ListLines?`) | `Full`, `Inset`, `None`; null leaves it to each item. `:lines-*` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Methods (1)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [ ] | `closeSlidingItems()` | — | Omitted: `ion-item-sliding` (#31) doesn't exist, so it could only ever return `false`, and a declared member has to work. #31 adds it, with its test |

No events, slots, parts or CSS variables in `core.json`. The children of an `ion-list` aren't slotted, because it has no
shadow root, so they're `Items`.

### Internal, from other controls

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `role="list"` on the host | peer, `AutomationControlType.List` | |
| [x] | `role="listitem"` on an `ion-item` in a list | `AutomationProperties.ControlTypeOverride`, in `Styles/List.axaml` | Not on a button item: see the differences |
| [ ] | `in-list` on `ion-item` | — | No stylesheet reads it for an item. `fab-button-in-list` is its own class |

## Behaviours

Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | Items stack at full width inside 8 of padding, on the item background: 112 high for two | `.list-md`; measured `l-plain` |
| 2 | `Lines="None"` removes both of a default item's borders | `.list-md-lines-none`; measured `n1` |
| 3 | `Lines="Full"` draws the outer border, which makes the item 49 high | measured `f1` |
| 4 | `Lines="Inset"` draws the inner border, and the item stays 48 | measured `i1` |
| 5 | An item's own `Lines` beats the list's, either way | `.item-lines-default`; measured `n2`, `n3`, `f2`, `f3`, `i2`, `i3` |
| 6 | A list's lines beat an interactive item's borders, because outer styles beat `:host` | measured with `ion-input`: `nif` |
| 7 | Lines reach items at any depth: in a group, in a panel | descendant selector; measured `fg1`, `indiv` |
| 8 | Between nested lists the order in the CSS decides, not the nesting: full beats none whichever is inside | measured `nest1`, `nest2` |
| 9 | `Lines` changes live, and null restores the default | |
| 10 | Lines also reach items an `ItemTemplate` makes | Avalonia: a `ContentPresenter` is in between |
| 11 | `Inset`: 16 of margin, radius 2, clips its items. Keeps its 8 of padding. A plain list has none of that, and still clips (`contain: content`) | `.list-inset`; measured `a1` |
| 12 | An inset list's last item has no border, whatever its lines, per parent, live, and from a template | `:last-of-type`, `:only-child`; measured `a1z`, `a2x`, `a4y`, `a6z`, `a7y`, `a8y`, `a10x` |
| 13 | A plain list's last item keeps its border | measured `p2` |
| 14 | An inset list straight after an inset list has no top margin, and it follows a change of either list or of what's between them | `.list-inset + ion-list.list-inset`; measured `adj1` to `adj4` |
| 15 | Each button item is its own tab stop, and arrow keys don't move focus | measured: `t1` to `t3`, with arrows staying on `t1` |
| 16 | It reads as a list, with list items at any depth. A button item stays a button | `role`, `hostContext`; measured |
| 17 | It doesn't scroll: what it's inside does | no `overflow`; measured with a scrolling parent |
| 18 | With a `VirtualizingStackPanel` in a `ScrollViewer`, only what's in view is realised, and lines and the last item still hold | Avalonia |

## Design

**`AvonicList` is an `ItemsControl`.** `ion-list` is a `display: block` element with no shadow root. It doesn't scroll,
select, or manage focus: it only gives CSS something to hang from. Three bases could hold that:

| Base | For | Against |
|---|---|---|
| A `Panel` subclass | The closest to a block: it only lays out | No `ItemsSource`, so a long list can't virtualise. Avalonia virtualises through an item generator, which only an `ItemsControl` has |
| `ListBox` | Virtualises by default | Selection and a roving focus, which Ionic doesn't have. `ListBoxItem` containers between the list and every item |
| `ItemsControl` | `Items` as XAML content, `ItemsSource` and `ItemTemplate`, and `ItemsPanel`, and a control is its own container, so `<c:AvonicList><c:AvonicItem/></c:AvonicList>` has nothing in between | It moves focus with the arrow keys, which Ionic doesn't, so that comes off here |

An `ItemsControl` moves focus with the arrow keys. Measured in Ionic, Tab visits each button item and the arrows do nothing,
so the list overrides `OnKeyDown`. An `ItemsControl` doesn't change Tab, only a `ListBox`'s theme does: breaking it on purpose showed
an override here did nothing, so there isn't one.

The default panel stays a `StackPanel`, because a list in a phone page usually isn't long, and a list inside a group has
to size to its content. For a long one, an app sets `ItemsPanel`, and puts the list in a `ScrollViewer`, which `ion-content`
(#11) will be. There's no `ScrollViewer` in the template, because `ion-list` doesn't scroll, and a `VirtualizingStackPanel`
reads its viewport from any ancestor. A test holds it to at most 40 of 10,000 items at 400 high.

**The rules are in `Styles/List.axaml`, not in code.** Each is a descendant selector, as in the CSS, that ends in the item's
template: `c|AvonicList:lines-none c|AvonicItem:lines-default /template/ Border#PART_Native`. A selector is checked from its
right end, and most `Border`s fail the name or the `/template/`, so nothing walks up unless it's that part of an item with
default lines. Styles beat a `ControlTheme`, which is how outer CSS beats `:host`, so behaviour 6 needs nothing else. The
selectors are in the CSS's order, because that's what decides behaviour 8.

**An item from a template has a `ContentPresenter` between it and the list.** The presenter's logical parent is the list, so
descendant selectors go straight through. Only the "last child" rule has to name it.

**`:last-of-type` is `:nth-last-child(1)`.** Avalonia has no `of-type` selectors. `:nth-last-child` asks the parent, through
`IChildIndexProvider`, which `Panel` and `ItemsControl` both implement, and the `ItemsControl` answers from the whole item count.
So it knows the last item of 10,000 without realising the rest, it stays live, and it needs no code. The price is
in the differences below. Doing it exactly would mean code that watches every parent inside the list, for the sake of two
results that Ionic's own comment doesn't intend: see "For you to decide" in the PR.

**`.list-inset + ion-list.list-inset` is a pseudoclass.** Avalonia has no sibling combinator, so the control works out
`:after-inset` from its visual parent's `Children`, and the theme's `^:inset:after-inset` is the rest of the rule. Each list
listens to that collection, and tells the next list when its own `Inset` changes. An `ItemTemplate` that makes inset lists has a
presenter around each, so the control looks through those, as a sticky divider does.

**`role="listitem"` is a style.** `AvonicItem` can't be edited here, and `AutomationProperties.ControlTypeOverride` is
all it takes. It means the role depends on the theme: see the differences, and "Proposed for shared files" in the PR.

**`ListLines` isn't `ItemLines`,** though they have the same values. The mapping rule is `<Component><Prop>`, and it's
mechanical.

## Deliberate differences

| Difference | Why |
|---|---|
| **The last item in an inset list is the last child, not the last of its type.** A lone item after a header or divider (measured `a3x`, `a6x`) keeps its border in Ionic and loses it here. Items before a trailing non-item (measured `a5y`) lose it in Ionic and keep it here | See the design. Ionic's CSS comment says it means "the last item in a list", so these look like accidents. Recorded for you to decide |
| A last item in a parent that isn't a `Panel` or an `ItemsControl`, such as a `Border`, keeps its border, where Ionic's `:only-child` removes it (measured `a10x`, in a `div`) | `:nth-last-child` needs `IChildIndexProvider`. A `StackPanel` works |
| A button item in a list is a `Button` to automation, not a `ListItem` | Ionic has a `listitem` host with a `<button>` inside. Avalonia has one peer for both, and being invokable matters more |
| `role="listitem"` goes if an app replaces the theme | It's the one behaviour here in a style, because the item can't be edited in this PR |
| An explicit `Lines` beats a token | The same as `ion-item`'s plan |
| Margins don't collapse | Avalonia has none. Measured: beside a block with a 30 margin, the gap is 30 in CSS, and 46 here. In a parent with no padding, the parent is 144 high with the 16 outside it in CSS, and 176 here with it inside, though what's in the parent lands in the same place. Between inset lists, the `+` rule means they never meet |
| `Items`, not `Content` | `core.json` has no slot here, and a list has many children |

## Not in this PR

| What | Goes with |
|---|---|
| `closeSlidingItems()`, `.list-inset ion-item-sliding:last-of-type ion-item`, `.list-md-lines-* .item-options-md` | `ion-item-sliding` (#31) |
| `ion-card .list-md` | `ion-card` (#13), in its own `Styles` file |
| `hostContext('ion-radio-group')`, which stops an item being a `listitem`, and `ion-list .radio-group-top` | `ion-radio-group` (#22) |
| `select-popover`'s and `select-modal`'s rules for `ion-list` | `ion-select` (#45) |
| `.list-md > .input:last-child::after` | Dead: nothing emits the class `input` |
| `transform: translateZ(0)` on an inset list | No visible effect, only a stacking context |
| `--ion-safe-area-*` | `ion-content` (#11), as for `ion-item` |

## Dependencies

`ion-item`, done, and its `:lines-default`. `AvonicListHeader`, `AvonicItemDivider` and `AvonicItemGroup` come with this PR.
