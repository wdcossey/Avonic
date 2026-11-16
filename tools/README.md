# tools/

Developer tooling. Nothing here ships in a package.

| Tool | Purpose | Step in `docs/PROCESS.md` |
|---|---|---|
| `ionic-api.js <ion-tag>` | Prints the component's complete public API from `@ionic/docs` as a Markdown checklist — the spec a control is built from. `--list` for all tags, `--json` for raw data. | **Plan** |
| `ionic-ref/render.js <ion-tag>` | Renders the real Ionic web component in headless Chromium (md mode, 390×844 phone viewport) and writes screenshots, light/shadow DOM, bounding boxes, computed layout and every resolved CSS variable to `docs/components/<tag>/ref/`. `--html` for a custom snippet, `--tap` for press/release frames. Each `ref/README.md` starts with the command that made it: run it again to regenerate. | **Plan** (baseline) and **Compare** |
| `ionic-ref/boxes.js <page.html> [shot.png] [--mobile]` | Prints one line for each `ion-item`, `ion-item-divider` and `ion-list-header` in a page of them: its height, its inner part and its wrapper as `x width height` (`.item-inner` and `.input-wrapper`, `.item-divider-inner` and `.item-divider-wrapper`, `.list-header-inner` and `-`, as a list header has no wrapper), and each element inside with an `id`, its `y` from the top of the row. A desktop page 390×844 at 2× by default; `--mobile` measures on `render.js`'s phone instead (390×844 at 2×, mobile and touch, Chrome for Android's user agent: [`ionic-ref/phone.js`](ionic-ref/phone.js)), where a `<textarea>` keeps 4 px for its scroll bar, not 15. The page isn't wrapped in `ion-app`. | **Plan** and **Compare** (layouts) |

`render.js` and `boxes.js` load Roboto from
[`fonts/roboto/`](fonts/roboto/README.md), the files the tests and the sample embed, so that text measures the same on both sides.

For example, `ion-textarea` in an item's start slot, with no `cols` and with `cols="10"`, measures 175 and 95 wide by default, and 164
and 84 with `--mobile`, which is what `AvonicTextarea` matches.

### How the Chromium tools wait

`render.js`, `boxes.js` and `compare-icons.js` wait through [`ionic-ref/settle.js`](ionic-ref/settle.js), never a fixed delay:

1. Every `ion-*` tag in the page is defined (`customElements.whenDefined`).
2. Every lazy module of Ionic's bundle (`node_modules/@ionic/core/dist/ionic/*.entry.js`) is imported, and only then does the
   snippet go into the page, from the `<template>` that `settle.defer()` holds it in. Each component waits for its own module, so
   without this they start in whatever order their modules arrive. That order decides some layouts: an `ion-item` with two
   `ion-label`s takes the style of the one that starts last, and the stacked-label row of the item cases was 48 high in some runs
   and 55 in others (#104). With the modules in first, components start in document order, as in Ionic's custom-elements build,
   and the ids Ionic counts (`ion-cb-0`, `ion-tg-0`, `ion-r-0`) follow the document too.
3. `componentOnReady()` on every `ion-*` element, in the light DOM and in shadow roots, until no new one appears.
4. Two animation frames, for what components do in a frame after they load.
5. Frames until the page keeps still for three in a row: every element's layout box (`offset*`, which a transform doesn't move),
   scroll position and classes unchanged, the fonts loaded, every image complete, and no finite animation or transition running. Infinite ones,
   a spinner's or a skeleton's shimmer, are left out, so the moment they're captured at is still arbitrary.

If that doesn't happen within 20 s (120 s for the icons), the tool fails and names what kept changing, rather than reading early.
`page.html` holds the snippet in place, not in the `<template>`, so it opens in a browser as it is, but there the components start
in whatever order their modules arrive, as before. `side.js` waits for its two
pictures to decode.

`ion-app` isn't needed for this: wrapped in it, the stacked-label row still changed from run to run. `render.js` wraps a snippet in
`ion-app` and `ion-content` unless `--bare`; `boxes.js` doesn't, so that the page is as tall as its rows.

Both need `npm install`. `playwright-core` is pinned to the Chromium build the cloud environment
preinstalls (`PLAYWRIGHT_BROWSERS_PATH=/opt/pw-browsers`); on a machine without it, run
`npx playwright-core install chromium` once.
