namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-spinner</c> <c>name</c> (<c>SpinnerTypes</c> in <c>spinner-configs.js</c>): which of the nine spinners to draw.
/// </summary>
public enum SpinnerName
{
    /// <summary><c>bubbles</c>: nine dots in a ring, each shrinking away in turn.</summary>
    Bubbles,

    /// <summary><c>circles</c>: eight dots in a ring, each fading in turn.</summary>
    Circles,

    /// <summary><c>circular</c>: an arc that grows and runs round a circle. md's default.</summary>
    Circular,

    /// <summary><c>crescent</c>: a fixed arc, turning.</summary>
    Crescent,

    /// <summary><c>dots</c>: three dots in a row, pulsing.</summary>
    Dots,

    /// <summary><c>lines</c>: eight long spokes, each fading in turn.</summary>
    Lines,

    /// <summary><c>lines-small</c>: eight short spokes.</summary>
    LinesSmall,

    /// <summary><c>lines-sharp</c>: twelve long, thin spokes.</summary>
    LinesSharp,

    /// <summary><c>lines-sharp-small</c>: twelve short, thin spokes.</summary>
    LinesSharpSmall,
}
