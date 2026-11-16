# Plan: ion-app → AvonicApp (#65)

`ion-app` is the root of an Ionic app. It has no props, slots, parts or CSS variables, and one method. What it does is start
Ionic's mobile utilities when it loads, and a native Avalonia app needs those too. ADR 0002 brought it into scope for exactly
that: hardware back, keyboard assist, status tap and `setFocus`. **An app doesn't need it to get working controls**: every control
still works without it, and an overlay still dismisses on back.

Sources, `@ionic/core` 8.8.1, `dist/collection/`: `components/app/app.js` and `app.css`; `utils/hardware-back-button.js`;
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
| `startStatusTap` | `statusTap`, `isHybrid` | `StatusTap`, started for the app's `TopLevel`. `AvonicConfig.StatusTap`, default true |
| `startInputShims` | `inputShims`, `needInputShims()`: iOS, or Android in a browser | See [input shims](#input-shims) |
| `startHardwareBackButton`, or `blockHardwareBackButton` | `hardwareBackButton`, `isHybrid \|\| closeWatcher` | `HardwareBackButton`, started for the app's `TopLevel`. `AvonicConfig.HardwareBackButton`, default true |
| `startKeyboardAssist` | always | `KeyboardAssist`, started for the app's `TopLevel` |
| `startFocusVisible` | always | Already each control's, through `Behaviors.FocusVisible`. `SetFocus` reaches it |

`render()` gives the host the classes `md`, `ion-page` and `force-statusbar-padding` (from the private `_forceStatusbarPadding`
config, which sets `--ion-safe-area-top: 20px`). `app.css` adds `user-select: none` on a mobile platform.

## Measured

Real Ionic 8.8.1, md, headless Chromium (Playwright 1.56's), 390 x 844 at 2×, from a throwaway script outside the repo. Capacitor's
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
- B3 The back request is handled only when something registers, so that with nothing registered Android leaves the app, as an
  app with no handler would.
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

**S: the status bar tap** (`StatusTap`, Ionic's `startStatusTap`)

- S1 `StatusTapEvent` on the `TopLevel`: in the next frame, the content under the middle of the window has its scrolling held off,
  and scrolls to the top in 300 ms. Then its own `ScrollY` comes back (M4).
- S2 Nothing when the middle of the window isn't in a content (M4b).
- S3 Nothing with `AvonicConfig.StatusTap` off, or without an `AvonicApp`.

**F: `SetFocus`** (Ionic's `focusVisible.setFocus`)

- F1 It shows `:ion-focused` on exactly the elements given, and takes it off everything else in the `TopLevel` (M6).
- F2 A key in `FOCUS_KEYS` keeps it. Any other key, or a press, clears it (M6).
- F3 Focus moved by code keeps it. Focus moved by keyboard, or focus leaving everything, clears it (M6).
- F4 Given the element that already has keyboard focus, it lasts until focus moves on.

**P: the page**

- P1 It has the class `ion-page`, fills what it's in, and doesn't clip (H). `AvonicContent` already takes the closest `.ion-page`
  as its page (`getPageElement`: `ion-app, ion-page, .ion-page`), so nothing in the content changes.

## How it's built

| Piece | Where | What |
|---|---|---|
| `AvonicApp` | `Controls/App/AvonicApp.cs`, `.axaml` | A `ContentControl`, class `ion-page`. On attach it starts the utilities for its `TopLevel`, as its config says then; on detach it stops them. `SetFocus` |
| `HardwareBackButton` | `Controls/App/HardwareBackButton.cs` | `IonBackButtonEvent` (direct, on the `TopLevel`), `BackButtonEventArgs.Register(priority, handler)`, `OverlayBackButtonPriority` 100, `MenuBackButtonPriority` 99. It listens to `TopLevel.BackRequested`, which Avalonia raises for Android's back, a mouse's back button and Alt+Left |
| `KeyboardAssist` | `Controls/App/KeyboardAssist.cs` | `IonKeyboardDidShowEvent` (`KeyboardDidShowEventArgs.KeyboardHeight`), `IonKeyboardDidHideEvent`, from `TopLevel.InputPane` |
| `StatusTap` | `Controls/App/StatusTap.cs` | `StatusTapEvent`, for the app to raise |
| `FocusVisible.SetFocus` | `Behaviors/FocusVisible.cs`, internal | Ionic's `setFocus`, beside the per-surface code that's there |
| The overlays | `Overlays/OverlayHost.cs` | They register at 100 through `IonBackButtonEvent`, and handle `BackRequested` directly only when no app has started the registry for the `TopLevel` |

The three utilities are started per `TopLevel` and counted, so two apps in one window, or an app moved to another window, work.
Ionic starts them once and never stops them, because `ion-app` stays for the page's whole life. Here an app can be taken out, so
they stop. Ionic waits for an idle callback before it starts. Here they start on attach, as Avalonia has no idle callback with
the same meaning, and a back press during load would otherwise go unanswered.

### Input shims

Ionic turns them on for iOS, and for Android in Chrome, where since Chrome 108 the keyboard covers the page instead of resizing
it. In a Capacitor or Cordova Android app the webview resizes, so they're off. The question for Avalonia is which of those it is
like. Avalonia 12 on Android 15 and later forces edge to edge, its `SafeAreaPadding` leaves out the IME, and nothing resizes the
view for the keyboard. **To be measured on the Pixel** before deciding: see COMPARISON.md.

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
| Root `aria-hidden` while an overlay is up (`setRootAriaHidden`) | It's the overlays', recorded in `docs/infra/overlays/PLAN.md` |

## Decided before, and asked again

ADR 0002 decided three things while `ion-app` was out of scope. The issue asks for them to be asked again now that it's in.
The answers are under "For you to decide" in the PR, and this section records what was built.

| Question | Before | Recommended now |
|---|---|---|
| D1 Default text size | Avalonia's | `AvonicApp`'s theme gives 16, Avonic's font, text colour and background to what's inside it, at style priority, as `html` and `body` do for an Ionic page (H: 16px) |
| D2 Typography | Opt in | Opt in, as Ionic's `typography.css` is a file the app imports |
| D3 `Avonic.SafeArea*` | The app fills them | `AvonicApp` fills them in its `TopLevel`'s resources from `InsetsManager`, when the root isn't padded by Avalonia (`AutoSafeAreaPadding` false: Avalonia's `viewport-fit=cover`), and leaves them 0 when it is |

## Avonic extensions

None.

## Dependencies

`AvonicContent` (#11) for the status tap's scroll and the page; the overlay system (#5) for back; `FocusVisible` (#1). All exist.
