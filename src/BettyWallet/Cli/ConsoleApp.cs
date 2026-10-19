using BettyWallet.Domain;
using BettyWallet.Gaming;

namespace BettyWallet.Cli;

/// <summary>The interactive loop: prompt, parse, execute, report the balance. Runs until "exit" or EOF.</summary>
public sealed class ConsoleApp(IConsoleIO io, Wallet wallet, Game game)
{
    public void Run()
    {
        while (true)
        {
            io.WriteLine("Please, submit action:");

            var line = io.ReadLine();
            if (line is null)
            {
                break;
            }

            if (!CommandParser.TryParse(line, out var command, out var error))
            {
                io.WriteLine($"Error: {error}");
            }
            else if (command is ExitCommand)
            {
                break;
            }
            else
            {
                Execute(command);
            }

            io.WriteLine(string.Empty);
        }

        io.WriteLine("Thank you for playing! Hope to see you again soon.");
        io.WriteLine(string.Empty);
        io.WriteLine("Press any key to exit.");
        io.WaitForKey();
    }

    private void Execute(Command command)
    {
        try
        {
            switch (command)
            {
                case DepositCommand deposit:
                    wallet.Deposit(deposit.Amount);
                    WriteWithBalance($"Your deposit of {Money.Format(deposit.Amount)} was successful.");
                    break;

                case WithdrawCommand withdraw:
                    wallet.Withdraw(withdraw.Amount);
                    WriteWithBalance($"Your withdrawal of {Money.Format(withdraw.Amount)} was successful.");
                    break;

                case BetCommand bet:
                    var result = game.Play(bet.Amount);
                    WriteWithBalance(result.WinAmount > 0
                        ? $"Congrats - you won {Money.Format(result.WinAmount)}!"
                        : "No luck this time!");
                    break;

                default:
                    throw new NotSupportedException($"Unhandled command {command.GetType().Name}.");
            }
        }
        catch (RuleViolationException ex)
        {
            WriteWithBalance($"Error: {ex.Message}");
        }
    }

    private void WriteWithBalance(string message) =>
        io.WriteLine($"{message} Your current balance is: {Money.Format(wallet.Balance)}");
}
