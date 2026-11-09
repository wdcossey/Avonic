using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;

namespace Avonic.Components.Tests.Overlays;

/// <summary>
/// The smallest overlay: a backdrop, tappable when <see cref="BackdropDismiss"/> is, and a box with what it's given. Its md
/// animations are alert's: the backdrop from 0.01 to 0.32 and the box from 0.01 to 1, 150 ms, <c>ease-in-out</c>.
/// </summary>
internal sealed class TestOverlay : AvonicOverlay
{
    public static readonly StyledProperty<bool> BackdropDismissProperty =
        AvaloniaProperty.Register<TestOverlay, bool>(nameof(BackdropDismiss), defaultValue: true);

    public TestOverlay(params Control[] content)
    {
        Backdrop = new AvonicBackdrop();
        Backdrop.Bind(AvonicBackdrop.TappableProperty, this.GetObservable(BackdropDismissProperty));
        Box = new StackPanel { Width = 200, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, ZIndex = 10 };
        Box.Children.AddRange(content);
        Before = new Border { Focusable = true, Width = 1, Height = 1 };
        After = new Border { Focusable = true, Width = 1, Height = 1 };
        Template = new FuncControlTemplate<TestOverlay>((overlay, _) => Scoped
            ? new Panel { Children = { overlay.Backdrop, overlay.Before, overlay.Box, overlay.After } }
            : new Panel { Children = { overlay.Backdrop, overlay.Box } });
    }

    public AvonicBackdrop Backdrop { get; }

    public StackPanel Box { get; }

    /// <summary>The sentinels of a scoped overlay, around its wrapper.</summary>
    public Border Before { get; }

    public Border After { get; }

    public bool BackdropDismiss
    {
        get => GetValue(BackdropDismissProperty);
        set => SetValue(BackdropDismissProperty, value);
    }

    /// <summary>A toast's: no focus taken, given back or trapped.</summary>
    public bool Steals { get; init; } = true;

    public bool Trap { get; init; } = true;

    /// <summary>The scoped variant of the trap, with <see cref="Box"/> as its wrapper and two sentinels.</summary>
    public bool Scoped { get; init; }

    public int Base { get; init; } = 20000;

    public object? Options { get; init; }

    public List<object?> OptionsSeen { get; } = [];

    protected internal override bool BackdropDismissEnabled => BackdropDismiss;

    protected internal override bool StealsFocus => Steals;

    protected internal override bool FocusTrapEnabled => Trap;

    protected internal override Control? FocusTrapWrapper => Scoped ? Box : null;

    protected override int ZIndexBase => Base;

    protected override object? AnimationOptions => Options;

    protected override AnimationBuilder MdEnterAnimation => (baseElement, options) =>
    {
        OptionsSeen.Add(options);
        var overlay = (TestOverlay)baseElement;
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing("ease-in-out")
            .Duration(150)
            .AddAnimation(
                AvonicAnimation.Create().AddElement(overlay.Backdrop).FromTo("opacity", 0.01, 0.32)
                    .BeforeStyles(new() { ["pointer-events"] = "none" }).AfterClearStyles(["pointer-events"]),
                AvonicAnimation.Create().AddElement(overlay.Box).FromTo("opacity", 0.01, 1));
    };

    protected override AnimationBuilder MdLeaveAnimation => (baseElement, options) =>
    {
        OptionsSeen.Add(options);
        var overlay = (TestOverlay)baseElement;
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing("ease-in-out")
            .Duration(150)
            .AddAnimation(
                AvonicAnimation.Create().AddElement(overlay.Backdrop).FromTo("opacity", 0.32, 0),
                AvonicAnimation.Create().AddElement(overlay.Box).FromTo("opacity", 0.99, 0));
    };

    /// <summary>A button for the box.</summary>
    public static Button Button(string name) => new() { Name = name, Content = name, Height = 40 };
}
