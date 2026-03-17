namespace Avonic.Components.Controls.Enums;

/// <summary>
/// Controls how the label and interactive control are packed within a line.
/// </summary>
/// <remarks>
/// Ionic reference: <c>justify</c> prop on ion-checkbox, ion-toggle, ion-radio.
/// </remarks>
public enum CheckableJustify
{
    /// <summary>
    /// The label and control appear on opposite ends of the line with space between them. Default.
    /// </summary>
    SpaceBetween,

    /// <summary>The label and control appear at the start of the line (left in LTR, right in RTL).</summary>
    Start,

    /// <summary>The label and control appear at the end of the line (right in LTR, left in RTL).</summary>
    End,
}
