using Avalonia;
using Avalonia.Data;
using Avonic.Components.Behaviors;

namespace Avonic.Components;

/// <summary>
/// A prop that Ionic gives its <see cref="AvonicConfig"/> value in <c>componentWillLoad</c> or <c>componentDidLoad</c>, as
/// <c>if (this.foo === undefined) this.foo = config.get('foo', fallback)</c>: once, when the control first loads, and never again.
/// Made in the control's constructor; the control calls <see cref="Load"/> where it first loads, which is its first attach, or
/// earlier, when another control waits for it as Ionic's <c>componentOnReady</c> does.
/// </summary>
/// <remarks>
/// Two ways to give it, as CLAUDE.md's config row has them, by what clearing the prop later must give:
/// <list type="bullet">
/// <item><see cref="AtStylePriority"/>: at style priority, through a binding made in the constructor, before any style, so an app's
/// value and every style of its win, and the config's comes back when they go. For a prop whose own default is Ionic's fallback.</item>
/// <item><see cref="IfUnset"/>: the current value, given only if the prop isn't set then, so clearing it later gives its default,
/// Ionic's <c>undefined</c>. For a prop whose <c>undefined</c> means something of its own, such as no spinner.</item>
/// </list>
/// </remarks>
internal sealed class ConfigOnLoad<T>
{
    private readonly AvaloniaObject _owner;
    private readonly StyledProperty<T> _property;
    private readonly StyleValue<T>? _styleValue;

    private ConfigOnLoad(AvaloniaObject owner, StyledProperty<T> property, StyleValue<T>? styleValue)
    {
        _owner = owner;
        _property = property;
        _styleValue = styleValue;
    }

    // whether Load has run
    private bool _loaded;

    /// <summary>
    /// The config's value at style priority. Binds now, so call it from the constructor: of two values at one priority the later
    /// wins, and an app's style is applied after the constructor.
    /// </summary>
    public static ConfigOnLoad<T> AtStylePriority(AvaloniaObject owner, StyledProperty<T> property)
    {
        var styleValue = new StyleValue<T>();
        owner.Bind(property, styleValue, BindingPriority.Style);
        return new ConfigOnLoad<T>(owner, property, styleValue);
    }

    /// <summary>The config's value as the current value, if the prop isn't set when it loads.</summary>
    public static ConfigOnLoad<T> IfUnset(AvaloniaObject owner, StyledProperty<T> property) => new(owner, property, null);

    /// <summary>
    /// The first time only: <paramref name="read"/>'s value, which it gives as Ionic's <c>config.get</c> would, with Ionic's
    /// fallback in it. A value with none (<see cref="Optional{T}.Empty"/>) gives nothing, for a fallback that is the prop's own
    /// default. <see cref="IfUnset"/> calls <paramref name="read"/> only for a prop that isn't set, as Ionic reads the config only
    /// for one that's <c>undefined</c>; <see cref="AtStylePriority"/> always does, as its value stays under whatever an app sets,
    /// now or later.
    /// </summary>
    public void Load(Func<Optional<T>> read)
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        if (_styleValue is not null)
        {
            if (read() is { HasValue: true } value)
            {
                _styleValue.Set(value.Value);
            }
        }
        else if (!_owner.IsSet(_property) && read() is { HasValue: true } value)
        {
            _owner.SetCurrentValue(_property, value.Value);
        }
    }
}
