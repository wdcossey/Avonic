namespace Avonic.Components.Controls.Enums;

/// <summary>
/// Controls where the label is rendered relative to the range slider.
/// </summary>
/// <remarks>
/// Ionic reference: <c>labelPlacement</c> prop on <c>ion-range</c>.
/// </remarks>
public enum RangeLabelPlacement
{
    /// <summary>Label is rendered to the left of the slider. Default.</summary>
    Start,

    /// <summary>Label is rendered to the right of the slider.</summary>
    End,

    /// <summary>Same as <see cref="Start"/> but with a fixed width; long text is truncated with ellipsis.</summary>
    Fixed,

    /// <summary>Label is rendered above the slider.</summary>
    Stacked,
}
