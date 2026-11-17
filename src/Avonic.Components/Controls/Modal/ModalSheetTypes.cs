using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>ModalHandleBehavior</c>: what a press on a sheet modal's handle does.</summary>
public enum ModalHandleBehavior
{
    /// <summary>Nothing: the handle is decoration, out of the tab order.</summary>
    None,

    /// <summary>The sheet goes to its next breakpoint, other than 0, and from the last back to the first.</summary>
    Cycle,
}

/// <summary>Ionic's <c>ModalBreakpointChangeEventDetail</c>, for <see cref="AvonicModal.IonBreakpointDidChange"/>.</summary>
public sealed class ModalBreakpointChangeEventArgs(RoutedEvent routedEvent, double breakpoint) : RoutedEventArgs(routedEvent)
{
    /// <summary>The breakpoint the sheet is at now.</summary>
    public double Breakpoint { get; } = breakpoint;
}

/// <summary>
/// Ionic's <c>ModalDragEventDetail</c>, for <see cref="AvonicModal.IonDragMove"/> and <see cref="AvonicModal.IonDragEnd"/>.
/// Positions are in the top level's coordinates, as the gesture's are.
/// </summary>
public sealed class ModalDragEventArgs : RoutedEventArgs
{
    internal ModalDragEventArgs(
        RoutedEvent routedEvent, double currentY, double deltaY, double velocityY, double progress, double? snapBreakpoint, bool? isDismissing)
        : base(routedEvent)
    {
        CurrentY = currentY;
        DeltaY = deltaY;
        VelocityY = velocityY;
        Progress = progress;
        SnapBreakpoint = snapBreakpoint;
        IsDismissing = isDismissing;
    }

    /// <summary>Ionic <c>currentY</c>: where the pointer is.</summary>
    public double CurrentY { get; }

    /// <summary>Ionic <c>deltaY</c>: how far it's moved since the drag started.</summary>
    public double DeltaY { get; }

    /// <summary>Ionic <c>velocityY</c>, in pixels per millisecond.</summary>
    public double VelocityY { get; }

    /// <summary>Ionic <c>progress</c>: 0 at the lowest breakpoint and 1 at the highest.</summary>
    public double Progress { get; }

    /// <summary>Ionic <c>snapBreakpoint</c>: where it would settle if it were let go now.</summary>
    public double? SnapBreakpoint { get; }

    /// <summary>Ionic <c>isDismissing</c>, on <see cref="AvonicModal.IonDragEnd"/> only: whether the release dismisses it, or may.</summary>
    public bool? IsDismissing { get; }
}

/// <summary>
/// Ionic's <c>ModalAnimationOptions</c>, what <see cref="AvonicModal"/> gives an enter or leave animation: the breakpoint it's at,
/// or null for a modal that isn't a sheet, and its other sheet props. <c>presentingEl</c> is ios only.
/// </summary>
public sealed record ModalAnimationOptions(double? CurrentBreakpoint, double BackdropBreakpoint, bool ExpandToScroll);
