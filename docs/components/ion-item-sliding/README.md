# ion-item-sliding, ion-item-options, ion-item-option

Status: done · [#31](https://github.com/wdcossey/Avonic/issues/31)

`AvonicItemSliding` is an `AvonicItem` that a finger slides sideways to show the `AvonicItemOptions` behind it, each a row of
`AvonicItemOption` buttons. The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in
[COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicList>
    <c:AvonicItemSliding>
        <c:AvonicItemOptions Side="Start">
            <c:AvonicItemOption Color="success" Content="Archive" Click="OnArchive" />
        </c:AvonicItemOptions>
        <c:AvonicItem>
            <c:AvonicLabel Content="Swipe me" />
        </c:AvonicItem>
        <c:AvonicItemOptions IonSwipe="OnDelete">
            <c:AvonicItemOption Content="Favorite" Click="OnFavorite" />
            <c:AvonicItemOption Color="danger" Expandable="True" Content="Delete" Click="OnDelete" />
        </c:AvonicItemOptions>
    </c:AvonicItemSliding>
</c:AvonicList>
```

A drag left shows the end options, a drag right the start ones (the other way round in RTL). Released past half their width, or
flicked open, the item rests open; otherwise it closes. Dragged 30 px past their width it's a full swipe: an `Expandable` option
grows over the row, and on release `IonSwipe` is raised on those options and the item rests open, for the app to act on (Ionic
leaves deleting the item to the app). Only one sliding item is open at a time: pressing another closes it.

## ion-item-sliding — `AvonicItemSliding`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `disabled` | `Disabled`, default `false` | No sliding, and nothing else: the item stays enabled and clickable. Not `IsEnabled`, which disables the item too |
| [x] | `ionDrag` | `IonDrag` (`ItemSlidingDragEventArgs`: `Amount`, `Ratio`) | On every move and where it rests; not for 0 |
| [x] | `close()` | `Close()` | |
| [x] | `closeOpened()` | `CloseOpened()` → `bool` | Closes whichever sliding item is open |
| [x] | `getOpenAmount()` | `GetOpenAmount()` → `double` | Positive with the right options showing |
| [x] | `getSlidingRatio()` | `GetSlidingRatio()` → `double` | Negative for the left |
| [x] | `open(side)` | `Open(ItemOptionsSide? side = null)` | Opens at the next frame |
| [x] | children | `Items` | The item and the options, in any order |

## ion-item-options — `AvonicItemOptions`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `side` | `Side` (`ItemOptionsSide`: `End` default, `Start`) | `:options-start` / `:options-end` are the physical left and right |
| [x] | `ionSwipe` | `IonSwipe` (`ItemOptionsSwipeEventArgs`: `Side`) | Bubbles |
| [x] | children | `Items` | The options |

In a list with `Lines="Full"` the options have a 1 px line at the bottom, and end options with `Lines="Inset"`, as Ionic's do.

## ion-item-option — `AvonicItemOption`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | |
| [x] | `disabled` | `IsEnabled` | The content at 0.5, no input |
| [x] | `expandable` | `Expandable` | `:expandable` |
| [ ] | `href` | — | Out of scope: router. Use `Click` |
| [ ] | `download`, `rel`, `target` | — | Not applicable: only apply with `href` |
| [ ] | `type` | — | Not applicable: HTML form |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | default slot | `Content`, `ContentTemplate` | Uppercased when it's a string (`TextTransform`) |
| [x] | slots `top`, `bottom`, `start`, `end`, `icon-only` | `TopContent`, `BottomContent`, `StartContent`, `EndContent`, `IconOnlyContent` (+ templates) | |
| [x] | parts `native`, `inner`, `container` | `PART_Native`, `PART_Inner`, `PART_Container` | Also `PART_Host` (the host's background), `PART_Expander`, `PART_Ripple`, and a presenter per slot |
| [x] | `--background` | `Avonic.ItemOption.Background` | Unset → primary |
| [x] | `--color` | `Avonic.ItemOption.Color` | Unset → primary contrast |

## Behaviours

All of [PLAN.md](PLAN.md)'s, each with a test in `AvonicItemSlidingTests`: the drag from the capturing move, one side only when
only one has options, the elastic 0.55 past the options' width, the full swipe 30 px further, the release rules (fast is over
0.3 px/ms; the close zone is under half), `IonSwipe` after it rests open, `IonDrag`, the 500 ms
`cubic-bezier(0.36, 0.66, 0.04, 1)` ease, the 600 ms close in which the options take no input and the gesture is off, the open
item taking no taps, one open at a time, the 15 px edge, the methods, the options shown only on the open side and behind the item,
RTL, `Disabled`, an enclosing `AvonicContent`'s `ScrollY` off during a drag, children found again when they change, a vertical
drag scrolling a scroller, the expandable option's 600 ms `cubic-bezier(0.65, 0.05, 0.36, 1)` growth, and the outermost option padded by
`Avonic.SafeAreaLeft` or `Right` on its side.

## Extensions

| Avonic | What |
|---|---|
| `AvonicItemOption.Click` | The native button's click, on release, Enter and Space, as on `AvonicFabButton`. This is how an app acts on an option |
| `AvonicItemOption.Command`, `CommandParameter` | Run after `Click`, as on `AvonicButton` |
| `AvonicItemOption.TextTransform` | `text-transform`, as on `AvonicButton`; the theme sets `Uppercase` |

## Resource keys

| Key | Default |
|---|---|
| `Avonic.ItemOption.Background` | Unset → `Avonic.Color.PrimaryBrush` |
| `Avonic.ItemOption.Color` | Unset → `Avonic.Color.PrimaryContrastBrush` |

The options' line uses `Avonic.ItemBorderColorBrush`, as the item's does.

## Known gaps

- **An option wider than the row doesn't shrink** (`flex-shrink: 1`); options keep their own width and the row overflows inwards.
  Options are short, so this needs a row narrower than its options to show.
- **The item's `z-index: 2` is `ZIndex` on the item,** which only sorts siblings: an item wrapped in another control inside the
  sliding item draws under the options (`ZIndex` 1). Put the `AvonicItem` straight in `Items`, as Ionic's examples do.
- **One open item per process,** where Ionic's is one per document, as with `GestureController`.
- **An app's margin on slotted content adds to the slot's,** where Ionic's `::slotted` rule replaces it: the 5 px and 10 px are on
  the slot's presenter, which the option's `ControlTheme` can reach without a rule in `Styles/`.
- **Ionic's own quirks are kept,** as the port is line for line: `Open()` of the other side, or within 600 ms of a close, cancels
  the timer that ends the close, so the options take no taps and the gesture stays off until the next close (measured: Chromium
  kept `item-sliding-closing` after `open('start')` on an item open at the end); and in RTL `Open()` with no side opens the
  physical right when only the left has options.
- **In an inset list,** Ionic takes the line off the item in the last sliding item, and keeps it on an item that's last inside its
  sliding item; `AvonicList`'s rule looks at the item's own position, so it does the opposite in both cases. The fix is in the
  list's `Styles/List.axaml`, proposed in the PR.
- **`ion-list`'s `closeSlidingItems()` isn't on `AvonicList` yet,** as that's the list's file: proposed in the PR. Until then,
  `CloseOpened()` on any sliding item does the same.
- Tried in a real window with mouse, touch and keys by `tools/real-window`, which found that no mouse could open one (fixed in the gesture); not on a device.
