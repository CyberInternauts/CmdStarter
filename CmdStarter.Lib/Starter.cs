using System.Collections.Immutable;
using System.CommandLine;
using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using static com.cyberinternauts.csharp.CmdStarter.Lib.Reflection.Helper;
using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib
{
    public enum ClassesBuildingMode
    {
        Both = 0,
        OnlyAttributes = 1,
        OnlyNamespaces = 2
    }

    public class Starter
    {
        public const string EXCLUSION_SYMBOL = "~";
        public const string ANY_CHAR_SYMBOL = "?";
        public const string ANY_CHAR_SYMBOL_INCLUDE_DOTS = "??";
        public const string MULTI_ANY_CHAR_SYMBOL = "*";
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

        public Starter() : this(Array.Empty<string>())
        {
        }

        public Starter(string[] namespaces) : this(new List<string>(namespaces))
        {
        }

        public Starter(List<string> namespaces)
        {
            GlobalOptionsManager = new(this);
            Namespaces = Namespaces.AddRange(namespaces);
        }

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

        /// <summary>
        /// Configure how to build the commands tree: See remarks.
        /// </summary>
        /// <remarks>
        /// (Order is important. Ending when ClassC is assigned)
        /// 
        /// - Both: Use (Parent|Children)Attributes and if nothing then namespaces.
        ///     - If ClassC has a <see cref="ParentAttribute"/> set to ClassP ==> Assign ClassC as subcommand of ClassP.
        ///     - If ClassP has a <see cref="ChildrenAttribute"/> set to the namespace of ClassC ==> Assign ClassC as subcommand of ClassP.
        ///     - If ClassC's parent namespace has only one CmdStarterCommand (ClassP) AND :
        ///         - ClassC doesn't have a <see cref="ParentAttribute"/> ==> Assign ClassC as subcommand of ClassP.
        ///         - ClassC is not covered by a <see cref="ChildrenAttribute"/> ==> Assign ClassC as subcommand of ClassP.
        /// 
        /// - OnlyAttributes: Use only (Parent|Children)Attributes.
        ///     - If ClassC has a <see cref="ParentAttribute"/>  set to ClassP ==> Assign ClassC as subcommand of ClassP.
        ///     - If ClassP has a <see cref="ChildrenAttribute"/> set to the namespace of ClassC ==> Assign ClassC as subcommand of ClassP.
        /// 
        /// - OnlyNamespaces:
        ///     - If ClassC's parent namespace has only one CmdStarterCommand (ClassP)
        /// </remarks>
        public ClassesBuildingMode ClassesBuildingMode {
            get => classesBuildingMode;

            set
            {
                ResetTrees();
                classesBuildingMode = value;
            }
        }

        public ImmutableList<Type> CommandsTypes { get => commandsTypes; }

        public TreeNode<Type> CommandsTypesTree { get; set; } = new(null);

        public RootCommand RootCommand { get => rootCommand; }

        /// <summary>
        /// Find all classes implementing <see cref="StarterCommand"/>, build a tree based on their namespaces and try to execute a command
        /// </summary>
        public async Task<int> Start(string[] args)
        {
            InstantiateCommands();

            var b = new CommandLineBuilder(RootCommand);





            if (hasToUseDefaults)
            {
                hasToUseDefaults = false;
                b.UseDefaults();
            }

            var parser = b.Build();


            return await parser.InvokeAsync(args);
        }

        /// <summary>
        /// Find all classes implementing <see cref="StarterCommand"/>, build a tree based on their namespaces and try to execute a command
        /// </summary>
        public async Task<int> Start(IServiceCollection provider, string[] args)
        {
            //TODO: Copy changes in Start(args)
            provider.AddTransient<string, string>();
            InstantiateCommands();
            var b = new CommandLineBuilder(RootCommand);
            await b.UseDefaults().Build().InvokeAsync(args);
            await Task.Delay(500);
            return 0;
        }

        public Command? FindCommand<CommandType>() where CommandType : Command
        {
            var loopBody = (Command child) =>
            {
                if (child is CommandType) return child;
                return null;
            };

            return VisitCommands(RootCommand, loopBody);
        }

        /// <summary>
        /// Find all <see cref="StarterCommand "/>s that have a namespace
        /// </summary>
        /// <exception cref="Exceptions.InvalidNamespaceException"></exception>
        public void FindCommandsTypes()
        {
            if (!hasToFindCommands) return; // Quit, job already done
            hasToFindCommands = false;

            var commandsTypes = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(StarterCommand)) && t.Namespace != null));

            // Filter by namespaces
            commandsTypes = FilterTypesByNamespaces(commandsTypes, Namespaces.ToList());
            if (!commandsTypes.Any()) throw new NoCommandFoundException(NoCommandFoundException.Filters.Namespaces);

            // Filter by namespaces
            commandsTypes = FilterTypesByClasses(commandsTypes, Classes.ToList());
            if (!commandsTypes.Any()) throw new NoCommandFoundException(NoCommandFoundException.Filters.Classes);

            this.commandsTypes = CommandsTypes.Clear();
            if (commandsTypes != null)
            {
                var commandsToAdd = commandsTypes.ToArray();
                this.commandsTypes = CommandsTypes.AddRange(commandsToAdd);
            }
        }

        public void BuildTree()
        {
            if (!hasToBuildTree) return;
            hasToBuildTree = false;

            FindCommandsTypes();

            var builder = new CommandsTreeBuilder(ClassesBuildingMode, CommandsTypes);
            CommandsTypesTree = builder.BuildTree();
            CommandsTypesTree.SetImmutable();
        }

        public void InstantiateCommands()
        {
            if (!hasToInstantiate) return;
            hasToInstantiate = false;

            BuildTree();

            // Lonely command
            if (CommandsTypes.Count == 1)
            {
                var command = CreateCommand(CommandsTypes[0])!;
                command.IsHidden = true;
                command.Initialize(RootCommand);
                RootCommand.Add(command);
                RootCommand.Handler = CommandHandler.Create((InvocationContext context) => {
                    return command.Handler?.Invoke(context);
                });
            } else
            {
                AddLevel(RootCommand, CommandsTypesTree);

                // Doing Initialize after having created all because the command may act differently if it has children or not
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

        public void VisitCommands(Action<Command> action)
        {
            VisitCommands(RootCommand, action);
        }

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
            var command = Activator.CreateInstance(commandType) as StarterCommand;
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

        private string? GetParentNamespace(string? namespaceToCut)
        {
            if (namespaceToCut == null) return null;

            var dotIndex = namespaceToCut.LastIndexOf(".");
            if (dotIndex == -1) return null;

            return namespaceToCut[..dotIndex];
        }

        /// <summary>
        /// Visit commands from a specific command until not null is returned
        /// </summary>
        /// <typeparam name="CommandType">Command type </typeparam>
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
    }
}