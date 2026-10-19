using System.Globalization;
using BettyWallet.Cli;

namespace BettyWallet.Tests;

public class CommandParserTests
{
    [Fact]
    public void ParsesEachCommand()
    {
        Assert.Equal(new DepositCommand(10m), Parse("deposit 10"));
        Assert.Equal(new WithdrawCommand(44.00m), Parse("withdraw 44.00"));
        Assert.Equal(new BetCommand(5.5m), Parse("bet 5.5"));
        Assert.Equal(new ExitCommand(), Parse("exit"));
    }

    [Theory]
    [InlineData("DEPOSIT 10")]
    [InlineData("  Deposit   10  ")]
    [InlineData("deposit\t10")]
    public void IsCaseAndWhitespaceTolerant(string line)
    {
        Assert.Equal(new DepositCommand(10m), Parse(line));
    }

    [Fact]
    public void AmountSyntax_IgnoresTheCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            Assert.Equal(new DepositCommand(10.5m), Parse("deposit 10.5"));
            AssertRejected("deposit 10,5");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void NegativeAndZeroAmounts_AreSyntacticallyValid_AndLeftToTheDomain()
    {
        Assert.Equal(new WithdrawCommand(-5m), Parse("withdraw -5"));
        Assert.Equal(new BetCommand(0m), Parse("bet 0"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hello")]
    [InlineData("deposit")]
    [InlineData("bet 1 2")]
    [InlineData("deposit abc")]
    [InlineData("deposit 1,000")]
    [InlineData("deposit 1e3")]
    [InlineData("deposit $5")]
    [InlineData("deposit 1.2.3")]
    [InlineData("deposit 99999999999999999999999999999999")]
    [InlineData("exit now")]
    public void InvalidInput_IsRejectedWithAnError(string? line)
    {
        AssertRejected(line);
    }

    private static Command Parse(string line)
    {
        Assert.True(CommandParser.TryParse(line, out var command, out var error), error);
        return command;
    }

    private static void AssertRejected(string? line)
    {
        Assert.False(CommandParser.TryParse(line, out var command, out var error));
        Assert.Null(command);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
