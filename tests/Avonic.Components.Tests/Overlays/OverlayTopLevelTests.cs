using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avonic.Components.Overlays;
using static Avonic.Components.Tests.Overlays.AvonicOverlayTests;

namespace Avonic.Components.Tests.Overlays;

/// <summary>The open top levels are process-wide, so the tests that read them can't run beside tests that open windows.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OpenTopLevelsCollection
{
    public const string Name = "OpenTopLevels";
}

/// <summary>
/// O15, which top level <c>Present()</c> takes, for the lifetimes the tests can't have as the application's: Android's, under
/// Avalonia's <c>MainViewFactory</c> or with a <c>MainView</c>, and iOS's and the browser's single view. See
/// docs/infra/overlays/PLAN.md.
/// </summary>
[Collection(OpenTopLevelsCollection.Name)]
public class OverlayTopLevelTests
{
    /// <summary>
    /// Android with Avalonia's <c>MainViewFactory</c>, which makes a view for each activity and leaves <c>MainView</c> null: the
    /// current activity's top level is the last to come that's still open, as a second activity on top comes after the first, and
    /// one that's finished closes.
    /// </summary>
    [AvaloniaFact]
    public void O15_Present_Under_A_Main_View_Factory_Uses_The_Last_Open_Top_Level()
    {
        using var stage = new Stage();
        var android = LifetimeDouble.Android();
        ((IActivityApplicationLifetime)android).MainViewFactory = () => new Panel();
        Assert.Null(((ISingleViewApplicationLifetime)android).MainView);
        Assert.Same(stage.Window, OverlayHost.TopLevelOf(android));

        var second = new Scene(new Panel());
        Assert.Same(second.Window, OverlayHost.TopLevelOf(android));

        // A template applied again, as for a new theme, isn't a new top level.
        var applied = false;
        stage.Window.TemplateApplied += (_, _) => applied = true;
        stage.Window.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Window>((_, _) => new VisualLayerManager());
        stage.Window.ApplyTemplate();
        Assert.True(applied);
        Assert.Same(second.Window, OverlayHost.TopLevelOf(android));

        second.Dispose();
        Assert.Same(stage.Window, OverlayHost.TopLevelOf(android));
    }

    /// <summary>
    /// What Android's <c>AvaloniaView</c> does with each activity's view: an <c>EmbeddableControlRoot</c>, prepared and rendering,
    /// disposed with the activity, under <c>SimpleTheme</c>, as the sample's. The headless platform makes no embeddable top level,
    /// so it's on a headless window's platform view.
    /// </summary>
    [AvaloniaFact]
    public async Task O15_An_Embedded_Root_Is_The_Current_Top_Level_Until_It_Is_Disposed()
    {
        using var stage = new Stage();
        var android = LifetimeDouble.Android();
        var root = new EmbeddableControlRoot(PlatformManager.CreateWindow()) { Content = new Panel() };
        root.Prepare();

        // There once its template is applied, in Prepare(), before it loads: the view in it is attached after, and can present.
        Assert.False(root.IsLoaded);
        Assert.Same(root, OverlayHost.TopLevelOf(android));

        root.StartRendering();
        Stage.Settle();
        Assert.Same(root, OverlayHost.TopLevelOf(android));

        // Its template has the overlay layer, as the window's has.
        var overlay = new TestOverlay();
        var present = overlay.Present(root);
        stage.At(Finished);
        await present;
        Assert.Same(root, TopLevel.GetTopLevel(overlay));
        Assert.True(overlay.IsShown);

        root.StopRendering();
        root.Dispose();
        Assert.Same(stage.Window, OverlayHost.TopLevelOf(android));
    }

    /// <summary>An activity finished before its root loads, as with <c>finish()</c> in <c>OnCreate</c>, is gone too.</summary>
    [AvaloniaFact]
    public void O15_A_Root_Disposed_Before_It_Loads_Is_Gone()
    {
        using var stage = new Stage();
        var root = new EmbeddableControlRoot(PlatformManager.CreateWindow()) { Content = new Panel() };
        root.Prepare();
        Assert.Same(root, OverlayHost.TopLevelOf(LifetimeDouble.Android()));

        root.Dispose();
        Assert.False(root.IsLoaded);
        Assert.Same(stage.Window, OverlayHost.TopLevelOf(LifetimeDouble.Android()));
    }

    /// <summary>
    /// A main view the app gives Android's lifetime is every activity's, so it comes before the last open. iOS's and the browser's
    /// single view has no activities, so with no main view there's nowhere.
    /// </summary>
    [AvaloniaFact]
    public void O15_A_Main_View_Comes_Before_The_Last_Open_Top_Level()
    {
        using var stage = new Stage();
        var android = LifetimeDouble.Android();
        ((ISingleViewApplicationLifetime)android).MainView = stage.Other;
        using var second = new Scene(new Panel());
        Assert.Same(stage.Window, OverlayHost.TopLevelOf(android));

        // Out of every top level, as while an activity is recreated: the current activity's, still.
        ((ISingleViewApplicationLifetime)android).MainView = new Border();
        Assert.Same(second.Window, OverlayHost.TopLevelOf(android));

        var singleView = (ISingleViewApplicationLifetime)LifetimeDouble.SingleView();
        Assert.Null(OverlayHost.TopLevelOf(singleView));
        singleView.MainView = new Border();
        Assert.Null(OverlayHost.TopLevelOf(singleView));
        singleView.MainView = stage.Other;
        Assert.Same(stage.Window, OverlayHost.TopLevelOf(singleView));
    }

    /// <summary>
    /// Only Android's lifetime reads the open top levels, so a desktop and a single view don't listen. The tests' application has
    /// no lifetime, which listens, as it can't tell.
    /// </summary>
    [AvaloniaFact]
    public void O15_Only_An_Activity_Lifetime_Listens_For_Top_Levels()
    {
        Assert.False(OpenTopLevels.Start(DesktopLifetime()));
        Assert.False(OpenTopLevels.Start(LifetimeDouble.SingleView()));
        Assert.Null(Avalonia.Application.Current!.ApplicationLifetime);
    }
}
