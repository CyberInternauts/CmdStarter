namespace CyberInternauts.CmdStarter.Samples.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class RequiresConfirmationAttribute : Attribute
{
}
