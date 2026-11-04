# Coverage justifications

Lines and branches of `CmdStarter.Lib`, `CmdStarter.Hosting` and `CmdStarter.Analyzers` that no test reaches. `measure-coverage.ps1` lists the uncovered code in `coverage/report/uncovered.md`; a line or branch only goes here when it cannot be covered, and Main decides each entry. No line is excluded from the measure with `[ExcludeFromCodeCoverage]`: the measure stays honest and this file explains the gap.

A guard against a case that is impossible by construction is not justified: it is proposed for removal (**Proposal**: `remove (impossible by construction)`), with the proof of the impossibility. Nothing is removed by the author of the entry: Main decides, then the removal is done as a correction.

## Entry format

Each entry is a level-3 heading `File:line`, then eight items:

- **File**: path from the repository root.
- **Line**: line number at the time of the entry (the exact code below allows finding it again), and the branch when the line is only partly covered.
- **Code**: the exact code, in a code block.
- **Reason**: why no test reaches it.
- **Change needed**: what would have to change in the library to reach it.
- **Why refused**: why that change is not acceptable ("defensive guard" alone is not an answer).
- **Proposal**: `keep (justified)`, or `remove (impossible by construction)` followed by the proof of the impossibility (who calls it, with what).
- **Decision**: filled in only after Main has decided, for example `kept, accepted by Main on 2026-11-04`. An entry accepted for removal leaves this file once the code is removed.

## Entries

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
- **Decision**: kept, accepted by Main on 2026-11-04.

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
- **Decision**: kept, accepted by Main on 2026-11-04.

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
- **Decision**: kept, accepted by Main on 2026-11-04.

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
- **Decision**: kept, accepted by Main on 2026-11-04.

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
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs:46

- **File**: `CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs`
- **Line**: 46, the branches where `ApplicationSyntaxReference` is `null` (2 of 4 branches are covered).
- **Code**:

  ```csharp
  var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? method.Locations.FirstOrDefault();
  ```

- **Reason**: Roslyn gives a `SymbolAnalysisContext` only the symbols declared in the compilation being analyzed, and the attributes of a source symbol are bound from their `AttributeSyntax`, so `ApplicationSyntaxReference` is never `null` for them (probe on net10 with Roslyn 5.0: the attributes of a source class, of its method and of a partial class all have a reference; only the attributes of a type loaded from metadata, here `AliasAttribute`, give `null`).
- **Change needed**: A method symbol that does not come from source would have to be analyzed.
- **Why refused**: The analyzer is registered with `RegisterSymbolAction`, which does not receive symbols of referenced assemblies.
- **Proposal**: `keep (justified)`: « la nullité est déclarée par Roslyn, pas par notre construction ; une garde coûte un `?.`, une erreur coûte la construction de l'utilisateur (D3 le montre). »
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs:74

- **File**: `CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs`
- **Line**: 74, the branch where `ApplicationSyntaxReference` is `null` (1 of 2 branches is covered).
- **Code**:

  ```csharp
  context.ReportDiagnostic(Diagnostic.Create(Descriptors.MixedModels, methods[0].GetAttributes().First(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, commandAttribute)).ApplicationSyntaxReference?.GetSyntax().GetLocation(), type.Name));
  ```

- **Reason**: Roslyn gives a `SymbolAnalysisContext` only the symbols declared in the compilation being analyzed, and the attributes of a source symbol are bound from their `AttributeSyntax`, so `ApplicationSyntaxReference` is never `null` for them (probe on net10 with Roslyn 5.0: the attributes of a source class, of its method and of a partial class all have a reference; only the attributes of a type loaded from metadata, here `AliasAttribute`, give `null`).
- **Change needed**: A type that does not come from source would have to be analyzed.
- **Why refused**: Same as line 46: the symbol action only receives source symbols.
- **Proposal**: `keep (justified)`: « la nullité est déclarée par Roslyn, pas par notre construction ; une garde coûte un `?.`, une erreur coûte la construction de l'utilisateur (D3 le montre). »
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs:80

- **File**: `CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs`
- **Line**: 80, the branches where `ApplicationSyntaxReference` is `null` and where the fallback location is used (2 of 4 branches are covered).
- **Code**:

  ```csharp
  context.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyGroup, classAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault(), type.Name));
  ```

- **Reason**: Roslyn gives a `SymbolAnalysisContext` only the symbols declared in the compilation being analyzed, and the attributes of a source symbol are bound from their `AttributeSyntax`, so `ApplicationSyntaxReference` is never `null` for them (probe on net10 with Roslyn 5.0: the attributes of a source class, of its method and of a partial class all have a reference; only the attributes of a type loaded from metadata, here `AliasAttribute`, give `null`).
- **Change needed**: A type that does not come from source would have to be analyzed.
- **Why refused**: Same as line 46.
- **Proposal**: `keep (justified)`: « la nullité est déclarée par Roslyn, pas par notre construction ; une garde coûte un `?.`, une erreur coûte la construction de l'utilisateur (D3 le montre). »
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Analyzers/AutoCompleteAttributeAnalyzer.cs:37

- **File**: `CmdStarter.Analyzers/AutoCompleteAttributeAnalyzer.cs`
- **Line**: 37, the branch where `attributeClass` is `null` (5 of 6 branches are covered).
- **Code**:

  ```csharp
  if (attributeClass is null || (!SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition, auto) && !SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition, generic)))
  ```

- **Reason**: `AttributeClass` is declared nullable, but an attribute that does not bind has an error type, not `null` (probe on `[Unknown] public string P`: class kind `Error`, name `Unknown`). The analyzer tests run on such code (`AnalyzersDoNotFailOnCodeThatDoesNotCompile`) and the branch is not taken.
- **Change needed**: An attribute with a `null` `AttributeClass` would have to be read.
- **Why refused**: Roslyn only returns `null` for an `AttributeData` it could not bind to a type at all, and the probe shows that even an attribute that does not resolve gets an error type.
- **Proposal**: `keep (justified)`: « la nullité est déclarée par Roslyn, pas par notre construction ; une garde coûte un `?.`, une erreur coûte la construction de l'utilisateur (D3 le montre). »
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Analyzers/AutoCompleteAttributeAnalyzer.cs:42

- **File**: `CmdStarter.Analyzers/AutoCompleteAttributeAnalyzer.cs`
- **Line**: 42, the branches where `ApplicationSyntaxReference` is `null` (2 of 4 branches are covered).
- **Code**:

  ```csharp
  var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? context.Symbol.Locations.FirstOrDefault();
  ```

- **Reason**: Roslyn gives a `SymbolAnalysisContext` only the symbols declared in the compilation being analyzed, and the attributes of a source symbol are bound from their `AttributeSyntax`, so `ApplicationSyntaxReference` is never `null` for them (probe on net10 with Roslyn 5.0: the attributes of a source class, of its method and of a partial class all have a reference; only the attributes of a type loaded from metadata, here `AliasAttribute`, give `null`).
- **Change needed**: A property or parameter that does not come from source would have to be analyzed.
- **Why refused**: Same as `CommandDefinitionAnalyzer.cs:46`.
- **Proposal**: `keep (justified)`: « la nullité est déclarée par Roslyn, pas par notre construction ; une garde coûte un `?.`, une erreur coûte la construction de l'utilisateur (D3 le montre). »
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Analyzers/ParentAttributeAnalyzer.cs:37

- **File**: `CmdStarter.Analyzers/ParentAttributeAnalyzer.cs`
- **Line**: 37, the branches where `ApplicationSyntaxReference` is `null` (2 of 4 branches are covered).
- **Code**:

  ```csharp
  context.ReportDiagnostic(Diagnostic.Create(Descriptors.MultipleParents, attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault(), type.Name));
  ```

- **Reason**: Roslyn gives a `SymbolAnalysisContext` only the symbols declared in the compilation being analyzed, and the attributes of a source symbol are bound from their `AttributeSyntax`, so `ApplicationSyntaxReference` is never `null` for them (probe on net10 with Roslyn 5.0: the attributes of a source class, of its method and of a partial class all have a reference; only the attributes of a type loaded from metadata, here `AliasAttribute`, give `null`).
- **Change needed**: A type that does not come from source would have to be analyzed.
- **Why refused**: Same as `CommandDefinitionAnalyzer.cs:46`.
- **Proposal**: `keep (justified)`: « la nullité est déclarée par Roslyn, pas par notre construction ; une garde coûte un `?.`, une erreur coûte la construction de l'utilisateur (D3 le montre). »
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Analyzers/ParentAttributeAnalyzer.cs:46

- **File**: `CmdStarter.Analyzers/ParentAttributeAnalyzer.cs`
- **Line**: 46, the branches where `ApplicationSyntaxReference` is `null` (2 of 4 branches are covered).
- **Code**:

  ```csharp
  var firstLocation = attrs[0].ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault();
  ```

- **Reason**: Roslyn gives a `SymbolAnalysisContext` only the symbols declared in the compilation being analyzed, and the attributes of a source symbol are bound from their `AttributeSyntax`, so `ApplicationSyntaxReference` is never `null` for them (probe on net10 with Roslyn 5.0: the attributes of a source class, of its method and of a partial class all have a reference; only the attributes of a type loaded from metadata, here `AliasAttribute`, give `null`).
- **Change needed**: A type that does not come from source would have to be analyzed.
- **Why refused**: Same as `CommandDefinitionAnalyzer.cs:46`.
- **Proposal**: `keep (justified)`: « la nullité est déclarée par Roslyn, pas par notre construction ; une garde coûte un `?.`, une erreur coûte la construction de l'utilisateur (D3 le montre). »
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Lib/Repl/Remote/ReplPipeServer.cs:78

- **File**: `CmdStarter.Lib/Repl/Remote/ReplPipeServer.cs`
- **Line**: 78, the `catch` around the wait for a client.
- **Code**:

  ```csharp
  catch (IOException)
  ```

- **Reason**: `WaitForConnectionAsync` throws `IOException` when a client connected to the pipe instance and closed it before the server started to wait. A probe with a raw `NamedPipeServerStream` (create the instance, connect a client and dispose it, then wait) throws `IOException` ("The pipe is being closed", French on the test machine) on 5 runs out of 5. In `RunSessionAsync` the wait starts right after the instance is created, so a test cannot place a client between the two without a race.
- **Change needed**: a seam between the creation of the pipe instance and the wait (for example an internal hook or an injectable pipe factory), so that a test connects and closes a client first.
- **Why refused**: it adds an injection point to the server only for this test, and without it the case is a timing race that would make the test unstable (the remote tests already had an unstable test). Removing the `catch` is refused too: a client that leaves at that moment would stop the whole server, since the exception would leave `RunAsync`.
- **Proposal**: `keep (justified)`.
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Lib/Repl/Remote/ReplPipeServer.cs:79

- **File**: `CmdStarter.Lib/Repl/Remote/ReplPipeServer.cs`
- **Line**: 79, the opening brace of the `catch`.
- **Code**:

  ```csharp
  {
  ```

- **Reason**: only reached through line 78.
- **Change needed**: same as line 78.
- **Why refused**: same as line 78.
- **Proposal**: `keep (justified)`.
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Lib/Repl/Remote/ReplPipeServer.cs:81

- **File**: `CmdStarter.Lib/Repl/Remote/ReplPipeServer.cs`
- **Line**: 81.
- **Code**:

  ```csharp
  return;
  ```

- **Reason**: only reached through line 78: the session ends before it began, and `RunAsync` waits for the next client.
- **Change needed**: same as line 78.
- **Why refused**: same as line 78.
- **Proposal**: `keep (justified)`.
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Lib/Binding/CommandActivation.cs:26

- **File**: `CmdStarter.Lib/Binding/CommandActivation.cs`
- **Line**: 26, the branch where `disposed` is already `true` (1 of 2 branches is covered).
- **Code**:

  ```csharp
  if (disposed) return;
  ```

- **Reason**: It is only reached by a second `DisposeAsync` on the same `CommandActivation`. Each activation has one releaser: `InstantiateCommandsAsync` (a `foreach` that awaits each activation once) and the `await using var activation` of the two execution methods. Probe: with a `throw` in place of the `return` (here and in the synchronous `Dispose`), the whole suite (591 tests) passed, so neither guard is ever taken.
- **Change needed**: Something would have to dispose the same activation twice, or the class would have to become reachable by the tests.
- **Why refused**: `CommandActivation` is `internal` and the library has no `InternalsVisibleTo`; the tests only reach it through `Starter`, which releases every activation once.
- **Proposal**: `remove (impossible by construction)`: the `disposed` test of `DisposeAsync` (line 26) can go; the field stays for the synchronous `Dispose`.
- **Decision**:

### CmdStarter.Lib/Binding/CommandBinder.cs:36

- **File**: `CmdStarter.Lib/Binding/CommandBinder.cs`
- **Line**: 36 to 37, the branches of the pattern in `FindGetter` that need another `GetValue` overload (5 of 10 branches are covered).
- **Code**:

  ```csharp
  .First(method => method.GetParameters() is [{ ParameterType.IsGenericType: true } parameter]
      && parameter.ParameterType.GetGenericTypeDefinition() == symbolType);
  ```

- **Reason**: `ParseResult` has three generic `GetValue` methods, listed by reflection in this order: `GetValue<T>(Argument<T>)`, `GetValue<T>(Option<T>)`, `GetValue<T>(string)` (probe on System.CommandLine 2.0.14). `First` stops at the match, so the lookup of `Argument<>` tests one method and the lookup of `Option<>` two; the `string` overload is never tested, and it is the only one that has a non-generic parameter. Every overload has exactly one parameter.
- **Change needed**: Either the order of `GetMethods`, which the documentation leaves unspecified, or a different set of `GetValue` overloads in System.CommandLine would have to change.
- **Why refused**: A test can choose neither. The pattern is what keeps the lookup correct whatever the order; replacing it by an explicit signature lookup (`GetMethod("GetValue", 1, new[] { typeof(Option<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })`) would remove the branches, but that is a change of the library for Main to decide.
- **Proposal**: `keep (justified)`.
- **Decision**:

### CmdStarter.Lib/Binding/CommandBinder.cs:69

- **File**: `CmdStarter.Lib/Binding/CommandBinder.cs`
- **Line**: 69, the branch where `i >= arguments.Length` (1 of 2 branches is covered).
- **Code**:

  ```csharp
  var argument = i < arguments.Length ? arguments[i] : null;
  ```

- **Reason**: `arguments` comes from `Loader.LoadArguments`, which creates an array with one entry per parameter of the handler (`new Argument?[parameters.Length]`, `Reflection/Loader.cs` line 55), and it is the only value passed as `arguments` (`Starter.cs` lines 1007 and 1071). Probe: with a `throw` on that branch, the whole suite passed.
- **Change needed**: A handler with more parameters than the array would have to be invoked, that is a `HandlingMethod` whose signature differs between the description instance and the execution instance.
- **Why refused**: The two instances are of the same type and `HandlingMethod` is one property of it; nothing in the library or in a test can give them different signatures without breaking the command itself.
- **Proposal**: `remove (impossible by construction)`: `var argument = arguments[i];`.
- **Decision**:

### CmdStarter.Lib/Binding/CommandBinder.cs:82

- **File**: `CmdStarter.Lib/Binding/CommandBinder.cs`
- **Line**: 82 to 85, the branch where the parameter has a default value (lines 83, 84 and 85 are uncovered, and 1 of 2 branches of line 82).
- **Code**:

  ```csharp
  else if (parameters[i].HasDefaultValue)
  {
      values[i] = parameters[i].DefaultValue;
  }
  ```

- **Reason**: A parameter reaches this chain only when `LoadArguments` gave it no argument and it is neither a `CancellationToken` nor a `CommandConsole`: its `Name` is `null` (`Loader.cs` line 60). Probe: the parameter of a `DynamicMethod` delegate has a `null` name and `HasDefaultValue` is `false`; the parameter of a method emitted with `MethodBuilder`, named `null` and given a default value, has the name `""` (not `null`), so it becomes an argument.
- **Change needed**: A handler parameter with a `null` name and a default value would have to exist.
- **Why refused**: The two ways to build a handler without parameter names (a `DynamicMethod`) or with an empty one (Reflection.Emit) give, respectively, no default value and a non-null name; the probe found no third way.
- **Proposal**: `remove (impossible by construction)`: the `else if` and its body (lines 82 to 85); the following `else` stays.
- **Decision**:

### CmdStarter.Lib/Binding/CommandBinder.cs:103

- **File**: `CmdStarter.Lib/Binding/CommandBinder.cs`
- **Line**: 103, the statement after `ExceptionDispatchInfo.Throw()`.
- **Code**:

  ```csharp
  throw;
  ```

- **Reason**: `ExceptionDispatchInfo.Throw()` never returns, so the statement is never executed. The C# compiler does not use that fact: without it, the build fails with CS0161 ("not all code paths return a value", checked by removing the line).
- **Change needed**: The method would have to end in a way the compiler accepts without a statement after the call, for example `throw exception.InnerException` (which loses the original stack trace, the reason for `ExceptionDispatchInfo`).
- **Why refused**: Keeping the stack trace of the handler's exception is the point of this method.
- **Proposal**: `keep (justified)`.
- **Decision**:

### CmdStarter.Lib/GlobalOptionsManager.cs:52

- **File**: `CmdStarter.Lib/GlobalOptionsManager.cs`
- **Line**: 52 to 63, the body of `SetGlobalOptions` (10 lines, 2 branches).
- **Code**:

  ```csharp
  internal void SetGlobalOptions<TGlobalOptions>(TGlobalOptions globalOption) where TGlobalOptions : class, IGlobalOptionsContainer
  {
      var type = typeof(TGlobalOptions);

      if (globalOptions.ContainsKey(type))
      {
          globalOptions[type] = globalOption;
      }
      else
      {
          globalOptions.Add(type, globalOption);
      }
  }
  ```

- **Reason**: The method has no caller. `globalOptions` is filled by `BindGlobalOptions` (line 113) only. Search over the whole repository: `SetGlobalOptions` appears once, at its declaration, and the library has no `InternalsVisibleTo`, so no test project can call an `internal` member either.
- **Change needed**: A caller would have to exist.
- **Why refused**: None would be added only to cover this method.
- **Proposal**: `remove (impossible by construction)`: remove the whole method (dead code).
- **Decision**:

### CmdStarter.Lib/Loader/CommandsTreeBuilder.cs:87

- **File**: `CmdStarter.Lib/Loader/CommandsTreeBuilder.cs`
- **Line**: 87, the branch where `parentAttribute` is `null` (3 of 4 branches are covered).
- **Code**:

  ```csharp
  if (parentAttribute?.Parent == null)
  ```

- **Reason**: `command` comes from `CommandsTypes.Where(t => t.GetCustomAttributes(false).Any(IsParent))` and `GetParentAttribute` returns the attribute that the same `IsParent` predicate accepts, converted with `as ParentAttribute`, which succeeds for any type assignable to `ParentAttribute`. More than one such attribute throws before the return. `parentAttribute` is therefore never `null` here (it can be `null` at line 117, for the parent type, and that case is covered).
- **Change needed**: A command type would have to pass `IsParent` and still not yield a `ParentAttribute`.
- **Why refused**: Both use the same test on the same attributes.
- **Proposal**: `remove (impossible by construction)`: `parentAttribute!.Parent == null`.
- **Decision**:

### CmdStarter.Lib/Loader/CommandsTreeBuilder.cs:145

- **File**: `CmdStarter.Lib/Loader/CommandsTreeBuilder.cs`
- **Line**: 145, the branch where the cast yields `null` (3 of 4 branches are covered; the `?? string.Empty` one is covered by `[Children(null)]`).
- **Code**:

  ```csharp
  .Select(a => (a as ChildrenAttribute)?.Namespace ?? string.Empty)
  ```

- **Reason**: The previous line keeps only the attributes for which `c is ChildrenAttribute` is true, so the `as` cast cannot give `null`.
- **Change needed**: An attribute would have to pass `is ChildrenAttribute` and fail `as ChildrenAttribute`.
- **Why refused**: The two operators test the same thing.
- **Proposal**: `remove (impossible by construction)`: replace the `Where`/`Select` by `OfType<ChildrenAttribute>()` or write `((ChildrenAttribute)a).Namespace ?? string.Empty`.
- **Decision**:

### CmdStarter.Lib/Loader/CommandsTreeBuilder.cs:155

- **File**: `CmdStarter.Lib/Loader/CommandsTreeBuilder.cs`
- **Line**: 155, the branch where `t.Namespace` is `null` (1 of 2 branches is covered).
- **Code**:

  ```csharp
  childrenNamespace.Any(n => t.Namespace?.StartsWith(n, StringComparison.Ordinal) ?? false)
  ```

- **Reason**: `CommandsTreeBuilder` is internal and built in one place, from `Starter.CommandsTypes` (`Starter.cs` line 727). That list is either found by discovery, which keeps only the types with a namespace (`Starter.cs` line 670), or given to the `CommandsTypes` setter, which refuses a type without a namespace (`ValidateCommandsTypes`, test `CommandsTypesSetter` with `GlobalNamespaceCommand`). The tree defined with `UseTree` does not use the builder.
- **Change needed**: A command type without a namespace would have to reach the builder.
- **Why refused**: The setter rejects it and discovery skips it.
- **Proposal**: `remove (impossible by construction)`: `t.Namespace!.StartsWith(n, StringComparison.Ordinal)`.
- **Decision**:

### CmdStarter.Lib/Loader/CommandsTreeBuilder.cs:214

- **File**: `CmdStarter.Lib/Loader/CommandsTreeBuilder.cs`
- **Line**: 214, the branch where `t.Namespace` is `null` (1 of 2 branches is covered).
- **Code**:

  ```csharp
  var children = CommandsTypes.Where(t => t.Namespace?.StartsWith(parentNode.Value!.Namespace + ".", StringComparison.Ordinal) ?? false);
  ```

- **Reason**: Same as line 155: every type of `CommandsTypes` has a namespace.
- **Change needed**: Same as line 155.
- **Why refused**: Same as line 155.
- **Proposal**: `remove (impossible by construction)`: `t.Namespace!.StartsWith(...)`, without the `?? false`.
- **Decision**:

### CmdStarter.Lib/Loader/CommandsTreeBuilder.cs:221

- **File**: `CmdStarter.Lib/Loader/CommandsTreeBuilder.cs`
- **Line**: 221, the branch where `childNode.Parent` is `null` (5 of 6 branches are covered).
- **Code**:

  ```csharp
  var currentNamespace = childNode.Parent?.Value?.Namespace ?? string.Empty;
  ```

- **Reason**: `childNode` comes from `tree.FindNode(child)` with a non-null `child`. `FindNode` compares values, and the only node without a value is the root (`new TreeNode<Type>(null)`), so the node found is never the root and always has a parent. The `Value?` (the parent is the root) and `Namespace` null cases are covered by the other branches.
- **Change needed**: `FindNode` would have to return the root of the tree.
- **Why refused**: It returns a node only when its value equals the one given, and the root's value is `null`.
- **Proposal**: `remove (impossible by construction)`: `childNode.Parent!.Value?.Namespace ?? string.Empty`.
- **Decision**:

### CmdStarter.Lib/Loader/CommandsTreeBuilder.cs:222

- **File**: `CmdStarter.Lib/Loader/CommandsTreeBuilder.cs`
- **Line**: 222, the branch where the namespace of the parent is `null` (1 of 2 branches is covered).
- **Code**:

  ```csharp
  var isDeeperNamespace = parentNode.Value!.Namespace?.StartsWith(currentNamespace, StringComparison.Ordinal) ?? false;
  ```

- **Reason**: `parentNode` is the node of a type of `CommandsTypes`, which always has a namespace (see line 155).
- **Change needed**: Same as line 155.
- **Why refused**: Same as line 155.
- **Proposal**: `remove (impossible by construction)`: `parentNode.Value!.Namespace!.StartsWith(currentNamespace, StringComparison.Ordinal)`.
- **Decision**:

### CmdStarter.Lib/Reflection/Helper.cs:74

- **File**: `CmdStarter.Lib/Reflection/Helper.cs`
- **Line**: 74 to 83, the two branches where `p.DeclaringType` is `null` (12 of 14 branches are covered).
- **Code**:

  ```csharp
  (p.DeclaringType?.IsAssignableTo(typeof(IStarterCommand)) ?? false)
  ...
  (p.DeclaringType?.IsAssignableTo(typeof(IGlobalOptionsContainer)) ?? false))
  ```

- **Reason**: `p` comes from `type.GetProperties(BindingFlags.Public | BindingFlags.Instance)`. A property returned by `Type.GetProperties` is declared by a type; `DeclaringType` is `null` only for a member of a module, which C# and `Type.GetProperties` cannot give.
- **Change needed**: `Type.GetProperties` would have to return a property without a declaring type.
- **Why refused**: The framework does not do it.
- **Proposal**: `remove (impossible by construction)`: `p.DeclaringType!.IsAssignableTo(...)` in both places, and in `Attribute.IsDefined(p.DeclaringType, ...)` which already assumes it.
- **Decision**:

### CmdStarter.Lib/Reflection/Loader.cs:193

- **File**: `CmdStarter.Lib/Reflection/Loader.cs`
- **Line**: 193, the branch where `optionResult` is `null` (3 of 4 branches are covered).
- **Code**:

  ```csharp
  if (optionResult is null || optionResult.Implicit)
  ```

- **Reason**: The validator belongs to the option and runs on the result of that option; `result.GetResult(option)` gives back that same result. Probe on validators added to a real argument and a real option: for the argument `ReferenceEquals(result.GetResult(argument), result)` is `true`, and for the option `result.GetResult(option)` is not `null`.
- **Change needed**: System.CommandLine would have to run an option validator on a result that is not the one of this option.
- **Why refused**: The validator is registered on this option only.
- **Proposal**: `remove (impossible by construction)`: use `result` itself, then test `result.Implicit`.
- **Decision**:

### CmdStarter.Lib/Reflection/Loader.cs:210

- **File**: `CmdStarter.Lib/Reflection/Loader.cs`
- **Line**: 210, the branch where `argumentResult` is `null` (2 of 4 branches are covered; the `Implicit` branch is the defect D4 and is not listed here).
- **Code**:

  ```csharp
  if (argumentResult is null || argumentResult.Implicit)
  ```

- **Reason**: Same as line 193, for the argument: the probe shows that `GetResult(argument)` is the very result being validated.
- **Change needed**: Same as line 193.
- **Why refused**: Same as line 193.
- **Proposal**: `remove (impossible by construction)`: use `result` itself.
- **Decision**:

### CmdStarter.Lib/Reflection/Loader.cs:242

- **File**: `CmdStarter.Lib/Reflection/Loader.cs`
- **Line**: 242, the branch where `validationResult` is `null` (3 of 4 branches are covered).
- **Code**:

  ```csharp
  result.AddError(validationResult?.ErrorMessage ?? $"The value of '{displayName}' is invalid.");
  ```

- **Reason**: The line is reached when `validationResult != ValidationResult.Success`, and `ValidationResult.Success` is `null`: `validationResult` cannot be `null` here. The `?? ...` part is covered by `MessagelessAttribute`.
- **Change needed**: `validationResult` would have to be `null` and different from `ValidationResult.Success` at the same time.
- **Why refused**: They are the same value.
- **Proposal**: `remove (impossible by construction)`: `validationResult!.ErrorMessage ?? ...`.
- **Decision**: