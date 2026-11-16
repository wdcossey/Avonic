# ion-content — Plan

The scrolling area of a page. Scrolling is Avalonia's `ScrollViewer`, including momentum and the touch gesture, so this PR
is mostly what Ionic adds around it: scroll events on Ionic's timeline, scroll methods, fullscreen and the fixed slot.
Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide, and those 8.8.19 changed (`Fullscreen` after a header's
change, 27) again on 8.8.19. **Touch hasn't been tried on real hardware**, here
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

`--ion-safe-area-*` → `Avonic.SafeAreaTop`, `…Bottom`, `…Left`, `…Right` (`double`, 0), in `Tokens/Global.axaml` since #72.

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
| 27 | `Fullscreen` follows a change, and the page's layout, at once | measured `toggle on`, `toggle off`, `head grows`; on 8.8.19, `head grows` (the content resized) and `head and foot` (moved, same size) |
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

**Frames.** `requestAnimationFrame` and `readTask` are `AvonicConfig.RequestFrame`, since #72, and `TestClock` runs it at 16 ms so that it asserts them exactly. A
position comes from `AvonicConfig.Now`, so a late frame doesn't slow the scroll.

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
- **Keyboard scrolling, and the scroll indicator,** since #67: what a browser gives Ionic. No property: see [below](#keyboard-scrolling-and-the-scroll-indicator-67).
- **`ScrollBars`, and `AvonicConfig.ContentScrollBars`,** since #175: a desktop browser's scroll bar, off by default. See [below](#a-draggable-scroll-bar-by-config-175).

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
| `Fullscreen` follows the layout at once, a move included | Since 8.8.19 Ionic's `ResizeObserver` on the host re-reads the offsets a frame after the host's own size changes, while `fullscreen` is on (not in a popover, and not while hidden): measured `head grows` 56 → 100, where 8.8.1 stayed at 56 until a window resize and 100 ms after it. A move that keeps the size fires nothing: measured `head and foot`, the head 20 taller and the foot 20 shorter, stays at 56 and 40 there, and follows here. A layout pass already batches, so following it costs nothing more |
| A wrapper's position counts from the page, whatever's positioned | `offsetTop` is from the nearest positioned ancestor. Avalonia has no such idea. The same in the measured `nest` |
| A removed and re-added control keeps its scroll position | A browser resets it, measured `afterReattachTop` 0. Avalonia's doesn't, and a page that's been navigated away from and back is better for it |
| A waiting `IonScroll` is dropped on removal | Ionic's `readTask` still emits on the removed element, where no ancestor can hear it |
| `ScrollTop` can be fractional | Chromium rounds `scrollTop`, measured 60.5 → 61. Avalonia's offset is a double, and its layout rounds what's drawn |
| RTL `Offset.X` counts up from the start edge | CSS counts down from 0, measured `-100`. `ScrollToPoint(100, ...)` in RTL scrolls where `scrollToPoint(-100, ...)` does in Ionic |
| Keyboard scrolling and the scroll indicator | Since #67: see [below](#keyboard-scrolling-and-the-scroll-indicator-67), with their own differences |

## Known gaps

| Gap | Why |
|---|---|
| **No elastic overscroll.** Avalonia's `ScrollContentPresenter` limits the offset, so `ForceOverscroll`'s pixel doesn't bounce, as it does with iOS's OS-level bounce | Avalonia has none to inherit. What can be measured, the extent, matches. The bounce itself can't be compared in headless Chromium either |
| **Momentum** is Avalonia's `ScrollGestureRecognizer`, with its own curve, not a browser's | Issue: document what's inherited. Not compared, and not tried on hardware |

## Keyboard scrolling and the scroll indicator (#67)

A browser gives these to Ionic for free, so they aren't in `core.json`, and they're **extensions**: behaviour with no property, so nothing
shadows an Ionic name. Everything here was measured in Chromium 141 (Playwright 1.56's), md, on `ion-content` in `ion-app`, with the
scripts kept in the issue's temp folder: the headless shell in Android emulation (390 wide, 2×, touch, a Pixel 8's user agent) for
every number, and again in Chromium's full headless mode and with no emulation, which gave the same numbers. **No phone was attached**
(`adb devices` was empty), so nothing here is from a device.

### What Chromium does with the keys

A content 300 high with 3000 or 5000 of content, focused by Tab unless it says otherwise.

| Key | Measured |
|---|---|
| Up, Down | 40 |
| Left, Right | 40, only with `scroll-x`. Physical: in RTL, Left goes towards the end (`scrollLeft` -40) |
| Page Up, Page Down | 87.5% of the viewport, rounded down: 87, 175, 262, 350, 525 and 738 for 100, 200, 300, 400, 600 and 844 |
| Space, Shift+Space | a page down, a page up |
| Alt+Up, Alt+Down | a page |
| Home, End, Ctrl+Home, Ctrl+End | the top, the bottom. Vertical only: with only `scroll-x` they do nothing |
| Cmd+Up, Cmd+Down, on macOS | the top, the bottom: Chromium's macOS key bindings, **not measured** (no Mac here). Meta+Down measured nothing on Windows |
| Ctrl, Shift or Meta with an arrow; any modifier with Page Down; Shift+Home, Shift+End; Ctrl+Space, Alt+Space | nothing |
| With `scroll-x` only | Up, Down, Page Down, Space, Home and End do nothing |
| At the limit | nothing; nested, the key goes on to the content around, **even with `overscroll-behavior: contain`**: an inner one at its end, and then Down scrolls the outer by 40, Page Down by 262, End to its end |

**It's animated,** whatever the key: 150 ms of `cubic-bezier(0.42, 0, 0.58, 1)`, from the key, for 40 px as for 4,398. That's Chromium's
`ScrollOffsetAnimationCurve` with a constant duration (`kConstantDuration` 9 / `kDurationDivisor` 60 s), and a frame-by-frame fit at 240 Hz
in full headless matched it. **A key during one adds to its target,** not to where it's got to: two Downs 57 ms apart end at 80, at about
207 ms; two Page Downs 50 ms apart at 524; ten repeats of a held Down, 47 ms apart, at 400, 150 ms after the last. The new segment keeps
the speed it has, and lasts at most 150 ms and at most 2.5 times what the distance left takes at that speed: Page Down, then Up 70 ms
later, ends at 222 at about 135 ms. A model of the curve's `UpdateTarget` (Chromium's `scroll_offset_animation_curve.cc`) gave every frame
of the 240 Hz traces of those two to within a pixel.

**Focus.**

| Case | Measured |
|---|---|
| Tab, with nothing focusable inside | the scroller takes focus (`ion-content`'s shadow `.inner-scroll`); Shift+Tab from after it too |
| Content that fits; `scroll-y="false"` | not a tab stop. `forceOverscroll`'s one pixel makes it one |
| Nested, the inner can scroll | the outer isn't a tab stop, as it has the inner; Tab goes to the inner |
| Nested, the inner fits | the outer is the tab stop |
| A button inside, then Tab | the button. Tab again leaves the content |
| A press on blank content, by mouse or touch | focus doesn't move (`document.activeElement` is `body`), and the keys scroll the content after it: Down 40, Page Down 262. Nothing pressed: nothing scrolls. A focused button or input loses focus |
| The ring | `outline: auto 1px`, 2 px, from 1 outside to 1 inside, about 3.5 px round at its outer corner: `rgb(229, 151, 0)` in emulation, Android's, and `rgb(16, 16, 16)` without |

**What's focused inside.**

| Focus | Measured |
|---|---|
| A button, a checkbox, a button `ion-item` | Space clicks or toggles it and doesn't scroll; the arrows, Page Down, Home and End scroll |
| An `<input>`, an `ion-input` | the arrows, Home, End and Space edit, and don't scroll; Page Up and Page Down scroll by a page. A caret that moves out of view scrolls the field back into view |
| A `<textarea>` | Page Up and Page Down move the caret first, and scroll once it's at the end; the arrows, Home and End edit |

### What Chromium draws while scrolling

With `--hide-scrollbars` off, as Playwright's headless sets it. Read from screenshots and a CDP screencast's frame times.

| What | Measured |
|---|---|
| The thumb | 4 px thick, flush with the end edge (the left in RTL) or the bottom, square ends, `rgba(128, 128, 128, 0.5)`: 191 on white, 64 on black |
| Its length | the track × viewport / extent, rounded down, at least 4: 75 for 1200 in 300, 237 for 3000 in 844, 4 for 30,000 in 300 |
| Its place | (track − length) × offset / (extent − viewport), rounded down: 75, 84, 2 |
| The track | the scroller's whole side; less 4 at the corner when both axes scroll (74 long at 74 for 1200 in 300, and 96 at 74 across for 1560 in 390). Not inset by the padding |
| When | a scroll by the user: the wheel, a finger, the fling. **Not** a script's `scrollTop`, `scrollTo({ behavior: 'smooth' })`, Ionic's `scrollToPoint`, or a key |
| How long | all there until about 300 ms after the last scroll, then a fade at a steady rate over about 300 ms: 0.5, 0.47, 0.44 … 0.02 and 0, a frame apart from 302 to 584 ms. A finger held still fades it too |
| Both axes | one fades with the other |
| Desktop, no emulation | a classic 15 px scroll bar, which takes the width: `clientWidth` 335 of 350 |
| From load | **emulation shows the thumb from the page's load until the first scroll by the user**, and only then fades it |

### Design

- **Keys:** `PART_Container` takes a key from something in the content that didn't take it, and `PART_Scroll`'s tunnel takes one with focus on
  the scroller itself, so both come before Avalonia's `ScrollViewer`, whose own Page Up and Page Down go a whole viewport. A key that scrolls
  is handled. One that can't, at the limit or on an axis that's off, is passed over Avalonia's scroller, as the wheel is with no axis on, and
  goes on to the content around, as Chromium's does. A `TextBox`, or anything in one, keeps all but Page Up and Page Down, which it doesn't
  handle itself. The target is from where an animation is going, so a key during one adds to it, and the limit is checked there.
- **The curve** is `ContentKeyboardScroll`, internal, Chromium's `ScrollOffsetAnimationCurve` for a key, on `AvonicConfig`'s clock, with
  `CubicBezierEasing.Slope` (internal, new) for the speed a retarget keeps. Not `SmoothScroll`, which is Chromium's programmatic curve:
  √distance / 60 s, and no retarget. Anything else that scrolls it in the middle stops it, as in `SmoothScroll`.
- **Tab:** `PART_Scroll` is `Focusable` while it can scroll on an axis that's on, and gives up focus when it stops, as Chromium blurs what stops being focusable. An overlay's focus trap skips it (`OverlayHost.Focusables`), as Ionic's `focusableQueryString` can't see into `ion-content`'s shadow root; Tab still stops at it inside an overlay, as Chromium's does. When Tab brings focus to it and something in it can take focus,
  it sends it on, to that, or with Shift past the scroller, read at that moment, as what's inside changes.
- **A press:** Avalonia sends keys only to what has focus, and has nothing like Chromium's last-pressed node, so **a press on the content
  focuses the scroller**, as Avalonia focuses the closest focusable ancestor of what's pressed. That's a deliberate difference, below.
  **By a finger or a pen, only a tap does:** a touch that becomes a scroll sends no `mousedown` in Chromium and moves no focus, so a
  field being typed in keeps it, and its keyboard on a phone. Avalonia already does this: it focuses a finger's tap on release, and
  nothing after a scroll. The first review raised it; tests hold it, and a breakage run showed code for it did nothing, so there's none.
  Focus brings what it lands on into view, so a press on a nested content part way out of view made the outer one jump, which a test
  found; the scroller refuses its own bring-into-view from a press to its release, and Tab still brings it in. A press on a focused `ion-input`'s label
  now blurs the field and focuses it again, as the scroller takes focus on the way down and the label gives it back on release: Chromium
  does the same, measured `ionBlur`, `blur`, `ionFocus`, `focus`.
- **The ring** is `PART_FocusRing`, a `Border` first in the scroller's template, so under the content, at a margin of -1, shown on
  `:focus-visible`, which Avalonia sets for focus by key only, as CSS does. The margin is the theme's, `Avonic.Content.FocusRingMargin`. It draws the scroller's own `BorderBrush`, `BorderThickness`
  and `CornerRadius`, which the theme sets: `Avonic.Content.FocusRingBrush` (`#E59700`), 2 and 3.5. The scroller doesn't clip, so the ring
  reaches a pixel outside, and its presenter still clips the content. **Not a `FocusAdorner`,** which was tried first: Avalonia's adorner
  layer draws over everything, so the ring was 2 px over rows with a background, where Chromium paints the outline under the scrolling
  content and shows only the pixel outside (measured: 2 device pixels at 2×, against 4 over a transparent child). The base theme's adorner
  is turned off on it, as the ring stands for `outline: auto`. The scroller's own background, a `Color`'s, is `PART_ScrollBackground`,
  under the ring, as an element's own background is under its outline (the second review found it covered the inner pixel), and
  the presenter's background is transparent where it has one, so it takes input where it did.
- **The indicator** is `ContentScrollIndicator`, two of them in the content's `ScrollViewer` template, `PART_VerticalIndicator` and
  `PART_HorizontalIndicator`, over the presenter, so the fixed slot stays on top, and Fullscreen takes them behind the header, as Chromium's
  bar is on the scroller. Behaviour is in Components, and the look in the theme: `Background` from `Avonic.Content.ScrollIndicatorBrush`
  (`#80808080`), and a `Width` or `Height` of 4 on `:vertical` and `:horizontal`. It draws the thumb itself, rounded down as measured, the
  minimum its own thickness. It shows when the offset changes while a wheel or a scroll gesture is being dispatched to its scroller, which a
  static class handler marks (a wheel's tunnel at the `ScrollViewer`, a gesture at its `ScrollContentPresenter` before the presenter's own
  handler), only for a scroller with an indicator, and fades on `AvonicConfig`'s clock: 300 ms after the last, with one wait that looks
  again when it's up, then 300 ms down to nothing. An axis that can't scroll doesn't show. It takes no input.
- **Its relation to Avalonia's `ScrollBar`:** the content's `ScrollViewer` has its own theme with no bars, since #11, and keeps it: a base theme's
  `ScrollBar` is a desktop control that takes input, and on a phone Chromium draws only this thumb. On desktop too, md here is the phone's:
  the reference is a phone, and #11's behaviour 2 is that nothing takes room for a bar. A desktop browser's 15 px bar is recorded below, and
  since #175 an app can turn one on ([below](#a-draggable-scroll-bar-by-config-175)).

### Behaviours

Tests in `AvonicContentKeyboardTests` (B38 to B52) and `AvonicContentIndicatorTests` (I1 to I6), named after these numbers.

| # | Behaviour | Measured |
|---|---|---|
| 38 | Up and Down scroll 40, Left and Right 40 with `ScrollX`, physical in RTL | `tab`, `scrollx`, `scrollx-rtl` |
| 39 | Page Up, Page Down and Space go 87.5% of the viewport, rounded down; Shift+Space up; Alt with Up or Down a page | `page100` … `page844`, `mods` |
| 40 | Home and End, with or without Ctrl, go to the top and the bottom, vertical only | `tab`, `mods`, `scrollXOnly` |
| 41 | Other modifiers do nothing, and nor do other keys; Avalonia's own page keys never act | `mods` |
| 42 | 150 ms of `cubic-bezier(0.42, 0, 0.58, 1)` from the key, whatever the distance | `arrow`, `pageDown`, `end` |
| 43 | A key during one adds to its target, and keeps its speed: two Downs end at 80 at 207 ms; Page Down then Up ends at 222 at about 135 | `twoQuick`, `reverse`, `held`, `twoPages` |
| 44 | Something else scrolling it stops it, and the next key starts from where that left it | not measured; as `SmoothScroll` |
| 45 | An axis that's off, or a limit, sends the key on to the content around | `noscrolly`, `scrollXOnly`, `nested…AtEnd` |
| 46 | Tab stops on a content that can scroll with nothing focusable inside, and Shift+Tab; not on one that fits or has `ScrollY` off; `ForceOverscroll`'s pixel counts. One that stops being able to scroll gives up focus. An overlay's focus trap never lands on it | `tab`, `fits`, `noscrolly`, `shiftTab`, `overscrollTab` |
| 47 | With something focusable inside, Tab goes into it and Shift+Tab past it; nested, the inner is the stop | `button`, `nested`, `nestedFits` |
| 48 | A press on the content lets the keys scroll it after, and doesn't scroll a content around it. A finger that scrolls leaves a field its focus; a tap takes it | `click`, `tap`, `clickWithButton` |
| 49 | A focused control that takes Space keeps it; the arrows and pages still scroll | `button`, `checkbox`, `ion-item` |
| 50 | A text field keeps the arrows, Home, End and Space, and Page Up and Page Down scroll | `input`, `ion-input` |
| 51 | The ring: 2 px, from 1 outside to 1 inside, Android's colour, by key only, under the content, so a row with a background covers its inner half, and over the content's own `Color` | ring, `ring2` |
| 52 | The scroll events report a key's scroll, as any other | Ionic's `onScroll` on the scroll element |
| I1 | The thumb's thickness, length, place, minimum, edge and RTL side, and the corner with both axes | `short`, `long`, `page`, `x`, `both`, `rtl`, `padded` |
| I2 | It shows on the wheel and a drag, not on a script, `ScrollToPoint` or a key | `wheel`, `script`, `scrollToPoint`, `smooth scrollTo`, `keys`, `touch drag` |
| I3 | 300 ms, then a steady fade over 300 ms, and a new scroll brings it straight back | screencast |
| I4 | Nothing on an axis that can't scroll | `x` |
| I5 | It takes no input, and draws over the content, under the fixed slot | Chromium's overlay bar |
| I6 | The colour is a token, the same on a dark page | `dark` |

### Deliberate differences

| Difference | Why |
|---|---|
| A press on the content focuses its scroller, where Chromium leaves focus on `body` and remembers the press | Avalonia sends keys to what has focus, and has no last-pressed node. Focus moves off a focused button or input either way, as measured `clickBlurs`, `clickBlursInput`; the scroller is what has it here, with no ring, as Avalonia shows the adorner only for focus by key. Tab from there goes into the content, to its first focusable child, where Chromium's measured `clickWithButtonTab` went too |
| A field's caret moving out of view doesn't scroll the content back to it | Measured `input`: `scrollTop` 262 back to 3 on a Down in the field. Avalonia's `TextBox` keeps its caret in view in its own scroller only. Not a key scroll, so not in this issue |
| A multi-line `TextBox` with Page Down: whatever Avalonia's `TextBox` does | Chromium's `<textarea>` moves the caret to the end first, then scrolls. Avalonia's `TextBox` in a base theme decides, and this scrolls only if it doesn't take the key. Not measured with a base theme |
| The thumb isn't there from the page's load | Measured in emulation only, where it shows from load until the first scroll by the user. A phone isn't believed to: raised under "For you to decide", with no device to measure |
| The track loses the indicator's own thickness at the corner | Chromium leaves the other bar's: both are 4 here, so it's the same, unless a theme makes them differ |
| Nothing follows the OS's "reduce motion" | Chromium's smooth keyboard scroll follows the system's animation setting on Windows. Not measured, and Avalonia doesn't expose it. `AvonicConfig.Animated` doesn't turn it off, as Ionic's `animated` doesn't reach a browser's scrolling |
| On desktop, the same 4 px thumb, where a desktop Chromium has a 15 px bar that takes the width | md here is a phone's, as the reference is. #11 decided nothing takes room for a bar. Since #175, `ScrollBars` or `AvonicConfig.ContentScrollBars` gives the bar |

## A draggable scroll bar, by config (#175)

#67 gave every content md's phone thumb, and no bar to drag, on desktop too, as md here is a phone's. #175 keeps that as the default and
lets an app turn on a scroll bar, a desktop browser's: an **extension**, off by default, with no Ionic name. Measured in Chromium 141
(Playwright 1.56's headless shell) on Windows 11, **no emulation**, with `--hide-scrollbars` off, at 1× and 2×: an `ion-content` 390 × 300
in `ion-app`, with 30 `ion-item`s (1440 high), 30,000 of content, `scroll-x` with 1560 of width, and `dir="rtl"`. The script is in the
issue's temp folder, not the repo.

### What desktop Chromium draws

It's Chromium's Fluent scroll bar, Windows 11's, not the classic grey one.

| What | Measured |
|---|---|
| The bar | 15 thick, taking the width: `clientWidth` 375 of 390; with `scroll-x`, `clientHeight` 285 of 300 too. At the end edge, on the left in RTL, and along the bottom |
| The track | `#FCFCFC`, the whole side; the corner, 15 × 15, the same colour when both axes scroll |
| A button at each end | 18 long, an arrow in it: a triangle 9 across and 7 deep with rounded corners, 4.5 from the bar's end, `#8B8B8B`, and `#636363` while the pointer is over the button |
| The thumb | 9 thick, centred (3 from each side), with round ends, `#8B8B8B`, and `#636363` under the pointer and while it's dragged; the track doesn't change. Its length is the track's share that the viewport is of the extent, rounded down, at least 17; the track is the side less the two buttons: 55 at 18 for 1440 in 300, 17 for 30,000 |
| A drag of the thumb | the content follows the thumb: 50 down scrolls 273, 1140 × 50 / (264 − 55). 82 to the side still drags; **182 to the side snaps back** to where the drag began (Windows' own) |
| A click on an arrow | 40, eased over about 150 ms, as a key is |
| A click on the track | a page, 87.5% of the viewport rounded down, 262 of 300, eased the same way |
| Held | a step, then after about 250 ms a steady scroll: an arrow at 800 px/s, the track at 20 pages a second, **until the thumb reaches the pointer** (it stopped at 1048, the thumb from 210 to 265, held at 250) |
| A press on the bar | moves no focus: `document.activeElement` stays `body` |
| A dark Ionic page | not measured: Ionic's dark palettes set no `color-scheme`, which is what Chromium themes its bars by, so the bar stays light |

### Design

- **The switch:** `AvonicConfig.ContentScrollBars` (`bool`, false), and `AvonicContent.ScrollBars` (`bool`), a property per content, whose
  default the key gives. The property beats the key, as Ionic's props beat its config: one page, or one content, can have a bar in an app
  that has none. **Read in the constructor, at style priority,** as `toggleOnOffLabels` is (CLAUDE.md's config row: a prop's default is
  read when the control is made): so an app's local value or style wins, and a change of the key reaches contents made after it, which
  is when an app sets it, at start-up. A key read later, at each attach, would make a content moved between pages change its bar. Neither
  name is Ionic's; `ScrollBars` is plural, as there's one for each axis that scrolls.
- **What "on" draws:** Avalonia's `ScrollBar`, two of them, `PART_VerticalScrollBar` and `PART_HorizontalScrollBar`, in the content's
  `ScrollViewer` template, docked at the end edge and the bottom, **so they take the width,** as Chromium's do (docked, not in a base
  theme's `*,Auto` grid: measured with no height, as in a page's `ScrollViewer`, the grid kept the content's 2000 as the viewport when
  arranged at 300, which `B29` caught), and **instead of the
  indicator**, not beside it: a desktop browser draws one or the other, never both, and the bar shows the place all the time, which is
  what the thumb is for. The control makes the axes `Auto` where they were `Hidden` (`overflow: auto`, the bar only when there's
  somewhere to scroll); `ScrollY` off stays `Hidden`, with no bar, as `overflow: hidden` has none. Each indicator shows only where its
  axis is `Hidden` or `Disabled`, from the scroller's own visibility, so the two can't both draw. The corner is a part of its own that
  takes the vertical bar's `Background` and the horizontal one's height. The scroller's theme sets its vertical axis `Hidden`, so a
  `ScrollViewer` an app gives it keeps no bar, as before.
- **Other gestures:** a press on a `ScrollBar` starts no `Gesture` (`Gestures/Gesture.cs`), as Chromium sends a `mousedown` for a press
  on a bar and no moves while its thumb is dragged: measured, a drag of the thumb at the top of an `ion-content` scrolled it to 818
  and started no `ion-refresher` pull, where the same drag on the content pulled (`ionStart`, 18 `ionPull`s).
- **The bar's look** is a `ControlTheme`, `AvonicContentScrollBar`, structure in the stub's file and the look in the theme's
  `ContentScrollBar.axaml`, as Chromium's Fluent bar measured above. Its own theme, so that a base theme's `ScrollBar` style (Fluent's thin
  bar that widens under the pointer) doesn't reach it, and a plain `ScrollBar` elsewhere in the app keeps the base theme's. Colours are
  tokens: `Avonic.Content.ScrollBarTrackBrush`, `…ThumbBrush`, `…ThumbHoverBrush`. Sizes are literals in the theme (15, 18, 9, 17), as
  Chromium has no variable for them. The thumb takes the pointer across the bar's whole thickness, and is drawn 9 thick.
- **Dragging is Avalonia's** `Track` and `Thumb`: the offset follows the thumb in proportion, as measured.
- **The arrows and the track scroll as Chromium does, not as Avalonia's `ScrollBar`,** which jumps by 16 and a whole viewport: their
  `RepeatButton`s are named `PART_LineUp`, `PART_LineDown`, `PART_PageUp` and `PART_PageDown`, not Avalonia's `PART_LineUpButton` and the
  rest, so the `ScrollBar` leaves them alone, and the content takes their `Click`: an arrow 40, the track 87.5% of the viewport along the
  bar, rounded down, both through the keys' curve (`ContentKeyboardScroll`, 150 ms of `cubic-bezier(0.42, 0, 0.58, 1)`, a step during one
  adding to its target), as Chromium eases a click as it eases a key. They click on the press (`ClickMode="Press"`), as Chromium scrolls on
  `mousedown`, and repeat after 250 ms every 50 ms, which gives Chromium's 800 px/s and 20 pages a second. A repeat of the track stops once
  the thumb covers the pointer, as Chromium's does; the first click always pages.
- **Focus:** the bar and its buttons aren't focusable. Measured: a press on the bar moves no focus (a focused field keeps it, and Page
  Down in it still scrolls the content, 262), and with nothing focused the keys scroll the content after it (Down, 40). So a press on
  the bar keeps the focus where it is (`KeepFocus`), and with nothing focused the scroller takes it, as from a press on the content
  since #67, with no ring, so the keys scroll it.

### Behaviours

Tests in `AvonicContentScrollBarTests` (S1 to S9), named after these numbers.

| # | Behaviour | Measured |
|---|---|---|
| S1 | Off by default: no bar, the indicator, nothing takes the width | #67's |
| S2 | `ScrollBars` on: a vertical bar 15 thick at the end edge that takes the width, the left in RTL; a horizontal one with `ScrollX`, and the corner | `v`, `both`, `rtl` |
| S3 | Only where it can scroll, and none with `ScrollY` off | `overflow: auto`, `overflow: hidden` |
| S4 | `AvonicConfig.ContentScrollBars` is the default for contents made after it; a local value and a style beat it | not Ionic's |
| S5 | The indicator never shows with a bar | — |
| S6 | Dragging the thumb scrolls in proportion | `drag 50` |
| S7 | An arrow scrolls 40 and the track a page, on the press, eased as a key, and a step during one adds to it | `arrow`, `track` |
| S8 | Held, they repeat; the track stops when the thumb reaches the pointer | `held` |
| S9 | The look: 18 buttons, a 9 thick thumb at least 17 long, the colours, the hover | `v`, `long`, `hover` |

### Deliberate differences

| Difference | Why |
|---|---|
| A thumb dragged far to the side doesn't snap back | Windows' own behaviour, which Chromium copies on Windows only: Avalonia's `Thumb` has none, and an app on another OS wouldn't expect it |
| Held, an arrow or the track steps every 50 ms, each eased, where Chromium scrolls at a steady speed | The same speeds, from Avalonia's `RepeatButton`; the steps on the track are a page each, so the last can carry the thumb past the pointer by up to a page's worth |
| The thumb's length and place aren't rounded down to a pixel | Avalonia's `Track` places it. Under a pixel |
| With nothing focused, a press on the bar focuses the scroller | As a press on the content does, for the keys (#67's difference). No ring shows. With something focused, it keeps it, as measured |
| It's on for desktop and phone alike, by the app's choice | Chromium draws this bar on desktop and the thumb on a phone; Avonic can't tell which an app is for, so the app says |

## Not in this PR

| What | Why, and where it goes |
|---|---|
| `role` off inside `ion-menu`, `ion-popover`, `ion-modal` | Those controls don't exist. Each can turn it off with a style: `c\|AvonicModal c\|AvonicContent { AutomationProperties.LandmarkType: null }` |
| `.content-sizing` inside `ion-popover`, and `recalculateDimensions`, `getBackgroundElement` | #5's overlays. A popover's content sizes to its content, so it needs the `contain: size` in `MeasureOverride` turned off |
| `--internal-content-safe-area-padding-bottom`, added to the scroll part's bottom padding in 8.8.19's `content.css` | Only `ion-modal` sets it (`applyFullscreenSafeAreaTo`): `var(--ion-safe-area-bottom, 0px)` on its content, in a modal that isn't a sheet or a card and has no footer, so the content scrolls clear of the system's bar. 0 everywhere else, as here. The modal (#42) adds it |
| A popover's exclusion from 8.8.19's fullscreen `ResizeObserver` | No popover yet; `contain: none` there would feed the offsets back into the host's size. The popover (#43) adds it |
| `ion-tabs` as the page, and `ionTabBarLoaded` | The tabs control. Until then, a class `ion-page` on the tabs' host does it |
| `ion-app` as the page | No such control. Measured: it would win over a plain parent |
| `.transition-effect`, `.content-ltr`, `.content-rtl` | iOS only: `transitionShadow = mode === 'ios'` |
| `.ion-padding` and friends | Not planned, per the colour system's plan. `Padding` on the control does it |
| `--keyboard-offset` set by a keyboard | `scroll-assist`, with the inputs. A per-instance `Resources` value is the way in |
| The default text size inside the content: Avalonia's, where a web page's is 16 | It affects everything. An open project question, so left |

## Dependencies

None on unbuilt controls. `ion-item` is only used by a test, for tap-through.
