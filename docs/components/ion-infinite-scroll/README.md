# ion-infinite-scroll

Status: done · [#36](https://github.com/wdcossey/Avonic/issues/36)

`AvonicInfiniteScroll` and `AvonicInfiniteScrollContent`, Ionic's `ion-infinite-scroll` and `ion-infinite-scroll-content`. The
scroll raises `IonInfinite` when the `AvonicContent` around it scrolls within its threshold of the end; the app loads more, then
calls `Complete()`. The content shows a spinner, and a line of text if it has one, while that happens. Plan: [PLAN.md](PLAN.md);
comparison: [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicContent>
    <StackPanel>
        <ItemsControl ItemsSource="{Binding Items}" />
        <c:AvonicInfiniteScroll IonInfinite="OnInfinite">
            <c:AvonicInfiniteScrollContent LoadingText="Loading more data…" />
        </c:AvonicInfiniteScroll>
    </StackPanel>
</c:AvonicContent>
```

```csharp
private async void OnInfinite(object? sender, RoutedEventArgs e)
{
    await LoadMoreAsync();                         // adds to Items
    var infinite = (AvonicInfiniteScroll)sender!;
    infinite.Complete();
    infinite.IsEnabled = Items.Count < Total;      // no more: hidden, and it stops listening
}
```

## Checklist

### ion-infinite-scroll

| ✓ | Ionic | Avonic | Default | |
|---|---|---|---|---|
| [x] | `disabled` | `IsEnabled` (inverted) | `true` | False: hidden, as `display: none`, at style priority so an app's `IsVisible` wins; not listening; not loading. Its own value: a disabled ancestor doesn't count, as in Ionic |
| [x] | `position` | `Position` | `Bottom` | `InfiniteScrollPosition`: `Bottom`, `Top` |
| [x] | `threshold` | `Threshold` | `"15%"` | A `%` anywhere: that share of the scroller's height. Otherwise pixels. Read as JavaScript's `parseFloat`, so `"100px"` and `"100"` are 100, and a string that doesn't start with a number never fires |
| [x] | `ionInfinite` | `IonInfinite` | | Routed, bubbling |
| [x] | `complete()` | `Complete()` | | Synchronous, as Ionic's resolves at once |
| [ ] | `mode` | none | | Not exposed (ADR 0001) |

No slots, parts or CSS variables; its child is `Content`.

### ion-infinite-scroll-content

| ✓ | Ionic | Avonic | Default | |
|---|---|---|---|---|
| [x] | `loadingSpinner` | `LoadingSpinner` | unset | `SpinnerName?`. Unset: `AvonicConfig.InfiniteLoadingSpinner`, else `AvonicConfig.Spinner`, else `Crescent`, once, on the first attach. `null`: no spinner |
| [x] | `loadingText` | `LoadingText` | `null` | Plain text, shown as it's written. `""` is a line of no height, as in Ionic |
| [ ] | `mode` | none | | Not exposed (ADR 0001) |

No events, methods, slots, parts or CSS variables.

## Behaviour

- **The scroller** is the nearest `AvonicContent`'s, or that of the nearest element with the class `ion-content-scroll-host`,
  among the scroll and its visual ancestors, found when it's attached: the element itself if it's a `ScrollViewer`, or else the
  `ScrollViewer` around the scroll inside it, as in a `ListBox`'s template. Outside both it logs `AvonicInfiniteScroll must be used inside
  AvonicContent.` at `Error` in `LogArea.Control`, and does nothing; with a host that has no `ScrollViewer`, `AvonicInfiniteScroll
  found no ScrollViewer in {Host}.`
- **A check** runs at the frame after the scroller's `Offset` changes, once however many times it changed, as a browser dispatches
  `scroll`, if it's listening then: disabled and enabled again, or moved and back, before the frame, it still checks. A change of layout alone isn't a scroll, so a list too short to scroll never fires. It reads the layout that's due
  first, as a browser's reads force one.
- **It fires** when the distance is below 0, strictly: at the bottom `scrollHeight - its height - scrollTop - threshold - the
  scroller's height`; at the top `scrollTop - its height - threshold`. Hidden, its height is 0, and it doesn't check. Once fired, it
  waits for `Complete()`.
- **At the top** it scrolls to the end at the first frame after it's attached, and `Complete()` waits four frames, Ionic's
  `raf`, `readTask`, `raf`, `writeTask`, then scrolls by the height added above, so the rows shown stay put. It can't fire in the
  meantime. Call `Complete()` after adding the rows, before a layout: see Known gaps.
- **Disabled while loading, then enabled again, it never fires again**, as in Ionic: disabling clears the loading state, so
  `Complete()` has nothing to complete, and the state that blocks a second fire stays. Call `Complete()` before disabling it.
- **The content** shows its loading part only while the nearest `AvonicInfiniteScroll` around it is loading
  (`:infinite-scroll-loading`, on both). The spinner keeps the inherited colour, as md's rule for it can't reach its shadow root.

## Resource keys

None of its own. The theme, `Controls/AvonicInfiniteScrollContent.axaml`, has `infinite-scroll-content.md.css`: a minimum height of
84, the loading part centred with 32 below it, the spinner's line 32 high, the text's margin `32,4,32,0`, centred and wrapping, in
the global `Avonic.TextColorStep400Brush` (`--ion-text-color-step-400`, `#666666`). The infinite scroll has no look, so no theme of
its own.

**Set `FontSize` where you use it.** The text inherits it, as Ionic's does, and Avalonia's default isn't a browser's 16 px.

## Extensions

None.

## Known gaps

- **`Complete()` reads the last layout, not a forced one**, so it measures the rows from before the new ones are laid out, which is
  what Ionic's comment and framework examples have. An app that waits for a layout between adding rows and calling `Complete()`
  at the top sees them jump, as a plain-DOM page does in Chromium.
- **The functional stub has no minimum height**, as 84 is md's (and ios's) skin. With the stub alone, and nothing else giving the
  content a height, the infinite scroll is 0 high, and, as in Ionic, a scroll of no height never fires.
- **The spinner's line is 32 high** at any font size: 28 and the descent of 16 px Roboto. Chromium's line follows the inherited
  font, so at another size the gap under the spinner is a pixel or two different.
- **The content follows the nearest infinite scroll around it**, where Ionic's descendant rule follows any loading ancestor, which
  only differs for one infinite scroll inside another's content.
- **A scroll host with `ion-content-scroll-host`** has to be a `ScrollViewer` or have one around the infinite scroll, as only a
  `ScrollViewer` scrolls here. If it has neither, the error is logged and nothing fires.
- **`loadingText` has no HTML**, as with Ionic's default.
- Not tried in a real window, nor on a device: a momentum flick hasn't been seen.
