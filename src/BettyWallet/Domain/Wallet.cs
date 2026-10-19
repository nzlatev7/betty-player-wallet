namespace BettyWallet.Domain;

/// <summary>
/// A single player's balance. Every operation validates first and commits last,
/// so a rejected operation leaves the balance untouched. It knows nothing about games or randomness.
/// </summary>
public sealed class Wallet
{
    public decimal Balance { get; private set; }

    public void Deposit(decimal amount)
    {
        Money.EnsureValidAmount(amount);
        EnsureWithinCapacity(Balance, amount);

        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        Money.EnsureValidAmount(amount);
        EnsureFunds(amount);

        Balance -= amount;
    }

    /// <summary>
    /// Settles one game round in a single step: new balance = old balance - stake + win.
    /// <paramref name="winAmount"/> is the gross win (zero for a lost round), not the net profit.
    /// </summary>
    public void Settle(decimal stake, decimal winAmount)
    {
        Money.EnsureValidAmount(stake);
        if (winAmount < 0 || !Money.HasValidPrecision(winAmount))
        {
            throw new ArgumentOutOfRangeException(nameof(winAmount), winAmount, "Win must be zero or a positive amount in cents.");
        }

        EnsureFunds(stake);
        var balanceAfterStake = Balance - stake;
        EnsureWithinCapacity(balanceAfterStake, winAmount);

        Balance = balanceAfterStake + winAmount;
    }

    private void EnsureFunds(decimal amount)
    {
        if (amount > Balance)
        {
            throw new RuleViolationException($"Insufficient funds for {Money.Format(amount)}.");
        }
    }

    private static void EnsureWithinCapacity(decimal balance, decimal increase)
    {
        if (increase > decimal.MaxValue - balance)
        {
            throw new RuleViolationException("The resulting balance exceeds the maximum supported amount.");
        }
    }
}
