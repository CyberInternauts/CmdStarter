# 0031. Public API Tracking

Status: Accepted
Date: 2026-11-04

## Context
Version 1.0.0 freezes the public surface of the two shipped libraries, `CmdStarter.Lib` and `CmdStarter.Hosting` ([ADR 0030](0030-version-number-of-the-rewrite.md)). From there the numbers follow semantic versioning: a change of the public surface that breaks a user needs a new major version. Until now nothing made such a change visible: a member could be added, renamed or made non-public in a diff that does not look like an API change.

## Decision
`Microsoft.CodeAnalysis.PublicApiAnalyzers` 5.6.0 (the last stable version on nuget.org on 2026-11-04) is referenced by `CmdStarter.Lib` and `CmdStarter.Hosting` with `PrivateAssets="all"`, so it is not a dependency of the packages. Each project has `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`, each starting with `#nullable enable`. The package adds both files to the compilation by itself.

All the public API is in `PublicAPI.Unshipped.txt`, and `PublicAPI.Shipped.txt` is empty, until the publication. This is the same choice as for `AnalyzerReleases.Shipped.md` ([ADR 0030](0030-version-number-of-the-rewrite.md)): moving the lines to `Shipped` is a step of the publication, not of the development. With `TreatWarningsAsErrors`, a public symbol that is not in the files fails the build (RS0016), and so does a declared symbol that no longer exists (RS0017).

`CmdStarter.Analyzers` is not tracked. Its public types are public only because Roslyn loads an analyzer and a generator through them; they are not an API for the users of the package, and the package carries the assembly under `analyzers/dotnet/cs` without a reference to it.

The rules RS0026 and RS0027 (several public overloads with optional parameters) are set to `none` in `.editorconfig`, in a section limited to the two projects. Every overload they report has a single optional parameter, the trailing `CancellationToken cancellationToken = default`, and differs from the others of its name by its required parameters, so the compiler resolves every call without ambiguity; the overloads `(…, CommandConsole console, CancellationToken cancellationToken = default)` next to `(…, CancellationToken cancellationToken)` are moreover intended ([ADR 0021](0021-command-console.md)). Reshaping them before 1.0 would be churn without a gain. These are not rules of the `latest-recommended` .NET analyzers, so the statement of [ADR 0008](0008-code-quality-gates.md) and [ADR 0013](0013-public-api-names.md) that no rule of those analyzers is disabled stays true. When the section was added, RS0026 reported `CmdStarterApp.RunReplAsync` (2 overloads), `ReplStarter.Launch` (3), `ReplPipeClient.ExecuteAsync` (2), `Starter.Start` (2: the overloads with a console) and `Starter.StartSequence` (2: the overloads with a console); RS0027 reported nothing.

## Consequences
- Every change of the public surface is a line of diff in a `PublicAPI` file, which the review reads.
- After the publication, the lines move to `PublicAPI.Shipped.txt`. Removing or changing a shipped member then needs a `*REMOVED*` line in `Unshipped` and a new major version.
- A contributor who adds a public member runs the code fix of RS0016 and reads the lines it adds (see [Public API tracking](../advanced/public-api.md)).
- The packages are unchanged: they do not depend on the analyzer and do not contain the `PublicAPI` files.
