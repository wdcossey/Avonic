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
`ItemStatus`, `ItemType`, `AccessibilityView`, `IsRequiredForForm`. Avalonia's docs say errors from its data validation reach
the peer as help text by themselves; not tried here, so a control with its own error text still gives it to its peer.

| Platform | API | Avalonia's docs say |
|---|---|---|
| Windows | UI Automation | Full support. Narrator finds landmarks only with `AccessibilityView` at least `Control` |
| macOS | NSAccessibility | Full support. Heading levels 0-6 (Windows 1-9: use 1-6) |
| Linux | AT-SPI2 over D-Bus | Full support, when AT-SPI2 is running; Orca reads standard controls; Accerciser to inspect |
| iOS | UIAccessibility | Supported |
| Android | `AccessibilityNodeInfo` | Supported (TalkBack). The docs give no detail beyond that |
| Browser | ARIA attributes | Partial |

On Windows, a UIA search that is under way when a subtree it has reached is hidden or removed fails with E_FAIL, as Avalonia's
provider gives that subtree's elements no fragment root: an animated change (a nav's transition, an overlay's dismiss) can meet
a screen reader's search (`docs/infra/avalonia-issues.md`, #254).

Not tried here: TalkBack on the Pixel, Orca on Linux, VoiceOver. Only Windows' UI Automation is exercised, by
`tools/real-window/drive.ps1`, which finds and reads every control through it: a real-window check is also a check of
what a screen reader gets. Avalonia's own end-to-end tests use Appium (Windows, macOS); its docs say Appium has no stable
Linux desktop driver.

## Ionic's ARIA in Avonic

| Ionic | Avalonia | In Avonic |
|---|---|---|
| `role` | the peer's control type, or `ControlTypeOverride` | per control (CLAUDE.md's mapping rules) |
| `aria-label`, `aria-labelledby` | `Name`, `LabeledBy`, the peer's `GetNameCore` | an overlay names itself from its header (`AvonicAlert`, `AvonicActionSheet`) |
| `aria-describedby` | `HelpText`, the peer's `GetHelpTextCore` | `AvonicCheckbox`, `AvonicToggle`, `AvonicRadioGroup`; `AvonicInput`, `AvonicTextarea` on the native field |
| `aria-required` | `IsRequiredForForm` | checkbox, input, textarea. **Not** the toggle, which has no `Required` (#238) |
| `aria-checked="mixed"` | `IToggleProvider`, indeterminate | `AvonicCheckbox` |
| `role="heading"`, `aria-level` | `HeadingLevel` | `AvonicCardTitle` (2), `AvonicCardSubtitle` (3) |
| `role="banner"`, `contentinfo`, `main`, `search`, `navigation` | `LandmarkType` | `AvonicHeader` (banner), `AvonicFooter` (contentinfo), `AvonicContent` (main), `AvonicSearchbar` (search), `AvonicMenu` (navigation, named `menu` unless the app names it). **Gap:** Ionic gives content `main` only outside a menu, popover or modal, and a header `banner` only outside a menu; Avonic's content is `main` in a modal or popover too (#237) |
| `aria-live="polite"`, `role="alert"` | `LiveSetting` `Polite`, `Assertive` | the helper and error text of `AvonicCheckbox` and `AvonicRadioGroup` (`ion-radio` has none), though at a priority an app's style can't beat. **Not yet** on `AvonicInput`, `AvonicTextarea`, `AvonicToggle`, which Ionic gives the same (#236, both) |
| `role="status"` with `aria-live="polite"` | `LiveSetting` `Polite` | `AvonicToast`'s content (`ToastContent`, #38), a default an app's value beats; its header and message hidden from automation (`AccessibilityView` `Raw`) until the enter has finished, as Ionic's `aria-hidden` copies. `aria-atomic` has no counterpart |
| `role="radiogroup"` | no control type | recorded as a gap in the alert and the action sheet |
| `aria-activedescendant` | no counterpart | recorded as a gap in the alert |
| `aria-expanded` | `IExpandCollapseProvider` on the peer | `AvonicSelect`, expanded while its interface is open; an `AvonicItem` that is an accordion's header |
| `aria-haspopup`, `aria-invalid` | no counterpart | recorded as a gap in `AvonicSelect` |

## For a new control

- Read the `.js` for `role`, `aria-*` and `tabindex`, and map each by the table above; a gap goes in COMPARISON with why.
- A landmark or live region set by the control is a default (`OverrideDefaultValue`, or style priority), so an app's own
  value wins. The checkbox's and radio group's live regions don't yet (#236).
- A real-window check that finds an element by its name also proves the name a screen reader reads.
