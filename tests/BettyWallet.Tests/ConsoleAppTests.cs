using BettyWallet.Cli;
using BettyWallet.Domain;
using BettyWallet.Gaming;
using BettyWallet.Tests.Support;

namespace BettyWallet.Tests;

public class ConsoleAppTests
{
    private const string Goodbye = "Thank you for playing! Hope to see you again soon.";

    [Fact]
    public void CompleteSession_ReproducesTheTaskExample()
    {
        // Rolls and multipliers chosen to produce the wins from the task's sample session.
        var random = new SequenceRandom(
            95, 707,  // bet 5  -> big win 7.07x = 35.35
            50, 119,  // bet 10 -> small win 1.19x = 11.90
            60, 166,  // bet 10 -> small win 1.66x = 16.60
            70, 153,  // bet 10 -> small win 1.53x = 15.30
            10);      // bet 10 -> lose

        var (io, wallet) = Run(random,
            "deposit 10", "bet 5", "bet 10", "bet 10", "bet 10", "bet 10", "withdraw 44.00", "exit");

        Assert.Equal(
            """
            Please, submit action:
            Your deposit of $10 was successful. Your current balance is: $10

            Please, submit action:
            Congrats - you won $35.35! Your current balance is: $40.35

            Please, submit action:
            Congrats - you won $11.90! Your current balance is: $42.25

            Please, submit action:
            Congrats - you won $16.60! Your current balance is: $48.85

            Please, submit action:
            Congrats - you won $15.30! Your current balance is: $54.15

            Please, submit action:
            No luck this time! Your current balance is: $44.15

            Please, submit action:
            Your withdrawal of $44.00 was successful. Your current balance is: $0.15

            Please, submit action:
            Thank you for playing! Hope to see you again soon.

            Press any key to exit.
            """.ReplaceLineEndings("\n"),
            io.Transcript);
        Assert.Equal(0.15m, wallet.Balance);
        Assert.True(io.WaitedForKey);
    }

    [Fact]
    public void Exit_StopsImmediately_AndIgnoresRemainingInput()
    {
        var (io, wallet) = Run(new SequenceRandom(), "exit", "deposit 10");

        Assert.Equal(1, io.UnreadInputCount);
        Assert.Equal(0m, wallet.Balance);
        Assert.Contains(Goodbye, io.Output);
        Assert.True(io.WaitedForKey);
    }

    [Fact]
    public void EndOfInput_EndsTheSessionGracefully()
    {
        var (io, wallet) = Run(new SequenceRandom(), "deposit 5");

        Assert.Equal(5m, wallet.Balance);
        Assert.Contains(Goodbye, io.Output);
    }

    [Fact]
    public void EmptyInput_EndsTheSessionGracefully()
    {
        var (io, wallet) = Run(new SequenceRandom());

        Assert.Equal(0m, wallet.Balance);
        Assert.Equal(["Please, submit action:", Goodbye, "", "Press any key to exit."], io.Output);
    }

    [Theory]
    [InlineData("dance", "Error: Unknown command 'dance'.")]
    [InlineData("", "Error: No command entered.")]
    [InlineData("deposit", "Error: Usage: deposit <amount>")]
    [InlineData("deposit ten", "Error: 'ten' is not a valid amount.")]
    [InlineData("deposit 99999999999999999999999999999999", "Error: '99999999999999999999999999999999' is not a valid amount.")]
    public void UnparseableInput_ShowsAnError_AndSessionContinues(string input, string expectedError)
    {
        var (io, wallet) = Run(new SequenceRandom(), "deposit 10", input, "withdraw 1", "exit");

        Assert.Contains(io.Output, line => line.StartsWith(expectedError));
        Assert.Contains("Your withdrawal of $1 was successful. Your current balance is: $9", io.Output);
        Assert.Equal(9m, wallet.Balance);
    }

    [Theory]
    [InlineData("withdraw 0", "Error: Amount must be greater than zero. Your current balance is: $10")]
    [InlineData("withdraw -5", "Error: Amount must be greater than zero. Your current balance is: $10")]
    [InlineData("deposit 1.234", "Error: Amount must have at most 2 decimal places. Your current balance is: $10")]
    [InlineData("withdraw 10.01", "Error: Insufficient funds for $10.01. Your current balance is: $10")]
    [InlineData("bet 0.99", "Error: Bet must be between $1 and $10. Your current balance is: $10")]
    [InlineData("bet 10.01", "Error: Bet must be between $1 and $10. Your current balance is: $10")]
    public void RejectedOperations_ExplainWhy_AndShowTheUnchangedBalance(string input, string expectedLine)
    {
        var random = new SequenceRandom();
        var (io, wallet) = Run(random, "deposit 10", input, "exit");

        Assert.Contains(expectedLine, io.Output);
        Assert.Equal(10m, wallet.Balance);
        Assert.Empty(random.Requests);
    }

    [Fact]
    public void BetWithoutFunds_IsRejected()
    {
        var (io, wallet) = Run(new SequenceRandom(0), "bet 5", "exit");

        Assert.Contains("Error: Insufficient funds for $5. Your current balance is: $0", io.Output);
        Assert.Equal(0m, wallet.Balance);
    }

    private static (ScriptedConsoleIO Io, Wallet Wallet) Run(SequenceRandom random, params string[] input)
    {
        var io = new ScriptedConsoleIO(input);
        var wallet = new Wallet();
        var game = new Game(wallet, new GameConfig(), random);

        new ConsoleApp(io, wallet, game).Run();

        return (io, wallet);
    }
}
