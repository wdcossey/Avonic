using Avalonia.Media;
using Avalonia.Metadata;

namespace Avonic.Components.Behaviors;

/// <summary>
/// A CSS <c>box-shadow</c> in XAML, as <see cref="BoxShadows"/> with the blur converted by <see cref="CssBoxShadow.Blur"/>, so the
/// numbers stay CSS's: <c>BoxShadow="{b:CssShadow '0 0 10px rgba(0, 0, 0, 0.4)'}"</c>, or as a resource,
/// <c>&lt;b:CssShadow x:Key="…"&gt;0 3px 1px -2px rgba(0, 0, 0, 0.2), 0 2px 2px 0 rgba(0, 0, 0, 0.14)&lt;/b:CssShadow&gt;</c>.
/// Avalonia's own <c>BoxShadow="0 0 10 0 #66000000"</c> reads the blur as its own, about two thirds of CSS's.
/// </summary>
public sealed class CssShadowExtension
{
    public CssShadowExtension()
    {
    }

    public CssShadowExtension(string value) => Value = value;

    /// <summary>The CSS value, as a <c>box-shadow</c> declaration has it.</summary>
    [Content]
    public string? Value { get; set; }

    public BoxShadows ProvideValue(IServiceProvider serviceProvider) =>
        CssBoxShadow.Parse(Value ?? throw new InvalidOperationException("CssShadow needs a box-shadow value."));
}
