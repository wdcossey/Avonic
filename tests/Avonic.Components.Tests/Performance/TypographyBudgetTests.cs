using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avonic.Components.Tests.Typography;

namespace Avonic.Components.Tests.Performance;

/// <summary>
/// What <c>Css/Typography.axaml</c> costs an app that merges it. <see cref="StyleBudgetTests"/> doesn't count it, as AvonicTheme
/// doesn't merge it. It's a ControlTheme for TextBlock, so a TextBlock is checked against its rules and nothing else is. See
/// docs/infra/typography/README.md.
/// </summary>
public class TypographyBudgetTests
{
    /// <summary>One rule for each of h1 to h6, with no commas. Each gets past a TextBlock's type.</summary>
    private const int Alternatives = 6;

    /// <summary>The ControlTheme itself, which Avalonia traces as one more check that stops at the type.</summary>
    private const int Theme = 1;

    private static (int All, int Reaching) Count(Control element)
    {
        var checks = StyleTrace.ChecksFor(element);
        return (
            checks.Sum(c => c.Alternatives.Count),
            checks.Where(c => c.ReachedElement).Sum(c => c.Alternatives.Count(a => StyleTrace.Reaches(a, element.GetType()))));
    }

    private static (int All, int Reaching) Added(Func<Control> make)
    {
        (int, int) added = default;
        InstalledTheme.Run(_ =>
        {
            var without = Count(make());
            var file = TypographyTests.File();
            Application.Current!.Resources.MergedDictionaries.Add(file);
            try
            {
                Dispatcher.UIThread.RunJobs();
                var with = Count(make());
                added = (with.All - without.All, with.Reaching - without.Reaching);
            }
            finally
            {
                Application.Current!.Resources.MergedDictionaries.Remove(file);
            }
        });
        return added;
    }

    [AvaloniaFact]
    public void A_TextBlock_Is_Checked_Against_Six_Alternatives_More()
    {
        Assert.Equal((Alternatives + Theme, Alternatives), Added(() => new TextBlock()));
    }

    [AvaloniaFact]
    public void A_Heading_Costs_No_More_Checks_Than_Any_TextBlock()
    {
        Assert.Equal((Alternatives + Theme, Alternatives), Added(() => new TextBlock { Classes = { "h1" } }));
    }

    [AvaloniaTheory]
    [MemberData(nameof(OtherElements))]
    public void Other_Elements_Are_Not_Checked_Against_It(string name)
    {
        Assert.Equal((0, 0), Added(() => StyleBudgetTests.Make(name)));
    }

    public static TheoryData<string> OtherElements() =>
        [.. StyleBudgetTests.CommonElements().Select(row => row.Data).Where(name => name != nameof(TextBlock))];
}
