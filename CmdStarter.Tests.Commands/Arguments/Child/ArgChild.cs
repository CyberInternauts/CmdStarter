using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments.Child
{
    [Parent<ArgParent>]
    public class ArgChild : StarterCommand
    {
        public override Delegate MethodForHandling => (int p1) => { };
    }
}
