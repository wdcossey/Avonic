using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-breadcrumbs", Order = 33)]
public partial class BreadcrumbsPage : UserControl
{
    private static readonly string[] Path = ["Home", "Electronics", "Photography", "Cameras"];

    public BreadcrumbsPage()
    {
        InitializeComponent();
        ShowPath(Path.Length);
    }

    /// <summary>
    /// A trail of links, each followed from its <c>Click</c>, as there's no router: the trail ends at the one clicked, and the
    /// breadcrumbs makes the new last one active.
    /// </summary>
    private void ShowPath(int length)
    {
        Trail.Items.Clear();
        for (var index = 0; index < length; index++)
        {
            var depth = index + 1;
            var last = depth == length;
            var breadcrumb = new AvonicBreadcrumb { Content = Path[index], Href = last ? null : "#" + Path[index].ToLowerInvariant() };
            breadcrumb.Click += (_, _) => ShowPath(depth);
            Trail.Items.Add(breadcrumb);
        }

        Showing.Text = "Showing " + Path[length - 1] + ". Click a link to go back to it.";
    }

    /// <summary>What an app would list in a popover: the breadcrumbs behind the indicator. Then the trail shows them all.</summary>
    private void OnCollapsedClick(object? sender, BreadcrumbCollapsedClickEventArgs e)
    {
        Hidden.Text = "Hidden: " + string.Join(", ", e.CollapsedBreadcrumbs.Select(breadcrumb => breadcrumb.Content));
        Collapsed.MaxItems = null;
    }
}
