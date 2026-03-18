# AvonicSearchbar

A search input with an integrated search icon, clear button, and optional cancel button.

**Ionic reference:** [`ion-searchbar`](https://ionicframework.com/docs/api/searchbar)

---

## Usage

```xml
<!-- Basic -->
<avonic:AvonicSearchbar Placeholder="Search…" TextChanged="OnSearch" />

<!-- With cancel button -->
<avonic:AvonicSearchbar Placeholder="Search…"
                        ShowCancelButton="True"
                        CancelButtonText="Cancel"
                        Cancelled="OnCancelled" />

<!-- Two-way text binding -->
<avonic:AvonicSearchbar Text="{Binding SearchQuery}" Debounce="300" />
```

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Text` | `string?` | `null` | Current search text (two-way bindable) |
| `Placeholder` | `string?` | `"Search"` | Placeholder text |
| `ShowCancelButton` | `bool` | `false` | Shows a "Cancel" button to the right |
| `CancelButtonText` | `string` | `"Cancel"` | Label for the cancel button |
| `Debounce` | `int` | `250` | Milliseconds to wait before raising `TextChanged` |
| `Animated` | `bool` | `false` | Enables animated search icon slide-in |
| `IsEnabled` | `bool` | `true` | Disables interaction when `false` |

## Events

| Event | Description |
|---|---|
| `TextChanged` | Raised after the debounce period with `SearchTextChangedEventArgs.Text` |
| `Cleared` | Raised when the ✕ clear button is tapped |
| `Cancelled` | Raised when the cancel button is tapped; focus is moved away |
| `SearchFocused` | Raised when the inner `TextBox` receives focus |
| `SearchBlurred` | Raised when the inner `TextBox` loses focus |

## Pseudoclasses

| Pseudoclass | When active |
|---|---|
| `:has-value` | `Text` is non-empty (shows the clear button) |
| `:show-cancel` | `ShowCancelButton=true` |
| `:focused` | The inner text input has keyboard focus |
| `:disabled` | `IsEnabled=false` |

## Template parts

| Name | Type | Purpose |
|---|---|---|
| `PART_Input` | `TextBox` | The inner search text box |
| `PART_ClearButton` | `Button` | Clears the text |
| `PART_CancelButton` | `Button` | Triggers cancel and removes focus |

## Tokens

| Key | Default | Purpose |
|---|---|---|
| `Avonic.Searchbar.MinHeight` | `44` | Minimum height of the search field |
| `Avonic.MutedBrush` | `#F4F4F5` | Background of the search pill |
| `Avonic.CornerRadius.Pill` | `999` | Fully rounded search container |
