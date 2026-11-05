# 0004. Class-Based Command Model

Status: Accepted
Date: 2026-11-03

## Context
CmdStarter maps command classes and their members into a command tree. Users need a predictable model for options, arguments, and parentage.

## Decision
One class represents one command; properties are options and HandlingMethod parameters are arguments. Build the tree from namespaces or [Parent]/[Children]. A method-based [Command] model may complement this later.

## Consequences
The class model remains supported alongside the additive [method-based model](0009-method-based-command-model.md).

Amended on 2026-11-05 (lot 037): a class that implements no interface of CmdStarter can be a command of the class model too, a plain command: it carries `[Command]` and declares no `[Command]` method, its public read/write properties are its options and its handler is a method of the class. See [ADR 0032](0032-plain-class-commands.md).
