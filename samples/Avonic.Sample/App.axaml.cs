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
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            // Android: a shell for each activity, as Avalonia recommends. AvonicOverlay.Present() finds the current activity's
            // TopLevel without a MainView (#114). See docs/infra/android/PLAN.md.
            activity.MainViewFactory = () => new SampleShell();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new SampleShell();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
