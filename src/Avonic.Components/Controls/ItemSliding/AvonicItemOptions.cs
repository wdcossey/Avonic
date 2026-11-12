using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using Avalonia.VisualTree;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-item-options</c>: the buttons on one side of an <see cref="AvonicItemSliding"/>, behind its item. Its
/// <see cref="AvonicItemOption"/>s are its items. The look is in the theme. See <c>docs/components/ion-item-sliding/README.md</c>.
/// </summary>
/// <remarks>
/// <c>:options-start</c> and <c>:options-end</c> are Ionic's <c>item-options-start</c> and <c>-end</c>: the physical left and right,
/// so <see cref="ItemOptionsSide.End"/> is <c>:options-start</c> in RTL. <c>:in-list-lines-full</c> and <c>:in-list-lines-inset</c>
/// follow the nearest <see cref="AvonicList"/>'s <see cref="AvonicList.Lines"/>, for <c>.list-md-lines-full .item-options-md</c>.
/// </remarks>
[PseudoClasses(":options-start", ":options-end", ":in-list-lines-full", ":in-list-lines-inset")]
public class AvonicItemOptions : ItemsControl
{
    /// <summary>The expandable option's <c>transition-duration: 0.6s</c> on a full swipe.</summary>
    internal static readonly TimeSpan ExpandDuration = TimeSpan.FromMilliseconds(600);

    /// <summary><c>:host(.item-option-expandable)</c>'s <c>transition-timing-function</c>.</summary>
    internal static readonly CubicBezierEasing ExpandEasing = new(0.65, 0.05, 0.36, 1);

    /// <summary>Ionic <c>side</c>: which side of the item the options are on.</summary>
    public static readonly StyledProperty<ItemOptionsSide> SideProperty =
        AvaloniaProperty.Register<AvonicItemOptions, ItemOptionsSide>(nameof(Side));

    /// <summary>Ionic <c>ionSwipe</c>: the item was fully swiped to these options, and released.</summary>
    public static readonly RoutedEvent<ItemOptionsSwipeEventArgs> IonSwipeEvent =
        RoutedEvent.Register<AvonicItemOptions, ItemOptionsSwipeEventArgs>(nameof(IonSwipe), RoutingStrategies.Bubble);

    public ItemOptionsSide Side
    {
        get => GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    public event EventHandler<ItemOptionsSwipeEventArgs>? IonSwipe
    {
        add => AddHandler(IonSwipeEvent, value);
        remove => RemoveHandler(IonSwipeEvent, value);
    }

    private AvonicList? _list;
    private IDisposable? _linesSubscription;
    private IDisposable? _frame;
    private TimeSpan _expandStart;

    static AvonicItemOptions()
    {
        // z-index: 1, under the item's 2.
        ZIndexProperty.OverrideDefaultValue<AvonicItemOptions>(1);
    }

    public AvonicItemOptions()
    {
        UpdateSide();
        Items.CollectionChanged += (_, _) =>
        {
            UpdateOuter();
            FindSliding()?.OptionsChanged();
        };
    }

    /// <summary>
    /// Ionic <c>isEndSide(side)</c>: whether the options are on the physical right, which <c>item-options-end</c> means. Ionic reads
    /// the document's <c>dir</c>; this is the options' own <see cref="Visual.FlowDirection"/>, as the mapping rule for <c>isRTL</c> says.
    /// </summary>
    internal bool IsRight => (Side == ItemOptionsSide.End) != (FlowDirection == Avalonia.Media.FlowDirection.RightToLeft);

    /// <summary>Ionic <c>fireSwipeEvent()</c>, which is <c>@internal</c>: the sliding item calls it.</summary>
    internal void FireSwipeEvent() => RaiseEvent(new ItemOptionsSwipeEventArgs(IonSwipeEvent, this, Side));

    /// <summary>
    /// Whether the sliding item is in a full swipe to this side: <c>.item-sliding-active-swipe-end .item-options-end</c>, or start.
    /// An expandable option then goes to the outer end and grows by the options' width over 600 ms; it snaps back when it ends, as
    /// the rule's <c>transition-property</c> goes back to <c>none</c>.
    /// </summary>
    internal bool IsSwiping { get; private set; }

    internal void SetSwiping(bool swiping)
    {
        if (swiping == IsSwiping)
        {
            return;
        }

        IsSwiping = swiping;
        _frame?.Dispose();
        _frame = null;
        if (swiping)
        {
            _expandStart = AvonicConfig.Now();
            _frame = AvonicConfig.RequestFrame(this, OnExpandFrame);
        }

        UpdateExpansion();
        ItemsPanelRoot?.InvalidateArrange();
    }

    private void OnExpandFrame()
    {
        _frame = null;
        if (!IsSwiping)
        {
            return;
        }

        UpdateExpansion();
        if (AvonicConfig.Now() - _expandStart < ExpandDuration)
        {
            _frame = AvonicConfig.RequestFrame(this, OnExpandFrame);
        }
    }

    /// <summary><c>padding-left: 100%</c>: a percentage of the options' width, eased from 0 since the swipe began.</summary>
    private void UpdateExpansion()
    {
        var progress = IsSwiping ? Math.Clamp((AvonicConfig.Now() - _expandStart) / ExpandDuration, 0, 1) : 0;
        var expansion = ExpandEasing.Ease(progress) * Bounds.Width;
        var before = Side != ItemOptionsSide.Start;
        foreach (var option in Items.OfType<AvonicItemOption>())
        {
            option.SetExpansion(option.Expandable ? expansion : 0, before);
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (IsSwiping)
        {
            UpdateExpansion();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SideProperty || change.Property == FlowDirectionProperty)
        {
            UpdateSide();
            UpdateOuter();
            ItemsPanelRoot?.InvalidateArrange();

            // The sliding item's left and right options follow, whichever of the two changes first in an inherited change.
            FindSliding()?.OptionsChanged();
        }
    }

    /// <summary>
    /// The option at the outer edge: the first of start options, the last of end options, which is the physical left or right
    /// one in either direction, as Avalonia mirrors the row in RTL.
    /// </summary>
    private void UpdateOuter()
    {
        var options = Items.OfType<AvonicItemOption>().ToList();
        for (var i = 0; i < options.Count; i++)
        {
            options[i].SetOuter(Side == ItemOptionsSide.Start && i == 0, Side == ItemOptionsSide.End && i == options.Count - 1);
        }
    }

    private void UpdateSide()
    {
        PseudoClasses.Set(":options-start", !IsRight);
        PseudoClasses.Set(":options-end", IsRight);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _list = this.FindAncestorOfType<AvonicList>();
        _linesSubscription = _list?.GetObservable(AvonicList.LinesProperty).Subscribe(new AnonymousObserver<ListLines?>(_ => UpdateLines()));
        UpdateLines();
        FindSliding()?.OptionsChanged();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _linesSubscription?.Dispose();
        _linesSubscription = null;
        _list = null;
        SetSwiping(false);
        UpdateLines();
    }

    private void UpdateLines()
    {
        var lines = _list?.Lines;
        PseudoClasses.Set(":in-list-lines-full", lines == ListLines.Full);
        PseudoClasses.Set(":in-list-lines-inset", lines == ListLines.Inset);
    }

    private AvonicItemSliding? FindSliding() => this.FindAncestorOfType<AvonicItemSliding>() ?? Parent as AvonicItemSliding;

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Control>(item, out recycleKey);

    /// <summary>A plain element, where an <see cref="ItemsControl"/>'s own peer would call it a list.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new AvonicItemSliding.GroupPeer(this);
}
