# 0008. Code Quality Gates

Status: Accepted
Date: 2026-11-03

## Context
Consistent style and compiler/analyzer feedback reduce maintenance defects and keep changes reviewable.

## Decision
Use file-scoped namespaces, TreatWarningsAsErrors, and dotnet format whitespace --verify-no-changes in CI. Keep latest-recommended .NET analyzers on the library. CA1707, CA1716, and CA1822 are active, and `.editorconfig` contains no analyzer exceptions.

## Consequences
Build and formatting failures block integration. CA1715 is enabled; public generic type parameters use the `T` prefix. CA1010 is enabled. Public identifiers follow .NET naming rules, and members that do not use instance state are static where CA1822 requires it.
