﻿using com.cyberinternauts.csharp.CmdStarter.Lib.Loader;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.Naming
{
    [TestParent(ClassesBuildingMode.Both, typeof(Main))]
    [TestParent(ClassesBuildingMode.OnlyAttributes, null)]
    [TestParent(ClassesBuildingMode.OnlyNamespaces, typeof(Main))]
    public class Word : StarterCommand
    {
    }
}
