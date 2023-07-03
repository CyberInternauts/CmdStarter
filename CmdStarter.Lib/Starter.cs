using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Helper;
using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.Data;
using com.cyberinternauts.csharp.CmdStarter.Lib.SpecialCommands;
using com.cyberinternauts.csharp.CmdStarter.Lib.Loader;
using System.Diagnostics.CodeAnalysis;

namespace com.cyberinternauts.csharp.CmdStarter.Lib
{
    /// <summary>
    /// Main class executing the command using the command line arguments
    /// </summary>
    public sealed class Starter
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

        private RootCommand rootCommand = new ();
        private ImmutableList<string> namespaces = ImmutableList<string>.Empty;
        private ImmutableList<string> classes = ImmutableList<string>.Empty;
        private ImmutableList<Type> commandsTypes = ImmutableList<Type>.Empty;
        private bool hasToFindCommands = true;
        private bool hasToBuildTree = true;
        private bool hasToInstantiate = true;
        private bool hasToUseDefaults = true;
        private ClassesBuildingMode classesBuildingMode = ClassesBuildingMode.Both;
        private bool isRootingLonelyCommand = true;
        private Parser? parser = null;

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
            set {
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
        public ClassesBuildingMode ClassesBuildingMode {
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
        public ImmutableList<Type> CommandsTypes { 
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

            return await BuildParser().InvokeAsync(args);
        }

        /// <summary>
        /// Find all classes implementing <see cref="IStarterCommand"/>, build a tree based on their namespaces and try to execute a command
        /// </summary>
        public async Task<int> Start(string args)
        {
            InstantiateCommands();

            return await BuildParser().InvokeAsync(args);
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
            var loopBody = (Command child) =>
            {
                if (child is GenericStarterCommand<CommandType>) return child;
                if (child is CommandType) return child;
                return null;
            };

            return VisitCommands(RootCommand, loopBody);
        }

        /// <summary>
        /// Find all <see cref="IStarterCommand "/>s that correspond to filters
        /// </summary>
        /// <exception cref="Exceptions.NoCommandFoundException"></exception>
        public void FindCommandsTypes()
        {
            if (!hasToFindCommands) return; // Quit, job already done
            hasToFindCommands = false;

            var specialCommandsNamespace = typeof(GenericStarterCommand<>).Namespace;
            var commandsTypes = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes()
                            .Where(t => 
                                t.IsClass && 
                                !t.IsAbstract && 
                                t.IsAssignableTo(typeof(IStarterCommand)) &&
                                t.Namespace != null &&
                                t.Namespace != specialCommandsNamespace && 
                                !t.Namespace.StartsWith(specialCommandsNamespace + ".")
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
                var command = CreateCommand(CommandsTypes[0])!;
                command.IsHidden = true;
                command.Initialize(RootCommand);
                RootCommand.Add(command);
                RootCommand.Handler = CommandHandler.Create((InvocationContext context) => {
                    return command.Handler?.Invoke(context);
                });
            } 
            else
            {
                AddLevel(RootCommand, CommandsTypesTree);

                // Doing Initialize after having created all because a command may act differently if it has children or not
                VisitCommands(RootCommand, (child) =>
                {
                    if (child is StarterCommand command)
                    {
                        command.Initialize();
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
                var command = CreateCommand(childNode.Value!); // childNode.Value can't be null, because only the root has a null Value
                if (command != null)
                {
                    currentParent.AddCommand(command);
                    AddLevel(currentParent.Subcommands[currentParent.Subcommands.Count - 1], childNode);
                }
            }
        }

        private StarterCommand? CreateCommand(Type commandType)
        {
            var getInstanceMethod = typeof(StarterCommand).GetMethod(nameof(StarterCommand.GetInstance))!.MakeGenericMethod(commandType);
            var command = getInstanceMethod!.Invoke(null, null) as StarterCommand; // This shall always returns a <see cref="StarterCommand">

            if (command != null) command.GlobalOptionsManager = GlobalOptionsManager;
            return command;
        }

        private void ResetCommands()
        {
            hasToFindCommands = true;
            this.commandsTypes = commandsTypes.Clear();
            ResetTrees();
        }

        private void ResetTrees()
        {
            hasToUseDefaults = true;
            hasToBuildTree = true;
            hasToInstantiate = true;
            this.CommandsTypesTree = new TreeNode<Type>(null);
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

        private Parser BuildParser()
        {
            var b = new CommandLineBuilder(RootCommand);

            if (hasToUseDefaults)
            {
                hasToUseDefaults = false;
                b.UseDefaults();
            }

            return b.Build();
        }
    }
}