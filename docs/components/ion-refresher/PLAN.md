# ion-refresher, ion-refresher-content — plan

Issue [#35](https://github.com/wdcossey/Avonic/issues/35). Ionic 8.8.1, md. Read from `refresher.js`, `refresher.utils.js`,
`refresher-content.js`, `refresher.md.css`, and measured in Chromium (`ref/`, below).

## Which refresher

`refresher.js` has two refreshers. `shouldUseNativeRefresher` picks md's **native** one when the `ion-refresher-content` has a
spinner as its `pullingIcon` and a `refreshingSpinner`, which md's defaults are (`circular` both). It's a round icon that follows the
finger down from under the header and spins while the app refreshes; the content doesn't move. The **legacy** one, which moves the
content down and shows texts and an arrow icon, is md's only with `pullingIcon` set to an icon or either spinner set to `null`.

Avonic builds the native path, faithfully. The legacy path is a known gap: with no spinner in either place the refresher does nothing.
Its five props only apply to it (each says "Does not apply when the refresher content uses a spinner"), so they aren't declared, and
nor are the content's two texts, which md's native refresher hides (`.refresher-native .refresher-pulling-text { display: none }`).

## Checklist: ion-refresher

| ✓ | Ionic | Avonic | |
|---|---|---|---|
| [ ] | `closeDuration` | — | legacy only; not declared |
| [x] | `disabled` | `IsEnabled` (inverted) | `gesture.enable(!disabled)` |
| [ ] | `mode` | — | not exposed (ADR 0001) |
| [ ] | `pullFactor` | — | legacy only; not declared |
| [ ] | `pullMax` | — | legacy only; not declared |
| [ ] | `pullMin` | — | legacy only; not declared |
| [ ] | `snapbackDuration` | — | legacy only; not declared |
| [x] | `ionPull` | `IonPull` | |
| [x] | `ionPullEnd` | `IonPullEnd`, `RefresherPullEndEventArgs.Reason` | `'complete' \| 'cancel'` → `RefresherPullEndReason` |
| [x] | `ionPullStart` | `IonPullStart` | |
| [x] | `ionRefresh` | `IonRefresh`, `RefresherEventArgs.Complete()` | |
| [x] | `ionStart` | `IonStart` | |
| [x] | `cancel()` | `Cancel()` | sync, as the promise resolves at once |
| [x] | `complete()` | `Complete()` | sync |
| [x] | `getProgress()` | `GetProgress()` | sync, a `double` |
| — | slots, parts, CSS variables | none | the default slot is `Content` |

## Checklist: ion-refresher-content

| ✓ | Ionic | Avonic | |
|---|---|---|---|
| [x] | `pullingIcon` | `PullingIcon`, `SpinnerName?` | narrowed to spinners: an icon name is the legacy refresher's. Unset → `Circular` on first attach (`componentWillLoad`) |
| [ ] | `pullingText` | — | hidden in md's native refresher; legacy only |
| [x] | `refreshingSpinner` | `RefreshingSpinner`, `SpinnerName?` | unset → `AvonicConfig.Spinner ?? Circular` on first attach |
| [ ] | `refreshingText` | — | hidden in md's native refresher; legacy only |

## Behaviours (the test list, `AvonicRefresherTests`)

1. Only in an `ion-content`'s fixed slot: elsewhere it logs and does nothing (`connectedCallback`). B14, B19
2. The gesture is on the content's scroll element: `refresher`, priority 31, `y`, threshold 5. B1
3. `canStart`: not refreshing or completing, and the content scrolled to the top. B9, B11
4. The first move after the capture starts the pull, unless it's upward with no progress (cancelled for the rest of the press):
   state `pulling`, `ionStart`, `ionPullStart`. B1, B12
5. Each later move: `progress = clamp(0, deltaY / 180 × 0.5, 1)`, the pulling animation stepped to it, `ionPull`. B1
6. The pulling animation (`createPullingAnimation`), 1,000 ms `ease-out` read at the step: the icon from `translateY(-60px)` to
   `translateY(100px)`, with `scale(0)`→`scale(1)` when no header is before the content; the circle's dash, the spinner's turn, the
   arrow's container, the arrow, the opacity. B2, B3, B4
7. Letting go at progress ≤ 0.4: the animation plays back over progress × 500 ms, then inactive and `ionPullEnd` `cancel`. B6
8. Past 0.4: the next frame, the icon snaps back from `translateY(progress × 100px)` to 0 in 125 ms, linear, with the rest held; then
   `beginRefresh`: `refreshing`, focus cleared, `ionRefresh`. B5, B17
9. `complete()`: two frames, `completing`; the refreshing icon scales to 0 over its 200 ms transition; at its end inactive,
   `ionPullEnd` `complete`. B7
10. `cancel()`: two frames, `cancelling`, both icons shown; no transition ends, so inactive at Ionic's 700 ms fallback. B8
11. `disabled`. B13
12. The content's defaults, and no spinner → not native. B15
13. A non-circular pulling spinner is a paused spinner that turns, with no arrow (md's arrow is only for `circular`). B16
14. The refreshing circle starts 655 ms into its animation (`animation-delay: -655ms`). B20, B21
15. `pointer-events: none`, `width: 100%`, `height: 60px`. B18
16. An upward drag from the top scrolls the content (Chromium's own scroll, beside Ionic's passive gesture). B10
17. `complete()` and `cancel()` when it isn't native, detached say, take `close()`: the state at once, inactive and `ionPullEnd`
    600 ms later. B19, R6
18. Each `resetNativeRefresher` waits for the refreshing icon's `transitionend`, so two `complete()`s, or a `cancel()` then a
    `complete()`, all end with the one transition, each raising `ionPullEnd`. R5

R1 to R6 came from the reviews: a press dropped on detach, the fixed slot through a panel, `IsEnabled` kept, a new content used.

## Visual states and keys

`refresher.md.css` has no CSS variables. The native icon: 42 px round (24 + 8 × 2 + 1 × 2), `border: 1px solid #ececec`, white,
`box-shadow: 0 1px 6px rgba(0,0,0,.1)`, from global variables that Ionic's light theme doesn't set, so their literals:
`Avonic.Refresher.Md.NativeIconBorderBrush`, `.NativeIconBackgroundBrush`, `.NativeIconBoxShadow`. The spinners and the arrow are
`Avonic.Color.PrimaryBrush`. Which part shows in which state is in the stub theme, as a pseudoclass of the content.

## Dependencies

`AvonicContent` (#11: `GetScrollElement()`, `FixedContent`), `AvonicSpinner` (#15, and its `SpinnerConfigs`/`SpinnerFrame`, internal),
`AvonicIcon` (#3), `Gestures/` (#4), `AvonicHeader` (for the animation type). All merged.

## Extensions

None.
