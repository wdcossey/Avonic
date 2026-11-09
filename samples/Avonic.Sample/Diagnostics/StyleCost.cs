using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// The style benchmark's shape, shared by the headless test (<c>StyleCostBenchmark</c>) and the sample's runner on a device
/// (<see cref="StyleCostRunner"/>), so that the two can't drift: what's attached, how often, and the row for the table in
/// docs/infra/performance/README.md.
/// </summary>
public static class StyleCost
{
    public const int Elements = 5000;
    public const int Depth = 20;
    public const int WarmUpRounds = 4;
    public const int Rounds = 25;

    /// <summary>The two columns of the table, in its order: an element that most rules look past, and one that fewer do.</summary>
    public static readonly IReadOnlyList<Func<Control>> Kinds = [() => new TextBlock { Text = "x" }, () => new Border()];

    /// <summary><paramref name="elements"/> made by <paramref name="make"/> in a <c>StackPanel</c>, under <paramref name="depth"/> borders.</summary>
    public static Control Tree(Func<Control> make, int elements = Elements, int depth = Depth)
    {
        var panel = new StackPanel();
        for (var i = 0; i < elements; i++)
        {
            panel.Children.Add(make());
        }

        Control root = panel;
        for (var i = 0; i < depth; i++)
        {
            root = new Border { Child = root };
        }

        return root;
    }

    public static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        return sorted[sorted.Length / 2];
    }

    /// <summary>A cell: µs per element without the theme, to with it, and the difference.</summary>
    public static string Cell(IEnumerable<double> withoutMs, IEnumerable<double> withMs, int elements = Elements)
    {
        var before = Median(withoutMs) * 1000 / elements;
        var after = Median(withMs) * 1000 / elements;
        return string.Create(CultureInfo.InvariantCulture, $"{before:0.0} to {after:0.0} ({after - before:+0.0;-0.0})");
    }

    /// <summary>Avonic's controls, as the table counts them.</summary>
    public static int Controls() =>
        typeof(AvonicItem).Assembly.GetExportedTypes().Count(t =>
            t.Namespace == typeof(AvonicItem).Namespace && t.Name.StartsWith("Avonic", StringComparison.Ordinal) && t.IsSubclassOf(typeof(StyledElement)));

    /// <summary>A row for the table: when, controls, rules, alternatives, then the cells.</summary>
    public static string Row(DateTime when, (int Rules, int Alternatives) census, IReadOnlyList<string> cells) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"| {when:yyyy-MM-dd} | {Controls()} | {census.Rules} | {census.Alternatives} | {string.Join(" | ", cells)} |");
}
