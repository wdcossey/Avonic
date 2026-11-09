using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-grid</c>: a block of <see cref="AvonicRow"/>s, with padding, and a fixed width per breakpoint if
/// <see cref="Fixed"/>. Both come from the theme, from the viewport's breakpoint, which is the <see cref="TopLevel"/>'s width.
/// </summary>
[PseudoClasses(FixedPseudoClass, ":breakpoint-sm", ":breakpoint-md", ":breakpoint-lg", ":breakpoint-xl")]
public class AvonicGrid : ItemsControl
{
    /// <summary><c>grid-fixed</c>.</summary>
    public const string FixedPseudoClass = ":fixed";

    /// <summary>The style class whose <c>:host(.ion-no-padding)</c> rule takes the padding off the grid's columns.</summary>
    public const string NoPaddingClass = "ion-no-padding";

    /// <summary>Ionic <c>fixed</c>: a fixed width based on the screen size.</summary>
    public static readonly StyledProperty<bool> FixedProperty =
        AvaloniaProperty.Register<AvonicGrid, bool>(nameof(Fixed));

    /// <summary>The six <c>--ion-grid-column-padding*</c> variables, which <c>:host(.ion-no-padding)</c> sets to 0.</summary>
    private static readonly string[] ColumnPaddingKeys =
    [
        "Avonic.GridColumnPadding", "Avonic.GridColumnPaddingXs", "Avonic.GridColumnPaddingSm",
        "Avonic.GridColumnPaddingMd", "Avonic.GridColumnPaddingLg", "Avonic.GridColumnPaddingXl",
    ];

    private GridViewport? _viewport;
    private ResourceDictionary? _noPadding;

    static AvonicGrid()
    {
        // overflow: visible. A TemplatedControl clips otherwise.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicGrid>(false);
    }

    public AvonicGrid()
    {
        Classes.CollectionChanged += OnClassesChanged;
    }

    public bool Fixed
    {
        get => GetValue(FixedProperty);
        set => SetValue(FixedProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FixedProperty)
        {
            PseudoClasses.Set(FixedPseudoClass, Fixed);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _viewport = GridViewport.Of(this);
        if (_viewport is not null)
        {
            _viewport.Changed += UpdateBreakpoint;
        }

        UpdateBreakpoint();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_viewport is not null)
        {
            _viewport.Changed -= UpdateBreakpoint;
            _viewport = null;
        }
    }

    /// <summary>An <see cref="ItemsControl"/> moves focus with the arrow keys. A grid doesn't.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
    }

    /// <summary>No <c>role</c>: a <c>div</c>, not a list.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);

    private void UpdateBreakpoint() => GridViewport.SetPseudoClasses(PseudoClasses, _viewport?.Breakpoint ?? 0);

    /// <summary>
    /// <c>:host(.ion-no-padding)</c> sets the column padding variables to 0, and they inherit to every column inside, a nested
    /// grid's too. Resources do the same here. They're merged, so an app's own key on this grid still wins, as an app's style
    /// beats a <c>:host</c> rule. The grid's own padding is <c>padding.css</c>'s, in the theme.
    /// </summary>
    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var noPadding = Classes.Contains(NoPaddingClass);
        if (noPadding == _noPadding is not null)
        {
            return;
        }

        if (noPadding)
        {
            _noPadding = [];
            foreach (var key in ColumnPaddingKeys)
            {
                _noPadding[key] = 0d;
            }

            Resources.MergedDictionaries.Add(_noPadding);
        }
        else
        {
            Resources.MergedDictionaries.Remove(_noPadding!);
            _noPadding = null;
        }
    }
}
