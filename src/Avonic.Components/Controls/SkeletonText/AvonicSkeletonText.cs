using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-skeleton-text</c>: a bar that stands in for a line of text while it loads, which <see cref="Animated"/> sweeps a
/// lighter band across. The look is in the theme. See <c>docs/components/ion-skeleton-text/README.md</c>.
/// </summary>
/// <remarks>
/// The band is a gradient the theme paints in <c>PART_Shimmer</c>, a part 800 wider than the host. The control moves it with a
/// <see cref="Visual.RenderTransform"/> on <see cref="AvonicConfig"/>'s clock, as <c>background-position</c> moves the tile in CSS,
/// so a frame re-measures nothing.
/// </remarks>
[TemplatePart("PART_Shimmer", typeof(Control))]
[PseudoClasses(":animated", ":in-media")]
public class AvonicSkeletonText : TemplatedControl
{
    // ── Ionic constants (skeleton-text.css) ──────────────────────────────────────────────────

    /// <summary><c>background-size: 800px 104px</c>, across: the gradient's tile, which repeats.</summary>
    internal const double ShimmerTile = 800;

    /// <summary><c>@keyframes shimmer { 0% { background-position: -400px 0 } }</c>.</summary>
    internal const double ShimmerFrom = -400;

    /// <summary><c>100% { background-position: 400px 0 }</c>.</summary>
    internal const double ShimmerTo = 400;

    /// <summary><c>animation-duration: 1s</c>, linear and forever.</summary>
    internal static readonly TimeSpan ShimmerDuration = TimeSpan.FromSeconds(1);

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>animated</c>.</summary>
    public static readonly StyledProperty<bool> AnimatedProperty =
        AvaloniaProperty.Register<AvonicSkeletonText, bool>(nameof(Animated));

    /// <summary>
    /// If true, a lighter band sweeps across it once a second, unless <see cref="AvonicConfig.Animated"/> was off when it was
    /// read: when the skeleton was first attached, or when this last changed, as Ionic reads its config when it renders.
    /// </summary>
    public bool Animated
    {
        get => GetValue(AnimatedProperty);
        set => SetValue(AnimatedProperty, value);
    }

    static AvonicSkeletonText()
    {
        // pointer-events: none
        IsHitTestVisibleProperty.OverrideDefaultValue<AvonicSkeletonText>(false);

        // display: block, height auto: at the top of a taller box, as a block is. As a default, not a theme value, so that an
        // item's flex row still centres it, as align-items: center does, and in-media's fill can set it.
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicSkeletonText>(VerticalAlignment.Top);
    }

    /// <summary>The frame callback is made once, not at every frame.</summary>
    public AvonicSkeletonText()
    {
        _onFrame = OnFrame;
        _hiddenChain = new HiddenChainWatcher(Restart);
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private Control? _shimmer;
    private TranslateTransform? _shimmerTransform;
    private bool _rendered;
    private bool _attached;
    private bool _animated;
    private TimeSpan _startedAt;
    private IDisposable? _frame;

    /// <summary>A frame that finds it hidden watches its chain, and showing it starts the band again from 0.</summary>
    private readonly HiddenChainWatcher _hiddenChain;
    private readonly Action _onFrame;

    /// <summary>Whether a frame is requested: only while the band moves.</summary>
    internal bool IsFrameLoopRunning => _frame is not null;

    /// <summary>Where the gradient's tile starts, in the host's space, as <c>background-position</c> puts it now.</summary>
    internal double ShimmerOffset { get; private set; }

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _shimmer = e.NameScope.Find<Control>("PART_Shimmer");
        _shimmerTransform = null;
        if (_shimmer is not null)
        {
            // The tile repeats every 800 px, so a part 800 wider than the host, moved left by up to 800, always covers it
            _shimmer.Margin = new Thickness(0, 0, -ShimmerTile, 0);
            _shimmer.RenderTransform = _shimmerTransform = new TranslateTransform();

            // background-position and linear-gradient(to right) are physical: in right-to-left the band still runs from the
            // left, so the part isn't mirrored
            _shimmer.SetValue(FlowDirectionProperty, FlowDirection.LeftToRight, BindingPriority.Template);
        }

        Update();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;

        if (!_rendered)
        {
            // componentWillLoad's emitStyle, once: an item clips its label for it. Then the first render, which is the only one
            // until animated changes: measured, moving it or changing the config later changes nothing.
            _rendered = true;
            RaiseEvent(new IonStyleEventArgs(new Dictionary<string, bool> { ["skeleton-text"] = true }));
            Render();
        }

        // A CSS animation starts when its element appears, so attaching restarts the band
        Restart();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        StopFrames();
        _hiddenChain.Dispose();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FlowDirectionProperty)
        {
            Update();
        }
        else if (change.Property == AnimatedProperty && _rendered)
        {
            // Stencil renders again: the config and the context are read again, and the class going and coming back
            // restarts the animation
            var was = _animated;
            Render();
            if (_animated && !was)
            {
                Restart();
            }
            else if (!_animated)
            {
                StopFrames();
                _hiddenChain.Dispose();
            }
        }
    }

    /// <summary>
    /// <c>render()</c>: <c>animated &amp;&amp; config.getBoolean('animated', true)</c>, and <c>in-media</c> from an avatar or a
    /// thumbnail anywhere above it.
    /// </summary>
    private void Render()
    {
        _animated = Animated && AvonicConfig.Animated;
        PseudoClasses.Set(":animated", _animated);
        PseudoClasses.Set(":in-media", this.GetVisualAncestors().Any(ancestor => ancestor is AvonicAvatar or AvonicThumbnail));
    }

    // ── Frames ───────────────────────────────────────────────────────────────────────────────

    private void Restart()
    {
        _hiddenChain.Dispose();
        _startedAt = AvonicConfig.Now();
        Update();
    }

    /// <summary>Moves the band to the clock's current time. Tests call it to read an exact time between frames.</summary>
    internal void Tick() => Update();

    private void OnFrame()
    {
        _frame = null;
        Update();
    }

    private void StopFrames()
    {
        _frame?.Dispose();
        _frame = null;
    }

    private void Update()
    {
        if (!_animated || !_attached)
        {
            return;
        }

        if (!IsEffectivelyVisible)
        {
            // display: none stops it: this frame requests no other. Showing it starts it again from 0.
            _hiddenChain.Watch(this);
            return;
        }

        var elapsed = AvonicConfig.Now() - _startedAt;
        var t = elapsed.Ticks % ShimmerDuration.Ticks / (double)ShimmerDuration.Ticks;
        ShimmerOffset = ShimmerFrom + (ShimmerTo - ShimmerFrom) * t;
        if (_shimmerTransform is not null)
        {
            // The tile's place, less whole tiles. The part starts at the host's left and reaches 800 past its right, so it's
            // moved back a tile. In right-to-left it's laid out from the right, reaching 800 past the left, and its transform
            // moves it in the mirrored space, so it's moved the other way, from there.
            var offset = (ShimmerOffset % ShimmerTile + ShimmerTile) % ShimmerTile;
            _shimmerTransform.X = FlowDirection == FlowDirection.RightToLeft ? -offset : offset - ShimmerTile;
        }

        _frame ??= AvonicConfig.RequestFrame(this, _onFrame);
    }

    // ── Converters for the theme ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>background: var(--background)</c>, where <c>--background</c> is <c>rgba(var(--background-rgb, …), n)</c> unless it's
    /// set: the first value, a brush, if it's set, or else <see cref="Css.Rgba"/> of the others.
    /// </summary>
    public static readonly IMultiValueConverter BackgroundOrRgba = new BackgroundConverter();

    /// <summary>
    /// <c>border-radius: var(--border-radius, inherit)</c>: the values are the key, the parent's <see cref="CornerRadius"/>, the
    /// skeleton's size and its <c>FlowDirection</c>. A key that's set is read as <see cref="BorderRadius.ToCornerRadius"/> reads it;
    /// one that isn't gives the parent's radius.
    /// </summary>
    public static readonly IMultiValueConverter RadiusOrInherit = new RadiusOrInheritConverter();

    private sealed class BackgroundConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values.Count > 0 && Css.IsSet(values[0])
                ? values[0]
                : Css.Rgba.Convert(values.Skip(1).ToList(), targetType, parameter, culture);
    }

    private sealed class RadiusOrInheritConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 4)
            {
                return AvaloniaProperty.UnsetValue;
            }

            if (Css.IsSet(values[0]))
            {
                return BorderRadius.ToCornerRadius.Convert([values[0], values[2], values[3]], targetType, parameter, culture);
            }

            return values[1] is CornerRadius inherited ? inherited : default(CornerRadius);
        }
    }
}
