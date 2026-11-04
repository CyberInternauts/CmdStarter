namespace CyberInternauts.CmdStarter.Attributes;

/// <summary>
/// Defines a property as an option.
/// <para>
/// Excludes all properties from forming into options except the ones with the <see cref="OptionAttribute"/>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class OptionAttribute : Attribute
{
}
