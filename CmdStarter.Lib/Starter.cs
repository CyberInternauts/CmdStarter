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
    private ImmutableList<Type> commandsTypes = ImmutableList<Type>.Empty;
    private bool hasToFindCommands = true;
    private bool hasToBuildTree = true;
    private bool hasToInstantiate = true;
    private ClassesBuildingMode classesBuildingMode = ClassesBuildingMode.Both;
    private bool isRootingLonelyCommand = true;

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
    /// Change the factory used by <see cref="IStarterCommand.GetInstance{CommandType}"/> default implementation
    /// </summary>
    /// <param name="factory"></param>
    public static void SetFactory(Func<Type, IStarterCommand> factory) => FactoryBag.SetFactory(factory);

    /// <summary>
    /// Set back to default implementation the factory used by <see cref="IStarterCommand.GetInstance{CommandType}"/>
    /// </summary>
    public static void SetDefaultFactory() => FactoryBag.SetDefaultFactory();

    /// <summary>
    /// Find all classes implementing <see cref="IStarterCommand"/>, build a tree based on their namespaces and try to execute a command
    /// </summary>
    public async Task<int> Start(string[] args)
    {
        InstantiateCommands();

        return await RootCommand.Parse(args).InvokeAsync();
    }

    /// <summary>
    /// Find all classes implementing <see cref="IStarterCommand"/>, build a tree based on their namespaces and try to execute a command
    /// </summary>
    public async Task<int> Start(string args)
    {
        InstantiateCommands();

        return await RootCommand.Parse(args).InvokeAsync();
    }

    /// <summary>
    /// Find all classes implementing <see cref="IStarterCommand"/>, build a tree based on their namespaces and try to execute a command
    /// </summary>
    public async Task<int> Start(IServiceManager serviceManager, string[] args)
    {
        SetFactory(serviceManager);

        return await Start(args);
    }

    /// <summary>
    /// Find all classes implementing <see cref="IStarterCommand"/>, build a tree based on their namespaces and try to execute a command
    /// </summary>
    public async Task<int> Start(IServiceManager serviceManager, string args)
    {
        SetFactory(serviceManager);

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
    /// Find the command instance used to handle executions.
    /// </summary>
    /// <typeparam name="CommandType">Command type to find.</typeparam>
    /// <returns>The command instance, or <see langword="null"/> when no matching command exists.</returns>
    public CommandType? FindCommandInstance<CommandType>() where CommandType : class, IStarterCommand
    {
        return registrationsByType.TryGetValue(typeof(CommandType), out var registration)
            ? registration.Instance as CommandType
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
            ? registration.Instance
            : null;
    }

    /// <summary>
    /// Find all <see cref="IStarterCommand "/>s that correspond to filters
    /// </summary>
    /// <exception cref="Exceptions.NoCommandFoundException"></exception>
    public void FindCommandsTypes()
    {
        if (!hasToFindCommands) return; // Quit, job already done
        hasToFindCommands = false;

        var commandsTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes()
                        .Where(t =>
                            t.IsClass &&
                            !t.IsAbstract &&
                            t.IsAssignableTo(typeof(IStarterCommand)) &&
                            t.Namespace != null
                        )
                    );

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

        BuildTree();

        // Lonely command
        if (IsRootingLonelyCommand && CommandsTypes.Count == 1)
        {
            var registration = CreateCommand(CommandsTypes[0]);
            registration.CliCommand.Hidden = true;
            InitializeRegistration(registration, RootCommand);
            registration.CliCommand.Hidden = true;
            RootCommand.Add(registration.CliCommand);
            RootCommand.SetAction((parseResult, cancellationToken) => HandleCommandAsync(registration, parseResult));
        }
        else
        {
            AddLevel(RootCommand, CommandsTypesTree);

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

    private void AddLevel(Command currentParent, TreeNode<Type> currentNode)
    {
        foreach (var childNode in currentNode.Children)
        {
            var registration = CreateCommand(childNode.Value!); // childNode.Value can't be null, because only the root has a null Value
            currentParent.Subcommands.Add(registration.CliCommand);
            AddLevel(registration.CliCommand, childNode);
        }
    }

    private CommandRegistration CreateCommand(Type commandType)
    {
        var getInstanceMethod = FindGetInstanceMethod(commandType);
        var instance = (IStarterCommand)getInstanceMethod.Invoke(null, null)!;
        instance.GlobalOptionsManager = GlobalOptionsManager;

        var name = instance is StarterCommand starterCommand
            ? starterCommand.Name
            : commandType.Name.PascalToKebabCase();
        var cliCommand = new Command(name);
        var registration = new CommandRegistration
        {
            CommandType = commandType,
            Instance = instance,
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
            registration.HandlerArguments = Reflection.Loader.LoadArguments(registration.Instance.HandlingMethod.Method, receptacle);
            command.SetAction((parseResult, cancellationToken) => HandleCommandAsync(registration, parseResult));
        }

        registration.OptionBindings = Reflection.Loader.LoadOptions(registration.Instance.GetType(), receptacle);
        foreach (var binding in registration.OptionBindings)
        {
            binding.InitialValue = binding.Property.GetValue(registration.Instance);
        }
        Reflection.Loader.LoadDescription(registration.Instance.GetType(), receptacle);
        Reflection.Loader.LoadAliases(registration.Instance.GetType(), receptacle.Aliases);
        command.Hidden = Attribute.IsDefined(registration.Instance.GetType(), typeof(HiddenAttribute));
    }

    private async Task<int> HandleCommandAsync(CommandRegistration registration, ParseResult parseResult)
    {
        GlobalOptionsManager.BindGlobalOptions(parseResult);
        CommandBinder.BindOptions(parseResult, registration.Instance, registration.OptionBindings);
        return await CommandBinder.InvokeHandlerAsync(parseResult, registration.Instance.HandlingMethod, registration.HandlerArguments);
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

    private void SetFactory(IServiceManager serviceManager)
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
