using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Themes;

namespace Avonic.Components.Tests;

/// <summary>
/// The token values are Ionic's, so these read them from Ionic's CSS. An Ionic upgrade that changes a
/// value fails here. They skip without <c>node_modules</c>, and CI always has it.
/// </summary>
public partial class ColorTokensTests
{
    private static readonly string[] Names = ["primary", "secondary", "tertiary", "success", "warning", "danger", "light", "medium", "dark"];
    private static readonly (string Css, string Key)[] Variants = [("", ""), ("-contrast", "Contrast"), ("-shade", "Shade"), ("-tint", "Tint")];
    private static readonly int[] Steps = Enumerable.Range(1, 19).Select(i => i * 50).ToArray();

    private static string Pascal(string name) => char.ToUpperInvariant(name[0]) + name[1..];

    private static string RepoFile(string relative)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Avonic.sln")))
            {
                return Path.Combine(dir.FullName, relative);
            }
        }

        throw new InvalidOperationException("Avonic.sln not found above " + AppContext.BaseDirectory);
    }

    private static string IonicFile(string relative)
    {
        var path = RepoFile(Path.Combine("node_modules", "@ionic", "core", relative));
        if (!File.Exists(path))
        {
            Assert.Skip($"{path} is missing: run npm install");
        }

        return path;
    }

    /// <summary>The declarations in the first <c>selector { … }</c> of a minified stylesheet.</summary>
    private static Dictionary<string, string> Block(string css, string selector)
    {
        var start = css.IndexOf(selector + "{", StringComparison.Ordinal);
        Assert.True(start >= 0, $"No {selector} block");
        start += selector.Length + 1;
        var body = css[start..css.IndexOf('}', start)];

        return body.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(declaration => declaration.Split(':', 2))
            .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());
    }

    private static void WithTheme(Action test)
    {
        var app = Application.Current!;
        var theme = new AvonicTheme();
        app.Styles.Add(theme);
        try
        {
            Dispatcher.UIThread.RunJobs();
            test();
        }
        finally
        {
            app.Styles.Remove(theme);
        }
    }

    private static void WithDark(Action test) => WithTheme(() =>
    {
        var app = Application.Current!;
        var dark = Dark();
        app.Resources.MergedDictionaries.Add(dark);
        try
        {
            Dispatcher.UIThread.RunJobs();
            test();
        }
        finally
        {
            app.Resources.MergedDictionaries.Remove(dark);
        }
    });

    private static ResourceInclude Dark() => new(new Uri("avares://Avonic.Themes/"))
    {
        Source = new Uri("avares://Avonic.Themes/Tokens/Palettes/Dark.axaml"),
    };

    private static T Resource<T>(string key)
    {
        Assert.True(Application.Current!.TryGetResource(key, null, out var value), $"{key} doesn't resolve");
        return Assert.IsAssignableFrom<T>(value);
    }

    private static Color BrushColor(string key) => Resource<ISolidColorBrush>(key).Color;

    /// <summary>Black mixed into white at N/1000, the ramp behind Ionic's step variables.</summary>
    private static Color Mix(int step)
    {
        var value = (byte)((255 * (1000 - step) + 500) / 1000); // Integers: 1 - 0.9 isn't 0.1 in floating point.
        return Color.FromRgb(value, value, value);
    }

    // ── Light palette ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Palette_Equals_Core_Css()
    {
        var root = Block(File.ReadAllText(IonicFile("css/core.css")), ":root");

        WithTheme(() =>
        {
            foreach (var name in Names)
            {
                foreach (var (css, key) in Variants)
                {
                    var tokenKey = $"Avonic.Color.{Pascal(name)}{key}";
                    Assert.True(Color.Parse(root[$"--ion-color-{name}{css}"]) == Resource<Color>(tokenKey), $"{tokenKey} differs from core.css");
                }
            }
        });
    }

    [AvaloniaFact]
    public void Every_Palette_Color_Has_A_Brush_With_The_Same_Value() => WithTheme(() =>
    {
        foreach (var name in Names)
        {
            foreach (var (_, key) in Variants)
            {
                var tokenKey = $"Avonic.Color.{Pascal(name)}{key}";
                Assert.True(Resource<Color>(tokenKey) == BrushColor(tokenKey + "Brush"), $"{tokenKey}Brush differs");
            }
        }
    });

    // The reason -rgb variants have no key of their own.
    [AvaloniaFact]
    public void Rgb_Variables_Are_The_Same_Value_As_The_Color()
    {
        var root = Block(File.ReadAllText(IonicFile("css/core.css")), ":root");

        foreach (var name in Names)
        {
            foreach (var css in new[] { "", "-contrast" })
            {
                var channels = root[$"--ion-color-{name}{css}-rgb"].Split(',').Select(byte.Parse).ToArray();
                Assert.Equal(Color.Parse(root[$"--ion-color-{name}{css}"]), Color.FromRgb(channels[0], channels[1], channels[2]));
            }
        }
    }

    // ── Global tokens ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Global_Tokens_Have_Ionics_Md_Defaults() => WithTheme(() =>
    {
        Assert.Equal(Colors.White, Resource<Color>("Avonic.BackgroundColor"));
        Assert.Equal(Colors.Black, Resource<Color>("Avonic.TextColor"));
        Assert.Equal(Color.FromArgb(0x21, 0, 0, 0), Resource<Color>("Avonic.BorderColor")); // rgba(0, 0, 0, 0.13)
        Assert.Equal(Colors.Black, Resource<Color>("Avonic.BackdropColor"));
        Assert.Equal(0.32, Resource<double>("Avonic.BackdropOpacity"));
        Assert.Equal(0.6, Resource<double>("Avonic.PlaceholderOpacity"));
        Assert.Equal(new FontFamily("Roboto, Helvetica Neue, sans-serif"), Resource<FontFamily>("Avonic.FontFamily"));
    });

    [AvaloniaFact]
    public void Global_Colors_Have_Brushes() => WithTheme(() =>
    {
        foreach (var key in new[] { "Avonic.BackgroundColor", "Avonic.TextColor", "Avonic.BorderColor", "Avonic.BackdropColor" })
        {
            Assert.Equal(Resource<Color>(key), BrushColor(key + "Brush"));
        }
    });

    [AvaloniaFact]
    public void Step_Ramps_Follow_The_Formula() => WithTheme(() =>
    {
        foreach (var step in Steps)
        {
            Assert.Equal(Mix(step), Resource<Color>($"Avonic.BackgroundColorStep{step}"));
            Assert.Equal(Mix(1000 - step), Resource<Color>($"Avonic.TextColorStep{step}"));
            Assert.Equal(Mix(step), BrushColor($"Avonic.BackgroundColorStep{step}Brush"));
            Assert.Equal(Mix(1000 - step), BrushColor($"Avonic.TextColorStep{step}Brush"));
        }
    });

    // Ionic's only complete copy of the 19 values: black to white in dark ios, so swapped.
    [Fact]
    public void Step_Ramps_Match_The_Dark_Ios_Ramps()
    {
        var ios = Block(File.ReadAllText(IonicFile("css/palettes/dark.always.css")), ":root.ios");

        foreach (var step in Steps)
        {
            Assert.Equal(Color.Parse(ios[$"--ion-text-color-step-{step}"]), Mix(step));
            Assert.Equal(Color.Parse(ios[$"--ion-background-color-step-{step}"]), Mix(1000 - step));
        }
    }

    // Fallbacks that are one component's own default, so they belong in that control's tokens.
    private static readonly string[] ComponentOwnStepFallbacks =
    [
        "#fff", "#ffffff", "#eef1f3", "#dfe5e8", "#c1c4cd", "#ececec", "#edeef0", "#c0c0be",
        "#35404e", "#677483", "#73849a", "#7d8894", "#222d3a",
    ];

    [GeneratedRegex(@"var\(--ion-(background|text)-color-step-(\d+),\s*([^)]+?)\)")]
    private static partial Regex StepFallback();

    // Light ramps aren't variables in Ionic. Its fallbacks are the evidence for the formula.
    [Fact]
    public void Step_Ramps_Match_Ionics_Fallbacks()
    {
        var components = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(IonicFile("css/core.css"))!, "..", "dist", "collection", "components"));
        var checkedCount = 0;

        foreach (var file in Directory.EnumerateFiles(components, "*.md.css", SearchOption.AllDirectories))
        {
            foreach (Match match in StepFallback().Matches(File.ReadAllText(file)))
            {
                var fallback = match.Groups[3].Value;
                if (fallback.StartsWith("rgba", StringComparison.Ordinal) || ComponentOwnStepFallbacks.Contains(fallback))
                {
                    continue;
                }

                var step = int.Parse(match.Groups[2].Value);
                var expected = match.Groups[1].Value == "background" ? Mix(step) : Mix(1000 - step);
                Assert.True(expected == Color.Parse(fallback), $"{Path.GetFileName(file)}: {match.Value} isn't {expected}");
                checkedCount++;
            }
        }

        Assert.True(checkedCount > 40, $"Only {checkedCount} fallbacks found: the pattern probably no longer matches");
    }

    // Each is var(--ion-…, var(--ion-background-color, #fff)) in Ionic.
    [AvaloniaTheory]
    [InlineData("Avonic.OverlayBackgroundColorBrush")]
    [InlineData("Avonic.ItemBackgroundBrush")]
    [InlineData("Avonic.ToolbarBackgroundBrush")]
    [InlineData("Avonic.TabBarBackgroundBrush")]
    [InlineData("Avonic.CardBackgroundBrush")]
    public void Chained_Tokens_Follow_The_Background(string key) => WithTheme(() =>
    {
        Assert.Equal(Colors.White, BrushColor(key));

        var app = Application.Current!;
        app.Resources["Avonic.BackgroundColor"] = Colors.Lime;
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Colors.Lime, BrushColor(key));
        }
        finally
        {
            app.Resources.Remove("Avonic.BackgroundColor");
        }
    });

    [AvaloniaFact]
    public void Placeholder_Follows_Its_Text_Step() => WithTheme(() =>
    {
        Assert.Equal(Mix(400), BrushColor("Avonic.PlaceholderColorBrush"));

        var app = Application.Current!;
        app.Resources["Avonic.TextColorStep600"] = Colors.Lime;
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Colors.Lime, BrushColor("Avonic.PlaceholderColorBrush"));
        }
        finally
        {
            app.Resources.Remove("Avonic.TextColorStep600");
        }
    });

    [AvaloniaFact]
    public void Overriding_A_Color_Updates_Its_Brush() => WithTheme(() =>
    {
        var app = Application.Current!;
        var before = Resource<ISolidColorBrush>("Avonic.Color.PrimaryBrush");
        app.Resources["Avonic.Color.Primary"] = Colors.Lime;
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Colors.Lime, BrushColor("Avonic.Color.PrimaryBrush"));
            Assert.Same(before, Resource<ISolidColorBrush>("Avonic.Color.PrimaryBrush"));
        }
        finally
        {
            app.Resources.Remove("Avonic.Color.Primary");
        }
    });

    // ── Dark palette ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Dark_Is_Not_Merged_By_The_Theme()
    {
        var includes = new AvonicTheme().Resources.MergedDictionaries.OfType<ResourceInclude>().Select(i => i.Source!.AbsolutePath);

        Assert.Contains("/Tokens/Colors.axaml", includes);
        Assert.Contains("/Tokens/Global.axaml", includes);
        Assert.DoesNotContain(includes, path => path.Contains("Palettes", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void Dark_Palette_Equals_Dark_Css()
    {
        var root = Block(File.ReadAllText(IonicFile("css/palettes/dark.always.css")), ":root");

        WithDark(() =>
        {
            foreach (var name in Names)
            {
                foreach (var (css, key) in Variants)
                {
                    var tokenKey = $"Avonic.Color.{Pascal(name)}{key}";
                    var expected = Color.Parse(root[$"--ion-color-{name}{css}"]);
                    Assert.True(expected == Resource<Color>(tokenKey), $"{tokenKey} differs from dark.always.css");
                    Assert.True(expected == BrushColor(tokenKey + "Brush"), $"{tokenKey}Brush differs from dark.always.css");
                }
            }
        });
    }

    [AvaloniaFact]
    public void Dark_Globals_Equal_Dark_Css_For_Md()
    {
        var md = Block(File.ReadAllText(IonicFile("css/palettes/dark.always.css")), ":root.md");

        WithDark(() =>
        {
            Assert.Equal(Color.Parse(md["--ion-background-color"]), Resource<Color>("Avonic.BackgroundColor"));
            Assert.Equal(Color.Parse(md["--ion-text-color"]), Resource<Color>("Avonic.TextColor"));
            Assert.Equal(Color.Parse(md["--ion-background-color"]), BrushColor("Avonic.BackgroundColorBrush"));

            foreach (var step in Steps)
            {
                Assert.Equal(Color.Parse(md[$"--ion-background-color-step-{step}"]), Resource<Color>($"Avonic.BackgroundColorStep{step}"));
                Assert.Equal(Color.Parse(md[$"--ion-text-color-step-{step}"]), Resource<Color>($"Avonic.TextColorStep{step}"));
            }

            Assert.Equal(Color.Parse(md["--ion-item-background"]), BrushColor("Avonic.ItemBackgroundBrush"));
            Assert.Equal(Color.Parse(md["--ion-toolbar-background"]), BrushColor("Avonic.ToolbarBackgroundBrush"));
            Assert.Equal(Color.Parse(md["--ion-tab-bar-background"]), BrushColor("Avonic.TabBarBackgroundBrush"));
            Assert.Equal(Color.Parse(md["--ion-card-background"]), BrushColor("Avonic.CardBackgroundBrush"));

            // Not set in dark: follows its fallback, or the background.
            Assert.Equal(Color.Parse(md["--ion-background-color-step-150"]), Resource<Color>("Avonic.BorderColor"));
            Assert.Equal(Color.Parse(md["--ion-background-color"]), BrushColor("Avonic.OverlayBackgroundColorBrush"));
            Assert.Equal(Color.Parse(md["--ion-text-color-step-600"]), BrushColor("Avonic.PlaceholderColorBrush"));
        });
    }

    [AvaloniaFact]
    public void Removing_Dark_Restores_Light()
    {
        Color primary = default;
        WithDark(() => primary = BrushColor("Avonic.Color.PrimaryBrush"));
        Assert.Equal(Color.Parse("#4d8dff"), primary);

        WithTheme(() => Assert.Equal(Color.Parse("#0054e9"), BrushColor("Avonic.Color.PrimaryBrush")));
    }
}
