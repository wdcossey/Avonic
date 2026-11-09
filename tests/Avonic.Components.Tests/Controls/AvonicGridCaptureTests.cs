using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicGridCaptureTests
{
    private static readonly IBrush Teal = new SolidColorBrush(Color.Parse("#135D54"));

    /// <summary>
    /// Ionic's own demo style for a grid: <c>ion-col { background-color: #135d54; border: solid 1px #fff; color: #fff;
    /// text-align: center }</c>.
    /// </summary>
    private static AvonicCol Col(string text, string? size = null) => new()
    {
        Size = size,
        Content = text,
        Background = Teal,
        BorderBrush = Brushes.White,
        BorderThickness = new Thickness(1),
        Foreground = Brushes.White,
        HorizontalContentAlignment = HorizontalAlignment.Center,
    };

    private static AvonicRow Row(params AvonicCol[] columns)
    {
        var row = new AvonicRow();
        foreach (var column in columns)
        {
            row.Items.Add(column);
        }

        return row;
    }

    private static AvonicGrid Grid(params AvonicRow[] rows)
    {
        var grid = new AvonicGrid();
        foreach (var row in rows)
        {
            grid.Items.Add(row);
        }

        return grid;
    }

    private static AvonicCol With(AvonicCol column, Action<AvonicCol> set)
    {
        set(column);
        return column;
    }

    // The same content as the command at the top of docs/components/ion-grid/COMPARISON.md.
    internal static Control ReferencePage()
    {
        var noPadding = Grid(Row(Col("ion-no-padding"), Col("ion-no-padding")));
        noPadding.Classes.Add("ion-no-padding");

        var rtl = Grid(Row(Col("size 6", "6"), Col("rest"), Col("size 3", "3")));
        rtl.FlowDirection = FlowDirection.RightToLeft;

        var page = new StackPanel
        {
            Children =
            {
                Grid(
                    Row(Col("1 of 3"), Col("2 of 3"), Col("3 of 3")),
                    Row(Col("size 6", "6"), Col("rest"), Col("size 3", "3")),
                    Row(Col("auto content", "auto"), Col("rest")),
                    Row(With(Col("offset 3", "3"), c => c.Offset = "3"), Col("size 3", "3")),
                    Row(With(Col("push 3", "9"), c => c.Push = "3"), With(Col("pull 9", "3"), c => c.Pull = "9")),
                    Row(Col("Supercalifragilisticexpialidocious"), Col("b"), Col("c")),
                    Row(Col("A column whose text wraps onto a second line", "6"), Col("stretched")),
                    Row(Col("8", "8"), Col("8 wraps", "8"))),
                noPadding,
                rtl,
            },
        };
        TextElement.SetFontSize(page, 16);
        return page;
    }

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        using var scene = new Scene(ReferencePage(), 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        Frames.Save(frame, "avonic-grid-page.png");

        // The first column of the first row is teal, inside the grid's padding, and the page's corner isn't.
        Assert.Equal(((ISolidColorBrush)Teal).Color, Frames.PixelAt(frame, 2 * 20, 2 * 15));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 2, 2));
    });
}
