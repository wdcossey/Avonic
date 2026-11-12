# Avonic — CLAUDE.md

**Avonic** is a faithful recreation of [Ionic Framework](https://ionicframework.com/)'s components
for [AvaloniaUI](https://avaloniaui.net/), mobile/tablet first. Behaviour is recreated from Ionic's
implementation; visuals recreate Ionic's **md** mode as a single, fully restylable skin.

This file states **only what exists**. Plans live in `docs/ROADMAP.md`; process in
`docs/PROCESS.md`; why v1 was discarded in `docs/LESSONS_LEARNED.md`; decisions in `docs/decisions/`.

---

## Persona

Principal engineer on an AvaloniaUI control library. You care about fidelity to the reference,
correctness on touch devices, and never declaring something that doesn't work. You read the
Ionic implementation before writing a line, and you build and test before every commit.

---

## Repository

```
src/Avonic.Components/   controls: behaviour, properties, events, structure — zero visual opinion
  Controls/<Name>/       one folder per control: class, enums, functional stub ControlTheme (.axaml)
  Controls/Flex/         FlexRow and FlexSlot: display: flex and <slot>, for the parts in a host's template, with a gap, padding,
                         justify-content and a basis that shrinks to min-width (the OTP's group); MinContent, the
                         min-content width both FlexRow and the grid's RowPanel use; LabelledControlPanel, the label and the
                         control's part of a checkbox, a radio and a toggle, for labelPlacement, justify and alignment
  Controls/TextField/    what ion-input, ion-textarea and ion-searchbar share: NativeTextField (the native field kept in step
                         with Value, maxlength 0, the keyboard, debounce), LabelledTextField (the input's and the textarea's
                         label, automation and counter), FieldConverters for their themes, IInputHost for their slotted
                         margins, and the one-line native TextBox's ControlTheme
  Controls/List/         AvonicList, and in AvonicList.StuckDivider.cs the copy it draws of a sticky AvonicItemDivider whose row
                         its virtualising panel has recycled (for items that are data, not controls)
  Behaviors/             Ionic's utilities as attached behaviours (NativeButton = what a native <button> does, for the controls with one;
                         AccessibleText = what a browser names a button from; TextContent = textContent, for a label to hide;
                         Activatable = tap-click; FocusVisible = focus-visible; IonStyle = the internal ionStyle event;
                         ColorPalette = the `color` prop, see docs/infra/color-system/README.md;
                         Css = var(), em, padding and transform values for themes;
                         BorderRadius = border-radius with percentages, for themes;
                         HiddenChainWatcher = waits for display: none on a control or an ancestor to end;
                         StyleValue = a value the control's code gives a property at style priority, or nothing;
                         KeepFocus = a pointerdown's preventDefault(), so that a press moves no focus)
  Gestures/              Ionic's utils/gesture: Gesture.Create (createGesture), GestureController (priority, capture,
                         blockers); capture is Avalonia's pointer capture, and an undecided gesture keeps a ScrollViewer
                         waiting; Gesture.StopsTouchStart (a touchstart stopped on its way) and
                         GestureConfig.ReleaseAgainstDirection (docs/infra/gesture/README.md)
  Overlays/              Ionic's utils/overlays: AvonicOverlay (present, dismiss, events, stacking, focus trap, Escape and back),
                         hosted per TopLevel in its OverlayLayer (docs/infra/overlays/README.md); Present() with no TopLevel
                         finds the current activity's under Android's MainViewFactory (OpenTopLevels, the last one still open)
  Animation/             Ionic's createAnimation: AvonicAnimation on AvonicConfig's clock; CubicBezierEasing, CSS's cubic-bezier
                         as Chromium solves it, and {a:CubicBezier} for it in XAML; CubicBezier, Ionic's cubic-bezier.js;
                         SmoothScroll, Chromium's scrollTo({ behavior: 'smooth' }) on either axis; CssTransition, a CSS
                         transition of one number with a timing function
  AvonicConfig.cs        Ionic global config equivalents; internal clock/scheduler hooks for tests
  Icons/                 generated: all of ionicons, compressed, for AvonicIcon (tools/ionicons/generate.js)
  AvonicComponentsStyles.cs   merges every discovered Controls/**/*.axaml; AvonicTheme includes it
src/Avonic.Themes/       md-mode templates + resource keys (one per Ionic CSS variable); AvonicTheme
                         merges every discovered Tokens/**/*.axaml, Controls/**/*.axaml and Styles/**/*.axaml
  Tokens/Colors.axaml    Ionic's palette; Tokens/Global.axaml: global --ion-* variables
  Tokens/Palettes/       merged by the consumer, not the theme (Dark.axaml)
  Styles/                <Styles> files for ::slotted rules, which a ControlTheme can't reach
  Css/Typography.axaml   opt-in, merged by the app's Resources, not the theme: Ionic's typography.css, a ControlTheme for
                         TextBlock's h1 to h6, and the line height a label's and card content's headings read
                         (docs/infra/typography/README.md)
src/Avonic/              umbrella NuGet package — references both, no code
tests/Avonic.Components.Tests/   Avalonia.Headless (Skia) + xUnit v3 behaviour tests; frame capture for the compare step;
                                 Performance/ holds the style budget (docs/infra/performance/README.md)
samples/Avonic.Sample/   the sample, a library its heads share: SampleShell (the page list, a page, Back), one page per
                         control, each marked [SamplePage], and a device page; a Debug build shows what each page cost
                         to open (Diagnostics/)
samples/Avonic.Sample.Desktop/   the desktop head: Program.cs, opening MainWindow, a phone-sized window
samples/Avonic.Sample.Android/   the Android head, not in Avonic.sln, as it needs the android workload; CI's android job
                         builds the APK. Run on a Pixel 8 (docs/infra/android/README.md)
Directory.Build.targets  generates XamlManifest.Paths from a project's AvonicDiscoveredXaml globs
tools/ionic-api.js       core.json → fidelity checklist (the spec for every control)
tools/ionic-ref/         renders the real Ionic component in headless Chromium (md, phone viewport):
                         screenshots, shadow DOM, boxes, resolved CSS vars → docs/components/<tag>/ref/;
                         compare-icons.js compares every icon against it, pixel by pixel; side.js puts two
                         frames side by side
tools/ionicons/          generates src/Avonic.Components/Icons/ from the ionicons package
tools/fonts/roboto/      Roboto 400, 500 and 700 (OFL): the sample and the tests embed it (Avonic.Sample.Roboto), and render.js and
                         boxes.js load it, so that text measures the same on both sides. The libraries don't ship it
tools/breakage/          breaks each behaviour in a copy of the repo, and checks that a test notices
tools/real-window/       drives the real sample window with real input and checks it; takes over the PC, so ask first
docs/                    PROCESS, ROADMAP, decisions, per-component plan/comparison/docs; infra/swarm-brief.md
                         for a batch of agents, infra/avalonia-issues.md for what to report to Avalonia
.mcp.json                Claude Code MCP servers: avalonia-docs (Avalonia's hosted Build MCP — docs and API lookup)
```

A control adds only its own files; templates, tokens and sample pages are auto-discovered
([convention](docs/infra/auto-discovery/README.md)).

Stack: libraries target **net8.0**; tests and the sample target **net10.0** (xunit v3 needs the SDK's apphost, and Avalonia 12
on Android needs net10.0), the Android head **net10.0-android**;
built with the **.NET 10 SDK** (`global.json`). **Avalonia 12.1.4**, **xunit v3** — versions are
central in `Directory.Packages.props`; warnings are errors. Solution file is `Avonic.sln`
(classic format — `.slnx` needs SDK 9.0.200+).

## Components

| Ionic | Avonic | Status |
|---|---|---|
| `ion-ripple-effect` | `AvonicRippleEffect` (+ `Behaviors.Activatable` = Ionic `tap-click`; `AvonicConfig.Animated` / `.RippleEffect`) | done (signed off 2026-10-15) — [docs](docs/components/ion-ripple-effect/README.md) |
| `ion-icon` | `AvonicIcon` (`AvonicIcon.AddIcons` = Ionic `addIcons`) | done — [docs](docs/components/ion-icon/README.md) |
| `ion-item` | `AvonicItem` (+ `IItemControl`, how a control tells an item about itself) | done — [docs](docs/components/ion-item/README.md) |
| `ion-label` | `AvonicLabel` | done — [docs](docs/components/ion-label/README.md) |
| `ion-note` | `AvonicNote` | done — [docs](docs/components/ion-note/README.md) |
| `ion-text` | `AvonicText`, and `AvonicTextSpan` for words in a sentence | done — [docs](docs/components/ion-text/README.md) |
| `ion-list` | `AvonicList` | done — [docs](docs/components/ion-list/README.md) |
| `ion-list-header` | `AvonicListHeader` | done — [docs](docs/components/ion-list-header/README.md) |
| `ion-item-divider` | `AvonicItemDivider` | done — [docs](docs/components/ion-item-divider/README.md) |
| `ion-item-group` | `AvonicItemGroup` | done — [docs](docs/components/ion-item-group/README.md) |
| `ion-button` | `AvonicButton` (+ `ButtonInner`, its template's inner part; `Command`, `TextTransform` as extensions) | done — [docs](docs/components/ion-button/README.md) |
| `ion-content` | `AvonicContent` | done — [docs](docs/components/ion-content/README.md) |
| `ion-card` | `AvonicCard`, `AvonicCardHeader`, `AvonicCardTitle`, `AvonicCardSubtitle`, `AvonicCardContent` | done — [docs](docs/components/ion-card/README.md) |
| `ion-header` | `AvonicHeader` | done — [docs](docs/components/ion-header/README.md) |
| `ion-footer` | `AvonicFooter` | done — [docs](docs/components/ion-footer/README.md) |
| `ion-toolbar` | `AvonicToolbar` | done — [docs](docs/components/ion-toolbar/README.md) |
| `ion-title` | `AvonicTitle` | done — [docs](docs/components/ion-title/README.md) |
| `ion-buttons` | `AvonicButtons` | done — [docs](docs/components/ion-buttons/README.md) |
| `ion-grid` | `AvonicGrid` | done — [docs](docs/components/ion-grid/README.md) |
| `ion-row` | `AvonicRow` (+ `RowPanel`, the row's panel) | done — [docs](docs/components/ion-grid/README.md) |
| `ion-col` | `AvonicCol` | done — [docs](docs/components/ion-grid/README.md) |
| `ion-spinner` | `AvonicSpinner` (`AvonicConfig.Spinner` = Ionic's `spinner` config) | done — [docs](docs/components/ion-spinner/README.md) |
| `ion-badge` | `AvonicBadge` | done — [docs](docs/components/ion-badge/README.md) |
| `ion-avatar` | `AvonicAvatar` | done — [docs](docs/components/ion-avatar/README.md) |
| `ion-thumbnail` | `AvonicThumbnail` | done — [docs](docs/components/ion-thumbnail/README.md) |
| `ion-img` | `AvonicImg` | done — [docs](docs/components/ion-img/README.md) |
| `ion-skeleton-text` | `AvonicSkeletonText` | done — [docs](docs/components/ion-skeleton-text/README.md) |
| `ion-progress-bar` | `AvonicProgressBar` | done — [docs](docs/components/ion-progress-bar/README.md) |
| `ion-chip` | `AvonicChip` (+ `Click`, from `Behaviors.NativeButton`, and `Command`, as extensions) | done — [docs](docs/components/ion-chip/README.md) |
| `ion-backdrop` | `AvonicBackdrop` (with the [overlay system](docs/infra/overlays/README.md)) | done — [docs](docs/components/ion-backdrop/README.md) |
| `ion-checkbox` | `AvonicCheckbox` | done — [docs](docs/components/ion-checkbox/README.md) |
| `ion-toggle` | `AvonicToggle` | done — [docs](docs/components/ion-toggle/README.md) |
| `ion-radio`, `ion-radio-group` | `AvonicRadio`, `AvonicRadioGroup` | done — [docs](docs/components/ion-radio/README.md) |
| `ion-range` | `AvonicRange` (+ `RangeValue`, one value or two) | done — [docs](docs/components/ion-range/README.md) |
| `ion-input`, `ion-input-password-toggle` | `AvonicInput`, and `AvonicInputPasswordToggle` for `ion-input-password-toggle` | done — [docs](docs/components/ion-input/README.md) |
| `ion-segment`, `ion-segment-button`, `ion-segment-view`, `ion-segment-content` | `AvonicSegment`, `AvonicSegmentButton`, `AvonicSegmentView`, `AvonicSegmentContent` | done — [docs](docs/components/ion-segment/README.md) |
| `ion-tabs`, `ion-tab`, `ion-tab-bar`, `ion-tab-button` | `AvonicTabs`, `AvonicTab`, `AvonicTabBar`, `AvonicTabButton` (standalone, with no router) | done — [docs](docs/components/ion-tabs/README.md) |
| `ion-fab`, `ion-fab-button`, `ion-fab-list` | `AvonicFab`, `AvonicFabButton`, `AvonicFabList` | done — [docs](docs/components/ion-fab/README.md) |
| `ion-accordion`, `ion-accordion-group` | `AvonicAccordion`, `AvonicAccordionGroup` | done — [docs](docs/components/ion-accordion/README.md) |
| `ion-breadcrumbs`, `ion-breadcrumb` | `AvonicBreadcrumbs`, `AvonicBreadcrumb` | done — [docs](docs/components/ion-breadcrumbs/README.md) |
| `ion-infinite-scroll`, `ion-infinite-scroll-content` | `AvonicInfiniteScroll`, `AvonicInfiniteScrollContent` | done — [docs](docs/components/ion-infinite-scroll/README.md) |
| `ion-textarea` | `AvonicTextarea` (+ `TextareaNativeInput`, its native `<textarea>`: `Rows`, `Cols`, `AutoGrow`, Chromium's `line-height: normal`) | done — [docs](docs/components/ion-textarea/README.md) |
| `ion-searchbar` | `AvonicSearchbar` | done — [docs](docs/components/ion-searchbar/README.md) |
| `ion-input-otp` | `AvonicInputOtp` (+ `InputOtpBox` and `InputOtpSeparator`, the boxes it makes in a `FlexRow`) | done — [docs](docs/components/ion-input-otp/README.md) |
| `ion-item-sliding`, `ion-item-options`, `ion-item-option` | `AvonicItemSliding`, `AvonicItemOptions`, `AvonicItemOption` (+ `Click`, `Command` and `TextTransform` on the option, as extensions; `Disabled` turns only the swipe off) | done — [docs](docs/components/ion-item-sliding/README.md) |
| `ion-reorder`, `ion-reorder-group` | `AvonicReorder`, `AvonicReorderGroup` (`Disabled`, true by default as Ionic's, turns only the drag off; `Complete(list)` reorders a list) | done — [docs](docs/components/ion-reorder/README.md) |
| `ion-refresher`, `ion-refresher-content` | `AvonicRefresher`, `AvonicRefresherContent` (md's native refresher; Ionic's legacy one isn't here) | done — [docs](docs/components/ion-refresher/README.md) |
| `ion-picker`, `ion-picker-column`, `ion-picker-column-option` | `AvonicPicker`, `AvonicPickerColumn`, `AvonicPickerColumnOption` (+ `PickerColumnPanel`, the column's snapping panel; `HorizontalContentAlignment` on the column, as an extension) | done — [docs](docs/components/ion-picker/README.md) |

Every row has completed every step in `docs/PROCESS.md`. Rows are added in a roll-up at the end of
each priority tier, not by the control's own PR, so a recently finished control may be missing. For
the latest, see the `Status:` line in `docs/components/<ion-tag>/README.md`
([process](docs/PROCESS.md#rolling-up-the-tables)).

---

## Ionic reference material

Fetched by `npm install` (never committed):

| What | Path |
|---|---|
| **API spec** — every prop, event, method, slot, part, CSS var | `node_modules/@ionic/docs/core.json` |
| **Implementation** — readable Stencil output | `node_modules/@ionic/core/dist/collection/components/<name>/<name>.js` |
| **Base skin** we recreate | `node_modules/@ionic/core/dist/collection/components/<name>/<name>.md.css` |
| Icons | `node_modules/ionicons/dist/svg/*.svg`; `ion-icon` itself is in `node_modules/ionicons/dist/collection/components/icon/`, not `@ionic/core` or `core.json` |

`node tools/ionic-api.js <ion-tag>` prints the spec as a Markdown checklist. **Start every control
from that output.** `node tools/ionic-ref/render.js <ion-tag> [--tap]` renders the real component
and writes the evidence used in the plan and the compare step. The `src/` directory does not exist
in the published npm package — do not look for `.tsx` or `.scss` files.

---

## Mapping rules (mechanical — never improvise)

| Ionic | Avonic | Example |
|---|---|---|
| `@Prop() fooBar` | `StyledProperty` `FooBar`, same name | `labelPlacement` → `LabelPlacement`; `pin` → `Pin` (not `ShowPin`) |
| `disabled` | `IsEnabled` (Avalonia built-in) | |
| A `disabled` that turns off only a gesture, and leaves the children enabled | A `Disabled` property with Ionic's default, as `IsEnabled` would disable and fade every descendant | `AvonicReorderGroup.Disabled` (default true), `AvonicItemSliding.Disabled` (default false, and `IsEnabled` false stops the swipe too) |
| `name` | `<Control>Name`, because `StyledElement.Name` exists | `ion-icon` → `IconName` |
| `color` | `string? Color` via `ColorPalette.ColorProperty.AddOwner`, resolved to `Avonic.Color.<Name>` keys | `color="danger"` → `Avonic.Color.Danger` |
| `.ion-color`, `--ion-color-base` | pseudoclass `:ion-color`, `ColorPalette.Base` (also `Contrast`, `Shade`, `Tint`, `BaseColor`, `ContrastColor`) | `:host(.ion-color)` → `^:ion-color` |
| `mode` | not exposed (ADR 0001) | |
| `@Event() ionFoo` | routed event `IonFooEvent`, CLR event `IonFoo` — prefix kept, no collisions with Avalonia members | `ionChange` → `IonChange` |
| `@Method() foo()` | public method `Foo()`; `Promise<T>` → `T` (sync) unless genuinely async | `close()` → `Close()` |
| default slot | `Content` | |
| Many children, with no slot in `core.json` | `Items` | `ion-list` → `AvonicList.Items` |
| named slot `foo` | `FooContent` (+ `FooContentTemplate`) | `start` → `StartContent`; `icon-only` → `IconOnlyContent` |
| `part="foo"` | template element `PART_Foo` | `native` → `PART_Native` |
| host class from a prop | pseudoclass `:<prop>-<value>` | `button-solid` → `:fill-solid` |
| `--ion-color-primary` | `Avonic.Color.Primary` (`Color`) + `Avonic.Color.PrimaryBrush` | keep Ionic's `-contrast` / `-shade` / `-tint` names |
| `--ion-foo` | `Avonic.Foo` | a literal colour is `Color` + `Avonic.FooBrush` |
| `--ion-foo` falling back to another variable | `Avonic.FooBrush` only, following that variable | `--ion-item-background` → `Avonic.ItemBackgroundBrush` |
| `--foo-bar` on `ion-item` | `Avonic.Item.FooBar` | `--inner-padding-start` → `Avonic.Item.InnerPaddingStart` |
| `--foo` falling back to something else | key set to `<b:CssUnset />`, read through `Css.Var` with the fallback as the next value | `--color-hover: var(--color)`; `currentColor` is the host's `Foreground` |
| `1.25em` | `Css.Em` on the font size | `--detail-icon-font-size` → factor `1.25` |
| `color: inherit` | `$parent[Control].(TextElement.Foreground)`, as the last value of a `Css.Var` | A binding that yields nothing gives the default, not the inherited value |
| `translateY(50%) scale(0.75)` | `Css.TranslateY` `0.5` and `Css.Scale` `0.75`, read by `Css.TranslateScale` with `Bounds.Height` | Ease those two, not the transform: a change of size moves it at once in CSS |
| `calc(0px - var(--x))` | `Css.Negate`, the first set value negated; for a `Thickness`, the `ConverterParameter` names its side | `ion-range`'s knob handle, `margin-inline-start: calc(0px - var(--knob-handle-size) / 2)` |
| CSS specificity | a rule with a pseudoclass or class beats one without, then later beats earlier | Global `Styles` beat a `ControlTheme`, and files in `Styles/` merge in ordinal order |
| `:host-context(.foo)` | a descendant selector, in `Styles/` | `:host-context(.item-input)` → `c\|AvonicItem:item-input c\|AvonicLabel` |
| `:host-context(.item)` | `:is(c\|IItemHost) c\|Foo`, which an item, a divider and a group implement, and a list header doesn't. A type, so that it adds no activators and doesn't rise over an app's plain style, as a pseudoclass would | `Styles/Label.axaml` |
| A `::slotted` rule that `input.md.css` and `textarea.md.css` both have, word for word | `:is(c\|IInputHost) /template/ ContentPresenter#PART_Start > :is(Control)`, in `Styles/TextField.axaml`, with each host's own rules after it in the same file | the slotted margins |
| A `::slotted(ion-foo)` rule that `item.md.css` and `item-divider.md.css` both have, word for word | `:is(c\|IItemHost) /template/ ContentPresenter > c\|AvonicFoo`, one rule for both hosts, in `Styles/Foo.axaml`, unless it sets `Margin`: then a copy in `Styles/Item.axaml` and `Styles/ItemDivider.axaml` each, after the host's rule for any child. A group has no slots, so it costs nothing there | `::slotted(ion-note)` → `Styles/Note.axaml`; `::slotted(ion-avatar)` → `Item.axaml` and `ItemDivider.axaml` |
| `::slotted([slot=start])` setting one side, beside a slotted control's own margin | A rule for that control in the host's `Styles/` file, after the host's rule for any child, with no activator, so an app's plain style wins. Tests merge the control's file first and last, and add an app's plain style | `Item.axaml`: a chip's `4,4,16,4`, an avatar's `0,8,16,8`. Beside a stacked label the rule needs an activator, and the app's style loses: record it |
| `z-index` on a slotted child, over a part outside the slot | The part goes before the slots, as `ZIndex` only sorts siblings | `::slotted(ion-button)` → `PART_Ripple` before `PART_Layout` |
| A class the app sets | a style class of the same name | `class="ion-text-wrap"` → `Classes="ion-text-wrap"` |
| `<h1>`…`<h6>`, `<p>` | `TextBlock` with that style class | `<TextBlock Classes="h2" />` |
| A CSS rule for every descendant, on text | `>`, and `> :is(Panel) >`, not a descendant selector | A rule for a `TextBlock` is checked for every `TextBlock` in the app, and a descendant selector walks up its ancestors |
| Commas between selectors | Each alternative costs every element in the app about 30 ns, so prefer fewer. `StyleBudgetTests` holds the total | `> :is(Panel) >` is a second alternative for each target |
| `:host-context(.foo)` in a `ControlTheme` | Not possible: `c\|Host ^` compiles, ignores what's left of `^`, and applies everywhere | Use `Styles/` |
| `::slotted(ion-foo[slot=start])` | `c\|AvonicHost /template/ ContentPresenter#PART_Start > c\|AvonicFoo`, in `Styles/` | `>` means direct children, as `::slotted` does |
| `.ion-focusable`, `.ion-focused` | `FocusVisible.IsEnabled`, `:ion-focused` | |
| `outline: none` | `FocusAdorner` set to `{x:Null}` in the theme | Unset falls back to the base theme's dashed rectangle. It reads null either way, so a test has to check the `AdornerLayer` |
| `.ion-activatable:hover` | `:ion-hover`, from `Activatable`, for mouse and pen only | not `:pointerover`, which also applies during a touch |
| A native `<button>`, with no Ionic member | `Behaviors/NativeButton`, and an extension `Click`, not `IonClick` | On release, every press, and an element's own DOM `click`, with no keys: `AvonicChip`. A mouse has no movement limit, and a finger has 15 px. A child that handles the press stops it, except a stock `Button`, and so does a handled `Click` from inside. A child's `Tapped` doesn't. With `StockButtonsClick`, for a control whose click is the plain DOM `click`, a stock `Button` inside that clicks from a key clicks it too, and one that clicks in a press, once: `AvonicChip`, `AvonicBreadcrumb`'s indicator. A control around hears every native button's click as `NativeButton.ClickInsideEvent`: the accordion's header |
| Binding a button-like control to a command, with no Ionic member | extensions `Command`, `CommandParameter`, following Avalonia's `Button` | on `AvonicButton`, and `AddOwner` on `AvonicItem`, `AvonicCard` and `AvonicChip`, where they need `Button`; always on the chip |
| string-union prop `'a' \| 'b'` | enum `<Component><Prop>` | `ion-ripple-effect` `type` → `RippleEffectType { Bounded, Unbounded }` |
| A value of a string union that no md rule styles | Kept, with Ionic's host class as its pseudoclass, and no md rule on it | `TitleSize.Large` → `:size-large`; `HeaderCollapse.Fade` → `:collapse-fade` |
| A prop that resolves differently by context (`fill = inToolbar ? clear : solid`) | A setter in `Styles/` on the pseudoclass for that context, so a value set on the control wins and its own pseudoclasses follow | `c\|AvonicButton:in-buttons { Fill: Clear }` |
| visual constant with **no** CSS variable | a literal in the theme, so that it can be restyled | a card's margin of 10 |
| a constant a control's code needs | a constant in `Avonic.Components` | ripple opacity 0.16, 225 ms expand |
| `:last-of-type` | `:nth-last-child(1)`, and `ContentPresenter:nth-last-child(1) > c\|AvonicFoo` for templated items | Not of-type: Avalonia has no such selector |
| `:only-child` | `:nth-child(1):nth-last-child(1)` | `:nth-last-child(1)` alone is also the last of several |
| `.foo + .bar` | a pseudoclass the control works out from its parent's `Children`. As a row of a virtualising panel, whose children are only what's realised: the row before through the panel's owner's `IndexFromContainer` and `ContainerFromIndex`, read from `LayoutUpdated`, as the panel gives a row its index only at the end of the pass; a row before that isn't realised is built from its template, with its item, to read it, and not shown | `.list-inset + ion-list.list-inset` → `:after-inset`, `AvonicList` in a `VirtualizingStackPanel` too; `ion-card-header + .card-content-md` → `:after-card-header` |
| `position: sticky` | `RenderTransform` from `LayoutUpdated` at style priority, in the control. As a row of a virtualising panel, which recycles the row once its place is above the top: the list draws a second container for the item, made by its own `PrepareContainerForItemOverride`, until the row is realised again; rows never realised are built from the template, with their item, to find the section's divider, up to 200 rows back. Not for an item that is itself a control, which can be in one place only: such a list needs data and an `ItemTemplate` | `AvonicItemDivider.Sticky`. It follows the nearest `ScrollViewer`, as CSS follows the nearest scroll container. `AvonicList.StuckDivider.cs` |
| `background-clip: border-box` under a translucent border | `BackgroundSizing="OuterBorderEdge"` | CSS's default, not Avalonia's |
| `hostContext('ion-foo', el)` in `render` | A pseudoclass the control works out from its visual ancestors when it's attached and when one of its own props changes, as Ionic reads it at render. Rules read the pseudoclass, not the ancestor | `AvonicButton`'s `:in-toolbar`, `:in-toolbar-color`, `:in-buttons` |
| `ionStyle` to a host other than an item | The host turns each true key into `:<host>-<key>`, kept per type of child, and handles the event, as Ionic's `stopPropagation`. `IonStyleClasses` does it | `AvonicToolbar`'s `:toolbar-title-large` |
| `role` from `hostContext` | The peer, if the control is yours. `AutomationProperties.ControlTypeOverride` in `Styles/` otherwise | `AvonicItem` in a list → `ListItem` |
| `role="heading"`, `aria-level` | `AutomationProperties.HeadingLevelProperty.OverrideDefaultValue<T>(n)`, with a peer of type `Text` | `ion-card-title` → 2 |
| An element that's inline in a sentence | An `Inline` beside the control, `Avonic<Name>Span : Span`, in a `TextBlock` | `ion-text` → `AvonicText` and `AvonicTextSpan` |
| `display: flex`, in a row | `FlexRow`, with a `<slot>` in it as `FlexSlot`, which is styled as a `ContentPresenter` | `AvonicItem`'s parts. Not `Auto` and `*` columns, which can't shrink or share |
| `flex: 1` | `FlexRow.Flex`, set on the part or the slotted child. 0 is `flex: initial` | `::slotted(ion-label:not([slot=end]))` → `c:FlexRow.Flex` in `Styles/Label.axaml` |
| `align-items` on a flex row | `VerticalContentAlignment` on its `FlexSlot`s | `.item-label-stacked` → `Top` |
| `align-self` on a slotted child | Its `VerticalAlignment`, which `FlexSlot` honours once it's set | `ion-note` → `Top`. A control aligned some way by default uses `OverrideDefaultValue`, not its theme, whose value counts as set: `AvonicButton`, `AvonicIcon` |
| `overflow: hidden` on a flex item | `ClipToBounds` set on it, as `FlexRow` reads it to take the minimum away | `.ion-text-nowrap`. A `TemplatedControl` clips by default, and that doesn't count |
| `@media (min-width: …)`, `matchMedia` | Cumulative pseudoclasses `:breakpoint-sm`, `:breakpoint-md`, `:breakpoint-lg`, `:breakpoint-xl`, each set from that width of the `TopLevel`'s client width up (a popup's: the `TopLevel` it belongs to), so a theme's later rule wins as the later query does | `ion-grid`'s padding; `ion-col`'s sizes per breakpoint |
| `display: flex; flex-wrap: wrap` around columns | `RowPanel`, which reads what each `AvonicCol` works out from its props | `ion-row` |
| A `:host(.foo)` rule that sets CSS variables for what's inside | A `ResourceDictionary` the control merges into its own `Resources` while the class is on, so an app's own key there still wins | `ion-grid.ion-no-padding` → the six `Avonic.GridColumnPadding*` keys at 0 |
| `width: Npx; max-width: 100%; margin: auto` | `MaxWidth` on a stretched control | `ion-grid[fixed]` |
| A slotted child with no height of its own, in a panel | It collapses, as `align-items: center` does in CSS. `VerticalAlignment="Stretch"` on it fills the panel | A background behind text: put the `Grid` in a `Border`, which isn't a panel |
| `::slotted(ion-foo:not(:first-child))` | `c\|Host > :is(Panel) > c\|Foo:not(:nth-child(1))` | a panel stands for the slot |
| `ion-foo.ion-color .ion-inherit-color` | `c\|Foo:ion-color c\|Bar`, with `Foreground` bound to `$parent[Control].(TextElement.Foreground)` | after `:ion-color`, which it beats |
| `--ion-safe-area-top` | `Avonic.SafeAreaTop` (`double`), 0 by default, as Avalonia pads its root view itself. The app fills them | also `Bottom`, `Left`, `Right`. `calc(var(--padding-start) + var(--ion-safe-area-left))` → `Css.SafeAreaBox`, which swaps sides in RTL. The item, the divider and the list header read them |
| `requestAnimationFrame`, `readTask` | `AvonicConfig.RequestFrame` | `TestClock` runs it at 16 ms, so that it asserts the timeline |
| Ionic's global config (`config.get('foo')`) | A static property on `AvonicConfig`, read when Ionic reads it: in `render()`, that's the first attach and each change of a prop; as a prop's default, in the constructor, at style priority, so an app's value wins; in `componentWillLoad` or `componentDidLoad`, once, on the first attach, at style priority through a `StyleValue` bound in the constructor, or with `SetCurrentValue` if the prop isn't set, where clearing it later must give Ionic's `undefined` | `spinner` → `AvonicConfig.Spinner`; `backButtonIcon` → `AvonicConfig.BackButtonIcon`, the searchbar's `CancelButtonIcon`; `toggleOnOffLabels` → `AvonicConfig.ToggleOnOffLabels` (constructor); `tabButtonLayout` → `AvonicConfig.TabButtonLayout` (first attach, style priority); `infiniteLoadingSpinner` → `AvonicConfig.InfiniteLoadingSpinner` (first attach, `SetCurrentValue`: cleared, no spinner) |
| `overflow: hidden` on a scroller's vertical axis | `Hidden`, with input blocked in the control, not `Disabled`, which resets the position | `ScrollY="False"` |
| `position: absolute` with no inset, on a slot's child | the child's alignments, at style priority, so an app's value wins. A panel that overlaps its children (a `Panel`, a `Canvas`, a `Grid` with no rows or columns) stands for a slot that takes many elements, and is left to fill it | `slot="fixed"` → `Left`, `Top`; several `AvonicFab`s in a `Panel` in `FixedContent` |
| `--foo` with a different default per host class | key set to `<b:CssUnset />`, default from an `Avonic.<Name>.Md.*` key as `Css.Var`'s last value | `--background-hover-opacity` → `Avonic.Button.Md.BackgroundHoverOpacitySolid` |
| `color` on a shadow part, inheriting the host's | `TextElement.Foreground` on the part, falling back to the host's `Foreground` | `.button-native { color: var(--ion-color-contrast) }` |
| `overflow: hidden` beside `box-shadow` | a part of its own inside the `Border`: a `Border`'s `ClipToBounds` clips its own shadow, and a `TemplatedControl` clips by default, so turn that off | `PART_Clip`, `ClipToBoundsProperty.OverrideDefaultValue` |
| `line-height: 1` | `TextBlock.LineHeight` as `Css.Em`, `ClipToBounds="False"`, and half the leading as a margin, because Avalonia keeps a short line at the top | `Css.HalfLeading` |
| `text-transform` | a property, applied to strings through the template's converter | `TextTransform` |
| `border-radius` with a percentage | a key that takes a `CornerRadius`, a number or a CSS string, read by `BorderRadius.ToCornerRadius` and `.ToClip` with the element's size; `BorderRadius.VarToCornerRadius` when the variable falls back to another | `--border-radius: 50%` on `ion-avatar`; `ion-input-otp`'s shapes. Corners are physical, so the `CornerRadius` swaps in RTL |
| `object-fit: cover` | `Stretch="UniformToFill"` | `::slotted(img)` in `ion-avatar` |
| A shape drawn by animated SVG in the shadow DOM | Drawn in code from the same config, on `AvonicConfig`'s clock, with constants in `Avonic.Components` | `ion-spinner`'s nine names |
| `createGesture`, `GESTURE_CONTROLLER` | `Gesture.Create(GestureConfig)`, `GestureController.Instance`; `ionGestureCaptured` → `IonGestureCaptured` | Capturing takes Avalonia's pointer capture, which cancels a pressed `Button` or `Activatable` surface; losing it outside the element is `touchcancel` |
| An overlay (`present`, `dismiss`, `onDidDismiss`) | A control deriving from `AvonicOverlay`; what only some overlays have is a protected override | `backdropDismiss` → the property, and `BackdropDismissEnabled` returning it |
| `createAnimation()` | `AvonicAnimation.Create()`, members in PascalCase; CSS values as strings, `var()` read from the resource by the builder | `.fromTo('opacity', 0.01, 'var(--backdrop-opacity)')` → `.FromTo("opacity", 0.01, backdropOpacity)` |
| `Promise<T>` of an overlay | `Task<T>`, completing on the dispatcher with continuations run asynchronously, as a promise's | `onDidDismiss()` → `OnDidDismiss()` |
| An overlay's generic `willPresent`, `didPresent`, `willDismiss`, `didDismiss` | `WillPresent`, `DidPresent`, `WillDismiss`, `DidDismiss` on `AvonicOverlay`; each control adds its per-control `Ion<Name>WillPresent` set | `ion<Name>WillPresent` → `Ion<Name>WillPresent`, from the first overlay control (P3) |
| `ionBackButton` at a priority | `TopLevel.BackRequested`, handled when acted on, until `ion-app` (#65) brings the registry | an overlay's 100 |
| A text node beside slotted elements | Not counted for `:nth-child`: the host's `IChildIndexProvider` skips an item shown as its text | `AvonicChip` |
| A host that wraps every item in a slot | Clear `ContentTemplate` on the slot for a control, as a stock `ItemsControl` doesn't wrap a control | `AvonicChip` |
| A CSS transition on a value the control computes | `Animation/CssTransition`, internal, with the timing function: on `AvonicConfig`'s clock, from the value shown, with CSS's reversing shortening factor through the timing function, and none before the control's first frame | `AvonicProgressBar`'s progress and track (`transition: transform 150ms linear`); the reorder group's `ease` and the sliding item's `cubic-bezier(0.36, 0.66, 0.04, 1)` |
| `cubic-bezier(…)` on a CSS transition or animation | `CubicBezierEasing`, and `Easing="{a:CubicBezier 0.4, 0, 0.2, 1}"` in XAML, not `Easing="0.4,0,0.2,1"`, which Avalonia reads as its `SplineEasing`, about 2e-3 out | `ion-label`'s float, `ion-button`'s shadow, the ripple |
| `display: none` on the control or an ancestor stops its CSS animations | Stop the frames when a frame finds it hidden, and restart from 0 when every hidden visual in its chain shows. A hide and show between two frames restarts nothing, as in Chromium | `AvonicProgressBar`, `AvonicSkeletonText`, with `HiddenChainWatcher`. `AvonicSpinner` differs: it restarts on any show |
| `IntersectionObserver` with no `rootMargin` | `EffectiveViewportChanged`, listened to only while waiting, an edge counting. Ask for an arrange when starting, as a new listener hears nothing until a layout pass; watch the hidden chain with `HiddenChainWatcher`, as showing raises nothing; check again after a pass that changed the size. A viewport of no width and no height intersects nothing | `AvonicImg` |
| An `<img src>` | `AvonicImg.Loader`, internal: `http(s)`, `data:` base64, `avares:`, `file:`, paths, read and decoded on the thread pool; tests replace it | `ion-img` |
| What a browser draws for an `<img>` with no picture (the broken-image icon, the alt text, the box) | Parts of the template, which the control shows when Chromium shows its own, measured; the icon as path data in the theme, Avonic's own drawing, with `Avonic.<Name>.*Brush` keys. An inline icon is an empty inline space with the icon drawn over it, as an embedded control with a resource is measured during the render | `AvonicImg`'s `PART_Alt`, `PART_Broken` |
| `background-position` animated over a repeating gradient | A part one tile wider than the host, painted by the theme, moved by its `RenderTransform` on `AvonicConfig`'s clock. Physical in RTL: the part is left to right, and its move is mirrored | `AvonicSkeletonText`'s `PART_Shimmer` |
| `height: inherit` | `Height` bound to `$parent[Control].Height` in the theme | `AvonicSkeletonText` |
| `border-radius: inherit` | `$parent[Control].(Border.CornerRadius)`, which a panel doesn't have, so 0 | `AvonicSkeletonText.RadiusOrInherit` |
| `::slotted(ion-foo)` in another control's CSS, when the style budget is tight | A pseudoclass the child works out from its `Parent` when it's attached, and rules in its own `ControlTheme`, which cost nothing elsewhere and lose to an app's style as `::slotted` does | `AvonicImg`'s `:in-avatar`, `:in-thumbnail` |
| `::slotted(ion-icon) { font-size }` that changes with the host's state | `AvonicIcon.SlotFontSize`, set on the host per state by its own `ControlTheme`; an icon that is its logical child takes it at style priority, through a binding made before the icon is styled, so an app's plain style still wins. Not a pseudoclass rule in the icon's `ControlTheme`: one with a pseudoclass beats an app's plain style (measured 24 over 30) | `AvonicFabButton`'s 24, and 18 `:in-list` |
| Vertical margins between blocks collapse | Not in Avalonia: a `StackPanel` adds both. Record it; an app sets one side | `AvonicSkeletonText` |
| A slot order that differs from the DOM's (flex `order`) | The template puts the slots in visual order, and each slot is its own tab group, with a `TabIndex` in DOM order, inside a container with `KeyboardNavigation.TabNavigation="Local"` so that the indexes don't mix with the window's | `AvonicToolbar`'s md order, `PART_Container` |
| A mutable prop the user changes (`checked`, a group's `value`) | A `StyledProperty`, two-way by default, set from input with `SetCurrentValue` | `AvonicCheckbox.Checked`, `AvonicRadioGroup.Value` |
| A `@State` that styles the host (`checked`) | A pseudoclass and the automation peer, with no public property | `AvonicRadio`'s `:checked` |
| An `@internal` event between two of your controls (`ionValueChange`) | An internal CLR event | `AvonicRadioGroup.ValueChanged`, `AvonicAccordionGroup.ValueChange` |
| A prop whose name an Avalonia member already has | `<Control><Prop>`, as for `name` | `swipeGesture` → `SegmentSwipeGesture`, as `InputElement.SwipeGesture` is an event |
| `role="checkbox"`, `aria-checked="mixed"` | A peer with `IToggleProvider`: `On`, `Off`, `Indeterminate`; `Toggle` is a click | `AvonicCheckbox` |
| `aria-required`, `aria-describedby` | `AutomationProperties.IsRequiredForForm` at style priority; the peer's `GetHelpTextCore`, raising `HelpTextProperty` when it changes | `AvonicCheckbox`. `AvonicInput` and `AvonicTextarea` set `IsRequiredForForm`, `HelpText` and `Name` on their native `TextBox` instead, through `NativeFieldAutomation`, which the field binds in its own constructor, before its styles, so an app's style for it wins. A plain `TextBox` in an app's own template is bound when it's found, after its styles, which then lose |
| `ion-touched`, `ion-invalid`, set by the app | Style classes of the same name, followed through `Classes.CollectionChanged`, as Ionic's `MutationObserver` | `AvonicCheckbox` |
| `[slot=start]` on the host, read by its own CSS | `:slot-start`, `:slot-end`, from the name of the presenter it's in | `AvonicCheckbox` |
| A CSS transition in the theme, which a first style doesn't run | The theme's `Transitions` under a `:drawn` pseudoclass the control sets from its first frame, as a transition can't be taken back once it's started | `AvonicCheckbox` |
| `labelPlacement`, `justify`, `alignment` on a form control (`.xxx-wrapper`) | `LabelledControlPanel` as `PART_Wrapper`, the control's own enums mapped by name through `LabelledControlPanel.FromControl` | `AvonicCheckbox`, `AvonicRadio`, `AvonicToggle` |
| `opacity` on a control whose parts overlap | `OpacityMask` with a solid brush at that alpha, as Avalonia's `Opacity` fades each part on its own | `AvonicToggle`'s disabled 0.38; `AvonicInput`, `AvonicTextarea`, `AvonicSearchbar` |
| `isRTL(el)` | The control's `FlowDirection`, which inherits from any ancestor, where Ionic reads only the element's and the document's `dir` | `AvonicToggle`'s drag |
| A native `<input>` | A `TextBox` with a minimal `ControlTheme`, `Value` kept in step through `Text`'s property change (synchronous; `TextChanged` is posted), so only the user's edits raise the input event: `NativeTextField`, in `Controls/TextField/`, with what each control's `.js` does differently left to the control | `AvonicInput`, `AvonicTextarea`, `AvonicSearchbar` |
| An `<input>` as wide as its `size`, 20 (`width: auto` on the host) | `InputNativeInput` wants Chromium's width: `ceil(ceil(20 × x) + round(xMax − xMin) − x)`, `x` the `x` glyph's advance and `xMax − xMin` from the font's `head` table, at most what's available: within 1 px at 14 sizes. A `<textarea>` is `width: auto` there too, as wide as its default `cols`, 20, by its own formula (the `rows`, `cols` row). Both are `width: 100%`, so compressible: `MinContent` gives an `IInputHost`'s `TextBox` only its box | `AvonicInput` in an item's start or end slot: 199 at 16 px Roboto; `AvonicTextarea`: 164 |
| `<label for>` around a control | `NativeButton` with no keys on the host; a click whose path from the source holds focusable content other than the control does nothing, as HTML's label activation | `AvonicInput` |
| `pointerdown` with `preventDefault()`: a button that mustn't take focus | `Behaviors/KeepFocus.During(e)` from an ancestor's tunnel handler: every focusable element on the press's path is unfocusable until the press is handled, as Avalonia focuses in a class handler before the element's own handlers | `AvonicInput`'s clear button, `AvonicInputPasswordToggle`, `AvonicSearchbar`'s clear button |
| `mousedown`, `touchstart` with `preventDefault()` and `stopPropagation()`, acting at once | The host's `PointerPressed` tunnel handler, for a press under the part: handled, which moves no focus, then the action. Not if an ancestor handled the press on its way down | `AvonicSearchbar`'s cancel button |
| A part name that other parts carry in a state (`part="header expanded"`) | A pseudoclass on the host, as the state isn't an element of its own | `ion-accordion`'s `expanded` → `:expanded` |
| `max-height` animated on a box with `overflow: hidden` | A `Decorator` that measures its child at its own height and clips it, with `MaxHeight` eased on `AvonicConfig`'s clock | `AccordionContentClip` |
| A container that sets its children's props (`breadcrumb.active = true`) | `SetCurrentValue` from the container, so an app reads it back and a style or binding it sets later still wins | `AvonicBreadcrumbs` |
| `slotchange` | Posted to the dispatcher once for any number of changes before it runs, and only once the control has first been attached; changes while detached still count | `AvonicBreadcrumbs` |
| A child's margin as a percentage of its host's width (`calc(100% + 10px)`) | A panel of the host's that places the child, with the fixed part as the child's `Margin` in its theme | `FabPanel` |
| `visibility: hidden` | Transparent and scaled from the theme; `IsHitTestVisible`, `Focusable` and a click off from the control at style priority; offscreen to automation | `AvonicFabButton` in a closed list |
| `background-clip: padding-box` beside a `box-shadow` | Leave `BackgroundSizing` alone: `InnerBorderEdge` draws no `BoxShadow`, and nor do `OuterBorderEdge` and an uneven `BorderThickness`. A shadow on a part that has those goes on a `Border` of its own, over or under it | `AvonicFabButton`; `AvonicReorderGroup`'s `PART_Lift`, over a lifted item with full lines |
| `z-index` over something outside the parent | Not possible: `ZIndex` sorts siblings, so the ancestor that is a sibling needs it | an edge `AvonicFab` over `AvonicHeader` |
| `scrollTo({ behavior: 'smooth' })` | `Animation/SmoothScroll`, internal, along an `Orientation`: √distance / 60 s, `cubic-bezier(0.42, 0, 0.58, 1)`, measured in Chromium | the scrollable segment, the segment view (horizontal); a picker column (vertical) |
| A DOM `scroll` listener | A change of the `ScrollViewer`'s `Offset`, checked once at the next `AvonicConfig.RequestFrame`; not `ScrollChanged`, which also comes for the extent and the viewport | `AvonicInfiniteScroll` |
| Reading `scrollHeight`, `offsetHeight` (a forced reflow) | `UpdateLayout()` first, then `Extent`, `Bounds` | `AvonicInfiniteScroll`'s checks |
| Many `::slotted` rules that change with the host's state, when the style budget is tight | Inheriting attached properties that the host's `ControlTheme` sets per state, read by one rule per kind of child in `Styles/` | `TabButtonPanel.IconMargin` and the rest |
| A CSS variable a child inherits from its host (`--color` on `ion-tab-bar`, read by `ion-tab-button`) | The child's key unset, falling back to the host's key, so a resource set on the host reaches the child | `Avonic.TabButton.Color` → `Avonic.TabBar.Color` |
| A union of a number and an object (`number \| { lower, upper }`) | A struct with Ionic's name, a `TypeConverter` from text and a number, and Ionic's own reading of each form | `RangeValue` |
| Elements the component makes in `render`, one per item, with a part on each | A part that draws them, with a brush property per part. When they take input or focus, and what goes between them: types of their own with their own `ControlTheme`s, as `/template/` rules can't reach what code makes, and the host copying its state to each box as pseudoclasses | `RangeTicks` for `tick`, `tick-active`; `InputOtpBox`, `InputOtpSeparator` |
| `::slotted(ion-icon[slot])` on a host with slots | A pseudoclass the host works out from what's in each slot, read by its own `ControlTheme`, so nothing goes in `Styles/` | `AvonicRange`'s `:start-icon`, `:end-icon` |
| `rows`, `cols`, `autoGrow` on a `<textarea>` | A `TextBox` that measures `Rows` lines of its line height (2 when unset), and with `AutoGrow` as many lines as its text has, never fewer; `ceil(Cols × c) + 4`, `c` the `x` glyph's advance, or that rounded when it rounds up (fitted at 52 sizes and counts), which shows when the host is as wide as its content; `AutoGrow` drops the 4 (`overflow: hidden`) and widens to its longest line | `TextareaNativeInput`: 84, 164 and 324 for 10, 20 and 40 columns of 16 px Roboto, 133 for 20 of 13 px |
| `line-height: normal`, where lines add up | Chromium's: the font's ascent, descent and line gap, each rounded, at style priority on the text box, so an app's style wins and an inherited value doesn't | `TextareaNativeInput`: 19 for 16 px Roboto, where Avalonia's is 18.75. It rounds the descent and the gap together, the same for Roboto, whose gap is 0 |
| `display: flex; justify-content: center; gap`, with a `width` that's a basis and can shrink to `min-width` | `FlexRow` with `Spacing`, `Padding`, `HorizontalContentAlignment="Center"` (overflowing both sides) and the attached `FlexRow.FlexBasis`, as an Avalonia `Width` can't shrink | `ion-input-otp`'s group, `PART_Group` |
| `setTimeout(fn, ms)` | `AvonicConfig.Schedule(fn, TimeSpan)`, internal, which tests drive on the same clock as `RequestFrame`. A wait counted in frames (`16 * 4`) is still a timer of that many ms | `AvonicSearchbar`'s clear, 64 ms |
| A native `change` event, on Enter or blur | Raised only after a user's edit, and only if the text isn't what it was at focus or at the last change; on blur, before the control's own blur event, as Chromium does | `AvonicSearchbar`: its `onChange` raises `IonChange` before `IonBlur` after an edit |
| `box-shadow` blur `B` | `BoxShadows` blur `(B / 2 - 0.5) / 0.288675`: Avalonia's Skia takes a blur `B` as sigma `0.288675 B + 0.5`, and CSS's is `B / 2`. Only the reorder's token converts by it so far, and the refresher's by the ratio 1.56 (`6px` → 9.4, where this gives 8.66); the others hold CSS's numbers until [#139](https://github.com/wdcossey/Avonic/issues/139) settles one rule for all | `0 0 10px` → `0 0 15.6`, `Avonic.ReorderGroup.SelectedBoxShadow` |
| A class a container puts on a child (`reorder-selected`) | A pseudoclass set on the child through `IPseudoClasses`, styled per type of child; what every type needs (the z-index, a shadow) comes from the container | `:reorder-selected`; `AvonicReorderGroup`'s `ZIndex` at style priority and `PART_Lift` |
| An inline `style.transform` a component sets on a child | The child's `RenderTransform`, eased by a `CssTransition` as the CSS transition: at `Animation` priority, so the app's own value comes back when it's cleared, or local and cleared at rest | `AvonicReorderGroup` (animation priority), `AvonicItemSliding`'s item (local) |
| A module-level singleton (`let openSlidingItem`) | A static, one per process, as `GestureController` is | `AvonicItemSliding`'s open item |
| `padding-left: 100%` on a flex item, transitioned | A `Decorator` in the item's template that adds the width before (or after) its child, set by the container on each frame | `ItemOptionExpander`, from `AvonicItemOptions` |
| `progressStart`, `progressStep` on an animation of what `AvonicAnimation` can't drive (`stroke-dasharray`) | A pure function from the step to a frame, which tests pin to Chromium's values, applied to the parts on `AvonicConfig`'s clock and cleared at `destroy()`: properties at `Animation` priority, so the theme's values come back; the circle's dash, a value of its own drawing, set directly and reset to its base | `RefresherPullFrame`, applied by `AvonicRefresherContent` |
| A vertical gesture inside a vertical scroller, where a drag the other way scrolls | `GestureConfig.ReleaseAgainstDirection`: the gesture lets go of a press moving the other way at its threshold, before it captures and before the move is held from the scroller, which has followed the press, and scrolls | `AvonicRefresher`: an upward drag from the top |
| A `touchstart` listener that calls `stopPropagation()`, so an outer gesture never starts | `Gesture.StopsTouchStart` on the element, internal: a touch or pen press from it or inside it starts no `Gesture` whose element is around it; a mouse press still does, as only `touchstart` is stopped | `AvonicPicker`, over an `AvonicItemSliding` |
| `findClosestIonContent`, `getScrollElement` (`utils/content`) | `ContentUtils`, internal, in `Controls/Content/`: the closest `AvonicContent` or control with `AvonicContent.ScrollHostClass`, from the control itself; its `ScrollViewer`: the content's, the host if it is one, or the nearest one on the way up to it | `AvonicInfiniteScroll`, `AvonicReorderGroup`; `AvonicRefresher` finds its content by its fixed slot and calls `GetScrollElement` |
| `scroll-snap-type: y mandatory` on a scroller of options | A panel that is `IScrollSnapPointsInfo`, with a point at each option's centre, and `VerticalSnapPointsType="Mandatory"`; a wheel the control handles, 100 px a notch and then the nearest point, as Chromium does | `PickerColumnPanel`, `AvonicPickerColumn` |
| `elementsFromPoint` at a scroller's centre | The child whose arranged box holds the centre; nothing in a disabled column, whose options are `pointer-events: none` | `AvonicPickerColumn.OptionAtCentre` |
| A global CSS file an app opts into (`typography.css`) | A `ResourceDictionary` in `Css/`, merged by the app's `Resources`, not by `AvonicTheme`; rules on one type as a `ControlTheme` for it, so a host's global style beats it in any order, as a more specific CSS rule does, and nothing else is checked against it | `Css/Typography.axaml` |
| A value a host's rule leaves to an opt-in file (the line height of a label's `h1`) | A key the host's rule reads, set to what Chromium measures without the file, which the file sets | `Avonic.Label.HeadingLineHeight`, `Avonic.CardContent.HeadingLineHeight` |

Avonic-only additions (touch-target enforcement, extra events) are allowed but live in a separate
"Extensions" section of the control's doc and never shadow an Ionic name.

---

## Rules

1. **Build gate.** `dotnet build Avonic.sln && dotnet test Avonic.sln` green before every commit.
2. **One control or family per PR**, through every step of `docs/PROCESS.md`. Plan is written before code and reviewed at the PR.
3. **No dead props.** Declared ⇒ works ⇒ has a test that fails without it.
4. **Tests assert Ionic behaviour** (from reading `<name>.js`), not the implementation's own pseudoclasses.
5. **No visual opinion in Components.** No hex colours or `Brushes.*` in Themes outside `Tokens/`.
6. **No emoji or Unicode glyphs as icons.** Icons come from ionicons.
7. **This file is ground truth.** If it says something exists, it exists and works. Aspirations go in `docs/ROADMAP.md`.
8. **Faithful, with deviations explained.** Avalonia doesn't offer every technique html, CSS and JS do, so a deviation it forces is allowed, if it's recorded with its measurement and the reason. It isn't a licence to skip measuring, or to deviate where Avalonia could do it ([ADR 0002](docs/decisions/0002-decisions-from-the-first-swarm.md)).

---

## Attribution & licence

Apache-2.0. Avonic recreates the API, behaviour and Material Design visuals of
**Ionic Framework** (© Ionic, Inc., MIT). It is an independent implementation for AvaloniaUI and
is not affiliated with or endorsed by Ionic, Inc. or the AvaloniaUI team. This attribution appears
in `README.md` and the NuGet package description. The bundled artwork is **Ionicons** (MIT); its
notice ships at `src/Avonic.Components/Icons/LICENSE`.
