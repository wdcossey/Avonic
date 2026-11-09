using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Themes.Simple;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Behaviours 1–15 and 17 from docs/components/ion-img/PLAN.md, read from img.js and measured in Chromium. The loader is
/// replaced by <see cref="FakeLoader"/>, which the test finishes itself, so nothing here needs the network.
/// </summary>
public class AvonicImgTests
{
    private static List<string> Record(Interactive target)
    {
        var events = new List<string>();
        target.AddHandler(AvonicImg.IonImgWillLoadEvent, (_, _) => events.Add("willLoad"));
        target.AddHandler(AvonicImg.IonImgDidLoadEvent, (_, _) => events.Add("didLoad"));
        target.AddHandler(AvonicImg.IonErrorEvent, (_, _) => events.Add("error"));
        return events;
    }

    /// <summary>A page 358 wide at 16 px, as Chromium's body, for the alt text.</summary>
    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(16) };
        TextElement.SetFontSize(page, 16);
        page.Children.AddRange(children);
        return page;
    }

    /// <summary>TestApp has no base theme, and a ScrollViewer's template comes from one.</summary>
    private static void Scrolling(Action test)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            Dispatcher.UIThread.RunJobs();
            test();
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    private static T Part<T>(AvonicImg img, string name) where T : Control =>
        img.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    private static void Layout(Scene scene)
    {
        Dispatcher.UIThread.RunJobs();
        scene.Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    // ── 1. In the viewport: willLoad, the load, didLoad ──────────────────────────────────────

    [AvaloniaFact]
    public void B1_In_View_It_Raises_WillLoad_Then_DidLoad_When_The_Picture_Is_Shown()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        var events = Record(img);

        // Not attached: nothing
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(loader.Requests);
        Assert.Empty(events);

        using var scene = new Scene(Page(img));
        Layout(scene);
        Assert.Equal(["willLoad"], events);
        Assert.Equal("a.png", Assert.Single(loader.Requests).Src);
        Assert.Null(img.Picture);

        loader.Finish(0);
        Assert.Equal(["willLoad", "didLoad"], events);
        Assert.Same(loader.Requests[0].Picture, img.Picture);
        Assert.Same(img.Picture, Part<Image>(img, "PART_Image").Source);
    }

    [AvaloniaFact]
    public void B1_A_Src_Given_To_A_Laid_Out_Image_Loads_With_Nothing_Else_Asking_For_A_Layout()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg();
        using var scene = new Scene(Page(img));
        Layout(scene);

        // Only the dispatcher's own work, as in an app where nothing else changes: the observer's first callback still comes
        img.Src = "a.png";
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Assert.Single(loader.Requests);
    }

    // ── 2. The viewport's edge, in a scroller, with no margin ────────────────────────────────

    [AvaloniaTheory]
    [InlineData(843, true)]
    [InlineData(844, true)]
    [InlineData(845, false)]
    public void B2_Below_The_Fold_Of_A_ScrollViewer_It_Loads_At_The_Edge(double top, bool loads) => Scrolling(() =>
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        var scroll = new ScrollViewer { Content = new StackPanel { Children = { new Border { Height = top }, img, new Border { Height = 2000 } } } };
        using var scene = new Scene(scroll, 390, 844);
        Layout(scene);
        Assert.Equal(loads, loader.Requests.Count == 1);

        if (!loads)
        {
            // Measured: a scroll of 1 px brings its top to the edge
            scroll.Offset = new Vector(0, top - 844);
            Layout(scene);
            Assert.Equal(top - 844, scroll.Offset.Y);
            Assert.Single(loader.Requests);
        }
    });

    [AvaloniaFact]
    public void B2_In_AvonicContent_It_Loads_When_Scrolled_Into_View_And_Not_Before()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png", Alt = "Alt" };
        var content = new AvonicContent { Content = new StackPanel { Children = { new Border { Height = 1200 }, img, new Border { Height = 2000 } } } };
        using var scene = new Scene(content, 390, 844);
        Layout(scene);
        Assert.Empty(loader.Requests);

        content.ScrollToPoint(null, 300);
        Layout(scene);
        Assert.Empty(loader.Requests);

        content.ScrollToPoint(null, 1200 - 844);
        Layout(scene);
        Assert.Single(loader.Requests);
    }

    // ── 3. Clipped by an ancestor ────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(150, false)]
    [InlineData(100, true)]
    public void B3_Outside_An_Ancestors_Clip_It_Does_Not_Load_And_On_Its_Edge_It_Does(double left, bool loads)
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png", Width = 50 };
        Canvas.SetLeft(img, left);
        var clip = new Canvas { Width = 100, Height = 20, ClipToBounds = true, Children = { img } };
        using var scene = new Scene(Page(clip));
        Layout(scene);
        Assert.Equal(loads, loader.Requests.Count == 1);
    }

    [AvaloniaFact]
    public void B3_A_Viewport_Clipped_Away_Entirely_Intersects_Nothing_Even_At_The_Origin()
    {
        // What Avalonia gives a control at the origin of an ancestor that's clipped out of view
        Assert.False(AvonicImg.Intersects(new Rect(0, 0, 0, 0), new Size(50, 20)));
        // A clip of no height in view, as Chromium's b, still has a width, and an edge counts
        Assert.True(AvonicImg.Intersects(new Rect(0, 0, 358, 0), new Size(358, 0)));
        Assert.True(AvonicImg.Intersects(new Rect(-100, 0, 100, 20), new Size(50, 20)));
        Assert.False(AvonicImg.Intersects(new Rect(-150, 0, 100, 20), new Size(50, 20)));
    }

    // ── 4. Hidden ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Under_A_Hidden_Ancestor_It_Does_Not_Load_And_Shown_It_Does()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        var wrap = new Panel { IsVisible = false, Children = { img } };
        using var scene = new Scene(Page(wrap));
        Layout(scene);
        Assert.Empty(loader.Requests);

        wrap.IsVisible = true;
        Layout(scene);
        Assert.Single(loader.Requests);
    }

    [AvaloniaFact]
    public void B4_Hidden_Itself_After_It_Was_Laid_Out_It_Does_Not_Load_And_Shown_It_Does()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg();
        using var scene = new Scene(Page(img));
        Layout(scene);

        // Hidden in a laid-out tree, and then given a src: the viewport doesn't move when it's shown again
        img.IsVisible = false;
        img.Src = "a.png";
        Layout(scene);
        Assert.Empty(loader.Requests);

        img.IsVisible = true;
        Layout(scene);
        Assert.Single(loader.Requests);
    }

    // ── 5. Detached before it loaded ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Removed_Before_It_Loaded_It_Loads_When_Attached_Again_In_View()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        var hidden = new Panel { IsVisible = false, Children = { img } };
        var page = Page(hidden);
        using var scene = new Scene(page);
        Layout(scene);
        Assert.Empty(loader.Requests);

        hidden.Children.Remove(img);
        Layout(scene);
        page.Children.Add(img);
        Layout(scene);
        Assert.Single(loader.Requests);
    }

    // ── 6. Once ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_It_Loads_Once_And_Then_Watches_Nothing() => Scrolling(() =>
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        var scroll = new ScrollViewer { Content = new StackPanel { Children = { img, new Border { Height = 3000 } } } };
        using var scene = new Scene(scroll, 390, 844);
        Layout(scene);
        Assert.False(img.IsWatching);
        loader.Finish(0);

        scroll.Offset = new Vector(0, 2000);
        Layout(scene);
        Assert.Equal(2000, scroll.Offset.Y);
        scroll.Offset = new Vector(0, 0);
        Layout(scene);
        Assert.Single(loader.Requests);
    });

    // ── 7. Size ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_Loaded_It_Is_As_Wide_As_Its_Parent_And_As_Tall_As_The_Pictures_Aspect_Ratio()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        using var scene = new Scene(Page(img));
        Layout(scene);
        loader.Finish(0, 300, 200);
        Layout(scene);

        // Measured: 358 × 238.66
        Assert.Equal(358, img.Bounds.Width);
        // Avalonia rounds layout to whole pixels at a scale of 1
        Assert.Equal(358 * 200 / 300.0, img.Bounds.Height, 0.5);
    }

    [AvaloniaFact]
    public void B7_In_A_Box_Of_Its_Own_Size_The_Picture_Is_Contained_And_Centred() => InstalledTheme.Run(_ =>
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png", Width = 200, Height = 200, HorizontalAlignment = HorizontalAlignment.Left };
        using var scene = new Scene(new Panel { Background = Brushes.White, Children = { img } }, 390, 300);
        Layout(scene);
        loader.Finish(0, 300, 200);
        Layout(scene);

        // object-fit: contain: 200 × 133.33, from y 33.33. Red top left, blue bottom left, white above and below
        var frame = scene.Window.CaptureRenderedFrame()!;
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 50, 30));
        Assert.Equal(TestPicture.Red, Frames.PixelAt(frame, 50, 37));
        Assert.Equal(TestPicture.Blue, Frames.PixelAt(frame, 50, 163));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 50, 170));
    });

    [AvaloniaFact]
    public void B7_In_A_Taller_Box_It_Is_At_The_Top_As_Tall_As_The_Picture()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        using var scene = new Scene(Page(new Panel { MinHeight = 400, Children = { img } }));
        Layout(scene);
        loader.Finish(0, 300, 200);
        Layout(scene);

        // A block with height auto, at the top of a div that something else makes taller
        Assert.Equal(0, img.Bounds.Y);
        Assert.Equal(238.67, img.Bounds.Height, 0.5);
    }

    // ── 8. Before and while loading ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_With_No_Picture_It_Shows_The_Alt_Text_And_With_No_Alt_It_Is_Empty() => InstalledTheme.Run(_ =>
    {
        using var loader = new FakeLoader();
        var alt = new AvonicImg { Alt = "No src" };
        var none = new AvonicImg();
        using var scene = new Scene(Page(alt, none));
        Layout(scene);

        // Measured: 19 high at 16 px Roboto, the text in the inherited font
        var text = Part<TextBlock>(alt, "PART_Alt");
        Assert.True(text.IsEffectivelyVisible);
        Assert.Equal("No src", text.Text);
        Assert.InRange(alt.Bounds.Height, 18, 19.5);
        Assert.Equal(358, alt.Bounds.Width);
        Assert.Equal(0, none.Bounds.Height);
        Assert.Empty(loader.Requests);
    });

    [AvaloniaFact]
    public void B8_While_A_First_Picture_Loads_Nothing_Shows_And_Then_The_Picture_Does() => InstalledTheme.Run(_ =>
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Alt = "Loading", Src = "a.png" };
        using var scene = new Scene(Page(img));
        Layout(scene);

        Assert.Single(loader.Requests);
        Assert.False(Part<TextBlock>(img, "PART_Alt").IsVisible);
        Assert.Equal(0, img.Bounds.Height);

        loader.Finish(0);
        Layout(scene);
        Assert.False(Part<TextBlock>(img, "PART_Alt").IsVisible);
        Assert.Equal(238.67, img.Bounds.Height, 0.5);
    });

    // ── 9. Failure ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_A_Failure_Raises_IonError_And_No_DidLoad_And_Shows_The_Alt_Text() => InstalledTheme.Run(_ =>
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Alt = "Missing picture", Src = "missing.png" };
        var events = Record(img);
        using var scene = new Scene(Page(img));
        Layout(scene);

        loader.Fail(0);
        Layout(scene);
        Assert.Equal(["willLoad", "error"], events);
        Assert.Null(img.Picture);
        Assert.True(Part<TextBlock>(img, "PART_Alt").IsVisible);
        Assert.InRange(img.Bounds.Height, 18, 19.5);
    });

    // ── 10. A change of src ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_A_Change_Of_Src_Loads_Again_Keeping_The_Old_Picture_Until_The_New_One_Shows()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        var events = Record(img);
        using var scene = new Scene(Page(img));
        Layout(scene);
        loader.Finish(0, 300, 200);
        Layout(scene);
        var first = img.Picture;

        img.Src = "b.png";
        Layout(scene);
        Assert.Equal(["willLoad", "didLoad", "willLoad"], events);
        Assert.Equal("b.png", loader.Requests[1].Src);
        Assert.Same(first, img.Picture);
        Assert.Equal(238.67, img.Bounds.Height, 0.5);

        loader.Finish(1, 200, 200);
        Layout(scene);
        Assert.Equal(["willLoad", "didLoad", "willLoad", "didLoad"], events);
        Assert.Same(loader.Requests[1].Picture, img.Picture);
        Assert.Equal(358, img.Bounds.Height);
    }

    [AvaloniaFact]
    public void B10_A_Change_To_A_Picture_That_Fails_Keeps_The_Old_One_Until_It_Fails_Then_Shows_The_Alt_Text() => InstalledTheme.Run(_ =>
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png", Alt = "Alt" };
        var events = Record(img);
        using var scene = new Scene(Page(img));
        Layout(scene);
        loader.Finish(0);
        var first = img.Picture;

        img.Src = "missing.png";
        Layout(scene);
        Assert.Same(first, img.Picture);
        Assert.False(Part<TextBlock>(img, "PART_Alt").IsVisible);

        loader.Fail(1);
        Layout(scene);
        Assert.Equal(["willLoad", "didLoad", "willLoad", "error"], events);
        Assert.Null(img.Picture);
        Assert.True(Part<TextBlock>(img, "PART_Alt").IsVisible);
        Assert.InRange(img.Bounds.Height, 18, 19.5);
    });

    // ── 11. Two changes in a row ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Of_Two_Changes_In_A_Row_Only_The_Last_Ones_Result_Shows_Or_Is_Raised()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        var events = Record(img);
        using var scene = new Scene(Page(img));
        Layout(scene);

        img.Src = "b.png";
        Layout(scene);
        Assert.True(loader.Requests[0].Token.IsCancellationRequested);

        // The last one first, then the dropped one: nothing more
        loader.Finish(1);
        loader.Finish(0);
        Assert.Equal(["willLoad", "willLoad", "didLoad"], events);
        Assert.Same(loader.Requests[1].Picture, img.Picture);

        // And a failure of a dropped one raises nothing
        img.Src = "c.png";
        Layout(scene);
        img.Src = "d.png";
        Layout(scene);
        loader.Fail(2);
        loader.Finish(3);
        Assert.Equal(["willLoad", "willLoad", "didLoad", "willLoad", "willLoad", "didLoad"], events);
    }

    // ── 12. A null src ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_Src_Set_To_Null_Changes_Nothing_And_The_Picture_Stays()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        var events = Record(img);
        using var scene = new Scene(Page(img));
        Layout(scene);
        loader.Finish(0);
        var picture = img.Picture;

        img.Src = null;
        Layout(scene);
        Assert.Equal(["willLoad", "didLoad"], events);
        Assert.Same(picture, img.Picture);
        Assert.Single(loader.Requests);

        // The same src again is no change at all
        img.Src = "a.png";
        Layout(scene);
        img.Src = "a.png";
        Layout(scene);
        Assert.Equal(2, loader.Requests.Count);
    }

    [AvaloniaFact]
    public void B12_With_No_Src_At_First_Nothing_Is_Raised_Until_One_Is_Set()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg();
        var events = Record(img);
        using var scene = new Scene(Page(img));
        Layout(scene);
        Assert.Empty(events);
        Assert.False(img.IsWatching);

        img.Src = "a.png";
        Layout(scene);
        Assert.Equal(["willLoad"], events);
        Assert.Single(loader.Requests);
    }

    [AvaloniaFact]
    public void B12_A_Src_Set_And_Taken_Away_Before_It_Was_Ever_Attached_Loads_Nothing()
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        img.Src = null;
        var events = Record(img);
        using var scene = new Scene(Page(img));
        Layout(scene);
        Assert.Empty(events);
        Assert.Empty(loader.Requests);
    }

    // ── 13. Automation ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_It_Is_An_Image_Named_By_Alt_And_An_Empty_Alt_Leaves_It_Out()
    {
        var img = new AvonicImg { Alt = "Four quarters" };
        var empty = new AvonicImg { Alt = "" };
        using var scene = new Scene(Page(img, empty));

        var peer = ControlAutomationPeer.CreatePeerForElement(img);
        Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
        Assert.Equal("Four quarters", peer.GetName());
        Assert.True(peer.IsControlElement());

        var decorative = ControlAutomationPeer.CreatePeerForElement(empty);
        Assert.False(decorative.IsControlElement());
        Assert.False(decorative.IsContentElement());
    }

    // ── 14. In an avatar or a thumbnail ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_In_An_Avatar_It_Fills_And_Covers_It_And_Is_Cut_To_A_Circle() => InstalledTheme.Run(_ =>
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        var avatar = new AvonicAvatar { Content = img, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(new Panel { Background = Brushes.White, Children = { avatar } }, 390, 200);
        Layout(scene);
        loader.Finish(0, 300, 200);
        Layout(scene);

        Assert.Equal(new Size(64, 64), img.Bounds.Size);
        var frame = scene.Window.CaptureRenderedFrame()!;
        // Cover crops the sides: the quarters meet at the middle, and the corners are cut away
        Assert.Equal(TestPicture.Red, Frames.PixelAt(frame, 20, 20));
        Assert.Equal(TestPicture.Yellow, Frames.PixelAt(frame, 44, 44));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 2, 2));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 61, 61));
    });

    [AvaloniaFact]
    public void B14_In_A_Thumbnail_It_Fills_And_Covers_It_With_Square_Corners_And_Not_Deeper_Down() => InstalledTheme.Run(_ =>
    {
        using var loader = new FakeLoader();
        var img = new AvonicImg { Src = "a.png" };
        var nested = new AvonicImg { Src = "b.png" };
        var thumbnail = new AvonicThumbnail { Content = img };
        var deeper = new AvonicThumbnail { Content = new Panel { Children = { nested } } };
        using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { thumbnail, deeper } }, 390, 200);
        Layout(scene);
        loader.Finish(0, 300, 200);
        loader.Finish(1, 300, 200);
        Layout(scene);

        Assert.Equal(new Size(48, 48), img.Bounds.Size);
        var frame = scene.Window.CaptureRenderedFrame()!;
        Assert.Equal(TestPicture.Red, Frames.PixelAt(frame, 1, 1));
        Assert.Equal(TestPicture.Yellow, Frames.PixelAt(frame, 46, 46));

        // ::slotted is a direct child only: one inside a panel in a thumbnail is contained, 48 × 32, at the top
        Assert.Equal(new Size(48, 32), nested.Bounds.Size);
    });

    // ── 15. Bubbling ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_The_Events_Bubble()
    {
        using var loader = new FakeLoader();
        var ok = new AvonicImg { Src = "a.png" };
        var bad = new AvonicImg { Src = "b.png" };
        var page = Page(ok, bad);
        var events = Record(page);
        using var scene = new Scene(page);
        Layout(scene);
        loader.Finish(0);
        loader.Fail(1);
        Assert.Equal(["willLoad", "willLoad", "didLoad", "error"], events);
    }

    // ── 17. Off the UI thread ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_The_Picture_Is_Decoded_Off_The_UI_Thread()
    {
        var previous = ImgLoader.Decode;
        var thread = -1;
        ImgLoader.Decode = bytes =>
        {
            thread = Environment.CurrentManagedThreadId;
            return previous(bytes);
        };
        try
        {
            var src = "data:image/png;base64," + Convert.ToBase64String(TestPicture.Png(30, 20));
            var bitmap = ImgLoader.LoadAsync(src, CancellationToken.None).GetAwaiter().GetResult();
            Assert.Equal(new PixelSize(30, 20), bitmap.PixelSize);
            Assert.NotEqual(-1, thread);
            Assert.NotEqual(Environment.CurrentManagedThreadId, thread);
        }
        finally
        {
            ImgLoader.Decode = previous;
        }
    }
}

/// <summary>
/// Replaces <see cref="AvonicImg.Loader"/> for a test: each request waits until the test finishes it, with a picture or a
/// failure, and runs the dispatcher so that the result arrives.
/// </summary>
internal sealed class FakeLoader : IDisposable
{
    private readonly Func<string, CancellationToken, Task<Bitmap>> _previous = AvonicImg.Loader;

    public sealed class Request(string src, CancellationToken token)
    {
        public string Src { get; } = src;
        public CancellationToken Token { get; } = token;
        public TaskCompletionSource<Bitmap> Completion { get; } = new();
        public Bitmap? Picture { get; set; }
    }

    public List<Request> Requests { get; } = [];

    public FakeLoader()
    {
        AvonicImg.Loader = (src, token) =>
        {
            var request = new Request(src, token);
            Requests.Add(request);
            return request.Completion.Task;
        };
    }

    public void Finish(int index, int width = 300, int height = 200)
    {
        var request = Requests[index];
        request.Picture = TestPicture.Create(width, height);
        request.Completion.SetResult(request.Picture);
        Dispatcher.UIThread.RunJobs();
    }

    public void Fail(int index)
    {
        Requests[index].Completion.SetException(new FileNotFoundException(Requests[index].Src));
        Dispatcher.UIThread.RunJobs();
    }

    public void Dispose() => AvonicImg.Loader = _previous;
}

/// <summary>The picture Chromium was given: four quarters, red, green, blue and yellow, as the measuring script drew it.</summary>
internal static class TestPicture
{
    public static readonly Color Red = Color.FromRgb(220, 40, 40);
    public static readonly Color Green = Color.FromRgb(40, 160, 40);
    public static readonly Color Blue = Color.FromRgb(40, 40, 220);
    public static readonly Color Yellow = Color.FromRgb(230, 200, 30);

    public static unsafe WriteableBitmap Create(int width, int height)
    {
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var buffer = bitmap.Lock();
        for (var y = 0; y < height; y++)
        {
            var row = (uint*)((byte*)buffer.Address + y * buffer.RowBytes);
            for (var x = 0; x < width; x++)
            {
                var color = y < height / 2.0 ? (x < width / 2.0 ? Red : Green) : (x < width / 2.0 ? Blue : Yellow);
                row[x] = 0xFF000000u | (uint)color.R << 16 | (uint)color.G << 8 | color.B;
            }
        }

        return bitmap;
    }

    public static byte[] Png(int width, int height)
    {
        using var stream = new MemoryStream();
        Create(width, height).Save(stream, PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }

}
