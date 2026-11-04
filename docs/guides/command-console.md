# Command console

A command console holds the input, output and error of one execution. This guide covers how to give an execution its own console, how a handler receives it, and which console is used when none is given.

## The console

`CommandConsole` has an input reader, `In`, and two writers, `Out` and `Error`. `CommandConsole.Standard` is the process console. It is read when it is used, not when it is created.

A console is created from its three streams. The handler below reads its input from the console and returns the length of the text it read, so the exit code is that length:

<!-- snippet: CmdStarter.Tests/CommandConsoles.cs -->
```csharp
var console = new CommandConsole(new StringReader("abc"), output, error);
Assert.That(await starter.Start("read-length", console), Is.EqualTo(3));
```

## Start with a console

`Start` has overloads that take a console after the arguments: `Start(string[] args, CommandConsole console, CancellationToken = default)` and `Start(string args, CommandConsole console, CancellationToken = default)`. The console is used for that one command line only.

## Receive the console in a handler

A handler parameter of type `CommandConsole` receives the console of the execution. Like `CancellationToken`, it is neither an argument nor an option. This works for a class command's handler and for a `[Command]` method. The ReplServer sample writes its echo to the console it receives:

<!-- snippet: samples/CmdStarter.Samples.ReplServer/Commands/AdminCommands.cs -->
```csharp
[Command]
public static int Echo(CommandConsole console, string text)
{
    console.Out.WriteLine(text);
    return 0;
}
```

## What is written to the console

The help, the parse errors and the one-line message of an unhandled exception go to the console of the execution, to its `Out` and `Error`, instead of the process streams. Only the destination is fixed here; the wording of the help is set by System.CommandLine (see [System.CommandLine](../advanced/system-command-line.md)).

## Which console is used

Each execution uses the first of these that exists:

1. the console given to `Start`;
2. `Starter.DefaultConsole`;
3. the process console.

`ReplStarter` uses `DefaultConsole` for the commands it reads. Without any console, a handler writes to the process console as it is when the command runs: if the application replaces `Console.Out` before `Start`, the output goes to the new writer.

## Filters

A filter reads the same console from `CommandContext.Console`. It is the object that was given to `Start`. See [Filters](filters.md).

## Overlapping executions

Each execution keeps its own console, even when two `Start` calls overlap, or when a handler starts another execution with a different console: the output of the inner call does not reach the outer console. This does not make a `Starter` safe for concurrent use: the `Start` documentation still says that a `Starter` is not intended for concurrent `Start` calls, because it stores the parsed global options.

## Application starts an execution

When the application is built with `CmdStarterBuilder`, `CmdStarterApp.RunAsync` has no console parameter. To give an execution a console, call `Start` on the application's `Starter`, with the console as the second argument, or set the `Starter`'s `DefaultConsole` before `RunAsync`.

## See also

- Examples: [ReplServer](../../samples/CmdStarter.Samples.ReplServer/Program.cs).
- Neighbouring pages: [Testing](testing.md), [Filters](filters.md), [REPL](repl.md), [Remote REPL](remote-repl.md), [Sequences and pipes](sequences-and-pipes.md) (a pipe gives each command its own input and output), [Binding](../concepts/binding.md).
- ADR: [0021, command console](../adr/0021-command-console.md).
