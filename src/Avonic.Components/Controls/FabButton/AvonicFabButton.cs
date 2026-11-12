using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-fab-button</c>: the round button of an <see cref="AvonicFab"/>, or one of the buttons in an
/// <see cref="AvonicFabList"/>. The look is in the theme. See <c>docs/components/ion-fab/README.md</c>.
/// </summary>
/// <remarks>
/// One slot, so a <see cref="ContentControl"/>: the content is the button's logical child, and the theme's <c>::slotted</c> rules
/// are <c>c|AvonicFabButton &gt; c|X</c>.
/// </remarks>
[TemplatePart("PART_Native", typeof(Border))]
[TemplatePart("PART_CloseIcon", typeof(AvonicIcon))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(":close-active", ":show", ":size-small", ":in-list")]
public class AvonicFabButton : ContentControl
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>activated</c>: the close icon instead of the content. The fab sets it on its first button.</summary>
    public static readonly StyledProperty<bool> ActivatedProperty =
        AvaloniaProperty.Register<AvonicFabButton, bool>(nameof(Activated));

    /// <summary>Ionic <c>closeIcon</c>: the name of the icon shown while <see cref="Activated"/>.</summary>
    public static readonly StyledProperty<string?> CloseIconProperty =
        AvaloniaProperty.Register<AvonicFabButton, string?>(nameof(CloseIcon), "close");

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicFabButton>();

    /// <summary>Ionic <c>show</c>: shown, in a list. The list sets it. Outside a list it does nothing.</summary>
    public static readonly StyledProperty<bool> ShowProperty =
        AvaloniaProperty.Register<AvonicFabButton, bool>(nameof(Show));

    /// <summary>Ionic <c>size</c>. Null is the standard size.</summary>
    public static readonly StyledProperty<FabButtonSize?> SizeProperty =
        AvaloniaProperty.Register<AvonicFabButton, FabButtonSize?>(nameof(Size));

    public bool Activated
    {
        get => GetValue(ActivatedProperty);
        set => SetValue(ActivatedProperty, value);
    }

    public string? CloseIcon
    {
        get => GetValue(CloseIconProperty);
        set => SetValue(CloseIconProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool Show
    {
        get => GetValue(ShowProperty);
        set => SetValue(ShowProperty, value);
    }

    public FabButtonSize? Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionFocus</c>: the button has focus.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicFabButton, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>: the button lost focus.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicFabButton, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? IonFocus
    {
        add => AddHandler(IonFocusEvent, value);
        remove => RemoveHandler(IonFocusEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonBlur
    {
        add => AddHandler(IonBlurEvent, value);
        remove => RemoveHandler(IonBlurEvent, value);
    }

    // ── Extensions ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Avonic extension: the button was clicked, by a tap, Enter or Space, as its native <c>&lt;button&gt;</c> is in Ionic. It's
    /// raised after the fab toggles, so a handler sees the fab's new <see cref="AvonicFab.Activated"/>.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<AvonicFabButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    /// <summary>
    /// Avonic extension: run after <see cref="Click"/>, unless a handler set <c>Handled</c>. While it can't execute, the button is
    /// disabled. As on <see cref="AvonicButton"/>.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvonicButton.CommandProperty.AddOwner<AvonicFabButton>();

    /// <summary>Avonic extension: the parameter for <see cref="Command"/>.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvonicButton.CommandParameterProperty.AddOwner<AvonicFabButton>();

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

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

    private readonly NativeButton? _native;
    private readonly StyleValue<bool> _hidden = new();
    private AvonicFab? _fab;
    private bool _inList;

    static AvonicFabButton()
    {
        // A native <button> takes focus.
        FocusableProperty.OverrideDefaultValue<AvonicFabButton>(true);

        // A TemplatedControl clips by default, which would cut off .button-native's box-shadow.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicFabButton>(false);
    }

    public AvonicFabButton()
    {
        _native = new NativeButton(this, _ => OnNativeClick(), () => true, UpdateIsEffectivelyEnabled);

        // ion-activatable and ion-focusable, always. At style priority, here, before any style, so that an app's style wins.
        SetValue(Activatable.IsEnabledProperty, true, BindingPriority.Style);
        SetValue(FocusVisible.IsEnabledProperty, true, BindingPriority.Style);

        // visibility: hidden, for a list's button that isn't shown: no hit test and no focus. The theme gives the look.
        Bind(IsHitTestVisibleProperty, _hidden, BindingPriority.Style);
        Bind(FocusableProperty, _hidden, BindingPriority.Style);
    }

    /// <summary>A command that can't execute disables the button. The base class's constructor reads this before there's a <see cref="NativeButton"/>.</summary>
    protected override bool IsEnabledCore => base.IsEnabledCore && _native?.CanExecute != false;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ActivatedProperty)
        {
            PseudoClasses.Set(":close-active", change.GetNewValue<bool>());
            UpdateHostContext();
        }
        else if (change.Property == ShowProperty)
        {
            PseudoClasses.Set(":show", change.GetNewValue<bool>());
            UpdateHostContext();
        }
        else if (change.Property == SizeProperty)
        {
            PseudoClasses.Set(":size-small", change.GetNewValue<FabButtonSize?>() == FabButtonSize.Small);
            UpdateHostContext();
        }
        else if (change.Property == ColorProperty || change.Property == IsEnabledProperty || change.Property == CloseIconProperty)
        {
            // Ionic renders again when any of its props changes, and reads hostContext then.
            UpdateHostContext();
        }
    }

    /// <summary>
    /// <c>connectedCallback</c>'s <c>closest('ion-fab')</c>, and <c>hostContext('ion-fab-list')</c>, which <c>render</c> reads: from
    /// the visual ancestors when the button is attached and when one of its props changes, as the mapping rule for
    /// <c>hostContext</c> says. A fab's and a list's buttons are their items, so these are the light DOM's ancestors too.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateHostContext();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _fab = null;
    }

    private void UpdateHostContext()
    {
        if (TopLevel.GetTopLevel(this) is not null || this.GetVisualParent() is not null)
        {
            _fab = this.FindAncestorOfType<AvonicFab>();
            _inList = this.FindAncestorOfType<AvonicFabList>() is not null;
            PseudoClasses.Set(":in-list", _inList);
        }

        UpdateHidden();
    }

    /// <summary><c>::slotted(.fab-button-in-list)</c> is <c>visibility: hidden</c> until <c>.fab-button-show</c>.</summary>
    private bool IsHiddenInList => _inList && !Show;

    private void UpdateHidden() => _hidden.SetWhen(IsHiddenInList, false);

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The host's <c>onClick</c>, <c>fab.toggle()</c>, then the native button's click. A disabled button takes no click, and
    /// automation's <c>Invoke</c> doesn't reach one either. Nor does one that isn't shown: a closed list is <c>display: none</c>,
    /// which blurs its button in a browser, where Avalonia leaves the focus on it, so Enter would open the fab again.
    /// </summary>
    private void OnNativeClick()
    {
        if (!IsEffectivelyEnabled || !IsEffectivelyVisible || IsHiddenInList)
        {
            return;
        }

        _fab?.Toggle();
        _native?.Click(ClickEvent);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);

        // onFocus is on the native element, so focus inside the slot isn't this.
        if (ReferenceEquals(e.Source, this))
        {
            RaiseEvent(new RoutedEventArgs(IonFocusEvent, this));
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);

        if (ReferenceEquals(e.Source, this))
        {
            RaiseEvent(new RoutedEventArgs(IonBlurEvent, this));
        }
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new FabButtonPeer(this);

    /// <summary>A native button, named by the text inside it. The close icon is <c>aria-hidden</c>.</summary>
    private sealed class FabButtonPeer(AvonicFabButton owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override bool IsEnabledCore() => owner.IsEffectivelyEnabled;

        protected override string? GetNameCore()
        {
            var name = base.GetNameCore();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return owner.Content switch
            {
                string text => text,
                Control child => AccessibleText.Of(child),
                _ => owner.Presenter?.Child is { } built ? AccessibleText.Of(built) : null,
            };
        }

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        /// <summary><c>visibility: hidden</c> takes it out of the accessibility tree, so a list's button that isn't shown is offscreen.</summary>
        protected override bool IsOffscreenCore() => owner.IsHiddenInList || base.IsOffscreenCore();

        public void Invoke() => owner.OnNativeClick();
    }
}
