using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The look, with the theme, and a real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for
/// <c>COMPARISON.md</c>. The boxes are Chromium's, in docs/components/ion-infinite-scroll/ref/measurements.json (m8_boxes).
/// </summary>
public class AvonicInfiniteScrollCaptureTests
{
    private static readonly IBrush Grey = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE));

    /// <summary>
    /// The same scene as the --html at the top of docs/components/ion-infinite-scroll/COMPARISON.md. There each is loading
    /// through the class; here through a scroll, with a threshold that any scroll is inside.
    /// </summary>
    private sealed class Page : IDisposable
    {
        public TestClock Clock { get; } = new();
        public List<AvonicInfiniteScroll> Scrolls { get; } = [];
        public List<Border> Wrappers { get; } = [];
        public Scene Scene { get; }

        public Page()
        {
            var rows = new StackPanel();
            void Add(AvonicInfiniteScrollContent content, bool loading = true, bool disabled = false)
            {
                var infinite = new AvonicInfiniteScroll { Content = content, IsEnabled = !disabled };
                if (loading)
                {
                    infinite.Threshold = "100000px";
                }

                var wrapper = new Border { Background = Grey, Margin = new Thickness(0, 0, 0, 8), Child = infinite };
                Scrolls.Add(infinite);
                Wrappers.Add(wrapper);
                rows.Children.Add(wrapper);
            }

            Add(new AvonicInfiniteScrollContent());
            Add(new AvonicInfiniteScrollContent { LoadingText = "Loading more data…" });
            Add(new AvonicInfiniteScrollContent
            {
                LoadingSpinner = SpinnerName.Dots,
                LoadingText = "A longer loading text, which wraps onto a second line and is centred",
            });
            Add(new AvonicInfiniteScrollContent { LoadingSpinner = SpinnerName.LinesSharp, LoadingText = "" });
            Add(new AvonicInfiniteScrollContent { LoadingText = "Not loading: only the min-height" }, loading: false);
            Add(new AvonicInfiniteScrollContent { LoadingText = "Disabled: display none" }, disabled: true);
            Wrappers[^1].Margin = default;
            // Room to scroll, below the frame
            rows.Children.Add(new Border { Height = 900 });

            var content = new AvonicContent { Content = rows };
            Scene = new Scene(content, 390, 844);
            Scene.Window.FontSize = 16;
            Dispatcher.UIThread.RunJobs();

            var scroll = content.GetScrollElement()!;
            scroll.Offset = new Vector(0, 1);
            Clock.At(16);
            scroll.Offset = default;
            Clock.At(32);
            Dispatcher.UIThread.RunJobs();
        }

        public Rect Box(Visual visual) => new(visual.TranslatePoint(default, Scene.Window)!.Value, visual.Bounds.Size);

        public static T Part<T>(Control host, string name) where T : Control =>
            host.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    [AvaloniaFact]
    public void B17_The_Content_Has_Mds_Sizes_Centred_And_Its_Colours() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        var contents = page.Scrolls.Select(s => (AvonicInfiniteScrollContent)s.Content!).ToList();

        // No text: the 32 px spinner line, centred in 84 with its 32 px margin below: at 10
        Assert.Equal(new Rect(0, 0, 390, 84), page.Box(page.Scrolls[0]));
        Assert.Equal(new Rect(0, 10, 390, 32), page.Box(Page.Part<Control>(contents[0], "PART_Loading")).WithHeight(32));
        Assert.Equal(new Rect(181, 10, 28, 28), page.Box(Page.Part<Control>(contents[0], "PART_LoadingSpinner")));

        // A line of text: 32 + 4 + 19 + 32, the text 32 in from each side
        Assert.Equal(92, page.Box(page.Scrolls[1]).Y);
        Assert.Equal(87, page.Box(page.Scrolls[1]).Height);
        var text = page.Box(Page.Part<Control>(contents[1], "PART_LoadingText"));
        Assert.Equal(32, text.X);
        Assert.Equal(326, text.Width);
        Assert.Equal(92 + 36, text.Y);

        // Two lines
        Assert.Equal(106, page.Box(page.Scrolls[2]).Height);

        // An empty text: a box of no height, still 4 below the spinner, so 36 centred in 84
        var empty = page.Box(Page.Part<Control>(contents[3], "PART_Loading"));
        Assert.Equal(84, page.Box(page.Scrolls[3]).Height);
        Assert.Equal(36, empty.Height, 0.01);
        Assert.Equal(page.Box(page.Scrolls[3]).Y + 8, empty.Y, 0.01);

        // Not loading: only the minimum height; disabled: none at all
        Assert.Equal(84, page.Box(page.Scrolls[4]).Height);
        Assert.False(Page.Part<Control>(contents[4], "PART_Loading").IsVisible);
        Assert.Equal(0, page.Wrappers[5].Bounds.Height);

        // The text in --ion-text-color-step-400; the spinner in the inherited colour, as md's rule for it can't reach it
        Assert.Equal(Color.Parse("#666666"), ((ISolidColorBrush)contents[1].GetVisualDescendants().OfType<TextBlock>().Single().Foreground!).Color);
        contents[0].Foreground = Brushes.Red;
        Assert.Equal(Colors.Red, ((ISolidColorBrush)Page.Part<AvonicSpinner>(contents[0], "PART_LoadingSpinner").Foreground!).Color);
    });

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        page.Scene.Window.Background = Brushes.White;
        page.Scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = page.Scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, "avonic-ion-infinite-scroll-page.png");

        // The text's grey, inside a glyph of "Loading more data…"; the grey of the box around it
        Assert.Equal(Color.FromRgb(0xEE, 0xEE, 0xEE), Frames.PixelAt(frame, 20, 200));
    });
}
