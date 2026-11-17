# The Ionic version Avonic recreates

Avonic is measured against **Ionic 9.0.11** (`@ionic/core` and `@ionic/docs` in `package.json`; `ionicons` 8.1.0 and `@stencil/core`
4.45.3, as the lockfile holds them), the latest 9.x on 2026-11-17. It moved there from 8.8.19 in
[#190](https://github.com/wdcossey/Avonic/issues/190), and to 8.8.19 from 8.8.1 in [#188](https://github.com/wdcossey/Avonic/issues/188),
both from the assessment in #186.

#190's first step moved the packages, and what reads them. What 9.0 changes in a component is an issue of its own, in the
[table below](#what-9011-changed-and-what-avonic-does); until it closes, that component's docs describe 8.8.19, and say so.
`ion-img` keeps Ionic 8 as its reference ([ADR 0004](../../decisions/0004-keep-what-the-browser-does.md)).

## How a move is made

1. **The packages** (#193): `@ionic/core` and `@ionic/docs` move in `package.json` and `package-lock.json`, with nothing else.
2. **The diff:** `node tools/ionic-diff.js <old> <new>` takes both versions from the registry, removes Stencil's per-build `key:`
   hashes, leaves out tests and the ios skins, writes TypeScript's ES2017 down-levelling of `?.` and `??` back as the operators
   (Ionic 9 compiles to ES2020, so a raw 8 to 9 diff is mostly syntax), and prints the changed lines per component, the global
   CSS, `utils/` and the API. What syntax is left (`Object.assign` for a spread) still shows, so read a small diff before calling
   it a change.
   `--show <component>`, `--show css` and `--show utils` print each diff; `--checklists` diffs `tools/ionic-api.js`'s checklists;
   `--all` covers what Avonic doesn't have yet.
3. **Each change** is read, checked against what Avonic does, and measured in Chromium where it's behaviour: fixed with a test that
   fails without the fix, or recorded as already matching, with the test that proves it.
4. **The refs** (`ref/`) are re-rendered with `tools/ionic-ref/render.js`, with the command each `ref/README.md` records, for the
   components whose files changed. The others keep the version they were rendered with in their header.
5. **The docs and token comments** say the new version where it holds: for a component whose `.js` and `.md.css` are unchanged, and
   global CSS that's unchanged, what was read and measured holds for it. A line that records a measurement of the old version that
   the new one might not repeat is left as history.

## What 9.0.11 changed, and what Avonic does

From `node tools/ionic-diff.js 8.8.19 9.0.11`, `BREAKING.md`'s 9.x section and `CHANGELOG.md` up to 9.0.11 (read 2026-11-17: 9.0.11,
of 2026-11-11, is the latest; 9.0.12 is only in nightlies). After the normalising, these of Avonic's components are unchanged:
accordion, accordion-group, app, avatar, badge, buttons, card-content, card-header, card-subtitle, card-title, chip, fab, fab-list,
footer, grid, infinite-scroll, infinite-scroll-content, input-password-toggle, item-divider, item-group, list, list-header, note,
picker, radio-group, refresher-content, reorder, reorder-group, ripple-effect, row, segment-content, segment-view, skeleton-text,
spinner, tab, tab-bar, tabs, text, thumbnail. Of the rest, only input, textarea and picker-column-option change their `.md.css`
(and picker-column its `.css`).

| Ionic | Change | Avonic |
|---|---|---|
| 9.0.0, 9.0.4, 9.0.9, `ion-input`, `ion-textarea` | **Breaking.** A floating label floats for focus or a value, no longer for slotted content; `.input-start`, `.input-control`, `.input-end` wrappers (`.textarea-*`); `min-width: 2ch` on the native field; the label's `max-width` eased with it; an outline label offset by the start slot's measured width; one click for slotted content; textarea's md `min-height: 72px` | [#201](https://github.com/wdcossey/Avonic/issues/201) |
| 9.0.0, `ion-picker-column`, `ion-picker-column-option`; 9.0.1, `ion-picker-column` | No default `color`: the active option and the focused column read primary without one. A press outside the column during a user's scroll commits the centred option; a detached column drops its pending commit | [#202](https://github.com/wdcossey/Avonic/issues/202) |
| 9.0.0, `ion-input`, `ion-searchbar` | **Breaking.** `autocorrect` is a `boolean`, `false` by default | [#203](https://github.com/wdcossey/Avonic/issues/203) |
| 9.0.0, `ionicons` 8.1.0 | `ion-icon` shows a font glyph from a default slot, centred, at the icon's font size; `contain: layout style`. **The 1357 SVGs are byte-identical**: `tools/ionicons/generate.js` writes the same `ionicons.br`, and `compare-icons.js` gives `ion-icon`'s COMPARISON numbers again (1238 with no differing pixel, max 11, none over 5%) | [#204](https://github.com/wdcossey/Avonic/issues/204) |
| 9.0.7, `ion-input-otp` | A change of `length` takes the boxes' values from `value` again, and the tab indexes after the render | [#205](https://github.com/wdcossey/Avonic/issues/205) |
| 9.0.1, `ion-segment-button` | The `contentId` guard against `disabled` runs in `componentWillLoad`, and leaves a disabled `ion-segment`'s button disabled | [#206](https://github.com/wdcossey/Avonic/issues/206) |
| 9.0.10, label, title, progress bar, col, item, item-options, item-sliding | `document.dir` → `isRTL(el)`, the nearest ancestor's `dir`, which Avonic's inherited `FlowDirection` already follows: the deviation notes go | [#207](https://github.com/wdcossey/Avonic/issues/207) |
| 9.0.5, 9.0.11, button, card, item, breadcrumb, fab-button, item-option, segment-button, tab-button (`utils/attribute-controller.js`) | The host's `tabindex` moves to the native element, and aria attributes written after load reach it | [#207](https://github.com/wdcossey/Avonic/issues/207): the host is the focusable element here, so expected to need nothing; recorded per README |
| 9.0.0, `ion-img` | Deprecated: a warning in `componentDidLoad` | Kept as it is ([ADR 0004](../../decisions/0004-keep-what-the-browser-does.md)) |
| 9.0.0, `ion-radio` | `value` reflects to its attribute | Nothing: no attributes |
| 9.0.0, `ion-backdrop` | `visible`'s docs: it shows the background only, and the backdrop still blocks | #207 checks the README says so |
| 9.0.1, `ion-refresher`, `ion-item-sliding` | The slot, and each option's `side`, are read once frameworks have assigned them | Nothing: Avalonia sets properties before attach |
| 9.0.0, 9.0.4, `ion-content`, `css/core.css` | `content-sizing` in a modal whose `--height` is content-sized, read again on a resize | For the modal (#42) |
| 9.0.1, `utils/overlays.js`; 9.0.7, `utils/hardware-back-button.js` | The presented state is restored after a DOM move; one function for the root lock; `CloseWatcher` only while an overlay is open | For the modal (#42); `TopLevel.BackRequested` has no `CloseWatcher` |
| `utils/helpers.js` | `onCustomPropertyChange`, a probe that turns a CSS variable's change into a resize | For the modal's `--height` (#42) |
| 9.0.0, `ion-picker-legacy`; `ion-nav` with `ion-router`; `ion-modal`'s `handleBehavior`; `ion-select`; `ion-datetime`'s colour | Removed, or changed | Not built yet: #42, #45, #46 and #130 build on 9 |
| `css/float-elements.css` | No `:host-context([dir=rtl])` rules | Not used |
| `core.json` | A `(default)` slot on 45 more components; `ion-breadcrumb`'s `start`, `end` and `separator` | Docs only: `AvonicBreadcrumb` already has the three. The mapping rule for `Items` is #190's |

`core.css`'s palette, `css/palettes/dark.always.css` and `typography.css` are unchanged, so `Tokens/Colors.axaml`,
`Tokens/Palettes/Dark.axaml` and `Css/Typography.axaml` keep every value, and `ColorTokensTests` passes on 9.0.11. A reference
render made on 9.0.11 with `ion-badge`'s recorded command is byte-identical to the committed one: `render.js` and `boxes.js`
serve what they need from `node_modules` and import nothing, so v9's `exports` map doesn't reach them.

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
