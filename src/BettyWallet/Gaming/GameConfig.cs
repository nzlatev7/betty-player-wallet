using BettyWallet.Domain;

namespace BettyWallet.Gaming;

/// <summary>Game rules. Defaults are the rules from the task; <see cref="Validate"/> rejects inconsistent values.</summary>
public sealed record GameConfig
{
    public decimal MinBet { get; init; } = 1m;
    public decimal MaxBet { get; init; } = 10m;

    public int LoseChancePercent { get; init; } = 50;
    public int SmallWinChancePercent { get; init; } = 40;
    public int BigWinChancePercent { get; init; } = 10;

    /// <summary>A small win pays between 0 (exclusive) and this multiple of the stake (inclusive).</summary>
    public int SmallWinMaxMultiplier { get; init; } = 2;

    /// <summary>A big win pays between <see cref="SmallWinMaxMultiplier"/> (exclusive) and this multiple (inclusive).</summary>
    public int BigWinMaxMultiplier { get; init; } = 10;

    public void Validate()
    {
        if (MinBet <= 0 || !Money.HasValidPrecision(MinBet) || !Money.HasValidPrecision(MaxBet) || MaxBet < MinBet)
        {
            throw new ArgumentException("Bet limits must be positive amounts in cents with MinBet <= MaxBet.");
        }

        if (LoseChancePercent < 0 || SmallWinChancePercent < 0 || BigWinChancePercent < 0
            || LoseChancePercent + SmallWinChancePercent + BigWinChancePercent != 100)
        {
            throw new ArgumentException("Outcome chances must be non-negative and add up to 100 percent.");
        }

        if (SmallWinMaxMultiplier < 1 || BigWinMaxMultiplier <= SmallWinMaxMultiplier)
        {
            throw new ArgumentException("Multipliers must satisfy 1 <= SmallWinMaxMultiplier < BigWinMaxMultiplier.");
        }
    }
}
