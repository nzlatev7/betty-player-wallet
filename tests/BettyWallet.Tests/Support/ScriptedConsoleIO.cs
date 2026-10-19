using BettyWallet.Cli;

namespace BettyWallet.Tests.Support;

/// <summary>Feeds a fixed list of input lines (then EOF) and captures everything written.</summary>
public sealed class ScriptedConsoleIO(params string[] input) : IConsoleIO
{
    private readonly Queue<string> _input = new(input);

    public List<string> Output { get; } = [];

    public int UnreadInputCount => _input.Count;

    public bool WaitedForKey { get; private set; }

    public void WaitForKey() => WaitedForKey = true;

    public string? ReadLine() => _input.TryDequeue(out var line) ? line : null;

    public void WriteLine(string text) => Output.Add(text);

    public string Transcript => string.Join("\n", Output);
}
