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
- **Decision**:

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
- **Decision**:

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
- **Decision**: