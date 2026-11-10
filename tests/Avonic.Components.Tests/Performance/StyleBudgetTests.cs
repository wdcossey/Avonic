using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avonic.Sample.Diagnostics;

namespace Avonic.Components.Tests.Performance;

/// <summary>
/// The style budget. Avalonia checks every global rule against every element, in any app, even one that uses none of these
/// controls, at about 30 ns per alternative between commas. Going over means adding fewer rules, or raising a limit here on
/// purpose and saying so in the PR. See docs/infra/performance/README.md.
/// </summary>
public class StyleBudgetTests
{
    /// <summary>All the alternatives in the theme's global rules.</summary>
    private const int MaxAlternatives = 247;

    /// <summary>Don't raise the limit past this without measuring on a real phone. See the README.</summary>
    private const int Checkpoint = 600;

    /// <summary>Alternatives that get past a <c>TextBlock</c>'s type, the most common element in an app.</summary>
    private const int MaxAlternativesReachingTextBlock = 60;

    /// <summary>Alternatives that get past the type of any other common element, the most of any of them.</summary>
    private const int MaxAlternativesReachingAnother = 34;

    private static string Report(IEnumerable<StyleTrace.Check> checks) =>
        string.Join(Environment.NewLine, checks.OrderByDescending(c => c.Alternatives.Count).Take(15).Select(c => $"{c.Alternatives.Count,3}  {c.Selector}"));

    [AvaloniaFact]
    public void The_Theme_Stays_Within_The_Budget_For_Alternatives() => InstalledTheme.Run(_ =>
    {
        var checks = StyleTrace.ChecksFor(new TextBlock());
        var total = checks.Sum(c => c.Alternatives.Count);

        Assert.True(total <= MaxAlternatives, $"{total} alternatives, over the budget of {MaxAlternatives}. The largest:{Environment.NewLine}{Report(checks)}");
    });

    [Fact]
    public void The_Limit_Stays_At_Or_Under_The_Checkpoint()
    {
        var limit = MaxAlternatives;

        Assert.True(limit <= Checkpoint, "Stop here, and measure on a real phone before going on. See docs/infra/performance/README.md.");
    }

    [AvaloniaFact]
    public void The_Theme_Stays_Within_The_Budget_For_A_TextBlock() => InstalledTheme.Run(_ =>
    {
        var reached = StyleTrace.ChecksFor(new TextBlock()).Where(c => c.ReachedElement).ToList();
        var total = reached.Sum(c => c.Alternatives.Count(a => StyleTrace.Reaches(a, typeof(TextBlock))));

        Assert.True(total <= MaxAlternativesReachingTextBlock, $"{total} alternatives reach a TextBlock, over the budget of {MaxAlternativesReachingTextBlock}.{Environment.NewLine}{Report(reached)}");
    });

    [AvaloniaTheory]
    [MemberData(nameof(CommonElements))]
    public void The_Theme_Stays_Within_The_Budget_For_Another_Element(string name) => InstalledTheme.Run(_ =>
    {
        if (name == nameof(TextBlock))
        {
            return;
        }

        var element = Make(name);
        var reached = StyleTrace.ChecksFor(element).Where(c => c.ReachedElement).ToList();
        var total = reached.Sum(c => c.Alternatives.Count(a => StyleTrace.Reaches(a, element.GetType())));

        Assert.True(total <= MaxAlternativesReachingAnother, $"{total} alternatives reach a {name}, over the budget of {MaxAlternativesReachingAnother}.{Environment.NewLine}{Report(reached)}");
    });

    public static TheoryData<string> CommonElements() =>
        ["TextBlock", "Border", "Panel", "StackPanel", "Grid", "DockPanel", "Image", "Button", "ScrollViewer", "ContentPresenter"];

    private static Control Make(string name) => name switch
    {
        "TextBlock" => new TextBlock(),
        "Border" => new Border(),
        "Panel" => new Panel(),
        "StackPanel" => new StackPanel(),
        "Grid" => new Grid(),
        "DockPanel" => new DockPanel(),
        "Image" => new Image(),
        "Button" => new Button(),
        "ScrollViewer" => new ScrollViewer(),
        "ContentPresenter" => new Avalonia.Controls.Presenters.ContentPresenter(),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    // 385 ns at a depth of 20, against about 27. `c|AvonicContent TextBox` is allowed, as a TextBox is rare: measured free.
    [AvaloniaTheory]
    [MemberData(nameof(CommonElements))]
    public void No_Rule_Walks_The_Ancestors_Of_A_Common_Element(string name) => InstalledTheme.Run(_ =>
    {
        var element = Make(name);
        var walkers = StyleTrace.ChecksFor(element)
            .Where(c => c.ReachedElement)
            .SelectMany(c => c.Alternatives)
            .Where(a => StyleTrace.WalksAncestors(a) && StyleTrace.Reaches(a, element.GetType()))
            .ToList();

        Assert.True(walkers.Count == 0, $"These look at every ancestor of a {name}:{Environment.NewLine}{string.Join(Environment.NewLine, walkers)}");
    });

    // If Avalonia ever caches by type, this goes red, and the budget above needs rethinking. That would be good news.
    [AvaloniaFact]
    public void Every_Rule_Is_Checked_Against_Every_Element() => InstalledTheme.Run(theme =>
    {
        var (rules, alternatives) = StyleCensus.Count(theme);

        foreach (var element in new Control[] { new TextBlock(), new Border(), new Grid() })
        {
            var checks = StyleTrace.ChecksFor(element);
            Assert.Equal(rules, checks.Count);
            Assert.Equal(alternatives, checks.Sum(c => c.Alternatives.Count));
        }
    });

    // ── The trace itself, as the budget means nothing if it can't see a rule ─────────────────

    private static void WithRule(Style rule, Action test)
    {
        var styles = Application.Current!.Styles;
        styles.Add(rule);
        try
        {
            Dispatcher.UIThread.RunJobs();
            test();
        }
        finally
        {
            styles.Remove(rule);
        }
    }

    private static Style Rule(Func<Selector?, Selector> selector)
    {
        var rule = new Style(selector);
        rule.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
        return rule;
    }

    [AvaloniaFact]
    public void A_Rule_That_Cannot_Match_Is_Still_Counted()
    {
        var before = StyleTrace.ChecksFor(new TextBlock()).Sum(c => c.Alternatives.Count);

        WithRule(Rule(x => x.OfType<Image>()), () =>
        {
            var checks = StyleTrace.ChecksFor(new TextBlock());
            Assert.Equal(before + 1, checks.Sum(c => c.Alternatives.Count));
            Assert.Contains(checks, c => c.Selector == "Image" && !c.ReachedElement);
        });
    }

    [AvaloniaFact]
    public void A_Comma_Counts_As_Many_Alternatives()
    {
        var before = StyleTrace.ChecksFor(new TextBlock()).Sum(c => c.Alternatives.Count);

        WithRule(Rule(x => Selectors.Or(x.OfType<Image>(), x.OfType<Grid>(), x.OfType<Panel>())), () =>
        {
            Assert.Equal(before + 3, StyleTrace.ChecksFor(new TextBlock()).Sum(c => c.Alternatives.Count));
        });
    }

    [AvaloniaFact]
    public void A_Rule_For_The_Elements_Own_Type_Reaches_It()
    {
        WithRule(Rule(x => x.OfType<StackPanel>().Child().OfType<TextBlock>()), () =>
        {
            Assert.Contains(StyleTrace.ChecksFor(new TextBlock()), c => c.Selector == "StackPanel > TextBlock" && c.ReachedElement);
            Assert.Contains(StyleTrace.ChecksFor(new Border()), c => c.Selector == "StackPanel > TextBlock" && !c.ReachedElement);
        });
    }

    [AvaloniaFact]
    public void A_Rule_Without_Setters_Costs_Nothing_And_Is_Not_Counted()
    {
        var before = StyleTrace.ChecksFor(new TextBlock()).Count;
        var empty = new Style(x => x.OfType<Image>());

        WithRule(empty, () => Assert.Equal(before, StyleTrace.ChecksFor(new TextBlock()).Count));
    }

    [AvaloniaFact]
    public void The_Census_Counts_What_Avalonia_Checks()
    {
        var withSetter = new Style(x => Selectors.Or(x.OfType<Image>(), x.OfType<Grid>()));
        withSetter.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
        var nested = new Style(x => x!.Nesting().Class(":a"));
        nested.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
        var empty = new Style(x => x.OfType<Border>());
        empty.Children.Add(nested);
        var styles = new Styles { withSetter, empty };

        Assert.Equal((2, 3), StyleCensus.Count(styles));
    }

    [AvaloniaFact]
    public void A_Rule_With_Only_Animations_Is_Counted()
    {
        var rule = new Style(x => x.OfType<Image>());
        rule.Animations.Add(new Avalonia.Animation.Animation());

        Assert.Equal((1, 1), StyleCensus.Count(new Styles { rule }));
    }

    [AvaloniaFact]
    public void The_Census_Counts_The_Commas_Of_A_Nested_Rules_Parent()
    {
        var parent = new Style(x => Selectors.Or(x.OfType<Image>(), x.OfType<Grid>()));
        var nested = new Style(x => x!.Nesting().Class(":a"));
        nested.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
        parent.Children.Add(nested);

        Assert.Equal((1, 2), StyleCensus.Count(new Styles { parent }));
    }

    [Theory]
    [InlineData("A", 1)]
    [InlineData("A, B", 2)]
    [InlineData("A > B, C /template/ D, E F", 3)]
    [InlineData("A:not(B, C)", 1)]
    [InlineData("A:is(B, C), D:not(E, F)", 2)]
    [InlineData("A[Margin=1,2,3,4]", 1)]
    [InlineData("A[Tag=(], B", 2)]
    public void Alternatives_Split_At_Commas_Outside_Brackets(string selector, int count)
    {
        Assert.Equal(count, StyleCensus.SplitAlternatives(selector).Count);
    }

    [Theory]
    [InlineData("A", false)]
    [InlineData("A TextBlock", true)]
    [InlineData("A TextBlock.h1", true)]
    [InlineData("A > TextBlock", false)]
    [InlineData("A B > TextBlock", true)]
    [InlineData("A > B TextBlock", true)]
    [InlineData("AvonicItem:a AvonicLabel > TextBlock", false)]
    [InlineData("AvonicItem:a TextBlock", true)]
    [InlineData("AvonicItem:a :is(Panel) > TextBlock", true)]
    [InlineData("A /template/ Border", false)]
    [InlineData("A B /template/ Border", true)]
    [InlineData("AvonicItem:a AvonicLabel /template/ Border", false)]
    [InlineData("A Border#PART_Native", false)]
    [InlineData("A:not(B C)", false)]
    [InlineData("A:not(B C) D", true)]
    [InlineData("A TextBlock:not(B > C)", true)]
    [InlineData("A > TextBlock:not(B C)", false)]
    [InlineData("A[Tag=x y]", false)]
    [InlineData("A TextBlock[Tag=x > y]", true)]
    public void A_Descendant_Combinator_At_The_End_Walks_Ancestors(string alternative, bool walks)
    {
        Assert.Equal(walks, StyleTrace.WalksAncestors(alternative));
    }

    [Theory]
    [InlineData("TextBlock", true)]
    [InlineData("TextBlock.h1", true)]
    [InlineData("A > TextBlock.h1", true)]
    [InlineData("A /template/ TextBlock:foo", true)]
    [InlineData("A > :is(Control)", true)]
    [InlineData("A > :is(Control):not(B)", true)]
    [InlineData("A > :is(Panel)", false)]
    [InlineData("A > B", false)]
    [InlineData("TextBlock > A", false)]
    [InlineData("TextBox", false)]
    [InlineData("A > .foo", true)]
    public void An_Alternative_Is_Judged_By_Its_Last_Part(string alternative, bool reaches)
    {
        Assert.Equal(reaches, StyleTrace.Reaches(alternative, typeof(TextBlock)));
    }

    // One alternative for a TextBlock and another that walks a TextBox's ancestors, which isn't a walk for a TextBlock.
    [AvaloniaFact]
    public void A_Rule_Is_Judged_By_Each_Of_Its_Alternatives()
    {
        var rule = Rule(x => Selectors.Or(x.OfType<StackPanel>().Child().OfType<TextBlock>(), x.OfType<Panel>().Descendant().OfType<TextBox>()));

        WithRule(rule, () =>
        {
            var check = StyleTrace.ChecksFor(new TextBlock()).Single(c => c.Selector.Contains("TextBox", StringComparison.Ordinal));
            Assert.True(check.ReachedElement);
            Assert.Single(check.Alternatives, a => StyleTrace.Reaches(a, typeof(TextBlock)));
            Assert.DoesNotContain(check.Alternatives, a => StyleTrace.WalksAncestors(a) && StyleTrace.Reaches(a, typeof(TextBlock)));
        });
    }
}
