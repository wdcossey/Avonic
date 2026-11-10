namespace Avonic.Components.Controls;

/// <summary>Ionic <c>position</c> of <c>ion-infinite-scroll</c>: which end of the content it loads more at.</summary>
public enum InfiniteScrollPosition
{
    /// <summary>The end: it fires when the scroll comes near the bottom.</summary>
    Bottom,

    /// <summary>The start: it starts scrolled to the end, fires near the top, and keeps what's shown in place as rows are added above.</summary>
    Top,
}
