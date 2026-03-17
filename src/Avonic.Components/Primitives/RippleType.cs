namespace Avonic.Components.Primitives;

/// <summary>The expansion behaviour of an <see cref="AvonicRipple"/>.</summary>
/// <remarks>
/// Ionic reference: <c>ion-ripple-effect</c> <c>type</c> prop ("bounded" | "unbounded").
/// </remarks>
public enum RippleType
{
    /// <summary>
    /// Ripple expands from the pointer origin toward the control centre.
    /// The surface should have <c>ClipToBounds = True</c>.
    /// </summary>
    Bounded,

    /// <summary>
    /// Ripple always expands from the control centre and may overflow the bounds.
    /// The surface should allow overflow.
    /// </summary>
    Unbounded,
}
