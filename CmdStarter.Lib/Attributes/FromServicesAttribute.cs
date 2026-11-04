namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;

/// <summary>Marks a method parameter for resolution from dependency injection.</summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FromServicesAttribute : Attribute { }
