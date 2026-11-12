using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-infinite-scroll-content</c>: what an <see cref="AvonicInfiniteScroll"/> shows while it loads, a spinner and an
/// optional line of text. Outside a loading infinite scroll it shows nothing, and only takes its height.
/// </summary>
/// <remarks>
/// <c>:infinite-scroll-loading</c> is on while the nearest <see cref="AvonicInfiniteScroll"/> around it is loading, as Ionic's
/// <c>.infinite-scroll-loading ion-infinite-scroll-content &gt; .infinite-loading</c> is a descendant rule; the template shows
/// <c>PART_Loading</c> on it. See <c>docs/components/ion-infinite-scroll/README.md</c>.
/// </remarks>
[TemplatePart("PART_Loading", typeof(Control))]
[TemplatePart("PART_LoadingSpinner", typeof(AvonicSpinner))]
[TemplatePart("PART_LoadingText", typeof(Control))]
[PseudoClasses(AvonicInfiniteScroll.LoadingClass)]
public class AvonicInfiniteScrollContent : TemplatedControl
{
    /// <summary>
    /// Ionic <c>loadingSpinner</c>. Left unset, it becomes <see cref="AvonicConfig.InfiniteLoadingSpinner"/>, else
    /// <see cref="AvonicConfig.Spinner"/>, else <see cref="SpinnerName.Crescent"/>,
    /// md's default here, when it's first attached, as Ionic sets it in <c>componentDidLoad</c>. Null: no spinner.
    /// </summary>
    public static readonly StyledProperty<SpinnerName?> LoadingSpinnerProperty =
        AvaloniaProperty.Register<AvonicInfiniteScrollContent, SpinnerName?>(nameof(LoadingSpinner));

    /// <summary>
    /// Ionic <c>loadingText</c>: shown under the spinner while loading, as it's written, as Ionic shows it by default. Null: none.
    /// </summary>
    public static readonly StyledProperty<string?> LoadingTextProperty =
        AvaloniaProperty.Register<AvonicInfiniteScrollContent, string?>(nameof(LoadingText));

    private bool _didLoad;
    private AvonicInfiniteScroll? _infiniteScroll;

    /// <inheritdoc cref="LoadingSpinnerProperty"/>
    public SpinnerName? LoadingSpinner
    {
        get => GetValue(LoadingSpinnerProperty);
        set => SetValue(LoadingSpinnerProperty, value);
    }

    /// <inheritdoc cref="LoadingTextProperty"/>
    public string? LoadingText
    {
        get => GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (!_didLoad)
        {
            // componentDidLoad, once: config.get('infiniteLoadingSpinner', config.get('spinner', 'crescent')).
            _didLoad = true;
            if (!IsSet(LoadingSpinnerProperty))
            {
                SetCurrentValue(LoadingSpinnerProperty, AvonicConfig.InfiniteLoadingSpinner ?? AvonicConfig.Spinner ?? SpinnerName.Crescent);
            }
        }

        _infiniteScroll = this.FindAncestorOfType<AvonicInfiniteScroll>();
        if (_infiniteScroll is not null)
        {
            _infiniteScroll.LoadingChanged += OnLoadingChanged;
        }

        UpdateLoading();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_infiniteScroll is not null)
        {
            _infiniteScroll.LoadingChanged -= OnLoadingChanged;
            _infiniteScroll = null;
        }

        UpdateLoading();
    }

    private void OnLoadingChanged(object? sender, EventArgs e) => UpdateLoading();

    private void UpdateLoading() => PseudoClasses.Set(AvonicInfiniteScroll.LoadingClass, _infiniteScroll?.IsLoading == true);
}
