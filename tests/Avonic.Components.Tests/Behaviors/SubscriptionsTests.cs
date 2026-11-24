using Avonic.Components.Behaviors;

namespace Avonic.Components.Tests.Behaviors;

public class SubscriptionsTests
{
    [Fact]
    public void Disposing_Disposes_Each_In_Order()
    {
        var disposed = new List<int>();
        IDisposable[] array = [new Disposed(disposed, 1), new Disposed(disposed, 2)];
        new Subscriptions(array).Dispose();
        new Subscriptions(new List<IDisposable> { new Disposed(disposed, 3) }).Dispose();

        Assert.Equal([1, 2, 3], disposed);
    }

    private sealed class Disposed(List<int> log, int id) : IDisposable
    {
        public void Dispose() => log.Add(id);
    }
}
