using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// A utility <see cref="AvonicApp"/> starts for a <see cref="TopLevel"/>, counted, so that two apps in one top level, or one moved
/// to another, start it once and stop it when the last is done: <paramref name="attach"/> on the first start,
/// <paramref name="detach"/> when every start has been disposed.
/// </summary>
internal sealed class TopLevelStarts<TState>(Func<TopLevel, TState> create, Action<TState> attach, Action<TState> detach)
    where TState : class
{
    private readonly ConditionalWeakTable<TopLevel, Entry> _entries = new();
    private readonly Action<TState> _detach = detach;

    /// <summary>The top level's state while it's started.</summary>
    public TState? Started(TopLevel topLevel) =>
        _entries.TryGetValue(topLevel, out var entry) && entry.Count > 0 ? entry.State : null;

    public IDisposable Start(TopLevel topLevel)
    {
        var entry = _entries.GetValue(topLevel, top => new Entry(create(top)));
        if (entry.Count++ == 0)
        {
            attach(entry.State);
        }

        return new Stop(this, entry);
    }

    private sealed class Entry(TState state)
    {
        public TState State { get; } = state;

        public int Count { get; set; }
    }

    private sealed class Stop(TopLevelStarts<TState> owner, Entry entry) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (--entry.Count == 0)
                {
                    owner._detach(entry.State);
                }
            }
        }
    }
}
