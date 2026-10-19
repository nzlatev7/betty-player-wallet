using System.Globalization;
using BettyWallet.Domain;

namespace BettyWallet.Tests;

public class MoneyTests
{
    [Theory]
    [InlineData(0, "$0.00")]
    [InlineData(10, "$10.00")]
    [InlineData(0.15, "$0.15")]
    [InlineData(1234.5, "$1234.50")]
    public void Format_AlwaysShowsTwoDecimals_WithInvariantSeparators(double amount, string expected)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            Assert.Equal(expected, Money.Format((decimal)amount));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData(1.005, 1.01)]
    [InlineData(1.004, 1.00)]
    [InlineData(2.5, 2.50)]
    public void Round_RoundsToCents_MidpointsAwayFromZero(double value, double expected)
    {
        Assert.Equal((decimal)expected, Money.Round((decimal)value));
    }
}
