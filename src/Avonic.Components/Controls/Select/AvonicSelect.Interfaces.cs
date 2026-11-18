using System.Collections;
using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary><c>ion-select</c>'s interfaces: <c>open</c>, <c>createOverlay</c> and each overlay's options.</summary>
public partial class AvonicSelect
{
    /// <summary>The theme's resource that <c>.select-popover-rich-content</c> sets on the popover's <c>--width</c>, in three numbers.</summary>
    internal const string RichContentMinWidthKey = "Avonic.SelectPopover.Md.RichContentMinWidth";
    internal const string RichContentInsetKey = "Avonic.SelectPopover.Md.RichContentInset";
    internal const string RichContentMaxWidthKey = "Avonic.SelectPopover.Md.RichContentMaxWidth";

    /// <summary>
    /// Ionic <c>open(event)</c>: makes the interface, presents it in the select's top level, and returns it, or null when the
    /// select is disabled or already open. <paramref name="ev"/> is what a <c>popover</c> interface is placed by; without one, the
    /// alert is used instead, as Ionic does.
    /// </summary>
    public Task<AvonicOverlay?> Open(PopoverEvent? ev = null) => Open(ev, openedByEnter: false);

    private async Task<AvonicOverlay?> Open(PopoverEvent? ev, bool openedByEnter)
    {
        if (!IsEnabled || _isExpanded)
        {
            return null;
        }

        SetExpanded(true);
        var overlay = _overlay = CreateOverlay(ev, out var popoverEvent);
        if (overlay is AvonicPopover or AvonicModal)
        {
            // For modals and popovers, scroll and focus before they're visible: at will-present, as soon as the overlay is
            // shown for its enter animation, which in Avonic is just after the event (Ionic's overlay-hidden comes off then too).
            void OnWillPresent(object? sender, RoutedEventArgs e)
            {
                overlay.WillPresent -= OnWillPresent;
                Dispatcher.UIThread.Post(() => ScrollSelectedIntoView(overlay));
            }

            overlay.WillPresent += OnWillPresent;
            if (ContentList(overlay) is { } list)
            {
                list.IgnoreEnterUntilKeyUp = openedByEnter;
            }
        }
        else
        {
            // The alert's and the action sheet's content is there a frame after their will-present.
            void OnWillPresent(object? sender, RoutedEventArgs e)
            {
                overlay.WillPresent -= OnWillPresent;
                _ = AvonicConfig.RequestFrame(overlay, () => ScrollSelectedIntoView(overlay));
            }

            overlay.WillPresent += OnWillPresent;
        }

        _ = WaitForDismiss(overlay);

        if (overlay is AvonicPopover popover)
        {
            await popover.Present(popoverEvent);
        }
        else if (OwnTopLevel() is { } top)
        {
            await overlay.Present(top);
        }
        else
        {
            await overlay.Present();
        }

        return overlay;
    }

    /// <summary><c>overlay.onDidDismiss().then</c>: not expanded, <c>ionDismiss</c>, and the select focused again.</summary>
    private async Task WaitForDismiss(AvonicOverlay overlay)
    {
        await overlay.OnDidDismiss();
        if (_overlay == overlay)
        {
            _overlay = null;
        }

        SetExpanded(false);
        RaiseEvent(new RoutedEventArgs(IonDismissEvent, this));
        SetFocus();
    }

    private TopLevel? OwnTopLevel()
    {
        var top = TopLevel.GetTopLevel(this);
        while (top is PopupRoot { ParentTopLevel: { } parent })
        {
            top = parent;
        }

        return top;
    }

    private void SetExpanded(bool expanded)
    {
        _isExpanded = expanded;
        UpdateIcon();
        UpdateClasses();
        UpdateAutomation();

        // The watcher on isExpanded: styleChanged.
        EmitStyle();
        if (ControlAutomationPeer.FromElement(this) is SelectAutomationPeer peer)
        {
            peer.RaiseExpanded(!expanded, expanded);
        }
    }

    /// <summary><c>close()</c>: dismisses the open interface.</summary>
    internal Task<bool> Close() => _overlay?.Dismiss() ?? Task.FromResult(false);

    /// <summary>
    /// <c>createOverlay</c>: the action sheet can't take several values, and a popover needs an event; either uses the alert.
    /// </summary>
    private AvonicOverlay CreateOverlay(PopoverEvent? ev, out PopoverEvent? popoverEvent)
    {
        popoverEvent = null;
        var selectInterface = Interface;
        if (selectInterface == SelectInterface.ActionSheet && Multiple)
        {
            Warn("Interface cannot be \"action-sheet\" with a multi-value select. Using the \"alert\" interface instead.");
            selectInterface = SelectInterface.Alert;
        }

        if (selectInterface == SelectInterface.Popover && ev is null)
        {
            Warn("Interface cannot be a \"popover\" without passing an event. Using the \"alert\" interface instead.");
            selectInterface = SelectInterface.Alert;
        }

        return selectInterface switch
        {
            SelectInterface.ActionSheet => OpenActionSheet(),
            SelectInterface.Popover => OpenPopover(ev!, out popoverEvent),
            SelectInterface.Modal => OpenModal(),
            _ => OpenAlert(),
        };
    }

    private static void Warn(string message) =>
        Avalonia.Logging.Logger.TryGet(Avalonia.Logging.LogEventLevel.Warning, "Avonic")?.Log(null, "[ion-select] - " + message);

    /// <summary><c>updateOverlayOptions</c>: the open interface's options from the options as they are now.</summary>
    private void UpdateOverlayOptions()
    {
        switch (_overlay)
        {
            case AvonicActionSheet sheet:
                sheet.Buttons = CreateActionSheetButtons();
                break;
            case AvonicAlert alert:
                alert.Inputs = CreateAlertInputs();
                break;
            case AvonicPopover { Content: AvonicSelectPopover content }:
                content.Options = CreateOverlaySelectOptions();
                break;
            case AvonicModal { Content: AvonicSelectModal content }:
                content.Options = CreateOverlaySelectOptions();
                break;
        }
    }

    /// <summary>An option's classes, which Ionic copies (all but <c>hydrated</c>) after <c>select-interface-option</c>.</summary>
    private static string OptionClasses(AvonicSelectOption option) =>
        string.Join(' ', new[] { OptionClass }.Concat(option.Classes.Where(name => !name.StartsWith(':'))));

    /// <summary><c>createActionSheetButtons</c>: a radio per option, then Cancel.</summary>
    private List<ActionSheetButton> CreateActionSheetButtons()
    {
        var buttons = _items.Select(option =>
        {
            var value = option.OptionValue;
            return new ActionSheetButton
            {
                Text = option.Text,
                CssClass = OptionClasses(option),
                Disabled = !option.IsEnabled,
                Handler = () =>
                {
                    SetValueFromUser(value);
                    return null;
                },
                HtmlAttributes = new Dictionary<string, object?>
                {
                    ["aria-checked"] = IsOptionSelected(Value, value) ? "true" : "false",
                    ["role"] = "radio",
                },
            };
        }).ToList();

        buttons.Add(new ActionSheetButton
        {
            Text = CancelText,
            Role = "cancel",
            Handler = () =>
            {
                RaiseEvent(new RoutedEventArgs(IonCancelEvent, this));
                return null;
            },
        });
        return buttons;
    }

    /// <summary><c>createAlertInputs</c>: a radio, or a checkbox for <see cref="Multiple"/>, per option.</summary>
    private List<AlertInput> CreateAlertInputs()
    {
        var type = Multiple ? AlertInputType.Checkbox : AlertInputType.Radio;
        return _items.Select(option =>
        {
            var value = option.OptionValue;
            return new AlertInput
            {
                Type = type,
                CssClass = OptionClasses(option),
                Label = option.Text,
                Value = value,
                Checked = IsOptionSelected(Value, value),
                Disabled = !option.IsEnabled,
            };
        }).ToList();
    }

    /// <summary><c>createOverlaySelectOptions</c>: the popover's and the modal's options, whose handler sets the value.</summary>
    private List<SelectOverlayOption> CreateOverlaySelectOptions() => _items.Select(option =>
    {
        var value = option.OptionValue;
        return new SelectOverlayOption
        {
            Text = option.Text,
            CssClass = OptionClasses(option),
            Value = value,
            Checked = IsOptionSelected(Value, value),
            Disabled = !option.IsEnabled,
            Handler = selected =>
            {
                SetValueFromUser(selected);
                if (!Multiple)
                {
                    _ = Close();
                }

                return null;
            },
            Description = option.Description,
            LabelPlacement = option.LabelPlacement,
            Justify = option.Justify,
        };
    }).ToList();

    /// <summary><c>openAlert</c>.</summary>
    private AvonicAlert OpenAlert()
    {
        var options = InterfaceOptions;
        var alert = new AvonicAlert();
        SelectInterfaceOptions.Apply(alert, options);

        // header: interfaceOptions.header ? interfaceOptions.header : this.labelText. Then the inputs, buttons and class.
        alert.Header = SelectInterfaceOptions.Get(options, "header") is string { Length: > 0 } header ? header : LabelText;
        alert.Inputs = CreateAlertInputs();
        alert.Buttons =
        [
            new AlertButton
            {
                Text = CancelText,
                Role = "cancel",
                Handler = _ =>
                {
                    RaiseEvent(new RoutedEventArgs(IonCancelEvent, this));
                    return null;
                },
            },
            new AlertButton
            {
                Text = OkText,
                Handler = values =>
                {
                    SetValueFromUser(values);
                    return null;
                },
            },
        ];
        alert.CssClass = SelectInterfaceOptions.JoinClasses("select-alert", options,
            Multiple ? "multiple-select-alert" : "single-select-alert");
        return alert;
    }

    /// <summary><c>openActionSheet</c>.</summary>
    private AvonicActionSheet OpenActionSheet()
    {
        var options = InterfaceOptions;
        var sheet = new AvonicActionSheet();
        SelectInterfaceOptions.Apply(sheet, options);
        sheet.Buttons = CreateActionSheetButtons();
        sheet.CssClass = SelectInterfaceOptions.JoinClasses("select-action-sheet", options);
        return sheet;
    }

    /// <summary>
    /// <c>openPopover</c>: at the select's container, centred, as wide as its content; as wide as the select with a fill or a
    /// floating or stacked label; no backdrop in md. Then the interface options, which can replace those.
    /// </summary>
    private AvonicPopover OpenPopover(PopoverEvent ev, out PopoverEvent popoverEvent)
    {
        var options = InterfaceOptions;
        var cover = LabelPlacement is SelectLabelPlacement.Floating or SelectLabelPlacement.Stacked || Fill is not null;

        // Otherwise it's placed by the native wrapper, as the event's ionShadowTarget.
        popoverEvent = cover ? ev : new PopoverEvent(_container ?? ev.Target, ev.ClientPosition);
        var popover = new AvonicPopover
        {
            Alignment = PopoverAlignment.Center,
            Size = cover ? PopoverSize.Cover : PopoverSize.Auto,
            ShowBackdrop = false,
        };
        SelectInterfaceOptions.Apply(popover, options);
        if (SelectInterfaceOptions.Get(options, "event") is PopoverEvent replaced)
        {
            popoverEvent = replaced;
        }

        var overlayOptions = CreateOverlaySelectOptions();
        var richContent = overlayOptions.Any(option => !string.IsNullOrEmpty(option.Description));
        popover.Classes.Add("select-popover");
        if (richContent)
        {
            popover.Classes.Add("select-popover-rich-content");
            SetRichContentWidth(popover);
        }

        SelectInterfaceOptions.AddClasses(popover, options);
        popover.Content = new AvonicSelectPopover
        {
            Header = SelectInterfaceOptions.Get(options, "header") as string,
            SubHeader = SelectInterfaceOptions.Get(options, "subHeader") as string,
            Message = SelectInterfaceOptions.Get(options, "message") as string,
            Multiple = Multiple,
            Options = overlayOptions,
        };
        return popover;
    }

    /// <summary>
    /// <c>:host(.select-popover-rich-content) { --width: clamp(250px, calc(100vw - 40px), 400px) }</c>, worked out from the
    /// window's width now; the three numbers are the theme's.
    /// </summary>
    private void SetRichContentWidth(AvonicPopover popover)
    {
        if (OwnTopLevel() is not { } top
            || !this.TryFindResource(RichContentMinWidthKey, out var min) || min is not double minWidth
            || !this.TryFindResource(RichContentInsetKey, out var inset) || inset is not double insetWidth
            || !this.TryFindResource(RichContentMaxWidthKey, out var max) || max is not double maxWidth)
        {
            return;
        }

        // clamp(min, val, max) is max(min, min(val, max)).
        popover.Resources["Avonic.Popover.Width"] = Math.Max(minWidth, Math.Min(top.ClientSize.Width - insetWidth, maxWidth));
    }

    /// <summary><c>openModal</c>: the interface options first, then the class and the content.</summary>
    private AvonicModal OpenModal()
    {
        var options = InterfaceOptions;
        var modal = new AvonicModal();
        SelectInterfaceOptions.Apply(modal, options);
        modal.Classes.Add("select-modal");
        SelectInterfaceOptions.AddClasses(modal, options);
        modal.Content = new AvonicSelectModal
        {
            Header = SelectInterfaceOptions.Get(options, "header") as string,
            CancelText = CancelText,
            Multiple = Multiple,
            Options = CreateOverlaySelectOptions(),
        };
        return modal;
    }

    private static SelectOptionList? ContentList(AvonicOverlay overlay) => overlay switch
    {
        AvonicPopover { Content: AvonicSelectPopover content } => content.OptionList,
        AvonicModal { Content: AvonicSelectModal content } => content.OptionList,
        _ => null,
    };

    /// <summary>
    /// <c>scrollSelectedIntoView</c>: the option whose value is the select's (strictly, so never for a list) is brought into view,
    /// its radio or checkbox focused, and it's shown focused; with none, the first enabled radio or checkbox is focused and its
    /// item shown focused. The alert's and the action sheet's options are buttons, focused and shown focused themselves.
    /// </summary>

    private void ScrollSelectedIntoView(AvonicOverlay overlay)
    {

        var index = _items.ToList().FindIndex(option => Equals(option.OptionValue, Value));
        if (ContentList(overlay) is { } list)
        {
            // The content is made with the overlay's template, laid out for the first time as it's presented.
            overlay.ApplyTemplate();
            overlay.UpdateLayout();
            if (index > -1 && index < list.Rows.Count)
            {
                var (_, item, control) = list.Rows[index];
                item.BringIntoView();
                control.Focus(Avalonia.Input.NavigationMethod.Unspecified);

                // focusVisibleElement(selectedItem): an ion-item takes no focus of its own, so only the ring.
                ShowFocused(item);
            }
            else if (list.Rows.FirstOrDefault(row => row.Control.IsEffectivelyEnabled) is { Control: { } first, Item: { } firstItem })
            {
                first.Focus(Avalonia.Input.NavigationMethod.Unspecified);
                ShowFocused(firstItem);
            }

            return;
        }

        if (index > -1)
        {
            // .select-interface-option:nth-of-type(index + 1): the alert's and the action sheet's buttons carry the class.
            var selected = overlay.GetVisualDescendants().OfType<Control>()
                .Where(control => control.Classes.Contains(OptionClass) && control is InputElement { Focusable: true })
                .ElementAtOrDefault(index);
            if (selected is not null)
            {
                FocusVisibleElement(selected);
            }
        }
    }

    /// <summary>
    /// <c>focusVisibleElement</c>: focuses it, and if it's <c>ion-focusable</c>, shows it focused through the <c>ion-app</c>
    /// around the select, as Ionic's overlays are in the app.
    /// </summary>
    private void FocusVisibleElement(Control element)
    {
        element.Focus(Avalonia.Input.NavigationMethod.Unspecified);
        ShowFocused(element);
    }

    /// <summary><c>focusVisibleElement</c>'s ring: <c>ion-app</c>'s <c>setFocus</c>, for an <c>ion-focusable</c> element.</summary>
    private void ShowFocused(Control element)
    {
        if (FocusVisible.GetIsEnabled(element) && this.FindAncestorOfType<AvonicApp>() is { } app)
        {
            app.SetFocus(element);
        }
    }
}

/// <summary>
/// <c>interfaceOptions</c>, spread into an overlay's create options: each entry sets the overlay's property of that name, in
/// Ionic's camelCase or Avonic's PascalCase. A string is converted as XAML would (<c>"cover"</c>, <c>"action-sheet"</c>); a key
/// with no property, or a value the property can't take, is ignored, as an unknown property is on an element.
/// </summary>
internal static class SelectInterfaceOptions
{
    /// <summary>What the select reads itself rather than spreading: the content's, and the class.</summary>
    private static readonly HashSet<string> ReadBySelect = new(StringComparer.OrdinalIgnoreCase) { "cssClass", "event" };

    public static object? Get(IReadOnlyDictionary<string, object?>? options, string key)
    {
        if (options is null)
        {
            return null;
        }

        foreach (var (name, value) in options)
        {
            if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    public static void Apply(AvaloniaObject overlay, IReadOnlyDictionary<string, object?>? options)
    {
        if (options is null)
        {
            return;
        }

        foreach (var (key, value) in options)
        {
            if (ReadBySelect.Contains(key) || key.Length == 0)
            {
                continue;
            }

            var name = char.ToUpperInvariant(key[0]) + key[1..];
            if (AvaloniaPropertyRegistry.Instance.FindRegistered(overlay, name) is not { IsReadOnly: false } property
                || !TryConvert(value, property.PropertyType, out var converted))
            {
                continue;
            }

            overlay.SetValue(property, converted);
        }
    }

    private static bool TryConvert(object? value, Type type, out object? converted)
    {
        converted = value;
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (value is null)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
        }

        if (type.IsInstanceOfType(value) || target.IsInstanceOfType(value))
        {
            return true;
        }

        try
        {
            if (value is string text && target.IsEnum)
            {
                converted = Enum.Parse(target, text.Replace("-", ""), ignoreCase: true);
                return true;
            }

            if (value is string s && TypeDescriptor.GetConverter(target) is { } converter && converter.CanConvertFrom(typeof(string)))
            {
                converted = converter.ConvertFromInvariantString(s);
                return true;
            }

            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(target))
            {
                converted = Convert.ChangeType(value, target, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or InvalidCastException or NotSupportedException or OverflowException)
        {
        }

        return false;
    }

    /// <summary><c>interfaceOptions.cssClass</c>: a string of classes, or a list of them.</summary>
    private static IEnumerable<string> Classes(IReadOnlyDictionary<string, object?>? options) => Get(options, "cssClass") switch
    {
        string text => text.Split(' ', StringSplitOptions.RemoveEmptyEntries),
        IEnumerable list => list.OfType<string>().SelectMany(text => text.Split(' ', StringSplitOptions.RemoveEmptyEntries)),
        _ => [],
    };

    /// <summary>A <c>cssClass</c> array, as the alert's and the action sheet's <c>CssClass</c> string.</summary>
    public static string JoinClasses(string first, IReadOnlyDictionary<string, object?>? options, string? last = null) =>
        string.Join(' ', new[] { first }.Concat(Classes(options)).Concat(last is null ? [] : [last]));

    /// <summary>The popover's and the modal's <c>cssClass</c>, which their host gets as classes.</summary>
    public static void AddClasses(StyledElement overlay, IReadOnlyDictionary<string, object?>? options)
    {
        foreach (var name in Classes(options))
        {
            overlay.Classes.Add(name);
        }
    }
}
