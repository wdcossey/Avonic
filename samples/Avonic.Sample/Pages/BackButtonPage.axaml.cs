using Avalonia.Controls;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-back-button", Order = 47)]
public partial class BackButtonPage : UserControl
{
    public BackButtonPage()
    {
        InitializeComponent();

        // The app's router: one handler for every back button on the page, as one ion-router serves a document.
        AddHandler(AvonicBackButton.DefaultHrefRequestedEvent, OnDefaultHrefRequested);
        Nav.Root = (Func<Control>)(() => new BackButtonSampleView(1));
        Nav.IonNavDidChange += (_, _) => NavStatus.Text = "Stack: " + string.Join(" > ",
            Enumerable.Range(0, Nav.GetLength()).Select(i => (Nav.GetByIndex(i)?.Element as BackButtonSampleView)?.Title ?? "?"))
            + $" ({Nav.GetLength()})";
    }

    private void OnDefaultHrefRequested(object? sender, BackButtonDefaultHrefEventArgs e) =>
        HrefStatus.Text = $"Asked to go back to {e.DefaultHref}";
}

/// <summary>A page of the sample's nav: a header with a back button and a title, and a button that pushes the next page.</summary>
public sealed class BackButtonSampleView : UserControl
{
    public BackButtonSampleView(int depth, object? defaultPage = null)
    {
        Title = $"Page {depth}";
        var back = new AvonicBackButton { Name = "NavBackButton", DefaultPage = defaultPage };
        var header = new AvonicHeader
        {
            Items =
            {
                new AvonicToolbar
                {
                    StartContent = new AvonicButtons { Items = { back } },
                    Content = new AvonicTitle { Content = Title },
                },
            },
        };
        DockPanel.SetDock(header, Dock.Top);

        var push = new AvonicButton { Name = "PushButton", Content = $"Push page {depth + 1}" };
        push.Click += (_, _) => _ = this.FindAncestorOfType<AvonicNav>()?.Push((Func<Control>)(() => new BackButtonSampleView(depth + 1)));

        // A deep link: page 5 as the root, with nothing under it. Its back button shows by its DefaultPage (an extension), and goes
        // back to page 1 as the new root.
        var deepLink = new AvonicButton { Name = "DeepLinkButton", Content = "Open page 5 as the root", Fill = ButtonFill.Outline };
        deepLink.Click += (_, _) => _ = this.FindAncestorOfType<AvonicNav>()?.SetRoot(
            (Func<Control>)(() => new BackButtonSampleView(5, (Func<Control>)(() => new BackButtonSampleView(1)))));

        Content = new DockPanel
        {
            Children =
            {
                header,
                new AvonicContent
                {
                    Content = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(16),
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock
                            {
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                                Text = defaultPage is not null
                                    ? "A root with a default page: its back button shows, and sets page 1 as the root, going back."
                                    : depth == 1
                                        ? "The root page: its back button is hidden, as there's nothing under it."
                                        : "Its back button shows, as the nav marked the page can-go-back; it pops.",
                            },
                            push,
                            deepLink,
                        },
                    },
                },
            },
        };
    }

    public string Title { get; }
}
