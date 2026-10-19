namespace BettyWallet.Cli;

public abstract record Command;

public sealed record DepositCommand(decimal Amount) : Command;

public sealed record WithdrawCommand(decimal Amount) : Command;

public sealed record BetCommand(decimal Amount) : Command;

public sealed record ExitCommand : Command;
