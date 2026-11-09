using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avonic.Sample.Diagnostics;

namespace Avonic.Components.Tests.Sample;

/// <summary>
/// The style benchmark as the sample runs it on a phone (#66), with small counts: it gives a row of the table's shape, attaches
/// without the app's theme and then with it, and always puts the theme back where it was. Timings aren't checked: a CI machine's mean
/// nothing. See docs/infra/android/PLAN.md.
/// </summary>
public partial class StyleCostRunnerTests
{
    [GeneratedRegex(@"^\| \d{4}-\d\d-\d\d \| \d+ \| \d+ \| \d+ \| \d+\.\d to \d+\.\d \([+-]\d+\.\d\) \| \d+\.\d to \d+\.\d \([+-]\d+\.\d\) \|$")]
    private static partial Regex RowShape();

    /// <summary>Runs the dispatcher until <paramref name="task"/> ends, as the app's own loop would.</summary>
    private static T Wait<T>(Task<T> task)
    {
        var until = DateTime.UtcNow.AddSeconds(30);
        while (!task.IsCompleted && DateTime.UtcNow < until)
        {
            Dispatcher.UIThread.RunJobs();
        }

        Assert.True(task.IsCompleted, "the run never ended");
        return task.GetAwaiter().GetResult();
    }

    private static StyleCostRunner Runner(Panel host, IReadOnlyList<Func<Control>>? kinds = null, int rounds = 1) =>
        new(host, elements: 10, depth: 2, warmUpRounds: 1, rounds: rounds, kinds: kinds);

    [AvaloniaFact]
    public void It_Gives_A_Row_Of_The_Tables_Shape() => InstalledTheme.Run(_ =>
    {
        var host = new Panel();
        using var scene = new Scene(host);

        var row = Wait(Runner(host).Run());

        Assert.Matches(RowShape(), row);
        Assert.Empty(host.Children);
    });

    [AvaloniaFact]
    public void Each_Round_Attaches_Without_The_Theme_Then_With_It_And_Puts_It_Back_Where_It_Was() => InstalledTheme.Run(theme =>
    {
        var styles = Application.Current!.Styles;
        var index = styles.IndexOf(theme);
        var seen = new List<bool>();
        var host = new Panel();
        using var scene = new Scene(host);
        Control Make()
        {
            seen.Add(styles.Contains(theme));
            return new Border();
        }

        // Two rounds, one of them to warm up, of one element each, for one column.
        Wait(new StyleCostRunner(host, elements: 1, depth: 0, warmUpRounds: 1, rounds: 1, kinds: [Make]).Run());

        Assert.Equal([false, true, false, true], seen);
        Assert.Equal(index, styles.IndexOf(theme));
    });

    [AvaloniaFact]
    public void The_Theme_Goes_Back_When_A_Round_Throws() => InstalledTheme.Run(theme =>
    {
        var styles = Application.Current!.Styles;
        var index = styles.IndexOf(theme);
        var host = new Panel();
        using var scene = new Scene(host);

        var run = Runner(host, kinds: [() => throw new InvalidOperationException("broken")]).Run();

        Assert.Equal("broken", Assert.Throws<InvalidOperationException>(() => Wait(run)).Message);
        Assert.Equal(index, styles.IndexOf(theme));
    });

    [AvaloniaFact]
    public void A_Host_That_Has_Left_Stops_It_With_The_Theme_Back() => InstalledTheme.Run(theme =>
    {
        var styles = Application.Current!.Styles;
        var index = styles.IndexOf(theme);

        var run = Runner(new Panel()).Run();

        Assert.Contains("left the window", Assert.Throws<InvalidOperationException>(() => Wait(run)).Message, StringComparison.Ordinal);
        Assert.Equal(index, styles.IndexOf(theme));
    });

    [AvaloniaFact]
    public void An_App_With_No_Avonic_Theme_Is_Told_So()
    {
        var host = new Panel();
        using var scene = new Scene(host);

        var run = Runner(host).Run();

        Assert.Contains("no AvonicTheme", Assert.Throws<InvalidOperationException>(() => Wait(run)).Message, StringComparison.Ordinal);
    }

    // The test's column cells and the runner's come from one place, so a row from either has the same meaning.
    [AvaloniaFact]
    public void A_Cell_Is_Microseconds_Per_Element_From_The_Medians()
    {
        Assert.Equal("2.0 to 5.0 (+3.0)", StyleCost.Cell([10, 1, 30], [9, 25, 50], elements: 5000));
    }
}
