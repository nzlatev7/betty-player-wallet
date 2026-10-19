using System.Globalization;
using BettyWallet.Domain;

namespace BettyWallet.Tests;

public class MoneyTests
{
    [Fact]
    public void Format_ShowsTheDecimalsOwnScale_WithInvariantSeparators()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            Assert.Equal("$10", Money.Format(10m));
            Assert.Equal("$44.00", Money.Format(44.00m));
            Assert.Equal("$0.15", Money.Format(0.15m));
            Assert.Equal("$40.35", Money.Format(10m - 5m + 35.35m));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData(1.005, "1.01")]
    [InlineData(1.004, "1.00")]
    [InlineData(2.5, "2.50")]
    [InlineData(10, "10.00")]
    public void Round_RoundsToCents_MidpointsAwayFromZero_AndKeepsTwoDecimals(double value, string expected)
    {
        Assert.Equal(expected, Money.Round((decimal)value).ToString(CultureInfo.InvariantCulture));
    }
}
