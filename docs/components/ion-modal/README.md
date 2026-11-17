# ion-modal

Status: done · [#42](https://github.com/wdcossey/Avonic/issues/42) (the modal; the sheet comes in a second PR)

`AvonicModal`, Ionic's `ion-modal`, md: full screen on a phone and a centred 600 by 500 or 600 dialog on a tablet, over a
backdrop, on the [overlay system](../../infra/overlays/README.md). Plan: [PLAN.md](PLAN.md); comparison:
[COMPARISON.md](COMPARISON.md). The sheet modal (`Breakpoints` and the rest) isn't here yet: none of its members is declared.

![Ionic's dialog beside Avonic's, headless](compare/side-by-side-dialog.png)

## Usage

Inline, opened by a trigger, or by `IsOpen`:

```xml
<StackPanel>
    <c:AvonicButton Name="Open" Content="Open" />
    <c:AvonicModal Name="Modal" Trigger="Open">
        <Grid RowDefinitions="Auto,*">
            <c:AvonicHeader>
                <c:AvonicToolbar>
                    <c:AvonicTitle Content="Modal" />
                    <c:AvonicToolbar.EndContent>
                        <c:AvonicButtons>
                            <c:AvonicButton Content="Close" Click="OnClose" />
                        </c:AvonicButtons>
                    </c:AvonicToolbar.EndContent>
                </c:AvonicToolbar>
            </c:AvonicHeader>
            <c:AvonicContent Grid.Row="1">…</c:AvonicContent>
        </Grid>
    </c:AvonicModal>
</StackPanel>
```

```csharp
private void OnClose(object? sender, RoutedEventArgs e) => Modal.Dismiss(null, "cancel");
```

While it's presented, an inline modal is moved to the overlay layer, with an invisible placeholder in its place, and moved back
after, as Ionic teleports it to `ion-app`. It has to be in a `Panel`, a `Decorator` or a `ContentControl`. It keeps the page's
`DataContext`, so its bindings go on working; its own `DataContext` wins. If its place leaves the tree while it's presented, it
dismisses with the role `parent-removed`.

Or in code, as Ionic's controller:

```csharp
var modal = new AvonicModal { Content = new MyView(), CanDismiss = new ModalCanDismiss((data, role) => ConfirmAsync()) };
await modal.Present();
var detail = await modal.OnDidDismiss();   // detail.Data, detail.Role
```

`IsOpen` isn't set back to false by a dismiss: an app that binds it sets it back from `DidDismiss`, as Ionic's docs say.

## Checklist

| ✓ | Ionic | Avonic | Default | |
|---|---|---|---|---|
| [x] | `animated` | `Animated` | `true` | |
| [x] | `backdropDismiss` | `BackdropDismiss` | `true` | The backdrop's tap, Escape and back |
| [x] | `canDismiss` | `CanDismiss` | `true` | `ModalCanDismiss`: a `bool` or a `Func<object?, string?, Task<bool>>`; `True`/`False` in XAML. The role `handler` (`OverlayRoles.Handler`) skips it |
| [x] | `enterAnimation`, `leaveAnimation` | `EnterAnimation`, `LeaveAnimation` | | `AnimationBuilder` |
| [x] | `focusTrap` | `FocusTrap` | `true` | |
| [ ] | `htmlAttributes` | none | | Not applicable: set the control's properties, `Classes` and `AutomationProperties` |
| [x] | `isOpen` | `IsOpen` | `false` | On `AvonicOverlay`. Takes effect once it's attached; true then presents at the next frame |
| [x] | `keepContentsMounted` | `KeepContentsMounted` | `false` | The content is in the visual tree only while presented; true, from attach on, and after |
| [x] | `keyboardClose` | `KeyboardClose` | `true` | |
| [ ] | `mode` | none | | Not exposed (ADR 0001) |
| [ ] | `presentingElement` | none | | Not applicable: ios only, the card modal |
| [x] | `showBackdrop` | `ShowBackdrop` | `true` | False: transparent, and it still takes the tap |
| [x] | `trigger` | `Trigger` | | On `AvonicOverlay`: the name of a control in its name scope, whose `Click` (or `Tapped`) presents it |
| [ ] | `breakpoints`, `initialBreakpoint`, `backdropBreakpoint`, `handle`, `handleBehavior`, `expandToScroll` | | | The sheet, the second PR |
| [x] | `ionModalWillPresent`, `ionModalDidPresent`, `ionModalWillDismiss`, `ionModalDidDismiss` | `IonModalWillPresent`, … | | Routed, bubbling, each just before the generic one |
| [x] | `willPresent`, `didPresent`, `willDismiss`, `didDismiss` | `WillPresent`, … | | The overlay's |
| [ ] | `ionBreakpointDidChange`, `ionDragStart`, `ionDragMove`, `ionDragEnd` | | | The sheet |
| [x] | `present()`, `dismiss()`, `onDidDismiss()`, `onWillDismiss()` | `Present()`, `Dismiss(data, role)`, `OnDidDismiss()`, `OnWillDismiss()` | | `Present(TopLevel)` too, the overlay's extension |
| [ ] | `setCurrentBreakpoint()`, `getCurrentBreakpoint()` | | | The sheet |
| [x] | default slot | `Content`, `ContentTemplate` | | |
| [x] | `backdrop`, `content` parts | `PART_Backdrop`, `PART_Content` | | An `AvonicBackdrop`; a `ModalWrapper` |
| [ ] | `handle` part | | | The sheet |

## Behaviour

- **Present**: the md enter animation, 280 ms, `cubic-bezier(0.36, 0.66, 0.04, 1)`: the backdrop from 0.01 to its opacity, not
  hit until it ends, and the wrapper up 40 px from 0.01 to 1. `IonModalWillPresent`, `WillPresent`, then `IonModalDidPresent`,
  `DidPresent`. Focus goes to the wrapper, which is a dialog to automation, named from the modal's
  `AutomationProperties.Name`; back to what had it after the dismiss.
- **Dismiss**: modals presented inside this one are dismissed with `parent-dismissed`; then `CanDismiss`; then the md leave
  animation, 200 ms, `cubic-bezier(0.47, 0, 0.745, 0.715)`, the dismiss events, and it's removed.
- **Size**: full screen, and from a window of 768 by 600 (`:inset-small`) 600 by 500 with a radius of 2 and md's shadow; from 768
  by 768 (`:inset-large`) 600 by 600. An app's `Avonic.Modal.Width` and the rest win at every size.
- **Safe area**: inside the modal, `Avonic.SafeArea<Edge>` is 0 for an edge the wrapper doesn't reach, as Ionic's host style
  does, so a centred dialog's header isn't padded for a notch.
- Escape and back dismiss it with `backdrop`, as every overlay's do, through `CanDismiss`.

## Resource keys

In `Tokens/Modal.axaml`. A size takes a number (px) or a CSS string: `100%`, `600px`, `auto`, `fit-content`, `max-content`,
`min-content`, or `none` for a maximum.

| Key | Ionic | Default |
|---|---|---|
| `Avonic.Modal.BackdropOpacity` | `--backdrop-opacity` | unset: `Avonic.BackdropOpacity`, 0.32 |
| `Avonic.Modal.Background` | `--background` | unset: `Avonic.BackgroundColorBrush` |
| `Avonic.Modal.BorderColor` | `--border-color` | transparent |
| `Avonic.Modal.BorderRadius` | `--border-radius` | unset: `Avonic.Modal.Md.BorderRadius` 0, `…Md.BorderRadiusInset` 2 |
| `Avonic.Modal.BorderStyle` | `--border-style` | `none`; any other style is solid |
| `Avonic.Modal.BorderWidth` | `--border-width` | 0 |
| `Avonic.Modal.Width` | `--width` | unset: `…Md.Width` `100%`, `…Md.WidthInset` 600 |
| `Avonic.Modal.Height` | `--height` | unset: `…Md.Height` `100%`, `…Md.HeightInsetSmall` 500, `…Md.HeightInsetLarge` 600 |
| `Avonic.Modal.MinWidth`, `MaxWidth`, `MinHeight`, `MaxHeight` | `--min-width` and the rest | `auto`, `auto`, `auto`, `100%` |
| `Avonic.Modal.BoxShadow` | `--box-shadow` (not in `core.json`) | unset: `…Md.BoxShadow` none, `…Md.BoxShadowInset` `0 28px 48px rgba(0, 0, 0, 0.4)` |

The host's text is `Avonic.TextColorBrush`.

## Extensions

None of the modal's own. On `AvonicOverlay`, which every overlay gets: inline overlays keep the page's `DataContext`, and
`parent-removed` applies to every inline overlay, where Ionic has it only on the modal.

## Known gaps

- A full-screen modal with an `AvonicContent` and no footer doesn't pad the content's end by the bottom safe area, as Ionic 9's
  does: `AvonicContent` has no such padding yet.
- `Avonic.Modal.BorderStyle`: Avalonia's `Border` draws only solid lines.
- md's tablet shadow, `0 28px 48px`, is up to 15 levels lighter just under the wrapper than Chromium's (box-shadow is best effort).
- The page lifecycle (`ionViewWillEnter` and the rest on the content) waits for `ion-nav` (#130).
- Not tried in a real window: `tools/real-window/drive.ps1`'s `ion-modal` section is for the batch's runner.
