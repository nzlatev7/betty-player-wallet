using BettyWallet.Domain;

namespace BettyWallet.Gaming;

/// <summary>
/// One round of the game: validates the bet, draws the outcome and payout, then settles with the wallet.
/// The outcome is chosen by a roll in [0, 100): below the lose chance loses, the next small-win-chance rolls
/// win small, the rest win big. A win multiplier is drawn uniformly in hundredths from (low, high].
/// </summary>
public sealed class Game
{
    private const int Percent = 100;
    private const int MultiplierSteps = 100;

    private readonly Wallet _wallet;
    private readonly GameConfig _config;
    private readonly IRandomSource _random;

    public Game(Wallet wallet, GameConfig config, IRandomSource random)
    {
        config.Validate();
        _wallet = wallet;
        _config = config;
        _random = random;
    }

    public BetResult Play(decimal stake)
    {
        Money.EnsureValidAmount(stake);
        if (stake < _config.MinBet || stake > _config.MaxBet)
        {
            throw new RuleViolationException(
                $"Bet must be between {Money.Format(_config.MinBet)} and {Money.Format(_config.MaxBet)}.");
        }

        var result = Draw(stake);
        _wallet.Settle(stake, result.WinAmount);
        return result;
    }

    private BetResult Draw(decimal stake)
    {
        var roll = _random.Next(0, Percent);

        if (roll < _config.LoseChancePercent)
        {
            return new BetResult(stake, BetOutcome.Lose, 0m);
        }

        if (roll < _config.LoseChancePercent + _config.SmallWinChancePercent)
        {
            return Win(stake, BetOutcome.SmallWin, 0, _config.SmallWinMaxMultiplier);
        }

        return Win(stake, BetOutcome.BigWin, _config.SmallWinMaxMultiplier, _config.BigWinMaxMultiplier);
    }

    private BetResult Win(decimal stake, BetOutcome outcome, int lowMultiplier, int highMultiplier)
    {
        var steps = _random.Next(lowMultiplier * MultiplierSteps + 1, highMultiplier * MultiplierSteps + 1);
        var win = Math.Max(Money.SmallestUnit, Money.Round(stake * steps / MultiplierSteps));
        return new BetResult(stake, outcome, win);
    }
}
