using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-col</c>: a cell of an <see cref="AvonicRow"/>. Its size, offset, push and pull are numbers of columns out of
/// <c>Avonic.GridColumns</c> (12), each with a value per breakpoint, as <c>col.js</c> works them out. The row's
/// <see cref="RowPanel"/> lays it out. Its padding is in the theme.
/// </summary>
[PseudoClasses(":breakpoint-sm", ":breakpoint-md", ":breakpoint-lg", ":breakpoint-xl")]
public partial class AvonicCol : ContentControl
{
    /// <summary><c>var(--ion-grid-columns, 12)</c>: the fallback is in <c>col.js</c>, so it's here.</summary>
    public const double DefaultColumns = 12;

    /// <summary>The resource key for <c>--ion-grid-columns</c>.</summary>
    public const string ColumnsKey = "Avonic.GridColumns";

    public static readonly StyledProperty<string?> SizeProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(Size));
    public static readonly StyledProperty<string?> SizeXsProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(SizeXs));
    public static readonly StyledProperty<string?> SizeSmProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(SizeSm));
    public static readonly StyledProperty<string?> SizeMdProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(SizeMd));
    public static readonly StyledProperty<string?> SizeLgProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(SizeLg));
    public static readonly StyledProperty<string?> SizeXlProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(SizeXl));

    public static readonly StyledProperty<string?> OffsetProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(Offset));
    public static readonly StyledProperty<string?> OffsetXsProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(OffsetXs));
    public static readonly StyledProperty<string?> OffsetSmProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(OffsetSm));
    public static readonly StyledProperty<string?> OffsetMdProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(OffsetMd));
    public static readonly StyledProperty<string?> OffsetLgProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(OffsetLg));
    public static readonly StyledProperty<string?> OffsetXlProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(OffsetXl));

    public static readonly StyledProperty<string?> PushProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(Push));
    public static readonly StyledProperty<string?> PushXsProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(PushXs));
    public static readonly StyledProperty<string?> PushSmProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(PushSm));
    public static readonly StyledProperty<string?> PushMdProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(PushMd));
    public static readonly StyledProperty<string?> PushLgProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(PushLg));
    public static readonly StyledProperty<string?> PushXlProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(PushXl));

    public static readonly StyledProperty<string?> PullProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(Pull));
    public static readonly StyledProperty<string?> PullXsProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(PullXs));
    public static readonly StyledProperty<string?> PullSmProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(PullSm));
    public static readonly StyledProperty<string?> PullMdProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(PullMd));
    public static readonly StyledProperty<string?> PullLgProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(PullLg));
    public static readonly StyledProperty<string?> PullXlProperty = AvaloniaProperty.Register<AvonicCol, string?>(nameof(PullXl));

    // In BREAKPOINTS order: '', xs, sm, md, lg, xl.
    private static readonly StyledProperty<string?>[] SizeProperties =
        [SizeProperty, SizeXsProperty, SizeSmProperty, SizeMdProperty, SizeLgProperty, SizeXlProperty];

    private static readonly StyledProperty<string?>[] OffsetProperties =
        [OffsetProperty, OffsetXsProperty, OffsetSmProperty, OffsetMdProperty, OffsetLgProperty, OffsetXlProperty];

    private static readonly StyledProperty<string?>[] PushProperties =
        [PushProperty, PushXsProperty, PushSmProperty, PushMdProperty, PushLgProperty, PushXlProperty];

    private static readonly StyledProperty<string?>[] PullProperties =
        [PullProperty, PullXsProperty, PullSmProperty, PullMdProperty, PullLgProperty, PullXlProperty];

    private GridViewport? _viewport;
    private double _columns = DefaultColumns;

    static AvonicCol()
    {
        // overflow: visible. A ContentControl clips otherwise.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicCol>(false);
    }

    public AvonicCol()
    {
        // The column's own subscription, so it lives as long as the column, and follows it to any ancestor's resources.
        this.GetResourceObservable(ColumnsKey).Subscribe(new ColumnsObserver(this));
    }

    /// <summary>What <see cref="RowPanel"/> lays the column out as.</summary>
    internal ColumnKind Kind { get; private set; }

    /// <summary>A <see cref="ColumnKind.Fixed"/> column's width, as a fraction of the row.</summary>
    internal double Fraction { get; private set; }

    /// <summary><c>margin-inline-start</c>, as a fraction of the row.</summary>
    internal double OffsetFraction { get; private set; }

    /// <summary><c>left</c> or <c>right</c> on <c>position: relative</c>, as a fraction of the row, towards the end.</summary>
    internal double ShiftFraction { get; private set; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property.OwnerType == typeof(AvonicCol) && change.Property is StyledProperty<string?>)
        {
            UpdateColumn();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _viewport = GridViewport.Of(this);
        if (_viewport is not null)
        {
            _viewport.Changed += UpdateColumn;
        }

        UpdateColumn();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_viewport is not null)
        {
            _viewport.Changed -= UpdateColumn;
            _viewport = null;
        }
    }

    /// <summary><c>render()</c>: the inline style, from the props that match the viewport.</summary>
    private void UpdateColumn()
    {
        var breakpoint = _viewport?.Breakpoint ?? 0;
        GridViewport.SetPseudoClasses(PseudoClasses, breakpoint);

        var (kind, fraction) = CalculateSize(GetColumns(SizeProperties, breakpoint));
        var offset = Columns(GetColumns(OffsetProperties, breakpoint)) ?? 0;

        // Push sets left and pull right in LTR, and the other way round in RTL. With both, CSS ignores right in LTR and left
        // in RTL, so push wins either way. An invalid one is unset, which leaves the other.
        var shift = Columns(GetColumns(PushProperties, breakpoint)) ?? -Columns(GetColumns(PullProperties, breakpoint)) ?? 0;

        if (kind == Kind && fraction == Fraction && offset == OffsetFraction && shift == ShiftFraction)
        {
            return;
        }

        Kind = kind;
        Fraction = fraction;
        OffsetFraction = offset;
        ShiftFraction = shift;
        this.FindAncestorOfType<RowPanel>()?.InvalidateMeasure();
    }

    /// <summary>
    /// <c>getColumns</c>: the last breakpoint at or below the viewport with a value. The plain prop and <c>xs</c> always match,
    /// and <c>xs</c> comes later. An empty string is a value.
    /// </summary>
    private string? GetColumns(StyledProperty<string?>[] properties, int breakpoint)
    {
        string? matched = null;
        for (var i = 0; i < properties.Length; i++)
        {
            if (Math.Max(0, i - 1) <= breakpoint && GetValue(properties[i]) is { } columns)
            {
                matched = columns;
            }
        }

        return matched;
    }

    /// <summary>
    /// <c>calculateSize</c>. Nothing, or an empty string, keeps <c>col.css</c>. Anything else goes into
    /// <c>calc(n / var(--ion-grid-columns, 12) * 100%)</c>, where a value that isn't a number is invalid at computed-value
    /// time, which unsets <c>flex</c>, <c>width</c> and <c>max-width</c>.
    /// </summary>
    private (ColumnKind Kind, double Fraction) CalculateSize(string? columns)
    {
        if (string.IsNullOrEmpty(columns))
        {
            return (ColumnKind.Grow, 0);
        }

        if (columns == "auto")
        {
            return (ColumnKind.Auto, 0);
        }

        return Columns(columns) is { } fraction ? (ColumnKind.Fixed, fraction) : (ColumnKind.Content, 0);
    }

    /// <summary><c>calc(calc(n / var(--ion-grid-columns, 12)) * 100%)</c>, as a fraction, or null if it's no length.</summary>
    private double? Columns(string? columns) =>
        !string.IsNullOrEmpty(columns) && TryParseNumber(columns, out var n) ? n / _columns : null;

    /// <summary>A CSS <c>&lt;number&gt;</c>, with the spaces <c>calc()</c> allows around it.</summary>
    internal static bool TryParseNumber(string text, out double value)
    {
        value = 0;
        return CssNumber().IsMatch(text)
            && double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value);
    }

    [GeneratedRegex(@"^\s*[+-]?(\d*\.\d+|\d+)([eE][+-]?\d+)?\s*$")]
    private static partial Regex CssNumber();

    private void OnColumnsChanged(object? value)
    {
        var columns = value switch
        {
            string text when TryParseNumber(text, out var parsed) => parsed,
            IConvertible convertible and not string and not bool => SafeToDouble(convertible),
            _ => DefaultColumns,
        };

        _columns = columns > 0 && double.IsFinite(columns) ? columns : DefaultColumns;
        UpdateColumn();
    }

    private static double SafeToDouble(IConvertible value)
    {
        try
        {
            return value.ToDouble(CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return DefaultColumns;
        }
    }

    private sealed class ColumnsObserver(AvonicCol owner) : IObserver<object?>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(object? value) => owner.OnColumnsChanged(value);
    }

    public string? Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    public string? SizeXs { get => GetValue(SizeXsProperty); set => SetValue(SizeXsProperty, value); }

    public string? SizeSm { get => GetValue(SizeSmProperty); set => SetValue(SizeSmProperty, value); }

    public string? SizeMd { get => GetValue(SizeMdProperty); set => SetValue(SizeMdProperty, value); }

    public string? SizeLg { get => GetValue(SizeLgProperty); set => SetValue(SizeLgProperty, value); }

    public string? SizeXl { get => GetValue(SizeXlProperty); set => SetValue(SizeXlProperty, value); }

    public string? Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    public string? OffsetXs { get => GetValue(OffsetXsProperty); set => SetValue(OffsetXsProperty, value); }

    public string? OffsetSm { get => GetValue(OffsetSmProperty); set => SetValue(OffsetSmProperty, value); }

    public string? OffsetMd { get => GetValue(OffsetMdProperty); set => SetValue(OffsetMdProperty, value); }

    public string? OffsetLg { get => GetValue(OffsetLgProperty); set => SetValue(OffsetLgProperty, value); }

    public string? OffsetXl { get => GetValue(OffsetXlProperty); set => SetValue(OffsetXlProperty, value); }

    public string? Push { get => GetValue(PushProperty); set => SetValue(PushProperty, value); }

    public string? PushXs { get => GetValue(PushXsProperty); set => SetValue(PushXsProperty, value); }

    public string? PushSm { get => GetValue(PushSmProperty); set => SetValue(PushSmProperty, value); }

    public string? PushMd { get => GetValue(PushMdProperty); set => SetValue(PushMdProperty, value); }

    public string? PushLg { get => GetValue(PushLgProperty); set => SetValue(PushLgProperty, value); }

    public string? PushXl { get => GetValue(PushXlProperty); set => SetValue(PushXlProperty, value); }

    public string? Pull { get => GetValue(PullProperty); set => SetValue(PullProperty, value); }

    public string? PullXs { get => GetValue(PullXsProperty); set => SetValue(PullXsProperty, value); }

    public string? PullSm { get => GetValue(PullSmProperty); set => SetValue(PullSmProperty, value); }

    public string? PullMd { get => GetValue(PullMdProperty); set => SetValue(PullMdProperty, value); }

    public string? PullLg { get => GetValue(PullLgProperty); set => SetValue(PullLgProperty, value); }

    public string? PullXl { get => GetValue(PullXlProperty); set => SetValue(PullXlProperty, value); }
}

/// <summary>How <see cref="RowPanel"/> lays out a column, from <c>col.css</c> and the inline style <c>col.js</c> sets.</summary>
internal enum ColumnKind
{
    /// <summary>No size: <c>flex: 1 1 0; width: 100%; max-width: 100%</c>.</summary>
    Grow,

    /// <summary>A number: <c>flex: 0 0 n%; width: n%; max-width: n%</c>.</summary>
    Fixed,

    /// <summary><c>"auto"</c>: <c>flex: 0 0 auto; width: auto</c>, and <c>max-width: 100%</c>, as <c>auto</c> is invalid there.</summary>
    Auto,

    /// <summary>Not a number: the inline style is invalid, so <c>flex: 0 1 auto; width: auto; max-width: none</c>.</summary>
    Content,
}
