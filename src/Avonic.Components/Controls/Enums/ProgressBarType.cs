namespace Avonic.Components.Controls.Enums;

/// <summary>Animation style for <see cref="AvonicProgressBar"/>.</summary>
public enum ProgressBarType
{
    /// <summary>Shows a filled bar proportional to <see cref="AvonicProgressBar.Value"/>.</summary>
    Determinate,

    /// <summary>Shows a looping animation — use when progress is unknown.</summary>
    Indeterminate
}
