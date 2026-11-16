# ion-checkbox — Comparison

Ionic 8.8.19 in headless Chromium, md, 390 wide at a scale of 2, against Avonic in Avalonia.Headless (Skia) at the same size and
scale. Both use Roboto from `tools/fonts`. Headless only: not tried in a real window, and not tried on a device.

The comparison was first made against 8.8.1. For #188 the reference was rendered again with 8.8.19: the boxes and the frame are
the same, and the only change is the new `ion-focusable` on every standalone checkbox. 8.8.17's keyboard focus indicator is in
[its own section](#keyboard-focus-ionic-8817).

## The scene

Each standalone checkbox is in a `<div>` of its own, then a list of items. The reference:

```
node tools/ionic-ref/render.js ion-checkbox --bare --html '<ion-app style="position:static;display:block;contain:none;overflow:visible"><div><ion-checkbox id="c1">Label start</ion-checkbox></div><div><ion-checkbox id="c2" checked>Checked</ion-checkbox></div><div><ion-checkbox id="c3" indeterminate>Indeterminate</ion-checkbox></div><div><ion-checkbox id="c4" label-placement="end">Label end</ion-checkbox></div><div><ion-checkbox id="c5" label-placement="fixed">Fixed label that is long</ion-checkbox></div><div><ion-checkbox id="c6" label-placement="stacked" checked>Stacked</ion-checkbox></div><div><ion-checkbox id="c7" justify="space-between" checked color="danger">Space between, danger</ion-checkbox></div><div><ion-checkbox id="c8" disabled checked>Disabled</ion-checkbox></div><div><ion-checkbox id="c9" helper-text="Helper text">With helper</ion-checkbox></div><div><ion-checkbox id="c10" class="ion-touched ion-invalid" helper-text="Helper text" error-text="Error text">Invalid</ion-checkbox></div><div><ion-checkbox id="c11"></ion-checkbox></div><ion-list><ion-item><ion-checkbox id="i1">In an item</ion-checkbox></ion-item><ion-item><ion-checkbox id="i2" checked label-placement="end">In an item, end</ion-checkbox></ion-item><ion-item><ion-checkbox id="i3" slot="start"></ion-checkbox><ion-label>Start slot</ion-label></ion-item><ion-item><ion-checkbox id="i4" label-placement="stacked" alignment="start">Stacked, start</ion-checkbox></ion-item><ion-item><ion-checkbox id="i5" helper-text="Helper text">In an item, helper</ion-checkbox></ion-item></ion-list></ion-app>'
```

Avonic's is `AvonicCheckboxCaptureTests.Reference_Page`, the same content, saved with `AVONIC_CAPTURE_DIR`.

![Ionic left, Avonic right](compare/side-by-side.png)

The boxes, the timeline, the keys, the clicks and the accessibility tree were measured with a throwaway Playwright script in the
same page (Chromium's `getBoundingClientRect`, `getComputedStyle` sampled each `requestAnimationFrame`, `keyboard`, `mouse`,
and CDP's `Accessibility.getFullAXTree`).

## Boxes

x, y, width, height of the host. Avonic lays out at half pixels at a scale of 2.

| id | Ionic | Avonic | |
|---|---|---|---|
| c1 | 0 0 109.89 19 | 0 0 110 19 | match |
| c2 | 0 19 95.55 19 | 0 19 96 19 | match |
| c3 | 0 38 133.02 19 | 0 38 133.5 19 | match |
| c4 | 0 57 103 19, box at 0, label at 34 | 0 57 103 19, the same | match |
| c5 | 0 79.5 134 19, box at 116 | 0 76 134 19, box at 116 | match, but 3.5 higher: see below |
| c6 | 57.28 × 53, label 42.96 × 14.25 at + 7.16, box at + 19.64, + 35 | 57.5 × 53, the same | match |
| c7 | 390 wide, box at 372 | the same | match |
| c9 | 114.19 × 37 | 114.5 × 37.5 | match |
| c11 | 18 × 18 | 18 × 18 | match |
| i1 | 358 × 47 at 16 | the same | match |
| i3 | 18 × 18, 15 into the row | the same | match |
| i4 | 358 × 73, box 45 down | the same | match |
| i5 | 358 × 57 | 358 × 57.5 | match |

The standalone rows drift by a few pixels from `c5` down: the host is `display: inline-block` in a line box, and Chromium's line
box for `c4`'s `<div>` is 22.5 high, not 19, from the inline-block's baseline. That's the page's inline formatting around the
checkbox, which Avalonia doesn't have: a checkbox in a `StackPanel` is as high as itself. The items, which are blocks, line up
again.

## Behaviours

| # | Behaviour | Ionic | Avonic | |
|---|---|---|---|---|
| 1 | A click anywhere toggles once | the box, the label (`c1` at x 20) and the gap (`c7` at x 250): one `ionChange` each | the same | match |
| 2 | Indeterminate, clicked | `{checked: true}`, then `indeterminate` false | the same, `Indeterminate` still true during `IonChange` | match |
| 3 | `checked` set in code | no `ionChange` | none | match |
| 4 | Keys | Enter nothing; Space on key down, every key down, repeats too | the same | match |
| 5 | Focus | a tab stop; a click focuses; `ionFocus`, `ionBlur` | the same | match |
| 6 | Disabled | no click, no Space, no tab stop; label 0.38, box 0.63 | the same | match |
| 7–12 | Layout | see Boxes | | match |
| 13 | Colours | unchecked white with `rgba(0, 0, 0, 0.6)`, 2 px, radius 2.25; checked `rgb(0, 84, 233)` | the same | match |
| 14 | The mark | md's tick and bar, stroke 3 of 24 units, white | the same paths, drawn in code | match, see the zoom below |
| 15 | Drawing the mark | dash offset 30 until 80 ms, 26.67 at 97, 10.03 at 147, 0 at 181; unchecking removes it in the first frame | 30 until 90 ms, then linear to 0 at 180, on `AvonicConfig`'s clock; removed at once | match |
| 16 | The box background | eases 180 ms `cubic-bezier(0.4, 0, 0.2, 1)` both ways: `rgb(177, 203, 248)` at 48 ms; the border at once | a `BrushTransition` of 180 ms with that `CubicBezierEasing`, from the first frame; the border at once | match |
| 17 | Colour | `danger`: `rgb(197, 0, 15)` background and border | the same; an app's key beats it | match |
| 18 | No text | the label's box is `display: none` | hidden | match |
| 19 | Helper and error | helper 12 px `rgb(77, 77, 77)` 4 below; error `rgb(197, 0, 15)` with `ion-touched` and `ion-invalid`; classes added later are followed; `ion-invalid` alone shows the helper | the same | match |
| 21–23 | In an item | see Boxes | | match |
| 24 | A tap on the item's padding | toggles (`i1` at x 5) | toggles | match |
| 25 | Accessibility | `checkbox`, `checked: mixed` when indeterminate; name `With helper Helper text` (the label holds the helper), `Invalid` (not the error); `required` | `CheckBox`, `Indeterminate`; the same names; `AutomationProperties.IsRequiredForForm` | match |
| 26 | Live regions | helper `aria-live="polite"`, error `role="alert"` | `Polite`, `Assertive` | match |

### Deliberate differences and gaps

- **Inline layout.** As above, the few pixels a line box adds around an `inline-block` aren't there.
- **A long standalone label.** Ionic's `inline-block` is as wide as its label's one line even when that's wider than the page:
  measured, a 485.2 label makes a 519.2 host in a 390 page, with the box off screen at 501.2. Avalonia gives a control no more than
  the space there is, so here the label ends in an ellipsis and the box is at the end, at 372. In an item, or with `Justify` or
  `Alignment`, both shrink the label to fit (measured 324 in an item), and match.
- **Arranged narrower than measured.** A parent that measures the checkbox wider than it then arranges it clips the label at its
  box, without an ellipsis: measuring the label again during arrange would ask for another layout pass after every one.
- **`aria-invalid`.** Avalonia's automation has no invalid state, so it isn't exposed. The error text is the help text, and its
  change raises the help text's property-changed event, which is what a screen reader announces.
- **`name` and the form.** The hidden `<input>` exists for form submission, which Avalonia doesn't have; `value` is still in
  `IonChange`.
- **A label's text changed later.** Since 8.8.4 Ionic reads `textContent` again whenever text or children change inside the
  checkbox (a `MutationObserver` with `characterData`, `childList` and `subtree`), so text coming into an empty label shows at once:
  measured, `textContent = 'Later'` and a `<span>`'s text both show the label with no other change. Avonic reads it at each change
  of a prop and each measure, which a shown label's text changing causes, and, while the label is hidden and so not measured,
  watches the text and the children of what's inside (`TextContentWatch`). A match: `B18_Text_That_Comes_Later…`,
  `B18_A_Child_With_Text_Added_Later…`. The watch covers a `TextBlock`'s `Text` and `Inlines`, a `Run`'s `Text`, a content
  control's `Content`, a presenter's child, and any logical children coming or going.

## Keyboard focus (Ionic 8.8.17)

8.8.17 gave the checkbox a focus indicator and took Chromium's outline away. The frame has `class="ion-focused"` where keyboard
focus puts it, measured: on the checkbox when it's `ion-focusable`, and on the item when that is.

```
node tools/ionic-ref/render.js ion-checkbox --bare --html '<ion-app style="position:static;display:block;contain:none;overflow:visible"><div style="padding:16px;display:flex;flex-direction:column;gap:24px;align-items:flex-start"><ion-checkbox class="ion-focused">Unchecked</ion-checkbox><ion-checkbox class="ion-focused" checked>Checked</ion-checkbox><ion-checkbox class="ion-focused" checked color="danger">Danger</ion-checkbox><ion-checkbox class="ion-focused" label-placement="end">Label at the end</ion-checkbox><div dir="rtl"><ion-checkbox class="ion-focused">RTL</ion-checkbox></div></div><ion-list><ion-item class="ion-focused"><ion-checkbox>Single input: the item draws it</ion-checkbox></ion-item><ion-item><ion-checkbox slot="start"></ion-checkbox><ion-checkbox class="ion-focused">Multi-input</ion-checkbox></ion-item><ion-item class="ion-focused" button><ion-checkbox class="ion-focused">Clickable item</ion-checkbox></ion-item><ion-item><ion-checkbox class="ion-focused" label-placement="end">Label at the end, clipped</ion-checkbox><ion-checkbox slot="end"></ion-checkbox></ion-item><ion-item><div><ion-checkbox class="ion-focused">Wrapped</ion-checkbox></div><ion-checkbox slot="end"></ion-checkbox></ion-item><ion-item><ion-checkbox slot="start" class="ion-focused"></ion-checkbox><ion-label>Start slot</ion-label><ion-checkbox slot="end"></ion-checkbox></ion-item></ion-list></ion-app>' --out docs/components/ion-checkbox/ref/focus
```

Avonic's is `AvonicFocusIndicatorCaptureTests.Checkbox_Focus_Page`, with `:ion-focused` on the same controls. Ionic left:

![Ionic left, Avonic right](compare/side-focus.png)

The states were measured by focusing each checkbox from the keyboard (Playwright, in `ion-app`), reading the host's and the item's
classes, the `::after`'s computed style, its box, every clipping ancestor in the composed tree, and pixels from a screenshot:

| Case | Ionic 8.8.19 | Avonic | |
|---|---|---|---|
| Alone | `ion-focusable`; `ion-focused` by Tab; `.native-wrapper::after` 36 × 36 at −9, −9 from the 18 box, `rgb(26, 101, 235)` at 0.2, radius 50%, over the box: `rgb(209, 224, 251)` on white | `PART_Focus`, the same box and colour, over the box | match, `B27_Keyboard_Focus…` |
| Checked, `danger`, RTL, label at the end | the same circle, the primary tint whatever the colour, centred on the box | the same | match, `B27_The_Circle_Is_The_Primary_Tint…` |
| `:host(:focus)` | `outline: none` | `FocusAdorner` null: no adorner over a base theme | match, `B27_There_Is_No_Focus_Adorner…` |
| A click | no `ion-focused` | no circle | match, `B27_A_Click_Shows_No_Circle` |
| Single-input item | not `ion-focusable`; the item is, and draws its 0.12 overlay | the same | match, `B27_In_A_Single_Input_Item…` |
| Multi-input item (two checkboxes; a checkbox and a toggle) | `ion-focusable`, the circle; the item `item-focus-indicator-room`, and not `ion-focusable` with checkboxes alone | the same | match, `B27_In_A_Multi_Input_Item…` |
| Clickable item (`button`) | multi-input, so the circle; the item focused too | the same | match, `B27_In_A_Clickable_Item…` |
| Turning multi-input later | a second checkbox added inside a `<div>`: the first becomes `ion-focusable`; removed, it isn't | the same | match, `B27_It_Follows_The_Item…` |
| Clipping | the circle at the end of the default slot shows to 383 (`.input-wrapper` reaches 386); at the start it's clipped at `.item-inner`'s edge, 16 | the same | match, the item's `B36_…` |
| Inside a wrapper `<div>` | the `<div>` is as wide as the checkbox, so the box is after the label | an Avalonia panel in the slot stretches, so the box is at the slot's end; the frame aligns the panel left | not focus: a slotted panel's width, as before |

## The mark, zoomed

The `c2` box at six times, Ionic left: the tick's ends, joins and width match to the antialiasing.

![zoom](compare/zoom.png)
