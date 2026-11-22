# Real window

Headless tests can't see everything. The first run of this by hand found a control bug that 1,486 tests had missed, so it runs
on each batch before it's called ready to merge.

**It takes over the cursor and focus for about four minutes, so ask first.** It puts the cursor back and closes only the window
it opened.

```
pwsh -ExecutionPolicy Bypass -File tools/real-window/drive.ps1
```

Windows PowerShell 5.1 (`powershell`) runs it too. Windows only, for now (#133).

It builds the sample first, from the repo so that `global.json` picks the SDK: a stale build runs the old code, and fails checks
the new code passes. `-Configuration Release` builds and runs Release instead of Debug; only a Debug build checks the performance
line. `-NoBuild` runs the build as it is, and `-Exe <path>` runs another build or copy of the sample as it is. **Close the sample
first** if it's open: it locks its files, so the script stops rather than build. Frames go to a new folder in the temp folder for
each run, which it prints, unless you pass `-Out`. `-Only 'ion-picker,ion-re*'` runs only the sections with those titles, to work
on one, and fails if none matches; the whole run is the one that counts. It exits 1 if a check fails.

**If something takes the focus, it stops sending input** rather than type into another app, and fails those checks. It stops at
once if the window doesn't fit on the screen.

## What it does

- **Every page on the index,** with nothing to add for a new control: opens it, saves a frame, and fails on a button with no
  size, which is how the invisible button showed up, and on a missing performance line in a Debug build.
- **Real input on the pages that take it:** counts clicks, double clicks, Enter, Space and touch taps, and checks keyboard focus
  by pixel and sticky dividers by position. A new interactive control should get a section here. The P2 controls each have one:
  - `ion-item-sliding`: touch and mouse swipes open each side, a tap on an option clicks it, a tap outside leaves it open and a
    press on another sliding item closes it, as Ionic's `canStart` does, `CloseOpened()`, a full swipe's `IonSwipe`, the disabled
    row, and a drag up a row scrolling the page.
  - `ion-reorder`: touch and mouse drags on a handle, the page scrolling under a drag held near its bottom, a drag off the handles
    scrolling, and the toggle turning it off.
  - `ion-refresher`: a pull past the threshold refreshes, with the spinner, and completes; a short one cancels; a pull when
    scrolled down only scrolls; Disabled stops it. Since #176, four checks: after `Cancel the refresh`, a pull held still
    past the cancel's end and let go leaves it on, so the next pull refreshes (Ionic's C3), and a refresh begun while cancelling goes
    on past the cancel's end until the page completes it (C9). The held pull is updated in place every 40 ms (`TouchHold`), as a real
    screen reports a still finger: Windows drops an injected contact left 100 ms without an update, and the lift then fails (1460).
  - `ion-picker`: a wheel notch moving three rows and back, as in Chromium, a tap on an option, the arrows on the focused column
    skipping a disabled option, and a touch drag that snaps.
  - `ion-searchbar`: focus showing the cancel arrow, typing with the debounce, the clear button by mouse and touch keeping the
    focus, Escape, and the cancel button.
  - `ion-textarea`: Enter, auto grow, the counter (read from a line on the page, and by pixel) and clear on edit.
  - `ion-input-otp`: typing box to box, Backspace, `IonComplete`, a paste and `IonChange` on leaving. The paste puts the
    clipboard's text back afterwards, and is skipped if the clipboard holds anything else.
  - `ion-alert` (#39): a real click on the trigger presents the page's alert, Tab stays inside, Escape dismisses it with
    `backdrop`; a radio alert made in code returns `red` after a pick and OK; a text alert's Save handler returning false keeps it
    open, Enter in its field does nothing (Ionic's has no form), and Enter on Save submits `{name: "Ann"}`; the `IsOpen` alert with
    `BackdropDismiss` off stays on Escape and closes from a button. Run in the full sweep on `4383769`.
  - `ion-action-sheet` (#40): a real click on the trigger presents the page's sheet; Share dismisses it with `share` and no role,
    Delete with `delete` and `destructive`; a handler returning false keeps a sheet open, and its Cancel dismisses it with `cancel`;
    Escape and a click on the backdrop dismiss with `backdrop`; a radio sheet made in code, as `ion-select` builds one, has Banana
    checked and returns `cherry` after a click on Cherry; the `IsOpen` sheet with `BackdropDismiss` off stays on Escape and closes
    from its Close button. Run in the full sweep on `4383769`.
  - `ion-popover` (#43): a real click opens the menu below its button, at its left; ArrowDown twice from the popover focuses the
    second item, End the last enabled one; ArrowRight on More opens the submenu with focus on its first item, ArrowLeft closes it
    alone with focus back on More; Escape dismisses the menu with `backdrop`; a click on an item dismisses it with no role
    (`dismissOnSelect`), and one on the backdrop with `backdrop`; resting the pointer on the hover trigger presents its popover; a
    real right click opens the context menu at the pointer (its first item at the click's x, 8 to 11 below it). Frames:
    `popover-menu`, `popover-submenu`, `popover-hover`, `popover-context`. Run in the full sweep on `4383769`.
  - `ion-select` (#45): each interface opened by a real click on its select, found by the name a screen reader reads (`Alert, Pick
    one`): the alert's radio and OK, and Escape as a cancel; the action sheet's radio; the popover's radio by a click, then by the
    keys (ArrowDown moves focus and chooses nothing, Enter chooses), and the select expanded while it's open; the modal's radio
    and its header from `InterfaceOptions`; the multiple popover's checkboxes, which leave it open; and the disabled select
    disabled to automation. Every click is on an option of the interface on top. Run in the full sweep on `4383769`.
  - `ion-menu` (#44): a real click on the start menu's button (`menu`) opens it, its items within the left 304; a click on the
    backdrop and Escape close it with `backdrop`, a click on an item in a toggle with no role; a touch swipe from the left edge
    opens it, and one back closes it with `gesture`; Alt+Left closes the open menu (its back at 99) and the page stays; the end
    menu opens from its own button (`End menu`) at the right, and its toggle (`Close the end menu`) closes it; the start menu
    disabled hides its button, and enabled shows it. While a menu is open, every click is on the menu or the backdrop. Frames:
    `menu-start`, `menu-swiped`, `menu-end`. **Not yet run.**
  - `ion-split-pane` (#78): the side beside the main content at 390 with `xs`, the menu button hidden; `md` hides the side and the
    menu button opens it as an overlay; `xs` in that menu shows the side again and closes it; `Disabled` hides it, and turned off
    again from the opened menu shows it. Frames `split-pane-shown`, `split-pane-hidden`, `split-pane-menu-open`,
    `split-pane-enabled-again`. **Not yet run.**
  - `ion-toast` (#38): a real click on the trigger presents the bottom toast with its buttons near the window's bottom; Escape
    leaves it; a click on a page button beside it shows a top toast (the page isn't blocked), whose icon-only button, found by its
    `aria-label`, dismisses it with `cancel`; the bottom toast's cancel button; a button with no role (`none`); a handler returning
    false keeping the danger toast open, then its role; a real touch swipe up dismissing the middle toast and one down dismissing the
    stacked `IsOpen` toast, both with `gesture`; and a 2 s duration's `timeout`. **Not yet run.**
  - `ion-loading` (#41): a loading made in code from a real click, found by its message, its centre in the middle 40% of the
    window; Escape dismisses one with `BackdropDismiss` with `backdrop`, and so does a click on the backdrop; with no backdrop, a
    click where a page button is reaches the transparent backdrop, dismissing the loading, and the button opens nothing; the
    coloured one and `IsOpen` present and a backdrop click dismisses them; the page's loading from its trigger stays on Escape and
    its 2 s duration dismisses it with no role. **Not yet run.**
  - `ion-nav` (#130): the root loads; a click on a message link pushes it with its subject, and the inbox under it is hidden; a link
    on the pushed page pushes a draft, and back links pop to the inbox; a view model pushed through its data template; a root link
    sets a new root; the factory button pushes drafts and Pop to root goes back; a touch swipe from the left edge does nothing while
    Swipe back is off, and goes back with it on; Alt+Left pops the nav and the sample page stays, and at the root it's left to the
    shell, which goes back to the index. Each read from the page's `Stack:` line once its transition has ended. A search while a
    page leaves can fail with UIA's E_FAIL, Avalonia's (#254, `uia-findall/` below), so this section tries one once more.
    **Not yet run.**
  - `ion-back-button` (#47): five icon-only back buttons with a default href above the nav, one disabled, and none on the root
    page; a click with nothing to pop asks the app for its default href (`DefaultHrefRequested`), the disabled one asks nothing; a
    push shows the pushed page's back button in its header, and a click, automation's `Invoke` and Enter each pop the nav, asking
    for no href; a deep link (page 5 as the root) shows its button by its `DefaultPage`, and a click sets page 1 as the root. With
    the nav section's E_FAIL retry. **Not yet run.**
  - `typography`: frames only.
  - `ion-app` (#65): Alt+Left taken by the page's handler at priority 10 over the shell's 0, and with it off, the shell going back
    to the list at 0; a frame of `SetFocus`'s ring on row 1.
  - `ion-app: the touch keyboard` (#189): the page's `Input pane:` line shows a closed pane and the shims on; a touch tap on the
    last row's field, or failing that `ITipInvocation` (the taskbar's keyboard button), opens Windows' touch keyboard; the line
    then says `Open`, with the rect in the client's DIPs and on the screen. What the keyboard covers is the overlap both ways, as
    `KeyboardAssist` takes it. A keyboard beside the window has to pad nothing; the window is then moved over the keyboard
    (`SetWindowPos`, put back at the end) and the keyboard opened again, and the field has to end above the keyboard's top, 50
    above it with the content padded by what the keyboard covers when it had to move. It closes the keyboard again, and checks the
    padding goes with it, with the field still focused. Frames: `app-touch-keyboard`, `-moved`, and `-around` ones of the window and
    the keyboard together. A keyboard the pane never reports it can't tell open from closed, so it leaves it with a note. Skipped,
    not failed, when no keyboard comes or it won't cover the window. First run on `1f5064e` (2026-11-17, Windows 11 at 125%): the
    floating keyboard opened beside the window, which the shims rightly left alone, and the old checks, taking the heights alone,
    failed 2 of 7; the move over the keyboard came after, and passed in the full sweep on `4383769`.
  - `ion-content: keys and the scroll indicator` (#67): a press on a row, then Down, Home, Page Down, Space, Shift+Space and End,
    read from the page's `Ended at` line; and a strip at the end edge that a wheel changes until the indicator fades, and a key
    doesn't. Since #182, the same strip at the top differs at load from after the wheel's fade, as the thumb is there from load:
    run on `192cf20`, 13 of 13, and it fails (0 pixels) on `2f8eb1e`, before #182.
  - `ion-content: the scroll bar` (#175): the page's `Scroll bar` toggle on, then a mouse drag of the thumb, which has to scroll the
    content, and a click on the down arrow, 40 more, read from the `Ended at` line; then the toggle off. Run in the full sweep
    on `28c4288`.
  - `focus indicators` (#188): a checkbox in a multi-input item, focused by Shift+Tab then Tab, shows its circle 5 past the default
    slot's end (the item's room), and a letter clears it; a toggle in a multi-input item shows its circle beside the handle, told
    from the item's own focus overlay. Run in the full sweep on `28c4288`.
  - `ion-modal: trigger, focus trap, Escape, back, backdrop, canDismiss` (#42): the trigger, Tab staying inside, Escape and
    Alt+Left; with a nested modal open, the nested one's `Close the modal under this one` (the full-screen nested modal covers the
    parent's own Close, which a real click can't reach: 6 of 10 on `a1cd601`), read from the two status lines as `cancel` and
    `parent-dismissed`; `CanDismiss` against a touch tap on the backdrop; a modal made in code. Run in the full sweep on `4383769`.
  - `ion-modal: the sheet` (#42): the trigger opens it at 0.5; four clicks on the handle cycle 0.75, 1, 0.25, 0.5; a slow finger drag
    down from the header, held still before the lift, settles at 0.25; a mouse drag up takes it to 1; a fast finger drag down
    closes it with `gesture`. Each breakpoint read from the page's `The sheet is at` line, and the sheet's top from its title, within
    12 dips of where the breakpoint puts it. Run in the full sweep on `4383769`.
  - `ion-datetime` (#46): on the sample page, every value pinned to June 2023, the first calendar shows June; a real click on
    `Tuesday, June 20` confirms it at once with the time kept; `Next month` scrolls to July; a click on July 3, then Right and Enter,
    selects July 4; the time button sits in the right half of its row and opens its popover under it, where an hour confirms 15:47
    and Escape closes it; a touch swipe left on the calendar moves to August; then the datetime with buttons: `Done date` with no
    change confirms its value, and a day then `Cancel date` raises `ionCancel`. Days are found by their accessible names. Run by
    the main session on `1f39770` with the other `ion-datetime*` sections below, 54 of 54.
  - `ion-datetime: the single grid` (#248, #258), not Ionic's: the page's toggle draws the first datetime as one grid
    (`CalendarRendering="SingleGrid"`), each month seen by its days, as the month-year text isn't in automation (the button's name is
    `Show year picker`): 30 for June, where the scrolled calendar has about 91. Next and Previous fade to July and back
    (a frame and a pixel note about 60 ms in); a touch swipe left goes to August and a mouse drag right back to July; a short slow
    drag changes nothing, and no swipe selects a day; a click on July 3, then PageDown to August 3 and Up to July 27, each faded with
    the key's day focused after; the focused day's ring by pixel, Right within the month, Enter selecting July 28; and the toggle off
    bringing back the scrolled months on July. Frames: `datetime-grid`, `-fading`, `-swiped`, `-key`. Run by the main session on
    `1b37d6e`, 18 of 18, and on `1f39770`.
  - `ion-datetime: the months either side` (#248, #259): the first calendar scrolled by its own `ScrollPattern`, with no input (its
    scroller found in the raw view from a day of June up, the first that scrolls across, not the page's): halfway to July, July's
    first days and June's last are in the window; all the way, it rests on July; back to June; then Previous to May. Run by the
    main session on `1f39770`, 7 of 7.
  - `ion-datetime-button` (#79): every datetime pinned, each button found by its text: the modal datetime's date and time buttons
    side by side; `Jun 15, 2023` opens the modal with the calendar only and the button expanded, a day then `Done picking` confirms
    and the button reads `Jun 21, 2023`; `1:47 PM` opens the same modal with the time only, and `Cancel picking` closes it;
    `Jul 4, 2023` opens the popover under its button, a day confirms at once and Escape closes it; the disabled button is disabled
    to automation; the slot's and the several dates' names; the colour button collapsed while its datetime is out of view and
    expanded once a wheel brings it in. Run by the main session on `1f39770`.
- **480 checks** in 41 sections run on `docs/rollup-final` `96f263f` (2026-11-22), the roll-up of every component, all 480 passing.

**Look at the frames as well.** It saves a frame of each page, and most of what's wrong with a page isn't something it checks.

Proved on a clone with three bugs put back: the fixed slot measuring its children at zero, a nested `ScrollViewer`, and no
`FocusVisible` on a card. It failed on all three. The P2 sections found that no mouse could open an `ion-item-sliding`: Windows
sends a mouse move when the capture changes, which reached the gesture with no delta (see `docs/infra/gesture/README.md`).

<img src="example.png" alt="A sticky divider, stuck at the top in a real window" width="300">

## What it can't tell you

- **Touch** goes through Windows' touch injection, which is the real path through the OS, but it isn't a finger or a phone.
- **Scale** is whatever this PC's display is set to.
- **Windows only.** CI doesn't run it, as CI has no display.

The frames come from the screen, not `PrintWindow`, which offsets this window by its invisible border, and that looks like a bug
in the app.

## `uia-findall/`: a UIA search while the tree changes (#254)

The repro for an Avalonia issue (`docs/infra/avalonia-issues.md`, "A UIA search fails with E_FAIL when a subtree it's in leaves
the tree"), kept for when it's reported, and to check a new Avalonia against. `UiaRepro` is a window of pages in an `AvonicNav`
or a plain `Panel`, with Avalonia's UIA provider hooked (MonoMod, so net8.0) and each call logged; `UiaClient` changes the pages
through `Invoke` only and searches the window while they change, through the managed client or `IUIAutomation`, logging every
HRESULT; `run.ps1` builds both and the sample and runs each scenario, about 15 minutes. Neither project is in `Avonic.sln`.
Like `drive.ps1`, it brings windows to the front, so take the desktop's lease first:

```
pwsh -NoProfile -File tools/real-window/uia-findall/run.ps1 [-Only 'hide-*,fix-*'] [-Rounds 24]
```
