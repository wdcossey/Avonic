using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-card-header</c>, <c>-title</c>, <c>-subtitle</c> and <c>-content</c>. The numbers are from real Ionic in
/// Chromium, at 390 px wide, and the behaviour numbers are from docs/components/ion-card/PLAN.md.
/// </summary>
public class AvonicCardFamilyTests
{
    private static readonly Color Text150 = Color.Parse("#262626");
    private static readonly Color Text450 = Color.Parse("#737373");
    private static readonly Color Danger = Color.Parse("#C5000F");
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Light = Color.Parse("#F4F5F8");

    private static Color Solid(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Rect Box(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void AssertBox(double x, double y, double width, double height, Rect actual)
    {
        Assert.InRange(actual.X, x - 0.5, x + 0.5);
        Assert.InRange(actual.Y, y - 0.5, y + 0.5);
        Assert.InRange(actual.Width, width - 0.5, width + 0.5);
        Assert.InRange(actual.Height, height - 0.5, height + 0.5);
    }

    private static Scene Show(params Control[] controls)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(controls);
        return new Scene(panel);
    }

    private static TextBlock Heading(string name, double? fontSize = null)
    {
        var block = new TextBlock { Text = "x", Classes = { name } };
        if (fontSize is { } size)
        {
            block.FontSize = size;
        }

        return block;
    }

    private static AvonicCardHeader HeaderWith(params Control[] children)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(children);
        return new AvonicCardHeader { Content = panel };
    }

    private static (int Left, int Right) InkRange(WriteableBitmap frame, int top, int bottom)
    {
        unsafe
        {
            using var buffer = frame.Lock();
            var left = int.MaxValue;
            var right = int.MinValue;
            for (var y = top; y < bottom; y++)
            {
                for (var x = 0; x < buffer.Size.Width; x++)
                {
                    var pixel = (byte*)buffer.Address + y * buffer.RowBytes + x * 4;
                    if (pixel[0] < 128 && pixel[1] < 128 && pixel[2] < 128)
                    {
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                    }
                }
            }

            return (left, right);
        }
    }

    // ── Behaviour 15: the header's box ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_Header_Padding_Is_16_And_It_Fills_The_Width() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle { Content = "x" };
        var header = new AvonicCardHeader { Content = title };
        using var scene = Show(new AvonicCard { Content = header });

        AssertBox(10, 10, 370, 56, Box(header, scene.Window));
        AssertBox(26, 26, 338, 24, Box(title, scene.Window));
    });

    [AvaloniaFact]
    public void B15_Header_Inherits_Text_In_A_Card() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "x" };
        var header = new AvonicCardHeader { Content = text };
        var card = new AvonicCard { Content = header };
        card.Resources["Avonic.Card.Color"] = Brushes.Lime;
        using var scene = Show(card);

        Assert.Equal(Colors.Lime, Solid(header.Foreground));
        Assert.Equal(14, header.FontSize);
        Assert.Equal(14, text.FontSize);
    });

    [AvaloniaFact]
    public void B15_Header_Alone_Inherits_Text() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicCardHeader { Content = "x" };
        var parent = new Border { Child = header };
        TextElement.SetForeground(parent, Brushes.Teal);
        TextElement.SetFontSize(parent, 30);
        using var scene = new Scene(parent);

        Assert.Equal(Colors.Teal, Solid(header.Foreground));
        Assert.Equal(30, header.FontSize);
        Assert.Equal(new Thickness(16), header.Padding);
    });

    [AvaloniaFact]
    public void B15_Header_Is_Transparent_And_Takes_Input()
    {
        var presses = 0;
        var header = new AvonicCardHeader { Content = "x", Height = 60, VerticalAlignment = VerticalAlignment.Top };
        header.PointerPressed += (_, _) => presses++;
        using var scene = new Scene(header);

        Assert.Equal(Colors.Transparent, Solid(header.Background));
        scene.Window.MouseDown(new Point(380, 30), MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(new Point(380, 30), MouseButton.Left, RawInputModifiers.None);
        Assert.Equal(1, presses);
    }

    // ── Behaviour 16: the header's colour ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B16_Header_Color_Paints_Base_And_Contrast() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicCardHeader { Color = "primary" };
        var content = new AvonicCardContent();
        using var scene = Show(new AvonicCard { Content = new StackPanel { Children = { header, content } } });

        Assert.Equal(Primary, Solid(header.Background));
        Assert.Equal(Colors.White, Solid(header.Foreground));
        Assert.Equal(Text450, Solid(content.Foreground));
    });

    [AvaloniaFact]
    public void B16_Unknown_Header_Color_Inherits() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicCardHeader { Color = "nonexistent" };
        using var scene = Show(new AvonicCard { Content = header });

        Assert.Null(header.Background);
        Assert.Equal(Text450, Solid(header.Foreground));
    });

    [AvaloniaFact]
    public void B16_Unknown_Header_Color_Shows_An_Ancestors_Base() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicCardHeader { Color = "nonexistent" };
        using var scene = Show(new AvonicCard { Color = "primary", Content = header });

        Assert.Equal(Primary, Solid(header.Background));
        Assert.Equal(Colors.White, Solid(header.Foreground));
    });

    // ── Behaviour 17: the 8 above a title or subtitle ────────────────────────────────────────

    [AvaloniaFact]
    public void B17_Only_The_First_Has_No_Margin() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle { Content = "x" };
        var subtitle = new AvonicCardSubtitle { Content = "x" };
        var title2 = new AvonicCardTitle { Content = "x" };
        var subtitle2 = new AvonicCardSubtitle { Content = "x" };
        using var scene = Show(
            new AvonicCard { Content = HeaderWith(title, subtitle) },
            new AvonicCard { Content = HeaderWith(subtitle2, title2) });

        Assert.Equal(default, title.Margin);
        Assert.Equal(new Thickness(0, 8, 0, 0), subtitle.Margin);
        Assert.Equal(default, subtitle2.Margin);
        Assert.Equal(new Thickness(0, 8, 0, 0), title2.Margin);
    });

    [AvaloniaFact]
    public void B17_Whatever_Came_First_Counts() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle { Content = "x" };
        using var scene = Show(new AvonicCard { Content = HeaderWith(new Border(), title) });

        Assert.Equal(new Thickness(0, 8, 0, 0), title.Margin);
    });

    [AvaloniaFact]
    public void B17_It_Follows_A_Change() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicCardSubtitle { Content = "x" };
        var title = new AvonicCardTitle { Content = "x" };
        var header = HeaderWith(first, title);
        using var scene = Show(new AvonicCard { Content = header });
        Assert.Equal(new Thickness(0, 8, 0, 0), title.Margin);

        ((StackPanel)header.Content!).Children.Remove(first);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(default, title.Margin);

        ((StackPanel)header.Content!).Children.Insert(0, first);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(0, 8, 0, 0), title.Margin);
    });

    [AvaloniaFact]
    public void B17_Nested_Deeper_Or_Outside_A_Header_There_Is_None() => InstalledTheme.Run(_ =>
    {
        var nested = new AvonicCardTitle { Content = "x" };
        var outside = new AvonicCardTitle { Content = "x" };
        using var scene = Show(
            HeaderWith(new Border { Child = new StackPanel { Children = { new Border(), nested } } }),
            new StackPanel { Children = { new Border(), outside } });

        Assert.Equal(default, nested.Margin);
        Assert.Equal(default, outside.Margin);
    });

    // ── Behaviours 18-20: title and subtitle ─────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_Title_Typography() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "x" };
        var title = new AvonicCardTitle { Content = text };
        using var scene = Show(new AvonicCard { Content = title });

        Assert.Equal(20, title.FontSize);
        Assert.Equal(FontWeight.Medium, title.FontWeight);
        Assert.Equal(FontWeight.Medium, text.FontWeight);
        Assert.Equal(Text150, Solid(title.Foreground));
        Assert.Equal(24, text.LineHeight, 3);
        AssertBox(10, 10, 370, 24, Box(title, scene.Window));
    });

    [AvaloniaFact]
    public void B18_Title_Line_Height_Follows_Its_Own_Size() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle { Content = "x", FontSize = 40 };
        using var scene = Show(title);

        // Measured e-t: 48 for 40 px.
        Assert.Equal(48, title.Bounds.Height, 0);
    });

    [AvaloniaFact]
    public void B18_Title_Does_Not_Clip_Descenders() => InstalledTheme.Run(_ =>
    {
        // A line height far below the font's height, so anything clipped is obvious.
        var title = new AvonicCardTitle { Content = "ggg", FontSize = 40, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Top };
        var clipped = new TextBlock { Text = "ggg", LineHeight = 4 };
        using var scene = new Scene(new Panel { Children = { title } });
        scene.Window.Background = Brushes.White;
        var block = title.GetVisualDescendants().OfType<TextBlock>().Single();
        block.LineHeight = 4;
        Dispatcher.UIThread.RunJobs();

        Assert.False(block.ClipToBounds);
        Assert.True(clipped.ClipToBounds);
        var frame = scene.Window.CaptureRenderedFrame()!;
        var box = Box(block, scene.Window);
        var (left, _) = InkRange(frame, (int)box.Bottom + 1, (int)scene.Window.Bounds.Height);

        Assert.NotEqual(int.MaxValue, left);
    });

    [AvaloniaFact]
    public void B18_Title_In_A_Panel_Does_Not_Clip_But_Deeper_Keeps_The_Default() => InstalledTheme.Run(_ =>
    {
        var inPanel = new TextBlock { Text = "x" };
        var deeper = new TextBlock { Text = "x" };
        using var scene = Show(
            new AvonicCardTitle { Content = new Grid { Children = { inPanel } } },
            new AvonicCardTitle { Content = new Border { Child = deeper } });

        Assert.False(inPanel.ClipToBounds);
        Assert.True(deeper.ClipToBounds);
    });

    [AvaloniaFact]
    public void B19_Subtitle_Typography() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "x" };
        var subtitle = new AvonicCardSubtitle { Content = text };
        using var scene = Show(new AvonicCard { Content = subtitle });

        Assert.Equal(14, subtitle.FontSize);
        Assert.Equal(FontWeight.Medium, subtitle.FontWeight);
        Assert.Equal(Text450, Solid(subtitle.Foreground));
        Assert.True(double.IsNaN(text.LineHeight));
    });

    [AvaloniaFact]
    public void B19_Subtitle_In_Content_Inherits_Its_Line_Height() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "x" };
        using var scene = Show(new AvonicCardContent { Content = new AvonicCardSubtitle { Content = text } });

        // Measured f-s: 21.
        Assert.Equal(21, text.LineHeight, 3);
    });

    [AvaloniaFact]
    public void B20_Tokens_Set_The_Text() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle();
        title.Resources["Avonic.CardTitle.Color"] = Brushes.Lime;
        var subtitle = new AvonicCardSubtitle();
        subtitle.Resources["Avonic.CardSubtitle.Color"] = Brushes.Teal;
        using var scene = Show(title, subtitle);

        Assert.Equal(Colors.Lime, Solid(title.Foreground));
        Assert.Equal(Colors.Teal, Solid(subtitle.Foreground));
    });

    [AvaloniaFact]
    public void B20_Defaults_Follow_The_Palette() => InstalledTheme.Run(_ =>
    {
        var app = Application.Current!;
        app.Resources["Avonic.TextColorStep150"] = Colors.Lime;
        app.Resources["Avonic.TextColorStep450"] = Colors.Teal;
        try
        {
            var title = new AvonicCardTitle();
            var subtitle = new AvonicCardSubtitle();
            using var scene = Show(title, subtitle);

            Assert.Equal(Colors.Lime, Solid(title.Foreground));
            Assert.Equal(Colors.Teal, Solid(subtitle.Foreground));
        }
        finally
        {
            app.Resources.Remove("Avonic.TextColorStep150");
            app.Resources.Remove("Avonic.TextColorStep450");
        }
    });

    // ── Behaviours 21, 22: their colours ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B21_Color_Paints_Base() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle { Color = "danger" };
        var subtitle = new AvonicCardSubtitle { Color = "primary" };
        title.Resources["Avonic.CardTitle.Color"] = Brushes.Lime;
        subtitle.Resources["Avonic.CardSubtitle.Color"] = Brushes.Lime;
        using var scene = Show(new AvonicCard { Content = HeaderWith(title, subtitle) });

        Assert.Equal(Danger, Solid(title.Foreground));
        Assert.Equal(Primary, Solid(subtitle.Foreground));
    });

    [AvaloniaFact]
    public void B21_An_Unknown_Color_Inherits() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle { Color = "nonexistent" };
        var subtitle = new AvonicCardSubtitle { Color = "nonexistent" };
        using var scene = Show(new AvonicCard { Content = HeaderWith(title, subtitle) });

        Assert.Equal(Text450, Solid(title.Foreground));
        Assert.Equal(Text450, Solid(subtitle.Foreground));
    });

    [AvaloniaFact]
    public void B22_In_A_Coloured_Card_They_Inherit_Contrast() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicCardHeader();
        var title = new AvonicCardTitle();
        var subtitle = new AvonicCardSubtitle();
        header.Content = new StackPanel { Children = { title, subtitle } };
        using var scene = Show(new AvonicCard { Color = "danger", Content = header });

        Assert.Equal(Colors.White, Solid(header.Foreground));
        Assert.Equal(Colors.White, Solid(title.Foreground));
        Assert.Equal(Colors.White, Solid(subtitle.Foreground));
    });

    [AvaloniaFact]
    public void B22_It_Works_At_Any_Depth() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle();
        var subtitle = new AvonicCardSubtitle();
        using var scene = Show(new AvonicCard
        {
            Color = "danger",
            Content = new Border { Child = new Border { Child = new StackPanel { Children = { title, subtitle } } } },
        });

        Assert.Equal(Colors.White, Solid(title.Foreground));
        Assert.Equal(Colors.White, Solid(subtitle.Foreground));
    });

    [AvaloniaFact]
    public void B22_In_A_Coloured_Header_They_Inherit_Contrast() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle();
        var subtitle = new AvonicCardSubtitle();
        var header = HeaderWith(title, subtitle);
        header.Color = "warning";
        using var scene = Show(new AvonicCard { Content = header });

        Assert.Equal(Colors.Black, Solid(title.Foreground));
        Assert.Equal(Colors.Black, Solid(subtitle.Foreground));
    });

    [AvaloniaFact]
    public void B22_It_Beats_Their_Own_Color() => InstalledTheme.Run(_ =>
    {
        var cardTitle = new AvonicCardTitle { Color = "primary" };
        var cardSubtitle = new AvonicCardSubtitle { Color = "primary" };
        var headerTitle = new AvonicCardTitle { Color = "primary" };
        var headerSubtitle = new AvonicCardSubtitle { Color = "primary" };
        var header = HeaderWith(headerTitle, headerSubtitle);
        header.Color = "warning";
        using var scene = Show(
            new AvonicCard { Color = "success", Content = new StackPanel { Children = { cardTitle, cardSubtitle } } },
            new AvonicCard { Content = header });

        // success and warning both have a black contrast, where primary's base is blue.
        Assert.Equal(Colors.Black, Solid(cardTitle.Foreground));
        Assert.Equal(Colors.Black, Solid(cardSubtitle.Foreground));
        Assert.Equal(Colors.Black, Solid(headerTitle.Foreground));
        Assert.Equal(Colors.Black, Solid(headerSubtitle.Foreground));
    });

    [AvaloniaFact]
    public void B22_A_Coloured_Header_In_A_Coloured_Card_Keeps_Its_Background_And_Inherits_Text() => InstalledTheme.Run(_ =>
    {
        // Measured a-h: light's background, with danger's white text, where light's contrast is black.
        var title = new AvonicCardTitle();
        var header = new AvonicCardHeader { Color = "light", Content = title };
        using var scene = Show(new AvonicCard { Color = "danger", Content = header });

        Assert.Equal(Light, Solid(header.Background));
        Assert.Equal(Colors.White, Solid(header.Foreground));
        Assert.Equal(Colors.White, Solid(title.Foreground));
    });

    [AvaloniaFact]
    public void B22_A_Coloured_Header_In_A_Coloured_Header_Inherits_Text() => InstalledTheme.Run(_ =>
    {
        // Measured o-i: danger's background, with light's black text, where danger's contrast is white.
        var inner = new AvonicCardHeader { Color = "danger" };
        using var scene = Show(new AvonicCardHeader { Color = "light", Content = inner });

        Assert.Equal(Danger, Solid(inner.Background));
        Assert.Equal(Colors.Black, Solid(inner.Foreground));
    });

    [AvaloniaFact]
    public void B22_An_Unknown_Card_Colour_Also_Makes_Them_Inherit() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle();
        var parent = new Border { Child = new AvonicCard { Color = "nonexistent", Content = title } };
        TextElement.SetForeground(parent, Brushes.Teal);
        using var scene = new Scene(parent);

        Assert.Equal(Colors.Teal, Solid(title.Foreground));
    });

    [AvaloniaFact]
    public void B22_A_Coloured_Item_Does_Not_Do_This() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle();
        using var scene = Show(new AvonicItem { Color = "primary", Content = title });

        // Measured ii-t.
        Assert.Equal(Text150, Solid(title.Foreground));
    });

    [AvaloniaFact]
    public void B22_A_Plain_Card_And_Header_Do_Not_Do_This() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle();
        var subtitle = new AvonicCardSubtitle();
        using var scene = Show(new AvonicCard { Content = HeaderWith(title, subtitle) });

        Assert.Equal(Text150, Solid(title.Foreground));
        Assert.Equal(Text450, Solid(subtitle.Foreground));
    });

    // ── Behaviour 23: automation ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B23_Title_Is_A_Heading_At_Level_2()
    {
        var title = new AvonicCardTitle { Content = "Hello" };
        using var scene = new Scene(title);

        var peer = ControlAutomationPeer.CreatePeerForElement(title);

        Assert.Equal(AutomationControlType.Text, peer.GetAutomationControlType());
        Assert.Equal(2, peer.GetHeadingLevel());
        Assert.Equal("Hello", peer.GetName());
        Assert.True(peer.IsContentElement());
        Assert.True(peer.IsControlElement());
    }

    [AvaloniaFact]
    public void B23_Subtitle_Is_A_Heading_At_Level_3()
    {
        var subtitle = new AvonicCardSubtitle { Content = "Hello" };
        using var scene = new Scene(subtitle);

        var peer = ControlAutomationPeer.CreatePeerForElement(subtitle);

        Assert.Equal(AutomationControlType.Text, peer.GetAutomationControlType());
        Assert.Equal(3, peer.GetHeadingLevel());
        Assert.Equal("Hello", peer.GetName());
    }

    [AvaloniaFact]
    public void B23_An_Apps_Name_And_Level_Win()
    {
        var title = new AvonicCardTitle { Content = "Hello" };
        AutomationProperties.SetName(title, "Named");
        AutomationProperties.SetHeadingLevel(title, 4);
        using var scene = new Scene(title);

        var peer = ControlAutomationPeer.CreatePeerForElement(title);

        Assert.Equal("Named", peer.GetName());
        Assert.Equal(4, peer.GetHeadingLevel());
    }

    // ── Behaviour 24: the content's box ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B24_Content_Box_And_Text() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "x" };
        var content = new AvonicCardContent { Content = text };
        using var scene = Show(new AvonicCard { Content = content });

        // Measured co-c: 47, which is 13 + 21 + 13.
        AssertBox(10, 10, 370, 47, Box(content, scene.Window));
        AssertBox(26, 23, 338, 21, Box(text, scene.Window));
        Assert.Equal(14, text.FontSize);
    });

    [AvaloniaFact]
    public void B24_Alone_It_Has_The_Same_Box_And_Inherits_Colour() => InstalledTheme.Run(_ =>
    {
        var content = new AvonicCardContent { Content = "x" };
        var parent = new Border { Child = content };
        TextElement.SetForeground(parent, Brushes.Teal);
        TextElement.SetFontSize(parent, 30);
        using var scene = new Scene(parent);

        Assert.Equal(new Thickness(16, 13, 16, 13), content.Padding);
        Assert.Equal(14, content.FontSize);
        Assert.Equal(Colors.Teal, Solid(content.Foreground));
    });

    [AvaloniaFact]
    public void B24_Line_Height_Follows_Its_Own_Size() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "x" };
        var content = new AvonicCardContent { Content = text, FontSize = 20 };
        using var scene = Show(content);

        Assert.Equal(30, text.LineHeight, 3);
    });

    [AvaloniaFact]
    public void B24_In_A_Coloured_Card_It_Inherits_Contrast() => InstalledTheme.Run(_ =>
    {
        var content = new AvonicCardContent { Content = "x" };
        using var scene = Show(new AvonicCard { Color = "danger", Content = content });

        Assert.Equal(Colors.White, Solid(content.Foreground));
    });

    [AvaloniaFact]
    public void B24_Text_Wraps_On_The_Stub_As_Well() => Assert.All(
        new ContentControl[] { new AvonicCardHeader(), new AvonicCardTitle(), new AvonicCardSubtitle(), new AvonicCardContent() },
        control =>
        {
            control.Content = string.Concat(Enumerable.Repeat(" word", 200));
            using var scene = new Scene(new StackPanel { Children = { control } });

            Assert.True(control.Bounds.Height > 40, control.GetType().Name);
            Assert.False(control.ClipToBounds, control.GetType().Name);
        });

    [AvaloniaFact]
    public void B24_The_Whole_Box_Takes_Input_On_The_Stub() => Assert.All(
        new ContentControl[] { new AvonicCardTitle(), new AvonicCardSubtitle(), new AvonicCardContent() },
        control =>
        {
            var presses = 0;
            control.Content = new Border { Width = 10, Height = 10 };
            control.Height = 60;
            control.VerticalAlignment = VerticalAlignment.Top;
            control.PointerPressed += (_, _) => presses++;
            using var scene = new Scene(control);

            scene.Window.MouseDown(new Point(380, 30), MouseButton.Left, RawInputModifiers.None);
            scene.Window.MouseUp(new Point(380, 30), MouseButton.Left, RawInputModifiers.None);
            Assert.Equal(1, presses);
        });

    // ── Behaviour 25: after a header ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B25_Straight_After_A_Header_There_Is_No_Top_Padding() => InstalledTheme.Run(_ =>
    {
        var content = new AvonicCardContent { Content = "x" };
        using var scene = Show(new AvonicCard { Content = new StackPanel { Children = { new AvonicCardHeader(), content } } });

        Assert.Equal(new Thickness(16, 0, 16, 13), content.Padding);
    });

    [AvaloniaFact]
    public void B25_Outside_A_Card_Too() => InstalledTheme.Run(_ =>
    {
        var content = new AvonicCardContent { Content = "x" };
        using var scene = Show(new AvonicCardHeader(), content);

        Assert.Equal(new Thickness(16, 0, 16, 13), content.Padding);
    });

    [AvaloniaFact]
    public void B25_Anything_Between_Stops_It() => InstalledTheme.Run(_ =>
    {
        var gap = new AvonicCardContent { Content = "x" };
        var first = new AvonicCardContent { Content = "x" };
        var afterContent = new AvonicCardContent { Content = "x" };
        using var scene = Show(
            new StackPanel { Children = { new AvonicCardHeader(), new Border(), gap } },
            new StackPanel { Children = { first, afterContent } });

        Assert.Equal(new Thickness(16, 13, 16, 13), gap.Padding);
        Assert.Equal(new Thickness(16, 13, 16, 13), first.Padding);
        Assert.Equal(new Thickness(16, 13, 16, 13), afterContent.Padding);
    });

    [AvaloniaFact]
    public void B25_Before_A_Header_Does_Not_Count() => InstalledTheme.Run(_ =>
    {
        var content = new AvonicCardContent { Content = "x" };
        using var scene = Show(content, new AvonicCardHeader());

        Assert.Equal(new Thickness(16, 13, 16, 13), content.Padding);
    });

    [AvaloniaFact]
    public void B25_A_Hidden_Header_Still_Counts() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicCardHeader();
        var content = new AvonicCardContent { Content = "x" };
        using var scene = Show(header, content);

        header.IsVisible = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Thickness(16, 0, 16, 13), content.Padding);
    });

    [AvaloniaFact]
    public void B25_It_Follows_A_Change()
    {
        var content = new AvonicCardContent { Content = "x" };
        var gap = new Border();
        var header = new AvonicCardHeader();
        var panel = new StackPanel { Children = { header, gap, content } };
        using var scene = new Scene(panel);
        Assert.DoesNotContain(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);

        panel.Children.Remove(gap);
        Assert.Contains(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);

        panel.Children.Insert(1, gap);
        Assert.DoesNotContain(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);

        panel.Children.Move(2, 1);
        Assert.Contains(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);

        panel.Children.Move(0, 2);
        Assert.DoesNotContain(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);
    }

    [AvaloniaFact]
    public void B25_Moving_To_Another_Panel_Follows_That_One_And_Forgets_The_Last()
    {
        var content = new AvonicCardContent();
        var from = new StackPanel { Children = { new AvonicCardHeader(), content } };
        var to = new StackPanel();
        using var scene = new Scene(new StackPanel { Children = { from, to } });
        Assert.Contains(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);

        from.Children.Remove(content);
        to.Children.Add(content);
        Assert.DoesNotContain(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);

        // The old panel changing mustn't reach it any more.
        from.Children.Clear();
        from.Children.Add(new AvonicCardHeader());
        Assert.DoesNotContain(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);

        to.Children.Insert(0, new AvonicCardHeader());
        Assert.Contains(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);
    }

    [AvaloniaFact]
    public void B25_Detached_It_Stops_Listening()
    {
        var content = new AvonicCardContent();
        var header = new AvonicCardHeader();
        var panel = new StackPanel { Children = { content } };
        var host = new Border { Child = panel };
        using var scene = new Scene(host);

        host.Child = null;
        Dispatcher.UIThread.RunJobs();
        panel.Children.Insert(0, header);
        Assert.DoesNotContain(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);

        host.Child = panel;
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);
    }

    [AvaloniaFact]
    public void B25_Not_In_A_Panel_It_Is_Not_After_Anything()
    {
        var content = new AvonicCardContent();
        using var scene = new Scene(new Border { Child = content });

        Assert.DoesNotContain(AvonicCardContent.AfterCardHeaderPseudoClass, content.Classes);
    }

    // ── Behaviour 26: h1-h6 and p ────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData("h1", 24, 0, 2, 28.8)]
    [InlineData("h2", 16, 2, 2, 19.2)]
    [InlineData("h3", 14, 2, 2, 16.8)]
    [InlineData("h4", 14, 2, 2, 16.8)]
    [InlineData("h5", 14, 2, 2, 16.8)]
    [InlineData("h6", 14, 2, 2, 16.8)]
    [InlineData("p", 14, 0, 2, 21)]
    public void B26_Typography_Directly_And_In_A_Panel(string name, double size, double top, double bottom, double lineHeight) => InstalledTheme.Run(_ =>
    {
        var direct = Heading(name);
        var inPanel = Heading(name);
        using var scene = Show(
            new AvonicCardContent { Content = direct, FontWeight = FontWeight.Bold },
            new AvonicCardContent { Content = new StackPanel { Children = { inPanel } }, FontWeight = FontWeight.Bold });

        foreach (var block in new[] { direct, inPanel })
        {
            Assert.Equal(size, block.FontSize);
            Assert.Equal(new Thickness(0, top, 0, bottom), block.Margin);
            Assert.Equal(lineHeight, block.LineHeight, 3);
            Assert.Equal(FontWeight.Normal, block.FontWeight);
            Assert.False(block.ClipToBounds);
        }
    });

    [AvaloniaTheory]
    [InlineData("h1", 48)]
    [InlineData("h2", 48)]
    [InlineData("h3", 48)]
    [InlineData("h4", 48)]
    [InlineData("h5", 48)]
    [InlineData("h6", 48)]
    [InlineData("p", 60)]
    public void B26_Line_Height_Follows_Their_Own_Size(string name, double lineHeight) => InstalledTheme.Run(_ =>
    {
        var block = Heading(name, 40);
        using var scene = Show(new AvonicCardContent { Content = block });

        Assert.Equal(lineHeight, block.LineHeight, 3);
    });

    [AvaloniaTheory]
    [InlineData("h1")]
    [InlineData("h2")]
    [InlineData("h3")]
    [InlineData("p")]
    public void B26_Deeper_Or_Outside_Content_They_Have_No_Rules(string name) => InstalledTheme.Run(_ =>
    {
        var deeper = Heading(name);
        var outside = Heading(name);
        using var scene = Show(new AvonicCardContent { Content = new Border { Child = deeper } }, outside);

        Assert.Equal(14, deeper.FontSize);
        Assert.Equal(default, deeper.Margin);
        Assert.True(deeper.ClipToBounds);
        Assert.Equal(default, outside.Margin);
        Assert.True(double.IsNaN(outside.LineHeight));
        Assert.True(outside.ClipToBounds);
    });

    [AvaloniaFact]
    public void B26_They_Inherit_The_Text_Colour() => InstalledTheme.Run(_ =>
    {
        var plain = Heading("h1");
        var coloured = Heading("p");
        using var scene = Show(
            new AvonicCard { Content = new AvonicCardContent { Content = plain } },
            new AvonicCard { Color = "danger", Content = new AvonicCardContent { Content = coloured } });

        Assert.Equal(Text450, Solid(plain.Foreground));
        Assert.Equal(Colors.White, Solid(coloured.Foreground));
    });

    [AvaloniaFact]
    public void B26_A_Line_Height_Does_Not_Clip_Descenders() => InstalledTheme.Run(_ =>
    {
        var block = new TextBlock { Text = "ggg", Classes = { "p" }, LineHeight = 4, Foreground = Brushes.Black };
        var content = new AvonicCardContent { Content = block, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(new Panel { Children = { content } });
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();

        var frame = scene.Window.CaptureRenderedFrame()!;
        var box = Box(block, scene.Window);
        var (left, _) = InkRange(frame, (int)box.Bottom + 1, (int)box.Bottom + 30);

        Assert.NotEqual(int.MaxValue, left);
    });

    // ── Behaviour 27: right to left ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B27_RTL_Text_Starts_At_The_Right_And_The_Boxes_Stay_Symmetrical() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle { Content = "x", Foreground = Brushes.Black };
        var card = new AvonicCard
        {
            FlowDirection = FlowDirection.RightToLeft,
            Content = new AvonicCardHeader { Content = title },
        };
        using var scene = new Scene(new StackPanel { Children = { card } });
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();

        AssertBox(10, 10, 370, card.Bounds.Height, Box(card, scene.Window));
        AssertBox(26, 26, 338, 24, Box(title, scene.Window));

        var (left, right) = InkRange(scene.Window.CaptureRenderedFrame()!, 26, 50);
        Assert.InRange(right, 340, 364);
        Assert.InRange(left, 340, 364);
    });

    // ── Behaviour 28: all together ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B28_Header_Over_Content_With_No_Gap() => InstalledTheme.Run(_ =>
    {
        var title = new AvonicCardTitle { Content = "Title" };
        var subtitle = new AvonicCardSubtitle { Content = "Subtitle" };
        var header = HeaderWith(title, subtitle);
        var content = new AvonicCardContent { Content = "Content" };
        var card = new AvonicCard { Content = new StackPanel { Children = { header, content } } };
        using var scene = Show(card);

        var headerBox = Box(header, scene.Window);
        var contentBox = Box(content, scene.Window);

        AssertBox(10, 10, 370, headerBox.Height, headerBox);
        AssertBox(10, headerBox.Bottom, 370, 34, contentBox);
        AssertBox(26, 26, 338, 24, Box(title, scene.Window));
        Assert.Equal(58, Box(subtitle, scene.Window).Y, 0);
        Assert.Equal(card.Bounds.Height, headerBox.Height + contentBox.Height, 0);
    });

    [AvaloniaFact]
    public void B28_Spacing_Of_Minus_10_Gives_The_Gap_Of_Collapsed_Margins() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicCard { Height = 30 };
        var second = new AvonicCard { Height = 30 };
        using var scene = new Scene(new StackPanel { Spacing = -10, Children = { first, second } });

        // Measured s1, s2: 10 apart.
        Assert.Equal(10, Box(second, scene.Window).Y - Box(first, scene.Window).Bottom);
    });
}
