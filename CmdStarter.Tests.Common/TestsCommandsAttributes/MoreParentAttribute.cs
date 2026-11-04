using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Common.TestsCommandsAttributes;

public sealed class MoreParentAttribute<ParentClass> : ParentAttribute<ParentClass> where ParentClass : IStarterCommand
{
}
