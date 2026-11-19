# ion-datetime — Plan

`AvonicDatetime`: Ionic's date and time picker, md, whole: the calendar grid with its month swipe, the month/year wheel, the time
button and its popover, the wheel presentations, multiple dates, min/max and the value lists, `isDateEnabled`, highlighted dates,
the header, the default buttons, and the parent overlay's confirm and cancel. It builds on `AvonicPicker` ([#37](../ion-picker/README.md))
for every wheel, `AvonicPopover` ([#43](../ion-popover/README.md)) for the time popover, `AvonicButton` and `AvonicButtons` for the
navigation and footer buttons, and `SmoothScroll` for the month scroll. `ion-datetime-button` ([#79](https://github.com/wdcossey/Avonic/issues/79))
is a separate issue; what it needs from the datetime is kept here as internal members (below).

Measured in real Ionic 9.0.11 in headless Chromium, md, in `ion-app`, on `render.js`'s phone (390 x 844 at 2x, mobile and touch,
Roboto from `tools/fonts/roboto/`), with the clock pinned to 2023-06-20 10:00 UTC (Playwright's `clock.setFixedTime`), the time
zone UTC and the locale `en-US` unless a case says otherwise, and the value `2023-06-15T13:47:00` unless it says otherwise. The raw
output is [`ref/layout.json`](ref/layout.json) (29 layouts: every box and text), [`ref/behaviour.json`](ref/behaviour.json)
(34 cases: events with their times and details, state after each step, scroll positions), [`ref/intl.json`](ref/intl.json)
(every `Intl` call Ionic makes, in 20 locales) and [`ref/locales.json`](ref/locales.json) (Arabic in RTL, Japanese, British
English, German, Korean), with a frame of each case in [`ref/`](ref/). The scripts aren't in the repo, as the swarm brief asks;
COMPARISON.md describes them.

## Fidelity checklist

`node tools/ionic-api.js ion-datetime`.

### Props (32)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `cancelText` | `CancelText` | `string` | `"Cancel"` | |
| [x] | `clearText` | `ClearText` | `string` | `"Clear"` | |
| [x] | `color` | `Color` | `string?` | | `ColorPalette.ColorProperty.AddOwner`; `:ion-color`. Passed on to the footer buttons and every wheel column and option, as Ionic's `color={this.color}` |
| [x] | `dayValues` | `DayValues` | `DatetimeValueList?` | | Ionic's `number \| number[] \| string`: a struct from an `int`, an `int[]` and text (`"1,15,30"`, `"[1, 15]"`), read by Ionic's `convertToArrayOfNumbers` |
| [x] | `disabled` | `IsEnabled` | `bool` | `true` | The mapping rule. `aria-disabled` is the peer's `IsEnabled` |
| [x] | `doneText` | `DoneText` | `string` | `"Done"` | |
| [x] | `firstDayOfWeek` | `FirstDayOfWeek` | `int` | `0` | Sunday; read `% 7`, as Ionic does |
| [x] | `formatOptions` | `FormatOptions` | `DatetimeFormatOptions?` | | `Date` and `Time`, each a `DateTimeFormatOptions`: Intl's options as properties (below) |
| [x] | `highlightedDates` | `HighlightedDates` | `DatetimeHighlightedDates?` | | Ionic's array-or-callback: a struct from a list of `DatetimeHighlight` and from `Func<string, DatetimeHighlightStyle?>`, as `ModalCanDismiss` is from a bool or a function |
| [x] | `hourCycle` | `HourCycle` | `DatetimeHourCycle?` | null | `H11`, `H12`, `H23`, `H24`; null is the locale's |
| [x] | `hourValues` | `HourValues` | `DatetimeValueList?` | | As `dayValues` |
| [x] | `isDateEnabled` | `IsDateEnabled` | `Func<string, bool>?` | | Given `"2023-06-15"`; an exception is logged and the day stays enabled, as Ionic's `try`/`catch` leaves it |
| [x] | `locale` | `Locale` | `string` | `"default"` | A culture name; `"default"` is `CultureInfo.CurrentCulture`, as Ionic's is the browser's. Formatting is .NET's (below) |
| [x] | `max` | `Max` | `string?` | | ISO 8601, any precision (`"2023"`, `"2023-07"`, `"2023-07-20T17:15"`), as Ionic's `parseMaxParts` |
| [x] | `min` | `Min` | `string?` | | As `max`, with `parseMinParts` |
| [x] | `minuteValues` | `MinuteValues` | `DatetimeValueList?` | | As `dayValues` |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |
| [x] | `monthValues` | `MonthValues` | `DatetimeValueList?` | | As `dayValues` |
| [x] | `multiple` | `Multiple` | `bool` | `false` | Ionic's warnings for a presentation other than `date`, or `preferWheel`, are logged, as Ionic's are |
| [ ] | `name` | none | | | Not applicable: an HTML form's hidden input (`renderHiddenInput`), as for the select, the checkbox and the input |
| [x] | `preferWheel` | `PreferWheel` | `bool` | `false` | |
| [x] | `presentation` | `Presentation` | `DatetimePresentation` | `DateTime` | `Date`, `DateTime`, `Month`, `MonthYear`, `Time`, `TimeDate`, `Year` |
| [x] | `readonly` | `Readonly` | `bool` | `false` | |
| [x] | `showAdjacentDays` | `ShowAdjacentDays` | `bool` | `false` | |
| [x] | `showClearButton` | `ShowClearButton` | `bool` | `false` | |
| [x] | `showDefaultButtons` | `ShowDefaultButtons` | `bool` | `false` | |
| [x] | `showDefaultTimeLabel` | `ShowDefaultTimeLabel` | `bool` | `true` | |
| [x] | `showDefaultTitle` | `ShowDefaultTitle` | `bool` | `false` | |
| [x] | `size` | `Size` | `DatetimeSize` | `Fixed` | `Cover`, `Fixed` |
| [x] | `titleSelectedDatesFormatter` | `TitleSelectedDatesFormatter` | `Func<IReadOnlyList<string>, string>?` | | |
| [x] | `value` | `Value` | `DatetimeValue?` | null | Ionic's `string \| string[] \| null`: a struct of one string or a list, with a `TypeConverter` from text and implicit conversions from `string` and `string[]`. Two-way by default, as it's the user's (the mapping rule for a mutable prop). Its `ToString()` is Ionic's `formatValue`, the strings joined by `,` |
| [x] | `yearValues` | `YearValues` | `DatetimeValueList?` | | As `dayValues` |

### Events (4)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionBlur` | `IonBlur` (routed, bubbles) | When focus leaves the datetime and its parts. Moving inside raises nothing (measured: one `ionFocus` on Tab in, one `ionBlur` on Tab out, none for the arrow keys between days). The inner buttons' own `IonFocus` and `IonBlur` stop at the datetime, as Ionic's `stopPropagation` |
| [x] | `ionCancel` | `IonCancel` (routed) | |
| [x] | `ionChange` | `IonChange` (routed, `DatetimeChangeEventArgs.Value`, a `DatetimeValue?`) | At every confirm, even of the same value (measured: a second click on the selected day raises it again), never for a value set in code |
| [x] | `ionFocus` | `IonFocus` (routed, bubbles) | As `ionBlur` |

Internal events Ionic has, kept internal for `ion-datetime-button` (#79): `ionValueChange` → `internal event ValueChange`, `ionRender`
→ `internal event Rendered`. `ionStyle` is the `IonStyle` routed event, `{ "interactive-disabled": !IsEnabled }`, as the select
raises it; the item reads `interactive` and `datetime` from `IItemControl.ItemControlType.Datetime`, as for the select.

### Methods (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `cancel(closeOverlay?)` | `Cancel(bool closeOverlay = false)` | Synchronous: Ionic's does nothing asynchronous before it returns. The parent overlay is dismissed with role `datetime-cancel` |
| [x] | `confirm(closeOverlay?)` | `Confirm(bool closeOverlay = false)` | Role `datetime-confirm` |
| [x] | `reset(startDate?)` | `Reset(string? startDate = null)` | |

`getDefaultPart()` (`@internal`, for `ion-datetime-button`) → `internal DatetimeParts GetDefaultPart()`.

### Slots (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `buttons` | `ButtonsContent` (+ `ButtonsContentTemplate`) | Replaces the default buttons; while it's set, a selection waits for `Confirm()`, as Ionic's `querySelector('[slot="buttons"]')` |
| [x] | `time-label` | `TimeLabelContent` (+ `…Template`) | Default `"Time"`; shown when set even with `ShowDefaultTimeLabel` false, as Ionic |
| [x] | `title` | `TitleContent` (+ `…Template`) | Default `"Select Date"`; when set, the header shows without `ShowDefaultTitle`, as Ionic |

### Shadow parts (18)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `calendar-day` | `DatetimeCalendarDay`, a type with its own `ControlTheme` | Ionic makes one per day in `render`: by the mapping rule for elements made per item that take input and focus, a type of its own, styled as `c\|DatetimeCalendarDay` |
| [x] | `calendar-day active` | `DatetimeCalendarDay:active` | |
| [x] | `calendar-day disabled` | `DatetimeCalendarDay:day-disabled` | Ionic's `disabled` part is the day's own state (min, max, the value lists, `isDateEnabled`), not the datetime's `disabled`, which also disables the button: hence a name of its own beside Avalonia's `:disabled` |
| [x] | `calendar-day today` | `DatetimeCalendarDay:today` | |
| [x] | `calendar-days-of-week` | `PART_CalendarDaysOfWeek` | |
| [x] | `calendar-header` | `PART_CalendarHeader` | |
| [x] | `datetime-header` | `PART_DatetimeHeader` | |
| [x] | `datetime-selected-date` | `PART_DatetimeSelectedDate` | |
| [x] | `datetime-title` | `PART_DatetimeTitle` | |
| [x] | `month-year-button` | `PART_MonthYearButton` | |
| [x] | `navigation-button` | `PART_PreviousButton`, `PART_NextButton`, each with the style class `navigation-button` | One element can't have two part names in Avalonia; the shared name is a class |
| [x] | `next-button` | `PART_NextButton` | |
| [x] | `previous-button` | `PART_PreviousButton` | |
| [x] | `time-button` | `PART_TimeButton` | |
| [x] | `time-button active` | `PART_TimeButton` with `:active` | |
| [x] | `wheel` | Each `AvonicPickerColumn` the datetime makes, with the style class `wheel` | The columns are made in code, one per wheel, as Ionic's; a class names them, as `AvonicPickerColumn` is already a type |
| [x] | `wheel-item` | Each `AvonicPickerColumnOption` it makes, with the class `wheel-item` | |
| [x] | `wheel-item active` | … and the class `active` on the working value's | Set at render, as Ionic's part is, not as the column scrolls |

### CSS custom properties → resource keys (6)

| ✓ | Ionic | Key | md default | Notes |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Datetime.Background` | `#ffffff` | `var(--ion-color-step-100, var(--ion-background-color-step-100, #ffffff))`: Ionic's light theme sets neither step, so the literal; `Palettes/Dark.axaml` follows `Avonic.BackgroundColorStep100`, as dark sets it |
| [x] | `--background-rgb` | `Avonic.Datetime.BackgroundRgb` | unset | Only `datetime.ios.css` reads it (the ios wheel fades). Kept as a `CssUnset` key that nothing in md reads, as the picker's `FadeBackgroundRgb` and the skeleton text's `BackgroundRgb`; recorded |
| [x] | `--title-color` | `Avonic.Datetime.TitleColor` | primary's contrast | `:ion-color`: the colour's contrast |
| [x] | `--wheel-fade-background-rgb` | `Avonic.Datetime.WheelFadeBackgroundRgb` | unset | Passed to the picker as its `--fade-background-rgb`: unset in md, so the picker's own |
| [x] | `--wheel-highlight-background` | `Avonic.Datetime.WheelHighlightBackground` | unset | The picker's `--highlight-background`: unset in md, so transparent |
| [x] | `--wheel-highlight-border-radius` | `Avonic.Datetime.WheelHighlightBorderRadius` | unset | The picker's `--highlight-border-radius`: unset in md, so the picker's 8 |

Other values in `datetime.md.css` with no variable are literals in the theme (rule: a visual constant with no CSS variable), with keys
`Avonic.Datetime.Md.*` where a global step colour falls back to a literal that Ionic's light theme doesn't set (the time button's
background `#edeef0`), which `Palettes/Dark.axaml` points at the step, as the refresher's and the modal's handle are.

## What Ionic does

Read in `datetime.js` and `utils/` (parse, format, manipulation, comparison, state, data, helpers, validate), and measured. The
numbers are the behaviours to test.

### State

1. **Two sets of parts.** `workingParts` is what's shown (the month, the wheels' positions); `activeParts` is what's selected
   (one, or a list with `multiple`). `defaultParts` is today, snapped by the `*Values` lists and min/max (`getClosestValidDate`),
   worked out once at load; `todayParts` is today, for the today ring and the "Today" label.
2. **A value in code** (`value` set, or at load) parses (`parseDate`: Ionic's own ISO 8601 regex, a time alone first), sets the
   active parts (the list, or one clamped to min/max by date), and moves the working parts there: animated (the month scroll) in the
   grid when the month changes and the calendar is shown, at once otherwise. No `ionChange` (measured). An unparsable value is
   logged and changes nothing. A list with `multiple` false is logged, and its last date is shown. The working month of a list is
   its last date's (measured: June and July shows July).
3. **Time zones are ignored.** `parseDate` reads the wall clock as written and drops `Z` and any offset; every value the datetime
   writes has no zone (`2023-06-15T13:47:00`). Avonic does the same: no `DateTime` conversion, the parts as text.
4. **The value written** is `convertDataToISO`: `YYYY-MM-DDTHH:mm:00` for a date with a time, `YYYY-MM-DD` for a date alone (each of
   `multiple`'s dates, which drop the time), `HH:mm` for a time with no date. With no value, a selection takes the time from the
   default parts, today's (measured: day 22 → `2023-06-22T10:00:00`, even with `presentation="date"`).
5. **Selecting** (`setActiveParts`): nothing when `readonly`; the parts validated (`validateParts`: clamped by date, the day cut to
   the month, the time raised or lowered on min's or max's day); then, unless the default buttons are on or the `buttons` slot is
   used, `confirm()` at once (measured: the click raises `ionChange`). A second click on the selected day confirms again (measured:
   a second `ionChange` with the same value).
6. **`confirm()`** writes the active parts, raising `ionChange` even when nothing changed (measured: twice for two calls). With
   nothing selected: the grid writes no value (`ionChange` with no value, measured); `preferWheel` writes the working parts
   (measured: `2023-06-20T10:00:00`). `closeOverlay` dismisses the nearest `ion-modal` or `ion-popover` around it with role
   `datetime-confirm` (measured).
7. **`cancel()`** raises `ionCancel`; with `closeOverlay` and a parent overlay, dismisses it with `datetime-cancel` and leaves the
   selection for the next present to discard; otherwise discards it now, the shown month going back to the value's (measured, animated).
8. **`reset(start)`** is a value in code without the value: the active and working parts move (measured: `reset('2023-02-11')`
   shows February with the 11th selected, `value` unchanged); `reset()` clears the selection and shows today's month.
9. **The parent overlay's present** (`ionModalWillPresent`, `ionPopoverWillPresent` of the nearest modal or popover, not a nested
   one's) discards an unconfirmed selection, with no animation (measured: day 22 chosen, dismissed, presented again: back to 15).
10. **Clear** (`showClearButton`): `reset()` then the value cleared, with `ionChange` and no value (measured).

### The calendar grid (`date`, `date-time`, `time-date`, without `preferWheel`)

11. **Three months**, the working one in the middle of a horizontal scroller that snaps a month at a time (`scroll-snap-type: x
    mandatory`, `scroll-snap-stop: always`). Each month a grid of 7 columns and 6 rows (`getDaysOfMonth`): blank cells before the
    1st, from `firstDayOfWeek`; with `showAdjacentDays`, 42 days, the previous and next months' in grey.
12. **The month changes when the scroll rests** on the first or last month (50 ms after the last scroll event, measured 66 ms after
    the last frame): the working month moves, and the scroller goes back to the middle at once. A month outside min/max can't be
    snapped to (`scroll-snap-align: none`) and isn't moved to.
13. **Previous and next** scroll smoothly by a month (measured: 350 px in about 300 ms, Chromium's smooth scroll, which
    `SmoothScroll` already recreates); at once when `AvonicConfig.Animated` is off (`config.getBoolean('animated')`). Each is
    disabled when its month is outside min/max (`isPrevMonthDisabled`, `isNextMonthDisabled`, measured), or when the datetime is.
14. **A value in another month** animates there: the target month is drawn in place of the next (or previous) one, scrolled to, and
    then becomes the working month (`forceRenderDate`, measured: September from June in one scroll).
15. **Days**: the selected day (`:active`), today (`:today`, a ring), disabled days (before min, after max, not in `dayValues`, a
    month or year not in `monthValues`/`yearValues`, or `isDateEnabled` false: `:day-disabled`, at 0.4). The text is the locale's
    day number (`formatToParts`' `day`: Arabic-Indic digits in `ar-EG`, no `日` in Japanese). `isDateEnabled` is asked for every day
    of the three months, given `"2023-05-01"` (measured: 92 calls).
16. **Clicking a day** selects it (5), keeping the active time; with `multiple`, adds it, or removes it if it's selected (measured);
    an adjacent day selects it and animates to its month.
17. **Highlighted dates**: a day's text colour, background and border from the list (matched by `YYYY-MM-DD`) or the callback,
    never on the selected day, padding or adjacent days (measured: the active 15th kept primary; the 20th's border replaced the
    today ring's).
18. **The month/year button** shows the working month's long name and year (`June 2023`), with a caret down, up when open; it
    swaps the calendar for a month and year wheel (`show-month-and-year`), hiding the navigation, the days of the week, the calendar,
    the time and the footer; `aria-label` "Show year picker" / "Hide year picker". In single mode, a month or year chosen there is a
    selection (measured: August → `ionChange` with `2023-08-15T13:47:00`); with `multiple`, only the shown month moves.
19. **The keyboard** (measured): Tab reaches the month/year button, previous, next, then the calendar, which hands focus to the
    working day; the arrows move by a day and a week (not mirrored in RTL, measured), Home and End to the start and end of the row,
    Page Up and Down by a month, with Shift by a year; a move to a day disabled by min or max does nothing; Enter and Space select.
    Days are out of the Tab order (`tabindex="-1"`).

### The time

20. **`date-time` and `time-date`** show a "Time" row: the label (or the slot) and a button with the time in the locale's format
    (`1:47 PM`; `13:47` in `en-GB`), part `time-button`, `active` while its popover is open. It opens a popover centred under the
    button, 10 px up (`--offset-y: -10px`), with no arrow, holding the hour, minute and AM/PM wheels (measured: 250 x 200, its top at
    the button's bottom − 10). Escape closes it (measured).
21. **The time wheels** (`getTimeColumnsData`): hours by the hour cycle (12, 1 … 11 for `h12`; 00 … 23 for `h23`; 0 … 11; 1 … 24),
    filtered by `hourValues`; minutes 00 … 59 by `minuteValues`; AM and PM unless 24-hour (the locale's names, or `AM`/`PM` when
    the locale is 24-hour, measured), first in a locale whose time starts with the period (Korean, measured). Once a date is selected,
    min and max limit them on their own day (measured: max 17:15 on its day leaves 12 … 5 PM). Choosing one is a selection (5).
    AM/PM moves the hour by 12 (`calculateHourFromAMPM`). The hour and minute wheels take typed digits (the picker's numeric input).
22. **`presentation="time"`** shows the wheels in place of the button.

### Wheels (`preferWheel`, `month`, `month-year`, `year`)

23. **Columns**: `date` with `preferWheel` has month, day and year (day first where the locale puts the day first: `en-GB`,
    measured); `date-time`/`time-date` with `preferWheel` have one date column (`Thu, Jun 15`, "Today" for today, three months
    around the working date, narrowed by min and max) and the time columns, after or before it; `month`, `month-year` and `year`
    their columns. The years run from 100 before today's year to today's (or min's and max's), the months and days within min and
    max. Choosing one is a selection (5); the date column's window moves with it (measured).
24. **The month and day wheels follow the working month** (measured: February leaves 28 days).

### The header and footer

25. **Header**: shown when `showDefaultTitle` or the title slot is set; "Select Date" (uppercase in md), and, except for the wheel
    and time presentations, the selected date (`Thu, Jun 15`, or `formatOptions.date`'s format); with `multiple`, `N days` for
    other than one, or `titleSelectedDatesFormatter`'s text (measured).
26. **Footer**: with `showDefaultButtons`, Cancel (`cancel(true)`) and Done (`confirm(true)`); with `showClearButton`, Clear; or the
    `buttons` slot. Disabled when the datetime is disabled or read-only. Hidden while the month/year wheel is open.

### Host

27. **`disabled`**: nothing responds (measured); the days of the week and the time at 0.4, the days disabled.
28. **`readonly`**: the calendar can be browsed (previous, next, swipe, the month/year wheel) but nothing selected (measured: a
    click on a day focuses the calendar and changes nothing); the time button and the wheels don't take pointer input
    (`pointer-events: none`), the footer buttons are disabled at full opacity.
29. **In an item**: `item-interactive`, `item-datetime` (measured), and `interactive-disabled` when disabled.
30. **`size`**: `fixed` is at most 350 wide (a wheel presentation at least 350 and as wide as its content); `cover` fills its
    container (measured 358 in 358).
31. **Hidden in an overlay**: the month/year wheel closes when the datetime is hidden, so it reopens on the calendar.

### Locale

32. **Every text is the locale's** (`Intl` in Ionic): the month and year, the days of the week (narrow), the day numbers, the month
    names, the years, the date column's `Thu, Jun 15`, the header, the days' accessible names (`Thursday, June 15`, `Today, …`), the
    time, AM and PM, "Today", the hour cycle, which column comes first, and where AM/PM goes.

## Measured

Facts the tests and the theme take from Chromium (`ref/layout.json` unless said):

| | Ionic, md | |
|---|---|---|
| M1 | width 350 (`max-width`), `date-time` 391 high, `date` 344, `time` and the wheels 200 | |
| M2 | the header 124: 20 padding, the title 14 (0.75rem, uppercase), the date 40 at 64 (2.125rem) | `title` |
| M3 | the calendar header 84: the month/year button 130.9 × 48 (padding 12, 20 then 16), the arrows 48 × 48 at 254 and 302, the days of the week 36 (line height), 0.875rem, `#808080` | |
| M4 | the calendar 260: 4 + 6 rows of 42 + 4; a day 42 × 42, 0.875rem, a circle; columns 47.1 apart from 10 in | |
| M5 | the time row 47: padding 8 and 16; the label 31 high; the button 82.9 × 31, padding 6 and 12, radius 8, `#edeef0` | |
| M6 | the footer 52: 10 padding, buttons 32 high; Cancel 74 wide, Done 56; without Clear they sit at the end (Cancel at 204), with Clear Cancel is at the start (12) | `title`, `disabled` |
| M7 | the month/year wheel 350 × 200 under the 48 header; the host 248 | `monthYearOpen` |
| M8 | the wheels: month 138.7 wide, day 58, year 81.5, hour and minute 58, AM/PM 65.6, the date column 157.3; centred | |
| M9 | the time popover 250 × 200, its top 10 above the button's bottom | `behaviour.json` `timeButton` |
| M10 | the today ring 1 px primary, primary text; the active day primary with white text; adjacent days `#808080`; a focused day primary at 0.2 with a 4 px ring of it | |

## Locale: Chromium's `Intl` and .NET's `CultureInfo`

ADR 0004 says .NET's `CultureInfo` does `Intl`'s job, and to measure where they differ. Measured in 20 locales (`ref/intl.json`;
the .NET side read from `CultureInfo` on Windows, which uses ICU since .NET 5):

- **The same:** the month names (`MonthNames`, the standalone forms, as `{ month: 'long' }` alone gives), the narrow day names
  (`ShortestDayNames` equals Intl's `narrow` in all 20, Arabic, Hebrew, Chinese and Thai included), the month and year
  (`YearMonthPattern`: `June 2023`, `junio de 2023`, `июнь 2023 г.`, `2023年6月`, `۱۴۰۲ خرداد`), the default hour cycle (from
  `ShortTimePattern`), the time (`h:mm tt`, `HH:mm`, `H:mm`, `tt h:mm` give what Intl gives), AM and PM where the locale is
  12-hour, which of day and month comes first (`ShortDatePattern`), and each locale's own calendar (Hijri for `ar-SA`, Persian for
  `fa-IR`, Buddhist for `th-TH`, in both).
- **What .NET doesn't do, and Avonic does:** native digits (Intl writes Arabic-Indic digits in `ar-EG`, `ar-SA`, `fa-IR`; .NET
  writes Latin ones, and says `DigitSubstitution.Context` for exactly those three, so Avonic substitutes `NativeDigits` there);
  the hour cycles `h11` and `h24`, which .NET's format strings can't write (Avonic writes the hour itself); `narrow` weekdays
  (`ShortestDayNames`); a day number alone with its suffix (`15日`, `15일`, the letters after `d` in `MonthDayPattern`).
- **What .NET doesn't have:** Intl's skeletons (`{ weekday: 'short', month: 'short', day: 'numeric' }` → `Thu, Jun 15`). Avonic
  builds them from `LongDatePattern`, dropping the fields not asked for with the literals that belong to them, and shortening the
  rest: exact in `en-US`, `en-GB`, `fr-FR` and others, close elsewhere (`ja-JP` `6月15日木` for Intl's `6月15日(木)`; `de-DE` `Do, 15.
  Jun` for `Do., 15. Juni`); each measured locale's result is in COMPARISON.md. "Today" (`Intl.RelativeTimeFormat`) has no .NET
  counterpart: a table for the measured languages, English otherwise.

## Not copied from Ionic

Bugs by [ADR 0005](../../decisions/0005-dont-copy-an-ionic-bug.md), recorded in `docs/infra/ionic-issues.md`, each with a test:

- **An adjacent day ignores the default buttons.** Its click calls `confirm()` directly, so with `showDefaultButtons` (or the
  `buttons` slot) it commits at once (measured: `ionChange` with `2023-07-03T13:47:00` before Done), and it never sets
  `hasUnconfirmedSelection`. Ionic's own docs say the default buttons are what update the value, and every other selection goes
  through `setActiveParts`. Avonic selects it as any other day (5), then shows its month.
- **Done with nothing chosen clears a wheel that always shows.** `confirm()` writes the working parts only when `preferWheel` is
  set, "if the datetime is using a wheel picker … the user has confirmed the initial value presented to them" (its comment); but
  `time`, `month`, `month-year` and `year` always show a wheel, and write no value (measured: `presentation="time"` with Done →
  `ionChange` with no value). Avonic writes the working parts for every wheel presentation, as the comment means.

Followed, though odd: the arrow keys aren't mirrored in RTL; `confirm()` raises `ionChange` with an unchanged value; `isDateEnabled`
only sees the grid's and the wheels' days, not the keyboard's moves (which check min and max only).

## Design

- **`AvonicDatetime : TemplatedControl, IItemControl`**, in `Controls/Datetime/`. Its state is Ionic's: `DatetimeParts` (an internal
  record struct of month, day, year, hour, minute, AM/PM, adjacent, day of week) for the working, active, default, today, min and max
  parts. Ionic's utilities are ported one for one into internal static classes (`DatetimeParse`, `DatetimeCompare`,
  `DatetimeManipulation`, `DatetimeData`, `DatetimeState`), so that each can be tested against Ionic's own behaviour.
- **Rendering**: Ionic re-renders everything from state. `Render()` updates the parts from state after each change: texts,
  pseudoclasses, the three months' cells (reused: 42 `DatetimeCalendarDay`s each), and the wheels (options rebuilt only when their
  values change, so a column isn't reset under the user's finger).
- **Template parts**: `PART_DatetimeHeader` (`PART_DatetimeTitle`, `PART_DatetimeSelectedDate`); `PART_CalendarHeader` with
  `PART_MonthYearButton` (a `Button` with `Activatable`, `FocusVisible` and a ripple), `PART_PreviousButton`/`PART_NextButton`
  (`AvonicButton`s in `AvonicButtons`, icon-only chevrons that flip in RTL), `PART_CalendarDaysOfWeek`; `PART_CalendarBody`, a
  `ScrollViewer` around `DatetimeCalendarBodyPanel` (three pages, a snap point each, none for a disabled month) holding three
  `DatetimeCalendarMonth` grids; `PART_MonthYearWheel`, an `AvonicPicker`; `PART_Time` with `PART_TimeLabel`, `PART_TimeButton`
  and `PART_TimePopover` (an `AvonicPopover` holding `PART_TimeWheel`, an `AvonicPicker`); `PART_Wheel`, the `AvonicPicker` of the
  wheel presentations; `PART_Footer` with the default `AvonicButton`s or `ButtonsContent`. Which are shown follows the presentation,
  as `renderDatetime` does, through pseudoclasses on the host named after Ionic's host classes: `:presentation-date-time` …,
  `:size-fixed`, `:size-cover`, `:prefer-wheel`, `:grid`, `:show-month-and-year`, `:month-year-picker-open`, `:readonly`, `:ion-color`.
- **The month swipe** is Avalonia's scroll snapping (`HorizontalSnapPointsType="MandatorySingle"`, a snap at most one page from
  where it started, as `scroll-snap-stop: always`), the month change on a 50 ms rest (`AvonicConfig.Schedule`), and the arrows
  through `SmoothScroll`. In RTL the scroller is mirrored by Avalonia, so the offsets don't change sign as Chromium's do.
- **Locale**: `DatetimeIntl`, internal: Ionic's format calls (`getMonthAndYear`, `getDaysOfWeek`, `getDay`, `getYear`,
  `getLocalizedTime`, `getLocalizedDateTime`, `generateDayAriaLabel`, `getTodayLabel`, `getLocalizedDayPeriod`, `getHourCycle`,
  `isMonthFirstLocale`, `isLocaleDayPeriodRTL`) on a `CultureInfo`, through a small renderer of .NET date patterns that writes
  every field itself (hour cycles, narrow days, native digits).
- **`formatOptions`**: `DatetimeFormatOptions { Date, Time }`, each a `DateTimeFormatOptions` with Intl's `Weekday`, `Era`, `Year`,
  `Month`, `Day`, `Hour`, `Minute`, `Second`, `HourCycle`, `Hour12`, `DateStyle`, `TimeStyle`, `TimeZone` and `TimeZoneName` as enums
  (Intl's string values). `TimeZone` and `TimeZoneName` are logged and ignored, as Ionic warns and strips them. Ionic's warnings for
  a presentation without its object are logged.
- **Today** is `AvonicDatetime.Now` (internal, `DateTime.Now` by default), which tests pin, as Ionic's `getToday()` is the clock's.
- **The parent overlay** is the nearest `AvonicModal` or `AvonicPopover` among the logical ancestors, as `closest('ion-modal,
  ion-popover')`; its `IonModalWillPresent` / `IonPopoverWillPresent`, from itself, discards an unconfirmed selection.
- **For `ion-datetime-button` (#79)**: it moves the datetime into a modal or popover and reads its text, so the datetime works
  wherever it's moved (its present listener is attached on each attach to the logical tree), and keeps internal what the button
  reads: `ValueChange`, `Rendered`, `GetDefaultPart()`, and `DatetimeIntl`'s formatting. Nothing here assumes the datetime stays
  where it was declared.

### Not done, with why

- `name`: an HTML form's hidden input.
- `mode`: ADR 0001.
- `navigator.maxTouchPoints`' iOS rubber-band fix: iOS mode only.
- Intl options Ionic passes through but Avonic doesn't model (`calendar`, `numberingSystem`, `fractionalSecondDigits`,
  `dayPeriod`, `formatMatcher`, `localeMatcher`, `timeZone`): `calendar` and the digits follow the culture; the rest are recorded.

## Behaviours to test

One or more tests each, in `AvonicDatetimeTests` (behaviour), `DatetimeUtilsTests` (Ionic's utilities, against values read from
the `.js` and measured), `DatetimeIntlTests` (the locale table above), and `AvonicDatetimeCaptureTests` (frames and boxes):
B1-B32 are the numbered behaviours above; B33 and B34 the two ADR 0005 differences.

## Extensions

None.

## Dependencies

`ion-picker` (#37), `ion-popover` (#43), `ion-toolbar` (#12, its `ion-buttons`), `ion-button` (#10), `ion-icon`, `ion-ripple-effect`:
all merged. `ion-datetime-button` (#79) comes after.
