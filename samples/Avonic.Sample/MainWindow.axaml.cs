using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Sample.Pages;

namespace Avonic.Sample;

public partial class MainWindow : Window
{
    /// <summary>One entry per Avonic control, in roadmap order.</summary>
    private static readonly (string Title, Func<Control> Create)[] Pages =
    [
        ("ion-ripple-effect", () => new RippleEffectPage()),
    ];

    public MainWindow()
    {
        InitializeComponent();
        ShowIndex();
    }

    private void ShowIndex()
    {
        var list = new StackPanel { Margin = new(16) };
        foreach (var (title, create) in Pages)
        {
            var button = new Button { Content = title, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, Margin = new(0, 0, 0, 8) };
            button.Click += (_, _) => ShowPage(title, create());
            list.Children.Add(button);
        }

        PageTitle.Text = "Avonic";
        BackButton.IsVisible = false;
        Host.Content = list;
    }

    private void ShowPage(string title, Control page)
    {
        PageTitle.Text = title;
        BackButton.IsVisible = true;
        Host.Content = new ScrollViewer { Content = page };
    }

    private void OnBack(object? sender, RoutedEventArgs e) => ShowIndex();
}
