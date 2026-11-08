# ion-button

Status: done · [#10](https://github.com/wdcossey/Avonic/issues/10)

`AvonicButton` is Ionic's button: solid, outline or clear, round, three sizes, block or full width, with a colour and icons
in slots. Inside an [`AvonicItem`](../ion-item/README.md) it's small by default, and the item treats it as a clickable. The
design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicButton Content="Save" Click="OnSave" />
<c:AvonicButton Content="Save" Command="{Binding Save}" CommandParameter="{Binding Draft}" />

<c:AvonicButton Content="Delete" Color="danger" Fill="Outline" Shape="Round" />
<c:AvonicButton Content="Continue" Expand="Block" Size="Large" Strong="True" />

<c:AvonicButton Content="Favourite">
    <c:AvonicButton.StartContent>
        <c:AvonicIcon IconName="heart" />
    </c:AvonicButton.StartContent>
</c:AvonicButton>

<!-- With no text, say what it does. -->
<c:AvonicButton AutomationProperties.Name="Favourite">
    <c:AvonicButton.IconOnlyContent>
        <c:AvonicIcon IconName="heart" />
    </c:AvonicButton.IconOnlyContent>
</c:AvonicButton>

<c:AvonicItem Content="Small, because it's in an item">
    <c:AvonicItem.EndContent>
        <c:AvonicButton Content="Edit" />
    </c:AvonicItem.EndContent>
</c:AvonicItem>
```

Avalonia has no inline flow, so a row of buttons is a horizontal `StackPanel` or a `WrapPanel`.

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | See the [colour system](../../infra/color-system/README.md) |
| [x] | `disabled` | `IsEnabled` | |
| [x] | `expand` | `Expand` | `Block`, `Full`, or null |
| [x] | `fill` | `Fill` | `Clear`, `Default`, `Outline`, `Solid`, or null, which is solid, or clear in an `AvonicButtons` or an `AvonicListHeader`. `Default` is a class with no rules: no background, inherited text, and it ignores `Color`, as measured |
| [x] | `shape` | `Shape` | `Round`, or null |
| [x] | `size` | `Size` | `Default`, `Large`, `Small`, or null, which is small inside an item or a divider |
| [x] | `strong` | `Strong` | |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `buttonType` | — | Not applicable: it only renames the host classes |
| [ ] | `href`, `routerAnimation`, `routerDirection` | — | Out of scope: router. Use `Click` |
| [ ] | `download`, `rel`, `target` | — | Not applicable: only apply with `href` |
| [ ] | `type`, `form` | — | Not applicable: HTML form |

### Events

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionFocus` | `IonFocus`, `IonFocusEvent` | Any focus, including a press. It bubbles |
| [x] | `ionBlur` | `IonBlur`, `IonBlurEvent` | Disabling a focused button blurs it |

There are no methods.

### Slots

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | default | `Content`, `ContentTemplate` |
| [x] | `start` | `StartContent`, `StartContentTemplate` |
| [x] | `end` | `EndContent`, `EndContentTemplate` |
| [x] | `icon-only` | `IconOnlyContent`, `IconOnlyContentTemplate` |

A slot holds one child. An icon directly in a slot gets Ionic's sizes and margins, and one nested deeper doesn't.

### Parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `native` | `PART_Native` (`Border`): background, border, shadow, radius |

Also `PART_Clip` (`overflow: hidden`), `PART_Overlay` (`::after`), `PART_Ripple`, `PART_Inner` (`.button-inner`, a
`ButtonInner`, with the padding as its margin), `PART_IconOnly`, `PART_Start`, `PART_Content` and `PART_End`.

### Resource keys

A key with no value here is `CssUnset`, and its default depends on the class. Any value overrides the default for every
class, as a variable set from outside does. A known `Color` still beats `Background`, `Color`, `BorderColor`,
`BackgroundFocused` and `BackgroundHover`, as in Ionic.

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Button.Background` | brush | Solid: primary |
| [x] | `--background-activated` | `Avonic.Button.BackgroundActivated` | brush | transparent |
| [x] | `--background-activated-opacity` | `Avonic.Button.BackgroundActivatedOpacity` | `double` | 0 |
| [x] | `--background-focused` | `Avonic.Button.BackgroundFocused` | brush | Solid: primary contrast. Outline, clear: primary |
| [x] | `--background-focused-opacity` | `Avonic.Button.BackgroundFocusedOpacity` | `double` | Solid 0.24. Outline, clear 0.12 |
| [x] | `--background-hover` | `Avonic.Button.BackgroundHover` | brush | Solid: primary contrast. Outline, clear: primary |
| [x] | `--background-hover-opacity` | `Avonic.Button.BackgroundHoverOpacity` | `double` | Solid 0.08. Outline, clear 0.04 |
| [x] | `--border-color` | `Avonic.Button.BorderColor` | brush | Outline: primary. Only read on outline |
| [x] | `--border-radius` | `Avonic.Button.BorderRadius` | `CornerRadius` | 4. Round 999. Full and not round is 0, which beats it |
| [ ] | `--border-style` | — | | Not applicable: Avalonia's `Border` only draws solid |
| [x] | `--border-width` | `Avonic.Button.BorderWidth` | `Thickness` | Outline: 2. Only read on outline |
| [x] | `--box-shadow` | `Avonic.Button.BoxShadow` | `BoxShadows` | Solid: 2 dp, 8 dp while pressed |
| [x] | `--color` | `Avonic.Button.Color` | brush | Solid: primary contrast. Outline, clear: primary. Default: inherits |
| [x] | `--color-activated` | `Avonic.Button.ColorActivated` | brush | Follows `Color` |
| [x] | `--color-focused` | `Avonic.Button.ColorFocused` | brush | Follows `Color` |
| [x] | `--color-hover` | `Avonic.Button.ColorHover` | brush | Follows `Color` |
| [x] | `--opacity` | `Avonic.Button.Opacity` | `double` | 1. On the native part |
| [x] | `--padding-top`, `-bottom` | `Avonic.Button.PaddingTop`, `…Bottom` | `double`, px | 8. Round 0, large 14, small 4, icon-only 0 |
| [x] | `--padding-start`, `-end` | `Avonic.Button.PaddingStart`, `…End` | `double`, px | 1.1 em. Round 26, large 1 em, small 0.9 em, icon-only 0 |
| [x] | `--ripple-color` | `Avonic.Button.RippleColor` | brush | The current colour |
| [x] | `--transition` | `Avonic.Button.Transition` | `Transitions` | Box shadow 280 ms; background and colour 15 ms |

`Avonic.Button.Md.*` holds what `button.md.css` gives those variables per class, because `Css.Var`'s values are bindings and
a number needs a key. They aren't Ionic's variables: override the keys above instead.

## Behaviour

The full list is in [PLAN.md](PLAN.md#behaviours). The ones that might surprise you:

- **Later beats earlier.** Hover beats focus, and a press beats hover, so pressing with the default tokens shows no overlay,
  only the ripple. `ion-item` goes the other way, because its rule has higher specificity. Both are measured.
- **An unknown `Color`** paints no background and leaves the text as it was, so a solid one is white text on nothing, with a
  shadow. It's what Ionic does. Inside a coloured ancestor it paints that ancestor's base.
- **A disabled button lets a tap through** to what's behind it, as `pointer-events: none` does. A disabled ancestor fades it to
  0.5 too, except a disabled item, which has its own 0.3.
- **A press and a release that both land on the button click it,** however far the pointer went between them, unless a child
  handles `PointerPressed`, which is how to stop it, as `stopPropagation` does in Ionic.
- **In an item, directly in a slot,** a small button has font 12 and height 25. Nested deeper it has Ionic's plain small values,
  13 and 27.3. A panel directly in a slot counts as the slot.
- **In an item divider** it's small too, as Ionic's `inItem` looks for either, but with the plain small values, 13 and 27.3, as
  a divider has no rule for a small button. In a start or end slot of either, it keeps its own 2 and 4 on the sides the slot
  doesn't set, and an app's plain `Margin` style beats that, as in CSS, except beside a stacked or floating label.

## Extensions

Avonic-only. None of these exist in Ionic.

| Extension | What it does |
|---|---|
| `Click`, `ClickEvent` | A routed event, raised by a tap on release, Enter on press, Space on release, or automation. Ionic's button has none of its own because the native `<button>` has one |
| `Command`, `CommandParameter` | As on Avalonia's `Button`. It runs after `Click`, unless a handler set `Handled`. While it can't execute, the button is disabled, `IsEnabled` is untouched, and it asks again when `CanExecuteChanged` fires or the parameter changes |
| `TextTransform` | CSS's `text-transform`, for text given as a string: `None` or `Uppercase`. The theme sets `Uppercase`, and `None` is what `text-transform: none` does in Ionic |

## Known gaps

- `TextTransform` only reaches strings, not a `TextBlock` or a template's own text. In CSS it inherits. Avalonia has no
  `text-transform`, and text it doesn't own can't be changed.
- `ß` stays `ß` where CSS gives `SS`, and `ﬁ` stays where CSS gives `FI`, because .NET maps one character to one.
- A nested `TextBlock` with its own `FontSize` gets the button's line height, because `TextBlock.LineHeight` inherits as a size.
- A slotted icon with `Size` set takes its margin from `FontSize`, not its real size. At `IconSize.Large` that's about 4 px.
- Solid's shadow is lighter at 2x than Chromium's: 205 against 165 on the first row under the button.

## In a toolbar

`AvonicButtons` (#12) turns a null `Fill` into clear, and a toolbar has its own colour rules and an unbounded ripple. They're in
[`ion-toolbar`](../ion-toolbar/README.md), and they read three pseudoclasses the button works out itself, as `button.js`'s
`hostContext` does (#89):

| Ionic class | Pseudoclass | True when |
|---|---|---|
| `in-toolbar` | `:in-toolbar` | Any `AvonicToolbar` is a visual ancestor |
| `in-toolbar-color` | `:in-toolbar-color` | Any `AvonicToolbar` around it has a `Color`, even an empty one, as `ion-toolbar[color]` matches. Not only the nearest |
| `in-buttons` | `:in-buttons` | Any `AvonicButtons` is a visual ancestor |

They're read at each render, as Ionic does, and here a render is a change of `Fill`, `Size`, `Shape`, `Expand`, `Strong`,
`IconOnlyContent`, `Color` or `IsEnabled`. So a toolbar's `Color` set later shows at the button's next render, as measured in
Ionic. They're also read when the button is attached, where Ionic keeps a moved button's until its next render (measured), because
a moved button's old place may be gone. A container a virtualizing panel recycles stays attached, so a toolbar `Color` bound to
its item also waits for the button's next render, and so does a toolbar `Color` whose binding only resolves after the buttons are
attached, such as from a `DataContext` set later. Ionic's frameworks usually set the attribute before the first render, so it
shows less there. Until the button renders, it keeps the uncoloured toolbar's rules: with the default tokens, only a pressed outline
button directly in the toolbar shows it, white where it would stay transparent. They follow the visual tree, so a button in a flyout opened from a toolbar isn't
in it, as a popover's content isn't inside the toolbar in Ionic's DOM.

## In a list header

A null `Fill` is clear inside an `AvonicListHeader`, at any depth, as Ionic's `closest('ion-list-header')` makes it. A `Fill` you
set wins. The header's `Color` doesn't reach it: in a danger header its text stays primary, measured. Ionic reads it once, on
first load; here it's read each time the button is attached, as for an item, so a moved button follows.
