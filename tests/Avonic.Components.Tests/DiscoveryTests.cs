using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Threading;
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

    /// <summary>Every key in a dictionary, including theme variants and nested dictionaries.</summary>
    private static IEnumerable<(ThemeVariant? Variant, object Key)> Entries(IResourceDictionary dictionary)
    {
        foreach (var key in dictionary.Keys)
        {
            yield return (null, key);
        }

        foreach (var (variant, provider) in dictionary.ThemeDictionaries)
        {
            if (provider is IResourceDictionary themed)
            {
                foreach (var key in themed.Keys)
                {
                    yield return (variant, key);
                }
            }
        }

        foreach (var merged in dictionary.MergedDictionaries)
        {
            var nested = merged is ResourceInclude include ? include.Loaded : merged as IResourceDictionary;
            if (nested is not null)
            {
                foreach (var entry in Entries(nested))
                {
                    yield return entry;
                }
            }
        }
    }

    private static void WithInstalledTheme(Action<AvonicTheme> test) => InstalledTheme.Run(test);

    private static void AssertResolvesRippleTheme(Styles styles)
    {
        Assert.True(styles.TryGetResource(typeof(AvonicRippleEffect), null, out var found));
        var theme = Assert.IsType<ControlTheme>(found);
        Assert.Equal(typeof(AvonicRippleEffect), theme.TargetType);
    }

    [AvaloniaFact]
    public void Components_Discovers_The_Ripple_ControlTheme() =>
        AssertResolvesRippleTheme(new AvonicComponentsStyles());

    [Fact]
    public void Components_Merge_Order_Is_Ordinal()
    {
        string[] paths = ["Controls/b/B.axaml", "Controls/a/A.axaml", "Controls/B/B.axaml"];

        Assert.Equal(
            ["Controls/B/B.axaml", "Controls/a/A.axaml", "Controls/b/B.axaml"],
            AvonicComponentsStyles.OrderForMerge(paths));
    }

    [AvaloniaFact]
    public void Theme_Discovers_Tokens() =>
        Assert.Contains(
            Includes(new AvonicTheme()),
            include => include.Source == new Uri("avares://Avonic.Themes/Tokens/AvonicTokens.axaml"));

    // A control's ::slotted rules, in a <Styles> file, because a ControlTheme's styles can't reach them.
    [AvaloniaFact]
    public void Theme_Discovers_Style_Files_And_They_Load()
    {
        var includes = new AvonicTheme().OfType<StyleInclude>().ToList();

        Assert.Contains(includes, include => include.Source == new Uri("avares://Avonic.Themes/Styles/Item.axaml"));
        Assert.All(includes, include => Assert.IsType<Styles>(include.Loaded, exactMatch: false));
    }

    [AvaloniaFact]
    public void Theme_Does_Not_Merge_Style_Files_As_Dictionaries() =>
        Assert.DoesNotContain(
            Includes(new AvonicTheme()),
            include => include.Source!.AbsolutePath.StartsWith("/Styles/", StringComparison.Ordinal));

    [AvaloniaFact]
    public void Theme_Still_Layers_Over_The_Component_Stubs() =>
        WithInstalledTheme(AssertResolvesRippleTheme);

    [Fact]
    public void Theme_Merge_Order_Is_Ordinal()
    {
        string[] paths = ["Tokens/Z.axaml", "Controls/b.axaml", "Tokens/A.axaml", "Controls/B.axaml"];

        Assert.Equal(
            ["Controls/B.axaml", "Controls/b.axaml", "Tokens/A.axaml", "Tokens/Z.axaml"],
            AvonicTheme.OrderForMerge(paths));
    }

    // Dictionary entries are built on first lookup, so loading a file proves nothing about its
    // contents. Looking up every key is what catches a mistyped {StaticResource} token.
    [AvaloniaFact]
    public void Every_Discovered_Resource_Builds_With_The_Theme_Installed() =>
        WithInstalledTheme(theme =>
        {
            foreach (var styles in new Styles[] { new AvonicComponentsStyles(), theme })
            {
                var includes = Includes(styles);
                Assert.NotEmpty(includes);

                foreach (var (variant, key) in includes.SelectMany(include => Entries(include.Loaded)))
                {
                    Assert.True(theme.TryGetResource(key, variant, out _), $"Could not resolve {key}");
                }
            }
        });

    [AvaloniaFact]
    public void No_Key_Is_Defined_Twice_Within_A_Project()
    {
        Assert.Empty(DuplicateKeys(new AvonicComponentsStyles()));
        Assert.Empty(DuplicateKeys(new AvonicTheme()));

        static IEnumerable<(ThemeVariant? Variant, object Key)> DuplicateKeys(Styles styles) =>
            Includes(styles)
                .SelectMany(include => Entries(include.Loaded))
                .GroupBy(entry => entry)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);
    }

    [AvaloniaFact]
    public void Sample_Catalog_Finds_The_Ripple_Page() =>
        Assert.IsType<RippleEffectPage>(
            Assert.Single(
                SamplePageCatalog.Discover(typeof(MainWindow).Assembly),
                entry => entry.Title == "ion-ripple-effect").Create());

    [AvaloniaFact]
    public void Sample_Every_Page_Can_Be_Shown_Under_The_Theme() =>
        WithInstalledTheme(_ =>
        {
            foreach (var entry in SamplePageCatalog.Discover(typeof(MainWindow).Assembly))
            {
                var window = new Window { Content = entry.Create() };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                window.Close();
            }
        });

    // The shell gives a page the window's height and no more, so a taller one would be cut off with no way to reach the rest.
    [AvaloniaFact]
    public void Sample_A_Page_Taller_Than_The_Window_Scrolls_Itself() =>
        WithInstalledTheme(_ =>
        {
            const double ShellHeight = 844 - 56;
            foreach (var entry in SamplePageCatalog.Discover(typeof(MainWindow).Assembly))
            {
                var page = entry.Create();
                var window = new Window { Width = 390, Height = 844, Content = page };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                page.Measure(new Size(390, double.PositiveInfinity));
                var scrolls = page.GetLogicalDescendants().Any(child => child is ScrollViewer or AvonicContent);
                var height = page.DesiredSize.Height;
                window.Close();

                Assert.True(scrolls || height <= ShellHeight, $"{entry.Title} is {height} high, in {ShellHeight}, and doesn't scroll.");
            }
        });

    // Titles sort the opposite way to Order, so a title-only sort can't pass by coincidence.
    [SamplePage("b-no-order")]
    private sealed class NoOrderPage;

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

        Assert.Equal(["y-order-1", "z-order-1", "a-order-2", "b-no-order"], titles);
    }

    [Fact]
    public void Sample_Every_Page_Is_Marked_For_Discovery()
    {
        var unmarked = typeof(MainWindow).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("Page", StringComparison.Ordinal)
                           && !type.IsAbstract
                           && typeof(Control).IsAssignableFrom(type)
                           && type.GetCustomAttribute<SamplePageAttribute>() is null);

        Assert.Empty(unmarked);
    }
}
