using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Parsing;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Helper;
using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.Data;
using com.cyberinternauts.csharp.CmdStarter.Lib.Loader;
using System.Diagnostics.CodeAnalysis;
using com.cyberinternauts.csharp.CmdStarter.Lib.Binding;
using com.cyberinternauts.csharp.CmdStarter.Lib.Reflection;
using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace com.cyberinternauts.csharp.CmdStarter.Lib;

/// <summary>
/// Main class executing the command using the command line arguments
/// </summary>
public class Starter
{
    /// <summary>
    /// Exclusion symbol
    /// </summary>
    public const string EXCLUSION_SYMBOL = "~";

    /// <summary>
    /// Any one character except dots
    /// </summary>
    public const string ANY_CHAR_SYMBOL = "?";

    /// <summary>
    /// Any one character including dots
    /// </summary>
    public const string ANY_CHAR_SYMBOL_INCLUDE_DOTS = "??";

    /// <summary>
    /// Any multi characters except dots
    /// </summary>
    public const string MULTI_ANY_CHAR_SYMBOL = "*";

    /// <summary>
    /// Any multi characters including dots
    /// </summary>
    public const string MULTI_ANY_CHAR_SYMBOL_INCLUDE_DOTS = "**";

    private RootCommand rootCommand = new();
    private readonly Dictionary<Command, CommandRegistration> registrationsByCommand = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Type, CommandRegistration> registrationsByType = new();
    private ImmutableList<string> namespaces = ImmutableList<string>.Empty;
    private ImmutableList<string> classes = ImmutableList<string>.Empty;
    private ImmutableList<Assembly> assemblies = ImmutableList<Assembly>.Empty;
    private ImmutableList<Type> commandsTypes = ImmutableList<Type>.Empty;
    private bool hasToFindCommands = true;
    private bool hasToBuildTree = true;
    private bool hasToInstantiate = true;
    private ClassesBuildingMode classesBuildingMode = ClassesBuildingMode.Both;
    private bool isRootingLonelyCommand = true;
    private Func<Type, IStarterCommand>? factory;

    /// <summary>Raised after a command handler returns, before its instance and services are released. It is not raised when parsing fails or the handler throws.</summary>
    public event EventHandler<CommandHandledEventArgs>? CommandHandled;

    /// <summary>Assemblies searched for commands and global options. An empty list searches all assemblies loaded in the AppDomain.</summary>
    public ImmutableList<Assembly> Assemblies
    {
        get => assemblies;
        set
        {
            ResetCommands();
            assemblies = value ?? ImmutableList<Assembly>.Empty;
        }
    }

    /// <summary>Gets or sets the Microsoft.Extensions.DependencyInjection service provider used to create command instances.</summary>
    /// <remarks>Each execution gets an async scope when the provider exposes <see cref="IServiceScopeFactory"/>, and that scope is disposed when execution ends, including when execution throws. The description instance is resolved in a separate scope disposed after tree construction. Unregistered command types are created with <see cref="ActivatorUtilities"/>. This takes precedence over <see cref="SetFactory(Func{Type, IStarterCommand})"/>; a command type's own <see cref="IStarterCommand.GetInstance{CommandType}"/> takes precedence over both.</remarks>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// Constructor without filters
    /// </summary>
    public Starter() : this(Array.Empty<string>())
    {
    }

    /// <summary>
    /// Constructor with namespace filter
    /// </summary>
    public Starter(string[] namespaces) : this(new List<string>(namespaces))
    {
    }

    /// <summary>
    /// Constructor with namespace filter
    /// </summary>

    public Starter(List<string> namespaces)
    {
        GlobalOptionsManager = new(this);
        Namespaces = Namespaces.AddRange(namespaces);
    }

    /// <summary>
    /// Access to the global options manager
    /// </summary>
    public GlobalOptionsManager GlobalOptionsManager { get; init; }

    /// <summary>
    /// Filter by namespaces (includes child namespaces).<br/><br/>
    /// 
    /// Using "~" in front of a namespace means to exclude this namespace and everything under.
    /// </summary>
    /// <remarks>
    /// An empty list means no filter.<br/><br/>
    /// 
    /// Changing namespaces empty the commands.
    /// </remarks>
    public ImmutableList<string> Namespaces
    {
        get => namespaces;
        set
        {
            ResetCommands();
            namespaces = value ?? ImmutableList<string>.Empty;
        }
    }

    /// <summary>
    /// Filter by classes.<br/><br/>
    /// 
    /// - Wildcard "*" and "?" can be used<br/>
    /// - Wildcard "**" and "??" can also be used<br/>
    /// - Can include partial namespace<br/>
    /// - Using "~" in front of a class means to exclude this class
    /// </summary>
    /// <remarks>
    /// "*" and "?" do not include dots<br/>
    /// "**" and "??" do include dots<br/><br/>
    /// 
    /// An empty list means no filter.<br/><br/>
    /// 
    /// Changing classes empty the commands.
    /// </remarks>
    public ImmutableList<string> Classes
    {
        get => classes;
        set
        {
            ResetCommands();
            classes = value ?? ImmutableList<string>.Empty;
        }
    }

    /// <inheritdoc cref="CommandsTreeBuilder.ClassesBuildingMode"/>
    public ClassesBuildingMode ClassesBuildingMode
    {
        get => classesBuildingMode;

        set
        {
            ResetTrees();
            classesBuildingMode = value;
        }
    }

    /// <summary>
    /// If true and only one command is found, assign the options and arguments to root instead.
    /// </summary>
    public bool IsRootingLonelyCommand
    {
        get => isRootingLonelyCommand;
        set
        {
            ResetTrees();
            isRootingLonelyCommand = value;
        }
    }

    /// <summary>
    /// Commands types that will be used to build the tree
    /// </summary>
    public ImmutableList<Type> CommandsTypes
    {
        get => commandsTypes;
        set
        {
            ResetTrees();
            commandsTypes = value ?? ImmutableList<Type>.Empty;
        }
    }

    /// <summary>
    /// Commands types tree that will be used to create the real commands tree under <see cref="RootCommand"/>
    /// </summary>
    public TreeNode<Type> CommandsTypesTree { get; set; } = new(null);

    /// <summary>
    /// Access to the root command
    /// </summary>
    public RootCommand RootCommand { get => rootCommand; }

    /// <summary>
    /// Set the factory used by this Starter for command types that do not declare their own GetInstance method.
    /// </summary>
    /// <param name="factory"></param>
    public void SetFactory(Func<Type, IStarterCommand> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        this.factory = factory;
    }

    /// <summary>
    /// Restore default command creation for this Starter.
    /// </summary>
    public void SetDefaultFactory() => factory = null;

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command. Each execution creates and releases a new command instance.
    /// A Starter is not intended for concurrent Start calls because it stores parsed global options.
    /// </summary>
    public async Task<int> Start(string[] args)
    {
        InstantiateCommands();

        return await RootCommand.Parse(args).InvokeAsync();
    }

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command. Each execution creates and releases a new command instance.
    /// A Starter is not intended for concurrent Start calls because it stores parsed global options.
    /// </summary>
    public async Task<int> Start(string args)
    {
        InstantiateCommands();

        return await RootCommand.Parse(args).InvokeAsync();
    }

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command. Each execution creates and releases a new command instance.
    /// A Starter is not intended for concurrent Start calls because it stores parsed global options.
    /// </summary>
    public async Task<int> Start(IServiceManager serviceManager, string[] args)
    {
        UseServiceManager(serviceManager);

        return await Start(args);
    }

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command. Each execution creates and releases a new command instance.
    /// A Starter is not intended for concurrent Start calls because it stores parsed global options.
    /// </summary>
    public async Task<int> Start(IServiceManager serviceManager, string args)
    {
        UseServiceManager(serviceManager);

        return await Start(args);
    }

    /// <summary>
    /// Find a command using an <see cref="IStarterCommand"/> type
    /// </summary>
    /// <typeparam name="CommandType">Command type to find</typeparam>
    /// <returns>a <see cref="StarterCommand"/> derived class</returns>
    public Command? FindCommand<CommandType>() where CommandType : IStarterCommand
    {
        return registrationsByType.TryGetValue(typeof(CommandType), out var registration)
            ? registration.CliCommand
            : null;
    }

    /// <summary>
    /// Find the description instance created to read command metadata. It is never executed and is disposed when the tree is built when CmdStarter owns it.
    /// </summary>
    /// <typeparam name="CommandType">Command type to find.</typeparam>
    /// <returns>The description instance, or <see langword="null"/> when no matching command exists.</returns>
    public CommandType? FindCommandInstance<CommandType>() where CommandType : class, IStarterCommand
    {
        return registrationsByType.TryGetValue(typeof(CommandType), out var registration)
            ? registration.DescriptionInstance as CommandType
            : null;
    }

    /// <summary>
    /// Find the command instance associated with a node in the command tree.
    /// </summary>
    /// <param name="command">The System.CommandLine command node.</param>
    /// <returns>The associated command instance, or <see langword="null"/> when the node is not a CmdStarter command.</returns>
    public IStarterCommand? FindCommandInstance(Command command)
    {
        return registrationsByCommand.TryGetValue(command, out var registration)
            ? registration.DescriptionInstance
            : null;
    }

    /// <summary>
    /// Gets the full command path of a command type: its name preceded by all its parents names, excluding the root command.
    /// </summary>
    /// <remarks>Works for every command type, including those that only implement <see cref="IStarterCommand"/>.</remarks>
    /// <typeparam name="CommandType">Command type to look for.</typeparam>
    /// <returns>The command path, or <see langword="null"/> when no matching command exists.</returns>
    public string? GetFullCommandString<CommandType>() where CommandType : IStarterCommand
    {
        var command = FindCommand<CommandType>();
        return command is null ? null : GetCommandPath(command);
    }

    internal static string GetCommandPath(Command command)
    {
        var names = new List<string> { command.Name };
        var currentCommand = command.Parents.FirstOrDefault();
        while (currentCommand is not null)
        {
            names.Add(currentCommand.Name);
            currentCommand = currentCommand.Parents.FirstOrDefault();
        }

        names.Reverse();
        return string.Join(" ", names.Skip(1));
    }

    /// <summary>
    /// Find all <see cref="IStarterCommand "/>s that correspond to filters
    /// </summary>
    /// <exception cref="Exceptions.NoCommandFoundException"></exception>
    public void FindCommandsTypes()
    {
        if (!hasToFindCommands) return; // Quit, job already done
        hasToFindCommands = false;

        var searchAssemblies = Assemblies.IsEmpty ? AppDomain.CurrentDomain.GetAssemblies() : Assemblies.Distinct();
        var commandsTypes = searchAssemblies
                    .SelectMany(a => a.GetTypes()
                        .Where(t =>
                            t.IsClass &&
                            !t.IsAbstract &&
                            t.IsAssignableTo(typeof(IStarterCommand)) &&
                            t.Namespace != null
                        )
                    );

        if (!Assemblies.IsEmpty && !commandsTypes.Any()) throw new NoCommandFoundException(NoCommandFoundException.Filter.Assemblies);

        // Filter by namespaces
        commandsTypes = FilterTypesByNamespaces(commandsTypes, Namespaces.ToList());
        if (!commandsTypes.Any()) throw new NoCommandFoundException(NoCommandFoundException.Filter.Namespaces);

        // Filter by classes
        commandsTypes = FilterTypesByClasses(commandsTypes, Classes.ToList());
        if (!commandsTypes.Any()) throw new NoCommandFoundException(NoCommandFoundException.Filter.Classes);

        this.commandsTypes = CommandsTypes.Clear();
        if (commandsTypes != null)
        {
            var commandsToAdd = commandsTypes.ToArray();
            this.commandsTypes = CommandsTypes.AddRange(commandsToAdd);
        }
    }

    /// <summary>
    /// Build tree from commands types found by <see cref="FindCommandsTypes"/>
    /// </summary>
    public void BuildTree()
    {
        if (!hasToBuildTree) return;
        hasToBuildTree = false;

        FindCommandsTypes();

        var builder = new CommandsTreeBuilder(ClassesBuildingMode, CommandsTypes);
        CommandsTypesTree = builder.BuildTree();
        CommandsTypesTree.SetImmutable();
    }

    /// <summary>
    /// Instantiate commands using the tree built by <see cref="BuildTree" />
    /// </summary>
    public void InstantiateCommands()
    {
        if (!hasToInstantiate) return;
        hasToInstantiate = false;
        var activations = new List<CommandActivation>();
        try
        {
            BuildTree();

            // Lonely command
            if (IsRootingLonelyCommand && CommandsTypes.Count == 1)
            {
                var registration = CreateCommand(CommandsTypes[0], activations);
                registration.CliCommand.Hidden = true;
                InitializeRegistration(registration, RootCommand);
                registration.CliCommand.Hidden = true;
                RootCommand.Add(registration.CliCommand);
                RootCommand.SetAction((parseResult, cancellationToken) => HandleCommandAsync(registration, parseResult));
            }
            else
            {
                AddLevel(RootCommand, CommandsTypesTree, activations);

                // Doing Initialize after having created all because a command may act differently if it has children or not
                VisitCommands(RootCommand, (child) =>
                {
                    if (registrationsByCommand.TryGetValue(child, out var registration))
                    {
                        InitializeRegistration(registration);
                    }
                });
            }

            // Global options
            GlobalOptionsManager.FilterTypes();
            GlobalOptionsManager.LoadOptions(RootCommand);
        }
        finally
        {
            foreach (var activation in activations) activation.Dispose();
        }
    }

    /// <summary>
    /// Execute an action on all commands
    /// </summary>
    /// <param name="action">Action to execute</param>
    public void VisitCommands(Action<Command> action)
    {
        VisitCommands(RootCommand, action);
    }

    /// <summary>
    /// Execution an action on a node and below
    /// </summary>
    /// <param name="currentParent">Starting command</param>
    /// <param name="action">Action to execute</param>
    public void VisitCommands(Command currentParent, Action<Command> action)
    {
        Command? loopBody(Command command)
        {
            action(command);
            return null;
        }
        VisitCommands(currentParent, loopBody);
    }

    private void AddLevel(Command currentParent, TreeNode<Type> currentNode, List<CommandActivation> activations)
    {
        foreach (var childNode in currentNode.Children)
        {
            var registration = CreateCommand(childNode.Value!, activations); // childNode.Value can't be null, because only the root has a null Value
            currentParent.Subcommands.Add(registration.CliCommand);
            AddLevel(registration.CliCommand, childNode, activations);
        }
    }

    private CommandRegistration CreateCommand(Type commandType, List<CommandActivation> activations)
    {
        var activation = Activate(commandType);
        activations.Add(activation);
        var instance = (IStarterCommand)activation.Instance!;
        instance.GlobalOptionsManager = GlobalOptionsManager;

        var name = instance is StarterCommand starterCommand
            ? starterCommand.Name
            : commandType.Name.PascalToKebabCase();
        var cliCommand = new Command(name);
        var registration = new CommandRegistration
        {
            CommandType = commandType,
            DescriptionInstance = instance,
            CliCommand = cliCommand
        };

        registrationsByCommand.Add(cliCommand, registration);
        registrationsByType.Add(commandType, registration);
        if (instance is StarterCommand commandInstance)
        {
            commandInstance.CliCommand = cliCommand;
        }

        return registration;
    }

    private void InitializeRegistration(CommandRegistration registration, Command? receptacle = null)
    {
        var command = registration.CliCommand;
        receptacle ??= command;

        if (command.Subcommands.Count == 0)
        {
            registration.HandlerArguments = Reflection.Loader.LoadArguments(registration.DescriptionInstance.HandlingMethod.Method, receptacle);
            command.SetAction((parseResult, cancellationToken) => HandleCommandAsync(registration, parseResult));
        }

        registration.OptionBindings = Reflection.Loader.LoadOptions(registration.DescriptionInstance.GetType(), receptacle);
        foreach (var binding in registration.OptionBindings)
        {
            binding.InitialValue = binding.Property.GetValue(registration.DescriptionInstance);
        }
        Reflection.Loader.LoadDescription(registration.DescriptionInstance.GetType(), receptacle);
        Reflection.Loader.LoadAliases(registration.DescriptionInstance.GetType(), receptacle.Aliases);
        command.Hidden = Attribute.IsDefined(registration.DescriptionInstance.GetType(), typeof(HiddenAttribute));
    }

    private async Task<int> HandleCommandAsync(CommandRegistration registration, ParseResult parseResult)
    {
        await using var activation = Activate(registration.CommandType);
        var instance = (IStarterCommand)activation.Instance!;
        instance.GlobalOptionsManager = GlobalOptionsManager;
        if (instance is StarterCommand starterCommand) starterCommand.CliCommand = registration.CliCommand;
        GlobalOptionsManager.BindGlobalOptions(parseResult);
        CommandBinder.BindOptions(parseResult, instance, registration.OptionBindings);
        var exitCode = await CommandBinder.InvokeHandlerAsync(parseResult, instance.HandlingMethod, registration.HandlerArguments);
        CommandHandled?.Invoke(this, new CommandHandledEventArgs(registration.CliCommand, instance, exitCode));
        return exitCode;
    }

    private void ResetCommands()
    {
        hasToFindCommands = true;
        this.commandsTypes = commandsTypes.Clear();
        ResetTrees();
    }

    private void ResetTrees()
    {
        hasToBuildTree = true;
        hasToInstantiate = true;
        this.CommandsTypesTree = new TreeNode<Type>(null);
        registrationsByCommand.Clear();
        registrationsByType.Clear();
        rootCommand = new();
    }

    /// <summary>
    /// Visit commands from a specific command until not null is returned
    /// </summary>
    /// <param name="currentParent">Command where to start visiting</param>
    /// <param name="func">Function that is applied a command</param>
    /// <returns>A command if func returned one, otherwise null</returns>
    private Command? VisitCommands(Command currentParent, Func<Command, Command?> func)
    {
        if (currentParent == null) return null;

        foreach (var child in currentParent.Subcommands)
        {
            var funcReturn = func(child);
            if (funcReturn != null) return funcReturn;

            var childChild = VisitCommands(child, func);
            if (childChild != null) return childChild;
        }

        return null;
    }

    private CommandActivation Activate(Type commandType)
    {
        if (Helper.DeclaresGetInstance(commandType))
        {
            var instance = (IStarterCommand)FindGetInstanceMethod(commandType).Invoke(null, null)!;
            return new CommandActivation(instance, ownsInstance: true);
        }

        if (ServiceProvider is not null)
        {
            var scopeFactory = ServiceProvider.GetService<IServiceScopeFactory>();
            if (scopeFactory is null)
            {
                var resolved = ServiceProvider.GetService(commandType);
                return resolved is not null
                    ? new CommandActivation((IStarterCommand)resolved, ownsInstance: false)
                    : new CommandActivation((IStarterCommand)ActivatorUtilities.CreateInstance(ServiceProvider, commandType), ownsInstance: true);
            }

            var scope = scopeFactory.CreateAsyncScope();
            try
            {
                var resolvedInScope = scope.ServiceProvider.GetService(commandType);
                var instance = (IStarterCommand)(resolvedInScope ?? ActivatorUtilities.CreateInstance(scope.ServiceProvider, commandType));
                return new CommandActivation(instance, ownsInstance: resolvedInScope is null, scope);
            }
            catch
            {
                scope.DisposeAsync().AsTask().GetAwaiter().GetResult();
                throw;
            }
        }

        if (factory is not null)
        {
            var instance = factory(commandType);
            return instance is null
                ? throw new InvalidOperationException($"The command factory returned no instance for {commandType.FullName}.")
                : new CommandActivation(instance, ownsInstance: false);
        }

        return new CommandActivation((IStarterCommand)Activator.CreateInstance(commandType)!, ownsInstance: true);
    }

    private void UseServiceManager(IServiceManager serviceManager)
    {
        // Set factory
        var creationMethod = (Type commandType) =>
        {
            return serviceManager.GetService(commandType) as IStarterCommand;
        };
        SetFactory(creationMethod);

        // Register commands
        FindCommandsTypes();
        CommandsTypes.ForEach(type => serviceManager.SetService(type));
    }

}
