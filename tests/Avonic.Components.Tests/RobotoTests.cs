using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests;

/// <summary>
/// The theme's text is drawn with the Roboto files in <c>tools/fonts/roboto/</c>, the same bytes the Chromium tools load with
/// <c>@font-face</c>, so that text measures the same on both sides of a comparison and on every machine. Each check compares the
/// bytes of the font Avalonia drew with, so a fallback such as Segoe UI or DejaVu Sans fails it, and so does an installed Roboto
/// of another build, or the right family at the wrong weight. See <c>Avonic.Sample.Roboto</c> and <c>tools/fonts/roboto/README.md</c>.
/// </summary>
public class RobotoTests
{
    public static TheoryData<int, string> Weights => new()
    {
        { 400, "Roboto-Regular.ttf" },
        { 500, "Roboto-Medium.ttf" },
        { 700, "Roboto-Bold.ttf" },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Weights))]
    public void The_Plain_Name_Roboto_Is_The_Committed_File(int weight, string file)
    {
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("Roboto", FontStyle.Normal, (FontWeight)weight), out var glyphs));

        AssertIsFile(glyphs, file);
    }

    [AvaloniaFact]
    public void The_Theme_Draws_Its_Text_With_The_Committed_Files() => InstalledTheme.Run(_ =>
    {
        // md: a label is 400, a button 500, and a badge bold.
        var label = new AvonicLabel { Content = "Label" };
        var button = new AvonicButton { Content = "Button" };
        var badge = new AvonicBadge { Content = "22" };
        using var scene = new Scene(new StackPanel { Children = { new AvonicItem { Content = label }, button, badge } });

        AssertDrawnWith(Text(label, "Label"), FontWeight.Normal, "Roboto-Regular.ttf");
        AssertDrawnWith(Text(button, "BUTTON"), FontWeight.Medium, "Roboto-Medium.ttf");
        AssertDrawnWith(Text(badge, "22"), FontWeight.Bold, "Roboto-Bold.ttf");
    });

    /// <summary>
    /// The Chromium side can't run here, as CI has no Chromium, so this reads what it loads. <c>roboto.js</c> checks at run time
    /// that every face loaded, and this checks that each face is the file of its weight, with that file's own line metrics, and
    /// that the tools that render Ionic text use it.
    /// </summary>
    [Fact]
    public void The_Chromium_Tools_Load_The_Same_Files_At_Their_Weights()
    {
        var css = File.ReadAllText(RepoFile("tools", "fonts", "roboto", "roboto.css"));
        var faces = Regex.Matches(css, @"@font-face\s*\{(?<body>[^}]*)\}").Select(face => Declarations(face.Groups["body"].Value)).ToList();

        Assert.Equal(["400", "500", "700"], faces.Select(face => face["font-weight"]).Order());
        foreach (var face in faces)
        {
            Assert.Equal("\"Roboto\"", face["font-family"]);
            Assert.Equal("normal", face["font-style"]);
            var file = Regex.Match(face["src"], @"^url\(""(?<file>[^""/]+\.ttf)""\) format\(""truetype""\)$").Groups["file"].Value;
            var font = File.ReadAllBytes(RepoFile("tools", "fonts", "roboto", file));

            // OS/2's usWeightClass, and hhea's ascender, descender and line gap over head's unitsPerEm, as percentages.
            Assert.Equal(face["font-weight"], BigEndian(font, Table(font, "OS/2") + 4).ToString());
            double em = BigEndian(font, Table(font, "head") + 18);
            var hhea = Table(font, "hhea");
            Assert.Equal(Percent((short)BigEndian(font, hhea + 4) / em), face["ascent-override"]);
            Assert.Equal(Percent(-(short)BigEndian(font, hhea + 6) / em), face["descent-override"]);
            Assert.Equal(Percent((short)BigEndian(font, hhea + 8) / em), face["line-gap-override"]);
        }

        foreach (var tool in new[] { "render.js", "boxes.js" })
        {
            var source = File.ReadAllText(RepoFile("tools", "ionic-ref", tool));
            Assert.Contains("require('../fonts/roboto/roboto')", source);
            Assert.Contains("${roboto.LINK}", source);
            Assert.Contains("await roboto.loadRoboto(", source);
        }
    }

    private static Dictionary<string, string> Declarations(string block) =>
        block.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(declaration => declaration.Split(':', 2, StringSplitOptions.TrimEntries))
            .ToDictionary(pair => pair[0], pair => pair[1]);

    private static string Percent(double fraction) => (fraction * 100).ToString(CultureInfo.InvariantCulture) + "%";

    private static ushort BigEndian(byte[] font, int offset) => (ushort)((font[offset] << 8) | font[offset + 1]);

    private static int Table(byte[] font, string tag)
    {
        for (var i = 0; i < BigEndian(font, 4); i++)
        {
            var record = 12 + (i * 16);
            if (Encoding.ASCII.GetString(font, record, 4) == tag)
            {
                return (BigEndian(font, record + 8) << 16) | BigEndian(font, record + 10);
            }
        }

        throw new InvalidOperationException($"No {tag} table");
    }

    private static TextBlock Text(Control host, string text) =>
        host.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Text == text);

    private static void AssertDrawnWith(TextBlock text, FontWeight weight, string file)
    {
        Assert.Equal(weight, text.FontWeight);
        var run = text.TextLayout.TextLines.SelectMany(line => line.TextRuns).OfType<ShapedTextRun>().First();
        AssertIsFile(run.GlyphRun.GlyphTypeface, file);
    }

    private static void AssertIsFile(GlyphTypeface glyphs, string file)
    {
        Assert.Equal(FontSimulations.None, glyphs.FontSimulations);
        Assert.True(glyphs.PlatformTypeface.TryGetStream(out var stream), $"{glyphs.FamilyName} has no stream");
        using (stream)
        {
            var drawn = SHA256.HashData(stream);
            var committed = SHA256.HashData(File.ReadAllBytes(RepoFile("tools", "fonts", "roboto", file)));
            Assert.True(drawn.SequenceEqual(committed), $"Drew with {glyphs.FamilyName} {glyphs.Weight}, not tools/fonts/roboto/{file}");
        }
    }

    private static string RepoFile(params string[] parts)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Avonic.sln")))
            {
                return Path.Combine([dir.FullName, .. parts]);
            }
        }

        throw new InvalidOperationException("Avonic.sln not found above " + AppContext.BaseDirectory);
    }
}
