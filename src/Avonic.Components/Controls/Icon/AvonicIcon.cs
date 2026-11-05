using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionicons' <c>ion-icon</c>. A box <c>1em</c> square showing one SVG, in the current text colour.
/// </summary>
/// <remarks>
/// It draws itself, so it has no template. Ionic's only inner element, <c>.icon-inner</c>, is there
/// to carry the RTL flip. See <c>docs/components/ion-icon/README.md</c>.
/// </remarks>
public class AvonicIcon : Control
{
    // ── Ionic constants (icon.css, at the browser's 16 px rem) ───────────────────────────────

    internal const double SmallSize = 18;   // 1.125rem
    internal const double LargeSize = 32;   // 2rem
    internal const double DefaultStrokeWidth = 32; // var(--ionicon-stroke-width, 32px)

    internal const string StrokeWidthKey = "Avonic.Icon.IoniconStrokeWidth";

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's <c>name</c>, such as <c>chevron-forward</c>. Renamed because
    /// <see cref="StyledElement.Name"/> already exists.
    /// </summary>
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<AvonicIcon, string?>(nameof(IconName));

    /// <summary>Ionic's <c>md</c>: which icon to use on md mode. Beats <see cref="IconName"/>.</summary>
    public static readonly StyledProperty<string?> MdProperty =
        AvaloniaProperty.Register<AvonicIcon, string?>(nameof(Md));

    /// <summary>Ionic's <c>icon</c>: a name, or a source if it contains <c>/</c> or <c>.</c>.</summary>
    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<AvonicIcon, string?>(nameof(Icon));

    /// <summary>
    /// Ionic's <c>src</c>: an <c>avares://</c> or <c>data:image/svg+xml</c> source. Anything else is blank.
    /// </summary>
    public static readonly StyledProperty<string?> SrcProperty =
        AvaloniaProperty.Register<AvonicIcon, string?>(nameof(Src));

    /// <summary>Ionic's <c>color</c>. Beats <see cref="Foreground"/>, as <c>!important</c> does.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicIcon>();

    /// <summary>Ionic's <c>size</c>. Beats <see cref="FontSize"/>, as <c>!important</c> does.</summary>
    public static readonly StyledProperty<IconSize?> SizeProperty =
        AvaloniaProperty.Register<AvonicIcon, IconSize?>(nameof(Size));

    /// <summary>
    /// Ionic's <c>flipRtl</c>: whether to mirror in right-to-left layouts. By default, only arrows and
    /// chevrons do.
    /// </summary>
    public static readonly StyledProperty<bool?> FlipRtlProperty =
        AvaloniaProperty.Register<AvonicIcon, bool?>(nameof(FlipRtl));

    /// <summary>Ionic's <c>currentColor</c>. Inherited.</summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<AvonicIcon>();

    /// <summary>Ionic's <c>1em</c>: the size of the box. Inherited.</summary>
    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<AvonicIcon>();

    private SvgArtwork _artwork = SvgArtwork.Empty;
    private string? _iconName;
    private double _strokeWidth = DefaultStrokeWidth;
    private IDisposable? _strokeWidthSubscription;

    static AvonicIcon()
    {
        AffectsMeasure<AvonicIcon>(FontSizeProperty, SizeProperty);
        AffectsRender<AvonicIcon>(ForegroundProperty, ColorProperty, ColorPalette.BaseProperty, FlipRtlProperty, FlowDirectionProperty);

        // display: inline-block. A 1em box at the start of its parent, rather than stretching to fill it.
        HorizontalAlignmentProperty.OverrideDefaultValue<AvonicIcon>(HorizontalAlignment.Left);
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicIcon>(VerticalAlignment.Top);

        IconNameProperty.Changed.AddClassHandler<AvonicIcon>((icon, _) => icon.LoadIcon());
        MdProperty.Changed.AddClassHandler<AvonicIcon>((icon, _) => icon.LoadIcon());
        IconProperty.Changed.AddClassHandler<AvonicIcon>((icon, _) => icon.LoadIcon());
        SrcProperty.Changed.AddClassHandler<AvonicIcon>((icon, _) => icon.LoadIcon());

        SizeProperty.Changed.AddClassHandler<AvonicIcon>((icon, _) => icon.UpdatePseudoClasses());
        FlipRtlProperty.Changed.AddClassHandler<AvonicIcon>((icon, _) => icon.UpdatePseudoClasses());
    }

    public AvonicIcon()
    {
        // contain: strict, and the SVG's overflow: hidden.
        ClipToBounds = true;
    }

    /// <inheritdoc cref="IconNameProperty"/>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <inheritdoc cref="MdProperty"/>
    public string? Md
    {
        get => GetValue(MdProperty);
        set => SetValue(MdProperty, value);
    }

    /// <inheritdoc cref="IconProperty"/>
    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <inheritdoc cref="SrcProperty"/>
    public string? Src
    {
        get => GetValue(SrcProperty);
        set => SetValue(SrcProperty, value);
    }

    /// <inheritdoc cref="ColorProperty"/>
    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <inheritdoc cref="SizeProperty"/>
    public IconSize? Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <inheritdoc cref="FlipRtlProperty"/>
    public bool? FlipRtl
    {
        get => GetValue(FlipRtlProperty);
        set => SetValue(FlipRtlProperty, value);
    }

    /// <inheritdoc cref="ForegroundProperty"/>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <inheritdoc cref="FontSizeProperty"/>
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Ionicons' <c>addIcons</c>: makes each source available by name. It beats a bundled icon of the
    /// same name, and the first registration of a name wins. A camelCase key also registers in kebab-case.
    /// </summary>
    public static void AddIcons(IReadOnlyDictionary<string, string> icons) => IconStore.AddIcons(icons);

    /// <summary>Avalonia mirrors drawing under a right-to-left ancestor. Ionic doesn't, so opt out.</summary>
    protected override bool BypassFlowDirectionPolicies => true;

    /// <summary><c>role="img"</c>. <c>AutomationProperties.Name</c> is Ionic's <c>aria-label</c>.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new IconAutomationPeer(this);

    private sealed class IconAutomationPeer(AvonicIcon owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;
        protected override string GetClassNameCore() => nameof(AvonicIcon);
    }

    private double EffectiveSize => Size switch
    {
        IconSize.Small => SmallSize,
        IconSize.Large => LargeSize,
        _ => FontSize,
    };

    /// <summary>
    /// <c>flipRtl || shouldAutoFlip</c>. Uses the name even if <see cref="Src"/> supplies the artwork.
    /// </summary>
    private bool ShouldBeFlippable =>
        FlipRtl == true ||
        (FlipRtl != false && _iconName is not null && (_iconName.Contains("arrow") || _iconName.Contains("chevron")));

    /// <summary><c>loadIcon</c>, with the order from <c>getUrl</c>.</summary>
    private void LoadIcon()
    {
        _iconName = IconResolver.GetName(IconName, Icon, Md);

        var artwork =
            IconResolver.GetSrc(Src) is { } src ? IconStore.GetSource(src) :
            _iconName is not null ? IconStore.GetNamed(_iconName) :
            IconResolver.GetSrc(Icon) is { } iconSrc ? IconStore.GetSource(iconSrc) :
            null;

        // `if (url)`: a change to no icon at all leaves the last one showing.
        if (artwork is not null)
        {
            _artwork = artwork;

            _strokeWidthSubscription?.Dispose();
            _strokeWidthSubscription = null;
            if (artwork.UsesStrokeWidthVariable)
            {
                _strokeWidthSubscription = this.GetResourceObservable(StrokeWidthKey).Subscribe(new Avalonia.Reactive.AnonymousObserver<object?>(value =>
                {
                    _strokeWidth = value is double width ? width : DefaultStrokeWidth;
                    InvalidateVisual();
                }));
            }
        }

        UpdatePseudoClasses();
        InvalidateVisual();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":size-small", Size == IconSize.Small);
        PseudoClasses.Set(":size-large", Size == IconSize.Large);
        PseudoClasses.Set(":flip-rtl", ShouldBeFlippable);
    }

    protected override Size MeasureOverride(Size availableSize) => new(EffectiveSize, EffectiveSize);

    public override void Render(DrawingContext context)
    {
        var bounds = Bounds.Size;
        if (_artwork.Layers.Count == 0 || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var color = CurrentColor();

        // :host(.flip-rtl):host-context([dir='rtl']) .icon-inner { transform: scaleX(-1); }
        var flip = FlowDirection == FlowDirection.RightToLeft && ShouldBeFlippable
            ? new Matrix(-1, 0, 0, 1, bounds.Width, 0)
            : Matrix.Identity;

        using (context.PushTransform(ViewBoxTransform(bounds) * flip))
        {
            foreach (var layer in _artwork.Layers)
            {
                using (context.PushTransform(layer.Transform))
                {
                    context.DrawGeometry(BrushFor(layer.Fill, color), PenFor(layer, color), layer.Geometry);
                }
            }
        }
    }

    /// <summary>:host(.ion-color) { color: var(--ion-color-base) !important; }</summary>
    private IBrush? CurrentColor()
    {
        var hasColor = !string.IsNullOrEmpty(Color);
        return hasColor ? ColorPalette.GetBase(this) ?? Foreground : Foreground;
    }

    private static IBrush? BrushFor(SvgArtwork.Paint? paint, IBrush? currentColor) => paint switch
    {
        null => null,
        { Brush: { } brush } => brush,
        _ => currentColor,
    };

    private Pen? PenFor(SvgArtwork.Layer layer, IBrush? currentColor)
    {
        var brush = BrushFor(layer.Stroke, currentColor);
        var width = layer.StrokeWidthFromVariable ? _strokeWidth : layer.StrokeWidth;
        return brush is null || width <= 0
            ? null
            : new Pen(brush, width, lineCap: layer.LineCap, lineJoin: layer.LineJoin, miterLimit: layer.MiterLimit);
    }

    /// <summary>The SVG's default, <c>preserveAspectRatio="xMidYMid meet"</c>.</summary>
    private Matrix ViewBoxTransform(Size bounds)
    {
        if (_artwork.ViewBox is not { } viewBox)
        {
            return Matrix.Identity;
        }

        var scale = Math.Min(bounds.Width / viewBox.Width, bounds.Height / viewBox.Height);
        return Matrix.CreateTranslation(-viewBox.X, -viewBox.Y)
             * Matrix.CreateScale(scale, scale)
             * Matrix.CreateTranslation((bounds.Width - viewBox.Width * scale) / 2, (bounds.Height - viewBox.Height * scale) / 2);
    }
}
