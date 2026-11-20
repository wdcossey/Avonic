using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages.Nav;

/// <summary>A page of the nav sample: a title for the status line, and its will-enter reported for the timing.</summary>
public class NavSampleView : UserControl
{
    public NavSampleView()
    {
        AvonicNav.AddIonViewWillEnterHandler(this, (_, _) => WillEnter?.Invoke(this));
    }

    /// <summary>Told when any sample page is about to enter.</summary>
    internal static Action<NavSampleView>? WillEnter { get; set; }

    /// <summary>What the status line calls the page.</summary>
    public virtual string Title => GetType().Name;
}

/// <summary>The settings page's view model, which the nav's data template turns into a <see cref="NavSettingsView"/>.</summary>
public sealed class NavSettingsViewModel
{
    public string Heading => "Settings";

    public string Summary => "Built from a view model by the nav's data template; this text is bound to it.";
}
