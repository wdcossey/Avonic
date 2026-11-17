using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// The <c>&lt;button&gt;</c> an <see cref="AvonicAlert"/> renders for a radio or checkbox <see cref="AlertInput"/>
/// (<c>.alert-tappable</c>): a native button, clicked on release, with Enter and Space, <c>ion-focusable</c> and
/// <c>ion-activatable</c>, its <c>aria-checked</c> the pseudoclass <c>:checked</c>. Ionic draws its own icon here, not an
/// <c>ion-radio</c> or <c>ion-checkbox</c>, and so does the theme. The alert makes it, and sets its state.
/// </summary>
[PseudoClasses(":checked", ":drawn")]
public abstract class AlertOptionButton : TemplatedControl
{
    /// <summary>The input's <c>label</c>.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<AlertOptionButton, string?>(nameof(Label));

    private bool _checked;
    private OptionPeer? _peer;
    private IDisposable? _firstFrame;

    static AlertOptionButton()
    {
        FocusableProperty.OverrideDefaultValue<AlertOptionButton>(true);
    }

    private protected AlertOptionButton()
    {
        _ = new NativeButton(this, _ => Clicked?.Invoke(this, EventArgs.Empty), () => true, static () => { });
        SetValue(Activatable.IsEnabledProperty, true, BindingPriority.Style);
        SetValue(FocusVisible.IsEnabledProperty, true, BindingPriority.Style);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>The processed input it shows.</summary>
    internal AlertInput? Input { get; set; }

    /// <summary><c>aria-checked</c>.</summary>
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
            PseudoClasses.Set(":checked", value);
            _peer?.RaiseToggled(!value, value);
        }
    }

    /// <summary>A click, Enter or Space.</summary>
    internal event EventHandler? Clicked;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // :drawn, from the first frame: CSS doesn't transition a first style, so a radio checked from the start shows its dot
        // at once, and the theme's transition waits for this.
        _firstFrame?.Dispose();
        _firstFrame = AvonicConfig.RequestFrame(this, () =>
        {
            _firstFrame = null;
            PseudoClasses.Set(":drawn", true);
        });
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _firstFrame?.Dispose();
        _firstFrame = null;
        PseudoClasses.Set(":drawn", false);
    }

    private protected abstract AutomationControlType ControlType { get; }

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new OptionPeer(this);

    /// <summary><c>role="radio"</c> or <c>checkbox</c> with <c>aria-checked</c>, named by its label; toggling it is a click.</summary>
    private sealed class OptionPeer(AlertOptionButton owner) : ControlAutomationPeer(owner), IToggleProvider
    {
        public ToggleState ToggleState => owner.IsChecked ? ToggleState.On : ToggleState.Off;

        public void Toggle()
        {
            if (owner.IsEffectivelyEnabled)
            {
                owner.Clicked?.Invoke(owner, EventArgs.Empty);
            }
        }

        public void RaiseToggled(bool old, bool now) =>
            RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                old ? ToggleState.On : ToggleState.Off, now ? ToggleState.On : ToggleState.Off);

        protected override AutomationControlType GetAutomationControlTypeCore() => owner.ControlType;

        protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : owner.Label;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }
}

/// <summary>An <see cref="AvonicAlert"/>'s <c>.alert-radio-button</c>. Only one is a tab stop, as Ionic's roving <c>tabindex</c>.</summary>
public class AlertRadioButton : AlertOptionButton
{
    private protected override AutomationControlType ControlType => AutomationControlType.RadioButton;
}

/// <summary>An <see cref="AvonicAlert"/>'s <c>.alert-checkbox-button</c>, with md's ripple. Enter doesn't toggle it, only Space does.</summary>
public class AlertCheckboxButton : AlertOptionButton
{
    public AlertCheckboxButton()
    {
        // onCheckboxKeydown: Enter's default is prevented, so the native button doesn't click.
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter && ReferenceEquals(e.Source, this))
            {
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
    }

    private protected override AutomationControlType ControlType => AutomationControlType.CheckBox;
}
