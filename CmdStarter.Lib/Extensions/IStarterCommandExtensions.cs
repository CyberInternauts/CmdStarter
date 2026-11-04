using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Extensions;

/// <summary>
/// <see cref="IStarterCommand"/> extensions
/// </summary>
public static class IStarterCommandExtensions
{
    /// <summary>
    /// Shortcut to retrieve the proper global options object
    /// </summary>
    /// <remarks>Defined as an extension method so the <see cref="IStarterCommand.HandlingMethod"/> can access it without casting itself to <see cref="IStarterCommand"/> </remarks>
    /// <typeparam name="TGlobalOptions"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static TGlobalOptions? GetGlobalOptions<TGlobalOptions>(this IStarterCommand obj) where TGlobalOptions : class, IGlobalOptionsContainer
    {
        return obj.GlobalOptionsManager?.GetGlobalOptions<TGlobalOptions>();
    }
}
