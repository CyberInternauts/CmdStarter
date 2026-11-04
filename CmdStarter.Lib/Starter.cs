using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Parsing;
using static CyberInternauts.CmdStarter.Reflection.Helper;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Loader;
using System.Diagnostics.CodeAnalysis;
using CyberInternauts.CmdStarter.Binding;
using CyberInternauts.CmdStarter.Catalog;
using CyberInternauts.CmdStarter.Reflection;
using CyberInternauts.CmdStarter.Extensions;
using CyberInternauts.CmdStarter.Attributes;
using System.Reflection;
using System.Runtime.CompilerServices;
using CyberInternauts.CmdStarter.Sequences;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter;

/// <summary>
/// Main class executing the command using the command line arguments
/// </summary>
public class Starter
{
    /// <summary>
    /// Exclusion symbol
    /// </summary>
    public const string ExclusionSymbol = "~";

    /// <summary>
    /// Any one character except dots
    /// </summary>
    public const string AnyCharSymbol = "?";

    /// <summary>
    /// Any one character including dots
    /// </summary>
    public const string AnyCharSymbolIncludeDots = "??";

    /// <summary>
    /// Any multi characters except dots
    /// </summary>
    public const string MultiAnyCharSymbol = "*";

    /// <summary>
    /// Any multi characters including dots
    /// </summary>
    public const string MultiAnyCharSymbolIncludeDots = "**";

    private RootCommand rootCommand = new();
    private readonly Dictionary<Command, CommandRegistration> registrationsByCommand = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Type, CommandRegistration> registrationsByType = new();
    private ImmutableList<string> namespaces = ImmutableList<string>.Empty;
    private ImmutableList<string> classes = ImmutableList<string>.Empty;
    private ImmutableList<Assembly> assemblies = ImmutableList<Assembly>.Empty;
    private bool useCommandCatalogs = true;
    private ImmutableList<Type> commandsTypes = ImmutableList<Type>.Empty;
    private ImmutableList<Type> methodCommandsTypes = ImmutableList<Type>.Empty;
    private readonly List<MethodCommandRegistration> methodRegistrations = new();
    private readonly List<FilterRegistration> filters = new();
    private readonly Dictionary<Command, CommandStateMetadata> commandStateMetadata = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Command> implicitMethodGroups = new(ReferenceEqualityComparer.Instance);
    private TreeNode<Type>? definedTree;
    private CommandTreeDefinition.TreeState? definedTreeState;
    private Dictionary<Type, List<CommandTreeDefinition.GroupSegment>> groupPaths = new();
    private bool hasToFindCommands = true;
    private bool hasToBuildTree = true;
    private bool hasToInstantiate = true;
    private ClassesBuildingMode classesBuildingMode = ClassesBuildingMode.Both;
    private bool groupsByNamespace;
    private bool isRootingLonelyCommand = true;
    private string? sequenceOption;
    private Func<Type, IStarterCommand>? factory;
    private readonly ConditionalWeakTable<InvocationConfiguration, CommandConsole> consoles = new();

    /// <summary>Raised after a command handler returns, before its instance and services are released. It is not raised when parsing fails or the handler throws.</summary>
    public event EventHandler<CommandHandledEventArgs>? CommandHandled;

    internal IReadOnlyList<Type> FilterTypes => filters.Where(filter => filter.FilterType is not null).Select(filter => filter.FilterType!).ToArray();

    /// <summary>Adds a command filter instance.</summary>
    /// <param name="filter">The filter instance to use for every command execution.</param>
    /// <returns>This starter.</returns>
    public Starter AddFilter(ICommandFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        filters.Add(new FilterRegistration(filter, null));
        return this;
    }

    /// <summary>Adds a command filter type, activated for each command execution.</summary>
    /// <typeparam name="TFilter">The filter type.</typeparam>
    /// <returns>This starter.</returns>
    public Starter AddFilter<TFilter>() where TFilter : class, ICommandFilter
    {
        filters.Add(new FilterRegistration(null, typeof(TFilter)));
        return this;
    }

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

    /// <summary>
    /// Gets or sets whether an assembly that has a <see cref="Catalog.CommandCatalogAttribute"/> is read from its catalog, which the generator of the CmdStarter package writes
    /// at compile time, instead of being scanned by reflection. The default is <see langword="true"/>. When it is <see langword="false"/>, every assembly is scanned.
    /// An assembly without a catalog is always scanned.
    /// </summary>
    /// <remarks>
    /// Changing it resets the discovery. The global options containers are found once for the process, by the first <see cref="Starter"/> that builds its tree,
    /// and that <see cref="Starter"/> setting decides how: a later change does not find them again.
    /// </remarks>
    public bool UseCommandCatalogs
    {
        get => useCommandCatalogs;
        set
        {
            ResetCommands();
            useCommandCatalogs = value;
        }
    }

    // False when the runtime cannot generate code (Native AOT). Then only the catalogs are read: no assembly is scanned and GetReferencedAssemblies is never called, because the runtime does not support it,
    // and the methods of a class keep the order that GetMethods gives, because MetadataToken does not exist there. Tests set it.
    internal bool DynamicCodeSupported { get; init; } = RuntimeFeature.IsDynamicCodeSupported;

    /// <summary>Gets or sets the Microsoft.Extensions.DependencyInjection service provider used to create command instances.</summary>
    /// <remarks>Each execution gets an async scope when the provider exposes <see cref="IServiceScopeFactory"/>, and that scope is disposed when execution ends, including when execution throws. The description instance is resolved in a separate scope disposed after tree construction. Unregistered command types are created with <see cref="ActivatorUtilities"/>. This takes precedence over <see cref="SetFactory(Func{Type, IStarterCommand})"/>; a command type's own <see cref="IStarterCommand.GetInstance{TCommand}"/> takes precedence over both.</remarks>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <summary>Gets or sets whether unhandled command exceptions include their full details.</summary>
    /// <remarks>When <see langword="false"/>, an exception from command activation, binding, handling, or release writes only its message on one line to <c>parseResult.InvocationConfiguration.Error</c> and returns exit code 1. When <see langword="true"/>, System.CommandLine's default handler writes the full exception and returns exit code 1.</remarks>
    public bool ShowExceptionDetails { get; set; }

    /// <summary>Gets or sets the console of every execution that is started without an explicit console. <see langword="null"/> means the console of the process, read when each command executes.</summary>
    public CommandConsole? DefaultConsole { get; set; }

    /// <summary>Gets or sets the application state provider. When unset, CmdStarter resolves an <see cref="ICommandStateProvider"/> from <see cref="ServiceProvider"/>.</summary>
    /// <remarks>When registered with dependency injection, the state provider should be a singleton so state changes remain visible across command scopes.</remarks>
    public ICommandStateProvider? StateProvider { get; set; }

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

    /// <summary>Gets or sets whether root commands are placed in groups derived from their namespaces, such as <c>App.Commands.Users.List</c> becoming <c>users list</c>.</summary>
    /// <remarks>This applies only when no fluent tree is defined and class building mode is not <see cref="ClassesBuildingMode.OnlyAttributes"/>.</remarks>
    public bool GroupsByNamespace
    {
        get => groupsByNamespace;
        set
        {
            ResetTrees();
            groupsByNamespace = value;
        }
    }

    /// <summary>
    /// Gets or sets the option that carries a whole sequence of commands (for example <c>-c</c>), or <see langword="null"/> (the default) for none.
    /// When set, <c>Start</c> with exactly two arguments, the first one equal to this value, runs the second one with <see cref="StartSequence(string, CancellationToken)"/>:
    /// <c>app -c "login; list"</c>. Building the commands throws <see cref="InvalidAttributeException"/> when this value is also the name or an alias of a root option or of a first-level command.
    /// </summary>
    public string? SequenceOption
    {
        get => sequenceOption;
        set
        {
            ResetTrees();
            sequenceOption = value;
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
    /// Class commands types that will be used to build the tree.
    /// </summary>
    /// <remarks>
    /// Every type must be a non-abstract class that implements <see cref="IStarterCommand"/>, has a namespace, and is listed once:
    /// otherwise an <see cref="ArgumentException"/> that names the type is thrown and nothing changes. <see langword="null"/> means an empty list.<br/><br/>
    /// Setting a list resets the tree. Before the first discovery, a non-empty list stands for the discovery of the class commands:
    /// the discovery does not run, so there are no method commands. After the discovery, only the class commands change and the discovered method commands stay.
    /// An empty list goes back to the discovery. Changing <see cref="Namespaces"/>, <see cref="Classes"/> or <see cref="Assemblies"/> afterwards runs the discovery again.
    /// </remarks>
    /// <exception cref="ArgumentException">A type is <see langword="null"/>, is not a non-abstract class implementing <see cref="IStarterCommand"/>, has no namespace or is listed twice.</exception>
    public ImmutableList<Type> CommandsTypes
    {
        get => commandsTypes;
        set
        {
            value ??= ImmutableList<Type>.Empty;
            ValidateCommandsTypes(value, nameof(value));

            if (value.IsEmpty)
            {
                ResetCommands();
                return;
            }

            ResetTrees();
            commandsTypes = value;
            hasToFindCommands = false;
        }
    }

    // paramName is the parameter of the setter, so the exception names what the caller passed
    private static void ValidateCommandsTypes(ImmutableList<Type> types, string paramName)
    {
        var seen = new HashSet<Type>();
        foreach (var type in types)
        {
            if (type is null)
            {
                throw new ArgumentException("A command type cannot be null.", paramName);
            }

            if (!type.IsClass || type.IsAbstract || !type.IsAssignableTo(typeof(IStarterCommand)))
            {
                throw new ArgumentException($"Type '{type.FullName}' is not a non-abstract class implementing IStarterCommand.", paramName);
            }

            if (type.Namespace is null)
            {
                throw new ArgumentException($"Type '{type.FullName}' has no namespace.", paramName);
            }

            if (!seen.Add(type))
            {
                throw new ArgumentException($"Command type '{type.FullName}' is listed twice.", paramName);
            }
        }
    }

    /// <summary>Gets the method-model command classes that pass the configured filters.</summary>
    public ImmutableList<Type> MethodCommandsTypes { get => methodCommandsTypes; private set => methodCommandsTypes = value; }

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

    /// <summary>Defines and replaces the placement of command classes with an explicit fluent tree.</summary>
    /// <remarks>For command classes in this definition, discovery and placement filters and attributes do not apply: <see cref="Namespaces"/>, <see cref="Classes"/>, <see cref="Assemblies"/>, <see cref="ClassesBuildingMode"/>, <c>[Parent]</c> and <c>[Children]</c> are ignored. Method commands and global options continue to be discovered and filtered as before.</remarks>
    /// <param name="define">Callback that defines the command tree.</param>
    /// <returns>This starter.</returns>
    public Starter UseTree(Action<CommandTreeDefinition> define)
    {
        ArgumentNullException.ThrowIfNull(define);
        var root = new TreeNode<Type>(null);
        var definition = new CommandTreeDefinition(root);
        try
        {
            define(definition);
        }
        finally
        {
            definition.Close();
        }

        root.SetImmutable();
        definedTree = root;
        definedTreeState = definition.ExportState();
        ResetCommands();
        return this;
    }

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command. Each execution creates and releases a new command instance.
    /// A Starter is not intended for concurrent Start calls because it stores parsed global options.
    /// </summary>
    public async Task<int> Start(string[] args)
    {
        return await Start(args, CancellationToken.None);
    }

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command with an invocation cancellation token.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    public Task<int> Start(string[] args, CancellationToken cancellationToken)
    {
        if (IsSequenceInvocation(args))
        {
            return StartSequence(args[1], cancellationToken);
        }

        return StartCoreAsync(root => root.Parse(args), null, cancellationToken);
    }

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command with the given console for this execution only.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="console">The input, output and error of this execution: handlers, help, parse errors and exception messages use it instead of <see cref="DefaultConsole"/>.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="console"/> is null.</exception>
    public Task<int> Start(string[] args, CommandConsole console, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(console);
        if (IsSequenceInvocation(args))
        {
            return StartSequence(args[1], console, cancellationToken);
        }

        return StartCoreAsync(root => root.Parse(args), console, cancellationToken);
    }

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command. Each execution creates and releases a new command instance.
    /// A Starter is not intended for concurrent Start calls because it stores parsed global options.
    /// </summary>
    public async Task<int> Start(string args)
    {
        return await Start(args, CancellationToken.None);
    }

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command with an invocation cancellation token.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    public Task<int> Start(string args, CancellationToken cancellationToken)
    {
        return StartCoreAsync(root => root.Parse(args), null, cancellationToken);
    }

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command with the given console for this execution only.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="console">The input, output and error of this execution: handlers, help, parse errors and exception messages use it instead of <see cref="DefaultConsole"/>.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="console"/> is null.</exception>
    public Task<int> Start(string args, CommandConsole console, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(console);
        return StartCoreAsync(root => root.Parse(args), console, cancellationToken);
    }

    /// <summary>
    /// Runs a line of commands joined by <c>;</c> (always run the next), <c>&amp;&amp;</c> (run the next if the last executed code is 0) and <c>||</c>
    /// (run the next if it is not), from left to right, with the state of the application kept from one command to the next.
    /// Operators only count outside double quotes. A cancelled command (code 130) stops the sequence.
    /// A line without operator is the same as <see cref="Start(string, CancellationToken)"/>, even when it is not a valid sequence (an unclosed quote).
    /// </summary>
    /// <param name="commandLine">The line to run.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <returns>The exit code of the last executed command; 1 (with the reason written to the error output of <see cref="DefaultConsole"/> or of the process) when the line is not a valid sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="commandLine"/> is null.</exception>
    public Task<int> StartSequence(string commandLine, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        return RunSequenceAsync(commandLine, null, (command, commandConsole) => commandConsole is null ? Start(command, cancellationToken) : Start(command, commandConsole, cancellationToken));
    }

    /// <summary>
    /// Runs a sequence of commands (see <see cref="StartSequence(string, CancellationToken)"/>) with the given console for every command of the sequence.
    /// </summary>
    /// <param name="commandLine">The line to run.</param>
    /// <param name="console">The input, output and error of this execution, also used to report an invalid line.</param>
    /// <param name="cancellationToken">Token used to cancel command execution.</param>
    /// <returns>The exit code of the last executed command; 1 when the line is not a valid sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="commandLine"/> or <paramref name="console"/> is null.</exception>
    public Task<int> StartSequence(string commandLine, CommandConsole console, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentNullException.ThrowIfNull(console);
        // commandConsole is never null: RunSequenceAsync and RunPipelineAsync pass the console of this overload
        return RunSequenceAsync(commandLine, console, (command, commandConsole) => Start(command, commandConsole!, cancellationToken));
    }

    // True when the sequence option is set and the arguments are exactly that option followed by the line to run.
    private protected bool IsSequenceInvocation(string[]? args) =>
        SequenceOption is not null && args is { Length: 2 } && string.Equals(args[0], SequenceOption, StringComparison.Ordinal);

    // The semantics of a sequence live in CommandSequence.ExecuteAsync and those of a pipeline in RunPipelineAsync;
    // runCommand runs one command with the console it is given (null: the console of the process or DefaultConsole, as a plain Start).
    private protected async Task<int> RunSequenceAsync(string commandLine, CommandConsole? console, Func<string, CommandConsole?, Task<int>> runCommand)
    {
        if (!CommandSequence.TryParse(commandLine, out var sequence, out var error))
        {
            // A line without operator is a plain command line, even with an unclosed quote: it runs as a Start would.
            if (!CommandSequence.HasOperator(commandLine))
            {
                return await runCommand(commandLine, console);
            }

            var errorOutput = (console ?? DefaultConsole)?.Error ?? Console.Error;
            errorOutput.WriteLine(error);
            return 1;
        }

        if (sequence.Steps.Count == 0)
        {
            return await runCommand(commandLine, console);
        }

        return await sequence.ExecuteAsync(step => RunPipelineAsync(step.Pipeline, console, async (command, commandConsole) => await runCommand(command, commandConsole)));
    }

    // Runs the commands of a step. A single command runs with the console as given (a plain Start, nothing forced).
    // Several commands are buffered: the first reads the input of the execution console, each command output becomes the input of the next,
    // the last writes to the output of the execution console, and every command writes its errors to the error output of the execution console.
    // Returns the code of the last command, 130 as soon as a command is cancelled, or null when a command asks to stop the sequence.
    private protected async Task<int?> RunPipelineAsync(IReadOnlyList<string> pipeline, CommandConsole? console, Func<string, CommandConsole?, Task<int?>> runCommand)
    {
        if (pipeline.Count == 1)
        {
            return await runCommand(pipeline[0], console);
        }

        var execution = console ?? DefaultConsole ?? CommandConsole.Standard;
        var input = execution.In;
        int? code = 0;
        for (var index = 0; index < pipeline.Count; index++)
        {
            using var buffer = index == pipeline.Count - 1 ? null : new StringWriter();
            code = await runCommand(pipeline[index], new CommandConsole(input, buffer ?? execution.Out, execution.Error));
            if (code is null or 130)
            {
                return code;
            }

            if (buffer is not null)
            {
                input = new StringReader(buffer.ToString());
            }
        }

        return code;
    }

    // The single implementation of every Start. The console of the execution is attached to the invocation configuration itself,
    // so concurrent executions and nested Start calls on the same Starter each keep their own console.
    private async Task<int> StartCoreAsync(Func<RootCommand, ParseResult> parse, CommandConsole? console, CancellationToken cancellationToken)
    {
        await InstantiateCommandsAsync();
        UpdateCommandAvailability();

        var parseResult = parse(RootCommand);
        console ??= DefaultConsole;
        var configuration = CreateInvocationConfiguration();
        if (console is not null)
        {
            configuration ??= new InvocationConfiguration();
            configuration.Output = console.Out;
            configuration.Error = console.Error;
            consoles.AddOrUpdate(configuration, console);
        }

        return await parseResult.InvokeAsync(configuration, cancellationToken);
    }

    /// <summary>Creates the System.CommandLine invocation configuration.</summary>
    /// <returns>The configuration, or <see langword="null"/> to keep System.CommandLine defaults. A non-null configuration must be a new instance at each call: a console given to <c>Start</c> is attached to it.</returns>
    protected virtual InvocationConfiguration? CreateInvocationConfiguration() => null;

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command. Each execution creates and releases a new command instance.
    /// A Starter is not intended for concurrent Start calls because it stores parsed global options.
    /// </summary>
    [Obsolete("IServiceManager is obsolete: set Starter.ServiceProvider or use CmdStarterBuilder (Microsoft.Extensions.DependencyInjection), or call Starter.SetFactory for another container.")]
    public async Task<int> Start(IServiceManager serviceManager, string[] args)
    {
        UseServiceManager(serviceManager);

        return await Start(args);
    }

    /// <summary>
    /// Find matching commands, build the tree, and execute the requested command. Each execution creates and releases a new command instance.
    /// A Starter is not intended for concurrent Start calls because it stores parsed global options.
    /// </summary>
    [Obsolete("IServiceManager is obsolete: set Starter.ServiceProvider or use CmdStarterBuilder (Microsoft.Extensions.DependencyInjection), or call Starter.SetFactory for another container.")]
    public async Task<int> Start(IServiceManager serviceManager, string args)
    {
        UseServiceManager(serviceManager);

        return await Start(args);
    }

    /// <summary>
    /// Find a command using an <see cref="IStarterCommand"/> type
    /// </summary>
    /// <typeparam name="TCommand">Command type to find</typeparam>
    /// <returns>a <see cref="StarterCommand"/> derived class</returns>
    public Command? FindCommand<TCommand>() where TCommand : IStarterCommand
    {
        return registrationsByType.TryGetValue(typeof(TCommand), out var registration)
            ? registration.CliCommand
            : null;
    }

    /// <summary>
    /// Find the description instance created to read command metadata. It is never executed and is disposed when the tree is built when CmdStarter owns it.
    /// </summary>
    /// <typeparam name="TCommand">Command type to find.</typeparam>
    /// <returns>The description instance, or <see langword="null"/> when no matching command exists.</returns>
    public TCommand? FindCommandInstance<TCommand>() where TCommand : class, IStarterCommand
    {
        return registrationsByType.TryGetValue(typeof(TCommand), out var registration)
            ? registration.DescriptionInstance as TCommand
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
    /// <typeparam name="TCommand">Command type to look for.</typeparam>
    /// <returns>The command path, or <see langword="null"/> when no matching command exists.</returns>
    public string? GetFullCommandString<TCommand>() where TCommand : IStarterCommand
    {
        var command = FindCommand<TCommand>();
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

        try
        {
            DiscoverCommandsTypes();
        }
        catch
        {
            ResetCommands(); // A failed discovery leaves nothing half done: the next call runs it again
            throw;
        }
    }

    private void DiscoverCommandsTypes()
    {
        var searchAssemblies = Assemblies.IsEmpty ? AppDomain.CurrentDomain.GetAssemblies() : Assemblies.Distinct();

        // An assembly with a catalog is read from it, without loading its types or its references; the others are scanned.
        // The predicates below apply to both sources, so a catalog cannot add a type that the scan would not find.
        var classCandidates = new List<Type>();
        var methodCandidates = new List<Type>();
        foreach (var assembly in searchAssemblies)
        {
            // The references of the assembly are asked once: they decide whether it can carry a catalog and whether its methods are searched. Without dynamic code, they cannot be asked,
            // and every assembly may carry a catalog.
            var referencesLibrary = DynamicCodeSupported && CatalogReader.ReferencesLibrary(assembly);
            if (CatalogReader.TryRead(assembly, UseCommandCatalogs, referencesLibrary || !DynamicCodeSupported, out var catalog))
            {
                classCandidates.AddRange(catalog.CommandTypes);
                methodCandidates.AddRange(catalog.MethodCommandTypes);
                continue;
            }

            // Only the catalogs can be read without dynamic code: an assembly that has none is not scanned.
            if (!DynamicCodeSupported)
            {
                continue;
            }

            if (definedTree is null)
            {
                classCandidates.AddRange(GetLoadableTypes(assembly));
            }

            if (referencesLibrary)
            {
                methodCandidates.AddRange(GetLoadableTypes(assembly));
            }
        }

        var classTypes = definedTree?.FlattenValues().ToArray() ?? classCandidates
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.IsAssignableTo(typeof(IStarterCommand)) &&
                t.Namespace != null
            ).ToArray();
        var methodTypes = methodCandidates.Where(type => type.IsClass && type.Namespace is not null && MethodCommandsLoader.DeclaresCommands(type)).ToArray();
        if (!Assemblies.IsEmpty && classTypes.Length == 0 && methodTypes.Length == 0) throw new NoCommandFoundException(NoCommandFoundException.Filter.Assemblies);

        // Filter by namespaces
        if (definedTree is null) classTypes = FilterTypesByNamespaces(classTypes, Namespaces.ToList()).ToArray();
        methodTypes = FilterTypesByNamespaces(methodTypes, Namespaces.ToList()).ToArray();
        if (classTypes.Length == 0 && methodTypes.Length == 0) throw new NoCommandFoundException(NoCommandFoundException.Filter.Namespaces);

        // Filter by classes
        if (definedTree is null) classTypes = FilterTypesByClasses(classTypes, Classes.ToList()).ToArray();
        methodTypes = FilterTypesByClasses(methodTypes, Classes.ToList()).ToArray();
        if (classTypes.Length == 0 && methodTypes.Length == 0) throw new NoCommandFoundException(NoCommandFoundException.Filter.Classes);

        // Deterministic order: assemblies loading and reflection order must not change the tree or the help
        this.commandsTypes = CommandsTypes.Clear().AddRange(definedTree is null ? classTypes.OrderBy(type => type.FullName, StringComparer.Ordinal) : classTypes);
        MethodCommandsTypes = ImmutableList<Type>.Empty.AddRange(methodTypes.OrderBy(type => type.FullName, StringComparer.Ordinal));
    }

    /// <summary>
    /// Build tree from commands types found by <see cref="FindCommandsTypes"/>
    /// </summary>
    public void BuildTree()
    {
        if (!hasToBuildTree) return;
        hasToBuildTree = false;

        try
        {
            BuildCommandsTree();
        }
        catch
        {
            ResetTrees(); // A failed build leaves nothing half done: the next call runs it again
            throw;
        }
    }

    private void BuildCommandsTree()
    {
        FindCommandsTypes();

        if (definedTree is not null)
        {
            CommandsTypesTree = definedTree;
        }
        else
        {
            var builder = new CommandsTreeBuilder(ClassesBuildingMode, CommandsTypes);
            CommandsTypesTree = builder.BuildTree();
            groupPaths = GroupsByNamespace && ClassesBuildingMode != ClassesBuildingMode.OnlyAttributes
                ? BuildNamespaceGroupPaths()
                : new Dictionary<Type, List<CommandTreeDefinition.GroupSegment>>();
            CommandsTypesTree.SetImmutable();
        }

        if (definedTree is not null)
        {
            groupPaths = definedTreeState!.GroupPaths;
        }
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
            InstantiateCommandsCore(activations);
        }
        catch
        {
            ResetTrees(); // A failed instantiation leaves nothing half done: the next call runs it again
            throw;
        }
        finally
        {
            foreach (var activation in activations) activation.Dispose();
        }
    }

    /// <summary>
    /// Instantiate commands using the tree built by <see cref="BuildTree" />, like <see cref="InstantiateCommands"/>,
    /// but the description instances are released with <c>DisposeAsync</c> when they are <see cref="IAsyncDisposable"/>, instead of blocking on it.
    /// </summary>
    /// <returns>A task that completes when the commands are instantiated and the description instances are released.</returns>
    public async Task InstantiateCommandsAsync()
    {
        if (!hasToInstantiate) return;
        hasToInstantiate = false;
        var activations = new List<CommandActivation>();
        try
        {
            InstantiateCommandsCore(activations);
        }
        catch
        {
            ResetTrees(); // A failed instantiation leaves nothing half done: the next call runs it again
            throw;
        }
        finally
        {
            foreach (var activation in activations) await activation.DisposeAsync();
        }
    }

    // Builds the commands. The description instances it creates are added to activations, which the caller releases.
    private void InstantiateCommandsCore(List<CommandActivation> activations)
    {
        BuildTree();

        // Lonely command
        var lonelyCommandHasGroups = CommandsTypes.Count == 1 && HasGroupPath(CommandsTypes[0]);
        if (IsRootingLonelyCommand && CommandsTypes.Count == 1 && MethodCommandsTypes.IsEmpty && !lonelyCommandHasGroups)
        {
            var registration = CreateCommand(CommandsTypes[0], activations);
            AddStateMetadata(registration.CliCommand, registration.DescriptionInstance.GetType(), canHide: false);
            InitializeRegistration(registration, RootCommand);
            registration.CliCommand.Hidden = true;
            RootCommand.Add(registration.CliCommand);
            RootCommand.SetAction((parseResult, cancellationToken) => HandleCommandAsync(registration, parseResult, cancellationToken));
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

        // Global options: the filters are applied before the method commands, which check their container parameters against them
        GlobalOptionsManager.FilterTypes();

        foreach (var type in MethodCommandsTypes)
        {
            MethodCommandsLoader.Validate(type);
            AddMethodCommands(type);
        }

        GlobalOptionsManager.LoadOptions(RootCommand);
        if (commandStateMetadata.Values.Any(metadata => metadata.State is not null) && ResolveStateProvider() is null)
        {
            throw new InvalidAttributeException("Commands use [AvailableInState] but no ICommandStateProvider is configured: set Starter.StateProvider or register an ICommandStateProvider service.");
        }

        SymbolNamesValidator.Validate(RootCommand, SequenceOption);
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
    /// <exception cref="ArgumentNullException"><paramref name="currentParent"/> or <paramref name="action"/> is null.</exception>
    public static void VisitCommands(Command currentParent, Action<Command> action)
    {
        ArgumentNullException.ThrowIfNull(currentParent);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var child in currentParent.Subcommands)
        {
            action(child);
            VisitCommands(child, action);
        }
    }

    private void AddLevel(Command currentParent, TreeNode<Type> currentNode, List<CommandActivation> activations)
    {
        var childrenWithoutGroups = currentNode.Children.Where(child => !HasGroupPath(child.Value!));
        var childrenWithGroups = currentNode.Children.Where(child => HasGroupPath(child.Value!));
        foreach (var childNode in childrenWithoutGroups)
        {
            var registration = CreateCommand(childNode.Value!, activations); // childNode.Value can't be null, because only the root has a null Value
            currentParent.Subcommands.Add(registration.CliCommand);
            AddLevel(registration.CliCommand, childNode, activations);
        }

        foreach (var childNode in childrenWithGroups)
        {
            var groups = groupPaths[childNode.Value!];
            var groupParent = ResolveGroupParent(currentParent, groups);
            var registration = CreateCommand(childNode.Value!, activations);
            groupParent.Subcommands.Add(registration.CliCommand);
            AddLevel(registration.CliCommand, childNode, activations);
        }
    }

    private bool HasGroupPath(Type commandType) => groupPaths.TryGetValue(commandType, out var groups) && groups.Count > 0;

    private Dictionary<Type, List<CommandTreeDefinition.GroupSegment>> BuildNamespaceGroupPaths()
    {
        var result = new Dictionary<Type, List<CommandTreeDefinition.GroupSegment>>();
        if (CommandsTypes.IsEmpty)
        {
            return result;
        }

        var namespaces = CommandsTypes.Select(commandType => commandType.Namespace!.Split('.')).ToList();
        var baseSegments = namespaces[0].ToList();
        foreach (var commandNamespace in namespaces.Skip(1))
        {
            var commonLength = 0;
            while (commonLength < baseSegments.Count
                && commonLength < commandNamespace.Length
                && string.Equals(baseSegments[commonLength], commandNamespace[commonLength], StringComparison.Ordinal))
            {
                commonLength++;
            }

            baseSegments = baseSegments.Take(commonLength).ToList();
        }

        foreach (var child in CommandsTypesTree.Children)
        {
            var commandType = child.Value!;
            if (commandType.GetCustomAttributes(false).Any(attribute => attribute is ParentAttribute))
            {
                continue;
            }

            var commandNamespace = commandType.Namespace!.Split('.');
            var pathLength = commandNamespace.Length - baseSegments.Count;
            var representsNamespace = child.Children.Any(descendant =>
                descendant.Value!.Namespace!.StartsWith(commandType.Namespace + ".", StringComparison.Ordinal));
            if (representsNamespace)
            {
                pathLength--;
            }

            var groups = commandNamespace
                .Skip(baseSegments.Count)
                .Take(Math.Max(pathLength, 0))
                .Select(segment => new CommandTreeDefinition.GroupSegment(segment.ToCommandName()))
                .ToList();
            result.Add(commandType, groups);
        }

        return result;
    }

    private Command ResolveGroupParent(Command start, IReadOnlyList<CommandTreeDefinition.GroupSegment> groups)
    {
        var parent = start;
        foreach (var segment in groups)
        {
            var child = parent.Subcommands.FirstOrDefault(command => string.Equals(command.Name, segment.Name, StringComparison.Ordinal));
            if (child is null)
            {
                child = new Command(segment.Name);
                AddMissingAliases(child, segment.Aliases);
                parent.Subcommands.Add(child);
                implicitMethodGroups.Add(child);
            }
            else
            {
                AddMissingAliases(child, segment.Aliases);
            }

            parent = child;
        }

        return parent;
    }

    private static void AddMissingAliases(Command command, IEnumerable<string> aliases)
    {
        foreach (var alias in aliases)
        {
            if (!command.Aliases.Contains(alias, StringComparer.Ordinal)) command.Aliases.Add(alias);
        }
    }

    private CommandRegistration CreateCommand(Type commandType, List<CommandActivation> activations)
    {
        var activation = Activate(commandType);
        activations.Add(activation);
        var instance = (IStarterCommand)activation.Instance!;
        instance.GlobalOptionsManager = GlobalOptionsManager;

        var name = definedTreeState?.Names.TryGetValue(commandType, out var definedName) == true
            ? definedName
            : instance is StarterCommand starterCommand
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
        AddStateMetadata(cliCommand, commandType, canHide: true);
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
            command.SetAction((parseResult, cancellationToken) => HandleCommandAsync(registration, parseResult, cancellationToken));
        }

        registration.OptionBindings = Reflection.Loader.LoadOptions(registration.DescriptionInstance.GetType(), receptacle);
        foreach (var binding in registration.OptionBindings)
        {
            binding.InitialValue = binding.Property.GetValue(registration.DescriptionInstance);
            Reflection.Loader.SetImplicitDefault(binding.Option, binding.InitialValue);
        }
        Reflection.Loader.LoadDescription(registration.DescriptionInstance.GetType(), receptacle);
        if (string.IsNullOrEmpty(receptacle.Description)
            && registration.DescriptionInstance is StarterCommand starterCommand
            && starterCommand.Description is not null)
        {
            receptacle.Description = starterCommand.Description;
        }
        Reflection.Loader.LoadAliases(registration.DescriptionInstance.GetType(), receptacle.Aliases);
        if (definedTreeState?.Aliases.TryGetValue(registration.CommandType, out var aliases) == true)
        {
            AddMissingAliases(receptacle, aliases);
        }
        command.Hidden = Attribute.IsDefined(registration.DescriptionInstance.GetType(), typeof(HiddenAttribute));
    }

    [SuppressMessage("Design", "CA1031", Justification = "Converts any command failure into a one-line message and exit code 1 unless ShowExceptionDetails is set.")]
    private async Task<int> HandleCommandAsync(CommandRegistration registration, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!registration.IsAvailable)
        {
            parseResult.InvocationConfiguration.Error.WriteLine($"Command '{GetCommandPath(registration.CliCommand)}' is not available in the current state.");
            return 1;
        }

        try
        {
            return await HandleCommandCoreAsync(registration, parseResult, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return 130;
        }
        catch (Exception exception) when (!ShowExceptionDetails)
        {
            parseResult.InvocationConfiguration.Error.WriteLine(exception.Message.ReplaceLineEndings(" "));
            return 1;
        }
    }

    private async Task<int> HandleCommandCoreAsync(CommandRegistration registration, ParseResult parseResult, CancellationToken cancellationToken)
    {
        await using var activation = Activate(registration.CommandType);
        var instance = (IStarterCommand)activation.Instance!;
        instance.GlobalOptionsManager = GlobalOptionsManager;
        if (instance is StarterCommand starterCommand) starterCommand.CliCommand = registration.CliCommand;
        GlobalOptionsManager.BindGlobalOptions(parseResult);
        CommandBinder.BindOptions(parseResult, instance, registration.OptionBindings);
        var console = GetExecutionConsole(parseResult);
        var context = new CommandContext(registration.CliCommand, GetCommandPath(registration.CliCommand), registration.CommandType, instance, parseResult, activation.Services, console, cancellationToken);
        return await InvokeFiltersAsync(context, async () =>
        {
            int exitCode;
            try
            {
                exitCode = await CommandBinder.InvokeHandlerAsync(parseResult, instance.HandlingMethod, registration.HandlerArguments, console, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                exitCode = 130;
            }
            CommandHandled?.Invoke(this, new CommandHandledEventArgs(registration.CliCommand, instance, exitCode));
            return exitCode;
        });
    }

    [SuppressMessage("Design", "CA1031", Justification = "Converts any command failure into a one-line message and exit code 1 unless ShowExceptionDetails is set.")]
    private async Task<int> HandleMethodCommandAsync(MethodCommandRegistration registration, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!registration.IsAvailable)
        {
            parseResult.InvocationConfiguration.Error.WriteLine($"Command '{GetCommandPath(registration.CliCommand)}' is not available in the current state.");
            return 1;
        }

        try
        {
            return await HandleMethodCommandCoreAsync(registration, parseResult, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return 130;
        }
        catch (Exception exception) when (!ShowExceptionDetails)
        {
            parseResult.InvocationConfiguration.Error.WriteLine(exception.Message.ReplaceLineEndings(" "));
            return 1;
        }
    }

    private async Task<int> HandleMethodCommandCoreAsync(MethodCommandRegistration registration, ParseResult parseResult, CancellationToken cancellationToken)
    {
        await using var activation = ActivateMethodCommand(registration.Method);
        GlobalOptionsManager.BindGlobalOptions(parseResult);
        var console = GetExecutionConsole(parseResult);
        var values = new object?[registration.Parameters.Count];
        for (var index = 0; index < registration.Parameters.Count; index++)
        {
            var binding = registration.Parameters[index];
            values[index] = binding.Kind switch
            {
                ParameterBindingKind.CancellationToken => cancellationToken,
                ParameterBindingKind.Console => console,
                ParameterBindingKind.GlobalOptions => GlobalOptionsManager.GetGlobalOptions(binding.Parameter.ParameterType),
                ParameterBindingKind.Service => GetMethodService(activation, binding.Parameter),
                ParameterBindingKind.Option => GetMethodOptionValue(parseResult, (Option)binding.Symbol!, binding.Parameter),
                _ => CommandBinder.GetArgumentValue(parseResult, (Argument)binding.Symbol!)
            };
        }
        var context = new CommandContext(registration.CliCommand, GetCommandPath(registration.CliCommand), registration.Method, activation.Instance, parseResult, activation.Services, console, cancellationToken);
        return await InvokeFiltersAsync(context, async () =>
        {
            int exitCode;
            try
            {
                var result = CommandBinder.Invoke(() => registration.Method.Invoke(activation.Instance, values));
                exitCode = await CommandBinder.ToExitCodeAsync(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                exitCode = 130;
            }
            CommandHandled?.Invoke(this, new CommandHandledEventArgs(registration.CliCommand, activation.Instance, exitCode));
            return exitCode;
        });
    }

    // The console attached to this invocation by StartCoreAsync, else the console of the process read now.
    private CommandConsole GetExecutionConsole(ParseResult parseResult) =>
        consoles.TryGetValue(parseResult.InvocationConfiguration, out var console) ? console : CommandConsole.Standard;

    private async Task<int> InvokeFiltersAsync(CommandContext context, Func<Task<int>> command)
    {
        var activatedFilters = new List<(ICommandFilter Filter, bool Owned)>();
        try
        {
            foreach (var registration in filters)
            {
                if (registration.Instance is not null)
                {
                    activatedFilters.Add((registration.Instance, false));
                    continue;
                }

                var filterType = registration.FilterType!;
                var resolved = context.Services?.GetService(filterType);
                var filter = resolved as ICommandFilter;
                var owned = false;
                if (filter is null)
                {
                    filter = context.Services is null
                        ? (ICommandFilter)Activator.CreateInstance(filterType)!
                        : (ICommandFilter)ActivatorUtilities.CreateInstance(context.Services, filterType);
                    owned = true;
                }
                activatedFilters.Add((filter, owned));
            }

            Func<Task<int>> next = command;
            for (var index = activatedFilters.Count - 1; index >= 0; index--)
            {
                var filter = activatedFilters[index].Filter;
                var following = next;
                next = () => filter.InvokeAsync(context, following);
            }
            return await next();
        }
        finally
        {
            for (var index = activatedFilters.Count - 1; index >= 0; index--)
            {
                if (!activatedFilters[index].Owned) continue;
                var filter = activatedFilters[index].Filter;
                if (filter is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (filter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    private static object? GetMethodOptionValue(ParseResult result, Option option, ParameterInfo parameter)
    {
        if (CommandBinder.TryGetOptionValue(result, option, out var value)) return value;
        return parameter.ParameterType.IsValueType ? SymbolFactories.For(parameter.ParameterType).DefaultValue : null;
    }

    private void AddMethodCommands(Type type)
    {
        var groupAttribute = type.GetCustomAttribute<CommandAttribute>();
        Command? group = null;
        if (groupAttribute is not null)
        {
            var groupPath = GetCommandPath(groupAttribute.Name ?? type.Name, type, "<group>");
            var groupParent = ResolveMethodCommandParent(RootCommand, groupPath);
            var groupName = groupPath[^1];
            group = groupParent.Subcommands.FirstOrDefault(command => string.Equals(command.Name, groupName, StringComparison.Ordinal) && implicitMethodGroups.Contains(command));
            if (group is null)
            {
                group = new Command(groupName);
                EnsureUniqueName(groupParent, group, type, "<group>");
                groupParent.Subcommands.Add(group);
            }
            else
            {
                implicitMethodGroups.Remove(group);
            }

            Reflection.Loader.LoadDescription(type, group);
            Reflection.Loader.LoadAliases(type, group.Aliases);
            group.Hidden = Attribute.IsDefined(type, typeof(HiddenAttribute));
            AddStateMetadata(group, type, canHide: true);
        }
        var parent = group ?? RootCommand;
        foreach (var method in MethodCommandsLoader.GetCommandMethods(type, DynamicCodeSupported))
        {
            var attribute = method.GetCustomAttribute<CommandAttribute>()!;
            var inferredName = method.Name.EndsWith("Async", StringComparison.Ordinal) ? method.Name[..^5] : method.Name;
            var name = attribute.Name ?? inferredName;
            var commandPath = GetCommandPath(name, type, method.Name);
            var commandParent = ResolveMethodCommandParent(parent, commandPath);
            var command = new Command(commandPath[^1]);
            Reflection.Loader.LoadDescription(method, command);
            Reflection.Loader.LoadAliases(method, command.Aliases);
            command.Hidden = Attribute.IsDefined(method, typeof(HiddenAttribute));
            AddStateMetadata(command, method, canHide: true);
            var parameters = new List<ParameterBinding>();
            foreach (var parameter in method.GetParameters())
            {
                var parameterType = parameter.ParameterType;
                if (parameterType == typeof(CancellationToken)) { parameters.Add(new(parameter, ParameterBindingKind.CancellationToken, null)); continue; }
                if (parameterType == typeof(CommandConsole)) { parameters.Add(new(parameter, ParameterBindingKind.Console, null)); continue; }
                if (typeof(IGlobalOptionsContainer).IsAssignableFrom(parameterType))
                {
                    if (!GlobalOptionsManager.GlobalOptionsTypes.Contains(parameterType))
                    {
                        throw new InvalidAttributeException($"Parameter '{parameter.Name}' of method '{method.Name}' (type '{type.FullName}') is the global options container '{parameterType.FullName}', which the filters exclude.");
                    }

                    parameters.Add(new(parameter, ParameterBindingKind.GlobalOptions, null));
                    continue;
                }
                if (Attribute.IsDefined(parameter, typeof(FromServicesAttribute)))
                {
                    if (ServiceProvider is null) throw new InvalidAttributeException($"[FromServices] requires ServiceProvider. Type '{type.FullName}', method '{method.Name}'.");
                    parameters.Add(new(parameter, ParameterBindingKind.Service, null)); continue;
                }
                if (Attribute.IsDefined(parameter, typeof(OptionAttribute)))
                {
                    var option = Reflection.Loader.CreateParameterOption(parameter);
                    command.Options.Add(option);
                    parameters.Add(new(parameter, ParameterBindingKind.Option, option)); continue;
                }
                if (ServiceProvider?.GetService<IServiceProviderIsService>()?.IsService(parameterType) == true)
                {
                    parameters.Add(new(parameter, ParameterBindingKind.Service, null)); continue;
                }
                var argument = Reflection.Loader.CreateArgument(parameter);
                command.Arguments.Add(argument);
                parameters.Add(new(parameter, ParameterBindingKind.Argument, argument));
            }
            EnsureUniqueName(commandParent, command, type, method.Name);
            commandParent.Subcommands.Add(command);
            var registration = new MethodCommandRegistration(method, command, parameters);
            methodRegistrations.Add(registration);
            command.SetAction((parseResult, token) => HandleMethodCommandAsync(registration, parseResult, token));
        }
    }

    private static string[] GetCommandPath(string name, Type type, string memberName)
    {
        var path = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => segment.ToCommandName())
            .ToArray();
        if (path.Length == 0)
        {
            throw new InvalidAttributeException($"Command name cannot be empty. Type '{type.FullName}', method '{memberName}'.");
        }

        return path;
    }

    private Command ResolveMethodCommandParent(Command start, string[] path)
    {
        var parent = start;
        for (var index = 0; index < path.Length - 1; index++)
        {
            var segment = path[index];
            var child = parent.Subcommands.FirstOrDefault(command => string.Equals(command.Name, segment, StringComparison.Ordinal));
            if (child is null)
            {
                child = new Command(segment);
                parent.Subcommands.Add(child);
                implicitMethodGroups.Add(child);
            }

            parent = child;
        }

        return parent;
    }

    private static void EnsureUniqueName(Command parent, Command candidate, Type type, string methodName)
    {
        if (parent.Subcommands.Any(command => string.Equals(command.Name, candidate.Name, StringComparison.Ordinal)))
            throw new InvalidAttributeException($"Duplicate command name '{candidate.Name}' under '{parent.Name}'. Type '{type.FullName}', method '{methodName}'.");
    }

    private void AddStateMetadata(Command command, MemberInfo member, bool canHide)
    {
        var state = member.GetCustomAttribute<AvailableInStateAttribute>(inherit: false);
        var hidden = Attribute.IsDefined(member, typeof(HiddenAttribute));
        commandStateMetadata[command] = new CommandStateMetadata(state, hidden, canHide);
    }

    private ICommandStateProvider? ResolveStateProvider() =>
        StateProvider ?? ServiceProvider?.GetService(typeof(ICommandStateProvider)) as ICommandStateProvider;

    private protected void UpdateCommandAvailability()
    {
        var metadata = commandStateMetadata.Values;
        if (!metadata.Any(item => item.State is not null)) return;

        var currentState = ResolveStateProvider()!.CurrentState;
        var methodRegistrationsByCommand = methodRegistrations.ToDictionary<MethodCommandRegistration, Command>(registration => registration.CliCommand, ReferenceEqualityComparer.Instance);
        void Update(Command parent, bool parentAvailable)
        {
            foreach (var child in parent.Subcommands)
            {
                // A command CmdStarter did not create (added to RootCommand by the application) has no metadata and is left as it is.
                var hasMetadata = commandStateMetadata.TryGetValue(child, out var item);
                var available = parentAvailable && (item?.State is null || item.State.States.Any(state => Equals(state, currentState)));
                // The rooted lonely command stays hidden: its symbols live on the root command.
                if (hasMetadata && item!.CanHide)
                {
                    child.Hidden = item.Hidden || !available;
                }

                if (registrationsByCommand.TryGetValue(child, out var commandRegistration)) commandRegistration.IsAvailable = available;
                if (methodRegistrationsByCommand.TryGetValue(child, out var methodRegistration)) methodRegistration.IsAvailable = available;
                Update(child, available);
            }
        }

        Update(RootCommand, parentAvailable: true);
    }

    private sealed record CommandStateMetadata(AvailableInStateAttribute? State, bool Hidden, bool CanHide);

    private CommandActivation ActivateMethodCommand(MethodInfo method)
    {
        if (ServiceProvider is not null) return ActivateFromServices(method.IsStatic ? null : method.DeclaringType);
        if (method.IsStatic) return new CommandActivation(null, ownsInstance: false);
        return new CommandActivation(Activator.CreateInstance(method.DeclaringType!)!, ownsInstance: true);
    }

    private static object? GetMethodService(CommandActivation activation, ParameterInfo parameter)
    {
        if (activation.Services is null)
            throw new InvalidOperationException($"No service provider is available for parameter '{parameter.Name}' of method '{parameter.Member.Name}'.");
        return activation.Services.GetRequiredService(parameter.ParameterType);
    }

    private void ResetCommands()
    {
        hasToFindCommands = true;
        this.commandsTypes = commandsTypes.Clear();
        MethodCommandsTypes = ImmutableList<Type>.Empty;
        ResetTrees();
    }

    private void ResetTrees()
    {
        hasToBuildTree = true;
        hasToInstantiate = true;
        this.CommandsTypesTree = new TreeNode<Type>(null);
        groupPaths = new Dictionary<Type, List<CommandTreeDefinition.GroupSegment>>();
        registrationsByCommand.Clear();
        registrationsByType.Clear();
        methodRegistrations.Clear();
        commandStateMetadata.Clear();
        implicitMethodGroups.Clear();
        rootCommand = new();
    }

    private CommandActivation Activate(Type commandType)
    {
        var getInstance = FindGetInstanceMethod(commandType);
        if (getInstance.DeclaringType != typeof(IStarterCommand))
        {
            var instance = (IStarterCommand)getInstance.Invoke(null, null)!;
            return new CommandActivation(instance, ownsInstance: true);
        }

        if (ServiceProvider is not null)
        {
            return ActivateFromServices(commandType);
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

    private CommandActivation ActivateFromServices(Type? instanceType)
    {
        var provider = ServiceProvider!;
        var scopeFactory = provider.GetService<IServiceScopeFactory>();
        if (scopeFactory is null)
        {
            var resolved = instanceType is null ? null : provider.GetService(instanceType);
            var instance = instanceType is null ? null : resolved ?? ActivatorUtilities.CreateInstance(provider, instanceType);
            return new CommandActivation(instance, ownsInstance: instanceType is not null && resolved is null, services: provider);
        }

        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var resolvedInScope = instanceType is null ? null : scope.ServiceProvider.GetService(instanceType);
            var instance = instanceType is null ? null : resolvedInScope ?? ActivatorUtilities.CreateInstance(scope.ServiceProvider, instanceType);
            return new CommandActivation(instance, ownsInstance: instanceType is not null && resolvedInScope is null, scope, scope.ServiceProvider);
        }
        catch
        {
            scope.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw;
        }
    }

    [Obsolete("IServiceManager is obsolete: set Starter.ServiceProvider or use CmdStarterBuilder (Microsoft.Extensions.DependencyInjection), or call Starter.SetFactory for another container.")]
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

    private sealed record FilterRegistration(ICommandFilter? Instance, Type? FilterType);

}
