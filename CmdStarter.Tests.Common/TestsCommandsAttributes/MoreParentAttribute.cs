using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes
{
    public class MoreParentAttribute<ParentClass> : ParentAttribute<ParentClass> where ParentClass : IStarterCommand
    {
    }
}
