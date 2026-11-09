using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Tests.Behaviors;

/// <summary>
/// The watch that the progress bar, the skeleton text and the img keep on a hidden chain: <c>display: none</c> on an ancestor,
/// which Avalonia doesn't tell a control about.
/// </summary>
public class HiddenChainWatcherTests
{
    /// <summary>outer → inner → leaf, in a shown window.</summary>
    private sealed class Chain : IDisposable
    {
        public readonly Border Outer = new();
        public readonly Border Inner = new();
        public readonly Border Leaf = new() { Width = 10, Height = 10 };
        public int Shown;
        public readonly HiddenChainWatcher Watcher;
        private readonly Scene _scene;

        public Chain()
        {
            Inner.Child = Leaf;
            Outer.Child = Inner;
            _scene = new Scene(Outer);
            Watcher = new HiddenChainWatcher(() => Shown++);
        }

        public void Dispose()
        {
            Watcher.Dispose();
            _scene.Dispose();
        }
    }

    [AvaloniaFact]
    public void Nothing_Hidden_Is_Nothing_To_Wait_For()
    {
        using var chain = new Chain();

        chain.Watcher.Watch(chain.Leaf);

        Assert.False(chain.Watcher.IsWaiting);
        Assert.Equal(0, chain.Shown);
    }

    [AvaloniaFact]
    public void Calls_Back_Once_When_A_Hidden_Ancestor_Shows()
    {
        using var chain = new Chain();
        chain.Inner.IsVisible = false;

        chain.Watcher.Watch(chain.Leaf);
        Assert.True(chain.Watcher.IsWaiting);

        chain.Inner.IsVisible = true;
        Assert.Equal(1, chain.Shown);
        Assert.False(chain.Watcher.IsWaiting);

        // It stopped watching: hiding and showing again calls nothing.
        chain.Inner.IsVisible = false;
        chain.Inner.IsVisible = true;
        Assert.Equal(1, chain.Shown);
    }

    [AvaloniaFact]
    public void Watches_The_Visual_Itself_Too()
    {
        using var chain = new Chain();
        chain.Leaf.IsVisible = false;

        chain.Watcher.Watch(chain.Leaf);
        chain.Leaf.IsVisible = true;

        Assert.Equal(1, chain.Shown);
    }

    [AvaloniaFact]
    public void Waits_Until_Everything_Hidden_In_The_Chain_Shows()
    {
        using var chain = new Chain();
        chain.Outer.IsVisible = false;
        chain.Leaf.IsVisible = false;

        chain.Watcher.Watch(chain.Leaf);
        chain.Leaf.IsVisible = true;
        Assert.Equal(0, chain.Shown);
        Assert.True(chain.Watcher.IsWaiting);

        chain.Outer.IsVisible = true;
        Assert.Equal(1, chain.Shown);
    }

    [AvaloniaFact]
    public void Another_Property_Calls_Nothing()
    {
        using var chain = new Chain();
        chain.Inner.IsVisible = false;
        chain.Watcher.Watch(chain.Leaf);

        chain.Inner.Opacity = 0.5;
        chain.Leaf.Opacity = 0.5;

        Assert.Equal(0, chain.Shown);
        Assert.True(chain.Watcher.IsWaiting);
    }

    [AvaloniaFact]
    public void Watching_Again_While_It_Waits_Changes_Nothing()
    {
        using var chain = new Chain();
        chain.Inner.IsVisible = false;
        chain.Watcher.Watch(chain.Leaf);

        // As a frame that finds it still hidden does: what's hidden now isn't added.
        chain.Outer.IsVisible = false;
        chain.Watcher.Watch(chain.Leaf);

        chain.Inner.IsVisible = true;
        Assert.Equal(1, chain.Shown);
    }

    [AvaloniaFact]
    public void Dispose_Stops_It_Without_Calling_Back_And_It_Can_Watch_Again()
    {
        using var chain = new Chain();
        chain.Inner.IsVisible = false;
        chain.Watcher.Watch(chain.Leaf);

        chain.Watcher.Dispose();
        Assert.False(chain.Watcher.IsWaiting);
        chain.Inner.IsVisible = true;
        Assert.Equal(0, chain.Shown);

        chain.Inner.IsVisible = false;
        chain.Watcher.Watch(chain.Leaf);
        chain.Inner.IsVisible = true;
        Assert.Equal(1, chain.Shown);
    }

    [AvaloniaFact]
    public void The_Callback_Can_Watch_Again()
    {
        var outer = new Border();
        var leaf = new Border { Width = 10, Height = 10 };
        outer.Child = leaf;
        using var scene = new Scene(outer);
        HiddenChainWatcher? watcher = null;
        var shown = 0;
        watcher = new HiddenChainWatcher(() =>
        {
            shown++;
            leaf.IsVisible = false;
            watcher!.Watch(leaf);
        });

        outer.IsVisible = false;
        watcher.Watch(leaf);
        outer.IsVisible = true;
        Assert.Equal(1, shown);
        Assert.True(watcher.IsWaiting);

        leaf.IsVisible = true;
        Assert.Equal(2, shown);
        watcher.Dispose();
    }
}
