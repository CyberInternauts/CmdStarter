# System.CommandLine

CmdStarter builds a System.CommandLine command tree from your code and hands the command line to System.CommandLine. This page says what CmdStarter delegates, and how to reach the underlying objects when you need them.

## What is delegated

System.CommandLine does the parsing, the help, the completions and the invocation. CmdStarter does not reimplement them. The package uses the stable System.CommandLine 2.0.14, as decided in [ADR 0002](../adr/0002-system-commandline-2-stable.md).

The help text is localized by System.CommandLine, so help output and parse error messages follow the language of the machine. Check exit codes or symbols in tests, not help wording.

## The root command

`Starter.RootCommand` is the System.CommandLine `RootCommand`. After the tree is built, its `Subcommands` are the top-level commands.

`Starter.FindCommand<TCommand>()` returns the System.CommandLine `Command` of a class command, or `null` when the type is not a command of the tree. `Starter.FindCommandInstance(Command)` goes from such a node to the instance that CmdStarter created to describe the command. `Starter.GetFullCommandString<TCommand>()` returns the command path, for example `list folders`, or `null` when the type is not in the tree.

`StarterCommand` is not a System.CommandLine `Command`: an internal adapter builds the System.CommandLine tree from it. To reach the node of a class command, use `FindCommand`.

## Commands added by the application

Start runs a command that the application added to `RootCommand`, and CmdStarter leaves it as it is when availability changes.

Changing a setting that rebuilds the tree, such as `Starter.Namespaces`, discards the built tree, commands added by hand included: `Starter.RootCommand` then returns a new object. Add your own commands again after such a change.

## Customize the invocation

Override `CreateInvocationConfiguration` in a subclass of `Starter` to change the System.CommandLine invocation. The method is called for each execution and returns the `InvocationConfiguration` for it, or `null` to keep the System.CommandLine defaults. CmdStarter sets the `Output` and `Error` of the configuration from the console of the execution, when there is one (see [Command console](../guides/command-console.md)).

The console of an execution is attached to the configuration instance that `CreateInvocationConfiguration` returns, so a subclass must return a new instance at each call, or `null`.

## See also

- Neighbouring pages: [Commands](../concepts/commands.md), [Command console](../guides/command-console.md), [Errors and exit codes](../guides/errors-and-exit-codes.md), [Performance](performance.md).
- ADRs: [0002, System.CommandLine 2.0 stable](../adr/0002-system-commandline-2-stable.md), [0005, breaking changes and adapter](../adr/0005-breaking-changes-and-adapter.md).
- Migration: [1.x to 2.0](../migration/1.x-to-2.0.md).
