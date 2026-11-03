# 0004. Class-Based Command Model

Status: Accepted
Date: 2026-11-03

## Context
CmdStarter maps command classes and their members into a command tree. Users need a predictable model for options, arguments, and parentage.

## Decision
One class represents one command; properties are options and HandlingMethod parameters are arguments. Build the tree from namespaces or [Parent]/[Children]. A method-based [Command] model may complement this later.

## Consequences
The class model remains the primary API while allowing a future additive model.
