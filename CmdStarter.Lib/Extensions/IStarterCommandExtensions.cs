using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.Runtime.CompilerServices;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Extensions
{
    public static class IStarterCommandExtensions
    {
        /// <summary>
        /// Shortcut to retrieve the proper global options object
        /// </summary>
        /// <remarks>Defined as an extension method so the <see cref="IStarterCommand.HandlingMethod"/> can access it without casting itself to <see cref="IStarterCommand"/> </remarks>
        /// <typeparam name="GlobalOptionsType"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static GlobalOptionsType? GetGlobalOptions<GlobalOptionsType>(this IStarterCommand obj) where GlobalOptionsType : class, IGlobalOptionsContainer
        {
            return obj.GlobalOptionsManager?.GetGlobalOptions<GlobalOptionsType>();
        }
    }
}
