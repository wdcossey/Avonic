using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Logging;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Tests.Behaviors;

/// <summary>
/// The by-name setter that <c>ion-nav</c>'s <c>componentProps</c> and <c>ion-select</c>'s <c>interfaceOptions</c> share: what both
/// do, and each rule only one of them has.
/// </summary>
public class PropertiesByNameTests
{
    private static readonly PropertiesByName.Rules Plain = new("[test] props");

    private static Dictionary<string, object?> Values(params (string Key, object? Value)[] pairs) =>
        pairs.ToDictionary(pair => pair.Key, pair => pair.Value);

    // ── What both do ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Keys_In_Either_Case_Set_Avalonia_Properties_With_Strings_Converted_As_Xaml_Would()
    {
        var border = new Border();
        PropertiesByName.Apply(border, Values(
            ("width", "120"),
            ("Height", 50),
            ("horizontalAlignment", "left"),
            ("flowDirection", "right-to-left"),
            ("Tag", "mine")), Plain);

        Assert.Equal(120, border.Width);
        Assert.Equal(50, border.Height);
        Assert.Equal(HorizontalAlignment.Left, border.HorizontalAlignment);
        Assert.Equal(Avalonia.Media.FlowDirection.RightToLeft, border.FlowDirection);
        Assert.Equal("mine", border.Tag);

        // A type only its TypeConverter reads a string as, not IConvertible.
        var probe = new Probe();
        PropertiesByName.Apply(probe, Values(("delay", "00:00:02")), Plain);
        Assert.Equal(TimeSpan.FromSeconds(2), probe.GetValue(Probe.DelayProperty));
    }

    [AvaloniaFact]
    public void Unknown_Keys_Read_Only_Properties_And_Values_That_Dont_Fit_Are_Ignored()
    {
        var border = new Border { Tag = "before", Height = 30 };
        PropertiesByName.Apply(border, null, Plain);
        PropertiesByName.Apply(border, Values(
            ("nothing", 1),
            ("", 1),
            ("bounds", new Rect(0, 0, 10, 10)),
            ("width", "wide"),
            ("height", null),
            ("tag", null),
            ("minWidth", 5.0)), Plain);

        Assert.True(double.IsNaN(border.Width));
        Assert.Equal(30, border.Height);
        Assert.Equal(default, border.Bounds);
        Assert.Null(border.Tag);
        Assert.Equal(5, border.MinWidth);
    }

    [AvaloniaFact]
    public void A_Value_The_Property_Refuses_Is_Logged_And_The_Keys_After_It_Still_Set()
    {
        // Either way a lookup error goes: the select's and the nav's.
        foreach (var lookupErrorsLogged in new[] { false, true })
        {
            var probe = new Probe();
            using var sink = Sink.Start();
            PropertiesByName.Apply(probe, Values(("positive", -1), ("width", 10.0), ("throwing", "x"), ("height", 20.0)),
                Plain with { ClrProperties = true, LogLookupErrors = lookupErrorsLogged });

            Assert.Equal(1, probe.Positive);
            Assert.Equal(10, probe.Width);
            Assert.Equal(20, probe.Height);
            Assert.Contains(sink.From(probe), m => m.Template.StartsWith("[test] props") && Equals(m.Key, "positive"));
            Assert.Contains(sink.From(probe), m => Equals(m.Key, "throwing"));
        }
    }

    // ── The select's rule: its own keys skipped, Avalonia properties only ─────────────────────

    [AvaloniaFact]
    public void Skip_Leaves_Out_The_Keys_The_Caller_Reads_Itself()
    {
        var border = new Border();
        var rules = Plain with { Skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "width" } };
        PropertiesByName.Apply(border, Values(("Width", 10.0), ("height", 10.0)), rules);

        Assert.True(double.IsNaN(border.Width));
        Assert.Equal(10, border.Height);
    }

    [AvaloniaFact]
    public void Clr_Properties_Are_Set_Only_When_Asked()
    {
        var off = new Probe();
        PropertiesByName.Apply(off, Values(("count", "7"), ("Label", "seven")), Plain);
        Assert.Equal(0, off.Count);
        Assert.Null(off.Label);

        var on = new Probe();
        PropertiesByName.Apply(on, Values(("count", "7"), ("Label", "seven"), ("privatelySet", "x")), Plain with { ClrProperties = true });
        Assert.Equal(7, on.Count);
        Assert.Equal("seven", on.Label);

        // Public settable only.
        Assert.Null(on.PrivatelySet);
    }

    // ── The nav's rules: CLR properties after Avalonia's, drops and lookup errors logged ─────

    [AvaloniaFact]
    public void An_Avalonia_Property_Comes_Before_A_Clr_Property_Of_The_Name_And_A_Read_Only_One_Gives_Way()
    {
        var probe = new Probe();
        PropertiesByName.Apply(probe, Values(("shadowed", "styled"), ("mixed", "abc"), ("fallen", "clr")),
            Plain with { ClrProperties = true });

        // The Avalonia property is set, and not the CLR property that hides its name.
        Assert.Equal("styled", probe.GetValue(Probe.ShadowedProperty));
        Assert.Null(probe.Shadowed);

        // A value the Avalonia property can't take isn't given to a CLR property of the name instead.
        Assert.Equal(0, probe.GetValue(Probe.MixedProperty));
        Assert.Null(probe.Mixed);

        // A read-only Avalonia property isn't one to set: the CLR property of the name is.
        Assert.Equal("clr", probe.FallenClr);
    }

    [AvaloniaFact]
    public void A_Value_That_Doesnt_Convert_Is_Logged_Only_When_Asked()
    {
        var quiet = new Border();
        using (var sink = Sink.Start())
        {
            PropertiesByName.Apply(quiet, Values(("width", "wide")), Plain);
            Assert.Empty(sink.From(quiet));
        }

        var logged = new Border();
        using (var sink = Sink.Start())
        {
            PropertiesByName.Apply(logged, Values(("width", "wide")), Plain with { LogDropped = true });
            var message = Assert.Single(sink.From(logged));
            Assert.Equal("[test] props {0}: {1} isn't a value it takes", message.Template);
            Assert.Equal("width", message.Key);
        }
    }

    [AvaloniaFact]
    public void A_Key_Avalonias_Registry_Refuses_Leaves_Apply_Unless_Lookup_Errors_Are_Logged()
    {
        var thrown = new Border();
        Assert.Throws<InvalidOperationException>(() =>
            PropertiesByName.Apply(thrown, Values(("grid.row", 1), ("width", 10.0)), Plain));
        Assert.True(double.IsNaN(thrown.Width));

        var logged = new Border();
        using var sink = Sink.Start();
        PropertiesByName.Apply(logged, Values(("grid.row", 1), ("width", 10.0)), Plain with { LogLookupErrors = true });
        Assert.Equal(10, logged.Width);
        Assert.Contains(sink.From(logged), m => Equals(m.Key, "grid.row"));
    }

    private sealed class Probe : Control
    {
        public static readonly StyledProperty<int> PositiveProperty =
            AvaloniaProperty.Register<Probe, int>("Positive", 1, validate: value => value > 0);

        public static readonly StyledProperty<string?> ShadowedProperty = AvaloniaProperty.Register<Probe, string?>("Shadowed");

        public static readonly StyledProperty<TimeSpan> DelayProperty = AvaloniaProperty.Register<Probe, TimeSpan>("Delay");

        public static readonly StyledProperty<int> MixedProperty = AvaloniaProperty.Register<Probe, int>("Mixed");

        public static readonly DirectProperty<Probe, string?> FallenProperty =
            AvaloniaProperty.RegisterDirect<Probe, string?>("Fallen", probe => probe.FallenClr);

        public int Positive => GetValue(PositiveProperty);

        public int Count { get; set; }

        public string? Label { get; set; }

        public string? PrivatelySet { get; private set; }

        /// <summary>A CLR property with the name of an Avalonia property that isn't it.</summary>
        public string? Shadowed { get; set; }

        /// <summary>A CLR property of another type than the Avalonia property of its name.</summary>
        public string? Mixed { get; set; }

        public string? FallenClr { get; private set; }

        /// <summary>Settable, where the Avalonia property of its name is read-only.</summary>
        public string? Fallen
        {
            get => FallenClr;
            set => FallenClr = value;
        }

        public string? Throwing
        {
            get => null;
            set => throw new InvalidOperationException("no");
        }
    }

    private sealed record Message(object? Source, string Template, object? Key);

    /// <summary>Avalonia's log while it's open; other tests may log meanwhile, so read it by source.</summary>
    private sealed class Sink : ILogSink, IDisposable
    {
        private readonly ILogSink? _previous;
        private readonly List<Message> _messages = [];

        private Sink(ILogSink? previous) => _previous = previous;

        public static Sink Start()
        {
            var sink = new Sink(Logger.Sink);
            Logger.Sink = sink;
            return sink;
        }

        public List<Message> From(object source)
        {
            lock (_messages)
            {
                return _messages.Where(m => ReferenceEquals(m.Source, source)).ToList();
            }
        }

        public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
            Add(source, messageTemplate, null);

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Add(source, messageTemplate, propertyValues.Length > 0 ? propertyValues[0] : null);

        private void Add(object? source, string template, object? key)
        {
            lock (_messages)
            {
                _messages.Add(new Message(source, template, key));
            }
        }

        public void Dispose() => Logger.Sink = _previous;
    }
}
