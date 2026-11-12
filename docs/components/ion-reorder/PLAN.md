# ion-reorder, ion-reorder-group — Plan

`ion-reorder` is a drag handle; `ion-reorder-group` is the container whose direct children are dragged up and down by it.
Avonic: `AvonicReorder` and `AvonicReorderGroup`. Read from `reorder.js`, `reorder.md.css`, `reorder-group.js` and
`reorder-group.css` (Ionic 8.8.1), and measured in Chromium: [`ref/measurements.json`](ref/measurements.json).

## Fidelity checklist

### ion-reorder

| ✓ | Ionic | Avonic | Notes |
|---|-------|--------|-------|
| [x] | part `icon` | `PART_Icon` | an `AvonicIcon`, `reorder-two-sharp` (md), shown while the default slot is empty |
| [x] | default slot (not in `core.json`) | `Content` | `<slot>` with the icon as its fallback: custom content replaces the icon |

No props, events, methods or CSS variables.

### ion-reorder-group

| ✓ | Ionic | Avonic | Notes |
|---|-------|--------|-------|
| [x] | `disabled` (default `true`) | `Disabled`, default `true` | **Not `IsEnabled`**: Avalonia's disables every descendant, and Ionic's only hides the handles and stops the gesture. The items stay clickable. See "Decided" |
| [x] | `ionItemReorder` (deprecated) | `IonItemReorder`, `ItemReorderEventArgs` | only when `from != to`, before `ionReorderEnd` |
| [x] | `ionReorderStart` | `IonReorderStart`, `RoutedEventArgs` | |
| [x] | `ionReorderMove` | `IonReorderMove`, `ReorderMoveEventArgs` | `from` is the last `to`, not where the drag began |
| [x] | `ionReorderEnd` | `IonReorderEnd`, `ReorderEndEventArgs` | `From`, `To`, `Complete(...)` |
| [x] | `complete(listOrReorder?)` | `Complete()`, `Complete(bool)`, `Complete<T>(IList<T>)` | synchronous, as Ionic's only wraps a value in a promise |
| — | children, with no slot in `core.json` | `Items` | an `ItemsControl` |

## What Ionic does (the behaviours to test)

**ion-reorder**

1. `:host([slot]) { display: none }`: a handle in a slot of an item is hidden, unless an `ion-reorder-group` around it at any depth
   is enabled (`.reorder-enabled ion-reorder { display: block }`). A handle that isn't slotted (wrapping an item) always shows.
2. The md icon is `reorder-two-sharp`, 31 px (`1.9375rem`), at opacity 0.3, in the slot's fallback. Measured: 31 × 31, with the
   item's 16 px slot margin before it, centred in the inner row.
3. A click on it, in the capture phase: `stopImmediatePropagation` unless it's in a disabled group, so the item it's in, or the item
   it wraps, never clicks. A disabled group lets the click through.
4. `cursor: grab` in an enabled group.

**ion-reorder-group**

5. `disabled` turns the gesture off, and on when false. Default `true`.
6. A gesture: name `reorder`, priority 110, threshold 0 (captures at the press), direction y. `canStart` only for a press inside an
   `ion-reorder` (`closest`), whose ancestor that is a direct child of the group is the item; never while a drag is selected or not
   yet completed.
7. `onStart`: caches the cumulative heights of the children, the group's box and the closest `ion-content`'s scroll box; marks the
   item `reorder-selected`; raises `ionReorderStart`.
8. `onMove`: auto-scrolls, then works out the index under the finger, clamped to the group's box; when it changes, translates every
   child between the old and the new place by the item's height, toward the gap; translates the item by the finger's travel plus the
   scroll; raises `ionReorderMove` `{ from: lastToIndex, to }`.
9. The siblings ease: `transition: transform 300ms` (`ease`) while the list is active. The selected item doesn't
   (`transition: none !important`). Measured: −1.8 px at 16 ms, −48 at 300.
10. Auto-scroll: within 60 px of the scroll element's top or bottom, `scrollBy(0, ∓10)` on each move. Only on a move: a finger held
    still doesn't scroll. Measured: 10 px per move.
11. `onEnd`: `ionItemReorder` if `from != to`, then always `ionReorderEnd` `{ from, to, complete }`. If `from == to` it completes by
    itself first.
12. `complete()` / `complete(true)`: the item moves in the DOM to `to`. `complete(false)`: it goes back. `complete(array)`: the DOM
    doesn't move; the array is reordered in place, and a copy is returned. The class and the state go at once; the move and the
    transforms are cleared in the next frame, together, so nothing eases (measured: one frame lifted-but-unshadowed, then 0).
    Nothing happens unless a drag has ended: `complete` before then, or twice, returns its argument untouched.
13. Not completed: the group stays as dropped, the item stays lifted and no new drag can start (`canStart`: state not idle).
    Measured: a `complete()` 500 ms later still moves it.
14. Lifted look: `.reorder-selected { box-shadow: 0 0 10px rgba(0,0,0,.4); opacity: .8; z-index: 100 }`.
15. `disconnectedCallback` ends a drag (`onEnd`, with its events) and destroys the gesture. Disabling mid-drag ends it too (the
    gesture's `enable(false)`).

**Dead or not applicable**

- `::slotted(ion-icon) { font-size: dynamic-font(16px) }` is an uncompiled SCSS function in the shipped CSS, so Chromium drops it:
  a custom icon takes the font size it inherits (measured 16 px in an item).
- `.reorder-visible ion-reorder .reorder-icon`: nothing sets `reorder-visible`, and a light-DOM rule can't reach a shadow part.
- `item.md.css`'s `::slotted(ion-reorder[slot]) { margin-top: 0; margin-bottom: 0 }`: no md rule gives a slotted control a vertical
  margin that it could beat (the multi-line and stacked rules are more specific and win).
- Haptics (`hapticSelectionStart`, …): Capacitor's, native only. Avonic has none.
- `user-select: none`, `touch-action: none`, `pointer-events: all`, `will-change`, `display: block` on the children: browser
  concerns. A captured gesture already keeps the scroller off the pointer.

## Design

- `AvonicReorder : ContentControl`. `IsVisible` and `Cursor` at style priority through `StyleValue<T>`, bound in the constructor:
  hidden when its parent is an item's `PART_Start` or `PART_End` presenter (`[slot]`, as `AvonicCheckbox`'s `:slot-start`) and no
  enabled group is around it; `Hand` (Avalonia has no grab cursor) in an enabled group. It listens to each group around it.
  The click: a tunnel `PointerPressed` handler that handles the press unless the nearest group is disabled. `NativeButton`
  (the item's click) and a stock `Button` both ignore a handled press, and Ionic's capture listener stops a wrapped item's click
  the same way. The gesture and `Activatable` listen with `handledEventsToo`, so the drag and the ripple are untouched.
- `AvonicReorderGroup : ItemsControl`, `StackPanel`. "Children" are `ItemsPanelRoot.Children`; the element Ionic would mark is
  the child, or a template's `ContentPresenter`'s child (as `AvonicList.ContainerOf`), as an `ItemsControl`'s wrapper isn't in the DOM.
- Transforms: one `TranslateTransform` per child, set as `RenderTransform` at `Animation` priority (Ionic's inline style beats the
  app's sheet; this gives the app's own value back when it's cleared), eased on `AvonicConfig`'s clock with `cubic-bezier(.25,.1,.25,1)`
  and CSS's reversing rule, as `AvonicProgressBar`'s transition.
- The lift: the pseudoclass `:reorder-selected` on the element, and `ZIndex` 100 at style priority on the child in the panel (it has
  to be over its siblings to be seen, whatever its type). The shadow and opacity are the theme's: `Styles/ReorderGroup.axaml` for
  `AvonicItem`, `AvonicReorder`'s own theme for a wrapping handle.
- The scroll element: the closest `AvonicContent`'s `GetScrollElement()`, or a control with the class `ion-content-scroll-host`, as
  `AvonicInfiniteScroll` finds it, at attach.
- `Complete` with `ItemsSource` set: the source isn't Avonic's to change, so `Complete()` only clears, as Ionic's array form does in a
  framework; `Complete(list)` reorders the list given, with `Move` on an `ObservableCollection<T>`.

## Tests

One per behaviour above, by real touch on the headless window on `TestClock` (`GestureTests.Finger`), in an `AvonicContent` where
it matters, so the scroller and capture paths run.

## Dependencies

`ion-item` (#6), `ion-list` (#9), gesture (#4), `ion-icon` (#3), `ion-content`: all merged.
