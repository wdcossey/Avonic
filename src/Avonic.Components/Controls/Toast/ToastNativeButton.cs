using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// An <see cref="AvonicToast"/>'s <c>.toast-button</c>: the native <c>&lt;button&gt;</c> Ionic renders for each
/// <see cref="ToastButton"/>, clicked on release, with Enter and Space, a tab stop, <c>ion-focusable</c> and <c>ion-activatable</c>,
/// with its icon, its text and md's ripple. The toast makes it and sets its state: <c>:role-&lt;role&gt;</c>
/// (<c>toast-button-&lt;role&gt;</c>), <c>:cancel</c> (part <c>button cancel</c>: the role <c>cancel</c> or <c>backdrop</c>) and
/// <c>:icon-only</c> (<c>toast-button-icon-only</c>: an icon and no text).
/// </summary>
[PseudoClasses(Cancel, IconOnly, InColor)]
public class ToastNativeButton : TemplatedControl
{
    /// <summary>Part <c>button cancel</c>: a <c>cancel</c> or <c>backdrop</c> role (<c>isCancel</c>).</summary>
    public const string Cancel = ":cancel";

    /// <summary><c>.toast-button-icon-only</c>: an icon and no text.</summary>
    public const string IconOnly = ":icon-only";

    /// <summary>The toast has a <c>color</c> (<c>:host(.ion-color)</c>): the button takes the toast's text colour.</summary>
    public const string InColor = ":in-color";

    /// <summary><c>text-transform: uppercase</c>, for the label.</summary>
    public static readonly IValueConverter Uppercase =
        new FuncValueConverter<string?, string?>(text => text?.ToUpperInvariant());

    /// <summary>The button's <c>text</c>.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ToastNativeButton, string?>(nameof(Text));

    /// <summary>The button's <c>icon</c>, for its <see cref="AvonicIcon"/>.</summary>
    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<ToastNativeButton, string?>(nameof(Icon));

    private string? _role;
    private ButtonPeer? _peer;

    static ToastNativeButton()
    {
        FocusableProperty.OverrideDefaultValue<ToastNativeButton>(true);
    }

    public ToastNativeButton()
    {
        _ = new NativeButton(this, _ => Clicked?.Invoke(this, EventArgs.Empty), () => true, static () => { });
        SetValue(Activatable.IsEnabledProperty, true, BindingPriority.Style);
        SetValue(FocusVisible.IsEnabledProperty, true, BindingPriority.Style);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>The <see cref="ToastButton"/> it shows, which a render of the toast can change.</summary>
    internal ToastButton? Button { get; set; }

    /// <summary>The button's role, whose pseudoclass is <c>:role-&lt;role&gt;</c>.</summary>
    internal string? Role
    {
        get => _role;
        set
        {
            if (_role is not null)
            {
                PseudoClasses.Remove(":role-" + _role);
            }

            _role = value;
            if (_role is not null)
            {
                PseudoClasses.Add(":role-" + _role);
            }

            PseudoClasses.Set(Cancel, Overlays.OverlayRoles.IsCancel(value));
        }
    }

    /// <summary><c>:in-color</c>, which the toast copies from its own <c>:ion-color</c>.</summary>
    internal bool IsInColor
    {
        get => PseudoClasses.Contains(InColor);
        set => PseudoClasses.Set(InColor, value);
    }

    /// <summary>A click, Enter or Space.</summary>
    internal event EventHandler? Clicked;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == IconProperty)
        {
            PseudoClasses.Set(IconOnly, Icon is not null && Text is null);
        }

        // A render that reuses the control can change what names it.
        if (change.Property == TextProperty || change.Property == AutomationProperties.NameProperty)
        {
            _peer?.NameChanged();
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new ButtonPeer(this);

    /// <summary>A <c>&lt;button&gt;</c>, named by its <c>aria-label</c>, or its text; invoking it is a click.</summary>
    private sealed class ButtonPeer(ToastNativeButton owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        private string? _name;

        public void Invoke()
        {
            if (owner.IsEffectivelyEnabled)
            {
                owner.Clicked?.Invoke(owner, EventArgs.Empty);
            }
        }

        /// <summary>The name, read again, and raised if it changed.</summary>
        public void NameChanged()
        {
            var name = CurrentName();
            if (name != _name)
            {
                RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, _name, name);
                _name = name;
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override string? GetNameCore() => _name = CurrentName();

        private string? CurrentName() => base.GetNameCore() is { Length: > 0 } name ? name : owner.Text;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }
}
