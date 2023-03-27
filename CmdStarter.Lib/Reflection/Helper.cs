using System.Reflection;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Reflection
{
    public static class Helper
    {

        public static IEnumerable<PropertyInfo> GetProperties(object obj)
        {
            return GetProperties(obj.GetType());
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                    p.CanWrite && p.CanRead
                    && (p.DeclaringType?.IsSubclassOf(typeof(StarterCommand)) ?? false)
                );

            return properties;
        }
    }
}
