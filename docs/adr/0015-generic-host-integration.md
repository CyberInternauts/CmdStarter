# 0015. Generic Host Integration

Status: Accepted
Date: 2026-11-03

## Context

Modern .NET command line applications often use Generic Host for configuration, logging, options, dependency injection, and hosted services. CmdStarter should integrate with that composition model without making its core package depend on Hosting.

## Decision

Generic Host integration is provided by the separate `CmdStarter.Hosting` package. The core package remains independent of `Microsoft.Extensions.Hosting` and exposes `IServiceCollection.AddCmdStarter` for registration with Microsoft DI.

The Hosting package adds `IHostApplicationBuilder.AddCmdStarter`, which registers the starter and replaces the host lifetime. Applications run commands through `IHost.RunCmdStarterAsync`.

`RunCmdStarterAsync` starts the host so hosted services can run, executes the command or REPL, and stops the host afterward. It links the caller's cancellation token with `IHostApplicationLifetime.ApplicationStopping` for command execution.

The package replaces the default `ConsoleLifetime`. That lifetime intercepts Ctrl+C to stop the whole host and writes “Application started…” to standard output. A CLI command handles Ctrl+C through System.CommandLine, while `ReplStarter` handles it for a REPL, so the host lifetime must not take over console cancellation or emit its startup status.

Applications that may be launched from any working directory should set `ContentRootPath = AppContext.BaseDirectory` when creating the host builder. This lets configuration files such as `appsettings.json` load from beside the executable.

## Consequences

Applications can adopt Generic Host configuration, logging, options, and hosted services while using CmdStarter. Applications that need only Microsoft DI can register CmdStarter through `IServiceCollection.AddCmdStarter` without referencing the Hosting package.
