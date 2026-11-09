using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;

namespace Avonic.Sample.Pages;

[SamplePage("overlays", Order = 5)]
public partial class OverlaysPage : UserControl
{
    private int _taps;
    private int _presented;

    public OverlaysPage()
    {
        InitializeComponent();
        Standalone.IonBackdropTap += (_, _) => BackdropStatus.Text = $"IonBackdropTap {++_taps}";
        Tappable.IsCheckedChanged += (_, _) => Standalone.Tappable = Tappable.IsChecked == true;
        Visible.IsCheckedChanged += (_, _) => Standalone.Visible = Visible.IsChecked == true;
        Present.Click += (_, _) => Show(new SampleOverlay(this));
        PresentLocked.Click += (_, _) => Show(new SampleOverlay(this) { BackdropDismiss = false });
        PresentStill.Click += (_, _) => Show(new SampleOverlay(this) { Animated = false });
    }

    /// <summary>Presents an overlay, and says what it was dismissed with.</summary>
    internal async void Show(SampleOverlay overlay)
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        overlay.Number = ++_presented;
        var dismissed = overlay.OnDidDismiss();
        await overlay.Present(top);
        Status.Text = $"Overlay {overlay.Number} presented";
        var detail = await dismissed;
        Status.Text = $"Overlay {overlay.Number} dismissed, role {detail.Role ?? "none"}";
    }
}

/// <summary>
/// A TEST VEHICLE, not a control: the smallest overlay, through <see cref="AvonicOverlay"/>, until the real overlays come. A
/// backdrop, dismissing when <see cref="BackdropDismiss"/> is true, and a box with buttons and a text box to Tab through, with
/// alert's md animations: 150 ms, <c>ease-in-out</c>, the backdrop to the theme's backdrop opacity and the box from 0.01 and
/// <c>scale(0.9)</c>. The modal's focus trap: focus anywhere behind it comes back.
/// </summary>
internal sealed class SampleOverlay : AvonicOverlay
{
    public static readonly StyledProperty<bool> BackdropDismissProperty =
        AvaloniaProperty.Register<SampleOverlay, bool>(nameof(BackdropDismiss), defaultValue: true);

    private readonly OverlaysPage _page;
    private readonly AvonicBackdrop _backdrop = new();
    private readonly Border _box;
    private readonly TextBlock _title = new() { FontSize = 20, FontWeight = FontWeight.Medium, TextWrapping = TextWrapping.Wrap };

    public SampleOverlay(OverlaysPage page)
    {
        _page = page;
        _backdrop.Bind(AvonicBackdrop.TappableProperty, this.GetObservable(BackdropDismissProperty));
        var stack = new AvonicButton { Content = "Stack another" };
        stack.Click += (_, _) => _page.Show(new SampleOverlay(_page));
        var close = new AvonicButton { Content = "Dismiss" };
        close.Click += (_, _) => _ = Dismiss("closed", "cancel");
        _box = new Border
        {
            Width = 300,
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ZIndex = 10,
            [!Border.BackgroundProperty] = this.GetResourceObservable("Avonic.OverlayBackgroundColorBrush").ToBinding(),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    _title,
                    new TextBlock { Text = "A test vehicle for the overlay system.", TextWrapping = TextWrapping.Wrap },
                    new TextBox { PlaceholderText = "Tab through me" },
                    stack,
                    close,
                },
            },
        };
        Template = new FuncControlTemplate<SampleOverlay>((overlay, _) => new Panel { Children = { overlay._backdrop, overlay._box } });
    }

    public bool BackdropDismiss
    {
        get => GetValue(BackdropDismissProperty);
        set => SetValue(BackdropDismissProperty, value);
    }

    public int Number
    {
        get => _number;
        set
        {
            _number = value;
            _title.Text = BackdropDismiss ? $"Overlay {value}" : $"Overlay {value}, not dismissable by its backdrop";
        }
    }

    private int _number;

    protected override bool BackdropDismissEnabled => BackdropDismiss;

    protected override AnimationBuilder MdEnterAnimation => (baseElement, _) =>
    {
        var opacity = baseElement.TryFindResource("Avonic.BackdropOpacity", out var value) && value is double d ? d : 0.32;
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing("ease-in-out")
            .Duration(150)
            .AddAnimation(
                AvonicAnimation.Create().AddElement(_backdrop).FromTo("opacity", 0.01, opacity)
                    .BeforeStyles(new() { ["pointer-events"] = "none" }).AfterClearStyles(["pointer-events"]),
                AvonicAnimation.Create().AddElement(_box).Keyframes([
                    new AnimationKeyframe(0) { ["opacity"] = 0.01, ["transform"] = "scale(0.9)" },
                    new AnimationKeyframe(1) { ["opacity"] = 1, ["transform"] = "scale(1)" },
                ]));
    };

    protected override AnimationBuilder MdLeaveAnimation => (baseElement, _) =>
    {
        var opacity = baseElement.TryFindResource("Avonic.BackdropOpacity", out var value) && value is double d ? d : 0.32;
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing("ease-in-out")
            .Duration(150)
            .AddAnimation(
                AvonicAnimation.Create().AddElement(_backdrop).FromTo("opacity", opacity, 0),
                AvonicAnimation.Create().AddElement(_box).FromTo("opacity", 0.99, 0));
    };
}
