using CyberInternauts.CmdStarter.Catalog;

[assembly: CommandCatalog(typeof(CyberInternauts.CmdStarter.Tests.Commands.Catalog.HandWrittenCatalog))]

namespace CyberInternauts.CmdStarter.Tests.Commands.Catalog;

// A catalog written by hand, as the generator of the package writes one: it declares Listed and PlainListed and nothing else,
// except an abstract command, which the engine ignores because the scan would not find it.
public sealed class HandWrittenCatalog : ICommandCatalog
{
    public void Describe(ICommandCatalogBuilder builder)
    {
        builder.AddCommand<Listed>();
        builder.AddCommand<PlainListed>();
        builder.AddCommand<AbstractListed>();
    }
}
