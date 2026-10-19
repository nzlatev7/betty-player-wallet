namespace BettyWallet.Domain;

/// <summary>
/// An expected, user-correctable failure: the requested operation breaks a wallet or game rule.
/// The message is safe to show to the player. Anything else is a defect and must not be caught.
/// </summary>
public sealed class RuleViolationException(string message) : Exception(message);
