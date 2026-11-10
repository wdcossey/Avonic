using Avalonia;
using Avalonia.Data.Converters;

namespace Avonic.Components.Controls;

/// <summary>
/// For themes: an inset accordion's top and bottom corners, which Ionic sets apart (<c>border-start-start-radius</c> and the
/// rest), by its place in the group and its neighbours'. A rule sets one, and another the other, and the template makes the
/// <c>CornerRadius</c> from both with <see cref="ToCornerRadius"/>, as one rule can't set half a <c>CornerRadius</c>.
/// </summary>
public static class AccordionCorners
{
    /// <summary>The radius of both top corners.</summary>
    public static readonly AttachedProperty<double> TopProperty =
        AvaloniaProperty.RegisterAttached<AvonicAccordion, double>("Top", typeof(AccordionCorners));

    /// <summary>The radius of both bottom corners.</summary>
    public static readonly AttachedProperty<double> BottomProperty =
        AvaloniaProperty.RegisterAttached<AvonicAccordion, double>("Bottom", typeof(AccordionCorners));

    public static double GetTop(AvonicAccordion element) => element.GetValue(TopProperty);

    public static void SetTop(AvonicAccordion element, double value) => element.SetValue(TopProperty, value);

    public static double GetBottom(AvonicAccordion element) => element.GetValue(BottomProperty);

    public static void SetBottom(AvonicAccordion element, double value) => element.SetValue(BottomProperty, value);

    /// <summary>Top, then bottom, to a <see cref="CornerRadius"/>. The same on both sides, so it needs nothing for RTL.</summary>
    public static readonly IMultiValueConverter ToCornerRadius =
        new FuncMultiValueConverter<object?, CornerRadius>(values =>
        {
            var list = values.ToList();
            var top = list.Count > 0 && list[0] is double t ? t : 0;
            var bottom = list.Count > 1 && list[1] is double b ? b : 0;
            return new CornerRadius(top, top, bottom, bottom);
        });
}
