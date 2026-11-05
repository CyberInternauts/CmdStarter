# Dependency injection and command instances

This page explains how CmdStarter creates the instances of the commands, how a command gets its services, and how Microsoft Dependency Injection (DI) is set up with `CmdStarterBuilder`, `CmdStarterApp` and `AddCmdStarter`. A command instance is created for each execution, and CmdStarter also creates one description instance when it builds the command tree.

## Ways to create a command instance

CmdStarter creates a command instance by the first of these means that applies:

1. a static `GetInstance<T>()` method declared by the command type;
2. `Starter.ServiceProvider`, the Microsoft DI container;
3. the factory set with `Starter.SetFactory`, one per `Starter`;
4. the parameterless constructor.

A plain command (a class marked `[Command]` that declares no `[Command]` method, with no base class and no interface) has no `GetInstance`: the first means does not apply to it, and the other three do.

So a command that declares `GetInstance` is never taken from the container or the factory, and with a service provider set, the factory is not called. The execution of such a command still has its service scope when a service provider is set: the filters registered by type are created with their services, `CommandContext.Services` is not `null`, and the scope is released at the end of the execution.

A `GetInstance` method is generic with one type parameter, static and public, and it returns a new instance on each call. The following command has no parameterless constructor, so it is created through this method:

<!-- snippet: CmdStarter.Tests.Commands/Loader/ByInterface/ConstructorWithParams.cs -->
```csharp
public static IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
{
    return new ConstructorWithParams(42);
}
```

## Services from `Starter.ServiceProvider`

With `Starter.ServiceProvider` set, a command that is registered in the container is taken from it. Otherwise CmdStarter creates the command with `ActivatorUtilities`, so that the constructor receives its services from the provider. In the DependencyInjection sample, the `Greet` command takes an `IGreetingService` in its constructor:

<!-- snippet: samples/CmdStarter.Samples.DependencyInjection/Commands/Greet.cs -->
```csharp
public sealed class Greet : StarterCommand
{
    private readonly IGreetingService service;

    public Greet(IGreetingService service) => this.service = service;
```

CmdStarter disposes the instances that it creates itself, through `GetInstance`, `Activator` or `ActivatorUtilities`. It does not dispose the instances that a registered service or a factory returns.

## One scope per execution

With Microsoft DI, each execution gets its own scope (`AsyncServiceScope`). The command and its scoped services come from that scope. Scoped services are shared inside one execution and renewed for the next one. The scope is disposed when the execution ends, even when the handler throws. Singleton services live as long as the application, and transient services are renewed at each resolution.

The ScopedServices sample registers three scoped services:

<!-- snippet: samples/CmdStarter.Samples.ScopedServices/Program.cs -->
```csharp
builder.Services.AddScoped<RequestContext>();
builder.Services.AddScoped<AuditLog>();
builder.Services.AddScoped<UnitOfWork>();
```

Its handler receives them as parameters:

<!-- snippet: samples/CmdStarter.Samples.ScopedServices/Commands/WorkCommands.cs -->
```csharp
[Command]
public static int Run(RequestContext context, AuditLog audit, UnitOfWork work)
```

The tests of the sample check that the services of one run share one scope, and that the unit of work is disposed.

## The method model

Each run of a `[Command]` method builds a fresh instance of the declaring class from the scope of that run. A static method has no instance. The constructor of the class can take services, and the parameters of the method can be services (see [Binding](binding.md)). Without a service provider, a static method needs no instance, and an instance method gets one made with its parameterless constructor. The DevOpsTool sample injects a client into the constructor of a method group:

<!-- snippet: samples/CmdStarter.Samples.DevOpsTool/Commands/ServiceCommands.cs -->
```csharp
[Command("service")]
public sealed class ServiceCommands(IDeploymentClient client)
```

## The description instance

When the command tree is built, each class command type is instantiated once, so that CmdStarter can read its metadata: its name, the signature of its handler, and the initial values of its options. This description instance is released at the end of the tree building. Each execution creates a fresh instance and releases it afterwards. The `CommandHandled` event exposes the instance of the execution before it is released.

## Building an application: `CmdStarterBuilder` and `CmdStarterApp`

`CmdStarterBuilder` builds an application that uses Microsoft DI:

- `Services` is the service collection;
- `AddCommandsFromAssembly` says where the commands are;
- `ConfigureStarter` configures the `Starter`;
- `Build()` returns a `CmdStarterApp`, and it can be called once.

The provider that the builder creates validates scopes and the build. The DependencyInjection sample uses the builder like this:

<!-- snippet: samples/CmdStarter.Samples.DependencyInjection/Program.cs -->
```csharp
var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
builder.Services.AddSingleton<IGreetingService, GreetingService>();
await using var app = builder.Build();
return await app.RunAsync(args);
```

`CmdStarterApp` wraps the `Starter` and the service provider. It exposes `Starter`, `Services`, `RunAsync(string[])` and `RunAsync(string)`, and it also has `RunReplAsync`, described in [the REPL guide](../guides/repl.md). Disposing the application disposes the provider and its singletons.

## Registering in an existing `IServiceCollection`

`IServiceCollection.AddCmdStarter(Action<CmdStarterOptions>? configure = null)` is in `CmdStarter.Lib`, so it needs no extra package. It registers a configured `Starter` as a singleton whose `ServiceProvider` is the root provider. The provider is set only when it is not already set. With `UseRepl`, the registered `Starter` is a `ReplStarter`, which can also be resolved as `ReplStarter`.

<!-- snippet: CmdStarter.Tests/ServiceCollectionRegistration.cs -->
```csharp
var services = new ServiceCollection();
services.AddSingleton<ProbeLog>();
services.AddScoped<ScopeProbe>();
services.AddCmdStarter(options =>
```

The `CmdStarter.Hosting` package is described in [the hosting guide](../guides/hosting.md).

## Another container: `SetFactory`

To use another container, set a factory with `Starter.SetFactory`, a function from a `Type` to an `object`. The instance it returns must be of the type asked for, otherwise the execution throws `InvalidOperationException`. The application registers the command types in that container itself. The factory is called once for the tree and once per execution. CmdStarter does not dispose the instances that the factory returns. The SimpleInjector demo uses this form:

<!-- snippet: CmdStarter.Demo.SimpleInjector/Program.cs -->
```csharp
starter.SetFactory(type => (IStarterCommand)container.GetInstance(type));
```

## See also

- Examples: [DependencyInjection](../../samples/CmdStarter.Samples.DependencyInjection), [ScopedServices](../../samples/CmdStarter.Samples.ScopedServices), [DevOpsTool](../../samples/CmdStarter.Samples.DevOpsTool).
- Neighbouring pages: [Commands](commands.md), [Binding](binding.md), [Attributes](attributes.md), [Discovery](discovery.md), [REPL](../guides/repl.md), [Hosting](../guides/hosting.md).
- ADR: [0006, dependency injection](../adr/0006-dependency-injection.md).
