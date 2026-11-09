# ion-img

Status: done · [#17](https://github.com/wdcossey/Avonic/issues/17)

`AvonicImg` is a picture that loads only once it's in the viewport, and raises an event when it starts, finishes or fails. It has
no look of its own: it's as wide as its parent and as tall as the picture's aspect ratio makes it. The design and evidence are in
[PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicImg Src="https://example.com/photo.jpg" Alt="The view from the top" IonImgDidLoad="OnLoaded" />
<c:AvonicImg Src="avares://MyApp/Assets/photo.png" Alt="An app's own asset" />

<c:AvonicAvatar>
    <c:AvonicImg Src="https://example.com/face.jpg" Alt="Her picture" />
</c:AvonicAvatar>
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `alt` | `Alt` (`string?`) | The automation name, and the text shown where there's no picture. An empty one makes it decorative |
| [x] | `src` | `Src` (`string?`) | `http:`, `https:`, `avares:`, `file:`, a `data:` URI in base64, or a file path, relative to the working directory |

### Events

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionImgWillLoad` | `IonImgWillLoad` | It has come into the viewport, and starts to load `Src` |
| [x] | `ionImgDidLoad` | `IonImgDidLoad` | The picture has loaded, and shows |
| [x] | `ionError` | `IonError` | It couldn't be loaded or decoded |

All three bubble. No methods or slots, as in Ionic.

### Parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `image` | `PART_Image`, an `Image`, whose `Source` the control sets |

`PART_Alt` is the text a browser shows where there's no picture; the control shows one of the two.

### Pseudoclasses

`:in-avatar` and `:in-thumbnail`, when it's the direct child of an `AvonicAvatar` or an `AvonicThumbnail`, which Ionic's
`::slotted(ion-img)` reaches: there it fills the box, covers it, and is cut to its radius.

### Resource keys

None: `ion-img` has no variables. In an avatar or a thumbnail it's cut to `Avonic.Avatar.BorderRadius` or
`Avonic.Thumbnail.BorderRadius`.

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **It loads when it touches the viewport,** with no margin: an image whose top is exactly at the bottom edge loads, one a pixel
  further down waits for a scroll. That's a `ScrollViewer`'s viewport, or any ancestor's that clips. Hidden, it waits until it's
  shown; detached, until it's attached.
- **It loads once.** Once it has started, it watches nothing more.
- **Before the picture comes,** the alt text shows, in the inherited font and colour; with no `Alt`, nothing. While a first
  picture loads, nothing shows. While another loads, the old one stays.
- **A failure** raises `IonError` and shows the alt text.
- **A change of `Src`** loads again, with `IonImgWillLoad`, as soon as it's in view. Of two quick changes only the last one's
  picture shows. Set to null, it changes nothing, and the picture stays, as in Ionic.
- **The picture is decoded off the UI thread,** as `decoding="async"` does.

## Extensions

None.

## Known gaps and deliberate differences

- **No broken-image icon** before the alt text, and nothing for a failure with no `Alt`: those are Chromium's artwork.
- **SVG doesn't load,** and raises `IonError`: Avalonia's `Bitmap` doesn't decode it.
- **No cache:** the same URL in two images is fetched twice, where a browser's cache fetches it once.
- **A relative path is a file,** relative to the working directory. Use `avares://` for an app's own asset.

All of them, with their measurements, are in [PLAN.md](PLAN.md#deliberate-differences).
