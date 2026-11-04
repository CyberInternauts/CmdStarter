# 0008. Code Quality Gates

Status: Accepted
Date: 2026-11-03

## Context
Consistent style and compiler/analyzer feedback reduce maintenance defects and keep changes reviewable.

## Decision
Use file-scoped namespaces, TreatWarningsAsErrors, and dotnet format whitespace --verify-no-changes in CI. Keep latest-recommended .NET analyzers on the library. Configure analyzer exceptions in `.editorconfig` when correcting a warning would break the public API.

## Consequences
Build and formatting failures block integration. CA1715 is enabled; public generic type parameters use the `T` prefix. CA1010 is enabled. CA1716 remains disabled because fixing its findings would rename public namespaces under `com.cyberinternauts.csharp.CmdStarter.Lib`. CA1707 and CA1822 remain disabled pending a decision because fixing them would rename public constants or change instance methods to static methods.
