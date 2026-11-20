# ion-datetime-button

Status: done · [#79](https://github.com/wdcossey/Avonic/issues/79)

`AvonicDatetimeButton` is Ionic's compact way to show a datetime on a phone, md: the value of an `AvonicDatetime`
([#46](../ion-datetime/README.md)) on one or two buttons, as the datetime's locale writes it. A tap presents the `AvonicModal` or
`AvonicPopover` the datetime is in, showing only the half that was tapped: the calendar from the date button, the time from the time
button. The active button is in the accent while the datetime shows. The design and the evidence are in [PLAN.md](PLAN.md), and the
comparison with real Ionic is in [COMPARISON.md](COMPARISON.md). It follows Ionic 9.0.11.

## Usage

```xml
<!-- The datetime in a modal that keeps its contents mounted, as Ionic's docs show it; the button names it. -->
<c:AvonicDatetimeButton Datetime="Starts" />
<c:AvonicModal KeepContentsMounted="True">
    <c:AvonicDatetime Name="Starts" Value="2023-06-15T13:47:00" ShowDefaultButtons="True" />
</c:AvonicModal>

<!-- In a popover, placed under the button tapped; a colour for the active button; text of its own. -->
<c:AvonicDatetimeButton Datetime="Day" Color="danger">
    <c:AvonicDatetimeButton.DateTargetContent>
        <TextBlock Text="Pick a day" />
    </c:AvonicDatetimeButton.DateTargetContent>
</c:AvonicDatetimeButton>
<c:AvonicPopover KeepContentsMounted="True">
    <c:AvonicDatetime Name="Day" Presentation="Date" />
</c:AvonicPopover>
```

`Datetime` is the datetime's `Name`, looked up in the name scopes around the button, nearest first, when the button is first
attached, as Ionic reads `datetime` once, at load. Once found, a change does nothing, as in Ionic; a name that comes after an attach
that found none (a binding) is looked up then.

## Checklist

### Props

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | The active button's text |
| [x] | `datetime` | `Datetime` | The datetime's `Name`; read once, at the first attach |
| [x] | `disabled` | `IsEnabled` | Both buttons, at 0.4 |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Events, methods

None, as in Ionic.

### Slots

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `date-target` | `DateTargetContent` (+ `DateTargetContentTemplate`), in place of the date's text, while there's a date button |
| [x] | `time-target` | `TimeTargetContent` (+ `TimeTargetContentTemplate`), in place of the time's text, while there's a time button |

### Parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `native` (both buttons, `#date-button` and `#time-button`) | `PART_DateButton` and `PART_TimeButton`, each a `DatetimeButtonNative` with the style class `native` |

### CSS custom properties

None in Ionic. The md literals are keys: `Avonic.DatetimeButton.Md.Background`/`BackgroundBrush` (`#edeef0`; dark: step 300),
`Avonic.DatetimeButton.Md.DisabledMask` (0.4), `Avonic.DatetimeButton.FocusRingColor`/`Brush` (Chromium's `#E59700`), and
`Avonic.DatetimeButton.Overlay`, the dictionary of `core.css`'s `.ion-datetime-button-overlay` sizes (`Avonic.Modal.Width`,
`Avonic.Modal.Height`, `Avonic.Popover.Width`, `Avonic.Popover.Height` = `fit-content`).

## Behaviours

All measured in Chromium or read from the `.js`; [COMPARISON.md](COMPARISON.md) has each with its case.

- **The text** is the datetime's, through its own locale helpers: `Jun 15, 2023` and `1:47 PM` for `DateTime` and `TimeDate`, one
  button `Jun 15, 2023 1:47 PM` with `PreferWheel`, `June 2023` for `MonthYear`, `June`, `2023`; the datetime's `FormatOptions`,
  `HourCycle` and `Locale`. No value is the datetime's default (today, snapped to its value lists). With `Multiple`, `3 days`, or the
  datetime's `TitleSelectedDatesFormatter`. A value that doesn't parse leaves the text as it was.
- **It follows the datetime's value**: a confirmed choice, a value set in code. Not a selection waiting for Done, nor a change of the
  locale or presentation alone, which the next value then shows. The datetime's presentation is read once, at load.
- **A tap on the date button** sets a `DateTime` or `TimeDate` datetime's presentation to `Date` (not with `PreferWheel`), and the
  time button to `Time`; then the modal is presented, or the popover, under the button tapped, a frame later, once the datetime has
  drawn its new presentation. In the page, only the presentation changes.
- **Active**: while the datetime shows (at least 1% of it, in view, not hidden), the last button tapped, or the presentation's
  (`time` for `Time` and `TimeDate`), is in the primary colour, or the `Color`'s, eased over 150 ms; both buttons are expanded to
  automation.
- **The overlay** gets the style class `ion-datetime-button-overlay`, and is the datetime's size: a calendar 320 wide.
- **The buttons** are native buttons: a ripple, Tab from the date to the time, Enter and Space, Chromium's focus ring on a Tab.

Deliberately not as Ionic ([ADR 0005](../../decisions/0005-dont-copy-an-ionic-bug.md), `docs/infra/ionic-issues.md`):

- **`Month` and `Year` are formatted by `FormatOptions.Date`**; Ionic reads `formatOptions.time` there (a month picker's button shows
  `01:47 PM`).

## Extensions

None.

## Known gaps

- **What's in view is checked on each layout pass**, as Avalonia has no `IntersectionObserver`, and when the overlay's present and
  dismiss end: a sheet sliding up is active once it's up (Chromium: as it enters), and a datetime moved into or out of view by a
  render transform alone isn't seen until the next layout. Ancestors that clip by default in Avalonia (`TemplatedControl`) clip
  what counts.
- **The text is the datetime's locale text**, with [its gaps](../ion-datetime/README.md#known-gaps): measured against Chromium on
  Windows; Linux's ICU gives some locales' AM and PM in English (`PM 1:47` for Korean, which CI showed).
- **After Escape, the button has its focus again without Chromium's ring**: the overlays give focus back without `:focus-visible`.
- **The overlay's popover is in the window's overlay layer**, as every Avonic popover: in a real popup window (another `TopLevel`) it
  can't be tested headless.
- **Not tried in a real window** yet: `tools/real-window/drive.ps1` has its section.
