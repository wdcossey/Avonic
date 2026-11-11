using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>#header</c> in <c>ion-accordion</c>'s shadow root: a <see cref="ContentPresenter"/> whose <c>pointer-events: none</c> for
/// <c>readonly</c> it holds itself, bound at style priority as it's made, before any style reaches it, so that an app's style on
/// it still wins, as <c>::part(header)</c> does. Styled as a <see cref="ContentPresenter"/>.
/// </summary>
public sealed class AccordionHeaderPresenter : ContentPresenter
{
    public AccordionHeaderPresenter() => Bind(IsHitTestVisibleProperty, NoHit, BindingPriority.Style);

    /// <summary><c>:host(.accordion-readonly) #header { pointer-events: none }</c>.</summary>
    internal StyleValue<bool> NoHit { get; } = new();

    protected override Type StyleKeyOverride => typeof(ContentPresenter);
}
