using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-select-option</c>: one choice of an <see cref="AvonicSelect"/>, in its <see cref="AvonicSelect.Items"/>. Data the
/// select reads, as Ionic's host is <c>display: none</c>: it's never drawn, and the select's interfaces show its text. See
/// <c>docs/components/ion-select/README.md</c>.
/// </summary>
/// <remarks>
/// <c>disabled</c> is <see cref="InputElement.IsEnabled"/>, its own, as Ionic reads <c>option.disabled</c>. The <c>start</c> and
/// <c>end</c> slots aren't here: Ionic shows them only with <c>innerHTMLTemplatesEnabled</c>, off by default (see the plan).
/// </remarks>
public partial class AvonicSelectOption : Control
{
    /// <summary>Ionic <c>value</c>. Null is the option's text, as <c>getOptionValue</c> reads an option with none.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicSelectOption, object?>(nameof(Value));

    /// <summary>Ionic <c>description</c>: a line under the text in the popover and the modal.</summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<AvonicSelectOption, string?>(nameof(Description));

    /// <summary>Ionic <c>labelPlacement</c>: the label beside the option's radio or checkbox. Null is md's <c>end</c>.</summary>
    public static readonly StyledProperty<SelectOptionLabelPlacement?> LabelPlacementProperty =
        AvaloniaProperty.Register<AvonicSelectOption, SelectOptionLabelPlacement?>(nameof(LabelPlacement));

    /// <summary>Ionic <c>justify</c>: how the label and the option's control share a line. Null is md's <c>start</c>.</summary>
    public static readonly StyledProperty<SelectOptionJustify?> JustifyProperty =
        AvaloniaProperty.Register<AvonicSelectOption, SelectOptionJustify?>(nameof(Justify));

    /// <summary>Ionic's default slot: the option's text, a string, or controls whose text is read.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<AvonicSelectOption, object?>(nameof(Content));

    /// <summary>Collapsible white space, as <c>getDefaultSlotPlainText</c> collapses it. NBSP isn't.</summary>
    [GeneratedRegex("[ \t\n\r\f]+")]
    private static partial Regex CollapsibleSpace();

    static AvonicSelectOption()
    {
        // display: none.
        IsVisibleProperty.OverrideDefaultValue<AvonicSelectOption>(false);
        FocusableProperty.OverrideDefaultValue<AvonicSelectOption>(false);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public SelectOptionLabelPlacement? LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public SelectOptionJustify? Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Ionic's watcher on the options: one of them changed what the select shows of it.</summary>
    internal event Action<AvonicSelectOption>? Changed;

    /// <summary><c>getDefaultSlotPlainText</c>: the text of the default slot, white space collapsed and trimmed.</summary>
    internal string Text => CollapsibleSpace().Replace(TextOf(Content), " ").Trim(' ', '\t', '\n', '\r', '\f');

    /// <summary><c>getOptionValue</c>: the value, or the text when there's none.</summary>
    internal object? OptionValue => Value ?? Text;

    /// <summary>The text a node renders, as <c>getRenderedTextContent</c>: concatenated with no separator.</summary>
    internal static string TextOf(object? content) => content switch
    {
        null => "",
        string text => text,
        TextBlock block => block.Inlines is { Count: > 0 } inlines ? inlines.Text ?? "" : block.Text ?? "",
        Run run => run.Text ?? "",
        Span span => string.Concat(span.Inlines.Select(TextOf)),
        ContentControl control => TextOf(control.Content),
        ILogical logical => string.Concat(logical.LogicalChildren.Select(TextOf)),
        _ => content.ToString() ?? "",
    };

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            // The default slot's controls are the option's, as a light DOM child is the element's.
            if (change.OldValue is ILogical old)
            {
                LogicalChildren.Remove(old);
            }

            if (change.NewValue is ILogical now)
            {
                LogicalChildren.Add(now);
            }
        }

        if (change.Property == ValueProperty || change.Property == ContentProperty || change.Property == DescriptionProperty
            || change.Property == IsEnabledProperty || change.Property == LabelPlacementProperty || change.Property == JustifyProperty)
        {
            Changed?.Invoke(this);
        }
    }

    /// <summary>Never drawn, so nothing for a screen reader: <c>display: none</c> takes it out of the tree.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);
}
