using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The keyboard half of <c>footer-toolbar-padding</c>: <c>!keyboardVisible</c>, from Ionic's keyboard controller. Measured in
/// Chromium with a stub Capacitor keyboard plugin (resize mode <c>ionic</c>) and <c>keyboardWillShow</c> and <c>keyboardWillHide</c>
/// dispatched on the window, <c>ion-app</c> 800 high: see the footer's README. A headless window has no input pane, so a fake one
/// stands for it, as Avalonia raises its <c>StateChanged</c> when the keyboard starts to open or close.
/// </summary>
public sealed class AvonicFooterKeyboardTests
{
    /// <summary>
    /// An input pane. Avalonia keeps <see cref="IInputPane"/> from being implemented in C#, so it's a proxy that answers each of its
    /// members.
    /// </summary>
    private sealed class FakePane
    {
        private EventHandler<InputPaneStateEventArgs>? _stateChanged;

        public FakePane()
        {
            Pane = DispatchProxy.Create<IInputPane, Proxy>();
            ((Proxy)(object)Pane).Owner = this;
        }

        public IInputPane Pane { get; }

        public InputPaneState State { get; private set; }

        public Rect OccludedRect { get; private set; }

        public void Set(InputPaneState state)
        {
            var start = OccludedRect;
            State = state;
            OccludedRect = state == InputPaneState.Open ? new Rect(0, 500, 390, 300) : default;
            _stateChanged?.Invoke(Pane, new InputPaneStateEventArgs(state, start, OccludedRect));
            Dispatcher.UIThread.RunJobs();
        }

        public class Proxy : DispatchProxy
        {
            public FakePane? Owner { get; set; }

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                var owner = Owner!;
                switch (targetMethod?.Name)
                {
                    case "get_State":
                        return owner.State;
                    case "get_OccludedRect":
                        return owner.OccludedRect;
                    case "add_StateChanged":
                        owner._stateChanged += (EventHandler<InputPaneStateEventArgs>)args![0]!;
                        return null;
                    case "remove_StateChanged":
                        owner._stateChanged -= (EventHandler<InputPaneStateEventArgs>)args![0]!;
                        return null;
                    default:
                        throw new NotSupportedException(targetMethod?.Name);
                }
            }
        }
    }

    /// <summary>Gives the scene's window the fake pane, and every other top level what it had.</summary>
    private sealed class PaneFor : IDisposable
    {
        private readonly Func<TopLevel, IInputPane?> _previous = KeyboardController.InputPaneOf;

        public PaneFor(TopLevel top, FakePane fake)
        {
            var pane = fake.Pane;
            var previous = _previous;
            KeyboardController.InputPaneOf = candidate => ReferenceEquals(candidate, top) ? pane : previous(candidate);
        }

        public void Dispose() => KeyboardController.InputPaneOf = _previous;
    }

    private static bool Padded(AvonicFooter footer) => ((IPseudoClasses)footer.Classes).Contains(AvonicFooter.FooterToolbarPadding);

    private static (Window Window, Panel Page) Window(double height = 800)
    {
        var page = new StackPanel();
        var window = new Window { Width = 390, Height = height, Content = page };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, page);
    }

    private static void Resize(Window window, double height)
    {
        window.Height = height;
        Dispatcher.UIThread.RunJobs();
    }

    // Measured: start true; keyboardWillShow false; keyboardWillHide with ion-app at its height, true at once.
    [AvaloniaFact]
    public void K1_The_Keyboard_Opening_Takes_The_Safe_Area_Away_And_Closing_Gives_It_Back()
    {
        var (window, page) = Window();
        var pane = new FakePane();
        using var hook = new PaneFor(window, pane);
        var footer = new AvonicFooter { Items = { new AvonicToolbar { Content = "F" } } };
        page.Children.Add(footer);
        Dispatcher.UIThread.RunJobs();

        Assert.True(Padded(footer));
        pane.Set(InputPaneState.Open);
        Assert.False(Padded(footer));
        pane.Set(InputPaneState.Closed);
        Assert.True(Padded(footer));
        window.Close();
    }

    // The padding rule follows the pseudoclass: the last toolbar's bottom padding is the safe area, and 0 while the keyboard's open.
    [AvaloniaFact]
    public void K1_The_Last_Toolbar_Loses_Its_Bottom_Padding_While_The_Keyboard_Is_Open() => InstalledTheme.Run(_ =>
    {
        var (window, page) = Window();
        page.Resources["Avonic.SafeAreaBottom"] = 20d;
        var pane = new FakePane();
        using var hook = new PaneFor(window, pane);
        var toolbar = new AvonicToolbar { Content = "F" };
        page.Children.Add(new AvonicFooter { Items = { toolbar } });
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(20, toolbar.Padding.Bottom);
        pane.Set(InputPaneState.Open);
        Assert.Equal(0, toolbar.Padding.Bottom);
        pane.Set(InputPaneState.Closed);
        Assert.Equal(20, toolbar.Padding.Bottom);
        window.Close();
    });

    // Measured: ion-app 500 while the keyboard is open, keyboardWillHide: still false; at 600, false; back at 800, true. The
    // height is the one from before the keyboard first opened.
    [AvaloniaFact]
    public void K2_Closing_After_The_App_Resized_Waits_For_It_To_Resize_Back()
    {
        var (window, page) = Window();
        var pane = new FakePane();
        using var hook = new PaneFor(window, pane);
        var footer = new AvonicFooter();
        page.Children.Add(footer);
        Dispatcher.UIThread.RunJobs();

        pane.Set(InputPaneState.Open);
        Resize(window, 500);
        pane.Set(InputPaneState.Closed);
        Assert.False(Padded(footer));

        Resize(window, 600);
        Assert.False(Padded(footer));

        Resize(window, 800);
        Assert.True(Padded(footer));

        // Opening again keeps the first height: closing at 800 gives it back at once.
        pane.Set(InputPaneState.Open);
        Assert.False(Padded(footer));
        pane.Set(InputPaneState.Closed);
        Assert.True(Padded(footer));
        window.Close();
    }

    // Measured: a footer attached while the keyboard is open has the padding, as the controller hears only changes; one detached
    // while it's open, and attached again after it closed, has none, as Ionic keeps keyboardVisible.
    [AvaloniaFact]
    public void K3_Only_Changes_Count_And_A_Detached_Footer_Hears_None()
    {
        var (window, page) = Window();
        var pane = new FakePane();
        using var hook = new PaneFor(window, pane);
        var footer = new AvonicFooter();
        page.Children.Add(footer);
        Dispatcher.UIThread.RunJobs();

        pane.Set(InputPaneState.Open);
        var late = new AvonicFooter();
        page.Children.Add(late);
        Dispatcher.UIThread.RunJobs();
        Assert.True(Padded(late));

        page.Children.Remove(footer);
        pane.Set(InputPaneState.Closed);
        page.Children.Add(footer);
        Dispatcher.UIThread.RunJobs();
        Assert.False(Padded(footer));
        window.Close();
    }

    // With no input pane, a desktop's, there are no events, as on the web: the padding stays.
    [AvaloniaFact]
    public void K4_With_No_Input_Pane_The_Padding_Stays()
    {
        var (window, page) = Window();
        var footer = new AvonicFooter();
        page.Children.Add(footer);
        Dispatcher.UIThread.RunJobs();

        Assert.Null(KeyboardController.InputPaneOf(window));
        Assert.True(Padded(footer));
        window.Close();
    }
}
