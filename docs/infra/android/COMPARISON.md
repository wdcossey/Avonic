# The sample on a phone

Run on 2026-11-09 on a **Google Pixel 8** (`shiba`) with **Android 17 (API level 37)**: 1080 × 2400, density 420, which Avalonia
reports as **411 × 914 at 2.63×**. The Debug APK was used for every check except the benchmark, which used the Release APK, both built
from 073b4cf. Input came over ADB: `input tap`, `input swipe` and `input motionevent`, run on the device so that the timing doesn't
cross USB. The screenshots are in [`compare/`](compare/).

| # | Check | Result |
|---|---|---|
| 1 | Touch and the ripple | **Pass.** A press held with no movement shows the ripple: it covers the surface 0.6 s in ([held](compare/pixel8-ripple-held.png)), and it has faded 0.15 s after release. Caught mid-expansion, the circle is centred under the finger, near the left of the surface ([expanding](compare/pixel8-ripple-expanding.png)). On `ion-button`, three taps counted 3, a press that slid 300 px off the button counted nothing, and a press held 0.9 s counted once |
| 2 | Scrolling momentum | **Pass.** A 300 dp drag in 240 ms (`input swipe 540 1700 540 912 240`): 800 ms after the release the status said `Top 841, 0.21 px/ms` ([800 ms](compare/pixel8-momentum-800ms.png)), and it ended at 941 ([ended](compare/pixel8-momentum-ended.png)). The Windows run with injected touch had 0.15 px/ms at the same point. By hand, the user judged a flick close to Settings |
| 3 | Safe areas | **Pass.** Portrait insets: top 50.3, bottom 24. Landscape: top 28.2, bottom 24, left 50.3 (the camera's side). Avalonia's way pads the whole app inside the bars ([portrait](compare/pixel8-device-avalonia-way.png), [landscape](compare/pixel8-landscape-avalonia-way.png)). Ionic's way runs edge to edge: the header's background reaches under the status bar with its title below it, and in landscape the divider's text starts at 66, its 16 and the inset of 50.3 ([portrait](compare/pixel8-device-ionic-way.png), [landscape](compare/pixel8-landscape-ionic-way.png)) |
| 4 | Shadows at 2.63× | **Look right, by eye.** The cards' shadows ([card](compare/pixel8-card.png)) and the solid buttons' show nothing out of place at this density. md's offsets are 1 to 3 px, so an offset that doesn't scale is 1 to 3 device pixels where it should be about 3 to 8, which isn't visible at a glance. Not measured pixel by pixel |
| 5 | Back | **Pass.** From a page, back goes to the list. With an overlay up, back dismisses it with the role `backdrop`, as Ionic's hardware back does, and stays on the page ([up](compare/pixel8-overlay.png), [after](compare/pixel8-overlay-after-back.png)). With one the backdrop can't dismiss, back goes to the list and the overlay stays up, as in Ionic ([after](compare/pixel8-undismissable-after-back.png)) |
| 6 | An overlay in a single view | **Pass.** "Present one through the app", with no `TopLevel`, presents like the others ([overlay 2](compare/pixel8-overlay-through-the-app.png)) |
| 7 | The benchmark | **Run** (Release), on the phone and on the PC from the same page. The rows are below: the phone is 3 to 6 times slower |
| 8 | Rotation | **Pass.** Rotating on the device page kept the page open, and the layout followed with nothing restarting |
| — | Roboto and resources | **Pass.** The text is Roboto, as on desktop |

## The benchmark

From the device page, in the Release build, with 5,000 elements at a depth of 20, 25 rounds and 4 to warm up. Times are µs per
element, without and with the theme:

| Where | When | Controls | Rules | Alternatives | `TextBlock` | `Border` |
|---|---|---|---|---|---|---|
| Pixel 8, the sample's device page | 2026-11-09 | 35 | 146 | 243 | 55.3 to 107.7 (+52.4) | 15.1 to 53.6 (+38.4) |
| The PC (Windows 11), the desktop sample's device page | 2026-11-09 | 35 | 146 | 243 | 9.7 to 26.8 (+17.1) | 2.7 to 10.2 (+7.6) |
| The PC, the headless test (for scale only) | 2026-11-09 | 35 | 146 | 243 | 20.2 to 27.1 (+6.9) | 2.3 to 10.1 (+7.8) |

The first two rows are the same page in each sample's Release build, so they compare: without the theme the phone is 5.7 times slower
for a `TextBlock` and 5.6 for a `Border`, and the theme's share is 3.1 and 5.1 times the PC's. That's in line with a phone's CPU against
a desktop's, so nothing is wrong on Android in particular. The test's row has no `SimpleTheme` and no layout, and is only for scale. At the
phone's +52 µs, a page of 450 elements, `ion-button`'s, spends about 20 ms on the theme's style checks when it opens.

The cost is style matching, on the UI thread before anything is drawn, so a faster renderer wouldn't change it. The ways to cut it are
those in [performance](../performance/README.md): fewer alternatives, or controls working out their host's state in code.

## Found

- **A held touch shows the ripple without moving.** On Windows with injected touch, a held touch showed nothing until the finger moved or
  lifted. That was the injection or Avalonia's Win32 backend, not `Activatable`.
- **The sample's pages show a desktop scroll bar.** The pages in a stock `ScrollViewer` get `SimpleTheme`'s scroll bar, a grey track with
  arrows along the right, when they overflow, as `ion-card`'s does. `ion-content`'s page doesn't. It's the sample's, not Avonic's: [#115](https://github.com/wdcossey/Avonic/issues/115).
