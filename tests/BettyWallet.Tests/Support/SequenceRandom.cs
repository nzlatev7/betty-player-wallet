using BettyWallet.Gaming;

namespace BettyWallet.Tests.Support;

/// <summary>
/// Returns scripted values in order and records the range requested for each draw.
/// A scripted value outside the requested range fails the test, so a wrong script cannot pass silently.
/// </summary>
public sealed class SequenceRandom(params int[] values) : IRandomSource
{
    private readonly Queue<int> _values = new(values);

    public List<(int Min, int Max)> Requests { get; } = [];

    public int Next(int minInclusive, int maxExclusive)
    {
        Requests.Add((minInclusive, maxExclusive));

        var value = _values.Dequeue();
        Assert.InRange(value, minInclusive, maxExclusive - 1);
        return value;
    }
}
