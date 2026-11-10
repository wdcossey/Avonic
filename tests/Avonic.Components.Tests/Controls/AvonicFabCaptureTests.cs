using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicFabCaptureTests
{
    private static AvonicIcon Icon(string name) => new() { IconName = name };

    private static AvonicFab Fab(FabHorizontal horizontal, FabVertical vertical, params Control[] items)
    {
        var fab = new AvonicFab { Horizontal = horizontal, Vertical = vertical };
        foreach (var item in items)
        {
            fab.Items.Add(item);
        }

        return fab;
    }

    private static AvonicFabList List(FabListSide side, params Control[] buttons)
    {
        var list = new AvonicFabList { Side = side };
        foreach (var button in buttons)
        {
            list.Items.Add(button);
        }

        return list;
    }

    /// <summary>
    /// The same content as the command at the top of docs/components/ion-fab/COMPARISON.md: a header, a content and a footer in a
    /// column, and six fabs in the content's fixed slot.
    /// </summary>
    internal static (Grid Page, AvonicFab[] Fabs) ReferencePage()
    {
        var edgeTop = Fab(FabHorizontal.Start, FabVertical.Top,
            new AvonicFabButton { Content = Icon("share") },
            List(FabListSide.Bottom, new AvonicFabButton { Content = Icon("logo-github") }, new AvonicFabButton { Content = Icon("heart") }),
            List(FabListSide.End, new AvonicFabButton { Content = Icon("star") }, new AvonicFabButton { Content = Icon("pin") }));
        edgeTop.Edge = true;
        edgeTop.Activated = true;

        var bottomEnd = Fab(FabHorizontal.End, FabVertical.Bottom,
            new AvonicFabButton { Content = Icon("add") },
            List(FabListSide.Top, new AvonicFabButton { Content = "A" }, new AvonicFabButton { Content = "B" }),
            List(FabListSide.Start, new AvonicFabButton { Content = Icon("heart") }));
        bottomEnd.Activated = true;

        var edgeBottom = Fab(FabHorizontal.Start, FabVertical.Bottom, new AvonicFabButton { Content = Icon("add"), IsEnabled = false });
        edgeBottom.Edge = true;

        AvonicFab[] fabs =
        [
            edgeTop,
            Fab(FabHorizontal.Start, FabVertical.Center, new AvonicFabButton { Content = Icon("add") }),
            Fab(FabHorizontal.Center, FabVertical.Center, new AvonicFabButton { Content = Icon("add"), Color = "danger", Size = FabButtonSize.Small }),
            Fab(FabHorizontal.End, FabVertical.Center, new AvonicFabButton { Content = "Fab", Color = "secondary" }),
            bottomEnd,
            edgeBottom,
        ];

        // Several fabs in the one fixed slot: a panel as big as the content holds them, as the slot does.
        var fixedPanel = new Panel { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        fixedPanel.Children.AddRange(fabs);

        var content = new AvonicContent
        {
            Content = new TextBlock { Text = "Content", Margin = new Thickness(16), FontSize = 16 },
            FixedContent = fixedPanel,

            // Over the header and the footer, whose ZIndex is 10, so that an edge fab paints over them, as z-index: 999 does in
            // Ionic. ZIndex only sorts siblings, so the content has to be the one above.
            ZIndex = 11,
        };

        var page = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        var header = new AvonicHeader { Items = { new AvonicToolbar { Content = new AvonicTitle { Content = "Fab" } } } };
        var footer = new AvonicFooter { Items = { new AvonicToolbar { Content = new AvonicTitle { Content = "Footer" } } } };
        Grid.SetRow(content, 1);
        Grid.SetRow(footer, 2);
        page.Children.Add(header);
        page.Children.Add(content);
        page.Children.Add(footer);
        return (page, fabs);
    }

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (page, fabs) = ReferencePage();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();

        // The lists' buttons appear 30 ms apart, and the close icons turn over 300 ms.
        clock.At(400);
        Dispatcher.UIThread.RunJobs();
        foreach (var part in page.GetVisualDescendants().OfType<Control>().Where(c => c.Name is "PART_CloseIcon" or "PART_ContentPresenter"))
        {
            part.Transitions = null;
        }

        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, "avonic-fab-page.png");

        // Measured in Ionic: the fabs at 10, 56 (28 high); 10, 394; 167, 394; 324, 394; 324, 722; 10, 760 (28 high).
        Rect Box(Visual visual) => new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(page)!.Value);
        Assert.Equal(new Rect(10, 56, 56, 28), Box(fabs[0]));
        Assert.Equal(new Rect(10, 394, 56, 56), Box(fabs[1]));
        Assert.Equal(new Rect(167, 394, 56, 56), Box(fabs[2]));
        Assert.Equal(new Rect(324, 394, 56, 56), Box(fabs[3]));
        Assert.Equal(new Rect(324, 722, 56, 56), Box(fabs[4]));
        Assert.Equal(new Rect(10, 760, 56, 28), Box(fabs[5]));
    });
}
