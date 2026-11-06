# ion-content — Plan

The scrolling area of a page. Scrolling is Avalonia's `ScrollViewer`, including momentum and the touch gesture, so this PR
is mostly what Ionic adds around it: scroll events on Ionic's timeline, scroll methods, fullscreen and the fixed slot.
Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide. **Touch hasn't been tried on real hardware**, here
or anywhere in this project. Touch below means Avalonia's headless `TouchBegin`, `TouchMove` and `TouchEnd`.

## Fidelity checklist

`node tools/ionic-api.js ion-content`.

### Props (7)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `fixedSlotPlacement` | `FixedSlotPlacement` (`ContentFixedSlotPlacement`) | `After`, `Before` |
| [x] | `forceOverscroll` | `ForceOverscroll` (`bool?`) | Null is off in md, because Ionic's default is only on for iOS |
| [x] | `fullscreen` | `Fullscreen` | Works with anything around it, so it doesn't wait for #12 |
| [x] | `scrollEvents` | `ScrollEvents` | |
| [x] | `scrollX` | `ScrollX` | |
| [x] | `scrollY` | `ScrollY` | |

### Events (3)

| ✓ | Ionic | Avonic | Detail |
|---|---|---|---|
| [x] | `ionScroll` | `IonScroll`, `IonScrollEvent` | `ScrollDetailEventArgs` |
| [x] | `ionScrollEnd` | `IonScrollEnd`, `IonScrollEndEvent` | `ScrollBaseDetailEventArgs` |
| [x] | `ionScrollStart` | `IonScrollStart`, `IonScrollStartEvent` | `ScrollBaseDetailEventArgs` |

All bubble, as Ionic's do. The detail has Ionic's fields except two: `event`, because there's no DOM event, and `data`, because
Ionic never sets it.

### Methods (5)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `getScrollElement() => Promise<HTMLElement>` | `ScrollViewer? GetScrollElement()` | Synchronous. Null before it's in a tree |
| [x] | `scrollByPoint(x, y, duration) => Promise<void>` | `Task ScrollByPoint(double x, double y, double duration)` | |
| [x] | `scrollToBottom(duration?) => Promise<void>` | `Task ScrollToBottom(double duration = 0)` | |
| [x] | `scrollToPoint(x, y, duration?) => Promise<void>` | `Task ScrollToPoint(double? x, double? y, double duration = 0)` | |
| [x] | `scrollToTop(duration?) => Promise<void>` | `Task ScrollToTop(double duration = 0)` | |

Internal, with no Avonic member: `recalculateDimensions` and `getBackgroundElement`, which only overlays call. See
[Not in this PR](#not-in-this-pr).

### Slots, parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | default slot | `Content`, `ContentTemplate` |
| [x] | `fixed` | `FixedContent`, `FixedContentTemplate` |
| [x] | `background` | `PART_Background` (`Border`) |
| [x] | `scroll` | `PART_Scroll` (`ScrollViewer`) |

`PART_Container` is the presenter inside the scroll part, and `PART_Fixed` is the fixed slot's.

### CSS variables (9)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Content.Background` | brush | Unset → `Avonic.BackgroundColorBrush` |
| [x] | `--color` | `Avonic.Content.Color` | brush | Unset → `Avonic.TextColorBrush` |
| [x] | `--keyboard-offset` | `Avonic.Content.KeyboardOffset` | `double` | 0. Sets the `KeyboardOffset` extension |
| [x] | `--padding-bottom` | `Avonic.Content.PaddingBottom` | `double` | 0 |
| [x] | `--padding-end` | `Avonic.Content.PaddingEnd` | `double` | 0 |
| [x] | `--padding-start` | `Avonic.Content.PaddingStart` | `double` | 0 |
| [x] | `--padding-top` | `Avonic.Content.PaddingTop` | `double` | 0 |
| [ ] | `--offset-bottom` | — | | Not applicable: dead. See below |
| [ ] | `--offset-top` | — | | Not applicable: dead. See below |

**`--offset-top` and `--offset-bottom` have no key.** Ionic sets them inline on the host on every render. Measured with
`#o1 { --offset-top: 50px }` and with `style="--offset-top: 50px"`: both compute to `0px`, and nothing moves. A key would be a dead
key. In Avonic they're private fields that `Fullscreen` sets.

### Global variables

`--ion-safe-area-*` → `Avonic.SafeAreaTop`, `…Bottom`, `…Left`, `…Right` (`double`, 0), in `Tokens/Content.axaml`.

`ion-item` deferred these to this control, but **`content.css` doesn't read them**, so `AvonicContent` doesn't either. They're read
by `ion-item`, `ion-header`, `ion-footer`, `ion-toolbar` and `ion-fab`. They're defined here so that #12 and the item have one
place to read them from, with the exact change for the item under "Proposed for shared files" in the PR.

The colour system's plan said a static 0 would be wrong for a device value. For Avalonia it's right: since 11.1 it pads the root view
by the safe area itself, through `TopLevel.AutoSafeAreaPadding`, per its docs, so adding the inset again would double it. An
app that turns that off for edge-to-edge sets these from `InsetsManager.SafeAreaPadding`. **None of that has been tried on a
device.**

## Behaviours

Each becomes a test, named after its number in `AvonicContentTests`. Scene names are from my measuring script.

| # | Behaviour | Source |
|---|---|---|
| 1 | It fills what it's given whatever its content, and an alignment doesn't shrink it. With no height to fill, in a `StackPanel` or an `Auto` row, it's zero high | `width: 100%; height: 100%; contain: size`; measured `c3` 150 of 200, `c2` 390×0 |
| 2 | The parts fill the host. Nothing takes room for scroll bars | measured `c`: clientWidth 390 with scrollHeight 1000 |
| 3 | White with black text and the global font. The globals are the fallback, and the tokens beat them | `--background`, `--color`; measured `v` |
| 4 | `Color` paints the scroll part, base and contrast, and the background part stays. It beats `--color` | `:host(.ion-color) .inner-scroll`; measured `p`, `vp` |
| 5 | An unknown `Color`: no background and the host's text colour, or a coloured ancestor's. It still takes input | measured `bad`, `in`; `var()` invalid at computed time |
| 6 | The fixed slot keeps what the host inherits: `--color` and `Color` are on the scroll part | measured `pf`, `vf`, `gf` |
| 7 | Padding moves and narrows the content, scrolls with it and counts in the extent. The keyboard offset adds to the bottom, whatever sets the padding. RTL mirrors it | `.inner-scroll`; measured `pa`, `pb`, `kb`, `rtl` |
| 8 | Long text wraps to the width. A `TextBox` stays on one line unless it asks to wrap | measured `fitc`; an `<input>` |
| 9 | A child is as high as its content, unless it says otherwise | block flow; measured `blkc` 18 |
| 10 | The wheel moves only the axes that are on, and they can change at runtime. With `ScrollY` off it's still a scroller to a script and keeps its position, and the wheel and page keys go on to whatever's outside | `.scroll-x`, `.scroll-y`; measured `y`, `nn`, `xx`, `xo`, `scrollY false` |
| 11 | At its limit it keeps the wheel. If it fits, or with `ScrollY` off, it passes it on | `overscroll-behavior: contain`; measured `nested` |
| 12 | A touch drag scrolls, on blank content too, on the axes that are on. At its limit a nested one keeps the drag, and if it can't scroll it passes it on. A drag over a button item doesn't click it, and a tap does | `touch-action`, `overscroll-behavior`; measured nested drags, and drag: `activated` false, 0 clicks. Headless touch, not hardware |
| 13 | `ForceOverscroll` lets content that fits scroll by exactly one pixel, whatever the padding. Long content is unchanged. Null and false do nothing. It needs `ScrollY` | `.overscroll::before`; measured `t` 301, `pd` 301, `l` 1000, `d`, `f`, `nx` |
| 14 | `ScrollEvents` off raises nothing. On: start at once, `IonScroll` a frame later, end when a 100 ms watchdog finds 120 ms of quiet | `onScroll`, `onScrollStart`; measured `one` ends at 203, `gap 90` at 310 |
| 15 | Scrolls in one frame are one event with the latest position. No change, no event. Content shrinking under the offset is a scroll, and a change of size alone isn't | measured `same tick twice`, `no real change`, `content shrinks` |
| 16 | The detail: start and delta, velocity smoothed at 0.7/0.3 when the gap is under 100 ms, and reset at a new start | `updateScrollDetail`; measured `steps` |
| 17 | Start and end carry `IsScrolling`. All three bubble from the content | measured |
| 18 | Removing it ends a scroll at once, and an animation that outlives it starts nothing new. Idle, it raises nothing | `disconnectedCallback`; measured `removed while scrolling` |
| 19 | Turning `ScrollEvents` off stops new ones and finishes the current one | `onScroll` goes, the watchdog stays |
| 20 | `GetScrollElement` is the scroll part, once it's in a tree, even before it's shown. A template with no parts is safe | `part="scroll"` |
| 21 | Under 32 ms it jumps, limited to the range. A null coordinate stays | `duration < 32`; measured `31 vs 32`, `null keeps`, `bottom` |
| 22 | At 32 ms or more: a frame's wait, then a cubic ease-out, floored, from where it was. An axis with no change isn't written | `scrollToPoint`; measured `frac anim` |
| 23 | Nothing cancels an animation: not a second one, not the user, not removal. Each task finishes | measured `concurrent`, `user during`, `detached` |
| 24 | `AvonicConfig.Animated` off changes nothing: it still eases | measured `animated:false`: 264 of 500 after 60 ms |
| 25 | `ScrollToTop`, `ScrollToBottom` and `ScrollByPoint` go through it. Content added just now counts. Before load, a scroll waits for load | measured `bottom`, `short`, `added then bottom`, `before attached` |
| 26 | `Fullscreen` takes the background and scroll parts to the page's edges, and pads the content back, so it starts where it would have and can scroll clear. They paint and take input there, unless something with a higher `ZIndex` is on top. The page is the closest `.ion-page`, or else the parent | `readDimensions`, `getPageElement`; measured `fs`, `nest`, `hit at head` |
| 27 | `Fullscreen` follows a change, and the page's layout, at once | measured `toggle on`, `toggle off`, `head grows` |
| 28 | The fixed slot is at the host's corner, not the page's | measured `fs` fixed: 466 |
| 29 | The fixed slot is as big as its content, at the corner. An alignment places it, or stretches it | `position: absolute`; measured `nop-f`, `ri-f`, `wh-f` |
| 30 | It doesn't scroll, and only takes input on its own child | measured `scroll keeps fixed` |
| 31 | `After` is on top. `Before` is under the scroll part, which takes the pointer | measured `after`, `before`: the hit is the body |
| 32 | `After` tabs to the content first, and `Before` to the fixed slot first. A change moves it without remaking the child | measured `tab order` |
| 33 | It's the `Main` landmark, with a peer that a screen reader's tree includes | `role="main"`; measured |
| 34 | Focus scrolls a child into view | Avalonia's `ScrollViewer`, as a browser's |
| 35 | Slots take templates | Mapping rule |
| 36, 37 | The tokens have Ionic's defaults, and the safe area is 0 | `content.css` |

## What's async

The mapping rule is that `Promise<T>` becomes `T` unless it's genuinely async.

| Method | Avonic | Why |
|---|---|---|
| `getScrollElement` | Synchronous | Ionic waits for the component to load. Avalonia can apply the template on demand once it's in a tree. Before that a theme can't be found, so a constructor gets null, where Ionic's promise would wait. See "For you to decide" in the PR |
| `scrollToPoint` and the rest | `Task` | Genuinely async with a duration, because the task finishes with the animation, and before the first layout, because there's no extent to scroll in yet |

With no duration, once loaded, the task is already finished and the offset already set, where Ionic has done it after one
`await`. A `Task` is the type either way, so calling code doesn't change when a duration is added.

**Before the first layout a scroll waits for `Loaded`,** because Avalonia limits an offset by the last layout, which would lose it.
Ionic's waits too: measured `before attached`, not finished until it's in the page, then at 100. After that the control doesn't wait
again. Removing it before it loads finishes a waiting task, unless it's a move and it's back by the next idle moment, so a removed control's task finishes.

**A scroll lays out first,** with `UpdateLayout()`. In a browser, reading `scrollHeight` or setting `scrollTop` forces layout. Without this,
adding content and calling `ScrollToBottom()` goes to the old end. Measured `added then bottom`: 1300 in Ionic.

**Frames.** `requestAnimationFrame` and `readTask` are `AvonicConfig.Schedule` at 16 ms, so that `TestClock` asserts them exactly. A
position comes from `AvonicConfig.Now`, so a late frame doesn't slow the scroll, but a 120 Hz screen only gets 60 updates.

## Design

**`TemplatedControl` with plain `ContentPresenter`s,** because it has two slots. The presenter for the default slot is inside the `ScrollViewer`.

**The `ScrollViewer` has its own theme, `AvonicContentScrollViewer`, in the stub's file.** A base theme's template has scroll bars, and
`TestApp` has no base theme, so a plain `ScrollViewer` wouldn't scroll in tests at all. It's copied from `SimpleTheme`, with its `ScrollGestureRecognizer`, which is
what makes a drag scroll, and without the bars. The recognizer isn't given `Offset`, `Viewport` and `Extent`. With those it gives up at the
scroller's limit and the outer scroller takes the drag, as a test found, where measured `inner at end` stays at `0, 200` with the outer unmoved.

**`ClipToBounds` is off on the host,** in the control. Avalonia clips by default, which silently stopped `Fullscreen` painting or
taking input outside the host while its layout tests passed. A hit test found it.

**Padding is the control's `Padding` property.** The theme sets it from the tokens with `Css.Box`. The control adds the keyboard offset and
`Fullscreen`'s offsets and puts the result on `PART_Container`. It's inside the scroll part, because CSS padding on a scroller scrolls with
the content and counts in its extent, measured `pb`. So `Padding="16"` on a control also works, in place of `.ion-padding`, which the colour
system's plan says isn't planned. The keyboard offset is its own property, an extension, so a local `Padding` doesn't lose it.

**A text box stays on one line.** `TextBlock.TextWrapping` inherits, and it's on for text, as CSS wraps. A `TextBox` shares the property, so without
`c|AvonicContent TextBox` in `Styles/Content.axaml` every single-line field in a page would wrap and grow. It's a descendant selector, which the project
avoids for common types, because it walks ancestors on attach. Measured with and without, alternating, on 5,000 at depth 20 in Release: 104 ms against 104 ms, so no measurable cost. It's only in the
theme, because a stub's `ControlTheme` can't reach a descendant. `AvonicItem` has the same inherited wrap.

**Behaviour's in the control, at style priority where a child is involved.** A child's `VerticalAlignment` is `Top`, and a fixed child's alignments are
`Left` and `Top`, set with `SetValue(..., BindingPriority.Style)`, and those frames are disposed when the child goes. An app's own value beats them.

**The fixed slot's place** is its place in the template's `Panel`, moved with `Children.Move`, as a slot in the DOM decides both paint order
and tab order. Moving doesn't detach the child.

**`Fullscreen` follows `LayoutUpdated`,** subscribed only while it's on. Its answer depends on the page's layout, and margins on the parts don't change the host's bounds, so this settles.

**Scroll events read `OffsetProperty`,** not `ScrollChanged`, which also fires for a change of extent or viewport, and only after layout.

**`IsScrollChainingEnabled` is off,** for `overscroll-behavior: contain`, and Avalonia's handling matches what's measured in `nested`: a
scroller at its limit keeps the wheel, and one that fits doesn't.

**`ScrollY` off is still `Hidden`, not `Disabled`.** `overflow: hidden` is a scroller to a script and keeps its position, and locking a page while a
sheet is open is a common use. `Disabled` would collapse the extent and send it to the top. So the control blocks user input instead. The recognizer's
`CanVerticallyScroll` comes from `ScrollY`. Avalonia's presenter takes the wheel, and its viewer the page keys, whenever there's somewhere to go, without asking
about the axis, and they can't be skipped from outside. So `PART_Container` marks all unhandled wheel and key input handled to get past them, and `PART_Scroll` takes it back, so it
carries on to whatever's outside. A drag at an angle can't be blocked that way, because the gesture's handler is in the presenter's constructor, so with
`ScrollX` on and `ScrollY` off the vertical axis is `Disabled`.

### What #12 needs from this

- **A `ZIndex` above 0 on the header and footer.** In Ionic `ion-header` and `ion-footer` have `z-index: 10`. With `Fullscreen`, the content
  extends behind them and would otherwise cover whichever comes earlier in the panel. `B26` shows it working with plain `Border`s.
- **Nothing from the header to make `Fullscreen` work.** It measures the layout, so a header in a `Grid` row just works, including when it changes height.
- **`Avonic.SafeAreaTop`, `Avonic.SafeAreaBottom`, `Avonic.SafeAreaLeft` and `Avonic.SafeAreaRight`** for `header.md.css`, `footer.md.css` and `toolbar.md.css`.
- **`GetScrollElement()`** for what `header.js` and `footer.js` do with `findIonContent`, though the fade and condense paths are iOS-only.
- **A class `ion-page` on the page** if there's a wrapper between it and the content. The parent is the page otherwise.

### Extensions

- **`KeyboardOffset`**, a property the token sets, so that a local `Padding` doesn't lose it, and a keyboard utility has somewhere to write.

## Deliberate differences

| Difference | Why |
|---|---|
| `ScrollX` off means text wraps and a wider child is clipped, with no way to reach it by script | `Disabled` measures at the viewport's width, so text wraps. CSS can have both, and measured `y` reaches `scrollLeft` 50 by script. Avalonia limits a desired size to what's available, so a wider child can't report its width at a finite measure |
| `ScrollX` on means text doesn't wrap at the viewport, unless it's given a width | `Hidden` measures at infinite width. Measured `fitc` wraps at 390 with `scroll-x`. Same cause |
| With `ScrollX` on and `ScrollY` off, a script can't scroll vertically, and turning `ScrollY` off goes to the top | See the design. Measured `xo` takes `scrollTop` 100. `ForceOverscroll` also needs `ScrollY`, where measured `sx` has 301 with only `scroll-x`, reachable by script only |
| Turning `ScrollX` off goes back to the start | `Disabled`, as above. In CSS it keeps `scrollLeft`. Read from the CSS, not measured |
| `GetScrollElement()` is null before it's in a tree | Ionic's promise waits. A theme is found through the tree, so there's no template to apply in a constructor. `ScrollToPoint` and the others do wait, so use those, or read it on `Loaded` |
| A `Margin` on the control is honoured | `content.css` has `:host { margin: 0 !important }`. A control's margin is how Avalonia lays out, so it isn't removed. Read from the CSS, not measured |
| A child's alignment from its own `ControlTheme` loses to the slot's default | Global `Styles` beat a `ControlTheme`, and this is set at style priority, as the project's other slot defaults are. A local value or a `Style` wins. A control like `ion-fab` needs to set its alignment in code or with a style |
| `Fullscreen` follows the layout at once | Measured `head grows`: Ionic stays at 56 until a window resize and 100 ms after it. Its comment says that's to limit thrashing, and a layout pass here already batches |
| A wrapper's position counts from the page, whatever's positioned | `offsetTop` is from the nearest positioned ancestor. Avalonia has no such idea. The same in the measured `nest` |
| A removed and re-added control keeps its scroll position | A browser resets it, measured `afterReattachTop` 0. Avalonia's doesn't, and a page that's been navigated away from and back is better for it |
| A waiting `IonScroll` is dropped on removal | Ionic's `readTask` still emits on the removed element, where no ancestor can hear it |
| `ScrollTop` can be fractional | Chromium rounds `scrollTop`, measured 60.5 → 61. Avalonia's offset is a double, and its layout rounds what's drawn |
| RTL `Offset.X` counts up from the start edge | CSS counts down from 0, measured `-100`. `ScrollToPoint(100, ...)` in RTL scrolls where `scrollToPoint(-100, ...)` does in Ionic |
| A browser scrolls by key when focus is on the scroller | See known gaps |

## Known gaps

| Gap | Why |
|---|---|
| **The keyboard.** A browser makes a scroller with no focusable child a tab stop, and scrolls it by arrow (40), Page Down and Space (262 of 300), Home and End, measured `keys`. Avalonia's `ScrollViewer` only takes Page Up and Page Down, by a full viewport, with focus inside | Browser behaviour, not Ionic's, on a phone-first control. Doing it means a tab stop and a key handler that defers to a focused text box. Raised under "For you to decide" |
| **No scroll indicator.** A phone browser draws one while scrolling, and this draws none | It's the browser's, not Ionic's: the reference, with hidden scroll bars, shows none. A base theme's `ScrollBar` is desktop-sized. Raised under "For you to decide" |
| **No elastic overscroll.** Avalonia's `ScrollContentPresenter` limits the offset, so `ForceOverscroll`'s pixel doesn't bounce, as it does with iOS's OS-level bounce | Avalonia has none to inherit. What can be measured, the extent, matches. The bounce itself can't be compared in headless Chromium either |
| **Momentum** is Avalonia's `ScrollGestureRecognizer`, with its own curve, not a browser's | Issue: document what's inherited. Not compared, and not tried on hardware |

## Not in this PR

| What | Why, and where it goes |
|---|---|
| `role` off inside `ion-menu`, `ion-popover`, `ion-modal` | Those controls don't exist. Each can turn it off with a style: `c\|AvonicModal c\|AvonicContent { AutomationProperties.LandmarkType: null }` |
| `.content-sizing` inside `ion-popover`, and `recalculateDimensions`, `getBackgroundElement` | #5's overlays. A popover's content sizes to its content, so it needs the `contain: size` in `MeasureOverride` turned off |
| `ion-tabs` as the page, and `ionTabBarLoaded` | The tabs control. Until then, a class `ion-page` on the tabs' host does it |
| `ion-app` as the page | No such control. Measured: it would win over a plain parent |
| `.transition-effect`, `.content-ltr`, `.content-rtl` | iOS only: `transitionShadow = mode === 'ios'` |
| `.ion-padding` and friends | Not planned, per the colour system's plan. `Padding` on the control does it |
| `--keyboard-offset` set by a keyboard | `scroll-assist`, with the inputs. A per-instance `Resources` value is the way in |
| The default text size inside the content: Avalonia's, where a web page's is 16 | It affects everything. An open project question, so left |

## Dependencies

None on unbuilt controls. `ion-item` is only used by a test, for tap-through.
