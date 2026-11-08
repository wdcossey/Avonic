namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-progress-bar</c> <c>type</c>.</summary>
public enum ProgressBarType
{
    /// <summary>The bar shows <see cref="AvonicProgressBar.Value"/>, and the stream when <see cref="AvonicProgressBar.Buffer"/> is under 1.</summary>
    Determinate,

    /// <summary>Two bars slide across on a 2 s loop, as the time the process takes isn't known.</summary>
    Indeterminate,
}
