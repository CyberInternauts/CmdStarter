using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using com.cyberinternauts.csharp.CmdStarter.Lib;
using Erroneous = com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous;
using System.Data;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.WrongClassTypes;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments.Child;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options;
using Newtonsoft.Json;
using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    [Category("* All *")]
    [Category("CmdStarter")]
    [Category("Commands")]
    public class CommandsBasics
    {
        private CmdStarter.Lib.Starter starter;

        //TODO: Shall add tests when using attribute on wrong class type

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
        public void FindsCommandsForListing()
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(Commands.Listing.List).Namespace ?? string.Empty);
            starter.FindCommandsTypes();
            var listingCommands = starter.CommandsTypes;
            Assert.That(listingCommands, Has.Count.EqualTo(TestsCommon.NUMBER_OF_COMMANDS_IN_LISTING));

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
            AssertListing(null, null);
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
            AssertListing(null, starter.CommandsTypesTree);
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
            AssertListing(mainCommand, null);
            AssertTreeWithChildrenAttribute(mainCommand);
            AssertChilding(mainCommand, true);
        }


        [TestCase(typeof(Commands.Naming.Word), "Word", "word")]
        [TestCase(typeof(Commands.Naming.NameFor), "NameFor", "name-for")]
        [TestCase(typeof(Commands.Naming.NameOverride), "NameOverride", "name-overriden")]
        [TestCase(typeof(Commands.Naming.NameKebab), "NameKebab", "name-to-kebab")]
        public void EnsuresKebabCase(Type commandTypeToTest, string originalName, string expectedName)
        {
            Assert.That(commandTypeToTest.Name, Is.EqualTo(originalName)); // This ensures to adjust the test if command name has changed

            var commandToTest = Activator.CreateInstance(commandTypeToTest) as StarterCommand;
            Assert.That(commandToTest?.Name, Is.EqualTo(expectedName));
        }

        [Test]
        public void ThrowsOnDuplicateNames([Values]ClassesBuildingMode mode)
        {
            starter.ClassesBuildingMode = mode;
            // Normal case
            starter.InstantiateCommands();
            starter.VisitCommands(command => {
                if (command is StarterCommand starterCommand)
                {
                    var cmdString = starterCommand.GetFullCommandString();
                    Assert.DoesNotThrow(
                        () => {
                            try
                            {
                                starter.Start(cmdString.Split(" ")).Wait();
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
                    );;
                }
            });

            // Error case
            starter.Namespaces = starter.Namespaces.Clear().Add(typeof(Commands.Erroneous.DuplicateNames.Same1).Namespace ?? string.Empty);
            Assert.Throws<AggregateException>(
                () => starter.Start(Array.Empty<string>()).Wait()
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
        public void EnsuresTypesTreeIsReadOnly()
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
            Assert.Fail("NOT DONE - Should not fail");
        }

        [Test]
        public void ThrowsOnDuplicateParentAttribute()
        {
            Assert.Fail("NOT DONE");
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
        public void FindsCommand<CommandType>() where CommandType : StarterCommand
        {
            starter.InstantiateCommands();
            Assert.That(starter.FindCommand<CommandType>(), Is.Not.Null);
        }

        [TestCase(nameof(OptHandling.MyOptInt), OptHandling.MY_OPT_INT_KEBAB, 111, 999)]
        [TestCase(nameof(OptHandling.MyOptBool), OptHandling.MY_OPT_BOOL_KEBAB, false, true)]
        [TestCase(nameof(OptHandling.MyOptListInt), OptHandling.MY_OPT_LIST_INT_KEBAB, null, new int[] {11, 22})] 
        public async Task IsPropertyFilledWithOption(string propertyName, string optionName, object defaultValue, object expectedValue)
        {
            var propertyTested = typeof(OptHandling).GetProperty(propertyName);

            // Test without option for default value
            starter.Namespaces = starter.Namespaces.Add(typeof(OptHandling).Namespace!);
            await starter.Start(new string[] { nameof(OptHandling).PascalToKebabCase() });
            var optionCommand = starter.FindCommand<OptHandling>() as OptHandling;
            Assert.That(optionCommand, Is.Not.Null);
            Assert.That(propertyTested!.GetValue(optionCommand), Is.EqualTo(defaultValue));

            // Test with option
            starter = TestsCommon.CreateCmdStarter();
            starter.Namespaces = starter.Namespaces.Add(typeof(OptHandling).Namespace!);

            var argsString = nameof(OptHandling).PascalToKebabCase() + " " + TestsCommon.PrintOption(optionName, expectedValue);
            await starter.Start(argsString.Split(" "));

            optionCommand = starter.FindCommand<OptHandling>() as OptHandling;
            Assert.That(optionCommand, Is.Not.Null);
            Assert.That(propertyTested!.GetValue(optionCommand), Is.EqualTo(expectedValue));
        }

        [TestCase<OptComplete>(OptComplete.INT_OPT_KEBAB, true)]
        [TestCase<OptComplete>(OptComplete.STRING_OPT_KEBAB, false)]
        public void HasRequiredOption<OptClass>(string optionName, bool isRequired) where OptClass : StarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(OptClass).Namespace!);
            starter.InstantiateCommands();

            var optionCommand = starter.FindCommand<OptClass>() as OptClass;
            Assert.That(optionCommand, Is.Not.Null);

            var option = optionCommand.Options.FirstOrDefault(o => o.Name == optionName);
            Assert.That(option, Is.Not.Null);
            Assert.That(option.IsRequired, Is.EqualTo(isRequired));
        }

        [TestCase<OptComplete>(OptComplete.INT_OPT_KEBAB, "")]
        [TestCase<OptComplete>(OptComplete.STRING_OPT_KEBAB, OptComplete.STRING_OPT_DESC)]
        public void HasOptionDescription<OptClass>(string optionName, string description) where OptClass : StarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(OptClass).Namespace!);
            starter.InstantiateCommands();

            var optionCommand = starter.FindCommand<OptClass>() as OptClass;
            Assert.That(optionCommand, Is.Not.Null);

            var option = optionCommand.Options.FirstOrDefault(o => o.Name == optionName);
            Assert.That(option, Is.Not.Null);
            Assert.That(option.Description, Is.EqualTo(description));
        }

        [TestCase<OptComplete>(OptComplete.INT_OPT_KEBAB, true)]
        [TestCase<OptComplete>(OptComplete.STRING_OPT_KEBAB, true)]
        [TestCase<OptComplete>(OptComplete.DATE_OPT_KEBAB, true)]
        [TestCase<OptComplete>(OptComplete.PRIVATE_OPT_KEBAB, false)]
        [TestCase<OptComplete>(OptComplete.PROTECTED_OPT_KEBAB, false)]
        [TestCase<OptComplete>(OptComplete.READ_ONLY_OPT_KEBAB, false)]
        [TestCase<OptComplete>(OptComplete.WRITE_ONLY_OPT_KEBAB, false)]
        [TestCase<OptComplete>(OptComplete.STATIC_OPT_KEBAB, false)]
        [TestCase<OptCompleteDerived>(OptComplete.INT_OPT_KEBAB, true)]
        [TestCase<OptCompleteDerived>(OptComplete.STRING_OPT_KEBAB, true)]
        [TestCase<OptCompleteDerived>(OptComplete.DATE_OPT_KEBAB, true)]
        [TestCase<OptCompleteDerived>(OptCompleteDerived.NEW_INT_OPT_KEBAB, true)]
        public void EnsuresOptionsAreProperlyCreated<OptClass>(string optionName, bool shallBePresent) where OptClass : StarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(OptClass).Namespace!);
            starter.InstantiateCommands();

            var optionCommand = starter.FindCommand<OptClass>() as OptClass;
            Assert.That(optionCommand, Is.Not.Null);

            var option = optionCommand.Options.FirstOrDefault(o => o.Name == optionName);
            if (shallBePresent)
            {
                Assert.That(option, Is.Not.Null);
            } 
            else
            {
                Assert.That(option, Is.Null);
            }
        }

        [TestCase<FullArgs>]
        [TestCase<NoArgs>]
        [TestCase<ArgParent>]
        [TestCase<ArgChild>]
        public void EnsuresArgumentsAreProperlyCreated<CommandType>() where CommandType : StarterCommand
        {
            starter.Namespaces = starter.Namespaces.Clear().Add(typeof(CommandType).Namespace!);
            starter.InstantiateCommands();

            var command = starter.FindCommand<CommandType>() as StarterCommand;
            Assert.That(command, Is.Not.Null);

            if (command.Subcommands.Count > 0) // Not a leaf, arguments can't be used
            {
                Assert.That(command.Arguments, Has.Count.EqualTo(0));
                return;
            }

            var parameters = command.MethodForHandling.Method.GetParameters();
            Assert.That(parameters, Is.Not.Null);
            Assert.That(command.Arguments, Has.Count.EqualTo(parameters.Length));

            Assert.Multiple(() =>
            {
                var index = 0;
                foreach (var parameter in parameters)
                {
                    var description = (parameter.GetCustomAttributes(false)
                        .FirstOrDefault(a => a is System.ComponentModel.DescriptionAttribute) as System.ComponentModel.DescriptionAttribute)
                        ?.Description;

                    var message = () => "Error for parameter:" + parameter.Name;
                    var arg = command.Arguments[index];
                    Assert.That(arg.Name, Is.EqualTo(parameter.Name), message);
                    if (description != null)
                    {
                        Assert.That(arg.Description, Is.EqualTo(description), message);
                    }
                    else
                    {
                        Assert.That(String.IsNullOrEmpty(arg.Description), Is.True, message);
                    }
                    if (parameter.DefaultValue is not System.DBNull)
                    {
                        Assert.That(arg.HasDefaultValue, Is.True, message);
                        Assert.That(arg.GetDefaultValue(), Is.EqualTo(parameter.DefaultValue), message);
                    }
                    else
                    {
                        Assert.That(arg.HasDefaultValue, Is.False, message);
                    }

                    index++;
                }
            });
        }


        private static TreeNode<Type>? GetSubType(TreeNode<Type> commandNode, Type subCommandType)
        {
            return commandNode.Children.FirstOrDefault(c => c.Value?.Equals(subCommandType) ?? false);
        }

        private void AssertListing(TreeNode<Type>? parentOfList, TreeNode<Type>? parentOfFiles)
        {
            parentOfList ??= starter.CommandsTypesTree;
            var listCommand = GetSubType(parentOfList, typeof(Commands.Listing.List));
            Assert.That(listCommand, Is.Not.Null);

            parentOfFiles ??= listCommand;
            var filesCommand = GetSubType(parentOfFiles!, typeof(Commands.Listing.Types.Files)); // "!" used because can't be null here due to Assert above
            Assert.That(filesCommand, Is.Not.Null);

            var foldersCommand = GetSubType(listCommand, typeof(Commands.Listing.Types.Folders));
            Assert.That(foldersCommand, Is.Not.Null);
        }
        private void AssertTreeWithChildrenAttribute(TreeNode<Type>? parentOfParent)
        {
            parentOfParent ??= starter.CommandsTypesTree;
            var parentCommand = GetSubType(parentOfParent, typeof(Commands.TreeWithChildrenAttribute.ParentWithChildren));
            Assert.That(parentCommand, Is.Not.Null);

            var childNoParent = GetSubType(parentCommand!, typeof(Commands.TreeWithChildrenAttribute.ChildrenOfParent.ChildNoParent)); // "!" used because can't be null here due to Assert above
            Assert.That(childNoParent, Is.Not.Null);

            var childWithParent = GetSubType(parentCommand, typeof(Commands.TreeWithChildrenAttribute.ChildrenOfParent.ChildWithParent));
            Assert.That(childWithParent, Is.Not.Null);
        }

        private void AssertChilding(TreeNode<Type>? parentOfParent, bool isSubChild1ASub)
        {
            parentOfParent ??= starter.CommandsTypesTree;
            var parentCommand = GetSubType(parentOfParent, typeof(Commands.Childing.ChildingParent));
            Assert.That(parentCommand, Is.Not.Null);

            var child1Command = GetSubType(parentCommand, typeof(Commands.Childing.Children.Child1));
            Assert.That(child1Command, Is.Not.Null);

            var parentOfSubChild1 = (isSubChild1ASub ? child1Command : parentCommand);
            var subChild1Command = GetSubType(parentOfSubChild1!, typeof(Commands.Childing.Children.SubChildren.SubChild1)); // "!" used because can't be null here due to Assert above
            Assert.That(subChild1Command, Is.Not.Null);
        }
    }
}
