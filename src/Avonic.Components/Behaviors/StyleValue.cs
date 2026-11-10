using Avalonia.Data;

namespace Avonic.Components.Behaviors;

/// <summary>
/// A value a control's code gives one of its properties at style priority, or nothing, so that the binding gives way to
/// whatever else sets the property. Bind it once, in the constructor, and push values through it: of two values at one priority
/// the later wins, so a frame made when a state turns on would beat an app's style that was already there, and this doesn't.
/// </summary>
internal sealed class StyleValue<T> : IObservable<BindingValue<T>>
{
    private readonly List<IObserver<BindingValue<T>>> _observers = [];
    private bool _hasValue;
    private T? _value;

    /// <summary>Gives the property <paramref name="value"/>.</summary>
    public void Set(T value) => Update(true, value);

    /// <summary>Takes the value away, so that the property falls back to whatever else sets it.</summary>
    public void Clear() => Update(false, default);

    /// <summary><paramref name="value"/> while <paramref name="on"/>, and nothing otherwise.</summary>
    public void Set(bool on, T value) => Update(on, on ? value : default);

    private void Update(bool hasValue, T? value)
    {
        if (hasValue == _hasValue && EqualityComparer<T>.Default.Equals(value, _value))
        {
            return;
        }

        _hasValue = hasValue;
        _value = value;
        foreach (var observer in _observers.ToArray())
        {
            observer.OnNext(Current);
        }
    }

    private BindingValue<T> Current => _hasValue ? new BindingValue<T>(_value!) : BindingValue<T>.Unset;

    public IDisposable Subscribe(IObserver<BindingValue<T>> observer)
    {
        _observers.Add(observer);
        observer.OnNext(Current);
        return new Unsubscriber(_observers, observer);
    }

    private sealed class Unsubscriber(List<IObserver<BindingValue<T>>> observers, IObserver<BindingValue<T>> observer) : IDisposable
    {
        public void Dispose() => observers.Remove(observer);
    }
}
