namespace BettyWallet.Cli;

public interface IConsoleIO
{
    /// <summary>Returns the next input line, or null when input is exhausted (EOF).</summary>
    string? ReadLine();

    void WriteLine(string text);
}

public sealed class SystemConsoleIO : IConsoleIO
{
    public string? ReadLine() => Console.ReadLine();

    public void WriteLine(string text) => Console.WriteLine(text);
}
