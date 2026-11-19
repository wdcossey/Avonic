using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>.toast-content</c>: the header and the message, in a live region (<c>role="status"</c>, <c>aria-live="polite"</c>), as
/// <see cref="AutomationLiveSetting.Polite"/> by default, so an app's own value wins. Its converters read <c>white-space</c> for the
/// message (<c>--white-space</c>) and the header (<c>normal</c>).
/// </summary>
public partial class ToastContent : Border
{
    static ToastContent()
    {
        AutomationProperties.LiveSettingProperty.OverrideDefaultValue<ToastContent>(AutomationLiveSetting.Polite);
    }

    /// <summary>
    /// The text as <c>white-space</c> shows it: <c>normal</c> and <c>nowrap</c> collapse each run of spaces, tabs and line breaks to
    /// one space, and drop it at the ends; <c>pre-line</c> collapses spaces and keeps the breaks; <c>pre</c>, <c>pre-wrap</c> and
    /// <c>break-spaces</c> keep it as it is. Values: the text, then the <c>white-space</c> (a string, <c>normal</c> when unset).
    /// </summary>
    public static readonly IMultiValueConverter WhiteSpaceText = new WhiteSpaceTextConverter();

    /// <summary>
    /// <c>white-space</c> as a <see cref="TextWrapping"/>: <c>nowrap</c> and <c>pre</c> don't wrap; the others do. The value, a
    /// string, <c>normal</c> when unset.
    /// </summary>
    public static readonly IMultiValueConverter WhiteSpaceWrapping = new WhiteSpaceWrappingConverter();

    /// <summary>A <see cref="AutomationControlType.Group"/>, Avalonia's nearest to <c>role="status"</c>: a live region of what's in it.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new ContentPeer(this);

    /// <summary>The content's children, shown to automation again: a new announcement, as Ionic renders new elements.</summary>
    internal void Revealed() => (ControlAutomationPeer.FromElement(this) as ContentPeer)?.Changed();

    private static string Mode(object? value) => value is string s && !string.IsNullOrWhiteSpace(s) ? s.Trim().ToLowerInvariant() : "normal";

    [GeneratedRegex(@"[ \t\r\n\f]+")]
    private static partial Regex AnyWhiteSpace();

    [GeneratedRegex(@"[ \t\f]*(\r\n|\r|\n)[ \t\f]*")]
    private static partial Regex BreakAndSpaces();

    [GeneratedRegex(@"[ \t\f]+")]
    private static partial Regex Spaces();

    private sealed class WhiteSpaceTextConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 0 || values[0] is not string text)
            {
                return null;
            }

            switch (Mode(values.Count > 1 ? values[1] : null))
            {
                case "pre":
                case "pre-wrap":
                case "break-spaces":
                    return text;
                case "pre-line":
                    var lines = BreakAndSpaces().Replace(text, "\n");
                    return Spaces().Replace(lines, " ").Trim(' ');
                default:
                    return AnyWhiteSpace().Replace(text, " ").Trim(' ');
            }
        }
    }

    private sealed class WhiteSpaceWrappingConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            Mode(values.Count > 0 ? values[0] : null) is "nowrap" or "pre" ? TextWrapping.NoWrap : TextWrapping.Wrap;
    }

    private sealed class ContentPeer(ToastContent owner) : ControlAutomationPeer(owner)
    {
        public void Changed() => InvalidateChildren();

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;
    }
}
