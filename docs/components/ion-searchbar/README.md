# ion-searchbar

Status: done · [#26](https://github.com/wdcossey/Avonic/issues/26)

`AvonicSearchbar` is a search field: the native input, a search icon at its start, a clear button at its end and, in md, a cancel
button (a back arrow) that takes the search icon's place. The design and evidence are in [PLAN.md](PLAN.md), and the comparison
with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicSearchbar Placeholder="Search" Debounce="300" IonInput="OnSearch" />

<c:AvonicToolbar>
    <c:AvonicSearchbar ShowCancelButton="Focus" IonCancel="OnCancel" />
</c:AvonicToolbar>

<c:AvonicSearchbar Color="primary" ShowClearButton="Focus" />
```

## API

### Properties

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `animated` | `Animated` (`bool`): the classes `:animated`, `:no-animate`, `:left-aligned`, which md has no rule on |
| [x] | `autocapitalize` | `Autocapitalize` (`string`, `"off"`) |
| [ ] | `autocomplete` | — (no autofill in Avalonia) |
| [x] | `autocorrect` | `Autocorrect` (`SearchbarAutocorrect`) |
| [x] | `cancelButtonIcon` | `CancelButtonIcon` (`string?`; unset, `AvonicConfig.BackButtonIcon` when made, as Ionic's `config.get('backButtonIcon')`; null is `arrow-back-sharp`, not mirrored in RTL) |
| [x] | `cancelButtonText` | `CancelButtonText` (`string?`, `"Cancel"`): the cancel button's automation name |
| [x] | `clearIcon` | `ClearIcon` (`string?`; null is `close-sharp`) |
| [x] | `color` | `Color` |
| [x] | `debounce` | `Debounce` (`int?`, ms) |
| [x] | `disabled` | `IsEnabled` |
| [x] | `enterkeyhint` | `Enterkeyhint` (`SearchbarEnterkeyhint?`) |
| [x] | `inputmode` | `Inputmode` (`SearchbarInputmode?`) |
| [x] | `maxlength` | `Maxlength` (`int?`) |
| [ ] | `minlength` | — (HTML form validation) |
| [ ] | `mode` | — (ADR 0001) |
| [ ] | `name` | — (HTML form) |
| [x] | `placeholder` | `Placeholder` (`string?`, `"Search"`) |
| [x] | `searchIcon` | `SearchIcon` (`string?`; null is `search-sharp`) |
| [x] | `showCancelButton` | `ShowCancelButton` (`SearchbarShowCancelButton`, `Never`) |
| [x] | `showClearButton` | `ShowClearButton` (`SearchbarShowClearButton`, `Always`) |
| [ ] | `spellcheck` | — (no spell checker in Avalonia's `TextBox`) |
| [x] | `type` | `Type` (`SearchbarType`, `Search`) |
| [x] | `value` | `Value` (`string?`, `""`, two-way) |

### Events and methods

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionInput` | `IonInput` (`SearchbarInputEventArgs.Value`) |
| [x] | `ionChange` | `IonChange` (`SearchbarChangeEventArgs.Value`) |
| [x] | `ionFocus`, `ionBlur` | `IonFocus`, `IonBlur` |
| [x] | `ionClear` | `IonClear` |
| [x] | `ionCancel` | `IonCancel` |
| [x] | `setFocus()` | `SetFocus()` |
| [x] | `getInputElement()` | `GetInputElement()`: the `TextBox` |

No slots and no parts in Ionic. Template parts: `PART_Container`, `PART_NativeInput`, `PART_CancelButton`, `PART_CancelIcon`,
`PART_SearchIcon`, `PART_ClearButton`, `PART_ClearIcon`.

### Resource keys

Each is in `Tokens/Searchbar.axaml`.

| ✓ | Ionic | Key |
|---|---|---|
| [x] | `--background` | `Avonic.Searchbar.Background` |
| [x] | `--border-radius` | `Avonic.Searchbar.BorderRadius` |
| [x] | `--box-shadow` | `Avonic.Searchbar.BoxShadow`, CSS through `{a:CssShadow}` |
| [x] | `--cancel-button-color` | `Avonic.Searchbar.CancelButtonColor` |
| [x] | `--clear-button-color` | `Avonic.Searchbar.ClearButtonColor` |
| [x] | `--color` | `Avonic.Searchbar.Color` |
| [x] | `--icon-color` | `Avonic.Searchbar.IconColor` |
| [x] | `--placeholder-color` | `Avonic.Searchbar.PlaceholderColor` |
| [x] | `--placeholder-font-style` | `Avonic.Searchbar.PlaceholderFontStyle` |
| [x] | `--placeholder-font-weight` | `Avonic.Searchbar.PlaceholderFontWeight` |
| [x] | `--placeholder-opacity` | `Avonic.Searchbar.PlaceholderOpacity` |

## Behaviours

Each has a test in `AvonicSearchbarTests`, named after its number in [PLAN.md](PLAN.md#behaviours).

- Typing raises `IonInput` with each value; Enter and blur commit `IonChange` once after an edit, in Chromium's order: the native
  change before `IonBlur`, Ionic's own check (a value set in code while focused) after it.
- The clear button raises `IonClear` at once and clears 64 ms later with an `IonInput`, focusing the field; a press on it never
  moves focus. It's a tab stop, at 0.5 while focused.
- The cancel button acts on the press, not the release: `IonCancel`, `IonClear`, then the clear, a commit and the blur. Enter on it
  does nothing, as in Ionic.
- Escape clears a `Search` field, as Chromium's search input does, unless a key handler handled it.
- `Debounce`, `Maxlength`, the type and the keyboard hints, as `ion-input`.
- md's look, measured: the white card with md's shadow, the icons, the toolbar's tighter padding, `Color`, disabled at 0.4, RTL.
- An item takes it as `item-searchbar` and counts it as an input; the host is a search landmark.
- In a flex row it shrinks to its padding and the input's, 126 at md, whatever its value, as the `<input>` is `width: 100%`:
  measured in Chromium, its `min-content` is 126 with or without a value or the cancel button, and beside text that doesn't wrap in
  a row 358 wide it's 126. `MinContent` takes its field as it does the input's and the textarea's. Compared headless in
  [compare/155-flex-row.png](compare/155-flex-row.png), Ionic on the left, from `AvonicSearchbarCaptureTests.Flex_Row_Page` and
  `node tools/ionic-ref/render.js ion-searchbar --bare --html '<div style="padding:16px;font-size:16px"><div style="display:flex;width:358px"><div style="flex:none;white-space:nowrap">Text that doesn't wrap</div><ion-searchbar value="A long search value that is wider than the box"></ion-searchbar></div></div>'`.
  That frame shows a gap too: a value longer than the field shows its end, where Chromium's unfocused input shows its start.

## Extensions

Not in Ionic's API, but needed by the theme: the converters `FieldConverters.Or` (md's default icons) and
`FieldConverters.FlipUnlessDefault` (the default cancel arrow isn't mirrored). The native field is shared with the input and the
textarea (`Controls/TextField/`).

## Known gaps

See [COMPARISON.md](COMPARISON.md#deliberate-differences-and-gaps): `dir` set on the searchbar itself mirrors everything; the typed text sits 0.5 px lower; only a tunnel key handler can stop Escape's clear; a disabled searchbar's buttons can't be reached, where Ionic's still can by Tab; the buttons have no automation invoke. Tried in a real window by `tools/real-window`; not with a soft
keyboard, or on a device.

Measured in Chromium in #155, and not done: the host's `width: 100%` as a flex basis, and the `<input>`'s width from its `size`
of 20 when nothing stretches it. In an item's start or end slot Chromium's searchbar is 310 wide at 390, Avonic's 176; shrunk to
fit (`display: inline-block`) it's 325, its padding, the input's and 199 for 20 characters, where Avonic's is 176; beside a label
in an item's default slot, the label keeps its narrowest, 53, and the searchbar the rest, 273. Avonic's wants its field's own
width, the text's and the padding's, and takes no more unless something stretches it.
