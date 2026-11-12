namespace Avonic.Components.Controls;

/// <summary>
/// A field whose <c>start</c> and <c>end</c> slots give what's in them the margins <c>input.md.css</c> and <c>textarea.md.css</c>
/// both give, word for word: <see cref="AvonicInput"/> and <see cref="AvonicTextarea"/>. It's a type, for one
/// <c>:is(c|IInputHost)</c> rule in place of one per host, which every element in an app would pay for.
/// </summary>
public interface IInputHost;
