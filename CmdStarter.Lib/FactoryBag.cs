using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib
{
    internal static class FactoryBag
    {
        private static readonly Func<Type, IStarterCommand> DEFAULT_FACTORY = (commandType) => (IStarterCommand)Activator.CreateInstance(commandType)!;

        private static Func<Type, IStarterCommand> Factory { get; set; } = DEFAULT_FACTORY;

        internal static IStarterCommand RunFactory<CommandType>() where CommandType : IStarterCommand
        {
            return Factory(typeof(CommandType));
        }

        internal static void SetFactory(Func<Type, IStarterCommand> factory)
        {
            Factory = factory;
        }

        internal static void SetDefaultFactory()
        {
            Factory = DEFAULT_FACTORY;
        }
    }
}
