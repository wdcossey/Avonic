using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// The <c>.alert-input-wrapper</c> an <see cref="AvonicAlert"/> renders for a text <see cref="AlertInput"/>, with its native field,
/// <c>PART_Native</c>: <see cref="AlertTextInput"/>'s <c>&lt;input&gt;</c> or <see cref="AlertTextareaInput"/>'s
/// <c>&lt;textarea&gt;</c>. Each edit writes the field's text to the alert's copy of the input, as Ionic's <c>onInput</c> does.
/// </summary>
/// <remarks>
/// <c>width: 100%</c>: it wants no width of its own, so that what's typed doesn't widen the alert, as an <c>&lt;input&gt;</c>'s
/// intrinsic width comes from its <c>size</c>, not its value; the wrapper's minimum of 250 is wider than that anyway.
/// </remarks>
[TemplatePart("PART_Native", typeof(TextBox))]
public abstract class AlertTextField : TemplatedControl
{
    private TextBox? _native;

    /// <summary>The processed input it shows.</summary>
    internal AlertInput? Input { get; set; }

    /// <summary>The native field.</summary>
    internal TextBox? Native => _native;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_native is not null)
        {
            _native.PropertyChanged -= OnNativePropertyChanged;
        }

        _native = e.NameScope.Find<TextBox>("PART_Native");
        if (_native is not null && Input is { } input)
        {
            Configure(_native, input);
            _native.PropertyChanged += OnNativePropertyChanged;
        }
    }

    protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize).WithWidth(0);

    /// <summary><c>onInput</c>: the value is the field's text, on every change the field makes.</summary>
    private void OnNativePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty && Input is { } input)
        {
            input.Value = e.NewValue as string ?? "";
        }
    }

    /// <summary>What <c>renderInput</c> puts on the native field, with <c>attributes</c> spread after it.</summary>
    private static void Configure(TextBox native, AlertInput input)
    {
        var attributes = input.Attributes ?? new Dictionary<string, object?>();
        string? Attribute(string name) => OverlayAttributes.Read(attributes, name);

        native.Text = Convert.ToString(input.Value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
        int? maxlength = int.TryParse(Attribute("maxlength"), out var max) ? max : null;
        var type = input.Type ?? AlertInputType.Text;
        NativeTextField.Apply(native, maxlength, input.Placeholder ?? "", ContentType(Attribute("inputmode"), type), null,
            Attribute("autocapitalize"));
        if (type == AlertInputType.Password)
        {
            native.PasswordChar = NativeTextField.PasswordMask;
        }

        if (attributes.TryGetValue("readonly", out var readOnly))
        {
            native.IsReadOnly = AlertClasses.IsTrue(readOnly);
        }

        if (input.Id is { } id)
        {
            AutomationProperties.SetAutomationId(native, id);
        }

        // Chromium names a field by its aria-label, else its placeholder.
        if (Attribute("aria-label") is { Length: > 0 } label)
        {
            AutomationProperties.SetName(native, label);
        }
        else if (!string.IsNullOrEmpty(input.Placeholder))
        {
            AutomationProperties.SetName(native, input.Placeholder);
        }
    }

    /// <summary>The keyboard: <c>inputmode</c> if it's set, as it beats the type's, else the type's.</summary>
    private static TextInputContentType ContentType(string? inputmode, AlertInputType type) =>
        NativeTextField.ContentType(Enum.TryParse<InputInputmode>(inputmode?.Trim(), ignoreCase: true, out var mode) ? mode : null, type);
}

/// <summary>An <see cref="AvonicAlert"/>'s <c>&lt;input class="alert-input"&gt;</c>, in its wrapper.</summary>
public class AlertTextInput : AlertTextField
{
}

/// <summary>An <see cref="AvonicAlert"/>'s <c>&lt;textarea class="alert-input"&gt;</c>, in its wrapper: two rows, as HTML's default.</summary>
public class AlertTextareaInput : AlertTextField
{
}
