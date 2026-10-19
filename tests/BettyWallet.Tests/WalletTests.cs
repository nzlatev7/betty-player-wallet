using BettyWallet.Domain;

namespace BettyWallet.Tests;

public class WalletTests
{
    [Fact]
    public void NewWallet_StartsWithZeroBalance()
    {
        Assert.Equal(0m, new Wallet().Balance);
    }

    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var wallet = new Wallet();

        wallet.Deposit(10m);
        wallet.Deposit(0.50m);

        Assert.Equal(10.50m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_DecreasesBalance_AndMayEmptyTheWallet()
    {
        var wallet = WalletWith(20m);

        wallet.Withdraw(5.25m);
        wallet.Withdraw(14.75m);

        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_MoreThanBalance_IsRejected_AndBalanceIsUnchanged()
    {
        var wallet = WalletWith(10m);

        var ex = Assert.Throws<RuleViolationException>(() => wallet.Withdraw(10.01m));

        Assert.Contains("Insufficient funds", ex.Message);
        Assert.Equal(10m, wallet.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    [InlineData(1.001)]
    [InlineData(0.005)]
    public void InvalidAmounts_AreRejected_AndBalanceIsUnchanged(double raw)
    {
        var amount = (decimal)raw;
        var wallet = WalletWith(50m);

        Assert.Throws<RuleViolationException>(() => wallet.Deposit(amount));
        Assert.Throws<RuleViolationException>(() => wallet.Withdraw(amount));
        Assert.Throws<RuleViolationException>(() => wallet.Settle(amount, 0m));

        Assert.Equal(50m, wallet.Balance);
    }

    [Fact]
    public void Deposit_ThatWouldOverflow_IsRejected_AndBalanceIsUnchanged()
    {
        var wallet = WalletWith(decimal.MaxValue);

        Assert.Throws<RuleViolationException>(() => wallet.Deposit(1m));

        Assert.Equal(decimal.MaxValue, wallet.Balance);
    }

    [Fact]
    public void Settle_LostRound_DeductsOnlyTheStake()
    {
        var wallet = WalletWith(20m);

        wallet.Settle(stake: 10m, winAmount: 0m);

        Assert.Equal(10m, wallet.Balance);
    }

    [Fact]
    public void Settle_WonRound_AppliesNewBalanceFormula()
    {
        var wallet = WalletWith(10m);

        wallet.Settle(stake: 5m, winAmount: 35.35m);

        Assert.Equal(40.35m, wallet.Balance);
    }

    [Fact]
    public void Settle_StakeAboveBalance_IsRejected_EvenIfTheWinWouldCoverIt()
    {
        var wallet = WalletWith(4.99m);

        Assert.Throws<RuleViolationException>(() => wallet.Settle(stake: 5m, winAmount: 20m));

        Assert.Equal(4.99m, wallet.Balance);
    }

    [Fact]
    public void Settle_ThatWouldOverflow_IsRejected_AndBalanceIsUnchanged()
    {
        var wallet = WalletWith(decimal.MaxValue);

        Assert.Throws<RuleViolationException>(() => wallet.Settle(stake: 1m, winAmount: 5m));

        Assert.Equal(decimal.MaxValue, wallet.Balance);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.005)]
    public void Settle_InvalidWin_IsAProgrammingError_AndBalanceIsUnchanged(double raw)
    {
        var wallet = WalletWith(10m);

        Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Settle(5m, (decimal)raw));

        Assert.Equal(10m, wallet.Balance);
    }

    private static Wallet WalletWith(decimal balance)
    {
        var wallet = new Wallet();
        wallet.Deposit(balance);
        return wallet;
    }
}
