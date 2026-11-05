using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>The numbers are from real ion-list-header in Chromium, at 390 px wide. See docs/components/ion-list-header/PLAN.md.</summary>
public class AvonicListHeaderTests
{
    private const string Long = "Very long text that certainly must overflow the phone viewport because it just goes on and on and on and on and on";

    private static readonly Thickness NoBorder = new(0);
    private static readonly Thickness Bottom = new(0, 0, 0, 1);
    private static readonly Color Primary = Color.FromRgb(0, 84, 233);
    private static readonly Color Danger = Color.FromRgb(197, 0, 15);
    private static readonly Color Token = Color.FromRgb(18, 52, 86);
    private static readonly Color BorderGrey = Color.FromArgb(0x21, 0, 0, 0);

    private static Border Inner(Control header) =>
        header.GetVisualDescendants().OfType<Border>().First(border => border.Name == "PART_Inner");

    private static Border Host(Control header) => header.GetVisualChildren().OfType<Border>().First();

    private static Color Solid(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Rect Box(Visual visual, Visual root)
    {
        var topLeft = visual.TranslatePoint(default, root)!.Value;
        return new Rect(topLeft, visual.Bounds.Size);
    }

    private static void Run() => Dispatcher.UIThread.RunJobs();

    private static unsafe uint Pixel(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        return ((uint*)(buffer.Address + y * buffer.RowBytes))[x] & 0xFFFFFF;
    }

    [AvaloniaFact]
    public void H1_It_Has_Its_Minimum_Height_Padding_And_Text_Size() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { header } });

        Assert.Equal(new Rect(0, 0, 390, 45), Box(header, scene.Window));
        Assert.Equal(new Rect(16, 0, 374, 45), Box(Inner(header), scene.Window));
        Assert.Equal(14, header.FontSize);
        Assert.Equal(14, header.GetVisualDescendants().OfType<TextBlock>().Single().FontSize);
    });

    [AvaloniaFact]
    public void H1_Its_Text_Is_Vertically_Centred_And_Black_On_Transparent() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { header } });

        var text = header.GetVisualDescendants().OfType<TextBlock>().Single();
        var box = Box(text, header);
        Assert.InRange(box.Top - (45 - box.Bottom), -1, 1);
        Assert.True(box.Top > 0);
        Assert.Equal(Colors.Black, Solid(TextElement.GetForeground(text)));
        Assert.Equal(Colors.Transparent, Solid(Host(header).Background));
    });

    [AvaloniaFact]
    public void H1_Its_Weight_Is_Inherited() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader();
        var parent = new StackPanel { Children = { header } };
        TextElement.SetFontWeight(parent, FontWeight.Bold);
        using var scene = new Scene(parent);

        Assert.Equal(FontWeight.Bold, header.FontWeight);
    });

    [AvaloniaFact]
    public void H1_Its_Text_Follows_The_Text_Colour_Token() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader();
        using var scene = new Scene(header);
        scene.Window.Resources["Avonic.TextColorBrush"] = new SolidColorBrush(Token);
        Run();

        Assert.Equal(Token, Solid(header.Foreground));
    });

    [AvaloniaFact]
    public void H1_The_Stub_Passes_On_What_A_Theme_Sets()
    {
        var header = new AvonicListHeader
        {
            Background = Brushes.Red,
            BorderBrush = Brushes.Blue,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            Content = 1,
            ContentTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock { Text = "t" }),
        };
        using var scene = new Scene(header);

        Assert.Same(Brushes.Red, Host(header).Background);
        Assert.Same(Brushes.Blue, Host(header).BorderBrush);
        Assert.Equal(new Thickness(1), Host(header).BorderThickness);
        Assert.Equal(new Thickness(4), Host(header).Padding);
        Assert.Equal("t", header.GetVisualDescendants().OfType<TextBlock>().Single().Text);
    }

    [AvaloniaFact]
    public void H1_It_Uses_The_Font_Token() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { Content = "x" };
        using var scene = new Scene(header);
        scene.Window.Resources["Avonic.FontFamily"] = new FontFamily("Courier New");
        Run();

        Assert.Equal("Courier New", header.FontFamily.Name);
    });

    [AvaloniaFact]
    public void H1_It_Works_On_The_Stub_Without_A_Theme()
    {
        var content = new TextBlock { Text = "x" };
        var header = new AvonicListHeader { Content = content };
        using var scene = new Scene(new StackPanel { Children = { header } });

        Assert.Same(header, content.GetLogicalParent());
        Assert.True(content.Bounds.Height > 0);
        Assert.NotNull(Inner(header));
    }

    [AvaloniaFact]
    public void H2_Without_Lines_There_Is_No_Border() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader();
        using var scene = new Scene(new StackPanel { Children = { header } });

        Assert.Equal(NoBorder, Host(header).BorderThickness);
        Assert.Equal(NoBorder, Inner(header).BorderThickness);
        Assert.Equal(45, header.Bounds.Height);
    });

    [AvaloniaFact]
    public void H2_Full_Draws_The_Outer_Border_And_Adds_To_The_Height() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { Lines = ListHeaderLines.Full };
        using var scene = new Scene(new StackPanel { Children = { header } });

        Assert.Equal(Bottom, Host(header).BorderThickness);
        Assert.Equal(NoBorder, Inner(header).BorderThickness);
        Assert.Equal(46, header.Bounds.Height);
        Assert.Equal(45, Inner(header).Bounds.Height);
    });

    [AvaloniaFact]
    public void H2_Inset_Draws_The_Inner_Border_Inside_The_Height() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { Lines = ListHeaderLines.Inset };
        using var scene = new Scene(new StackPanel { Children = { header } });

        Assert.Equal(NoBorder, Host(header).BorderThickness);
        Assert.Equal(Bottom, Inner(header).BorderThickness);
        Assert.Equal(45, header.Bounds.Height);
        Assert.Equal(new Rect(16, 0, 374, 45), Box(Inner(header), scene.Window));
    });

    [AvaloniaFact]
    public void H2_None_Beats_The_Tokens() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { Lines = ListHeaderLines.None };
        using var scene = new Scene(new StackPanel { Children = { header } });
        scene.Window.Resources["Avonic.ListHeader.BorderWidth"] = new Thickness(0, 0, 0, 3);
        scene.Window.Resources["Avonic.ListHeader.InnerBorderWidth"] = new Thickness(0, 0, 0, 3);
        Run();

        Assert.Equal(NoBorder, Host(header).BorderThickness);
        Assert.Equal(NoBorder, Inner(header).BorderThickness);
    });

    [AvaloniaFact]
    public void H2_Full_And_Inset_Beat_The_Other_Token() => InstalledTheme.Run(_ =>
    {
        var full = new AvonicListHeader { Lines = ListHeaderLines.Full };
        var inset = new AvonicListHeader { Lines = ListHeaderLines.Inset };
        using var scene = new Scene(new StackPanel { Children = { full, inset } });
        scene.Window.Resources["Avonic.ListHeader.BorderWidth"] = new Thickness(0, 0, 0, 3);
        scene.Window.Resources["Avonic.ListHeader.InnerBorderWidth"] = new Thickness(0, 0, 0, 3);
        Run();

        Assert.Equal(NoBorder, Inner(full).BorderThickness);
        Assert.Equal(NoBorder, Host(inset).BorderThickness);
    });

    [AvaloniaFact]
    public void H2_A_Lists_Lines_Do_Not_Reach_Its_Header() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader();
        using var scene = new Scene(new AvonicList { Lines = ListLines.Full, Items = { header } });

        Assert.Equal(NoBorder, Host(header).BorderThickness);
    });

    [AvaloniaFact]
    public void H2_Lines_Change_Live() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { Lines = ListHeaderLines.Full };
        using var scene = new Scene(new StackPanel { Children = { header } });

        header.Lines = null;
        Run();

        Assert.Equal(NoBorder, Host(header).BorderThickness);
        Assert.Equal(45, header.Bounds.Height);
    });

    [AvaloniaFact]
    public void H3_Both_Borders_Use_The_Border_Colour_Even_In_A_Colour() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicListHeader { Lines = ListHeaderLines.Full };
        var colored = new AvonicListHeader { Lines = ListHeaderLines.Full, Color = "primary" };
        var inset = new AvonicListHeader { Lines = ListHeaderLines.Inset, Color = "danger" };
        using var scene = new Scene(new StackPanel { Children = { plain, colored, inset } });

        Assert.Equal(BorderGrey, Solid(Host(plain).BorderBrush));
        Assert.Equal(BorderGrey, Solid(Host(colored).BorderBrush));
        Assert.Equal(BorderGrey, Solid(Inner(inset).BorderBrush));
    });

    [AvaloniaFact]
    public void H4_A_Colour_Paints_Base_And_Contrast() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { Color = "primary", Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { header } });

        Assert.Equal(Primary, Solid(Host(header).Background));
        Assert.Equal(Colors.White, Solid(TextElement.GetForeground(header.GetVisualDescendants().OfType<TextBlock>().Single())));
    });

    [AvaloniaFact]
    public void H4_The_Translucent_Border_Darkens_The_Background_Under_It() => InstalledTheme.Run(_ =>
    {
        // CSS's background reaches under the border: rgb(0, 84, 233) with 13% black over it.
        var header = new AvonicListHeader { Color = "primary", Lines = ListHeaderLines.Full };
        using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { header } });

        var pixel = Pixel(scene.Window.CaptureRenderedFrame()!, 100, 45);
        Assert.Equal(73, (int)(pixel >> 8) & 0xFF, 2d);
    });

    [AvaloniaFact]
    public void H4_An_Unknown_Colour_Is_Transparent_And_Inherits() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { Color = "nonexistent", Content = "x" };
        var parent = new StackPanel { Children = { header } };
        TextElement.SetForeground(parent, new SolidColorBrush(Token));
        using var scene = new Scene(parent);

        Assert.Null(Host(header).Background);
        Assert.Equal(Token, Solid(TextElement.GetForeground(header)));
    });

    [AvaloniaTheory]
    [InlineData("Avonic.ListHeader.Background")]
    [InlineData("Avonic.ListHeader.BorderColor")]
    [InlineData("Avonic.ListHeader.Color")]
    public void H5_Each_Brush_Token_Is_Read(string key) => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { Lines = ListHeaderLines.Full };
        var inset = new AvonicListHeader { Lines = ListHeaderLines.Inset };
        using var scene = new Scene(new StackPanel { Children = { header, inset } });
        scene.Window.Resources[key] = new SolidColorBrush(Token);
        Run();

        switch (key)
        {
            case "Avonic.ListHeader.Background":
                Assert.Equal(Token, Solid(Host(header).Background));
                break;
            case "Avonic.ListHeader.BorderColor":
                Assert.Equal(Token, Solid(Host(header).BorderBrush));
                Assert.Equal(Token, Solid(Inner(inset).BorderBrush));
                break;
            default:
                Assert.Equal(Token, Solid(header.Foreground));
                break;
        }
    });

    [AvaloniaFact]
    public void H5_The_Width_Tokens_Are_Read() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader();
        using var scene = new Scene(new StackPanel { Children = { header } });
        scene.Window.Resources["Avonic.ListHeader.BorderWidth"] = new Thickness(0, 0, 0, 2);
        scene.Window.Resources["Avonic.ListHeader.InnerBorderWidth"] = new Thickness(0, 0, 0, 3);
        Run();

        Assert.Equal(new Thickness(0, 0, 0, 2), Host(header).BorderThickness);
        Assert.Equal(new Thickness(0, 0, 0, 3), Inner(header).BorderThickness);
    });

    [AvaloniaFact]
    public void H6_A_Label_Fills_The_Row_With_No_Margin() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var header = new AvonicListHeader { Content = label };
        using var scene = new Scene(new StackPanel { Children = { header } });

        var box = Box(label, header);
        Assert.Equal(16, box.Left);
        Assert.Equal(374, box.Width);
        Assert.Equal(new Thickness(0), label.Margin);
        Assert.InRange(box.Top - (45 - box.Bottom), -1, 1);
        Assert.True(box.Top > 0);
        Assert.Equal(14, label.FontSize);
    });

    [AvaloniaFact]
    public void H6_A_Label_Inherits_The_Colour_And_Ignores_Its_Token_Outside_A_Group() => InstalledTheme.Run(_ =>
    {
        // Measured h-var: a header has no class "item", so label's color variable isn't read.
        var label = new AvonicLabel { Content = "x" };
        var colored = new AvonicLabel { Content = "x" };
        var danger = new AvonicLabel { Content = "x", Color = "danger" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicListHeader { Content = label },
                new AvonicListHeader { Color = "primary", Content = colored },
                new AvonicListHeader { Color = "primary", Content = danger },
            },
        });
        scene.Window.Resources["Avonic.Label.Color"] = new SolidColorBrush(Token);
        Run();

        Assert.Equal(Colors.Black, Solid(TextElement.GetForeground(label)));
        Assert.Equal(Colors.White, Solid(TextElement.GetForeground(colored)));
        Assert.Equal(Danger, Solid(TextElement.GetForeground(danger)));
    });

    [AvaloniaFact]
    public void H6_Without_An_Item_Around_It_A_Nowrap_Label_Clips_Without_An_Ellipsis() => InstalledTheme.Run(_ =>
    {
        // Measured h-nw: nowrap, but text-overflow is only set on :host-context(.item).
        var label = new AvonicLabel { Content = Long, Classes = { "ion-text-nowrap" } };
        using var scene = new Scene(new AvonicListHeader { Content = label }, 390, 100);

        var text = label.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(TextWrapping.NoWrap, text.TextWrapping);
        Assert.Equal(TextTrimming.None, text.TextTrimming);
    });

    [AvaloniaFact]
    public void H7_Long_Text_Wraps_Up_To_The_End_And_Grows_The_Header() => InstalledTheme.Run(_ =>
    {
        var longer = Long + " " + Long;
        var header = new AvonicListHeader { Content = longer };
        var oneLine = new AvonicListHeader { Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { header, oneLine } });

        var text = header.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(374, text.Bounds.Width);
        Assert.True(text.Bounds.Height > 2 * oneLine.GetVisualDescendants().OfType<TextBlock>().Single().Bounds.Height);
        Assert.Equal(text.Bounds.Height, header.Bounds.Height);
    });

    [AvaloniaFact]
    public void H8_It_Clips_What_Overflows_It() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader();
        using var scene = new Scene(header);

        Assert.True(header.ClipToBounds);
    });

    [AvaloniaFact]
    public void H9_Right_To_Left_Mirrors_The_Padding() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader { FlowDirection = FlowDirection.RightToLeft };
        using var scene = new Scene(new StackPanel { Children = { header } });

        // Measured: the inner box is 0 to 374.
        var inner = Inner(header);
        var left = inner.TranslatePoint(new Point(inner.Bounds.Width, 0), scene.Window)!.Value.X;
        Assert.Equal(0, left);
        Assert.Equal(374, inner.Bounds.Width);
    });

    [AvaloniaFact]
    public void H10_A_Label_And_A_Button_Share_The_Row_In_A_Grid() => InstalledTheme.Run(_ =>
    {
        // What flex: 1 1 auto does beside a sibling, in one slot. Measured hlb: label to the button's start.
        var label = new AvonicLabel { Content = "x" };
        var button = new Border { Width = 76 };
        Grid.SetColumn(button, 1);
        var header = new AvonicListHeader { Content = new Grid { ColumnDefinitions = new("*,Auto"), Children = { label, button } } };
        using var scene = new Scene(new StackPanel { Children = { header } });

        Assert.Equal(298, label.Bounds.Width);
        Assert.Equal(new Thickness(0), label.Margin);
    });

    [AvaloniaFact]
    public void H11_Its_Template_Has_The_Inner_Part() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicListHeader();
        using var scene = new Scene(header);

        Assert.Contains(header.GetVisualDescendants().OfType<Border>(), border => border.Name == "PART_Inner");
        Assert.Equal(VerticalAlignment.Stretch, Inner(header).VerticalAlignment);
    });
}
