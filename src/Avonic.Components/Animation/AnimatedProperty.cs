using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace Avonic.Components.Animation;

/// <summary>
/// A CSS property an <see cref="AvonicAnimation"/> can animate or style, and the Avalonia property it is. Keyframe values are
/// applied at animation priority, as a Web Animation's beat every style; inline styles are local values, as <c>el.style</c> is.
/// </summary>
internal abstract class AnimatedProperty
{
    private static readonly AnimatedProperty OpacityProperty = new OpacityAnimated();
    private static readonly AnimatedProperty TransformProperty = new TransformAnimated();
    private static readonly AnimatedProperty PointerEventsProperty = new PointerEvents();

    /// <summary>The CSS name.</summary>
    internal abstract string Name { get; }

    /// <exception cref="ArgumentException">A property no Ionic md animation animates.</exception>
    internal static AnimatedProperty ForKeyframes(string name) => name switch
    {
        "opacity" => OpacityProperty,
        "transform" => TransformProperty,
        _ => throw new ArgumentException($"'{name}' isn't a property an animation can animate here: opacity or transform.", nameof(name)),
    };

    /// <exception cref="ArgumentException">A property no Ionic md animation styles.</exception>
    internal static AnimatedProperty ForStyles(string name) => name switch
    {
        "opacity" => OpacityProperty,
        "pointer-events" => PointerEventsProperty,
        _ => throw new ArgumentException($"'{name}' isn't a property an animation can style here: opacity or pointer-events.", nameof(name)),
    };

    /// <summary>A keyframe's value: a number or a string, as Ionic gives either.</summary>
    internal abstract object Parse(object value);

    internal abstract object Interpolate(object from, object to, double progress);

    /// <summary>The value with no animation, for a keyframe list with no first or last frame, as Web Animations' underlying value.</summary>
    internal abstract object Underlying(Visual element);

    /// <summary>Binds the element's property to <paramref name="values"/>, at animation priority. Unset means no value.</summary>
    internal abstract IDisposable Bind(Visual element, IObservable<object?> values);

    /// <summary>An inline style; <c>""</c> removes it.</summary>
    internal abstract void SetStyle(Visual element, object value);

    private static double Number(object value) => value switch
    {
        double d => d,
        float f => f,
        int i => i,
        string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
        _ => throw new ArgumentException($"'{value}' isn't a number."),
    };

    private sealed class OpacityAnimated : AnimatedProperty
    {
        internal override string Name => "opacity";

        internal override object Parse(object value) => Number(value);

        // CSS clamps opacity's computed value to 0..1, as an easing that overshoots can take it past.
        internal override object Interpolate(object from, object to, double progress) =>
            Math.Clamp((double)from + ((double)to - (double)from) * progress, 0, 1);

        internal override object Underlying(Visual element) => element.GetValue(Visual.OpacityProperty);

        internal override IDisposable Bind(Visual element, IObservable<object?> values) =>
            element.Bind(Visual.OpacityProperty, values, BindingPriority.Animation);

        internal override void SetStyle(Visual element, object value)
        {
            if (value is "")
            {
                element.ClearValue(Visual.OpacityProperty);
            }
            else
            {
                element.SetValue(Visual.OpacityProperty, Math.Clamp(Number(value), 0, 1));
            }
        }
    }

    private sealed class TransformAnimated : AnimatedProperty
    {
        internal override string Name => "transform";

        internal override object Parse(object value) =>
            value is string s ? CssTransform.Parse(s) : throw new ArgumentException($"'{value}' isn't a transform.");

        internal override object Interpolate(object from, object to, double progress) =>
            CssTransform.Interpolate((CssTransform)from, (CssTransform)to, progress);

        internal override object Underlying(Visual element) => CssTransform.None;

        /// <summary>A percentage is of the element's size, so the matrix is made again whenever its bounds change.</summary>
        internal override IDisposable Bind(Visual element, IObservable<object?> values) =>
            element.Bind(Visual.RenderTransformProperty, new SizedTransform(element, values), BindingPriority.Animation);

        internal override void SetStyle(Visual element, object value) =>
            throw new ArgumentException("A transform can't be an animation's style here.");
    }

    private sealed class PointerEvents : AnimatedProperty
    {
        internal override string Name => "pointer-events";

        internal override object Parse(object value) => throw new ArgumentException("pointer-events can't be animated.");

        internal override object Interpolate(object from, object to, double progress) => throw new NotSupportedException();

        internal override object Underlying(Visual element) => throw new NotSupportedException();

        internal override IDisposable Bind(Visual element, IObservable<object?> values) => throw new NotSupportedException();

        internal override void SetStyle(Visual element, object value)
        {
            switch (value)
            {
                case "":
                    element.ClearValue(InputElement.IsHitTestVisibleProperty);
                    break;
                case "none":
                    element.SetValue(InputElement.IsHitTestVisibleProperty, false);
                    break;
                case "auto":
                    element.SetValue(InputElement.IsHitTestVisibleProperty, true);
                    break;
                default:
                    throw new ArgumentException($"'{value}' isn't a pointer-events this supports: none, auto or \"\".");
            }
        }
    }

    /// <summary>A <see cref="CssTransform"/> and the element's bounds, as a <see cref="MatrixTransform"/>.</summary>
    private sealed class SizedTransform(Visual element, IObservable<object?> values) : IObservable<object?>
    {
        public IDisposable Subscribe(IObserver<object?> observer)
        {
            object? current = AvaloniaProperty.UnsetValue;

            void Push() => observer.OnNext(current is CssTransform t
                ? new MatrixTransform(t.ToMatrix(element.Bounds.Size))
                : AvaloniaProperty.UnsetValue);

            var bounds = element.GetObservable(Visual.BoundsProperty).Subscribe(new Observer(_ => Push()));
            var source = values.Subscribe(new Observer(value =>
            {
                current = value;
                Push();
            }));
            return new Both(bounds, source);
        }

        private sealed class Observer(Action<object?> next) : IObserver<object?>, IObserver<Rect>
        {
            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(object? value) => next(value);

            public void OnNext(Rect value) => next(value);
        }

        private sealed class Both(IDisposable a, IDisposable b) : IDisposable
        {
            public void Dispose()
            {
                a.Dispose();
                b.Dispose();
            }
        }
    }
}

/// <summary>The latest value, given to each subscriber at once and then as it changes.</summary>
internal sealed class ValueSubject : IObservable<object?>
{
    private readonly List<IObserver<object?>> _observers = [];

    internal object? Value { get; private set; } = AvaloniaProperty.UnsetValue;

    internal void OnNext(object? value)
    {
        if (Equals(value, Value))
        {
            return;
        }

        Value = value;
        foreach (var observer in _observers.ToArray())
        {
            observer.OnNext(value);
        }
    }

    public IDisposable Subscribe(IObserver<object?> observer)
    {
        _observers.Add(observer);
        observer.OnNext(Value);
        return new Unsubscriber(this, observer);
    }

    private sealed class Unsubscriber(ValueSubject subject, IObserver<object?> observer) : IDisposable
    {
        public void Dispose() => subject._observers.Remove(observer);
    }
}
