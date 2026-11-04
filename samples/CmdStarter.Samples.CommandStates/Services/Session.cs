using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Samples.CommandStates.Services;

public sealed class Session : ICommandStateProvider
{
    public SessionState State { get; set; } = SessionState.LoggedOut;
    public string? User { get; set; }
    public object? CurrentState => State;
}
