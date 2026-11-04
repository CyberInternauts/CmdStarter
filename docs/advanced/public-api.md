# Public API tracking

This page is for contributors to CmdStarter. From version 1.0.0 the public surface of the two shipped libraries, `CmdStarter.Lib` (package `cints.CmdStarter`) and `CmdStarter.Hosting`, is recorded in text files and checked at each build. A change of the public surface can then not go unnoticed. The decision is recorded in [ADR 0031](../adr/0031-public-api-tracking.md).

## The files

Each of the two projects has two files next to its project file:

- `PublicAPI.Shipped.txt`, empty until the publication.
- `PublicAPI.Unshipped.txt`, one line per public symbol.

Both start with `#nullable enable`. `Microsoft.CodeAnalysis.PublicApiAnalyzers` 5.6.0 reads them, and it is referenced with `PrivateAssets="all"`: the packages do not depend on it and do not contain the files (the `.nuspec` of each built package lists only System.CommandLine and Microsoft.Extensions.DependencyInjection for `cints.CmdStarter`, and `cints.CmdStarter` and Microsoft.Extensions.Hosting for `cints.CmdStarter.Hosting`). `CmdStarter.Analyzers` is not tracked, because its public types exist only for Roslyn.

## When the build fails with RS0016

A public member that is not in the files fails the build, because warnings are errors. The error names the symbol in the form the file expects. For a member added to `Starter`, it names `CyberInternauts.CmdStarter.Starter.BogusPublicMember() -> void`.

If the new public member is intended:

1. Run the code fix of the rule on the project that failed. For example:

   ```powershell
   dotnet format analyzers CmdStarter.Lib/CmdStarter.Lib.csproj --diagnostics RS0016 --severity info
   ```

2. Read the lines it added to `PublicAPI.Unshipped.txt`: they are the public change of your branch, and the review reads them.
3. Build again. The build must have no warning.

If the member should not be public, make it `internal` or `private`.

## When the build fails with RS0017

A line of the files names a symbol that no longer exists or is no longer public. The error is reported at the line of the file. Remove the line if the removal is intended. After the publication, removing a shipped member is a breaking change that needs a new major version ([ADR 0031](../adr/0031-public-api-tracking.md)).

## Optional parameters

The rules RS0026 and RS0027, about several public overloads with optional parameters, are turned off in `.editorconfig` for the two projects only. Every overload they report has a single optional parameter, the trailing `CancellationToken`, and differs from its siblings by its required parameters, so the compiler resolves every call without ambiguity; the reason is in [ADR 0031](../adr/0031-public-api-tracking.md).

## See also

- Neighbouring pages: [Analyzers](analyzers.md), [Source generator](source-generator.md), [Trimming](trimming.md).
- ADR: [0031, public API tracking](../adr/0031-public-api-tracking.md), [0030, version number of the rewrite](../adr/0030-version-number-of-the-rewrite.md).
