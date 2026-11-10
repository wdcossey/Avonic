# ion-infinite-scroll — Plan

`AvonicInfiniteScroll` and `AvonicInfiniteScrollContent`, one family (#36). The scroll watches the nearest `ion-content`'s scroller
and raises `IonInfinite` when it comes within a threshold of the end; the app loads more and calls `Complete()`. The content is
what shows while it loads: a spinner and an optional line of text. Measurements are from real Ionic 8.8.1 in Chromium, md,
390 x 844, in `ion-app` and `ion-content`, by scripts kept out of the repo; their output is in `ref/measurements.json`.

## Fidelity checklist

`node tools/ionic-api.js ion-infinite-scroll` and `node tools/ionic-api.js ion-infinite-scroll-content`.

### ion-infinite-scroll: props

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `disabled` | `IsEnabled` (inverted) | `bool` | `true` | False: hidden, no longer listening, and not loading |
| [x] | `position` | `Position` | `InfiniteScrollPosition` | `Bottom` | `Bottom`, `Top` |
| [x] | `threshold` | `Threshold` | `string` | `"15%"` | Read as `parseFloat` reads it: `%` anywhere is a share of the scroller's height, otherwise pixels |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |

### ion-infinite-scroll: events and methods

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionInfinite` | `IonInfinite` | Routed, bubbling (`bubbles: true`); `RoutedEventArgs` |
| [x] | `complete() => Promise<void>` | `Complete()` | `void`: Ionic's resolves at once, so it's synchronous |

No slots, parts or CSS variables. The light DOM child, the content, is `Content` (a `ContentControl`).

### ion-infinite-scroll-content: props

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `loadingSpinner` | `LoadingSpinner` | `SpinnerName?` | unset | Unset: `AvonicConfig.Spinner`, else `Crescent`, on the first attach. `null`: no spinner |
| [x] | `loadingText` | `LoadingText` | `string?` | `null` | Plain text, as Ionic's default `innerHTMLTemplatesEnabled: false` makes it. See Design |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |

No events, methods, slots, parts or CSS variables. It depends on `ion-spinner`, which exists.

## What Ionic does

`infinite-scroll.js`:

- `connectedCallback`: `el.closest('ion-content, .ion-content-scroll-host')`; none: `console.error('<ion-infinite-scroll> must be
  used inside ion-content.')` and nothing else. Otherwise it awaits the scroll element, parses the threshold, and listens to its
  `scroll` unless disabled. With `position="top"` it then sets `scrollTop = scrollHeight - clientHeight` in a `writeTask`.
- `onScroll`, on every `scroll` event: nothing unless `canStart()` (not disabled, not busy, not loading, has a scroller), or if the
  host's `offsetHeight` is 0. Then `threshold = thrPc ? offsetHeight(scroller) * thrPc : thrPx`, and
  `distance = scrollHeight - infiniteHeight - scrollTop - threshold - height` at the bottom, `scrollTop - infiniteHeight - threshold`
  at the top. `distance < 0` and not `didFire`: `isLoading = true`, `didFire = true`, `ionInfinite`.
- `complete()`: nothing unless loading and connected. `isLoading = false`. Bottom: `didFire = false`. Top: `isBusy = true`,
  `prev = scrollHeight - scrollTop`; then `raf → readTask`: `newTop = scrollHeight - prev`; then `raf → writeTask`:
  `scrollTop = newTop`, `isBusy = false`, `didFire = false`. That keeps what's on screen in place when items are added above.
- `disabledChanged`: disabled clears `isLoading` and `isBusy`, **not `didFire`**, and stops listening; enabled listens again.
- `disconnectedCallback`: stops listening and forgets the scroller.
- `render`: host classes `infinite-scroll-loading` (loading) and `infinite-scroll-enabled` (not disabled).

`infinite-scroll.css`: `ion-infinite-scroll { display: none; width: 100% }`, `.infinite-scroll-enabled { display: block }`.

`infinite-scroll-content.js`: in `componentDidLoad`, an undefined `loadingSpinner` becomes
`config.get('infiniteLoadingSpinner', config.get('spinner', 'crescent'))` in md. It renders `div.infinite-loading`, holding
`div.infinite-loading-spinner > ion-spinner[name]` when `loadingSpinner` is truthy and `div.infinite-loading-text` when
`loadingText !== undefined`. The text is a text node unless `innerHTMLTemplatesEnabled` is on, which is off by default.

`infinite-scroll-content.md.css`: the host is a flex column, `justify-content: center`, `min-height: 84px`, `text-align: center`,
`user-select: none`. `.infinite-loading { margin: 0 0 32px; display: none; width: 100% }`, shown by
`.infinite-scroll-loading ion-infinite-scroll-content > .infinite-loading { display: block }`. `.infinite-loading-text`:
`margin: 4px 32px 0`, `color: var(--ion-color-step-600, var(--ion-text-color-step-400, #666666))`. The md rules that colour the
spinner's `line` and `circle` are **dead**: they're global rules, and the shapes are in `ion-spinner`'s shadow root, so the spinner
keeps the inherited text colour (M8).

## Measured

| | Case | Result |
|---|---|---|
| M1 | 30 rows of 50 px, then the scroll: `scrollHeight` 1584, `offsetHeight` 844, the scroll 84 high, 15% | The boundary is 529.4: `scrollTop` 529 doesn't fire, 530 does. Fired once; scrolling on while loading doesn't fire again |
| M2 | `threshold="100px"` | 556 doesn't fire (distance exactly 0), 557 does: `< 0`, strict |
| M3 | Event order | `scroll` is dispatched at the next frame, before `requestAnimationFrame` callbacks; `ionInfinite` is raised inside it |
| M4 | Two rows and the scroll, nothing to scroll | No `ionInfinite`: only a scroll checks |
| M5 | `position="top"`, 30 rows | Starts at `scrollTop` 740, the end. At 30: fires. `complete()`, then 10 rows of 50 px added above: `scrollTop` 30 for three frames, 530 from the fourth |
| M6 | Disabled while loading, enabled, `complete()` | `complete()` does nothing, as loading was cleared, and `didFire` stays: it never fires again |
| M7 | Disabled | `display: none`, height 0, no `infinite-scroll-enabled` |
| M8 | The content, loading, with and without text | No text: `.infinite-loading` 32 high (the 28 px spinner on a 16 px line), centred in 84, at y 10. Text: the host 87 = 32 + 4 + 19 + 32; text box x 32, width 326; colour `rgb(102, 102, 102)`, 16 px Roboto 400. Two lines: 106. `loading-text=""`: 36, centred, at y 8. Not loading: 84, empty. The spinner, every name, is `rgb(0, 0, 0)` |
| M9 | `loading-text="<b>bold</b> &amp;amp; x"` | Shown as written: `<b>bold</b> &amp; x` |
| M10 | Outside `ion-content` | `console.error`: `<ion-infinite-scroll> must be used inside ion-content.` |
| M11 | The content's default spinner | `loadingSpinner` becomes `"crescent"` |

## Design

`Controls/InfiniteScroll/`: `AvonicInfiniteScroll : ContentControl`, `InfiniteScrollPosition`, `AvonicInfiniteScrollContent :
TemplatedControl`, and a stub `ControlTheme` for each.

- **The scroller**, on attach: the nearest of itself and its visual ancestors that is an `AvonicContent`, whose
  `GetScrollElement()` it takes, or a `ScrollViewer` with the class `ion-content-scroll-host`, which is itself, as Ionic's
  selector has both. None: an error in Avalonia's log, at `LogArea.Control`, with Ionic's message, and nothing else.
- **The `scroll` event**: a change of the scroller's `Offset`, coalesced to one check at the next frame on `AvonicConfig.RequestFrame`
  (M3), cancelled when it stops listening. Not `ScrollChanged`, which also comes for a change of extent or viewport, which a
  browser doesn't call a scroll.
- **The numbers**: `offsetHeight` is `Bounds.Height`, 0 while not effectively visible, as a hidden element's is; the scroller's
  `offsetHeight` is its `Bounds.Height`, `clientHeight` its `Viewport.Height`, `scrollHeight` the larger of `Extent.Height` and the
  viewport's, and `scrollTop` `Offset.Y`. Reading them forces a layout in a browser, so each check does `UpdateLayout()` first,
  except `complete()`'s first read: there Ionic's comment says the UI hasn't updated yet, which is true in Angular, React and Vue,
  whose DOM follows later. Added items reach Avalonia's tree at once but its layout later, so not forcing that read keeps the
  documented behaviour: what's on screen stays put.
- **Frames**: `raf → readTask → raf → writeTask` is four `RequestFrame`s, so the top's position is restored on the fourth (M5).
- **Disabled**: `IsEnabled`, its own value, not `IsEffectivelyEnabled`, as a `disabled` attribute is the element's. False hides it,
  with `IsVisible` false at style priority, as `display: none` does, so an app's value wins.
- **Pseudoclasses**: `:infinite-scroll-loading` and `:infinite-scroll-enabled`, Ionic's class names, as they're states, not a
  prop's value. The content takes `:infinite-scroll-loading` from the nearest `AvonicInfiniteScroll` among its ancestors, as
  `.infinite-scroll-loading ion-infinite-scroll-content` is a descendant rule, and its theme shows `PART_Loading` on it. A
  pseudoclass, not a rule in `Styles/`, so it costs the style budget nothing.
- **The content's template**: `PART_Loading`, a `StackPanel` of `PART_LoadingSpinner` (an `AvonicSpinner`, shown when
  `LoadingSpinner` isn't null) and `PART_LoadingText` (a `Border`, the div, shown when `LoadingText` isn't null, around a
  `TextBlock`, the text node, shown when it isn't empty: an empty `TextBlock` is a line high, and an empty div is 0). The stub has only the
  structure and the loading rule. The theme has md's sizes: `MinHeight` 84, the loading part centred with a bottom margin of 32,
  the spinner's box 32 high (28 and the line's descent below it), the text's margin `32,4,32,0`, centred and wrapping, in
  `Avonic.TextColorStep400Brush` (`--ion-text-color-step-400`, `#666666`, already global). The spinner isn't coloured, as the md
  rules for it are dead (M8).
- **`loadingText`**: a `string`, shown as it's written. Ionic's own default is plain text, and HTML only with
  `innerHTMLTemplatesEnabled`, which is off unless an app turns it on; Avonic has no HTML to render, so it has no such switch.
- **`infiniteLoadingSpinner`**: Ionic's config key has no `AvonicConfig` member yet, as `AvonicConfig.cs` is shared, so the default
  comes from `AvonicConfig.Spinner`, then `Crescent`. Proposed for shared files.
- **An Ionic quirk kept**: disabling while loading, then enabling, leaves it unable to fire again (M6). It's what Ionic does.

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| B1 | Bottom, 15%: fires at `Offset.Y` 530 and not at 529, once, and not again while loading | M1 |
| B2 | `Threshold="100px"`: 557 fires, 556 doesn't; a change of threshold takes effect; a percentage reads the scroller's height | M2, `thresholdChanged` |
| B3 | The check runs at the next frame, once for several offsets in that frame | M3 |
| B4 | Nothing fires without a scroll, even when everything fits | M4 |
| B5 | `Complete()` lets it fire again on the next scroll; while loading it doesn't | `complete`, `didFire` |
| B6 | Disabled: hidden, height 0, not listening, loading cleared; enabled again: listening | M7, `disabledChanged` |
| B7 | Disabled while loading, then enabled: `Complete()` does nothing and it never fires again | M6 |
| B8 | Top: scrolls to the end at the first frame, fires near the top | M5 |
| B9 | Top `Complete()`: busy until the fourth frame, then the offset grows by what was added above, read through a forced layout | M5 |
| B10 | A check reads a layout that's due, as a browser's forced reflow | `onScroll`'s reads |
| B11 | `IonInfinite` bubbles | `bubbles: true` |
| B12 | It finds `AvonicContent`, or a `ScrollViewer` with `ion-content-scroll-host`; outside both it logs Ionic's error | M10, `findClosestIonContent` |
| B13 | Removed from the tree, it stops listening | `disconnectedCallback` |
| B14 | Not effectively visible: no check, as `offsetHeight` is 0 | `onScroll` |
| B15 | The content shows its spinner and text only inside a loading scroll | the md descendant rule |
| B16 | The content's spinner: `Crescent` by default, `AvonicConfig.Spinner` when set, none for `null`, set once, on the first attach | M11, `componentDidLoad` |
| B17 | The content's look: 84 minimum, 87 with a line of text, 106 with two, 36 for empty text, centred; text colour; the spinner keeps the inherited colour | M8 |
| B18 | Loading text is shown as written | M9 |

## Not applicable

- `user-select: none`: an Avalonia `TextBlock` isn't selectable.
- `IonicSafeString`: there's no HTML, so nothing to mark safe.

## Dependencies

`AvonicContent` (#11) and `AvonicSpinner` (#15), both merged. Nothing shared changes.
