using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>getClassMap</c> and the attribute values the alert reads, for its parts.</summary>
internal static class AlertClasses
{
    /// <summary>Space-separated classes, as style classes. Returns what it added, so it can be taken off again.</summary>
    public static string[] Apply(Control control, string? cssClass)
    {
        var classes = Split(cssClass);
        foreach (var name in classes)
        {
            if (!control.Classes.Contains(name))
            {
                control.Classes.Add(name);
            }
        }

        return classes;
    }

    public static string[] Split(string? cssClass) =>
        string.IsNullOrWhiteSpace(cssClass) ? [] : cssClass.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// A boolean such as <c>disabled</c> spread from script: Stencil sets the element's property, so it's JavaScript's truthiness,
    /// where only <c>false</c>, <c>null</c>, <c>0</c> and <c>""</c> are false.
    /// </summary>
    public static bool IsTrue(object? value) => value switch
    {
        null => false,
        bool b => b,
        string s => s.Length > 0,
        int i => i != 0,
        double d => d != 0 && !double.IsNaN(d),
        _ => true,
    };
}
