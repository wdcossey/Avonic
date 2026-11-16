using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-app", Order = 65)]
public partial class AppPage : UserControl
{
    private readonly AvonicItem _first;
    private TopLevel? _topLevel;
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
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel?.RemoveHandler(HardwareBackButton.IonBackButtonEvent, OnIonBackButton);
        _topLevel?.RemoveHandler(KeyboardAssist.IonKeyboardDidShowEvent, OnKeyboardDidShow);
        _topLevel?.RemoveHandler(KeyboardAssist.IonKeyboardDidHideEvent, OnKeyboardDidHide);
        _topLevel = null;
    }

    private void OnIonBackButton(object? sender, BackButtonEventArgs e)
    {
        if (TakeBack.IsChecked == true)
        {
            e.Register(10, _ => Status.Text = $"Back taken at priority 10 ({++_backs})");
        }
    }

    private void OnKeyboardDidShow(object? sender, KeyboardDidShowEventArgs e) =>
        Status.Text = $"IonKeyboardDidShow, keyboardHeight {e.KeyboardHeight:0.#}";

    private void OnKeyboardDidHide(object? sender, RoutedEventArgs e) => Status.Text = "IonKeyboardDidHide";
}
