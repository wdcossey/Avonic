using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>RefresherEventDetail</c>, the detail of <c>ionRefresh</c>: <c>complete()</c>, to call when the refresh is done.</summary>
public sealed class RefresherEventArgs : RoutedEventArgs
{
    private readonly Action _complete;

    internal RefresherEventArgs(RoutedEvent routedEvent, Action complete)
        : base(routedEvent)
    {
        _complete = complete;
    }

    /// <summary>Ionic <c>complete()</c>: the same as <see cref="AvonicRefresher.Complete"/> on the refresher that raised it.</summary>
    public void Complete() => _complete();
}

/// <summary>Ionic's <c>RefresherPullEndEventDetail</c>, the detail of <c>ionPullEnd</c>.</summary>
public sealed class RefresherPullEndEventArgs : RoutedEventArgs
{
    internal RefresherPullEndEventArgs(RoutedEvent routedEvent, RefresherPullEndReason reason)
        : base(routedEvent)
    {
        Reason = reason;
    }

    /// <summary>Ionic <c>reason</c>: whether the refresh completed or was cancelled.</summary>
    public RefresherPullEndReason Reason { get; }
}

/// <summary>Ionic's <c>'complete' | 'cancel'</c>, the <c>reason</c> of <c>ionPullEnd</c>.</summary>
public enum RefresherPullEndReason
{
    /// <summary><c>complete</c>: the refresher closed after <see cref="AvonicRefresher.Complete"/>.</summary>
    Complete,

    /// <summary><c>cancel</c>: the refresher closed after <see cref="AvonicRefresher.Cancel"/>, or a pull that wasn't far enough.</summary>
    Cancel,
}
