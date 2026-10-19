namespace BettyWallet.Cli;

public interface IConsoleIO
{
    /// <summary>Returns the next input line, or null when input is exhausted (EOF).</summary>
    string? ReadLine();

    void WriteLine(string text);

    /// <summary>Blocks until the player presses a key; does nothing when there is no interactive console.</summary>
    void WaitForKey();
}

public sealed class SystemConsoleIO : IConsoleIO
{
    public string? ReadLine() => Console.ReadLine();

    public void WriteLine(string text) => Console.WriteLine(text);

    public void WaitForKey()
    {
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
