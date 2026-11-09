namespace Avonic.Components.Animation;

/// <summary>Ionic's <c>AnimationDirection</c>, Web Animations' <c>direction</c>.</summary>
public enum AnimationDirection
{
    /// <summary><c>'normal'</c>, Ionic's default.</summary>
    Normal,

    /// <summary><c>'reverse'</c>.</summary>
    Reverse,

    /// <summary><c>'alternate'</c>: every other iteration runs backwards, starting forwards.</summary>
    Alternate,

    /// <summary><c>'alternate-reverse'</c>: every other iteration runs backwards, starting backwards.</summary>
    AlternateReverse,
}
