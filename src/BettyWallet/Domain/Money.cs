using System.Globalization;

namespace BettyWallet.Domain;

/// <summary>Currency rules: amounts are decimals with at most two fractional digits (cents).</summary>
public static class Money
{
    public const int Decimals = 2;
    public const decimal SmallestUnit = 0.01m;

    public static bool HasValidPrecision(decimal amount) => decimal.Round(amount, Decimals) == amount;

    /// <summary>Rounds to cents (midpoints away from zero); the result always carries two decimals, e.g. 10.00.</summary>
    public static decimal Round(decimal amount) =>
        decimal.Round(amount, Decimals, MidpointRounding.AwayFromZero) + 0.00m;

    /// <summary>
    /// Formats with the decimal's own scale, as in the task's sample: "$10" for a deposit typed as 10,
    /// "$44.00" for a withdrawal typed as 44.00, "$40.35" for a payout-driven balance.
    /// </summary>
    public static string Format(decimal amount) => "$" + amount.ToString(CultureInfo.InvariantCulture);

    /// <summary>Input amounts must be strictly positive and must not carry sub-cent precision.</summary>
    public static void EnsureValidAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new RuleViolationException("Amount must be greater than zero.");
        }

        if (!HasValidPrecision(amount))
        {
            throw new RuleViolationException($"Amount must have at most {Decimals} decimal places.");
        }
    }
}
