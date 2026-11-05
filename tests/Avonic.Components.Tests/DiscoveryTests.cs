using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avonic.Components.Controls;
using Avonic.Sample;
using Avonic.Sample.Pages;
using Avonic.Themes;

namespace Avonic.Components.Tests;

/// <summary>
/// Controls are found from a build-time manifest and <c>[SamplePage]</c>, not a registration list.
/// A broken manifest would silently unstyle every control, so these tests make it fail CI.
/// </summary>
public class DiscoveryTests
{
    private static List<ResourceInclude> Includes(Styles styles) =>
        styles.Resources.MergedDictionaries.OfType<ResourceInclude>().ToList();

    private static void AssertResolvesRippleTheme(Styles styles)
    {
        Assert.True(styles.TryGetResource(typeof(AvonicRippleEffect), null, out var found));
        var theme = Assert.IsType<ControlTheme>(found);
        Assert.Equal(typeof(AvonicRippleEffect), theme.TargetType);
    }

    [AvaloniaFact]
    public void Components_Discovers_The_Ripple_ControlTheme() =>
        AssertResolvesRippleTheme(new AvonicComponentsStyles());

    [AvaloniaFact]
    public void Components_Every_Discovered_Include_Loads()
    {
        var includes = Includes(new AvonicComponentsStyles());

        Assert.NotEmpty(includes);
        Assert.All(includes, include => Assert.NotNull(include.Loaded));
    }

    [Fact]
    public void Components_Merge_Order_Is_Ordinal()
    {
        string[] paths = ["Controls/b/B.axaml", "Controls/a/A.axaml", "Controls/B/B.axaml"];

        Assert.Equal(
            ["Controls/B/B.axaml", "Controls/a/A.axaml", "Controls/b/B.axaml"],
            AvonicComponentsStyles.OrderForMerge(paths));
    }

    [AvaloniaFact]
    public void Theme_Discovers_Tokens_And_Every_Include_Loads()
    {
        var includes = Includes(new AvonicTheme());

        Assert.Contains(
            includes,
            include => include.Source == new Uri("avares://Avonic.Themes/Tokens/AvonicTokens.axaml"));
        Assert.All(includes, include => Assert.NotNull(include.Loaded));
    }

    [AvaloniaFact]
    public void Theme_Still_Layers_Over_The_Component_Stubs() =>
        AssertResolvesRippleTheme(new AvonicTheme());

    [Fact]
    public void Theme_Merges_Tokens_Before_Controls()
    {
        string[] paths = ["Controls/B.axaml", "Tokens/Z.axaml", "Controls/A.axaml", "Tokens/A.axaml"];

        Assert.Equal(
            ["Tokens/A.axaml", "Tokens/Z.axaml", "Controls/A.axaml", "Controls/B.axaml"],
            AvonicTheme.OrderForMerge(paths));
    }

    [AvaloniaFact]
    public void Sample_Catalog_Finds_And_Creates_The_Ripple_Page()
    {
        var page = Assert.Single(
            SamplePageCatalog.Discover(typeof(MainWindow).Assembly),
            entry => entry.Title == "ion-ripple-effect");

        Assert.IsType<RippleEffectPage>(page.Create());
    }

    // Titles sort the opposite way to Order, so a title-only sort can't pass by coincidence.
    [SamplePage("a-order-2", Order = 2)]
    private sealed class OrderTwoPage;

    [SamplePage("z-order-1", Order = 1)]
    private sealed class OrderOneLastPage;

    [SamplePage("y-order-1", Order = 1)]
    private sealed class OrderOneFirstPage;

    [Fact]
    public void Sample_Catalog_Orders_By_Order_Then_Title()
    {
        var titles = SamplePageCatalog.Discover(typeof(DiscoveryTests).Assembly).Select(entry => entry.Title);

        Assert.Equal(["y-order-1", "z-order-1", "a-order-2"], titles);
    }

    [Fact]
    public void Sample_Every_Page_Is_Marked_For_Discovery()
    {
        var unmarked = typeof(MainWindow).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(RippleEffectPage).Namespace
                           && typeof(UserControl).IsAssignableFrom(type)
                           && type.GetCustomAttribute<SamplePageAttribute>() is null);

        Assert.Empty(unmarked);
    }
}
