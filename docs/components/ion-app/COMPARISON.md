# Comparison: ion-app → AvonicApp

> Reference frame, from the worktree's root:
>
> ```
> node tools/ionic-ref/render.js ion-app --bare --html '<ion-app><ion-header><ion-toolbar><ion-title>ion-app</ion-title></ion-toolbar></ion-header><ion-content><div style="padding:16px">Plain text in ion-app is 16px Roboto, in the text colour.</div><ion-item><ion-label>An item</ion-label></ion-item></ion-content></ion-app>' --out docs/components/ion-app/ref
> ```
>
> The behaviours were measured with a throwaway Playwright script outside the repo, in Chromium (Playwright 1.56's), Ionic 8.8.1,
> md, 390 x 844 at 2×, with Capacitor's events dispatched by hand: see [PLAN.md](PLAN.md#measured).

## The frame

Ionic on the left, Avonic on the right (headless, `AvonicAppCaptureTests.Reference_Page`), at 2×:

<img src="compare/side-page.png" alt="ion-app beside AvonicApp: a header, plain text and an item" width="640">

The plain text is 16 Roboto in the text colour on both, from `html` and `body` in Ionic and from `AvonicApp`'s theme here, and it
wraps at the same word. Without an `AvonicApp`, Avalonia's 14 would apply.

## Behaviours

The numbers are from [PLAN.md](PLAN.md#behaviours). Each has tests in `AvonicAppTests`, named after its number.

| | Behaviour | Ionic | Avonic | |
|---|---|---|---|---|
| B1 | Highest priority, ties to the last registered, each `next()` runs one more | M1, M2d | the same | match |
| B2 | Busy while a handler's promise runs; a throw doesn't stick | M2, M2b | the same, with a `Task` | match |
| B3 | A back press nothing takes | Capacitor's App plugin takes it: "nothing will happen" | handled; `HandleUnclaimedBack` off leaves it to Android | match, with an extension |
| B4 | Without `ion-app`, nothing; with it, an overlay at 100 over the app's 0, and none when it can't be dismissed | M3b, M8 | the same; an overlay still dismisses on back without an app | match, and a deliberate addition (D5) |
| B5 | `hardwareBackButton: false` | no `ionBackButton`, and back is swallowed (`blockHardwareBackButton`) | nothing raised, overlays stay, and back is left alone | deliberate difference (D6) |
| B6 | Taken out of the page | never happens in Ionic | stops | deliberate: Avalonia apps swap roots |
| B7 | A back request the app already handled | n/a: the document's event | left alone | Avalonia's routing |
| B8 | A desktop's back | the browser's, not `backbutton` | Alt+Left and the mouse's back button, as Avalonia raises `BackRequested` for them | deliberate (D4) |
| K1 | `ionKeyboardDidShow {keyboardHeight}` and `ionKeyboardDidHide`, each as it comes | M5 | the same, once the input pane's animation has finished | match |
| K2 | A change before the last has finished | each passed on | the newer replaces it | deliberate: Avalonia reports at the animation's start, Capacitor at its end |
| T1 | Text in the app | 16px, Roboto, the text colour, on the background (H) | the same, from the theme | match (D1) |
| A1–A3 | The safe area | `env(safe-area-inset-*)` with `viewport-fit=cover` | the keys while `AutoSafeAreaPadding` is off | match (D3) |
| F1–F5 | `setFocus` | M6 | the same | match |
| P1 | `.ion-page`, filling, not clipping | H | the same | match |
| I1–I7 | Scroll assist and scroll padding | S1–S3 in [PLAN.md](PLAN.md#input-shims), with a Pixel's user agent | the same numbers: 290 at once, 128 by about 400 ms, 0 120 ms after blur, a second's wait without room | match |
| I8 | The keyboard's own height | Ionic keeps its 290; Chrome scrolls the focused field into the visual viewport itself | the assist runs again with it when its scroll ends | deliberate difference: Avalonia doesn't scroll a focused field above the keyboard (measured below) |
| I9 | The page's height | `innerHeight`, above the navigation bar | the client height less the bottom inset, as the keyboard's height is above the bar | match |

## On the Pixel

A Google Pixel 8, Android 17, 1080 × 2400 at density 420 (2.625×), on 2026-11-16, with the Debug APK. Input over ADB.

| Check | Result |
|---|---|
| Does the keyboard cover a field? | **Yes.** On the `ion-input` page, which has no `AvonicApp`, the lowest field ended under the keyboard: the view doesn't resize and nothing scrolls ([frame](compare/pixel8-input-covered.png)). So the input shims are needed (D8) |
| The input shims | With Ionic's 290 alone, the field moved up but stayed partly under the keyboard, which is about 355. With I8, the field's item is above the keyboard, and with I9, 50 above it as in Ionic ([frame](compare/pixel8-input-assisted.png), before I9: 26 above, the 24 of the navigation bar) |
| `IonKeyboardDidShow` | `keyboardHeight 354.3`, from the sample's log; `IonKeyboardDidHide` on closing |
| Back with the keyboard up | Closes the keyboard only, and the page stays: the keyboard takes the press, as in a native app |
| Back at priority 10 | The page's handler takes it over the shell's 0, and the page stays ([frame](compare/pixel8-back-taken.png)); without it, back goes to the list at 0 |

Two things the coordinator asked to look at while the phone was out, which aren't `ion-app`'s:

- **[#172](https://github.com/wdcossey/Avonic/pull/172)'s scroll indicator at load.** Chrome on the phone, Ionic's `ion-content` with 40 items in `ion-app`, no scroll:
  the thumb is there from load, 10 device pixels wide (3.8), grey 191 on white, from y 296 to 1000, and still there about a minute
  later ([frame](compare/pixel8-chrome-indicator-at-load.png)). So a real phone shows it as Chromium's emulation did, where Avonic
  hides it until the first scroll. Not changed here.
- **[#139](https://github.com/wdcossey/Avonic/issues/139) and [#174](https://github.com/wdcossey/Avonic/issues/174), shadows on a GPU at 2.625×.** A danger `ion-fab-button` in the middle of
  an `ion-content`, in Chrome ([frame](compare/pixel8-chrome-fab.png)) and in the sample ([frame](compare/pixel8-avonic-fab.png)). Darkness
  (255 less the grey) outward from the disc's edge, in device pixels:

  | | Chrome | Avonic |
  |---|---|---|
  | Sideways, where only the blur counts: reaches 2 | 36 | 38 (+6%) |
  | Sideways: reaches 8 | 19 | 21 (+10%) |
  | Sideways: summed | 417 | 458 (+10%) |
  | Above and below: summed | 218 and 1033 (4.7 to 1) | 360 and 677 (1.9 to 1) |

  The blur is 6 to 10% wider on the GPU at 2.625× than Chrome's, in line with the estimate of about 6%. The offset isn't scaled:
  Chrome's shadow sits far more below the button than Avonic's. The cards ([Chrome](compare/pixel8-chrome-card.png),
  [Avonic](compare/pixel8-avonic-card.png)) look alike by eye; their shadow is 1 to 5 px, too small to measure apart.

## Not tried

- In a real window: the `ion-app` section added to `tools/real-window/drive.ps1` is for the coordinator to run.
- On iOS: no device.
