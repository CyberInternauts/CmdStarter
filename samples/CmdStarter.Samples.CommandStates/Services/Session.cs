using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Samples.CommandStates.Services;

public sealed class Session : ICommandStateProvider
{
    public SessionState State { get; set; } = SessionState.LoggedOut;
    public string? User { get; set; }
    public object? CurrentState => State;
}
