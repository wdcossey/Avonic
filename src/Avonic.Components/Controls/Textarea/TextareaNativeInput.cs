using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
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
/// <item>With <see cref="Cols"/>, or 20 columns, HTML's default, while <see cref="SizesToCharacters"/>, it wants
/// <c>ceil(cols × c) + 4</c> wide, where <c>c</c> is the <c>x</c> glyph's advance, or that rounded when it rounds up. That shows when
/// the host is as wide as its content: with <c>cols</c> and without <c>autoGrow</c> (with it the field is stretched, as
/// <c>width: 100%</c> beats it), and in an item's start or end slot. Chromium on Windows takes the <c>x</c> glyph when the font
/// gives it no average width, and keeps 4 px for a phone's overlay scroll bar, which <c>autoGrow</c>'s <c>overflow: hidden</c>
/// takes away; <c>autoGrow</c> also widens it to its longest line. Fitted to Chromium at 36 sizes and column counts, and 16
/// sizes of 20 columns: 84, 164 and 324 for 10, 20 and 40 columns of 16 px Roboto, 133 for 20 of 13 px.</item>
/// <item>Its lines are <c>line-height: normal</c> as Chromium works it out: the font's ascent, descent and gap, each rounded, which
/// is 19 for 16 px Roboto, where Avalonia's own is 18.75. At style priority, so an app's style wins, and an inherited value
/// doesn't, as <c>.textarea-wrapper</c> sets <c>line-height: normal</c>.</item>
/// </list>
/// </remarks>
public sealed class TextareaNativeInput : TextBox, INativeFieldAutomation
{
    /// <summary>HTML's <c>rows</c> when it isn't set.</summary>
    public const int DefaultRows = 2;

    /// <summary>HTML's <c>cols</c> when it isn't set, which counts when the textarea is as wide as its content.</summary>
    public const int DefaultCols = 20;

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
    private readonly NativeFieldAutomation _automation;
    private double _normal = double.NaN;
    private double _xAdvance;
    private bool _sizesToCharacters;
    private TextPresenter? _presenter;

    static TextareaNativeInput()
    {
        AffectsMeasure<TextareaNativeInput>(RowsProperty, ColsProperty, AutoGrowProperty, LineHeightProperty);

        // A <textarea> takes Return. Its line break is a line feed, which AvonicTextarea makes of whatever the text box types.
        AcceptsReturnProperty.OverrideDefaultValue<TextareaNativeInput>(true);
    }

    public TextareaNativeInput()
    {
        Bind(LineHeightProperty, _normalLineHeight, BindingPriority.Style);
        _automation = new NativeFieldAutomation(this);
        UpdateFontMetrics();
    }

    NativeFieldAutomation INativeFieldAutomation.Automation => _automation;

    /// <summary>
    /// Wants the width of <see cref="DefaultCols"/> columns when <see cref="Cols"/> is null, as Chromium's <c>&lt;textarea&gt;</c>
    /// does, rather than its text's: the textarea is <c>width: auto</c>, in an item's start or end slot.
    /// </summary>
    internal bool SizesToCharacters
    {
        get => _sizesToCharacters;
        set
        {
            if (_sizesToCharacters != value)
            {
                _sizesToCharacters = value;
                InvalidateMeasure();
            }
        }
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
            UpdateFontMetrics();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _presenter?.RemoveHandler(RequestBringIntoViewEvent, OnPresenterRequestBringIntoView);
        _presenter = e.NameScope.Find<TextPresenter>("PART_TextPresenter");

        // Avalonia's text presenter brings its caret into view whenever the caret moves, focused or not, and the caret moves to
        // the end of an assigned value. Chromium scrolls neither the page nor the textarea's own lines to a textarea that isn't
        // focused. At the presenter, as the field's own scroller is the first to act on it.
        _presenter?.AddHandler(RequestBringIntoViewEvent, OnPresenterRequestBringIntoView);
    }

    private void OnPresenterRequestBringIntoView(object? sender, RequestBringIntoViewEventArgs e)
    {
        if (!IsKeyboardFocusWithin)
        {
            e.Handled = true;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        var lineHeight = LineHeight is var set && double.IsFinite(set) && set > 0 ? set : _normal;
        var chrome = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;
        // A rows of 0 or less isn't valid HTML, which gives the default.
        var rows = (Rows is { } count && count > 0 ? count : DefaultRows) * lineHeight + chrome;

        var height = AutoGrow ? Math.Max(size.Height, rows) : rows;
        var columns = Cols is { } cols && cols > 0 ? cols : _sizesToCharacters ? DefaultCols : 0;
        if (columns == 0)
        {
            return new Size(size.Width, height);
        }

        // autoGrow's overflow: hidden leaves no room for a scroll bar, and its copy of the text, in the same grid cell, widens the
        // field to its longest line.
        var width = ColumnsWidth(columns) + (AutoGrow ? 0 : ColsGutter)
                    + Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;
        return new Size(AutoGrow ? Math.Max(width, size.Width) : width, height);
    }

    /// <summary>
    /// <c>cols</c> columns: <c>ceil(cols × c)</c>, where <c>c</c> is the <c>x</c> glyph's advance, or that rounded when it rounds
    /// up. Fitted to Chromium: 13 px Roboto's 6.45 stays, and 129 for 20 columns; 16 px's 7.94 is 8, and 160.
    /// </summary>
    private double ColumnsWidth(int columns) =>
        Math.Ceiling(columns * Math.Max(_xAdvance, Math.Round(_xAdvance, MidpointRounding.AwayFromZero)));

    private Typeface Typeface => new(FontFamily, FontStyle, FontWeight, FontStretch);

    /// <summary>
    /// Chromium's <c>line-height: normal</c>, the ascent, the descent and the gap each rounded on its own, and the <c>x</c> glyph's
    /// advance, for the columns. Worked out when the font changes, not on each measure.
    /// </summary>
    private void UpdateFontMetrics()
    {
        using var layout = new TextLayout("x", Typeface, FontSize, null);
        if (layout.TextLines.Count > 0)
        {
            var line = layout.TextLines[0];
            _normal = Math.Round(line.Baseline, MidpointRounding.AwayFromZero)
                      + Math.Round(line.Height - line.Baseline, MidpointRounding.AwayFromZero);
        }

        _xAdvance = layout.WidthIncludingTrailingWhitespace;
        _normalLineHeight.Set(_normal);
    }
}
