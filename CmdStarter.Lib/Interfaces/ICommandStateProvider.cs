namespace com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

/// <summary>Provides the application's current command state.</summary>
public interface ICommandStateProvider
{
    /// <summary>Gets the current application state.</summary>
    object? CurrentState { get; }
}
