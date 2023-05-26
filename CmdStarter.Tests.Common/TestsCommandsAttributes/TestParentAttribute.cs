using com.cyberinternauts.csharp.CmdStarter.Lib.Loader;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TestParentAttribute : Attribute
    {
        public ClassesBuildingMode BuildingMode { get; private set; }

        public Type? Parent { get; private set; }

        public TestParentAttribute(ClassesBuildingMode buildingMode, Type? parent)
        {
            Parent = parent;
            BuildingMode = buildingMode;
        }
    }
}
