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

### CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs:70

- **File**: `CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs`
- **Line**: 70, the branch where the attribute of the class has no `ApplicationSyntaxReference` (5 of 6 branches are covered: a class without `[Command]` takes the fallback location).
- **Code**:

  ```csharp
  var classLocation = classAttribute?.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault();
  ```

- **Reason**: Roslyn gives a `SymbolAnalysisContext` only the symbols declared in the compilation being analyzed, and the attributes of a source symbol are bound from their `AttributeSyntax`, so `ApplicationSyntaxReference` is never `null` for them (probe on net10 with Roslyn 5.0: the attributes of a source class, of its method and of a partial class all have a reference; only the attributes of a type loaded from metadata, here `AliasAttribute`, give `null`). This line replaces the location of the former CMDS0011 report (formerly line 80, same reason), lot 037.
- **Change needed**: A type that does not come from source would have to be analyzed.
- **Why refused**: Same as line 46: the symbol action only receives source symbols.
- **Proposal**: `keep (justified)`: same family as line 46.
- **Decision**: kept, accepted by Main on 2026-11-05 (same family as 2026-11-04).

### CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs:76

- **File**: `CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs`
- **Line**: 76 (formerly 74; the lines moved in lot 037), the branch where `ApplicationSyntaxReference` is `null` (1 of 2 branches is covered).
- **Code**:

  ```csharp
  context.ReportDiagnostic(Diagnostic.Create(Descriptors.MixedModels, methods[0].GetAttributes().First(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, commandAttribute)).ApplicationSyntaxReference?.GetSyntax().GetLocation(), type.Name));
  ```

- **Reason**: Roslyn gives a `SymbolAnalysisContext` only the symbols declared in the compilation being analyzed, and the attributes of a source symbol are bound from their `AttributeSyntax`, so `ApplicationSyntaxReference` is never `null` for them (probe on net10 with Roslyn 5.0: the attributes of a source class, of its method and of a partial class all have a reference; only the attributes of a type loaded from metadata, here `AliasAttribute`, give `null`).
- **Change needed**: A type that does not come from source would have to be analyzed.
- **Why refused**: Same as line 46: the symbol action only receives source symbols.
- **Proposal**: `keep (justified)`: « la nullité est déclarée par Roslyn, pas par notre construction ; une garde coûte un `?.`, une erreur coûte la construction de l'utilisateur (D3 le montre). »
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs:147

- **File**: `CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs`
- **Line**: 147, in `HandlerLocation`, the branches where `ApplicationSyntaxReference` is `null` and the fallback location is used (2 of 4 branches are covered).
- **Code**:

  ```csharp
  attribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax syntax
  ```

- **Reason**: `HandlerLocation` is asked only for the `[Command]` attribute of a source symbol that names a `Handler`, and Roslyn binds the attributes of a source symbol from their `AttributeSyntax`, so `ApplicationSyntaxReference` is never `null` for them (same probe as line 46). The other guards of the method (`ArgumentList`, the `Handler` argument itself) were impossible by construction and were removed in lot 037: the attribute names a `Handler`, which is a named argument, so the argument is in the syntax.
- **Change needed**: A type that does not come from source would have to be analyzed.
- **Why refused**: Same as line 46: the symbol action only receives source symbols.
- **Proposal**: `keep (justified)`: same family as line 46.
- **Decision**: kept, accepted by Main on 2026-11-05 (same family as 2026-11-04).

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

### CmdStarter.Lib/Repl/Remote/ReplPipeClient.cs:72

- **File**: `CmdStarter.Lib/Repl/Remote/ReplPipeClient.cs`
- **Line**: 72, the `catch` around the read of the announcement in `ConnectAsync`.
- **Code**:

  ```csharp
  catch (IOException)
  ```

- **Reason**: on Unix a named pipe is a Unix socket. A connection that waits in the queue of the listening socket is reset (`ECONNRESET`) when the instance of the running session is released, and reading it throws `IOException`. On Windows a pipe that the server closed reads as the end of the stream (`ERROR_BROKEN_PIPE` gives 0 bytes), never as an `IOException`, so the measure, which runs on Windows, cannot reach this line. `ReplRemote.ConnectTriesAgainWhenAQueuedConnectionIsClosed` reaches it on Linux: coverlet, run on that test alone under WSL (Ubuntu 20.04, .NET 10.0.403, lot 036), counts one hit on lines 72, 73, 76 and 77.
- **Change needed**: a coverage measure on Unix, merged with the Windows one.
- **Why refused**: `measure-coverage.ps1` runs on Windows only. Removing the `catch` is refused too: under Unix a queued connection reset at the end of a session would leave `ConnectAsync` with an `IOException` instead of trying again, which is the failure lot 036 fixes.
- **Proposal**: `keep (justified)`.
- **Decision**: kept, accepted by Main on 2026-11-04: reached on Unix only (ECONNRESET of a queued connection), proven by the test in the WSL run; Windows reads a closed pipe as end of stream.

### CmdStarter.Lib/Repl/Remote/ReplPipeClient.cs:73

- **File**: `CmdStarter.Lib/Repl/Remote/ReplPipeClient.cs`
- **Line**: 73, the opening brace of the `catch`.
- **Code**:

  ```csharp
  {
  ```

- **Reason**: only reached through line 72.
- **Change needed**: same as line 72.
- **Why refused**: same as line 72.
- **Proposal**: `keep (justified)`.
- **Decision**: kept, accepted by Main on 2026-11-04 (same decision as line 72).

### CmdStarter.Lib/Repl/Remote/ReplPipeClient.cs:76

- **File**: `CmdStarter.Lib/Repl/Remote/ReplPipeClient.cs`
- **Line**: 76.
- **Code**:

  ```csharp
  line = null;
  ```

- **Reason**: only reached through line 72: the reset connection is treated as the end of the stream, and the loop tries again with what is left of the timeout.
- **Change needed**: same as line 72.
- **Why refused**: same as line 72.
- **Proposal**: `keep (justified)`.
- **Decision**: kept, accepted by Main on 2026-11-04 (same decision as line 72).

### CmdStarter.Lib/Repl/Remote/ReplPipeClient.cs:77

- **File**: `CmdStarter.Lib/Repl/Remote/ReplPipeClient.cs`
- **Line**: 77, the closing brace of the `catch`.
- **Code**:

  ```csharp
  }
  ```

- **Reason**: only reached through line 72.
- **Change needed**: same as line 72.
- **Why refused**: same as line 72.
- **Proposal**: `keep (justified)`.
- **Decision**: kept, accepted by Main on 2026-11-04 (same decision as line 72).

### CmdStarter.Lib/Binding/CommandActivation.cs:25

- **File**: `CmdStarter.Lib/Binding/CommandActivation.cs`
- **Line**: 25, the branch where `disposed` is already `true` (1 of 2 branches is covered).
- **Code**:

  ```csharp
  if (disposed) return;
  ```

- **Reason**: It is only reached by a second `DisposeAsync` on the same `CommandActivation`. Each activation has one releaser: `InstantiateCommandsAsync` (a `foreach` that awaits each activation once) and the `await using var activation` of the two execution methods. Probe: with a `throw` in place of the `return` (here and in the synchronous `Dispose`), the whole suite (591 tests) passed, so neither guard is ever taken.
- **Change needed**: Something would have to dispose the same activation twice, which the library never does.
- **Why refused**: Removing the guard would make a second call run the disposal again. The idempotence of `Dispose`/`DisposeAsync` is the .NET contract of `IDisposable` ("can be called multiple times"), not a case of our construction: a caller outside the library, or a later change, may legitimately release twice.
- **Proposal**: `keep (justified)`: contract of IDisposable.
- **Decision**: kept, accepted by Main on 2026-11-04.

### CmdStarter.Lib/Binding/CommandBinder.cs:86

- **File**: `CmdStarter.Lib/Binding/CommandBinder.cs`
- **Line**: 86, the statement after `ExceptionDispatchInfo.Throw()`.
- **Code**:

  ```csharp
  throw;
  ```

- **Reason**: `ExceptionDispatchInfo.Throw()` never returns, so the statement is never executed. The C# compiler does not use that fact: without it, the build fails with CS0161 ("not all code paths return a value", checked by removing the line).
- **Change needed**: The method would have to end in a way the compiler accepts without a statement after the call, for example `throw exception.InnerException` (which loses the original stack trace, the reason for `ExceptionDispatchInfo`).
- **Why refused**: Keeping the stack trace of the handler's exception is the point of this method.
- **Proposal**: `keep (justified)`.
- **Decision**: kept, accepted by Main on 2026-11-04.


### CmdStarter.Lib/Repl/ConsoleReplProvider.cs:46

- **File**: `CmdStarter.Lib/Repl/ConsoleReplProvider.cs`
- **Line**: 46 to 63, the branch of the editor (lines 47 to 63 are uncovered, and 3 of 6 branches of line 46 are covered).
- **Code**:

  ```csharp
  if (LineEditing && !Console.IsInputRedirected && !Console.IsOutputRedirected)
  {
      cancellationToken.ThrowIfCancellationRequested();
      editor ??= new ReplLineEditor(() => Console.ReadKey(intercept: true), Console.Out) { HistorySize = HistorySize };
      editor.CompletionSource = CompletionSource;

      // Ctrl+C is a key while the line is read, so it abandons the line instead of ending the process; a running command gets the signal again.
      var treatControlCAsInput = Console.TreatControlCAsInput;
      Console.TreatControlCAsInput = true;
      try
      {
          return editor.ReadLine(Prompt);
      }
      finally
      {
          Console.TreatControlCAsInput = treatControlCAsInput;
      }
  }
  ```

- **Reason**: The branch needs a console on which neither the standard input nor the standard output is redirected, because it reads keys with `Console.ReadKey`, which throws on a redirected input. Probe in the test host: `Console.IsInputRedirected`, `Console.IsOutputRedirected` and `Console.IsErrorRedirected` are all `True`. The separate processes of `AppProcessTests` and `AppRunner` redirect standard input too. The raw path (`LineEditing = false`, or a redirected stream) is covered in memory by `ConsoleReplProviderInput`, and the editor itself by `ReplLineEditing` and `ReplLineEditorEdges` with simulated keys.
- **Change needed**: Either the test would run the application in a pseudo-console (ConPTY on Windows, a pty elsewhere), or the provider would take the console detection, the key reader and the Ctrl+C mode (`Console.TreatControlCAsInput`, which only a real console accepts) as injectable members (for example public `Func<bool>` and `Func<ConsoleKeyInfo>` init properties).
- **Why refused**: A pseudo-console test needs platform-specific native code (P/Invoke of the ConPTY API, another mechanism on Linux) and a child process that really owns a console: that is a test infrastructure much larger than the 5 lines it would cover. An internal seam is possible since lot 031's `InternalsVisibleTo`, but it would still leave line 49 uncovered, the lambda `() => Console.ReadKey(intercept: true)` that is exactly the binding to the real console, and it would add a test-only member to cover four lines; that `InternalsVisibleTo` serves only to prove the symbol factories. The pseudo-console driver that the Helper used for the manual check of Ctrl+C (about 250 lines of Windows-only P/Invoke) stays out of the suite for the same reason.
- **Proposal**: `keep (justified)`.
- **Decision**: kept, accepted by Main on 2026-11-05.
