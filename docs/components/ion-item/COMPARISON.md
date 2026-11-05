# ion-item — Comparison against the web component

> Ionic side: `node tools/ionic-ref/render.js ion-item --bare --tap` (Chromium, md, 390×844@2x) → [`ref/`](ref/).
> Avonic side: `AVONIC_CAPTURE_DIR=… dotnet test` → `AvonicItemCaptureTests` (headless Skia, 2×) → [`compare/`](compare/).

## Frames

| Ionic | Avonic |
|---|---|
| ![](ref/page.png) | ![](compare/avonic-page.png) |

Rows from the top: default lines, full, none, a button with detail, slotted icons, danger, primary, disabled and RTL.
**Match.** Row heights are identical, at 48, 49, 48, 48, 49, 48, 48, 48 and 49.

The text is the one visible difference. Roboto isn't installed on this machine, so each renderer takes its own
fallback: Arial in Chromium and Segoe UI in Avalonia. That also moves the text by about 1 px, as they have different
metrics. Icons and borders are in the same places.

## Behaviours

The numbers are from [PLAN.md](PLAN.md#behaviours). Each has tests in `AvonicItemTests`, named after its number. The
Ionic results were measured in Chromium, with `ion-app` for the states, because it's what starts `tap-click` and
`focus-visible`.

| # | Behaviour | Ionic | Result |
|---|---|---|---|
| 1 | `button` is activatable, with a ripple and a pointer cursor | `BUTTON`, `ion-activatable`, ripple, `cursor: pointer` | match |
| 2 | A plain item has none of that | `DIV`, no ripple, overlay stays at 0 on hover | match |
| 3 | A tap on a `button` item clicks; a disabled one doesn't | `btn<-input-wrapper`; `disabled`: nothing | match |
| 4 | A tap on a child inside also clicks | `btn-nested<-inner-btn` | match |
| 5 | Enter and Space activate | `btn<-item-native` for both | match |
| 6 | Tab stops | `btn`, `btn-nodetail`, `color`, `badcolor`, `btn-nested`, `inner-btn`, `inner-btn2` | match: `plain`, `disabled` and `detail` are skipped |
| 7 | Only `detail` true shows the icon | `btn`: none; `detail`: shown | match |
| 8 | The default icon flips in RTL, a custom one doesn't | `flip-rtl` class on the default only | match |
| 9 | Detail icon box and style | 20×20 at x=360, 0.25, `8px`/`-6px`; 25×25 at 20 px | match |
| 10 | Lines | default and inset: inner 1 px. Full: outer 1 px. None: neither | match |
| 11 | Full lines make it 49 high | 49 | match |
| 12 | Colour | `rgb(197, 0, 15)`, white, borders `rgb(173, 0, 13)` | match |
| 13 | Unknown colour | Transparent, black borders | match |
| 14 | No colour in a coloured ancestor | from the colour system's rule | match |
| 15 | Hover | Black at 0.04; contrast on colour | match. Mouse and pen only |
| 16 | Keyboard focus | Black at 0.12, also while hovered. A click clears it | match |
| 17 | Pressing with the mouse | `ion-activated`, overlay stays at 0.04 | match |
| 18 | On colour, the overlay uses contrast | `rgb(255, 255, 255)` at 0.04 and 0.12 | match |
| 19 | Disabled | Opacity 0.3, `pointer-events: none`, `aria-disabled`, not a tab stop | match |
| 20 | Focus inside | `btn-nested` shows it from `inner-btn`; `plain-nested` doesn't | match |
| 21 | `multipleInputs`, `hasCover`, `canActivate`, `isFocusable` | from `item.js` | match, on stub controls |
| 22 | A tap on the padding forwards | from `item.js` | match, on stub controls |
| 23 | A tap on a slotted child doesn't | from `item.js` | match, on stub controls |
| 24 | Pointer cursor class | from `item.js` | match, on stub controls |
| 25 | `ionStyle` to `:item-<key>` | from `item.js` | match, on stub controls |
| 26 | `.item-interactive`, `.item-label-stacked`, `.item-textarea` | Minimum 55, detail icon and start at the top, end margins `7px 0 7px 16px`, stretched slots | match, on stub controls |
| 27 | `.item-interactive-disabled` | from `item.md.css` | match, on stub controls |
| 28 | Box layout | Inner x=16, w=374; container w=358, h=47. With icons: 16,12.5 and 350,12; inner x=72 | match, within layout rounding |
| 29 | Long text wraps | Item grows to 55 | match. The height depends on the font |
| 30 | Font and icon sizes | 16 px, icons 24, or 30 at 20 px, 54% black | match |
| 31 | Slot margins | `16`; icons `0 32 12`, `16 0 12` | match |
| 32 | Only direct children | `::slotted` | match |
| 33 | RTL | Start on the right, inner x=0 | match |
| 34 | Corners clip | `overflow: hidden` | match |
| 35 | Automation | `button`; `aria-disabled` | match |

Behaviours 21–27 can only be checked on stub controls, because the controls they're for don't exist yet. Those issues
compare them for real.

## Deliberate differences

See [PLAN.md](PLAN.md#deliberate-differences).

## Gaps

None.
