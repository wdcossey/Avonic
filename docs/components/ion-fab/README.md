# ion-fab, ion-fab-button, ion-fab-list

Status: done · [#30](https://github.com/wdcossey/Avonic/issues/30)

`AvonicFab` is a floating action button, placed over the content from `AvonicContent.FixedContent`. Its `AvonicFabButton` opens
the `AvonicFabList`s beside it, whose smaller buttons appear one by one. The design and evidence are in [PLAN.md](PLAN.md), and the
comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicContent>
    <c:AvonicContent.FixedContent>
        <c:AvonicFab Horizontal="End" Vertical="Bottom">
            <c:AvonicFabButton>
                <c:AvonicIcon IconName="add" />
            </c:AvonicFabButton>
            <c:AvonicFabList Side="Top">
                <c:AvonicFabButton Click="OnShare">
                    <c:AvonicIcon IconName="share" />
                </c:AvonicFabButton>
                <c:AvonicFabButton Content="A" />
            </c:AvonicFabList>
        </c:AvonicFab>
    </c:AvonicContent.FixedContent>
</c:AvonicContent>
```

A tap on the main button opens the lists, and a tap on any button in them closes the fab. A fab with no list doesn't open.

`FixedContent` holds one child. For several fabs, put them in a `Panel` with `HorizontalAlignment` and `VerticalAlignment`
`Stretch`, which covers the content as the slot does. For an edge fab to paint over the header or footer, the content has to be
above them: give it a `ZIndex` over theirs, which is 10. Avalonia's `ZIndex` only sorts siblings, so this also covers the header's
shadow. See [COMPARISON.md](COMPARISON.md).

## ion-fab — `AvonicFab`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `activated` | `Activated` | The first fab button's and every list's, at load and on change. Two-way, as the fab sets it itself |
| [x] | `edge` | `Edge` | With `Vertical` top or bottom, half the button out of the content |
| [x] | `horizontal` | `Horizontal` (`FabHorizontal?`) | `Start`, `Center`, `End`: 10 in from the side, plus the safe area, swapped in RTL |
| [x] | `vertical` | `Vertical` (`FabVertical?`) | `Top`, `Center`, `Bottom`: 10 in, with no safe area |
| [x] | `close()` | `Close()` | |
| [x] | `toggle()` | internal | `@internal` in Ionic |

The default slot holds several children and `core.json` lists no slot, so they're `Items`. A control is its own container, so
the button and the lists are the fab's logical children. `FabPanel`, its items panel, lays them out: the button in the flow, the
lists out of it, beside the button. With neither `Horizontal` nor `Vertical`, the fab stays where its container puts it, at its
top start in `FixedContent`. Pseudoclasses: `:horizontal-start`, `:horizontal-center`, `:horizontal-end`, `:vertical-top`,
`:vertical-center`, `:vertical-bottom`, `:edge`.

## ion-fab-button — `AvonicFabButton`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `activated` | `Activated` | The close icon turns in and the content out, over 300 ms. The fab sets it |
| [x] | `closeIcon` | `CloseIcon` | `"close"` |
| [x] | `color` | `Color` | See the [colour system](../../infra/color-system/README.md) |
| [x] | `disabled` | `IsEnabled` | 0.5, and no input |
| [x] | `show` | `Show` | In a list only. The list sets it |
| [x] | `size` | `Size` (`FabButtonSize?`) | `Small`: 40 with 8 margins |
| [ ] | `href`, `download`, `rel`, `target`, `routerAnimation`, `routerDirection` | — | Out of scope: router, as on `ion-button`. Use `Click` |
| [ ] | `translucent` | — | Not applicable: only ios draws it |
| [ ] | `type` | — | Not applicable: HTML form |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `ionFocus`, `ionBlur` | `IonFocus`, `IonBlur` | |
| [x] | default slot | `Content` | A `ContentControl`, so the content is the button's logical child |
| [x] | part `native` | `PART_Native` | `Border`: background, radius, shadow, the content's colour |
| [x] | part `close-icon` | `PART_CloseIcon` | `AvonicIcon` |

The template also has `PART_Overlay` (`::after`), `PART_Ripple` and `PART_ContentPresenter` (`.button-inner`). It's activatable
and focus-visible: `:activated` while pressed, `:ion-focused`, `:ion-hover`. Its own pseudoclasses: `:close-active`, `:show`,
`:size-small`, and `:in-list` in any `AvonicFabList`, where it's 40 square, light, and hidden until `Show`, which takes no tap and no
focus.

### Resource keys

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.FabButton.Background` | brush | Primary; in a list, light |
| [x] | `--background-activated` | `Avonic.FabButton.BackgroundActivated` | brush | Transparent |
| [x] | `--background-activated-opacity` | `Avonic.FabButton.BackgroundActivatedOpacity` | `double` | 0 |
| [x] | `--background-focused` | `Avonic.FabButton.BackgroundFocused` | brush | The current colour; in a list, light shade |
| [x] | `--background-focused-opacity` | `Avonic.FabButton.BackgroundFocusedOpacity` | `double` | 0.24 |
| [x] | `--background-hover` | `Avonic.FabButton.BackgroundHover` | brush | The current colour; in a list, light tint |
| [x] | `--background-hover-opacity` | `Avonic.FabButton.BackgroundHoverOpacity` | `double` | 0.08 |
| [x] | `--border-color` | `Avonic.FabButton.BorderColor` | brush | The current colour |
| [x] | `--border-radius` | `Avonic.FabButton.BorderRadius` | `CornerRadius`, number or CSS string | `50%` |
| [ ] | `--border-style` | — | | Not applicable: Avalonia's `Border` only draws solid |
| [x] | `--border-width` | `Avonic.FabButton.BorderWidth` | `Thickness` | 0 |
| [x] | `--box-shadow` | `Avonic.FabButton.BoxShadow` | `BoxShadows` | 6 dp; 12 dp pressed |
| [x] | `--close-icon-font-size` | `Avonic.FabButton.CloseIconFontSize` | `double` | 24 |
| [x] | `--color` | `Avonic.FabButton.Color` | brush | Primary contrast; in a list, `rgba(text, 0.54)` |
| [x] | `--color-activated` | `Avonic.FabButton.ColorActivated` | brush | Follows `Color` |
| [x] | `--color-focused` | `Avonic.FabButton.ColorFocused` | brush | Follows `Color`; in a list, `ColorActivated` |
| [x] | `--color-hover` | `Avonic.FabButton.ColorHover` | brush | Follows `Color` |
| [ ] | `--padding-top`, `--padding-end`, `--padding-bottom`, `--padding-start` | — | | They do nothing in Ionic, measured: every child of `.button-native` is absolutely placed |
| [x] | `--ripple-color` | `Avonic.FabButton.RippleColor` | brush | The current colour |
| [x] | `--transition` | `Avonic.FabButton.Transition` | `Transitions` | Shadow, background and colour, 280 ms |

A brush key is `CssUnset` until an app sets it, and then it wins in every state, as setting the variable does. The
`Avonic.FabButton.Md.*` keys hold what `fab-button.md.css` gives per state, and `Avonic.FabButton.Md.CloseTransition` the close
icon's 300 ms; they aren't Ionic's variables.

## ion-fab-list — `AvonicFabList`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `activated` | `Activated` | Shown, and its fab buttons, at any depth, shown 30 ms apart; hidden at once, and its buttons after 0 ms |
| [x] | `side` | `Side` (`FabListSide`) | `Bottom` (default), `Top`, `Start`, `End`: a column, a column upward, a row backward, a row |

A list's button that isn't shown takes no tap, focus, key or automation `Invoke`, and is offscreen to automation, as
`visibility: hidden` is. Its buttons are `Items`, laid out by `FabListPanel`, centred across, at least 56 square, with 5 between them and 10 from the
fab's button. Tab goes through them in their order, not the order on screen. Pseudoclasses: `:active`, `:side-bottom`, `:side-top`,
`:side-start`, `:side-end`.

## Extensions

| Member | On | What |
|---|---|---|
| `Click` | `AvonicFabButton` | A tap, Enter or Space, as Ionic's native `<button>`. Raised after the fab toggles, so a handler sees its new `Activated` |
| `Command`, `CommandParameter` | `AvonicFabButton` | As on `AvonicButton` |

## Known gaps

- **An edge fab paints over the header or footer only if the content is above them**, and the content then covers the header's
  shadow. Avalonia's `ZIndex` only sorts siblings.
- **Shadows are lighter** than Chromium's, as on `ion-button`: Avalonia draws the same values lighter.
- **An app's plain style for a list button's `Margin` loses** to the list's rule, which has an activator. A local value wins.
- **`href` and the router props** aren't there, as on `ion-button`.
- **A fab whose own `FlowDirection` differs from its container's** takes start and end from its own. RTL all through matches.
- **A list that leaves the tree during its stagger finishes it when it comes back**, where Ionic leaves the rest hidden.
- **Not tried in a real window, and not tried on a device.**
