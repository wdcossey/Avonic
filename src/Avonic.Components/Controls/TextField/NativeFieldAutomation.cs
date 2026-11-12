using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Data;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// What <see cref="AvonicInput"/> and <see cref="AvonicTextarea"/> give their native field for a screen reader: its name
/// (<c>aria-labelledby</c>), its hint (<c>aria-describedby</c>) and <c>aria-required</c>. At style priority, so that an app's
/// style for the field wins, and bound in the field's own constructor, before any style reaches it: a binding made when the
/// host's template is applied comes after the field's styles, and of two values at one priority the later wins.
/// </summary>
internal sealed class NativeFieldAutomation : IDisposable
{
    private readonly IDisposable[] _bindings;

    /// <param name="field">The native field, bound now: from its constructor, or, for a plain <see cref="TextBox"/> in an app's
    /// own template, as late as it's found.</param>
    public NativeFieldAutomation(TextBox field)
    {
        _bindings =
        [
            field.Bind(AutomationProperties.NameProperty, Name, BindingPriority.Style),
            field.Bind(AutomationProperties.HelpTextProperty, HelpText, BindingPriority.Style),
            field.Bind(AutomationProperties.IsRequiredForFormProperty, IsRequiredForForm, BindingPriority.Style),
        ];
    }

    public StyleValue<string?> Name { get; } = new();

    public StyleValue<string?> HelpText { get; } = new();

    public StyleValue<bool> IsRequiredForForm { get; } = new();

    public void Dispose()
    {
        foreach (var binding in _bindings)
        {
            binding.Dispose();
        }
    }
}

/// <summary>A native field that binds its <see cref="NativeFieldAutomation"/> in its constructor.</summary>
internal interface INativeFieldAutomation
{
    NativeFieldAutomation Automation { get; }
}
