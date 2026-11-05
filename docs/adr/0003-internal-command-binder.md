# 0003. Internal Command Binder

Status: Accepted
Date: 2026-11-03

## Context
The System.CommandLine.NamingConventionBinder package never shipped a stable version. CmdStarter needs explicit binding that fits its command model.

## Decision
Use the internal binder under CmdStarter.Lib/Binding/. Bind options to properties and arguments to HandlingMethod parameters.

## Consequences
Binding behavior stays under CmdStarter control without a preview dependency.
