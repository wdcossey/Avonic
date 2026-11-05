# Colour system

Status: done · [#2](https://github.com/wdcossey/Avonic/issues/2)

Ionic's palette and global `--ion-*` variables as resource keys, and the `color` property every control
uses. The design and the evidence for it are in [PLAN.md](PLAN.md).

## How a control uses it

**1. Add the property.** This is the only colour code a control needs:

```csharp
public static readonly StyledProperty<string?> ColorProperty =
    ColorPalette.ColorProperty.AddOwner<AvonicBadge>();

public string? Color
{
    get => GetValue(ColorProperty);
    set => SetValue(ColorProperty, value);
}
```

**2. Translate the CSS.** `:host(.ion-color)` becomes `^:ion-color`, and each variable becomes a
`ColorPalette` property:

| Ionic | Avonic |
|---|---|
| `.ion-color` | `:ion-color` |
| `var(--ion-color-base)` | `ColorPalette.Base` |
| `var(--ion-color-contrast)` | `ColorPalette.Contrast` |
| `var(--ion-color-shade)` | `ColorPalette.Shade` |
| `var(--ion-color-tint)` | `ColorPalette.Tint` |
| `rgba(var(--ion-color-base-rgb), a)` | `ColorPalette.BaseColor` through `ColorPalette.WithAlpha` |
| `rgba(var(--ion-color-contrast-rgb), a)` | `ColorPalette.ContrastColor` through `ColorPalette.WithAlpha` |

**Every read goes under `:ion-color`,** including in template parts. The values inherit, so a control with
no colour inside a coloured parent can see the parent's, and Ionic's rules ignore them there too. Put each
part's default in a style as well, because a style can't override a value set directly in the template.

```xml
<!-- :host(.ion-color) { background: var(ion-color-base); color: var(ion-color-contrast); } -->
<Style Selector="^:ion-color">
    <Setter Property="Background"
            Value="{Binding (b:ColorPalette.Base), RelativeSource={RelativeSource Self}}" />
    <Setter Property="Foreground"
            Value="{Binding (b:ColorPalette.Contrast), RelativeSource={RelativeSource Self},
                            Converter={x:Static b:ColorPalette.NullToUnset}}" />
</Style>

<!-- A template part -->
<Style Selector="^:ion-color /template/ Border#PART_Native">
    <Setter Property="BorderBrush" Value="{TemplateBinding (b:ColorPalette.Shade)}" />
</Style>

<!-- rgba(var(ion-color-base-rgb), 0.08) -->
<Style Selector="^:ion-color /template/ Border#PART_Native">
    <Setter Property="Background"
            Value="{Binding (b:ColorPalette.BaseColor), RelativeSource={RelativeSource Self},
                            Converter={x:Static b:ColorPalette.WithAlpha}, ConverterParameter=0.08}" />
</Style>
```

`tests/Avonic.Components.Tests/Behaviors/ColorPaletteUsage.axaml` is a complete working example.

### Rules

- **Use `NullToUnset` on text colours.** An unknown colour leaves `Contrast` null, and a null `Foreground`
  hides the text.
- **Don't use it on backgrounds.** Null is correct there. Ionic's solid button with an unknown colour has
  a transparent background.
- **Test the paint, not `:ion-color`.** Render the control with no `Color` inside a coloured ancestor, and
  assert every part keeps its default. That's the case a missed guard gets wrong.
- **Names are case-sensitive, like a CSS class.** `Danger` isn't a colour. Neither are the variants of the
  nine built-in colours, such as `danger-shade`. A custom `sea-tint` is fine.

## Unknown colours

`color="nonexistent"` doesn't keep the control's default colours. As in Ionic, `:ion-color` still applies
with nothing behind it, so a solid control loses its background. The one exception is inside a coloured
ancestor, where the ancestor's values show through.

Text is the one difference from Ionic. It's black rather than inheriting the parent's colour, so on the
dark palette it's close to invisible; see [PLAN.md](PLAN.md#one-known-difference-text-with-an-unknown-colour).

## Tokens

| Ionic | Key | Type |
|---|---|---|
| `--ion-color-danger` | `Avonic.Color.Danger`, `Avonic.Color.DangerBrush` | `Color`, brush |
| `--ion-color-danger-contrast`, `-shade`, `-tint` | `Avonic.Color.DangerContrast`, … | `Color`, brush |
| `--ion-background-color`, `--ion-text-color` | `Avonic.BackgroundColor`, `Avonic.TextColor`, plus `…Brush` | `Color`, brush |
| `--ion-background-color-step-150` | `Avonic.BackgroundColorStep150`, plus `…Brush` | `Color`, brush |
| `--ion-text-color-step-150` | `Avonic.TextColorStep150`, plus `…Brush` | `Color`, brush |
| `--ion-border-color`, `--ion-backdrop-color` | `Avonic.BorderColor`, `Avonic.BackdropColor`, plus `…Brush` | `Color`, brush |
| `--ion-backdrop-opacity`, `--ion-placeholder-opacity` | `Avonic.BackdropOpacity`, `Avonic.PlaceholderOpacity` | `double` |
| `--ion-placeholder-color` | `Avonic.PlaceholderColorBrush` | brush |
| `--ion-overlay-background-color` | `Avonic.OverlayBackgroundColorBrush` | brush |
| `--ion-item-background`, `--ion-toolbar-background`, `--ion-tab-bar-background`, `--ion-card-background` | `Avonic.ItemBackgroundBrush`, `Avonic.ToolbarBackgroundBrush`, `Avonic.TabBarBackgroundBrush`, `Avonic.CardBackgroundBrush` | brush |
| `--ion-font-family` | `Avonic.FontFamily` | `FontFamily` |

Other global variables are deferred to the controls that read them. [PLAN.md](PLAN.md#deferred) lists them.

### Rules for control tokens

- **A literal colour gets two keys:** a `Color`, plus a `…Brush` whose `Color` is a `{DynamicResource}` on
  it. Overriding the `Color` then updates both.
- **A token that falls back to another variable is a brush that follows it.**
- **A fallback that differs from the global value goes in the control's own tokens.** `--ion-toolbar-color`
  falls back to `#424242`, not the global `#000`. An Avonic key always has a value, so it can't copy CSS's
  "was this variable set?" behaviour, and that control's token won't follow `Avonic.TextColor`.
- **Token files can't quote a variable with its leading dashes in a comment.** XML comments can't contain
  two hyphens in a row.

## Restyling an app

Override the `Color` key, at any level:

```xml
<Application.Resources>
    <Color x:Key="Avonic.Color.Primary">#7C3AED</Color>
</Application.Resources>
```

Tokens that only have a brush, such as `Avonic.ItemBackgroundBrush`, are overridden by replacing the brush.
A card doesn't follow an overridden item background, though it does in Ionic, so override both.

**Add a custom colour** by defining its keys, the equivalent of Ionic's `.ion-color-favorite`. `Color="favorite"`
then works on every control, and `my-brand` maps to `Avonic.Color.MyBrand…`:

```xml
<Color x:Key="Avonic.Color.Favorite">#69BB7B</Color>
<Color x:Key="Avonic.Color.FavoriteContrast">#000000</Color>
<Color x:Key="Avonic.Color.FavoriteShade">#5CA56C</Color>
<Color x:Key="Avonic.Color.FavoriteTint">#78C288</Color>
```

**Override for a subtree** by putting the same keys in that element's `Resources`. `Color="danger"` follows
it. A direct `{DynamicResource Avonic.Color.DangerBrush}` doesn't, because that brush is resolved at the
application, so in a subtree either override the brush too or use `ColorPalette`.

## Dark palette

Merge it yourself, in `Application.Resources` so it wins over the theme. It can be added or removed at runtime:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://Avonic.Themes/Tokens/Palettes/Dark.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

`AvonicTheme` doesn't merge anything under `Tokens/Palettes/`. Theme-variant switching isn't wired up.

## Testing

Tests that read colours need the theme installed, as in `ColorTokensTests`. The brushes follow their `Color`
through `{DynamicResource}`, which gets no value on a detached `new AvonicTheme()`.
