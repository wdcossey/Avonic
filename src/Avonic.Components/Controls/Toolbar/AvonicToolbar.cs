using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-toolbar</c>: a row of slots, usually in an <see cref="AvonicHeader"/> or <see cref="AvonicFooter"/>. The look,
/// and md's order of the slots, are in the theme. See <c>docs/components/ion-toolbar/README.md</c>.
/// </summary>
[TemplatePart("PART_Background", typeof(Border))]
[TemplatePart("PART_Container", typeof(Border))]
[TemplatePart("PART_Content", typeof(Border))]
[TemplatePart("PART_Start", typeof(ContentPresenter))]
[TemplatePart("PART_Secondary", typeof(ContentPresenter))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_Primary", typeof(ContentPresenter))]
[TemplatePart("PART_End", typeof(ContentPresenter))]
public class AvonicToolbar : TemplatedControl
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicToolbar>();

    /// <summary>Ionic's default slot, between the named ones.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<AvonicToolbar, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<AvonicToolbar, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Ionic's <c>start</c> slot: the left in LTR, the right in RTL.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicToolbar, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicToolbar, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic's <c>secondary</c> slot: straight after the default slot in md.</summary>
    public static readonly StyledProperty<object?> SecondaryContentProperty =
        AvaloniaProperty.Register<AvonicToolbar, object?>(nameof(SecondaryContent));

    public static readonly StyledProperty<IDataTemplate?> SecondaryContentTemplateProperty =
        AvaloniaProperty.Register<AvonicToolbar, IDataTemplate?>(nameof(SecondaryContentTemplate));

    /// <summary>Ionic's <c>primary</c> slot: after the secondary slot in md.</summary>
    public static readonly StyledProperty<object?> PrimaryContentProperty =
        AvaloniaProperty.Register<AvonicToolbar, object?>(nameof(PrimaryContent));

    public static readonly StyledProperty<IDataTemplate?> PrimaryContentTemplateProperty =
        AvaloniaProperty.Register<AvonicToolbar, IDataTemplate?>(nameof(PrimaryContentTemplate));

    /// <summary>Ionic's <c>end</c> slot: the right in LTR, the left in RTL.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicToolbar, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicToolbar, IDataTemplate?>(nameof(EndContentTemplate));

    private AvonicButtons? _first;
    private AvonicButtons? _last;

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    [Avalonia.Metadata.Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public object? StartContent
    {
        get => GetValue(StartContentProperty);
        set => SetValue(StartContentProperty, value);
    }

    public IDataTemplate? StartContentTemplate
    {
        get => GetValue(StartContentTemplateProperty);
        set => SetValue(StartContentTemplateProperty, value);
    }

    public object? SecondaryContent
    {
        get => GetValue(SecondaryContentProperty);
        set => SetValue(SecondaryContentProperty, value);
    }

    public IDataTemplate? SecondaryContentTemplate
    {
        get => GetValue(SecondaryContentTemplateProperty);
        set => SetValue(SecondaryContentTemplateProperty, value);
    }

    public object? PrimaryContent
    {
        get => GetValue(PrimaryContentProperty);
        set => SetValue(PrimaryContentProperty, value);
    }

    public IDataTemplate? PrimaryContentTemplate
    {
        get => GetValue(PrimaryContentTemplateProperty);
        set => SetValue(PrimaryContentTemplateProperty, value);
    }

    public object? EndContent
    {
        get => GetValue(EndContentProperty);
        set => SetValue(EndContentProperty, value);
    }

    public IDataTemplate? EndContentTemplate
    {
        get => GetValue(EndContentTemplateProperty);
        set => SetValue(EndContentTemplateProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StartContentProperty || change.Property == SecondaryContentProperty
            || change.Property == PrimaryContentProperty || change.Property == EndContentProperty)
        {
            UpdateButtonsSlots();
        }
    }

    protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        UpdateButtonsSlots();
    }

    /// <summary>
    /// <c>componentWillLoad</c>: <c>buttons-first-slot</c> on the first <c>ion-buttons</c> in the start slot, and
    /// <c>buttons-last-slot</c> on the last in the end slot, or else primary, or else secondary.
    /// </summary>
    private void UpdateButtonsSlots()
    {
        var first = InSlot(StartContent).FirstOrDefault();
        var last = InSlot(EndContent).LastOrDefault()
            ?? InSlot(PrimaryContent).LastOrDefault()
            ?? InSlot(SecondaryContent).LastOrDefault();

        if (!ReferenceEquals(first, _first))
        {
            _first?.SetSlotClass(":buttons-first-slot", false);
            _first = first;
        }

        if (!ReferenceEquals(last, _last))
        {
            _last?.SetSlotClass(":buttons-last-slot", false);
            _last = last;
        }

        _first?.SetSlotClass(":buttons-first-slot", true);
        _last?.SetSlotClass(":buttons-last-slot", true);
    }

    /// <summary>What has <c>slot="…"</c>: the slot's content, or what a panel there holds, as a panel stands for the slot.</summary>
    private static IEnumerable<AvonicButtons> InSlot(object? content) => content switch
    {
        AvonicButtons buttons => [buttons],
        Panel panel => panel.Children.OfType<AvonicButtons>(),
        _ => [],
    };
}
