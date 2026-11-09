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

Where there's no picture, a browser shows something of its own, and the template has it: `PART_Alt`, the alt text after the
broken-image icon, and `PART_Broken`, a box with a border, with the icon, `PART_BrokenIcon`, in it. The control shows which, as
Chromium does: see Behaviours.

### Pseudoclasses

`:in-avatar` and `:in-thumbnail`, when it's the direct child of an `AvonicAvatar` or an `AvonicThumbnail`, which Ionic's
`::slotted(ion-img)` reaches: there it fills the box, covers it, and is cut to its radius. `:sized` when it has a size of its own,
a `Height` or an avatar's or a thumbnail's, so that the broken-image box fills it.

### Resource keys

`ion-img` has no variables. In an avatar or a thumbnail it's cut to `Avonic.Avatar.BorderRadius` or
`Avonic.Thumbnail.BorderRadius`. For the broken image, which is the browser's, not Ionic's, Avonic has keys of its own in `Tokens/Img.axaml`:

| Key | md default |
|---|---|
| `Avonic.Img.BrokenImageBrush` | `Avonic.Color.Danger`: the icon |
| `Avonic.Img.BrokenImageBorderBrush` | `Avonic.BackgroundColorStep250`: the box's border, where Chromium's is `rgb(192, 192, 192)` |

The icon's shape is path data in the theme, `Avonic.Img.Md.BrokenImageIcon`, 16 × 16.

### Replacing the broken-image icon

Avonic ships its own drawing and no other artwork for this, so a different icon is the app's to choose. The theme draws
`Avonic.Img.Md.BrokenImageIcon` as a 1 px outline at its own size. A filled icon, drawn on a grid of any size, needs one
style in the app, which beats the theme. For example, ionicons' `alert-circle`, from `node_modules/ionicons/dist/svg/alert-circle.svg`:

```xml
<Style Selector="c|AvonicImg /template/ Path.broken-image"
       xmlns:c="using:Avonic.Components.Controls">
  <Setter Property="Data" Value="M256 48C141.31 48 48 141.31 48 256s93.31 208 208 208 208-93.31 208-208S370.69 48 256 48m0 319.91a20 20 0 1 1 20-20 20 20 0 0 1-20 20m21.72-201.15-5.74 122a16 16 0 0 1-32 0l-5.74-121.94v-.05a21.74 21.74 0 1 1 43.44 0Z" />
  <Setter Property="Fill" Value="{DynamicResource Avonic.Img.BrokenImageBrush}" />
  <Setter Property="Stroke" Value="{x:Null}" />
  <Setter Property="Stretch" Value="Uniform" />
</Style>
```

`Stretch="Uniform"` scales the icon's grid (512 here) into the 16 × 16 space, and the brush keeps it in the danger colour. Any
single-colour icon made of filled paths works the same way, such as Carbon's `no-image` (32 × 32, Apache-2.0); both were
checked in a headless frame. An outline ionicon, such as `alert-circle-outline`, doesn't: its lines are 32-unit strokes on the
512 grid, which a 1 px pen at 16 px can't reproduce, so use its filled variant. It costs the app one global style.

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **It loads when it touches the viewport,** with no margin: an image whose top is exactly at the bottom edge loads, one a pixel
  further down waits for a scroll. That's a `ScrollViewer`'s viewport, or any ancestor's that clips. Hidden, it waits until it's
  shown; detached, until it's attached.
- **It loads once.** Once it has started, it watches nothing more.
- **Before the picture comes,** the broken-image icon and the alt text show, in the inherited font and colour, the text from x 16
  as in Chromium; with no `Alt`, nothing, unless it has a size of its own, when the empty box shows. While a first picture
  loads, nothing shows. While another loads, the old one stays.
- **A failure** raises `IonError` and shows the icon and the alt text; with no `Alt`, or an empty one, a box with a grey border
  and the icon in it, the width of the parent and 20 high at the top of a line 24 high, or the size it has. All of it measured in
  Chromium, which shows its own icon in the same places at the same times.
- **A change of `Src`** loads again, with `IonImgWillLoad`, as soon as it's in view. Of two quick changes only the last one's
  picture shows. Set to null, it changes nothing, and the picture stays, as in Ionic.
- **The picture is read and decoded off the UI thread,** as `decoding="async"` does, and a picture that's replaced is freed.
- **A web request says what it is,** with a `User-Agent` and an `Accept` for pictures, as some servers refuse one that doesn't.

## Extensions

None.

## Known gaps and deliberate differences

- **The broken-image icon is Avonic's own drawing:** a square with a cross, in the danger colour, standing in for Chromium's
  artwork, which isn't Ionic's and can't be shipped. Restyle it through the keys above.
- **SVG doesn't load,** and raises `IonError`: Avalonia's `Bitmap` doesn't decode it.
- **No cache:** the same URL in two images is fetched twice, where a browser's cache fetches it once.
- **A relative path is a file,** relative to the working directory. Use `avares://` for an app's own asset.

All of them, with their measurements, are in [PLAN.md](PLAN.md#deliberate-differences).
