using System.Runtime.CompilerServices;

// The tests of the typed symbol factories need SymbolFactories: it is the only way to observe that a generated factory is the one in use.
[assembly: InternalsVisibleTo("CmdStarter.Tests")]
