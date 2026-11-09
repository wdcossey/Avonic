namespace Avonic.Components.Animation;

/// <summary>
/// Ionic's <c>AnimationKeyFrame</c>: an <see cref="Offset"/> and a value for each property, as
/// <c>{ offset: 0, opacity: 0.01, transform: 'scale(0.9)' }</c> is
/// <c>new AnimationKeyframe(0) { ["opacity"] = 0.01, ["transform"] = "scale(0.9)" }</c>. See
/// <see cref="AvonicAnimation.Keyframes"/> for the properties.
/// </summary>
public sealed class AnimationKeyframe : Dictionary<string, object>
{
    /// <param name="offset">Where it is, from 0 to 1. Null spaces it evenly between its neighbours, as Web Animations does.</param>
    public AnimationKeyframe(double? offset = null)
        : base(StringComparer.Ordinal)
    {
        if (offset is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "A keyframe's offset is from 0 to 1.");
        }

        Offset = offset;
    }

    /// <summary>Ionic <c>offset</c>.</summary>
    public double? Offset { get; }
}
