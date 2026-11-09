# ion-img → `AvonicImg` — Plan

Ionic 8.8.1, from `dist/collection/components/img/img.js`, `img.css` (there's no `img.md.css`) and `core.json`. Every number here
was measured in Chromium, md, at 390 wide, by throwaway `playwright-core` scripts that served the repo, a 300 × 200 PNG of four
coloured quarters, a 404 and a file that isn't an image, each with a delay a query string sets, and logged every `ionImg*` and
`ionError` event with `performance.now()`. The reference render is [`ref/`](ref/), and the comparison is
[COMPARISON.md](COMPARISON.md). It's built with `ion-skeleton-text`, in one PR, for #17.

## What it is

An `<img>` that waits until it's in the viewport before it loads, and says when it starts, finishes or fails. It has no look of
its own: `display: block`, the picture as wide as its parent, and as tall as its aspect ratio makes it, `object-fit: contain`.

## Fidelity checklist (`node tools/ionic-api.js ion-img`)

### Props (2)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `alt` | `Alt` | `string?` | null | The automation name, and the text shown where no picture is, as Chromium shows `alt`. See "Before, while and after loading" |
| [x] | `src` | `Src` | `string?` | null | A URL: `http`, `https`, `file`, `avares`, `data:…;base64,` or a file path. See "Loading" |

### Events (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionError` | `IonError` | Routed, bubbling, as `bubbles: true` |
| [x] | `ionImgDidLoad` | `IonImgDidLoad` | Routed, bubbling |
| [x] | `ionImgWillLoad` | `IonImgWillLoad` | Routed, bubbling |

### Methods (0), slots (0): none.

### Shadow parts (1)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `image` | `PART_Image` | An Avalonia `Image`, whose `Source` the control sets |

### CSS custom properties (0): none.

### Attributes that aren't props

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [ ] | `draggable` (copied onto the `<img>`) | — | Not applicable: HTML drag and drop |
| [x] | `decoding="async"` | — | The picture is decoded off the UI thread |

## Measured

| What | Chromium |
|---|---|
| Size, loaded | 300 × 200 in a 358 wide parent: 358 × 238.66. Bare at 390: 390 × 260. `object-fit: contain` |
| Size, nothing loaded, with `alt` | 19 high at 16 px Roboto: Chromium's broken-image icon, 16 × 16, then the alt text in the inherited font and colour |
| Size, nothing loaded, no `alt` | 0 high |
| Size, loading the first picture | 0 high, with or without `alt`: nothing shows until it has loaded |
| Size, failed, no `alt` | 24 high: Chromium draws a grey 1 px box the width of the parent, with the icon in it |
| The broken image (#108's follow-up, measured on [`ref-broken/`](ref-broken/) and at 2× pixel by pixel) | With an `alt`, failed or with no `src` yet: the icon, 16 × 16, at the top left, and the text from x 16 on the first line, wrapping to x 0; no box; 19 high at 16 px. With no `alt`, or `alt=""`, and failed: a box with a 1 px `rgb(192, 192, 192)` border, 20 high at the top of a line 24 high, the width of the parent, the icon in it at 2, 2. With a size of its own (120 × 60): the box fills it. No `alt` and no `src`: nothing, or, with a size, the box with no icon. Loading, with a size or not, with an `alt` or not: nothing. In an avatar the circle clips all of it |
| When `ionImgWillLoad` fires | At the first `IntersectionObserver` callback, the frame after it's attached: 1 ms after an append. All in view fire in the same frame |
| The observer | `new IntersectionObserver(cb)`: no `rootMargin`, no threshold, the viewport as root. Not the 200 ms `setTimeout` fallback, which is for browsers without one |
| The edge | At the bottom of an `ion-content` 844 high, an image whose top is at 843 or 844 loads; at 845 it doesn't, until a scroll of 1 px. Edge-adjacent counts, and so does a box of no height, with or without `alt` |
| Clipped | Inside a 100 wide `overflow: hidden` box: at x 150, it doesn't load; at x 100, on the clip's edge, it does. In a box of no height with `overflow: hidden`, inside the viewport, it does |
| Hidden | Under `display: none`: no load. Shown: loads in the next frame. `visibility: hidden` and `opacity: 0` load |
| Detached | Not attached: no load. Attached, or attached again after it was removed before it had loaded: loads in the next frame |
| A change of `src` | `ionImgWillLoad` at once (in view), the old picture stays, at its size, until the new one has loaded, then `ionImgDidLoad` |
| A change to a picture that fails | `ionImgWillLoad`, the old picture stays until the failure, then `ionError` and the alt text, 19 high |
| Two changes in a row | Two `ionImgWillLoad`s, and only the last one's `ionImgDidLoad`: the first request's result is dropped |
| `src` set to `undefined` | Nothing: no event, and the picture stays |
| The same `src` again | Nothing |
| A `src` set by an `ionImgWillLoad` handler | Dropped: one request, for the first, and its `ionImgDidLoad`. `load()` raises the event, the watcher starts a new observer, and `removeIO()` then disconnects it |
| `src=""` | `ionImgWillLoad`, then `ionError` (17 ms later) |
| Not an image, or a 404 | `ionError` |

## Behaviours

Each becomes a test in `AvonicImgTests`.

| # | Behaviour | Source |
|---|---|---|
| 1 | Nothing loads until it's attached, visible and intersects the viewport; then `IonImgWillLoad`, the load, and `IonImgDidLoad` when the picture is decoded and shown | `addIO`, `load`, `onLoad`; measured |
| 2 | In a `ScrollViewer` or `AvonicContent`, one below the fold loads when it's scrolled to the viewport's edge, not before: an edge counts, with no margin. One that grows into the viewport loads too | `new IntersectionObserver(cb)`; measured at 843, 844 and 845 |
| 3 | An ancestor that clips (`ClipToBounds`) and has it outside the clip stops it loading; on the clip's edge it loads | measured `c`, `d` |
| 4 | Hidden (`IsVisible` false on it or an ancestor) it doesn't load; shown, it does | measured |
| 5 | Removed before it loaded, it loads when it's attached again and in view | the observer stays; measured `g`, `h` |
| 6 | It loads once: scrolling away and back loads nothing more, and nothing is watched once it has started | `removeIO()` after `load()` |
| 7 | Loaded: as wide as its parent and as tall as the picture's aspect ratio, 358 × 238.66; in a box of its own size, contained and centred | `img.css`; measured |
| 8 | No picture yet, or none: the alt text in the inherited font and colour, 19 high at 16 px; with no `Alt`, nothing, 0 high. Loading a first picture: nothing, 0 high | Chromium's `<img>`; measured |
| 9 | A failure (a missing file, not an image, an empty `Src`, a 404) raises `IonError` and no `IonImgDidLoad`, and shows the alt text | `onError`; measured |
| 10 | A change of `Src` watches again: `IonImgWillLoad`, the old picture stays until the new one is shown, then `IonImgDidLoad`; or `IonError` and the alt text. The old picture is freed. One set by an `IonImgWillLoad` handler is dropped, and a layout a handler causes loads nothing more | `srcChanged`, `load()` before `removeIO()`; measured |
| 11 | Two changes in a row: only the last one's result is shown or raised | measured |
| 12 | `Src` set to null after a picture: nothing happens, and the picture stays. Null at first: no event, until one is set | `if (this.src === undefined) return`; measured |
| 13 | Automation: an `Image`, named by `Alt` | `<img alt>` |
| 14 | In an `AvonicAvatar` or `AvonicThumbnail`: fills it, covers it, and is cut to its radius, whose corners are physical in right-to-left | `::slotted(ion-img)` in `avatar.md.css` and `thumbnail.css` |
| 15 | The events bubble | `bubbles: true` |
| 16 | The loader: a file path, a `file:` URI, a `data:` URI in base64, an `avares:` asset and `http:` (from a server on the loopback) each load; anything else, or a picture that doesn't decode, fails | Avonic's own: see "Loading" |
| 17 | The picture is read and decoded off the UI thread | `decoding="async"` |
| 18 | Where there's no picture, the broken image shows exactly when Chromium shows its own: the icon with the alt text, at the top left, the text from x 16; with no alt and a failure, the box with the icon in it, 358 × 20 in a line 24 high, or the host's size; with no alt and no `src`, the box alone where the host has a size, and nothing otherwise; nothing while loading | Chromium's `<img>`; measured |

## Design

### Intersecting the viewport

Ionic's `IntersectionObserver` is `EffectiveViewportChanged`. Measured in Avalonia 12.1.4 headless: the viewport it gives is the
window's, clipped by every ancestor that clips, a `ScrollViewer`'s presenter included, and moved by a scroll, as an observer's
root is. The control subscribes only while it's waiting to load, as Ionic observes only then, so an image that has loaded costs
the layout manager nothing.

Four differences from an observer have to be made up for:

- **A new listener hears nothing until a layout pass,** and a layout pass doesn't happen by itself. Measured: a listener added to
  a laid-out tree heard nothing through `RunJobs` and a render tick, and heard at the next `UpdateLayout`. So the control asks for
  an arrange when it starts watching, as the observer's first callback comes in the next frame whatever happens.
- **Showing it raises nothing,** as the viewport hasn't moved. Measured: hiding and showing a listener raised no event. So a check
  that finds it hidden watches `IsVisible` on what in its chain is hidden, as `AvonicProgressBar` does, and checks again once it
  all shows and has been laid out.
- **A box that grows into the viewport raises nothing either,** as the viewport hasn't moved in its own space. So a change of
  size checks again, after the layout pass, by which time the viewport has been raised if it moved too.
- **A viewport clipped away completely is `(0, 0, 0, 0)` in the clipping ancestor,** moved into the control's space. With edges
  counting, that would intersect a control at that ancestor's origin. Chromium's equivalent, a clip of no area, intersects only
  when it's in view, and then it has a width, as `b` did. So a viewport of no width and no height intersects nothing.

The intersection counts edges, as `isIntersecting` does: `viewport.X ≤ width`, `0 ≤ viewport.Right`, and the same down.

The callback calls `load()` and then `removeIO()`, as Ionic does, so a `Src` that an `IonImgWillLoad` handler sets starts a watch
that `removeIO()` ends, as measured. Where a handler causes a layout, Avalonia would call back again during the callback, which an
observer never does, so the control ignores a viewport change while it's in its own callback.

### Loading

Avalonia has no loader for an `http(s)` `src`, and the conservative choice is no new package, as a `.csproj` is a shared file.
`HttpClient` is in the framework, so it needs none:

| `Src` | How |
|---|---|
| `http:`, `https:` | One shared `HttpClient`, which sends a `User-Agent` and an `Accept` for pictures, as a browser does, since some servers refuse a request without one; a status that isn't success fails |
| `data:…;base64,…` | Decoded from the URI. Not base64: fails |
| `avares:` | `AssetLoader.Open` |
| `file:`, or a path | A file, a relative one relative to the working directory. A path is read as it's written, so `%41` in a file's name stays `%41`; only a `file:` URI is unescaped |
| `""`, or any other scheme | Fails, as Chromium fails `src=""` |

The bytes are read and decoded into a `Bitmap` on the thread pool, as `decoding="async"` does it off the main thread, and the
result comes back to the UI thread. A newer `Src` drops an older result, and cancels its request, and a picture that's replaced
is disposed. A request ends at once when it's cancelled, so the newer one is counted first, and the older one finds itself
superseded rather than failed. A control that's removed keeps its picture, as an element does, since it may come back. Whatever Avalonia's `Bitmap` decodes loads: PNG, JPEG, WebP,
GIF's first frame, BMP. SVG doesn't, so it fails: see "Deliberate differences".

The loader is `AvonicImg.Loader`, an internal `Func<string, CancellationToken, Task<Bitmap>>` on the control, in its own folder,
which tests replace with one they finish themselves, so that no test needs the network. The default's own tests use a file, a
`data:` URI, an asset and a server on the loopback.

### Before, while and after loading

What Chromium shows in an `<img>` is the browser's, not Ionic's, but Ionic's docs promise it: "Users will see this text displayed
if the image URL is wrong, the image is not in one of the supported formats, or if the image is not yet downloaded". So the
template has `PART_Image`, `PART_Alt` for the alt text after the broken-image icon, and `PART_Broken`, the box, with the icon
`PART_BrokenIcon` in it, and the control shows which, as Chromium does:

| State | With an alt text | With none, or an empty one |
|---|---|---|
| Not loaded yet, or `Src` null | The icon and the alt text | Nothing; with a size of its own, the box alone |
| Loading the first picture | Nothing | Nothing |
| Loading another | The old picture | The old picture |
| Loaded | The picture | The picture |
| Failed | The icon and the alt text | The box, with the icon in it |

A size of its own is a `Height`, or an avatar's or a thumbnail's, which `height: 100%` fills; the control sets `:sized` for
the theme, which then lets the box fill the host instead of being 20 high at the top of a line 24 high.

The icon is Avonic's own drawing, in the theme: a 16 × 16 square with a 1 px border and a cross from corner to corner, in
`Avonic.Img.BrokenImageColor`, which is the danger colour. It stands in for Chromium's artwork, which isn't Ionic's, isn't in
ionicons, and can't be shipped. It's path data, `Avonic.Img.Md.BrokenImageIcon`, not an SVG file, as `AvonicImg` can't decode
SVG. With an alt text it sits in the text's first line: the line keeps an empty space 16 wide and 0 high, so the text starts at
16 and the line stays 19 high, and the icon is drawn over the space. An embedded control with a resource of its own is measured
during the render in Avalonia 12.1.4, which throws, so the icon isn't the embedded control itself.

### Inside an avatar or a thumbnail

`::slotted(ion-img)` is the avatar's and the thumbnail's, so it would go in a `Styles/` file, but the style budget is at its limit,
and those two rules would add two alternatives that every element in an app pays for. So the control works out `:in-avatar` and
`:in-thumbnail` from its parent when it's attached, as `::slotted` matches a direct child, and its `ControlTheme` gives it the
cover, which fills the box, and the clip there, at no cost to anything else. The clip is on `PART_Image`, which Avalonia doesn't mirror in
right-to-left, so a radius that isn't the same at every corner stays where CSS puts it. A theme rule also loses to an app's style, as `::slotted` loses
to an outer stylesheet.

### Resource keys

`ion-img` has no variables. The radius in an avatar or a thumbnail is theirs, `Avonic.Avatar.BorderRadius` and
`Avonic.Thumbnail.BorderRadius`. For what the browser draws, which isn't Ionic's, `Tokens/Img.axaml` has Avonic's own keys:

| Key | Type | Default |
|---|---|---|
| `Avonic.Img.BrokenImageColor` | brush | `Avonic.Color.Danger` |
| `Avonic.Img.BrokenImageBorderColor` | brush | `Avonic.BackgroundColorStep250`, `#BFBFBF`, where Chromium's is `rgb(192, 192, 192)` |

and the theme has the icon's path data as `Avonic.Img.Md.BrokenImageIcon`.

## Extensions

None. `Loader` is internal, for tests.

## Deliberate differences

| Difference | Why |
|---|---|
| The broken-image icon is Avonic's own drawing, a square with a cross in the danger colour, where Chromium draws a torn picture. Its place, its size, the box around it and when each shows are Chromium's, measured | Chromium's artwork isn't Ionic's, isn't in ionicons (rule 6), and can't be shipped. The human chose this drawing in #108 |
| In an avatar the alt text and the box aren't cut to the circle, where Chromium cuts them | Only the picture is clipped, on `PART_Image`, so that its corners stay physical in right-to-left |
| SVG doesn't load, and raises `IonError` | Avalonia's `Bitmap` doesn't decode SVG, and a decoder would be a package |
| No cache: the same URL in two images is fetched twice, where a browser's cache fetches it once | A cache is a policy an app should choose; see "For you to decide" in the PR |
| A relative path is a file, relative to the working directory, where a browser resolves it against the page | There's no page. `avares://` names an app's own asset |
| A request isn't cancelled when the control is detached, as Ionic's `disconnectedCallback` cancels nothing; its result is dropped when `Src` changes | Measured: the first of two quick changes never raised `ionImgDidLoad` |

## Dependencies

`AvonicAvatar` and `AvonicThumbnail` for behaviour 14; `AvonicContent` for behaviour 2. All done.
