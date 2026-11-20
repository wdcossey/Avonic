using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ViewController</c>: one page of an <see cref="AvonicNav"/>'s stack, its component and props, and the page once it's
/// built. <see cref="AvonicNav.GetActive"/> and the other getters return these; an app can pass one back to
/// <see cref="AvonicNav.PopTo(ViewController, NavOptions?, TransitionDoneFn?)"/>, or make one to push.
/// </summary>
public sealed class ViewController
{
    internal const int StateNew = 1;
    internal const int StateAttached = 2;
    internal const int StateDestroyed = 3;

    /// <summary>The class <c>attachComponent</c> gives a page while it waits for its transition: <c>opacity: 0</c>.</summary>
    internal const string InvisibleClass = "ion-page-invisible";

    // The page's state the nav gives at style priority, each bound once, when the page is built, so an app's own style wins.
    private readonly StyleValue<bool> _visible = new();
    private readonly StyleValue<bool> _hitTestVisible = new();
    private readonly StyleValue<int> _zIndex = new();
    private readonly StyleValue<IBrush?> _invisible = new();
    private readonly List<IDisposable> _bindings = [];
    private bool _hidden;
    private bool _displayNone;

    /// <summary>A view of <paramref name="component"/>, which <see cref="AvonicNav"/> builds when it's first entered.</summary>
    /// <param name="component">A page form: see <see cref="AvonicNav"/>.</param>
    /// <param name="params">Set on the page by name before it's attached.</param>
    public ViewController(object component, IReadOnlyDictionary<string, object?>? @params = null)
    {
        Component = component ?? throw new ArgumentNullException(nameof(component));
        Params = @params;
    }

    /// <summary>Ionic <c>component</c>: what the page is made from.</summary>
    public object Component { get; }

    /// <summary>Ionic <c>params</c>: the component's props.</summary>
    public IReadOnlyDictionary<string, object?>? Params { get; }

    /// <summary>Ionic <c>element</c>: the page, null until it's built.</summary>
    public Control? Element { get; private set; }

    /// <summary>Ionic <c>nav</c>: the nav it's in, null when it's in none.</summary>
    public AvonicNav? Nav { get; internal set; }

    /// <summary>Ionic <c>state</c>: new, attached or destroyed.</summary>
    internal int State { get; private set; } = StateNew;

    /// <summary>The builder it was entered with, which a pop back to it uses.</summary>
    internal Animation.AnimationBuilder? AnimationBuilder { get; set; }

    /// <summary>
    /// Ionic <c>init</c>, with <c>attachComponent</c>: builds the page from its component, gives it class <c>ion-page</c> and
    /// <c>ion-page-invisible</c>, sets its props, and adds it to the nav. A page that's already built is only attached.
    /// </summary>
    internal void Init(AvonicNav nav)
    {
        if (Element is not null)
        {
            State = StateAttached;
            return;
        }

        var page = Build(nav, Component);
        if (page.Parent is not null || page.GetVisualParent() is not null)
        {
            // A DOM element would move; an Avalonia control has one parent, so it has to leave the other first.
            throw new InvalidOperationException("The page is already in the tree: remove it from its parent, or pop it, first.");
        }

        page.Classes.Add("ion-page");
        page.Classes.Add(InvisibleClass);
        NavProps.Apply(page, Params);
        Element = page;
        Bind(page);
        State = StateAttached;
        nav.AddPage(page);
    }

    /// <summary>The page forms, in order: a control, a <see cref="Type"/>, a factory, then anything else through a data template.</summary>
    private static Control Build(AvonicNav nav, object component)
    {
        switch (component)
        {
            case Control control:
                return control;
            case Type type when typeof(Control).IsAssignableFrom(type):
                return (Control)(Activator.CreateInstance(type)
                    ?? throw new InvalidOperationException($"{type} could not be made."));
            case Func<Control> factory:
                return factory() ?? throw new InvalidOperationException("A page factory returned null.");
            case Type type:
                throw new InvalidOperationException($"A page Type is a Control's, not {type}.");
            default:
                // The framework's delegate: Avalonia's data templates, the nav's, its ancestors' and the application's.
                var template = nav.FindDataTemplate(component)
                    ?? throw new InvalidOperationException(
                        $"framework delegate is missing: no data template builds a page for {component.GetType()}.");
                var page = template.Build(component)
                    ?? throw new InvalidOperationException($"The data template for {component.GetType()} built nothing.");
                page.DataContext = component;
                return page;
        }
    }

    private void Bind(Control page)
    {
        _bindings.Add(page.Bind(Visual.IsVisibleProperty, _visible, BindingPriority.Style));
        _bindings.Add(page.Bind(Avalonia.Input.InputElement.IsHitTestVisibleProperty, _hitTestVisible, BindingPriority.Style));
        _bindings.Add(page.Bind(Visual.ZIndexProperty, _zIndex, BindingPriority.Style));
        _bindings.Add(page.Bind(Visual.OpacityMaskProperty, _invisible, BindingPriority.Style));
        // A transition's opacity fades the page as one picture, as CSS does.
        Animation.AnimationGroupOpacity.SetIsEnabled(page, true);
        _bindings.Add(new ActionDisposable(() => page.ClearValue(Animation.AnimationGroupOpacity.IsEnabledProperty)));
        page.Classes.CollectionChanged += OnClassesChanged;
        _bindings.Add(new ActionDisposable(() => page.Classes.CollectionChanged -= OnClassesChanged));
        UpdateOpacity();
    }

    private void OnClassesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => UpdateOpacity();

    private sealed class ActionDisposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }

    /// <summary>
    /// <c>.ion-page-invisible { opacity: 0 }</c>, while the page has the class, as a transition's builder removes it: as a mask, the
    /// page's group opacity, which a transition's animated opacity replaces, as a Web Animation's beats the class in CSS.
    /// </summary>
    private void UpdateOpacity() =>
        _invisible.SetWhen(Element?.Classes.Contains(InvisibleClass) == true, Animation.AnimationGroupOpacity.Mask(0));

    /// <summary>
    /// Ionic <c>setPageHidden</c>: <c>ion-page-hidden</c> (<c>display: none</c>) and <c>aria-hidden</c>. Hidden, it's out of the
    /// layout and the automation tree. Shown again, the leaving page's <c>display: none</c> from the md transition goes too.
    /// </summary>
    internal void SetHidden(bool hidden)
    {
        _hidden = hidden;
        if (!hidden)
        {
            _displayNone = false;
        }

        _visible.SetWhen(_hidden || _displayNone, false);
    }

    /// <summary>The md back transition's <c>style.display = 'none'</c> on the leaving page when it ends.</summary>
    internal void SetDisplayNone()
    {
        _displayNone = true;
        _visible.Set(false);
    }

    /// <summary><c>pointer-events: none</c> during a transition, and back after.</summary>
    internal void SetTransitioning(bool on) => _hitTestVisible.SetWhen(on, false);

    /// <summary>The <c>z-index</c> a transition leaves on the page.</summary>
    internal void SetZIndex(int zIndex) => _zIndex.Set(zIndex);

    /// <summary>Ionic <c>_destroy</c>: the page leaves the nav, and the view can't be used again.</summary>
    internal void Destroy()
    {
        if (Element is { } page && Nav is { } nav)
        {
            nav.RemovePage(page);
        }

        // A page an app made can be pushed again, as a new view: this one's state leaves it.
        foreach (var binding in _bindings)
        {
            binding.Dispose();
        }

        _bindings.Clear();
        Element?.Classes.Remove(InvisibleClass);
        if (Element is { } built)
        {
            ((IPseudoClasses)built.Classes).Remove(AvonicNav.CanGoBackPseudoClass);
        }

        Nav = null;
        State = StateDestroyed;
    }
}

/// <summary>
/// <c>Object.assign(el, componentProps)</c>: each key the page's Avalonia property by name, camelCase or PascalCase, or else its
/// public settable CLR property, the value converted as XAML would convert a string. A key with neither is ignored, as is a value
/// the property won't take (logged).
/// </summary>
/// <remarks>
/// A CLR property is found by reflection. A trimmed app (Android's head is) can strip a page's property that no code of its own
/// uses; an Avalonia property is registered, and always found.
/// </remarks>
internal static class NavProps
{
    public static void Apply(Control page, IReadOnlyDictionary<string, object?>? props)
    {
        if (props is null)
        {
            return;
        }

        foreach (var (key, value) in props)
        {
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var name = char.ToUpperInvariant(key[0]) + key[1..];
            try
            {
                if (AvaloniaPropertyRegistry.Instance.FindRegistered(page, name) is { IsReadOnly: false } property)
                {
                    if (TryConvert(value, property.PropertyType, out var converted))
                    {
                        page.SetValue(property, converted);
                    }
                    else
                    {
                        Dropped(page, key, value);
                    }

                    continue;
                }

                if (FindClrProperty(page.GetType(), name) is { } clr)
                {
                    if (TryConvert(value, clr.PropertyType, out var clrValue))
                    {
                        clr.SetValue(page, clrValue);
                    }
                    else
                    {
                        Dropped(page, key, value);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.TryGet(LogEventLevel.Warning, "Avonic")?.Log(page, "[ion-nav] componentProps {0}: {1}", key, exception.Message);
            }
        }
    }

    private static void Dropped(Control page, string key, object? value) =>
        Logger.TryGet(LogEventLevel.Warning, "Avonic")?.Log(page, "[ion-nav] componentProps {0}: {1} isn't a value it takes", key, value);

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Documented: a trimmed app may strip a page's CLR property.")]
    private static PropertyInfo? FindClrProperty(Type type, string name) =>
        type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) is { CanWrite: true, SetMethod.IsPublic: true } property
            && property.GetIndexParameters().Length == 0 ? property : null;

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
                converted = Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
                return true;
            }
        }
        catch (Exception)
        {
            // A value that can't be converted is dropped.
        }

        return false;
    }
}
