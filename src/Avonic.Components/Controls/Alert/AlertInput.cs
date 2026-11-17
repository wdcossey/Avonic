namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>AlertInput</c>'s <c>type</c>: <c>TextFieldTypes</c>, <c>textarea</c>, <c>radio</c> and <c>checkbox</c>. An alert's
/// inputs are all text fields (any of the first, and <see cref="Textarea"/>), all radios, or all checkboxes.
/// </summary>
public enum AlertInputType
{
    Text,
    Date,
    Email,
    Number,
    Password,
    Search,
    Tel,
    Url,
    Time,
    Week,
    Month,
    DatetimeLocal,
    Textarea,
    Radio,
    Checkbox,
}

/// <summary>
/// Ionic's <c>AlertInput</c>: an input of an <see cref="AvonicAlert"/>, plain data, as Ionic's is. The alert works on a copy, as
/// Ionic's <c>processedInputs</c> are, so an app's object isn't changed; a <see cref="Handler"/> is given the copy.
/// </summary>
public sealed class AlertInput
{
    /// <summary>Ionic <c>type</c>; null is <see cref="AlertInputType.Text"/>.</summary>
    public AlertInputType? Type { get; set; }

    /// <summary>Ionic <c>name</c>: a text field's key in the values; its index when null.</summary>
    public string? Name { get; set; }

    /// <summary>Ionic <c>placeholder</c>.</summary>
    public string? Placeholder { get; set; }

    /// <summary>Ionic <c>value</c>: a radio's or checkbox's value, or a text field's text, which an edit replaces.</summary>
    public object? Value { get; set; }

    /// <summary>Ionic <c>label</c>: a radio's or checkbox's text.</summary>
    public string? Label { get; set; }

    /// <summary>Ionic <c>checked</c>.</summary>
    public bool Checked { get; set; }

    /// <summary>Ionic <c>disabled</c>.</summary>
    public bool Disabled { get; set; }

    /// <summary>Ionic <c>id</c>: the control's <c>AutomationId</c>; <c>alert-input-&lt;overlay index&gt;-&lt;index&gt;</c> when null.</summary>
    public string? Id { get; set; }

    /// <summary>Ionic <c>handler</c>: called with the alert's copy after a radio or checkbox is clicked.</summary>
    public Action<AlertInput>? Handler { get; set; }

    /// <summary>Ionic <c>cssClass</c>: style classes on the input's control, space-separated.</summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Ionic <c>attributes</c>, spread on a text field: <c>disabled</c>, <c>class</c>, <c>maxlength</c>, <c>readonly</c>,
    /// <c>aria-label</c>, <c>inputmode</c> and <c>autocapitalize</c> map; others have nothing to do in Avalonia, and are ignored.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Attributes { get; set; }

    internal AlertInput Copy() => (AlertInput)MemberwiseClone();
}
