using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;
using Avonic.Components;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-app", Order = 65)]
public partial class AppPage : UserControl
{
    private readonly AvonicItem _first;
    private TopLevel? _topLevel;
    private IInputPane? _pane;
    private int _backs;

    public AppPage()
    {
        InitializeComponent();

        _first = new AvonicItem { Button = true, Content = new AvonicLabel { Content = "Row 1" } };
        Rows.Children.Add(_first);
        for (var i = 2; i <= 30; i++)
        {
            Rows.Children.Add(new AvonicItem { Content = new AvonicLabel { Content = $"Row {i}" } });
        }

        Rows.Children.Add(new AvonicItem { Content = new AvonicInput { Label = "The last row", LabelPlacement = InputLabelPlacement.Floating } });
        Ring.Click += (_, _) =>
        {
            _first.Focus();
            App.SetFocus(_first);
        };
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(HardwareBackButton.IonBackButtonEvent, OnIonBackButton);
        _topLevel?.AddHandler(KeyboardAssist.IonKeyboardDidShowEvent, OnKeyboardDidShow);
        _topLevel?.AddHandler(KeyboardAssist.IonKeyboardDidHideEvent, OnKeyboardDidHide);
        _pane = _topLevel?.InputPane;
        if (_pane is not null)
        {
            _pane.StateChanged += OnPaneChanged;
        }

        if (_topLevel is not null)
        {
            _topLevel.PropertyChanged += OnTopLevelChanged;
        }

        Main.PropertyChanged += OnMainChanged;
        ShowPane();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel?.RemoveHandler(HardwareBackButton.IonBackButtonEvent, OnIonBackButton);
        _topLevel?.RemoveHandler(KeyboardAssist.IonKeyboardDidShowEvent, OnKeyboardDidShow);
        _topLevel?.RemoveHandler(KeyboardAssist.IonKeyboardDidHideEvent, OnKeyboardDidHide);
        if (_pane is not null)
        {
            _pane.StateChanged -= OnPaneChanged;
            _pane = null;
        }

        if (_topLevel is not null)
        {
            _topLevel.PropertyChanged -= OnTopLevelChanged;
        }

        Main.PropertyChanged -= OnMainChanged;
        _topLevel = null;
    }

    private void OnPaneChanged(object? sender, InputPaneStateEventArgs e) => ShowPane();

    private void OnTopLevelChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty)
        {
            ShowPane();
        }
    }

    private void OnMainChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == AvonicContent.KeyboardOffsetProperty)
        {
            ShowPane();
        }
    }

    /// <summary>
    /// The probe for #189: the input pane's state and occluded rect, in the window's device-independent pixels, where its top is on
    /// the screen in physical pixels, the content's keyboard padding, and the window's client size.
    /// </summary>
    private void ShowPane()
    {
        if (_topLevel is not { } top)
        {
            return;
        }

        var size = top.ClientSize;
        var shims = AvonicConfig.InputShims ? "on" : "off";
        if (_pane is not { } pane)
        {
            Pane.Text = FormattableString.Invariant($"Input pane: none; shims {shims}; window {size.Width:0.#}x{size.Height:0.#}");
            return;
        }

        // Invariant, so that tools/real-window reads the numbers whatever the PC's culture.
        var r = pane.OccludedRect;
        var screenTop = top.PointToScreen(new Point(0, r.Y)).Y;
        Pane.Text = FormattableString.Invariant(
            $"Input pane: {pane.State}, at {r.X:0.#},{r.Y:0.#} {r.Width:0.#}x{r.Height:0.#}, screen top {screenTop}; padding {Main.KeyboardOffset:0.#}; shims {shims}; window {size.Width:0.#}x{size.Height:0.#}");
        Console.WriteLine($"AvonicApp: {Pane.Text}");
    }

    private void OnIonBackButton(object? sender, BackButtonEventArgs e)
    {
        if (TakeBack.IsChecked == true)
        {
            e.Register(10, _ => Status.Text = $"Back taken at priority 10 ({++_backs})");
        }
    }

    // Also to the log (logcat's DOTNET tag on Android), as the status line is under the keyboard.
    private void OnKeyboardDidShow(object? sender, KeyboardDidShowEventArgs e)
    {
        Status.Text = $"IonKeyboardDidShow, keyboardHeight {e.KeyboardHeight:0.#}";
        Console.WriteLine($"AvonicApp: {Status.Text}");
    }

    private void OnKeyboardDidHide(object? sender, RoutedEventArgs e)
    {
        Status.Text = "IonKeyboardDidHide";
        Console.WriteLine($"AvonicApp: {Status.Text}");
    }
}
