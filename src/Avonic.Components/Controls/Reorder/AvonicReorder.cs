using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-reorder</c>: the handle that drags a child of an <see cref="AvonicReorderGroup"/>. Its content replaces the icon, and
/// can be the item itself, which is then dragged from anywhere. In an item's start or end slot it shows only inside an enabled
/// group.
/// </summary>
public class AvonicReorder : ContentControl
{
    private static Cursor? s_grab;

    private readonly StyleValue<bool> _hidden = new();
    private readonly StyleValue<Cursor?> _cursor = new();
    private readonly List<AvonicReorderGroup> _groups = [];

    public AvonicReorder()
    {
        Bind(IsVisibleProperty, _hidden, BindingPriority.Style);
        Bind(CursorProperty, _cursor, BindingPriority.Style);

        // onClick, a capture listener: stopImmediatePropagation, unless a disabled group is around it. A handled press is no click to
        // an item (NativeButton) or a Button, inside or around this. The group's gesture and tap-click listen to handled presses too.
        AddHandler(PointerPressedEvent, OnPressedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <summary><c>cursor: grab</c>. Avalonia has no grab cursor; the hand is the nearest.</summary>
    private static Cursor Grab => s_grab ??= new Cursor(StandardCursorType.Hand);

    private void OnPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        // el.closest('ion-reorder-group')
        if (this.FindAncestorOfType<AvonicReorderGroup>() is not { Disabled: true })
        {
            e.Handled = true;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        foreach (var group in this.GetVisualAncestors().OfType<AvonicReorderGroup>())
        {
            _groups.Add(group);
            group.DisabledChanged += OnGroupDisabledChanged;
        }

        Update();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        foreach (var group in _groups)
        {
            group.DisabledChanged -= OnGroupDisabledChanged;
        }

        _groups.Clear();
    }

    private void OnGroupDisabledChanged(object? sender, EventArgs e) => Update();

    /// <summary>
    /// <c>:host([slot]) { display: none }</c>, and <c>.reorder-enabled ion-reorder { display: block; cursor: grab }</c>, for a group
    /// at any depth around it. A slot is an item's start or end, as <c>slot="start"</c> is.
    /// </summary>
    private void Update()
    {
        var slotted = Parent is ContentPresenter { Name: "PART_Start" or "PART_End" };
        var enabled = _groups.Exists(group => !group.Disabled);
        _hidden.SetWhen(slotted && !enabled, false);
        _cursor.SetWhen(enabled, Grab);
    }
}
