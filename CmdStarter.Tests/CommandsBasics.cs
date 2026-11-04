using Erroneous = CyberInternauts.CmdStarter.Tests.Commands.Erroneous;
using System.Data;
using CyberInternauts.CmdStarter.Tests.Common.TestsCommandsAttributes;
using CyberInternauts.CmdStarter.Tests.Commands.Erroneous.WrongClassTypes;
using CyberInternauts.CmdStarter.Tests.Commands.Arguments;
using CyberInternauts.CmdStarter.Tests.Commands.Arguments.Child;
using CyberInternauts.CmdStarter.Tests.Commands.Options;
using CyberInternauts.CmdStarter.Extensions;
using System.CommandLine;
using CyberInternauts.CmdStarter.Reflection;
using CyberInternauts.CmdStarter.Tests.Commands.Erroneous.MultipleParentAttributes;
using CyberInternauts.CmdStarter.Tests.Commands;
using CyberInternauts.CmdStarter.Tests.Commands.Attributes.Children.DuplicatedChildren;
using CyberInternauts.CmdStarter.Tests.Commands.Attributes.Description;
using CyberInternauts.CmdStarter.Tests.Commands.Loader.Naming;
using CyberInternauts.CmdStarter.Tests.Commands.Loader.TreeWithChildrenAttribute.ChildrenOfParent;
using CyberInternauts.CmdStarter.Tests.Commands.Loader.TreeWithChildrenAttribute;
using CyberInternauts.CmdStarter.Tests.Commands.Loader.Childing.Children.SubChildren;
using CyberInternauts.CmdStarter.Tests.Commands.Loader.Childing.Children;
using CyberInternauts.CmdStarter.Tests.Commands.Loader.Childing;
using CyberInternauts.CmdStarter.Tests.Commands.Demo.Types;
using CyberInternauts.CmdStarter.Tests.Commands.Loader.ByInterface;
using static CyberInternauts.CmdStarter.Tests.TestsCommon;
using CyberInternauts.CmdStarter.Loader;
using CyberInternauts.CmdStarter.Tests.Commands.Execution;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("CmdStarter")]
[Category("Commands")]
public class CommandsBasics
{
    private Starter starter;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        TestsCommon.GlobalSetup();
    }

    [SetUp]
    public void MethodSetup()
    {
        starter = TestsCommon.CreateCmdStarter();
    }

    [Test]
    public void HasCommands()
    {
        // All methods have to ensure commands where found
        starter = new CmdStarter.Starter();
        starter.FindCommandsTypes();
        Assert.That(starter.CommandsTypes, Is.Not.Empty, "FIX THIS FIRST!!!!!");
    }

    [Test]
    public void FindsCommandsForDemo()
    {
        starter.Namespaces = starter.Namespaces.Add(typeof(Commands.Demo.List).Namespace ?? string.Empty);
        starter.FindCommandsTypes();
        var listingCommands = starter.CommandsTypes;
        Assert.That(listingCommands, Has.Count.EqualTo(TestsCommon.NUMBER_OF_COMMANDS_IN_DEMO));

        starter.Namespaces = starter.Namespaces.Clear();
        starter.FindCommandsTypes();
        Assert.That(listingCommands, Has.Count.LessThan(starter.CommandsTypes.Count));
    }

    [Test]
    public void ThrowsUponLoopChildrenInTree()
    {
        Assert.DoesNotThrow(starter.BuildTree); // Ensure normal case doesn't throw an exception

        // Test erroneous simple case
        var namespaces = new string[] {
            typeof(Erroneous.LoopChildren.Parent.LoopParent).Namespace ?? string.Empty ,
            typeof(Erroneous.LoopChildren.Child.LoopChild).Namespace ?? string.Empty
        };
        starter.Namespaces = starter.Namespaces.Clear().AddRange(namespaces);
        Assert.Throws<LoopException>(starter.BuildTree);

        // Test erroneous multilevel case
        namespaces = new string[] {
            typeof(Erroneous.LoopChildrenMoreLevels.Parent.LoopParent).Namespace ?? string.Empty ,
            typeof(Erroneous.LoopChildrenMoreLevels.Child.LoopChild).Namespace ?? string.Empty ,
            typeof(Erroneous.LoopChildrenMoreLevels.ChildChild.LoopChildChild).Namespace ?? string.Empty
        };
        starter.Namespaces = starter.Namespaces.Clear().AddRange(namespaces);
        Assert.Throws<LoopException>(starter.BuildTree);
    }

    [Test]
    public void ThrowsUponLoopParentInTree()
    {
        Assert.DoesNotThrow(starter.BuildTree); // Ensure normal case doesn't throw an exception

        // Test erroneous multilevel case
        var namespaces = new string[] {
            typeof(Erroneous.LoopParentMoreLevels.Parent.LoopParent).Namespace ?? string.Empty ,
            typeof(Erroneous.LoopParentMoreLevels.Child.LoopChild).Namespace ?? string.Empty ,
            typeof(Erroneous.LoopParentMoreLevels.ChildChild.LoopChildChild).Namespace ?? string.Empty
        };
        starter.Namespaces = starter.Namespaces.Clear().AddRange(namespaces);
        Assert.Throws<LoopException>(starter.BuildTree);
    }

    [Test]
    public void IsCommandsTreeCorrectlyOrderedUsingBoth()
    {
        starter.ClassesBuildingMode = ClassesBuildingMode.Both;
        starter.BuildTree();

        // Main commands
        var mainCommand = GetSubType(starter.CommandsTypesTree, typeof(Commands.Main));
        Assert.That(mainCommand, Is.Not.Null);

        // Additionnal order validation tests by group
        AssertDemo(null, null);
        AssertTreeWithChildrenAttribute(null);
        AssertChilding(mainCommand, true);
    }

    [Test]
    public void IsCommandsTreeCorrectlyOrderedUsingAttribs()
    {
        starter.ClassesBuildingMode = ClassesBuildingMode.OnlyAttributes;
        starter.BuildTree();

        // Main commands
        var mainCommand = GetSubType(starter.CommandsTypesTree, typeof(Commands.Main));
        Assert.That(mainCommand, Is.Not.Null);

        // Additionnal order validation tests by group
        AssertDemo(null, starter.CommandsTypesTree);
        AssertTreeWithChildrenAttribute(null);
        AssertChilding(starter.CommandsTypesTree, false);
    }

    [Test]
    public void IsCommandsTreeCorrectlyOrderedUsingNamespaces()
    {
        starter.ClassesBuildingMode = ClassesBuildingMode.OnlyNamespaces;
        starter.BuildTree();

        // Main commands
        var mainCommand = GetSubType(starter.CommandsTypesTree, typeof(Commands.Main));
        Assert.That(mainCommand, Is.Not.Null);

        // Additionnal order validation tests by group
        AssertDemo(mainCommand, null);
        AssertTreeWithChildrenAttribute(mainCommand);
        AssertChilding(mainCommand, true);
    }

    [TestCase<Word>("Word", "word")]
    [TestCase<NameFor>("NameFor", "name-for")]
    [TestCase<NameOverride>("NameOverride", "name-overridden")]
    [TestCase<NameKebab>("NameKebab", "name-to-kebab")]
    [TestCase<FirstByInterface>("FirstByInterface", "first-by-interface")]
    public void EnsuresKebabCase<CommandType>(string originalName, string expectedName) where CommandType : IStarterCommand
    {
        var commandTypeToTest = typeof(CommandType);
        Assert.That(commandTypeToTest.Name, Is.EqualTo(originalName)); // This ensures to adjust the test if command name has changed

        starter.Classes = starter.Classes.Add(commandTypeToTest.FullName!);
        starter.InstantiateCommands();

        var command = starter.FindCommand<CommandType>();
        Assert.That(command, Is.Not.Null);
        Assert.That(command?.Name, Is.EqualTo(expectedName));
    }

    [Test]
    public void LookupsOfUnknownCommandsFindNothing()
    {
        starter.Classes = starter.Classes.Add(typeof(ExecSum).FullName!);
        starter.InstantiateCommands();

        Assert.Multiple(() =>
        {
            Assert.That(starter.FindCommand<ExecArgumentTypes>(), Is.Null);
            Assert.That(starter.GetFullCommandString<ExecArgumentTypes>(), Is.Null);
            Assert.That(starter.FindCommandInstance<ExecArgumentTypes>(), Is.Null);
            Assert.That(starter.FindCommandInstance(new Command("unknown")), Is.Null);
        });
    }

    [Test]
    public void VisitingNoCommandVisitsNothing()
    {
        var visited = 0;

        Starter.VisitCommands(null!, _ => visited++);

        Assert.That(visited, Is.Zero);
    }

    [Test]
    public void NullFilterListsMeanNoFilter()
    {
        starter.Assemblies = null!;
        starter.Classes = null!;
        starter.CommandsTypes = null!;

        Assert.Multiple(() =>
        {
            Assert.That(starter.Assemblies, Is.Empty);
            Assert.That(starter.Classes, Is.Empty);
            Assert.That(starter.CommandsTypes, Is.Empty);
        });
    }

    [Test]
    public void ThrowsOnDuplicateNames([Values] ClassesBuildingMode mode)
    {
        starter.ClassesBuildingMode = mode;
        // Normal case
        starter.InstantiateCommands();
        starter.VisitCommands(command =>
        {
            var commandInstance = starter.FindCommandInstance(command);
            if (commandInstance is not StarterCommand) return;

            var commandNames = new List<string> { command.Name };
            var parentCommand = command.Parents.FirstOrDefault();
            while (parentCommand != null)
            {
                commandNames.Add(parentCommand.Name);
                parentCommand = parentCommand.Parents.FirstOrDefault();
            }
            commandNames.Reverse();
            var cmdString = string.Join(" ", commandNames.Skip(1));
            Assert.DoesNotThrowAsync(async () => await starter.Start(cmdString.Split(" ")));
        });

        // Error case
        starter.Namespaces = starter.Namespaces.Clear().Add(typeof(Commands.Erroneous.DuplicateNames.Same1).Namespace ?? string.Empty);
        Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
    }

    [Test]
    public void ChangesOnBuildingModeEmptiesTrees()
    {
        // Initial
        TestsCommon.AssertCommandsAreEmpty(starter);

        // Normal after building tree
        starter.ClassesBuildingMode = ClassesBuildingMode.Both;
        starter.InstantiateCommands();
        TestsCommon.AssertCommandsExists(starter);

        // Emptied after change
        starter.ClassesBuildingMode = ClassesBuildingMode.OnlyAttributes;
        TestsCommon.AssertCommandsAreEmpty(starter, false);
    }

    [Test]
    public void ThrowsUponReadOnlyTypesTreeModification()
    {
        starter.BuildTree();
        var newNode = new TreeNode<Type>(this.GetType());
        Assert.Throws<ReadOnlyException>(() => starter.CommandsTypesTree.AddChild(newNode));
    }

    [Test]
    public void EnsuresOrderViaTestParentAttribute([Values] ClassesBuildingMode classesBuildingMode)
    {
        starter.ClassesBuildingMode = classesBuildingMode;
        starter.InstantiateCommands();
        var typesToVerify = starter.CommandsTypesTree.FlattenNodes().Where(n =>
            n.Value?.GetCustomAttributes(false).Any(a => a is TestParentAttribute aa && aa.BuildingMode == classesBuildingMode) ?? false
        );
        if (!typesToVerify.Any())
        {
            Assert.Warn("There is no " + nameof(TestParentAttribute) + " set on commands");
        }

        foreach (var typeNode in typesToVerify)
        {
            var parentAttrib = typeNode.Value?.GetCustomAttributes(false)
                .FirstOrDefault(a => a is TestParentAttribute aa && aa.BuildingMode == classesBuildingMode) as TestParentAttribute;
            var parentExpectation = parentAttrib?.Parent;
            var realParent = typeNode.Parent?.Value;

            Assert.That(realParent, Is.EqualTo(parentExpectation));
        }
    }

    [Test]
    public void EnsuresTwoChildrenAttributeWithSameNamespace()
    {
        starter.Namespaces = starter.Namespaces.Add(typeof(DuplicateOnSame).Namespace!);
        Assert.DoesNotThrow(starter.InstantiateCommands);
    }

    [Test]
    public void ThrowsOnDuplicateParentAttribute()
    {
        // Normal case
        Assert.DoesNotThrow(starter.BuildTree);

        // Erroneous case
        starter = TestsCommon.CreateCmdStarter();
        starter.Namespaces = starter.Namespaces.Clear(); // Allow Erroneous namespace
        starter.Classes = starter.Classes.AddRange(new string[] {
            typeof(MultiParent).FullName!,
            typeof(Main).FullName!,
            typeof(Folders).FullName!
        });

        Assert.Throws<InvalidAttributeException>(starter.BuildTree);
    }

    [Test]
    public void EnsuresNoWrongClassesTypes()
    {
        starter.Namespaces = starter.Namespaces.Clear().Add(typeof(GoodForEase).Namespace!);
        starter.InstantiateCommands();

        Assert.That(starter.CommandsTypes, Has.None.EqualTo(typeof(WrongTypeForParent)));
        Assert.That(starter.CommandsTypes, Has.None.EqualTo(typeof(WrongTypeForChidlren)));
        Assert.That(starter.CommandsTypes, Has.None.EqualTo(typeof(WrongTypeForGlobalOption)));

        Assert.That(starter.CommandsTypes.Where(t => !t.IsSubclassOf(typeof(StarterCommand))), Is.Empty); // Generic validation
    }

    [TestCase<ArgParent>]
    [TestCase<ArgChild>]
    [TestCase<FirstByInterface>]
    [TestCase<SecondByInterface>]
    public void FindsCommand<CommandType>() where CommandType : IStarterCommand
    {
        starter.InstantiateCommands();
        Assert.That(starter.FindCommand<CommandType>(), Is.Not.Null);
    }

    [Test]
    public async Task IsLonelyCommandRooted([Values] bool isRootingLonelyCommand)
    {
        starter.IsRootingLonelyCommand = isRootingLonelyCommand;
        starter.Classes = starter.Classes.Add(typeof(FullArgs).FullName!);
        starter.InstantiateCommands();

        Assert.That(starter.RootCommand.Subcommands, Has.Count.EqualTo(1));

        var command = starter.FindCommandInstance<FullArgs>();
        Assert.That(command, Is.Not.Null);

        var commandNode = starter.FindCommand<FullArgs>()!;

        var receptable = isRootingLonelyCommand ? (Command)starter.RootCommand : commandNode; // Null test was already applied

        // Ensure arguments
        AssertArguments(command, receptable);

        // Ensure options
        AssertOptionsPresence(command, receptable);

        // Ensure handle
        var optionValue = command.MyOpt + 1;
        FullArgs? executed = null;
        starter.CommandHandled += (_, e) => executed = e.Instance as FullArgs;
        var commandName = isRootingLonelyCommand ? string.Empty : typeof(FullArgs).Name.PascalToKebabCase();
        var successArgs = commandName + (isRootingLonelyCommand ? string.Empty : " ") // Command name
            + "--my-opt " + optionValue // Options
            + " my 222"; // Arguments  ==> FROM FullArgs: private void HandleExecution([Description("First param")] string param1, int param2, bool param3 = true)
        Assert.DoesNotThrowAsync(async () => await starter.Start(successArgs.Split(" ")));
        Assert.That(executed, Is.Not.Null);
        Assert.That(executed!.MyOpt, Is.EqualTo(optionValue));

        var failingArgs = (isRootingLonelyCommand ? Array.Empty<string>() : new string[] { commandName });
        Assert.That(await starter.Start(failingArgs), Is.Not.EqualTo(0));
    }

    [TestCase<NoDesc>("")]
    [TestCase<SingleDesc>(SingleDesc.DESC)]
    [TestCase<MultipleDesc>(MultipleDesc.FIRST_DESC + StarterCommand.DescriptionJoiner + MultipleDesc.SECOND_DESC)]
    [TestCase<SingleDescByInterface>(SingleDescByInterface.DESC)]
    [TestCase<ConstructorDesc>(ConstructorDesc.DESC)]
    [TestCase<ConstructorAndAttributeDesc>(ConstructorAndAttributeDesc.ATTRIBUTE_DESC)]
    public void HasCommandDescription<DescClass>(string description) where DescClass : IStarterCommand
    {
        starter.InstantiateCommands();

        var command = starter.FindCommand<DescClass>();
        Assert.That(command, Is.Not.Null);
        Assert.That(command.Description, Is.EqualTo(description));
    }

    private static TreeNode<Type>? GetSubType(TreeNode<Type> commandNode, Type subCommandType)
    {
        return commandNode.Children.FirstOrDefault(c => c.Value?.Equals(subCommandType) ?? false);
    }

    private void AssertDemo(TreeNode<Type>? parentOfList, TreeNode<Type>? parentOfFiles)
    {
        parentOfList ??= starter.CommandsTypesTree;
        var listCommand = GetSubType(parentOfList, typeof(Commands.Demo.List));
        Assert.That(listCommand, Is.Not.Null);

        parentOfFiles ??= listCommand;
        var filesCommand = GetSubType(parentOfFiles!, typeof(Files)); // "!" used because can't be null here due to Assert above
        Assert.That(filesCommand, Is.Not.Null);

        var foldersCommand = GetSubType(listCommand, typeof(Folders));
        Assert.That(foldersCommand, Is.Not.Null);
    }
    private void AssertTreeWithChildrenAttribute(TreeNode<Type>? parentOfParent)
    {
        parentOfParent ??= starter.CommandsTypesTree;
        var parentCommand = GetSubType(parentOfParent, typeof(ParentWithChildren));
        Assert.That(parentCommand, Is.Not.Null);

        var childNoParent = GetSubType(parentCommand!, typeof(ChildNoParent)); // "!" used because can't be null here due to Assert above
        Assert.That(childNoParent, Is.Not.Null);

        var childWithParent = GetSubType(parentCommand, typeof(ChildWithParent));
        Assert.That(childWithParent, Is.Not.Null);
    }

    private void AssertChilding(TreeNode<Type>? parentOfParent, bool isSubChild1ASub)
    {
        parentOfParent ??= starter.CommandsTypesTree;
        var parentCommand = GetSubType(parentOfParent, typeof(ChildingParent));
        Assert.That(parentCommand, Is.Not.Null);

        var child1Command = GetSubType(parentCommand, typeof(Child1));
        Assert.That(child1Command, Is.Not.Null);

        var parentOfSubChild1 = (isSubChild1ASub ? child1Command : parentCommand);
        var subChild1Command = GetSubType(parentOfSubChild1!, typeof(SubChild1)); // "!" used because can't be null here due to Assert above
        Assert.That(subChild1Command, Is.Not.Null);
    }
}
