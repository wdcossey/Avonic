using System.Collections;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Gestures;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-reorder-group</c>: its children can be dragged up and down by an <see cref="AvonicReorder"/> handle in them. It
/// shows nothing of its own, so it's an <see cref="ItemsControl"/> on a <see cref="StackPanel"/>. The order changes only when the
/// app calls <see cref="Complete()"/>, usually from <see cref="IonReorderEnd"/>. A virtualising panel isn't supported: the drag reads
/// every child.
/// </summary>
public class AvonicReorderGroup : ItemsControl
{
    /// <summary>The pseudoclass of the child being dragged: Ionic's <c>reorder-selected</c> class.</summary>
    public const string SelectedPseudoClass = ":reorder-selected";

    /// <summary>Within this of the scroller's top or bottom, a move scrolls it.</summary>
    internal const double AutoScrollMargin = 60;

    /// <summary>How far each move scrolls, near an edge.</summary>
    internal const double ScrollJump = 10;

    internal const int GesturePriority = 110;

    /// <summary><c>.reorder-selected { z-index: 100 }</c>: over its siblings, whatever its type.</summary>
    internal const int SelectedZIndex = 100;

    /// <summary><c>.reorder-list-active &gt; * { transition: transform 300ms }</c>.</summary>
    internal static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Ionic <c>disabled</c>: true hides the handles and turns the drag off. Not <c>IsEnabled</c>, which would disable the items.</summary>
    public static readonly StyledProperty<bool> DisabledProperty =
        AvaloniaProperty.Register<AvonicReorderGroup, bool>(nameof(Disabled), defaultValue: true);

    /// <summary>Ionic <c>ionItemReorder</c>, deprecated there: only when the item moved, before <see cref="IonReorderEnd"/>.</summary>
    public static readonly RoutedEvent<ItemReorderEventArgs> IonItemReorderEvent =
        RoutedEvent.Register<AvonicReorderGroup, ItemReorderEventArgs>(nameof(IonItemReorder), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionReorderStart</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonReorderStartEvent =
        RoutedEvent.Register<AvonicReorderGroup, RoutedEventArgs>(nameof(IonReorderStart), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionReorderMove</c>.</summary>
    public static readonly RoutedEvent<ReorderMoveEventArgs> IonReorderMoveEvent =
        RoutedEvent.Register<AvonicReorderGroup, ReorderMoveEventArgs>(nameof(IonReorderMove), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionReorderEnd</c>.</summary>
    public static readonly RoutedEvent<ReorderEndEventArgs> IonReorderEndEvent =
        RoutedEvent.Register<AvonicReorderGroup, ReorderEndEventArgs>(nameof(IonReorderEnd), RoutingStrategies.Bubble);

    private enum ReorderGroupState
    {
        Idle,
        Active,
        Complete,
    }

    private readonly List<double> _cachedHeights = [];
    private readonly Dictionary<Control, Shift> _shifts = [];
    private Gesture? _gesture;
    private ScrollViewer? _scrollEl;
    private ReorderGroupState _state;
    private Control? _selectedItemEl;
    private IDisposable? _selectedZIndex;
    private int _selectedIndex;
    private int _lastToIndex = -1;
    private double _selectedItemHeight;
    private double _scrollElTop;
    private double _scrollElBottom;
    private double _scrollElInitial;
    private double _containerTop;
    private double _containerBottom;
    private IDisposable? _tick;

    public bool Disabled
    {
        get => GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    public event EventHandler<ItemReorderEventArgs>? IonItemReorder
    {
        add => AddHandler(IonItemReorderEvent, value);
        remove => RemoveHandler(IonItemReorderEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonReorderStart
    {
        add => AddHandler(IonReorderStartEvent, value);
        remove => RemoveHandler(IonReorderStartEvent, value);
    }

    public event EventHandler<ReorderMoveEventArgs>? IonReorderMove
    {
        add => AddHandler(IonReorderMoveEvent, value);
        remove => RemoveHandler(IonReorderMoveEvent, value);
    }

    public event EventHandler<ReorderEndEventArgs>? IonReorderEnd
    {
        add => AddHandler(IonReorderEndEvent, value);
        remove => RemoveHandler(IonReorderEndEvent, value);
    }

    /// <summary>Raised when <see cref="Disabled"/> changes, for the handles inside, which show only in an enabled group.</summary>
    internal event EventHandler? DisabledChanged;

    /// <summary>
    /// Ionic <c>complete()</c>: finishes a reorder that has ended, leaving the item where it was dropped. Until then it stays lifted
    /// and no new drag can start. The item moves in <see cref="ItemsControl.Items"/>, as Ionic moves it in the DOM, in the next
    /// frame. With <see cref="ItemsControl.ItemsSource"/> set, the source is the app's, so nothing moves: it shows where the source
    /// has it. Move it there, or pass it to <see cref="Complete{T}(IList{T})"/>. Before a drag has ended, or a second time, it does
    /// nothing.
    /// </summary>
    public void Complete() => CompleteReorder(true);

    /// <summary>Ionic <c>complete(boolean)</c>: true is <see cref="Complete()"/>, and false puts the item back where it began.</summary>
    public void Complete(bool reorder) => CompleteReorder(reorder);

    /// <summary>
    /// Ionic <c>complete(array)</c>: finishes the reorder with nothing moved in the group, and moves the item in
    /// <paramref name="list"/> instead, which is changed in place, unless it's read-only, as Ionic's array is: an
    /// <see cref="ObservableCollection{T}"/> with one <c>Move</c>. Bound as the <see cref="ItemsControl.ItemsSource"/>, that reorders
    /// the group too, as a framework does when it renders the array.
    /// </summary>
    /// <returns>A copy of the list in its new order, or <paramref name="list"/> itself if there was nothing to complete.</returns>
    public IList<T> Complete<T>(IList<T> list)
    {
        var from = _selectedIndex;
        var to = _lastToIndex;
        return CompleteReorder(false) ? ReorderList(list, from, to) : list;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DisabledProperty)
        {
            _gesture?.Enable(!Disabled);
            DisabledChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Ionic's <c>connectedCallback</c>: the closest content's scroller, and the gesture.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _scrollEl = FindScrollElement();
        _gesture = Gesture.Create(new GestureConfig
        {
            Element = this,
            GestureName = "reorder",
            GesturePriority = GesturePriority,
            Threshold = 0,
            Direction = GestureDirection.Y,
            CanStart = CanStart,
            OnStart = OnStart,
            OnMove = OnMove,
            OnEnd = _ => OnEnd(),
        });
        _gesture.Enable(!Disabled);
    }

    /// <summary>Ionic's <c>disconnectedCallback</c>: a drag ends, with its events, and the gesture goes.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        OnEnd();
        _gesture?.Destroy();
        _gesture = null;
        _scrollEl = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// <c>findClosestIonContent</c>: <c>closest('ion-content, .ion-content-scroll-host')</c>, then its scroll element. A host with
    /// the class scrolls itself in a browser; here only a <see cref="ScrollViewer"/> can, so it's the host, or the one around this
    /// on the way to it.
    /// </summary>
    private ScrollViewer? FindScrollElement()
    {
        ScrollViewer? inside = null;
        foreach (var visual in this.GetVisualAncestors())
        {
            if (visual is AvonicContent content)
            {
                return content.GetScrollElement();
            }

            if (visual is Control control && control.Classes.Contains("ion-content-scroll-host"))
            {
                return control as ScrollViewer ?? inside;
            }

            inside ??= visual as ScrollViewer;
        }

        return null;
    }

    private IReadOnlyList<Control> Children => ItemsPanelRoot?.Children ?? (IReadOnlyList<Control>)[];

    // ── The gesture ──────────────────────────────────────────────────────────────────────────

    private bool CanStart(GestureDetail detail)
    {
        if (_selectedItemEl is not null || _state != ReorderGroupState.Idle)
        {
            return false;
        }

        // target.closest('ion-reorder')
        var reorderEl = (detail.Event?.Source as Visual)?.GetSelfAndVisualAncestors().OfType<AvonicReorder>().FirstOrDefault();
        if (reorderEl is null)
        {
            return false;
        }

        if (FindReorderItem(reorderEl) is not { } item)
        {
            return false;
        }

        detail.Data = item;
        return true;
    }

    /// <summary>The ancestor of the handle that is a child of this group: in Avalonia, of its panel.</summary>
    private Control? FindReorderItem(Visual node)
    {
        var container = ItemsPanelRoot;
        for (Visual? current = node; current is not null; current = current.GetVisualParent())
        {
            if (container is not null && current.GetVisualParent() == container)
            {
                return current as Control;
            }
        }

        return null;
    }

    private void OnStart(GestureDetail detail)
    {
        var item = _selectedItemEl = (Control)detail.Data!;
        var heights = _cachedHeights;
        heights.Clear();
        var children = Children;
        if (children.Count == 0)
        {
            return;
        }

        // offsetHeight, summed; and $ionIndex.
        var sum = 0.0;
        for (var i = 0; i < children.Count; i++)
        {
            sum += children[i].Bounds.Height;
            heights.Add(sum);
            if (children[i] == item)
            {
                _selectedIndex = i;
            }
        }

        var top = TopLevel.GetTopLevel(this);
        var box = this.TranslatePoint(default, top!) ?? default;
        _containerTop = box.Y;
        _containerBottom = box.Y + Bounds.Height;
        if (_scrollEl is { } scrollEl && scrollEl.TranslatePoint(default, top!) is { } scrollBox)
        {
            _scrollElInitial = scrollEl.Offset.Y;
            _scrollElTop = scrollBox.Y + AutoScrollMargin;
            _scrollElBottom = scrollBox.Y + scrollEl.Bounds.Height - AutoScrollMargin;
        }
        else
        {
            _scrollElInitial = 0;
            _scrollElTop = 0;
            _scrollElBottom = 0;
        }

        _lastToIndex = _selectedIndex;
        _selectedItemHeight = item.Bounds.Height;
        _state = ReorderGroupState.Active;
        SetSelected(item, true);
        RaiseEvent(new RoutedEventArgs(IonReorderStartEvent, this));
    }

    private void OnMove(GestureDetail detail)
    {
        if (_selectedItemEl is not { } selectedItem)
        {
            return;
        }

        // Scroll if we reach the scroll margins
        var scroll = AutoScroll(detail.CurrentY);

        var top = _containerTop - scroll;
        var bottom = _containerBottom - scroll;
        var currentY = Math.Max(top, Math.Min(detail.CurrentY, bottom));
        var deltaY = scroll + currentY - detail.StartY;
        var normalizedY = currentY - top;
        var fromIndex = _lastToIndex;
        var toIndex = ItemIndexForTop(normalizedY);
        if (toIndex != _lastToIndex)
        {
            _lastToIndex = toIndex;
            ReorderMove(_selectedIndex, toIndex);
        }

        // Update selected item position
        Translate(selectedItem, deltaY, animate: false);
        RaiseEvent(new ReorderMoveEventArgs(IonReorderMoveEvent, this, fromIndex, toIndex));
    }

    private void OnEnd()
    {
        var selectedItemEl = _selectedItemEl;
        _state = ReorderGroupState.Complete;
        if (selectedItemEl is null)
        {
            _state = ReorderGroupState.Idle;
            return;
        }

        var toIndex = _lastToIndex;
        var fromIndex = _selectedIndex;
        if (toIndex == fromIndex)
        {
            CompleteReorder(true);
        }
        else
        {
            RaiseEvent(new ItemReorderEventArgs(IonItemReorderEvent, this, fromIndex, toIndex));
        }

        RaiseEvent(new ReorderEndEventArgs(IonReorderEndEvent, this, fromIndex, toIndex));
    }

    /// <returns>Whether there was a reorder to complete.</returns>
    private bool CompleteReorder(bool reorder)
    {
        if (_selectedItemEl is not { } selectedItemEl || _state != ReorderGroupState.Complete)
        {
            return false;
        }

        var children = Children.ToArray();
        var toIndex = _lastToIndex;
        var fromIndex = _selectedIndex;

        // The move and the cleared transforms in one frame, so that nothing eases.
        AvonicConfig.RequestFrame(this, () =>
        {
            if (toIndex != fromIndex && reorder)
            {
                MoveItem(fromIndex, toIndex);
            }

            foreach (var child in children)
            {
                ClearTranslate(child);
            }
        });

        SetSelected(selectedItemEl, false);
        _selectedItemEl = null;
        _state = ReorderGroupState.Idle;
        return true;
    }

    /// <summary><c>insertBefore</c>: the item is the child at <paramref name="from"/>, and ends at <paramref name="to"/>.</summary>
    private void MoveItem(int from, int to)
    {
        if (ItemsSource is not null || from < 0 || from >= Items.Count || to < 0 || to >= Items.Count)
        {
            return;
        }

        var item = Items[from];
        Items.RemoveAt(from);
        Items.Insert(to, item);
    }

    /// <summary><c>reorderArray</c>: the list changed in place, and a copy returned.</summary>
    private static IList<T> ReorderList<T>(IList<T> list, int from, int to)
    {
        var copy = new List<T>(list);
        if (from < 0 || from >= copy.Count || to < 0 || to >= copy.Count)
        {
            return copy;
        }

        var element = copy[from];
        copy.RemoveAt(from);
        copy.Insert(to, element);

        if (list.IsReadOnly || from == to)
        {
            return copy;
        }

        if (list is ObservableCollection<T> observable)
        {
            observable.Move(from, to);
        }
        else if (list is IList { IsFixedSize: true })
        {
            for (var i = 0; i < copy.Count; i++)
            {
                list[i] = copy[i];
            }
        }
        else
        {
            list.RemoveAt(from);
            list.Insert(to, element);
        }

        return copy;
    }

    private int ItemIndexForTop(double deltaY)
    {
        var heights = _cachedHeights;
        for (var i = 0; i < heights.Count; i++)
        {
            if (heights[i] > deltaY)
            {
                return i;
            }
        }

        return heights.Count - 1;
    }

    /// <summary>Each child between the item's place and its new one moves by the item's height, toward the gap.</summary>
    private void ReorderMove(int fromIndex, int toIndex)
    {
        var itemHeight = _selectedItemHeight;
        var children = Children;
        for (var i = 0; i < children.Count; i++)
        {
            var value = 0.0;
            if (i > fromIndex && i <= toIndex)
            {
                value = -itemHeight;
            }
            else if (i < fromIndex && i >= toIndex)
            {
                value = itemHeight;
            }

            Translate(children[i], value, animate: children[i] != _selectedItemEl);
        }
    }

    private double AutoScroll(double posY)
    {
        if (_scrollEl is not { } scrollEl)
        {
            return 0;
        }

        var amount = 0.0;
        if (posY < _scrollElTop)
        {
            amount = -ScrollJump;
        }
        else if (posY > _scrollElBottom)
        {
            amount = ScrollJump;
        }

        if (amount != 0)
        {
            // scrollBy, which a browser clamps to the extent.
            var max = Math.Max(0, scrollEl.Extent.Height - scrollEl.Viewport.Height);
            scrollEl.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(scrollEl.Offset.X, Math.Clamp(scrollEl.Offset.Y + amount, 0, max)));
        }

        return scrollEl.Offset.Y - _scrollElInitial;
    }

    // ── What the drag shows ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// The <c>reorder-selected</c> class goes on the element the DOM has, which is a template's presenter's child: an
    /// <see cref="ItemsControl"/>'s wrapper isn't in the DOM. Its z-index goes on the child of the panel, as that's what
    /// <c>ZIndex</c> sorts.
    /// </summary>
    private void SetSelected(Control child, bool selected)
    {
        var element = child is ContentPresenter { TemplatedParent: null, Child: { } inner } ? inner : child;
        ((IPseudoClasses)element.Classes).Set(SelectedPseudoClass, selected);
        _selectedZIndex?.Dispose();
        _selectedZIndex = selected ? child.SetValue(ZIndexProperty, SelectedZIndex, BindingPriority.Style) : null;
    }

    /// <summary>
    /// <c>style.transform = translateY(…)</c>, which beats the app's stylesheet as an inline style does: a transform at animation
    /// priority, which gives the app's own value back when it's cleared.
    /// </summary>
    private void Translate(Control child, double y, bool animate)
    {
        if (!_shifts.TryGetValue(child, out var shift))
        {
            if (y == 0)
            {
                return;
            }

            var transform = new TranslateTransform();
            shift = new Shift(transform, child.SetValue(RenderTransformProperty, transform, BindingPriority.Animation));
            _shifts[child] = shift;
        }

        var now = AvonicConfig.Now();
        if (animate)
        {
            shift.Transition.Start(y, now, TransitionDuration);
            shift.Transform.Y = shift.Transition.At(now);
            if (shift.Transition.IsRunning(now))
            {
                _tick ??= AvonicConfig.RequestFrame(this, Tick);
            }
        }
        else
        {
            shift.Transition.Snap(y);
            shift.Transform.Y = y;
        }
    }

    private void Tick()
    {
        _tick = null;
        var now = AvonicConfig.Now();
        var running = false;
        foreach (var shift in _shifts.Values)
        {
            shift.Transform.Y = shift.Transition.At(now);
            running |= shift.Transition.IsRunning(now);
        }

        if (running)
        {
            _tick = AvonicConfig.RequestFrame(this, Tick);
        }
    }

    /// <summary><c>style.transform = ''</c>.</summary>
    private void ClearTranslate(Control child)
    {
        if (_shifts.Remove(child, out var shift))
        {
            shift.Frame?.Dispose();
        }

        if (_shifts.Count == 0)
        {
            _tick?.Dispose();
            _tick = null;
        }
    }

    private sealed class Shift(TranslateTransform transform, IDisposable? frame)
    {
        public TranslateTransform Transform { get; } = transform;

        public IDisposable? Frame { get; } = frame;

        public ReorderTransition Transition { get; } = new();
    }
}
