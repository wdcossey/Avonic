# ion-nav family — Plan

`AvonicNav` and `AvonicNavLink`: Ionic's stack of pages with an animated transition, and the element that pushes, pops or sets the
root of the closest nav when it's clicked ([#130](https://github.com/wdcossey/Avonic/issues/130)). `nav.js` never reads `window`,
`history` or `location`, so it isn't routing; it's the navigation contract `ion-back-button` (#47) needs: `closest('ion-nav')`,
`canGoBack()`, `pop({ skipIfBusy: true })`, and the `can-go-back` class it puts on a page that has one under it.

Measured in real Ionic 9.0.11 in headless Chromium, md, on `render.js`'s phone (390 x 844 at 2x, mobile and touch). The raw output
is [`ref/measurements.txt`](ref/measurements.txt) (M1 to M20) and [`ref/extra.txt`](ref/extra.txt) (X1 to X8), from four
throwaway Playwright scripts kept outside the repo (`measure.js`, `extra.js`, `extra2.js`, `modal.js`), each on `render.js`'s
phone with `settle.js` and the repo's Roboto; the pages are plain custom elements (`page-one`, `page-two`, ...), as an app without a
framework writes them. `ref/push-*.png` and `ref/pop-*.png` are the transition paused with the Web Animations API at the times in
their names (`frames.js`); `ref/page.png` is `render.js`'s, its command in [`ref/README.md`](ref/README.md).

**Decided on 2026-11-20**, before any code, as the issue asks ([§ Decisions](#decisions)): option B for the page forms, with
`componentProps` reaching CLR properties too; swipe back offered, off by default; hardware back as an extension, on by default;
the stack kept across a detach. The checklists below are ticked as built; the README has the final ones.

## What a page is (the decision)

Ionic's `NavComponent` is `Function | HTMLElement | ViewController | string | null`. Core builds a page in `attachComponent`: a
**string** is a tag name, made with `createElement`; an **HTMLElement** is used as it is; a **Function** needs a framework's
`delegate` (Angular, React and Vue pass one; core alone throws `framework delegate is missing`); a **ViewController** is a view
already made. `componentProps` is `Object.assign(el, componentProps)`, **before** the element is appended (measured M2: the page's
`connectedCallback` already sees the prop). So the question is what Avonic's equivalents of those four forms are, and what
`Object.assign` becomes.

How an Avalonia app would use a nav:

- **XAML pages** (`UserControl`s): `<c:AvonicNav Root="{x:Type pages:InboxPage}"/>`, and on a page
  `<c:AvonicNavLink Component="{x:Type pages:MessagePage}">`. A `Type` is built only when it's pushed: a list of 20 links each
  holding a page instance would build 20 pages up front, at 150 to 200 ms each on a Pixel 8 (#248).
- **MVVM** (CommunityToolkit.Mvvm, ReactiveUI; Avalonia's own template ships a `ViewLocator : IDataTemplate` in `App.axaml`): the
  view model is what's navigated to, and the view comes from the data templates, with the view model as its `DataContext`, as a
  `ContentControl` shows its content. Ionic's frameworks do the same through the `delegate`: React and Vue pass a component, the
  framework builds it.
- **Code, with constructor arguments or DI:** `nav.Push(() => new MessagePage(store, id))`. That is Ionic's `Function` form with
  a framework's delegate.

Whatever the form, the page is a `Control`; it gets class `ion-page` (as `AvonicTab` gives its component, so an `AvonicContent` in
it finds its page), sits in the nav stretched to fill it, and inherits the nav's `DataContext` unless the form sets one. Its
lifetime is Ionic's in all three options: built when it's first entered (`insertPages` builds only the last), kept and hidden
under the pages above it (state and scroll position kept: measured M11, 600 before, 600 after), and removed from the nav when it's
popped or replaced (`ionViewWillUnload`); a page the app made itself can be pushed again later (M12). `ion-back-button` (#47) is
the same in every option: it finds the closest `AvonicNav` among its visual ancestors and reads `:can-go-back` on its page.

### Option A — Ionic's union, as `ion-tab` already maps it

`Root`, `NavLink.Component` and the `component` argument of `Push`, `Insert` and `SetRoot` are `object?`, as `AvonicTab.Component`
is: a **`Control`** is used as it is (`HTMLElement`); a **`Type`** of control with a parameterless constructor is made at push
(the tag-name `string`); a **`ViewController`** is a view already made. Anything else throws, as `attachComponent` does.
`componentProps` is `IReadOnlyDictionary<string, object?>`, applied by name before the page is added, by the rule `interfaceOptions`
already has (`AvonicSelect.InterfaceOptions`): each key the page's Avalonia property by name, camelCase or PascalCase, a string
converted as XAML would, a key with no property ignored.

- **For:** mechanical; the same as `ion-tab`'s `Component`; smallest surface and test list.
- **Against:** no MVVM (a view model can't be pushed), no constructor arguments, and `componentProps` reaches only Avalonia
  properties: a `UserControl` page's plain CLR property is ignored, where `Object.assign` sets anything.

### Option B — A, plus a factory and view models through data templates (recommended)

A's three forms, and two more, in this order:

- **`Func<Control>`** is called at push: the `Function` form, with .NET's own constructor as the delegate. Constructor arguments
  and DI work, and it's still lazy.
- **Any other object** (a view model) is built through the nav's data templates (`FindDataTemplate`: the nav's, its ancestors',
  then `Application.DataTemplates`, where an app's `ViewLocator` is), and the view gets the object as its `DataContext`. No template
  found throws, as a missing delegate does in Ionic. This is the `delegate` path: Avalonia's data templates are the framework.

`componentProps` as A, and a public settable CLR property when there's no Avalonia property of that name, so a `UserControl`'s
plain properties work as `Object.assign` would. A view model's own state is its parameters; props given with one go to the view
built for it, as they go to the element in Ionic.

- **For:** covers how Avalonia apps are written (XAML pages, MVVM with a `ViewLocator`, DI), each lazily; `GetActive().Component`
  is the view model, which is what an MVVM app wants back.
- **Against:** a larger surface (five forms) and more tests; the CLR fallback uses reflection, which a trimmed app (the Android
  head is trimmed) can strip from a page whose property its own code never touches: documented, and Avalonia properties don't have
  that problem.

### Option C — Avalonia's content rules only

What `ContentPresenter` does with content: a `Control` is itself, anything else goes through the data templates. No `Type` form
and no factory. `componentProps` as A.

- **For:** no new rules for an Avalonia developer; the smallest bridge to MVVM.
- **Against:** a XAML link (`AvonicNavLink.Component`) can't name a page without building it, unless the app writes a data
  template per page type; `ion-tab`'s `Component` takes a `Type`, so the two would differ.

### Recommendation

**B.** It's A (Ionic's forms, and what `ion-tab` does) with the two forms Ionic's frameworks get through their delegate, mapped to
what Avalonia uses for the same job, so XAML pages, MVVM and DI each push without building anything early. If the reflection
fallback for CLR properties isn't wanted, B without it is the same as A for `componentProps`.

The rest of this plan is written for B; A and C are B with forms taken out (their tests too).

## Fidelity checklist

`node tools/ionic-api.js ion-nav`, `ion-nav-link`. Types in the Avonic column are for option B.

### ion-nav: props (5)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `animated` | `Animated` | `bool` | `true` | Off: no transition (M13). `AvonicConfig.Animated` off has the same effect, read at each transition, as `config.getBoolean('animated', true)` |
| [x] | `animation` | `Animation` | `AnimationBuilder?` | null | Used when the call's options give none; else md's. The builder gets the nav and a `NavTransitionOptions` (entering and leaving page, direction, duration, `ShowGoBack`), Ionic's `animationOpts`. `config.get('navAnimation')` is `AvonicConfig.NavAnimation` (added at review, by CLAUDE.md's config rule) |
| [x] | `root` | `Root` | `object?` (a page form) | null | Set at the first load, unanimated (`SetRoot`); a change after load calls `SetRoot(Root, RootParams)` (M15). Null does nothing |
| [x] | `rootParams` | `RootParams` | `IReadOnlyDictionary<string, object?>?` | null | Read with `Root`; a change of its own does nothing (M15) |
| [x] | `swipeGesture` | `NavSwipeGesture` | `bool?` | null | Proposed: offered. Null takes Ionic's `swipeBackEnabled` config, false in md (M20), at the first load. True turns on the edge swipe back (`swipe-back.js`), driving the md transition by its progress, as Ionic does in md when it's set. See [§ Swipe back](#swipe-back) |

### ion-nav: events (2), methods (14), slot

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionNavWillChange` | `IonNavWillChange` | Routed, `Direct` (`bubbles: false`; measured M16). Raised at the start of every transition, synchronously inside the call that starts it (M2: before the page is built), also for one that then fails (M4) |
| [x] | `ionNavDidChange` | `IonNavDidChange` | Routed, `Direct`. After the transition, and after the task's result is set, before an awaiting caller continues (M2: `ionNavDidChange` then `push resolved`). Not raised when a transition fails |
| [x] | `push(component, componentProps?, opts?, done?)` | `Push(object component, IReadOnlyDictionary<string, object?>? componentProps = null, NavOptions? opts = null, TransitionDoneFn? done = null)` | `Task<bool>`: genuinely async. `insert(-1, …)` |
| [x] | `insert(insertIndex, component, componentProps?, opts?, done?)` | `Insert(int, object, …)` | `insertPages(index, [{ component, componentProps }])` |
| [x] | `insertPages(insertIndex, insertComponents, opts?, done?)` | `InsertPages(int, IEnumerable<object>, …)` | Each item a page form or a `NavComponentWithProps`. A plain string in the array throws in Ionic: a deliberate difference (D1) |
| [x] | `pop(opts?, done?)` | `Pop(NavOptions?, TransitionDoneFn?)` | `removeIndex(-1, 1)` |
| [x] | `popTo(indexOrViewCtrl, opts?, done?)` | `PopTo(int, …)`, `PopTo(ViewController, …)` | Two overloads for `number \| ViewController` |
| [x] | `popToRoot(opts?, done?)` | `PopToRoot(…)` | `removeIndex(1, -1)` |
| [x] | `removeIndex(startIndex, removeCount = 1, opts?, done?)` | `RemoveIndex(int, int removeCount = 1, …)` | |
| [x] | `setRoot(component, componentProps?, opts?, done?)` | `SetRoot(object, …)` | `setPages([{ component, componentProps }])` |
| [x] | `setPages(views, opts?, done?)` | `SetPages(IEnumerable<object>, …)` | Unanimated unless `opts.Animated` is true (M9, M9b) |
| [x] | `getActive()` | `GetActive()` → `ViewController?` | Synchronous: Ionic's is an `async` wrapper of `getActiveSync` |
| [x] | `getByIndex(index)` | `GetByIndex(int)` → `ViewController?` | Synchronous; null out of range |
| [x] | `canGoBack(view?)` | `CanGoBack(ViewController? view = null)` → `bool` | Synchronous |
| [x] | `getPrevious(view?)` | `GetPrevious(ViewController? view = null)` → `ViewController?` | Synchronous |
| [x] | `getLength()` | `GetLength()` → `int` | Synchronous |
| [x] | `(default)` slot | none | Ionic's `<slot>` holds the pages it makes; a child the app puts in the nav itself isn't a page. The nav's pages are its children; there's no `Content` |

### Ionic's other public types and members

Not in `core.json` but part of the API an app uses:

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `NavOptions` | `NavOptions` class | `Animated`, `AnimationBuilder`, `Duration` (ms), `Direction` (`NavDirection.Back` / `Forward`), `SkipIfBusy`, `ShowGoBack`, `DeepWait`, `ViewIsReady` (`Func<Control, Task>`), `ProgressAnimation` (the swipe's). `easing` is read only by the iOS transition; `keyboardClose` by nothing; `mode` isn't exposed (ADR 0001); `delegate` is `@internal` |
| [x] | `ViewController` | `ViewController` class | `Component`, `Params`, `Element` (the page, null until it's built), `Nav`. Its state is internal. Made by the nav, or by an app to pass a view in |
| [x] | `NavComponentWithProps` | `NavComponentWithProps` record | `Component`, `ComponentProps`, for `InsertPages` and `SetPages` |
| [x] | `TransitionDoneFn` | `TransitionDoneFn` delegate | `(hasCompleted, requiresTransition, enteringView, leavingView, direction)`; called before the task completes, and with `(false, false, reason)` on a failure |
| [x] | `ionViewWillEnter`, `ionViewDidEnter`, `ionViewWillLeave`, `ionViewDidLeave`, `ionViewWillUnload` | `AvonicNav.IonViewWillEnterEvent` and the other four | Routed, `Direct`, raised on the page (`bubbles: false`), with `AvonicNav.AddIonViewWillEnterHandler(page, …)` and the rest |
| [x] | `can-go-back` on the entering page | `:can-go-back` on the page | Set before each transition when the entering view has one under it (`showGoBack`, which `opts.ShowGoBack` overrides), removed otherwise. #47's back button shows by it (`.can-go-back > ion-header ion-back-button`) |
| [x] | `ion-page` on each page | class `ion-page` | As `AvonicTab` gives its component, for `AvonicContent`'s page lookup |
| — | `setRouteId`, `getRouteId` | none | **Not in Ionic 9:** removed with `ion-router` support ("as of Ionic 9 ion-nav is no longer a router outlet", `router/utils/dom.js`). Nothing to omit |
| — | `delegate`, `ionNavWillLoad` | none | `@internal`: the framework's delegate (option B's forms play its part) and the router's wait for an outlet |
| — | `focusManagerPriority` (the transition's focus controller) | none | Experimental global config, off by default; off, the controller does nothing |

### ion-nav-link: props (4)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `component` | `Component` | `object?` (a page form) | null | Used for `Forward` and `Root`; null does nothing then (`navLink` resolves false) |
| [x] | `componentProps` | `ComponentProps` | `IReadOnlyDictionary<string, object?>?` | null | |
| [x] | `routerAnimation` | `RouterAnimation` | `AnimationBuilder?` | null | Passed as the call's `AnimationBuilder` |
| [x] | `routerDirection` | `RouterDirection` | `NavLinkRouterDirection` | `Forward` | `Back` pops, `Forward` pushes, `Root` sets the root; each with `SkipIfBusy` |

No events, methods, slots, parts or CSS variables. Its default slot (not in `core.json`) is `Content`: an `AvonicNavLink` is a
`ContentControl`.

### CSS

`nav.css` is `:host { position: absolute; inset: 0; contain: layout size style; z-index: 0 }`, and there's no `nav.md.css` and no
CSS variable: **no visual, so no theme in `Avonic.Themes`**, only the stub `ControlTheme` in Components, a panel that stretches
its pages over each other (`.ion-page`'s `position: absolute; inset: 0`). The md transition's numbers are constants in
Components (40 px, 280 and 200 ms, the two curves), as Ionic hard-codes them in `md.transition.js`; an app replaces the transition
through `Animation`. `core.css`'s `.ion-page-hidden { display: none }`, `.ion-page-invisible { opacity: 0 }` and
`.can-go-back > ion-header ion-back-button { display: block }` are the nav's and #47's to apply.

`ion-nav-link` has no CSS; its host is `display: inline` (measured).

## What Ionic does

From `nav.js`, `view-controller.js`, `constants.js`, `nav-link.js`, `nav-link-utils.js`, `utils/transition/index.js`,
`md.transition.js`, `utils/gesture/swipe-back.js` and `utils/framework-delegate.js`.

- **Load** (`componentWillLoad`, `componentDidLoad`): `swipeGesture` undefined takes `swipeBackEnabled` (md: false). Then
  `root`, if set, is `setRoot(root, rootParams)`: unanimated. A push made before then queues behind it (X5).
- **The queue** (`queueTrns`, `nextTrns`, `runTransition`): every method is an instruction queued and run one at a time.
  `skipIfBusy` while a transition runs resolves false at once and queues nothing (M5: 50 ms into a push, false at 59 ms).
  `runTransition` emits `ionNavWillChange`, normalises the instruction (`prepareTI`: negative indexes, counts, whether the top
  changes), finds the entering view, builds it if new (`ViewController.init` → `attachComponent`: classes `ion-page
  ion-page-invisible`, `Object.assign` the props, append, wait for `componentOnReady`), inserts and removes views
  (`postViewInit`), transitions if the top changes, then `success` (the `done` callback, the promise) and `ionNavDidChange`.
- **Views removed from the middle** (`postViewInit`): each gets `ionViewWillLeave`, `ionViewDidLeave`, `ionViewWillUnload` at once,
  in order, and is destroyed, before any transition (M6, M7, M9).
- **Direction:** back when the instruction removes views, else forward when the top is inserted; `opts.direction` wins (M14).
- **Back uses the entering page's animation** (`runTransition`): popping back to a view uses the builder it was pushed with,
  unless the call gives one.
- **The transition** (`transition()` in `utils/transition`): on the next frame (`writeTask`), `can-go-back` on the entering page
  as `showGoBack`, `z-index` 101 on it going forward and 99 going back, 100 on the leaving page; both pages shown and
  `pointer-events: none` (X4: a tap mid-transition reaches neither page). Then the animation: wait for the entering page and
  every Ionic component in it to be ready (`deepReady`, and `viewIsReady`), build it, `ionViewWillLeave` on the leaving page and
  `ionViewWillEnter` on the entering, play; on finish `ionViewDidEnter`, `ionViewDidLeave`, destroy the animation, take
  `ion-page-invisible` and `pointer-events` off both. With no animation (`animated` off, no leaving page, or `duration: 0`), the
  same events in one go after the wait (M13).
- **md transition** (`md.transition.js`): forward, the entering page goes from `translateY(40px)`, opacity 0.01, to 0 and 1 over
  280 ms, `cubic-bezier(0.36,0.66,0.04,1)`, and the leaving page doesn't move (M2). Back, the leaving page goes from 0 and 1 to
  40 px and 0 over 200 ms, `cubic-bezier(0.47,0,0.745,0.715)`, then `display: none`, and the entering page under it doesn't move
  (M3). `fill: both`. `opts.duration` replaces either duration (M14: 1000).
- **After** (`transitionFinish`, `unmountInactiveViews`): pages above the active one get `ionViewWillUnload` and are removed;
  pages under it get `ion-page-hidden` (`display: none`) and `aria-hidden="true"` (M2). The leaving page of a pop: `ionViewDidLeave`,
  then `ionViewWillUnload`, then `ionNavDidChange` (M3).
- **Failure** (`failed`, `fireError`): no views at all (`no views in the stack to be removed`, X6), or a stack that would be empty
  (`navigation stack needs at least one root page`, with a console warning, M4): the promise rejects, `done(false, false, reason)`,
  no `ionNavDidChange`, and **the rest of the queue is dropped** without settling (X1b, D2).
- **Destroyed** (`disconnectedCallback`): every view gets `ionViewWillUnload` and is removed, the queue is emptied, the gesture
  destroyed. Moving the nav in the DOM does it too, and the root isn't set again (`componentDidLoad` runs once): the stack is lost
  (X8), and a nav in an inline `ion-modal`, which the modal moves to `ion-app` when it's presented, shows nothing (X7, D3).
- **Swipe back** (`canStart`, `onStart`, `onMove`, `onEnd`, `swipe-back.js`): with `swipeGesture` on, a horizontal drag from
  within 50 px of the start edge (the end edge in RTL), threshold 10, gesture priority 101, starts when nothing is transitioning or
  queued and it can go back: `pop({ direction: 'back', progressAnimation: true })`, the animation driven by `deltaX / width`, and
  at the end it completes when the velocity is ≥ 0 and (> 0.2 or past half the width), over at most 540 ms, with the reversed curve
  `cubic-bezier(1, 0, 0.68, 0.28)` when it goes back. With animations off it waits for the end and pops then, blocking clicks.
- **No hardware back** (M19): `nav.js` doesn't listen to `ionBackButton`. With `hardwareBackButton: true` and a nav two deep, the
  back button raises `ionBackButton` and the nav stays where it is. In Ionic only `ion-router` (core) and the frameworks' router
  outlets go back, at priority 0.
- **`ion-nav-link`** (`navLink`): on a `click` anywhere in it (its host's `onClick`), the closest `ion-nav`: `forward` pushes
  `component`, `root` sets it as the root, `back` pops, each with `skipIfBusy` and `routerAnimation` (M17). A click on plain text in
  it navigates; Space and Enter on a native button in it click it, and so navigate (X2, X2c); a double click pushes once (the second
  is `skipIfBusy`). Its host has no `role` and no `tabindex`: it's not focusable itself (X2b).
- **`ion-back-button`** (#47, read for the contract): `closest('ion-nav')`, `canGoBack()`, then `pop({ animationBuilder:
  routerAnimation, skipIfBusy: true })`; shown in a page with `can-go-back` (M2: `display: block` on Page Two, M17 click pops).

## Measured

All in [`ref/measurements.txt`](ref/measurements.txt) and [`ref/extra.txt`](ref/extra.txt); times from the call, at 60 Hz.

| # | What | Result |
|---|---|---|
| M1 | Load with `root` | `ionNavWillChange`, page connected, `ionViewWillEnter`, `ionViewDidEnter` at +28 ms, `ionNavDidChange`. Nav 390 x 844, absolute, `z-index` 0. Root page `ion-page`, `z-index` 99. `canGoBack` false, length 1, `getActive()` `{ component: 'page-one', params: undefined }` |
| M2 | `push` | `ionNavWillChange` and the page connected (with its prop) in the call; two frames at opacity 0 (`ion-page-invisible`); `ionViewWillLeave` then `ionViewWillEnter` at +31; animation from the frame at +44 (opacity 0.010, 40 px) to +311; `ionViewDidEnter`, `ionViewDidLeave`, `ionNavDidChange`, resolve at +327. Leaving page unmoved throughout; after, `ion-page-hidden`, `aria-hidden`, `display: none`, `z-index` 100; entering `can-go-back`, `z-index` 101 |
| M3 | `pop` | Events at once (+2); leaving page 0 → 40 px, 1 → 0, 200 ms, `cubic-bezier(0.47, 0, 0.745, 0.715)`; previous page shown under it, `z-index` 99; `ionViewDidEnter`, `ionViewDidLeave`, `ionViewWillUnload`, `ionNavDidChange` at +226; popped page removed |
| M4 | `pop` at the root | Rejects `navigation stack needs at least one root page`, warning logged; `ionNavWillChange` only; stack unchanged |
| M5 | Two pushes at once, then `skipIfBusy` | Run one after the other (+313, +629); the `skipIfBusy` push resolves false at once |
| M6 | `popToRoot` from 3 | Middle page: `WillLeave`, `DidLeave`, `WillUnload` at +0; then the back transition from the top |
| M7 | `insert(1)`, `removeIndex(1)` | No transition, no enter events. The inserted page is built (it's the entering view) and left in the nav at opacity 0 under the top one, not hidden (a quirk, D4); removing it fires its leave and unload events at once |
| M8 | `insertPages(-1, [a, b])` | Only `b` built; `a` built when popped into later. A string in the array throws a `TypeError` (D1) |
| M9 | `setRoot`, `setPages` | Unanimated by default; replaced pages' events at once; `setPages(…, { animated: true })` animates back |
| M10 | `popTo(view)` | As `popToRoot`; a page with no element is built when entered |
| M11 | Scroll position | Kept under a pushed page and after the pop: 600, 600 |
| M12 | An element pushed | The same element; after a pop it's out of the DOM, and can be pushed again |
| M13 | `animated = false` | Events in one go at +29 (two frames), no animation |
| M14 | `duration`, `direction` | `duration: 1000` replaces 280; `direction: 'back'` on a push runs the back animation on the leaving page |
| M15 | `root` changed | `setRoot`, unanimated; `rootParams` alone: nothing |
| M16 | Bubbling | Neither the nav's events nor the page's reach `ion-app` |
| M17 | `ion-nav-link` | Forward, back and root by a click; a click on its text navigates; a double click pushes once; host `display: inline`, no role or tabindex |
| M18 | A page that takes 150 ms to build (script), or 400 items (layout) | The animation's first frame is its start (opacity 0.010, `currentTime` 0) on the frame after the build: +181 (build ended +152) and +133. **It never starts before the page is ready**, and no frame is skipped |
| M19 | Hardware back | `ionBackButton` raised, the nav doesn't pop |
| M20 | `swipeGesture` in md | `false` |
| X1b | A failure behind a running push, a push queued after it | The queued push never settles and never runs (D2) |
| X4 | Mid-transition tap | Both pages `pointer-events: none`; the hit goes through to `ion-app` |
| X5 | `push` before load | Queued after the root |
| X7 | In an inline `ion-modal` | Empty after the first present (D3) |
| X8 | Moved in the DOM | Every page unloaded, length 0 |

## Design

For option B. Names are Ionic's by the mapping rules; what changes for A or C is only the page forms.

- **`AvonicNav : TemplatedControl`**, its template a `Panel` part (`PART_Pages`) that overlaps the pages, as `.ion-page` does; it
  doesn't clip (no overflow rule). Pages are logical children of the nav (`DataContext`, resources and styles inherit) and visual
  children of the panel. No visual of its own: the stub theme is the whole template.
- **Views:** `ViewController` holds the component, the params and the built page. Building (`ViewController.Init`) follows the
  form: `Control` as is; `Type` by `Activator.CreateInstance`; `Func<Control>` called; anything else through `FindDataTemplate` from
  the nav, with `DataContext` set; then class `ion-page`, the props applied, `ion-page-invisible`'s opacity 0, and added to the panel.
- **The queue and the transition:** Ionic's `runTransition` as `async Task` methods on the dispatcher, the instruction a private
  class, results through `TaskCompletionSource` with continuations run asynchronously (as an overlay's `Task`s). `IonNavWillChange`
  is raised synchronously in the call, as in Ionic.
- **Ready before it moves** (M18, and #248): after the page is added, the nav waits for a layout pass that includes it (`deepReady`
  and `componentOnReady`: in Avalonia, the page's templates apply and it's measured in the first layout after it's attached), then
  the next frame (`writeTask`), then fires the will-events and plays. `AvonicAnimation` starts its clock at the first frame after
  `Play`, which comes after that layout, so the first frame shown is the start, however long the build took, as in Chromium. The
  nav records the build, the wait and the transition as diagnostics in Debug for the compare step.
- **md transition:** `AvonicAnimation` with Ionic's keyframes, easings and durations; `fill: both`, destroyed at the end, as
  `transition()` does.
- **Pages' state at style priority**, so an app's own value comes back: hidden pages `IsVisible` false (`ion-page-hidden`, which
  also hides them from automation, `aria-hidden`), `IsHitTestVisible` false during a transition (`pointer-events: none`), `ZIndex`
  101, 100 and 99 as Ionic leaves them, `:can-go-back`. Opacity and transform belong to the animation.
- **Destroyed:** see D3. Detaching doesn't destroy; the views are kept.
- **Hardware back (extension, E1):** see [§ Extensions](#extensions).
- **`AvonicNavLink : ContentControl`**, with `NativeButton` on the host and no keys, as `<label for>` is mapped: a press and
  release on it clicks, and so does a native button inside clicking (`ClickInsideEvent`, `StockButtonsClick`), as a DOM `click`
  bubbles to Ionic's host. Not focusable, no automation role of its own (`NoneAutomationPeer`), as the host has neither.

### Swipe back

Proposed as offered, as Ionic does in md when `swipeGesture` is set: `Gesture.Create` with Ionic's config (`goback-swipe`, priority
101, threshold 10, a start within 50 px of the start edge of the window, Ionic's `win.innerWidth`, read as the top level's width),
the md transition driven by `ProgressStart`, `ProgressStep` and `ProgressEnd`, with Ionic's end rule and reversed curve. The gesture
and progress API exist (`Gestures/`, the sheet modal). `swipeBackEnabled` would be a new `AvonicConfig.SwipeBackEnabled` (a shared
file, explained in the PR). If it isn't wanted, `SwipeGesture` isn't declared, and the README says why: md leaves it off.

## Behaviours to test

Each a test on `TestClock` that fails without the behaviour; names in brackets are the code and the measurement.

| # | Behaviour | Source |
|---|---|---|
| N1 | `Root` at the first attach: built unanimated, `IonNavWillChange`, `IonViewWillEnter`, `IonViewDidEnter`, `IonNavDidChange`; the page has class `ion-page`; length 1, `CanGoBack` false | `componentDidLoad`, `rootChanged`; M1 |
| N2 | `Root` changed after load sets the root, unanimated, with `RootParams`; `RootParams` alone does nothing | `rootChanged`; M15 |
| N3 | Each page form builds as specified (`Control`, `Type`, `Func<Control>`, view model through a data template with `DataContext`, `ViewController`); an unknown form fails the task as `framework delegate is missing` does | `attachComponent`, `convertToViews` |
| N4 | `componentProps` are set by name before the page is attached (Avalonia property, CLR property, camelCase key, string converted; unknown key ignored) | `attachComponent`'s `Object.assign`; M2 |
| N5 | Push: `IonNavWillChange` inside the call; the entering page at opacity 0 until the animation; will-events leaving then entering; the md forward keyframes, 280 ms and curve on the clock (frames pinned to Chromium's at 70, 140, 210 ms); leaving page unmoved; did-events entering then leaving; the leaving page hidden after; `IonNavDidChange` before the awaiting caller continues; `true` | `runTransition`, `transition`, `md.transition`; M2 |
| N6 | Pop: the md back keyframes, 200 ms and curve; the previous page shown under it; `DidEnter`, `DidLeave`, `WillUnload` on the popped page, then `IonNavDidChange`; the popped page leaves the nav | M3 |
| N7 | The animation doesn't start before the entering page has been laid out: a page whose build and layout take longer than a frame shows the start values on the first animated frame | `waitForReady`, `deepReady`; M18 |
| N8 | `ZIndex` 101 / 100 forward and 99 / 100 back; both pages not hit-testable during the transition, and hit-testable after; an app's own `IsHitTestVisible` comes back | `setZIndex`, `beforeTransition`, `afterTransition`; X4 |
| N9 | `:can-go-back` on the entering page when a view is under it, and `ShowGoBack` overrides it | `beforeTransition`; M2 |
| N10 | The queue: calls run one at a time, in order; `SkipIfBusy` during a transition gives false at once and queues nothing | `queueTrns`, `nextTrns`; M5 |
| N11 | Failure: a pop at the root faults the task with Ionic's message, logs the warning, raises `IonNavWillChange` and not `IonNavDidChange`, calls `done(false, false, reason)`, leaves the stack; with no views, `no views in the stack to be removed`; the nav works after | `failed`, `postViewInit`; M4, X6 |
| N12 | Views removed from the middle (`RemoveIndex`, `PopToRoot`, `PopTo`, `SetRoot`, `SetPages`) get `WillLeave`, `DidLeave`, `WillUnload` at once and leave the nav, before the transition | `postViewInit`; M6, M7, M9, M10 |
| N13 | `Insert` in the middle: no transition and no enter events; `InsertPages` at the end builds only the last; a view with no page is built when it's entered | `prepareTI`, `unmountInactiveViews`; M7, M8 |
| N14 | `SetRoot` and `SetPages` unanimated unless `Animated = true` in the options | `setPages`; M9 |
| N15 | `Animated` false on the nav, or `AvonicConfig.Animated` false: the events in one go at the next frame, no animation | `transition`, `noAnimation`; M13 |
| N16 | Options: `Duration` replaces the duration; `Direction = Back` on a push runs the back animation; `AnimationBuilder` beats `Animation`, which beats md's; a pop back uses the builder the entering page was pushed with | `runTransition`, `transition`; M14 |
| N17 | Getters: `GetActive`, `GetByIndex`, `GetPrevious(view)`, `CanGoBack(view)`, `GetLength` | `getActiveSync` and the rest |
| N18 | Pages under the top are hidden (not visible, not in automation), keep their state and scroll position; a popped page an app made can be pushed again | `unmountInactiveViews`; M11, M12 |
| N19 | `IonNavWillChange`, `IonNavDidChange` and the five page events don't bubble | M16 |
| N20 | Detached and attached again (as an inline modal's content is moved), the stack is kept (D3) | X7, X8 |
| N21 | A failure drops the instructions queued behind it, and each completes false (D2) | `failed`; X1b |
| N22 | `InsertPages` and `SetPages` take a bare page form in the list (D1) | `convertToViews`; M8 |
| N23 | Swipe back (if offered): starts only from the edge, when it can go back and nothing is queued; drives the transition; completes or cancels by Ionic's rule; mirrored in RTL; `SwipeGesture` null takes the config at load | `swipe-back.js`, `onStart`, `onEnd`; M20 |
| N24 | Hardware back (E1): pops at priority 0 while it can go back and is visible; the innermost nav first; left alone at the root, and under an overlay's 100 | E1 |
| L1 | `AvonicNavLink`: `Forward` pushes `Component` with `ComponentProps`, `Root` sets the root, `Back` pops, each with `SkipIfBusy` and `RouterAnimation`; on the closest nav | `navLink`; M17 |
| L2 | A press on plain content in the link, an `AvonicButton` in it clicked by a tap or by Enter and Space, a stock `Button` in it: each navigates once; a double tap pushes once | the host's `onClick`; M17, X2c |
| L3 | No nav around it, or no `Component` for `Forward` and `Root`: nothing happens | `navLink` |
| L4 | The link isn't focusable and adds no automation element | X2b |

## Deliberate differences (ADR 0005)

Each recorded in `docs/infra/ionic-issues.md` with its page, and tested.

- **D1 — a string in `insertPages` or `setPages`** throws `TypeError: Cannot use 'in' operator to search for 'component'`
  (`convertToViews` tests `'component' in page` on a primitive; measured M8), though the type is `NavComponent[]` and a string is a
  `NavComponent`. Avonic takes any page form in the list, as the type says. (Avonic has no string form, but the same check on a
  `Type` or a view model is what's meant.)
- **D2 — a failed transition drops the queue without settling it:** `failed()` empties `transInstr`, so a call queued behind the
  failure never runs and its promise never settles (X1b: a push queued behind a pop at the root is never heard from again; an
  `await` on it hangs). Avonic drops them too, as Ionic means to, and completes each with false (and `done(false, false)`), so no
  caller waits for ever.
- **D3 — detaching destroys the stack:** `disconnectedCallback` unloads every view, and `componentDidLoad` doesn't set the root
  again, so a nav moved in the DOM is empty (X8), and a nav in an inline `ion-modal`, the usual way to navigate in a modal, shows
  nothing once the modal is presented (X7). Avalonia moves controls routinely (`InlinePlacement` moves an inline overlay at every
  present), and has no "destroyed": Avonic keeps the views across a detach. What Ionic means by it, releasing the pages when the
  nav goes away, is the garbage collector's job here.
- **D4 — not a difference, recorded:** `insert` in the middle builds the inserted page and leaves it at opacity 0 under the top
  one, not hidden (M7). Kept, as it's invisible and covered; it is in the automation tree, as it is in the accessibility tree.

## Extensions

- **E1 — hardware back pops the nav.** The issue asks for it; Ionic's core nav doesn't (M19): Ionic leaves it to the framework's
  router outlet, which goes back at priority 0. Proposed: while it can go back, is visible and is in an `AvonicApp`, the nav
  registers `Pop()` at priority 0 (Ionic's router's) on `IonBackButton`, so an overlay (100) and a menu (99) come first and a root
  page leaves the request alone (the platform's back, or `HandleUnclaimedBack`). Of two navs, the one attached later (the inner
  one) registers later and wins at the same priority, as Ionic keeps the later of equal ones. A property to turn it off,
  `HandleBackButton` (default true), so an app that routes back itself can. The sample shell's own back is at 0 too, registered
  before, so a nav on the sample page pops first and the shell goes back to the list once the nav is at its root.
- The forms of option B beyond Ionic's three (`Func<Control>`, a view model) are the framework delegate's part, not extensions;
  the README lists them under the page forms.

## Performance

Measured in phase 2 on the desktop headless and in the sample (Debug's page-cost line); the phone isn't used here.

- **Ionic's budget:** a light page's push is two frames to the first animated frame (+44 ms) and 327 ms in all; a page that takes
  150 ms to build starts its animation the frame after (M18). So a slow build delays the start, and the animation itself is never
  cut short.
- **What a push costs in Avonic:** the build (constructor and `InitializeComponent`), the first layout of the page (#248: 83% of a
  page's cost on the desktop, about 0.3 ms per element on a Pixel 8, so 150 to 200 ms for a typical sample page), then 280 ms of
  animation that composes the two pages each frame. The nav adds: the instruction and its task, one forced layout of the new page,
  and the animation's frames; the record goes in COMPARISON.md with the build, the wait and the transition timed separately.
- Pages under the top stay built and hidden, as Ionic's do: memory for state, no layout cost while hidden (an invisible control
  isn't measured).

## Dependencies

All merged in `v2`: `AvonicAnimation` (#5), `AvonicContent` (#11), `AvonicApp` and `HardwareBackButton` (#65), `Gestures/` (for the
swipe), `Behaviors/NativeButton` (the link), the overlays (a nav in a modal), `AvonicHeader`, `AvonicToolbar`, `AvonicTitle`,
`AvonicButton` for the sample and the frames. `ion-back-button` (#47) comes after and builds on `:can-go-back` and `CanGoBack`/`Pop`.

## Decisions

Taken by the human on 2026-11-20, from the proposal above:

1. **The page forms: B**, and `componentProps` sets public CLR properties by reflection as well as Avalonia properties, with the
   trimming caveat documented.
2. **Swipe back: offered, off by default** (`NavSwipeGesture`, named as `SegmentSwipeGesture` is, and
   `AvonicConfig.SwipeBackEnabled`, false), as an Avonic choice under md, with the note that on Android a left-edge drag competes with
   the system's back gesture.
3. **Hardware back (E1): an extension, on by default**: pops at priority 0 while the nav can go back, the innermost first;
   `HandleBackButton="False"` turns it off, and an app can also take the request first above priority 0.
4. **D3: the stack is kept** across a detach (ADR 0005).

Found while building: Avalonia's `Opacity` isn't group opacity, so the pages' transition fades as an `OpacityMask`
(`AnimationGroupOpacity`, a small opt-in in `Animation/AnimatedProperty.cs`); see COMPARISON.md.
