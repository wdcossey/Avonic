using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Typography;

/// <summary>
/// Ionic's css/typography.css, as <c>Css/Typography.axaml</c>, which an app merges itself. Every value here was measured in
/// Chromium with typography.css on and off: docs/infra/typography/PLAN.md.
/// </summary>
public class TypographyTests
{
    internal static ResourceInclude File() => new(new Uri("avares://Avonic.Themes/"))
    {
        Source = new Uri("avares://Avonic.Themes/Css/Typography.axaml"),
    };

    /// <summary>The theme in the app's styles, and the file in the app's resources, as the docs say to merge it.</summary>
    internal static void WithFile(Action test) => InstalledTheme.Run(_ =>
    {
        var app = Application.Current!;
        var file = File();
        app.Resources.MergedDictionaries.Add(file);
        try
        {
            Dispatcher.UIThread.RunJobs();
            test();
        }
        finally
        {
            app.Resources.MergedDictionaries.Remove(file);
        }
    });

    private static TextBlock Heading(string name, double? fontSize = null)
    {
        var block = new TextBlock { Text = "x", Classes = { name } };
        if (fontSize is { } size)
        {
            block.FontSize = size;
        }

        return block;
    }

    private static Scene Show(params Control[] controls)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(controls);
        return new Scene(panel);
    }

    private static void AssertTypography(TextBlock block, double size, double top)
    {
        Assert.Equal(size, block.FontSize);
        Assert.Equal(new Thickness(0, top, 0, 10), block.Margin);
        Assert.Equal(FontWeight.Medium, block.FontWeight);
        Assert.Equal(size * 1.2, block.LineHeight, 6);
        Assert.False(block.ClipToBounds);
    }

    // ── 1. h1 to h6, anywhere ────────────────────────────────────────────────────────────────

    // Measured bh1 to bh6: sizes 26 to 16, weight 500, line height 1.2, 10 below, 20, 18 or 16 above.
    [AvaloniaTheory]
    [InlineData("h1", 26, 20)]
    [InlineData("h2", 24, 18)]
    [InlineData("h3", 22, 16)]
    [InlineData("h4", 20, 16)]
    [InlineData("h5", 18, 16)]
    [InlineData("h6", 16, 16)]
    public void B1_Each_Heading_Has_Its_Measured_Values(string name, double size, double top) => WithFile(() =>
    {
        var block = Heading(name);
        using var scene = Show(block);

        AssertTypography(block, size, top);
    });

    [AvaloniaTheory]
    [InlineData("h1")]
    [InlineData("h3")]
    [InlineData("h6")]
    public void B1_Without_The_File_A_Heading_Has_No_Rules(string name) => InstalledTheme.Run(_ =>
    {
        var block = Heading(name);
        var plain = new TextBlock { Text = "x" };
        using var scene = Show(block, plain);

        Assert.Equal(plain.FontSize, block.FontSize);
        Assert.Equal(default, block.Margin);
        Assert.Equal(FontWeight.Normal, block.FontWeight);
        Assert.True(double.IsNaN(block.LineHeight));
        Assert.True(block.ClipToBounds);
    });

    // typography.css has no rule for p. Measured bp: the same with and without it, apart from the font.
    [AvaloniaFact]
    public void B1_A_Paragraph_Has_No_Rules() => WithFile(() =>
    {
        var p = Heading("p");
        var plain = new TextBlock { Text = "x" };
        using var scene = Show(p, plain);

        Assert.Equal(plain.FontSize, p.FontSize);
        Assert.Equal(default, p.Margin);
        Assert.True(double.IsNaN(p.LineHeight));
    });

    [AvaloniaFact]
    public void B1_Line_Height_Is_A_Factor_Of_The_Headings_Own_Size() => WithFile(() =>
    {
        var block = Heading("h1");
        using var scene = Show(block);

        block.FontSize = 40;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(48, block.LineHeight, 6);
    });

    // A rule beats an inherited value in CSS: the size doesn't follow the parent's.
    [AvaloniaFact]
    public void B1_A_Heading_Does_Not_Inherit_Its_Parents_Size_Or_Weight() => WithFile(() =>
    {
        var block = Heading("h2");
        using var scene = Show(new Border { Child = block, [TextElement.FontSizeProperty] = 40, [TextElement.FontWeightProperty] = FontWeight.Bold });

        AssertTypography(block, 24, 18);
    });

    [AvaloniaFact]
    public void B1_A_Class_Added_Later_Applies_And_Removed_Goes() => WithFile(() =>
    {
        var block = new TextBlock { Text = "x" };
        using var scene = Show(block);

        block.Classes.Add("h4");
        Dispatcher.UIThread.RunJobs();
        AssertTypography(block, 20, 16);

        block.Classes.Remove("h4");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(default, block.Margin);
        Assert.True(double.IsNaN(block.LineHeight));
    });

    [AvaloniaFact]
    public void B1_Descenders_Are_Not_Clipped() => WithFile(() =>
    {
        // A line height far below the font's height, so anything clipped is obvious: the file mustn't clip it.
        var block = new TextBlock { Text = "ggg", Classes = { "h1" }, Foreground = Brushes.Black, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        using var scene = new Scene(new Panel { Children = { block } });
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();
        block.LineHeight = 4;
        Dispatcher.UIThread.RunJobs();

        var frame = scene.Window.CaptureRenderedFrame()!;
        var below = (int)Math.Ceiling(block.Bounds.Bottom) + 1;
        var inked = false;
        for (var y = below; y < below + 20 && y < frame.PixelSize.Height; y++)
        {
            for (var x = 0; x < (int)block.Bounds.Right; x++)
            {
                inked |= Frames.PixelAt(frame, x, y) != Colors.White;
            }
        }

        Assert.True(inked);
    });

    // ── 2. An app's rules win ────────────────────────────────────────────────────────────────

    // An app's own h1 rule beats typography.css's, as it's as specific and later.
    [AvaloniaFact]
    public void B2_An_Apps_Style_Wins() => WithFile(() =>
    {
        var style = new Style(x => x.OfType<TextBlock>().Class("h1"));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 40.0));
        Application.Current!.Styles.Add(style);
        try
        {
            var block = Heading("h1");
            using var scene = Show(block);

            Assert.Equal(40, block.FontSize);
            Assert.Equal(48, block.LineHeight, 6);
        }
        finally
        {
            Application.Current!.Styles.Remove(style);
        }
    });

    // A plain TextBlock rule, with no class, loses to the file's h1, as CSS's * does: a rule with a class beats one without in Avalonia,
    // even from a ControlTheme.
    [AvaloniaFact]
    public void B2_An_Apps_Plain_TextBlock_Style_Loses() => WithFile(() =>
    {
        var style = new Style(x => x.OfType<TextBlock>());
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 40.0));
        Application.Current!.Styles.Add(style);
        try
        {
            var block = Heading("h1");
            var plain = new TextBlock { Text = "x" };
            using var scene = Show(block, plain);

            Assert.Equal(26, block.FontSize);
            Assert.Equal(40, plain.FontSize);
        }
        finally
        {
            Application.Current!.Styles.Remove(style);
        }
    });

    [AvaloniaFact]
    public void B2_A_Local_Value_Wins() => WithFile(() =>
    {
        var block = new TextBlock { Text = "x", Classes = { "h1" }, Margin = new Thickness(4) };
        using var scene = Show(block);

        Assert.Equal(new Thickness(4), block.Margin);
        Assert.Equal(26, block.FontSize);
    });

    // ── 3. Where it's merged ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Merged_In_A_Controls_Resources_It_Applies_Inside_Only() => InstalledTheme.Run(_ =>
    {
        var inside = Heading("h1");
        var outside = Heading("h1");
        var host = new StackPanel { Children = { inside } };
        host.Resources.MergedDictionaries.Add(File());
        using var scene = Show(host, outside);

        AssertTypography(inside, 26, 20);
        Assert.Equal(default, outside.Margin);
        Assert.Equal(FontWeight.Normal, outside.FontWeight);
    });

    // The file in the app's resources and AvonicTheme in its styles: whichever was added last, the label's rules beat the file's, as a
    // global style beats a ControlTheme, and the file's keys beat the theme's, as the app's resources are looked up before its styles'.
    [AvaloniaFact]
    public void B3_Whichever_Is_Added_Last_The_Label_And_The_File_Share_As_Measured() => WithFile(() =>
    {
        var app = Application.Current!;
        var theme = app.Styles.OfType<Avonic.Themes.AvonicTheme>().Single();
        app.Styles.Remove(theme);
        app.Styles.Add(theme);
        var block = Heading("h1");
        using var scene = Show(new AvonicItem { Content = new AvonicLabel { Content = block } });

        Assert.Equal(24, block.FontSize);
        Assert.Equal(FontWeight.Normal, block.FontWeight);
        Assert.Equal(new Thickness(0, 0, 0, 2), block.Margin);
        Assert.Equal(28.8, block.LineHeight, 6);
    });

    // A base theme with its own ControlTheme for TextBlock would be replaced by the file. SimpleTheme has none.
    [AvaloniaFact]
    public void B3_SimpleTheme_Has_No_Theme_For_TextBlock_That_The_File_Replaces()
    {
        var simple = new Avalonia.Themes.Simple.SimpleTheme();

        Assert.False(simple.TryGetResource(typeof(TextBlock), null, out _));
    }

    // ── 4. Hosts: what wins where, as measured ───────────────────────────────────────────────

    // Measured lh1 to lh6: the label's scoped rules beat the file's, more specific, but they set no line height for h1 and h2,
    // so the file's 1.2 applies to them. h3 to h6 keep the label's normal.
    [AvaloniaTheory]
    [InlineData("h1", 24, 0, 28.8)]
    [InlineData("h2", 16, 2, 19.2)]
    [InlineData("h3", 14, 2, double.NaN)]
    [InlineData("h6", 14, 2, double.NaN)]
    public void B4_In_A_Label_The_Labels_Rules_Win_But_For_Line_Height(string name, double size, double top, double lineHeight) => WithFile(() =>
    {
        var direct = Heading(name);
        var inPanel = Heading(name);
        using var scene = Show(
            new AvonicItem { Content = new AvonicLabel { Content = direct } },
            new AvonicItem { Content = new AvonicLabel { Content = new StackPanel { Children = { inPanel } } } });

        foreach (var block in new[] { direct, inPanel })
        {
            Assert.Equal(size, block.FontSize);
            Assert.Equal(new Thickness(0, top, 0, 2), block.Margin);
            Assert.Equal(FontWeight.Normal, block.FontWeight);
            Assert.Equal(lineHeight, block.LineHeight, 6);
        }
    });

    // In a wrapping label the file's 1.2 still beats the 1.5 they'd inherit.
    [AvaloniaFact]
    public void B4_In_A_Wrapping_Label_Headings_Have_The_Files_Line_Height() => WithFile(() =>
    {
        var h2 = Heading("h2");
        var label = new AvonicLabel { Content = new StackPanel { Children = { h2 } }, Classes = { "ion-text-wrap" } };
        using var scene = Show(new AvonicItem { Content = label });

        Assert.Equal(19.2, h2.LineHeight, 6);
    });

    // Measured ch1 to ch6: card content's rules beat the file's, and the file's 1.2 beats the 1.5 they'd inherit.
    [AvaloniaTheory]
    [InlineData("h1", 24, 0, 28.8)]
    [InlineData("h2", 16, 2, 19.2)]
    [InlineData("h3", 14, 2, 16.8)]
    [InlineData("h6", 14, 2, 16.8)]
    public void B4_In_Card_Content_Its_Rules_Win_But_For_Line_Height(string name, double size, double top, double lineHeight) => WithFile(() =>
    {
        var direct = Heading(name);
        var inPanel = Heading(name);
        using var scene = Show(
            new AvonicCardContent { Content = direct },
            new AvonicCardContent { Content = new StackPanel { Children = { inPanel } } });

        foreach (var block in new[] { direct, inPanel })
        {
            Assert.Equal(size, block.FontSize);
            Assert.Equal(new Thickness(0, top, 0, 2), block.Margin);
            Assert.Equal(FontWeight.Normal, block.FontWeight);
            Assert.Equal(lineHeight, block.LineHeight, 6);
        }
    });

    // A card's p sets its own 1.5, which the file doesn't touch. Measured cp: 21 both ways.
    [AvaloniaFact]
    public void B4_In_Card_Content_A_Paragraph_Keeps_Its_Own() => WithFile(() =>
    {
        var p = Heading("p");
        using var scene = Show(new AvonicCardContent { Content = p });

        Assert.Equal(21, p.LineHeight, 6);
    });

    // Measured dh1 to dh6: the file beats the divider's ::slotted(h1) margins, as a document's rule beats a shadow root's.
    [AvaloniaTheory]
    [InlineData("h1", 26, 20)]
    [InlineData("h2", 24, 18)]
    [InlineData("h3", 22, 16)]
    [InlineData("h6", 16, 16)]
    public void B4_In_An_Item_Divider_The_File_Wins(string name, double size, double top) => WithFile(() =>
    {
        var block = Heading(name);
        using var scene = Show(new AvonicItemDivider { Content = block });

        AssertTypography(block, size, top);
    });

    // Measured in Chromium with the file: an h1 in a card title is 26, 31.2 high, 20 above and 10 below. The title sets its size on itself,
    // which the h1 doesn't inherit, and its rule for any child gives the same 1.2.
    [AvaloniaFact]
    public void B4_In_A_Card_Title_The_File_Wins() => WithFile(() =>
    {
        var block = Heading("h1");
        using var scene = Show(new AvonicCard { Content = new AvonicCardHeader { Content = new AvonicCardTitle { Content = block } } });

        AssertTypography(block, 26, 20);
    });

    // Measured: an h1 in a button is 26 and 31.2 high, as anywhere. Its margins differ here, deliberately: see the plan.
    [AvaloniaFact]
    public void B4_In_A_Button_The_Files_Size_And_Line_Height_Apply() => WithFile(() =>
    {
        var block = Heading("h1");
        using var scene = Show(new AvonicButton { Content = block });

        Assert.Equal(26, block.FontSize);
        Assert.Equal(31.2, block.LineHeight, 6);
        Assert.Equal(FontWeight.Medium, block.FontWeight);
    });
}
