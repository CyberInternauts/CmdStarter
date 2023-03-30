using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cyberinternauts.csharp.CmdStarter.Lib
{
    public interface IStarterCommand
    {
        int GlobalOptionsManager { get; internal set; }
    }
}
