using System.Diagnostics.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Reflection;

// The one place where the annotation of a Type is restored. The commands, the classes of method commands, the global options containers and the filters are registered through annotated APIs,
// and then kept in collections (CommandsTypes, MethodCommandsTypes, GlobalOptionsTypes, the filters), which loses the annotation: the trimmer can no longer follow a Type that comes out of them.
internal static class KeptTypes
{
    /// <summary>Gives a type that comes out of a collection of the starter the annotation of the API it was registered through.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2068", Justification = "Every type that reaches this method was registered through an annotated API: the command catalog (AddCommand, AddMethodCommands and AddGlobalOptions keep all the members of the type), AddFilter (keeps the public constructors), or it is a type that the application hands to the starter itself, which then answers for it.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    internal static Type Of(Type type) => type;
}
