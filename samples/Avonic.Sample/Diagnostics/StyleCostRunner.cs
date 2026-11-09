using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avonic.Components;
using Avonic.Themes;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// The style benchmark in a running app, for a phone, which can't run the test: <see cref="StyleCost"/>'s trees attached to
/// <paramref name="host"/> and laid out, alternating without and with the app's <see cref="AvonicTheme"/>. Without it means
/// <see cref="AvonicComponentsStyles"/> alone, the functional templates the theme layers over, in the theme's place, as the
/// test's app has: so Avonic's controls keep a template, and anything inside one stays attached. The theme goes back where it was.
/// One attach at a time on the dispatcher, so that the app keeps drawing and Android doesn't call it not responding. Give it a host
/// outside Avonic's controls (the shell's), so that its ancestors are the same both ways. Its times include the app's own base
/// theme, so compare its rows with a row from this runner on desktop, not with the headless test's.
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

    // The theme is the app's, so one run at a time.
    private static bool s_running;

    /// <summary>Whether a row from here can go in the table: a Release build, with Avalonia's diagnostics off.</summary>
    public static string? WhyNotForTheTable()
    {
#if DEBUG
        return "a Debug build";
#else
        return AppContext.TryGetSwitch(PerformanceMonitor.DiagnosticsSwitch, out var on) && on ? "Avalonia's diagnostics are on" : null;
#endif
    }

    /// <summary>Runs it, and gives the row for the table. <paramref name="cancel"/> stops it between attaches, with the theme back.</summary>
    /// <exception cref="InvalidOperationException">
    /// Another run is going, the app has no <see cref="AvonicTheme"/> in its styles, or the host went away.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancel"/> stopped it.</exception>
    public async Task<string> Run(IProgress<string>? progress = null, CancellationToken cancel = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rounds, 1);
        if (s_running)
        {
            throw new InvalidOperationException("A benchmark is already running.");
        }

        s_running = true;
        try
        {
            return await RunCore(progress, cancel);
        }
        finally
        {
            s_running = false;
        }
    }

    private async Task<string> RunCore(IProgress<string>? progress, CancellationToken cancel)
    {
        var styles = Application.Current!.Styles;
        var theme = styles.OfType<AvonicTheme>().FirstOrDefault()
                    ?? throw new InvalidOperationException("The app has no AvonicTheme in Application.Styles to take out and put back.");
        var census = StyleCensus.Count(theme);
        var stubs = new AvonicComponentsStyles();
        var cells = new List<string>();
        var total = warmUpRounds + rounds;
        for (var kind = 0; kind < _kinds.Count; kind++)
        {
            var without = new List<double>();
            var with = new List<double>();
            for (var round = 0; round < total; round++)
            {
                cancel.ThrowIfCancellationRequested();
                progress?.Report($"Column {kind + 1} of {_kinds.Count}, round {round + 1} of {total}");
                var index = styles.IndexOf(theme);
                styles[index] = stubs;
                double plain;
                try
                {
                    // Lets the app restyle without the theme before the clock starts.
                    await Settle();
                    plain = Attach(_kinds[kind]);
                }
                finally
                {
                    styles[index] = theme;
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
