namespace BettyWallet.Gaming;

/// <summary>The only source of randomness in the game, so tests can script every draw.</summary>
public interface IRandomSource
{
    /// <summary>Returns an integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
    int Next(int minInclusive, int maxExclusive);
}

public sealed class SystemRandomSource : IRandomSource
{
    public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);
}
