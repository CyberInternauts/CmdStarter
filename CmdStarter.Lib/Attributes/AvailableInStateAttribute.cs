namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;

/// <summary>
/// Restricts a command to the listed application states. A command without this attribute is always available.
/// Hiding a command is not a security measure; sensitive handlers must check their own authorization.
/// For a command rooted on the root command, availability is still enforced even though the command is not hidden from help.
/// </summary>
/// <remarks>
/// State values are compared with <see cref="object.Equals(object?, object?)"/>. An unavailable parent makes its whole subtree unavailable.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class AvailableInStateAttribute : Attribute
{
    /// <summary>Creates an attribute for one or more application states.</summary>
    /// <param name="state">The first available state.</param>
    /// <param name="otherStates">Additional available states.</param>
    public AvailableInStateAttribute(object state, params object[] otherStates)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(otherStates);
        States = Array.AsReadOnly(new[] { state }.Concat(otherStates).ToArray());
    }

    /// <summary>Gets the available state values in declaration order.</summary>
    public IReadOnlyList<object> States { get; }
}
