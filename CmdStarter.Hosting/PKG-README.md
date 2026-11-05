# CmdStarter.Hosting

CmdStarter.Hosting adds Generic Host integration to the `cints.CmdStarter` package: it registers CmdStarter with the services of the host, so commands can use the host's configuration, options and logging. It runs the requested command, or the REPL, between the start and the stop of the host's hosted services.

<!-- snippet: samples/CmdStarter.Samples.Hosting/Program.cs -->
```csharp
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });
// ...
builder.AddCmdStarter(options => options.AddCommandsFromAssembly(typeof(Program).Assembly));
using var host = builder.Build();
return await host.RunCmdStarterAsync(args);
```

- [Hosting guide](https://github.com/CyberInternauts/CmdStarter/blob/master/docs/guides/hosting.md)
- [Documentation index](https://github.com/CyberInternauts/CmdStarter/blob/master/docs/index.md)
