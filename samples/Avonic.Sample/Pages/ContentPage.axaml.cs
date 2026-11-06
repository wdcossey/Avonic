using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-content", Order = 11)]
public partial class ContentPage : UserControl
{
    public ContentPage()
    {
        InitializeComponent();

        for (var i = 1; i <= 40; i++)
        {
            Rows.Children.Add(new AvonicItem { Content = new AvonicLabel { Content = $"Row {i}" } });
        }
    }

    private void OnTop(object? sender, RoutedEventArgs e) => _ = Main.ScrollToTop(300);

    private void OnBottom(object? sender, RoutedEventArgs e) => _ = Main.ScrollToBottom(500);

    private void OnBy(object? sender, RoutedEventArgs e) => _ = Main.ScrollByPoint(0, 200, 300);

    private void OnColor(object? sender, RoutedEventArgs e) => Main.Color = Main.Color is null ? "primary" : null;

    private void OnFixed(object? sender, RoutedEventArgs e) => Status.Text = "The fixed button stays put";

    private void OnScrollStart(object? sender, ScrollBaseDetailEventArgs e) => Status.Text = "Started";

    private void OnScroll(object? sender, ScrollDetailEventArgs e) =>
        Status.Text = string.Create(CultureInfo.InvariantCulture, $"Top {e.ScrollTop:0}, {e.VelocityY:0.00} px/ms");

    private void OnScrollEnd(object? sender, ScrollBaseDetailEventArgs e) =>
        Status.Text = string.Create(CultureInfo.InvariantCulture, $"Ended at {Main.GetScrollElement()!.Offset.Y:0}");
}
