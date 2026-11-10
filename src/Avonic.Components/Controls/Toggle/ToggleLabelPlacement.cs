namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-toggle</c> <c>labelPlacement</c>: where the label sits beside the track.</summary>
public enum ToggleLabelPlacement
{
    /// <summary>After the track: to its right in LTR, and its left in RTL.</summary>
    End,

    /// <summary>As <see cref="Start"/>, with the label 100 px wide, trimmed with an ellipsis.</summary>
    Fixed,

    /// <summary>Above the track, at 0.75 scale. <see cref="AvonicToggle.Alignment"/> places the two across.</summary>
    Stacked,

    /// <summary>Before the track: to its left in LTR, and its right in RTL. The default.</summary>
    Start,
}
