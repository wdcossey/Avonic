# ADR 0004 — Keep a component Ionic drops because the browser does its job

**Date:** November 2026 · **Status:** accepted

## Context

Ionic 9 deprecates `ion-img`. From 9.0.0 its `componentDidLoad` logs "[ion-img] - This component is deprecated and will be
removed in Ionic v10. Use a native <img> with the loading="lazy" attribute instead.", and `core.json` gives it a `deprecation`.
Diffed against 8.8.19, that warning and the `@deprecated` tag are all that changed in `img.js`, and `img.css` is the same.

Ionic can drop it because a browser's `<img loading="lazy">` now does the job. Avonic isn't a browser. Avalonia's `Image` shows
a `Bitmap` the app has already made: it loads nothing from a URL, doesn't wait for the viewport, has no alt text or broken
image, and raises no load or error event. `AvonicImg` ([#17](https://github.com/wdcossey/Avonic/issues/17)) does all of that,
off the UI thread. Dropping it with Ionic would leave every app to write it again.

It came up while assessing Ionic 9 ([#186](https://github.com/wdcossey/Avonic/issues/186)). The human decided it on 2026-11-16
([#191](https://github.com/wdcossey/Avonic/issues/191)).

## Decision

**When Ionic deprecates or removes a component because the browser now does its job, Avonic keeps its own, or builds one if it
has none, unless Avalonia or .NET already does that job.** The question is whether an Avalonia app gets the browser's feature
without it. If it does, nothing is built, and the survey below says what does the job.

A component kept this way:

- **is Avonic's API, kept on purpose.** It isn't marked `[Obsolete]` and logs no warning, as an Avonic app has no native
  element to move to. Its names stay Ionic's, by the mapping rules, so `AvonicImg`, `Src`, `IonImgDidLoad` don't change.
- **keeps Ionic's last version as its reference:** the last version that has the component, or the last one Avonic measured
  if it's still there. It isn't re-pointed at the browser's feature, which doesn't behave the same: an `<img loading="lazy">`
  raises no `ionImgWillLoad`, and Chromium picks its own distance from the viewport to start loading, where `ion-img` waits
  until it touches it. When Avonic moves to an Ionic that no longer has it, its `PLAN.md`, `COMPARISON.md` and `ref/` stay at
  that version; a new render for it uses that version's package, in a temp folder.
- **says so in its README.** The status line ends with `kept:`, the Ionic version that deprecates or removes it, what the
  browser does instead, and a link to this ADR. A short "Reference" section names the Ionic version its plan and render read.
  `git grep -l "^Status: done"` still lists it.
- **follows the loop like any other control** for a change: Avonic's own, or one Ionic made before it dropped it.

Not covered here:

- **A deprecation in favour of another Ionic API,** such as the legacy form-control syntax, `ionStart` for `ionPullStart`, or
  `ion-picker-legacy` for `ion-picker`. Avonic follows Ionic, as it always has: the member stays while Avonic's Ionic version
  has it, and goes at the migration to the version that removes it.
- **A replacement by a third-party library,** such as `ion-slides` for Swiper. Noted in the survey, and not decided.

## Survey

Every component Ionic has deprecated or removed in favour of something else, from `BREAKING.md` and its archive (v4 to v9),
`CHANGELOG.md`, the deprecation warnings in `@ionic/core` 5.6.0 to 5.9.0 and 6.0.0, and every `deprecation` in `core.json`
8.8.19 and 9.0.11 (read 2026-11-17; 9.0.11 is the latest). Props are listed where the browser took their job. Other props
Ionic removed for its own reasons aren't, as they're the case above.

**Kept or built:** `AvonicImg`, kept. Nothing else needs building.

### Replaced by what the browser, or the app's framework, does

| Ionic | When | Replaced by | Avonic | Why |
|---|---|---|---|---|
| `ion-img` | deprecated in 9.0, removal planned for 10 | `<img loading="lazy">` | **keep** `AvonicImg` | Avalonia's `Image` has no URL loading, viewport wait, alt text, broken image or events |
| `ion-virtual-scroll` | deprecated in 5.7 (it warns from 5.7.0, not 5.6.0), removed in 7.0 ([#25808](https://github.com/ionic-team/ionic-framework/issues/25808)); #191 says 6 | a virtual scroll from Angular, React or Vue, or a library for one | **not needed** | An `ItemsControl` on a `VirtualizingStackPanel` virtualises. `AvonicList` takes one as its `ItemsPanel`, sticky dividers included |
| `ion-show-when`, `ion-hide-when` | removed in 4.0 | CSS media queries | **not needed** | Avalonia's `ContainerQuery` (with the `TopLevel` as the container, a media query), `OnFormFactor`, `OnPlatform`, `IsVisible` |
| `ion-datetime` `monthNames`, `monthShortNames`, `dayNames`, `dayShortNames` | removed in 6.0 | `Intl`, from `locale` | **not needed** | .NET's `CultureInfo` does `Intl`'s job. For #46: its names may differ from Chromium's, so measure there |
| `ion-input` `size` | removed in 8.0 | CSS `width` | **not needed** | Avalonia's `Width`. `InputNativeInput` still takes Chromium's width for an `<input>`'s default `size`, 20, as Ionic's input has it |
| `ion-skeleton-text` `width` | removed in 5.0 | CSS `width` | **not needed** | Avalonia's `Width` |
| `ion-content` `resize()` | removed in 4.0 | flex layout | **not needed** | Avalonia's layout passes |
| the global `[hidden]` rule | removed in 7.0 | the native `hidden` attribute | **not needed** | `IsVisible` |

### Replaced by a third-party library (noted, not decided)

| Ionic | When | Replaced by | Note |
|---|---|---|---|
| `ion-slides`, `ion-slide` | deprecated in 5.7, removed in 7.0 | Swiper.js, with Ionic's `IonicSlides` module | Avalonia has `Carousel`, and `AvonicSegmentView` already swipes between pages. Whether Avonic recreates Swiper's look is a separate question |

### Replaced by another Ionic API (follow Ionic)

| Ionic | When | Replaced by | Avonic |
|---|---|---|---|
| The legacy form-control syntax: `legacy` on checkbox, input, radio, range, select, textarea and toggle; the item's `helper` and `error` slots, `counter`, `fill`, `shape` and highlight variables | deprecated in 7, removed in 8.0 | the modern syntax, on the control | **not applicable:** Avonic was built on Ionic 8, with only the modern syntax |
| `ion-picker` (the overlay), then `ion-picker-legacy` | renamed in 8.0, removed in 9.0 | the inline `ion-picker` | **not applicable:** out of scope in `ROADMAP.md`; `AvonicPicker` is the inline one |
| `ion-refresher` `ionStart` | `deprecation` in `core.json` 8.8.19 and 9.0.11 | `ionPullStart` | `IonStart` stays while Ionic has it |
| `ion-reorder-group` `ionItemReorder` | `deprecation` in `core.json` 8.8.19 and 9.0.11 | `ionReorderEnd` | `IonItemReorder` stays while Ionic has it |
| `ion-scroll` | removed in 4.0 | `ion-content` | **not applicable:** `AvonicContent` |
| `ion-navbar` | removed in 4.0 | `ion-toolbar` with `ion-back-button` | **not applicable:** `AvonicToolbar`, and #47 |
| `ion-nav-push`, `ion-nav-back`, `ion-nav-set-root` | removed in 5.0 | `ion-nav-link` | **not applicable:** goes with nav, #130 |
| `ion-anchor` | renamed in 5.0 | `ion-router-link` | **not applicable:** the router is out of scope |
| the `ion-*-controller` elements | removed in 5.0 | controllers imported in code | **not applicable:** an overlay is presented with `AvonicOverlay.Present()` |

The only other `@deprecated` in 8.8.19's `dist/collection` is the legacy `pickerController`, and in 9.0.11 none. Angular's
`IonicModule`, deprecated in 9, is framework code.

## Consequences

- `AvonicImg` stays through the Ionic 9 migration ([#190](https://github.com/wdcossey/Avonic/issues/190)) and after Ionic 10.
  #186's plan to mark it deprecated, and maybe `[Obsolete]`, until Ionic 10 removes it, no longer applies.
- Its reference is Ionic 8's `ion-img`. After Avonic moves to Ionic 10, `render.js` can't render it from `node_modules`.
- A component Ionic drops for the browser in a later version gets a row in this survey, and a decision by this rule, at the
  migration that brings it.
- `tools/ionic-api.js` doesn't print a `deprecation` that is `""`, which is what 9.0.11 gives `ion-img`. The migration to 9
  should make it print one, so that the next is seen.
