namespace Avonic.Components.Controls.Enums;

/// <summary>
/// Controls where the label is rendered relative to the input field.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-input</c> / <c>ion-textarea</c> <c>labelPlacement</c> prop.
/// </remarks>
public enum InputLabelPlacement
{
    /// <summary>Label appears to the left of the input (default).</summary>
    Start,

    /// <summary>Label appears to the right of the input.</summary>
    End,

    /// <summary>Label appears to the left with a fixed width, aligning multiple inputs.</summary>
    Fixed,

    /// <summary>Label appears above the input.</summary>
    Stacked,

    /// <summary>
    /// Label starts inside the input and floats above it when the input is focused or has a value.
    /// </summary>
    Floating,
}
