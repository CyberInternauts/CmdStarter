using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    /// <summary>
    /// Class for tests that include commands responses
    /// </summary>
    [Category("* All *")]
    [Category("Commands")]
    public class CommandsResults
    {
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            TestsCommon.GlobalSetup();
        }


    }
}
