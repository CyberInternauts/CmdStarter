using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using System.Collections.Immutable;
using System.CommandLine;
using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;

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
        private ClassesBuildingMode classesBuildingMode = ClassesBuildingMode.Both;

        public Starter() { }

        public Starter(string[] namespaces) : this(new List<string>(namespaces))
        {
        }

        public Starter(List<string> namespaces)
        {
            Namespaces = Namespaces.AddRange(namespaces);
        }

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

            return await b.UseDefaults().Build().InvokeAsync(args);
        }

        /// <summary>
        /// Find all classes implementing <see cref="StarterCommand"/>, build a tree based on their namespaces and try to execute a command
        /// </summary>
        public async Task<int> Start(IServiceCollection provider, string[] args)
        {
            provider.AddTransient<string, string>();
            InstantiateCommands();
            var b = new CommandLineBuilder(RootCommand);
            await b.UseDefaults().Build().InvokeAsync(args);
            await Task.Delay(500);
            return 0;
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
            commandsTypes = FilterTypesByNamespaces(commandsTypes);

            // Filter by namespaces
            commandsTypes = FilterTypesByClasses(commandsTypes);

            this.commandsTypes = CommandsTypes.Clear();
            if (commandsTypes != null)
            {
                var commandsToAdd = (Type[])commandsTypes.ToArray();
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

            AddLevel(RootCommand, CommandsTypesTree);
        }

        private void AddLevel(Command currentParent, TreeNode<Type> currentNode)
        {
            var namesAdded = new List<string>();

            foreach (var childNode in currentNode.Children)
            {
                var command = Activator.CreateInstance(childNode.Value!) as StarterCommand; // childNode.Value can't be null, because only the root has a null Value
                if (command != null)
                {
                    namesAdded.Add(command.Name);
                    currentParent.AddCommand(command);
                    AddLevel(currentParent.Subcommands.Last(), childNode);
                }
            }
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
            rootCommand = new();
        }

        private string? GetParentNamespace(string? namespaceToCut)
        {
            if (namespaceToCut == null) return null;

            var dotIndex = namespaceToCut.LastIndexOf(".");
            if (dotIndex == -1) return null;

            return namespaceToCut[..dotIndex];
        }

        private IEnumerable<Type> FilterTypesByNamespaces(IEnumerable<Type> commandsTypes)
        {
            var nbCommands = commandsTypes.Count();
            if (nbCommands != 0 && Namespaces.Any())
            {
                var namespacesIncluded = Namespaces.Where(n => !String.IsNullOrWhiteSpace(n) && !n.StartsWith(EXCLUSION_SYMBOL));
                var hasIncluded = namespacesIncluded.Any();
                var namespacesExcluded = Namespaces.Where(n => !String.IsNullOrWhiteSpace(n) && n.StartsWith(EXCLUSION_SYMBOL));

                commandsTypes = commandsTypes.Where(c =>
                {
                    var outNamespaces = namespacesExcluded.Any(n => c.Namespace?.StartsWith(n[1..]) ?? false);
                    var inNamespaces = !hasIncluded || namespacesIncluded.Any(n => c.Namespace?.StartsWith(n) ?? false);

                    return !outNamespaces && inNamespaces;
                });
                if (!commandsTypes.Any())
                {
                    throw new Exceptions.InvalidNamespaceException();
                }
            }

            return commandsTypes;
        }

        private IEnumerable<Type> FilterTypesByClasses(IEnumerable<Type> commandsTypes)
        {
            var nbCommands = commandsTypes.Count();

            if (nbCommands == 0 || !Classes.Any()) return commandsTypes;

            bool onlyExclude = classes.All(filter => filter.StartsWith(EXCLUSION_SYMBOL));

            Regex dotRegex = new(@"\\.");

            Regex[] excludes = classes.Where(filter => filter.StartsWith(EXCLUSION_SYMBOL))
                .Select(filter =>
                {
                    var pattern = WildcardsToRegex(filter[1..]);
                    return new Regex(pattern, RegexOptions.RightToLeft);
                }).ToArray();

            Regex[] filters = classes.Where(filter => !filter.StartsWith(EXCLUSION_SYMBOL))
                .Select(filter =>
                {
                    var pattern = WildcardsToRegex(filter);
                    return new Regex(pattern, RegexOptions.RightToLeft);
                }).ToArray();

            commandsTypes = commandsTypes.Where(type =>
            {
                bool included = (onlyExclude || filters.Any(rgx => rgx.IsMatch(type.FullName ?? string.Empty)));

                bool xcluded = excludes.Any(rgx => rgx.IsMatch(type.FullName ?? string.Empty));

                return included && !xcluded;
            });

            if (!commandsTypes.Any()) throw new InvalidClassException();

            return commandsTypes;
        }

        private static string WildcardsToRegex(string wildcard)
        {
            const string STAR_PLACEHOLDER = "<-starplaceholder->";

            return (@$"(.|^){wildcard}$")
                .Replace(".", @"\.")
                .Replace("??", ".")
                .Replace("?", @"\w")
                .Replace("**", @$".{STAR_PLACEHOLDER}")
                .Replace("*", @"\w*")
                .Replace(STAR_PLACEHOLDER, "*");
        }
    }
}