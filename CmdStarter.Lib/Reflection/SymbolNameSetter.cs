using System.Reflection;
using System.CommandLine;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Reflection;

internal static class SymbolNameSetter
{
    // Transitoire : à retirer quand StarterCommand ne dérivera plus de Command (lot 004).
    private static readonly FieldInfo NameField = typeof(Symbol).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Le champ privé <Name>k__BackingField de System.CommandLine.Symbol est introuvable.");

    internal static void SetName(Symbol symbol, string name) => NameField.SetValue(symbol, name);
}
