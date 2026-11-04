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

### CmdStarter.Analyzers/OptionsAnalyzer.cs:160

- **File**: `CmdStarter.Analyzers/OptionsAnalyzer.cs`
- **Line**: 160, the branch where the first constructor argument is not a `bool` (3 of 4 branches are covered).
- **Code**:

  ```csharp
  var usePrefix = attribute.ConstructorArguments.Length > 1 && attribute.ConstructorArguments[0].Value is bool value && value;
  ```

- **Reason**: More than one constructor argument means the constructor `AliasAttribute(bool usePrefix, params string[] aliases)`, whose first argument is always a `bool`. A failed binding (code that does not compile) gives no constructor argument at all (probe on `[Alias(undefined)]` and `[Alias(undefined, "x")]`), so `Length > 1` is false.
- **Change needed**: An attribute with two or more arguments whose first one is not a `bool` would have to reach this line.
- **Why refused**: The only `AliasAttribute` looked up is the one of CmdStarter.Lib, which has two constructors: `(params string[])` and `(bool, params string[])`.
- **Proposal**: `remove (impossible by construction)`: with `Length > 1` the first argument is the `bool` of the second constructor. The expression can become `attribute.ConstructorArguments.Length > 1 && true.Equals(attribute.ConstructorArguments[0].Value)`.
- **Decision**:

### CmdStarter.Analyzers/OptionsAnalyzer.cs:173

- **File**: `CmdStarter.Analyzers/OptionsAnalyzer.cs`
- **Line**: 173, the branch where `values.Value` is a `string`.
- **Code**:

  ```csharp
  else if (values.Value is string text)
  ```

- **Reason**: The alias argument is always an array (the `params string[]` of both constructors), or missing (`default` `TypedConstant`, `Value` `null`) when the code does not compile (probe: `[Alias(undefined)]` has no constructor argument).
- **Change needed**: A constructor of `AliasAttribute` taking a single `string` would have to exist.
- **Why refused**: The attribute is in CmdStarter.Lib and has no such constructor; adding one only for the analyzer is out of the question.
- **Proposal**: `remove (impossible by construction)`: the `else if` and its body (lines 173 to 176) can go; the `values.Kind == TypedConstantKind.Array` test already covers every case that yields an alias.
- **Decision**:

### CmdStarter.Analyzers/OptionsAnalyzer.cs:174

- **File**: `CmdStarter.Analyzers/OptionsAnalyzer.cs`
- **Line**: 174, the opening brace of the `else if` body.
- **Code**:

  ```csharp
  {
  ```

- **Reason**: Only reached when `values.Value` is a `string` (see line 173).
- **Change needed**: Same as line 173.
- **Why refused**: Same as line 173.
- **Proposal**: `remove (impossible by construction)`, with line 173.
- **Decision**:

### CmdStarter.Analyzers/OptionsAnalyzer.cs:175

- **File**: `CmdStarter.Analyzers/OptionsAnalyzer.cs`
- **Line**: 175, the line and its two branches (`usePrefix` true and false).
- **Code**:

  ```csharp
  yield return (usePrefix ? prefix : string.Empty) + text;
  ```

- **Reason**: Only reached when `values.Value` is a `string` (see line 173).
- **Change needed**: Same as line 173.
- **Why refused**: Same as line 173.
- **Proposal**: `remove (impossible by construction)`, with line 173.
- **Decision**:

### CmdStarter.Analyzers/OptionsAnalyzer.cs:176

- **File**: `CmdStarter.Analyzers/OptionsAnalyzer.cs`
- **Line**: 176, the closing brace of the `else if` body.
- **Code**:

  ```csharp
  }
  ```

- **Reason**: Only reached after line 175 (see line 173).
- **Change needed**: Same as line 173.
- **Why refused**: Same as line 173.
- **Proposal**: `remove (impossible by construction)`, with line 173.
- **Decision**:

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
- **Proposal**: `remove (impossible by construction)`: `attribute.ApplicationSyntaxReference!.GetSyntax().GetLocation()`. The attribute was found with `FirstOrDefault` on the method of a source symbol and tested for `null` just above.
- **Decision**:

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
- **Proposal**: `remove (impossible by construction)`: `?.` becomes `!.`, for the same reason as line 46.
- **Decision**:

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
- **Proposal**: `remove (impossible by construction)`: `classAttribute.ApplicationSyntaxReference!.GetSyntax().GetLocation()`, without the `??`.
- **Decision**:

### CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs:116

- **File**: `CmdStarter.Analyzers/CommandDefinitionAnalyzer.cs`
- **Line**: 116, the branch where `constant.Value` is neither `null` nor a `string` (7 of 8 branches are covered).
- **Code**:

  ```csharp
  if (constant.HasValue && (constant.Value is null || constant.Value is string name && string.IsNullOrWhiteSpace(name)))
  ```

- **Reason**: The constant is the argument of a constructor already resolved to `StarterCommand(string name, string? description)`, so its value is a `string` or `null`. A value of another type does not resolve the constructor: probe on `base(5)`, `GetSymbolInfo(initializer).Symbol` is `null` (`OverloadResolutionFailure`, 2 candidates), so the method returns on line 108.
- **Change needed**: A constant of another type than `string` would have to be passed to a resolved `StarterCommand` constructor.
- **Why refused**: The language does not allow it; nothing in the analyzer or in CmdStarter.Lib can make it happen.
- **Proposal**: `remove (impossible by construction)`: the condition can be `constant.HasValue && string.IsNullOrWhiteSpace(constant.Value as string)`, which keeps `null` and blank names reported.
- **Decision**:

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
- **Proposal**: `remove (impossible by construction)`: `attributeClass is null ||` can go; `!` on the property keeps the nullable analysis quiet.
- **Decision**:

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
- **Proposal**: `remove (impossible by construction)`: `attribute.ApplicationSyntaxReference!.GetSyntax().GetLocation()`.
- **Decision**:

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
- **Proposal**: `remove (impossible by construction)`: `attribute.ApplicationSyntaxReference!.GetSyntax().GetLocation()`.
- **Decision**:

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
- **Proposal**: `remove (impossible by construction)`: `attrs[0].ApplicationSyntaxReference!.GetSyntax().GetLocation()`.
- **Decision**:

### CmdStarter.Analyzers/ParentAttributeAnalyzer.cs:49

- **File**: `CmdStarter.Analyzers/ParentAttributeAnalyzer.cs`
- **Line**: 49, the branch where `current` is `null` (3 of 4 branches are covered).
- **Code**:

  ```csharp
  while (current is not null && seen.Add(current))
  ```

- **Reason**: `current` starts as `type` (never `null`) and is only reassigned to `next`, which comes from the pattern `is { } next` on line 52: it is never `null`.
- **Change needed**: A `null` would have to be assigned to `current`.
- **Why refused**: The two assignments are on lines 48 and 63 and both are non-null; the variable is inferred `INamedTypeSymbol`.
- **Proposal**: `remove (impossible by construction)`: `while (seen.Add(current))`.
- **Decision**:

### CmdStarter.Analyzers/ParentAttributeAnalyzer.cs:69

- **File**: `CmdStarter.Analyzers/ParentAttributeAnalyzer.cs`
- **Line**: 69, the branch where `AttributeClass` is `null` (3 of 4 branches are covered).
- **Code**:

  ```csharp
  if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.OriginalDefinition, genericParent))
  ```

- **Reason**: `GetParentType` is only called with an attribute that `IsParent` accepted on line 52, and `IsParent` returns `false` for a `null` class.
- **Change needed**: An attribute with a `null` `AttributeClass` would have to be passed to `GetParentType`.
- **Why refused**: Its only caller is line 52, after `IsParent` returned `true`.
- **Proposal**: `remove (impossible by construction)`: `attribute.AttributeClass!.OriginalDefinition` (the line after already uses `attribute.AttributeClass.TypeArguments` without a test).
- **Decision**:
