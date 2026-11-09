using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avonic.Sample.Pages;

[SamplePage("ion-skeleton-text", Order = 17)]
public partial class SkeletonTextPage : UserControl
{
    public SkeletonTextPage() => InitializeComponent();

    private void OnToggle(object? sender, RoutedEventArgs e) => Toggled.Animated = !Toggled.Animated;
}
