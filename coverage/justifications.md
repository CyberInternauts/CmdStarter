# Coverage justifications

Lines and branches of `CmdStarter.Lib`, `CmdStarter.Hosting` and `CmdStarter.Analyzers` that no test reaches. `measure-coverage.ps1` lists the uncovered code in `coverage/report/uncovered.md`; a line or branch only goes here when it cannot be covered, and Main decides each entry. No line is excluded from the measure with `[ExcludeFromCodeCoverage]`: the measure stays honest and this file explains the gap.

A guard against a case that is impossible by construction is not justified: it is proposed for removal (**Proposal**: `remove (impossible by construction)`), with the proof of the impossibility. Nothing is removed by the author of the entry: Main decides, then the removal is done as a correction.

## Entry format

Each entry is a level-3 heading `File:line`, then seven items:

- **File**: path from the repository root.
- **Line**: line number at the time of the entry (the exact code below allows finding it again), and the branch when the line is only partly covered.
- **Code**: the exact code, in a code block.
- **Reason**: why no test reaches it.
- **Change needed**: what would have to change in the library to reach it.
- **Why refused**: why that change is not acceptable ("defensive guard" alone is not an answer).
- **Proposal**: `keep (justified)`, or `remove (impossible by construction)` followed by the proof of the impossibility (who calls it, with what).

## Entries

### CmdStarter.Lib/Starter.cs:419

- **File**: `CmdStarter.Lib/Starter.cs`
- **Line**: 419, the branch where `commandConsole` is `null`.
- **Code**:

  ```csharp
  return RunSequenceAsync(commandLine, console, (command, commandConsole) => Start(command, commandConsole ?? console, cancellationToken));
  ```

- **Reason**: `commandConsole` is never `null` in this overload.
- **Change needed**: a caller of the callback would have to pass `null` as the console.
- **Why refused**: the callback is private to this line and only called by `RunSequenceAsync` and `RunPipelineAsync`, which pass the `console` they were given.
- **Proposal**: `remove (impossible by construction)`. `RunSequenceAsync(commandLine, console, runCommand)` receives a non-null `console` from this overload (`ArgumentNullException.ThrowIfNull(console)` on line 418) and calls `runCommand(commandLine, console)`; `RunPipelineAsync` calls `runCommand(pipeline[0], console)` with that same non-null `console` for a single command, and `new CommandConsole(...)` for several. The callback can be `(command, commandConsole) => Start(command, commandConsole!, cancellationToken)`.

### CmdStarter.Lib/Starter.cs:870

- **File**: `CmdStarter.Lib/Starter.cs`
- **Line**: 870, the branch where `Namespace` is `null`.
- **Code**:

  ```csharp
  descendant.Value!.Namespace?.StartsWith(commandType.Namespace + ".", StringComparison.Ordinal) == true);
  ```

- **Reason**: the namespace of a command type in the tree is never `null` when this line runs.
- **Change needed**: a command type without a namespace would have to reach line 870 with `GroupsByNamespace` on.
- **Why refused**: discovery refuses such a type, and `BuildNamespaceGroupPaths` dereferences `commandType.Namespace!` on line 844, for every type, before line 870.
- **Proposal**: `remove (impossible by construction)`. `BuildNamespaceGroupPaths` runs only without a tree definition (`BuildTree`, `definedTree is null`), so the types come from `CommandsTypes`; `FindCommandsTypes` keeps only `t.Namespace != null`. The public `CommandsTypes` setter does not check anything, but a type without a namespace injected through it (probe, once `FindCommandsTypes` has run, otherwise discovery overwrites the list) with `GroupsByNamespace` on stops at line 844 with a raw `NullReferenceException` (`Starter.<>c.<BuildNamespaceGroupPaths>b__126_0`, `Starter.cs:844`), and with `GroupsByNamespace` off the tree is built without reaching any of this code. So the `null` branch of line 870 is unreachable. The `?.` can become `.`. The exception of line 844 is a separate question (see the report: ADR 0010).

### CmdStarter.Lib/Starter.cs:1065

- **File**: `CmdStarter.Lib/Starter.cs`
- **Line**: 1065 (branch of the `switch` expression) and 1073 (the line `_ => null`).
- **Code**:

  ```csharp
  _ => null
  ```

- **Reason**: `ParameterBindingKind` has six members and the switch handles all of them: the `_` arm only matches an undefined enum value.
- **Change needed**: the internal enum would have to be given a seventh member, or a value cast from an integer.
- **Why refused**: the enum is internal and only created by `AddMethodCommands` (lines 1205 to 1225) with the six known members; a cast would only serve to reach the line.
- **Proposal**: `remove (impossible by construction)`. The arm exists only because a `switch` expression over an enum without a default arm is warning CS8509, an error with `TreatWarningsAsErrors`. Making the last explicit case (`ParameterBindingKind.Argument`) the `_` arm removes the unreachable arm without losing any case; the six members are the only values ever stored in `ParameterBinding.Kind`.

### CmdStarter.Lib/Starter.cs:1155

- **File**: `CmdStarter.Lib/Starter.cs`
- **Line**: 1155, the branch where `parameter.HasDefaultValue` is `true`.
- **Code**:

  ```csharp
  if (parameter.HasDefaultValue) return parameter.DefaultValue;
  ```

- **Reason**: when the option is absent from the command line and the parameter has a default value, `TryGetOptionValue` still succeeds, so the line before returns.
- **Change needed**: an option created from a parameter with a default value would have to have no parse result when absent.
- **Why refused**: `Loader.CreateParameterOption` sets `DefaultValueFactory` for every parameter that has a default value (`HasDefaultValue && DefaultValue is not DBNull`), and System.CommandLine then gives such an option an implicit parse result (checked: for `show-options` with no argument, `GetResult` of `--label`, whose default is `null`, is a result, while `--count` and `--name`, which have no default, have none).
- **Proposal**: `remove (impossible by construction)`. The only caller is the `ParameterBindingKind.Option` arm, whose option comes from `CreateParameterOption`, which sets the default factory whenever `HasDefaultValue` is true; `DefaultValue` is `DBNull` only when `HasDefaultValue` is false. The line can go: the next line already handles a parameter without default.

### CmdStarter.Lib/Starter.cs:1160

- **File**: `CmdStarter.Lib/Starter.cs`
- **Line**: 1160, one of the four branches: `GetResult(argument)` is `null` AND `parameter.HasDefaultValue` is `true`. The other three are covered (argument with a result; no result and no default, tested by `ArrayArgumentWithoutValuesReachesTheHandler`, which receives an empty array).
- **Code**:

  ```csharp
  result.GetResult(argument) is null && parameter.HasDefaultValue ? parameter.DefaultValue : CommandBinder.GetArgumentValue(result, argument);
  ```

- **Reason**: an argument created from a parameter that has a default value always has a parse result when it is absent, so `GetResult` is never `null` for it.
- **Change needed**: an argument created from a parameter with a default value would have to have no parse result when absent.
- **Why refused**: `Loader.CreateArgument` sets `DefaultValueFactory` whenever `parameter.HasDefaultValue`, and System.CommandLine then gives the absent argument an implicit result. That is the fact of System.CommandLine 2.0.14, not something the library decides.
- **Proposal**: `remove (impossible by construction)`. Facts (probe by the Helper, System.CommandLine 2.0.14 alone, `Parse` without a token): an `Argument<string>` with a `DefaultValueFactory`, absent, has a result and no error; an `Argument<string[]>` without default (arity 0 to 100000), absent, has NO result and no error (the covered branch); a required `Argument<string>`, absent, has a result and one error. `GetMethodArgumentValue` is called only for the `ParameterBindingKind.Argument` arm, with an argument from `CreateArgument`, which has a factory exactly when `HasDefaultValue`; so "no result and `HasDefaultValue`" cannot happen. The expression can become `CommandBinder.GetArgumentValue(result, argument)` alone.

### CmdStarter.Lib/Starter.cs:1364

- **File**: `CmdStarter.Lib/Starter.cs`
- **Line**: 1364, the branch where `funcReturn` is not `null`.
- **Code**:

  ```csharp
  if (funcReturn != null) return funcReturn;
  ```

- **Reason**: the only caller passes a function that always returns `null`.
- **Change needed**: a caller would have to give a function that returns a command, to stop the visit.
- **Why refused**: the method is private; its only caller is the public `VisitCommands(Command, Action<Command>)`, which wraps the action in `loopBody`, which returns `null` every time. Making the method public to test it would add an API for a use nobody has.
- **Proposal**: `remove (impossible by construction)`. The overloads of `VisitCommands` in `Starter` are: the public instance one, the public static one (action, wrapped by `loopBody` that returns `null`), and this private one, called only from the public static one and from itself with the same function. The early exits (lines 1364 and 1367) and the `Command?` return value can be dropped by making the private method a plain `Action<Command>` visit.

### CmdStarter.Lib/Starter.cs:1367

- **File**: `CmdStarter.Lib/Starter.cs`
- **Line**: 1367, the branch where `childChild` is not `null`.
- **Code**:

  ```csharp
  if (childChild != null) return childChild;
  ```

- **Reason**: the recursive call returns what the function returns for a command, and the function always returns `null`.
- **Change needed**: same as line 1364.
- **Why refused**: same as line 1364.
- **Proposal**: `remove (impossible by construction)`. Same proof as line 1364: the recursion uses the same function, so `childChild` is `null` whenever `funcReturn` is.

### CmdStarter.Lib/Repl/ReplStarter.cs:331

- **File**: `CmdStarter.Lib/Repl/ReplStarter.cs`
- **Line**: 331, the branch where `commandConsole` is `null`.
- **Code**:

  ```csharp
  return RunSequenceAsync(commandLine, console, (command, commandConsole) => Start(command, commandConsole ?? console, cancellationToken));
  ```

- **Reason**: `commandConsole` is never `null` in this overload.
- **Change needed**: a caller of the callback would have to pass `null` as the console.
- **Why refused**: the callback is private to this line; its callers are in `Starter` and pass the console they were given.
- **Proposal**: `remove (impossible by construction)`. Checked for this line, not copied from `Starter.cs:419`: `RunSequenceAsync(commandLine, console, runCommand)` receives the `console` of this overload, which is non-null (`ArgumentNullException.ThrowIfNull(console)` on line 330). It calls `runCommand(commandLine, console)` for a line without operator or without step, and `RunPipelineAsync(step.Pipeline, console, ...)` for the steps. `RunPipelineAsync` calls `runCommand(pipeline[0], console)` for one command (the same non-null console) and `runCommand(..., new CommandConsole(...))` for several. `ReplStarter.ExecuteLineAsync` does not go through this callback (it passes `null` to `RunPipelineAsync` with its own callback `RunReplStepAsync`). The callback can be `(command, commandConsole) => Start(command, commandConsole!, cancellationToken)`.

### CmdStarter.Lib/Repl/ReplStarter.cs:77

- **File**: `CmdStarter.Lib/Repl/ReplStarter.cs`
- **Line**: 77, the opening brace of the `Console.CancelKeyPress` handler.
- **Code**:

  ```csharp
  {
  ```

- **Reason**: the handler only runs when the operating system raises Ctrl+C for the process; see the entry for line 78.
- **Change needed**: same as line 78.
- **Why refused**: same as line 78.
- **Proposal**: `keep (justified)`.

### CmdStarter.Lib/Repl/ReplStarter.cs:78

- **File**: `CmdStarter.Lib/Repl/ReplStarter.cs`
- **Line**: 78, the line and its two branches (`CancelCurrentCommand()` true and false).
- **Code**:

  ```csharp
  if (CancelCurrentCommand())
  ```

- **Reason**: the handler is a lambda registered on `Console.CancelKeyPress`, which only the operating system raises (Ctrl+C or Ctrl+Break on the process). The event cannot be raised from managed code, and its argument `ConsoleCancelEventArgs` is a sealed class with no public constructor (only `internal ConsoleCancelEventArgs(ConsoleSpecialKey)`, checked by reflection on net10). The decision it relies on, `CancelCurrentCommand()`, is covered (cancelling the current command, and returning `false` when none runs); only the glue to the event is not.
- **Change needed**: the handler would have to be a method of `ReplStarter` that a test can call without `ConsoleCancelEventArgs`, for example `internal bool HandleCancelKey() => CancelCurrentCommand();` with the lambda reduced to `(_, eventArgs) => eventArgs.Cancel |= HandleCancelKey()`. Even then the lambda line would stay uncovered: the line to cover is the one registered on the event. The only other way is a test that signals its own process (`GenerateConsoleCtrlEvent`), or builds `ConsoleCancelEventArgs` through its internal constructor: the first reaches every process attached to the console (the test host and `dotnet test`), the second is reflection on `System.Console` internals.
- **Why refused**: the extra method would exist only to be called by a test, would not cover the lambda, and the two real paths either kill the test run or depend on a BCL internal. No test sends a real Ctrl+C either (no process test does: searched for `Ctrl`, `CancelKey` and `GenerateConsoleCtrlEvent` in `CmdStarter.Tests`); in memory, the decision is checked through `CancelCurrentCommand()`.
- **Proposal**: `keep (justified)`. Main decides whether the signal glue deserves a seam anyway.

### CmdStarter.Lib/Repl/ReplStarter.cs:79

- **File**: `CmdStarter.Lib/Repl/ReplStarter.cs`
- **Line**: 79, the opening brace of the `if` body.
- **Code**:

  ```csharp
  {
  ```

- **Reason**: only reached when the handler runs and `CancelCurrentCommand()` is true; see line 78.
- **Change needed**: same as line 78.
- **Why refused**: same as line 78.
- **Proposal**: `keep (justified)`.

### CmdStarter.Lib/Repl/ReplStarter.cs:80

- **File**: `CmdStarter.Lib/Repl/ReplStarter.cs`
- **Line**: 80.
- **Code**:

  ```csharp
  eventArgs.Cancel = true;
  ```

- **Reason**: it keeps the process alive when a command was cancelled; it needs a real `ConsoleCancelEventArgs` raised by the operating system.
- **Change needed**: same as line 78.
- **Why refused**: same as line 78.
- **Proposal**: `keep (justified)`.

### CmdStarter.Lib/Repl/ReplStarter.cs:81

- **File**: `CmdStarter.Lib/Repl/ReplStarter.cs`
- **Line**: 81, the closing brace of the `if` body.
- **Code**:

  ```csharp
  }
  ```

- **Reason**: only reached after line 80; see line 78.
- **Change needed**: same as line 78.
- **Why refused**: same as line 78.
- **Proposal**: `keep (justified)`.