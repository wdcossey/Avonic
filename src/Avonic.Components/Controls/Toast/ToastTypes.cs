namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>ToastPosition</c>: where an <see cref="AvonicToast"/> is, before <see cref="AvonicToast.PositionAnchor"/>.</summary>
public enum ToastPosition
{
    /// <summary><c>'bottom'</c>, the default: 8 from the bottom, above the bottom safe area.</summary>
    Bottom,

    /// <summary><c>'top'</c>: 8 from the top, below the top safe area.</summary>
    Top,

    /// <summary><c>'middle'</c>: centred, top to bottom.</summary>
    Middle,
}

/// <summary>Ionic's <c>ToastLayout</c>.</summary>
public enum ToastLayout
{
    /// <summary><c>'baseline'</c>, the default: the message and the buttons on one line; the message wraps.</summary>
    Baseline,

    /// <summary><c>'stacked'</c>: the buttons on a line of their own, under the message (above it, for start buttons).</summary>
    Stacked,
}

/// <summary>Ionic's <c>ToastSwipeGestureDirection</c>.</summary>
public enum ToastSwipeGestureDirection
{
    /// <summary><c>'vertical'</c>: swiped up at the top, down at the bottom, either way in the middle, to dismiss it.</summary>
    Vertical,
}

/// <summary>Ionic's <c>ToastButton.side</c>.</summary>
public enum ToastButtonSide
{
    /// <summary><c>'start'</c>: before the icon and the content.</summary>
    Start,

    /// <summary><c>'end'</c>, which any value but <c>'start'</c> is: after the content.</summary>
    End,
}

/// <summary>
/// Ionic's <c>ToastPresentOptions</c> and <c>ToastDismissOptions</c>: what an <see cref="AvonicToast"/> gives its enter and leave
/// animations. <see cref="Top"/> and <see cref="Bottom"/> are the wrapper's <c>translateY</c> in px, worked out at present: 8 from
/// the edge and the safe area, or the anchor's edge. Ionic's are CSS strings, with <c>var(--ion-safe-area-…)</c> in them.
/// </summary>
public sealed record ToastPresentOptions(ToastPosition Position, double Top, double Bottom);
