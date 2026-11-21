using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;
using Avonic.Themes;

namespace UiaRepro;

/// <summary>
/// #254's repro: a phone-sized window whose "pages" change while a UIA client walks it. The pages are the same in every mode; only
/// the container differs:
/// <list type="bullet">
/// <item><c>--mode nav</c>: an <see cref="AvonicNav"/>; each page has "Push a page" and "Pop this page" (md's transition, or
/// none with <c>--unanimated</c>).</item>
/// <item><c>--mode hide</c>: a plain <see cref="Panel"/> of two pages, B over A; "Toggle" flips B's <c>IsVisible</c>.</item>
/// <item><c>--mode remove</c>: the same, and "Toggle" removes B from the panel or adds it back.</item>
/// </list>
/// <c>--auto MS</c> does the change itself every MS ms (push and pop in turn for the nav), with no client input.
/// <c>--content plain</c> builds the pages from stock <c>Button</c>s and <c>TextBlock</c>s instead of Avonic's item, list and
/// content. <c>--log DIR</c> is where the logs go. "Status: ..." under the pages says what changed, and when.
/// </summary>
internal static class Program
{
    public static Options Options { get; private set; } = new();

    [STAThread]
    public static int Main(string[] args)
    {
        Options = Options.Parse(args);
        Log.Start(Options.LogDir);
        Log.Event($"start: {string.Join(' ', args)}; Avalonia {typeof(Application).Assembly.GetName().Version}");
        if (!Options.NoHooks)
        {
            try
            {
                Hooks.Install();
            }
            catch (Exception ex)
            {
                Log.Event($"HOOKS FAILED: {ex}");
            }
        }

        if (Options.HooksOnly)
        {
            // A check that the hooks install and see a call, with no window: run.ps1's first step.
            Hooks.SelfTest();
            Log.Flush();
            var events = File.ReadAllText(Path.Combine(Options.LogDir, "app-events.log"));
            var dump = Directory.GetFiles(Options.LogDir, "app-dump-*.log").Select(File.ReadAllText).FirstOrDefault() ?? "";
            return events.Contains("hooks installed: 13") && dump.Contains("GetRuntimeId on Border#selftest") ? 0 : 1;
        }

        try
        {
            return AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Log.Event("exit");
            Log.Flush();
        }
    }
}

internal sealed class Options
{
    public string Mode { get; private set; } = "nav";

    public string Content { get; private set; } = "avonic";

    public int AutoMs { get; private set; }

    public bool Animated { get; private set; } = true;

    public bool NoHooks { get; private set; }

    public bool HooksOnly { get; private set; }

    public int Rows { get; private set; } = 12;

    public string LogDir { get; private set; } = Path.Combine(Path.GetTempPath(), "uia-repro");

    public static Options Parse(string[] args)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
            switch (args[i])
            {
                case "--mode": o.Mode = Next(); break;
                case "--content": o.Content = Next(); break;
                case "--auto": o.AutoMs = int.Parse(Next()); break;
                case "--unanimated": o.Animated = false; break;
                case "--no-hooks": o.NoHooks = true; break;
                case "--hooks-only": o.HooksOnly = true; break;
                case "--rows": o.Rows = int.Parse(Next()); break;
                case "--log": o.LogDir = Next(); break;
                default: throw new ArgumentException($"unknown argument {args[i]}");
            }
        }

        if (o.Mode is not ("nav" or "hide" or "remove"))
        {
            throw new ArgumentException($"--mode is nav, hide or remove, not {o.Mode}");
        }

        return o;
    }
}

internal sealed class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new Avalonia.Themes.Simple.SimpleTheme());
        Styles.Add(new AvonicTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Log.UiThreadId = Environment.CurrentManagedThreadId;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new ReproWindow(Program.Options);
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class ReproWindow : Window
{
    private readonly Options _o;
    private readonly TextBlock _status = new() { Name = "Status", Margin = new Thickness(16, 8), TextWrapping = TextWrapping.Wrap };
    private readonly Panel _panel = new() { Name = "Pages" };
    private readonly AvonicNav? _nav;
    private Control? _b;
    private int _pageCount;
    private int _changes;

    public ReproWindow(Options o)
    {
        _o = o;
        Title = $"UiaRepro {o.Mode} {o.Content}";
        Width = 412;
        Height = 860;
        var dock = new DockPanel();
        DockPanel.SetDock(_status, Dock.Bottom);
        dock.Children.Add(_status);
        if (o.Mode == "nav")
        {
            _nav = new AvonicNav { Name = "Nav", Animated = o.Animated };
            _nav.IonNavWillChange += (_, _) => Log.Event("nav: ionNavWillChange");
            _nav.IonNavDidChange += (_, _) =>
            {
                Log.Event($"nav: ionNavDidChange, length {_nav.GetLength()}");
                SetStatus($"Stack: {_nav.GetLength()}");
            };
            _nav.Root = new Func<Control>(() => Page());
            dock.Children.Add(_nav);
        }
        else
        {
            var toggle = new Button { Name = "ToggleButton", Content = "Toggle", Margin = new Thickness(16, 8) };
            toggle.Click += (_, _) => Toggle("click");
            DockPanel.SetDock(toggle, Dock.Top);
            dock.Children.Add(toggle);
            _panel.Children.Add(Page());
            _b = Page();
            _panel.Children.Add(_b);
            dock.Children.Add(_panel);
            SetStatus("Toggled 0: B shown");
        }

        Content = dock;
        if (o.AutoMs > 0)
        {
            var push = true;
            DispatcherTimer.Run(() =>
            {
                if (_nav is not null)
                {
                    Log.Event(push ? "auto: push" : "auto: pop");
                    _ = push ? _nav.Push(new Func<Control>(() => Page())) : _nav.Pop();
                    push = !push;
                }
                else
                {
                    Toggle("auto");
                }

                return true;
            }, TimeSpan.FromMilliseconds(o.AutoMs));
        }
    }

    private void SetStatus(string text)
    {
        _changes++;
        _status.Text = $"Status: {text} (change {_changes} at {Log.Now})";
    }

    private void Toggle(string why)
    {
        var b = _b!;
        if (_o.Mode == "hide")
        {
            b.IsVisible = !b.IsVisible;
            Log.Event($"{why}: B.IsVisible = {b.IsVisible}");
            SetStatus($"Toggled: B {(b.IsVisible ? "shown" : "hidden")}");
        }
        else if (b.Parent is null)
        {
            _panel.Children.Add(b);
            Log.Event($"{why}: B added");
            SetStatus("Toggled: B added");
        }
        else
        {
            _panel.Children.Remove(b);
            Log.Event($"{why}: B removed");
            SetStatus("Toggled: B removed");
        }
    }

    /// <summary>A page: a title, the nav's buttons, and rows of buttons and text, from Avonic's controls or stock ones.</summary>
    private Control Page()
    {
        var n = ++_pageCount;
        var name = _nav is not null ? $"page{n}" : n == 1 ? "A" : "B";
        var rows = new StackPanel { Name = $"{name}-rows" };
        if (_nav is not null)
        {
            rows.Children.Add(NavButton("Push a page", () => _ = _nav.Push(new Func<Control>(() => Page()))));
            rows.Children.Add(NavButton("Pop this page", () => _ = _nav.Pop()));
        }

        for (var i = 1; i <= _o.Rows; i++)
        {
            rows.Children.Add(Row($"Row {i} of {name}"));
        }

        Control body = _o.Content == "plain"
            ? new ScrollViewer { Content = rows }
            : new AvonicContent { Content = rows };
        var title = new TextBlock { Text = $"Page {name}", FontSize = 20, Margin = new Thickness(16) };
        DockPanel.SetDock(title, Dock.Top);
        var page = new Border { Name = name, Background = Brushes.White, Child = new DockPanel { Children = { title, body } } };
        page.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.IsVisibleProperty)
            {
                Log.Event($"page {name}: IsVisible = {e.NewValue}");
            }
        };
        page.AttachedToVisualTree += (_, _) => Log.Event($"page {name}: attached");
        page.DetachedFromVisualTree += (_, _) => Log.Event($"page {name}: detached");
        return page;
    }

    private Control NavButton(string text, Action action)
    {
        if (_o.Content == "plain")
        {
            var b = new Button { Content = text, Margin = new Thickness(16, 4) };
            b.Click += (_, _) => { Log.Event($"click: {text}"); action(); };
            return b;
        }

        var link = new AvonicButton { Content = text, Fill = ButtonFill.Outline };
        link.Click += (_, _) => { Log.Event($"click: {text}"); action(); };
        return link;
    }

    private Control Row(string text) =>
        _o.Content == "plain"
            ? new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { new Button { Content = text }, new TextBlock { Text = "note", Margin = new Thickness(8, 0) } },
            }
            : new AvonicItem { Button = true, Detail = true, Content = new AvonicLabel { Content = text } };
}
