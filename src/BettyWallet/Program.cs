using BettyWallet.Cli;
using BettyWallet.Domain;
using BettyWallet.Gaming;

// Composition root: the only place that knows the concrete implementations.
var wallet = new Wallet();
var game = new Game(wallet, new GameConfig(), new SystemRandomSource());

new ConsoleApp(new SystemConsoleIO(), wallet, game).Run();
