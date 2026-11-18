using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>ion-select-popover</c> (<c>@internal</c>): what an <see cref="AvonicSelect"/> with the <c>popover</c> interface puts
/// in its <see cref="AvonicPopover"/>. A list, with a header, a sub header and a message, and an item per option with a radio, or
/// a checkbox when <see cref="Multiple"/>. Public, so that a theme can style it. See <c>docs/components/ion-select/README.md</c>.
/// </summary>
[TemplatePart("PART_List", typeof(ItemsControl))]
public class AvonicSelectPopover : TemplatedControl
{
    /// <summary>Ionic <c>header</c>: an <c>ion-list-header</c> above the options.</summary>
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<AvonicSelectPopover, string?>(nameof(Header));

    /// <summary>Ionic <c>subHeader</c>: an <c>h3</c> in an item above the options.</summary>
    public static readonly StyledProperty<string?> SubHeaderProperty =
        AvaloniaProperty.Register<AvonicSelectPopover, string?>(nameof(SubHeader));

    /// <summary>Ionic <c>message</c>: a <c>p</c> in the same item.</summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<AvonicSelectPopover, string?>(nameof(Message));

    /// <summary>Ionic <c>multiple</c>: checkboxes when true, radios otherwise.</summary>
    public static readonly StyledProperty<bool?> MultipleProperty =
        AvaloniaProperty.Register<AvonicSelectPopover, bool?>(nameof(Multiple));

    /// <summary>Ionic <c>options</c>.</summary>
    public static readonly StyledProperty<IList<SelectOverlayOption>?> OptionsProperty =
        AvaloniaProperty.Register<AvonicSelectPopover, IList<SelectOverlayOption>?>(nameof(Options));

    private readonly SelectOptionList _options;
    private ItemsControl? _list;

    public AvonicSelectPopover()
    {
        _options = new SelectOptionList(this, DismissParentPopover, linesNone: false);
    }

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? SubHeader
    {
        get => GetValue(SubHeaderProperty);
        set => SetValue(SubHeaderProperty, value);
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool? Multiple
    {
        get => GetValue(MultipleProperty);
        set => SetValue(MultipleProperty, value);
    }

    public IList<SelectOverlayOption>? Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    internal SelectOptionList OptionList => _options;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _list = e.NameScope.Find<ItemsControl>("PART_List");
        Render();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OptionsProperty)
        {
            if (change.OldValue is INotifyCollectionChanged old)
            {
                old.CollectionChanged -= OnOptionsChanged;
            }

            if (change.NewValue is INotifyCollectionChanged now)
            {
                now.CollectionChanged += OnOptionsChanged;
            }
        }

        if (change.Property == HeaderProperty || change.Property == SubHeaderProperty || change.Property == MessageProperty
            || change.Property == MultipleProperty || change.Property == OptionsProperty)
        {
            Render();
        }
    }

    private void OnOptionsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Render();

    /// <summary>
    /// <c>render</c>: the list header with a header; an item of the sub header and message when either is set; then the options.
    /// </summary>
    private void Render()
    {
        if (_list is null)
        {
            return;
        }

        _options.RememberFocus();
        _list.Items.Clear();
        if (Header is { } header)
        {
            _list.Items.Add(new AvonicListHeader { Content = header });
        }

        if (SubHeader is not null || Message is not null)
        {
            var text = new StackPanel();
            if (SubHeader is { } subHeader)
            {
                var h3 = new TextBlock { Text = subHeader };
                h3.Classes.Add("h3");
                text.Children.Add(h3);
            }

            if (Message is { } message)
            {
                var p = new TextBlock { Text = message };
                p.Classes.Add("p");
                text.Children.Add(p);
            }

            var label = new AvonicLabel { Content = text };
            label.Classes.Add("ion-text-wrap");
            _list.Items.Add(new AvonicItem { Content = label });
        }

        foreach (var control in _options.Render(Options, Multiple == true))
        {
            _list.Items.Add(control);
        }
    }

    /// <summary><c>dismissParentPopover</c>: <c>closest('ion-popover').dismiss()</c>.</summary>
    private void DismissParentPopover()
    {
        var popover = this.GetLogicalAncestors().OfType<AvonicPopover>().FirstOrDefault()
                      ?? this.GetVisualAncestors().OfType<AvonicPopover>().FirstOrDefault();
        _ = popover?.Dismiss();
    }
}
