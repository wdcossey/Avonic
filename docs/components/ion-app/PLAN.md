# Plan: ion-app → AvonicApp (#65)

`ion-app` is the root of an Ionic app. It has no props, slots, parts or CSS variables, and one method. What it does is start
Ionic's mobile utilities when it loads, and a native Avalonia app needs those too. ADR 0002 brought it into scope for exactly
that: hardware back, keyboard assist, status tap and `setFocus`. The status tap is left for an issue of its own, to be tried on an
iPhone, as only iOS has one. **An app doesn't need it to get working controls**: every control
still works without it, and an overlay still dismisses on back.

Sources, `@ionic/core` 8.8.19, `dist/collection/`: `components/app/app.js` and `app.css`; `utils/hardware-back-button.js`;
`utils/keyboard/keyboard.js` and `keyboard-controller.js`; `utils/status-tap.js`; `utils/focus-visible.js`; `utils/input-shims/`
(`input-shims.js`, `hacks/scroll-assist.js`, `scroll-data.js`, `scroll-padding.js`, `common.js`, `hide-caret.js`, `input-blurring.js`);
`utils/helpers.js` (`focusVisibleElement`); `utils/overlays.js` and `utils/menu-controller/index.js` for who registers at what priority;
`css/core.css`, `structure.css` and `typography.css` for what a page gets from `html` and `body`.

## The checklist

From `node tools/ionic-api.js ion-app`:

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `setFocus(elements: HTMLElement[]) => Promise<void>` | `SetFocus(IEnumerable<Control>)`, `SetFocus(params Control[])` | Synchronous, as nothing in it waits |

There are no props, events, slots, shadow parts or CSS custom properties.

## What `ion-app` does

`componentDidLoad` waits for `requestIdleCallback` (or 32 ms), and then:

| Ionic | Config, and its default | In Avonic |
|---|---|---|
| `startTapClick` | unless `_testing` | Already each control's, through `Behaviors.Activatable`. Unchanged |
| `startStatusTap` | `statusTap`, `isHybrid` | Not in this PR: an issue of its own. Avalonia doesn't report a tap on the status bar, and only iOS has one |
| `startInputShims` | `inputShims`, `needInputShims()`: iOS, or Android in a browser | `InputShims`. `AvonicConfig.InputShims`, default on Android, iOS and, since #189, Windows: see [input shims](#input-shims) and [on Windows](#input-shims-on-windows-189) |
| `startHardwareBackButton`, or `blockHardwareBackButton` | `hardwareBackButton`, `isHybrid \|\| closeWatcher` | `HardwareBackButton`, started for the app's `TopLevel`. `AvonicConfig.HardwareBackButton`, default true |
| `startKeyboardAssist` | always | `KeyboardAssist`, started for the app's `TopLevel` |
| `startFocusVisible` | always | Already each control's, through `Behaviors.FocusVisible`. `SetFocus` reaches it |

`render()` gives the host the classes `md`, `ion-page` and `force-statusbar-padding` (from the private `_forceStatusbarPadding`
config, which sets `--ion-safe-area-top: 20px`). `app.css` adds `user-select: none` on a mobile platform.

## Measured

Real Ionic 8.8.19, md, headless Chromium (Playwright 1.56's), 390 x 844 at 2×, from a throwaway script outside the repo. Capacitor's
events were dispatched by hand: `backbutton` on the document, `statusTap` on the window, and `keyboardDidShow` and `keyboardDidHide`
on the window with a fake Capacitor Keyboard plugin, so that keyboard assist takes its native path.

| | Case | Result |
|---|---|---|
| H | The host | Classes `md ion-page hydrated`. `position: absolute`, inset 0, `display: flex`, `flex-direction: column`, `overflow: visible`, `contain: size layout style`, `z-index: 0`, `user-select: none`, `font-size: 16px` (the browser's default) |
| M1 | `hardwareBackButton: true`; handlers at 0, 100, 100, 99, -1 | The second 100 runs. It calls `next()`, and the first 100 runs, which doesn't call it, so it stops there |
| M2 | A handler returning a 300 ms promise; back at 0, 100 and 400 ms | It runs at 0 and at 419. The press at 100 does nothing |
| M2b | A handler that throws | The next press still runs its handler |
| M2c | The `ionBackButton` event | `bubbles: false`, `cancelable: false`, dispatched on the document |
| M2d | A handler at 5 that calls `next()` twice, then handlers at 4 and 3 | 5, 4, 3: each call runs one more |
| M3 | The browser's default config | `backbutton` raises no `ionBackButton`. `statusTap` leaves the content at 500 |
| M3b | `hardwareBackButton: true` and no `ion-app` | No `ionBackButton` |
| M4 | `statusTap`, the content at 600 | `--overflow: hidden` from the first frame (10 ms) until about 320 ms. The scroll starts in the frame after (from 600 at 27 ms, 504 at 43), and reaches 0 at 293 ms: `scrollToTop(300)` |
| M4b | `statusTap` with a transparent fixed element over the middle of the window | Nothing scrolls |
| M5 | `keyboardDidShow` (300), `keyboardDidShow` (320), `keyboardDidHide` twice | `ionKeyboardDidShow {keyboardHeight: 300}`, `{keyboardHeight: 320}`, `ionKeyboardDidHide` twice: each passed on as it comes, with no `bubbles` |
| M6 | `setFocus` | `[a, b]`: both `ion-focused`. `[cc]`: only `cc`. A Tab keydown keeps it, an `a` keydown clears it, as do `mousedown` and `touchstart`. After a touch, a scripted `b1.focus()` keeps `a`; after a Tab keydown, focus on `b1` clears `a` and gives `b1` its own. A blur to the body clears it. `[]` clears everything |
| M7 | `hardwareBackButton: false` | No `ionBackButton` |
| M8 | An alert (`backdropDismiss`), and an app's handler at 0 | Back dismisses the alert with `backdrop`, and the app's handler doesn't run. With an alert that has `backdropDismiss: false`, back runs the app's handler and the alert stays |

## Behaviours

Each becomes a test in `AvonicAppTests`, named after its number.

**B: the hardware back button** (`HardwareBackButton`, Ionic's `utils/hardware-back-button`)

- B1 For each back request, `IonBackButtonEvent` goes out on the `TopLevel`. Of what's registered, the highest priority runs, and
  of equal ones the last registered. `next` runs the next highest, and each call of it runs one more (M1, M2d).
- B2 While a handler's task is still running, a back press does nothing. A handler that throws doesn't leave it busy (M2, M2b).
- B3 The back request is handled even when nothing registers, so that Android stays in the app: Ionic's docs say "if no previous
  view exists in the navigation stack, nothing will happen", as Capacitor's App plugin takes every back press, and an app exits
  through its own handler at -1. The extension `AvonicConfig.HandleUnclaimedBack` off leaves such a request to the platform.
- B4 Without an `AvonicApp`, nothing is raised (M3b). With one, an overlay registers at 100 to dismiss with `backdrop`, over the
  app's own 0. One that can't be dismissed registers nothing, and the app's handler runs (M8).
- B5 `AvonicConfig.HardwareBackButton` off: nothing is raised, and an overlay stays (M7).
- B6 An app taken out of the tree stops raising it.
- B7 A back request the app has already handled is left alone.
- B8 On desktop, Avalonia's back gesture (Alt+Left) is a back press.

**K: keyboard assist** (`KeyboardAssist`, Ionic's `startKeyboardAssist`, native path)

- K1 Each time the input pane opens, `IonKeyboardDidShowEvent` with the height it covers, and each time it closes,
  `IonKeyboardDidHideEvent`, both on the `TopLevel`, once the pane's animation has finished (M5).
- K2 A change before the last one has finished replaces it.
- K3 Without an `AvonicApp`, nothing is raised.

**F: `SetFocus`** (Ionic's `focusVisible.setFocus`)

- F1 It shows `:ion-focused` on exactly the elements given, and takes it off everything else in the `TopLevel` (M6).
- F2 A key in `FOCUS_KEYS` keeps it. Any other key, or a press, clears it (M6).
- F3 Focus moved by code keeps it. Focus moved by keyboard, or focus leaving everything, clears it (M6).
- F4 Given the element that already has keyboard focus, it lasts until focus moves on.

**T: what a page gets from `html` and `body`** (decided by the user, D1)

- T1 What's in the app has a font size of 16, Avonic's font family and text colour, on Avonic's background, from the theme, so
  an app's own style wins. Outside it nothing changes.

**A: the safe area** (decided by the user, D3)

- A1 While the root isn't padded by Avalonia (`TopLevel.AutoSafeAreaPadding` off, Avalonia's `viewport-fit=cover`), the four
  `Avonic.SafeArea*` keys are the insets, in the `TopLevel`'s resources, so that the overlay layer has them too, and they follow
  `SafeAreaChanged`.
- A2 While Avalonia pads the root, they're left to the theme's 0, and they follow the property turning off and on.
- A3 An app taken out takes them away.

**P: the page**

- P1 It has the class `ion-page`, fills what it's in, and doesn't clip (H). `AvonicContent` already takes the closest `.ion-page`
  as its page (`getPageElement`: `ion-app, ion-page, .ion-page`), so nothing in the content changes.

## How it's built

| Piece | Where | What |
|---|---|---|
| `AvonicApp` | `Controls/App/AvonicApp.cs`, `.axaml` | A `ContentControl`, class `ion-page`. On attach it starts the utilities for its `TopLevel`, as its config says then; on detach it stops them. `SetFocus` |
| `HardwareBackButton` | `Controls/App/HardwareBackButton.cs` | `IonBackButtonEvent` (direct, on the `TopLevel`), `BackButtonEventArgs.Register(priority, handler)`, `OverlayBackButtonPriority` 100, `MenuBackButtonPriority` 99. It listens to `TopLevel.BackRequested`, which Avalonia raises for Android's back, a mouse's back button and Alt+Left |
| `KeyboardAssist` | `Controls/App/KeyboardAssist.cs` | `IonKeyboardDidShowEvent` (`KeyboardDidShowEventArgs.KeyboardHeight`), `IonKeyboardDidHideEvent`, from `TopLevel.InputPane` |
| `AppSafeArea` | `Controls/App/AppSafeArea.cs`, internal | The four keys, from `TopLevel.InsetsManager` |
| `InputShims` | `Controls/App/InputShims.cs`, internal | Scroll assist and scroll padding |
| `TopLevelStarts` | `Controls/App/TopLevelStarts.cs`, internal | One start count per `TopLevel`, for each utility |
| The theme | `Avonic.Themes/Controls/AvonicApp.axaml` | Font size 16, `Avonic.FontFamily`, `Avonic.TextColorBrush`, `Avonic.BackgroundColorBrush` |
| `FocusVisible.SetFocus` | `Behaviors/FocusVisible.cs`, internal | Ionic's `setFocus`, beside the per-surface code that's there |
| The overlays | `Overlays/OverlayHost.cs` | They register at 100 through `IonBackButtonEvent`, and handle `BackRequested` directly only when no app has started the registry for the `TopLevel` |

The three utilities are started per `TopLevel` and counted, so two apps in one window, or an app moved to another window, work.
Ionic starts them once and never stops them, because `ion-app` stays for the page's whole life. Here an app can be taken out, so
they stop. Ionic waits for an idle callback before it starts. Here they start on attach, as Avalonia has no idle callback with
the same meaning, and a back press during load would otherwise go unanswered.

### Input shims

Ionic turns them on for iOS, and for Android in Chrome, where since Chrome 108 the keyboard covers the page instead of resizing
it. In a Capacitor or Cordova Android app the webview resizes, so they're off. **Measured on the Pixel 8:** Avalonia 12 on
Android 15 and later draws edge to edge, and the keyboard covers the view, which doesn't resize and doesn't scroll: the lowest
field of the `ion-input` page ended under it ([frame](compare/pixel8-input-covered.png)). That's Chrome's case, so they're on by
default on Android and iOS (the user's answer, D8), and off on a desktop.

`InputShims` does `scroll-assist` and `scroll-padding` for an `AvonicInput` or `AvonicTextarea` in an `AvonicContent`, as Ionic
registers `ion-input` and `ion-textarea`. Measured in Chromium with a Pixel's user agent, which makes Ionic's `needInputShims()`
true (`plt-android plt-mobile plt-mobileweb`), on a page with a 56 header and the field's item at 584 to 632:

| | Case | Result |
|---|---|---|
| S1 | Focus, room to scroll (scrollHeight 1104) | `--keyboard-offset: 290px` at once; `scrollTop` 15 in the next frame, 128 by 408 ms: `calcScrollData`'s 632 − (844 − 290 − 50), over min(400, 128 / 0.3) |
| S2 | Blur | `0px` about 120 ms later |
| S3 | Focus, no room (scrollHeight 788, the padding gives 78) | It waits for `ionKeyboardDidShow`, or 1000 ms, then scrolls as far as it can: 78 |

Behaviours, in `AvonicAppTests`:

- I1 A field the keyboard would cover pads the content by the keyboard's height (`AvonicContent.KeyboardOffset`, at animation
  priority, as an inline `--keyboard-offset`) and scrolls it so that the item is 50 above the keyboard, over Ionic's duration;
  the padding goes 120 ms after the field loses focus (S1, S2).
- I2 With too little room, it waits for the keyboard to show, or a second (S3).
- I3 A field already clear of the keyboard neither pads nor scrolls.
- I4 Nothing with `AvonicConfig.InputShims` off, or without an `AvonicApp`.
- I5 A later keyboard show runs it again with the keyboard's own height, without waiting.
- I6 `ScrollAssist`, `ScrollPadding` and `KeyboardHeight` come from the config, read when the app is attached, as Ionic reads them
  in `startInputShims`.
- I7 A password field with too little room waits for a second keyboard show (iOS's passwords bar).
- I8 **A deviation:** once the keyboard says its real height, if that isn't what the assist reckoned with, it runs again with it
  when its scroll ends. Ionic keeps its estimate, as Chrome then scrolls the focused field into the visual viewport itself.
  Avalonia doesn't, and on the Pixel the keyboard is about 355, so 290 left the field under it.

Not done: moving the native input away and focusing it again (`relocateInput`), which stops a browser scrolling the page itself,
as Avalonia doesn't, and so neither the click 8.8.1 fired on the component a frame after it, which 8.8.19 no longer fires; hide caret on scroll (WebKit draws the caret over a scrolling page); input blurring (off since Ionic 8, and
going); a footer's field, for which Ionic only relocates.

### Input shims on Windows (#189)

The user asked for the shims on Windows tablets too, where the touch keyboard covers the window as a phone's covers the view
(decided 2026-11-16). Ionic has nothing to copy here: `needInputShims()` is false in every desktop browser. What Avalonia gives:

**Avalonia 12.1.4 on Windows, read from its source** (`Avalonia.Win32.dll`, decompiled with ilspycmd 11.2):

- `WindowImpl`'s constructor calls `WindowsInputPane.TryCreate(this)`, which makes a pane when
  `WinRTApiInformation.IsTypePresent("Windows.UI.ViewManagement.InputPane")`, so on Windows 10 and later, for **every** window,
  touch or not. `TryGetFeature(typeof(IInputPane))` returns it, so `TopLevel.InputPane` is never null there.
- It creates Windows' `FrameworkInputPane` (`IFrameworkInputPane`) and calls `AdviseWithHWND` with the window's handle. Its handler's
  `Showing(RECT*)` sets `State` to `Open` and `OccludedRect` to the keyboard's place on the screen, converted to the window's
  client coordinates (`PointToClient` for the corner, the size divided by `DesktopScaling`); `Hiding` sets `Closed` and an empty
  rect. `StateChanged` is raised, through `Dispatcher.UIThread.Send`, only when the state or the rect changed, with no start
  rect and no animation (`AnimationDuration` zero), so `IonKeyboardDidShow` comes at once.
- Nothing in `Avalonia.Win32` opens the touch keyboard (no `TabTip`, `ITipInvocation` or `TryShow`): Windows does, from UI
  Automation focus on an edit control, for a touch focus in tablet posture or as its touch keyboard setting says.
- The rect is the keyboard's, wherever the window is: a window that ends above the bottom of the screen is covered by part of it,
  or none. Android's, by contrast, is always `(0, client height − bottom inset − ime, client width, ime)`, inside the view.

So "a Windows tablet" can't be told from the pane's being there: every window has one. It can be told from the pane opening over
the window, which happens only when the touch keyboard docks. The design follows that:

- `AvonicConfig.InputShims` defaults to on for Android, iOS and Windows (`InputShimsByDefault`); off for macOS and Linux, which
  have no pane.
- Where the keyboard isn't the only one (not Android or iOS: `AvonicConfig.KeyboardIsTheOnlyOne`), and the top level has a pane
  or it's Windows (a hosted Windows top level has none), the shims **follow the pane**: a field's focus records the field and does
  nothing more; the pane opening runs Ionic's `jsSetFocus` with the height it covers and the window's height then, without
  waiting; a field focused while it's open runs at once with that height, without waiting for a show that won't come; the pane
  closing takes the padding away 120 ms later, and the next opening runs again. Android and iOS keep Ionic's estimate at focus,
  and macOS and Linux, with no pane, get Ionic's too where an app turns the shims on, as a desktop browser with
  `inputShims: true` would.
- `KeyboardAssist`'s height is what the pane covers of the top level (`CoveredHeight`, the rect's overlap with the client area),
  as its doc already said; the same as before on a phone. A pane with a rect that covers none of the top level is reported as
  hidden; an empty rect is left as it was.

Behaviours, in `AvonicAppTests`:

- W1 The default is on for Android, iOS and Windows, and off for Linux, macOS and others.
- W2 On Windows, a field's focus neither pads nor scrolls; the pane opening pads by its height and scrolls the item 50 above it.
- W3 A field focused with the pane already open runs at once with its height, also when nothing had focus before it opened.
- W4 The keyboard's height is what it covers of the top level: 244 of a 400 high keyboard from 600 in an 844 high window; one
  over none of it is reported as hidden.
- W5 A touch keyboard below the window changes nothing.
- W6 On a phone with a pane, focus still reckons with the estimate at once.
- W7 Each focus runs when the pane opens, even at the height the last one used.
- W8 On Windows with no pane, focus does nothing.
- W9 **A deviation:** the padding goes 120 ms after the keyboard closes or ends off the window, with the field still focused,
  and the next opening runs again. Ionic's stays until blur, as a phone's keyboard closes with the focus.
- W10 A field focused with the pane open and too little room scrolls as far as it can at once, without the second's wait.
- W11 The window's height is read when the keyboard opens, not at the focus.

W3 found a bug the change brought into reach: with the keyboard open before any field had focus, the page's height was never
taken, and the field scrolled to the top of the content (528 where 138 was right). On a phone the keyboard comes with focus, so
it couldn't happen there; on Windows the touch keyboard can be opened from the taskbar first.

## Not in this PR

| What | Why |
|---|---|
| `force-statusbar-padding` | It's the private `_forceStatusbarPadding` config, which only Ionic's own tests set |
| `user-select: none` on a mobile platform | Avalonia's `TextBlock` isn't selectable, and `SelectableTextBlock` is the app's choice |
| `md` as a class | There is only md (ADR 0001) |
| The idle callback before starting | Avalonia has none with the same meaning; it starts on attach |
| `experimentalCloseWatcher` | A browser's API. Avalonia's `BackRequested` already covers the desktop's back gestures |
| `blockHardwareBackButton` | With the config off, the back request is left to the app and the platform, as swallowing it would also hide it from the app's own handlers |
| The visual-viewport path of keyboard assist | It's for a browser with no native keyboard plugin. An Avalonia app has the input pane, which is the native path |
| Tap-click and focus-visible | Already each control's, through `Activatable` and `FocusVisible` |
| Ionic's router at priority 0 | No router. The sample's shell takes back last, through `BackRequested`'s `RouteFinished` |
| The status bar tap | Its own issue (D9) |
| Root `aria-hidden` while an overlay is up (`setRootAriaHidden`) | It's the overlays', recorded in `docs/infra/overlays/PLAN.md` |

## Decided before, and asked again

ADR 0002 decided three things while `ion-app` was out of scope, and the issue asked for them to be asked again. The user's
answers, 2026-11-16:

| Question | Before | Now |
|---|---|---|
| D1 Default text size | Avalonia's | Inside `AvonicApp`, 16, Avonic's font, text colour and background, from its theme, as `html` and `body` give an Ionic page (H: 16px). Outside it, Avalonia's |
| D2 Typography | Opt in | Opt in, as Ionic's `typography.css` is a file the app imports |
| D3 `Avonic.SafeArea*` | The app fills them | `AvonicApp` fills them while the root isn't padded by Avalonia |

And the other choices that change what every app sees:

| Question | Answer |
|---|---|
| D4 `HardwareBackButton` by default | On everywhere, as in a Capacitor app. On a desktop it's the mouse's back button and Alt+Left |
| D5 Back without `AvonicApp` | Overlays still dismiss on it. Registering handlers needs an `AvonicApp`, as `ionBackButton` needs `ion-app` |
| D6 `HardwareBackButton` off | Back is left alone. A deviation: Ionic's `blockHardwareBackButton` takes it and does nothing, which here would also hide it from the app's own `BackRequested` handlers |
| D7 A back request nothing takes | Handled, so the app stays, as in Ionic on Capacitor. An extension, `AvonicConfig.HandleUnclaimedBack`, turns that off |
| D8 Input shims | On for Android and iOS, off on desktop: the Pixel showed the keyboard covering the view. Since #189, on for Windows too, following the touch keyboard |
| D9 Status tap | An issue of its own |

## Avonic extensions

| Extension | Why |
|---|---|
| `AvonicConfig.HandleUnclaimedBack`, default true | Whether a back request nothing registers for is still handled. Ionic's is, always, as Capacitor's App plugin takes every press; an Avalonia app may want Android's own back, to leave the app, without writing a handler |

## Dependencies

`AvonicContent` (#11) for the page; the overlay system (#5) for back; `FocusVisible` (#1). All exist.
