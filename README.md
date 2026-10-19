# Betty Player Wallet

A .NET console application that simulates a single player's wallet and a simple game of chance
(Betty backend technical assessment). The player starts with $0, can deposit, withdraw and bet,
and sees the balance after every operation until they exit.

## Prerequisites

- .NET SDK 10.0 or newer (developed and verified with 10.0.402). The projects target `net10.0`;
  no API newer than .NET 8 is used, so `net8.0` should also work (not verified here: only SDKs 6 and 10 are installed).
- No other dependencies. The test project uses xUnit (restored from NuGet).

## Build, test, run

```bash
dotnet build
dotnet test
dotnet run --project src/BettyWallet
```

Commands (case-insensitive): `deposit <amount>`, `withdraw <amount>`, `bet <amount>`, `exit`.
End of input (Ctrl+Z then Enter on Windows, Ctrl+D elsewhere) also ends the session.

## Example session

```text
Please, submit action:
deposit 10
Your deposit of $10 was successful. Your current balance is: $10

Please, submit action:
bet 5
Congrats - you won $35.35! Your current balance is: $40.35

Please, submit action:
bet 10
No luck this time! Your current balance is: $30.35

Please, submit action:
withdraw 30.35
Your withdrawal of $30.35 was successful. Your current balance is: $0.00

Please, submit action:
exit
Thank you for playing! Hope to see you again soon.

Press any key to exit.
```

A rejected operation explains why and still shows the unchanged balance:

```text
Please, submit action:
withdraw 50
Error: Insufficient funds for $50. Your current balance is: $0
```

The automated test `CompleteSession_ReproducesTheTaskExample` replays the exact session from the task PDF
with scripted randomness and asserts the whole transcript.

## Requirements and assumptions

Explicit in the task: start at $0 and show the balance after each operation; deposit, withdraw, bet, exit;
bets from $1 to $10; 50% lose, 40% win up to x2, 10% win between x2 and x10;
`new balance = old balance - bet + win`; amounts always positive, withdrawals included.

My interpretations (the task is silent):

| Topic | Decision |
| --- | --- |
| "Win amount" | The **gross** payout, as in the task's sample (balance 10, bet 5, "won $35.35" gives 40.35). Net profit is `win - bet`. |
| Payout distribution | Uniform within each band: small win pays x(0, 2] of the stake, big win pays x(2, 10]. Multipliers are drawn in steps of 0.01x, so the upper bound is reachable and the lower bound is not. |
| Small win below the stake | Allowed ("up to x2"). A small win of x0.5 is a win that still loses money; with uniform bands the expected return is about 100%. |
| Currency precision | Decimals with at most 2 fractional digits. Input with more digits (`1.234`) is **rejected**, never silently rounded. |
| Payout rounding | To cents, midpoints away from zero (`1.515` becomes `1.52`); a win is never less than $0.01. |
| Amount syntax | Invariant culture: `.` decimal separator; no thousands separators, currency symbols or exponents. `10`, `10.5`, `10.50` are valid; `1,000`, `$5`, `1e3` are not. Zero and negative amounts are rejected with a clear message. |
| Commands | Case-insensitive, extra whitespace ignored, exact argument count required. |
| Overflow | Numbers beyond `decimal` are rejected as invalid; a balance that would exceed `decimal.MaxValue` is rejected. |
| Display | As in the task's sample, amounts print with the decimal's own scale: what you typed is echoed (`deposit 10` gives `$10`, `withdraw 44.00` gives `$44.00`) and balances follow decimal arithmetic. Payouts always have two decimals (`$11.90`). |
| Exit | Prints "Press any key to exit." and waits for a key, like the sample; skipped when input is redirected (pipes, tests), where there is no key to read. |

Outcome probabilities are per round, not guaranteed frequencies over a short session.

### Configurable rules

`GameConfig` (defaults are the task's rules, validated when the `Game` is created): `MinBet`, `MaxBet`,
`LoseChancePercent`, `SmallWinChancePercent`, `BigWinChancePercent` (must total 100),
`SmallWinMaxMultiplier`, `BigWinMaxMultiplier`. It is plain C#; `Program.cs` could bind it from a file
if needed, but nothing in the task justifies that yet.

## Architecture

```text
src/BettyWallet
  Domain/   Wallet, Money, RuleViolationException   money rules and balance; no I/O, no randomness
  Gaming/   Game, GameConfig, BetResult, IRandomSource   rules of the game; settles through the wallet
  Cli/      IConsoleIO, CommandParser, Command, ConsoleApp   everything about text in and out
  Program.cs                                          composition root
tests/BettyWallet.Tests
```

Dependencies point inward: `Cli` -> `Gaming` -> `Domain`. The wallet knows nothing about games or the console.

- **Constructor injection with manual wiring.** There are three collaborators; a DI container or generic host would add
  more code than it removes. `Program.cs` is the single place that picks concrete types.
- **Two seams, both abstracted because tests need them:** `IConsoleIO` (input and output; `ReadLine` returns `null` at EOF)
  and `IRandomSource` (one `Next(min, max)` method). Everything else is a concrete class.
- **`Wallet.Settle(stake, win)`** applies `-stake + win` in one validated step, so a round can never be half-applied.
- **Parser separate from rules.** `CommandParser` checks syntax only and returns a `Command` record. Positivity, precision, bet limits
  and funds are decided once, in the domain, so messages are not duplicated.
- **Expected vs unexpected failures.** Rule violations throw `RuleViolationException`, the only exception `ConsoleApp` catches.
  Invalid configuration or a bad argument from our own code (`ArgumentException`) is a defect and is not caught.
- **Outcome selection.** One integer roll in `[0, 100)`: `< 50` lose, `< 90` small win, otherwise big win. Integers keep the
  thresholds exact, with no floating-point edge cases.

## Trade-offs and deliberately omitted

- No DI container, hosting, logging framework, external configuration, Docker, database, HTTP API or persistence:
  none is required, and each would add surface to maintain.
- Exceptions for rule violations keep method signatures simple for a small app; a `Result` type would be an alternative
  if failures became part of a richer API.
- `System.Random` is fine for a simulation. A regulated product would need a certified, auditable RNG behind `IRandomSource`.
- The wallet is not thread-safe because there is one player and one console loop.
- Statistical tests are intentionally absent: they would be flaky. Thresholds and payout bounds are tested deterministically
  by scripting `IRandomSource`.

See `INTERVIEW_NOTES.md` for the reasoning behind these choices.
