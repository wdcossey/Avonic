# ion-select — Plan

`AvonicSelect`, `AvonicSelectOption`, `AvonicSelectModal` and `AvonicSelectPopover`: Ionic 9.0.11's `ion-select` family, md, in one
PR (#45). The four interfaces are the overlays already on `v2`, reused as they are: `AvonicAlert` (#39), `AvonicActionSheet` (#40),
`AvonicPopover` (#43) and `AvonicModal` (#42). The select builds each in code, as Ionic's controllers do, and presents it in its own
top level. `ion-select-popover` is `@internal` in Ionic and has no `core.json` entry; it's here as `AvonicSelectPopover`, public, as
the alert's `AlertRadioButton` is, so that a theme can style it. `ion-select-modal` is in `core.json`, with four props.

One PR, not two: the popover's and the modal's content are the same list of items with radios or checkboxes, with the same
`select-popover.md.css` and `select-modal.md.css` item rules word for word, so they share one builder
(`SelectOptionList`). Splitting off the modal would have left half of it in each.

Measured in real Ionic 9.0.11 in headless Chromium, md, on `render.js`'s phone (390 x 844 at 2x, mobile and touch, Roboto): one
script (`measure.js`, in the agent's temp folder as the brief asks) builds a page of selects in an `ion-app`, taps each into its
interface, and dumps every box, colour and transform, light and shadow DOM, with a frame of each. Its output is in [`ref/`](ref/);
[COMPARISON.md](COMPARISON.md) says which test asserts which number.

## Fidelity checklist — `ion-select`

`node tools/ionic-api.js ion-select`.

### Props (23)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `cancelText` | `CancelText` | `string` | `"Cancel"` | The alert's and the action sheet's cancel button; the modal's close button |
| [x] | `color` | `Color` | `string?` | | `ColorPalette.ColorProperty.AddOwner`: `:ion-color`, the highlight's focused colour is the colour's base |
| [x] | `compareWith` | `CompareWith` | `object?` | | As `AvonicRadioGroup.CompareWith`: a `Func<object?, object?, bool>` given the current value then the option's, or the name of a property (or dictionary key) compared on both. Anything else is strict equality, `Equals`, and an `IList` option value matches any of its items, as `compareOptions` does |
| [x] | `disabled` | `IsEnabled` | `bool` | `true` | `:select-disabled`; `open()` does nothing |
| [x] | `errorText` | `ErrorText` | `string?` | | Under the select while it has the classes `ion-touched` and `ion-invalid` |
| [x] | `expandedIcon` | `ExpandedIcon` | `string?` | | An `AvonicIcon.Icon`; shown while open, and then no rotation |
| [x] | `fill` | `Fill` | `SelectFill?` | | `Outline`, `Solid` |
| [x] | `helperText` | `HelperText` | `string?` | | |
| [x] | `interface` | `Interface` | `SelectInterface` | `Alert` | `Alert`, `ActionSheet`, `Popover`, `Modal` |
| [x] | `interfaceOptions` | `InterfaceOptions` | `IReadOnlyDictionary<string, object?>?` | | See [Interface options](#interface-options) |
| [x] | `justify` | `Justify` | `SelectJustify?` | | `Start`, `End`, `SpaceBetween`; not with a floating or stacked label |
| [x] | `label` | `Label` | `string?` | | Beats `LabelContent` |
| [x] | `labelPlacement` | `LabelPlacement` | `SelectLabelPlacement` | `Start` | `Start`, `End`, `Fixed`, `Floating`, `Stacked` |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |
| [x] | `multiple` | `Multiple` | `bool` | `false` | The value is then a list: `object?[]` from the interfaces; any `IList` that isn't a string is read as one, as `Array.isArray` |
| [ ] | `name` | none | | | Not applicable: `renderHiddenInput`'s form field; Avalonia has no HTML form. `StyledElement.Name` is the control's name |
| [x] | `okText` | `OkText` | `string` | `"OK"` | The alert's OK button |
| [x] | `placeholder` | `Placeholder` | `string?` | | Shown when the text is `""`; `:has-placeholder` |
| [x] | `required` | `Required` | `bool` | `false` | `AutomationProperties.IsRequiredForForm` at style priority |
| [x] | `selectedText` | `SelectedText` | `string?` | | Shown in place of the options' text when not null or `""` |
| [x] | `shape` | `Shape` | `SelectShape?` | | `Round` |
| [x] | `toggleIcon` | `ToggleIcon` | `string?` | | Null is md's `caret-down-sharp` |
| [x] | `value` | `Value` | `object?` | | Two-way by default; set by the interfaces with `SetCurrentValue`. Null is `undefined` |

### Events (5)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionBlur` | `IonBlur` | The select lost focus |
| [x] | `ionCancel` | `IonCancel` | The alert's or the action sheet's cancel button's handler: its button, the backdrop, Escape or back |
| [x] | `ionChange` | `IonChange` | `SelectChangeEventArgs` with `Value`. Only from an interface, and only when the value isn't equal to the last (`isValueEqual`, a multiset compare for `multiple`) |
| [x] | `ionDismiss` | `IonDismiss` | After the overlay's `onDidDismiss` |
| [x] | `ionFocus` | `IonFocus` | The select has focus |

`ionStyle` is `@internal`: `IonStyle` with `interactive-disabled`, which `AvonicItem` already reads.

### Methods (1)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `open(event?: UIEvent)` | `Task<AvonicOverlay?> Open(PopoverEvent? ev = null)` | The overlay, presented, or null when disabled or already open. The event is the popover's position, as for `AvonicPopover.Present`; with none, a `popover` interface falls back to `alert`, as Ionic's does |

### Slots (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `end` | `EndContent` (+ `EndContentTemplate`) | `PART_End`, after the icon of a floating or stacked label |
| [x] | `label` | `LabelContent` (+ `LabelContentTemplate`) | Shown when `Label` is null |
| [x] | `start` | `StartContent` (+ `StartContentTemplate`) | `PART_Start` |

The options are `Items`, a list of `AvonicSelectOption` (`[Content]`), as `ion-select` reads its `ion-select-option` children; Ionic
lists no default slot for them.

### Shadow parts (13)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `wrapper` | `PART_Wrapper` | `.select-wrapper`, the `<label>` |
| [x] | `start` | `PART_SelectStart` | `.select-start`; its slot is `PART_Start`, which `Styles/TextField.axaml`'s `IInputHost` rule reads |
| [x] | `control` | `PART_Control` | `SelectControlPanel`: the label and the container |
| [x] | `label` | `PART_Label` | `.label-text-wrapper` |
| [x] | `container` | `PART_Container` | `.native-wrapper`: the text, and the icon when the label isn't floating or stacked |
| [x] | `text`, `placeholder` | `PART_Text` | One `TextBlock`, `:has-placeholder-shown` when it shows the placeholder, as Ionic swaps the part name |
| [x] | `icon` | `PART_Icon`, `PART_EndIcon` | One `AvonicIcon` in each place; the template shows the one the placement wants |
| [x] | `end` | `PART_SelectEnd` | `.select-end`; its slot is `PART_End` |
| [x] | `bottom` | `PART_Bottom` | |
| [x] | `supporting-text`, `helper-text` | `PART_HelperText` | |
| [x] | `supporting-text`, `error-text` | `PART_ErrorText` | |

### CSS custom properties (23)

| ✓ | Ionic | Avonic resource key | md default |
|---|---|---|---|
| [x] | `--background` | `Avonic.Select.Background` | unset: transparent; `step-50` solid, `step-100` hovered, `step-150` focused or open |
| [x] | `--border-color` | `Avonic.Select.BorderColor` | unset: `Avonic.ItemBorderColorBrush`; step 500 solid, 300 outline, 750 hovered, the highlight focused or open |
| [x] | `--border-radius` | `Avonic.Select.BorderRadius` | unset: 4 with a fill, 16 round, 28 outline round |
| [x] | `--border-style` | none | Not applicable: Avalonia draws solid borders only. Ionic's default is `solid` |
| [x] | `--border-width` | `Avonic.Select.BorderWidth` | 1 |
| [x] | `--highlight-color-focused` | `Avonic.Select.HighlightColorFocused` | unset: primary, or the colour's base |
| [x] | `--highlight-color-invalid` | `Avonic.Select.HighlightColorInvalid` | danger |
| [x] | `--highlight-color-valid` | `Avonic.Select.HighlightColorValid` | success |
| [x] | `--highlight-height` | `Avonic.Select.HighlightHeight` | 2 |
| [x] | `--padding-bottom`, `--padding-end`, `--padding-start`, `--padding-top` | `Avonic.Select.PaddingBottom` and the rest | unset: 0, or 16 at the sides with a fill, 32 for a round outline |
| [x] | `--placeholder-color` | `Avonic.Select.PlaceholderColor` | unset: the text's colour |
| [x] | `--placeholder-opacity` | `Avonic.Select.PlaceholderOpacity` | unset: `Avonic.PlaceholderOpacity`, 0.6 |
| [x] | `--ripple-color` | none | Not applicable: md's select has no ripple (`select.js` renders none; the variable is only documented) |
| [ ] | `--select-text-gap`, `--select-text-media-*` (7) | none | Not applicable: they style the rich content the select text shows only with `innerHTMLTemplatesEnabled`, off by default; Avonic has no HTML to show (as `ion-infinite-scroll`'s `loadingText`) |

## Fidelity checklist — `ion-select-option`

`node tools/ionic-api.js ion-select-option`. A host with `display: none`: data the select reads.

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `description` | `Description` | `string?`: a line under the option's text in the popover and the modal. **Not** in the alert and the action sheet, whose inputs and buttons have no such field on `v2` (see [Dependencies](#dependencies)) |
| [x] | `disabled` | `IsEnabled` | Its own, not an ancestor's, as `option.disabled` |
| [x] | `justify` | `Justify` | `SelectOptionJustify?`: the option's radio or checkbox in the popover and the modal; null is md's `start`. Not in the alert (as `description`) |
| [x] | `labelPlacement` | `LabelPlacement` | `SelectOptionLabelPlacement?`: as `Justify`; null is md's `end` |
| [ ] | `mode` | none | Not exposed (ADR 0001) |
| [x] | `value` | `Value` | `object?`; null is the option's text, as `getOptionValue` |
| [x] | `(default)` slot | `Content` | Its text is the option's: a string, or the text of what's in it (`getDefaultSlotPlainText`: concatenated, white space collapsed and trimmed) |
| [ ] | `start`, `end` slots | none | Not applicable: Ionic reads them only with `innerHTMLTemplatesEnabled` (off by default), clones them into the interface and the select text, and ignores them otherwise. Avonic has no such switch, and a control can't be in two places; declared, they would do nothing (rule 3) |

## Fidelity checklist — `ion-select-modal`

`node tools/ionic-api.js ion-select-modal`.

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `cancelText` | `CancelText` | `"Close"`: the toolbar's button, which dismisses the modal |
| [x] | `header` | `Header` | `ion-title`, when not null |
| [x] | `multiple` | `Multiple` | `bool?`: checkboxes when true, radios otherwise |
| [x] | `options` | `Options` | `IList<SelectOverlayOption>` |

`ion-select-popover` (`@internal`): `Header`, `SubHeader`, `Message`, `Multiple`, `Options`, the same way.

`SelectOverlayOption` is Ionic's interface of that name (`SelectModalOption`, `SelectPopoverOption` and the rich fields): `Text`,
`Value`, `Disabled`, `Checked`, `CssClass`, `Handler` (`Func<object?, object?>`, given the value or values), `Description`,
`LabelPlacement`, `Justify`.

## Behaviours

From `select.js`, `select-popover.js`, `select-modal.js`, `select-option-render.js`, `overlay-control-label.js`,
`compare-with-utils.js`, `watch-options.js`, `click-controller.js` and the three `.md.css`. Each is a test (`AvonicSelectTests`,
`AvonicSelectInterfaceTests`, `AvonicSelectCaptureTests`).

### The select

- **B1 Text.** `getText`: `SelectedText` if not null or `""`; else the text of the option whose value matches (`textForValue`), or
  for a list each match's text joined with `", "`, skipping values with no option; `""` when the value is null. `hasValue` is
  text not `""`. With `""` and a `Placeholder`, the placeholder shows, at `--placeholder-opacity`.
- **B2 Options.** An option's value is its `Value`, or its text when null. Adding or removing an option, or changing one's text,
  value or `IsEnabled`, renders the text again (`watchForOptions`' `forceUpdate`), and updates an open interface's options
  (`updateOverlayOptions`).
- **B3 compareWith.** A function, a property name, or strict equality; an option value that's a list matches any item.
- **B4 Open.** A click anywhere on the host (`onClick`), or Enter or Space while focused (the hidden `<button>`), focuses the
  select and opens the interface; not when disabled or already open. A click that starts on interactive content in the start or
  end slot (a button, a checkbox) doesn't open it (`INTERACTIVE_SLOTTED_CONTENT`); decorative slotted content does.
- **B5 Interface choice.** `ActionSheet` with `Multiple` uses the alert; `Popover` with no event (`Open()` from code) uses the
  alert.
- **B6 Expanded.** While open: `:select-expanded`; the md icon turns 180° over 150 ms unless `ExpandedIcon` is set, which then
  shows instead; a floating label floats. After `onDidDismiss`: not expanded, `IonDismiss`, then the select is focused again.
- **B7 setValue.** An interface's choice sets `Value` and raises `IonChange` with it, unless it equals the current value
  (`isValueEqual`: both null; for `Multiple`, the same length and each new value matching a distinct current one).
- **B8 Focus.** `:has-focus`, `IonFocus` and `IonBlur` follow focus however it comes; `FocusVisible.IsEnabled`
  (`ion-focusable`), so an item around reacts.
- **B9 Label.** `:label-floating` when stacked, or floating with a value, focus or open; a floating label sits on the field at
  rest (`translateY(100%)`), the placeholder hidden until it floats.
- **B10 Disabled.** `opacity: 0.38` as one (`OpacityMask`), no input, and `ionStyle` `interactive-disabled` to an item.
- **B11 Validation.** `ion-touched` and `ion-invalid` (classes, followed): the error text replaces the helper text, the
  highlight, border, label and icon take the invalid colour; `ion-valid` the valid colour while focused or open.
- **B12 Layout.** The row: the label then the container (text and icon), `space-between` unless `Justify`, `End` reversed,
  `Fixed` 100 wide; the container gives way before the label (`flex-shrink: 100000`), its text trimmed; an empty text keeps 2ch.
  Floating and stacked: a column, the container the row's width, the icon in `.select-end`. 44 high, 56 with a fill or a floating
  or stacked label. In an item, `flex: 1 1 0`.
- **B13 Fill and shape.** Solid: background, bottom border, the top corners 4; hover and focus colours. Outline: the notched
  border, the label in the notch. Round: 16, or 28 with the outline and 32 padding.
- **B14 Highlight.** Not with the outline, nor in an item: a bar at the bottom that scales from 0 to 1 over 200 ms when focused or
  open.
- **B15 Automation.** A button peer with `IExpandCollapseProvider` (`aria-haspopup`, `aria-expanded`), named `"<label>, <text>"`
  (`ariaLabel`: an app's `AutomationProperties.Name` beats the label; the placeholder when there's no text), help text from
  `getHintTextId`, `IsRequiredForForm` from `Required`; the text and icon are raw (`aria-hidden`).
- **B16 Item.** In an `AvonicItem`, `IItemControl` `Select`: a tap on the item's padding opens it.

### The interfaces

- **B17 Alert.** Header `interfaceOptions.header`, else the label's text; radios (or checkboxes with `Multiple`) with each option's
  text, value, checked state (`isOptionSelected`), disabled state and classes (`select-interface-option` and the option's own);
  Cancel (`CancelText`, role `cancel`, handler `IonCancel`) and OK (`OkText`, handler `setValue(values)`); `CssClass`
  `select-alert`, the option's own, `single-select-alert` or `multiple-select-alert`.
- **B18 Action sheet.** A radio button per option (`role` `radio`, `aria-checked`), whose handler sets the value; Cancel with role
  `cancel` and handler `IonCancel`; `CssClass` `select-action-sheet`.
- **B19 Popover.** `AvonicPopover` with `Event` the select's container (`ionShadowTarget`), `Alignment` center, `Size` auto, no
  backdrop in md; `Size` cover with a fill or a floating or stacked label, against the whole select. Classes `select-popover`, and
  `select-popover-rich-content` with a description, which makes `--width` `clamp(250px, 100vw - 40px, 400px)`.
- **B20 Modal.** `AvonicModal` with an `AvonicSelectModal`: header `interfaceOptions.header`, `CancelText`; classes
  `select-modal`.
- **B21 Select popover and modal content.** A list of items: radios in a radio group, or checkboxes; the checked item's
  background (`item-radio-checked`: primary at 0.08; focused 0.2, hovered 0.12) or colour (`item-checkbox-checked`: primary
  text); no inner border; the radio's mark hidden and its label's margins 0; md defaults `labelPlacement` `end` and `justify`
  `start` unless the option sets them; the description under the text, 12 px, step 700. The popover adds a list header
  (`header`) and an item of `subHeader` (`h3`) and `message` (`p`); the modal a header with a toolbar, the title and a clear
  button.
- **B22 Choosing.** A radio's click dismisses; its group's change calls the option's handler, which sets the value and closes.
  Space selects and dismisses; Enter commits the focused radio and dismisses on its key up if it went down on the same radio;
  the arrows move focus only, without choosing (radio-group's `inSelectInterface`). A checkbox toggles its option and calls the
  handler with every checked value, which sets the value and leaves it open.
- **B23 Focus at present.** At the popover's or modal's will-present, the selected option's radio or checkbox is focused and its
  item shown focused (`focusVisibleElement`, in an `AvonicApp`); with no match (`indexOf` by strict equality, so never for
  `Multiple`), the first enabled radio or checkbox. For the alert and the action sheet, a frame after their will-present, the
  selected option's button is focused.
- **B24 Interface options.** Applied to the overlay by property name, before the select's own settings for what Ionic puts after
  the spread; see below.
- **B25 Changes while open.** New options or values while open update the interface (`updateOverlayOptions`).

### Interface options

Ionic spreads `interfaceOptions` into the overlay's create options: any of its props, by name. Here it's a dictionary of names to
values, the conservative choice, as the alert's and the action sheet's `HtmlAttributes` are: each key is the overlay's property
in Ionic's camelCase or Avonic's PascalCase (`"header"`, `"Header"`, `"size"`), and a string value is converted to the property's
type as XAML would (`"cover"` to `PopoverSize.Cover`). A key the overlay has no property for is ignored, as an unknown property is
on an element. The select's own values for what Ionic sets after the spread win (the alert's inputs, buttons and class, the
popover's component and class, the modal's class); the popover's `event`, `alignment`, `size` and `showBackdrop` and the alert's
`header` come before it, so the dictionary beats them. `header`, `subHeader` and `message` go to the popover's content, and
`header` to the modal's, as `componentProps`. `cssClass` (a string or a list of strings) adds style classes.

A typed object per interface was the alternative; it would have to follow each overlay's properties by hand. For the human to
decide (in the PR).

## Visual states and keys

`select.md.css`, translated as `input.md.css` was for `AvonicInput`: the theme reuses `FieldConverters` and the same outline,
notch and label transform. `Tokens/Select.axaml` holds one key per variable above, `CssUnset` where Ionic falls back.
`select-popover.md.css` and `select-modal.md.css` have no variables: their item rules are a literal in the theme, as resource
dictionaries the content merges into each item's own `Resources` (`--background` on `.item-radio-checked` is the item's
`Avonic.Item.Background` there), and styles on the content's root for the radio's parts.

## Avonic extensions

None.

## Dependencies

All on `v2`: `AvonicAlert`, `AvonicActionSheet`, `AvonicPopover` (and `PopoverEvent`), `AvonicModal`, `AvonicItem`, `AvonicList`,
`AvonicListHeader`, `AvonicLabel`, `AvonicRadio`, `AvonicRadioGroup`, `AvonicCheckbox`, `AvonicHeader`, `AvonicToolbar`,
`AvonicTitle`, `AvonicButtons`, `AvonicButton`, `AvonicContent`, `AvonicIcon`, `FieldConverters`, `IInputHost`.

Not on `v2`, and left alone as other controls' files (the brief's shared-file rule):

- `AlertInput` and `ActionSheetButton` have no `description`, `labelPlacement`, `justify`, `startContent` or `endContent`, which
  Ionic 9's alert and action sheet render for the select (`renderOptionLabel`). The option's `Description`, `LabelPlacement` and
  `Justify` show in the popover and the modal only. Proposed in the PR, with an issue.
- `AvonicRadioGroup` has no `inSelectInterface` (arrows that only move focus, Enter that commits): the select's content does it
  for its own group, in its own handlers, before the group's. Proposed in the PR.
