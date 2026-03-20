namespace Avonic.Components.Controls;

/// <summary>Event args for <see cref="AvonicSearchbar.TextChanged"/>.</summary>
public class SearchTextChangedEventArgs : RoutedEventArgs
{
    public string? Text { get; }

    public SearchTextChangedEventArgs(RoutedEvent routedEvent, object source, string? text)
        : base(routedEvent, source)
    {
        Text = text;
    }
}
