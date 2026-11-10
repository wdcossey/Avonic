namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-input</c> <c>labelPlacement</c>.</summary>
public enum InputLabelPlacement
{
    /// <summary>Before the field: left in LTR, right in RTL. The default.</summary>
    Start,

    /// <summary>After the field.</summary>
    End,

    /// <summary>As <see cref="Start"/>, at a fixed 100 px, trimmed with an ellipsis.</summary>
    Fixed,

    /// <summary>On the field, until it has a value, focus, or anything in a slot; then small, above it.</summary>
    Floating,

    /// <summary>Small, above the field, always.</summary>
    Stacked,
}

/// <summary>Ionic <c>ion-input</c> <c>fill</c>. Null is no fill.</summary>
public enum InputFill
{
    /// <summary>Transparent, with a border all round and a notch for the label.</summary>
    Outline,

    /// <summary>A background and a bottom border.</summary>
    Solid,
}

/// <summary>Ionic <c>ion-input</c> <c>shape</c>. Null is the default.</summary>
public enum InputShape
{
    /// <summary>A larger radius.</summary>
    Round,
}

/// <summary>
/// Ionic <c>ion-input</c> <c>type</c>: the soft keyboard, and masking for <see cref="Password"/>. The date and time types aren't
/// here: Chromium gives them its own picker. See <c>docs/components/ion-input/PLAN.md</c>.
/// </summary>
public enum InputType
{
    /// <summary>Plain text. The default.</summary>
    Text,

    /// <summary>An email address.</summary>
    Email,

    /// <summary>A number.</summary>
    Number,

    /// <summary>Masked, and sensitive. Clears on edit by default.</summary>
    Password,

    /// <summary>A search.</summary>
    Search,

    /// <summary>A telephone number.</summary>
    Tel,

    /// <summary>A URL.</summary>
    Url,
}

/// <summary>Ionic <c>ion-input</c> <c>inputmode</c>: which soft keyboard, beating <see cref="InputType"/>'s. Null leaves it to the type.</summary>
public enum InputInputmode
{
    /// <summary>Numbers with a decimal separator.</summary>
    Decimal,

    /// <summary>An email address.</summary>
    Email,

    /// <summary>Digits only.</summary>
    Numeric,

    /// <summary>A search.</summary>
    Search,

    /// <summary>A telephone number.</summary>
    Tel,

    /// <summary>Plain text.</summary>
    Text,

    /// <summary>A URL.</summary>
    Url,
}

/// <summary>Ionic <c>ion-input</c> <c>enterkeyhint</c>: what the soft keyboard's Enter key says. Null is the platform's.</summary>
public enum InputEnterkeyhint
{
    /// <summary>Done.</summary>
    Done,

    /// <summary>A plain Enter, a new line.</summary>
    Enter,

    /// <summary>Go.</summary>
    Go,

    /// <summary>Next.</summary>
    Next,

    /// <summary>Previous.</summary>
    Previous,

    /// <summary>Search.</summary>
    Search,

    /// <summary>Send.</summary>
    Send,
}

/// <summary>Ionic <c>ion-input</c> <c>autocorrect</c>.</summary>
public enum InputAutocorrect
{
    /// <summary>No suggestions. The default.</summary>
    Off,

    /// <summary>The keyboard suggests and corrects.</summary>
    On,
}
