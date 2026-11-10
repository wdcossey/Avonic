using Avalonia;
using Avalonia.Data;

namespace Avonic.Components.Controls;

/// <summary>
/// A value the fab family's code gives a property at style priority, or nothing, so that the binding gives way to whatever
/// else sets it. One binding for the control's life, made in its constructor, so that an app's style, which comes later,
/// still wins, as a frame made when the state turns on wouldn't let it.
/// </summary>
internal sealed class FabStyleValue<T> : IObservable<BindingValue<T>>
{
    private readonly List<IObserver<BindingValue<T>>> _observers = [];
    private readonly T _value;
    private bool _on;

    public FabStyleValue(T value) => _value = value;

    public void Set(bool on)
    {
        if (on == _on)
        {
            return;
        }

        _on = on;
        foreach (var observer in _observers.ToArray())
        {
            observer.OnNext(Current);
        }
    }

    private BindingValue<T> Current => _on ? new BindingValue<T>(_value) : BindingValue<T>.Unset;

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
