using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// A segment button's indicator sliding in from the last one: Ionic's <c>checkButton</c> sets
/// <c>translate3d(x, 0, 0) scaleX(s)</c> with no transition, then puts <c>--indicator-transition</c> back and clears the
/// transform, so it transitions to none. This runs that transition on <see cref="AvonicConfig"/>'s clock, from the left edge as
/// <c>transform-origin: left</c> does, which the template gives.
/// </summary>
internal sealed class IndicatorSlide(Control indicator)
{
    /// <summary>The theme's <c>--indicator-transition</c>, read from the button as Ionic reads the variable there.</summary>
    internal const string TransitionKey = "Avonic.SegmentButton.IndicatorTransition";

    private IDisposable? _frame;
    private double _x;
    private double _scale;
    private TimeSpan _start;
    private TimeSpan _delay;
    private TimeSpan _duration;
    private Easing _easing = new LinearEasing();

    public bool IsRunning => _frame is not null;

    /// <summary>From <c>translateX(x) scaleX(scale)</c> to none, by the theme's transition, or at once with none.</summary>
    public void Start(double x, double scale, Control button)
    {
        Stop();
        var transition = button.TryFindResource(TransitionKey, out var resource) ? Find(resource) : null;
        if (transition is null || transition.Duration <= TimeSpan.Zero || !double.IsFinite(x) || !double.IsFinite(scale))
        {
            indicator.ClearValue(Visual.RenderTransformProperty);
            return;
        }

        _x = x;
        _scale = scale;
        _start = AvonicConfig.Now();
        _delay = transition.Delay;
        _duration = transition.Duration;
        _easing = transition.Easing;
        Apply(0);
        _frame = AvonicConfig.RequestFrame(indicator, OnFrame);
    }

    public void Stop()
    {
        _frame?.Dispose();
        _frame = null;
    }

    private static TransitionBase? Find(object? resource) => resource switch
    {
        TransitionBase transition => transition,
        Transitions transitions => transitions.OfType<TransitionBase>().FirstOrDefault(),
        _ => null,
    };

    private void OnFrame()
    {
        _frame = null;
        var elapsed = AvonicConfig.Now() - _start - _delay;
        var progress = elapsed <= TimeSpan.Zero ? 0 : Math.Clamp(elapsed.Ticks / (double)_duration.Ticks, 0, 1);
        if (progress >= 1)
        {
            indicator.ClearValue(Visual.RenderTransformProperty);
            return;
        }

        Apply(_easing.Ease(progress));
        _frame = AvonicConfig.RequestFrame(indicator, OnFrame);
    }

    /// <summary>The transform at a point of the transition: each function interpolated towards its identity.</summary>
    private void Apply(double eased)
    {
        var x = _x * (1 - eased);
        var scale = _scale + (1 - _scale) * eased;
        indicator.RenderTransform = new MatrixTransform(Matrix.CreateScale(scale, 1) * Matrix.CreateTranslation(x, 0));
    }

    /// <summary>The transform shown, for tests: the x and the scale.</summary>
    internal (double X, double Scale) Current =>
        indicator.RenderTransform is MatrixTransform { Matrix: var m } ? (m.M31, m.M11) : (0, 1);
}
