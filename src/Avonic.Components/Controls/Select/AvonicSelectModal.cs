using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-select-modal</c>: what an <see cref="AvonicSelect"/> with the <c>modal</c> interface puts in its
/// <see cref="AvonicModal"/>. A header with a toolbar of the title and a close button, and a list of an item per option with a radio,
/// or a checkbox when <see cref="Multiple"/>. See <c>docs/components/ion-select/README.md</c>.
/// </summary>
[TemplatePart("PART_List", typeof(ItemsControl))]
[TemplatePart("PART_Cancel", typeof(Control))]
public class AvonicSelectModal : TemplatedControl
{
    /// <summary>Ionic <c>cancelText</c>: the close button's text.</summary>
    public static readonly StyledProperty<string> CancelTextProperty =
        AvaloniaProperty.Register<AvonicSelectModal, string>(nameof(CancelText), "Close");

    /// <summary>Ionic <c>header</c>: the toolbar's <c>ion-title</c>, when not null.</summary>
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<AvonicSelectModal, string?>(nameof(Header));

    /// <summary>Ionic <c>multiple</c>: checkboxes when true, radios otherwise.</summary>
    public static readonly StyledProperty<bool?> MultipleProperty =
        AvaloniaProperty.Register<AvonicSelectModal, bool?>(nameof(Multiple));

    /// <summary>Ionic <c>options</c>.</summary>
    public static readonly StyledProperty<IList<SelectOverlayOption>?> OptionsProperty =
        AvaloniaProperty.Register<AvonicSelectModal, IList<SelectOverlayOption>?>(nameof(Options));

    private readonly SelectOptionList _options;
    private ItemsControl? _list;
    private Control? _cancel;

    public AvonicSelectModal()
    {
        _options = new SelectOptionList(this, CloseModal, linesNone: true);
    }

    public string CancelText
    {
        get => GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
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

        _cancel?.RemoveHandler(AvonicButton.ClickEvent, OnCancelClick);
        _cancel = e.NameScope.Find<Control>("PART_Cancel");
        _cancel?.AddHandler(AvonicButton.ClickEvent, OnCancelClick);

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

        if (change.Property == MultipleProperty || change.Property == OptionsProperty)
        {
            Render();
        }
    }

    private void OnOptionsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Render();

    /// <summary><c>render</c>'s list: checkboxes when <c>multiple === true</c>, radios otherwise.</summary>
    private void Render()
    {
        if (_list is null)
        {
            return;
        }

        _options.RememberFocus();
        _list.Items.Clear();
        foreach (var control in _options.Render(Options, Multiple == true))
        {
            _list.Items.Add(control);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => CloseModal();

    /// <summary><c>closeModal</c>: <c>closest('ion-modal').dismiss()</c>, with no role.</summary>
    private void CloseModal()
    {
        var modal = this.GetLogicalAncestors().OfType<AvonicModal>().FirstOrDefault()
                    ?? this.GetVisualAncestors().OfType<AvonicModal>().FirstOrDefault();
        _ = modal?.Dismiss();
    }
}
