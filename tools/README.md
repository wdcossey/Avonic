# tools/

Developer tooling. Nothing here ships in a package.

| Tool | Purpose | Step in `docs/PROCESS.md` |
|---|---|---|
| `ionic-api.js <ion-tag>` | Prints the component's complete public API from `@ionic/docs` as a Markdown checklist — the spec a control is built from. `--list` for all tags, `--json` for raw data. | **Plan** |
| `ionic-ref/render.js <ion-tag>` | Renders the real Ionic web component in headless Chromium (md mode, 390×844 phone viewport) and writes screenshots, light/shadow DOM, bounding boxes, computed layout and every resolved CSS variable to `docs/components/<tag>/ref/`. `--html` for a custom snippet, `--tap` for press/release frames. | **Plan** (baseline) and **Compare** |

`render.js`, and `ionic-ref/boxes.js <page.html>`, which prints the boxes in a page of items and dividers, load Roboto from
[`fonts/roboto/`](fonts/roboto/README.md), the files the tests and the sample embed, so that text measures the same on both sides.

Both need `npm install`. `playwright-core` is pinned to the Chromium build the cloud environment
preinstalls (`PLAYWRIGHT_BROWSERS_PATH=/opt/pw-browsers`); on a machine without it, run
`npx playwright-core install chromium` once.
