using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// An <see cref="AvonicActionSheet"/>'s <c>.action-sheet-button</c>: the native <c>&lt;button&gt;</c> Ionic renders for each
/// <see cref="ActionSheetButton"/>, clicked on release, with Enter and Space, a tab stop, <c>ion-focusable</c> and
/// <c>ion-activatable</c>, with its icon, its text and md's ripple. The sheet makes it and sets its state: <c>:role-&lt;role&gt;</c>
/// (<c>action-sheet-&lt;role&gt;</c>) and <c>:selected</c> (<c>action-sheet-selected</c>). A radio (<c>role="radio"</c>) is a radio
/// button to automation, with its checked state.
/// </summary>
[PseudoClasses(Selected)]
public class ActionSheetNativeButton : TemplatedControl
{
    /// <summary><c>.action-sheet-selected</c>: the <c>selected</c> role, or the checked radio.</summary>
    public const string Selected = ":selected";

    /// <summary>The button's <c>text</c>.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ActionSheetNativeButton, string?>(nameof(Text));

    /// <summary>The button's <c>icon</c>, for its <see cref="AvonicIcon"/>.</summary>
    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<ActionSheetNativeButton, string?>(nameof(Icon));

    private string? _role;
    private string? _cssClass;
    private string[] _cssClasses = [];
    private bool _checked;
    private ButtonPeer? _peer;

    static ActionSheetNativeButton()
    {
        FocusableProperty.OverrideDefaultValue<ActionSheetNativeButton>(true);
    }

    public ActionSheetNativeButton()
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

    /// <summary>The <see cref="ActionSheetButton"/> it shows, which a render of the sheet can change.</summary>
    internal ActionSheetButton? Button { get; set; }

    /// <summary>Its index among the buttons the sheet renders in its first group; -1 for the cancel button.</summary>
    internal int Index { get; set; } = -1;

    /// <summary><c>role="radio"</c>.</summary>
    internal bool IsRadio { get; set; }

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
        }
    }

    /// <summary>The button's <c>cssClass</c>: style classes, space-separated, taking off the ones the last value added.</summary>
    internal string? CssClass
    {
        get => _cssClass;
        set
        {
            foreach (var name in _cssClasses)
            {
                Classes.Remove(name);
            }

            _cssClass = value;
            _cssClasses = AlertClasses.Apply(this, value);
        }
    }

    /// <summary><c>.action-sheet-selected</c>.</summary>
    internal bool IsSelected
    {
        get => PseudoClasses.Contains(Selected);
        set => PseudoClasses.Set(Selected, value);
    }

    /// <summary>A radio's <c>aria-checked</c>.</summary>
    internal bool IsChecked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }

            _checked = value;
            _peer?.RaiseToggled(!value, value);
        }
    }

    /// <summary>A click, Enter or Space.</summary>
    internal event EventHandler? Clicked;

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new ButtonPeer(this);

    /// <summary>
    /// A <c>&lt;button&gt;</c>, named by its text, or its <c>aria-label</c>; invoking it is a click. A radio is a radio button
    /// whose toggle state is <c>aria-checked</c>, and toggling it is a click.
    /// </summary>
    private sealed class ButtonPeer(ActionSheetNativeButton owner) : ControlAutomationPeer(owner), IInvokeProvider, IToggleProvider
    {
        public ToggleState ToggleState => owner.IsChecked ? ToggleState.On : ToggleState.Off;

        public void Invoke() => Click();

        public void Toggle() => Click();

        public void RaiseToggled(bool old, bool now) =>
            RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                old ? ToggleState.On : ToggleState.Off, now ? ToggleState.On : ToggleState.Off);

        protected override object? GetProviderCore(Type providerType) =>
            providerType == typeof(IToggleProvider) && !owner.IsRadio || providerType == typeof(IInvokeProvider) && owner.IsRadio
                ? null
                : base.GetProviderCore(providerType);

        protected override AutomationControlType GetAutomationControlTypeCore() =>
            owner.IsRadio ? AutomationControlType.RadioButton : AutomationControlType.Button;

        protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : owner.Text;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        private void Click()
        {
            if (owner.IsEffectivelyEnabled)
            {
                owner.Clicked?.Invoke(owner, EventArgs.Empty);
            }
        }
    }
}
