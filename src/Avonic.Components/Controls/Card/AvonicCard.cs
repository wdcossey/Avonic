using System.Windows.Input;
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

    /// <summary>Avonic extension: runs when a <see cref="Button"/> card clicks. It does nothing on a plain card, which has no native button.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvonicButton.CommandProperty.AddOwner<AvonicCard>();

    /// <summary>Avonic extension: the parameter for <see cref="Command"/>.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvonicButton.CommandParameterProperty.AddOwner<AvonicCard>();

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private NativeButton? _native;
    private AvonicRippleEffect? _ripple;
    private IDisposable? _focusableFrame;

    public AvonicCard()
    {
        _native = new NativeButton(this, _ => Tap(), () => Button, UpdateIsEffectivelyEnabled);
    }

    /// <summary>A command only disables a button card. The base class's constructor reads this before there's a <see cref="NativeButton"/>.</summary>
    protected override bool IsEnabledCore => base.IsEnabledCore && (!Button || _native?.CanExecute != false);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ButtonProperty)
        {
            _native?.Reset();
            UpdateIsEffectivelyEnabled();
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
    /// <c>isClickable</c>. Unlike an item, hover has no state here. Ionic doesn't set <c>ion-focusable</c> on a card, so
    /// its keyboard focus is invisible. <see cref="FocusVisible"/> is a deliberate difference, to show it.
    /// </summary>
    private void UpdateInteractivity()
    {
        // Most cards are plain, and don't pay for this.
        if (_native is null && Button)
        {
            _native = new NativeButton(this, _ => Tap(), () => Button, UpdateIsEffectivelyEnabled);
            UpdateIsEffectivelyEnabled();
        }

        var clickable = Button;

        PseudoClasses.Set(":button", clickable);
        Activatable.SetIsEnabled(this, clickable);
        FocusVisible.SetIsEnabled(this, clickable);

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

    /// <summary>A disabled <c>&lt;button&gt;</c> doesn't click, even if it was disabled during the press.</summary>
    private void Tap()
    {
        if (Button && IsEffectivelyEnabled)
        {
            _native?.Click(ClickEvent);
        }
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
            return string.IsNullOrEmpty(name) && owner.Button ? AccessibleText.Of(owner) : name;
        }

        protected override bool IsContentElementCore() => owner.Button;

        protected override bool IsControlElementCore() => true;

        public void Invoke()
        {
            owner.Tap();
        }
    }
}
