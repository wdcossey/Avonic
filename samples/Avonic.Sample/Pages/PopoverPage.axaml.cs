using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-popover", Order = 43)]
public partial class PopoverPage : UserControl
{
    public PopoverPage()
    {
        InitializeComponent();
        foreach (var popover in new[] { MenuPopover, SubmenuPopover, HoverPopover, ContextPopover })
        {
            popover.DidDismiss += (_, e) => Status.Text = $"{popover.Name} dismissed, role {e.Role ?? "none"}";
        }

        foreach (var item in new[] { MenuCopy, MenuPaste, SubmenuShare, SubmenuPrint })
        {
            item.Click += (_, _) => Status.Text = $"{item.Content} picked";
        }

        // Presented by the click, beside the button it came from.
        OpenInCode.Click += (_, e) => new AvonicPopover
        {
            Content = new AvonicContent
            {
                Padding = new Avalonia.Thickness(16),
                Content = new TextBlock { Text = "Made in code, presented with the click's event", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            },
        }.Present(PopoverEvent.From(e));
    }
}
