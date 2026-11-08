# ion-toolbar

Status: done · [#12](https://github.com/wdcossey/Avonic/issues/12)

The toolbar family: `AvonicHeader`, `AvonicFooter`, `AvonicToolbar`, `AvonicTitle` and `AvonicButtons`. A toolbar is a 56 high bar
with a title and buttons, a header and a footer hold toolbars at the top and bottom of a page, and `AvonicButtons` makes the
buttons in it flat and toolbar-sized. The design and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in
[COMPARISON.md](COMPARISON.md). [`ion-header`](../ion-header/README.md), [`ion-footer`](../ion-footer/README.md),
[`ion-title`](../ion-title/README.md) and [`ion-buttons`](../ion-buttons/README.md) point here.

```xml
<Grid RowDefinitions="Auto,*,Auto">
    <c:AvonicHeader>
        <c:AvonicToolbar>
            <c:AvonicToolbar.StartContent>
                <c:AvonicButtons>
                    <c:AvonicButton>
                        <c:AvonicButton.IconOnlyContent>
                            <c:AvonicIcon IconName="menu" />
                        </c:AvonicButton.IconOnlyContent>
                    </c:AvonicButton>
                </c:AvonicButtons>
            </c:AvonicToolbar.StartContent>
            <c:AvonicTitle Content="Inbox" />
            <c:AvonicToolbar.EndContent>
                <c:AvonicButtons>
                    <c:AvonicButton Content="Edit" Click="OnEditClick" />
                </c:AvonicButtons>
            </c:AvonicToolbar.EndContent>
        </c:AvonicToolbar>
    </c:AvonicHeader>

    <c:AvonicContent Grid.Row="1">…</c:AvonicContent>

    <c:AvonicFooter Grid.Row="2">
        <c:AvonicToolbar Color="primary">
            <c:AvonicTitle Content="Footer" Size="Small" />
        </c:AvonicToolbar>
    </c:AvonicFooter>
</Grid>
```

## API

### `AvonicToolbar`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | Base behind contrast text. See the [colour system](../../infra/color-system/README.md) |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | default slot | `Content`, `ContentTemplate` | Takes what's left, and can shrink to nothing |
| [x] | `start`, `secondary`, `primary`, `end` slots | `StartContent`, `SecondaryContent`, `PrimaryContent`, `EndContent`, each with a `…Template` | Drawn in md's order: start, the default slot, secondary, primary, end. `start` and `end` swap in RTL |
| [x] | `background` part | `PART_Background` | Fills the host, under the safe area's padding |
| [x] | `container` part | `PART_Container` | A `Border` holding a `FlexRow`, inside the safe area, padded by the `--padding-*` keys |
| [x] | `content` part | `PART_Content` | Around the default slot |

### `AvonicHeader`, `AvonicFooter`

| ✓ | Control | Ionic | Avonic | Notes |
|---|---|---|---|---|
| [x] | header | `collapse` | `Collapse` (`HeaderCollapse?`: `Condense`, `Fade`) | `Condense` hides the header and drops its landmark, as md does with the header an ios page puts in its content for the large title. `Fade` is kept, with Ionic's host class as a pseudoclass and no md rule on it, so it looks like the default. The classes are `:collapse-none`, `:collapse-condense` and `:collapse-fade` |
| [ ] | header, footer | `translucent` | — | Not applicable: only ios draws it |
| [ ] | footer | `collapse` | — | Not applicable: ios only, and md has no rule for it |
| [ ] | both | `mode` | — | Not exposed (ADR 0001) |
| [x] | both | children | `Items` | They hold many children, and `core.json` lists no slot |

`Classes="ion-no-border"` removes the shadow.

### `AvonicTitle`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | Paints the text |
| [x] | `size` | `Size` (`TitleSize?`: `Large`, `Small`) | Null is Ionic's default. `Small` is 15 at normal weight, and wraps. `Large` is kept, with Ionic's host class as a pseudoclass and no md rule on it, so it looks like the default. The classes are `:size-default`, `:size-small` and `:size-large` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | children | `Content` | |

### `AvonicButtons`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [ ] | `collapse` | — | Not applicable: its rules are only in ios, for the collapsible large title |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | children | `Items` | A horizontal row |

None of the five has events or methods.

### Host classes an app can style

As in Ionic, these are set whether or not md has a rule on them, so a rule an app wrote for Ionic's class maps across.

| Control | Ionic | Avonic |
|---|---|---|
| `AvonicTitle` | `title-default`, `title-small`, `title-large` | `:size-default`, `:size-small`, `:size-large` |
| `AvonicHeader` | `header-collapse-none`, `-condense`, `-fade` | `:collapse-none`, `:collapse-condense`, `:collapse-fade` |
| `AvonicToolbar` | `toolbar-title-default`, `-small`, `-large`, from its title's `ionStyle`, and `toolbar-<key>` for any child's | `:toolbar-title-default`, `:toolbar-title-small`, `:toolbar-title-large`, `:toolbar-label` and so on. The toolbar stops `ionStyle`, so an item around it doesn't hear its label |
| `AvonicButton` | `in-toolbar`, `in-toolbar-color`, `in-buttons` | `:in-toolbar`, `:in-toolbar-color`, `:in-buttons`. See [ion-button](../ion-button/README.md#in-a-toolbar) |
| `AvonicButtons` | `buttons-first-slot`, `buttons-last-slot` | `:buttons-first-slot`, `:buttons-last-slot` |

### Resource keys

| Ionic | Key | Type | md default |
|---|---|---|---|
| `ion-toolbar` `--background` | `Avonic.Toolbar.Background` | brush | Unset → `Avonic.ToolbarBackgroundBrush` |
| `ion-toolbar` `--color` | `Avonic.Toolbar.Color` | brush | Unset → `Avonic.ToolbarColorBrush` → `#424242`, or in the dark palette `Avonic.TextColor`, `#FFFFFF` |
| `ion-toolbar` `--border-color` | `Avonic.Toolbar.BorderColor` | brush | Unset → `Avonic.ToolbarBorderColorBrush` → `#C1C4CD`, or in the dark palette `Avonic.BorderColor`, `#363636` |
| `ion-toolbar` `--border-width` | `Avonic.Toolbar.BorderWidth` | `Thickness` | 0 |
| `ion-toolbar` `--border-style` | — | — | Not possible: a `Border` draws solid lines only. Use a width of 0 for `none` |
| `ion-toolbar` `--min-height` | `Avonic.Toolbar.MinHeight` | `double` | 56 |
| `ion-toolbar` `--opacity` | `Avonic.Toolbar.Opacity` | `double` | 1, on the background only |
| `ion-toolbar` `--padding-top`, `-bottom`, `-start`, `-end` | `Avonic.Toolbar.PaddingTop`, `…Bottom`, `…Start`, `…End` | `double` | 0 |
| `ion-title` `--color` | `Avonic.Title.Color` | brush | Unset: the title inherits |
| `--ion-toolbar-background` | `Avonic.ToolbarBackgroundBrush` | brush | `Avonic.BackgroundColor`, white, in `Tokens/Global.axaml`; `#1F1F1F` in the dark palette. Set in the light palette too, where Ionic leaves it unset: see the known gaps |
| `--ion-toolbar-color` | `Avonic.ToolbarColorBrush` | brush | Unset. When set, a button in an uncoloured toolbar takes it too |
| `--ion-toolbar-border-color` | `Avonic.ToolbarBorderColorBrush` | brush | Unset |
| none: the header's and footer's `box-shadow` | `Avonic.Header.BoxShadow`, `Avonic.Footer.BoxShadow` | `BoxShadows` | md's three layers |
| none: `ion-buttons`' button radius | `Avonic.Buttons.Md.ButtonBorderRadius`, `…IconOnly` | `CornerRadius` | 2, and 24 for a clear icon-only button |

`Background`, `Foreground` and `BorderBrush` are the toolbar's own properties, which the theme fills from these keys, so they can be
set directly too. The `Avonic.Toolbar.Md.*` keys are the theme's literal fallbacks, not Ionic's variables. Override the keys
above instead. The dark palette sets the two that Ionic's dark palette changes, following `Avonic.TextColor` and `Avonic.BorderColor`.

## Behaviours

These are in [PLAN.md](PLAN.md#behaviours), numbered, each with tests named after its number. The ones that surprise:

- **The keyboard goes start, secondary, default, primary, end,** Ionic's slot order, though secondary is drawn after the content.
  Each slot is its own tab group, so an app's `TabIndex` only orders things within a slot.
- **A button in `AvonicButtons` with no `Fill` is clear,** at any depth. A `Fill` you give it wins, and a button in a toolbar but
  outside `AvonicButtons` stays solid, as in Ionic. It follows a button moving in or out, where Ionic only looks once.
- **A clear, icon-only button in `AvonicButtons` is a 48 circle with the unbounded ripple.**
- **In an uncoloured toolbar, buttons take the toolbar's text colour,** and a solid one in `AvonicButtons` is that colour behind
  the toolbar's background colour. A button's own `Color` wins.
- **"Uncoloured" means no toolbar around the button has a `Color`,** at any depth, even an empty one, as Ionic's
  `ion-toolbar[color]`: a plain toolbar inside a coloured one counts as coloured. The button reads it when it renders, so a toolbar's
  `Color` set later reaches its buttons' own rules at their next render, as in Ionic. The `AvonicButtons` rules for a coloured toolbar
  follow at once, as they're plain CSS in Ionic.
- **The first `AvonicButtons` in `StartContent` gets 4 at its start, and the last one in `EndContent`, or else `PrimaryContent`, or
  else `SecondaryContent`, gets 4 at its end.** A panel directly in a slot stands for the slot.
- **The safe area.** `Avonic.SafeAreaLeft` and `Right` pad every toolbar, on physical sides. The first toolbar in a header adds
  `Avonic.SafeAreaTop`, and the last in a footer adds `Avonic.SafeAreaBottom`, with the background under them. The app fills
  these keys; see [`ion-content`](../ion-content/README.md).
- **A header's and footer's shadow is drawn over what comes after,** as `z-index: 10` does, through `ZIndex` 10.
- **Automation:** a header is the `Banner` landmark and a footer the `ContentInfo` one.

## Extensions

None.

## Known gaps

- **A pressed outline button in an uncoloured toolbar is white** (`Avonic.ToolbarBackgroundBrush`), where Ionic's light palette fills
  one directly in the toolbar with its own colour, primary, so its primary text can't be seen while it's pressed, and leaves one in
  `AvonicButtons` transparent. Kept on purpose (#89): `Avonic.ToolbarBackgroundBrush` stays set in the light palette, where Ionic
  leaves `--ion-toolbar-background` unset, because Ionic's result looks accidental. Ionic does what Avonic does once the variable is
  set, as the dark palette does. See the [plan](PLAN.md#deliberate-differences).
- **A clear or outline button in `AvonicButtons` keeps the toolbar's text when focused, hovered or pressed,** even when an app sets
  `Avonic.Button.ColorFocused`, `ColorHover` or `ColorActivated`, which Ionic's follows, as `--color: initial` there inherits the
  button's own state colour. Measured; see the [plan](PLAN.md#deliberate-differences).
- **`AvonicButtons` in a coloured item,** not a toolbar, doesn't take the coloured-toolbar rules, where Ionic's
  `:host-context(.ion-color)` takes any coloured ancestor.
- **A button that moves** reads where it is when it's attached, where Ionic keeps its old classes until its next render. A
  recycled container that stays attached keeps them until its button's next render, as Ionic would.
- **A header's first toolbar has to be its first child,** and a footer's last its last, to get the safe area. Ionic finds the first
  toolbar among other children, and inside a wrapper.
- **A footer's bottom safe area stays while the keyboard is open,** where Ionic drops it. It needs the keyboard's state, which
  headless tests can't open.
- **A header and footer don't place themselves on a page,** as Avonic has no page yet. Put them in a `Grid`, as above.
- **Text in a toolbar is Avalonia's default size,** where a web page's is 16 ([ADR 0002](../../decisions/0002-decisions-from-the-first-swarm.md)).
