# ion-item

Status: done · [#6](https://github.com/wdcossey/Avonic/issues/6)

`AvonicItem` is a row with start, default and end content, a line under it and an optional detail icon. It can be
tappable, and it's what form controls inside it report to. The design and evidence are in [PLAN.md](PLAN.md), and the
comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicItem Content="A row" />

<c:AvonicItem Content="Settings" Button="True" Detail="True" Click="OnSettingsClick">
    <c:AvonicItem.StartContent>
        <c:AvonicIcon IconName="settings-outline" />
    </c:AvonicItem.StartContent>
</c:AvonicItem>

<c:AvonicItem Content="Danger" Color="danger" Lines="None" />
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `button` | `Button` | Tappable, with a ripple, hover and focus states, and a tab stop |
| [x] | `color` | `Color` | See the [colour system](../../infra/color-system/README.md) |
| [x] | `detail` | `Detail` | Only `true` shows it, which is md's rule |
| [x] | `detailIcon` | `DetailIcon` | A name or a source. Default `chevron-forward`, which flips in RTL |
| [x] | `disabled` | `IsEnabled` | |
| [x] | `lines` | `Lines` | `Full`, `Inset`, `None`, or null for the default |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `href`, `download`, `rel`, `target` | — | Out of scope: router. Use `Button` and `Click` |
| [ ] | `routerAnimation`, `routerDirection` | — | Out of scope: router |
| [ ] | `type` | — | Not applicable: HTML form |

### Slots and parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | default slot | `Content`, `ContentTemplate` |
| [x] | `start` | `StartContent`, `StartContentTemplate` |
| [x] | `end` | `EndContent`, `EndContentTemplate` |
| [x] | `native` | `PART_Native` |
| [x] | `inner` | `PART_Inner` |
| [x] | `container` | `PART_Container` |
| [x] | `detail-icon` | `PART_DetailIcon` |

Each slot holds one child. To put several in a slot, wrap them in a panel. Ionic's rules for slotted content apply
to direct children only, so they don't reach inside the panel, as `::slotted` doesn't.

### Layout

The parts are flex rows, as Ionic's are, and long content in any slot wraps.

- **A label in the start or default slot is `flex: 1`.** So a start label takes half the row, however short it is, and its row is shared with the default slot.
- **Anything else in a slot is as wide as its content,** down to its longest word. A long one takes all it can, and squeezes the default slot, to nothing if it has to.
- **What's in a slot is centred,** at the top beside a stacked or floating label, and stretched in a textarea item. A control that sets its own `VerticalAlignment` gets that, as `align-self` does. A note does.
- **A panel fills its slot,** and what's in it is centred, except a vertical stack, which is a group that's centred as one. So a child with no height of its own collapses, as it does in CSS, and a `Grid` holding a background behind text needs `VerticalAlignment="Stretch"` on the background, or a `Border` around the `Grid`, which isn't a panel.
- **For a long note and an icon in one slot, use a `Grid` with `*,Auto`.** A horizontal `StackPanel` gives its children all the width they ask for, so they don't wrap.

A squeezed label wraps at every character, which makes its row tall, as Ionic's does.

### Pseudoclasses

| Ionic | Avonic |
|---|---|
| `item-lines-default`, `-full`, `-inset`, `-none` | `:lines-default`, `:lines-full`, `:lines-inset`, `:lines-none` |
| `ion-color` | `:ion-color` |
| `ion-activatable` | `:ion-activatable` |
| `ion-activated` | `:activated` |
| `ion-focused` | `:ion-focused` |
| `:hover`, with `any-hover` | `:ion-hover`. Mouse and pen only |
| `item-disabled` | `:disabled` |
| `item-multiple-inputs` | `:item-multiple-inputs` |
| `item-control-needs-pointer-cursor` | `:item-control-needs-pointer-cursor` |
| `item-<key>`, from `ionStyle` | `:item-<key>` |

`:button` is an addition, for the theme's cursor. Ionic's CSS matches the `<button>` element.

## Resource keys

One per Ionic variable. A key whose default is "unset" falls back as Ionic's does, so overriding `Avonic.Item.Color`
also moves the hover colour. Any brush replaces it.

| ✓ | Ionic | Key | Type | Default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Item.Background` | brush | unset → `Avonic.ItemBackgroundBrush` |
| [x] | `--background-activated` | `Avonic.Item.BackgroundActivated` | brush | transparent |
| [x] | `--background-activated-opacity` | `Avonic.Item.BackgroundActivatedOpacity` | `double` | 0 |
| [x] | `--background-focused` | `Avonic.Item.BackgroundFocused` | brush | unset → current colour |
| [x] | `--background-focused-opacity` | `Avonic.Item.BackgroundFocusedOpacity` | `double` | 0.12 |
| [x] | `--background-hover` | `Avonic.Item.BackgroundHover` | brush | unset → current colour |
| [x] | `--background-hover-opacity` | `Avonic.Item.BackgroundHoverOpacity` | `double` | 0.04 |
| [x] | `--border-color` | `Avonic.Item.BorderColor` | brush | unset → `Avonic.ItemBorderColorBrush` |
| [x] | `--border-radius` | `Avonic.Item.BorderRadius` | `CornerRadius` | 0 |
| [ ] | `--border-style` | — | | Not applicable: `Border` only draws solid |
| [x] | `--border-width` | `Avonic.Item.BorderWidth` | `Thickness` | 0 |
| [x] | `--color` | `Avonic.Item.Color` | brush | unset → `Avonic.ItemColorBrush` |
| [x] | `--color-activated` | `Avonic.Item.ColorActivated` | brush | unset → `--color` |
| [x] | `--color-focused` | `Avonic.Item.ColorFocused` | brush | unset → `--color` |
| [x] | `--color-hover` | `Avonic.Item.ColorHover` | brush | unset → `--color` |
| [x] | `--detail-icon-color` | `Avonic.Item.DetailIconColor` | brush | unset → current colour |
| [x] | `--detail-icon-font-size` | `Avonic.Item.DetailIconFontSize` | `double`, in em | 1.25 |
| [x] | `--detail-icon-opacity` | `Avonic.Item.DetailIconOpacity` | `double` | 0.25 |
| [x] | `--inner-border-width` | `Avonic.Item.InnerBorderWidth` | `Thickness` | `0,0,0,1` |
| [x] | `--inner-box-shadow` | `Avonic.Item.InnerBoxShadow` | `BoxShadows` | none |
| [x] | `--inner-padding-top` | `Avonic.Item.InnerPaddingTop` | `double` | 0 |
| [x] | `--inner-padding-bottom` | `Avonic.Item.InnerPaddingBottom` | `double` | 0 |
| [x] | `--inner-padding-start` | `Avonic.Item.InnerPaddingStart` | `double` | 0 |
| [x] | `--inner-padding-end` | `Avonic.Item.InnerPaddingEnd` | `double` | 16 |
| [x] | `--min-height` | `Avonic.Item.MinHeight` | `double` | 48 |
| [x] | `--padding-top` | `Avonic.Item.PaddingTop` | `double` | 0 |
| [x] | `--padding-bottom` | `Avonic.Item.PaddingBottom` | `double` | 0 |
| [x] | `--padding-start` | `Avonic.Item.PaddingStart` | `double` | 16 |
| [x] | `--padding-end` | `Avonic.Item.PaddingEnd` | `double` | 0 |
| [x] | `--ripple-color` | `Avonic.Item.RippleColor` | brush | unset → current colour |
| [x] | `--transition` | `Avonic.Item.Transition` | `Transitions` | opacity and background, 15 ms |

Global variables only an item reads: `Avonic.ItemColorBrush` (`--ion-item-color`) and `Avonic.ItemBorderColorBrush`
(`--ion-item-border-color`). `Avonic.ItemBackgroundBrush` is in the [colour system](../../infra/color-system/README.md).

An explicit `Lines` beats `BorderWidth` and `InnerBorderWidth`, which are the default for an item without one. In CSS
a variable set by the app would win. [PLAN.md](PLAN.md#deliberate-differences) says why it doesn't here.

## Extensions

| Member | What it's for |
|---|---|
| `Click`, `ClickEvent` | Raised when a `Button` item is tapped or activated by keyboard. A native `<button>` gives Ionic this. `Tapped` works on any item |

## For control authors

### Knowing about the item

A control the item should know about implements `IItemControl`, so the item doesn't need a reference to it:

```csharp
public class AvonicToggle : TemplatedControl, IItemControl
{
    public ItemControlType ItemControlType => ItemControlType.Toggle;

    // A tap on the item's padding, when this is the first interactive control in it.
    public void OnItemTap() => Toggle();
}
```

| The type | Effect in an item |
|---|---|
| `Checkbox`, `Datetime`, `Select`, `Radio` | One of these alone makes the item activatable |
| `Toggle`, `Checkbox`, `Radio`, `Select`, `Input`, `Textarea` | Can receive a tap on the padding |
| `Input`, `Textarea` | Don't get a pointer cursor over the item |
| `Button` | Avalonia's `Button` counts without implementing this |

A control with `FocusVisible.IsEnabled` makes an item around it focusable too, so the item shows `:ion-focused` when
focus reaches the control by keyboard.

### Telling the item about yourself

Raise `IonStyle`, Ionic's internal `ionStyle`. Each true key sets `:item-<key>`. Keys are kept per type of control, so
this replaces the keys from the last time a control of this type raised it:

```csharp
RaiseEvent(new IonStyleEventArgs(new Dictionary<string, bool>
{
    ["interactive"] = true,
    ["interactive-disabled"] = !IsEnabled,
}));
```

The item's own rules read `interactive`, `interactive-disabled`, `label-stacked`, `label-floating` and `textarea`. It also
works out which control is the first interactive one again, so raise this when a control's `IsEnabled` changes.

### Slotted styles

Rules like `::slotted(ion-avatar)` are in `item.md.css`, and their controls don't exist yet. Each goes in that
control's own `src/Avonic.Themes/Styles/<Name>.axaml`:

```xml
<!-- ::slotted(ion-avatar[slot=start]) { margin-inline-end: 16px } -->
<Style Selector="c|AvonicItem /template/ ContentPresenter#PART_Start > c|AvonicAvatar">
    <Setter Property="Margin" Value="0,8,16,8" />
</Style>
```

`FlexRow` and `FlexSlot` in `Controls/Flex/` are `display: flex` and `<slot>`, for a host with parts like these. `FlexRow.Flex` is `flex`, as one
number, so a slotted control's file can say `<Setter Property="c:FlexRow.Flex" Value="1" />`.

`Css.Var`, `Css.Em`, `Css.Box` and `CssUnset` in `Behaviors/Css.cs` translate the CSS values Avalonia has no equivalent
for. `src/Avonic.Themes/Controls/AvonicItem.axaml` uses all of them.
