# 0025. Trimming and Native AOT

Status: Accepted
Date: 2026-11-04

## Context

CmdStarter finds command classes, reads attributes, invokes handlers and builds generic types at run time, all through reflection. An application may be published trimmed or as a Native AOT executable. The 2.0 preview was measured for both:

- The static analysis of the library build reports 42 distinct `IL` warnings: IL2026 (5), IL2060 (3), IL2067 (4), IL2070 (7), IL2072 (10), IL2075 (3), IL2091 (1), IL3050 (9).
- The trimmed test applications, HelloWorld and DevOpsTool, publish, but run with exit code -532462766 and a `NoCommandFoundException`, because the command classes are removed.
- The Native AOT test applications publish, once the native toolchain is on the PATH, but run with exit code -1073740791 and a `PlatformNotSupportedException` raised by `Assembly.GetReferencedAssemblies`.

The details and the messages are on the [Trimming](../advanced/trimming.md) and [Native AOT](../advanced/native-aot.md) pages.

## Decision

`IsTrimmable` and `IsAotCompatible` stay absent from the projects of CmdStarter.Lib and CmdStarter.Hosting. Version 2.0 does not support trimming or Native AOT.

## Consequences

- Existing applications are not affected, because neither trimming nor Native AOT is enabled by default.
- Workarounds, such as root files or `DynamicDependency` attributes, were not tried and are not supported.
- The decision would change if a source generator replaced discovery and binding with generated code. That would be a new decision, recorded in its own ADR.
