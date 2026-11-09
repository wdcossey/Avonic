using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-backdrop</c>: the layer under an overlay that takes a tap. md's opacity is 0.01, so on its own it's all but
/// invisible: an overlay's enter animation takes it to its backdrop opacity. Its look is in the theme's
/// <c>Controls/AvonicBackdrop.axaml</c>. See <c>docs/components/ion-backdrop/README.md</c>.
/// </summary>
public class AvonicBackdrop : TemplatedControl
{
    /// <summary>Ionic <c>visible</c>: false makes its background transparent. It still takes the tap.</summary>
    public static readonly StyledProperty<bool> VisibleProperty =
        AvaloniaProperty.Register<AvonicBackdrop, bool>(nameof(Visible), defaultValue: true);

    /// <summary>Ionic <c>tappable</c>: whether a tap raises <see cref="IonBackdropTap"/>.</summary>
    public static readonly StyledProperty<bool> TappableProperty =
        AvaloniaProperty.Register<AvonicBackdrop, bool>(nameof(Tappable), defaultValue: true);

    /// <summary>Ionic <c>stopPropagation</c>: whether the tap goes no further than the backdrop.</summary>
    public static readonly StyledProperty<bool> StopPropagationProperty =
        AvaloniaProperty.Register<AvonicBackdrop, bool>(nameof(StopPropagation), defaultValue: true);

    /// <summary>Ionic <c>ionBackdropTap</c>. It bubbles, as it's <c>composed</c>, to the overlay.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBackdropTapEvent =
        RoutedEvent.Register<AvonicBackdrop, RoutedEventArgs>(nameof(IonBackdropTap), RoutingStrategies.Bubble);

    /// <summary>The DOM's <c>click</c>, which only <see cref="NativeButton"/> needs: Ionic listens for it, and nothing else does.</summary>
    private static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<AvonicBackdrop, RoutedEventArgs>("AvonicBackdropClick", RoutingStrategies.Bubble);

    private readonly NativeButton _native;

    static AvonicBackdrop()
    {
        VisibleProperty.Changed.AddClassHandler<AvonicBackdrop>((backdrop, _) => backdrop.UpdatePseudoClasses());
        TappableProperty.Changed.AddClassHandler<AvonicBackdrop>((backdrop, _) => backdrop.UpdatePseudoClasses());

        // Its click, stopped: it goes no further, so a NativeButton around it hears a click that was stopped inside.
        ClickEvent.AddClassHandler<AvonicBackdrop>((backdrop, e) => e.Handled = backdrop.StopPropagation);

        // A parent's Tapped handler is how an Avalonia app listens for a click on a plain element.
        TappedEvent.AddClassHandler<AvonicBackdrop>((backdrop, e) => e.Handled |= backdrop.StopPropagation, handledEventsToo: true);
    }

    public AvonicBackdrop()
    {
        // A plain DOM click, which a press raises as a <button>'s: no keys, as the chip has it.
        _native = new NativeButton(this, _ => OnClick(), static () => false, static () => { });
        UpdatePseudoClasses();
    }

    public bool Visible
    {
        get => GetValue(VisibleProperty);
        set => SetValue(VisibleProperty, value);
    }

    public bool Tappable
    {
        get => GetValue(TappableProperty);
        set => SetValue(TappableProperty, value);
    }

    public bool StopPropagation
    {
        get => GetValue(StopPropagationProperty);
        set => SetValue(StopPropagationProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? IonBackdropTap
    {
        add => AddHandler(IonBackdropTapEvent, value);
        remove => RemoveHandler(IonBackdropTapEvent, value);
    }

    /// <summary>Ionic's <c>emitTap</c>.</summary>
    private void OnClick()
    {
        _native.Click(ClickEvent);
        if (Tappable)
        {
            RaiseEvent(new RoutedEventArgs(IonBackdropTapEvent, this));
        }
    }

    /// <summary><c>backdrop-hide</c> and <c>backdrop-no-tappable</c>, Ionic's host classes.</summary>
    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":backdrop-hide", !Visible);
        PseudoClasses.Set(":backdrop-no-tappable", !Tappable);
    }
}
