namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;

/// <summary>
/// Hide from help
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = false)]
public sealed class HiddenAttribute : Attribute
{
}
