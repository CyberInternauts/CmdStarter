# CmdStarter documentation

CmdStarter turns plain C# code into a command-line application, on top of System.CommandLine 2.0.14. This page says what it offers and lists every page of the documentation.

## What CmdStarter does

- **Two command models**: classes become commands, their public properties become options and the parameters of their handlers become arguments; or `[Command]` methods become commands.
- **Discovery or a tree in code**: commands are found and placed from namespaces and attributes, or described in code with `Starter.UseTree`.
- **Microsoft dependency injection**, with one scope per execution.
- **Command filters** around every execution.
- **A REPL** with history and completion, served locally or over a named pipe.
- **Sequences and pipes**: `a; b`, `a && b`, `a || b` and `a | b`.
- **Command states** that hide and refuse commands.
- **Generic Host** integration in the `cints.CmdStarter.Hosting` package.
- **A Roslyn analyzer** that reports definition mistakes at compile time.

The package is `cints.CmdStarter`, for .NET 10. Version 1.0 accepts breaking changes, listed in the [migration guide](migration/0.9-to-1.0.md).

Start with [Getting started](getting-started.md).

## Concepts

- [Commands](concepts/commands.md): the class model, the method model, exit codes and names.
- [Discovery](concepts/discovery.md): how commands are found, filtered, sorted and placed in the tree.
- [Attributes](concepts/attributes.md): every attribute CmdStarter defines or reads.
- [Binding](concepts/binding.md): options, arguments and initial values.
- [Dependency injection](concepts/dependency-injection.md): how command instances and services are created.

## Guides

- [Nested commands](guides/nested-commands.md): commands under other commands.
- [Global options](guides/global-options.md): options that every command accepts.
- [Validation](guides/validation.md): DataAnnotations attributes on options and arguments.
- [Configuration](guides/configuration.md): environment variables and `appsettings.json`.
- [Testing](guides/testing.md): running an application in memory with a fake service.
- [Errors and exit codes](guides/errors-and-exit-codes.md): every exit code and configuration error.
- [REPL](guides/repl.md): the read loop, built-in words, cancellation and line editing.
- [Command states](guides/command-states.md): commands available only in some states.
- [Hosting](guides/hosting.md): running on the .NET Generic Host.
- [Fluent command tree](guides/fluent-tree.md): describing the tree in code.
- [Filters](guides/filters.md): code that wraps every execution.
- [Command console](guides/command-console.md): the input and outputs of one execution.
- [Sequences and pipes](guides/sequences-and-pipes.md): several commands on one line.
- [Remote REPL](guides/remote-repl.md): a REPL served over a named pipe.

## Advanced

- [Analyzers](advanced/analyzers.md): the compile-time rules and their severity.
- [System.CommandLine](advanced/system-command-line.md): what is delegated and how to reach the underlying objects.
- [Performance](advanced/performance.md): startup and execution measurements.
- [Trimming](advanced/trimming.md): where the library stands with trimmed publication.
- [Native AOT](advanced/native-aot.md): where the library stands with Native AOT publication.

## Reference

- [Migration from 0.9 to 1.0](migration/0.9-to-1.0.md)
- [Architecture decisions](adr/README.md)
- [Samples](../samples/README.md)

## Build this documentation

The site is generated locally with DocFX from the guides and from the XML comments of the compiled libraries. Build the solution first: the API reference is read from the assemblies in `bin/Debug`.

```powershell
dotnet build CmdStarter.sln
dotnet tool restore
dotnet docfx docfx.json --serve
```

Links to the `samples/` folder point outside the DocFX content, so DocFX reports them as information, not warnings (`rules` in `docfx.json`); the documentation tests check that every relative link exists.
