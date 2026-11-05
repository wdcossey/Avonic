# `AvonicIcon` — ion-icon

Status: done · [#3](https://github.com/wdcossey/Avonic/issues/3)

Ionic reference: [`ion-icon`](https://ionicframework.com/docs/api/icon) · [Plan](PLAN.md) · [Comparison](COMPARISON.md) ·
reference render in [`ref/`](ref/)

A box `1em` square showing one SVG, in the current text colour. All 1357 ionicons are bundled.

## Usage

```xml
xmlns:c="using:Avonic.Components.Controls"

<c:AvonicIcon IconName="heart" />
<c:AvonicIcon IconName="search-sharp" Size="Large" />
<c:AvonicIcon IconName="alert-circle" Color="danger" FontSize="24" />
<c:AvonicIcon Src="avares://MyApp/Assets/logo.svg" />
```

**Note `IconName`, not `Name`.** `Name` is Avalonia's own element name, so Ionic's `name` prop can't use it.

**A bare name doesn't pick a variant.** `heart` is the file `heart.svg`, and there's also `heart-outline` and
`heart-sharp`. Ionic's components use specific ones, such as `arrow-back-sharp` for the md back button.

**Set `FontSize` where you use it.** The box is `1em`, and Avalonia's inherited default isn't the 16 px of a web
page, so an icon with no `FontSize` above it is small.

**It sits at the start of its parent.** It's `inline-block` in Ionic, so it doesn't stretch. Unlike most Avalonia
controls, it defaults to `HorizontalAlignment="Left"` and `VerticalAlignment="Top"`, so set `Center` to centre it.

**A change to no icon keeps the last one.** Setting `IconName` to null or an invalid name leaves the current icon
showing, as Ionic does. An unknown name is blank, because it counts as an icon that failed to load.

## Fidelity checklist

### Props

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `name` | `IconName` | `string?` | null | Renamed: `StyledElement.Name` exists |
| [x] | `md` | `Md` | `string?` | null | Beats `IconName` |
| [x] | `icon` | `Icon` | `string?` | null | A name, or a source if it contains `/` or `.` |
| [x] | `src` | `Src` | `string?` | null | See "Sources" |
| [x] | `color` | `Color` | `string?` | null | Beats `Foreground` |
| [x] | `size` | `Size` | `IconSize?` | null | `Small` = 18, `Large` = 32. Beats `FontSize` |
| [x] | `flipRtl` | `FlipRtl` | `bool?` | null | Null auto-flips arrows and chevrons |
| [ ] | `ios` | — | | | `not applicable`: ignored in md |
| [ ] | `mode` | — | | | `not applicable`: ADR 0001 |
| [ ] | `lazy` | — | | | `not applicable`: defers a fetch, and artwork here is local |
| [ ] | `sanitize` | — | | | `not applicable`: nothing executes, and unsafe content is always blank |

`aria-label` maps to `AutomationProperties.Name`. The automation peer is `Image`, for `role="img"`.

### Exported utility

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `addIcons({ name: url })` | `AvonicIcon.AddIcons(IReadOnlyDictionary<string, string>)` |

### Events, methods, slots, parts

None in Ionic.

### Pseudoclasses

| Ionic class | Avonic |
|---|---|
| `.ion-color` | `:ion-color` |
| `.icon-small`, `.icon-large` | `:size-small`, `:size-large` |
| `.flip-rtl` | `:flip-rtl` |

### Resource keys

| Ionic | Key | Default |
|---|---|---|
| `--ionicon-stroke-width` | `Avonic.Icon.IoniconStrokeWidth` | 32 |

**This doesn't change built-in icons, in Ionic or here.** No SVG in ionicons 8 reads it, and it only reaches a custom
SVG with the `ionicon-stroke-width` class. It has a fallback, so it isn't required.

## Colour

Icons paint in the inherited `Foreground`, so they follow text around them. `Color` gives `--ion-color-base`, the same
way as other controls. An unknown `Color` falls back to the inherited `Foreground`.

## Right to left

Avalonia would mirror the drawing under a right-to-left ancestor, and Ionic doesn't, so `AvonicIcon` opts out and flips
itself. Arrows and chevrons flip by themselves, by name. Set `FlipRtl="True"` for others, and `FlipRtl="False"` to stop it.

## Custom icons

**Register a name** the way `addIcons` does. The value is a source, as below:

```csharp
AvonicIcon.AddIcons(new Dictionary<string, string>
{
    ["myLogo"] = "avares://MyApp/Assets/logo.svg",
});
```

`IconName="my-logo"` and `IconName="mylogo"` differ: a camelCase key also registers in kebab-case, and names are lowercased
when they're read, so use `my-logo`. A registered name beats a bundled icon of that name. The first registration of a name
wins, and a different one after it logs a warning.

**Sources** work in `Src`, `Icon` and `AddIcons`:

| Source | Result |
|---|---|
| `avares://…` | Loaded |
| `data:image/svg+xml;utf8,…`, `;base64,…`, or percent-encoded | Parsed |
| `http(s)://…` | Blank. Fetching isn't implemented |
| A relative path | Blank. Use `avares://` |

**Custom SVGs can use** `path`, `circle`, `ellipse`, `rect`, `line`, `polyline`, `polygon` and `g`, with presentation
attributes such as `fill`, `stroke` and `transform`. Gradients, `<text>`, `<image>`, `style=` and `<style>` aren't drawn.
An SVG with `<script>` or an `on*` attribute is blank. Use `currentColor` to follow the icon's colour.

An SVG has to be well-formed XML. A `<!DOCTYPE>` is fine, and more than 256 levels of nesting is blank.

A malformed path draws nothing, where a browser keeps the part before the error. So do a few valid forms that
Avalonia's parser rejects, such as `+1`. If a custom icon has a blank layer, check its numbers. See
[PLAN.md](PLAN.md#streamgeometryparse-in-avalonia-1214). Be careful with SVGs from untrusted sources.

Add `class="ionicon"` to the root for Ionic's default stroke on every element.

## Cost

The artwork adds 160 KB to `Avonic.Components.dll`. It's decompressed on the first icon lookup, about 7 ms on a desktop,
and the 758 KB of text stays in memory. Each icon is parsed once, and that's cached. See
[PLAN.md](PLAN.md#measured-cost).

## Updating ionicons

```
node tools/ionicons/generate.js
```

`IconArtworkTests` fails if this hasn't been run after an upgrade. Afterwards, re-run the comparison in
[COMPARISON.md](COMPARISON.md).

## Writing a control that uses icons

Use the names Ionic's own component uses, from the `import … from "ionicons/icons"` line in its `.js`. A control's styles
set the icon's `FontSize` and `Foreground`, translating rules such as `::slotted(ion-icon) { font-size: 1.5em; }`.

## Extensions

None. `Foreground` and `FontSize` are Avalonia's own text properties, standing in for CSS `color` and `font-size`.

## Licence

The artwork is [Ionicons](https://ionic.io/ionicons), © Ionic, MIT licensed. Its notice is in
`src/Avonic.Components/Icons/LICENSE`, and it ships in the package.
