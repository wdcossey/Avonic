using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-fab-list</c>: the buttons an <see cref="AvonicFab"/> opens beside its own. The look is in the theme. See
/// <c>docs/components/ion-fab/README.md</c>.
/// </summary>
/// <remarks>
/// Its buttons are <see cref="ItemsControl.Items"/>, as the fab's children are, and are laid out by <see cref="FabListPanel"/>.
/// </remarks>
[PseudoClasses(":active", ":side-bottom", ":side-top", ":side-start", ":side-end")]
public class AvonicFabList : ItemsControl
{
    /// <summary>Ionic <c>activated</c>: the list is shown, and its buttons appear one by one.</summary>
    public static readonly StyledProperty<bool> ActivatedProperty =
        AvaloniaProperty.Register<AvonicFabList, bool>(nameof(Activated));

    /// <summary>Ionic <c>side</c>: where the list opens, relative to the fab's button.</summary>
    public static readonly StyledProperty<FabListSide> SideProperty =
        AvaloniaProperty.Register<AvonicFabList, FabListSide>(nameof(Side), FabListSide.Bottom);

    /// <summary>
    /// For the theme: <c>::slotted(.fab-button-in-list)</c>'s margin, along the list. The value is the list's <see cref="Side"/>
    /// and the parameter the margin, <c>5</c>: top and bottom in a column, start and end in a row.
    /// </summary>
    public static readonly IValueConverter ButtonMarginConverter = new FabListButtonMarginConverter();

    /// <summary><c>fab-list.css</c>'s 30 ms between one button's <c>show</c> and the next's.</summary>
    internal static readonly TimeSpan ShowInterval = TimeSpan.FromMilliseconds(30);

    private readonly List<IDisposable> _timeouts = [];
    private readonly FabStyleValue<bool> _hidden = new(false);

    static AvonicFabList()
    {
        // The buttons' shadows reach outside the list. A TemplatedControl clips by default.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicFabList>(false);
    }

    public AvonicFabList()
    {
        // display: none until .fab-list-active. In the control, so it survives a replaced theme, and at style priority, so that an
        // app's own value wins.
        Bind(IsVisibleProperty, _hidden, BindingPriority.Style);
        _hidden.Set(true);
        UpdateSide();
    }

    public bool Activated
    {
        get => GetValue(ActivatedProperty);
        set => SetValue(ActivatedProperty, value);
    }

    public FabListSide Side
    {
        get => GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ActivatedProperty)
        {
            var activated = change.GetNewValue<bool>();
            PseudoClasses.Set(":active", activated);
            _hidden.Set(!activated);
            ActivatedChanged(activated);
        }
        else if (change.Property == SideProperty)
        {
            UpdateSide();
            ItemsPanelRoot?.InvalidateMeasure();

            // The fab places the list from its side.
            (this.GetVisualParent() as FabPanel)?.InvalidateArrange();
        }
    }

    /// <summary>
    /// <c>activatedChanged</c>: each fab button inside, at any depth, shows 30 ms after the one before, from 0, or all hide after
    /// 0 ms. What was still pending from the last change is cancelled.
    /// </summary>
    private void ActivatedChanged(bool activated)
    {
        ClearTimeouts();
        var interval = activated ? ShowInterval : TimeSpan.Zero;
        var index = 0;
        foreach (var button in this.GetLogicalDescendants().OfType<AvonicFabButton>().ToList())
        {
            _timeouts.Add(AvonicConfig.Schedule(() => button.SetCurrentValue(AvonicFabButton.ShowProperty, activated), interval * index));
            index++;
        }
    }

    /// <summary><c>disconnectedCallback</c>.</summary>
    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        ClearTimeouts();
    }

    private void ClearTimeouts()
    {
        foreach (var timeout in _timeouts)
        {
            timeout.Dispose();
        }

        _timeouts.Clear();
    }

    private void UpdateSide()
    {
        var side = Side;
        PseudoClasses.Set(":side-bottom", side == FabListSide.Bottom);
        PseudoClasses.Set(":side-top", side == FabListSide.Top);
        PseudoClasses.Set(":side-start", side == FabListSide.Start);
        PseudoClasses.Set(":side-end", side == FabListSide.End);
    }

    /// <summary>A control is its own container, as a child in the light DOM is. Anything else is shown in a presenter.</summary>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Control>(item, out recycleKey);

    /// <summary>An <see cref="ItemsControl"/> moves focus with the arrow keys. A fab list doesn't: Tab does.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
    }

    /// <summary>A plain element, where an <see cref="ItemsControl"/>'s own peer would call it a list.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new AvonicFab.FabGroupPeer(this);

    private sealed class FabListButtonMarginConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not FabListSide side || parameter is not string text)
            {
                return AvaloniaProperty.UnsetValue;
            }

            var margin = double.Parse(text, CultureInfo.InvariantCulture);
            return side is FabListSide.Top or FabListSide.Bottom ? new Thickness(0, margin) : new Thickness(margin, 0);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
