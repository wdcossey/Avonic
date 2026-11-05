using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.LogicalTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-label</c>: text that tells the <see cref="AvonicItem"/> around it how it's placed. Its look
/// mostly depends on that item, so it's in the theme's <c>Styles/Label.axaml</c>.
/// </summary>
[PseudoClasses(":position-fixed", ":position-stacked", ":position-floating", ":label-no-animate")]
public class AvonicLabel : ContentControl
{
    /// <summary><c>componentDidLoad</c>'s timeout.</summary>
    internal static readonly TimeSpan NoAnimateDuration = TimeSpan.FromMilliseconds(1000);

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicLabel>();

    /// <summary>Ionic <c>position</c>: where and how the label behaves inside an item. Null is the default.</summary>
    public static readonly StyledProperty<LabelPosition?> PositionProperty =
        AvaloniaProperty.Register<AvonicLabel, LabelPosition?>(nameof(Position));

    private bool _attached;
    private bool _loaded;
    private IDisposable? _noAnimateTimer;

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public LabelPosition? Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PositionProperty)
        {
            var position = Position;
            PseudoClasses.Set(":position-fixed", position == LabelPosition.Fixed);
            PseudoClasses.Set(":position-stacked", position == LabelPosition.Stacked);
            PseudoClasses.Set(":position-floating", position == LabelPosition.Floating);

            if (_attached)
            {
                EmitStyle();
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;

        // componentWillLoad and componentDidLoad, which only happen once.
        if (!_loaded)
        {
            _loaded = true;
            if (Position == LabelPosition.Floating)
            {
                PseudoClasses.Set(":label-no-animate", true);
                _noAnimateTimer = AvonicConfig.Schedule(ClearNoAnimate, NoAnimateDuration);
            }
        }

        EmitStyle();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        ClearNoAnimate();
    }

    private void ClearNoAnimate()
    {
        _noAnimateTimer?.Dispose();
        _noAnimateTimer = null;
        PseudoClasses.Set(":label-no-animate", false);
    }

    /// <summary>
    /// <c>emitStyle</c>. Inside a range it doesn't, so it doesn't override the label that's a direct child of
    /// the item.
    /// </summary>
    private void EmitStyle()
    {
        if (this.GetLogicalAncestors().OfType<IItemControl>().Any(control => control.ItemControlType == ItemControlType.Range))
        {
            return;
        }

        var styles = new Dictionary<string, bool> { ["label"] = true };
        if (Position is { } position)
        {
            styles["label-" + position.ToString().ToLowerInvariant()] = true;
        }

        RaiseEvent(new IonStyleEventArgs(styles));
    }
}
