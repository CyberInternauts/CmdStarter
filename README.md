# CmdStarter

This library is a layer over [System.CommandLine](https://github.com/dotnet/command-line-api) to ease integration 
into existing projects. Currently, this dependency is still in beta version, hence this library's version will stay 
in beta too.

## Links

- Article: https://www.codeproject.com/Articles/5361921/Integrate-System-CommandLine-Easily-with-CmdStarte
- Nuget package: https://www.nuget.org/packages/cints.CmdStarter
- Jira: https://cyberinternauts.atlassian.net/browse/CMD

## Features
- Implement commands using an abstract class or an interface
- Filter classes to use in current execution by namespaces or by full class names
- Classes using dependency injection are supported
- Mark classes as global options container
- Easy access to the global options inside the executing method
- Lonely command can be rooted
- Autowiring properties to System.CommandLine command options
- Autowiring executing method parameters to System.CommandLine command arguments
- Alias, Hidden, Description and AutoComplete attributes are offered to set command options/arguments properties
- Automatic commands tree loading via namespaces or Parent|Children attributes

## Usage

- Import the [nuget package **cints.CmdStarter**](https://www.nuget.org/packages/cints.CmdStarter). 
  > Ensure to check *Prerelease* checkbox
- Command integration (Choose one):
  - Create a new class inheriting from `StarterCommand`.
  - Add `IStarterCommand` interface to an existing class having a constructor without parameter.
     > For dependency injection, see below.
- Create the Program class below.

```
internal class Program
{
    public static async Task Main(string[] args)
    {
        var starter = new CmdStarter.Lib.Starter();
        await starter.Start(args);
    }
}
```

### Dependency injection

Commands are created in this priority order: a command type's own `GetInstance<T>()`, `Starter.ServiceProvider`, `Starter.SetFactory`, then a parameterless constructor. The `IServiceManager` overload remains available for other containers. Microsoft DI users can get one scope per execution with `CmdStarterBuilder`:

```csharp
await using var app = new CmdStarterBuilder()
    .AddCommandsFromAssembly(typeof(MyCommand).Assembly)
    .ConfigureStarter(starter => starter.Namespaces = starter.Namespaces.Add("MyApp.Commands"))
    .Build();
int exitCode = await app.RunAsync(args);
```

See the [1.x to 2.0 migration guide](docs/migration/1.x-to-2.0.md) for instance lifetime, disposal, and assembly filtering details.

## Participation | Submit issues, ideas

The project uses a free open source licence of Jira to manage its development.

https://cyberinternauts.atlassian.net/browse/CMD

If you want to participate, there are two options:
- Fork this repository and submit a pull request
- Ask to join the team

## License

MIT License. See [LICENSE.txt](https://github.com/CyberInternauts/CmdStarter/blob/master/LICENSE.txt)

## Collaborators

- [Jonathan Boivin](https://github.com/djon2003): Project leader, main developer, reviewer
- [Norbert Ormándi](https://github.com/DeszkaCodes): Developer, reviewer. A special thank to him to have believed in this project!
