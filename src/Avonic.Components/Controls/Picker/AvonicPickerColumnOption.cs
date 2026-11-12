using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-picker-column-option</c>: one row of an <see cref="AvonicPickerColumn"/>. A click on it sets the column's value to
/// its own. The column marks the option at its centre <c>:option-active</c>, which shows the option's colour. Its text is
/// <see cref="ContentControl.Content"/>. See <c>docs/components/ion-picker/README.md</c>.
/// </summary>
[PseudoClasses(OptionActive)]
public class AvonicPickerColumnOption : ContentControl
{
    /// <summary>Ionic's <c>option-active</c> class, which the column sets on the option at its centre.</summary>
    internal const string OptionActive = ":option-active";

    /// <summary>Ionic <c>color</c>: the active option's text colour. <c>primary</c> unless set, as Ionic's default is.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicPickerColumnOption>();

    /// <summary>Ionic <c>value</c>: what the column's value becomes when this option is chosen.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicPickerColumnOption, object?>(nameof(Value));

    private readonly StyleValue<string?> _defaultColor = new();

    static AvonicPickerColumnOption()
    {
        IsEnabledProperty.Changed.AddClassHandler<AvonicPickerColumnOption>((option, _) => option.Column?.OnOptionChanged());
        IsVisibleProperty.Changed.AddClassHandler<AvonicPickerColumnOption>((option, _) => option.Column?.OnOptionChanged());
    }

    public AvonicPickerColumnOption()
    {
        // color = 'primary', at style priority so that an app's style still wins.
        Bind(ColorProperty, _defaultColor, BindingPriority.Style);
        _defaultColor.Set("primary");

        // onClick, on the div with role="button" inside: a DOM click on release, with no keys.
        _ = new NativeButton(this, _ => OnClick(), () => false, () => { });
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Ionic's <c>pickerColumn</c>: the closest column, found when it's attached.</summary>
    internal AvonicPickerColumn? Column { get; private set; }

    /// <summary>Whether it's the option at the column's centre, as Ionic's <c>option-active</c> class.</summary>
    internal bool IsActive => Classes.Contains(OptionActive);

    internal void SetActive(bool active) => PseudoClasses.Set(OptionActive, active);

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        // connectedCallback: el.closest('ion-picker-column').
        Column = this.FindLogicalAncestorOfType<AvonicPickerColumn>();
        // componentDidLoad: the column centres its value again, as the option may be the one it shows.
        Column?.OnOptionLoaded();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        Column?.OnOptionChanged();
        Column = null;
        base.OnDetachedFromLogicalTree(e);
    }

    private void OnClick() => Column?.SetValue(Value);

    protected override AutomationPeer OnCreateAutomationPeer() => new OptionPeer(this);

    /// <summary>The inner div's <c>role="button"</c>.</summary>
    private sealed class OptionPeer(AvonicPickerColumnOption owner) : ContentControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }
}
