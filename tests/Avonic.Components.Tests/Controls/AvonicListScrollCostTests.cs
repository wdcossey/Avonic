using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// What scrolling a long virtualised list costs, in Avalonia's own counts, by hand: see docs/infra/performance/README.md.
/// It does nothing unless <c>AVONIC_SCROLL_COST</c> is the absolute path of a file, which gets the table's rows. Counts,
/// not times: tracing costs more than what it traces.
/// </summary>
public class AvonicListScrollCostTests
{
    private const string Variable = "AVONIC_SCROLL_COST";
    private const string Source = "Avalonia.Diagnostic.Source";
    private const int Items = 1000;

    /// <summary>A row a phone's height scrolls by in about a frame of a quick fling.</summary>
    private const double Step = 48;

    private sealed class Counts
    {
        public int StyleChecks;
        public int Measures;
        public int Arranges;
        public int Built;
        public int Prepared;
        public int Passes;
    }

    private static (Control Root, ItemsControl List, ScrollViewer Scroller) Divided()
    {
        var list = new AvonicList
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
            ItemsSource = Enumerable.Range(0, Items).Select(index => index % 20 == 0 ? (object)("d" + index) : index).ToList(),
        };
        var scroller = new ScrollViewer { Content = list };
        return (scroller, list, scroller);
    }

    private static (Control Root, ItemsControl List, ScrollViewer Scroller) InsetSections()
    {
        var sections = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
            ItemsSource = Enumerable.Range(0, Items).ToList(),
        };
        var scroller = new ScrollViewer { Content = sections };
        return (scroller, sections, scroller);
    }

    private static Counts Scroll(Func<(Control Root, ItemsControl List, ScrollViewer Scroller)> make, Func<object, Control> build)
    {
        var counts = new Counts();
        var (root, list, scroller) = make();
        list.ItemTemplate = new FuncDataTemplate<object>((value, _) =>
        {
            counts.Built++;
            return build(value);
        });
        list.ContainerPrepared += (_, _) => counts.Prepared++;
        using var scene = new Scene(root, 390, 844);
        scroller.LayoutUpdated += (_, _) => counts.Passes++;

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Source,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                switch (activity.OperationName)
                {
                    case "Avalonia.AttachingStyle":
                        counts.StyleChecks++;
                        break;
                    case "Avalonia.MeasuringLayoutable":
                        counts.Measures++;
                        break;
                    case "Avalonia.ArrangingLayoutable":
                        counts.Arranges++;
                        break;
                }
            },
        };

        // From the top to the end and back, a row at a time, after the first screen, so only scrolling is counted.
        counts.Built = counts.Prepared = counts.Passes = 0;
        ActivitySource.AddActivityListener(listener);
        var end = scroller.Extent.Height - scroller.Viewport.Height;
        for (var offset = Step; offset <= end; offset += Step)
        {
            scroller.Offset = new Vector(0, offset);
            Dispatcher.UIThread.RunJobs();
            end = scroller.Extent.Height - scroller.Viewport.Height;
        }

        for (var offset = scroller.Offset.Y - Step; offset >= 0; offset -= Step)
        {
            scroller.Offset = new Vector(0, offset);
            Dispatcher.UIThread.RunJobs();
        }

        return counts;
    }

    private static string Row(string what, Counts counts) =>
        $"| {what} | {counts.Passes} | {counts.Prepared} | {counts.Built} | {counts.Measures} | {counts.Arranges} | {counts.StyleChecks} |";

    [AvaloniaFact]
    public void Scrolling_Counts()
    {
        if (Environment.GetEnvironmentVariable(Variable) is not { } file)
        {
            return;
        }

        var styles = Application.Current!.Styles;
        var baseTheme = new SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            var rows = new List<string>();
            InstalledTheme.Run(_ =>
            {
                rows.Add(Row("1,000 rows, a sticky divider every 20", Scroll(() => Divided(), value => value is string text
                    ? new AvonicItemDivider { Sticky = true, Content = text }
                    : new AvonicItem { Content = "Row" })));
                rows.Add(Row("1,000 rows, a divider every 20, not sticky", Scroll(() => Divided(), value => value is string text
                    ? new AvonicItemDivider { Content = text }
                    : new AvonicItem { Content = "Row" })));
                rows.Add(Row("1,000 inset lists of one item", Scroll(InsetSections, _ =>
                    new AvonicList { Inset = true, Items = { new AvonicItem { Content = "Row" } } })));
            });
            File.WriteAllLines(file, rows);
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }
}
