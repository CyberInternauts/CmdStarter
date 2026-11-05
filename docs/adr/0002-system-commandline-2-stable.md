# 0002. System.CommandLine 2.0 Stable

Status: Accepted
Date: 2026-11-03

## Context
CmdStarter needs a reliable parsing and invocation engine. Preview APIs can change and impose instability on consumers.

## Decision
Use stable System.CommandLine 2.0.x; exclude preview releases, including the 3.0 release candidate. CmdStarter does not reimplement parsing, help, or invocation.

## Consequences
Behavior and maintenance remain grounded in the supported upstream engine.
