using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-card</c>: a raised box for content, which can be tappable. Put an
/// <see cref="AvonicCardHeader"/> and an <see cref="AvonicCardContent"/> in a panel inside it.
/// </summary>
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(":button")]
public class AvonicCard : ContentControl
{
    /// <summary>Ionic <c>button</c>: the card is tappable.</summary>
    public static readonly StyledProperty<bool> ButtonProperty =
        AvaloniaProperty.Register<AvonicCard, bool>(nameof(Button));

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicCard>();

    public bool Button
    {
        get => GetValue(ButtonProperty);
        set => SetValue(ButtonProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    // ── Extensions ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Avonic extension: a <see cref="Button"/> card was tapped or activated by keyboard. Ionic gets
    /// this from its native <c>&lt;button&gt;</c>.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<AvonicCard, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private AvonicRippleEffect? _ripple;
    private bool _spacePressed;
    private object? _secondTapSource;
    private IPointer? _secondTapPointer;
    private Point _secondTapStart;
    private IPointer? _pressPointer;
    private Point _pressPosition;
    private IDisposable? _focusableFrame;

    public AvonicCard()
    {
        AddHandler(TappedEvent, (_, e) => Tap(e.Source));

        // A second quick press raises DoubleTapped, on the press, and no Tapped. A native <button> clicks each
        // time, on release, so a finger that goes down to scroll doesn't activate anything.
        AddHandler(PointerPressedEvent, OnPointerPressedBeforeSecondTap, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(DoubleTappedEvent, (_, e) =>
        {
            _secondTapSource = e.Source;
            _secondTapPointer = _pressPointer;
            _secondTapStart = _pressPosition;
        });
        AddHandler(PointerReleasedEvent, OnPointerReleasedAfterSecondTap, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ButtonProperty)
        {
            _spacePressed = false;
            UpdateInteractivity();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _ripple = e.NameScope.Find<AvonicRippleEffect>("PART_Ripple");
        UpdateInteractivity();
    }

    /// <summary>
    /// <c>isClickable</c>. Nothing sets <c>ion-focusable</c>, so unlike an item, focus and hover have no state here.
    /// </summary>
    private void UpdateInteractivity()
    {
        var clickable = Button;

        PseudoClasses.Set(":button", clickable);
        Activatable.SetIsEnabled(this, clickable);

        // A <button> takes focus. Behaviour, so it doesn't depend on the theme, which an app can replace. At style
        // priority, as CSS is, so an app's own value wins.
        if (clickable && _focusableFrame is null)
        {
            _focusableFrame = SetValue(FocusableProperty, true, BindingPriority.Style);
        }
        else if (!clickable)
        {
            _focusableFrame?.Dispose();
            _focusableFrame = null;
        }

        if (_ripple is not null)
        {
            _ripple.IsVisible = clickable;
        }
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    private void OnPointerPressedBeforeSecondTap(object? sender, PointerPressedEventArgs e)
    {
        // Another finger going down doesn't end this one's press.
        if (e.Pointer == _secondTapPointer)
        {
            _secondTapSource = null;
            _secondTapPointer = null;
        }

        _pressPointer = e.Pointer;
        _pressPosition = e.GetPosition(this);
    }

    private void OnPointerReleasedAfterSecondTap(object? sender, PointerReleasedEventArgs e)
    {
        if (_secondTapPointer is not null && e.Pointer == _secondTapPointer)
        {
            var source = _secondTapSource;
            _secondTapSource = null;
            _secondTapPointer = null;
            if (IsWithinTap(e))
            {
                Tap(source);
            }
        }
    }

    /// <summary>The limit Avalonia gives a first press's <c>Tapped</c>, so a finger that slides doesn't click on either.</summary>
    private bool IsWithinTap(PointerReleasedEventArgs e)
    {
        var size = Application.Current?.PlatformSettings?.GetTapSize(e.Pointer.Type) ?? new Size(4, 4);
        return new Rect(_secondTapStart, default(Size))
            .Inflate(new Thickness(size.Width / 2, size.Height / 2))
            .Contains(e.GetPosition(this));
    }

    /// <summary>A disabled <c>&lt;button&gt;</c> doesn't click, even if it was disabled during the press.</summary>
    private void Tap(object? source)
    {
        if (Button && IsEffectivelyEnabled && !IsInNestedButtonCard(source))
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        }
    }

    /// <summary>
    /// A browser has one click, which reaches each card on its way up. Here a nested card's <see cref="Click"/> already
    /// bubbles through this one, so raising another would run this card's handlers twice.
    /// </summary>
    private bool IsInNestedButtonCard(object? source)
    {
        for (var visual = source as Visual; visual is not null && visual != this; visual = visual.GetVisualParent())
        {
            if (visual is AvonicCard { Button: true })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A native <c>&lt;button&gt;</c>: Enter activates on press, and Space on release.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!Button || !ReferenceEquals(e.Source, this))
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            Tap(this);
            e.Handled = true;
        }
        else if (e.Key == Key.Space)
        {
            _spacePressed = true;
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Key == Key.Space && _spacePressed)
        {
            _spacePressed = false;
            Tap(this);
            e.Handled = true;
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        _spacePressed = false;
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new CardAutomationPeer(this);

    private sealed class CardAutomationPeer(AvonicCard owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        /// <summary>Only a <c>&lt;button&gt;</c> can be invoked. Without one, Ionic renders nothing but a slot.</summary>
        protected override object? GetProviderCore(Type providerType) =>
            providerType == typeof(IInvokeProvider) && !owner.Button ? null : base.GetProviderCore(providerType);

        protected override AutomationControlType GetAutomationControlTypeCore() =>
            owner.Button ? AutomationControlType.Button : AutomationControlType.Group;

        /// <summary>
        /// <c>inheritAttributes(['aria-label'])</c> gives the <c>&lt;button&gt;</c> the card's label, and a native
        /// button otherwise names itself from what's inside it. A plain card doesn't.
        /// </summary>
        protected override string? GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) && owner.Button ? CardText.Of(owner) : name;
        }

        protected override bool IsContentElementCore() => owner.Button;

        protected override bool IsControlElementCore() => true;

        public void Invoke()
        {
            owner.Tap(owner);
        }
    }
}
