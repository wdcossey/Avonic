using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// What <see cref="AvonicInput"/> and <see cref="AvonicTextarea"/> share around their <see cref="NativeTextField"/>: the
/// <c>&lt;label for&gt;</c> around the field, its accessible name and hint, the app's <c>ion-touched</c> and <c>ion-invalid</c>,
/// <c>autofocus</c>, the counter, and the host classes for the label, the fill and the shape. <c>input.js</c> and
/// <c>textarea.js</c> do these the same way.
/// </summary>
internal sealed class LabelledTextField
{
    /// <summary>The style class an app sets once the user has been in the field.</summary>
    public const string TouchedClass = "ion-touched";

    /// <summary>The style class an app sets when the value is invalid.</summary>
    public const string InvalidClass = "ion-invalid";

    /// <summary>The style class an app sets when the value is valid.</summary>
    public const string ValidClass = "ion-valid";

    private readonly TemplatedControl _host;
    private readonly NativeTextField _field;
    private readonly Action _setFocus;
    private readonly Action _updateAutomation;
    private Control? _wrapper;
    private ContentPresenter? _labelContent;
    private TextBox? _lateNative;
    private NativeFieldAutomation? _lateAutomation;
    private bool _autofocused;

    /// <param name="host">The input or the textarea.</param>
    /// <param name="field">Its native field.</param>
    /// <param name="setFocus">Its <c>setFocus()</c>.</param>
    /// <param name="updateAutomation">Its own <c>aria-*</c> on the native field, which calls <see cref="UpdateAutomation"/>.</param>
    public LabelledTextField(TemplatedControl host, NativeTextField field, Action setFocus, Action updateAutomation)
    {
        _host = host;
        _field = field;
        _setFocus = setFocus;
        _updateAutomation = updateAutomation;
    }

    /// <summary>Follows the app's classes: call it after the host's <see cref="NativeButton"/> for the label is made.</summary>
    public void FollowClasses() => _host.Classes.CollectionChanged += OnClassesChanged;

    /// <summary><c>checkInvalidState</c>: the app has marked it both touched and invalid.</summary>
    public bool IsInvalid => _host.Classes.Contains(TouchedClass) && _host.Classes.Contains(InvalidClass);

    /// <summary>Follows the template's wrapper, the <c>&lt;label&gt;</c>, and the presenter of the <c>label</c> slot.</summary>
    public void Attach(Control? wrapper, ContentPresenter? labelContent)
    {
        _wrapper = wrapper;
        if (_labelContent is not null)
        {
            _labelContent.PropertyChanged -= OnLabelContentPropertyChanged;
        }

        _labelContent = labelContent;
        if (_labelContent is not null)
        {
            // A templated label's element is made after the template's applied: name the field again then.
            _labelContent.PropertyChanged += OnLabelContentPropertyChanged;
        }
    }

    /// <summary>
    /// The <c>&lt;label for&gt;</c>'s activation: a click in the wrapper that isn't on the native field focuses it. A slotted
    /// button's click, or the input's clear button's, is its own, as <see cref="NativeButton"/> arranges. The bottom text is
    /// outside the label.
    /// </summary>
    public void OnLabelClick(object? source)
    {
        if (source is not Visual visual || _wrapper is null || (visual != _wrapper && !_wrapper.IsVisualAncestorOf(visual)))
        {
            return;
        }

        // A label does nothing when the click is on interactive content inside it, other than its own control.
        var native = _field.Native;
        for (var at = visual; at is not null && at != _wrapper && at != _host; at = at.GetVisualParent())
        {
            if (at is InputElement { Focusable: true } element && element != native && !(native?.IsVisualAncestorOf(element) ?? false))
            {
                return;
            }
        }

        _setFocus();
    }

    /// <summary>
    /// <c>autoFocus</c> on the native field: Chromium focuses it once, when it's first in the document, unless something else
    /// already has focus. Not again when it's loaded again.
    /// </summary>
    public void OnLoaded(bool autofocus)
    {
        if (autofocus && !_autofocused)
        {
            _autofocused = true;
            if (TopLevel.GetTopLevel(_host)?.FocusManager?.GetFocusedElement() is null)
            {
                _setFocus();
            }
        }
    }

    /// <summary>
    /// The native field's <c>aria-labelledby</c>: the label, or the slotted label's text, unless the host has an
    /// <c>aria-label</c>, which <c>inheritAriaAttributes</c> moves to the field. <c>aria-describedby</c>: <c>getHintTextID</c>.
    /// <c>aria-required</c>: <c>required</c> on the native field, to a screen reader; form validation isn't here. All at style
    /// priority, through <see cref="NativeFieldAutomation"/>, so that an app's style for the field wins.
    /// </summary>
    public void UpdateAutomation(TextBox native, string? label, object? labelContent, string? helperText, string? errorText,
        bool required)
    {
        var automation = AutomationOf(native);
        var name = AutomationProperties.GetName(_host);
        if (string.IsNullOrEmpty(name))
        {
            name = label ?? labelContent switch
            {
                Control control => AccessibleText.Of(control),
                string text => text,
                _ => _labelContent?.Child is { } child ? AccessibleText.Of(child) : null,
            };
        }

        automation.Name.SetWhen(name is not null, name);

        var hint = IsInvalid && !string.IsNullOrEmpty(errorText) ? errorText
            : !string.IsNullOrEmpty(helperText) ? helperText
            : null;
        automation.HelpText.SetWhen(hint is not null, hint);
        automation.IsRequiredForForm.Set(required);
    }

    /// <summary>
    /// The field's own <see cref="NativeFieldAutomation"/>, bound before its styles. A plain <see cref="TextBox"/> in an app's
    /// own template has none: it's bound now, after its styles, which then lose to it, and let go when another field takes its
    /// place.
    /// </summary>
    private NativeFieldAutomation AutomationOf(TextBox native)
    {
        if (_lateNative != native)
        {
            _lateAutomation?.Dispose();
            _lateAutomation = null;
            _lateNative = null;
        }

        if (native is INativeFieldAutomation field)
        {
            return field.Automation;
        }

        _lateNative = native;
        return _lateAutomation ??= new NativeFieldAutomation(native);
    }

    /// <summary><c>renderCounter</c> and <c>getCounterText</c>: null when there's no counter.</summary>
    public static string? CounterText(bool counter, int? maxlength, string value, Func<int, int, string>? formatter)
    {
        if (!counter || maxlength is not { } max)
        {
            return null;
        }

        var length = value.Length;
        var fallback = $"{length} / {max}";
        if (formatter is null)
        {
            return fallback;
        }

        try
        {
            return formatter(length, max);
        }
        catch (Exception)
        {
            // printIonError, then the default text.
            return fallback;
        }
    }

    /// <summary>
    /// <c>render</c>'s host classes that the input and the textarea share, in the order both set them, up to <c>:in-item</c>.
    /// The label floats when stacked, or floating with a value, focus, or anything in a slot.
    /// </summary>
    public static void SetClasses(IPseudoClasses classes, InputLabelPlacement placement, bool hasValue, bool hasFocus,
        bool hasStartEnd, bool outline, bool solid, bool round, bool inItem)
    {
        classes.Set(":has-value", hasValue);
        classes.Set(":has-focus", hasFocus);
        classes.Set(":label-floating",
            placement == InputLabelPlacement.Stacked
            || (placement == InputLabelPlacement.Floating && (hasValue || hasFocus || hasStartEnd)));

        classes.Set(":fill-outline", outline);
        classes.Set(":fill-solid", solid);
        classes.Set(":shape-round", round);
        classes.Set(":label-placement-start", placement == InputLabelPlacement.Start);
        classes.Set(":label-placement-end", placement == InputLabelPlacement.End);
        classes.Set(":label-placement-fixed", placement == InputLabelPlacement.Fixed);
        classes.Set(":label-placement-floating", placement == InputLabelPlacement.Floating);
        classes.Set(":label-placement-stacked", placement == InputLabelPlacement.Stacked);
        classes.Set(":in-item", inItem);
    }

    /// <summary>The textarea's placement as the input's, which <see cref="InputWrapperPanel"/> takes: the same five.</summary>
    public static InputLabelPlacement Placement(TextareaLabelPlacement placement) => placement switch
    {
        TextareaLabelPlacement.End => InputLabelPlacement.End,
        TextareaLabelPlacement.Fixed => InputLabelPlacement.Fixed,
        TextareaLabelPlacement.Floating => InputLabelPlacement.Floating,
        TextareaLabelPlacement.Stacked => InputLabelPlacement.Stacked,
        _ => InputLabelPlacement.Start,
    };

    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // The MutationObserver on class: a new aria-describedby. Only the two classes checkInvalidState reads, as the list
        // also holds every pseudoclass.
        if (Mentions(e.NewItems) || Mentions(e.OldItems) || e.Action == NotifyCollectionChangedAction.Reset)
        {
            _updateAutomation();
        }
    }

    private static bool Mentions(System.Collections.IList? classes) =>
        classes?.OfType<string>().Any(name => name is TouchedClass or InvalidClass) == true;

    private void OnLabelContentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentPresenter.ChildProperty)
        {
            _updateAutomation();
        }
    }
}
