namespace CyberInternauts.CmdStarter.Attributes;

/// <summary>
/// Stops all properties of the class from becoming options.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class AllOptionsExcludedAttribute : Attribute
{
}
