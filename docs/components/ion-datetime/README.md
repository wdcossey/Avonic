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

- **`CalendarRendering`** (`DatetimeCalendarRendering`), how the calendar of a grid presentation draws its months
  ([#248](https://github.com/wdcossey/Avonic/issues/248)). **The default stays Ionic's `ScrolledMonths`; `SingleGrid` is opt-in**, for
  an app that wants the cheaper calendar: for every datetime through `AvonicConfig.DatetimeCalendarRendering`, or for one through its
  `CalendarRendering`. On the Pixel 8 the single grid opens in about half the time and shows a new month 3 times sooner, with more
  frames over 33 ms during its fade (`docs/infra/performance/README.md`, "The single grid").
  - `ScrolledMonths`, the default, is Ionic's: the previous, current and next months side by side in a scroller that snaps, which a
    change of month scrolls and a swipe drags; 126 days.
  - `SingleGrid` is one month of 42 days, drawn again in place. A change of month, by the buttons, a swipe, the keys or a value in
    another month, fades the days out in 90 ms (md's accelerate curve, `cubic-bezier(0.4, 0, 1, 1)`) and in again in 210 ms (decelerate,
    `cubic-bezier(0, 0, 0.2, 1)`): md's fade through, 300 ms, on `AvonicConfig`'s clock. With `AvonicConfig.Animated` off it changes at
    once. A horizontal swipe on the grid changes the month once it ends past 50 px (or a flick of 20 px at 0.3 px/ms): toward the next
    month when the finger moves toward the start of the line (left, or right in RTL). It doesn't follow the finger. The keys, focus,
    selection, `Min` and `Max`, `HighlightedDates`, `IsDateEnabled`, `FirstDayOfWeek`, `ShowAdjacentDays` and the automation names are
    the scrolled calendar's (`AvonicDatetimeSingleGridTests` checks each day against it).
  - **The keys in `SingleGrid`**: an arrow, Home, End or a page key to a day in another month fades there too, where the scrolled
    calendar, as Ionic's, draws the new month at once. The focused day keeps its focus while the old month fades out; at the swap the
    key's day in the new month takes it, before the days fade in, so its ring fades in with them and shows once the grid does. A key
    during the fade goes on from the day on its way (PageDown then Right from June 30 is July 31; Home and End from its week), and
    Enter or Space during it chooses that day, the new month drawn at once. A key back to the month shown drops the change, and the
    days fade in again from where they were; once the month has changed, a key within it moves at once and lets the fade in end. With
    `AvonicConfig.Animated` off a key to another month moves at once. `ScrolledMonths` keeps Ionic's keys unchanged.
  - A change dropped during the fade out, by a value, a selection or the month/year wheel as well as a key, fades the days back in
    from where they were, rather than jump.
  - The fade's durations and the swipe's threshold are constants for now; making them settable in `AvonicConfig` is on the roadmap.

  Each rendering makes only its own days, when the datetime first draws them. `AvonicConfig.DatetimeCalendarRendering` is the default for
  datetimes made after it is set, at style priority, so a value set on the datetime or in a style wins:

  ```csharp
  AvonicConfig.DatetimeCalendarRendering = DatetimeCalendarRendering.SingleGrid; // at start-up
  ```
  ```xml
  <c:AvonicDatetime CalendarRendering="SingleGrid" />
  ```

  The theme's `PART_CalendarGrid`, a `DatetimeCalendarMonth`, is the grid; the `:calendar-single-grid` pseudoclass hides the scroller
  and shows it. A template without it draws the scrolled calendar.

## Known gaps

- **Some locales' short dates and eras differ from Chromium's** (COMPARISON.md's locale table), and "Today" is English outside the 21
  languages measured, as .NET has no `Intl.RelativeTimeFormat`.
- **The locale's text is the platform's ICU, through .NET**: measured on Windows; Linux's gives some locales' AM and PM in English and
  `ar-SA` in the Gregorian calendar, and non-Latin narrow day names may be longer there (COMPARISON.md). Android, iOS and macOS
  weren't measured.
- **`SingleGrid` isn't Ionic's** (Extensions), and is opt-in: its swipe doesn't drag the month, and the month fades rather than
  slides, by the keys too. Measured on the Pixel 8 with the perf build; its real-window section
  (`drive.ps1 -Only '*single grid'`) is written and not yet run.
- **The scrolled calendar makes the months either side of the one shown a few frames after it's drawn**, or sooner when a press, a
  scroll or a change of month needs them (#248, spike B; COMPARISON.md, "The months either side, made later"): nothing on screen
  differs, but `IsDateEnabled` and a `HighlightedDates` function are called for those months' days then, not at the first draw.
- **Snapping after a touch swipe is Avalonia's**: it jumps to the month, or aims a fling at it, where Chromium eases (as the picker
  and the segment view).
- **The month/year wheel closes at the parent overlay's next present**, not as soon as the datetime is hidden, as Avalonia doesn't say
  when an ancestor hides (the picker's known gap). Shown in a page, a tab or an accordion that hides it, it stays open.
- **Arabic text is taller** than Chromium's: Roboto has no Arabic, and Avalonia's fallback font has taller lines (523.5 for 515).
- **A day's selection isn't announced as selected** to assistive technology: Avalonia's button peer has no `aria-selected`; its name
  is Ionic's (`Today, Tuesday, June 20`).
- **The time popover is the overlay layer's**, as every Avonic popover: in a real popup window (another `TopLevel`) it can't be
  tested headless.
