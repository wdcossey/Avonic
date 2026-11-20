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

## `ion-toast`: Tab never reaches a toast's buttons

**Measured in Ionic 9.0.11, md, headless Chromium, mobile and desktop (#38).** A minimal page:

```html
<ion-app>
  <button id="a">A</button>
</ion-app>
<script>
  const t = document.createElement('ion-toast');
  t.message = 'Saved';
  t.buttons = ['Undo', { text: 'Dismiss', role: 'cancel' }];
  document.querySelector('ion-app').appendChild(t);
  t.present();
</script>
```

With the toast shown, Tab from A goes to `body` and back to A, and Shift+Tab the other way: neither reaches Undo or Dismiss
(`docs/components/ion-toast/ref/extra.json`, `"tab"`). A button focused another way (a tap, `focus()`) does Tab to the next one, then
out of the toast.

`render()` gives the host `tabindex="-1"`, and Chromium leaves everything in the shadow tree of a host with a negative `tabindex` out
of sequential navigation, so the buttons' own `tabIndex: 0` and `ion-focusable` never count. A keyboard user can't reach an Undo.
Moving `tabindex="-1"` off the host (it's there so that `keyboardClose` can focus it) or onto the wrapper would fix it.

**Avonic** doesn't copy it: the buttons are tab stops after the page's last, as their `tabindex="0"` asks
(`B21_Buttons_Are_Named_And_Invokable_And_Tab_Reaches_Them_After_The_Page`). Sure of the measurement; the cause is the HTML
spec's rule for a shadow host with a negative `tabindex`.

## Overlays: a toast on top stops Escape and the hardware back button dismissing the overlay under it

**Measured in Ionic 9.0.11, md, headless Chromium (#38), and the cause read in `utils/overlays.js`.** Present an `ion-alert`, then an
`ion-toast` over it, and press Escape: nothing is dismissed (`docs/components/ion-toast/ref/behaviour.json`, `"overAlert"`). Once the
toast has gone, Escape dismisses the alert.

`connectListeners` adds the close watcher's condition with `NON_TOAST_OVERLAYS` ("Every overlay except toast, which doesn't trap focus
or close on back"), but its `keydown` and `ionBackButton` listeners take `getPresentedOverlay(doc)`, of every kind: the toast is on
top, it has no `backdropDismiss`, and nothing happens. On Android the back press then falls through to the next handler, such as the
router's, which navigates while the alert or modal stays. Passing `NON_TOAST_OVERLAYS` to those two `getPresentedOverlay` calls
would fix it.

**Avonic** doesn't copy it: `OverlayHost`'s Escape and back take the top overlay that isn't a toast
(`B12_A_Toast_On_Top_Doesnt_Stop_Escape_Dismissing_The_Alert_Under_It`). Sure: the measurement and the code agree.

## `ion-toast`: a toast presented again at another position keeps the old one's inline style

**Read in `animations/md.enter.js` (#38); not measured.** The md enter sets the wrapper's inline `transform` at the top and the bottom,
and its inline `top` in the middle, and neither clears the other. An inline toast (`isOpen`, `trigger`) presented in the middle and
then, after `position = 'bottom'`, again has `top: 398px` from the first and `bottom: 0` from its class, so CSS ignores `bottom` and
it shows at 398 with `translateY(-8px)`. Presented at the bottom and then in the middle, it keeps the bottom's `translateY`.

**Avonic** clears the transform for the middle, and its panel reads the middle's offset only in the middle
(`B1_Top_Is_8_Down_And_The_Middle_Is_Floor_Of_Half_The_Rest_With_No_Transform`). Fairly sure, from the code.

## `ion-datetime`: an adjacent day ignores the default buttons and the buttons slot

**Measured in Ionic 9.0.11, md, headless Chromium (#46), and the cause read in `datetime.js`.** A minimal page:

```html
<ion-datetime presentation="date" show-adjacent-days="true" show-default-buttons="true" value="2023-06-15T13:47:00"></ion-datetime>
```

A click on an adjacent day, July 3 in June's grid, raises `ionChange` with `2023-07-03T13:47:00` at once, before Done
(`docs/components/ion-datetime/ref/behaviour.json`, `"adjacent"`). A day of the month itself waits for Done.

`renderMonth`'s click handler for an adjacent day sets `activeParts` itself and calls `this.confirm()`, where every other selection
goes through `setActiveParts`, which returns early with `showDefaultButtons` or a `buttons` slot, and sets `hasUnconfirmedSelection`.
Ionic's own docs say the default buttons are what update the value. Calling `setActiveParts` (and then `animateToDate`) instead would
fix it, and also let the parent overlay's present discard the selection as it does for any other.

**Avonic** doesn't copy it: an adjacent day is selected as any other, then its month is shown
(`B33_An_Adjacent_Day_Waits_For_Done_And_Shows_Its_Month`). Sure: the measurement and the code agree.

## `ion-datetime`: Done with nothing chosen clears the value of a time, month or year wheel

**Measured in Ionic 9.0.11, md, headless Chromium (#46), and the cause read in `datetime.js`.** A minimal page:

```html
<ion-datetime presentation="time" show-default-buttons="true"></ion-datetime>
```

Done, with the wheel untouched, raises `ionChange` with no value, and the value stays unset, though the wheel shows a time
(`ref/behaviour.json`, `"confirmNoSelectionTime"`). With `presentation="date" prefer-wheel="true"` the same Done writes the time
shown, `2023-06-20T10:00:00` (`"confirmNoSelectionWheel"`).

`confirm()` writes the working parts when nothing is selected only `if (preferWheel)`, under the comment "If the datetime is using a
wheel picker, but the active parts are empty, then the user has confirmed the initial value (working parts) presented to them." The
`time`, `month`, `month-year` and `year` presentations always show a wheel (the `preferWheel` docs say so), so the comment's case is
theirs too. `!this.isGridStyle` in place of `preferWheel` would fix it.

**Avonic** writes the working parts for every wheel presentation (`B34_Done_With_Nothing_Chosen_On_A_Time_Wheel_Writes_What_It_Shows`).
Fairly sure: the comment says what's meant, but an app could rely on no value.

## `ion-datetime`: a wheel can write a date past `max`

**Read in `utils/manipulation.js` (#46); not measured.** `validateParts` clamps the parts to `min` and `max` (`clampDate` returns
`maxParts` itself), and then, if the day asked for is past the end of the month asked for, sets the day of that clamped copy, which is
`maxParts`, to the length of the month asked for. With `max="2021-01-15"` on a date wheel, the 29th of February 2020 moved to 2021
gives 2021-02-29, past max, so it's clamped to 2021-01-15, and then cut to February's 28 days: the value is `2021-01-28T23:59:00`,
13 days past max, and `maxParts` itself is now the 28th, so max has moved for the rest of the datetime's life. Cutting the day only
when the parts weren't clamped would fix both.

**Avonic** cuts the day only when the date wasn't clamped (`ValidateParts_Keeps_A_Date_Clamped_To_Max_Whole`), and its parts are
values, so max can't move. Fairly sure, from the code.

## `ion-datetime-button`: the `month` and `year` presentations are formatted by `formatOptions.time`

**Measured in Ionic 9.0.11, md, headless Chromium (#79), and read in `datetime-button.js`.** A minimal page:

```html
<ion-app>
  <ion-datetime-button datetime="dt"></ion-datetime-button>
  <ion-modal keep-contents-mounted="true"><ion-datetime id="dt" presentation="month"></ion-datetime></ion-modal>
</ion-app>
<script>
  const dt = document.getElementById('dt');
  dt.formatOptions = { date: { month: 'short' }, time: { hour: '2-digit', minute: '2-digit' } };
  dt.value = '2024-07-15T13:47:00';
</script>
```

The button reads `01:47 PM`, a time, for a month picker (`docs/components/ion-datetime-button/ref/behaviour.json`, `x_fo0_month`).
With only `{ date: { month: 'short' } }` it reads `July`, the date's options ignored (`x_fo1_month`); `presentation="year"` with
`{ date: { year: '2-digit' } }` reads `2024` (`x_fo2_year`), and with `{ time: { hour: '2-digit' } }`, `01 PM` (`x_fo3_year`).
`setDateTimeText` formats `month` with `formatOptions?.time ?? { month: 'long' }` and `year` with `formatOptions?.time ?? { year:
'numeric' }`, where the `month-year` case just above it reads `formatOptions?.date`. The datetime itself formats those presentations by
`formatOptions.date` and warns when it's missing ("The 'month' presentation requires a date object in formatOptions"), so `time` looks
like a copy of the wrong line.

**Avonic** formats both by `formatOptions.date` (`Jul`, `24`), `B13_Month_And_Year_Are_Formatted_By_The_Date_Options`. Sure, from the
code and the measurement.

## `ion-loading`: a second `present()` starts a second duration timer, which can dismiss a later presentation

**Read in `loading.js` (#41), and its parts measured in Ionic 9.0.11, md, headless Chromium.** A minimal page:

```html
<ion-app>
  <script>
    const loading = document.createElement('ion-loading');
    loading.message = 'Loading...';
    loading.duration = 1000;
    document.querySelector('ion-app').appendChild(loading);
    loading.present();
    setTimeout(() => loading.present(), 500);   // already presented: the shared present() returns at once
    setTimeout(() => loading.present(), 1300);  // after the first timer's dismiss: presented again
  </script>
</ion-app>
```

`present()` runs `await present(this, …)`, which returns at once when the loading is already presented, and then always sets
`this.durationTimeout = setTimeout(() => this.dismiss(), this.duration + 10)`. The second call starts a timer for 1510 ms and loses the
first one's handle; the first still dismisses at about 1230 (200 ms of enter, then 1010) (measured: `willDismiss` 516 ms after `didPresent` for a duration of 500),
and the second then dismisses the presentation made at 1300 only 210 ms in, where its own timer would have run to about 2530. `dismiss()`
clears only the last handle, so a dismiss by hand doesn't stop the earlier one either.

**Avonic** starts the timer only when a present presents it (`OnPresented`), so a second `Present()` does nothing, and each
presentation has one timer, which its dismiss clears (`B7_A_Second_Present_Starts_No_Second_Timer`). Fairly sure, from the code; the
whole sequence hasn't been run in Chromium.

## `ion-loading`, `ion-toast`: a `duration` past 2^31 - 1 ms dismisses at once

**Measured in headless Chromium (#41); the cause is the browser's `setTimeout`.** Both pass `duration` (plus 10 for the loading) to
`setTimeout`, whose delay is a 32-bit integer: `setTimeout(fn, Infinity + 10)` and `setTimeout(fn, 3e9 + 10)` fire after 0 ms, while
`setTimeout(fn, 2147483647)` doesn't fire within 3 s. So `duration: Infinity`, which an app means as "until it's dismissed", shows the
overlay and dismisses it as soon as it's presented. **Avonic** never dismisses one that long by itself
(`AvonicLoadingTests.B7_A_Duration_Too_Long_For_A_Timer_Never_Comes`, and the toast's `B7_A_Duration_Too_Long_For_A_Timer_Never_Comes`).
Sure: measured.

## `ion-nav`: a string in `insertPages` or `setPages` throws a `TypeError`

**Measured in Ionic 9.0.11, md, headless Chromium (#130; `docs/components/ion-nav/ref/measurements.txt`, M8).** With a nav loaded,
`nav.insertPages(-1, ['page-three'])` rejects with `TypeError: Cannot use 'in' operator to search for 'component' in page-three`, and so
does `setPages(['page-one', 'page-two'])`. Their type is `NavComponent[] | NavComponentWithProps[]`, and a tag name is a
`NavComponent`: `convertToViews` tests `'component' in page`, which throws on a string. `{ component: 'page-three' }` works.

**Avonic** takes any page form in the list (`N22_InsertPages_and_SetPages_take_a_bare_page_form`). Sure: measured.

## `ion-nav`: a failed transition drops the calls queued behind it, which never settle

**Measured in Ionic 9.0.11, md, headless Chromium (#130; `ref/extra.txt`, X1b).** A minimal page:

```html
<ion-app><ion-nav root="page-one"></ion-nav></ion-app>
<script>
  const nav = document.querySelector('ion-nav');
  nav.push('page-two');      // resolves true
  nav.pop();                 // resolves true
  nav.pop();                 // rejects: navigation stack needs at least one root page
  nav.push('page-three');    // never resolves or rejects, and never runs
</script>
```

`failed()` empties `transInstr` (`this.transInstr.length = 0`) and settles only the failed call; the others are dropped without
calling their `resolve`, `reject` or `done`, so an `await` on one waits for ever. **Avonic** drops them too, and completes each with
false (and `done(false, false)`) (`N21_A_failure_drops_the_calls_queued_behind_it_and_each_completes_false`). Sure: measured.

## `ion-nav`: moving the nav in the DOM empties it, and a nav in an inline `ion-modal` shows nothing

**Measured in Ionic 9.0.11, md, headless Chromium (#130; `ref/extra.txt`, X7 and X8).**

```html
<ion-app>
  <ion-button id="open">Open</ion-button>
  <ion-modal trigger="open"><ion-nav root="page-one"></ion-nav></ion-modal>
</ion-app>
```

After a click on Open the modal is empty: `getLength()` is 0 and the nav has no children. `disconnectedCallback` unloads and removes
every view, and `componentDidLoad`, which sets `root`, runs once, so when `present()` moves the inline modal to `ion-app` (its delegate's
`attachViewToDom`) the nav comes back with nothing. The same with any nav moved to another parent (X8). Frameworks that mount the
modal's content only when it's presented don't meet it; core's inline modal does.

**Avonic** keeps the stack across a detach: Avalonia moves controls routinely (`InlinePlacement` moves an inline overlay at every
present) and has no "destroyed" (`N20_A_nav_moved_to_another_parent_keeps_its_stack`,
`N20_A_nav_in_an_inline_modal_keeps_its_stack_through_present_and_dismiss`). Sure: measured.
