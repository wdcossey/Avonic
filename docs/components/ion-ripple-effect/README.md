# `AvonicRippleEffect` — ion-ripple-effect

Ionic reference: [`ion-ripple-effect`](https://ionicframework.com/docs/api/ripple-effect) ·
[Plan](PLAN.md) · [Comparison](COMPARISON.md) · reference render in [`ref/`](ref/)

A presentation-only overlay that fills its parent and draws expanding "ink" circles on request.
It never listens for pointer input itself; the **`Activatable`** behaviour (Ionic's `tap-click`
utility) drives it on behalf of a surface, exactly as Ionic does for `ion-item`, `ion-button`, …

## Usage

```xml
xmlns:c="using:Avonic.Components.Controls"
xmlns:b="using:Avonic.Components.Behaviors"

<!-- An activatable surface with a ripple beneath its content -->
<Border Background="#E0E0E0" ClipToBounds="True" b:Activatable.IsEnabled="True">
    <Panel>
        <c:AvonicRippleEffect Foreground="#3880FF" />
        <TextBlock Text="Tap me" HorizontalAlignment="Center" VerticalAlignment="Center" />
    </Panel>
</Border>

<!-- Unbounded: expands from the centre and may overflow (icon buttons) -->
<c:AvonicRippleEffect Type="Unbounded" />

<!-- Instant: no 100 ms press defer (segment buttons, tab buttons) -->
<Border b:Activatable.IsEnabled="True" b:Activatable.IsInstant="True">…</Border>
```

From code, `var release = ripple.AddRipple(x, y); … release();`.

Place the ripple in the same `Panel` as the surface's content, beneath it. Its colour is its
`Foreground` (Ionic: `currentColor`) — a parent control sets this from its own `--ripple-color`
resource key. Clipping to rounded corners is the parent's job, as in Ionic.

## Fidelity

### Props

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| ✅ | `type` `'bounded' \| 'unbounded'` | `Type` : `RippleEffectType` | Ionic host class `unbounded` → pseudoclass `:type-unbounded` |

### Methods

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| ✅ | `addRipple(x, y) => Promise<() => void>` | `Action AddRipple(double x, double y)` | coordinates in the ripple's own space; the release action is returned at once and the 325 ms minimum lifetime is enforced inside (decision D3) |

Events, slots, shadow parts, CSS variables: none in Ionic, none here.

### Behaviour

All ten behaviours from `ripple-effect.js` / `.css` and all seven from `utils/tap-click` are
implemented and tested — see the tables in [PLAN.md](PLAN.md) and the results in
[COMPARISON.md](COMPARISON.md). In short:

- fills the parent, not hit-testable, hidden from accessibility; `Bounded` clips, `Unbounded` doesn't
- circle starts at `⌊max(w,h)·0.5⌋`, grows to the diagonal + 10 (bounded) or the larger side (unbounded), drifting toward the centre
- expand 225 ms `cubic-bezier(0.4, 0, 0.2, 1)`; fade-in 75 ms to opacity 0.16; release → fade-out 150 ms, removed 200 ms later; release never takes effect before 325 ms
- colour is the inherited `Foreground`

These constants are Ionic's and are not exposed as resource keys (decision D2).

## `Activatable` (Ionic `tap-click`)

Attached properties on any `Control`:

| Ionic | Avonic |
|---|---|
| class `ion-activatable` | `Activatable.IsEnabled="True"` |
| class `ion-activatable-instant` | `Activatable.IsInstant="True"` |
| class `ion-activated` | pseudoclass `:activated` on the surface |
| config `animated`, `rippleEffect` | `AvonicConfig.Animated`, `AvonicConfig.RippleEffect` |

Press → 100 ms defer → `:activated` + ripple at the press point (instant surfaces skip the defer; a
release inside the defer still activates). Release within 150 ms of activation clears `:activated`
150 ms after the release, otherwise at once. Only the innermost activatable under the pointer
activates, only one surface is active application-wide, and only the secondary button is ignored.
Losing pointer capture (a scroll gesture taking the pointer) cancels everything immediately.

## Extensions

None. Every member maps to an Ionic prop, method, class or config entry.

## Internals worth knowing

- The animation runs on a 16 ms UI-thread timer that stops while a held ripple is at its end
  state and restarts on release; the clock and the defer scheduler are injectable
  (`AvonicConfig.Now` / `AvonicConfig.Schedule`, internal) so tests assert exact milliseconds.
- Ripples are drawn in `RippleVisual.Render` from an eased progress value; no theme template is
  involved, so `Avonic.Themes` contributes nothing for this control.
