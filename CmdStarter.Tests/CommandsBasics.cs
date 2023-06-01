using Erroneous = com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous;
using System.Data;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.WrongClassTypes;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments.Child;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options;
using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using System.CommandLine;
using com.cyberinternauts.csharp.CmdStarter.Lib.Reflection;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.MultipleParentAttributes;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Children.DuplicatedChildren;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Description;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.Naming;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.TreeWithChildrenAttribute.ChildrenOfParent;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.TreeWithChildrenAttribute;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.Childing.Children.SubChildren;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.Childing.Children;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.Childing;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Demo.Types;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.ByInterface;
using static com.cyberinternauts.csharp.CmdStarter.Tests.TestsCommon;
using com.cyberinternauts.csharp.CmdStarter.Lib.Loader;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
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
            starter = new CmdStarter.Lib.Starter();
            starter.FindCommandsTypes();
            Assert.That(starter.CommandsTypes, Is.Not.Empty, "FIX THIS FIRST!!!!!", null);
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
        public void ThrowsOnDuplicateNames([Values]ClassesBuildingMode mode)
        {
            starter.ClassesBuildingMode = mode;
            // Normal case
            starter.InstantiateCommands();
            starter.VisitCommands(command => {
                var starterCommand = command as StarterCommand;
                if (starterCommand == null) return;

                var cmdString = starterCommand.GetFullCommandString();
                Assert.DoesNotThrowAsync(
                    async () => {
                        try
                        {
                            await starter.Start(cmdString.Split(" "));
                        }
                        catch (Exception ex)
                        {
                            // Missing required argument, normal as the cmdString doesn't include those
                            var parent = ex;
                            var isMissingArgumentException = false;
                            while (!isMissingArgumentException && parent != null)
                            {
                                isMissingArgumentException = parent is InvalidOperationException;
                                parent = parent.InnerException;
                            }
                            if (!isMissingArgumentException)
                            {
                                throw new Exception("Command: " + starterCommand.GetType().FullName, ex);
                            }
                        }
                    }
                );
            });

            // Error case
            starter.Namespaces = starter.Namespaces.Clear().Add(typeof(Commands.Erroneous.DuplicateNames.Same1).Namespace ?? string.Empty);
            Assert.ThrowsAsync<ArgumentException>(
                async () => await starter.Start(Array.Empty<string>())
            );
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
        public void EnsuresOrderViaTestParentAttribute([Values]ClassesBuildingMode classesBuildingMode)
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
        public void IsLonelyCommandRooted([Values] bool isRootingLonelyCommand)
        {
            starter.IsRootingLonelyCommand = isRootingLonelyCommand;
            starter.Classes = starter.Classes.Add(typeof(FullArgs).FullName!);
            starter.InstantiateCommands();

            Assert.That(starter.RootCommand.Subcommands, Has.Count.EqualTo(1));

            var command = starter.FindCommand<FullArgs>() as FullArgs;
            Assert.That(command, Is.Not.Null);

            var receptable = isRootingLonelyCommand ? (Command)starter.RootCommand : command!; // Null test was already applied

            // Ensure arguments
            AssertArguments(command, receptable);

            // Ensure options
            AssertOptionsPresence(command, receptable);

            // Ensure handle
            var optionValue = command.MyOpt + 1;
            var commandName = isRootingLonelyCommand ? string.Empty : typeof(FullArgs).Name.PascalToKebabCase();
            var successArgs = commandName + (isRootingLonelyCommand ? string.Empty : " ") // Command name
                + "--my-opt " + optionValue // Options
                + " my 222"; // Arguments  ==> FROM FullArgs: private void HandleExecution([Description("First param")] string param1, int param2, bool param3 = true)
            Assert.DoesNotThrowAsync(async () => await starter.Start(successArgs.Split(" ")));
            Assert.That(command.MyOpt, Is.EqualTo(optionValue));

            var failingArgs = (isRootingLonelyCommand ? Array.Empty<string>() : new string[] { commandName });
            Assert.That(async () => await starter.Start(failingArgs), Throws.Exception);
        }

        [TestCase<NoDesc>("")]
        [TestCase<SingleDesc>(SingleDesc.DESC)]
        [TestCase<MultipleDesc>(MultipleDesc.FIRST_DESC + StarterCommand.DESCRIPTION_JOINER + MultipleDesc.SECOND_DESC)]
        [TestCase<SingleDescByInterface>(SingleDescByInterface.DESC)]
        public void HasCommandDescription<DescClass>(string description) where DescClass : IStarterCommand
        {
            starter.InstantiateCommands();

            var command = starter.FindCommand<DescClass>() as StarterCommand;
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
}
