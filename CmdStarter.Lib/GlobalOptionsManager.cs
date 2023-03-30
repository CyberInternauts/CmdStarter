using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib
{
    public static class GlobalOptionsManager
    {

        public static List<Type> GlobalOptionsTypes { get; private set; } = new();
        private static Dictionary<Type, object> GlobalOptions { get; set; } = new();

        public static void FindTypes()
        {

        }

        public static GlobalOptionType? GetGlobalOptions<GlobalOptionType>() where GlobalOptionType : class, IGlobalOptionsContainer
        {
            return null;
        }

        public static void SetGlobalOptions<GlobalOptionType>(GlobalOptionType globalOption) where GlobalOptionType : class, IGlobalOptionsContainer
        {

        }
    }
}
