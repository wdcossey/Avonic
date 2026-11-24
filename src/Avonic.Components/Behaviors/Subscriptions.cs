namespace Avonic.Components.Behaviors;

/// <summary>Subscriptions disposed as one, in their order: a colour's resource lookups, a datetime's wheel variables.</summary>
internal sealed class Subscriptions(IReadOnlyList<IDisposable> items) : IDisposable
{
    public void Dispose()
    {
        foreach (var item in items)
        {
            item.Dispose();
        }
    }
}
