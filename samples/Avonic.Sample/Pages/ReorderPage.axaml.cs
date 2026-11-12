using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-reorder", Order = 34)]
public partial class ReorderPage : UserControl
{
    public ReorderPage()
    {
        InitializeComponent();

        // Enough items for the content to scroll, so that a drag near an edge scrolls it
        for (var i = 1; i <= 20; i++)
        {
            Group.Items.Add(new AvonicItem
            {
                Content = new AvonicLabel { Content = $"Item {i}" },
                EndContent = new AvonicReorder(),
            });
        }
    }

    /// <summary>What an Ionic app does in <c>ionReorderEnd</c>: say where it went, and complete.</summary>
    private void OnReorderEnd(object? sender, ReorderEndEventArgs e)
    {
        Status.Text = $"ionReorderEnd: from {e.From} to {e.To}";
        e.Complete();
    }

    private void OnReorderEndCancel(object? sender, ReorderEndEventArgs e)
    {
        Status.Text = $"ionReorderEnd: from {e.From} to {e.To}, then complete(false)";
        e.Complete(false);
    }
}
