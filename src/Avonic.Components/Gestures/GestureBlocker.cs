namespace Avonic.Components.Gestures;

/// <summary>
/// Ionic's <c>BlockerDelegate</c>, from <see cref="GestureController.CreateBlocker"/>: while blocked, the gestures it names can't
/// start, and with <c>disableScroll</c> <see cref="GestureController.IsScrollDisabled"/> is true. <c>ion-menu</c> holds one for
/// its backdrop.
/// </summary>
public sealed class GestureBlocker : IDisposable
{
    private readonly int _id;
    private readonly string[]? _disable;
    private readonly bool _disableScroll;
    private GestureController? _ctrl;

    internal GestureBlocker(GestureController ctrl, int id, string[]? disable, bool disableScroll)
    {
        _ctrl = ctrl;
        _id = id;
        _disable = disable;
        _disableScroll = disableScroll;
    }

    /// <summary>Ionic <c>block()</c>.</summary>
    public void Block()
    {
        if (_ctrl is null)
        {
            return;
        }

        if (_disable is not null)
        {
            foreach (var gesture in _disable)
            {
                _ctrl.DisableGesture(gesture, _id);
            }
        }

        if (_disableScroll)
        {
            _ctrl.DisableScroll(_id);
        }
    }

    /// <summary>Ionic <c>unblock()</c>.</summary>
    public void Unblock()
    {
        if (_ctrl is null)
        {
            return;
        }

        if (_disable is not null)
        {
            foreach (var gesture in _disable)
            {
                _ctrl.EnableGesture(gesture, _id);
            }
        }

        if (_disableScroll)
        {
            _ctrl.EnableScroll(_id);
        }
    }

    /// <summary>Ionic <c>destroy()</c>: unblocks, and does nothing after.</summary>
    public void Destroy()
    {
        Unblock();
        _ctrl = null;
    }

    /// <summary>The same as <see cref="Destroy"/>.</summary>
    public void Dispose() => Destroy();
}
