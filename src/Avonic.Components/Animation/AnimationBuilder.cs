using Avalonia.Controls;

namespace Avonic.Components.Animation;

/// <summary>
/// Ionic's <c>AnimationBuilder</c>, <c>(baseEl, opts?) =&gt; Animation</c>: what an overlay's <c>enterAnimation</c> and
/// <c>leaveAnimation</c> are, and its md animations.
/// </summary>
/// <param name="baseElement">The overlay.</param>
/// <param name="options">What the overlay gives its animations, such as a toast's position. Null for most.</param>
public delegate AvonicAnimation AnimationBuilder(Control baseElement, object? options);
