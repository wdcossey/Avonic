using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-img</c>: a picture that loads only once it's in the viewport, and says when it starts, finishes or fails. It
/// has no look of its own: as wide as its parent, as tall as the picture's aspect ratio, contained. See
/// <c>docs/components/ion-img/README.md</c>.
/// </summary>
/// <remarks>
/// Ionic's <c>IntersectionObserver</c> is <see cref="Layoutable.EffectiveViewportChanged"/>, which the control listens to only
/// while it waits to load, as Ionic observes only then. Its viewport is the window's, clipped by every ancestor that clips and
/// moved by a scroll, and an edge counts, as <c>isIntersecting</c> counts one.
/// </remarks>
[TemplatePart("PART_Image", typeof(Image))]
[TemplatePart("PART_Alt", typeof(Control))]
[TemplatePart("PART_Broken", typeof(Control))]
[TemplatePart("PART_BrokenIcon", typeof(Control))]
[PseudoClasses(":in-avatar", ":in-thumbnail", ":sized")]
public class AvonicImg : TemplatedControl
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>alt</c>.</summary>
    public static readonly StyledProperty<string?> AltProperty =
        AvaloniaProperty.Register<AvonicImg, string?>(nameof(Alt));

    /// <summary>Ionic <c>src</c>.</summary>
    public static readonly StyledProperty<string?> SrcProperty =
        AvaloniaProperty.Register<AvonicImg, string?>(nameof(Src));

    /// <summary>
    /// The text that describes the picture: its automation name, and what shows where there's no picture, before it loads or
    /// when it fails, as a browser shows <c>alt</c>.
    /// </summary>
    public string? Alt
    {
        get => GetValue(AltProperty);
        set => SetValue(AltProperty, value);
    }

    /// <summary>
    /// The picture's URL: <c>http:</c>, <c>https:</c>, <c>avares:</c>, <c>file:</c>, a <c>data:</c> URI in base64, or a file
    /// path. It loads once the control is in the viewport. Set to null, it changes nothing, as in Ionic.
    /// </summary>
    public string? Src
    {
        get => GetValue(SrcProperty);
        set => SetValue(SrcProperty, value);
    }

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionImgWillLoad</c>: the control has come into the viewport, and starts to load <see cref="Src"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonImgWillLoadEvent =
        RoutedEvent.Register<AvonicImg, RoutedEventArgs>(nameof(IonImgWillLoad), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionImgDidLoad</c>: the picture has loaded, and shows.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonImgDidLoadEvent =
        RoutedEvent.Register<AvonicImg, RoutedEventArgs>(nameof(IonImgDidLoad), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionError</c>: the picture couldn't be loaded or decoded.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonErrorEvent =
        RoutedEvent.Register<AvonicImg, RoutedEventArgs>(nameof(IonError), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? IonImgWillLoad
    {
        add => AddHandler(IonImgWillLoadEvent, value);
        remove => RemoveHandler(IonImgWillLoadEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonImgDidLoad
    {
        add => AddHandler(IonImgDidLoadEvent, value);
        remove => RemoveHandler(IonImgDidLoadEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonError
    {
        add => AddHandler(IonErrorEvent, value);
        remove => RemoveHandler(IonErrorEvent, value);
    }

    /// <summary>
    /// What fetches and decodes a <see cref="Src"/>: <see cref="ImgLoader"/>. Tests replace it with one they finish themselves, so
    /// that none needs the network. It's called on the UI thread, and its result comes back to it.
    /// </summary>
    internal static Func<string, CancellationToken, Task<Bitmap>> Loader { get; set; } = ImgLoader.LoadAsync;

    static AvonicImg()
    {
        // display: block, height auto: at the top of a taller box, as a block is. As a default, not a theme value, so that an
        // item's flex row still centres it, and an avatar's fill can set it.
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicImg>(VerticalAlignment.Top);
    }

    public AvonicImg() => _hiddenChain = new HiddenChainWatcher(RequestCheck);

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private Image? _image;
    private Control? _alt;
    private Control? _broken;
    private Control? _brokenIcon;
    private bool _attached;
    private bool _didLoad;

    /// <summary>Ionic's <c>io</c>: whether it's waiting to come into the viewport.</summary>
    private bool _watching;
    private Rect? _viewport;

    /// <summary>
    /// <c>display: none</c> never intersects, and showing it moves nothing, so the viewport raises nothing: this watches the
    /// chain, and checks again after the layout that follows.
    /// </summary>
    private readonly HiddenChainWatcher _hiddenChain;

    /// <summary>Whether it waits for something hidden in its chain to show.</summary>
    internal bool IsWaitingUntilShown => _hiddenChain.IsWaiting;
    private bool _checkAfterLayout;
    private bool _inCallback;

    /// <summary>The picture shown, which stays while another loads, as an <c>&lt;img&gt;</c>'s does.</summary>
    private Bitmap? _bitmap;
    private bool _loading;

    /// <summary>The last load failed, so a browser shows its broken image.</summary>
    private bool _failed;
    private int _generation;
    private CancellationTokenSource? _cancel;

    /// <summary>Whether it's waiting to come into the viewport. Once it has, it watches nothing.</summary>
    internal bool IsWatching => _watching;

    /// <summary>The picture shown, if any.</summary>
    internal Bitmap? Picture => _bitmap;

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _image = e.NameScope.Find<Image>("PART_Image");
        _alt = e.NameScope.Find<Control>("PART_Alt");
        _broken = e.NameScope.Find<Control>("PART_Broken");
        _brokenIcon = e.NameScope.Find<Control>("PART_BrokenIcon");
        UpdateParts();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;

        // ::slotted(ion-img) in an avatar or a thumbnail: a direct child, which is the logical parent here
        PseudoClasses.Set(":in-avatar", Parent is AvonicAvatar);
        PseudoClasses.Set(":in-thumbnail", Parent is AvonicThumbnail);
        UpdateParts();

        if (!_didLoad)
        {
            // componentDidLoad, once. Until then, Stencil calls no watcher. An observer that kept watching while it was away
            // hears again at the layout that attaching it causes.
            _didLoad = true;
            AddIO();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        _viewport = null;
        _hiddenChain.Dispose();
        StopCheckAfterLayout();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SrcProperty && _didLoad)
        {
            // srcChanged
            AddIO();
        }
        else if (change.Property == AltProperty || change.Property == HeightProperty)
        {
            UpdateParts();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        // A box that grows into the viewport intersects with no change of viewport. The size comes in the arrange, before the
        // pass raises the viewport, which may also have moved, so check after the pass.
        if (_watching)
        {
            CheckAfterLayout();
        }
    }

    // ── The observer ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>addIO</c>: with no <see cref="Src"/> it does nothing, not even stop an observer that's waiting, as in Ionic. Otherwise it
    /// watches again, from scratch.
    /// </summary>
    private void AddIO()
    {
        if (Src is null)
        {
            return;
        }

        RemoveIO();
        _watching = true;
        _viewport = null;
        EffectiveViewportChanged += OnEffectiveViewportChanged;
        RequestCheck();
    }

    /// <summary><c>removeIO</c>.</summary>
    private void RemoveIO()
    {
        if (!_watching)
        {
            return;
        }

        _watching = false;
        EffectiveViewportChanged -= OnEffectiveViewportChanged;
        _hiddenChain.Dispose();
        StopCheckAfterLayout();
    }

    /// <summary>
    /// An observer's first callback comes in the next frame whatever happens, but a new listener hears nothing until a layout
    /// pass, and nothing asks for one. So this asks for an arrange, and checks after it.
    /// </summary>
    private void RequestCheck()
    {
        if (!_attached)
        {
            return;
        }

        CheckAfterLayout();
        InvalidateArrange();
    }

    private void CheckAfterLayout()
    {
        if (!_checkAfterLayout)
        {
            _checkAfterLayout = true;
            LayoutUpdated += OnLayoutUpdated;
        }
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        StopCheckAfterLayout();
        Check();
    }

    private void StopCheckAfterLayout()
    {
        if (_checkAfterLayout)
        {
            _checkAfterLayout = false;
            LayoutUpdated -= OnLayoutUpdated;
        }
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        _viewport = e.EffectiveViewport;
        Check();
    }

    /// <summary>
    /// The observer's callback: if it intersects now, <c>load()</c> and <c>removeIO()</c>, in that order, as Ionic has them. So a
    /// <see cref="Src"/> set by an <see cref="IonImgWillLoad"/> handler starts a watch that <c>removeIO()</c> then ends, and only
    /// the first loads, as measured in Chromium. A layout that a handler causes doesn't come back in here, as no observer calls
    /// back during its own callback.
    /// </summary>
    private void Check()
    {
        if (_inCallback || !_watching || !_attached || _viewport is not { } viewport)
        {
            return;
        }

        if (!IsEffectivelyVisible)
        {
            // display: none never intersects. Showing it moves nothing, so the viewport raises nothing: watch the chain.
            _hiddenChain.Watch(this);
            return;
        }

        if (Intersects(viewport, Bounds.Size))
        {
            _inCallback = true;
            try
            {
                Load();
            }
            finally
            {
                _inCallback = false;
            }

            RemoveIO();
        }
    }

    /// <summary>
    /// <c>isIntersecting</c>: the box and the viewport touch, an edge included, as a box of no height in view intersects. A
    /// viewport clipped away entirely is a rect of no size at the clipping ancestor's origin, so one of no width and no height
    /// intersects nothing, where Chromium's clip of no area in view still has a width.
    /// </summary>
    internal static bool Intersects(Rect viewport, Size size) =>
        (viewport.Width > 0 || viewport.Height > 0)
        && viewport.X <= size.Width && viewport.Right >= 0
        && viewport.Y <= size.Height && viewport.Bottom >= 0;

    // ── Loading ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>load()</c>: <c>loadSrc = src</c> and <c>ionImgWillLoad</c>. The <c>&lt;img&gt;</c> then fetches it, and an older fetch's
    /// result is dropped. A null <see cref="Src"/> by now, from an observer that was still waiting, takes the picture away and
    /// raises nothing more, as an <c>&lt;img&gt;</c> without <c>src</c> does.
    /// </summary>
    private void Load()
    {
        // The generation first: a cancelled fetch can carry on at once, inside Cancel, and must find itself superseded
        var src = Src;
        var generation = ++_generation;
        CancelFetch();

        if (src is null)
        {
            Show(null);
        }

        _loading = src is not null;
        _failed = false;
        UpdateParts();
        RaiseEvent(new RoutedEventArgs(IonImgWillLoadEvent, this));

        // After the event, as a fetch can finish at once. A src a handler sets doesn't start one: see Check.
        if (src is not null && generation == _generation)
        {
            _cancel = new CancellationTokenSource();
            _ = FetchAsync(src, generation, _cancel.Token);
        }
    }

    private async Task FetchAsync(string src, int generation, CancellationToken cancellationToken)
    {
        Bitmap? bitmap = null;
        try
        {
            bitmap = await Loader(src, cancellationToken);
        }
        catch (Exception) when (generation == _generation)
        {
            // Anything that fails is onError: a missing file, a 404, bytes that aren't a picture
        }
        catch (Exception)
        {
            // A newer src has taken over, and this one's result doesn't matter
            return;
        }

        if (generation != _generation)
        {
            bitmap?.Dispose();
            return;
        }

        _loading = false;
        _failed = bitmap is null;
        _cancel?.Dispose();
        _cancel = null;
        Show(bitmap);
        RaiseEvent(new RoutedEventArgs(bitmap is null ? IonErrorEvent : IonImgDidLoadEvent, this));
    }

    private void CancelFetch()
    {
        var cancel = _cancel;
        _cancel = null;
        cancel?.Cancel();
        cancel?.Dispose();
    }

    /// <summary>Shows a picture, or none, and frees the one it replaces, which nothing else holds.</summary>
    private void Show(Bitmap? bitmap)
    {
        var old = _bitmap;
        _bitmap = bitmap;
        UpdateParts();
        if (old is not null && !ReferenceEquals(old, bitmap))
        {
            old.Dispose();
        }
    }

    /// <summary>
    /// The picture, or, where there's none and none is loading, what Chromium shows for an <c>&lt;img&gt;</c>, measured: nothing
    /// while a first picture loads, and the old one while another does. With no picture:
    /// <list type="bullet">
    /// <item>An alt text: the broken-image icon, then the text, whether it failed or has no <c>src</c> yet.</item>
    /// <item>No alt text, or an empty one, and it failed: a box with a border, and the icon in it.</item>
    /// <item>No alt text, no <c>src</c> yet, and a size of its own: the box, with no icon. Without a size, nothing.</item>
    /// </list>
    /// </summary>
    private void UpdateParts()
    {
        // A size of its own: a Height, or an avatar's or a thumbnail's, which height: 100% fills
        var sized = !double.IsNaN(Height) || PseudoClasses.Contains(":in-avatar") || PseudoClasses.Contains(":in-thumbnail");
        PseudoClasses.Set(":sized", sized);

        if (_image is not null)
        {
            _image.Source = _bitmap;
            _image.IsVisible = _bitmap is not null;
        }

        var none = _bitmap is null && !_loading;
        var alt = !string.IsNullOrEmpty(Alt);
        if (_alt is not null)
        {
            _alt.IsVisible = none && alt;
        }

        if (_broken is not null)
        {
            _broken.IsVisible = none && !alt && (_failed || sized);
        }

        if (_brokenIcon is not null)
        {
            _brokenIcon.IsVisible = _failed;
        }
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary>An <c>&lt;img&gt;</c>: an image named by its <c>alt</c>. An empty <c>alt</c> makes it decorative, out of the tree.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new ImgAutomationPeer(this);

    private sealed class ImgAutomationPeer(AvonicImg owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

        protected override string? GetNameCore() => owner.Alt is { Length: > 0 } alt ? alt : base.GetNameCore();

        protected override bool IsControlElementCore() => owner.Alt != string.Empty;

        protected override bool IsContentElementCore() => owner.Alt != string.Empty;
    }
}
