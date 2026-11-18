# Accessibility

What Avalonia offers assistive technology, and how Avonic maps Ionic's ARIA onto it. Recorded 18 Nov 2026 from Avalonia's
docs ([Accessibility](https://docs.avaloniaui.net/docs/app-development/accessibility), updated 2 Apr 2026, read through the
avalonia-docs MCP server) and Ionic 9.0.11's `.js`. Not yet a priority: this is so that new controls get it right as they go.

## How Avalonia does it

Each control has an **automation peer** (`OnCreateAutomationPeer`), as in WPF and UWP, which the platform layer translates
for the OS. A peer gives the control type, name, help text, bounds and state, and implements provider interfaces for what
the control does (`IInvokeProvider`, `IToggleProvider`, `IValueProvider`, `IRangeValueProvider`, `IExpandCollapseProvider`,
`ISelectionProvider`, `IScrollProvider`). An app or a theme sets `AutomationProperties`: `Name`, `HelpText`, `LabeledBy`,
`AutomationId`, `AcceleratorKey`, `AccessKey`, `ControlTypeOverride`, `LandmarkType`, `HeadingLevel`, `LiveSetting`,
`ItemStatus`, `ItemType`, `AccessibilityView`, `IsRequiredForForm`. Validation errors from Avalonia's data validation reach
the peer as help text by themselves.

| Platform | API | Avalonia's docs say |
|---|---|---|
| Windows | UI Automation | Full support. Narrator finds landmarks only with `AccessibilityView` at least `Control` |
| macOS | NSAccessibility | Full support. Heading levels 0-6 (Windows 1-9: use 1-6) |
| Linux | AT-SPI2 over D-Bus | Full support, when AT-SPI2 is running; Orca reads standard controls; Accerciser to inspect |
| iOS | UIAccessibility | Supported |
| Android | `AccessibilityNodeInfo` | Supported (TalkBack). The docs give no detail beyond that |
| Browser | ARIA attributes | Partial |

Not tried here: TalkBack on the Pixel, Orca on Linux, VoiceOver. Only Windows' UI Automation is exercised, by
`tools/real-window/drive.ps1`, which finds and reads every control through it: a real-window check is also a check of
what a screen reader gets. Avalonia's own end-to-end tests use Appium (Windows, macOS); its docs say Appium has no stable
Linux desktop driver.

## Ionic's ARIA in Avonic

| Ionic | Avalonia | In Avonic |
|---|---|---|
| `role` | the peer's control type, or `ControlTypeOverride` | per control (CLAUDE.md's mapping rules) |
| `aria-label`, `aria-labelledby` | `Name`, `LabeledBy`, the peer's `GetNameCore` | overlays name themselves from their header (`AvonicActionSheet`, `AvonicAlert`) |
| `aria-describedby` | `HelpText`, the peer's `GetHelpTextCore` | `AvonicCheckbox`; `AvonicInput`, `AvonicTextarea` on the native field |
| `aria-required` | `IsRequiredForForm` | checkbox, input, textarea |
| `aria-checked="mixed"` | `IToggleProvider`, indeterminate | `AvonicCheckbox` |
| `role="heading"`, `aria-level` | `HeadingLevel` | `AvonicCardTitle` |
| `role="banner"`, `contentinfo`, `main`, `search`, `navigation` | `LandmarkType` | `AvonicHeader` (banner), `AvonicFooter` (contentinfo), `AvonicContent` (main), `AvonicSearchbar` (search); `ion-menu`'s `navigation` waits for #44 |
| `aria-live="polite"`, `role="alert"` | `LiveSetting` `Polite`, `Assertive` | the helper and error text of `AvonicCheckbox`, `AvonicRadio`, `AvonicRadioGroup`. **Not yet** on `AvonicInput`, `AvonicTextarea`, `AvonicToggle`, which Ionic gives the same (#236) |
| `role="status"` with `aria-live="polite"` | `LiveSetting` `Polite` | `ion-toast` (#38), when it's built |
| `role="radiogroup"` | no control type | recorded as a gap in the alert and the action sheet |
| `aria-activedescendant` | no counterpart | recorded as a gap in the alert |

## For a new control

- Read the `.js` for `role`, `aria-*` and `tabindex`, and map each by the table above; a gap goes in COMPARISON with why.
- A landmark or live region set by the control is a default (`OverrideDefaultValue`, or style priority), so an app's own
  value wins.
- A real-window check that finds an element by its name also proves the name a screen reader reads.
