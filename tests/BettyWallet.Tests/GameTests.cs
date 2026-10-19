using BettyWallet.Domain;
using BettyWallet.Gaming;
using BettyWallet.Tests.Support;

namespace BettyWallet.Tests;

public class GameTests
{
    [Theory]
    [InlineData(0.99)]
    [InlineData(10.01)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1.001)]
    public void Bet_OutsideLimitsOrPrecision_IsRejected_WithoutDrawingOrChangingBalance(double raw)
    {
        var (game, wallet, random) = Create(100m);

        Assert.Throws<RuleViolationException>(() => game.Play((decimal)raw));

        Assert.Equal(100m, wallet.Balance);
        Assert.Empty(random.Requests);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void Bet_AtExactLimits_IsAccepted(int stake)
    {
        var (game, wallet, _) = Create(100m, 0);

        game.Play(stake);

        Assert.Equal(100m - stake, wallet.Balance);
    }

    [Fact]
    public void Bet_ExceedingBalance_IsRejected_AndBalanceIsUnchanged()
    {
        var (game, wallet, _) = Create(4m, 0);

        var ex = Assert.Throws<RuleViolationException>(() => game.Play(5m));

        Assert.Contains("Insufficient funds", ex.Message);
        Assert.Equal(4m, wallet.Balance);
    }

    [Theory]
    [InlineData(0, BetOutcome.Lose, 0)]
    [InlineData(49, BetOutcome.Lose, 0)]
    [InlineData(50, BetOutcome.SmallWin, 100)]
    [InlineData(89, BetOutcome.SmallWin, 100)]
    [InlineData(90, BetOutcome.BigWin, 500)]
    [InlineData(99, BetOutcome.BigWin, 500)]
    public void OutcomeBand_IsSelectedByExactRollThresholds(int roll, BetOutcome expected, int steps)
    {
        var (game, _, _) = expected == BetOutcome.Lose ? Create(100m, roll) : Create(100m, roll, steps);

        Assert.Equal(expected, game.Play(5m).Outcome);
    }

    [Fact]
    public void LostRound_PaysNothing_AndDeductsTheStake()
    {
        var (game, wallet, random) = Create(100m, 0);

        var result = game.Play(10m);

        Assert.Equal(0m, result.WinAmount);
        Assert.Equal(90m, wallet.Balance);
        Assert.Single(random.Requests);
    }

    [Theory]
    [InlineData(1, 0.10)]
    [InlineData(119, 11.90)]
    [InlineData(200, 20.00)]
    public void SmallWin_PaysBetweenZeroExclusiveAndTwiceTheStake(int steps, double expectedWin)
    {
        var (game, wallet, random) = Create(100m, 50, steps);

        var result = game.Play(10m);

        Assert.Equal((decimal)expectedWin, result.WinAmount);
        Assert.Equal(90m + (decimal)expectedWin, wallet.Balance);
        Assert.Equal((1, 201), random.Requests[1]);
    }

    [Theory]
    [InlineData(201, 20.10)]
    [InlineData(707, 70.70)]
    [InlineData(1000, 100.00)]
    public void BigWin_PaysBetweenTwiceExclusiveAndTenTimesTheStake(int steps, double expectedWin)
    {
        var (game, wallet, random) = Create(100m, 90, steps);

        var result = game.Play(10m);

        Assert.Equal((decimal)expectedWin, result.WinAmount);
        Assert.Equal(90m + (decimal)expectedWin, wallet.Balance);
        Assert.Equal((201, 1001), random.Requests[1]);
    }

    [Fact]
    public void Settlement_UsesGrossWin_AsInTheTaskExample()
    {
        var (game, wallet, _) = Create(10m, 95, 707);

        var result = game.Play(5m);

        Assert.Equal(35.35m, result.WinAmount);
        Assert.Equal(40.35m, wallet.Balance);
    }

    [Theory]
    [InlineData(1.50, 50, 101, 1.52)]
    [InlineData(1.55, 90, 333, 5.16)]
    [InlineData(2.33, 50, 120, 2.80)]
    public void Payout_IsRoundedToCents_WithMidpointsAwayFromZero(double stake, int roll, int steps, double expectedWin)
    {
        var (game, _, _) = Create(100m, roll, steps);

        Assert.Equal((decimal)expectedWin, game.Play((decimal)stake).WinAmount);
    }

    [Fact]
    public void Win_NeverRoundsDownToZero()
    {
        var config = new GameConfig { MinBet = 0.01m };
        var (game, wallet, _) = Create(100m, config, 50, 1);

        var result = game.Play(0.01m);

        Assert.Equal(0.01m, result.WinAmount);
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void CustomConfig_ChangesLimitsChancesAndMultipliers()
    {
        var config = new GameConfig
        {
            MinBet = 2m,
            MaxBet = 3m,
            LoseChancePercent = 0,
            SmallWinChancePercent = 0,
            BigWinChancePercent = 100,
            SmallWinMaxMultiplier = 3,
            BigWinMaxMultiplier = 4,
        };
        var (game, _, random) = Create(100m, config, 0, 400);

        var result = game.Play(3m);

        Assert.Equal(BetOutcome.BigWin, result.Outcome);
        Assert.Equal(12m, result.WinAmount);
        Assert.Equal((301, 401), random.Requests[1]);
        Assert.Throws<RuleViolationException>(() => game.Play(3.01m));
        Assert.Throws<RuleViolationException>(() => game.Play(1.99m));
    }

    [Theory]
    [InlineData(0, 10, 50, 40, 10, 2, 10)]
    [InlineData(1, 0.5, 50, 40, 10, 2, 10)]
    [InlineData(1, 10, 50, 40, 20, 2, 10)]
    [InlineData(1, 10, -10, 60, 50, 2, 10)]
    [InlineData(1, 10, 50, 40, 10, 0, 10)]
    [InlineData(1, 10, 50, 40, 10, 5, 5)]
    [InlineData(1, 10.005, 50, 40, 10, 2, 10)]
    public void InvalidConfig_IsRejectedWhenCreatingTheGame(
        double minBet, double maxBet, int lose, int small, int big, int smallMax, int bigMax)
    {
        var config = new GameConfig
        {
            MinBet = (decimal)minBet,
            MaxBet = (decimal)maxBet,
            LoseChancePercent = lose,
            SmallWinChancePercent = small,
            BigWinChancePercent = big,
            SmallWinMaxMultiplier = smallMax,
            BigWinMaxMultiplier = bigMax,
        };

        Assert.Throws<ArgumentException>(() => new Game(new Wallet(), config, new SequenceRandom()));
    }

    [Fact]
    public void DefaultConfig_MatchesTheTaskRules()
    {
        var config = new GameConfig();

        config.Validate();
        Assert.Equal((1m, 10m), (config.MinBet, config.MaxBet));
        Assert.Equal((50, 40, 10), (config.LoseChancePercent, config.SmallWinChancePercent, config.BigWinChancePercent));
        Assert.Equal((2, 10), (config.SmallWinMaxMultiplier, config.BigWinMaxMultiplier));
    }

    private static (Game Game, Wallet Wallet, SequenceRandom Random) Create(decimal balance, params int[] draws) =>
        Create(balance, new GameConfig(), draws);

    private static (Game Game, Wallet Wallet, SequenceRandom Random) Create(
        decimal balance, GameConfig config, params int[] draws)
    {
        var wallet = new Wallet();
        wallet.Deposit(balance);
        var random = new SequenceRandom(draws);
        return (new Game(wallet, config, random), wallet, random);
    }
}
