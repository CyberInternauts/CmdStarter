using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Methods.Erroneous;

public sealed class DuplicateNameCommands { [Command("same")] public void First() { } [Command("same")] public void Second() { } }
