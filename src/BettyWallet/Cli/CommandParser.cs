using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BettyWallet.Cli;

/// <summary>
/// Turns a line such as "deposit 10.50" into a <see cref="Command"/>. Only syntax is checked here;
/// whether an amount is positive, precise enough or affordable is decided by the domain.
/// Commands are case-insensitive; amounts use '.' as the decimal separator, with no thousands
/// separators, currency symbols or exponents.
/// </summary>
public static class CommandParser
{
    public const string Usage = "Supported commands: deposit <amount>, withdraw <amount>, bet <amount>, exit.";

    private const NumberStyles AmountStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static bool TryParse(
        string? line,
        [NotNullWhen(true)] out Command? command,
        [NotNullWhen(false)] out string? error)
    {
        command = null;
        error = null;

        var parts = (line ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            error = "No command entered. " + Usage;
            return false;
        }

        var name = parts[0].ToLowerInvariant();
        switch (name)
        {
            case "exit":
                if (parts.Length != 1)
                {
                    error = "'exit' takes no arguments.";
                    return false;
                }

                command = new ExitCommand();
                return true;

            case "deposit":
                return TryParseAmountCommand(name, parts, amount => new DepositCommand(amount), out command, out error);
            case "withdraw":
                return TryParseAmountCommand(name, parts, amount => new WithdrawCommand(amount), out command, out error);
            case "bet":
                return TryParseAmountCommand(name, parts, amount => new BetCommand(amount), out command, out error);

            default:
                error = $"Unknown command '{parts[0]}'. " + Usage;
                return false;
        }
    }

    private static bool TryParseAmountCommand(
        string name,
        string[] parts,
        Func<decimal, Command> create,
        [NotNullWhen(true)] out Command? command,
        [NotNullWhen(false)] out string? error)
    {
        command = null;
        error = null;

        if (parts.Length != 2)
        {
            error = $"Usage: {name} <amount>";
            return false;
        }

        if (!decimal.TryParse(parts[1], AmountStyle, CultureInfo.InvariantCulture, out var amount))
        {
            error = $"'{parts[1]}' is not a valid amount.";
            return false;
        }

        command = create(amount);
        return true;
    }
}
