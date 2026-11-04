using CyberInternauts.CmdStarter.Attributes;
namespace CyberInternauts.CmdStarter.Tests.Commands.Methods.Erroneous;

public sealed class DuplicateNameCommands { [Command("same")] public void First() { } [Command("same")] public void Second() { } }
