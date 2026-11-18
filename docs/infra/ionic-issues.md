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
