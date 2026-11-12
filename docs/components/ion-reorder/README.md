# ion-reorder, ion-reorder-group

Status: done · [#34](https://github.com/wdcossey/Avonic/issues/34)

`AvonicReorderGroup` and `AvonicReorder`, Ionic's `ion-reorder-group` and `ion-reorder`. The group's children can be dragged up and
down by a handle in them; the order changes when the app calls `Complete()`, usually from `IonReorderEnd`. A group is disabled by
default, which hides its handles. Plan: [PLAN.md](PLAN.md); comparison: [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicContent>
    <c:AvonicList>
        <c:AvonicReorderGroup Disabled="False" IonReorderEnd="OnReorderEnd">
            <c:AvonicItem>
                <c:AvonicLabel Content="Item 1" />
                <c:AvonicItem.EndContent><c:AvonicReorder /></c:AvonicItem.EndContent>
            </c:AvonicItem>
            <!-- A handle around an item drags it from anywhere -->
            <c:AvonicReorder>
                <c:AvonicItem><c:AvonicLabel Content="Item 2" /></c:AvonicItem>
            </c:AvonicReorder>
        </c:AvonicReorderGroup>
    </c:AvonicList>
</c:AvonicContent>
```

```csharp
private void OnReorderEnd(object? sender, ReorderEndEventArgs e)
{
    e.Complete();                    // the item stays where it was dropped; Complete(false) puts it back
}

// Bound to a collection: reorder it, and the group follows
private void OnReorderEndBound(object? sender, ReorderEndEventArgs e) => e.Complete(ViewModel.Items);
```

## Checklist

### ion-reorder

- [x] part `icon` → `PART_Icon`, `reorder-two-sharp`
- [x] default slot → `Content`, which replaces the icon

### ion-reorder-group

- [x] `disabled` → `Disabled`, default `true`. Not `IsEnabled`, which would disable the items too
- [x] `ionItemReorder` (deprecated in Ionic) → `IonItemReorder`, `ItemReorderEventArgs`
- [x] `ionReorderStart` → `IonReorderStart`
- [x] `ionReorderMove` → `IonReorderMove`, `ReorderMoveEventArgs` (`From`, `To`)
- [x] `ionReorderEnd` → `IonReorderEnd`, `ReorderEndEventArgs` (`From`, `To`, `Complete(…)`)
- [x] `complete(listOrReorder?)` → `Complete()`, `Complete(bool)`, `Complete<T>(IList<T>)`, synchronous

## Behaviour

1. A handle in an item's start or end slot shows only inside an enabled group, at any depth. One that wraps its item always shows.
2. The md icon is 31 px at 0.3 opacity; content replaces it.
3. A press on a handle is handled, so the item it's in, or wraps, doesn't click, unless its group is disabled; so is Enter or Space
   on a button inside a handle that wraps it, not a text box's. With no group it's
   handled too, as Ionic's `onClick` does.
4. A handle in an enabled group has the hand cursor.
5. The drag starts at the press on a handle (gesture `reorder`, priority 110, threshold 0, vertical), never elsewhere, and never while
   a reorder waits for `Complete()`.
6. The item follows the finger, held inside the group, and the children it passes move by its height to make room, easing over
   300 ms (`ease`). `IonReorderMove` comes with each move, `From` being the last `To`.
7. Within 60 px of the closest `AvonicContent`'s top or bottom, each move scrolls it 10 px. A finger held still doesn't.
8. On release: `IonItemReorder` if it moved, then `IonReorderEnd` always. Dropped where it began, it completes itself.
9. `Complete()` moves the item in `Items` and clears every transform in the next frame, so nothing eases. `Complete(false)` puts it
   back. `Complete(list)` moves the item in the list (an `ObservableCollection<T>` with one `Move`) and returns a copy, and nothing
   moves in the group itself. Before the end, or a second time, it does nothing.
10. Until it's completed the item stays lifted, and no new drag starts.
11. The lifted child is at `ZIndex` 100 and has the pseudoclass `:reorder-selected`, with the group's `PART_Lift` shadow around it,
    whatever its type; an item, or a handle that wraps one, is at 0.8 opacity. The group doesn't clip it. The group has Ionic's host classes as `:reorder-enabled` and
    `:reorder-list-active` (from the press until it's completed).
12. Leaving the tree, or `Disabled` set, mid-drag ends the drag with its events.

With `ItemsSource` set, `Complete()` leaves the source alone, as it's the app's: pass it to `Complete(list)`, or move it yourself.
A virtualising panel isn't supported, as the drag reads every child.

## Resource keys

| Key | Value | Ionic |
|---|---|---|
| `Avonic.ReorderGroup.SelectedBoxShadow` | `0 0 15.6 0 #66000000` | `.reorder-selected { box-shadow: 0 0 10px rgba(0,0,0,.4) }`: the same Gaussian in Avalonia's terms |

Neither has CSS variables. The shadow is on `PART_Lift` in `Controls/AvonicReorderGroup.axaml`, at 0.8, as CSS fades it with the
child. The handle's 31 px and 0.3 are literals in `Controls/AvonicReorder.axaml`; the lifted item's 0.8 is in
`Styles/ReorderGroup.axaml`.

## Extensions

None. `:reorder-selected`, `:reorder-enabled` and `:reorder-list-active` are Ionic's classes of those names, for an app's own rules.

## Known gaps

- **No grab or grabbing cursor** in Avalonia: the hand, and no change while dragging.
- **The lifted opacity is only for `AvonicItem` and a wrapping handle**; another type of child has the shadow and the z-index, and
  needs its own `:reorder-selected` style for the 0.8.
- **A press on a handle is handled**, as only that stops the click here, where Ionic stops only the click: a text box inside a
  handle with no group, or one waiting for `Complete()`, isn't focused by a tap. In an enabled group Ionic's drag prevents that too.
- **A slot is an item's start or end**: a handle in another host's named slot isn't hidden, as Ionic's `[slot]` would be.
- **The lifted item's opacity fades each part**, not the item as one layer. Over a white item it's the same.
- **No haptics.**
- **An `AvonicList` around the group clips** an item lifted past the list's edge, where `ion-list` doesn't.
- Tried in a real window with touch and the mouse by `tools/real-window`; not on a device.
