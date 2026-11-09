using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avonic.Themes;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// The style benchmark in a running app, for a phone, which can't run the test: <see cref="StyleCost"/>'s trees attached to
/// <paramref name="host"/> and laid out, alternating without and with the app's <see cref="AvonicTheme"/>, which it takes out of
/// <c>Application.Styles</c> and puts back where it was. One attach at a time on the dispatcher, so that the app keeps drawing and
/// Android doesn't call it not responding. Its times include the app's own base theme, so compare its rows with a row from this
/// runner on desktop, not with the headless test's.
/// </summary>
public sealed class StyleCostRunner(
    Panel host,
    int elements = StyleCost.Elements,
    int depth = StyleCost.Depth,
    int warmUpRounds = StyleCost.WarmUpRounds,
    int rounds = StyleCost.Rounds,
    IReadOnlyList<Func<Control>>? kinds = null)
{
    private readonly IReadOnlyList<Func<Control>> _kinds = kinds ?? StyleCost.Kinds;

    /// <summary>Whether a row from here can go in the table: a Release build, with Avalonia's diagnostics off.</summary>
    public static string? WhyNotForTheTable()
    {
#if DEBUG
        return "a Debug build";
#else
        return AppContext.TryGetSwitch(PerformanceMonitor.DiagnosticsSwitch, out var on) && on ? "Avalonia's diagnostics are on" : null;
#endif
    }

    /// <summary>Runs it, and gives the row for the table.</summary>
    /// <exception cref="InvalidOperationException">The app has no <see cref="AvonicTheme"/> in its styles, or the host went away.</exception>
    public async Task<string> Run(IProgress<string>? progress = null)
    {
        var styles = Application.Current!.Styles;
        var theme = styles.OfType<AvonicTheme>().FirstOrDefault()
                    ?? throw new InvalidOperationException("The app has no AvonicTheme in Application.Styles to take out and put back.");
        var census = StyleCensus.Count(theme);
        var cells = new List<string>();
        var total = warmUpRounds + rounds;
        for (var kind = 0; kind < _kinds.Count; kind++)
        {
            var without = new List<double>();
            var with = new List<double>();
            for (var round = 0; round < total; round++)
            {
                progress?.Report($"Column {kind + 1} of {_kinds.Count}, round {round + 1} of {total}");
                var index = styles.IndexOf(theme);
                styles.RemoveAt(index);
                double plain;
                try
                {
                    // Lets the app restyle without the theme before the clock starts.
                    await Settle();
                    plain = Attach(_kinds[kind]);
                }
                finally
                {
                    styles.Insert(index, theme);
                }

                await Settle();
                var themed = Attach(_kinds[kind]);
                if (round >= warmUpRounds)
                {
                    without.Add(plain);
                    with.Add(themed);
                }
            }

            cells.Add(StyleCost.Cell(without, with, elements));
        }

        return StyleCost.Row(DateTime.Now, census, cells);
    }

    private static async Task Settle() => await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

    private double Attach(Func<Control> make)
    {
        if (TopLevel.GetTopLevel(host) is null)
        {
            throw new InvalidOperationException("The benchmark's host has left the window, so there's nothing to attach to.");
        }

        var root = StyleCost.Tree(make, elements, depth);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var stopwatch = Stopwatch.StartNew();
        host.Children.Add(root);
        host.UpdateLayout();
        stopwatch.Stop();
        host.Children.Remove(root);
        return stopwatch.Elapsed.TotalMilliseconds;
    }
}
