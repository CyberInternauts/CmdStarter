# Attributes

This page lists the attributes that CmdStarter defines, and the .NET attributes that it reads. Each row says where the attribute is placed, what it does, and which page describes it in more detail. The library declares eleven attribute files, with fourteen public attribute types: three of them, `AutoComplete`, `Children` and `Parent`, also have a generic variant.

| Attribute | Placed on | What it does | Details |
|---|---|---|---|
| `[Alias]` | class, method, property, parameter | adds aliases to a command, a group, a method command, or an option | this page |
| `[AllOptionsExcluded]` | class | stops the properties declared in that class from becoming options | [Binding](binding.md) |
| `[AutoComplete]`, `[AutoComplete<T>]` | property, parameter | gives completions to an option or an argument | this page |
| `[AvailableInState]` | class, method | restricts a command to the listed application states | [Command states](../guides/command-states.md) |
| `[Children]`, `[Children<T>]` | class | places the commands of a namespace under the class | [Discovery](discovery.md) |
| `[Command]` | class, method | declares a method command, or a group class | [Commands](commands.md) |
| `[FromServices]` | parameter of a method command | resolves the parameter from dependency injection | [Binding](binding.md), [Dependency injection](dependency-injection.md) |
| `[Hidden]` | class, property, parameter, method | hides the element from the help | this page |
| `[NotOption]` | property | the property is not an option | [Binding](binding.md) |
| `[Option]` | property, parameter of a method command | makes the element an option | [Binding](binding.md) |
| `[Parent]`, `[Parent<T>]` | class | places the class under another command | [Discovery](discovery.md) |
| `DescriptionAttribute` (`System.ComponentModel`) | class, property, parameter, method | gives the description shown in the help | this page |
| `RequiredAttribute` (`System.ComponentModel.DataAnnotations`) | property, `[Option]` parameter | makes the option required | [Validation](../guides/validation.md) |
| other `ValidationAttribute` (.NET) | property, parameter, global options | validates the value | [Validation](../guides/validation.md) |

## `[Alias]`

`[Alias]` adds aliases. It can be placed on a command class, on a group class, on a `[Command]` method, on an option property, or on an `[Option]` parameter of a method command. `[Alias("oa")]` adds the alias as written. `[Alias(true, "o")]` adds the alias with the prefix `-`, which is the default prefix (`AliasAttribute.DefaultPrefix`). The `Prefix` property sets another prefix. The attribute can be repeated.

<!-- snippet: CmdStarter.Tests.Commands/Attributes/Alias/OneAlias.cs -->
```csharp
[Alias(COMMAND_ALIAS)]
public sealed class OneAlias : StarterCommand, IHasAliases
// ...
    [Alias(true, OPTION_ALIAS)]
    public int Option { get; set; }
```

In this example, `COMMAND_ALIAS` is `"oa"` and `OPTION_ALIAS` is `"o"`.

## `[Hidden]`

`[Hidden]` hides from the help a class command, a group class, a method command, an option property, or a parameter of a method command. It is also accepted on a parameter of a class command handler, where it hides that argument.

<!-- snippet: CmdStarter.Tests.Commands/Attributes/Hidden/HiddenCommand.cs -->
```csharp
[Hidden]
public sealed class HiddenCommand : StarterCommand
{
    // ...
    public override Delegate HandlingMethod => ([Hidden] int parameter) => { };
    // ...
    [Hidden]
    public bool Option { get; set; }
```

## `[AutoComplete]`

`[AutoComplete]` gives completions to an option property or to a parameter. The forms are:

- `[AutoComplete("Bob")]`, with one label or several;
- `[AutoComplete<T>()]`, where `T` is an enum (its names are the completions), an `IAutoCompleteProvider`, or an `IAutoCompleteFactory`;
- a constructor that takes a `Type` and labels.

A declaration that gives no completion at all, or a type that is not an enum, a provider or a factory, throws `NotSupportedException`.

<!-- snippet: CmdStarter.Tests.Commands/Attributes/AutoComplete/LonelyEnumAutoComplete.cs -->
```csharp
[AutoComplete<NameEnum>()]
public string PersonName { get; set; } = null!;
// ...
private void Execute([AutoComplete<AgeEnum>()] int age = 18)
```

## `[AllOptionsExcluded]`

`[AllOptionsExcluded]` on a class stops every property **declared in that class** from becoming an option. The attribute is not inherited: properties declared in a derived class are options again. `StarterCommand` carries the attribute, so its `Name` and `Description` properties are never options. See [Binding](binding.md).

## `[Option]` and `[NotOption]`

`[Option]` on a property makes it an option, and it switches the class to explicit mode: once one property of the class has `[Option]`, only the properties with `[Option]` (and without `[NotOption]`) become options. `[NotOption]` removes one property in the default mode, and it wins over `[Option]`. On a parameter of a method command, `[Option]` makes the parameter an option instead of an argument. See [Binding](binding.md).

## `[FromServices]`

`[FromServices]` on a parameter of a method command resolves it from dependency injection. It requires `Starter.ServiceProvider` to be set when the tree is built. Without it, `InvalidAttributeException` is thrown. See [Dependency injection](dependency-injection.md).

## `[Command]`, `[Parent]` and `[Children]`

`[Command]` marks a method, or a class used as a group, as a command of the method model. See [Commands](commands.md). `[Parent]` and `[Children]` place class commands in the tree. See [Discovery](discovery.md).

## `[AvailableInState]`

`[AvailableInState]` restricts a class command, a group class or a method command to the listed application states. The states come from the application. See [Command states](../guides/command-states.md).

## `DescriptionAttribute`

`DescriptionAttribute`, and any attribute derived from it, gives the description of a command class, an option property, a parameter of a handler or method, a method, or a group class. When several descriptions are given to the same element, they are joined with a new line (`StarterCommand.DescriptionJoiner`). For a `StarterCommand`, a description passed to its base constructor is used only when no description attribute gives one.

## `RequiredAttribute` and validation

`RequiredAttribute` on an option property, or on an `[Option]` parameter of a method command, makes the option required. Any other `ValidationAttribute` on an option property, a parameter of a handler or method, or a global option becomes a validator of the command line. The details are in [Validation](../guides/validation.md).

<!-- snippet: CmdStarter.Tests.Commands/Options/OptComplete.cs -->
```csharp
[Required]
public int IntOpt { get; set; }
```

## See also

- Examples: [NestedCommands](../../samples/CmdStarter.Samples.NestedCommands) (`[Parent<T>]`), [DevOpsTool](../../samples/CmdStarter.Samples.DevOpsTool) (`[Command]`, `[Option]`), [Validation](../../samples/CmdStarter.Samples.Validation).
- Neighbouring pages: [Commands](commands.md), [Discovery](discovery.md), [Binding](binding.md), [Dependency injection](dependency-injection.md), [Validation](../guides/validation.md), [Command states](../guides/command-states.md).
- ADRs: [0004, class-based command model](../adr/0004-class-based-command-model.md), [0009, method-based command model](../adr/0009-method-based-command-model.md), [0012, command states](../adr/0012-command-states.md), [0014, validation attributes](../adr/0014-validation-attributes.md).
