# The Ionic version Avonic recreates

Avonic is measured against **Ionic 8.8.19** (`@ionic/core` and `@ionic/docs` in `package.json`; `ionicons` 8.0.13), the v8 LTS line.
It moved there from 8.8.1 in [#188](https://github.com/wdcossey/Avonic/issues/188), from the assessment in #186. v9 is a later step
([#190](https://github.com/wdcossey/Avonic/issues/190)).

## How a move is made

1. **The packages** (#193): `@ionic/core` and `@ionic/docs` move in `package.json` and `package-lock.json`, with nothing else.
2. **The diff:** `node tools/ionic-diff.js <old> <new>` takes both versions from the registry, removes Stencil's per-build `key:`
   hashes, leaves out tests and the ios skins, and prints the changed lines per component, the global CSS, `utils/` and the API.
   `--show <component>`, `--show css` and `--show utils` print each diff; `--checklists` diffs `tools/ionic-api.js`'s checklists;
   `--all` covers what Avonic doesn't have yet.
3. **Each change** is read, checked against what Avonic does, and measured in Chromium where it's behaviour: fixed with a test that
   fails without the fix, or recorded as already matching, with the test that proves it.
4. **The refs** (`ref/`) are re-rendered with `tools/ionic-ref/render.js`, with the command each `ref/README.md` records, for the
   components whose files changed. The others keep the version they were rendered with in their header.
5. **The docs and token comments** say the new version where it holds: for a component whose `.js` and `.md.css` are unchanged, and
   global CSS that's unchanged, what was read and measured holds for it. A line that records a measurement of the old version that
   the new one might not repeat is left as history.

## What 8.8.19 changed, and what Avonic did

From `node tools/ionic-diff.js 8.8.1 8.8.19`. The focus indicators of `ion-checkbox`, `ion-toggle`, `ion-radio` and `ion-item`
(8.8.17), and `ion-checkbox`'s label (8.8.2, 8.8.4), are #188's other half, recorded in those components' docs.

| Ionic | Change | Avonic |
|---|---|---|
| 8.8.4, `css/palettes/dark.always.css` | `--ion-tab-bar-background-focused`, `#353535` for md | Added to `Tokens/Palettes/Dark.axaml`. `high-contrast-dark.always.css` has the same; Avonic has no high-contrast palette |
| 8.8.19, `ion-range` | `dualKnobsChanged`: the knobs follow the value when `dualKnobs` changes | Fixed (`AvonicRange`) |
| 8.8.6, `ion-segment` | A scroll of the segment view to a disabled button's content checks nothing | Fixed (`AvonicSegment`) |
| 8.8.3, `ion-input-otp` | No Backspace, Delete or paste when read-only or disabled | Fixed for read-only. Disabled: disabling moves focus out of the box, as in Chromium, so nothing reaches it |
| 8.8.17, 8.8.18, `ion-refresher`, `utils/content` | No gesture after a disconnect during setup; `findRefresherScrollHost`, the first host only | Already matched; tests added |
| 8.8.19, `ion-content` | A `ResizeObserver` re-reads the fullscreen offsets when the host's size changes; `--internal-content-safe-area-padding-bottom` | Already followed the layout, a move included, which Ionic doesn't: recorded. The padding is the modal's (#42) |
| 8.8.19, `utils/rtl/dir.js`, `utils/helpers.js` | `isRTL(el)` walks up to the nearest ancestor with `dir`; `isEndSide(side, hostEl)` | Now agrees with the inherited `FlowDirection` for the components that call `isRTL(el)`: range, input OTP, segment and segment view, toggle, searchbar (ios only), content (an ios class). Those that read `document.dir` (label, title, progress bar, col, item, item sliding, and `isEndSide` called without a host) keep their notes |
| 8.8.19, `utils/input-shims/hacks/scroll-assist.js` | No click after relocating the input | Avonic never relocated or clicked |
| 8.8.19, `utils/overlays.js` | `present()` focuses a `[role=dialog][tabindex]` wrapper, with `preventScroll` | For the modal (#42): `docs/infra/overlays/README.md` |
| 8.8.19, `ion-tab` | A lazy component that failed to load is tried again | Already did; test added |
| 8.8.19, `ion-picker-column` | An old active option is found among the column's options | Already did, as Ionic meant; the note that called it a difference is gone |
| 8.8.19, `ion-picker-column-option` | `[part~=active]` is coloured too | Only `ion-datetime` sets it: for #46 |
| 8.8.17, `ion-fab-button`, `ion-button` | `form`, and the hidden form button | Not applicable: no forms |
| 8.8.19, `ion-radio-group` | Enter commits the focused radio in a select interface | For the select (#45) |
| 8.8.19, `ion-searchbar` | iOS's placeholder position waits for the icon's width | iOS only |
| 8.8.19, `ion-datetime` | Reads `animated` for its months' scroll | For the datetime (#46) |
| 8.8.19, `utils/gesture/listener.js` | The probe for passive listeners removed | Nothing: Avonic's gestures have no DOM listeners |
| `core.json` | `ion-fab-button` `form`; `ion-split-pane` `when`'s default written out | `form` not applicable; no split pane yet |

`core.css` and `typography.css` are unchanged, so `Tokens/Colors.axaml`, `Tokens/Global.axaml` and `Css/Typography.axaml` needed no
value changed.
