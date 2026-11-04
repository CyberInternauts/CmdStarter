# Binding: options, arguments and initial values

This page explains how the parts of a command become the command line: the properties of a class command become options, the parameters of its handler become arguments, and the initial values of the properties become default values. It also lists how the parameters of a method command are classified.

## Properties become options

In the class model, a public instance property that has a getter and a setter becomes an option. Any setter counts, even a `private` or an `init` one: a property that is not meant to be an option must have no setter, or carry `[NotOption]`. Its name is `--` followed by the kebab-case of the property name. Static, private, protected, read-only and write-only properties do not become options.

Three attributes change this selection:

| Attribute | Effect |
|---|---|
| `[NotOption]` | the property is not an option, even in the default mode |
| `[Option]` | the property is an option, and the class switches to explicit mode |
| `[AllOptionsExcluded]` | no property declared in the class is an option; the attribute is not inherited |

The selection works in two modes:

- **Default mode**: every eligible property is an option, and `[NotOption]` removes one.
- **Explicit mode**: as soon as one eligible property has `[Option]`, only the properties with `[Option]` (and without `[NotOption]`) are options.

`[AllOptionsExcluded]` on a class removes all the properties that the class declares. `StarterCommand` carries it, so the `Name` and `Description` properties of a `StarterCommand` are never options.

The following class shows the defaults. `Greeting` and `Count` have initial values, and `Zero`, `Missing`, `Empty`, `Tags` and `Needed` have no default value that the help shows. The rule is in [Initial values become defaults](#initial-values-become-defaults).

<!-- snippet: CmdStarter.Tests.Commands/DefaultValues/Commands.cs -->
```csharp
public sealed class DefaultsCommand : StarterCommand
{
    public string Greeting { get; set; } = "hello-default";
    public int Count { get; set; } = 3;
    public int Zero { get; set; }
    public string? Missing { get; set; }
    public string Empty { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public bool Flag { get; set; }
    public bool FlagOn { get; set; } = true;

    [Required]
    public string Needed { get; set; } = "needed-default";
```

## Initial values become defaults

An option that is not given on the command line takes the initial value of its property. The help shows this value as the default.

The initial value does not become the help default in these cases:

- the option is `[Required]`;
- the value is `null`;
- the value is an empty string;
- the value is an empty collection;
- the value is the default value of a non-nullable value type, such as `0` or `false`.

`bool` options are an exception to the last rule, because System.CommandLine gives every boolean option a default of `false`. A `bool` property that starts at `false` therefore still shows `false`, and a `bool` property that starts at `true` keeps `true` when the option is not given. Passing `--flag-on false` replaces it.

The same rule applies to the options of global options containers. The rule itself is implemented in `Reflection/Loader.cs`.

## Handler parameters become arguments

In the class model, the parameters of the handler become positional arguments, named after the parameters. A parameter that has a default value is optional, and it takes that default when the argument is absent. The `Subtract` handler of the TwoCommands sample takes two arguments, `left` and `right`:

<!-- snippet: samples/CmdStarter.Samples.TwoCommands/Commands/Subtract.cs -->
```csharp
public override Delegate HandlingMethod => (Action<int, int>)((left, right) => Console.Out.WriteLine(left - right));
```

A `CommandConsole` parameter is not an argument: CmdStarter injects it into the handler.

A handler parameter can carry `[Description]`, `[Hidden]`, `[AutoComplete]` and validation attributes. See [Attributes](attributes.md) and [Validation](../guides/validation.md).

## Method commands: how parameters are classified

Each parameter of a `[Command]` method is classified when the command tree is built. The rules are checked in this order, and the first one that matches decides, so a `[FromServices]` parameter is a service even if it also carries `[Option]`, and an `[Option]` parameter is an option even if its type is a registered service:

- a `CancellationToken`, which receives the cancellation token of the execution;
- a `CommandConsole`, which receives the console of the execution (see [Command console](../guides/command-console.md));
- a type that implements `IGlobalOptionsContainer`, which receives the parsed global options;
- a parameter marked `[FromServices]`, which is resolved from the service provider; it requires `Starter.ServiceProvider` to be set;
- a parameter marked `[Option]`, which becomes an option named `--` followed by the kebab-case of the parameter name;
- a type that the service provider reports as registered (`IServiceProviderIsService`), which is resolved from the service provider; this needs `Starter.ServiceProvider` to be set before the tree is built;
- any other parameter, which becomes a positional argument named as the parameter.

The first four kinds and the service kind are not part of the command line. The following method has one argument, `name`, and one service, `service`, which is injected:

<!-- snippet: samples/CmdStarter.Samples.DependencyInjection/Commands/WelcomeCommands.cs -->
```csharp
public sealed class WelcomeCommands
{
    [Command]
    public static void Welcome(string name, IGreetingService service)
```

The next method declares two options, `--age` and `--admin`, which have default values, and a `CancellationToken` that is not an argument:

<!-- snippet: CmdStarter.Tests.Commands/Methods/UserCommands.cs -->
```csharp
[Command("create")]
[Description(CREATE_DESC)]
public int Create([Description(NAME_DESC)] string name, [Option] int age = 30, [Option] bool admin = false)
// ...
[Command]
public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken)
```

A method command whose parameter is an `[Option]` receives the parsed value of the option. When the option is absent, it receives the default value of the parameter. An argument that is absent and has a default value receives that default value.

A method command can also take a `CommandConsole` parameter, which receives the console of the execution:

<!-- snippet: CmdStarter.Tests.Commands/Consoles/ConsoleCommands.cs -->
```csharp
public static int Echo(CommandConsole console, string text)
```

## Global options

The global options containers, which are classes that implement `IGlobalOptionsContainer`, are bound once for each execution, before the options of the command. A class command reads them with `GetGlobalOptions<T>()`. A method command receives them as a parameter:

<!-- snippet: CmdStarter.Tests.Commands/Methods/ToolCommands.cs -->
```csharp
[Command, Alias("st")] public static int Status(MainGlobalOptions globals) => globals.IntGlobalOption;
```

Global options are described in [Global options](../guides/global-options.md).

## See also

- Examples: [HelloWorld](../../samples/CmdStarter.Samples.HelloWorld), [TwoCommands](../../samples/CmdStarter.Samples.TwoCommands), [DevOpsTool](../../samples/CmdStarter.Samples.DevOpsTool), [GlobalOptions](../../samples/CmdStarter.Samples.GlobalOptions), [DependencyInjection](../../samples/CmdStarter.Samples.DependencyInjection).
- Neighbouring pages: [Attributes](attributes.md), [Commands](commands.md), [Dependency injection](dependency-injection.md), [Global options](../guides/global-options.md), [Validation](../guides/validation.md).
- ADR: [0003, internal command binder](../adr/0003-internal-command-binder.md), [0014, validation attributes](../adr/0014-validation-attributes.md).
