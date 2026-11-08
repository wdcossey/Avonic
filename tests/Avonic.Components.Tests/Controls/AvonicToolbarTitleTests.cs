using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicToolbarCaptureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary><c>ion-title</c>. The numbers are from real Ionic in Chromium at 390 px wide; see docs/components/ion-toolbar/PLAN.md.</summary>
public class AvonicToolbarTitleTests
{
    private static TextBlock Text(AvonicTitle title) => title.GetVisualDescendants().OfType<TextBlock>().Single();

    // ── Ti1 ─────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Ti1_Twenty_At_500_Padded_20_Filling_The_Content_Centred() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicTitle { Content = "Header" };
        var toolbar = new AvonicToolbar
        {
            StartContent = Buttons(IconButton("menu")),
            Content = title,
            EndContent = Buttons(new AvonicButton { Content = "Edit" }, IconButton("search")),
        };
        using var scene = ToolbarTest.Show(toolbar);

        // Measured: the title 52 to the end buttons, its text 20 inside it, one line high in a 56 row, centred.
        var box = ToolbarTest.Box(title, toolbar);
        var end = ToolbarTest.Box((Control)toolbar.EndContent!, toolbar);
        Assert.Equal(52, box.X);
        ToolbarTest.Near(end.X, box.Right);
        // As high as its line, which is 23 in Chromium's Arial and 27 in Avalonia's Segoe UI, as Roboto isn't installed.
        Assert.Equal(Text(title).Bounds.Height, box.Height);
        ToolbarTest.Near(28, box.Center.Y, 1);
        Assert.Equal(new Thickness(20, 0), title.Padding);
        Assert.Equal(72, ToolbarTest.Box(Text(title), toolbar).X);
        Assert.Equal(20, Text(title).FontSize);
        Assert.Equal(FontWeight.Medium, Text(title).FontWeight);
        Assert.Equal(0.25, Text(title).LetterSpacing, 3);
        Assert.Equal(TextWrapping.NoWrap, Text(title).TextWrapping);
        Assert.Equal(TextTrimming.CharacterEllipsis, Text(title).TextTrimming);
        Assert.Equal(ToolbarTest.Toolbar, ToolbarTest.Solid(title.Foreground));
    });

    [AvaloniaFact]
    public void Ti1_A_Taller_Title_Centres_Its_Text() => InstalledTheme.Run(_ =>
    {
        // :host { display: flex; align-items: center }
        var title = new AvonicTitle { Content = "Tall", Height = 56 };
        using var scene = ToolbarTest.Show(title);

        ToolbarTest.Near(28, ToolbarTest.Box(Text(title), title).Center.Y, 1);
    });

    [AvaloniaFact]
    public void Ti1_The_Title_Inherits_The_Text_Colour() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicTitle { Content = "Primary" };
        using var scene = ToolbarTest.Show(new AvonicToolbar { Color = "primary", Content = title });

        Assert.Equal(Colors.White, ToolbarTest.Solid(title.Foreground));
    });

    // ── Ti2 ─────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Ti2_Small_Is_15_At_Normal_Weight_And_Wraps() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicTitle { Content = "Small title that is far too long to fit in the toolbar on one line", Size = TitleSize.Small };
        using var scene = ToolbarTest.Show(new AvonicToolbar { Content = title });

        Assert.Equal(15, Text(title).FontSize);
        Assert.Equal(FontWeight.Normal, Text(title).FontWeight);
        Assert.True(Text(title).TextLayout.TextLines.Count > 1);

        title.Size = null;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(20, Text(title).FontSize);
        Assert.Single(Text(title).TextLayout.TextLines);
    });

    // ── Ti3 ─────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Ti3_Large_Looks_Like_The_Default() => InstalledTheme.Run(_ =>
    {
        var large = new AvonicTitle { Content = "Title", Size = TitleSize.Large };
        var plain = new AvonicTitle { Content = "Title" };
        using var scene = new Scene(new StackPanel { Children = { new AvonicToolbar { Content = large }, new AvonicToolbar { Content = plain } } });

        Assert.Equal(plain.Bounds.Size, large.Bounds.Size);
        Assert.Equal(Text(plain).FontSize, Text(large).FontSize);
        Assert.Equal(Text(plain).FontWeight, Text(large).FontWeight);
        Assert.Equal(Text(plain).TextWrapping, Text(large).TextWrapping);
    });

    // ── Ti4 ─────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Ti4_Color_Paints_Base_An_Unknown_One_Inherits_And_The_Variable_Applies() => InstalledTheme.Run(_ =>
    {
        var tertiary = new AvonicTitle { Content = "a", Color = "tertiary" };
        var unknown = new AvonicTitle { Content = "b", Color = "nonexistent" };
        var variable = new AvonicTitle { Content = "c", Resources = { ["Avonic.Title.Color"] = Brushes.Green } };
        var page = new StackPanel
        {
            Children = { new AvonicToolbar { Content = tertiary }, new AvonicToolbar { Content = unknown }, new AvonicToolbar { Content = variable } },
        };
        using var scene = new Scene(page);

        Assert.Equal(Color.Parse("#6030FF"), ToolbarTest.Solid(tertiary.Foreground));
        Assert.Equal(ToolbarTest.Toolbar, ToolbarTest.Solid(unknown.Foreground));
        Assert.Equal(Colors.Green, ToolbarTest.Solid(variable.Foreground));
    });
}
