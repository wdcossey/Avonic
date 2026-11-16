# ion-app

Status: done · [#65](https://github.com/wdcossey/Avonic/issues/65)

`AvonicApp`, Ionic's `ion-app`: the root of an app. It starts what a native app needs from Ionic for the `TopLevel` it's in: the
hardware back button, the keyboard's events, the input shims and the safe area, and its theme gives what's in it a page's text. Plan:
[PLAN.md](PLAN.md); comparison: [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicApp>
    <DockPanel>
        <c:AvonicHeader DockPanel.Dock="Top">…</c:AvonicHeader>
        <c:AvonicContent>…</c:AvonicContent>
    </DockPanel>
</c:AvonicApp>
```

**No control needs it.** Every control works without it, and an overlay still dismisses on back. What only it gives is below.

## Checklist

| ✓ | Ionic | Avonic | |
|---|---|---|---|
| [x] | `setFocus(elements)` | `SetFocus(IEnumerable<Control>)`, `SetFocus(params Control[])` | Synchronous |
| [ ] | `mode` | none | Not exposed (ADR 0001) |

No props, events, slots, parts or CSS variables.

## The hardware back button

Ionic's `ionBackButton`: for each back request, `HardwareBackButton.IonBackButtonEvent` goes out on the `TopLevel`, and whatever
wants the press registers a handler at a priority. The highest runs, and of equal ones the last registered.

```csharp
topLevel.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) =>
    e.Register(10, next =>
    {
        // Do something, or call next() to let the next highest run.
    }));
```

| Priority | Who |
|---|---|
| 100 (`HardwareBackButton.OverlayBackButtonPriority`) | The top overlay, if its backdrop can dismiss it: it dismisses with the `backdrop` role |
| 99 (`MenuBackButtonPriority`) | An open menu, when there is one |
| 0 | Ionic's router. The sample's shell goes back to its list here |
| -1 | Where Ionic's docs exit the app |

- A handler is given `next`. Each call of it runs the next highest. A handler can return a `Task`; until it finishes, a back press
  does nothing. A handler that throws is logged, and doesn't stop the next press.
- It comes from `TopLevel.BackRequested`: Android's back button and gesture, and on a desktop the mouse's back button and Alt+Left.
  A request an app's own handler has already handled doesn't come.
- **The request is handled even when nothing registers,** so Android stays in the app, as in an Ionic app on Capacitor, where
  "nothing will happen". Ionic's docs exit through a handler of the app's own at -1, which calls Capacitor's `App.exitApp()`.
  Here the simplest way is to turn the extension `AvonicConfig.HandleUnclaimedBack` off: then a request nothing takes goes to
  Android, which leaves the app.
- `AvonicConfig.HardwareBackButton` (Ionic's `hardwareBackButton`, default true) off: Avonic leaves back alone. Nothing is raised,
  and overlays don't dismiss on it.
- **Without an `AvonicApp`**, nothing is raised, as `ionBackButton` needs `ion-app`, but an overlay still dismisses on back.

## The keyboard

Ionic's `ionKeyboardDidShow` and `ionKeyboardDidHide`: `KeyboardAssist.IonKeyboardDidShowEvent`, with `KeyboardHeight`, and
`IonKeyboardDidHideEvent`, on the `TopLevel`, each time the on-screen keyboard has opened or closed. They come from
`TopLevel.InputPane`, once its animation has finished. A desktop has no pane, and nothing comes.

## The input shims

Ionic's scroll assist and scroll padding. On a phone the keyboard covers the app, which Avalonia doesn't resize or scroll, so a
field low in the page would end up under it. When an `AvonicInput` or `AvonicTextarea` in an `AvonicContent` takes focus there,
the content is padded at its end by the keyboard's height (`KeyboardOffset`) and scrolled so that the field's item is 50 above the
keyboard. The padding goes 120 ms after the field loses focus.

| Config | Ionic | Default | |
|---|---|---|---|
| `AvonicConfig.InputShims` | `inputShims` | on for Android and iOS | Read when the app is attached |
| `AvonicConfig.ScrollAssist` | `scrollAssist` | true | Read when the app is attached |
| `AvonicConfig.ScrollPadding` | `scrollPadding` | true | Read when the app is attached |
| `AvonicConfig.KeyboardHeight` | `keyboardHeight` | 290 | The height reckoned with until the keyboard says its own. Read when the app is attached |

Once the keyboard says its own height, the assist runs again with it, where Ionic leaves that to the browser.

## The safe area

While the root isn't padded by Avalonia, the four `Avonic.SafeArea*` keys are the insets, in the `TopLevel`'s resources, so that
overlays have them too: as an Ionic page with `viewport-fit=cover` gets `env(safe-area-inset-*)`. While Avalonia pads it, which
is its default, they're the theme's 0. To run edge to edge:

```csharp
TopLevel.SetAutoSafeAreaPadding(root, false);     // root: the TopLevel's Content
topLevel.InsetsManager!.DisplayEdgeToEdgePreference = true;   // Forced on Android 15 and later
```

The header's first toolbar, items, dividers and list headers then pad themselves by them.

## Text

The theme gives what's in the app a font size of 16, `Avonic.FontFamily`, `Avonic.TextColorBrush` and an `Avonic.BackgroundColorBrush`
background, as `html` and `body` give an Ionic page. They're the theme's, so an app's own style wins. Outside an `AvonicApp`,
Avalonia's defaults stay. Typography for `h1` to `h6` is still opt in: [typography](../../infra/typography/README.md).

## `SetFocus`

Shows the focus ring, `:ion-focused`, on exactly the elements given, and takes it off everything else, without moving focus: for
focus moved by code after a user's action other than a key. It lasts until a press, a key that doesn't move focus, focus moving
by keyboard, or focus leaving everything.

## The page

It has the class `ion-page`, as `ion-app`'s host does, so an `AvonicContent` in it takes it as its page. It fills what it's in,
and doesn't clip.

## Extensions

| Extension | |
|---|---|
| `AvonicConfig.HandleUnclaimedBack`, default true | Whether a back request nothing registers for is still handled. False leaves it to the platform |

## Resource keys

None of its own. The theme reads `Avonic.FontFamily`, `Avonic.TextColorBrush` and `Avonic.BackgroundColorBrush`, and the app
fills `Avonic.SafeArea*`.

## Known gaps

- **The status bar tap** isn't here: an issue of its own, as only iOS has one.
- **The input shims run again with the keyboard's own height,** where Ionic keeps its estimate of 290 and leaves the rest to the
  browser, which scrolls a focused field above the keyboard itself. Avalonia doesn't, and on a Pixel 8 the keyboard is 354.
- **Not done from Ionic's shims:** moving the native input away while it takes focus (nothing here scrolls the page by itself),
  hide caret on scroll (WebKit's), input blurring (off since Ionic 8), a footer's field.
- `AvonicConfig.HardwareBackButton` off leaves back alone, where Ionic's `blockHardwareBackButton` swallows it.
- Ionic starts its utilities after an idle callback; here they start when the app is attached. Ionic never stops them; here they
  stop when the app is taken out.
- Tried on a Pixel 8 (Android 17) and headless; not on iOS, and not in a real window (`tools/real-window` has an `ion-app`
  section to run).
