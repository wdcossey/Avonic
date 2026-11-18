using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// What <c>select-popover.js</c> and <c>select-modal.js</c> render the same way: an <c>ion-item</c> per option, with an
/// <c>ion-radio</c> in an <c>ion-radio-group</c>, or an <c>ion-checkbox</c> for a multiple select, and what each does when the user
/// chooses. The item rules of both <c>.md.css</c> files, word for word the same, are resource dictionaries in the theme that each
/// item merges, as a CSS variable set on it is a resource of its own.
/// </summary>
internal sealed class SelectOptionList
{
    /// <summary><c>.select-interface-option</c>'s item, every item: <c>--inner-border-width: 0</c>.</summary>
    internal const string ItemKey = "Avonic.SelectOption.Md.Item";

    /// <summary><c>.item-radio-checked</c>: the checked radio's item.</summary>
    internal const string ItemRadioCheckedKey = "Avonic.SelectOption.Md.ItemRadioChecked";

    /// <summary><c>.item-checkbox-checked</c>: a checked checkbox's item.</summary>
    internal const string ItemCheckboxCheckedKey = "Avonic.SelectOption.Md.ItemCheckboxChecked";

    internal const string RadioCheckedClass = "item-radio-checked";
    internal const string CheckboxCheckedClass = "item-checkbox-checked";
    internal const string LabelClass = "select-option-label";
    internal const string ContentClass = "select-option-content";
    internal const string DescriptionClass = "select-option-description";
    internal const string RichContentClass = "select-option-has-rich-content";

    private readonly Control _host;
    private readonly Action _dismiss;
    private readonly bool _linesNone;
    private readonly List<(SelectOverlayOption Option, AvonicItem Item, Control Control)> _rows = [];
    private AvonicRadioGroup? _group;
    private bool _multiple;
    private AvonicRadio? _pendingEnter;

    /// <param name="host">The popover's or the modal's content, which finds the theme's dictionaries.</param>
    /// <param name="dismiss">Dismisses the overlay around it: <c>dismissParentPopover</c>, <c>closeModal</c>.</param>
    /// <param name="linesNone">The modal's radio items have <c>lines="none"</c>.</param>
    public SelectOptionList(Control host, Action dismiss, bool linesNone)
    {
        _host = host;
        _dismiss = dismiss;
        _linesNone = linesNone;

        // The radio group's keys inside a select interface, ahead of the group's own (see OnKeyDown).
        host.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        host.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>An Enter that opened the select is still down: its repeats aren't presses here, as <c>ev.repeat</c> says.</summary>
    public bool IgnoreEnterUntilKeyUp { get; set; }

    /// <summary>The rows, in order: each option, its item, and its radio or checkbox.</summary>
    public IReadOnlyList<(SelectOverlayOption Option, AvonicItem Item, Control Control)> Rows => _rows;

    /// <summary>
    /// <c>renderOptions</c>: the items, in the radio group for radios, as what goes in the list after anything before them. The
    /// options' <c>checked</c> are read now; a checkbox's click updates its own.
    /// </summary>
    public List<Control> Render(IList<SelectOverlayOption>? options, bool multiple)
    {
        var rendered = new List<Control>();
        _rows.Clear();
        _multiple = multiple;
        _pendingEnter = null;
        if (_group is not null)
        {
            _group.IonChange -= OnGroupChange;
            _group.Items.Clear();
            _group = null;
        }

        var list = options ?? [];
        if (multiple)
        {
            foreach (var option in list)
            {
                rendered.Add(CheckboxItem(option));
            }

            return rendered;
        }

        // renderRadioOptions: the first checked option's value is the group's.
        var checkedValue = list.FirstOrDefault(option => option.Checked)?.Value;
        _group = new AvonicRadioGroup { Value = checkedValue };
        _group.IonChange += OnGroupChange;
        foreach (var option in list)
        {
            _group.Items.Add(RadioItem(option, checkedValue));
        }

        rendered.Add(_group);
        return rendered;
    }

    private AvonicItem RadioItem(SelectOverlayOption option, object? checkedValue)
    {
        var radio = new AvonicRadio
        {
            Value = option.Value,
            IsEnabled = !option.Disabled,
            Justify = (RadioJustify)(option.Justify ?? SelectOptionJustify.Start),
            LabelPlacement = option.LabelPlacement == SelectOptionLabelPlacement.Start ? RadioLabelPlacement.Start : RadioLabelPlacement.End,
            Content = Label(option),
        };
        if (HasRichContent(option))
        {
            radio.Classes.Add(RichContentClass);
        }

        var item = Item(option, radio);
        if (_linesNone)
        {
            item.Lines = ItemLines.None;
        }

        // 'item-radio-checked': option.value === checked, strictly.
        SetChecked(item, RadioCheckedClass, ItemRadioCheckedKey, checkedValue is not null && Equals(option.Value, checkedValue));

        // The radio's onClick: dismiss. A tap on the item's padding clicks the radio too, as the item's click() does, so the item
        // is where the click is heard; a disabled one hears none.
        _ = new NativeButton(item, _ => _dismiss(), () => false, static () => { });
        _rows.Add((option, item, radio));
        return item;
    }

    private AvonicItem CheckboxItem(SelectOverlayOption option)
    {
        var checkbox = new AvonicCheckbox
        {
            Value = option.Value,
            IsEnabled = !option.Disabled,
            Checked = option.Checked,
            Justify = (CheckboxJustify)(option.Justify ?? SelectOptionJustify.Start),
            LabelPlacement = option.LabelPlacement == SelectOptionLabelPlacement.Start ? CheckboxLabelPlacement.Start : CheckboxLabelPlacement.End,
            Content = Label(option),
        };
        if (HasRichContent(option))
        {
            checkbox.Classes.Add(RichContentClass);
        }

        var item = Item(option, checkbox);
        SetChecked(item, CheckboxCheckedClass, ItemCheckboxCheckedKey, option.Checked);

        checkbox.IonChange += (_, e) =>
        {
            // setChecked, callOptionHandler with every checked value, then forceUpdate for the item's class.
            option.Checked = e.Checked;
            CallHandler(option, _rows.Where(row => row.Option.Checked).Select(row => row.Option.Value).ToArray());
            SetChecked(item, CheckboxCheckedClass, ItemCheckboxCheckedKey, option.Checked);
        };
        _rows.Add((option, item, checkbox));
        return item;
    }

    /// <summary>The <c>ion-item</c>: <c>disabled</c>, the option's classes, and the theme's item rule.</summary>
    private AvonicItem Item(SelectOverlayOption option, Control control)
    {
        var item = new AvonicItem { Content = control, IsEnabled = !option.Disabled };
        foreach (var name in (option.CssClass ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            item.Classes.Add(name);
        }

        Merge(item, ItemKey);
        return item;
    }

    /// <summary>
    /// <c>renderOptionLabel</c>: a plain label, or with a description, the text and the description under it. The start and end
    /// content only come with <c>innerHTMLTemplatesEnabled</c>, which Avonic doesn't have.
    /// </summary>
    private static Control Label(SelectOverlayOption option)
    {
        if (!HasRichContent(option))
        {
            var label = new TextBlock { Text = option.Text };
            label.Classes.Add(LabelClass);
            return label;
        }

        var content = new StackPanel();
        content.Classes.Add(ContentClass);
        content.Children.Add(new TextBlock { Text = option.Text });
        var description = new TextBlock { Text = option.Description };
        description.Classes.Add(DescriptionClass);
        content.Children.Add(description);
        return content;
    }

    private static bool HasRichContent(SelectOverlayOption option) => !string.IsNullOrEmpty(option.Description);

    /// <summary>A class that sets CSS variables on the item: its rule's resources merged while it's on, so the item's own win.</summary>
    private void SetChecked(AvonicItem item, string className, string key, bool on)
    {
        item.Classes.Set(className, on);
        var merged = item.Resources.MergedDictionaries;
        var rule = merged.OfType<RuleDictionary>().FirstOrDefault(dictionary => dictionary.Key == key);
        if (on && rule is null)
        {
            Merge(item, key);
        }
        else if (!on && rule is not null)
        {
            merged.Remove(rule);
        }
    }

    /// <summary>
    /// The theme's rule, copied for this item, as a dictionary has one owner. Its values are the theme's own objects, so a brush
    /// that follows a palette still does.
    /// </summary>
    private void Merge(AvonicItem item, string key)
    {
        if (!_host.TryFindResource(key, out var value) || value is not ResourceDictionary rule)
        {
            return;
        }

        var copy = new RuleDictionary(key);
        foreach (var name in rule.Keys)
        {
            if (rule.TryGetResource(name, null, out var resource))
            {
                copy[name] = resource;
            }
        }

        item.Resources.MergedDictionaries.Add(copy);
    }

    /// <summary>A copy of one of the theme's item rules, which knows which.</summary>
    private sealed class RuleDictionary(string key) : ResourceDictionary
    {
        public string Key { get; } = key;
    }

    /// <summary>The radio group's <c>ionChange</c>: the option's handler with the radio's value.</summary>
    private void OnGroupChange(object? sender, RadioGroupChangeEventArgs e)
    {
        if (_rows.FirstOrDefault(row => Equals(row.Option.Value, e.Value)) is { Option: { } option })
        {
            CallHandler(option, option.Value);
        }

        UpdateRadioChecked(e.Value);
    }

    private void UpdateRadioChecked(object? value)
    {
        foreach (var (option, item, _) in _rows)
        {
            SetChecked(item, RadioCheckedClass, ItemRadioCheckedKey, value is not null && Equals(option.Value, value));
        }
    }

    /// <summary><c>safeCall(option.handler, values)</c>: an exception is logged, not thrown.</summary>
    private static void CallHandler(SelectOverlayOption option, object? values)
    {
        try
        {
            option.Handler?.Invoke(values);
        }
        catch (Exception exception)
        {
            Avalonia.Logging.Logger.TryGet(Avalonia.Logging.LogEventLevel.Error, "Avonic")?.Log(option, "[ion-select] handler: {0}", exception);
        }
    }

    // ── Keys ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>ion-radio-group</c>'s <c>onKeydown</c> when it's in a select interface: the arrows move focus between the enabled radios
    /// without choosing, and Enter (not a repeat) commits the focused radio. The radio's own <c>onKeyDown</c> remembers where
    /// Enter went down. Space is the group's own, which chooses as anywhere else.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_group is not { } group || e.Source is not AvonicRadio current || current.Group != group)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            // !ev.repeat: a held key isn't a new press. Avalonia gives no repeat flag, so a down after a down with no up between is
            // one, and so is an Enter still held from opening the select (IgnoreEnterUntilKeyUp).
            if (_pendingEnter is null && !IgnoreEnterUntilKeyUp)
            {
                _pendingEnter = current;
                if (!Equals(group.Value, current.Value))
                {
                    group.SetCurrentValue(AvonicRadioGroup.ValueProperty, current.Value);
                    OnGroupChange(group, new RadioGroupChangeEventArgs(AvonicRadioGroup.IonChangeEvent, current.Value));
                }
            }

            return;
        }

        var radios = group.GetVisualDescendants().OfType<AvonicRadio>().Where(radio => radio.Group == group && radio.IsEnabled).ToList();
        var index = radios.IndexOf(current);
        if (index < 0)
        {
            return;
        }

        AvonicRadio? next = e.Key switch
        {
            Key.Down or Key.Right => radios[(index + 1) % radios.Count],
            Key.Up or Key.Left => radios[(index - 1 + radios.Count) % radios.Count],
            _ => null,
        };

        if (next is not null)
        {
            // setFocus(ev) stops the key and its default; the value stays.
            e.Handled = true;
            next.Focus(NavigationMethod.Directional);
        }
    }

    /// <summary>The radio's <c>onKeyUp</c>: Space dismisses; Enter dismisses if it went down on the same radio.</summary>
    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            IgnoreEnterUntilKeyUp = false;
        }

        if (e.Source is not AvonicRadio radio || radio.Group != _group)
        {
            return;
        }

        if (e.Key == Key.Space)
        {
            _dismiss();
        }
        else if (e.Key == Key.Enter)
        {
            var dismiss = _pendingEnter == radio;
            _pendingEnter = null;
            if (dismiss)
            {
                _dismiss();
            }
        }
    }
}
