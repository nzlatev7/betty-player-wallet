# Interview notes

## Design in one minute

`Program.cs` wires `Wallet`, `Game` and `ConsoleApp`. `ConsoleApp` reads a line through `IConsoleIO`, `CommandParser` turns it into a
`Command`, and the app calls `Wallet.Deposit/Withdraw` or `Game.Play`. `Game` validates the bet, draws the outcome through
`IRandomSource`, then settles through `Wallet.Settle`. The wallet only knows balances; the game only knows rules.

## Money model

- `decimal` everywhere, never `double`; 2 fractional digits (cents). Float values appear nowhere in money paths.
- Input with more than 2 decimals is rejected rather than rounded: silently changing what a player typed is worse than an error.
- Only payouts are rounded: `stake * multiplier`, to cents, midpoint away from zero (`Money.Round`). Never below $0.01, so a
  "win" is never $0.00.
- Every `Wallet` method validates everything first and assigns `Balance` last, so a rejected call cannot leave partial state.
- Overflow: numbers beyond `decimal` fail parsing; a balance that would pass `decimal.MaxValue` is rejected by an explicit check
  (no exception handling used for flow).

## Payout interpretation

- The task's own sample fixes it: balance 10, bet 5, "you won $35.35", new balance 40.35 = 10 - 5 + 35.35. So the number shown is the
  **gross win**, and net profit is `win - bet`.
- "40% win up to x2" and "10% win between x2 and x10": I draw a multiplier uniformly from (0, 2] and (2, 10] in 0.01 steps.
  I considered requiring a "win" to return at least the stake (range (1, 2]); that would push the expected return to
  120% (0.4 x 1.5 + 0.1 x 6), which no casino would ship. The literal reading gives about 100% (0.4 x 1 + 0.1 x 6) and matches the
  sample (it shows wins of x1.19, x1.53, x1.66 and x7.07).
- Consequence worth mentioning: a small win below x1 is a "win" that loses money. It is allowed by the wording; the lower bound is
  one line to change if the product owner disagrees.

## Randomness

- `IRandomSource.Next(min, max)` mirrors `Random.Next`. The roll is an integer in [0, 100): `< 50` lose, `< 90` small win, else big win.
  Integer thresholds are exact; with a `double` in [0, 1) you would have to reason about `0.5 + 0.4 == 0.9` and similar.
- A second draw picks the multiplier in hundredths: [1, 200] for small wins, [201, 1000] for big wins.
- 50/40/10 are probabilities per round. Short sessions can look very different; I do not test frequencies.
- `SequenceRandom` in tests returns scripted values and fails the test if a value is outside the requested range, so a wrong script
  cannot pass unnoticed.

## Dependency injection

- Constructor injection throughout, composition root in `Program.cs`, manual wiring. Five lines; a container would be more code
  than it replaces. If the app grew (several players, background services), I would switch to
  `Microsoft.Extensions.DependencyInjection` at the composition root without touching any class.
- Interfaces only where something real varies: console I/O and randomness. `Wallet`, `Game`, `CommandParser` are concrete, because
  they are deterministic and fast to test directly.
- `GameConfig` is injected into `Game` and validated there, so an invalid configuration fails at startup, not mid-game.

## I/O abstraction

- `IConsoleIO` has two members: `ReadLine()` (null means EOF) and `WriteLine(string)`.
- Alternative: inject `TextReader`/`TextWriter` (`Console.In/Out` vs `StringReader/StringWriter`). Equally testable and no custom type;
  I chose an interface because it names the intent, makes the EOF contract explicit, and lets tests inspect output line by line.
- `ConsoleApp` handles text; `Wallet` and `Game` never print. Messages that explain rule violations come from the domain exception,
  presentation sentences from `ConsoleApp`.

## Testing strategy

- 91 xUnit tests, no mocking framework: two small hand-written doubles (`SequenceRandom`, `ScriptedConsoleIO`).
- Wallet: initial balance, deposits, withdrawals, exact-balance withdrawal, insufficient funds, invalid amounts, overflow, and
  "balance unchanged after rejection" for every rejection path.
- Game: bet limits at the exact boundaries (0.99, 1, 10, 10.01), every roll threshold (0, 49, 50, 89, 90, 99), payout bounds
  (0.01x, 2x, 2.01x, 10x), rounding, gross-win settlement, config validation.
- Parser: valid forms, case/whitespace, culture independence, malformed input, overflow.
- End to end: the PDF session replayed through `ConsoleApp` with scripted randomness; exit, EOF, invalid commands, rejected operations.
- Tests assert behaviour (balances, messages, ranges requested) and do not touch private members.

## Alternatives considered

- **Result type instead of exceptions** for rule violations: more explicit, but more ceremony in a tiny app. Exceptions are used only
  for expected, user-correctable failures and caught in exactly one place.
- **Strategy/polymorphic outcome classes**: three outcomes with a `switch` are easier to read; a pattern would add types with no benefit.
- **Generic host/DI container, options binding, `appsettings.json`**: unnecessary for one executable with five objects.
- **Money value type**: would be nicer in a larger codebase; here `decimal` plus the `Money` helpers is enough and keeps the wallet API plain.
- **`Game` holding the wallet vs a separate application service**: the game coordinates one wallet; a third class would only forward calls.

## Likely questions

**Why is `Settle` one method and not `Withdraw(stake)` then `Deposit(win)`?**
Two steps can fail halfway (stake taken, win rejected on overflow). One method validates both and commits once, matching the
formula in the task.

**What happens if the player bets more than the balance?**
`Wallet.Settle` throws `RuleViolationException("Insufficient funds...")`, the balance is untouched, and `ConsoleApp` shows the
error and the balance. The bet limits are checked before any random draw.

**How do you test a 10% chance without flaky tests?**
I don't test frequencies. I test that roll 89 is a small win and roll 90 a big win by scripting `IRandomSource`. Frequencies come
from `Random`, which I do not need to test.

**Why `decimal`, and why reject `1.234`?**
`double` cannot represent most cents exactly. Rejecting extra precision avoids silently changing the player's number.

**Where is the gross vs net mistake easy to make?**
In the message "you won $X": X is the gross payout. The balance formula subtracts the stake separately. `Settlement_UsesGrossWin_AsInTheTaskExample` pins this.

**Why is `RuleViolationException` the only caught exception?**
Everything it covers is correctable by the player. A `NullReferenceException` or `ArgumentException` from our code is a defect and should crash loudly.

**Is the wallet thread-safe?**
No: one player, one loop. See below.

**How would you change the lower bound of small wins to x1?**
Pass `lowMultiplier` 1 for small wins in `Game.Draw`; expect the return to rise to about 120%, so the other bands would need retuning.

**Why not `Math.Round` default (banker's rounding)?**
Midpoints away from zero is what players and auditors expect for cents. It is stated once in `Money.Round` and tested.

## Future considerations (not implemented)

- **Persistence:** store balance and a ledger of transactions (append-only), not just the current balance; load on start.
- **Concurrency:** with many requests per player, serialize operations per wallet (lock, actor, or optimistic concurrency with a version
  column) so two operations cannot both read the same balance.
- **Idempotency:** a client retry must not deposit twice; give each operation a client-generated id and return the stored result for repeats.
- **Production randomness:** a certified, auditable RNG (cryptographically secure, seeded and logged per regulator rules) behind the same
  `IRandomSource`; keep round results and RNG outputs for dispute resolution.
- **Money type and currencies:** a `Money` value with currency, and per-currency precision.
