using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Lib.Reflection;
using System.CommandLine;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Helper;

namespace com.cyberinternauts.csharp.CmdStarter.Lib
{
    public class GlobalOptionsManager
    {

        private static IEnumerable<Type>? foundTypes;
        private readonly Dictionary<Type, object> globalOptions = new();
        private readonly Starter starter;

        internal GlobalOptionsManager(Starter starter) 
        {
            this.starter = starter;
            FindTypes();
        }

        public List<Type> GlobalOptionsTypes { get; private set; } = new();

        public GlobalOptionsType? GetGlobalOptions<GlobalOptionsType>() where GlobalOptionsType : class, IGlobalOptionsContainer
        {
            var type = typeof(GlobalOptionsType);

            return globalOptions.ContainsKey(type) ? (GlobalOptionsType)globalOptions[type] : null;
        }

        public void SetGlobalOptions<GlobalOptionsType>(GlobalOptionsType globalOption) where GlobalOptionsType : class, IGlobalOptionsContainer
        {
            var type = typeof(GlobalOptionsType);

            if (globalOptions.ContainsKey(type)) 
            {
                globalOptions[type] = globalOption;
            }
            else
            {
                globalOptions.Add(type, globalOption);
            }
        }

        internal void FilterTypes()
        {
            GlobalOptionsTypes.Clear();

            var filteredTypes = FilterTypesByNamespaces(foundTypes!, starter.Namespaces.ToList()); // foundTypes can't be null as initialized in constructor
            filteredTypes = FilterTypesByClasses(filteredTypes, starter.Classes.ToList());

            GlobalOptionsTypes.AddRange(filteredTypes);
        }

        internal void LoadOptions(Command receptacle)
        {
            foreach(var type in GlobalOptionsTypes)
            {
                CommandLineHelper.LoadOptions(type, receptacle);
            }
        }

        private static void FindTypes()
        {
            if (foundTypes != null) return;

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && t.IsAssignableTo(typeof(IGlobalOptionsContainer)));

            foundTypes = new List<Type>(types);
        }
    }
}
