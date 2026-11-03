# 0006. Dependency Injection

Status: Proposed
Date: 2026-11-03

## Context
Applications need per-invocation service scopes while CmdStarter should remain usable without a specific container.

## Decision
Keep the container-agnostic IServiceManager and Starter.SetFactory APIs, and add first-class Microsoft.Extensions.DependencyInjection support with one scope per invocation.

## Consequences
This proposal is planned for lot 005 and is not yet accepted implementation.
