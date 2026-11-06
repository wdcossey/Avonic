using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for each
/// <c>COMPARISON.md</c>. Each page has the same content as the command at the top of that file.
/// </summary>
public class AvonicListCaptureTests
{
    private const string Long = "Long text wraps and makes the row taller, because it does not fit on one line at this width at all";

    private static readonly IBrush PageBackground = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE));

    private static void Save(Scene scene, string name)
    {
        var frame = scene.Window.CaptureRenderedFrame()!;
        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, $"avonic-{name}.png"), PngBitmapEncoderOptions.Default);
        }
    }

    private static Scene Show(Control page, double height = 844)
    {
        var scene = new Scene(page, 390, height);
        scene.Window.Background = PageBackground;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        return scene;
    }

    private static AvonicIcon Icon(string name) => new() { IconName = name };

    [AvaloniaFact]
    public void Reference_Page_List() => InstalledTheme.Run(_ =>
    {
        var page = new StackPanel
        {
            Children =
            {
                new AvonicList
                {
                    Items =
                    {
                        new AvonicListHeader { Content = "Default lines" },
                        new AvonicItem { Content = "One" },
                        new AvonicItem { Content = "Two" },
                    },
                },
                new AvonicList
                {
                    Lines = ListLines.Full,
                    Items =
                    {
                        new AvonicItem { Content = "Lines full" },
                        new AvonicItem { Content = "Its own lines, none", Lines = ItemLines.None },
                    },
                },
                new AvonicList
                {
                    Lines = ListLines.None,
                    Items =
                    {
                        new AvonicItem { Content = "Lines none" },
                        new AvonicItem { Content = "Its own lines, inset", Lines = ItemLines.Inset },
                    },
                },
                new AvonicList
                {
                    Inset = true,
                    Items =
                    {
                        new AvonicItem { Content = "Inset, a button", Button = true, Detail = true },
                        new AvonicItem { Content = "Danger", Color = "danger" },
                        new AvonicItem { Content = "The last has no border" },
                    },
                },
                new AvonicList
                {
                    Inset = true,
                    Items = { new AvonicItem { Content = "An inset list after an inset list" } },
                },
            },
        };

        using var scene = Show(page);
        Save(scene, "list-page");

        // Measured on the reference: 157, 113, 112, then 16 + 160 + 16, then 64 + 16.
        Assert.Equal([157d, 113d, 112d, 160d, 64d], page.Children.Select(child => child.Bounds.Height));
    });

    [AvaloniaFact]
    public void Reference_Page_List_Header() => InstalledTheme.Run(_ =>
    {
        var page = new StackPanel
        {
            Children =
            {
                new AvonicListHeader { Content = "Default" },
                new AvonicListHeader { Content = "Lines full", Lines = ListHeaderLines.Full },
                new AvonicListHeader { Content = "Lines inset", Lines = ListHeaderLines.Inset },
                new AvonicListHeader { Content = "Lines none", Lines = ListHeaderLines.None },
                new AvonicListHeader { Content = "Primary", Color = "primary" },
                new AvonicListHeader { Content = "Danger, lines full", Color = "danger", Lines = ListHeaderLines.Full },
                new AvonicListHeader { Content = "Unknown colour", Color = "nonexistent" },
                new AvonicListHeader { Content = new AvonicLabel { Content = "A label" } },
                new AvonicListHeader { Content = Long },
                new AvonicListHeader { Content = "RTL, lines inset", Lines = ListHeaderLines.Inset, FlowDirection = FlowDirection.RightToLeft },
            },
        };

        using var scene = Show(page);
        Save(scene, "list-header-page");

        Assert.Equal([45d, 46d, 45d, 45d, 45d, 46d, 45d, 45d], page.Children.Take(8).Select(child => child.Bounds.Height));
    });

    [AvaloniaFact]
    public void Reference_Page_Item_Divider() => InstalledTheme.Run(_ =>
    {
        var page = new StackPanel
        {
            Children =
            {
                new AvonicItemDivider { Content = "Default" },
                new AvonicItemDivider { Content = "Primary", Color = "primary" },
                new AvonicItemDivider { Content = "Unknown colour", Color = "nonexistent" },
                new AvonicItemDivider { Content = new AvonicLabel { Content = "A label" } },
                new AvonicItemDivider { Content = new AvonicLabel { Content = "A label in tertiary" }, Color = "tertiary" },
                new AvonicItemDivider { StartContent = Icon("heart"), Content = new AvonicLabel { Content = "Icons" }, EndContent = Icon("star") },
                new AvonicItemDivider { StartContent = Icon("heart"), Content = new AvonicLabel { Content = "Icons in danger" }, EndContent = Icon("star"), Color = "danger" },
                new AvonicItemDivider { Content = new AvonicIcon { IconName = "heart", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left } },
                new AvonicItemDivider { Content = new TextBlock { Text = "A paragraph", Classes = { "p" } } },
                new AvonicItemDivider { Content = Long },
                new AvonicItemDivider
                {
                    StartContent = Icon("heart"),
                    Content = new AvonicLabel { Content = "RTL" },
                    EndContent = Icon("star"),
                    FlowDirection = FlowDirection.RightToLeft,
                },
            },
        };

        using var scene = Show(page);
        Save(scene, "item-divider-page");

        Assert.Equal(new[] { 31d, 31d, 31d }, page.Children.Take(3).Select(child => child.Bounds.Height));
        Assert.Equal(49, page.Children[5].Bounds.Height);
    });

    [AvaloniaFact]
    public void Reference_Page_Item_Group() => InstalledTheme.Run(_ =>
    {
        var page = new StackPanel
        {
            Children =
            {
                new AvonicList
                {
                    Items =
                    {
                        new AvonicItemGroup
                        {
                            Items =
                            {
                                new AvonicItemDivider { Content = "Group one" },
                                new AvonicItem { Content = "One" },
                                new AvonicItem { Content = "Two" },
                            },
                        },
                        new AvonicItemGroup
                        {
                            Items =
                            {
                                new AvonicItemDivider { Content = "Group two" },
                                new AvonicItem { Content = "Three" },
                            },
                        },
                    },
                },
                new AvonicItemGroup { Items = { new AvonicLabel { Content = "A label directly in a group" } } },
                new AvonicList
                {
                    Inset = true,
                    Items =
                    {
                        new AvonicItemGroup
                        {
                            Items =
                            {
                                new AvonicItemDivider { Content = "In an inset list" },
                                new AvonicItem { Content = "Four" },
                                new AvonicItem { Content = "The last has no border" },
                            },
                        },
                    },
                },
            },
        };

        using var scene = Show(page);
        Save(scene, "item-group-page");

        Assert.Equal(16 + (31 * 2) + (48 * 3), page.Children[0].Bounds.Height);
    });

    private static AvonicItemGroup StickyGroup(string title, string color, string row)
    {
        var group = new AvonicItemGroup { Items = { new AvonicItemDivider { Sticky = true, Color = color, Content = title } } };
        for (var i = 1; i <= 6; i++)
        {
            group.Items.Add(new AvonicItem { Content = $"{row} {i}" });
        }

        return group;
    }

    [AvaloniaTheory]
    [InlineData(100)]
    [InlineData(300)]
    public void Sticky_Frame(int offset)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var scroller = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    Content = new AvonicList
                    {
                        Items = { StickyGroup("Group one", "primary", "One"), StickyGroup("Group two", "danger", "Two") },
                    },
                };

                using var scene = Show(scroller, 300);
                scroller.Offset = new Vector(0, offset);
                Dispatcher.UIThread.RunJobs();
                Save(scene, $"list-sticky-{offset}");

                Assert.Equal(offset, scroller.Offset.Y);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }
}
