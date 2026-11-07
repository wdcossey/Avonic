# Plan: colour system (#2)

Sources: `@ionic/core` 8.8.1 `css/core.css`, `css/palettes/dark.always.css`,
`dist/collection/utils/theme.js`, and every `*.md.css` under `dist/collection/components`. 42
components declare a `color` prop, so this is the most reused API in the library.

## How Ionic does it

`createColorClasses` adds two classes when `color` is a non-empty string: `ion-color` and
`ion-color-<name>`. The second one only sets six custom properties:

```css
.ion-color-danger {
  --ion-color-base: var(--ion-color-danger, #c5000f) !important;
  --ion-color-base-rgb: var(--ion-color-danger-rgb, 197, 0, 15) !important;
  --ion-color-contrast: …; --ion-color-contrast-rgb: …; --ion-color-shade: …; --ion-color-tint: …;
}
```

Component CSS then reads those under `:host(.ion-color)`, for example
`:host(.ion-color) { background: var(--ion-color-base); color: var(--ion-color-contrast); }`.

## Measured behaviour

Rendered in real Ionic 8.8.1 (md, headless Chromium), reading computed styles. "Native" is
`ion-button`'s `.button-native`; a badge shows the same behaviour on its host.

| Case | `ion-color` class | `--ion-color-base` | Result |
|---|---|---|---|
| No `color` | no | unset | Default: native `rgb(0, 84, 233)` |
| `color="danger"` | yes | `#c5000f` | Native `rgb(197, 0, 15)` |
| `color="nonexistent"` | **yes** | unset | Native **transparent**; badge transparent with inherited black text |
| `color="Danger"` (wrong case) | yes | unset | Same as unknown |
| `color=""` | no | unset | Default |
| `color="favorite"` with a custom `.ion-color-favorite` | yes | `#69bb7b` | Uses the custom colour |
| Unknown `color` inside a `.ion-color-danger` wrapper | yes | `#c5000f` (inherited) | Native `rgb(197, 0, 15)` |
| No `color` inside a `.ion-color-danger` wrapper | no | `#c5000f` (inherited) | Default: ignored without `ion-color` |
| `--ion-color-danger` overridden on a wrapper | yes | `#00ff00` | Only that subtree changes |

### Correction to the issue

The issue says an unknown name means "the control keeps its default colours". Ionic doesn't do that.
`:host(.ion-color)` still applies, so `var(--ion-color-base)` is invalid and the property falls back
to `unset`: a solid button loses its background. This plan follows the measured behaviour, as
Rule 4 requires.

## Design: `ColorPalette`, inheriting attached properties

`--ion-color-base` and its siblings are inherited custom properties, and Avalonia's inheriting
attached properties have the same semantics. So each maps to one:

| Ionic | `Avonic.Components.Behaviors.ColorPalette` | Type |
|---|---|---|
| `color` prop | `Color` | `string?` |
| `.ion-color` class | `:ion-color` pseudoclass | set when `Color` is non-empty |
| `--ion-color-base` | `Base` | `IBrush?`, inherits |
| `--ion-color-contrast` | `Contrast` | `IBrush?`, inherits |
| `--ion-color-shade` | `Shade` | `IBrush?`, inherits |
| `--ion-color-tint` | `Tint` | `IBrush?`, inherits |
| `--ion-color-base-rgb` | `BaseColor` | `Color?`, inherits |
| `--ion-color-contrast-rgb` | `ContrastColor` | `Color?`, inherits |

A control adds `Color` with `AddOwner` and has no other colour code. A template translates the CSS
line by line:

```xml
<!-- :host(.ion-color) { background: var(--ion-color-base); } -->
<Style Selector="^:ion-color">
    <Setter Property="Background" Value="{Binding (b:ColorPalette.Base), RelativeSource={RelativeSource Self}}" />
</Style>
```

The full pattern, including text and alpha, is in [README.md](README.md).

Each measured case then follows with no special handling:

- **Unknown name:** nothing is bound, so `Base` stays null and the background goes transparent.
- **Inside a coloured ancestor:** a missing resource clears the local value, so the inherited one shows
  through. It has to be cleared: a binding that yields `UnsetValue` gives the default instead.
- **No `color`:** the values inherit, but `:ion-color` isn't set, so they're ignored.
- **Custom colours and scoped overrides:** values come from an element-level resource lookup, so a
  consumer's `Avonic.Color.Favorite` keys work, and so does an override on any ancestor.
- **Live changes:** the lookup is `GetResourceObservable`, so merging the dark palette at runtime or
  moving the element between scopes updates it.

`Base` and the others are built from the `Color` keys, not looked up from the `…Brush` keys. A subtree
override then needs one key, as in CSS, rather than two.

### One known difference: text with an unknown colour

In Ionic, `color: var(--ion-color-contrast)` with nothing behind it inherits the parent's text colour.
Avalonia can't do this from a style. A null `Foreground` hides the text, and a binding that yields
`UnsetValue` gives the property's default (black) rather than the inherited value.

`ColorPalette.NullToUnset` takes the second option. It matches the measured black text on the light
palette, but not under a parent with another text colour. **On the dark palette that's every parent**, so
the text is black on `#121212` and close to invisible.

This is accepted for now. It only happens after a mistyped colour name, and Ionic's own solid button goes
blank in that case: white text on a transparent background. Inheriting properly would need another
property that every control binds to, for a case that's already a bug in the app.

### Name mapping

Kebab to Pascal, case-sensitive like a CSS class: `danger` → `Avonic.Color.Danger`, `my-brand` →
`Avonic.Color.MyBrand`, `Danger` → unknown.

A variant of a built-in colour is treated as unknown. Without this, `color="danger-shade"` or
`color="dangerShade"` would find the real key `Avonic.Color.DangerShade` and paint it as the base, where
Ionic has no such class. Only the nine built-in colours are checked, so a custom colour called `sea-tint`
still works. The cost is that `favorite-shade`, for a custom `favorite`, finds that colour's shade.

`myBrand` and `my-brand` both map to `Avonic.Color.MyBrand`. Ionic would treat them as two classes.

### Alternatives rejected

- **Four brushes resolved once and set on the control.** Wouldn't reach a child that reads an ancestor's
  value, and needs its own re-resolve logic for scope and resource changes.
- **A markup extension (`{AvonicColor Base}`).** Extensions evaluate where they're written, so a shared
  `ControlTheme` can't follow one instance's `Color` without building all of the above anyway.
- **`Classes="ion-color-danger"` with a style per colour.** Custom colours would need a new style
  rather than resource keys, and class changes would have to be derived from a string property.

## Tokens

| File | Contents | Merged |
|---|---|---|
| `Tokens/Colors.axaml` | Nine colours × base, contrast, shade, tint | Automatically |
| `Tokens/Global.axaml` | Global tokens below | Automatically |
| `Tokens/Palettes/Dark.axaml` | The dark values | By the consumer. Excluded from discovery |

The dark palette lives under `Tokens/` so it stays within Rule 5 (hex only in `Tokens/`), and is
excluded from the glob because merging it automatically would replace the light values.

**Types.** A literal colour is a `Color` key plus a `…Brush` key whose `Color` is a
`{DynamicResource}` on the first, so overriding the `Color` updates both. A token that falls back to
another variable in Ionic is a brush only, which is what lets it follow that variable.

**`-rgb` variants have no keys.** In CSS they exist so `rgba()` can add alpha. An Avalonia `Color`
already serves that purpose, so a second key would be the same value under a second name.

### Global tokens

| Ionic | Key | Light value | Source |
|---|---|---|---|
| `--ion-background-color` | `Avonic.BackgroundColor` | `#ffffff` | Fallback in 17 components |
| `--ion-text-color` | `Avonic.TextColor` | `#000000` | Fallback in 15 components |
| `--ion-background-color-step-N` | `Avonic.BackgroundColorStepN` | Ramp | See below |
| `--ion-text-color-step-N` | `Avonic.TextColorStepN` | Ramp | See below |
| `--ion-border-color` | `Avonic.BorderColor` | `rgba(0,0,0,.13)` | Fallback in 11 components |
| `--ion-backdrop-color` | `Avonic.BackdropColor` | `#000` | `ion-backdrop` |
| `--ion-backdrop-opacity` | `Avonic.BackdropOpacity` | `0.32` | 5 of 6 readers; the other is `picker-legacy`, out of scope |
| `--ion-placeholder-opacity` | `Avonic.PlaceholderOpacity` | `0.6` | 4 components |
| `--ion-placeholder-color` | `Avonic.PlaceholderColorBrush` | → text step 600 | Fallback chain |
| `--ion-overlay-background-color` | `Avonic.OverlayBackgroundColorBrush` | → background | Fallback chain |
| `--ion-item-background` | `Avonic.ItemBackgroundBrush` | → background | Fallback chain; dark sets it |
| `--ion-toolbar-background` | `Avonic.ToolbarBackgroundBrush` | → background | Fallback chain; dark sets it |
| `--ion-tab-bar-background` | `Avonic.TabBarBackgroundBrush` | → background | Fallback chain; dark sets it |
| `--ion-card-background` | `Avonic.CardBackgroundBrush` | → background | Fallback chain; dark sets it |
| `--ion-font-family` | `Avonic.FontFamily` | `Roboto, Helvetica Neue, sans-serif` | `html.md` |

**The light ramps aren't defined anywhere as variables.** Ionic only sets them for dark and reads them
elsewhere with a fallback. They follow one formula, the mix of black into white at N/1000 (`step-150` =
`#d9d9d9`), and the text ramp is its reverse. All 19 values also appear in `:root.ios` in the dark
palette, with background and text swapped, and the md fallbacks agree. Tests check the ramps against
both. A few fallbacks are one component's own default rather than the ramp (`#eef1f3`, `#c1c4cd`, …);
the test lists them, and they go in those controls' tokens.

**Dark `--ion-border-color`.** It isn't set in dark. It resolves through its fallback to
`--ion-background-color-step-150`, so `Dark.axaml` sets `Avonic.BorderColor` to that step's `#363636`.

### A limit of always-defined keys

CSS can ask whether a variable was set: `var(--ion-text-color, #424242)` gives a toolbar its own
default until the app sets a global one. An Avonic key always has a value, so it can't make that
distinction. A control's token takes the control's own default (`#424242` for the toolbar) and doesn't
follow `Avonic.TextColor`. This affects outliers such as toolbar text, tab-bar borders and a few
`step-N` fallbacks. Those values belong in that control's `Tokens/<Name>.axaml`.

A brush also can't follow another brush, only a `Color`, and a `Color` can't follow anything. So:

- `--ion-card-background`, which falls back through `--ion-item-background` to the background, is unset since #72, and the card's
  theme follows that with `Css.Var`, as a control's own tokens do. Dark sets both.
- `--ion-border-color` falls back to `--ion-background-color-step-150` in dark, and `Avonic.BorderColor` is a
  copy of its value. Overriding the step doesn't change borders.

### A limit of the `…Brush` keys

A `…Brush` key resolves its `Color` from where the dictionary lives, the application. So a `Color` key
overridden for a subtree reaches `ColorPalette`, which looks up from the element, but not the `…Brush` keys.
In Ionic, a default that reads `--ion-color-primary` changes within that subtree. Here, a control with no
`Color` that defaults to `{DynamicResource Avonic.Color.PrimaryBrush}` doesn't.

For the same reason, the dark palette has to be merged at the application, as the README says. Merged on a
window, `ColorPalette` values would go dark and the `…Brush` keys wouldn't.

A subtree can override the brush as well. Fixing this properly means resolving brushes per element in each
control's theme, which isn't worth the cost until a control needs it.

## Deferred

| Not here | Why | Goes to |
|---|---|---|
| `--ion-safe-area-*`, `--ion-statusbar-padding` | Device values; a static 0 would be wrong | #11 content, #12 toolbar |
| `--ion-grid-*` | Only read by the grid | #14 |
| Other `--ion-item-*`, `--ion-toolbar-*`, `--ion-tab-bar-*`, `--ion-card-color`, segment tokens | Only read by that control | Their issues |
| `--ion-modal-offset-top` | Only read by the modal | #42 |
| `--ion-color-step-N` | Deprecated alias for both ramps; undefined by default | Not planned |
| `--ion-default-font`, `--ion-dynamic-font` | Internals behind `--ion-font-family` for ios and md | Not planned |
| `--ion-padding`, `--ion-margin` | Only used by the `.ion-padding` utility classes, which Avonic doesn't have | Not planned |
| Bundling Roboto | Avalonia falls back to the system font. A packaging decision rather than a token | Not planned here |
| Theme-variant switching | The issue says not to wire it | Later |

## Tests

| Test | Guards against |
|---|---|
| Palette `Color` keys equal `core.css` `:root`, nine × four | A wrong value, or an Ionic upgrade changing one |
| Each `-rgb` in `core.css` equals its `Color` | The claim that `-rgb` needs no key |
| Dark keys equal `dark.always.css` `:root` and `:root.md` | Same, for dark |
| Light ramps match the formula, the `:root.ios` ramps and the md fallbacks | A wrong ramp value |
| Each `…Brush` follows its `Color`, including a live override | Brushes that ignore an override |
| Dark isn't merged by `AvonicTheme`; merged by a consumer it takes effect live | Dark replacing light, or not applying |
| Every row in "Measured behaviour", one test each, with a rendered template for the inherited cases | Not matching Ionic, including a control painting an ancestor's colour |
| Changing and clearing `Color`; moving an element between scopes | Stale values or lost subscriptions |
| A control using `AddOwner`, with a real `ControlTheme` covering `Base`, `Contrast`, `TemplateBinding` and `WithAlpha` | The documented template pattern not working |
| Every sample page shows (existing test) | The sample page throwing |

Tests that read Ionic's CSS skip with a message if `node_modules` is absent. CI runs `npm ci`, so they
always run there.

## Sample

`Pages/ColorsPage`: the nine colours with contrast text, shade and tint, each driven only by
`ColorPalette.Color`.
