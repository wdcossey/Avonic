# ion-button — Plan

The first control with `fill`, `shape`, `size` and `expand`, so most of this is which class beats which, and how a
variable with a different default per class maps to one key. Measurements are from real Ionic 8.8.1 in Chromium, md, at
390 px wide, wrapped in `ion-app` so `tap-click` and `focus-visible` run.

## Fidelity checklist

`node tools/ionic-api.js ion-button`.

### Props (17)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `disabled` | `IsEnabled` | Mapping rule |
| [x] | `expand` | `Expand` (`ButtonExpand?`) | `Block`, `Full`; null is the default |
| [x] | `fill` | `Fill` (`ButtonFill?`) | `Clear`, `Default`, `Outline`, `Solid`; null resolves to solid. To clear in `ion-buttons` and `ion-list-header`: goes with #12 |
| [x] | `shape` | `Shape` (`ButtonShape?`) | `Round`; null is the default |
| [x] | `size` | `Size` (`ButtonSize?`) | `Default`, `Large`, `Small`; null is small inside an item |
| [x] | `strong` | `Strong` (`bool`) | |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `buttonType` | — | Not applicable: it only renames the host classes, so any other value drops every `button-*` rule. Nothing in Ionic sets it on `ion-button` |
| [ ] | `href` | — | Out of scope: router. Use the `Click` extension |
| [ ] | `download`, `rel`, `target` | — | Not applicable: only apply with `href` |
| [ ] | `routerAnimation`, `routerDirection` | — | Out of scope: router |
| [ ] | `type`, `form` | — | Not applicable: HTML form |

### Events, methods, slots, parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionFocus` | `IonFocusEvent`, `IonFocus` |
| [x] | `ionBlur` | `IonBlurEvent`, `IonBlur` |
| [x] | default slot | `Content`, `ContentTemplate` |
| [x] | `start` | `StartContent`, `StartContentTemplate` |
| [x] | `end` | `EndContent`, `EndContentTemplate` |
| [x] | `icon-only` | `IconOnlyContent`, `IconOnlyContentTemplate` |
| [x] | `native` | `PART_Native` (`Border`) |

No methods. Other parts are `PART_Overlay` (`.button-native::after`), `PART_Inner` (`.button-inner`, and it holds the
padding, because the overlay and ripple cover the padding), `PART_Ripple`, `PART_IconOnly`, `PART_Start`, `PART_Content`
and `PART_End`.

### CSS variables (23)

Each key is `CssUnset` unless it says otherwise, and what it falls back to depends on the class. In CSS a variable set
from outside beats every `:host(...)` default, so the key is what an app sets, and the class's own value is the fallback.

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Button.Background` | brush | Solid: primary. Otherwise none. A known `Color` beats it, measured `varsc` |
| [x] | `--background-activated` | `Avonic.Button.BackgroundActivated` | brush | transparent |
| [x] | `--background-activated-opacity` | `Avonic.Button.BackgroundActivatedOpacity` | `double` | 0 |
| [x] | `--background-focused` | `Avonic.Button.BackgroundFocused` | brush | Solid: primary contrast. Outline, clear: primary |
| [x] | `--background-focused-opacity` | `Avonic.Button.BackgroundFocusedOpacity` | `double` | Solid 0.24. Outline, clear 0.12 |
| [x] | `--background-hover` | `Avonic.Button.BackgroundHover` | brush | Solid: primary contrast. Outline, clear: primary |
| [x] | `--background-hover-opacity` | `Avonic.Button.BackgroundHoverOpacity` | `double` | Solid 0.08. Outline, clear 0.04 |
| [x] | `--border-color` | `Avonic.Button.BorderColor` | brush | Outline: primary. Only read on outline, see below |
| [x] | `--border-radius` | `Avonic.Button.BorderRadius` | `CornerRadius` | 4. Round 999. Full and not round is 0, which beats it, measured |
| [ ] | `--border-style` | — | | Not applicable: Avalonia's `Border` only draws solid |
| [x] | `--border-width` | `Avonic.Button.BorderWidth` | `Thickness` | Outline: 2. Only read on outline, see below |
| [x] | `--box-shadow` | `Avonic.Button.BoxShadow` | `BoxShadows` | Solid: two elevations. Otherwise none |
| [x] | `--color` | `Avonic.Button.Color` | brush | Solid: primary contrast. Outline, clear: primary. Default: inherits |
| [x] | `--color-activated` | `Avonic.Button.ColorActivated` | brush | Unset → `--color` |
| [x] | `--color-focused` | `Avonic.Button.ColorFocused` | brush | Unset → `--color` |
| [x] | `--color-hover` | `Avonic.Button.ColorHover` | brush | Unset → `--color` |
| [x] | `--opacity` | `Avonic.Button.Opacity` | `double` | 1. On the native part, not the host |
| [x] | `--padding-top`, `-bottom` | `Avonic.Button.PaddingTop`, `…Bottom` | `double`, px | 8. Round 0, large 14, small 4, icon-only 0 |
| [x] | `--padding-start`, `-end` | `Avonic.Button.PaddingStart`, `…End` | `double`, px | 1.1 em. Round 26, large 1 em, small 0.9 em, icon-only 0 |
| [x] | `--ripple-color` | `Avonic.Button.RippleColor` | brush | Unset → current colour |
| [x] | `--transition` | `Avonic.Button.Transition` | `Transitions` | Box shadow 280 ms `.4,0,.2,1`; background and colour 15 ms linear |

`--overflow` isn't documented, so it isn't a key: `PART_Native` clips, as `hidden` does.

`--border-width` and `--border-color` are only read on outline. Measured `varss`: on solid they draw nothing, because
`--border-style` stays `none`. Clear sets `--border-width: 0` too. With no `--border-style`, this is the same result.

## Behaviours

Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | A tap raises `Click`, on release, every time: on a second quick tap, after a tap on something else, and after a drag, if the press and release both landed on it. Not on the right button, with a second finger, in most chords, after a cancel, or disabled, even mid-press, or if a child handled the press | native `<button>`; measured `mouse click`, `double click`, `right click`, `press, release off`, the drags, a mid-press disable, `stopPropagation`, two fingers, chords, `touch cancelled` |
| 2 | Enter clicks on press, and Space on release, if it's still focused. Keys on a nested control are its own | native `<button>`; measured `enter down`, `space up` |
| 3 | It's a tab stop unless disabled, and a press focuses it | measured `tab from tab1`, `mouse click` |
| 4 | `IonFocus` and `IonBlur` fire on any focus, including a press, bubble, and don't fire for focus on a nested control. Disabling a focused button blurs it | `onFocus`, `onBlur`; `bubbles: true`; measured `focused then disabled` |
| 5 | Activatable: `:activated` and a bounded ripple on press, in the current colour, covering the padding. `Avonic.Button.RippleColor` overrides it | `ion-activatable`; measured `ripple on press`, `tok` |
| 6 | Hover overlay, mouse only. Keyboard-focus overlay, which a press or another key clears. No outline | `ion-focusable`, `any-hover`; measured `hover`, `focus`, `other key`; `outline: none` |
| 7 | Later beats earlier: hover beats focus, and press beats hover, so pressing with the default tokens shows no overlay | CSS order, not specificity; measured `focus+hov`, `press`. `ion-item` is the opposite |
| 8 | Overlays snap, with no transition | measured `tr=0s` |
| 9 | Fill solid: primary, contrast text, 2 dp shadow, rising to 8 dp while pressed | measured `solid`, `solid:press` |
| 10 | Fill outline: transparent, primary text, 2 px primary border | measured `outline` |
| 11 | Fill clear: transparent, primary text, no border | measured `clear` |
| 12 | `fill="default"`: no background, border or shadow, text inherits, and it ignores `Color`. Its overlay is opaque if only the colour is set | The class `button-default` has no rules; measured `fdef`, `fdefc`, `tokf` |
| 13 | `Color`: solid paints base and contrast; outline and clear paint base for text and border, and are transparent. A colour, even an unknown one, beats `--background`, and a known one beats `--color` | measured `danger*`, `varsc`, `c`, `d`, `e`, `f` |
| 14 | An unknown `Color`: transparent background, text from `--color`, including state colours; a solid keeps its shadow; an outline's border is the current colour | `var()` invalid at computed time; measured `bad*`, `tokbad` |
| 15 | An unknown `Color` inside a coloured ancestor paints that ancestor's base, and no `Color` keeps primary | `--ion-color-base` inherits; measured `initemcolbad`, `initemcol` |
| 16 | On a known `Color`, focus and hover overlays use contrast (solid) or base (outline, clear), beating the overlay tokens, but opacity tokens still apply. Text stays contrast or base through every state | `.ion-color.ion-focused`; measured `tokc` |
| 17 | Shape round: radius 999, padding 26 and 0. Size beats round's padding, in both directions | measured `round`, `rsmall`, `rlarge` |
| 18 | Size default box: 36 high, font 14, margin 2 and 4, padding 1.1 em and 8 | measured `def`, `sdef` |
| 19 | Size large: font 20, 56 high, padding 1 em and 14. Small: font 13, 27.3 high, padding 0.9 em and 4 | measured `large`, `small` |
| 20 | Expand block fills the width, keeping margin 2. Full also drops the radius and side borders, unless round | measured `block`, `full`, `fullo`, `fullr` |
| 21 | Strong: weight 700. Otherwise 500. Letter spacing 0.06 em. No kerning. Uppercase, in the display only. Line height 1, so a 14 px font has 14 px lines, centred in the box | measured `strong`, `def`, `long`; `font-kerning: none` |
| 22 | Long text wraps, centred, and grows the button: 4 lines is 72. A narrow parent wraps it to fit | measured `long`, `narrow` |
| 23 | Slotted icon: 1.35 em, no input. Start: margin −0.3 em and 0.3 em of its own size. End: 0.3 em and −0.2 em. Direct children only | measured `start`, `end`, `both`, `defic` |
| 24 | Icon-only: `:has-icon-only`, padding 0, min width and height clamped on font size: 40.04 by 40.04, icon 22.4. Small 28.08, icon 16. Large 50, icon 28. A slot change updates it. `--padding-top` sets all four | `slotChanged`; measured `only*` |
| 25 | Start and end don't shrink for long text, and sit at the edges of the centred group | measured `longic`, `blockic` |
| 26 | Disabled: opacity 0.5, no input, and a tap goes through to what's behind. In a disabled item it stays 1, or 0.5 if it's disabled itself, inside the item's 0.3 | `pointer-events: none`; measured `dis`, `initembtnd`, `inside`, `both` |
| 27 | Tokens: every variable applies, per side for padding, and `--opacity` is on the native part | measured `vars`, `varsr`, `varso` |
| 28 | In an item, `Size` null means small; `Size="Default"` stays standard; at any depth | `inItem`, `closest`; measured `initem`, `initemd`, `initemdeep` |
| 29 | Directly in an item's slot, a small button: font 12, 25 high, padding 0.6 em and 2, including icon-only, whose min width follows at 25.92 and icon at 14.77. Deeper, the plain small values | `::slotted(.button-small)`; measured `initem`, `initemo`, `initemssm`, `initemdeep` |
| 30 | Margins in an item: default slot 2 and 4; start 2, 4, 16, 4; end 16, 4, 2, 4 | `::slotted([slot=…])`; measured `initems`, `initeme` |
| 31 | The item treats it as a clickable: two buttons are multiple inputs, and its focus shows on the item | `setMultipleInputs`, `ion-focusable`; measured `it2`, `item while button focused` |
| 32 | A tap on a button in a button item clicks both. On a disabled one, only the item | measured `click initembtn`, `click initembtnd` |
| 33 | Automation: a button, named by its text in its original case, or `AutomationProperties.Name`. An icon gives no name. Can be invoked unless disabled | measured aria tree: `"Default"`, `"Named"`, unnamed icon-only, `"Favourite"` |
| 34 | RTL mirrors the slots and margins | measured `rtl` |
| 35 | Transitions: box shadow eases over 280 ms, background and colour over 15 ms, and the token overrides them | measured `tr` |
| 36 | Extensions: `Command`, `CommandParameter`, `TextTransform` | See below |

## Design

### Which class beats which

Avalonia has no specificity count: a rule with a pseudoclass beats one without, and then later beats earlier. Ionic's
state rules here all have the same specificity, so file order decides, and the theme uses CSS's order: focus, hover,
activated. That's why this differs from `ion-item`, whose hover rule carries `:not(.ion-focused)` and beats everything.

### One key, a default per class

`--background` has a different default per class, and a value set from outside beats all of them. So:

```xml
<Setter Property="Background">
    <MultiBinding Converter="{x:Static b:Css.Var}">
        <DynamicResource ResourceKey="Avonic.Button.Background" />
        <DynamicResource ResourceKey="Avonic.Color.PrimaryBrush" />
    </MultiBinding>
</Setter>
```

`Css.Var`'s values are bindings, so a number needs a key. Values with no variable of their own, such as solid's hover
opacity of 0.08, are `Avonic.Button.Md.*` keys in `Tokens/Button.axaml`. They're defaults, not one-to-one with Ionic's
variables, and an app overrides the variable's key instead.

`Css.Var` returning nothing gives a property's default, so `fill="default"`'s `opacity: var(--unset)` gets Avalonia's 1,
as CSS gets its initial 1. Measured `fdef:hover`, `after 1`.

### Colour lives on two elements, as in CSS

The host's `color` is the state colour. `.button-native`'s `color` inherits it, except on `.ion-color`. The theme does
the same: the host's `Foreground` follows state, and `PART_Native`'s `TextElement.Foreground` is only set on
`:ion-color`, falling back to the host. This is what gets `tokbad` right, where an unknown colour still shows
`--color-hover`, without a rule per state. It's also where the transition goes, because the native part is what eases.

### Padding

`--padding-start: 1.1em` means a key's value can't be plain px and still be relative. So a key is px, as an override,
and the default is the converter's parameter in CSS's terms:

```xml
<MultiBinding Converter="{x:Static c:AvonicButton.PaddingConverter}" ConverterParameter="1.1em 8">
    <Binding Path="FontSize" RelativeSource="{RelativeSource TemplatedParent}" />
    <DynamicResource ResourceKey="Avonic.Button.PaddingStart" />
    ...
```

The parameter `icon-only` is the chain in `:host(.button-has-icon-only)`, where `--padding-bottom` and `--padding-end`
follow `--padding-top`, and `--padding-start` follows `--padding-end`. Measured `onlyv`: `--padding-top: 5px` sets all four.

Five converters for the theme are new, and they live on `AvonicButton` because `Behaviors/` is shared: `PaddingConverter`,
`ClampConverter` for `clamp(30px, 2.86em, 60px)`, `IconMarginConverter` for a margin in the icon's own em,
`NoSideBordersConverter` for full width, and `Css.HalfLeading`, below. `TextTransformConverter` is for the template.

### Layout

`.button-inner` is a flex row with centred content, where start and end don't shrink and text does. That's
`ButtonInner`, a small panel in the control's folder. A `Grid` with `*` would push start and end to the edges of a block
button, where they stay beside the text (measured `blockic`). A `Grid` with `Auto` would never wrap. A `DockPanel` was the
first try, and it wrapped, but the group then shrank to its longest line and the icons moved in: 20 against 11.73,
measured `longic`.

The same panel gives `display: inline-block`'s shrink-to-fit: as wide as the content on one line, or everything available
if that doesn't fit. Measured `long`: 2 to 388, where Avalonia alone would give the width of the longest line.

`display: inline-block` is `HorizontalAlignment="Left"`, and `vertical-align: -webkit-baseline-middle` is `Center`,
measured: a 36 and a 56 in one line share a centre at y = 164. Avalonia has no inline flow, so a row of buttons is a
horizontal panel or a `WrapPanel`.

### In an item

`Styles/` files merge in ordinal order, so `Button.axaml` comes before `Item.axaml`, and the item's plain
`ContentPresenter#PART_End > :is(Control)` would win over a plain rule here. A rule with a pseudoclass beats one without,
so the margin rules target `c|AvonicButton:in-item`, which the control sets. It's Ionic's `inItem`.

`closest` in `componentWillLoad` is once only. Here it's every time it attaches, as `AvonicLabel` does for its report,
because recycled containers are normal in Avalonia.

`::slotted(ion-button)`'s `--margin-*` is dead: no stylesheet reads them. Its `z-index: 1` keeps a button above the item's
ripple, which a slot can't do here. See the gaps.

### Interaction

A native `<button>` clicks on release if the press and the release both landed on it, and nothing else matters. Measured:
a drag of 150 px within it clicks, a drag off and back clicks, two quick presses click twice, and disabling it mid-press doesn't.
A second finger means no click, whichever lifts first, and so does a cancelled touch. With two mouse buttons, what counts is
the order of the releases: if the left is released first, it clicks, and if the other one is, it doesn't. Measured in all four orders.

**Why not `Tapped`.** Since #72 the item and the card share this, in `Behaviors/NativeButton.cs`. `Tapped` has a small movement limit, so a shaky press does nothing.
It isn't raised for a second quick press, and `DoubleTapped`, which is, also needs the last press to have had the same
source, so a quick press after a tap that landed on something else is lost. A button tracks the press itself instead: armed in
a bubbling `PointerPressed`, and a click in `PointerReleased` if it's the same pointer and inside. Clicking on `DoubleTapped`,
which the item rejected because a finger going down to scroll would activate it, isn't what this does: a gesture that takes the
pointer means the release never comes here.

**A child that handles `PointerPressed` stops the click,** every time, as `stopPropagation` does in Ionic (measured, on one tap and
on two quick ones), and as Avalonia's own `Button` does. It doesn't handle anything itself, so an item around it still sees the
tap, which is what measured `click initembtn` needs.

### Line height

`line-height: 1` is `TextBlock.LineHeight` set to the font size, which inherits. It needed two more things, both found by
looking at the frames: a `TextBlock` clips to its line height, and Avalonia keeps a line with a smaller `LineHeight` at the
top, where CSS splits the leading above and below. That put text 3 px low in a 36 px button. `Css.HalfLeading`
works out half the leading from the font's own metrics, and a margin moves the text by that without changing its box.

### Clipping

A `TemplatedControl` clips by default, and a `Border`'s own `ClipToBounds` clips its own `BoxShadow`. Measured on a plain
`Border` under a 36 px box, the shadow reads 164, 219, 235, and with either clip on it reads 255. So the host turns it off,
and `overflow: hidden` is a part of its own inside the native part, `PART_Clip`. It takes the native part's radius, not the
smaller inner radius CSS gets by subtracting the border, so on an outline's 2 px border the clip is a little rounder than the
border's inside edge. The overlay is 4% to 12% and a corner is a few pixels, so it isn't visible in the frames.

### Uppercase

Avalonia has no `text-transform`. A string in a slot goes through a converter on its way to the presenter, so the shown
text is uppercase and `Content` isn't. That leaves two things: an app needs `text-transform: none`, which is a common
override in Ionic, and the converter can't reach text it doesn't own. So it's the `TextTransform` extension, set by the
theme, and a `TextBlock` or a template's own text in a slot isn't transformed. Those are recorded below.

## Extensions

Avonic-only, and never named like an Ionic member.

- **`Click`**, a routed event, as on `AvonicItem`. Ionic's button has no click event of its own, because the native
  `<button>` has one. Not `IonClick`.
- **`Command` and `CommandParameter`.** Avalonia apps expect these on anything button-like, and MVVM has no other way in.
  They follow `Avalonia.Controls.Button`: `Click` first, and a handler that sets `Handled` stops the command. `CanExecute`
  false disables the button through `IsEnabledCore`, so it looks and acts disabled and `IsEnabled` isn't touched. `CanExecute`
  is checked again when the parameter changes or `CanExecuteChanged` fires, and only while attached, so a long-lived
  command doesn't hold on to a removed button. **Not included:** `HotKey`, `IsDefault`, `IsCancel`, `ClickMode`. None has
  an Ionic counterpart, and they can come later if asked for.
- **`TextTransform`** (`ButtonTextTransform`: `None`, `Uppercase`). CSS's `text-transform` as a property, because there's
  no other way to turn it off. The stub leaves it `None`, and the md theme sets `Uppercase`.

## Deliberate differences

| Difference | Why |
|---|---|
| `href`, `download`, `rel`, `target`, router props, `type`, `form`, `buttonType` aren't declared | Each would be a dead prop |
| `Avonic.Button.Padding*` is px only | An em default stays relative, and an override is a plain number. Use a binding for a relative one |
| A row of buttons is a panel, not inline flow | Avalonia has no inline flow |
| A button in a disabled panel fades to 0.5 too, where a disabled `fieldset` does nothing to an `ion-button` (measured: opacity 1, and it still clicks) | In Avalonia a disabled ancestor disables the control, and it should look it. The exception is a disabled item, which has its own opacity, measured |
| A nested control that handles its press stops the click, except Avalonia's own `Button`, which always does, and a native `<button>` inside clicks both, measured | A handled `Click` from inside stops it too, since #72, which is `click.stopPropagation()` |
| A word wider than the button breaks onto more lines, where Chromium's button grows past its parent: 99.6 of 96 (measured `narrow`) | A control can't be measured wider than its parent, and Avalonia 12.1.4 breaks the word even with `WrapWithOverflow`, measured 4 lines |
| Two block or full buttons in a column are 8 apart, where CSS collapses their margins to 4 | Avalonia has no margin collapsing, as in `ion-label` |
| Solid's shadow is lighter at 2x: the first row under the button is 205, where Chromium's is 165, and it fades over about the same distance | Avalonia's own shadow rendering, with the same values. At 1x it reads 164 |
| `VerticalAlignment` is `Center`, where a flex parent stretches an Ionic button (measured `tall`, 92) | Set `VerticalAlignment="Stretch"`; content stays centred |
| A button moved to another item follows the new one | Ionic only checks on first load. Recycled containers are normal in Avalonia |
| Plain programmatic `Focus()` doesn't show `:ion-focused` | As `ion-item`: Avalonia reports how focus arrived |

## Known gaps

| Gap | Why |
|---|---|
| `TextTransform` only reaches strings, not a `TextBlock` or a template's own text. In CSS it inherits, measured on a nested `span` | Avalonia has no `text-transform`, and text it doesn't own can't be changed |
| `ß` stays `ß` where CSS gives `SS` (measured `ss`), and `ﬁ` stays where CSS gives `FI` | .NET's `ToUpperInvariant` maps one character to one |
| A nested `TextBlock` with its own `FontSize` gets the button's line height | `TextBlock.LineHeight` inherits as a size, as in `ion-label` |
| A slotted icon with `Size` set takes its margin from `FontSize`, not its real size | The real size isn't public. At `IconSize.Large` it's about 4 px |

## Not in this PR

| What | Goes with |
|---|---|
| `inToolbar`: `fill` null → clear; `rippleType` unbounded for clear and icon-only; `in-toolbar`, `in-toolbar-color`, `in-buttons` and their colour rules | #12 (`ion-buttons`), and the toolbar's issue |
| `inListHeader`: `fill` null → clear | `ion-list-header` |
| `closest('ion-item-divider')` for `inItem` | `ion-item-divider` |
| `:host(.button-outline.ion-activated.ion-color) .button-native` | Nothing: it sets `background: transparent` where it already is |
| The default text size outside a host | Open project question. A button sets its own, so it isn't affected |

## Dependencies

`ion-icon` and `ion-ripple-effect` are done, and so is `ion-item`, for `IItemControl`, `Activatable`, `FocusVisible` and `Css`.
