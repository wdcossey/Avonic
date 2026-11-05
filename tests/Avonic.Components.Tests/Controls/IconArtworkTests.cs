using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The bundled artwork. These read ionicons' own files, so an upgrade without a regenerate fails. They
/// skip without <c>node_modules</c>, and CI always has it.
/// </summary>
[Collection(IconStoreCollection.Name)]
public partial class IconArtworkTests
{
    private static string SvgDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Avonic.sln")))
            {
                var path = Path.Combine(dir.FullName, "node_modules", "ionicons", "dist", "svg");
                if (!Directory.Exists(path))
                {
                    Assert.Skip($"{path} is missing: run npm install");
                }

                return path;
            }
        }

        throw new InvalidOperationException("Avonic.sln not found above " + AppContext.BaseDirectory);
    }

    private static IEnumerable<(string Name, string Svg)> SourceIcons() =>
        Directory.EnumerateFiles(SvgDirectory(), "*.svg")
            .Select(file => (Path.GetFileNameWithoutExtension(file), File.ReadAllText(file)));

    [GeneratedRegex("<(path|circle|rect|ellipse|line|polyline|polygon)[\\s/>]")]
    private static partial Regex Shape();

    [AvaloniaFact]
    public void The_Bundle_Has_Every_Icon_In_Ionicons() =>
        Assert.Equal(
            SourceIcons().Select(icon => icon.Name).Order(StringComparer.Ordinal),
            IconStore.BundledNames.Order(StringComparer.Ordinal));

    [AvaloniaFact]
    public void The_Bundle_Holds_Each_Svg_Unchanged()
    {
        var mismatched = SourceIcons()
            .Where(icon => IconStore.BundledSvg(icon.Name) != icon.Svg.ReplaceLineEndings(" ").Trim())
            .Select(icon => icon.Name)
            .ToList();

        Assert.Empty(mismatched);
    }

    // A layer that failed to parse would silently vanish from the icon.
    [AvaloniaFact]
    public void Every_Icon_Parses_To_One_Layer_Per_Element()
    {
        var wrong = new List<string>();
        var count = 0;

        foreach (var (name, svg) in SourceIcons())
        {
            var expected = Shape().Matches(svg).Count;
            var actual = SvgArtwork.Parse(svg).Layers.Count;
            if (expected == 0 || actual != expected)
            {
                wrong.Add($"{name}: {actual} of {expected}");
            }

            count++;
        }

        Assert.Empty(wrong);
        Assert.True(count > 1000, $"Only {count} icons found");
    }

    [AvaloniaFact]
    public void The_Icons_Ionics_Components_Import_Exist()
    {
        string[] used =
        [
            "chevron-down", "arrow-back-sharp", "chevron-back", "chevron-forward-outline", "ellipsis-horizontal",
            "caret-down-sharp", "caret-up-sharp", "chevron-forward", "close", "close-circle", "close-sharp", "eye-off",
            "eye", "menu-outline", "menu-sharp", "arrow-down", "caret-back-sharp", "reorder-three-outline",
            "reorder-two-sharp", "search-outline", "search-sharp", "chevron-expand", "checkmark-outline",
            "remove-outline", "ellipse-outline",
        ];

        Assert.All(used, name => Assert.NotEmpty(IconStore.GetNamed(name).Layers));
    }
}
