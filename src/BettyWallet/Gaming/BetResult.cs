namespace BettyWallet.Gaming;

public enum BetOutcome
{
    Lose,
    SmallWin,
    BigWin,
}

/// <param name="WinAmount">Gross amount paid out for the round (zero when lost); net profit is WinAmount - Stake.</param>
public sealed record BetResult(decimal Stake, BetOutcome Outcome, decimal WinAmount);
