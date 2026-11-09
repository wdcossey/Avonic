using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Avonic.Sample;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // MainView, not Android's MainViewFactory, so that AvonicOverlay.Present() finds its TopLevel through the single-view
            // lifetime, which is the branch to try on a phone. See docs/infra/android/PLAN.md.
            singleView.MainView = new SampleShell();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
