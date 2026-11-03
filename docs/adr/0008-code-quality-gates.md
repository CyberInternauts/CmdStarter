# 0008. Code Quality Gates

Status: Accepted
Date: 2026-11-03

## Context
Consistent style and compiler/analyzer feedback reduce maintenance defects and keep changes reviewable.

## Decision
Use file-scoped namespaces, TreatWarningsAsErrors, and dotnet format whitespace --verify-no-changes in CI. Keep latest-recommended .NET analyzers on the library; disable public API rules in .editorconfig until API review.

## Consequences
Build and formatting failures block integration; API analyzer rules await review.
