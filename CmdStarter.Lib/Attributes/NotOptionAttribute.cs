namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    /// <summary>
    /// Stops a property from becoming an option.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class NotOptionAttribute : Attribute
    {
    }
}
