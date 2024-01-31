using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Loader
{
    /// <summary>
    /// Building mode (Order is important. Ending when ClassC is assigned)
    /// </summary>
    public enum ClassesBuildingMode : byte
    {
        /// <summary>
        /// Use (Parent|Children)Attributes and if nothing then namespaces.
        ///     - If ClassC has a <see cref="ParentAttribute"/> set to ClassP ==> Assign ClassC as subcommand of ClassP.
        ///     - If ClassP has a <see cref="ChildrenAttribute"/> set to the namespace of ClassC ==> Assign ClassC as subcommand of ClassP.
        ///     - If ClassC's parent namespace has only one <see cref="IStarterCommand"/> (ClassP) AND :
        ///         - ClassC doesn't have a <see cref="ParentAttribute"/> ==> Assign ClassC as subcommand of ClassP.
        ///         - ClassC is not covered by a <see cref="ChildrenAttribute"/> ==> Assign ClassC as subcommand of ClassP.
        /// </summary>
        Both = 0,

        /// <summary>
        /// Use only (Parent|Children)Attributes.
        ///     - If ClassC has a <see cref="ParentAttribute"/>  set to ClassP ==> Assign ClassC as subcommand of ClassP.
        ///     - If ClassP has a <see cref="ChildrenAttribute"/> set to the namespace of ClassC ==> Assign ClassC as subcommand of ClassP.
        /// </summary>
        OnlyAttributes = 1,

        /// <summary>
        ///     - If ClassC's parent namespace has only one <see cref="IStarterCommand"/> (ClassP)
        /// </summary>
        OnlyNamespaces = 2
    }
}
