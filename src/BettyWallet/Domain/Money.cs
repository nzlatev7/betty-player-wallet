using System.Globalization;

namespace BettyWallet.Domain;

/// <summary>Currency rules: amounts are decimals with at most two fractional digits (cents).</summary>
public static class Money
{
    public const int Decimals = 2;
    public const decimal SmallestUnit = 0.01m;

    public static bool HasValidPrecision(decimal amount) => decimal.Round(amount, Decimals) == amount;

    public static decimal Round(decimal amount) => decimal.Round(amount, Decimals, MidpointRounding.AwayFromZero);

    public static string Format(decimal amount) =>
        "$" + amount.ToString("F" + Decimals, CultureInfo.InvariantCulture);

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
