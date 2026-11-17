using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// An <see cref="AvonicAlert"/>'s <c>.alert-button</c>: the native <c>&lt;button&gt;</c> Ionic renders for each
/// <see cref="AlertButton"/>, clicked on release, with Enter and Space, a tab stop, <c>ion-focusable</c> and
/// <c>ion-activatable</c>, and md's ripple in its template. The alert makes it; its click is the alert's <c>buttonClick</c>.
/// Its role is the pseudoclass <c>:role-&lt;role&gt;</c>, as <c>alert-button-role-&lt;role&gt;</c>.
/// </summary>
public class AlertActionButton : TemplatedControl
{
    /// <summary>The button's <c>text</c>.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<AlertActionButton, string?>(nameof(Text));

    /// <summary>md's <c>text-transform: uppercase</c>, which the theme sets.</summary>
    public static readonly StyledProperty<ButtonTextTransform> TextTransformProperty =
        AvaloniaProperty.Register<AlertActionButton, ButtonTextTransform>(nameof(TextTransform));

    private readonly NativeButton _native;
    private string? _rolePseudoClass;

    static AlertActionButton()
    {
        FocusableProperty.OverrideDefaultValue<AlertActionButton>(true);
    }

    public AlertActionButton()
    {
        _native = new NativeButton(this, _ => Clicked?.Invoke(this, EventArgs.Empty), () => true, UpdateIsEffectivelyEnabled);
        SetValue(Activatable.IsEnabledProperty, true, BindingPriority.Style);
        SetValue(FocusVisible.IsEnabledProperty, true, BindingPriority.Style);
    }

    internal AlertActionButton(AlertButton button)
        : this()
    {
        Button = button;
        Text = button.Text;
        Role = button.Role;
        AlertClasses.Apply(this, button.CssClass);
        if (button.Id is { } id)
        {
            AutomationProperties.SetAutomationId(this, id);
        }

        if (button.HtmlAttributes is { } attributes)
        {
            if (attributes.TryGetValue("aria-label", out var label) && label is not null)
            {
                AutomationProperties.SetName(this, label.ToString());
            }

            if (attributes.TryGetValue("disabled", out var disabled) && AlertClasses.IsTrue(disabled))
            {
                IsEnabled = false;
            }
        }
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ButtonTextTransform TextTransform
    {
        get => GetValue(TextTransformProperty);
        set => SetValue(TextTransformProperty, value);
    }

    /// <summary>The <see cref="AlertButton"/> it was made for.</summary>
    internal AlertButton? Button { get; }

    /// <summary>The button's role, whose pseudoclass is <c>:role-&lt;role&gt;</c>.</summary>
    internal string? Role
    {
        get => _rolePseudoClass?[6..];
        set
        {
            if (_rolePseudoClass is not null)
            {
                PseudoClasses.Remove(_rolePseudoClass);
            }

            _rolePseudoClass = value is null ? null : ":role-" + value;
            if (_rolePseudoClass is not null)
            {
                PseudoClasses.Add(_rolePseudoClass);
            }
        }
    }

    /// <summary>A click, Enter or Space.</summary>
    internal event EventHandler? Clicked;

    protected override AutomationPeer OnCreateAutomationPeer() => new AlertActionButtonPeer(this);

    /// <summary>A <c>&lt;button&gt;</c>: named by its text, or its <c>aria-label</c>; invoking it is a click.</summary>
    private sealed class AlertActionButtonPeer(AlertActionButton owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        public void Invoke()
        {
            if (owner.IsEffectivelyEnabled)
            {
                owner.Clicked?.Invoke(owner, EventArgs.Empty);
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : owner.Text;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }
}
