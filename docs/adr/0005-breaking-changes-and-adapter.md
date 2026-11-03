# 0005. Breaking Changes and Command Adapter

Status: Accepted
Date: 2026-11-03

## Context
StarterCommand currently inherits from System.CommandLine.Command, coupling user instances to the CLI tree and limiting per-run construction.

## Decision
CmdStarter 2.0 accepts breaking changes. StarterCommand no longer derives from Command; an internal adapter builds the System.CommandLine tree. Document the changes in a migration guide.

## Consequences
Users migrate code that treats StarterCommand as a System.CommandLine node.
