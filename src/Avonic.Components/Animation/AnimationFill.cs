namespace Avonic.Components.Animation;

/// <summary>Ionic's <c>AnimationFill</c>, Web Animations' <c>fill</c>: whether an animation's values show before and after it runs.</summary>
public enum AnimationFill
{
    /// <summary><c>'auto'</c>, which is <c>'none'</c> for an animation of an element's style.</summary>
    Auto,

    /// <summary><c>'none'</c>.</summary>
    None,

    /// <summary><c>'forwards'</c>: the last value stays after the end.</summary>
    Forwards,

    /// <summary><c>'backwards'</c>: the first value shows during the delay.</summary>
    Backwards,

    /// <summary><c>'both'</c>, Ionic's default.</summary>
    Both,
}
