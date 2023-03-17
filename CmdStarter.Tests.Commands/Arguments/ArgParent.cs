using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments.Child;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments
{
    [Children<ArgChild>]
    public class ArgParent : StarterCommand
    {
        public override Delegate MethodForHandling => (string param1) => { };
    }
}
