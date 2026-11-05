using System.Reflection;
using Avalonia.Controls;

namespace Avonic.Sample;

/// <summary>Lists a page on the sample's index, with no other registration.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SamplePageAttribute(string title) : Attribute
{
    /// <summary>The Ionic tag, e.g. <c>ion-ripple-effect</c>.</summary>
    public string Title { get; } = title;

    /// <summary>
    /// The control's issue number, so parallel PRs don't have to coordinate. A page that omits it
    /// sorts last rather than jumping to the top of the index.
    /// </summary>
    public int Order { get; set; } = int.MaxValue;
}

public sealed record SamplePageEntry(string Title, Func<Control> Create);

public static class SamplePageCatalog
{
    /// <summary>Every <see cref="SamplePageAttribute"/> page in the assembly, by order then title.</summary>
    public static IReadOnlyList<SamplePageEntry> Discover(Assembly assembly) =>
        assembly.GetTypes()
            .Select(type => (Type: type, Page: type.GetCustomAttribute<SamplePageAttribute>()))
            .Where(found => found.Page is not null)
            .OrderBy(found => found.Page!.Order)
            .ThenBy(found => found.Page!.Title, StringComparer.Ordinal)
            .Select(found => new SamplePageEntry(
                found.Page!.Title,
                () => (Control)Activator.CreateInstance(found.Type)!))
            .ToArray();
}
