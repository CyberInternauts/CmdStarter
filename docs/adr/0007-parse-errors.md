# 0007. Parse Errors

Status: Accepted
Date: 2026-11-03

## Context
Invalid command-line input must be reported consistently and produce a failure status.

## Decision
System.CommandLine reports parse errors with a non-zero exit code. Starter.Start does not throw for parse errors.

## Consequences
Callers can rely on exit status to detect invalid input.
