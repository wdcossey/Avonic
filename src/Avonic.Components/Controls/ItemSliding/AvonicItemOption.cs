using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-item-option</c>: a button in an <see cref="AvonicItemOptions"/>, shown when its <see cref="AvonicItemSliding"/>
/// slides open. The look is in the theme. See <c>docs/components/ion-item-sliding/README.md</c>.
/// </summary>
/// <remarks>
/// Six slots, so a <see cref="TemplatedControl"/> with a <see cref="ContentPresenter"/> for each. A native <c>&lt;button&gt;</c>:
/// <see cref="Click"/> on release, Enter and Space, through <see cref="NativeButton"/>, as on <see cref="AvonicFabButton"/>.
/// </remarks>
[TemplatePart("PART_Native", typeof(Border))]
[TemplatePart("PART_Inner", typeof(Panel))]
[TemplatePart("PART_Container", typeof(Panel))]
[TemplatePart("PART_Expander", typeof(ItemOptionExpander))]
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(":expandable")]
public class AvonicItemOption : TemplatedControl
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicItemOption>();

    /// <summary>Ionic <c>expandable</c>: on a full swipe, it grows over the whole row.</summary>
    public static readonly StyledProperty<bool> ExpandableProperty =
        AvaloniaProperty.Register<AvonicItemOption, bool>(nameof(Expandable));

    /// <summary>Ionic's default slot, between the start and end slots.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        ContentControl.ContentProperty.AddOwner<AvonicItemOption>();

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        ContentControl.ContentTemplateProperty.AddOwner<AvonicItemOption>();

    /// <summary>Ionic slot <c>top</c>: above the text.</summary>
    public static readonly StyledProperty<object?> TopContentProperty =
        AvaloniaProperty.Register<AvonicItemOption, object?>(nameof(TopContent));

    public static readonly StyledProperty<IDataTemplate?> TopContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItemOption, IDataTemplate?>(nameof(TopContentTemplate));

    /// <summary>Ionic slot <c>bottom</c>: below the text.</summary>
    public static readonly StyledProperty<object?> BottomContentProperty =
        AvaloniaProperty.Register<AvonicItemOption, object?>(nameof(BottomContent));

    public static readonly StyledProperty<IDataTemplate?> BottomContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItemOption, IDataTemplate?>(nameof(BottomContentTemplate));

    /// <summary>Ionic slot <c>start</c>: before the text.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicItemOption, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItemOption, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic slot <c>end</c>: after the text.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicItemOption, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItemOption, IDataTemplate?>(nameof(EndContentTemplate));

    /// <summary>Ionic slot <c>icon-only</c>: an icon in an option with no text.</summary>
    public static readonly StyledProperty<object?> IconOnlyContentProperty =
        AvaloniaProperty.Register<AvonicItemOption, object?>(nameof(IconOnlyContent));

    public static readonly StyledProperty<IDataTemplate?> IconOnlyContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItemOption, IDataTemplate?>(nameof(IconOnlyContentTemplate));

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool Expandable
    {
        get => GetValue(ExpandableProperty);
        set => SetValue(ExpandableProperty, value);
    }

    [Content]
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

    public object? TopContent
    {
        get => GetValue(TopContentProperty);
        set => SetValue(TopContentProperty, value);
    }

    public IDataTemplate? TopContentTemplate
    {
        get => GetValue(TopContentTemplateProperty);
        set => SetValue(TopContentTemplateProperty, value);
    }

    public object? BottomContent
    {
        get => GetValue(BottomContentProperty);
        set => SetValue(BottomContentProperty, value);
    }

    public IDataTemplate? BottomContentTemplate
    {
        get => GetValue(BottomContentTemplateProperty);
        set => SetValue(BottomContentTemplateProperty, value);
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

    public object? IconOnlyContent
    {
        get => GetValue(IconOnlyContentProperty);
        set => SetValue(IconOnlyContentProperty, value);
    }

    public IDataTemplate? IconOnlyContentTemplate
    {
        get => GetValue(IconOnlyContentTemplateProperty);
        set => SetValue(IconOnlyContentTemplateProperty, value);
    }

    // ── Extensions ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Avonic extension: the option was clicked, by a tap, Enter or Space, as its native <c>&lt;button&gt;</c> is in Ionic, whose
    /// <c>click</c> an app listens to.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<AvonicItemOption, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    /// <summary>
    /// Avonic extension: run after <see cref="Click"/>, unless a handler set <c>Handled</c>. While it can't execute, the option is
    /// disabled. As on <see cref="AvonicButton"/>.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvonicButton.CommandProperty.AddOwner<AvonicItemOption>();

    /// <summary>Avonic extension: the parameter for <see cref="Command"/>.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvonicButton.CommandParameterProperty.AddOwner<AvonicItemOption>();

    /// <summary>
    /// Avonic extension: CSS's <c>text-transform</c>, for text given as a string, as on <see cref="AvonicButton"/>. The md theme
    /// sets <see cref="ButtonTextTransform.Uppercase"/>.
    /// </summary>
    public static readonly StyledProperty<ButtonTextTransform> TextTransformProperty =
        AvonicButton.TextTransformProperty.AddOwner<AvonicItemOption>();

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public ButtonTextTransform TextTransform
    {
        get => GetValue(TextTransformProperty);
        set => SetValue(TextTransformProperty, value);
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private readonly NativeButton? _native;
    private ItemOptionExpander? _expander;

    static AvonicItemOption()
    {
        // A native <button> takes focus.
        FocusableProperty.OverrideDefaultValue<AvonicItemOption>(true);
    }

    public AvonicItemOption()
    {
        _native = new NativeButton(this, _ => OnNativeClick(), () => true, UpdateIsEffectivelyEnabled);

        // ion-activatable, always. Not ion-focusable: item-option.md.css has no focused state.
        Activatable.SetIsEnabled(this, true);
    }

    /// <summary>A command that can't execute disables the option. The base class's constructor reads this before there's a <see cref="NativeButton"/>.</summary>
    protected override bool IsEnabledCore => base.IsEnabledCore && _native?.CanExecute != false;

    /// <summary>
    /// <c>padding-left: 100%</c> (or right) on a full swipe, in pixels, which the options set: the width added on the inner side,
    /// <see cref="Thickness.Left"/> for end options and <see cref="Thickness.Right"/> for start ones, logical, so that RTL mirrors it.
    /// </summary>
    internal double Expansion { get; private set; }

    /// <summary>Whether <see cref="Expansion"/> goes before the content (end options) or after it (start options).</summary>
    internal bool ExpandsBefore { get; private set; }

    internal void SetExpansion(double expansion, bool before)
    {
        if (expansion == Expansion && before == ExpandsBefore)
        {
            return;
        }

        Expansion = expansion;
        ExpandsBefore = before;
        _expander?.InvalidateMeasure();
        InvalidateMeasure();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _expander = e.NameScope.Find<ItemOptionExpander>("PART_Expander");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ExpandableProperty)
        {
            PseudoClasses.Set(":expandable", change.GetNewValue<bool>());
        }
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary>The native button's click. Ionic's host <c>onClick</c> only calls <c>preventDefault</c>, for a link or a form.</summary>
    private void OnNativeClick()
    {
        if (!IsEffectivelyEnabled)
        {
            return;
        }

        _native?.Click(ClickEvent);
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new ItemOptionPeer(this);

    /// <summary>A native button, named by the text inside it.</summary>
    private sealed class ItemOptionPeer(AvonicItemOption owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override bool IsEnabledCore() => owner.IsEffectivelyEnabled;

        protected override string? GetNameCore()
        {
            var name = base.GetNameCore();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            // The flattened tree, in slot order: top, start, icon-only, the default slot, end, bottom.
            var parts = new[] { owner.TopContent, owner.StartContent, owner.IconOnlyContent, owner.Content, owner.EndContent, owner.BottomContent }
                .Select(content => content switch
                {
                    string s => s,
                    Control child => AccessibleText.Of(child),
                    _ => null,
                })
                .Where(text => !string.IsNullOrWhiteSpace(text));
            var joined = string.Join(" ", parts);
            return joined.Length > 0 ? joined : null;
        }

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        public void Invoke() => owner.OnNativeClick();
    }
}
