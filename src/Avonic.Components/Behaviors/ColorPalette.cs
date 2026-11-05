using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avonic.Components.Behaviors;

/// <summary>
/// Ionic's <c>color</c> prop (<c>utils/theme.js</c> <c>createColorClasses</c>). Setting
/// <see cref="ColorProperty"/> sets the <c>:ion-color</c> pseudoclass (Ionic: class <c>ion-color</c>)
/// and resolves the current colour into inheriting properties, one per Ionic variable.
/// </summary>
/// <remarks>
/// <para>
/// Ionic's <c>--ion-color-base</c> and its siblings are inherited custom properties, and these have
/// the same semantics. An unknown name binds nothing, so the value stays unset or shows an ancestor's,
/// as it does in CSS. An element with no colour inherits the values, but has no <c>:ion-color</c>, so
/// its styles ignore them.
/// </para>
/// <para>
/// Values come from the <c>Avonic.Color.*</c> <see cref="Avalonia.Media.Color"/> keys, looked up from
/// the element, so a consumer can add a custom colour or override one for a subtree. The lookup is
/// live, so it follows merged dictionaries and moves between scopes.
/// </para>
/// </remarks>
/// <example>
/// A control adds the property with <c>ColorPalette.ColorProperty.AddOwner&lt;MyControl&gt;()</c>. Its
/// theme translates the CSS directly:
/// <code>
/// &lt;Style Selector="^:ion-color"&gt;
///     &lt;Setter Property="Background"
///             Value="{Binding (b:ColorPalette.Base), RelativeSource={RelativeSource Self}}" /&gt;
/// &lt;/Style&gt;
/// </code>
/// </example>
public static class ColorPalette
{
    public const string IonColorPseudoClass = ":ion-color";

    private const string KeyPrefix = "Avonic.Color.";

    // ── Attached properties ──────────────────────────────────────────────────────────────────

    /// <summary>Ionic's <c>color</c> prop, such as <c>danger</c>. Case-sensitive, like a CSS class.</summary>
    public static readonly AttachedProperty<string?> ColorProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("Color", typeof(ColorPalette));

    /// <summary>Ionic <c>--ion-color-base</c>.</summary>
    public static readonly AttachedProperty<IBrush?> BaseProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, IBrush?>("Base", typeof(ColorPalette), inherits: true);

    /// <summary>Ionic <c>--ion-color-contrast</c>.</summary>
    public static readonly AttachedProperty<IBrush?> ContrastProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, IBrush?>("Contrast", typeof(ColorPalette), inherits: true);

    /// <summary>Ionic <c>--ion-color-shade</c>.</summary>
    public static readonly AttachedProperty<IBrush?> ShadeProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, IBrush?>("Shade", typeof(ColorPalette), inherits: true);

    /// <summary>Ionic <c>--ion-color-tint</c>.</summary>
    public static readonly AttachedProperty<IBrush?> TintProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, IBrush?>("Tint", typeof(ColorPalette), inherits: true);

    /// <summary>Ionic <c>--ion-color-base-rgb</c>: use it to add alpha, as <c>rgba()</c> does.</summary>
    public static readonly AttachedProperty<Color?> BaseColorProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, Color?>("BaseColor", typeof(ColorPalette), inherits: true);

    /// <summary>Ionic <c>--ion-color-contrast-rgb</c>: use it to add alpha, as <c>rgba()</c> does.</summary>
    public static readonly AttachedProperty<Color?> ContrastColorProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, Color?>("ContrastColor", typeof(ColorPalette), inherits: true);

    private static readonly AttachedProperty<Subscriptions?> SubscriptionsProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, Subscriptions?>("Subscriptions", typeof(ColorPalette));

    /// <summary>
    /// For a text colour binding. An unknown colour leaves <see cref="ContrastProperty"/> null, and a
    /// null <c>Foreground</c> hides the text. Ionic's text stays visible, so this gives the property
    /// its default instead.
    /// </summary>
    public static readonly IValueConverter NullToUnset =
        new FuncValueConverter<object?, object?>(value => value ?? AvaloniaProperty.UnsetValue);

    public static string? GetColor(StyledElement element) => element.GetValue(ColorProperty);
    public static void SetColor(StyledElement element, string? value) => element.SetValue(ColorProperty, value);

    public static IBrush? GetBase(StyledElement element) => element.GetValue(BaseProperty);
    public static IBrush? GetContrast(StyledElement element) => element.GetValue(ContrastProperty);
    public static IBrush? GetShade(StyledElement element) => element.GetValue(ShadeProperty);
    public static IBrush? GetTint(StyledElement element) => element.GetValue(TintProperty);
    public static Color? GetBaseColor(StyledElement element) => element.GetValue(BaseColorProperty);
    public static Color? GetContrastColor(StyledElement element) => element.GetValue(ContrastColorProperty);

    static ColorPalette() => ColorProperty.Changed.AddClassHandler<StyledElement>(OnColorChanged);

    private static void OnColorChanged(StyledElement element, AvaloniaPropertyChangedEventArgs e)
    {
        element.GetValue(SubscriptionsProperty)?.Dispose();

        var name = e.GetNewValue<string?>();

        // createColorClasses: typeof color === 'string' && color.length > 0
        ((IPseudoClasses)element.Classes).Set(IonColorPseudoClass, !string.IsNullOrEmpty(name));

        var key = KeyFor(name);
        if (key is null)
        {
            element.ClearValue(SubscriptionsProperty);
            foreach (var property in new AvaloniaProperty[] { BaseProperty, ContrastProperty, ShadeProperty, TintProperty, BaseColorProperty, ContrastColorProperty })
            {
                element.ClearValue(property);
            }

            return;
        }

        element.SetValue(SubscriptionsProperty, new Subscriptions(
        [
            Track(element, key, BaseProperty, BaseColorProperty),
            Track(element, key + "Contrast", ContrastProperty, ContrastColorProperty),
            Track(element, key + "Shade", ShadeProperty, null),
            Track(element, key + "Tint", TintProperty, null),
        ]));
    }

    private static IDisposable Track(
        StyledElement element,
        string key,
        StyledProperty<IBrush?> brushProperty,
        StyledProperty<Color?>? colorProperty) =>
        element.GetResourceObservable(key).Subscribe(new Observer(value =>
        {
            if (value is Color color)
            {
                element.SetValue(brushProperty, new ImmutableSolidColorBrush(color));
                if (colorProperty is not null)
                {
                    element.SetValue(colorProperty, color);
                }
            }
            else
            {
                // Clearing, not setting null, so an ancestor's value shows through, as an undefined
                // .ion-color-<name> leaves an ancestor's --ion-color-base showing. A binding that
                // yields UnsetValue doesn't do this: it gives the default, not the inherited value.
                element.ClearValue(brushProperty);
                if (colorProperty is not null)
                {
                    element.ClearValue(colorProperty);
                }
            }
        }));

    /// <summary>
    /// <c>danger</c> → <c>Avonic.Color.Danger</c>; <c>my-brand</c> → <c>Avonic.Color.MyBrand</c>.
    /// Null if the name can't be a colour. A CSS class is case-sensitive, so <c>Danger</c> isn't one.
    /// <c>danger-shade</c> isn't either, though it would otherwise find the real key
    /// <c>Avonic.Color.DangerShade</c> and paint it as the base.
    /// </summary>
    internal static string? KeyFor(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.Any(char.IsAsciiLetterUpper))
        {
            return null;
        }

        var segments = name.Split('-');
        if (segments.Any(segment => segment.Length == 0) || segments[^1] is "contrast" or "shade" or "tint")
        {
            return null;
        }

        return KeyPrefix + string.Concat(segments.Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));
    }

    private sealed class Observer(Action<object?> onNext) : IObserver<object?>
    {
        public void OnNext(object? value) => onNext(value);
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    private sealed class Subscriptions(IDisposable[] items) : IDisposable
    {
        public void Dispose()
        {
            foreach (var item in items)
            {
                item.Dispose();
            }
        }
    }
}
