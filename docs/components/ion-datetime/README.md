# ion-datetime

Status: done · [#46](https://github.com/wdcossey/Avonic/issues/46)

`AvonicDatetime` is Ionic's date and time picker, md: a calendar grid that swipes a month at a time, with a month/year wheel and a
time button that opens a popover of wheels; or wheels alone (`PreferWheel`, and the `Time`, `Month`, `MonthYear` and `Year`
presentations). It picks one date or several (`Multiple`), within `Min` and `Max`, the value lists and `IsDateEnabled`, with
highlighted dates, a header, and default Cancel, Clear and Done buttons that confirm or cancel it, and close the `AvonicModal` or
`AvonicPopover` around it. Its wheels are `AvonicPicker`s ([#37](../ion-picker/README.md)), its time popover an `AvonicPopover`
([#43](../ion-popover/README.md)), its buttons `AvonicButton`s. The design and the evidence are in [PLAN.md](PLAN.md), and the
comparison with real Ionic is in [COMPARISON.md](COMPARISON.md). It follows Ionic 9.0.11. `ion-datetime-button` is
[#79](https://github.com/wdcossey/Avonic/issues/79).

## Usage

```xml
<!-- A calendar and a time; a choice is the value at once. -->
<c:AvonicDatetime Value="{Binding When}" IonChange="OnWhenChanged" />

<!-- A date, waiting for Done, with a header and Clear. -->
<c:AvonicDatetime Presentation="Date" ShowDefaultTitle="True" ShowDefaultButtons="True" ShowClearButton="True"
                  Min="2023-01-01" Max="2023-12-31" MinuteValues="0,15,30,45" Color="danger" />

<!-- Wheels, in Japanese (the year first). -->
<c:AvonicDatetime Presentation="Date" PreferWheel="True" Locale="ja-JP" />

<!-- In a popover, whose Done closes it with the role "datetime-confirm". -->
<c:AvonicPopover Trigger="Open" KeepContentsMounted="True">
    <c:AvonicDatetime ShowDefaultButtons="True" />
</c:AvonicPopover>
```

```csharp
datetime.IsDateEnabled = iso => DateTime.Parse(iso, CultureInfo.InvariantCulture).DayOfWeek is not DayOfWeek.Sunday;
datetime.HighlightedDates = new DatetimeHighlight[] { new() { Date = "2023-06-05", TextColor = "#800080", BackgroundColor = "#ffc0cb" } };
datetime.Value = new[] { "2023-06-05", "2023-06-09" };   // with Multiple
```

**Values are ISO 8601 strings, as Ionic's**: `2023-06-15T13:47:00`, `2023-06-15`, or `13:47`. A zone (`Z`, `+05:30`) is read and
dropped: the wall clock as written is the value, and what the datetime writes has no zone. A list of strings with `Multiple`.

## Checklist

### Props

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `cancelText`, `clearText`, `doneText` | `CancelText`, `ClearText`, `DoneText` | |
| [x] | `color` | `Color` | The header, today, the selected day, the wheels, the buttons |
| [x] | `dayValues`, `monthValues`, `yearValues`, `hourValues`, `minuteValues` | `DayValues` … (`DatetimeValueList?`) | A number, numbers, or text (`"0,15,30,45"`) |
| [x] | `disabled` | `IsEnabled` | |
| [x] | `firstDayOfWeek` | `FirstDayOfWeek` | 0 is Sunday |
| [x] | `formatOptions` | `FormatOptions` (`DatetimeFormatOptions`) | `Date` and `Time`, each Intl's options as a `DateTimeFormatOptions`; `TimeZone` and `TimeZoneName` are warned about and ignored, as Ionic's |
| [x] | `highlightedDates` | `HighlightedDates` (`DatetimeHighlightedDates?`) | A list of `DatetimeHighlight`, or a `Func<string, DatetimeHighlightStyle?>`; colours and borders as CSS text |
| [x] | `hourCycle` | `HourCycle` (`DatetimeHourCycle?`) | `H11`, `H12`, `H23`, `H24`; null is the locale's |
| [x] | `isDateEnabled` | `IsDateEnabled` (`Func<string, bool>?`) | Given `"2023-06-15"` |
| [x] | `locale` | `Locale` | A culture name; `"default"` is the current culture. See [Locale](#locale) |
| [x] | `max`, `min` | `Max`, `Min` | ISO 8601 of any precision |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `multiple` | `Multiple` | |
| [ ] | `name` | — | Not applicable: an HTML form's hidden input |
| [x] | `preferWheel` | `PreferWheel` | |
| [x] | `presentation` | `Presentation` (`DatetimePresentation`) | `Date`, `DateTime` (default), `Month`, `MonthYear`, `Time`, `TimeDate`, `Year` |
| [x] | `readonly` | `Readonly` | |
| [x] | `showAdjacentDays`, `showClearButton`, `showDefaultButtons`, `showDefaultTimeLabel`, `showDefaultTitle` | the same | |
| [x] | `size` | `Size` (`DatetimeSize`) | `Fixed` (default), `Cover` |
| [x] | `titleSelectedDatesFormatter` | `TitleSelectedDatesFormatter` (`Func<IReadOnlyList<string>, string>?`) | |
| [x] | `value` | `Value` (`DatetimeValue?`) | One string or a list; two-way; its `ToString()` is the strings joined by commas |

### Events and methods

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionChange` | `IonChange` (`DatetimeChangeEventArgs.Value`) | Each confirm, even of the same value; never for a value set in code |
| [x] | `ionCancel` | `IonCancel` | |
| [x] | `ionFocus`, `ionBlur` | `IonFocus`, `IonBlur` | When focus enters and leaves the datetime |
| [x] | `confirm(closeOverlay?)` | `Confirm(bool closeOverlay = false)` | Role `AvonicDatetime.ConfirmRole`, `datetime-confirm` |
| [x] | `cancel(closeOverlay?)` | `Cancel(bool closeOverlay = false)` | Role `AvonicDatetime.CancelRole`, `datetime-cancel` |
| [x] | `reset(startDate?)` | `Reset(string? startDate = null)` | |

For `ion-datetime-button` (#79), Ionic's internal `ionValueChange`, `ionRender` and `getDefaultPart()` are internal here too:
`ValueChange`, `Rendered`, `GetDefaultPart()`.

### Slots

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `title` | `TitleContent` (default "Select Date", upper-cased in md) |
| [x] | `time-label` | `TimeLabelContent` (default "Time") |
| [x] | `buttons` | `ButtonsContent`: in place of the default buttons; a selection then waits for `Confirm()` |

### Parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `calendar-day`, `… active`, `… today`, `… disabled` | `DatetimeCalendarDay`, `:active`, `:today`, `:day-disabled` (also `:adjacent-day`, `:padding`, `:constrained`) |
| [x] | `calendar-days-of-week`, `calendar-header`, `datetime-header`, `datetime-selected-date`, `datetime-title` | `PART_CalendarDaysOfWeek`, `PART_CalendarHeader`, `PART_DatetimeHeader`, `PART_DatetimeSelectedDate`, `PART_DatetimeTitle` |
| [x] | `month-year-button`, `previous-button`, `next-button`, `navigation-button` | `PART_MonthYearButton`, `PART_PreviousButton`, `PART_NextButton`, the class `navigation-button` on both |
| [x] | `time-button`, `time-button active` | `PART_TimeButton`, `:active` |
| [x] | `wheel`, `wheel-item`, `wheel-item active` | The classes `wheel`, `wheel-item` and `active` on the `AvonicPickerColumn`s and options the datetime makes |

### CSS custom properties

| ✓ | Ionic | Key | md |
|---|---|---|---|
| [x] | `--background` | `Avonic.Datetime.Background` | `#ffffff`; dark: `Avonic.BackgroundColorStep100` |
| [x] | `--background-rgb` | `Avonic.Datetime.BackgroundRgb` | unset; only `ios` reads it |
| [x] | `--title-color` | `Avonic.Datetime.TitleColor` | primary's contrast; with `Color`, the colour's |
| [x] | `--wheel-fade-background-rgb`, `--wheel-highlight-background`, `--wheel-highlight-border-radius` | `Avonic.Datetime.WheelFadeBackgroundRgb`, `…WheelHighlightBackground`, `…WheelHighlightBorderRadius` | unset: the picker's own; set, the datetime's wheels take them |

Also `Avonic.Datetime.Md.TimeButtonBackground` (`#edeef0`; dark: step 300) and `Avonic.Datetime.Md.DisabledMask` (0.4).

## Behaviours

All measured in Chromium or read from the `.js`; [COMPARISON.md](COMPARISON.md) has each with its case.

- **A choice is the value at once**, with `IonChange`, unless the default buttons or the buttons slot are there: then it waits for
  Done (`Confirm()`), and Cancel (`Cancel()`) goes back to the value. A second click on the selected day confirms again.
- **A value set in code** shows its month (scrolled to, in the grid) and selects its day, with no `IonChange`.
- **The calendar** swipes a month at a time; the month changes 50 ms after the scroll rests. Previous and next scroll there smoothly
  (at once with `AvonicConfig.Animated` off). A month outside `Min` and `Max` can't be reached. Days outside them, not in the value
  lists, or refused by `IsDateEnabled`, are disabled.
- **The keyboard**: Tab reaches the calendar, which gives focus to the working day; the arrows move a day or a week, Home and End to
  the row's ends, Page Up and Down a month (with Shift a year); Enter and Space select. The arrows aren't mirrored in RTL, as in Ionic.
- **The month/year button** swaps the calendar for a month and a year wheel; a month or year chosen there is a selection.
- **The time button** opens a popover of the hour, minute and AM/PM wheels, centred under it; Escape closes it.
- **The wheels** are Ionic's: month, day and year in the locale's order (year first in Japanese, day first in British English); one
  date column (`Thu, Jun 15`, "Today") for `DateTime` and `TimeDate` with `PreferWheel`; the time columns by the hour cycle, AM/PM
  first in Korean. The hour and minute take typed digits.
- **In a modal or popover**, `Confirm(true)` and `Cancel(true)` close it with their roles, and its next present discards a selection
  left unconfirmed.
- **In an item**, the item has `:item-interactive`, `:item-datetime`, and `:item-interactive-disabled` when disabled.

Deliberately not as Ionic ([ADR 0005](../../decisions/0005-dont-copy-an-ionic-bug.md), `docs/infra/ionic-issues.md`):

- **An adjacent day waits for Done** like any other; Ionic confirms it at once, past the default buttons.
- **Done with nothing chosen on a `Time`, `Month`, `MonthYear` or `Year` wheel writes what the wheel shows**, as Ionic's comment says
  it means; Ionic writes no value there.

- **A date clamped to `Max` stays inside it**: Ionic's `validateParts` can then cut its day to another month's length, past max.
- **A time alone in a calendar presentation** shows today's month with the time, where Ionic draws "Invalid Date"; a negative
  `FirstDayOfWeek` counts back from Sunday, where Ionic's grid breaks.

## Locale

`Locale` is a .NET culture, and the datetime's text is .NET's `CultureInfo`, where Ionic's is the browser's `Intl` (ADR 0004).
Measured in 20 locales against Chromium (`DatetimeIntlTests`, `ref/intl.json`): month names, narrow day names, the month and year,
the time, AM and PM, the hour cycle and the column order are the same in every one; `en-US`, `pt-BR`, `ar-EG`, `he-IL`, `hi-IN`,
`nl-NL`, `pl-PL` and `fr-FR` match in every call. Avonic adds what .NET's formatting lacks: Arabic-Indic and Persian digits, the
`h11` and `h24` cycles, narrow day names, and a day alone with its suffix (`15日`). Each locale's calendar is its own, as in Chromium
(Hijri for `ar-SA`, Persian for `fa-IR`, Buddhist for `th-TH`). What differs is in COMPARISON.md's table: chiefly the short date of
the header and the date column in some locales (`6月15日木` for Chromium's `6月15日(木)`), as .NET has no skeleton for it.

## Extensions

None.

## Known gaps

- **Some locales' short dates and eras differ from Chromium's** (COMPARISON.md's locale table), and "Today" is English outside the 21
  languages measured, as .NET has no `Intl.RelativeTimeFormat`.
- **The locale's text is the platform's ICU, through .NET**: measured on Windows; Linux's gives some locales' AM and PM in English and
  `ar-SA` in the Gregorian calendar, and non-Latin narrow day names may be longer there (COMPARISON.md). Android, iOS and macOS
  weren't measured.
- **Snapping after a touch swipe is Avalonia's**: it jumps to the month, or aims a fling at it, where Chromium eases (as the picker
  and the segment view).
- **The month/year wheel closes at the parent overlay's next present**, not as soon as the datetime is hidden, as Avalonia doesn't say
  when an ancestor hides (the picker's known gap). Shown in a page, a tab or an accordion that hides it, it stays open.
- **Arabic text is taller** than Chromium's: Roboto has no Arabic, and Avalonia's fallback font has taller lines (523.5 for 515).
- **A day's selection isn't announced as selected** to assistive technology: Avalonia's button peer has no `aria-selected`; its name
  is Ionic's (`Today, Tuesday, June 20`).
- **The time popover is the overlay layer's**, as every Avonic popover: in a real popup window (another `TopLevel`) it can't be
  tested headless.
