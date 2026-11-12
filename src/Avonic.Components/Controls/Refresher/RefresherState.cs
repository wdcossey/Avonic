namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>RefresherState</c>, with its values, which are flags: <c>_BUSY_</c> is refreshing, cancelling or completing. Internal,
/// as Ionic's is a <c>@State</c>: the refresher shows it through pseudoclasses.
/// </summary>
internal enum RefresherState
{
    /// <summary>Not pulled, and hidden.</summary>
    Inactive = 1,

    /// <summary>Being pulled, not yet far enough to refresh.</summary>
    Pulling = 2,

    /// <summary>Pulled far enough that letting go refreshes. Only the legacy refresher has it.</summary>
    Ready = 4,

    /// <summary>Waiting for the app to call <see cref="AvonicRefresher.Complete"/>.</summary>
    Refreshing = 8,

    /// <summary>Closing after <see cref="AvonicRefresher.Cancel"/>, or a pull that wasn't far enough.</summary>
    Cancelling = 16,

    /// <summary>Closing after <see cref="AvonicRefresher.Complete"/>.</summary>
    Completing = 32,
}
