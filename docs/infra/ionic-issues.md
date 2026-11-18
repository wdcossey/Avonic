# Ionic issues

Things in Ionic that looked wrong while building Avonic, which Avonic doesn't copy, kept here for an upstream report later. **None
has been reported.** Add to this as they're found, with a minimal page, the versions, what happens and what Avonic does, and say
how sure it is. Check each against the current Ionic before submitting ([#221](https://github.com/wdcossey/Avonic/issues/221)).

## `ion-action-sheet`: an inline sheet with radios loses its checked one when it's presented

**Measured in Ionic 9.0.11, md, headless Chromium (#40), and the cause read in `action-sheet.js`.** A minimal page:

```html
<ion-app>
  <ion-button id="open">Open</ion-button>
  <ion-action-sheet trigger="open"></ion-action-sheet>
</ion-app>
<script>
  document.querySelector('ion-action-sheet').buttons = ['Apple', 'Banana', 'Cherry'].map((text, i) => ({
    text, htmlAttributes: { role: 'radio', 'aria-checked': i === 1 ? 'true' : 'false' },
  }));
</script>
```

After a tap on Open, every radio has `aria-checked="false"` and `tabindex="-1"`, none is `.action-sheet-selected`, and Tab from the
sheet skips all of them to the next button (`docs/components/ion-action-sheet/ref/radios-inline.png`, `ref/layout.json`
`"radios-inline"`). The same buttons in a sheet from `actionSheetController.create`, as `ion-select` makes it, show Banana checked
(`ref/radios.png`).

`buttonsChanged` stores the checked button's id, `action-sheet-button-${overlayIndex}-${index}`. `present()` then moves an inline
sheet to the app root (`delegateController.attachViewToDom`), whose `connectedCallback` runs `prepareOverlay` again and gives the
sheet a new `overlayIndex`, so the render's ids no longer match the stored one. A sheet made by the controller isn't moved.

A second, read and not measured: `buttonsChanged` and `selectRadioButton` count a button's index in all the buttons, the cancel
button included (`getButtons().indexOf`), and `renderActionSheetButtons` counts it in the buttons it renders, without the cancel
button. A cancel button before the radios would shift every radio's id by one.

**Avonic** keys the checked radio by the button's `Id`, or its place among the rendered buttons, so an inline sheet and a sheet made
in code both show it, and the cancel button's place doesn't matter. Recorded in `docs/components/ion-action-sheet/PLAN.md` and
`COMPARISON.md`.

## `ion-menu`: the focus trap's `lastFocus` isn't cached, so focus leaving the menu always lands on its last element

**Measured in Ionic 9.0.11, md, headless Chromium (#44), and the cause read in `menu.js`.** A minimal page:

```html
<ion-app>
  <ion-menu content-id="main"><ion-content><button id="a">A</button><button id="b">B</button><button id="c">C</button></ion-content></ion-menu>
  <div class="ion-page" id="main"><ion-content><button id="m">Main</button></ion-content></div>
</ion-app>
<script>document.querySelector('ion-menu').open();</script>
```

Once it's open, Tab goes `ion-menu`, A, B, C, then C again and again; `document.getElementById('m').focus()` lands on C, not A
(`docs/components/ion-menu/ref/focus.txt`, mobile and desktop alike). Shift+Tab from A goes to `ion-menu`, then `body`, then C.

`trapKeyboardFocus`'s comment says "We cache the value here to avoid that": `focusFirstDescendant(el)` raises a `focus` event that
sets `this.lastFocus` to the first element before the check `this.lastFocus === doc.activeElement` runs. But nothing is cached, so the
check is always true, and every focus that leaves goes to the first element and then at once to the last. Tab past the last element
should wrap to the first. A fix: `const lastFocus = this.lastFocus;` before `focusFirstDescendant(el)`, and compare that.

**Avonic** copies what Ionic does, as it's what an app sees today (`AvonicMenu.TrapKeyboardFocus`, test
`B12_Focus_Leaving_The_Menu_Lands_On_Its_Last_Element_As_Ionics_Trap_Does`); if Ionic fixes it, so should Avonic. Fairly sure: the
measurement and the code agree.

## `menuController`: two menus asked to open in the same tick both open

**Measured in Ionic 9.0.11, md, headless Chromium (#44), the cause read in `utils/menu-controller/index.js`.** With two closed menus, `a`
at the start and `b` at the end, `Promise.all([a.open(), a.close(), b.open()])`, called in one tick, resolves `[true, false, true]`
with two `ionWillOpen`s and two `ionDidOpen`s, and leaves both menus open (`docs/components/ion-menu/ref/measurements.txt`, `double`).

`_setOpen` checks `isAnimatingSync()` and then `await this.getOpen()` before the open menu is closed and the new one starts: two calls
in one tick both pass the check before either has started, so the controller's one-open-at-a-time rule doesn't hold.

**Avonic** checks and starts in one synchronous step, so the second open returns false (`docs/components/ion-menu/COMPARISON.md`,
deliberate differences). Fairly sure of the cause; the measurement is direct.

## `ion-menu`: ways to leave a menu "animating" for good, after which no menu opens

**Read in `menu.js` (Ionic 9.0.11), not measured in Chromium (#44).** `menuController._setOpen` refuses every menu while any menu's
`isAnimating` is true, so a menu left with it set stops them all:

- **Disabled during a swipe.** `updateState` sets `operationCancelled` when a menu is disabled while animating. Only `_setOpen`
  clears it, after its own animation, and a swipe never goes through `_setOpen`. Enabled again, the next `open()` plays, finds the
  flag, returns false and never calls `afterAnimation`: `isAnimating` stays true.
- **A type with no animation.** `beforeAnimation` sets `isAnimating`, then `loadAnimation` throws "animation not registered" (a
  type cleared, or a builder that throws), and nothing sets it back.
- **Removed while it plays.** `disconnectedCallback`'s `close(false)` returns false while it animates, then `animation.destroy()`
  clears its finish callbacks, so the `play()` the open awaits never resolves. The element is unregistered, so this matters only if
  it's attached again (a framework moving it), when it registers still animating.

**Avonic** clears the cancel at the start of each operation, puts the menu back when no animation can be made, and closes a menu
detached mid-play at once, ending its play (`docs/components/ion-menu/COMPARISON.md`, deliberate differences, with tests).
