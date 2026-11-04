using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Methods.Erroneous;

public sealed class GenericMethodCommands { [Command] public void Run<T>() { } }
