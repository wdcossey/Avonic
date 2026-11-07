using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>The numbers are from real ion-item-divider in Chromium, at 390 px wide. See docs/components/ion-item-divider/PLAN.md.</summary>
public class AvonicItemDividerTests
{
    private const string Long = "Very long text that certainly must overflow the phone viewport because it just goes on and on and on and on and on";

    private static readonly Color Grey = Color.FromRgb(153, 153, 153);
    private static readonly Color ParagraphGrey = Color.FromRgb(102, 102, 102);
    private static readonly Color Primary = Color.FromRgb(0, 84, 233);
    private static readonly Color Danger = Color.FromRgb(197, 0, 15);
    private static readonly Color Token = Color.FromRgb(18, 52, 86);
    private static readonly Color BorderGrey = Color.FromArgb(0x21, 0, 0, 0);

    private static T Part<T>(Control control, string name)
        where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

    private static Border Host(Control divider) => divider.GetVisualChildren().OfType<Border>().First();

    private static Color Solid(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Rect Box(Visual visual, Visual root)
    {
        var topLeft = visual.TranslatePoint(default, root)!.Value;
        return new Rect(topLeft, visual.Bounds.Size);
    }

    private static void Run() => Dispatcher.UIThread.RunJobs();

    /// <summary>Close to a colour.</summary>
    private static bool IsColor(Color pixel, int r, int g, int b) =>
        Math.Abs(pixel.R - r) <= 2 && Math.Abs(pixel.G - g) <= 2 && Math.Abs(pixel.B - b) <= 2;

    // ── The row ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void D1_It_Is_31_High_With_A_30_Inner_And_Its_Padding() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider { Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        Assert.Equal(new Rect(0, 0, 390, 31), Box(divider, scene.Window));
        Assert.Equal(new Rect(16, 0, 374, 30), Box(Part<Border>(divider, "PART_Inner"), scene.Window));
        Assert.Equal(new Rect(16, 0, 358, 30), Box(Part<ContentPresenter>(divider, "PART_Container"), scene.Window));
        Assert.Equal(14, divider.GetVisualDescendants().OfType<TextBlock>().Single().FontSize);
    });

    [AvaloniaFact]
    public void D1_Its_Text_Is_Vertically_Centred() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider { Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        var box = Box(divider.GetVisualDescendants().OfType<TextBlock>().Single(), divider);
        Assert.InRange(box.Top - (30 - box.Bottom), -1, 1);
        Assert.True(box.Top > 0);
    });

    [AvaloniaFact]
    public void D1_Its_Weight_Is_Inherited() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider();
        var parent = new StackPanel { Children = { divider } };
        TextElement.SetFontWeight(parent, FontWeight.Bold);
        using var scene = new Scene(parent);

        Assert.Equal(FontWeight.Bold, divider.FontWeight);
    });

    [AvaloniaFact]
    public void D1_It_Uses_The_Font_Token() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider();
        using var scene = new Scene(divider);
        scene.Window.Resources["Avonic.FontFamily"] = new FontFamily("Courier New");
        Run();

        Assert.Equal("Courier New", divider.FontFamily.Name);
    });

    [AvaloniaFact]
    public void D1_It_Works_On_The_Stub_Without_A_Theme()
    {
        var start = new Border { Width = 5 };
        var content = new TextBlock { Text = "x" };
        var end = new Border { Width = 7 };
        var divider = new AvonicItemDivider { StartContent = start, Content = content, EndContent = end };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        Assert.Same(Part<ContentPresenter>(divider, "PART_Start"), start.GetVisualParent());
        Assert.Same(Part<ContentPresenter>(divider, "PART_Container"), content.GetVisualParent());
        Assert.Same(Part<ContentPresenter>(divider, "PART_End"), end.GetVisualParent());
        Assert.Equal(new Point(390 - 7, 0), end.TranslatePoint(default, divider)!.Value.WithY(0));
    }

    [AvaloniaFact]
    public void D1_The_Stub_Passes_On_What_A_Theme_Sets()
    {
        static IDataTemplate Marker(string text) => new FuncDataTemplate<object>((_, _) => new TextBlock { Text = text });
        var divider = new AvonicItemDivider
        {
            Background = Brushes.Red,
            BorderBrush = Brushes.Blue,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            StartContent = 1,
            StartContentTemplate = Marker("s"),
            Content = 2,
            ContentTemplate = Marker("c"),
            EndContent = 3,
            EndContentTemplate = Marker("e"),
        };
        using var scene = new Scene(divider);

        Assert.Same(Brushes.Red, Host(divider).Background);
        Assert.Same(Brushes.Blue, Host(divider).BorderBrush);
        Assert.Equal(new Thickness(1), Host(divider).BorderThickness);
        Assert.Equal(new Thickness(4), Host(divider).Padding);
        Assert.Equal(["s", "c", "e"], divider.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text));
    }

    [AvaloniaFact]
    public void D1_Each_Template_Is_Used() => InstalledTheme.Run(_ =>
    {
        static IDataTemplate Marker(string text) => new FuncDataTemplate<object>((_, _) => new TextBlock { Text = text });
        var divider = new AvonicItemDivider
        {
            StartContent = 1,
            StartContentTemplate = Marker("s"),
            Content = 2,
            ContentTemplate = Marker("c"),
            EndContent = 3,
            EndContentTemplate = Marker("e"),
        };
        using var scene = new Scene(divider);

        Assert.Equal(["s", "c", "e"], divider.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text));
    });

    [AvaloniaFact]
    public void D2_It_Has_A_Grey_Text_On_The_Background_With_A_Bottom_Border() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider { Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        Assert.Equal(Colors.White, Solid(Host(divider).Background));
        Assert.Equal(Grey, Solid(TextElement.GetForeground(divider.GetVisualDescendants().OfType<TextBlock>().Single())));
        Assert.Equal(new Thickness(0, 0, 0, 1), Host(divider).BorderThickness);
        Assert.Equal(BorderGrey, Solid(Host(divider).BorderBrush));
    });

    [AvaloniaFact]
    public void D3_A_Colour_Paints_Base_And_Contrast_And_Leaves_The_Border() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider { Color = "primary", Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        Assert.Equal(Primary, Solid(Host(divider).Background));
        Assert.Equal(Colors.White, Solid(divider.Foreground));
        Assert.Equal(BorderGrey, Solid(Host(divider).BorderBrush));
    });

    [AvaloniaFact]
    public void D3_The_Translucent_Border_Darkens_The_Background_Under_It() => InstalledTheme.Run(_ =>
    {
        // Measured: rgb(0, 84, 233) with 13% black over it, because CSS's background reaches under the border.
        var divider = new AvonicItemDivider { Color = "primary" };
        using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { divider } });

        var row = Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, 100, 30);
        Assert.True(IsColor(row, 0, 73, 203), row.ToString());
    });

    [AvaloniaFact]
    public void D3_An_Unknown_Colour_Is_Transparent_And_Inherits() => InstalledTheme.Run(_ =>
    {
        // Measured d-bad: rgb(0, 0, 0), from what's around it, not the grey of its own variable.
        var divider = new AvonicItemDivider { Color = "nonexistent", Content = "x" };
        var parent = new StackPanel { Children = { divider } };
        TextElement.SetForeground(parent, new SolidColorBrush(Token));
        using var scene = new Scene(parent);

        Assert.Null(Host(divider).Background);
        Assert.Equal(Token, Solid(divider.Foreground));
    });

    [AvaloniaFact]
    public void D4_The_Brush_Tokens_Are_Read() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider();
        using var scene = new Scene(divider);
        scene.Window.Resources["Avonic.ItemDivider.Background"] = new SolidColorBrush(Token);
        scene.Window.Resources["Avonic.ItemDivider.Color"] = new SolidColorBrush(Danger);
        Run();

        Assert.Equal(Token, Solid(Host(divider).Background));
        Assert.Equal(Danger, Solid(divider.Foreground));
    });

    [AvaloniaFact]
    public void D4_The_Padding_Tokens_Are_Read() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider();
        using var scene = new Scene(new StackPanel { Children = { divider } });
        scene.Window.Resources["Avonic.ItemDivider.PaddingStart"] = 1d;
        scene.Window.Resources["Avonic.ItemDivider.PaddingTop"] = 2d;
        scene.Window.Resources["Avonic.ItemDivider.PaddingEnd"] = 3d;
        scene.Window.Resources["Avonic.ItemDivider.PaddingBottom"] = 4d;
        scene.Window.Resources["Avonic.ItemDivider.InnerPaddingStart"] = 5d;
        scene.Window.Resources["Avonic.ItemDivider.InnerPaddingTop"] = 6d;
        scene.Window.Resources["Avonic.ItemDivider.InnerPaddingEnd"] = 7d;
        scene.Window.Resources["Avonic.ItemDivider.InnerPaddingBottom"] = 8d;
        Run();

        Assert.Equal(new Thickness(1, 2, 3, 4), Host(divider).Padding);
        Assert.Equal(new Thickness(5, 6, 7, 8), Part<Border>(divider, "PART_Inner").Padding);
    });

    // ── Slots ────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void D5_Slotted_Content_Has_16_Beside_The_Text() => InstalledTheme.Run(_ =>
    {
        var start = new Border { Width = 10, Height = 10 };
        var end = new Border { Width = 10, Height = 10 };
        var divider = new AvonicItemDivider { StartContent = start, EndContent = end };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        // Measured d-slot2: 16 to the text after the start, and the end ends at the inner padding.
        Assert.Equal(new Thickness(0, 0, 16, 0), start.Margin);
        Assert.Equal(new Thickness(16, 0, 0, 0), end.Margin);
        Assert.Equal(42, Box(Part<Border>(divider, "PART_Inner"), scene.Window).Left);
        Assert.Equal(364, Box(end, scene.Window).Left);

        // Centred in the 30 of the inner box.
        Assert.Equal(10, Box(start, scene.Window).Top);
        Assert.Equal(10, Box(end, scene.Window).Top);
    });

    [AvaloniaFact]
    public void D5_Slotted_Content_Keeps_Its_Own_Height_And_Is_Centred() => InstalledTheme.Run(_ =>
    {
        // Measured d-slot2: align-items: center, so a span in a slot is its own height, not the row's.
        var start = new TextBlock { Text = "x" };
        var end = new TextBlock { Text = "x" };
        var divider = new AvonicItemDivider { StartContent = start, EndContent = end };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        Assert.True(start.Bounds.Height < 30);
        Assert.True(end.Bounds.Height < 30);
        Assert.InRange(Box(start, divider).Top - (30 - Box(start, divider).Bottom), -1, 1);
        Assert.InRange(Box(end, divider).Top - (30 - Box(end, divider).Bottom), -1, 1);
    });

    [AvaloniaFact]
    public void D5_Slotted_Icons_Have_Their_Size_Margins_And_Colour() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "heart" };
        var end = new AvonicIcon { IconName = "star" };
        var divider = new AvonicItemDivider { StartContent = start, Content = new AvonicLabel { Content = "x" }, EndContent = end };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        // Measured d-slots: 49 high, inner from 72, wrapper to 334, end icon from 350.
        Assert.Equal(new Rect(16, 12, 24, 24), Box(start, scene.Window));
        Assert.Equal(new Thickness(0, 12, 32, 12), start.Margin);
        Assert.Equal(new Rect(350, 12, 24, 24), Box(end, scene.Window));
        Assert.Equal(new Thickness(16, 12, 0, 12), end.Margin);
        Assert.Equal(49, divider.Bounds.Height);
        Assert.Equal(new Rect(72, 0, 318, 48), Box(Part<Border>(divider, "PART_Inner"), scene.Window));
        Assert.Equal(new Rect(72, 0, 262, 48), Box(Part<ContentPresenter>(divider, "PART_Container"), scene.Window));

        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(start.Foreground);
        Assert.Equal(Colors.Black, brush.Color);
        Assert.Equal(0.54, brush.Opacity, 3);
    });

    [AvaloniaFact]
    public void D5_An_Icon_In_The_Default_Slot_Has_The_Size_But_No_Margin() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "heart", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        var divider = new AvonicItemDivider { Content = icon };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        Assert.Equal(new Thickness(0), icon.Margin);
        Assert.Equal(new Rect(16, 3, 24, 24), Box(icon, scene.Window));
        Assert.Equal(0.54, Assert.IsAssignableFrom<ISolidColorBrush>(icon.Foreground).Opacity, 3);
        Assert.Equal(31, divider.Bounds.Height);
    });

    [AvaloniaFact]
    public void D5_A_Panel_Stands_For_The_Slot_For_Size_And_Colour() => InstalledTheme.Run(_ =>
    {
        // <ion-icon/><ion-label/> are both slotted. Here they share a panel.
        var plain = new AvonicIcon { IconName = "heart" };
        var colored = new AvonicIcon { IconName = "heart" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicItemDivider { Content = new StackPanel { Children = { plain } } },
                new AvonicItemDivider { Color = "primary", Content = new StackPanel { Children = { colored } } },
            },
        });

        Assert.Equal(24, plain.Bounds.Height);
        Assert.Equal(0.54, Assert.IsAssignableFrom<ISolidColorBrush>(plain.Foreground).Opacity, 3);
        Assert.Equal(Colors.White, Solid(colored.Foreground));
        Assert.Equal(1, colored.Foreground!.Opacity);
    });

    [AvaloniaFact]
    public void D5_An_Icon_Nested_Deeper_Gets_None_Of_It() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "heart" };
        var divider = new AvonicItemDivider { StartContent = new Border { Child = icon } };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        Assert.Equal(new Thickness(0), icon.Margin);
        Assert.Equal(14, icon.FontSize);
    });

    [AvaloniaTheory]
    [InlineData("danger")]
    [InlineData("primary")]
    public void D5_In_A_Colour_Icons_Use_The_Contrast(string color) => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "heart" };
        var inContent = new AvonicIcon { IconName = "heart" };
        var end = new AvonicIcon { IconName = "star" };
        using var scene = new Scene(new AvonicItemDivider { Color = color, StartContent = start, Content = inContent, EndContent = end });

        Assert.All(new[] { start, inContent, end }, icon => Assert.Equal(Colors.White, Solid(icon.Foreground)));
        Assert.All(new[] { start, inContent, end }, icon => Assert.Equal(1, icon.Foreground!.Opacity));
    });

    [AvaloniaFact]
    public void D5_In_An_Unknown_Colour_Icons_Inherit() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "heart" };
        var parent = new StackPanel { Children = { new AvonicItemDivider { Color = "nonexistent", StartContent = icon } } };
        TextElement.SetForeground(parent, new SolidColorBrush(Token));
        using var scene = new Scene(parent);

        Assert.Equal(Token, Solid(icon.Foreground));
    });

    [AvaloniaFact]
    public void D11_Right_To_Left_Mirrors_The_Slots() => InstalledTheme.Run(_ =>
    {
        // Measured d-rtl: the start icon at 350, the end one at 16.
        var start = new AvonicIcon { IconName = "heart" };
        var end = new AvonicIcon { IconName = "star" };
        var divider = new AvonicItemDivider { FlowDirection = FlowDirection.RightToLeft, StartContent = start, EndContent = end };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        Assert.Equal(350, Box(start, scene.Window).Left);
        Assert.Equal(16, Box(end, scene.Window).Left);
    });

    // ── Labels and text ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void D6_A_Label_Has_13_Above_And_10_Below() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var divider = new AvonicItemDivider { Content = label };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        Assert.Equal(new Thickness(0, 13, 0, 10), label.Margin);
        Assert.Equal(14, label.FontSize);
        Assert.Equal(label.Bounds.Height + 13 + 10 + 1, divider.Bounds.Height);
    });

    [AvaloniaFact]
    public void D6_A_Label_In_A_Named_Slot_Adds_That_Slots_Margin() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicLabel { Content = "x" };
        var end = new AvonicLabel { Content = "x" };
        using var scene = new Scene(new AvonicItemDivider { StartContent = start, EndContent = end });

        Assert.Equal(new Thickness(0, 13, 16, 10), start.Margin);
        Assert.Equal(new Thickness(16, 13, 0, 10), end.Margin);
    });

    [AvaloniaFact]
    public void D6_A_Panel_Stands_For_The_Slot_But_Deeper_Gets_Nothing() => InstalledTheme.Run(_ =>
    {
        var inPanel = new AvonicLabel { Content = "x" };
        var deeper = new AvonicLabel { Content = "x" };
        using var scene = new Scene(new AvonicItemDivider
        {
            Content = new StackPanel { Children = { inPanel, new Border { Child = deeper } } },
        });

        Assert.Equal(new Thickness(0, 13, 0, 10), inPanel.Margin);
        Assert.Equal(new Thickness(0), deeper.Margin);
    });

    [AvaloniaFact]
    public void D7_A_Label_Inherits_Unless_Its_Token_Or_Colour_Says_Otherwise() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicLabel { Content = "x" };
        var coloredDivider = new AvonicLabel { Content = "x" };
        var danger = new AvonicLabel { Content = "x", Color = "danger" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicItemDivider { Content = plain },
                new AvonicItemDivider { Color = "primary", Content = coloredDivider },
                new AvonicItemDivider { Content = danger },
            },
        });

        Assert.Equal(Grey, Solid(TextElement.GetForeground(plain)));
        Assert.Equal(Colors.White, Solid(TextElement.GetForeground(coloredDivider)));
        Assert.Equal(Danger, Solid(TextElement.GetForeground(danger)));

        // Measured d-label-var: a divider has the class "item", so this is read. Its own colour still wins.
        scene.Window.Resources["Avonic.Label.Color"] = new SolidColorBrush(Token);
        Run();
        Assert.Equal(Token, Solid(TextElement.GetForeground(plain)));
        Assert.Equal(Danger, Solid(TextElement.GetForeground(danger)));
    });

    [AvaloniaFact]
    public void D7_A_Label_Uses_The_Font_Token() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        using var scene = new Scene(new AvonicItemDivider { Content = label });
        scene.Window.Resources["Avonic.FontFamily"] = new FontFamily("Courier New");
        Run();

        Assert.Equal("Courier New", label.FontFamily.Name);
    });

    [AvaloniaFact]
    public void D7_A_Nowrap_Label_Gets_An_Ellipsis_And_Clips() => InstalledTheme.Run(_ =>
    {
        // Measured d-nw: 358 wide, one line, ellipsis, hidden.
        var label = new AvonicLabel { Content = Long, Classes = { "ion-text-nowrap" } };
        var divider = new AvonicItemDivider { Content = label };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        var text = label.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(TextWrapping.NoWrap, text.TextWrapping);
        Assert.Equal(TextTrimming.CharacterEllipsis, text.TextTrimming);
        Assert.True(label.ClipToBounds);
        Assert.Equal(358, label.Bounds.Width);
    });

    [AvaloniaFact]
    public void D8_A_Paragraph_Is_Grey_At_14_With_2_Below_Even_In_A_Colour() => InstalledTheme.Run(_ =>
    {
        var paragraph = new TextBlock { Text = "x", Classes = { "p" } };
        var inColor = new TextBlock { Text = "x", Classes = { "p" } };
        var start = new TextBlock { Text = "x", Classes = { "p" } };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicItemDivider { Content = paragraph },
                new AvonicItemDivider { Color = "primary", Content = inColor },
                new AvonicItemDivider { StartContent = start },
            },
        });

        Assert.Equal(new Thickness(0, 0, 0, 2), paragraph.Margin);
        Assert.Equal(14, paragraph.FontSize);
        Assert.Equal(ParagraphGrey, Solid(paragraph.Foreground));
        Assert.Equal(ParagraphGrey, Solid(inColor.Foreground));
        Assert.Equal(ParagraphGrey, Solid(start.Foreground));
    });

    [AvaloniaFact]
    public void D8_A_Paragraph_In_A_Named_Slot_Keeps_That_Slots_Margin() => InstalledTheme.Run(_ =>
    {
        var start = new TextBlock { Text = "x", Classes = { "p" } };
        var end = new TextBlock { Text = "x", Classes = { "p" } };
        using var scene = new Scene(new AvonicItemDivider { StartContent = start, EndContent = end });

        Assert.Equal(new Thickness(0, 0, 16, 2), start.Margin);
        Assert.Equal(new Thickness(16, 0, 0, 2), end.Margin);
    });

    [AvaloniaFact]
    public void D8_A_Panel_Stands_For_The_Slot_But_Deeper_Gets_Nothing_And_Other_Text_Nothing() => InstalledTheme.Run(_ =>
    {
        var inPanel = new TextBlock { Text = "x", Classes = { "p" } };
        var deeper = new TextBlock { Text = "x", Classes = { "p" } };
        var plain = new TextBlock { Text = "x" };
        using var scene = new Scene(new AvonicItemDivider
        {
            Content = new StackPanel { Children = { inPanel, plain, new Border { Child = deeper } } },
        });

        Assert.Equal(ParagraphGrey, Solid(inPanel.Foreground));
        Assert.Equal(new Thickness(0, 0, 0, 2), inPanel.Margin);
        Assert.Equal(new Thickness(0), deeper.Margin);
        Assert.Equal(Grey, Solid(TextElement.GetForeground(deeper)));
        Assert.Equal(new Thickness(0), plain.Margin);
    });

    [AvaloniaFact]
    public void D9_Long_Text_Wraps_And_Grows_The_Divider() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider { Content = Long };
        using var scene = new Scene(new StackPanel { Children = { divider } });

        var text = divider.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(358, text.Bounds.Width);
        Assert.Equal(text.Bounds.Height + 1, divider.Bounds.Height);
        Assert.True(divider.Bounds.Height > 31);
    });

    [AvaloniaFact]
    public void D10_It_Clips_What_Overflows_It() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider();
        using var scene = new Scene(divider);

        Assert.True(divider.ClipToBounds);
        Assert.True(Part<Border>(divider, "PART_Inner").ClipToBounds);
    });

    [AvaloniaFact]
    public void D10_The_Default_Slot_Is_Clipped_Before_The_End_Slot() => InstalledTheme.Run(_ =>
    {
        // .item-divider-wrapper { overflow: hidden }: it ends at 334 with an end icon, measured.
        var divider = new AvonicItemDivider
        {
            Content = new Border { Width = 600, Height = 20, Background = Brushes.Red },
            EndContent = new AvonicIcon { IconName = "star" },
        };
        using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { divider } });

        var frame = scene.Window.CaptureRenderedFrame()!;
        Assert.NotEqual(Colors.White, Frames.PixelAt(frame, 330, 24));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 340, 24));
    });

    // ── Sticky ───────────────────────────────────────────────────────────────────────────────

    private static AvonicItem[] RowItems(int count) =>
        Enumerable.Range(0, count).Select(_ => new AvonicItem { Content = "r" }).ToArray();

    private static AvonicList Flat(AvonicItemDivider first, AvonicItemDivider second)
    {
        var list = new AvonicList();
        list.Items.Add(first);
        foreach (var item in RowItems(6))
        {
            list.Items.Add(item);
        }

        list.Items.Add(second);
        foreach (var item in RowItems(6))
        {
            list.Items.Add(item);
        }

        return list;
    }

    private static AvonicItemGroup Group(AvonicItemDivider divider)
    {
        var group = new AvonicItemGroup();
        group.Items.Add(divider);
        foreach (var item in RowItems(6))
        {
            group.Items.Add(item);
        }

        return group;
    }

    /// <summary>TestApp has no base theme, and a ScrollViewer's template comes from one.</summary>
    private static void Scrolling(Action test)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ => test());
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    private static double ScrollTo(ScrollViewer scroller, double y, Visual divider)
    {
        scroller.Offset = new Vector(0, y);
        Run();
        return Top(divider, scroller);
    }

    private static double Top(Visual visual, ScrollViewer scroller) =>
        visual.TranslatePoint(default, scroller)!.Value.Y;

    [AvaloniaFact]
    public void D12_In_A_Flat_List_Each_Sticks_And_The_Later_One_Covers_It() => Scrolling(() =>
    {
        var first = new AvonicItemDivider { Sticky = true, Content = "1" };
        var second = new AvonicItemDivider { Sticky = true, Content = "2" };
        var scroller = new ScrollViewer { Content = Flat(first, second) };
        using var scene = new Scene(scroller, 390, 200);

        // Measured s-flat, offset by its own numbers: 8 and 327 at the top, then stuck.
        Assert.Equal(8, Top(first, scroller));
        Assert.Equal(327, Top(second, scroller));

        Assert.Equal(0, ScrollTo(scroller, 50, first));
        Assert.Equal(277, Top(second, scroller));

        Assert.Equal(0, ScrollTo(scroller, 300, first));
        Assert.Equal(27, Top(second, scroller));

        Assert.Equal(0, ScrollTo(scroller, 330, first));
        Assert.Equal(0, Top(second, scroller));

        Assert.Equal(0, ScrollTo(scroller, 454, first));
        Assert.Equal(0, Top(second, scroller));

        var frame = (first.ZIndex, second.ZIndex);
        Assert.Equal((100, 100), frame);
    });

    [AvaloniaFact]
    public void D12_It_Moves_The_Drawing_Not_The_Layout() => Scrolling(() =>
    {
        var first = new AvonicItemDivider { Sticky = true };
        var second = new AvonicItemDivider { Sticky = true };
        var scroller = new ScrollViewer { Content = Flat(first, second) };
        using var scene = new Scene(scroller, 390, 200);
        var extent = scroller.Extent;
        var bounds = first.Bounds;

        scroller.Offset = new Vector(0, 300);
        Run();

        Assert.Equal(extent, scroller.Extent);
        Assert.Equal(bounds, first.Bounds);
    });

    [AvaloniaFact]
    public void D13_In_A_Group_It_Is_Pushed_Out_When_The_Group_Ends() => Scrolling(() =>
    {
        var first = new AvonicItemDivider { Sticky = true };
        var second = new AvonicItemDivider { Sticky = true };
        var scroller = new ScrollViewer { Content = new AvonicList { Items = { Group(first), Group(second) } } };
        using var scene = new Scene(scroller, 390, 200);

        // Measured s-group.
        Assert.Equal(8, Top(first, scroller));
        Assert.Equal(0, ScrollTo(scroller, 100, first));
        Assert.Equal(0, ScrollTo(scroller, 280, first));
        Assert.Equal(47, Top(second, scroller));
        Assert.Equal(0, ScrollTo(scroller, 296, first));
        Assert.Equal(-4, ScrollTo(scroller, 300, first));
        Assert.Equal(27, Top(second, scroller));
        Assert.Equal(-14, ScrollTo(scroller, 310, first));
        Assert.Equal(17, Top(second, scroller));
        Assert.Equal(-34, ScrollTo(scroller, 330, first));
        Assert.Equal(0, Top(second, scroller));
        Assert.Equal(-104, ScrollTo(scroller, 400, first));
        Assert.Equal(0, Top(second, scroller));
    });

    [AvaloniaFact]
    public void D13_Lower_In_A_Group_It_Still_Ends_With_The_Group() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider { Sticky = true };
        var group = new AvonicItemGroup { Items = { new AvonicItem { Content = "r" }, divider } };
        foreach (var item in RowItems(6))
        {
            group.Items.Add(item);
        }

        var scroller = new ScrollViewer { Content = new StackPanel { Children = { group, new Border { Height = 1000 } } } };
        using var scene = new Scene(scroller, 390, 200);

        Assert.Equal(48, Top(divider, scroller));
        Assert.Equal(0, ScrollTo(scroller, 100, divider));

        // The group is 48 + 31 + 288 = 367 high.
        Assert.Equal(-14, ScrollTo(scroller, 350, divider));
    });

    [AvaloniaFact]
    public void D13_Its_Margin_Is_Kept_At_The_End_But_Not_At_The_Top() => Scrolling(() =>
    {
        // Measured: margins of 8 and 6. Stuck at 0 at scroll 16, and -6 at 310 where the group's end is 25 + 6 away.
        var divider = new AvonicItemDivider { Sticky = true, Margin = new Thickness(0, 8, 0, 6) };
        var scroller = new ScrollViewer { Content = new AvonicList { Items = { Group(divider), Group(new AvonicItemDivider()) } } };
        using var scene = new Scene(scroller, 390, 200);

        Assert.Equal(16, Top(divider, scroller));
        Assert.Equal(0, ScrollTo(scroller, 16, divider));
        Assert.Equal(0, ScrollTo(scroller, 300, divider));
        Assert.Equal(-6, ScrollTo(scroller, 310, divider));
        Assert.Equal(-16, ScrollTo(scroller, 320, divider));
    });

    [AvaloniaFact]
    public void D13_From_A_Template_Its_Margin_Is_Counted_Inside_The_Presenter() => Scrolling(() =>
    {
        var list = new AvonicList
        {
            ItemsSource = new object[] { "d", 1, 2, 3, 4, 5, 6, 7, 8, 9 },
            ItemTemplate = new FuncDataTemplate<object>((value, _) =>
                value is string
                    ? new AvonicItemDivider { Sticky = true, Margin = new Thickness(0, 8, 0, 0) }
                    : new AvonicItem { Content = "r" }),
        };
        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 200);
        var divider = list.GetVisualDescendants().OfType<AvonicItemDivider>().Single();

        Assert.Equal(0, ScrollTo(scroller, 100, divider));
    });

    [AvaloniaFact]
    public void D12_It_Sticks_Inside_The_Scrollers_Padding() => Scrolling(() =>
    {
        // Measured: a scroller with padding-top: 30 sticks at 30.
        var divider = new AvonicItemDivider { Sticky = true };
        var list = new AvonicList { Items = { divider } };
        foreach (var item in RowItems(20))
        {
            list.Items.Add(item);
        }

        var scroller = new ScrollViewer { Padding = new Thickness(0, 30, 0, 0), Content = list };
        using var scene = new Scene(scroller, 390, 200);

        Assert.Equal(30, ScrollTo(scroller, 300, divider));
    });

    [AvaloniaFact]
    public void D13_It_Never_Goes_Above_Where_It_Would_Be() => Scrolling(() =>
    {
        // A group shorter than the divider has no room to move in.
        var divider = new AvonicItemDivider { Sticky = true };
        var group = new AvonicItemGroup { Items = { divider } };
        var scroller = new ScrollViewer
        {
            Content = new StackPanel { Children = { new Border { Height = 50 }, group, new Border { Height = 1000 } } },
        };
        using var scene = new Scene(scroller, 390, 200);

        Assert.Equal(50, Top(divider, scroller));
        Assert.Equal(-50, ScrollTo(scroller, 100, divider));
    });

    [AvaloniaFact]
    public void D14_In_An_Inset_List_It_Does_Not_Stick() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider { Sticky = true };
        var list = new AvonicList { Inset = true, Items = { divider } };
        foreach (var item in RowItems(12))
        {
            list.Items.Add(item);
        }

        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 200);

        // Measured s-inset: 24 at the top, then it moves with the page.
        Assert.Equal(24, Top(divider, scroller));
        Assert.Equal(-26, ScrollTo(scroller, 50, divider));
        Assert.Equal(-76, ScrollTo(scroller, 100, divider));

        // A plain list again, and it sticks.
        list.Inset = false;
        Run();
        Assert.Equal(0, ScrollTo(scroller, 60, divider));
    });

    [AvaloniaFact]
    public void D14_Moving_Out_Of_An_Inset_List_Lets_It_Stick() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider { Sticky = true };
        var inset = new AvonicList { Inset = true, Items = { divider } };
        var plain = new AvonicList();
        foreach (var item in RowItems(12))
        {
            plain.Items.Add(item);
        }

        var scroller = new ScrollViewer { Content = new StackPanel { Children = { inset, plain } } };
        using var scene = new Scene(scroller, 390, 200);

        inset.Items.Remove(divider);
        Run();
        plain.Items.Insert(0, divider);
        Run();

        Assert.Equal(0, ScrollTo(scroller, 100, divider));
    });

    [AvaloniaFact]
    public void D16_Moving_Out_Of_A_Scroller_Ends_It() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider { Sticky = true };
        var list = new AvonicList { Items = { divider } };
        foreach (var item in RowItems(12))
        {
            list.Items.Add(item);
        }

        var scroller = new ScrollViewer { Content = list };
        var outside = new StackPanel();
        using var scene = new Scene(new Grid { RowDefinitions = new("Auto,200"), Children = { outside, scroller } });
        Grid.SetRow(scroller, 1);
        Run();
        Assert.Equal(0, ScrollTo(scroller, 100, divider));

        list.Items.Remove(divider);
        Run();
        outside.Children.Add(divider);
        Run();

        Assert.Equal(0, divider.TranslatePoint(default, scene.Window)!.Value.Y);
        Assert.Equal(0, Assert.IsType<TranslateTransform>(divider.RenderTransform).Y);
    });

    [AvaloniaFact]
    public void D14_An_Inset_List_Outside_The_Scroller_Does_Not_Stop_It() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider { Sticky = true };
        var rows = new StackPanel { Children = { divider } };
        foreach (var item in RowItems(12))
        {
            rows.Children.Add(item);
        }

        var scroller = new ScrollViewer { Height = 200, Content = rows };
        using var scene = new Scene(new AvonicList { Inset = true, Items = { scroller } }, 390, 400);

        Assert.Equal(0, ScrollTo(scroller, 100, divider));
    });

    [AvaloniaFact]
    public void D15_By_Default_It_Scrolls_Away() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider();
        var list = new AvonicList { Items = { divider } };
        foreach (var item in RowItems(12))
        {
            list.Items.Add(item);
        }

        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 200);

        Assert.Equal(-42, ScrollTo(scroller, 50, divider));
        Assert.Equal(0, divider.ZIndex);
    });

    [AvaloniaFact]
    public void D15_Sticky_Changes_Live() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider();
        var list = new AvonicList { Items = { divider } };
        foreach (var item in RowItems(12))
        {
            list.Items.Add(item);
        }

        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 200);
        Assert.Equal(-42, ScrollTo(scroller, 50, divider));

        divider.Sticky = true;
        Run();
        Assert.Equal(0, Top(divider, scroller));
        Assert.Equal(100, divider.ZIndex);

        divider.Sticky = false;
        Run();
        Assert.Equal(-42, Top(divider, scroller));
        Assert.Equal(0, divider.ZIndex);
        Assert.Null(divider.RenderTransform);
    });

    [AvaloniaFact]
    public void D15_It_Stops_Listening_When_It_Leaves_And_Starts_Again_When_It_Returns() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider { Sticky = true };
        var list = new AvonicList { Items = { divider } };
        foreach (var item in RowItems(12))
        {
            list.Items.Add(item);
        }

        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 200);
        Assert.Equal(0, ScrollTo(scroller, 50, divider));

        list.Items.Remove(divider);
        Run();
        Assert.Equal(0, divider.ZIndex);

        list.Items.Insert(0, divider);
        Run();
        Assert.Equal(100, divider.ZIndex);
        Assert.Equal(0, ScrollTo(scroller, 60, divider));
    });

    [AvaloniaFact]
    public void D16_Without_Anything_Scrolling_It_Stays_Put() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider { Sticky = true };
        var panel = new StackPanel { Children = { new Border { Height = 50 }, divider } };
        using var scene = new Scene(panel);

        Assert.Equal(50, divider.TranslatePoint(default, scene.Window)!.Value.Y);
    });

    [AvaloniaFact]
    public void D17_A_Stuck_Divider_Takes_The_Input_At_The_Top() => Scrolling(() =>
    {
        // Measured: elementFromPoint at the top of the scroller finds the divider.
        var divider = new AvonicItemDivider { Sticky = true };
        var list = new AvonicList { Items = { divider } };
        foreach (var item in RowItems(12))
        {
            list.Items.Add(item);
        }

        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 200);
        ScrollTo(scroller, 100, divider);
        scene.Window.CaptureRenderedFrame();

        var pressed = false;
        divider.PointerPressed += (_, _) => pressed = true;
        scene.Window.MouseDown(new Point(100, 5), MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(new Point(100, 5), MouseButton.Left, RawInputModifiers.None);

        Assert.True(pressed);
    });

    [AvaloniaFact]
    public void D18_A_Stuck_Divider_Is_Drawn_Above_The_Rows_After_It() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider { Sticky = true, Color = "danger", Content = "x" };
        var list = new AvonicList { Items = { divider } };
        foreach (var item in RowItems(12))
        {
            list.Items.Add(item);
        }

        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 200);
        scene.Window.Background = Brushes.White;
        ScrollTo(scroller, 100, divider);

        // The rows are white, and the divider is red, at any point of its top line.
        Assert.NotEqual(Colors.White, Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, 300, 10));
    });

    private static AvonicList FromTemplate() => new()
    {
        ItemsSource = new object[] { "d1", 1, 2, 3, 4, 5, 6, "d2", 7, 8, 9, 10, 11, 12 },
        ItemTemplate = new FuncDataTemplate<object>((value, _) =>
            value is string
                ? new AvonicItemDivider { Sticky = true, Color = "danger", Content = value }
                : new AvonicItem { Content = "r" }),
    };

    [AvaloniaFact]
    public void D18_From_A_Template_It_Sticks_In_The_List_Not_Its_Presenter() => Scrolling(() =>
    {
        var list = FromTemplate();
        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 200);
        var divider = list.GetVisualDescendants().OfType<AvonicItemDivider>().First();
        scene.Window.Background = Brushes.White;

        Assert.Equal(0, ScrollTo(scroller, 100, divider));
        Assert.Equal(100, Assert.IsType<ContentPresenter>(divider.GetVisualParent()).ZIndex);
        Assert.NotEqual(Colors.White, Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, 300, 10));
    });

    [AvaloniaFact]
    public void D18_Known_Gap_In_A_Flat_Virtualising_Panel_It_Goes_With_Its_Row() => Scrolling(() =>
    {
        // Ionic would keep it stuck. A virtualising panel recycles the row, so a sticky divider there needs a group.
        var list = FromTemplate();
        list.ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());
        list.ItemsSource = new object[] { "d1" }.Concat(Enumerable.Range(0, 1000).Cast<object>()).ToList();
        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 200);
        Assert.Single(list.GetVisualDescendants().OfType<AvonicItemDivider>());

        scroller.Offset = new Vector(0, 5000);
        Run();

        Assert.DoesNotContain(list.GetVisualDescendants().OfType<AvonicItemDivider>(), divider => divider.IsVisible);
    });

    [AvaloniaFact]
    public void D18_A_ScrollViewers_Own_Presenter_Is_Left_Alone() => Scrolling(() =>
    {
        // It's a ContentPresenter too, and raising it would draw it over the scrollbars.
        var divider = new AvonicItemDivider { Sticky = true };
        var scroller = new ScrollViewer { Content = divider };
        using var scene = new Scene(scroller, 390, 200);

        Assert.Equal(0, Assert.IsAssignableFrom<Visual>(divider.GetVisualParent()).ZIndex);
        Assert.Equal(100, divider.ZIndex);
    });

    [AvaloniaFact]
    public void D18_Removing_It_Leaves_Nothing_On_The_Presenter() => Scrolling(() =>
    {
        var divider = new AvonicItemDivider { Sticky = true };
        var presenter = new ContentPresenter { Content = divider };
        using var scene = new Scene(new StackPanel { Children = { presenter } });
        Assert.Equal(100, presenter.ZIndex);

        divider.Sticky = false;
        Run();
        Assert.Equal(0, presenter.ZIndex);
    });

    [AvaloniaFact]
    public void D19_An_Apps_Own_Transform_Wins() => Scrolling(() =>
    {
        var own = new RotateTransform(0);
        var divider = new AvonicItemDivider { Sticky = true, RenderTransform = own };
        using var scene = new Scene(new ScrollViewer { Content = new StackPanel { Children = { divider } } });

        Assert.Same(own, divider.RenderTransform);
    });
}
