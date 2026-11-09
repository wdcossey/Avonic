namespace Avonic.Components.Overlays;

/// <summary>
/// Ionic's <c>createLockController</c>: one holder at a time, the rest wait in order. Free, it's taken without yielding, so
/// <see cref="AvonicOverlay.Present()"/> runs on to its first frame at once.
/// </summary>
internal sealed class OverlayLock
{
    private Task _tail = Task.CompletedTask;

    internal async Task<IDisposable> Lock()
    {
        var previous = _tail;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _tail = release.Task;
        await previous;
        return new Releaser(release);
    }

    private sealed class Releaser(TaskCompletionSource release) : IDisposable
    {
        public void Dispose() => release.TrySetResult();
    }
}
