using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// The native <c>&lt;textarea&gt;</c> of <see cref="AvonicTextarea"/>: a <see cref="TextBox"/> that takes Return, and sizes itself as
/// Chromium sizes a <c>&lt;textarea&gt;</c>, measured. See <c>docs/components/ion-textarea/PLAN.md</c>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>It's <see cref="Rows"/> lines high, 2 when that's null, and scrolls past them; with <see cref="AutoGrow"/>, as many lines
/// as its text has, never fewer. Arranged taller, as in a 56 px row, it fills the room, as the stretched <c>&lt;textarea&gt;</c> does.</item>
/// <item>With <see cref="Cols"/>, it wants <c>Cols</c> × the <c>x</c> glyph's advance, rounded, + 4 wide, which shows when the host is
/// as wide as its content (without <c>autoGrow</c>; with it the field is stretched, as <c>width: 100%</c> beats it):
/// Chromium on Windows takes the <c>x</c> glyph when the font gives it no average width, and keeps 4 px for a phone's
/// overlay scroll bar. Measured 84, 164 and 324 for 10, 20 and 40 columns of 16 px Roboto.</item>
/// <item>Its lines are <c>line-height: normal</c> as Chromium works it out: the font's ascent, descent and gap, each rounded, which
/// is 19 for 16 px Roboto, where Avalonia's own is 18.75. At style priority, so an app's style wins, and an inherited value
/// doesn't, as <c>.textarea-wrapper</c> sets <c>line-height: normal</c>.</item>
/// </list>
/// </remarks>
public sealed class TextareaNativeInput : TextBox
{
    /// <summary>HTML's <c>rows</c> when it isn't set.</summary>
    public const int DefaultRows = 2;

    /// <summary>What Chromium adds to the columns' width in a phone's viewport: its overlay scroll bar's room. Measured.</summary>
    public const double ColsGutter = 4;

    /// <summary>The textarea's <c>rows</c>: how many lines high. Null is 2.</summary>
    public static readonly StyledProperty<int?> RowsProperty =
        AvaloniaProperty.Register<TextareaNativeInput, int?>(nameof(Rows));

    /// <summary>The textarea's <c>cols</c>: how many average characters wide, when the host is as wide as its content.</summary>
    public static readonly StyledProperty<int?> ColsProperty =
        AvaloniaProperty.Register<TextareaNativeInput, int?>(nameof(Cols));

    /// <summary>Ionic's <c>autoGrow</c>: as many lines as the text has.</summary>
    public static readonly StyledProperty<bool> AutoGrowProperty =
        AvaloniaProperty.Register<TextareaNativeInput, bool>(nameof(AutoGrow));

    private readonly StyleValue<double> _normalLineHeight = new();

    static TextareaNativeInput()
    {
        AffectsMeasure<TextareaNativeInput>(RowsProperty, ColsProperty, AutoGrowProperty, LineHeightProperty);

        // A <textarea> takes Return, and its line break is a line feed, whatever the platform's.
        AcceptsReturnProperty.OverrideDefaultValue<TextareaNativeInput>(true);
        NewLineProperty.OverrideDefaultValue<TextareaNativeInput>("\n");
    }

    public TextareaNativeInput()
    {
        Bind(LineHeightProperty, _normalLineHeight, BindingPriority.Style);
        UpdateNormalLineHeight();
    }

    /// <summary>The look of a <see cref="TextBox"/>, as an app's own <c>TextBox</c> styles reach the input's field too.</summary>
    protected override Type StyleKeyOverride => typeof(TextBox);

    public int? Rows
    {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public int? Cols
    {
        get => GetValue(ColsProperty);
        set => SetValue(ColsProperty, value);
    }

    public bool AutoGrow
    {
        get => GetValue(AutoGrowProperty);
        set => SetValue(AutoGrowProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FontFamilyProperty || change.Property == FontSizeProperty || change.Property == FontStyleProperty
            || change.Property == FontWeightProperty || change.Property == FontStretchProperty)
        {
            UpdateNormalLineHeight();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        var lineHeight = LineHeight is var set && double.IsFinite(set) && set > 0 ? set : NormalLineHeight();
        var chrome = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;
        // A rows of 0 or less isn't valid HTML, which gives the default.
        var rows = (Rows is { } count && count > 0 ? count : DefaultRows) * lineHeight + chrome;

        var height = AutoGrow ? Math.Max(size.Height, rows) : rows;
        var width = Cols is { } cols && cols > 0
            ? Math.Round(Advance("x")) * cols + ColsGutter + Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right
            : size.Width;
        return new Size(width, height);
    }

    private void UpdateNormalLineHeight() => _normalLineHeight.Set(NormalLineHeight());

    private Typeface Typeface => new(FontFamily, FontStyle, FontWeight, FontStretch);

    /// <summary>Chromium's <c>line-height: normal</c>: the ascent, the descent and the gap, each rounded on its own.</summary>
    private double NormalLineHeight()
    {
        using var layout = new TextLayout("x", Typeface, FontSize, null);
        if (layout.TextLines.Count == 0)
        {
            return double.NaN;
        }

        var line = layout.TextLines[0];
        return Math.Round(line.Baseline, MidpointRounding.AwayFromZero)
               + Math.Round(line.Height - line.Baseline, MidpointRounding.AwayFromZero);
    }

    private double Advance(string text)
    {
        using var layout = new TextLayout(text, Typeface, FontSize, null);
        return layout.WidthIncludingTrailingWhitespace;
    }
}
