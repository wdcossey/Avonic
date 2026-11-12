# ion-searchbar — Plan

A search field: a native `<input type="search">` with a search icon at its start, a clear button at its end, and in md a
cancel button (a back arrow) that takes the search icon's place. md draws the input as a white card: 2 px corners, md's
three-part shadow, 55 px of padding at each side for the icons. Measurements are from real Ionic 8.8.1 in Chromium, md, at
390 px wide, wrapped in `ion-app`, with throwaway Playwright scripts (a mouse and keyboard in a desktop context for the events;
a mobile one for the boxes). The commands are at the top of [COMPARISON.md](COMPARISON.md).

## Fidelity checklist

`node tools/ionic-api.js ion-searchbar`.

### Props (23)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `animated` | `Animated` (`bool`) | md has no rule on what it changes: the host classes `searchbar-animated` (with `AvonicConfig.Animated`), `searchbar-no-animate` (the first 300 ms) and `searchbar-left-aligned` (`!animated`, a value or focus, worked out at load, focus, blur and a change of `showCancelButton`). Kept as `:animated`, `:no-animate` and `:left-aligned`, with no md rule, for an app's style, as `TitleSize.Large` is. iOS's centred placeholder and sliding cancel button are iOS only (ADR 0001) |
| [x] | `autocapitalize` | `Autocapitalize` (`string`, `"off"`) | As `ion-input`: `on`, `sentences`, `words` → `TextInputOptions.AutoCapitalization`; `characters` → `Uppercase` |
| [ ] | `autocomplete` | — | Not applicable: the browser's autofill. Avalonia has none (as `ion-input`) |
| [x] | `autocorrect` | `Autocorrect` (`SearchbarAutocorrect`, `Off`) | `On` → `TextInputOptions.ShowSuggestions` |
| [x] | `cancelButtonIcon` | `CancelButtonIcon` (`string`, `"arrow-back-sharp"`) | An `AvonicIcon.Icon`. Ionic's default is `config.get('backButtonIcon', arrowBackSharp)`; Avonic has no `backButtonIcon` config yet (proposed) |
| [x] | `cancelButtonText` | `CancelButtonText` (`string`, `"Cancel"`) | In md, the cancel button's `aria-label` only: its automation name |
| [x] | `clearIcon` | `ClearIcon` (`string?`) | Null is md's `close-sharp` |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`: the input's background is the base, the text, icons and buttons the contrast |
| [x] | `debounce` | `Debounce` (`int?`, ms) | Delays `IonInput` only, on `AvonicConfig.Schedule`, as `ion-input` |
| [x] | `disabled` | `IsEnabled` | Mapping rule |
| [x] | `enterkeyhint` | `Enterkeyhint` (`SearchbarEnterkeyhint?`) | → `TextInputOptions.ReturnKeyType`, as `ion-input` |
| [x] | `inputmode` | `Inputmode` (`SearchbarInputmode?`) | → `TextInputOptions.ContentType`, beating `Type`'s, as `ion-input`. `none` isn't declared, as there |
| [x] | `maxlength` | `Maxlength` (`int?`) | `TextBox.MaxLength`: typing stops there. Measured: `abcdef` types `abc` |
| [ ] | `minlength` | — | Not applicable: HTML form constraint validation (as `ion-input`) |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form (as `ion-input`) |
| [x] | `placeholder` | `Placeholder` (`string?`, `"Search"`) | `TextBox.PlaceholderText`. Plain text: Ionic's HTML in a placeholder is sanitised text anyway |
| [x] | `searchIcon` | `SearchIcon` (`string?`) | Null is md's `search-sharp` |
| [x] | `showCancelButton` | `ShowCancelButton` (`SearchbarShowCancelButton`, `Never`) | `Always`, `Focus`, `Never` |
| [x] | `showClearButton` | `ShowClearButton` (`SearchbarShowClearButton`, `Always`) | `Always`, `Focus`, `Never` |
| [ ] | `spellcheck` | — | Not applicable: Avalonia's `TextBox` has no spell checker (as `ion-input`) |
| [x] | `type` | `Type` (`SearchbarType`, `Search`) | `Email`, `Number`, `Password`, `Search`, `Tel`, `Text`, `Url`: the keyboard's `ContentType`; `Password` masks with `•` and is sensitive. `Search` also clears on Escape, as Chromium's search field does |
| [x] | `value` | `Value` (`string?`, `""`) | Two-way by default, as the input sets it |

### Events (6), methods (2)

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionInput` | `IonInputEvent`, `IonInput`, with `SearchbarInputEventArgs` (`Value`) |
| [x] | `ionChange` | `IonChangeEvent`, `IonChange`, with `SearchbarChangeEventArgs` (`Value`) |
| [x] | `ionFocus` | `IonFocusEvent`, `IonFocus` |
| [x] | `ionBlur` | `IonBlurEvent`, `IonBlur` |
| [x] | `ionClear` | `IonClearEvent`, `IonClear` |
| [x] | `ionCancel` | `IonCancelEvent`, `IonCancel` |
| [x] | `setFocus()` | `SetFocus()` |
| [x] | `getInputElement()` | `GetInputElement()`: the `TextBox`, applying the template first if it hasn't been |

No slots and no shadow parts (it's scoped). Template parts, by Ionic's class: `PART_Container` (`.searchbar-input-container`),
`PART_NativeInput` (`.searchbar-input`, a `TextBox`), `PART_CancelButton` (`.searchbar-cancel-button`) with `PART_CancelIcon`,
`PART_SearchIcon` (`.searchbar-search-icon`), `PART_ClearButton` (`.searchbar-clear-button`) with `PART_ClearIcon`.

### CSS variables (11)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Searchbar.Background` | brush | `--ion-background-color`, white; a known `Color`'s base beats it |
| [x] | `--border-radius` | `Avonic.Searchbar.BorderRadius` | `CornerRadius` | 2 |
| [x] | `--box-shadow` | `Avonic.Searchbar.BoxShadow` | `BoxShadows` | `0 2 2 0 .14, 0 3 1 -2 .2, 0 1 5 0 .12` black |
| [x] | `--cancel-button-color` | `Avonic.Searchbar.CancelButtonColor` | brush | text step 100 (`#1a1a1a`); a known `Color`'s contrast beats it |
| [x] | `--clear-button-color` | `Avonic.Searchbar.ClearButtonColor` | brush | `CssUnset`: `initial` makes `color` inherit, measured the host's `#262626` |
| [x] | `--color` | `Avonic.Searchbar.Color` | brush | text step 150 (`#262626`); a known `Color`'s contrast beats it |
| [x] | `--icon-color` | `Avonic.Searchbar.IconColor` | brush | text step 400 (`#666`); a known `Color`'s contrast beats it |
| [x] | `--placeholder-color` | `Avonic.Searchbar.PlaceholderColor` | brush | `CssUnset`: the input's colour, measured |
| [x] | `--placeholder-font-style` | `Avonic.Searchbar.PlaceholderFontStyle` | `FontStyle` | `CssUnset`: inherited |
| [x] | `--placeholder-font-weight` | `Avonic.Searchbar.PlaceholderFontWeight` | `FontWeight` | `CssUnset`: inherited |
| [x] | `--placeholder-opacity` | `Avonic.Searchbar.PlaceholderOpacity` | `double` | `CssUnset`, falling back to `--ion-placeholder-opacity`, 0.6 |

md's literals with no variable are in the theme: the host's 8 px padding (7 and 3 in a toolbar), the input's 55 px and 6 px
padding and 30 px line, 16 px text, the search icon's 21 px at 16 and 11, the cancel button's 24 px icon 9 px from the start,
the clear button's 22 px icon 13 px from the end, the clear button's 0.5 opacity when focused, and 0.4 opacity when disabled.

## Behaviours

From `searchbar.js` and `searchbar.md.css`, each checked in Chromium. Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | Typing sets `Value` and raises `IonInput` with each edit, after `IonFocus` | `onInput`; measured `type ab` |
| 2 | Enter raises `IonChange` once the user has edited and the value differs from the last commit (or the focus); again, nothing. A value set in code during the edit is what's committed | the native `change`, `onChange`; measured `enter`, `D` |
| 3 | Blur: after the user's edit, `IonChange` then `IonBlur` (the native `change` comes first). A value changed in code while focused: `IonBlur`, then `IonChange` (Ionic's own `onBlur` check). No change: only `IonFocus`, `IonBlur`. An edit undone: nothing | `onChange`, `onBlur`, `focusedValue`; measured `A`, `B`, `C`, `type c, blur` |
| 4 | A value set in code raises nothing, and shows; one set in an `IonInput` handler replaces what was typed | `valueChanged` |
| 5 | `Debounce`: `IonInput` waits that long after the last edit, once, with the last value | `debounceEvent`; measured `debounce` |
| 6 | The clear button shows with a value and `ShowClearButton` `Always`, or `Focus` while focused; never with `Never` | `searchbar-has-value.searchbar-should-show-clear` |
| 7 | A click on the clear button raises `IonClear` at once; 64 ms later, if there's a value, it's `""` with an `IonInput`, and then the input is focused (`IonFocus`) if it wasn't, so that its blur raises `IonChange` `""` after `IonBlur`. A press on it doesn't take focus. A second click within 64 ms restarts the wait | `onClearInput(true)`, `onPointerDown`; measured `clear click (focused)`, `(unfocused)` |
| 8 | The clear button is a tab stop: Enter on it clears and focuses the input; while it has focus it's at 0.5 | native `<button>`, `:focus`; measured `tab2`, `enter on clear` |
| 9 | The cancel button exists unless `ShowCancelButton` is `Never`; md shows it while focused or with `Always`, and hides the search icon then | `render`, md.css; measured `focused c`, `b` |
| 10 | A press on the cancel button (not its release) raises `IonCancel` and `IonClear` at once; 64 ms later the value is `""` with an `IonInput`, an `IonChange` if there was a value and the input wasn't focused, and the input loses focus (`IonChange` first if the user had edited, then `IonBlur`). A press doesn't take focus; Enter on the button does nothing, as it only listens to `mousedown` and `touchstart` | `onCancelSearchbar`; measured `cancel mousedown *`, `enter on it` |
| 11 | Escape in a `Search` field with a value clears it with an `IonInput` `""`, which a blur then commits; not in a `Text` field | Chromium's `<input type=search>`; measured `E`, `F` |
| 12 | `SetFocus()` focuses the native input; `GetInputElement()` returns it | `setFocus`, `getInputElement` |
| 13 | `ionStyle` `{ searchbar: true }` when connected, which an item takes as `item-searchbar`; an item counts it as an input (`item-multiple-inputs` beside another), and a tap on the item's padding doesn't reach it | `emitStyle`, `item.js` `totalNestedInputs`, `getFirstInteractive` |
| 14 | md layout: 8 px padding (7 × 3 in a toolbar); the input 42 px high with 55 px at each side; the search icon 21 px at 16, 11; the cancel button 24 × 42 at 9 with a 24 px icon; the clear button 22 × 42 at 13 from the end. RTL mirrors all of it | md.css; measured `boxes` |
| 15 | md colours: text step 150, the search icon step 400, the cancel button step 100, the clear button the host's colour, the placeholder the input's colour at 0.6; the input white with 2 px corners and md's shadow. With a `Color`: the input its base, the rest its contrast | md.css; measured `boxes` `p` |
| 16 | Disabled: 0.4 opacity over the whole, nothing focusable | `searchbar-disabled`; measured `dis` |
| 17 | The type and hints reach the platform: `Password` masks and is sensitive; `Type`, `Inputmode`, `Enterkeyhint`, `Autocapitalize`, `Autocorrect` set `TextInputOptions`; `Maxlength` stops typing | `<input>`'s attributes |
| 18 | Automation: the host is a search landmark, the input is named "search text", the clear button "reset", the cancel button `CancelButtonText`; the icons are hidden from it | `role="search"`, `aria-label`s, `aria-hidden` |
| 19 | `Animated` sets `:animated` (with `AvonicConfig.Animated`) and `:no-animate` for its first 300 ms; `:left-aligned` follows `!Animated`, a value or focus at load, focus and blur. md has no rule on them | `render`, `componentDidLoad`, `positionElements` |
| 20 | Leaving the tree drops a pending clear, as `disconnectedCallback` does | `disconnectedCallback` |
| 21 | The icons: `SearchIcon`, `ClearIcon`, `CancelButtonIcon`, each a name or a source, with md's defaults | `render` |

### Not reproduced

- **`dir` on the searchbar itself**: Ionic's `inheritAttributes` moves it to the native input, so only the text turns while the
  icons stay. Avalonia's `FlowDirection` on the host mirrors everything, as `dir` on any ancestor does in Ionic, which is how an
  app sets it. `lang` likewise has no Avalonia counterpart on a text box.
- **iOS's placeholder centring and sliding cancel button** (`positionPlaceholder`, `positionCancelButton`): iOS only.
- **The cancel button on a right-click**: Ionic's `mousedown` handler takes any button; here too, as `PointerPressed` does.

## Design

`AvonicSearchbar : TemplatedControl, IItemControl` (`ItemControlType.Searchbar`), not focusable itself: the native input is the
tab stop. No slots. The native input is a `TextBox` with its own minimal `ControlTheme`, functional in Components, as
`ion-input`'s is. `Value` and the text box's `Text` are kept in step with a guard, so only the user's edits raise `IonInput`.

```
Panel PART_Container                 .searchbar-input-container, inside the host's padding
├─ TextBox PART_NativeInput           .searchbar-input: background, corners and shadow, 55 px at each side
├─ Border PART_CancelButton           .searchbar-cancel-button, at the start, full height
│  └─ AvonicIcon PART_CancelIcon
├─ AvonicIcon PART_SearchIcon         .searchbar-search-icon, no hit test
└─ Border PART_ClearButton            .searchbar-clear-button, at the end, full height
   └─ AvonicIcon PART_ClearIcon
```

Host classes become pseudoclasses: `:has-value`, `:has-focus`, `:should-show-clear`, `:should-show-cancel`, `:animated`,
`:no-animate`, `:left-aligned` and `:searchbar-disabled` (`disabled`; Avalonia's own `:disabled` also follows ancestors).
md.css shows the cancel button for `searchbar-has-focus` or `searchbar-should-show-cancel`, but a rendered cancel button with
focus is always `should-show-cancel` too (`focus` and `always` both show it then), so the theme has the one rule; the deliberate
breakage proved the other did nothing. The cancel button's press is handled, which keeps focus where it is, as `preventDefault`.
`:host-context(ion-toolbar)` is a descendant rule in `Styles/Searchbar.axaml`, by the mapping rule.

The clear button is a `Border` with `NativeButton` (a click on release, Enter and Space), and a press on it keeps focus where it
is, through `KeepFocus`, as its `pointerdown` handler's `preventDefault` does. The cancel button acts on `PointerPressed`,
handled, which moves no focus, and has no key action. `onClearInput`'s 64 ms is `AvonicConfig.Schedule`. The native `change` event is kept apart
from Ionic's `onBlur` check, as they come in a different order: a flag for the user's edits and the text at the last commit.
Enter's change and Escape's clear are default actions: they run after the key's handlers, and not if one handled it.

## Dependencies

`ion-icon` (#3) and `ion-input` (#24, for the shape of the native input and `KeepFocus`): merged. `ion-item` (#6) and
`ion-toolbar` (#16) for the context rules: merged.
