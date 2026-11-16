using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Sample;
using Avonic.Sample.Pages;

namespace Avonic.Components.Tests.Sample;

/// <summary>
/// The pictures in <c>README.md</c> and <c>PREVIEW.md</c> (#196): each screen of the showcase in light and dark, and a frame of
/// every sample page, at 390 × 844 and a scale of 2, with the theme installed over <c>SimpleTheme</c>, as the sample has it. With
/// <c>AVONIC_PREVIEW_DIR</c> set they're saved there, as <c>showcase-&lt;screen&gt;-&lt;light|dark&gt;.png</c> and
/// <c>pages/&lt;title&gt;.png</c>; without it, each still has to render at the phone's size. See <c>PREVIEW.md</c>.
/// </summary>
public class PreviewCaptureTests
{
    private const double Width = 390;
    private const double Height = 844;
    private const int Scale = 2;

    /// <summary>Longer than the theme's longest transition, 300 ms.</summary>
    private static readonly TimeSpan TransitionsEnd = TimeSpan.FromMilliseconds(400);

    public static TheoryData<string, bool> ShowcaseScreens()
    {
        var data = new TheoryData<string, bool>();
        foreach (var screen in ShowcasePage.Screens)
        {
            data.Add(screen, false);
            data.Add(screen, true);
        }

        return data;
    }

    /// <summary>
    /// Pages left out of the gallery: the showcase, captured screen by screen above, and the device page, which shows the machine
    /// it runs on.
    /// </summary>
    public static readonly IReadOnlySet<string> NotInGallery = new HashSet<string> { "showcase", "device" };

    public static TheoryData<string> Pages() =>
        new(SamplePageCatalog.Discover(typeof(SampleShell).Assembly).Select(entry => entry.Title).Where(title => !NotInGallery.Contains(title)));

    [AvaloniaTheory]
    [MemberData(nameof(ShowcaseScreens))]
    public void Showcase_Screen(string screen, bool dark) => Staged(dark, clock =>
    {
        var page = new ShowcasePage();
        var pictures = new Pictures(page);
        using var scene = new Scene(page, Width, Height);
        page.ScreenTabs.Select(screen); // False for the first, which is selected already.
        AvonicItemSliding? opened = null;
        if (screen == "inbox")
        {
            // A message swiped open, to show its options. The second, so the list still starts with a whole message.
            Settle(scene, clock, pictures);
            opened = page.GetVisualDescendants().OfType<AvonicItemSliding>().ElementAt(1);
            opened.Open(ItemOptionsSide.End);
        }

        var frame = Capture(scene, clock, pictures);

        Assert.True(opened is null || opened.GetOpenAmount() > 0, "The message didn't open");
        Assert.False(pictures.Loading, "A picture is still loading");

        var tab = Assert.Single(page.GetVisualDescendants().OfType<AvonicTab>(), t => t.Tab == screen && t.IsVisible);
        Assert.True(tab.Bounds.Height > 400, $"The {screen} screen is {tab.Bounds.Height} high");
        Assert.Equal(screen, page.ScreenTabs.GetSelected());
        AssertBackground(frame, dark);
        Save(frame, $"showcase-{screen}-{(dark ? "dark" : "light")}.png");
    });

    [AvaloniaTheory]
    [MemberData(nameof(Pages))]
    public void Gallery_Page(string title) => Staged(dark: false, clock =>
    {
        var entry = Assert.Single(SamplePageCatalog.Discover(typeof(SampleShell).Assembly), e => e.Title == title);
        var page = entry.Create();

        // A header of its own with the page's title, as the shell has, without the shell's Debug timing line.
        var header = new AvonicHeader { Items = { new AvonicToolbar { Content = new AvonicTitle { Content = title } } } };
        DockPanel.SetDock(header, Dock.Top);
        var root = new DockPanel { Children = { header, page } };
        var pictures = new Pictures(root);
        using var scene = new Scene(root, Width, Height);

        var frame = Capture(scene, clock, pictures);

        Assert.Equal(Width, page.Bounds.Width);
        Assert.True(page.Bounds.Height > 600, $"{title} is {page.Bounds.Height} high");
        Assert.Equal(new PixelSize((int)Width * Scale, (int)Height * Scale), frame.PixelSize);
        Save(frame, Path.Combine("pages", $"{title}.png"));
    });

    /// <summary>
    /// Runs <paramref name="test"/> with the sample's two themes, the dark palette if asked, and a test clock, so that what runs
    /// on Avonic's clock is at a known time in every frame.
    /// </summary>
    private static void Staged(bool dark, Action<TestClock> test)
    {
        var app = Application.Current!;
        var simple = new Avalonia.Themes.Simple.SimpleTheme();
        var palette = new ResourceInclude(new Uri("avares://Avonic.Themes/"))
        {
            Source = new Uri("avares://Avonic.Themes/Tokens/Palettes/Dark.axaml"),
        };
        app.Styles.Insert(0, simple);
        if (dark)
        {
            app.Resources.MergedDictionaries.Add(palette);
        }

        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                test(clock);
            });
        }
        finally
        {
            app.Resources.MergedDictionaries.Remove(palette);
            app.Styles.Remove(simple);
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>The window at the phone's size and scale, on the theme's background, with its pictures loaded and a second gone by.</summary>
    private static WriteableBitmap Capture(Scene scene, TestClock clock, Pictures pictures)
    {
        scene.Window.SetRenderScaling(Scale);
        Settle(scene, clock, pictures);
        return scene.Window.CaptureRenderedFrame()!;
    }

    /// <summary>
    /// A second of frames on the test clock, so that what Avonic animates on it (a sliding item opening, a spinner) is where it
    /// is a second in, in every run, then as long as it takes every picture that started to load to finish, up to 10 s. Avalonia's own
    /// transitions run on real time, so it's also at least <see cref="TransitionsEnd"/> of that: a value that arrives after a
    /// control's first style, such as a toolbar's colour, eases in.
    /// </summary>
    private static void Settle(Scene scene, TestClock clock, Pictures pictures)
    {
        scene.Window.Background = Application.Current!.FindResource("Avonic.BackgroundColorBrush") as IBrush;
        var settled = DateTime.UtcNow + TransitionsEnd;
        var until = DateTime.UtcNow.AddSeconds(10);
        for (var frame = 0; frame < 60 || DateTime.UtcNow < settled || (pictures.Loading && DateTime.UtcNow < until); frame++)
        {
            Dispatcher.UIThread.RunJobs();
            if (frame < 60)
            {
                clock.At(clock.Now.TotalMilliseconds + 16);
            }
            else
            {
                Thread.Sleep(10);
            }
        }

        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    /// <summary>
    /// Counts the pictures under a root that have started to load and those that have finished, from before it's shown, as each
    /// starts when it's laid out in view and decodes on the thread pool.
    /// </summary>
    private sealed class Pictures
    {
        private int _started;
        private int _ended;

        public Pictures(Control root)
        {
            root.AddHandler(AvonicImg.IonImgWillLoadEvent, (_, _) => _started++);
            root.AddHandler(AvonicImg.IonImgDidLoadEvent, (_, _) => _ended++);
            root.AddHandler(AvonicImg.IonErrorEvent, (_, _) => _ended++);
        }

        public bool Loading => _ended < _started;
    }

    /// <summary>
    /// The frame is the phone's at 2×, and the middle of its left edge, a list's or the content's background in every screen, is
    /// light in light and dark in dark: so the dark palette reached the theme.
    /// </summary>
    private static void AssertBackground(WriteableBitmap frame, bool dark)
    {
        Assert.Equal(new PixelSize((int)Width * Scale, (int)Height * Scale), frame.PixelSize);
        var corner = Frames.PixelAt(frame, 2, frame.PixelSize.Height / 2);
        Assert.Equal(255, corner.A);
        var light = (corner.R + corner.G + corner.B) / 3 > 128;
        Assert.True(light != dark, $"The middle of the left edge is {corner} in {(dark ? "dark" : "light")}");
    }

    /// <summary>Saves the frame in <c>AVONIC_PREVIEW_DIR</c>, if it's set: a relative path is from the repository's root.</summary>
    private static void Save(WriteableBitmap frame, string name)
    {
        if (Environment.GetEnvironmentVariable("AVONIC_PREVIEW_DIR") is { Length: > 0 } dir)
        {
            var path = Path.Combine(Path.IsPathRooted(dir) ? dir : Path.Combine(RepoRoot(), dir), name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            frame.Save(path, PngBitmapEncoderOptions.Default);
        }
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Avonic.sln")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Avonic.sln not found above " + AppContext.BaseDirectory);
    }
}
