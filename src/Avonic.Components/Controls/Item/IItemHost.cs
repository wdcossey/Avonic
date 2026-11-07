namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's class <c>item</c>, which <c>ion-item</c>, <c>ion-item-divider</c> and <c>ion-item-group</c> have, and <c>label.md.css</c>'s
/// <c>:host-context(.item)</c> reads. It's a type, for <c>:is(c|IItemHost) c|AvonicLabel</c>, rather than a pseudoclass, as a
/// pseudoclass would raise those rules over an app's own plain style, and put an activator on every ancestor of every label.
/// </summary>
public interface IItemHost;
