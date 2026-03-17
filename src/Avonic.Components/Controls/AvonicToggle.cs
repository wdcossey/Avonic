using Avalonia.Input;

namespace Avonic.Components.Controls;

/// <summary>
/// A touch-first toggle switch with optional drag-to-toggle gesture.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-toggle</c><br/>
/// Drag logic mirrors Ionic's <c>shouldToggle</c>: a 5dp movement threshold activates
/// drag mode; 10dp directional margin commits the new state. A drag that does not
/// cross the margin is treated as a tap.
/// </remarks>
public class AvonicToggle : AvonicCheckableBase
{
    // ── Drag state ───────────────────────────────────────────────────────────

    private bool   _dragHandled;
    private double _dragStartX;

    // ── Pointer overrides ────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.Handled) return; // base rejected (disabled or non-left button)

        _dragHandled = false;
        _dragStartX  = e.GetPosition(this).X;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var deltaX = e.GetPosition(this).X - _dragStartX;

        if (Math.Abs(deltaX) < 5) return; // below activation threshold

        // LTR: right (+) → checked; left (−) → unchecked.
        // Ionic uses a 10dp directional margin before committing.
        var wantChecked = deltaX > 10  ? true
                        : deltaX < -10 ? false
                        : IsChecked;   // within margin — no state change yet

        if (wantChecked == IsChecked) return;

        IsChecked    = wantChecked;
        _dragHandled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _dragHandled = false; // cancel any in-flight drag without committing
    }

    // ── Toggle override ──────────────────────────────────────────────────────

    protected override void ToggleChecked()
    {
        if (_dragHandled)
        {
            // State was set live during drag — just broadcast the final value.
            _dragHandled = false;
            RaiseEvent(new CheckedChangedEventArgs(CheckedChangedEvent, this, IsChecked, Value));
            return;
        }

        base.ToggleChecked();
    }
}
